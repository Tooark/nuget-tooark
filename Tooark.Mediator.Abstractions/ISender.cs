using Tooark.Exceptions;

namespace Tooark.Mediator.Abstractions;

/// <summary>
/// Interface para o remetente de requisições.
/// </summary>
/// <remarks>
/// O remetente é responsável por enviar requisições para os manipuladores correspondentes e obter
/// as respostas dessas requisições. Ele é uma parte fundamental do padrão Mediator, permitindo a comunicação
/// entre diferentes partes do sistema sem que elas precisem conhecer os detalhes umas das outras.
/// </remarks>
public interface ISender
{
  /// <summary>
  /// Envia uma requisição para o manipulador correspondente e retorna a resposta.
  /// </summary>
  /// <typeparam name="TResponse">O tipo de resposta esperada da requisição.</typeparam>
  /// <param name="request">A requisição a ser enviada.</param>
  /// <param name="cancellationToken">O token de cancelamento para a operação assíncrona. Opcional.</param>
  /// <returns>A resposta da requisição.</returns>
  /// <exception cref="BadRequestException">Lançada quando a requisição é nula.</exception>
  /// <exception cref="InternalServerErrorException">Lançada quando o manipulador da requisição não é encontrado.</exception>
  /// <exception cref="InternalServerErrorException">Lançada quando o manipulador retorna uma tarefa nula.</exception>
  Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
