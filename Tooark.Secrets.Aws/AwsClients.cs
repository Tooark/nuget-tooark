using System.Net;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Tooark.Secrets.Aws.Options;

namespace Tooark.Secrets.Aws;

/// <summary>
/// Montagem dos clientes da AWS e leitura comum das respostas.
/// </summary>
internal static class AwsClients
{
  #region Methods

  /// <summary>
  /// Monta o cliente do Secrets Manager a partir das opções, com as credenciais da cadeia padrão da AWS.
  /// </summary>
  /// <param name="options">As opções já validadas.</param>
  /// <returns>O cliente do Secrets Manager.</returns>
  internal static AmazonSecretsManagerClient CreateSecretsManager(AwsSecretsOptions options) =>
    new(Configure(new AmazonSecretsManagerConfig(), options));

  /// <summary>
  /// Monta o cliente do Parameter Store a partir das opções, com as credenciais da cadeia padrão da AWS.
  /// </summary>
  /// <param name="options">As opções já validadas.</param>
  /// <returns>O cliente do Parameter Store.</returns>
  internal static AmazonSimpleSystemsManagementClient CreateParameterStore(AwsSecretsOptions options) =>
    new(Configure(new AmazonSimpleSystemsManagementConfig(), options));

  /// <summary>
  /// Lê o valor de um segredo, em texto ou binário.
  /// </summary>
  /// <param name="response">A resposta do Secrets Manager.</param>
  /// <returns>O valor em texto; o binário é lido como UTF-8.</returns>
  internal static string? ValueOf(GetSecretValueResponse response) =>
    response.SecretString ?? (response.SecretBinary is { } binary ? Encoding.UTF8.GetString(binary.ToArray()) : null);

  /// <summary>
  /// Indica se o erro é de acesso negado.
  /// </summary>
  /// <param name="exception">O erro do SDK.</param>
  /// <returns>Verdadeiro quando a identidade não tem permissão.</returns>
  internal static bool IsAccessDenied(Exception exception) =>
    exception is AmazonServiceException { StatusCode: HttpStatusCode.Forbidden } or
      AmazonServiceException { ErrorCode: "AccessDeniedException" };

  #endregion

  #region Private Methods

  /// <summary>
  /// Aplica a região e o endereço das opções à configuração do cliente.
  /// </summary>
  /// <typeparam name="T">O tipo da configuração do cliente.</typeparam>
  /// <param name="config">A configuração do cliente.</param>
  /// <param name="options">As opções já validadas.</param>
  /// <returns>A mesma configuração.</returns>
  private static T Configure<T>(T config, AwsSecretsOptions options) where T : ClientConfig
  {
    var region = options.Region?.Trim();

    if (string.IsNullOrWhiteSpace(options.ServiceUrl))
    {
      if (!string.IsNullOrEmpty(region))
      {
        config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
      }
    }
    else
    {
      // O SDK descarta a região ao receber o endereço: ela passa a valer só para a assinatura
      config.ServiceURL = options.ServiceUrl.Trim();

      if (!string.IsNullOrEmpty(region))
      {
        config.AuthenticationRegion = region;
      }
    }

    return config;
  }

  #endregion
}
