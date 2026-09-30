using Microsoft.Extensions.Configuration;
using Tooark.Exceptions;
using Tooark.Secrets.Configuration;
using Tooark.Secrets.Gcp.Configuration;
using Tooark.Secrets.Gcp.Options;

namespace Tooark.Secrets.Gcp.Injections;

/// <summary>
/// Classe para adicionar os segredos do Google Cloud à configuração da aplicação.
/// </summary>
public static class TooarkConfiguration
{
  /// <summary>
  /// Adiciona à configuração os segredos do Secret Manager e os parâmetros do Parameter Manager.
  /// </summary>
  /// <remarks>
  /// As opções vêm da seção <c>Secrets</c> das fontes já adicionadas (<c>appsettings.json</c>, variáveis de ambiente)
  /// e do ajuste programático. Chame depois delas: os valores do cofre passam a valer por cima. A leitura acontece
  /// uma vez, quando a configuração é montada.
  /// </remarks>
  /// <param name="builder">O builder da configuração.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>O builder, com a fonte do Google Cloud.</returns>
  /// <exception cref="InternalServerErrorException">
  /// Quando as opções são inválidas, quando nem <c>SecretsPrefix</c> nem <c>ParametersPrefix</c> foi informado, ou
  /// quando a carga falha e a fonte não é opcional.
  /// </exception>
  public static IConfigurationBuilder AddTooarkSecretsGcp(this IConfigurationBuilder builder, Action<GcpSecretsOptions>? configure = null)
  {
    var options = SecretsOptionsReader.Read(builder, configure);
    options.Validate();

    // Sem prefixo, a fonte não teria o que carregar
    if (string.IsNullOrWhiteSpace(options.SecretsPrefix) && string.IsNullOrWhiteSpace(options.ParametersPrefix))
    {
      throw new InternalServerErrorException("Options.Secrets.SourceNotConfigured");
    }

    return builder.Add(new GcpSecretsConfigurationSource(options));
  }
}
