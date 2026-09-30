using System.Runtime.CompilerServices;
using Google.Api.Gax.Grpc;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.ParameterManager.V1;
using Google.Cloud.SecretManager.V1;
using Grpc.Core;
using Tooark.Exceptions;

namespace Tooark.Secrets.Gcp.Clients;

/// <summary>
/// Operações do Google Cloud sobre os clientes do Secret Manager e do Parameter Manager.
/// </summary>
/// <remarks>
/// Os clientes são criados no primeiro uso, com o Application Default Credentials. Um cliente do Secret Manager
/// informado pela aplicação é usado no lugar do criado.
/// </remarks>
/// <param name="secretManager">O cliente do Secret Manager. Opcional.</param>
internal sealed class GcpSecretsClient(SecretManagerServiceClient? secretManager = null) : IGcpSecretsClient
{
  #region Private Fields

  /// <summary>
  /// Cliente do Secret Manager, criado no primeiro uso.
  /// </summary>
  private readonly Lazy<SecretManagerServiceClient> _secretManager =
    new(() => secretManager ?? Create(() => SecretManagerServiceClient.Create()));

  /// <summary>
  /// Cliente do Parameter Manager, criado no primeiro uso.
  /// </summary>
  private readonly Lazy<ParameterManagerClient> _parameterManager = new(() => Create(() => ParameterManagerClient.Create()));

  #endregion

  #region Methods

  /// <inheritdoc/>
  public async IAsyncEnumerable<string> ListSecretIdsAsync(
    string projectId,
    [EnumeratorCancellation] CancellationToken cancellationToken
  )
  {
    var request = new ListSecretsRequest { ParentAsProjectName = ProjectName.FromProject(projectId) };

    await foreach (var secret in _secretManager.Value.ListSecretsAsync(request, CallSettings.FromCancellationToken(cancellationToken)))
    {
      yield return secret.SecretName.SecretId;
    }
  }

  /// <inheritdoc/>
  public async Task<string?> AccessLatestSecretAsync(string projectId, string secretId, CancellationToken cancellationToken)
  {
    try
    {
      var name = SecretVersionName.FromProjectSecretSecretVersion(projectId, secretId, "latest");
      var response = await _secretManager.Value.AccessSecretVersionAsync(name, cancellationToken);

      return response.Payload.Data.ToStringUtf8();
    }
    catch (RpcException e) when (e.StatusCode is StatusCode.NotFound or StatusCode.FailedPrecondition)
    {
      // Segredo inexistente, ou sem versão habilitada
      return null;
    }
  }

  /// <inheritdoc/>
  public async IAsyncEnumerable<string> ListParameterIdsAsync(
    string projectId,
    [EnumeratorCancellation] CancellationToken cancellationToken
  )
  {
    var request = new ListParametersRequest { ParentAsLocationName = LocationName.FromProjectLocation(projectId, "global") };

    await foreach (var parameter in _parameterManager.Value.ListParametersAsync(request, CallSettings.FromCancellationToken(cancellationToken)))
    {
      yield return parameter.ParameterName.ParameterId;
    }
  }

  /// <inheritdoc/>
  public async Task<string?> RenderLatestParameterAsync(string projectId, string parameterId, CancellationToken cancellationToken)
  {
    var request = new ListParameterVersionsRequest
    {
      ParentAsParameterName = ParameterName.FromProjectLocationParameter(projectId, "global", parameterId)
    };

    // O Parameter Manager não tem o apelido latest: vale a versão habilitada criada por último
    ParameterVersion? latest = null;

    await foreach (var version in _parameterManager.Value.ListParameterVersionsAsync(request, CallSettings.FromCancellationToken(cancellationToken)))
    {
      if (!version.Disabled && (latest is null || version.CreateTime.ToDateTimeOffset() > latest.CreateTime.ToDateTimeOffset()))
      {
        latest = version;
      }
    }

    if (latest is null)
    {
      return null;
    }

    // A renderização resolve as referências a segredos do Secret Manager
    var rendered = await _parameterManager.Value.RenderParameterVersionAsync(latest.Name, cancellationToken);

    return rendered.RenderedPayload.IsEmpty ? rendered.Payload?.Data.ToStringUtf8() : rendered.RenderedPayload.ToStringUtf8();
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Cria um cliente, trocando a falta de credencial no ambiente por um erro de configuração do Tooark.
  /// </summary>
  /// <typeparam name="T">O tipo do cliente.</typeparam>
  /// <param name="create">A criação do cliente.</param>
  /// <returns>O cliente.</returns>
  /// <exception cref="InternalServerErrorException">Quando não há Application Default Credentials no ambiente.</exception>
  private static T Create<T>(Func<T> create)
  {
    try
    {
      return create();
    }
    catch (InvalidOperationException e)
    {
      throw new InternalServerErrorException("Options.Secrets.Gcp.CredentialsUnavailable", e);
    }
  }

  #endregion
}
