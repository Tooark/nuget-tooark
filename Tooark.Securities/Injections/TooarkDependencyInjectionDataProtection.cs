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
  /// Sem valor informado, a trava vem ligada quando a aplicação roda em contêiner
  /// (<c>DOTNET_RUNNING_IN_CONTAINER</c> ou <c>KUBERNETES_SERVICE_HOST</c>) e desligada fora dele.
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
  ) => AddTooarkDataProtectionCore(services, configuration, configure, ContainerEnvironment.IsRunningInContainer());

  /// <summary>
  /// Adiciona o Data Protection com o ambiente já resolvido, para os testes fixarem se há contêiner ou não.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do key ring.</param>
  /// <param name="runningInContainer">Indica se a aplicação roda em contêiner, o que liga a trava por padrão.</param>
  /// <returns>A coleção de serviços com o Data Protection adicionado.</returns>
  /// <exception cref="InternalServerErrorException">Quando as opções do key ring são inválidas.</exception>
  internal static IServiceCollection AddTooarkDataProtectionCore(
    IServiceCollection services,
    IConfiguration configuration,
    Action<KeyRingOptions>? configure,
    bool runningInContainer
  )
  {
    // Carrega da configuração e aplica os overrides programáticos por cima
    var options = new KeyRingOptions();
    StrictConfiguration.Bind(configuration, KeyRingOptions.Section, options);
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

    // Trava do armazenamento de chaves: o valor informado vence; sem valor, vem ligada em contêiner, onde as
    // chaves gravadas no perfil do usuário morrem com a instância
    if (options.RequirePersistentKeyStorage is bool required)
    {
      AddKeyStorageGuard(services, required);
    }
    else if (runningInContainer)
    {
      AddKeyStorageGuard(services, required: null);
    }

    return services;
  }

  /// <summary>
  /// Registra a trava que exige um armazenamento de chaves configurado.
  /// </summary>
  /// <remarks>
  /// A conferência fica para o startup porque o armazenamento pode ser registrado depois desta chamada, encadeado a
  /// <c>services.AddDataProtection()</c>. A última chamada com valor informado define se a trava vale: uma chamada
  /// com <c>false</c> desliga a trava ligada por uma anterior, e também a ligada pelo padrão de contêiner, que só
  /// vale enquanto nenhuma chamada informar um valor.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="required">Indica se o armazenamento de chaves é obrigatório; nulo liga a trava pelo padrão de contêiner.</param>
  private static void AddKeyStorageGuard(IServiceCollection services, bool? required)
  {
    if (required is bool informed)
    {
      services.Configure<KeyStorageGuardOptions>(guard => guard.Required = informed);
    }
    else
    {
      services.Configure<KeyStorageGuardOptions>(guard => guard.RequiredByDefault = true);
    }

    if (required ?? true)
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
  /// Valor informado pela aplicação em <see cref="KeyRingOptions.RequirePersistentKeyStorage"/>; nulo quando
  /// nenhuma chamada informou.
  /// </summary>
  public bool? Required { get; set; }

  /// <summary>
  /// Indica se a trava vale pelo padrão de contêiner, na falta de um valor informado.
  /// </summary>
  public bool RequiredByDefault { get; set; }

  /// <summary>
  /// Indica se o armazenamento de chaves é obrigatório: o valor informado vence o padrão.
  /// </summary>
  public bool IsRequired => Required ?? RequiredByDefault;
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
    if (guard.Value.IsRequired && options.XmlRepository is null)
    {
      throw new InternalServerErrorException("Options.DataProtection.KeyStorageNotConfigured");
    }

    return ValidateOptionsResult.Success;
  }
}
