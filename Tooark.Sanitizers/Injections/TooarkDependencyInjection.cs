using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tooark.Sanitizers.Interfaces;
using Tooark.Sanitizers.Options;

namespace Tooark.Sanitizers.Injections;

/// <summary>
/// Classe para adicionar os sanitizadores ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona os sanitizadores de HTML, URL e do <c>@tooark/wysiwyg</c>, com as opções lidas da seção
  /// <c>Sanitizers</c>.
  /// </summary>
  /// <remarks>
  /// A seção é opcional: sem ela, os sanitizadores usam os padrões seguros. As opções são validadas aqui, no
  /// registro, para que uma configuração que libera algo inseguro falhe no startup e não na primeira requisição.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com os sanitizadores adicionados.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções liberam algo inseguro.</exception>
  public static IServiceCollection AddTooarkSanitizers(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<SanitizerOptions>? configure = null
  ) => AddSanitizers(services, configuration.GetSection(SanitizerOptions.Section), configure);

  /// <summary>
  /// Adiciona os sanitizadores de HTML, URL e do <c>@tooark/wysiwyg</c>, sem ler a configuração da aplicação.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com os sanitizadores adicionados.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções liberam algo inseguro.</exception>
  public static IServiceCollection AddTooarkSanitizers(this IServiceCollection services, Action<SanitizerOptions>? configure = null) =>
    AddSanitizers(services, null, configure);

  /// <summary>
  /// Valida as opções e registra os sanitizadores.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="section">A seção de configuração, quando houver.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com os sanitizadores adicionados.</returns>
  private static IServiceCollection AddSanitizers(
    IServiceCollection services,
    IConfigurationSection? section,
    Action<SanitizerOptions>? configure
  )
  {
    // Carrega da configuração e aplica os overrides programáticos por cima
    void Apply(SanitizerOptions options)
    {
      section?.Bind(options);
      configure?.Invoke(options);
    }

    // Valida no startup, e não no primeiro uso do sanitizador
    var options = new SanitizerOptions();
    Apply(options);
    options.Validate();

    // Registra as opções pelo sistema de options, com a mesma leitura validada acima
    services.AddOptions<SanitizerOptions>().Configure(Apply);

    // Os sanitizadores não guardam estado por chamada, então uma instância atende a aplicação
    services.TryAddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();
    services.TryAddSingleton<IUrlSanitizerService, UrlSanitizerService>();
    services.TryAddSingleton<IWysiwygSanitizerService, WysiwygSanitizerService>();

    return services;
  }
}
