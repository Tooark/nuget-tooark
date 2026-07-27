using System.ComponentModel.DataAnnotations.Schema;
using Tooark.Notifications.Messages;

namespace Tooark.Notifications;

/// <summary>
/// Representa um objeto de notificações.
/// </summary>
public abstract class Notification
{
  /// <summary>
  /// Lista privada de notificações.
  /// </summary>
  private readonly IList<NotificationItem> _notifications;


  /// <summary>
  /// Construtor padrão da classe protegido para evitar instâncias diretas.
  /// </summary>
  protected Notification() => _notifications = [];


  /// <summary>
  /// Retorna a lista de notificações.
  /// </summary>
  /// <returns>Lista de notificações.</returns>
  [NotMapped]
  public IReadOnlyCollection<NotificationItem> Notifications => _notifications.AsReadOnly();


  /// <summary>
  /// Retorna True se a lista de notificações estiver vazia por nenhum erro ter gerado notificação.
  /// </summary>
  [NotMapped]
  public bool IsValid => _notifications.Count == 0;

  /// <summary>
  /// Retornar o tamanho da lista de notificações.
  /// </summary>
  [NotMapped]
  public long Count => _notifications.Count;

  /// <summary>
  /// Retorna a lista de códigos de erros das notificações.
  /// </summary>
  /// <returns>Lista de códigos de erros das notificações.</returns>
  [NotMapped]
  public IReadOnlyList<string> Codes => _notifications.Select(x => x.Code).ToList().AsReadOnly();

  /// <summary>
  /// Retorna a lista de chaves das notificações.
  /// </summary>
  /// <returns>Lista de chaves das notificações.</returns>
  [NotMapped]
  public IReadOnlyList<string> Keys => _notifications.Select(x => x.Key).ToList().AsReadOnly();

  /// <summary>
  /// Retorna a lista de mensagens das notificações.
  /// </summary>
  /// <returns>Lista de mensagens das notificações.</returns>
  [NotMapped]
  public IReadOnlyList<string> Messages => _notifications.Select(x => x.Message).ToList().AsReadOnly();


  /// <summary>
  /// Cria uma nova instância de NotificationItem.
  /// </summary>
  /// <param name="message">Mensagem da notificação.</param>
  /// <param name="key">Chave da notificação. Padrão é nulo.</param>
  /// <param name="code">Código de erro da notificação. Padrão é nulo.</param>
  /// <returns>Instância de NotificationItem.</returns>
  private static NotificationItem GetNotificationInstance(string message, string? key = null, string? code = null)
  {
    // Retorna uma nova instância de NotificationItem
    return (NotificationItem)Activator.CreateInstance(typeof(NotificationItem), message, key, code)!;
  }


  /// <summary>
  /// Adiciona uma nova notificação à lista de notificações.
  /// </summary>
  /// <param name="notification">Instância de notificação.</param>
  public void AddNotification(NotificationItem notification)
  {
    // Verifica se a instância não é nula
    if (notification != null)
    {
      // Adiciona a nova instância à lista de notificações
      _notifications.Add(notification);
    }
    else
    {
      // Cria uma nova instância de NotificationItem
      var newNotification = GetNotificationInstance(NotificationErrorMessages.NotificationIsNull);

      // Adiciona a nova instância à lista de notificações
      _notifications.Add(newNotification);
    }
  }

  /// <summary>
  /// Adiciona uma coleção de notificações à lista de notificações.
  /// </summary>
  /// <param name="property">Propriedade que gerou a notificação.</param>
  /// <param name="message">Mensagem da notificação.</param>
  public void AddNotification(Type property, string message) =>
    AddNotification(message, property?.Name ?? string.Empty);

  /// <summary>
  /// Adiciona uma coleção de notificações à lista de notificações.
  /// </summary>
  /// <param name="property">Propriedade que gerou a notificação.</param>
  /// <param name="message">Mensagem da notificação.</param>
  /// <param name="code">Código de erro da notificação.</param>
  public void AddNotification(Type property, string message, string code) =>
    AddNotification(message, property?.Name ?? string.Empty, code);

  /// <summary>
  /// Adiciona uma nova notificação à lista de notificações.
  /// </summary>
  /// <param name="key">Chave da notificação.</param>
  /// <param name="message">Mensagem da notificação.</param>
  public void AddNotification(string message, string key)
  {
    // Verifica se a mensagem é nula ou vazia
    if (string.IsNullOrEmpty(message))
    {
      // Atribui a mensagem de message nula ou vazia
      message = NotificationErrorMessages.MessageIsNullOrEmpty;
    }

    // Cria uma nova instância de NotificationItem
    var notification = GetNotificationInstance(message, key);

    // Adiciona a nova instância à lista de notificações
    _notifications.Add(notification);
  }

  /// <summary>
  /// Adiciona uma nova notificação à lista de notificações.
  /// </summary>
  /// <param name="key">Chave da notificação.</param>
  /// <param name="message">Mensagem da notificação.</param>
  /// <param name="code">Código de erro da notificação.</param>
  public void AddNotification(string message, string key, string code)
  {
    // Verifica se a mensagem é nula ou vazia
    if (string.IsNullOrEmpty(message))
    {
      // Atribui a mensagem de message nula ou vazia
      message = NotificationErrorMessages.MessageIsNullOrEmpty;
    }

    // Cria uma nova instância de NotificationItem
    var notification = GetNotificationInstance(message, key, code);

    // Adiciona a nova instância à lista de notificações
    _notifications.Add(notification);
  }

  /// <summary>
  /// Adiciona uma coleção de notificações à lista de notificações.
  /// </summary>
  /// <param name="notifications">Lista de notificações.</param>
  public void AddNotifications(ICollection<NotificationItem> notifications)
  {
    // Itera sobre a coleção de notificações
    foreach (var notification in notifications)
    {
      // Adiciona a notificação à lista de notificações
      _notifications.Add(notification);
    }
  }

  /// <summary>
  /// Adiciona a lista de notificações de uma notificação à lista de notificações.
  /// </summary>
  /// <param name="notification">Uma instancia de notificação.</param>
  public void AddNotifications(Notification notification)
  {
    // Verifica se o notificável não é nulo
    if (notification != null)
    {
      // Adiciona as notificações do notificável à lista de notificações
      AddNotifications(notification.Notifications.ToList());
    }
    else
    {
      // Cria uma nova instância de NotificationItem
      var newNotification = GetNotificationInstance(NotificationErrorMessages.NotificationIsNull);

      // Adiciona a nova instância à lista de notificações
      _notifications.Add(newNotification);
    }
  }

  /// <summary>
  /// Adiciona uma coleção de notificações à lista de notificações.
  /// </summary>
  /// <param name="notifications">Coleção de notificações.</param>
  public void AddNotifications(params Notification[] notifications)
  {
    // Itera sobre a coleção de notificações
    foreach (var notification in notifications)
    {
      // Verifica se a notificação não é nula
      if (notification != null)
      {
        // Adiciona o notificável à lista de notificações
        AddNotifications(notification);
      }
    }
  }

  /// <summary>
  /// Limpa a lista de notificações.
  /// </summary>
  public void Clear()
  {
    // Limpa a lista de notificações
    _notifications.Clear();
  }
}
