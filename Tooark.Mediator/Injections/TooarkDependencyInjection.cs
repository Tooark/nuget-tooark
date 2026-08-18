using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Behaviors;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Options;

namespace Tooark.Mediator.Injections;

/// <summary>
/// Classe para adicionar os serviços do Mediator ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  #region Methods

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
  /// Adiciona um behavior ao pipeline de requisições do Mediator.
  /// </summary>
  /// <remarks>
  /// Os behaviors são executados na ordem de registro: o primeiro registrado é o mais externo.
  /// Por isso não são descobertos pelo scan de assemblies, cuja ordem não é garantida.
  /// Aceita behaviors fechados (<c>ValidationBehavior&lt;CriarPedido, Guid&gt;</c>) e genéricos abertos
  /// (<c>LoggingBehavior&lt;,&gt;</c>), que se aplicam a todas as requisições.
  /// </remarks>
  /// <typeparam name="TBehavior">O tipo do behavior a ser registrado.</typeparam>
  /// <param name="services">Coleção de serviços.</param>
  /// <returns>A coleção de serviços com o behavior adicionado.</returns>
  public static IServiceCollection AddTooarkMediatorBehavior<TBehavior>(this IServiceCollection services)
    where TBehavior : class
  {
    return AddTooarkMediatorBehavior(services, typeof(TBehavior));
  }

  /// <summary>
  /// Adiciona um behavior ao pipeline de requisições do Mediator.
  /// </summary>
  /// <remarks>
  /// Os behaviors são executados na ordem de registro: o primeiro registrado é o mais externo.
  /// Por isso não são descobertos pelo scan de assemblies, cuja ordem não é garantida.
  /// Aceita behaviors fechados (<c>ValidationBehavior&lt;CriarPedido, Guid&gt;</c>) e genéricos abertos
  /// (<c>LoggingBehavior&lt;,&gt;</c>), que se aplicam a todas as requisições.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="behaviorType">O tipo do behavior a ser registrado.</param>
  /// <returns>A coleção de serviços com o behavior adicionado.</returns>
  /// <exception cref="InternalServerErrorException">Lançada quando o tipo não é um behavior de pipeline válido.</exception>
  public static IServiceCollection AddTooarkMediatorBehavior(this IServiceCollection services, Type behaviorType)
  {
    // Verifica se a coleção de serviços é nula
    if (services == null)
    {
      throw new InternalServerErrorException("Mediator.Null.Service");
    }

    // Verifica se o tipo do behavior é nulo
    if (behaviorType == null)
    {
      throw new InternalServerErrorException("Mediator.Null.Behavior");
    }

    // Obtém as interfaces de behavior implementadas pelo tipo
    var interfaces = behaviorType.GetInterfaces()
      .Where(@interface => @interface.IsGenericType)
      .Where(@interface => @interface.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
      .ToList();

    // Verifica se o tipo implementa algum behavior de pipeline
    if (interfaces.Count == 0)
    {
      throw new InternalServerErrorException($"Behavior.NotSupported;{behaviorType.FullName}");
    }

    // Itera sobre as interfaces de behavior encontradas
    foreach (var @interface in interfaces)
    {
      // Behavior genérico aberto é registrado pela definição genérica, para o container fechar no despacho
      if (behaviorType.ContainsGenericParameters)
      {
        // O container substitui os parâmetros da interface pelos do tipo, o que exige correspondência direta
        if (!MatchesOpenBehavior(behaviorType, @interface))
        {
          throw new InternalServerErrorException($"Behavior.NotSupported;{behaviorType.FullName}");
        }

        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), behaviorType));

        continue;
      }

      // Behavior fechado é registrado pela interface concreta que implementa
      services.TryAddEnumerable(ServiceDescriptor.Transient(@interface, behaviorType));
    }

    return services;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Verifica se um behavior genérico aberto pode ser fechado pelo container a partir da definição genérica.
  /// </summary>
  /// <param name="behaviorType">O tipo do behavior.</param>
  /// <param name="interface">A interface de behavior implementada pelo tipo.</param>
  /// <returns>True quando os parâmetros genéricos do tipo correspondem, em ordem, aos da interface.</returns>
  private static bool MatchesOpenBehavior(Type behaviorType, Type @interface)
  {
    // Obtém os parâmetros genéricos do tipo e os argumentos genéricos da interface
    var parameters = behaviorType.GetGenericArguments();
    var arguments = @interface.GetGenericArguments();

    // A definição genérica só pode ser fechada quando os parâmetros são os mesmos, na mesma ordem
    return parameters.Length == arguments.Length && parameters.SequenceEqual(arguments);
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

    // Garante que cada requisição tenha um único manipulador registrado
    EnsureSingleRequestHandler(services);

    return services;
  }

  /// <summary>
  /// Valida que cada requisição tenha um único manipulador registrado.
  /// </summary>
  /// <remarks>
  /// Uma requisição é processada por um único manipulador, resolvido do container no despacho. Com mais de um
  /// manipulador registrado para a mesma requisição, o container devolve o último e os demais nunca executam —
  /// silenciosamente. A validação cobre os manipuladores conhecidos no momento do registro.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <exception cref="InternalServerErrorException">Lançada quando uma requisição tem mais de um manipulador registrado.</exception>
  private static void EnsureSingleRequestHandler(IServiceCollection services)
  {
    // Agrupa os manipuladores de requisição registrados pelo tipo de serviço (requisição/resposta)
    var duplicated = services
      .Where(descriptor => descriptor.ServiceType.IsGenericType)
      .Where(descriptor => descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
      .GroupBy(descriptor => descriptor.ServiceType)
      .FirstOrDefault(group => group.Select(descriptor => descriptor.ImplementationType).Distinct().Count() > 1);

    // Verifica se existe requisição com mais de um manipulador registrado
    if (duplicated != null)
    {
      // Relaciona os manipuladores em conflito para identificar a origem do problema
      var implementations = string.Join(", ", duplicated
        .Select(descriptor => descriptor.ImplementationType?.FullName)
        .Where(name => name != null));

      throw new InternalServerErrorException($"Handler.Duplicated;{duplicated.Key.FullName};{implementations}");
    }
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

  #endregion
}
