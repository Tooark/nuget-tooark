using Tooark.Exceptions;

namespace Tooark.Mediator.Abstractions;

/// <summary>
/// Interface para o publicador de notificações.
/// </summary>
/// <remarks>
/// O publicador é responsável por publicar notificações para os manipuladores correspondentes. Ele é
/// uma parte fundamental do padrão Mediator, permitindo a comunicação entre diferentes partes do sistema
/// sem que elas precisem conhecer os detalhes umas das outras.
/// </remarks>
public interface IPublisher
{
  /// <summary>
  /// Publica uma notificação para todos os manipuladores de notificações registrados.
  /// </summary>
  /// <param name="notify">A notificação a ser publicada.</param>
  /// <param name="cancellationToken">O token de cancelamento para a operação assíncrona. Opcional.</param>
  /// <exception cref="BadRequestException">Lançada quando a notificação é nula.</exception>
  /// <exception cref="InternalServerErrorException">Lançada quando um manipulador de notificações retorna uma tarefa nula.</exception>
  Task PublishAsync(INotify notify, CancellationToken cancellationToken = default);
}
