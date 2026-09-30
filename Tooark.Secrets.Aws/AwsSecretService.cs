using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Secrets.Aws.Options;

namespace Tooark.Secrets.Aws;

/// <summary>
/// Serviço que lê segredos do AWS Secrets Manager em tempo de execução, com cache.
/// </summary>
/// <remarks>
/// O nome é o do segredo no Secrets Manager, ou o ARN dele. Os parâmetros do Parameter Store não passam por aqui:
/// são configuração, e entram pela fonte de configuração.
/// </remarks>
/// <param name="client">O cliente do Secrets Manager.</param>
/// <param name="options">As opções dos segredos na AWS.</param>
/// <param name="timeProvider">O relógio do cache. Opcional.</param>
public sealed class AwsSecretService(IAmazonSecretsManager client, IOptions<AwsSecretsOptions> options, TimeProvider? timeProvider = null)
  : SecretServiceBase(options.Value, timeProvider)
{
  #region Protected Methods

  /// <inheritdoc/>
  protected override async Task<string?> ReadAsync(string name, CancellationToken cancellationToken)
  {
    try
    {
      var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = name }, cancellationToken);

      return AwsClients.ValueOf(response);
    }
    catch (ResourceNotFoundException)
    {
      return null;
    }
    catch (Exception e) when (AwsClients.IsAccessDenied(e))
    {
      throw new InternalServerErrorException("Secrets.AccessDenied", e);
    }
    catch (Exception e) when (e is AmazonServiceException or AmazonClientException)
    {
      throw new InternalServerErrorException("Secrets.OperationFailed", e);
    }
  }

  #endregion
}
