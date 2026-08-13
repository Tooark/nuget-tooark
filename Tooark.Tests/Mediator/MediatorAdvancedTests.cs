using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Enums;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Options;

namespace Tooark.Tests.Mediator;

/// <summary>
/// Testes avançados do mediador para cobrir cenários complexos e edge cases.
/// </summary>
public class MediatorAdvancedTests
{
  #region CancellationToken Tests

  [Fact]
  public async Task SendAsync_ShouldPassCancellationTokenToHandler()
  {
    // Arrange
    var cts = new CancellationTokenSource();
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<CancellationTestRequest, bool>, CancellationTestHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.SendAsync(
      new CancellationTestRequest(cts.Token),
      cts.Token);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public async Task SendAsync_ShouldRespectCancellation()
  {
    // Arrange
    var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<CanceledRequest, Unit>, CanceledRequestHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    await Assert.ThrowsAsync<TaskCanceledException>(
      () => mediator.SendAsync(new CanceledRequest(), cts.Token));
  }

  [Fact]
  public async Task PublishAsync_ShouldPassCancellationTokenToHandlers()
  {
    // Arrange
    CancellationTokenTracker.ReceivedToken = default;
    var cts = new CancellationTokenSource();
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<CancellationTokenNotification>, CancellationTokenNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.PublishAsync(
      new CancellationTokenNotification(cts.Token),
      cts.Token);

    // Assert
    Assert.Equal(cts.Token, CancellationTokenTracker.ReceivedToken);
  }

  #endregion

  #region Sequential Strategy Tests

  [Fact]
  public async Task PublishAsync_ShouldExecuteSequentially_WhenStrategyIsSequential()
  {
    // Arrange
    ParallelPublishProbe.Reset();

    var services = new ServiceCollection();
    services.Configure<MediatorOptions>(options => options.NotifyPublishStrategy = ENotifyStrategy.Sequential);
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<ParallelProbeNotification>, BlockingProbeNotificationHandler>();
    services.AddTransient<INotifyHandler<ParallelProbeNotification>, FastProbeNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var publishTask = mediator.PublishAsync(new ParallelProbeNotification(), TestContext.Current.CancellationToken);
    await ParallelPublishProbe.WaitForFirstHandlerAsync();
    await Task.Delay(50, TestContext.Current.CancellationToken);

    // Assert - Sequencial: o segundo handler NÃO inicia enquanto o primeiro não concluir.
    Assert.False(ParallelPublishProbe.SecondHandlerStarted);

    // Libera o primeiro handler e aguarda a publicação completar
    ParallelPublishProbe.ReleaseFirstHandler();
    await publishTask;

    // Assert - Após a conclusão do primeiro, o segundo executa normalmente.
    Assert.True(ParallelPublishProbe.SecondHandlerStarted);
  }

  [Fact]
  public async Task PublishAsync_ShouldExecuteParallel_WhenStrategyIsParallelWhenAll()
  {
    // Arrange
    ParallelPublishProbe.Reset();

    var services = new ServiceCollection();
    services.Configure<MediatorOptions>(options => options.NotifyPublishStrategy = ENotifyStrategy.ParallelWhenAll);
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<ParallelProbeNotification>, BlockingProbeNotificationHandler>();
    services.AddTransient<INotifyHandler<ParallelProbeNotification>, FastProbeNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var publishTask = mediator.PublishAsync(new ParallelProbeNotification(), TestContext.Current.CancellationToken);
    await ParallelPublishProbe.WaitForFirstHandlerAsync();
    await Task.Delay(50, TestContext.Current.CancellationToken);

    // Assert - Second handler SHOULD have started (parallel)
    Assert.True(ParallelPublishProbe.SecondHandlerStarted);

    // Cleanup
    ParallelPublishProbe.ReleaseFirstHandler();
    await publishTask;
  }

  #endregion

  #region Error Handling Tests

  [Fact]
  public async Task SendAsync_ShouldThrowException_WhenHandlerThrows()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<ExceptionThrowingRequest, string>, ExceptionThrowingHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - A exceção do handler chega diretamente ao chamador (sem TargetInvocationException)
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
      () => mediator.SendAsync(new ExceptionThrowingRequest(), TestContext.Current.CancellationToken));

    Assert.Contains("Handler intentionally threw an exception", exception.Message);
  }

  [Fact]
  public async Task PublishAsync_ShouldThrowException_WhenHandlerThrows()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<ExceptionThrowingNotification>, ExceptionThrowingNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - A exceção do handler chega diretamente ao chamador (sem TargetInvocationException)
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
      () => mediator.PublishAsync(new ExceptionThrowingNotification(), TestContext.Current.CancellationToken));

    Assert.Contains("Notification handler intentionally threw an exception", exception.Message);
  }

  [Fact]
  public async Task PublishAsync_ShouldThrowInFirstExceptionOccurred_WhenParallel()
  {
    // Arrange
    var services = new ServiceCollection();
    services.Configure<MediatorOptions>(options => options.NotifyPublishStrategy = ENotifyStrategy.ParallelWhenAll);
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<ExceptionThrowingNotification>, ExceptionThrowingNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - A exceção do handler chega diretamente ao chamador (sem TargetInvocationException)
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => mediator.PublishAsync(new ExceptionThrowingNotification(), TestContext.Current.CancellationToken));
  }

  #endregion

  #region SendAsync with null validation

  [Fact]
  public async Task SendAsync_ShouldThrowBadRequestException_WhenRequestIsNull()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<BadRequestException>(
      () => mediator.SendAsync<string>(null!, TestContext.Current.CancellationToken));

    Assert.Contains("Request.Null", exception.Message);
  }

  #endregion

  #region PublishAsync with null validation

  [Fact]
  public async Task PublishAsync_ShouldThrowBadRequestException_WhenNotificationIsNull()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<BadRequestException>(
      () => mediator.PublishAsync(null!, TestContext.Current.CancellationToken));

    Assert.Contains("Notify.Null", exception.Message);
  }

  #endregion

  #region Handler resolution edge cases

  [Fact]
  public async Task SendAsync_ShouldThrowInternalServerErrorException_WhenHandlerNotFound()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    // Não registra o handler

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(
      () => mediator.SendAsync(new NoHandlerRequest(), TestContext.Current.CancellationToken));

    Assert.Contains("Handler.NotFound", exception.Message);
  }

  #endregion

  #region Multiple notification handlers with errors

  [Fact]
  public async Task PublishAsync_ShouldFailFast_WhenFirstHandlerThrowsInParallel()
  {
    // Arrange
    var services = new ServiceCollection();
    services.Configure<MediatorOptions>(options => options.NotifyPublishStrategy = ENotifyStrategy.ParallelWhenAll);
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<ExceptionThrowingNotification>, ExceptionThrowingNotificationHandler>();
    services.AddTransient<INotifyHandler<ExceptionThrowingNotification>, ExceptionThrowingNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert - A exceção do handler chega diretamente ao chamador (sem TargetInvocationException)
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => mediator.PublishAsync(new ExceptionThrowingNotification(), TestContext.Current.CancellationToken));
  }

  #endregion

  #region Complex handlers with business logic

  [Fact]
  public async Task SendAsync_ShouldIntegrateQueryHandlerCorrectly()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<SimpleQuery, string>, SimpleQueryHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.SendAsync(new SimpleQuery("test-query"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("test-query", result);
  }

  [Fact]
  public async Task SendAsync_ShouldIntegrateCommandHandlerCorrectly()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<SimpleCommand, string>, SimpleCommandHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.SendAsync(new SimpleCommand("test-command"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("test-command", result);
  }

  [Fact]
  public async Task SendAsync_ShouldHandleVoidCommand()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<VoidCommand, Unit>, VoidCommandHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.SendAsync(new VoidCommand(), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(Unit.Value, result);
  }

  #endregion

  #region MediatorOptions tests

  [Fact]
  public void MediatorOptions_DefaultStrategy_ShouldBeParallelWhenAll()
  {
    // Arrange
    var options = new MediatorOptions();

    // Assert
    Assert.Equal(ENotifyStrategy.ParallelWhenAll, options.NotifyPublishStrategy);
  }

  [Fact]
  public void MediatorOptions_ShouldAllowStrategyChange()
  {
    // Arrange
    var options = new MediatorOptions();

    // Act
    options.NotifyPublishStrategy = ENotifyStrategy.Sequential;

    // Assert
    Assert.Equal(ENotifyStrategy.Sequential, options.NotifyPublishStrategy);
  }

  #endregion

  #region Notification strategy enum tests

  [Fact]
  public void ENotifyStrategy_ParallelWhenAll_ShouldHaveValue0()
  {
    // Assert
    Assert.Equal(0, (int)ENotifyStrategy.ParallelWhenAll);
  }

  [Fact]
  public void ENotifyStrategy_Sequential_ShouldHaveValue1()
  {
    // Assert
    Assert.Equal(1, (int)ENotifyStrategy.Sequential);
  }

  #endregion

  #region Mediator constructor tests

  [Fact]
  public void Mediator_Constructor_WithDefaultOptions_ShouldWork()
  {
    // Arrange
    var services = new ServiceCollection();
    var provider = services.BuildServiceProvider();

    // Act
    var mediator = new global::Tooark.Mediator.Mediator(provider);

    // Assert
    Assert.NotNull(mediator);
  }

  [Fact]
  public void Mediator_Constructor_WithCustomOptions_ShouldWork()
  {
    // Arrange
    var services = new ServiceCollection();
    var provider = services.BuildServiceProvider();
    var options = new MediatorOptions
    {
      NotifyPublishStrategy = ENotifyStrategy.Sequential
    };

    // Act - as opções são recebidas pelo padrão Options
    var mediator = new global::Tooark.Mediator.Mediator(provider, Options.Create(options));

    // Assert
    Assert.NotNull(mediator);
  }

  #endregion

  #region Publisher/Sender interface tests

  [Fact]
  public async Task IPublisher_ShouldBeAccessibleFromMediator()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

    var provider = services.BuildServiceProvider();
    var publisher = provider.GetRequiredService<IPublisher>();

    // Act & Assert
    await publisher.PublishAsync(new TestNotification(), TestContext.Current.CancellationToken);
  }

  [Fact]
  public async Task ISender_ShouldBeAccessibleFromMediator()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
    services.AddTransient<IRequestHandler<PingRequest, string>, PingRequestHandler>();

    var provider = services.BuildServiceProvider();
    var sender = provider.GetRequiredService<ISender>();

    // Act
    var result = await sender.SendAsync(new PingRequest("test"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("test", result);
  }

  #endregion
}
