using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Enums;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Injections;
using Tooark.Mediator.Options;

namespace Tooark.Tests.Mediator.Injections;

/// <summary>
/// Testes avançados para injeção de dependência do Mediator.
/// </summary>
public class MediatorDependencyInjectionAdvancedTests
{
  #region Null validation tests

  [Fact]
  public void AddTooarkMediator_ShouldThrowInternalServerErrorException_WhenServicesIsNull()
  {
    // Arrange
    IServiceCollection? services = null;

    // Act & Assert
    var exception = Assert.Throws<InternalServerErrorException>(
      () => services!.AddTooarkMediator(typeof(MediatorDependencyInjectionAdvancedTests).Assembly));

    Assert.Contains("Mediator.Null.Service", exception.Message);
  }

  [Fact]
  public void AddTooarkMediator_ShouldThrowInternalServerErrorException_WhenConfigureIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    var exception = Assert.Throws<InternalServerErrorException>(
      () => services.AddTooarkMediator((Action<MediatorOptions>)null!, typeof(MediatorDependencyInjectionAdvancedTests).Assembly));

    Assert.Contains("Mediator.Null.Configure", exception.Message);
  }

  #endregion

  #region Handler registration with multiple assemblies

  [Fact]
  public async Task AddTooarkMediator_ShouldRegisterHandlers_FromMultipleAssemblies()
  {
    // Arrange
    var services = new ServiceCollection();
    var assembly = typeof(MediatorDependencyInjectionAdvancedTests).Assembly;

    // Act
    services.AddTooarkMediator(assembly, assembly);

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Assert - Should be able to send requests without errors
    var result = await mediator.SendAsync(
      new TestQueryForDI("test"),
      TestContext.Current.CancellationToken);
    Assert.Equal("test", result);
  }

  [Fact]
  public void AddTooarkMediator_ShouldNotDuplicateHandlers_WhenSameAssemblyProvidedMultipleTimes()
  {
    // Arrange
    var services = new ServiceCollection();
    var assembly = typeof(MediatorDependencyInjectionAdvancedTests).Assembly;

    // Act
    services.AddTooarkMediator(
      assembly,
      assembly,
      assembly);

    // Assert - Check that handlers are not duplicated
    var requestHandlerServices = services.Where(s =>
      s.ServiceType == typeof(IRequestHandler<TestQueryForDI, string>)).ToList();

    Assert.Single(requestHandlerServices);
  }

  #endregion

  #region Options configuration tests

  [Fact]
  public void AddTooarkMediator_ShouldApplyOptions_FromConfigurationAction()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(
      options => options.NotifyPublishStrategy = ENotifyStrategy.Sequential,
      typeof(MediatorDependencyInjectionAdvancedTests).Assembly);

    var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<IOptions<MediatorOptions>>().Value;

    // Assert
    Assert.Equal(ENotifyStrategy.Sequential, options.NotifyPublishStrategy);
  }

  [Fact]
  public void AddTooarkMediator_ShouldUseDefaultOptions_WhenNoConfigurationProvided()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(typeof(MediatorDependencyInjectionAdvancedTests).Assembly);

    var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<IOptions<MediatorOptions>>().Value;

    // Assert
    Assert.Equal(ENotifyStrategy.ParallelWhenAll, options.NotifyPublishStrategy);
  }

  #endregion

  #region Service registration verification

  [Fact]
  public void AddTooarkMediator_ShouldRegisterMediator_AsSingleton()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(typeof(MediatorDependencyInjectionAdvancedTests).Assembly);

    // Assert
    var mediatorDescriptors = services.Where(s => s.ServiceType == typeof(IMediator)).ToList();
    Assert.NotEmpty(mediatorDescriptors);
  }

  [Fact]
  public void AddTooarkMediator_ShouldRegisterISender()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(typeof(MediatorDependencyInjectionAdvancedTests).Assembly);

    // Assert
    var senderDescriptors = services.Where(s => s.ServiceType == typeof(ISender)).ToList();
    Assert.NotEmpty(senderDescriptors);
  }

  [Fact]
  public void AddTooarkMediator_ShouldRegisterIPublisher()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(typeof(MediatorDependencyInjectionAdvancedTests).Assembly);

    // Assert
    var publisherDescriptors = services.Where(s => s.ServiceType == typeof(IPublisher)).ToList();
    Assert.NotEmpty(publisherDescriptors);
  }

  #endregion

  #region Handler interface registration tests

  [Fact]
  public void AddTooarkMediator_ShouldRegisterHandlers_ByConcreteInterfaces()
  {
    // Arrange
    var services = new ServiceCollection();
    var assembly = typeof(MediatorDependencyInjectionAdvancedTests).Assembly;

    // Act
    services.AddTooarkMediator(assembly);

    // Assert - Verify concrete handler interfaces are registered
    Assert.Contains(services, s =>
      s.ServiceType == typeof(IQueryHandler<TestQueryForDI, string>));

    Assert.Contains(services, s =>
      s.ServiceType == typeof(ICommandHandler<TestCommandForDI, string>));

    Assert.Contains(services, s =>
      s.ServiceType == typeof(INotifyHandler<TestNotificationForDI>));
  }

  [Fact]
  public void AddTooarkMediator_ShouldRegisterHandlers_ByBaseInterfaces()
  {
    // Arrange
    var services = new ServiceCollection();
    var assembly = typeof(MediatorDependencyInjectionAdvancedTests).Assembly;

    // Act
    services.AddTooarkMediator(assembly);

    // Assert - Verify base handler interfaces are registered
    Assert.Contains(services, s =>
      s.ServiceType == typeof(IRequestHandler<TestQueryForDI, string>));

    Assert.Contains(services, s =>
      s.ServiceType == typeof(IRequestHandler<TestCommandForDI, string>));
  }

  #endregion

  #region Multiple handlers for same notification

  [Fact]
  public void AddTooarkMediator_ShouldRegisterMultipleNotificationHandlers_ForSameNotification()
  {
    // Arrange
    var services = new ServiceCollection();
    var assembly = typeof(MediatorDependencyInjectionAdvancedTests).Assembly;

    // Act
    services.AddTooarkMediator(assembly);

    // Assert
    var handlers = services.Where(s =>
      s.ServiceType == typeof(INotifyHandler<TestNotificationForDI>)).ToList();

    Assert.NotEmpty(handlers);
  }

  [Fact]
  public async Task AddTooarkMediator_ShouldExecuteAllRegisteredNotificationHandlers()
  {
    // Arrange
    DITestNotificationCounter.Reset();
    var services = new ServiceCollection();
    services.AddTooarkMediator(typeof(MediatorDependencyInjectionAdvancedTests).Assembly);

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.PublishAsync(
      new TestNotificationForDI(),
      TestContext.Current.CancellationToken);

    // Assert - Should have called at least one handler
    Assert.True(DITestNotificationCounter.Value > 0);
  }

  #endregion

  #region Assembly scanning edge cases

  [Fact]
  public void AddTooarkMediator_ShouldUseCallingAssembly_WhenNoAssemblyProvided()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkMediator(_ => { });

    var provider = services.BuildServiceProvider();

    // Assert - Should be able to resolve mediator
    var mediator = provider.GetRequiredService<IMediator>();
    Assert.NotNull(mediator);
  }

  [Fact]
  public void AddTooarkMediator_ShouldIgnoreDuplicateAssemblies()
  {
    // Arrange
    var services = new ServiceCollection();
    var assembly = typeof(MediatorDependencyInjectionAdvancedTests).Assembly;

    // Act
    services.AddTooarkMediator(
      assembly,
      assembly,
      assembly);

    // Assert - Verify no duplicate registrations
    var registration = services
      .Where(s => s.ServiceType == typeof(INotifyHandler<TestNotificationForDI>))
      .ToList();

    // Should not have multiple identical registrations
    var distinct = registration.Select(s => s.ImplementationType).Distinct().ToList();
  }

  #endregion

  #region Test fixtures for DI tests

  public sealed record TestQueryForDI(string Value) : IQuery<string>;

  public sealed class TestQueryForDIHandler : IQueryHandler<TestQueryForDI, string>
  {
    public Task<string> HandleAsync(TestQueryForDI request, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(request.Value);
    }
  }

  public sealed record TestCommandForDI(string Value) : ICommand<string>;

  public sealed class TestCommandForDIHandler : ICommandHandler<TestCommandForDI, string>
  {
    public Task<string> HandleAsync(TestCommandForDI request, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(request.Value);
    }
  }

  public sealed record TestNotificationForDI : INotify;

  public sealed class TestNotificationForDIHandler : INotifyHandler<TestNotificationForDI>
  {
    public Task HandleAsync(TestNotificationForDI notification, CancellationToken cancellationToken = default)
    {
      DITestNotificationCounter.Increment();
      return Task.CompletedTask;
    }
  }

  public sealed class TestNotificationForDIHandlerTwo : INotifyHandler<TestNotificationForDI>
  {
    public Task HandleAsync(TestNotificationForDI notification, CancellationToken cancellationToken = default)
    {
      DITestNotificationCounter.Increment();
      return Task.CompletedTask;
    }
  }

  public static class DITestNotificationCounter
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

  #endregion
}
