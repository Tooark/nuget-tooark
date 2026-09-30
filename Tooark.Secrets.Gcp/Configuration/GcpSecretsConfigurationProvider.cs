using Tooark.Secrets.Configuration;
using Tooark.Secrets.Gcp.Clients;
using Tooark.Secrets.Gcp.Options;

namespace Tooark.Secrets.Gcp.Configuration;

/// <summary>
/// Fonte de configuração com os segredos do Secret Manager e os parâmetros do Parameter Manager.
/// </summary>
/// <param name="options">As opções já validadas.</param>
/// <param name="client">Cria as operações do Google Cloud, usadas só durante a carga.</param>
internal sealed class GcpSecretsConfigurationProvider(GcpSecretsOptions options, Func<IGcpSecretsClient> client)
  : SecretsConfigurationProvider(options, "Google Cloud")
{
  #region Protected Methods

  /// <inheritdoc/>
  protected override async Task<IReadOnlyList<SecretEntry>> LoadEntriesAsync(CancellationToken cancellationToken)
  {
    var gcp = client();
    var projectId = options.ProjectId!;
    var entries = new List<SecretEntry>();

    // Os parâmetros vêm antes: com o mesmo nome, o segredo, lido depois, vence
    if (!string.IsNullOrWhiteSpace(options.ParametersPrefix))
    {
      await foreach (var id in gcp.ListParameterIdsAsync(projectId, cancellationToken))
      {
        if (RelativeName(id, options.ParametersPrefix.Trim()) is { } relative &&
            await gcp.RenderLatestParameterAsync(projectId, id, cancellationToken) is { } value)
        {
          entries.Add(new SecretEntry(relative, value));
        }
      }
    }

    if (!string.IsNullOrWhiteSpace(options.SecretsPrefix))
    {
      await foreach (var id in gcp.ListSecretIdsAsync(projectId, cancellationToken))
      {
        if (RelativeName(id, options.SecretsPrefix.Trim()) is { } relative &&
            await gcp.AccessLatestSecretAsync(projectId, id, cancellationToken) is { } value)
        {
          entries.Add(new SecretEntry(relative, value));
        }
      }
    }

    return entries;
  }

  #endregion
}
