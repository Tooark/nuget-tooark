using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Securities.Injections;

/// <summary>
/// Classe para adicionar o serviço de criptografia ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o serviço de criptografia ao container de injeção de dependência, permitindo configuração de options.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <returns>A coleção de serviços com o serviço de criptografia adicionado.</returns>
  public static IServiceCollection AddTooarkCryptography(this IServiceCollection services, IConfiguration configuration)
  {
    // Registra as opções da Criptografia a partir da configuração
    services.Configure<CryptographyOptions>(configuration.GetSection(CryptographyOptions.Section));

    // Registra o serviço de criptografia
    services.AddSingleton<ICryptographyService, CryptographyService>();

    return services;
  }
}
