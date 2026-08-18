using System.Collections.ObjectModel;
using System.Net;
using Tooark.Exceptions.Messages;
using Tooark.Notifications;

namespace Tooark.Exceptions;

/// <summary>
/// Classe base para exceções do Tooark.
/// </summary>
/// <remarks>
/// Concentra as mensagens de erro, as notificações equivalentes e o contrato de código HTTP.
/// As coleções expostas são somente leitura e nunca ficam vazias: entrada nula, vazia ou sem
/// mensagens é registrada com uma chave conhecida de <see cref="ExceptionErrorMessages"/>, em vez
/// de produzir uma exceção que se diz de erro mas não carrega erro algum.
/// As duas leituras andam juntas — a mesma posição descreve o mesmo erro em ambas.
/// </remarks>
public abstract class TooarkException : Exception
{
  #region Private Properties

  /// <summary>
  /// Lista de mensagens de erro.
  /// </summary>
  private readonly List<string> _errors;

  /// <summary>
  /// Lista de itens de notificação.
  /// </summary>
  private readonly List<NotificationItem> _notifications;

  /// <summary>
  /// Coleção somente leitura das mensagens de erro, reutilizada a cada leitura.
  /// </summary>
  private readonly ReadOnlyCollection<string> _readOnlyErrors;

  /// <summary>
  /// Coleção somente leitura dos itens de notificação, reutilizada a cada leitura.
  /// </summary>
  private readonly ReadOnlyCollection<NotificationItem> _readOnlyNotifications;

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor central da exceção.
  /// </summary>
  /// <remarks>
  /// Recebe as listas já normalizadas, com ao menos um item, e é o único ponto que define o estado
  /// da exceção — os demais construtores apenas montam as listas e delegam para cá.
  /// </remarks>
  /// <param name="errors">Mensagens de erro já normalizadas.</param>
  /// <param name="notifications">Itens de notificação, ou nulo para derivá-los das mensagens.</param>
  /// <param name="innerException">A exceção que originou esta exceção.</param>
  private TooarkException(List<string> errors, List<NotificationItem>? notifications, Exception? innerException)
    : base(errors[0], innerException)
  {
    _errors = errors;

    // Sem itens próprios, cada mensagem vira um item de notificação, mantendo as duas listas alinhadas
    _notifications = notifications ?? [.. errors.Select(error => new NotificationItem(error))];

    _readOnlyErrors = _errors.AsReadOnly();
    _readOnlyNotifications = _notifications.AsReadOnly();
  }

  /// <summary>
  /// Construtor da classe com mensagem única.
  /// </summary>
  /// <param name="message">Mensagem de erro. Nula, vazia ou em branco assume 'Exceptions.MessageNullEmpty'.</param>
  protected TooarkException(string message)
    : this(BuildErrors(message), null, null) { }

  /// <summary>
  /// Construtor da classe com mensagem única e exceção interna.
  /// </summary>
  /// <param name="message">Mensagem de erro. Nula, vazia ou em branco assume 'Exceptions.MessageNullEmpty'.</param>
  /// <param name="innerException">A exceção que originou esta exceção, preservada para diagnóstico.</param>
  protected TooarkException(string message, Exception? innerException)
    : this(BuildErrors(message), null, innerException) { }

  /// <summary>
  /// Construtor da classe com lista de mensagens.
  /// </summary>
  /// <param name="errors">Lista de mensagens de erro. Nula ou vazia assume 'Exceptions.ErrorsNullOrEmpty'.</param>
  protected TooarkException(IList<string> errors)
    : this(BuildErrors(errors), null, null) { }

  /// <summary>
  /// Construtor da classe com notificação.
  /// </summary>
  /// <remarks>
  /// Os itens da notificação são preservados como estão, mantendo a chave e o código de cada um.
  /// </remarks>
  /// <param name="notification">Notificação com as mensagens de erro. Nula ou sem itens assume 'Exceptions.ErrorsNullOrEmpty'.</param>
  protected TooarkException(Notification notification)
    : this(BuildErrors(notification), BuildNotifications(notification), null) { }

  /// <summary>
  /// Construtor da classe com suporte a formatação de mensagem.
  /// </summary>
  /// <remarks>
  /// Quando o formato e os parâmetros não são compatíveis, a mensagem é mantida como recebida em vez
  /// de a formatação lançar: a exceção existe para reportar o erro original, não para falhar na
  /// própria apresentação. Mensagens com chaves literais, como JSON, caem nesse caso.
  /// </remarks>
  /// <param name="messageFormat">Formato da mensagem de erro com marcadores {0}, {1}, etc.</param>
  /// <param name="args">Parâmetros para substituição nos marcadores.</param>
  protected TooarkException(string messageFormat, params object[] args)
    : this(BuildErrors(Format(messageFormat, args)), null, null) { }

  #endregion

  #region Methods

  /// <summary>
  /// Função para obter as mensagens de erro.
  /// </summary>
  /// <returns>Coleção somente leitura das mensagens de erro, com ao menos um item.</returns>
  public IReadOnlyList<string> GetErrorMessages() => _readOnlyErrors;

  /// <summary>
  /// Função para obter os itens de notificação.
  /// </summary>
  /// <returns>Coleção somente leitura dos itens de notificação, com ao menos um item.</returns>
  public IReadOnlyList<NotificationItem> GetNotifications() => _readOnlyNotifications;

  /// <summary>
  /// Função abstrata para obter o status code da exceção.
  /// </summary>
  /// <returns>Status code da exceção.</returns>
  public abstract HttpStatusCode GetStatusCode();

  #endregion

  #region Private Methods

  /// <summary>
  /// Aplica o formato à mensagem, mantendo-a como recebida quando o formato não é compatível.
  /// </summary>
  /// <param name="messageFormat">Formato da mensagem de erro.</param>
  /// <param name="args">Parâmetros para substituição nos marcadores.</param>
  /// <returns>Mensagem formatada, ou o formato original quando a formatação não é possível.</returns>
  private static string Format(string messageFormat, object[] args)
  {
    // Sem formato ou sem parâmetros não há o que substituir
    if (string.IsNullOrWhiteSpace(messageFormat) || args is not { Length: > 0 })
    {
      return messageFormat;
    }

    try
    {
      return string.Format(messageFormat, args);
    }
    catch (FormatException)
    {
      // Formato incompatível não pode substituir a exceção pedida por uma FormatException
      return messageFormat;
    }
  }

  /// <summary>
  /// Monta a lista de mensagens a partir de uma mensagem única.
  /// </summary>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Lista com a mensagem normalizada.</returns>
  private static List<string> BuildErrors(string message) => [Normalize(message)];

  /// <summary>
  /// Monta a lista de mensagens a partir de uma coleção.
  /// </summary>
  /// <param name="errors">Lista de mensagens de erro.</param>
  /// <returns>Lista normalizada, com ao menos um item.</returns>
  private static List<string> BuildErrors(IList<string> errors)
  {
    // Coleção ausente ou vazia registra a chave conhecida, evitando uma exceção sem erro
    if (errors is not { Count: > 0 })
    {
      return [ExceptionErrorMessages.ErrorsIsNullOrEmpty];
    }

    return [.. errors.Select(Normalize)];
  }

  /// <summary>
  /// Monta a lista de mensagens a partir de uma notificação.
  /// </summary>
  /// <param name="notification">Notificação com as mensagens de erro.</param>
  /// <returns>Lista normalizada, com ao menos um item.</returns>
  private static List<string> BuildErrors(Notification notification)
  {
    var messages = notification?.Messages;

    // Notificação ausente ou sem itens registra a chave conhecida
    if (messages is not { Count: > 0 })
    {
      return [ExceptionErrorMessages.ErrorsIsNullOrEmpty];
    }

    return [.. messages.Select(Normalize)];
  }

  /// <summary>
  /// Monta a lista de itens de notificação a partir de uma notificação.
  /// </summary>
  /// <param name="notification">Notificação com os itens de erro.</param>
  /// <returns>Cópia dos itens da notificação, ou nulo para derivá-los das mensagens.</returns>
  private static List<NotificationItem>? BuildNotifications(Notification notification)
  {
    var notifications = notification?.Notifications;

    // Sem itens próprios, o construtor central deriva a notificação da chave conhecida
    return notifications is { Count: > 0 } ? [.. notifications] : null;
  }

  /// <summary>
  /// Normaliza a mensagem de erro, aplicando a chave conhecida quando não há conteúdo.
  /// </summary>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Mensagem sem espaços nas extremidades, ou 'Exceptions.MessageNullEmpty' quando não há conteúdo.</returns>
  private static string Normalize(string message) =>
    string.IsNullOrWhiteSpace(message) ?
    ExceptionErrorMessages.MessageIsNullOrEmpty :
    message.Trim();

  #endregion
}
