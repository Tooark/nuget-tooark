using Microsoft.Extensions.Configuration;
using Tooark.Exceptions;
using Tooark.Secrets.Configuration;
using Tooark.Secrets.Vault.Configuration;
using Tooark.Secrets.Vault.Options;

namespace Tooark.Secrets.Vault.Injections;

/// <summary>
/// Classe para adicionar os segredos do Vault ou do OpenBao à configuração da aplicação.
/// </summary>
public static class TooarkConfiguration
{
  /// <summary>
  /// Adiciona à configuração os segredos do KV v2 abaixo do caminho das opções.
  /// </summary>
  /// <remarks>
  /// As opções vêm da seção <c>Secrets</c> das fontes já adicionadas (<c>appsettings.json</c>, variáveis de ambiente),
  /// das variáveis <c>VAULT_*</c> ou <c>BAO_*</c> e do ajuste programático. Chame depois das outras fontes: os
  /// valores do cofre passam a valer por cima. A leitura acontece uma vez, quando a configuração é montada.
  /// </remarks>
  /// <param name="builder">O builder da configuração.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>O builder, com a fonte do Vault.</returns>
  /// <exception cref="InternalServerErrorException">
  /// Quando as opções são inválidas, quando o <c>Path</c> não foi informado, ou quando a carga falha e a fonte não é
  /// opcional.
  /// </exception>
  public static IConfigurationBuilder AddTooarkSecretsVault(this IConfigurationBuilder builder, Action<VaultSecretsOptions>? configure = null) =>
    AddTooarkSecretsVault(builder, configure, Environment.GetEnvironmentVariable, null);

  /// <summary>
  /// Adiciona a fonte do Vault com o ambiente e o handler HTTP informados.
  /// </summary>
  /// <param name="builder">O builder da configuração.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <param name="environment">Lê uma variável de ambiente.</param>
  /// <param name="handler">O handler HTTP, ou nulo para o padrão do .NET.</param>
  /// <returns>O builder, com a fonte do Vault.</returns>
  internal static IConfigurationBuilder AddTooarkSecretsVault(
    IConfigurationBuilder builder,
    Action<VaultSecretsOptions>? configure,
    Func<string, string?> environment,
    HttpMessageHandler? handler
  )
  {
    var options = SecretsOptionsReader.Read(builder, configure);
    options.ApplyEnvironment(environment);
    options.Validate();

    // Sem caminho, a fonte não teria o que carregar
    if (string.IsNullOrWhiteSpace(options.Path))
    {
      throw new InternalServerErrorException("Options.Secrets.SourceNotConfigured");
    }

    return builder.Add(new VaultSecretsConfigurationSource(options, handler));
  }
}
