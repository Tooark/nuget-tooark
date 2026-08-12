using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Options;

namespace Tooark.Mediator.Injections;

/// <summary>
/// Classe para adicionar os serviços do Mediator ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona os serviços do Mediator ao container de injeção de dependência, escaneando os assemblies fornecidos para registrar os handlers.
  /// </summary>
  /// <remarks>
  /// Recomenda-se informar os assemblies explicitamente (ex: <c>typeof(Program).Assembly</c>).
  /// Se nenhum assembly for fornecido, o assembly chamador será escaneado como fallback.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="assemblies">Assemblies a serem escaneados para registrar os handlers do Mediator. Se nenhum for fornecido, o assembly chamador será usado.</param>
  /// <returns>A coleção de serviços com os serviços do Mediator adicionados.</returns>
  [MethodImpl(MethodImplOptions.NoInlining)] // NoInlining garante que GetCallingAssembly retorne o assembly do usuário
  public static IServiceCollection AddTooarkMediator(
    this IServiceCollection services,
    params Assembly[] assemblies
  )
  {
    // Captura o assembly chamador no ponto de entrada para o fallback de scan
    return AddTooarkMediatorCore(services, _ => { }, assemblies, Assembly.GetCallingAssembly());
  }

  /// <summary>
  /// Adiciona os serviços do Mediator ao container de injeção de dependência, permitindo
  /// configuração de options e escaneando os assemblies fornecidos para registrar os handlers.
  /// </summary>
  /// <remarks>
  /// Recomenda-se informar os assemblies explicitamente (ex: <c>typeof(Program).Assembly</c>).
  /// Se nenhum assembly for fornecido, o assembly chamador será escaneado como fallback.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configure">Ação para configurar as opções do Mediator.</param>
  /// <param name="assemblies">Assemblies a serem escaneados para registrar os handlers do Mediator. Se nenhum for fornecido, o assembly chamador será usado.</param>
  /// <returns>A coleção de serviços com os serviços do Mediator adicionados.</returns>
  [MethodImpl(MethodImplOptions.NoInlining)] // NoInlining garante que GetCallingAssembly retorne o assembly do usuário
  public static IServiceCollection AddTooarkMediator(
    this IServiceCollection services,
    Action<MediatorOptions> configure,
    params Assembly[] assemblies
  )
  {
    // Captura o assembly chamador no ponto de entrada para o fallback de scan
    return AddTooarkMediatorCore(services, configure, assemblies, Assembly.GetCallingAssembly());
  }

  /// <summary>
  /// Implementação central do registro dos serviços do Mediator.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configure">Ação para configurar as opções do Mediator.</param>
  /// <param name="assemblies">Assemblies a serem escaneados para registrar os handlers do Mediator.</param>
  /// <param name="fallbackAssembly">Assembly chamador, usado quando nenhum assembly é fornecido.</param>
  /// <returns>A coleção de serviços com os serviços do Mediator adicionados.</returns>
  private static IServiceCollection AddTooarkMediatorCore(
    IServiceCollection services,
    Action<MediatorOptions> configure,
    Assembly[] assemblies,
    Assembly fallbackAssembly
  )
  {
    // Verifica se a coleção de serviços é nula
    if (services == null)
    {
      throw new InternalServerErrorException("Mediator.Null.Service");
    }

    // Verifica se a ação de configuração é nula
    if (configure == null)
    {
      throw new InternalServerErrorException("Mediator.Null.Configure");
    }

    // Registra as opções do Mediator no padrão Options: chamadas múltiplas de AddTooarkMediator compõem as configurações (em ordem de registro)
    services.AddOptions<MediatorOptions>().Configure(configure);

    // Registra o MediatorOptions resolvido do IOptions, preservando a injeção direta de MediatorOptions no construtor do Mediator
    services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<MediatorOptions>>().Value);

    // Registra o Mediator e suas interfaces (ISender e IPublisher) no container de injeção de dependência
    services.TryAddTransient<IMediator, Mediator>();
    services.TryAddTransient<ISender>(serviceProvider => serviceProvider.GetRequiredService<IMediator>());
    services.TryAddTransient<IPublisher>(serviceProvider => serviceProvider.GetRequiredService<IMediator>());

    // Determina os assemblies a serem escaneados para registrar os handlers do Mediator
    var assembliesToScan = assemblies is { Length: > 0 } ? assemblies : [fallbackAssembly];

    // Itera sobre os assemblies a serem escaneados e registra os handlers do Mediator encontrados em cada assembly
    foreach (var assembly in assembliesToScan.Distinct())
    {
      RegisterHandlersFromAssembly(services, assembly);
    }

    return services;
  }

  /// <summary>
  /// Registra os handlers do Mediator encontrados no assembly fornecido, associando-os às suas
  /// interfaces correspondentes (IRequestHandler, INotifyHandler, ICommandHandler, IQueryHandler).
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="assembly">Assembly a ser escaneado para registrar os handlers do Mediator.</param>
  private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
  {
    // Define as interfaces genéricas dos handlers do Mediator que serão registradas
    var handlerInterfaces = new[]
    {
      typeof(IRequestHandler<,>),
      typeof(INotifyHandler<>),
      typeof(ICommandHandler<,>),
      typeof(ICommandHandler<>),
      typeof(IQueryHandler<,>)
    };

    // Obtém os tipos do assembly; em caso de falha parcial de carregamento, usa apenas os tipos carregáveis
    Type?[] types;
    try
    {
      types = assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
      types = ex.Types;
    }

    // Obtém todas as classes concretas fechadas do assembly (genéricos abertos não são suportados pelo dispatch)
    var implementations = types
      .OfType<Type>()
      .Where(type => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false });

    // Itera sobre as classes concretas
    foreach (var implementation in implementations)
    {
      // Obtém todas as interfaces genéricas implementadas pela classe concreta que correspondem aos handlers do Mediator
      var interfaces = implementation.GetInterfaces()
        .Where(@interface => @interface.IsGenericType)
        .Where(@interface => handlerInterfaces.Contains(@interface.GetGenericTypeDefinition()))
        .ToList();

      // Itera sobre as interfaces encontradas
      foreach (var @interface in interfaces)
      {
        // Registra a classe concreta como implementação da interface correspondente no container de injeção de dependência
        services.TryAddEnumerable(ServiceDescriptor.Transient(@interface, implementation));
      }
    }
  }
}
