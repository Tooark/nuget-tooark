using System.Net;
using Tooark.Notifications;

namespace Tooark.Exceptions;

/// <summary>
/// Representa uma exceção de acesso não autorizado (Unauthorized).
/// </summary>
public class UnauthorizedException : TooarkException
{
  /// <summary>
  /// Construtor padrão da exceção com mensagem única.
  /// </summary>
  /// <param name="message">A mensagem de erro associada à exceção.</param>
  public UnauthorizedException(string message) : base(message) { }

  /// <summary>
  /// Construtor da exceção com mensagem única e exceção interna.
  /// </summary>
  /// <param name="message">A mensagem de erro associada à exceção.</param>
  /// <param name="innerException">A exceção que originou esta exceção, preservada para diagnóstico.</param>
  public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }

  /// <summary>
  /// Construtor padrão da exceção com lista de mensagens.
  /// </summary>
  /// <param name="messages">A lista de mensagens de erros associadas à exceção.</param>
  public UnauthorizedException(IList<string> messages) : base(messages) { }

  /// <summary>
  /// Construtor padrão da exceção com notificação.
  /// </summary>
  /// <param name="notification">A notificação com as mensagens de erros associadas à exceção.</param>
  public UnauthorizedException(Notification notification) : base(notification) { }

  /// <summary>
  /// Construtor com suporte a formatação de mensagem.
  /// </summary>
  /// <param name="messageFormat">Formato da mensagem de erro com placeholders {0}, {1}, etc.</param>
  /// <param name="args">Parâmetros para substituição nos placeholders.</param>
  public UnauthorizedException(string messageFormat, params object[] args) : base(messageFormat, args) { }


  /// <summary>
  /// Obtém o código de status HTTP associado à exceção.
  /// </summary>
  /// <returns>O código de status HTTP 401 (Unauthorized).</returns>
  public override HttpStatusCode GetStatusCode() => HttpStatusCode.Unauthorized;
}
