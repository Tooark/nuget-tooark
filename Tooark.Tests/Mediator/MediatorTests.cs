using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Enums;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Options;

namespace Tooark.Tests.Mediator;

public class MediatorTests
{
  // Testa o despacho da requisição para o manipulador registrado
  [Fact]
  public async Task Send_ShouldDispatchRequestToHandler()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<PingRequest, string>, PingRequestHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.SendAsync(
      new PingRequest("pong"),
      TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("pong", result);
  }

  // Testa se requisição nula é erro do chamador, e não falha interna
  [Fact]
  public async Task Send_ShouldThrowBadRequestException_WhenRequestIsNull()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    await Assert.ThrowsAsync<BadRequestException>(
      () => mediator.SendAsync<string>(null!, TestContext.Current.CancellationToken));
  }

  // Testa se requisição sem manipulador falha com contexto, e não com referência nula
  [Fact]
  public async Task Send_ShouldThrowInternalServerErrorException_WhenHandlerDoesNotExist()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(
      () => mediator.SendAsync(new PingRequest("pong"), TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Handler.NotFound", exception.Message);
  }

  // Testa se manipulador que devolve tarefa nula falha com contexto, em vez de estourar ao aguardar
  [Fact]
  public async Task Send_ShouldThrowInternalServerErrorException_WhenHandlerReturnsNullTask()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<NullTaskRequest, string>, NullTaskRequestHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(
      () => mediator.SendAsync(new NullTaskRequest(), TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Handler.ExecutionFailed", exception.Message);
  }

  // Testa se a notificação alcança todos os manipuladores registrados para ela
  [Fact]
  public async Task Publish_ShouldInvokeAllNotificationHandlers()
  {
    // Arrange
    TestNotificationCounter.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<TestNotification>, TestNotificationHandlerOne>();
    services.AddTransient<INotifyHandler<TestNotification>, TestNotificationHandlerTwo>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.PublishAsync(new TestNotification(), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(2, TestNotificationCounter.Value);
  }

  // Testa se notificação sem manipulador é ignorada, ao contrário da requisição, que exige um
  [Fact]
  public async Task Publish_ShouldNotThrow_WhenNoNotificationHandlerExists()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    await mediator.PublishAsync(new TestNotification(), TestContext.Current.CancellationToken);
  }

  // Testa se notificação nula é erro do chamador, e não falha interna
  [Fact]
  public async Task Publish_ShouldThrowBadRequestException_WhenNotificationIsNull()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    await Assert.ThrowsAsync<BadRequestException>(
      () => mediator.PublishAsync(null!, TestContext.Current.CancellationToken));
  }

  // Testa se manipulador de notificação que devolve tarefa nula falha com contexto
  [Fact]
  public async Task Publish_ShouldThrowInternalServerErrorException_WhenHandlerReturnsNullTask()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<NullTaskNotification>, NullTaskNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(
      () => mediator.PublishAsync(new NullTaskNotification(), TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Handler.ExecutionFailed", exception.Message);
  }

  // Testa se um manipulador que falha ao iniciar não impede os demais na estratégia paralela
  [Fact]
  public async Task Publish_ShouldStartRemainingHandlers_WhenHandlerFailsToStart_InParallelStrategy()
  {
    // Arrange - o primeiro handler falha ao iniciar (tarefa nula) e o segundo precisa ser iniciado mesmo assim
    TrackedNullTaskNotificationHandler.Executed = false;

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<NullTaskNotification>, NullTaskNotificationHandler>();
    services.AddTransient<INotifyHandler<NullTaskNotification>, TrackedNullTaskNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act - antes a falha do primeiro abortava a iteração e abandonava os manipuladores já iniciados
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(
      () => mediator.PublishAsync(new NullTaskNotification(), TestContext.Current.CancellationToken));

    // Assert - a falha continua chegando ao chamador e o segundo handler executou
    Assert.Contains("Handler.ExecutionFailed", exception.Message);
    Assert.True(TrackedNullTaskNotificationHandler.Executed);
  }

  // Testa se a tarefa nula também é detectada na estratégia sequencial
  [Fact]
  public async Task Publish_ShouldThrowInternalServerErrorException_WhenHandlerReturnsNullTask_InSequentialStrategy()
  {
    // Arrange
    var services = new ServiceCollection();
    services.Configure<MediatorOptions>(options => options.NotifyPublishStrategy = ENotifyStrategy.Sequential);
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<NullTaskNotification>, NullTaskNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(
      () => mediator.PublishAsync(new NullTaskNotification(), TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Handler.ExecutionFailed", exception.Message);
  }

  // Testa se a estratégia padrão executa os manipuladores em paralelo
  [Fact]
  public async Task Publish_ShouldRunHandlersInParallel_ByDefault()
  {
    // Arrange
    ParallelPublishProbe.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<INotifyHandler<ParallelProbeNotification>, BlockingProbeNotificationHandler>();
    services.AddTransient<INotifyHandler<ParallelProbeNotification>, FastProbeNotificationHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var publishTask = mediator.PublishAsync(new ParallelProbeNotification(), TestContext.Current.CancellationToken);
    await ParallelPublishProbe.WaitForFirstHandlerAsync();
    await Task.Delay(50, TestContext.Current.CancellationToken);

    // Assert
    Assert.True(ParallelPublishProbe.SecondHandlerStarted);

    // Cleanup
    ParallelPublishProbe.ReleaseFirstHandler();
    await publishTask;
  }

  // Testa se a estratégia sequencial respeita a ordem, um manipulador por vez
  [Fact]
  public async Task Publish_ShouldRunHandlersSequentially_WhenConfigured()
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

  private sealed record PingRequest(string Message) : IRequest<string>;

  private sealed class PingRequestHandler : IRequestHandler<PingRequest, string>
  {
    public Task<string> HandleAsync(PingRequest request, CancellationToken cancellationToken)
    {
      return Task.FromResult(request.Message);
    }
  }

  private sealed record NullTaskRequest : IRequest<string>;

  private sealed class NullTaskRequestHandler : IRequestHandler<NullTaskRequest, string>
  {
    public Task<string> HandleAsync(NullTaskRequest request, CancellationToken cancellationToken)
    {
      return null!;
    }
  }

  private sealed record TestNotification : INotify;

  private static class TestNotificationCounter
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

  private sealed class TestNotificationHandlerOne : INotifyHandler<TestNotification>
  {
    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
      TestNotificationCounter.Increment();
      return Task.CompletedTask;
    }
  }

  private sealed class TestNotificationHandlerTwo : INotifyHandler<TestNotification>
  {
    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
      TestNotificationCounter.Increment();
      return Task.CompletedTask;
    }
  }

  private sealed record NullTaskNotification : INotify;

  private sealed class NullTaskNotificationHandler : INotifyHandler<NullTaskNotification>
  {
    public Task HandleAsync(NullTaskNotification notification, CancellationToken cancellationToken)
    {
      return null!;
    }
  }

  // Registra a execução para verificar que a falha de um manipulador não impede os demais de iniciar
  private sealed class TrackedNullTaskNotificationHandler : INotifyHandler<NullTaskNotification>
  {
    public static bool Executed { get; set; }

    public Task HandleAsync(NullTaskNotification notification, CancellationToken cancellationToken)
    {
      Executed = true;

      return Task.CompletedTask;
    }
  }

  private sealed record ParallelProbeNotification : INotify;

  private static class ParallelPublishProbe
  {
    private static TaskCompletionSource<bool> _firstHandlerStarted =
      new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<bool> _releaseFirstHandler =
      new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static int _secondHandlerStarted;

    public static bool SecondHandlerStarted => Volatile.Read(ref _secondHandlerStarted) == 1;

    public static void Reset()
    {
      _firstHandlerStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      _releaseFirstHandler = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      Interlocked.Exchange(ref _secondHandlerStarted, 0);
    }

    public static void MarkFirstHandlerStarted()
    {
      _firstHandlerStarted.TrySetResult(true);
    }

    public static void MarkSecondHandlerStarted()
    {
      Interlocked.Exchange(ref _secondHandlerStarted, 1);
    }

    public static Task WaitForReleaseAsync()
    {
      return _releaseFirstHandler.Task;
    }

    public static async Task WaitForFirstHandlerAsync()
    {
      using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
      await _firstHandlerStarted.Task.WaitAsync(timeoutCts.Token);
    }

    public static void ReleaseFirstHandler()
    {
      _releaseFirstHandler.TrySetResult(true);
    }
  }

  private sealed class BlockingProbeNotificationHandler : INotifyHandler<ParallelProbeNotification>
  {
    public async Task HandleAsync(ParallelProbeNotification notification, CancellationToken cancellationToken)
    {
      ParallelPublishProbe.MarkFirstHandlerStarted();
      await ParallelPublishProbe.WaitForReleaseAsync().WaitAsync(cancellationToken);
    }
  }

  private sealed class FastProbeNotificationHandler : INotifyHandler<ParallelProbeNotification>
  {
    public Task HandleAsync(ParallelProbeNotification notification, CancellationToken cancellationToken)
    {
      ParallelPublishProbe.MarkSecondHandlerStarted();
      return Task.CompletedTask;
    }
  }
}
