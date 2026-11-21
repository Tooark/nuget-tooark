using System.Net;
using Tooark.Exceptions;
using Tooark.Http.Internal;
using Tooark.Notifications;

namespace Tooark.Http.Exceptions;

/// <summary>
/// Representa uma resposta de erro do serviço chamado.
/// </summary>
/// <remarks>
/// É uma <see cref="BadGatewayException"/>: a resposta da aplicação continua 502, porque um 404 ou um 401 do serviço
/// chamado não é um 404 nem um 401 de quem chamou a aplicação. O status original fica em
/// <see cref="UpstreamStatusCode"/>. Quando o serviço chamado responde com o Problem Details do Tooark, os erros dele
/// chegam como itens de notificação, com o código e a chave originais; nos demais casos há um único item, com a chave
/// <c>Http.RequestFailed</c> e o status como parâmetro. O corpo bruto da resposta nunca entra na mensagem nem nas
/// notificações: fica em <see cref="UpstreamBody"/>, para a aplicação ler o que o serviço chamado disse e decidir o
/// que repassa.
/// </remarks>
public sealed class HttpRequestFailedException : BadGatewayException
{
  #region Constructors

  /// <summary>
  /// Cria a exceção com a mensagem padrão, <c>Http.RequestFailed</c> com o status como parâmetro.
  /// </summary>
  /// <param name="upstreamStatusCode">O status devolvido pelo serviço chamado.</param>
  /// <param name="requestUri">A URI da requisição, quando conhecida.</param>
  public HttpRequestFailedException(HttpStatusCode upstreamStatusCode, Uri? requestUri = null)
    : base(DefaultMessage(upstreamStatusCode))
  {
    UpstreamStatusCode = upstreamStatusCode;
    RequestUri = requestUri;
  }

  /// <summary>
  /// Cria a exceção com os erros devolvidos pelo serviço chamado.
  /// </summary>
  /// <param name="upstreamStatusCode">O status devolvido pelo serviço chamado.</param>
  /// <param name="requestUri">A URI da requisição, quando conhecida.</param>
  /// <param name="notifications">
  /// Os erros do serviço chamado. Nulo ou vazio assume a mensagem padrão, <c>Http.RequestFailed</c>.
  /// </param>
  public HttpRequestFailedException(
    HttpStatusCode upstreamStatusCode,
    Uri? requestUri,
    IEnumerable<NotificationItem>? notifications
  ) : base(UpstreamNotification.From(notifications, DefaultMessage(upstreamStatusCode)))
  {
    UpstreamStatusCode = upstreamStatusCode;
    RequestUri = requestUri;
  }

  /// <summary>
  /// Cria a exceção com os erros e o corpo devolvidos pelo serviço chamado.
  /// </summary>
  /// <param name="upstreamStatusCode">O status devolvido pelo serviço chamado.</param>
  /// <param name="requestUri">A URI da requisição, quando conhecida.</param>
  /// <param name="notifications">
  /// Os erros do serviço chamado. Nulo ou vazio assume a mensagem padrão, <c>Http.RequestFailed</c>.
  /// </param>
  /// <param name="upstreamBody">O corpo da resposta de erro, como texto. Vazio ou em branco vira nulo.</param>
  public HttpRequestFailedException(
    HttpStatusCode upstreamStatusCode,
    Uri? requestUri,
    IEnumerable<NotificationItem>? notifications,
    string? upstreamBody
  ) : this(upstreamStatusCode, requestUri, notifications)
  {
    UpstreamBody = string.IsNullOrWhiteSpace(upstreamBody) ? null : upstreamBody;
  }

  #endregion

  #region Properties

  /// <summary>
  /// O status devolvido pelo serviço chamado.
  /// </summary>
  public HttpStatusCode UpstreamStatusCode { get; }

  /// <summary>
  /// A URI da requisição que falhou, quando conhecida.
  /// </summary>
  /// <remarks>
  /// Pode carregar dados sensíveis na consulta (um token, por exemplo): não a devolva na resposta da aplicação.
  /// </remarks>
  public Uri? RequestUri { get; }

  /// <summary>
  /// O corpo da resposta de erro do serviço chamado, como texto. Nulo quando a resposta não tem corpo, quando o
  /// corpo passa de 64 KB ou quando a leitura dele falha.
  /// </summary>
  /// <remarks>
  /// É o que o serviço chamado escreveu, em qualquer formato: o JSON de erro de um provedor, um texto, uma página
  /// HTML. Serve para a aplicação ler o motivo da falha (<c>modelo inexistente</c>, <c>sem crédito</c>) e montar a
  /// própria mensagem. O Tooark não o coloca na mensagem, nas notificações nem no log, e ele não é confiável: pode
  /// trazer dados internos do serviço chamado ou texto controlado por terceiros. Não o devolva como veio na resposta
  /// da aplicação.
  /// </remarks>
  public string? UpstreamBody { get; }

  #endregion

  #region Private Methods

  /// <summary>
  /// Monta a chave padrão com o status como parâmetro.
  /// </summary>
  /// <param name="upstreamStatusCode">O status devolvido pelo serviço chamado.</param>
  /// <returns>A chave com o parâmetro.</returns>
  private static string DefaultMessage(HttpStatusCode upstreamStatusCode) =>
    $"Http.RequestFailed;{(int)upstreamStatusCode}";

  #endregion
}
