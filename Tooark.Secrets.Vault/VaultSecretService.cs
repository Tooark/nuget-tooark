using Microsoft.Extensions.Options;
using Tooark.Secrets.Vault.Clients;
using Tooark.Secrets.Vault.Options;

namespace Tooark.Secrets.Vault;

/// <summary>
/// Serviço que lê segredos do KV v2 do HashiCorp Vault ou do OpenBao em tempo de execução, com cache.
/// </summary>
/// <remarks>
/// O nome é o caminho do segredo no KV, como <c>arkuest/prod/tenants/acme</c>. O valor é o objeto JSON com os campos
/// do segredo; <see cref="SecretServiceBase.GetFieldAsync"/> lê um campo dele.
/// </remarks>
public sealed class VaultSecretService : SecretServiceBase, IDisposable
{
  #region Private Fields

  /// <summary>
  /// O cliente do cofre.
  /// </summary>
  private readonly VaultClient _client;

  #endregion

  #region Constructors

  /// <summary>
  /// Cria o serviço a partir das opções.
  /// </summary>
  /// <param name="options">As opções dos segredos no Vault.</param>
  /// <param name="timeProvider">O relógio do cache. Opcional.</param>
  public VaultSecretService(IOptions<VaultSecretsOptions> options, TimeProvider? timeProvider = null)
    : this(new VaultClient(options.Value), options, timeProvider)
  {
  }

  /// <summary>
  /// Cria o serviço sobre o cliente informado.
  /// </summary>
  /// <param name="client">O cliente do cofre.</param>
  /// <param name="options">As opções dos segredos no Vault.</param>
  /// <param name="timeProvider">O relógio do cache. Opcional.</param>
  internal VaultSecretService(VaultClient client, IOptions<VaultSecretsOptions> options, TimeProvider? timeProvider = null)
    : base(options.Value, timeProvider)
  {
    _client = client;
  }

  #endregion

  #region Methods

  /// <inheritdoc/>
  public void Dispose() => _client.Dispose();

  #endregion

  #region Protected Methods

  /// <inheritdoc/>
  protected override async Task<string?> ReadAsync(string name, CancellationToken cancellationToken) =>
    (await _client.ReadAsync(name.Trim('/'), cancellationToken))?.GetRawText();

  #endregion
}
