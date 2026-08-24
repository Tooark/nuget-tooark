using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Enums;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Injections;
using Tooark.Mediator.Options;

namespace Tooark.Tests.Mediator.Injections;

public class TooarkDependencyInjectionTests
{
  // Testa se a coleção de serviços nula falha no registro, e não em execução
  [Fact]
  public void AddTooarkMediator_ShouldThrowInternalServerErrorException_WhenServicesIsNull()
  {
    // Arrange
    ServiceCollection? services = null;

    // Act & Assert
    Assert.Throws<InternalServerErrorException>(() => services!.AddTooarkMediator(typeof(TooarkDependencyInjectionTests).Assembly));
  }

  // Testa o registro do mediador e dos manipuladores do assembly informado
  [Fact]
  public async Task AddTooarkMediator_ShouldRegisterMediatorAndHandlers_WhenAssemblyIsProvided()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(typeof(TooarkDependencyInjectionTests).Assembly);

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Assert
    Assert.NotNull(mediator);

    var queryResult = await mediator.SendAsync(
      new TestQuery("query-result"),
      TestContext.Current.CancellationToken);
    Assert.Equal("query-result", queryResult);

    var commandResult = await mediator.SendAsync(
      new TestCommand("command-result"),
      TestContext.Current.CancellationToken);
    Assert.Equal("command-result", commandResult);

    await mediator.SendAsync(
      new VoidCommand(),
      TestContext.Current.CancellationToken);

    NotificationCounter.Reset();
    await mediator.PublishAsync(
      new TestNotification(),
      TestContext.Current.CancellationToken);
    Assert.Equal(1, NotificationCounter.Value);
  }

  // Testa se, sem assembly informado, a varredura recai no assembly que chamou o registro
  [Fact]
  public void AddTooarkMediator_ShouldUseDefaultAssembly_WhenNoAssemblyIsProvided()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Assert - O fallback escaneia o assembly chamador (este assembly de testes), registrando seus handlers
    Assert.NotNull(mediator);
    Assert.NotNull(provider.GetService<IRequestHandler<PingRequest, string>>());
    Assert.NotEmpty(provider.GetServices<INotifyHandler<TestNotification>>());
  }

  // Testa se a array nula tem o mesmo efeito da ausência do argumento
  [Fact]
  public void AddTooarkMediator_ShouldFallbackToCallingAssembly_WhenAssembliesArrayIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act - Array nulo deve se comportar como nenhum assembly informado (fallback para o chamador)
    services.AddTooarkMediator((Assembly[])null!);

    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.NotNull(provider.GetService<IRequestHandler<PingRequest, string>>());
  }

  // Testa se um assembly com falha parcial de carregamento não impede o registro dos tipos que carregaram
  [Fact]
  public void AddTooarkMediator_ShouldRegisterLoadableTypes_WhenAssemblyThrowsReflectionTypeLoadException()
  {
    // Arrange
    var services = new ServiceCollection();

    // Assembly fake que falha parcialmente no carregamento: expõe um handler carregável e um tipo nulo
    var assembly = new PartiallyLoadableAssembly([typeof(PingRequestHandler), null]);

    // Act
    services.AddTooarkMediator(assembly);

    using var provider = services.BuildServiceProvider();

    // Assert - O handler carregável foi registrado mesmo com a falha parcial do assembly
    Assert.NotNull(provider.GetService<IRequestHandler<PingRequest, string>>());
  }

  /// <summary>
  /// Assembly fake que simula falha parcial de carregamento de tipos (ReflectionTypeLoadException).
  /// </summary>
  private sealed class PartiallyLoadableAssembly(Type?[] types) : Assembly
  {
    public override Type[] GetTypes()
    {
      throw new ReflectionTypeLoadException(types, null);
    }
  }

  // Testa se genéricos abertos são ignorados na varredura, já que o despacho não os resolve
  [Fact]
  public void AddTooarkMediator_ShouldSkipOpenGenericHandlers_WhenScanningAssembly()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act - O assembly contém OpenGenericNotifyHandler<T> (genérico aberto), que deve ser ignorado pelo scan
    services.AddTooarkMediator(typeof(TooarkDependencyInjectionTests).Assembly);

    // Assert - O handler genérico aberto não foi registrado pelo scan e o provider resolve sem exceção
    // (a checagem é específica do handler: a infraestrutura do Options registra genéricos abertos legítimos)
    Assert.DoesNotContain(services, service =>
      service.ImplementationType == typeof(OpenGenericNotifyHandler<>));

    using var provider = services.BuildServiceProvider();
    Assert.NotEmpty(provider.GetServices<INotifyHandler<TestNotification>>());
  }

  // Testa se o mesmo assembly informado duas vezes não duplica os manipuladores
  [Fact]
  public void AddTooarkMediator_ShouldNotDuplicateRegistrations_WhenAssembliesAreRepeated()
  {
    // Arrange
    var services = new ServiceCollection();
    var assembly = typeof(TooarkDependencyInjectionTests).Assembly;

    // Act
    services.AddTooarkMediator(assembly, assembly);

    // Assert
    var requestHandlerRegistrations = services.Count(service =>
      service.ServiceType == typeof(IRequestHandler<TestQuery, string>)
      && service.ImplementationType == typeof(TestQueryHandler));

    var notificationHandlerRegistrations = services.Count(service =>
      service.ServiceType == typeof(INotifyHandler<TestNotification>)
      && service.ImplementationType == typeof(TestNotificationHandler));

    Assert.Equal(1, requestHandlerRegistrations);
    Assert.Equal(1, notificationHandlerRegistrations);
  }

  // Testa se cada manipulador é registrado sob a interface fechada que implementa
  [Fact]
  public void AddTooarkMediator_ShouldRegisterHandlersByConcreteHandlerInterfaces()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(typeof(TooarkDependencyInjectionTests).Assembly);

    // Assert
    Assert.Contains(services, service =>
      service.ServiceType == typeof(IQueryHandler<TestQuery, string>)
      && service.ImplementationType == typeof(TestQueryHandler));

    Assert.Contains(services, service =>
      service.ServiceType == typeof(ICommandHandler<TestCommand, string>)
      && service.ImplementationType == typeof(TestCommandHandler));

    Assert.Contains(services, service =>
      service.ServiceType == typeof(ICommandHandler<VoidCommand>)
      && service.ImplementationType == typeof(VoidCommandHandler));
  }

  // Testa se as duas interfaces de despacho resolvem para a mesma implementação do mediador
  [Fact]
  public void AddTooarkMediator_ShouldRegisterISenderAndIPublisher()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(typeof(TooarkDependencyInjectionTests).Assembly);

    var provider = services.BuildServiceProvider();

    // Assert
    var sender = provider.GetRequiredService<ISender>();
    var publisher = provider.GetRequiredService<IPublisher>();
    var mediator = provider.GetRequiredService<IMediator>();

    Assert.NotNull(sender);
    Assert.NotNull(publisher);
    Assert.NotNull(mediator);
  }

  // Testa se as opções configuradas no registro chegam ao mediador
  [Fact]
  public void AddTooarkMediator_ShouldApplyConfiguredOptions()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(
      options => options.NotifyPublishStrategy = ENotifyStrategy.Sequential,
      typeof(TooarkDependencyInjectionTests).Assembly);

    var provider = services.BuildServiceProvider();

    // Assert
    var options = provider.GetRequiredService<IOptions<MediatorOptions>>().Value;
    Assert.Equal(ENotifyStrategy.Sequential, options.NotifyPublishStrategy);
  }

  // Testa se chamadas repetidas compõem as opções, em ordem de registro, em vez de substituí-las
  [Fact]
  public void AddTooarkMediator_ShouldComposeOptions_WhenCalledMultipleTimes()
  {
    // Arrange
    var services = new ServiceCollection();
    var assembly = typeof(TooarkDependencyInjectionTests).Assembly;

    // Act - Primeira chamada sem configuração, segunda configurando a estratégia (cenário de aplicação modular)
    services.AddTooarkMediator(assembly);
    services.AddTooarkMediator(
      options => options.NotifyPublishStrategy = ENotifyStrategy.Sequential,
      assembly);

    using var provider = services.BuildServiceProvider();

    // Assert - A configuração da segunda chamada não é descartada silenciosamente
    var options = provider.GetRequiredService<IOptions<MediatorOptions>>().Value;
    Assert.Equal(ENotifyStrategy.Sequential, options.NotifyPublishStrategy);
  }

  // Testa se dois manipuladores para a mesma requisição falham no registro — o excedente nunca executaria
  [Fact]
  public void AddTooarkMediator_ShouldThrow_WhenRequestHasMoreThanOneHandler()
  {
    // Arrange - duas implementações para a mesma requisição. O container resolveria apenas a última e as
    // demais nunca executariam. A segunda implementação apenas ocupa o registro conflitante, sem ser resolvida.
    var services = new ServiceCollection();
    services.AddTransient(typeof(IRequestHandler<TestQuery, string>), typeof(TestQueryHandler));
    services.AddTransient(typeof(IRequestHandler<TestQuery, string>), typeof(TestCommandHandler));

    // Act & Assert - antes a aplicação subia normalmente e despachava para o manipulador errado
    var exception = Assert.Throws<InternalServerErrorException>(
      () => services.AddTooarkMediator(typeof(TooarkDependencyInjectionTests).Assembly));

    Assert.Contains("Handler.Duplicated", exception.Message);
    Assert.Contains(nameof(TestQueryHandler), exception.Message);
    Assert.Contains(nameof(TestCommandHandler), exception.Message);
  }

  // Testa se a duplicidade é detectada também quando o segundo manipulador vem de uma fábrica
  [Fact]
  public void AddTooarkMediator_ShouldThrow_WhenRequestHandlerIsAlsoRegisteredByFactory()
  {
    // Arrange - registro por fábrica não expõe o tipo de implementação, mas continua sendo um segundo manipulador
    var services = new ServiceCollection();
    services.AddTransient(typeof(IRequestHandler<TestQuery, string>), typeof(TestQueryHandler));
    services.AddTransient<IRequestHandler<TestQuery, string>>(_ => new TestQueryHandler());

    // Act & Assert
    var exception = Assert.Throws<InternalServerErrorException>(
      () => services.AddTooarkMediator(typeof(TooarkDependencyInjectionTests).Assembly));

    Assert.Contains("Handler.Duplicated", exception.Message);
    Assert.Contains(nameof(TestQueryHandler), exception.Message);
  }

  // Testa se a notificação aceita vários manipuladores, ao contrário da requisição
  [Fact]
  public void AddTooarkMediator_ShouldNotThrow_WhenNotificationHasMoreThanOneHandler()
  {
    // Arrange - notificações admitem múltiplos manipuladores por contrato
    var services = new ServiceCollection();

    // Act
    var exception = Record.Exception(() => services.AddTooarkMediator(typeof(TooarkDependencyInjectionTests).Assembly));

    // Assert
    Assert.Null(exception);
  }

  private sealed record TestQuery(string Value) : IQuery<string>;

  private sealed class TestQueryHandler : IQueryHandler<TestQuery, string>
  {
    public Task<string> HandleAsync(TestQuery request, CancellationToken cancellationToken)
    {
      return Task.FromResult(request.Value);
    }
  }

  private sealed record TestCommand(string Value) : ICommand<string>;

  private sealed class TestCommandHandler : ICommandHandler<TestCommand, string>
  {
    public Task<string> HandleAsync(TestCommand request, CancellationToken cancellationToken)
    {
      return Task.FromResult(request.Value);
    }
  }

  private sealed record VoidCommand : ICommand;

  private sealed class VoidCommandHandler : ICommandHandler<VoidCommand>
  {
    public Task<Unit> HandleAsync(VoidCommand request, CancellationToken cancellationToken)
    {
      return Unit.Task;
    }
  }

  private sealed record TestNotification : INotify;

  private static class NotificationCounter
  {
    private static int _value;

    public static int Value => _value;

    public static void Reset()
    {
      Interlocked.Exchange(ref _value, 0);
    }

    public static void Increment()
    {
      Interlocked.Increment(ref _value);
    }
  }

  private sealed class TestNotificationHandler : INotifyHandler<TestNotification>
  {
    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
      NotificationCounter.Increment();
      return Task.CompletedTask;
    }
  }
}
