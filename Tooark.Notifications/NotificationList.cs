namespace Tooark.Notifications;

/// <summary>
/// Notificação montada a partir de itens que já existem.
/// </summary>
/// <remarks>
/// A <see cref="Notification"/> só recebe itens por métodos protegidos, porque quem notifica é o próprio objeto.
/// Esta classe cobre o caso em que os itens vêm de fora do objeto e precisam seguir como uma notificação: os erros
/// por campo de uma validação, os erros devolvidos por outro serviço, uma lista montada pela aplicação. As exceções
/// do Tooark aceitam uma <see cref="Notification"/> e preservam a chave e o código de cada item, então
/// <c>new BadRequestException(new NotificationList(itens))</c> leva a lista inteira até a resposta.
/// </remarks>
public sealed class NotificationList : Notification
{
  #region Constructor

  /// <summary>
  /// Cria a notificação com os itens informados, na ordem em que vieram.
  /// </summary>
  /// <param name="notifications">Itens de notificação. Itens nulos são ignorados.</param>
  /// <exception cref="ArgumentNullException">Se a coleção for nula.</exception>
  public NotificationList(IEnumerable<NotificationItem> notifications)
  {
    ArgumentNullException.ThrowIfNull(notifications);

    foreach (var notification in notifications)
    {
      // Um item nulo não descreve erro algum: fica de fora, como na agregação de coleções da base
      if (notification is not null)
      {
        AddNotification(notification);
      }
    }
  }

  #endregion
}
