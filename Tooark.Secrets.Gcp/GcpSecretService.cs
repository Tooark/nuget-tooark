using Google.Cloud.SecretManager.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Secrets.Gcp.Clients;
using Tooark.Secrets.Gcp.Options;

namespace Tooark.Secrets.Gcp;

/// <summary>
/// Serviço que lê segredos do Google Cloud Secret Manager em tempo de execução, com cache.
/// </summary>
/// <remarks>
/// O nome é o identificador do segredo no projeto das opções, e vale a versão <c>latest</c>. Os parâmetros do
/// Parameter Manager não passam por aqui: são configuração, e entram pela fonte de configuração.
/// </remarks>
public sealed class GcpSecretService : SecretServiceBase
{
  #region Private Fields

  /// <summary>
  /// As operações do Google Cloud.
  /// </summary>
  private readonly IGcpSecretsClient _client;

  /// <summary>
  /// O projeto dos segredos.
  /// </summary>
  private readonly string _projectId;

  #endregion

  #region Constructors

  /// <summary>
  /// Cria o serviço sobre o cliente do Secret Manager informado.
  /// </summary>
  /// <param name="client">O cliente do Secret Manager.</param>
  /// <param name="options">As opções dos segredos no Google Cloud.</param>
  /// <param name="timeProvider">O relógio do cache. Opcional.</param>
  public GcpSecretService(SecretManagerServiceClient client, IOptions<GcpSecretsOptions> options, TimeProvider? timeProvider = null)
    : this(new GcpSecretsClient(client), options, timeProvider)
  {
  }

  /// <summary>
  /// Cria o serviço sobre as operações do Google Cloud.
  /// </summary>
  /// <param name="client">As operações do Google Cloud.</param>
  /// <param name="options">As opções dos segredos no Google Cloud.</param>
  /// <param name="timeProvider">O relógio do cache. Opcional.</param>
  internal GcpSecretService(IGcpSecretsClient client, IOptions<GcpSecretsOptions> options, TimeProvider? timeProvider = null)
    : base(options.Value, timeProvider)
  {
    _client = client;
    _projectId = options.Value.ProjectId!;
  }

  #endregion

  #region Protected Methods

  /// <inheritdoc/>
  protected override async Task<string?> ReadAsync(string name, CancellationToken cancellationToken)
  {
    try
    {
      return await _client.AccessLatestSecretAsync(_projectId, name, cancellationToken);
    }
    catch (RpcException e) when (e.StatusCode is StatusCode.PermissionDenied or StatusCode.Unauthenticated)
    {
      throw new InternalServerErrorException("Secrets.AccessDenied", e);
    }
    catch (RpcException e)
    {
      throw new InternalServerErrorException("Secrets.OperationFailed", e);
    }
  }

  #endregion
}
