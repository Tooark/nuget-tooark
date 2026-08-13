using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Enums;
using Tooark.Mediator.Handlers;

namespace Tooark.Mediator.Wrappers;

/// <summary>
/// Wrapper abstrato para publicação de notificações sem reflection por chamada.
/// </summary>
/// <remarks>
/// Uma instância fechada de <see cref="NotifyHandlerWrapperImpl{TNotify}"/> é criada uma única vez por tipo
/// de notificação e mantida em cache pelo <see cref="Mediator"/>. Após a primeira chamada, o despacho é uma
/// chamada virtual comum: sem reflection, sem <see cref="System.Reflection.TargetInvocationException"/>
/// e compatível com AOT/trimming.
/// </remarks>
internal abstract class NotifyHandlerWrapper
{
  /// <summary>
  /// Resolve os manipuladores da notificação e a publica conforme a estratégia.
  /// </summary>
  /// <param name="notify">A notificação a ser publicada.</param>
  /// <param name="serviceProvider">O provedor de serviços para resolver os manipuladores.</param>
  /// <param name="strategy">A estratégia de publicação de notificações.</param>
  /// <param name="cancellationToken">O token de cancelamento para a operação assíncrona.</param>
  /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
  public abstract Task PublishAsync(INotify notify, IServiceProvider serviceProvider, ENotifyStrategy strategy, CancellationToken cancellationToken);
}

/// <summary>
/// Implementação tipada do wrapper de notificações.
/// </summary>
/// <typeparam name="TNotify">O tipo concreto da notificação.</typeparam>
internal sealed class NotifyHandlerWrapperImpl<TNotify> : NotifyHandlerWrapper
  where TNotify : INotify
{
  /// <inheritdoc/>
  public override async Task PublishAsync(INotify notify, IServiceProvider serviceProvider, ENotifyStrategy strategy, CancellationToken cancellationToken)
  {
    // Converte a notificação para o tipo concreto e resolve os manipuladores registrados.
    var typedNotify = (TNotify)notify;
    var handlers = serviceProvider.GetServices<INotifyHandler<TNotify>>();

    // Sequencial: inicia cada manipulador somente após o anterior concluir (fail-fast em caso de falha).
    if (strategy == ENotifyStrategy.Sequential)
    {
      foreach (var handler in handlers)
      {
        // Invoca o manipulador diretamente. Uma tarefa nula indica implementação inválida do manipulador.
        var task = handler.HandleAsync(typedNotify, cancellationToken)
          ?? throw new InternalServerErrorException($"Handler.ExecutionFailed;{typeof(TNotify).FullName}");

        await task;
      }

      return;
    }

    // Paralelo: inicia todos os manipuladores e aguarda a conclusão de todos com WhenAll.
    var tasks = new List<Task>();

    foreach (var handler in handlers)
    {
      try
      {
        // Invoca o manipulador diretamente. Uma tarefa nula indica implementação inválida do manipulador.
        var task = handler.HandleAsync(typedNotify, cancellationToken)
          ?? throw new InternalServerErrorException($"Handler.ExecutionFailed;{typeof(TNotify).FullName}");

        tasks.Add(task);
      }
      catch (Exception exception)
      {
        // Falha ao iniciar o manipulador vira tarefa com falha: os demais continuam sendo iniciados e as
        // tarefas já em execução seguem observadas pelo WhenAll, em vez de serem abandonadas.
        tasks.Add(Task.FromException(exception));
      }
    }

    await Task.WhenAll(tasks);
  }
}
