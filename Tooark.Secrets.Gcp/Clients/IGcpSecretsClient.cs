namespace Tooark.Secrets.Gcp.Clients;

/// <summary>
/// Operações do Google Cloud usadas pelos segredos, isoladas dos tipos paginados do SDK.
/// </summary>
internal interface IGcpSecretsClient
{
  /// <summary>
  /// Lista o identificador dos segredos do projeto.
  /// </summary>
  /// <param name="projectId">O projeto.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Os identificadores, como <c>arkuest-prod__Jwt__Secret</c>.</returns>
  IAsyncEnumerable<string> ListSecretIdsAsync(string projectId, CancellationToken cancellationToken);

  /// <summary>
  /// Lê a versão <c>latest</c> de um segredo.
  /// </summary>
  /// <param name="projectId">O projeto.</param>
  /// <param name="secretId">O identificador do segredo.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O valor, ou nulo quando o segredo não existe ou não tem versão habilitada.</returns>
  Task<string?> AccessLatestSecretAsync(string projectId, string secretId, CancellationToken cancellationToken);

  /// <summary>
  /// Lista o identificador dos parâmetros do projeto, no local <c>global</c>.
  /// </summary>
  /// <param name="projectId">O projeto.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Os identificadores dos parâmetros.</returns>
  IAsyncEnumerable<string> ListParameterIdsAsync(string projectId, CancellationToken cancellationToken);

  /// <summary>
  /// Renderiza a versão habilitada mais recente de um parâmetro.
  /// </summary>
  /// <param name="projectId">O projeto.</param>
  /// <param name="parameterId">O identificador do parâmetro.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O valor renderizado, ou nulo quando o parâmetro não tem versão habilitada.</returns>
  Task<string?> RenderLatestParameterAsync(string projectId, string parameterId, CancellationToken cancellationToken);
}
