using Tooark.Notifications;

namespace Tooark.Http.Internal;

/// <summary>
/// Notificação que carrega os erros devolvidos pelo serviço chamado até a exceção.
/// </summary>
/// <remarks>
/// A <see cref="Notification"/> só aceita itens por métodos protegidos, e a exceção do Tooark só preserva o código e
/// a chave de cada erro quando recebe uma notificação.
/// </remarks>
internal sealed class UpstreamNotification : Notification
{
  /// <summary>
  /// Monta a notificação com os erros do serviço chamado, ou com a mensagem padrão quando não há nenhum.
  /// </summary>
  /// <param name="notifications">Os erros do serviço chamado.</param>
  /// <param name="defaultMessage">A mensagem usada quando não há erros.</param>
  /// <returns>A notificação, com ao menos um item.</returns>
  internal static UpstreamNotification From(IEnumerable<NotificationItem>? notifications, string defaultMessage)
  {
    var notification = new UpstreamNotification();

    foreach (var item in notifications ?? [])
    {
      if (item is not null)
      {
        notification.AddNotification(item);
      }
    }

    // Uma exceção de erro sem erro algum cairia na chave genérica da base, que não diz o que aconteceu
    if (notification.Count == 0)
    {
      notification.AddNotification(new NotificationItem(defaultMessage));
    }

    return notification;
  }
}
