using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Securities.Options;

namespace Tooark.Securities.Injections;

/// <summary>
/// Classe para adicionar o serviços de injeção de dependência de Securities.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona os serviços de Securities ao container de injeção de dependência, permitindo configuração de options.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <returns>A coleção de serviços com os serviços de Securities adicionados.</returns>
  public static IServiceCollection AddTooarkSecurities(this IServiceCollection services, IConfiguration configuration)
  {
    // Verifica se a seção de configuração do JWT existe antes de registrar os serviços relacionados
    if (configuration.GetSection(JwtOptions.Section).Exists())
    {
      AddTooarkJwtToken(services, configuration);
    }

    // Verifica se a seção de configuração de Criptografia existe antes de registrar os serviços relacionados
    if (configuration.GetSection(CryptographyOptions.Section).Exists())
    {
      AddTooarkCryptography(services, configuration);
    }

    return services;
  }
}
