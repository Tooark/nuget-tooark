using Amazon.SecretsManager;
using Amazon.SimpleSystemsManagement;
using Microsoft.Extensions.Configuration;
using Tooark.Secrets.Aws.Options;

namespace Tooark.Secrets.Aws.Configuration;

/// <summary>
/// Fonte de configuração da AWS.
/// </summary>
/// <param name="options">As opções já validadas.</param>
/// <param name="secretsManager">Cria o cliente do Secrets Manager. Opcional: sem ele, vale o das opções.</param>
/// <param name="parameterStore">Cria o cliente do Parameter Store. Opcional: sem ele, vale o das opções.</param>
internal sealed class AwsSecretsConfigurationSource(
  AwsSecretsOptions options,
  Func<IAmazonSecretsManager>? secretsManager = null,
  Func<IAmazonSimpleSystemsManagement>? parameterStore = null
) : IConfigurationSource
{
  /// <inheritdoc/>
  public IConfigurationProvider Build(IConfigurationBuilder builder) => new AwsSecretsConfigurationProvider(
    options,
    secretsManager ?? (() => AwsClients.CreateSecretsManager(options)),
    parameterStore ?? (() => AwsClients.CreateParameterStore(options))
  );
}
