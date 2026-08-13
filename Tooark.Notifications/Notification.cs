using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using Tooark.Notifications.Messages;

namespace Tooark.Notifications;

/// <summary>
/// Representa um objeto de notificações.
/// </summary>
/// <remarks>
/// A criação e a limpeza de notificações são protegidas: apenas o próprio objeto decide o que notifica.
/// A agregação de notificações já existentes (<see cref="AddNotifications(Notification)"/>) é pública,
/// permitindo compor o resultado de validações de outros objetos.
/// </remarks>
public abstract class Notification
{
  /// <summary>
  /// Lista privada de notificações.
  /// </summary>
  private readonly List<NotificationItem> _notifications = [];

  /// <summary>
  /// Visão somente leitura da lista de notificações, reutilizada a cada acesso.
  /// </summary>
  private readonly ReadOnlyCollection<NotificationItem> _readOnlyNotifications;


  /// <summary>
  /// Construtor padrão da classe protegido para evitar instâncias diretas.
  /// </summary>
  protected Notification() => _readOnlyNotifications = _notifications.AsReadOnly();


  /// <summary>
  /// Retorna a lista de notificações.
  /// </summary>
  /// <returns>Lista de notificações.</returns>
  [NotMapped]
  public IReadOnlyCollection<NotificationItem> Notifications => _readOnlyNotifications;


  /// <summary>
  /// Retorna True se a lista de notificações estiver vazia por nenhum erro ter gerado notificação.
  /// </summary>
  [NotMapped]
  public bool IsValid => _notifications.Count == 0;

  /// <summary>
  /// Retornar o tamanho da lista de notificações.
  /// </summary>
  [NotMapped]
  public int Count => _notifications.Count;

  /// <summary>
  /// Retorna a lista de códigos de erros das notificações.
  /// </summary>
  /// <returns>Lista de códigos de erros das notificações.</returns>
  [NotMapped]
  public IReadOnlyList<string> Codes => [.. _notifications.Select(notification => notification.Code)];

  /// <summary>
  /// Retorna a lista de chaves das notificações.
  /// </summary>
  /// <returns>Lista de chaves das notificações.</returns>
  [NotMapped]
  public IReadOnlyList<string> Keys => [.. _notifications.Select(notification => notification.Key)];

  /// <summary>
  /// Retorna a lista de mensagens das notificações.
  /// </summary>
  /// <returns>Lista de mensagens das notificações.</returns>
  [NotMapped]
  public IReadOnlyList<string> Messages => [.. _notifications.Select(notification => notification.Message)];


  /// <summary>
  /// Adiciona a notificação de argumento nulo à lista de notificações.
  /// </summary>
  private void AddNullArgumentNotification() =>
    _notifications.Add(new NotificationItem(NotificationErrorMessages.NotificationIsNull));


  /// <summary>
  /// Adiciona um item de notificação à lista de notificações.
  /// </summary>
  /// <param name="notification">Instância de notificação. Nula gera a notificação 'Notifications.NotificationNull'.</param>
  protected void AddNotification(NotificationItem notification)
  {
    // Verifica se a instância não é nula
    if (notification != null)
    {
      // Adiciona a nova instância à lista de notificações
      _notifications.Add(notification);
    }
    else
    {
      // Adiciona a notificação de argumento nulo à lista de notificações
      AddNullArgumentNotification();
    }
  }

  /// <summary>
  /// Adiciona um item de notificação à lista de notificações, usando o nome do tipo como chave.
  /// </summary>
  /// <param name="property">Tipo que gerou a notificação.</param>
  /// <param name="message">Mensagem da notificação.</param>
  protected void AddNotification(Type property, string message) =>
    AddNotification(message, property?.Name ?? string.Empty);

  /// <summary>
  /// Adiciona um item de notificação à lista de notificações, usando o nome do tipo como chave.
  /// </summary>
  /// <param name="property">Tipo que gerou a notificação.</param>
  /// <param name="message">Mensagem da notificação.</param>
  /// <param name="code">Código de erro da notificação.</param>
  protected void AddNotification(Type property, string message, string code) =>
    AddNotification(message, property?.Name ?? string.Empty, code);

  /// <summary>
  /// Adiciona um item de notificação à lista de notificações.
  /// </summary>
  /// <param name="message">Mensagem da notificação.</param>
  /// <param name="key">Chave da notificação.</param>
  protected void AddNotification(string message, string key) =>
    _notifications.Add(new NotificationItem(message, key));

  /// <summary>
  /// Adiciona um item de notificação à lista de notificações.
  /// </summary>
  /// <param name="message">Mensagem da notificação.</param>
  /// <param name="key">Chave da notificação.</param>
  /// <param name="code">Código de erro da notificação.</param>
  protected void AddNotification(string message, string key, string code) =>
    _notifications.Add(new NotificationItem(message, key, code));

  /// <summary>
  /// Adiciona uma coleção de itens de notificação à lista de notificações.
  /// </summary>
  /// <param name="notifications">Coleção de itens de notificação. Itens nulos são ignorados.</param>
  protected void AddNotifications(ICollection<NotificationItem> notifications)
  {
    // Verifica se a coleção é nula
    if (notifications == null)
    {
      // Adiciona a notificação de argumento nulo à lista de notificações
      AddNullArgumentNotification();

      return;
    }

    // Itera sobre a coleção de notificações
    foreach (var notification in notifications)
    {
      // Ignora itens nulos para não invalidar a leitura das notificações
      if (notification != null)
      {
        // Adiciona a notificação à lista de notificações
        _notifications.Add(notification);
      }
    }
  }

  /// <summary>
  /// Adiciona a lista de notificações de uma notificação à lista de notificações.
  /// </summary>
  /// <param name="notification">Uma instancia de notificação. Nula gera a notificação 'Notifications.NotificationNull'.</param>
  /// <remarks>Adicionar a própria instância não altera a lista de notificações.</remarks>
  public void AddNotifications(Notification notification)
  {
    // Verifica se o notificável é nulo
    if (notification == null)
    {
      // Adiciona a notificação de argumento nulo à lista de notificações
      AddNullArgumentNotification();

      return;
    }

    // Adicionar a própria instância duplicaria as notificações e alteraria a lista durante a iteração
    if (ReferenceEquals(this, notification))
    {
      return;
    }

    // Itera sobre as notificações do notificável
    foreach (var item in notification._notifications)
    {
      // Adiciona a notificação à lista de notificações
      _notifications.Add(item);
    }
  }

  /// <summary>
  /// Adiciona uma coleção de notificações à lista de notificações.
  /// </summary>
  /// <param name="notifications">Coleção de notificações. Itens nulos são ignorados.</param>
  public void AddNotifications(params Notification[] notifications)
  {
    // Verifica se a coleção é nula
    if (notifications == null)
    {
      // Adiciona a notificação de argumento nulo à lista de notificações
      AddNullArgumentNotification();

      return;
    }

    // Itera sobre a coleção de notificações
    foreach (var notification in notifications)
    {
      // Ignora itens nulos para não gerar notificações de argumento nulo em lote
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
  protected void Clear()
  {
    // Limpa a lista de notificações
    _notifications.Clear();
  }
}
