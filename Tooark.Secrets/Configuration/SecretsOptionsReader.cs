using Microsoft.Extensions.Configuration;
using Tooark.Secrets.Options;

namespace Tooark.Secrets.Configuration;

/// <summary>
/// Lê as opções de um provedor de segredos a partir da configuração já montada.
/// </summary>
public static class SecretsOptionsReader
{
  /// <summary>
  /// Lê a seção <c>Secrets</c> das fontes já adicionadas ao builder e aplica o ajuste programático por cima.
  /// </summary>
  /// <remarks>
  /// O cofre entra depois do <c>appsettings.json</c> e das variáveis de ambiente, então é deles que vêm a região, o
  /// projeto ou o endereço do cofre. No <c>WebApplicationBuilder</c>, o builder já é a configuração e nada é lido de
  /// novo; num <c>ConfigurationBuilder</c> comum, as fontes são montadas uma vez para a leitura.
  /// </remarks>
  /// <typeparam name="T">O tipo das opções do provedor.</typeparam>
  /// <param name="builder">O builder da configuração.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>As opções lidas, ainda não validadas.</returns>
  public static T Read<T>(IConfigurationBuilder builder, Action<T>? configure) where T : SecretsOptions, new()
  {
    var options = new T();
    var configuration = builder as IConfiguration ?? builder.Build();

    configuration.GetSection(SecretsOptions.Section).Bind(options);
    configure?.Invoke(options);

    return options;
  }
}
