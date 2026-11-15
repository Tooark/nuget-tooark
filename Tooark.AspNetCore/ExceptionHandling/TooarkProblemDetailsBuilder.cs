using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Tooark.Notifications;

namespace Tooark.AspNetCore.ExceptionHandling;

/// <summary>
/// Monta o <see cref="ProblemDetails"/> do Tooark a partir dos itens de notificação.
/// </summary>
/// <remarks>
/// Compartilhado pelo <see cref="TooarkExceptionHandler"/> e pelo <c>TooarkResults</c>, para que uma exceção
/// capturada e um problema devolvido pela minimal API tenham exatamente o mesmo corpo.
/// </remarks>
internal static class TooarkProblemDetailsBuilder
{
  #region Constants

  /// <summary>
  /// Nome da extensão do Problem Details que carrega a lista de erros.
  /// </summary>
  internal const string ErrorsExtension = "errors";

  #endregion

  #region Internal Methods

  /// <summary>
  /// Monta o Problem Details com o status, o título padrão do status e a lista de erros traduzida.
  /// </summary>
  /// <param name="statusCode">Código de status HTTP da resposta.</param>
  /// <param name="notifications">Itens de notificação que viram a lista <c>errors</c>.</param>
  /// <param name="localizer">Localizador que traduz as mensagens.</param>
  /// <param name="instance">Caminho da requisição, quando conhecido no momento da montagem.</param>
  /// <returns>O Problem Details montado; o <c>type</c> fica para os padrões do ASP.NET Core.</returns>
  internal static ProblemDetails Build(
    int statusCode,
    IEnumerable<NotificationItem>? notifications,
    IStringLocalizer localizer,
    string? instance
  )
  {
    // Traduz cada item, descartando os nulos para não quebrar a lista
    var errors = (notifications ?? [])
      .Where(notification => notification is not null)
      .Select(notification => new ProblemDetailsError(
        notification.Code,
        notification.Key,
        localizer[notification.Message].Value))
      .ToList();

    // O título é a frase padrão do status; um status sem frase conhecida fica sem título
    var title = ReasonPhrases.GetReasonPhrase(statusCode);

    var problemDetails = new ProblemDetails
    {
      Status = statusCode,
      Title = string.IsNullOrEmpty(title) ? null : title,
      // O detalhe resume o problema com a primeira mensagem; a lista completa fica em errors
      Detail = errors.Count > 0 ? errors[0].Message : null,
      Instance = string.IsNullOrEmpty(instance) ? null : instance
    };

    problemDetails.Extensions[ErrorsExtension] = errors;

    return problemDetails;
  }

  #endregion
}
