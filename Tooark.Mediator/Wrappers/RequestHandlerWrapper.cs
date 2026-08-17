using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Behaviors;
using Tooark.Mediator.Handlers;

namespace Tooark.Mediator.Wrappers;

/// <summary>
/// Wrapper abstrato para despacho de requisições sem reflection por chamada.
/// </summary>
/// <remarks>
/// Uma instância fechada de <see cref="RequestHandlerWrapperImpl{TRequest, TResponse}"/> é criada uma única
/// vez por tipo de requisição e mantida em cache pelo <see cref="Mediator"/>. Após a primeira chamada, o
/// despacho é uma chamada virtual comum: sem reflection, sem <see cref="System.Reflection.TargetInvocationException"/>
/// e compatível com AOT/trimming.
/// Quando há behaviors registrados para a requisição, o wrapper monta a cadeia do pipeline em volta do
/// manipulador; sem behaviors, o despacho permanece sendo a invocação direta do manipulador.
/// </remarks>
/// <typeparam name="TResponse">O tipo de resposta da requisição.</typeparam>
internal abstract class RequestHandlerWrapper<TResponse>
{
  /// <summary>
  /// Resolve o manipulador da requisição e a processa.
  /// </summary>
  /// <param name="request">A requisição a ser processada.</param>
  /// <param name="serviceProvider">O provedor de serviços para resolver o manipulador.</param>
  /// <param name="cancellationToken">O token de cancelamento para a operação assíncrona.</param>
  /// <returns>A resposta da requisição.</returns>
  public abstract Task<TResponse> HandleAsync(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Implementação tipada do wrapper de requisições.
/// </summary>
/// <typeparam name="TRequest">O tipo concreto da requisição.</typeparam>
/// <typeparam name="TResponse">O tipo de resposta da requisição.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
  where TRequest : IRequest<TResponse>
{
  /// <inheritdoc/>
  public override Task<TResponse> HandleAsync(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
  {
    // Tenta resolver o manipulador do provedor de serviços. Se não encontrar, lança uma exceção.
    var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
      ?? throw new InternalServerErrorException($"Handler.NotFound;{typeof(TRequest).FullName}");

    // Converte a requisição para o tipo concreto uma única vez, reaproveitando em toda a cadeia.
    var typedRequest = (TRequest)request;

    // Resolve os behaviors registrados para a requisição, sem materializar quando já é uma lista.
    var resolved = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
    var behaviors = resolved as IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> ?? [.. resolved];

    // Sem behaviors registrados, o despacho continua sendo a invocação direta do manipulador.
    if (behaviors.Count == 0)
    {
      return Invoke(handler, typedRequest, cancellationToken);
    }

    // Etapa final do pipeline: o manipulador da requisição.
    RequestHandlerDelegate<TResponse> next = token => Invoke(handler, typedRequest, token);

    // Encadeia do último para o primeiro, de modo que a execução siga a ordem de registro.
    for (var index = behaviors.Count - 1; index >= 0; index--)
    {
      var behavior = behaviors[index];
      var current = next;

      next = token => behavior.HandleAsync(typedRequest, current, token)
        ?? throw new InternalServerErrorException($"Behavior.ExecutionFailed;{behavior.GetType().FullName}");
    }

    return next(cancellationToken);
  }

  /// <summary>
  /// Invoca o manipulador da requisição.
  /// </summary>
  /// <param name="handler">O manipulador da requisição.</param>
  /// <param name="request">A requisição a ser processada.</param>
  /// <param name="cancellationToken">O token de cancelamento para a operação assíncrona.</param>
  /// <returns>A resposta da requisição.</returns>
  /// <exception cref="InternalServerErrorException">Lançada quando o manipulador retorna uma tarefa nula.</exception>
  private static Task<TResponse> Invoke(IRequestHandler<TRequest, TResponse> handler, TRequest request, CancellationToken cancellationToken)
  {
    // Invoca o manipulador diretamente. Uma tarefa nula indica implementação inválida do manipulador.
    return handler.HandleAsync(request, cancellationToken)
      ?? throw new InternalServerErrorException($"Handler.ExecutionFailed;{typeof(TRequest).FullName}");
  }
}
