using Tooark.Mediator.Abstractions;

namespace Tooark.Mediator.Behaviors;

/// <summary>
/// Representa a próxima etapa do pipeline de uma requisição.
/// </summary>
/// <remarks>
/// A etapa seguinte é o próximo behavior registrado ou, na última posição, o manipulador da requisição.
/// O token de cancelamento é obrigatório para que um behavior não descarte o cancelamento por engano —
/// para propagar o token recebido, repasse-o; para aplicar um limite próprio, informe um token encadeado.
/// </remarks>
/// <typeparam name="TResponse">O tipo de resposta da requisição.</typeparam>
/// <param name="cancellationToken">O token de cancelamento para a operação assíncrona.</param>
/// <returns>A resposta da etapa seguinte do pipeline.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);

/// <summary>
/// Interface para os behaviors do pipeline de requisições.
/// </summary>
/// <remarks>
/// Os behaviors envolvem a execução do manipulador da requisição, permitindo tratar preocupações
/// transversais — validação, log, transação, cache, autorização — sem repeti-las em cada manipulador.
/// Cada behavior decide se invoca a etapa seguinte: não invocar interrompe o pipeline (curto-circuito)
/// e a resposta do behavior é retornada ao chamador.
/// Os behaviors são executados na ordem de registro: o primeiro registrado é o mais externo.
/// Aplicam-se apenas a requisições; notificações não passam pelo pipeline.
/// </remarks>
/// <typeparam name="TRequest">O tipo de requisição que o behavior processa.</typeparam>
/// <typeparam name="TResponse">O tipo de resposta da requisição.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
  where TRequest : IRequest<TResponse>
{
  /// <summary>
  /// Processa a requisição, envolvendo a etapa seguinte do pipeline.
  /// </summary>
  /// <param name="request">A requisição a ser processada.</param>
  /// <param name="next">A etapa seguinte do pipeline.</param>
  /// <param name="cancellationToken">O token de cancelamento para a operação assíncrona. Opcional.</param>
  /// <returns>A resposta da requisição.</returns>
  Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default);
}
