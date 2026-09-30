using Tooark.Exceptions;
using Tooark.Secrets.Options;

namespace Tooark.Secrets.Vault.Options;

/// <summary>
/// Classe que representa as opções dos segredos no HashiCorp Vault ou no OpenBao.
/// </summary>
/// <remarks>
/// Lidas da seção <c>Secrets</c>, junto com as opções comuns. O OpenBao mantém a API do Vault, então as mesmas
/// opções servem aos dois. Endereço, token e namespace ausentes são lidos das variáveis de ambiente do cliente de
/// linha de comando: <c>VAULT_ADDR</c>, <c>VAULT_TOKEN</c> e <c>VAULT_NAMESPACE</c>, ou <c>BAO_ADDR</c>,
/// <c>BAO_TOKEN</c> e <c>BAO_NAMESPACE</c>.
/// </remarks>
public class VaultSecretsOptions : SecretsOptions
{
  #region Constants

  /// <summary>
  /// Métodos de autenticação aceitos.
  /// </summary>
  private static readonly IReadOnlySet<string> AuthMethods =
    new HashSet<string>(["Token", "AppRole", "Kubernetes"], StringComparer.OrdinalIgnoreCase);

  #endregion

  #region Properties

  /// <summary>
  /// Endereço do cofre, como <c>https://vault.empresa.com:8200</c>. Obrigatório.
  /// </summary>
  public string? Address { get; set; }

  /// <summary>
  /// Namespace do cofre, no Vault Enterprise ou no OpenBao. Opcional.
  /// </summary>
  public string? Namespace { get; set; }

  /// <summary>
  /// Caminho de montagem do KV versão 2. Padrão: <c>secret</c>.
  /// </summary>
  public string Mount { get; set; } = "secret";

  /// <summary>
  /// Caminho no KV que vira configuração, como <c>arkuest/prod</c>. Obrigatório para a fonte de configuração.
  /// </summary>
  /// <remarks>
  /// O caminho é lido recursivamente. Cada campo de um segredo vira uma chave: o campo <c>SecretKey</c> do segredo
  /// <c>arkuest/prod/Storage</c> vira <c>Storage:SecretKey</c>, e os campos do segredo no próprio caminho ficam na raiz.
  /// </remarks>
  public string? Path { get; set; }

  /// <summary>
  /// Método de autenticação: <c>Token</c>, <c>AppRole</c> ou <c>Kubernetes</c>. Padrão: <c>Token</c>.
  /// </summary>
  public string AuthMethod { get; set; } = "Token";

  /// <summary>
  /// Caminho de montagem do método de autenticação. Opcional: sem ele, vale <c>approle</c> ou <c>kubernetes</c>.
  /// </summary>
  public string? AuthMount { get; set; }

  /// <summary>
  /// Token de acesso, no método <c>Token</c>.
  /// </summary>
  public string? Token { get; set; }

  /// <summary>
  /// Role ID, no método <c>AppRole</c>.
  /// </summary>
  public string? RoleId { get; set; }

  /// <summary>
  /// Secret ID, no método <c>AppRole</c>. Entregue pelo ambiente, e nunca pelo <c>appsettings.json</c> versionado.
  /// </summary>
  public string? SecretId { get; set; }

  /// <summary>
  /// Role do cofre, no método <c>Kubernetes</c>.
  /// </summary>
  public string? Role { get; set; }

  /// <summary>
  /// Caminho do token da conta de serviço do pod, no método <c>Kubernetes</c>. Padrão: o caminho montado pelo
  /// Kubernetes.
  /// </summary>
  public string KubernetesTokenPath { get; set; } = "/var/run/secrets/kubernetes.io/serviceaccount/token";

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções.
  /// </summary>
  /// <exception cref="InternalServerErrorException">
  /// Quando o endereço está ausente ou é inválido, o método de autenticação não existe ou faltam as credenciais dele.
  /// </exception>
  public override void Validate()
  {
    base.Validate();

    if (string.IsNullOrWhiteSpace(Address))
    {
      throw new InternalServerErrorException("Options.Secrets.Vault.AddressNotConfigured");
    }

    if (!(Uri.TryCreate(Address, UriKind.Absolute, out var url) && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp)))
    {
      throw new InternalServerErrorException($"Options.Secrets.Vault.AddressInvalid;{Address}");
    }

    if (!AuthMethods.Contains(AuthMethod ?? string.Empty))
    {
      throw new InternalServerErrorException($"Options.Secrets.Vault.AuthMethodInvalid;{AuthMethod}");
    }

    if (IsMethod("Token") && string.IsNullOrWhiteSpace(Token))
    {
      throw new InternalServerErrorException("Options.Secrets.Vault.TokenNotConfigured");
    }

    if (IsMethod("AppRole") && (string.IsNullOrWhiteSpace(RoleId) || string.IsNullOrWhiteSpace(SecretId)))
    {
      throw new InternalServerErrorException("Options.Secrets.Vault.AppRoleNotConfigured");
    }

    if (IsMethod("Kubernetes") && string.IsNullOrWhiteSpace(Role))
    {
      throw new InternalServerErrorException("Options.Secrets.Vault.KubernetesRoleNotConfigured");
    }
  }

  #endregion

  #region Internal Methods

  /// <summary>
  /// Indica se o método de autenticação configurado é o informado.
  /// </summary>
  /// <param name="method">O método.</param>
  /// <returns>Verdadeiro quando é o método configurado.</returns>
  internal bool IsMethod(string method) => string.Equals(AuthMethod, method, StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// Completa endereço, token e namespace ausentes com as variáveis de ambiente do Vault ou do OpenBao.
  /// </summary>
  /// <param name="environment">Lê uma variável de ambiente.</param>
  internal void ApplyEnvironment(Func<string, string?> environment)
  {
    Address = FirstFilled(Address, environment("VAULT_ADDR"), environment("BAO_ADDR"));
    Token = FirstFilled(Token, environment("VAULT_TOKEN"), environment("BAO_TOKEN"));
    Namespace = FirstFilled(Namespace, environment("VAULT_NAMESPACE"), environment("BAO_NAMESPACE"));
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Devolve o primeiro valor preenchido.
  /// </summary>
  /// <param name="values">Os valores, em ordem de preferência.</param>
  /// <returns>O primeiro valor preenchido, ou nulo.</returns>
  private static string? FirstFilled(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

  #endregion
}
