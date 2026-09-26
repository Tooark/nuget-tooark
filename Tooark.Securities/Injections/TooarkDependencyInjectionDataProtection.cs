using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Securities.Options;

namespace Tooark.Securities.Injections;

/// <summary>
/// Classe para adicionar o Data Protection do ASP.NET Core ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o Data Protection do ASP.NET Core ao container de injeção de dependência, com o key ring lido da
  /// seção <c>DataProtection</c>.
  /// </summary>
  /// <remarks>
  /// A seção é opcional: sem ela, e sem <paramref name="configure"/>, o Data Protection é registrado com os
  /// padrões do ASP.NET Core. As opções apenas configuram o builder nativo — a aplicação continua livre para
  /// chamar <c>services.AddDataProtection()</c> e encadear o que precisar, antes ou depois.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do key ring.</param>
  /// <returns>A coleção de serviços com o Data Protection adicionado.</returns>
  /// <exception cref="InternalServerErrorException">Quando as opções do key ring são inválidas.</exception>
  public static IServiceCollection AddTooarkDataProtection(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<KeyRingOptions>? configure = null
  )
  {
    // Carrega da configuração e aplica os overrides programáticos por cima
    var options = new KeyRingOptions();
    configuration.GetSection(KeyRingOptions.Section).Bind(options);
    configure?.Invoke(options);

    // Valida no startup, e não ao gravar a primeira chave
    options.Validate();

    // Marca o registro para que AddTooarkSecurities não reaplique a seção por cima destas opções
    if (!IsDataProtectionRegistered(services))
    {
      services.AddSingleton<DataProtectionMarker>();
    }

    // Registra o Data Protection nativo com as opções Tooark
    options.ConfigureBuilder(services.AddDataProtection());

    return services;
  }

  /// <summary>
  /// Indica se o Data Protection já foi registrado por <see cref="AddTooarkDataProtection"/>.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <returns>Verdadeiro quando o registro já foi feito por este pacote.</returns>
  private static bool IsDataProtectionRegistered(IServiceCollection services) =>
    services.Any(descriptor => descriptor.ServiceType == typeof(DataProtectionMarker));
}

/// <summary>
/// Marcador que indica que o Data Protection já foi registrado por este pacote.
/// </summary>
internal sealed class DataProtectionMarker
{
}
