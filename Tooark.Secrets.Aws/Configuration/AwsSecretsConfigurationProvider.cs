using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Tooark.Secrets.Aws.Options;
using Tooark.Secrets.Configuration;
using Filter = Amazon.SecretsManager.Model.Filter;

namespace Tooark.Secrets.Aws.Configuration;

/// <summary>
/// Fonte de configuração com os segredos do Secrets Manager e os parâmetros do Parameter Store.
/// </summary>
/// <param name="options">As opções já validadas.</param>
/// <param name="secretsManager">Cria o cliente do Secrets Manager, usado só durante a carga.</param>
/// <param name="parameterStore">Cria o cliente do Parameter Store, usado só durante a carga.</param>
internal sealed class AwsSecretsConfigurationProvider(
  AwsSecretsOptions options,
  Func<IAmazonSecretsManager> secretsManager,
  Func<IAmazonSimpleSystemsManagement> parameterStore
) : SecretsConfigurationProvider(options, "AWS")
{
  #region Protected Methods

  /// <inheritdoc/>
  protected override async Task<IReadOnlyList<SecretEntry>> LoadEntriesAsync(CancellationToken cancellationToken)
  {
    var entries = new List<SecretEntry>();

    // Os parâmetros vêm antes: com o mesmo nome, o segredo, lido depois, vence
    if (!string.IsNullOrWhiteSpace(options.ParametersPath))
    {
      using var client = parameterStore();
      await LoadParametersAsync(client, options.ParametersPath, entries, cancellationToken);
    }

    if (!string.IsNullOrWhiteSpace(options.SecretsPrefix))
    {
      using var client = secretsManager();
      await LoadSecretsAsync(client, options.SecretsPrefix, entries, cancellationToken);
    }

    return entries;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Lê os parâmetros abaixo do caminho, recursivamente e descriptografados.
  /// </summary>
  /// <param name="client">O cliente do Parameter Store.</param>
  /// <param name="path">O caminho configurado.</param>
  /// <param name="entries">As entradas em montagem.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  private static async Task LoadParametersAsync(
    IAmazonSimpleSystemsManagement client,
    string path,
    List<SecretEntry> entries,
    CancellationToken cancellationToken
  )
  {
    // O Parameter Store exige o caminho começando com a barra
    var root = "/" + path.Trim().Trim('/');
    string? token = null;

    do
    {
      var page = await client.GetParametersByPathAsync(new GetParametersByPathRequest
      {
        Path = root,
        Recursive = true,
        WithDecryption = true,
        NextToken = token
      }, cancellationToken);

      foreach (var parameter in page.Parameters ?? [])
      {
        if (RelativeName(parameter.Name, root) is { } relative)
        {
          entries.Add(new SecretEntry(relative, parameter.Value));
        }
      }

      token = page.NextToken;
    }
    while (!string.IsNullOrEmpty(token));
  }

  /// <summary>
  /// Lê os segredos cujo nome começa com o prefixo.
  /// </summary>
  /// <param name="client">O cliente do Secrets Manager.</param>
  /// <param name="prefix">O prefixo configurado.</param>
  /// <param name="entries">As entradas em montagem.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  private static async Task LoadSecretsAsync(
    IAmazonSecretsManager client,
    string prefix,
    List<SecretEntry> entries,
    CancellationToken cancellationToken
  )
  {
    var normalized = prefix.Trim();
    string? token = null;

    do
    {
      // O filtro por nome é por prefixo e sem diferenciar caixa; o limite de nível é conferido depois
      var page = await client.ListSecretsAsync(new ListSecretsRequest
      {
        Filters = [new Filter { Key = FilterNameStringType.Name, Values = [normalized.TrimEnd('/')] }],
        MaxResults = 100,
        NextToken = token
      }, cancellationToken);

      foreach (var secret in page.SecretList ?? [])
      {
        if (RelativeName(secret.Name, normalized) is not { } relative)
        {
          continue;
        }

        try
        {
          var value = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secret.Name }, cancellationToken);
          entries.Add(new SecretEntry(relative, AwsClients.ValueOf(value)));
        }
        catch (Amazon.SecretsManager.Model.ResourceNotFoundException)
        {
          // Excluído entre a listagem e a leitura: segue sem ele
        }
      }

      token = page.NextToken;
    }
    while (!string.IsNullOrEmpty(token));
  }

  #endregion
}
