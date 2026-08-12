using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Enums;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Options;

namespace Tooark.Tests.Mediator;

public class MediatorTests
{
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

  [Fact]
  public async Task Publish_ShouldThrowInternalServerErrorException_WhenHandlerReturnsNullTask_InSequentialStrategy()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton(new MediatorOptions
    {
      NotifyPublishStrategy = ENotifyStrategy.Sequential
    });
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

  [Fact]
  public async Task Publish_ShouldRunHandlersSequentially_WhenConfigured()
  {
    // Arrange
    ParallelPublishProbe.Reset();

    var services = new ServiceCollection();
    services.AddSingleton(new MediatorOptions
    {
      NotifyPublishStrategy = ENotifyStrategy.Sequential
    });
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
