using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
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
  /// <remarks>
  /// Com <see cref="CryptographyOptions.Keys"/> preenchido, registra o <see cref="KeyedCryptographyService"/> como
  /// <see cref="ICryptographyService"/> e como <see cref="IKeyedCryptographyService"/>: quem já injeta a interface
  /// de chave única passa a cifrar com a chave corrente e a abrir os valores legados sem mudar de código. Sem
  /// chaves identificadas, registra o <see cref="CryptographyService"/> de chave única, como sempre. As chaves
  /// identificadas são validadas aqui, no startup.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <returns>A coleção de serviços com o serviço de criptografia adicionado.</returns>
  /// <exception cref="InternalServerErrorException">Quando as chaves identificadas são inválidas.</exception>
  public static IServiceCollection AddTooarkCryptography(this IServiceCollection services, IConfiguration configuration)
  {
    var section = configuration.GetSection(CryptographyOptions.Section);

    // Registra as opções da Criptografia a partir da configuração
    services.Configure<CryptographyOptions>(section);

    // Lê as opções para decidir qual serviço registrar
    var options = new CryptographyOptions();
    section.Bind(options);

    // Chaves identificadas: um único serviço atende as duas interfaces
    if (options.Keys.Count > 0)
    {
      KeyedCryptographyService.ValidateOptions(options);

      services.AddSingleton<KeyedCryptographyService>();
      services.AddSingleton<IKeyedCryptographyService>(provider => provider.GetRequiredService<KeyedCryptographyService>());
      services.AddSingleton<ICryptographyService>(provider => provider.GetRequiredService<KeyedCryptographyService>());

      return services;
    }

    // Registra o serviço de criptografia de chave única
    services.AddSingleton<ICryptographyService, CryptographyService>();

    return services;
  }
}
