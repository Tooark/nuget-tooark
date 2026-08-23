using Microsoft.Extensions.DependencyInjection;
using Tooark.Extensions.Injections;

namespace Tooark.Dtos.Injections;

/// <summary>
/// Classe de extensão para adicionar os objetos de transferência de dados (Dto) ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o serviço de localização de recursos usado pelos DTOs ao container de injeção de dependência.
  /// </summary>
  /// <remarks>
  /// A tradução das mensagens de erro do <see cref="ResponseDto{T}"/> não depende deste registro: o
  /// <see cref="Dto"/> cria o próprio localizador quando nenhum foi configurado. O registro existe para a
  /// aplicação poder injetar <c>IStringLocalizer</c> nos próprios tipos.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <returns>A coleção de serviços com o serviço de localização de recursos adicionado.</returns>
  public static IServiceCollection AddTooarkDtos(this IServiceCollection services)
  {
    // Adiciona o serviço JsonStringLocalizer
    services.AddJsonStringLocalizer();

    // Retorna os serviços
    return services;
  }
}
