using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Tooark.AspNetCore.ExceptionHandling;
using Tooark.Exceptions;
using Tooark.Extensions;
using Tooark.Notifications;

namespace Tooark.AspNetCore.Results;

/// <summary>
/// Resultados de minimal API com o Problem Details do Tooark, para responder uma notificação ou uma exceção
/// sem lançá-la.
/// </summary>
/// <remarks>
/// O corpo é o mesmo que o <see cref="TooarkExceptionHandler"/> escreve: <c>status</c>, <c>title</c>,
/// <c>detail</c> com a primeira mensagem, <c>instance</c> com o caminho da requisição e a lista <c>errors</c>
/// com código, chave e mensagem traduzida de cada item. As mensagens são traduzidas pelo localizador JSON do
/// <c>Tooark.Extensions</c>, no idioma do fluxo de execução, sem depender de registro no container.
/// </remarks>
public static class TooarkResults
{
  #region Private Static Fields

  /// <summary>
  /// Localizador das mensagens, independente do container.
  /// </summary>
  private static readonly IStringLocalizer Localizer = new JsonStringLocalizerExtension();

  #endregion

  #region Methods

  /// <summary>
  /// Responde uma exceção do Tooark com Problem Details, no status que ela define.
  /// </summary>
  /// <param name="exception">Exceção com o status e os itens de notificação.</param>
  /// <returns>O resultado com o Problem Details.</returns>
  /// <exception cref="ArgumentNullException">Se a exceção for nula.</exception>
  public static IResult Problem(TooarkException exception)
  {
    ArgumentNullException.ThrowIfNull(exception);

    return Create((int)exception.GetStatusCode(), exception.GetNotifications());
  }

  /// <summary>
  /// Responde as notificações de uma entidade ou de um objeto de valor com Problem Details.
  /// </summary>
  /// <remarks>
  /// Uma notificação válida gera um problema sem erros: confira <see cref="Notification.IsValid"/> antes e
  /// devolva o sucesso quando não houver o que reportar.
  /// </remarks>
  /// <param name="notification">Notificação com os itens que viram a lista <c>errors</c>.</param>
  /// <param name="statusCode">Status da resposta, entre 400 e 599. Padrão: 400.</param>
  /// <returns>O resultado com o Problem Details.</returns>
  /// <exception cref="ArgumentNullException">Se a notificação for nula.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Se o status não for um status de erro.</exception>
  public static IResult ValidationProblem(Notification notification, int statusCode = StatusCodes.Status400BadRequest)
  {
    ArgumentNullException.ThrowIfNull(notification);

    return ValidationProblem(notification.Notifications, statusCode);
  }

  /// <summary>
  /// Responde uma lista de itens de notificação com Problem Details.
  /// </summary>
  /// <param name="notifications">Itens que viram a lista <c>errors</c>.</param>
  /// <param name="statusCode">Status da resposta, entre 400 e 599. Padrão: 400.</param>
  /// <returns>O resultado com o Problem Details.</returns>
  /// <exception cref="ArgumentNullException">Se a lista for nula.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Se o status não for um status de erro.</exception>
  public static IResult ValidationProblem(IEnumerable<NotificationItem> notifications, int statusCode = StatusCodes.Status400BadRequest)
  {
    ArgumentNullException.ThrowIfNull(notifications);
    ArgumentOutOfRangeException.ThrowIfLessThan(statusCode, StatusCodes.Status400BadRequest);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(statusCode, 599);

    return Create(statusCode, notifications);
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Monta o resultado; o <c>instance</c> é preenchido na execução, quando o caminho é conhecido.
  /// </summary>
  /// <param name="statusCode">Status da resposta.</param>
  /// <param name="notifications">Itens que viram a lista <c>errors</c>.</param>
  /// <returns>O resultado com o Problem Details.</returns>
  private static IResult Create(int statusCode, IEnumerable<NotificationItem> notifications) =>
    new TooarkProblemResult(TooarkProblemDetailsBuilder.Build(statusCode, notifications, Localizer, instance: null));

  #endregion
}
