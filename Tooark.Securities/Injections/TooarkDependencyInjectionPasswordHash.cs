using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tooark.Exceptions;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Securities.Injections;

/// <summary>
/// Classe para adicionar o serviço de hash de senha ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o serviço de hash de senha (PBKDF2-HMAC-SHA256) ao container de injeção de dependência.
  /// </summary>
  /// <remarks>
  /// A seção <c>PasswordHash</c> é opcional: sem ela, e sem <paramref name="configure"/>, valem os padrões
  /// (600.000 iterações, salt de 16 bytes, hash de 32 bytes). As opções são validadas aqui, no startup, e não no
  /// primeiro login. Chamar mais de uma vez não repete o registro do serviço.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration), opcional.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o serviço de hash de senha adicionado.</returns>
  /// <exception cref="InternalServerErrorException">Quando as opções ficam abaixo do mínimo seguro.</exception>
  public static IServiceCollection AddTooarkPasswordHash(
    this IServiceCollection services,
    IConfiguration? configuration = null,
    Action<PasswordHashOptions>? configure = null
  )
  {
    // Carrega da configuração e aplica os overrides programáticos por cima
    void Apply(PasswordHashOptions options)
    {
      if (configuration is not null)
      {
        StrictConfiguration.Bind(configuration, PasswordHashOptions.Section, options);
      }

      configure?.Invoke(options);
    }

    // Valida no startup, e não no primeiro hash
    var validated = new PasswordHashOptions();
    Apply(validated);
    validated.Validate();

    // Registra as opções e o serviço
    services.Configure<PasswordHashOptions>(Apply);
    services.TryAddSingleton<IPasswordHashService, PasswordHashService>();

    return services;
  }
}
