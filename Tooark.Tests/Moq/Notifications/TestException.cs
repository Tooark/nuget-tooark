using Tooark.Notifications;

namespace Tooark.Tests.Moq.Notifications;

/// <summary>
/// Notificação concreta usada pelos testes das exceções do <c>Tooark.Exceptions</c>.
/// </summary>
/// <remarks>
/// Os mutadores de <see cref="Notification"/> são protegidos, então o teste precisa de um tipo
/// derivado para alimentar a notificação que será entregue à exceção. Era declarado dentro de cada
/// um dos quinze arquivos de teste, sempre igual.
/// </remarks>
public class TestException : Notification
{
  /// <summary>
  /// Expõe o mutador protegido para uso nos testes.
  /// </summary>
  /// <param name="notification">O item de notificação a ser acrescentado.</param>
  public new void AddNotification(NotificationItem notification) => base.AddNotification(notification);
}
