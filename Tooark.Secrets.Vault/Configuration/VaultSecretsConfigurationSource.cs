using Microsoft.Extensions.Configuration;
using Tooark.Secrets.Vault.Clients;
using Tooark.Secrets.Vault.Options;

namespace Tooark.Secrets.Vault.Configuration;

/// <summary>
/// Fonte de configuração do Vault e do OpenBao.
/// </summary>
/// <param name="options">As opções já validadas.</param>
/// <param name="handler">O handler HTTP. Opcional: sem ele, vale o padrão do .NET.</param>
internal sealed class VaultSecretsConfigurationSource(VaultSecretsOptions options, HttpMessageHandler? handler = null) : IConfigurationSource
{
  /// <inheritdoc/>
  public IConfigurationProvider Build(IConfigurationBuilder builder) =>
    new VaultSecretsConfigurationProvider(options, () => new VaultClient(options, handler));
}
