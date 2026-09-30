using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tooark.Tests.Secrets.Vault;

/// <summary>
/// Cofre em memória com as rotas do KV v2 e dos logins de AppRole e Kubernetes, como o Vault e o OpenBao respondem.
/// </summary>
internal sealed class CofreFalso : HttpMessageHandler
{
  // Segredos por caminho no mount "secret"
  public Dictionary<string, JsonObject> Segredos { get; } = new(StringComparer.Ordinal);

  // Tokens aceitos pelo cofre
  public HashSet<string> TokensValidos { get; } = ["token-raiz"];

  // Credenciais aceitas nos logins
  public (string RoleId, string SecretId) AppRole { get; set; } = ("role-arkuest", "secret-arkuest");
  public (string Role, string Jwt) Kubernetes { get; set; } = ("arkuest", "jwt-do-pod");

  // Status forçado em todas as leituras, para simular falha do cofre
  public HttpStatusCode? FalhaForcada { get; set; }

  // Quantidade de logins e os cabeçalhos recebidos
  public int Logins { get; private set; }
  public List<(string Caminho, string? Token, string? Namespace)> Requisicoes { get; } = [];

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    var caminho = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath);
    var token = request.Headers.TryGetValues("X-Vault-Token", out var tokens) ? tokens.Single() : null;
    var ns = request.Headers.TryGetValues("X-Vault-Namespace", out var nss) ? nss.Single() : null;
    Requisicoes.Add((caminho + request.RequestUri.Query, token, ns));

    // Logins
    if (request.Method == HttpMethod.Post && caminho.StartsWith("/v1/auth/", StringComparison.Ordinal))
    {
      var corpo = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
      var aceito = caminho switch
      {
        "/v1/auth/approle/login" => (string?)corpo["role_id"] == AppRole.RoleId && (string?)corpo["secret_id"] == AppRole.SecretId,
        "/v1/auth/kubernetes/login" => (string?)corpo["role"] == Kubernetes.Role && (string?)corpo["jwt"] == Kubernetes.Jwt,
        _ => false
      };

      if (!aceito)
      {
        return Resposta(HttpStatusCode.BadRequest, new { errors = new[] { "invalid credentials" } });
      }

      Logins++;
      var novo = $"token-login-{Logins}";
      TokensValidos.Add(novo);

      return Resposta(HttpStatusCode.OK, new { auth = new { client_token = novo } });
    }

    if (token is null || !TokensValidos.Contains(token))
    {
      return Resposta(HttpStatusCode.Forbidden, new { errors = new[] { "permission denied" } });
    }

    if (FalhaForcada is { } falha)
    {
      return Resposta(falha, new { errors = new[] { "falha" } });
    }

    // Leitura: GET /v1/secret/data/{caminho}
    if (caminho.StartsWith("/v1/secret/data/", StringComparison.Ordinal))
    {
      var segredo = caminho["/v1/secret/data/".Length..];

      return Segredos.TryGetValue(segredo, out var dados)
        ? Resposta(HttpStatusCode.OK, new { data = new { data = dados, metadata = new { version = 1 } } })
        : Resposta(HttpStatusCode.NotFound, new { errors = Array.Empty<string>() });
    }

    // Listagem: GET /v1/secret/metadata/{caminho}?list=true
    if (caminho.StartsWith("/v1/secret/metadata/", StringComparison.Ordinal) && request.RequestUri.Query == "?list=true")
    {
      var pasta = caminho["/v1/secret/metadata/".Length..].TrimEnd('/') + "/";
      var chaves = Segredos.Keys
        .Where(chave => chave.StartsWith(pasta, StringComparison.Ordinal))
        .Select(chave => chave[pasta.Length..])
        .Select(resto => resto.Contains('/') ? resto[..(resto.IndexOf('/') + 1)] : resto)
        .Distinct()
        .ToArray();

      return chaves.Length == 0
        ? Resposta(HttpStatusCode.NotFound, new { errors = Array.Empty<string>() })
        : Resposta(HttpStatusCode.OK, new { data = new { keys = chaves } });
    }

    return Resposta(HttpStatusCode.NotFound, new { errors = Array.Empty<string>() });
  }

  // Monta uma resposta JSON
  private static HttpResponseMessage Resposta(HttpStatusCode status, object corpo) =>
    new(status) { Content = new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json") };
}
