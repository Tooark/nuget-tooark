using Microsoft.Extensions.Configuration;
using Tooark.Secrets.Gcp.Clients;
using Tooark.Secrets.Gcp.Options;

namespace Tooark.Secrets.Gcp.Configuration;

/// <summary>
/// Fonte de configuração do Google Cloud.
/// </summary>
/// <param name="options">As opções já validadas.</param>
/// <param name="client">Cria as operações do Google Cloud. Opcional: sem ele, valem os clientes do SDK.</param>
internal sealed class GcpSecretsConfigurationSource(GcpSecretsOptions options, Func<IGcpSecretsClient>? client = null) : IConfigurationSource
{
  /// <inheritdoc/>
  public IConfigurationProvider Build(IConfigurationBuilder builder) =>
    new GcpSecretsConfigurationProvider(options, client ?? (() => new GcpSecretsClient()));
}
