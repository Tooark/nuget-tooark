namespace Tooark.Notifications.Messages;

/// <summary>
/// Mensagens de erro para notificações.
/// </summary>
public static class NotificationErrorMessages
{
  /// <summary>
  /// Mensagem de erro para quando a mensagem da notificação é nula, vazia ou composta apenas por espaços em branco.
  /// </summary>
  public const string MessageIsNullOrEmpty = "Notifications.MessageNullEmpty";

  /// <summary>
  /// Mensagem de erro para quando a notificação recebida como argumento é nula.
  /// </summary>
  public const string NotificationIsNull = "Notifications.NotificationNull";
}
