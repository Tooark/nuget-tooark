using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Observability.Options;

namespace Tooark.Observability.Injections;

/// <summary>
/// Classe para adicionar os serviços de Observability ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona os serviços de Observability ao container de injeção de dependência, permitindo configuração de options.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções de Observability.</param>
  /// <returns>A coleção de serviços com os serviços de Observability adicionados.</returns>
  public static IServiceCollection AddTooarkObservability(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<ObservabilityOptions>? configure = null
  )
  {
    // Adiciona OpenTelemetry (registra as opções e aplica os overrides programáticos)
    AddTooarkOpenTelemetry(services, configuration, configure);

    return services;
  }
}
