using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tooark.Exceptions;
using Tooark.Secrets.Vault.Options;

namespace Tooark.Secrets.Vault.Clients;

/// <summary>
/// Cliente mínimo do KV versão 2 do HashiCorp Vault e do OpenBao, sobre a API HTTP.
/// </summary>
/// <remarks>
/// Cobre só o que os segredos usam: ler e listar no KV, e autenticar por token, AppRole ou Kubernetes. O token de
/// AppRole e de Kubernetes é obtido no primeiro uso e renovado com um novo login quando o cofre o recusa.
/// </remarks>
internal sealed class VaultClient : IDisposable
{
  #region Private Fields

  /// <summary>
  /// Cliente HTTP apontado para o endereço do cofre.
  /// </summary>
  private readonly HttpClient _http;

  /// <summary>
  /// As opções já validadas.
  /// </summary>
  private readonly VaultSecretsOptions _options;

  /// <summary>
  /// Garante um login por vez.
  /// </summary>
  private readonly SemaphoreSlim _login = new(1, 1);

  /// <summary>
  /// Token em uso.
  /// </summary>
  private string? _token;

  #endregion

  #region Constructor

  /// <summary>
  /// Cria o cliente a partir das opções.
  /// </summary>
  /// <param name="options">As opções já validadas.</param>
  /// <param name="handler">O handler HTTP. Opcional: sem ele, vale o padrão do .NET.</param>
  internal VaultClient(VaultSecretsOptions options, HttpMessageHandler? handler = null)
  {
    _options = options;
    _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
    _http.BaseAddress = new Uri(options.Address!.TrimEnd('/') + "/");

    // No método Token não há login: o token das opções vale desde já
    if (options.IsMethod("Token"))
    {
      _token = options.Token;
    }
  }

  #endregion

  #region Methods

  /// <summary>
  /// Lê os campos da versão atual de um segredo.
  /// </summary>
  /// <param name="path">O caminho do segredo no KV.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O objeto com os campos, ou nulo quando o segredo não existe.</returns>
  /// <exception cref="InternalServerErrorException">Quando o acesso é negado ou o cofre falha.</exception>
  internal async Task<JsonElement?> ReadAsync(string path, CancellationToken cancellationToken)
  {
    using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, KvUrl("data", path)), cancellationToken);

    if (response.StatusCode == HttpStatusCode.NotFound)
    {
      return null;
    }

    using var document = await ReadJsonAsync(response, cancellationToken);

    // A resposta do KV v2 traz os campos em data.data; um segredo excluído vem com data nulo
    return document.RootElement.TryGetProperty("data", out var envelope) &&
      envelope.ValueKind == JsonValueKind.Object &&
      envelope.TryGetProperty("data", out var data) &&
      data.ValueKind == JsonValueKind.Object
        ? data.Clone()
        : null;
  }

  /// <summary>
  /// Lista as chaves abaixo de um caminho. As que terminam em <c>/</c> são pastas.
  /// </summary>
  /// <param name="path">O caminho no KV.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>As chaves, ou lista vazia quando não há nada abaixo do caminho.</returns>
  /// <exception cref="InternalServerErrorException">Quando o acesso é negado ou o cofre falha.</exception>
  internal async Task<IReadOnlyList<string>> ListAsync(string path, CancellationToken cancellationToken)
  {
    // O GET com list=true equivale ao verbo LIST, e passa por proxies que não conhecem o verbo
    using var response = await SendAsync(
      () => new HttpRequestMessage(HttpMethod.Get, KvUrl("metadata", path) + "?list=true"),
      cancellationToken
    );

    if (response.StatusCode == HttpStatusCode.NotFound)
    {
      return [];
    }

    using var document = await ReadJsonAsync(response, cancellationToken);

    if (!document.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("keys", out var keys))
    {
      return [];
    }

    return [.. keys.EnumerateArray().Select(key => key.GetString()).OfType<string>()];
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    _http.Dispose();
    _login.Dispose();
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Envia uma requisição com o token, repetindo uma vez com novo login quando o cofre recusa o token.
  /// </summary>
  /// <param name="request">Monta a requisição; chamada de novo na repetição.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>A resposta com sucesso ou 404.</returns>
  /// <exception cref="InternalServerErrorException">Quando o acesso é negado ou o cofre falha.</exception>
  private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> request, CancellationToken cancellationToken)
  {
    var token = _token ?? await LoginAsync(null, cancellationToken);
    var response = await SendWithTokenAsync(request(), token, cancellationToken);

    // O token de AppRole e de Kubernetes expira: um novo login resolve; o do método Token não se renova
    if (response.StatusCode == HttpStatusCode.Forbidden && !_options.IsMethod("Token"))
    {
      response.Dispose();
      token = await LoginAsync(token, cancellationToken);
      response = await SendWithTokenAsync(request(), token, cancellationToken);
    }

    if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
    {
      return response;
    }

    var status = response.StatusCode;
    response.Dispose();

    throw status == HttpStatusCode.Forbidden
      ? new InternalServerErrorException("Secrets.AccessDenied")
      : new InternalServerErrorException("Secrets.OperationFailed", new HttpRequestException(null, null, status));
  }

  /// <summary>
  /// Envia a requisição com o token e o namespace.
  /// </summary>
  /// <param name="request">A requisição.</param>
  /// <param name="token">O token.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>A resposta.</returns>
  /// <exception cref="InternalServerErrorException">Quando o cofre não responde.</exception>
  private async Task<HttpResponseMessage> SendWithTokenAsync(HttpRequestMessage request, string token, CancellationToken cancellationToken)
  {
    using (request)
    {
      request.Headers.Add("X-Vault-Token", token);
      AddNamespace(request);

      try
      {
        return await _http.SendAsync(request, cancellationToken);
      }
      catch (HttpRequestException e)
      {
        throw new InternalServerErrorException("Secrets.OperationFailed", e);
      }
    }
  }

  /// <summary>
  /// Obtém um token pelo método de autenticação das opções.
  /// </summary>
  /// <param name="rejected">O token recusado, ou nulo no primeiro login.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O token em uso.</returns>
  /// <exception cref="InternalServerErrorException">Quando o cofre recusa o login ou o token do Kubernetes não existe.</exception>
  private async Task<string> LoginAsync(string? rejected, CancellationToken cancellationToken)
  {
    await _login.WaitAsync(cancellationToken);

    try
    {
      // Outra chamada já renovou o token enquanto esta esperava
      if (_token is not null && _token != rejected)
      {
        return _token;
      }

      var (defaultMount, body) = _options.IsMethod("AppRole")
        ? ("approle", (object)new { role_id = _options.RoleId, secret_id = _options.SecretId })
        : ("kubernetes", new { role = _options.Role, jwt = await ReadKubernetesTokenAsync(cancellationToken) });

      using var request = new HttpRequestMessage(HttpMethod.Post, $"v1/auth/{Escape(_options.AuthMount ?? defaultMount)}/login")
      {
        Content = JsonContent.Create(body)
      };
      AddNamespace(request);

      using var response = await _http.SendAsync(request, cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        throw new InternalServerErrorException("Secrets.Vault.LoginFailed");
      }

      using var document = await ReadJsonAsync(response, cancellationToken);

      _token = document.RootElement.GetProperty("auth").GetProperty("client_token").GetString()
        ?? throw new InternalServerErrorException("Secrets.Vault.LoginFailed");

      return _token;
    }
    catch (HttpRequestException e)
    {
      throw new InternalServerErrorException("Secrets.OperationFailed", e);
    }
    finally
    {
      _login.Release();
    }
  }

  /// <summary>
  /// Lê o token da conta de serviço do pod.
  /// </summary>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O token.</returns>
  /// <exception cref="InternalServerErrorException">Quando o arquivo do token não existe.</exception>
  private async Task<string> ReadKubernetesTokenAsync(CancellationToken cancellationToken)
  {
    if (!File.Exists(_options.KubernetesTokenPath))
    {
      throw new InternalServerErrorException($"Secrets.Vault.KubernetesTokenNotFound;{_options.KubernetesTokenPath}");
    }

    return (await File.ReadAllTextAsync(_options.KubernetesTokenPath, cancellationToken)).Trim();
  }

  /// <summary>
  /// Acrescenta o namespace, quando configurado.
  /// </summary>
  /// <param name="request">A requisição.</param>
  private void AddNamespace(HttpRequestMessage request)
  {
    if (!string.IsNullOrWhiteSpace(_options.Namespace))
    {
      request.Headers.Add("X-Vault-Namespace", _options.Namespace);
    }
  }

  /// <summary>
  /// Monta o endereço de uma operação do KV v2.
  /// </summary>
  /// <param name="operation">A operação: <c>data</c> ou <c>metadata</c>.</param>
  /// <param name="path">O caminho no KV.</param>
  /// <returns>O endereço relativo.</returns>
  private string KvUrl(string operation, string path) => $"v1/{Escape(_options.Mount)}/{operation}/{Escape(path)}";

  /// <summary>
  /// Escapa cada nível de um caminho, mantendo as barras entre eles.
  /// </summary>
  /// <param name="path">O caminho.</param>
  /// <returns>O caminho escapado.</returns>
  private static string Escape(string path) =>
    string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

  /// <summary>
  /// Lê o corpo JSON da resposta.
  /// </summary>
  /// <param name="response">A resposta.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O documento JSON.</returns>
  private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

    return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
  }

  #endregion
}
