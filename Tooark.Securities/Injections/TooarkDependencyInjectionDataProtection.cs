using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
  /// chamar <c>services.AddDataProtection()</c> e encadear o que precisar, antes ou depois. Com
  /// <see cref="KeyRingOptions.RequirePersistentKeyStorage"/> ligado, a falta de armazenamento de chaves lança
  /// <see cref="InternalServerErrorException"/> no startup do host, ou no primeiro uso do Data Protection sem host.
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

    // Trava do armazenamento de chaves, apenas quando informada
    if (options.RequirePersistentKeyStorage is bool required)
    {
      AddKeyStorageGuard(services, required);
    }

    return services;
  }

  /// <summary>
  /// Registra a trava que exige um armazenamento de chaves configurado.
  /// </summary>
  /// <remarks>
  /// A conferência fica para o startup porque o armazenamento pode ser registrado depois desta chamada, encadeado a
  /// <c>services.AddDataProtection()</c>. A última chamada define se a trava vale: uma chamada com <c>false</c>
  /// desliga a trava ligada por uma anterior.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="required">Indica se o armazenamento de chaves é obrigatório.</param>
  private static void AddKeyStorageGuard(IServiceCollection services, bool required)
  {
    services.Configure<KeyStorageGuardOptions>(guard => guard.Required = required);

    if (required)
    {
      services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<KeyManagementOptions>, KeyStorageValidator>());
      services.AddOptions<KeyManagementOptions>().ValidateOnStart();
    }
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

/// <summary>
/// Opções internas da trava do armazenamento de chaves.
/// </summary>
internal sealed class KeyStorageGuardOptions
{
  /// <summary>
  /// Indica se o armazenamento de chaves é obrigatório.
  /// </summary>
  public bool Required { get; set; }
}

/// <summary>
/// Validação que barra o Data Protection sem armazenamento de chaves quando a trava está ligada.
/// </summary>
/// <param name="guard">Opções da trava.</param>
internal sealed class KeyStorageValidator(IOptions<KeyStorageGuardOptions> guard) : IValidateOptions<KeyManagementOptions>
{
  /// <summary>
  /// Confere se há um repositório de chaves configurado.
  /// </summary>
  /// <param name="name">Nome das opções.</param>
  /// <param name="options">Opções de gerenciamento de chaves já configuradas.</param>
  /// <returns>Sucesso quando a trava está desligada ou há repositório.</returns>
  /// <exception cref="InternalServerErrorException">Quando a trava está ligada e não há repositório.</exception>
  public ValidateOptionsResult Validate(string? name, KeyManagementOptions options)
  {
    // Sem repositório, o ASP.NET Core cai no perfil do usuário ou na memória, e em contêiner as chaves morrem com a
    // instância. Lança a exceção das demais validações do key ring, com a mensagem em chave, em vez de devolver falha.
    if (guard.Value.Required && options.XmlRepository is null)
    {
      throw new InternalServerErrorException("Options.DataProtection.KeyStorageNotConfigured");
    }

    return ValidateOptionsResult.Success;
  }
}
