using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Tooark.Extensions.Factories;

namespace Tooark.Extensions.Injections;

/// <summary>
/// Classe de extensão para adicionar os serviços de localização de recursos ao container de injeção de dependência.
/// </summary>
public static class TooarkJsonStringLocalizerDependencyInjection
{
  /// <summary>
  /// Adiciona o serviço JsonStringLocalizer ao container de injeção de dependência.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <returns>A coleção de serviços com o serviço de localização de recursos adicionado.</returns>
  public static IServiceCollection AddJsonStringLocalizer(this IServiceCollection services)
  {
    // Adiciona a pasta de recursos
    services.AddLocalization(options => options.ResourcesPath = "Resources");

    // Adiciona o serviço de localização de recurso padrão
    services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();

    // Adiciona o serviço de localização de recursos
    services.AddTransient<IStringLocalizer>(provider =>
    {
      // Obtenção do serviço de localização de recurso
      var factory = provider.GetRequiredService<IStringLocalizerFactory>();

      // Criação do serviço de localização de recurso
      return factory.Create(typeof(JsonStringLocalizerExtension));
    });

    // Retorna os serviços
    return services;
  }
}
