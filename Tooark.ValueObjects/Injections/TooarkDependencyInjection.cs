using Microsoft.Extensions.DependencyInjection;
using Tooark.Extensions.Injections;

namespace Tooark.ValueObjects.Injections;

/// <summary>
/// Classe de extensão para adicionar os value objects ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o serviço de localização de recursos com o tipo ValueObject ao container de injeção de dependência.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <returns>A coleção de serviços com o serviço de localização de recursos com o tipo ValueObject adicionado.</returns>
  public static IServiceCollection AddTooarkValueObjects(this IServiceCollection services)
  {
    // Adiciona as dependências do Extensions
    services.AddTooarkExtensions();

    // Retorna os serviços
    return services;
  }
}
