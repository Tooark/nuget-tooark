using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Handlers;

namespace Tooark.Tests.Mediator;

#region Requests & Handlers

/// <summary>
/// Request de teste para enviar ao mediador.
/// </summary>
public sealed record PingRequest(string Response) : IRequest<string>;

/// <summary>
/// Handler de teste para PingRequest.
/// </summary>
public sealed class PingRequestHandler : IRequestHandler<PingRequest, string>
{
  public Task<string> HandleAsync(PingRequest request, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(request.Response);
  }
}

/// <summary>
/// Request que retorna uma tarefa nula (para testar erro).
/// </summary>
public sealed record NullTaskRequest : IRequest<string>;

/// <summary>
/// Handler que retorna tarefa nula (para testar erro).
/// </summary>
public sealed class NullTaskRequestHandler : IRequestHandler<NullTaskRequest, string>
{
  public Task<string> HandleAsync(NullTaskRequest request, CancellationToken cancellationToken = default)
  {
    return null!;
  }
}

/// <summary>
/// Request que não encontra handler.
/// </summary>
public sealed record NoHandlerRequest : IRequest<string>;

#endregion

#region Notifications & Handlers

/// <summary>
/// Notificação de teste.
/// </summary>
public sealed record TestNotification : INotify;

/// <summary>
/// Primeiro handler de notificação de teste.
/// </summary>
public sealed class TestNotificationHandlerOne : INotifyHandler<TestNotification>
{
  public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken = default)
  {
    TestNotificationCounter.Increment();
    return Task.CompletedTask;
  }
}

/// <summary>
/// Segundo handler de notificação de teste.
/// </summary>
public sealed class TestNotificationHandlerTwo : INotifyHandler<TestNotification>
{
  public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken = default)
  {
    TestNotificationCounter.Increment();
    return Task.CompletedTask;
  }
}

/// <summary>
/// Contador para notificações de teste.
/// </summary>
public static class TestNotificationCounter
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

/// <summary>
/// Handler genérico aberto (deve ser ignorado pelo scan de registro, pois o dispatch não suporta open generics).
/// </summary>
/// <typeparam name="T">Parâmetro genérico aberto.</typeparam>
public sealed class OpenGenericNotifyHandler<T> : INotifyHandler<TestNotification>
{
  public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }
}

/// <summary>
/// Notificação que retorna tarefa nula (para testar erro).
/// </summary>
public sealed record NullTaskNotification : INotify;

/// <summary>
/// Handler que retorna tarefa nula (para testar erro).
/// </summary>
public sealed class NullTaskNotificationHandler : INotifyHandler<NullTaskNotification>
{
  public Task HandleAsync(NullTaskNotification notification, CancellationToken cancellationToken = default)
  {
    return null!;
  }
}

#endregion

#region Parallel Execution Probes

/// <summary>
/// Notificação para testar execução paralela.
/// </summary>
public sealed record ParallelProbeNotification : INotify;

/// <summary>
/// Probe que bloqueia a execução.
/// </summary>
public sealed class BlockingProbeNotificationHandler : INotifyHandler<ParallelProbeNotification>
{
  public async Task HandleAsync(ParallelProbeNotification notification, CancellationToken cancellationToken = default)
  {
    ParallelPublishProbe.SetFirstHandlerStarted();
    await ParallelPublishProbe.WaitForReleaseAsync();
  }
}

/// <summary>
/// Probe de execução rápida.
/// </summary>
public sealed class FastProbeNotificationHandler : INotifyHandler<ParallelProbeNotification>
{
  public Task HandleAsync(ParallelProbeNotification notification, CancellationToken cancellationToken = default)
  {
    ParallelPublishProbe.SetSecondHandlerStarted();
    return Task.CompletedTask;
  }
}

/// <summary>
/// Probe para validar execução paralela vs sequencial.
/// </summary>
public static class ParallelPublishProbe
{
  private static bool _secondHandlerStarted;
  private static TaskCompletionSource<bool> _firstHandlerWaiter = new();
  private static TaskCompletionSource<bool> _releaseWaiter = new();

  public static bool SecondHandlerStarted => _secondHandlerStarted;

  public static void Reset()
  {
    _secondHandlerStarted = false;
    _firstHandlerWaiter = new TaskCompletionSource<bool>();
    _releaseWaiter = new TaskCompletionSource<bool>();
  }

  public static void SetFirstHandlerStarted()
  {
    _firstHandlerWaiter.TrySetResult(true);
  }

  public static void SetSecondHandlerStarted()
  {
    _secondHandlerStarted = true;
  }

  public static Task WaitForFirstHandlerAsync()
  {
    return _firstHandlerWaiter.Task;
  }

  public static void ReleaseFirstHandler()
  {
    _releaseWaiter.TrySetResult(true);
  }

  public static async Task WaitForReleaseAsync()
  {
    await _releaseWaiter.Task;
  }
}

#endregion

#region Cancellation Token Tests

/// <summary>
/// Request para testar CancellationToken.
/// </summary>
public sealed record CancellationTestRequest(CancellationToken ExpectedToken) : IRequest<bool>;

/// <summary>
/// Handler que valida CancellationToken.
/// </summary>
public sealed class CancellationTestHandler : IRequestHandler<CancellationTestRequest, bool>
{
  public Task<bool> HandleAsync(CancellationTestRequest request, CancellationToken cancellationToken = default)
  {
    var tokensMatch = request.ExpectedToken == cancellationToken;
    return Task.FromResult(tokensMatch);
  }
}

/// <summary>
/// Request que simula cancelamento.
/// </summary>
public sealed record CanceledRequest : IRequest<Unit>;

/// <summary>
/// Handler que simula cancelamento.
/// </summary>
public sealed class CanceledRequestHandler : IRequestHandler<CanceledRequest, Unit>
{
  public async Task<Unit> HandleAsync(CanceledRequest request, CancellationToken cancellationToken = default)
  {
    // Aguarda indefinidamente até o cancelamento, garantindo o TaskCanceledException de forma
    // determinística (sem corrida entre o delay do handler e o timer do CancellationTokenSource).
    await Task.Delay(Timeout.Infinite, cancellationToken);
    return Unit.Value;
  }
}

/// <summary>
/// Notificação para testar CancellationToken.
/// </summary>
public sealed record CancellationTokenNotification(CancellationToken ExpectedToken) : INotify;

/// <summary>
/// Handler que valida CancellationToken em notificação.
/// </summary>
public sealed class CancellationTokenNotificationHandler : INotifyHandler<CancellationTokenNotification>
{
  public Task HandleAsync(CancellationTokenNotification notification, CancellationToken cancellationToken = default)
  {
    CancellationTokenTracker.ReceivedToken = cancellationToken;
    return Task.CompletedTask;
  }
}

/// <summary>
/// Rastreador de CancellationToken.
/// </summary>
public static class CancellationTokenTracker
{
  public static CancellationToken ReceivedToken { get; set; }
}

#endregion

#region Error Handling Tests

/// <summary>
/// Request que lança exceção no handler.
/// </summary>
public sealed record ExceptionThrowingRequest : IRequest<string>;

/// <summary>
/// Handler que lança exceção.
/// </summary>
public sealed class ExceptionThrowingHandler : IRequestHandler<ExceptionThrowingRequest, string>
{
  public Task<string> HandleAsync(ExceptionThrowingRequest request, CancellationToken cancellationToken = default)
  {
    throw new InvalidOperationException("Handler intentionally threw an exception");
  }
}

/// <summary>
/// Notificação que lança exceção no handler.
/// </summary>
public sealed record ExceptionThrowingNotification : INotify;

/// <summary>
/// Handler que lança exceção em notificação.
/// </summary>
public sealed class ExceptionThrowingNotificationHandler : INotifyHandler<ExceptionThrowingNotification>
{
  public Task HandleAsync(ExceptionThrowingNotification notification, CancellationToken cancellationToken = default)
  {
    throw new InvalidOperationException("Notification handler intentionally threw an exception");
  }
}

#endregion

#region Query & Command Tests

/// <summary>
/// Query de teste simples.
/// </summary>
public sealed record SimpleQuery(string Value) : IQuery<string>;

/// <summary>
/// Handler para SimpleQuery.
/// </summary>
public sealed class SimpleQueryHandler : IQueryHandler<SimpleQuery, string>
{
  public Task<string> HandleAsync(SimpleQuery request, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(request.Value);
  }
}

/// <summary>
/// Command de teste simples.
/// </summary>
public sealed record SimpleCommand(string Value) : ICommand<string>;

/// <summary>
/// Handler para SimpleCommand.
/// </summary>
public sealed class SimpleCommandHandler : ICommandHandler<SimpleCommand, string>
{
  public Task<string> HandleAsync(SimpleCommand request, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(request.Value);
  }
}

/// <summary>
/// Command que não retorna resposta.
/// </summary>
public sealed record VoidCommand : ICommand;

/// <summary>
/// Handler para VoidCommand.
/// </summary>
public sealed class VoidCommandHandler : ICommandHandler<VoidCommand>
{
  public Task<Unit> HandleAsync(VoidCommand request, CancellationToken cancellationToken = default)
  {
    return Unit.Task;
  }
}

#endregion
