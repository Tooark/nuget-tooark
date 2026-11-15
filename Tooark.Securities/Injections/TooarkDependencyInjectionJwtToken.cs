using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Securities.Injections;

/// <summary>
/// Classe para adicionar o serviço de token JWT ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o serviço de token JWT ao container de injeção de dependência, permitindo configuração de options.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <returns>A coleção de serviços com o serviço de token JWT adicionado.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando a configuração estrita está ligada e a seção traz uma chave desconhecida.</exception>
  public static IServiceCollection AddTooarkJwtToken(this IServiceCollection services, IConfiguration configuration)
  {
    // Com a configuração estrita ligada, uma chave desconhecida na seção falha aqui, no registro: as opções
    // abaixo só são lidas no primeiro uso do serviço
    StrictConfiguration.Validate<JwtOptions>(configuration, JwtOptions.Section);

    // Registra as opções do JWT a partir da configuração
    services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));

    // Registra o serviço de token JWT
    services.AddSingleton<IJwtTokenService, JwtTokenService>();

    return services;
  }
}
