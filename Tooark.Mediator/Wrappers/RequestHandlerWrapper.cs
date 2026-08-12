using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
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

    // Invoca o manipulador diretamente. Uma tarefa nula indica implementação inválida do manipulador.
    return handler.HandleAsync((TRequest)request, cancellationToken)
      ?? throw new InternalServerErrorException($"Handler.ExecutionFailed;{typeof(TRequest).FullName}");
  }
}
