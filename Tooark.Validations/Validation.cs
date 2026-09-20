using Tooark.Notifications;

namespace Tooark.Validations;

/// <summary>
/// Classe base de Validação
/// </summary>
public partial class Validation : Notification
{
  #region Default Validation
  /// <summary>
  /// Agrega as notificações das validações informadas que estiverem inválidas.
  /// </summary>
  /// <param name="notifications">Notificações a serem agregadas. Coleção e itens nulos são ignorados.</param>
  /// <returns>Validação.</returns>
  public Validation Join(params Notification[] notifications)
  {
    // Se a lista de notificações for nula, retorna a instância atual
    if (notifications == null)
    {
      return this;
    }

    // Percorre a lista de notificações
    foreach (var notification in notifications)
    {
      // Se a notificação for válida ou nula, não há o que agregar
      if (notification == null || notification.IsValid)
      {
        continue;
      }

      // Adiciona a notificação
      AddNotifications(notification);
    }

    // Retorna a instância atual
    return this;
  }
  #endregion

  #region Validates
  /// <summary>
  /// Função para validar nulo.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="returnAwait">Retorno aguardado da comparação.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  private Validation ValidateNull<T>(T? value, bool returnAwait, string property, string message)
  {
    // Se a condição for verdadeira, adicione a notificação.
    if (EqualityComparer<T>.Default.Equals(value, default) == returnAwait)
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.NUL1");
    }

    // Retorna uma validação.
    return this;
  }
  #endregion
}
