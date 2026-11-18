using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Tooark.Extensions;
using Tooark.Notifications;

namespace Tooark.AspNetCore.Extensions;

/// <summary>
/// Converte entre o formato de validação do ASP.NET Core, com as mensagens por campo, e os itens de notificação
/// do Tooark.
/// </summary>
/// <remarks>
/// A conversão é explícita, nos dois sentidos, e não um operador implícito: um campo com várias mensagens vira
/// vários itens, e o caminho de volta perde o código de cada item. Uma conversão implícita esconderia essa perda.
/// Para responder as falhas de validação do ASP.NET Core na lista do Tooark sem chamar estes métodos, ligue
/// <c>TooarkProblemDetailsOptions.ConvertValidationProblems</c>.
/// </remarks>
public static class ValidationProblemExtension
{
  #region Private Fields

  /// <summary>
  /// Localizador JSON do <c>Tooark.Extensions</c>, que traduz no idioma do fluxo de execução.
  /// </summary>
  private static readonly IStringLocalizer Localizer = new JsonStringLocalizerExtension();

  #endregion

  #region Methods

  /// <summary>
  /// Converte as mensagens por campo do ASP.NET Core em itens de notificação.
  /// </summary>
  /// <remarks>
  /// Cada mensagem vira um item, com o campo como chave e o código padrão <c>T.ERR</c>: o formato do ASP.NET Core
  /// não tem código. Os campos seguem a ordem do dicionário, e as mensagens de um campo, a ordem em que foram
  /// registradas. Uma mensagem em branco vira a chave <c>Field.Invalid;{campo}</c>, ou <c>BadRequest</c> quando
  /// não está ligada a um campo, como no <c>GetErrors</c> do ModelState. A mensagem não é traduzida aqui: o
  /// Problem Details do Tooark a traduz ao responder.
  /// </remarks>
  /// <param name="errors">As mensagens por campo, como em <c>HttpValidationProblemDetails.Errors</c>.</param>
  /// <returns>Um item de notificação por mensagem.</returns>
  /// <exception cref="ArgumentNullException">Se o dicionário for nulo.</exception>
  public static IReadOnlyList<NotificationItem> ToNotificationItems(this IDictionary<string, string[]> errors)
  {
    ArgumentNullException.ThrowIfNull(errors);

    List<NotificationItem> notifications = [];

    foreach (var (key, messages) in errors)
    {
      // Um campo sem mensagens não gera item
      foreach (var message in messages ?? [])
      {
        notifications.Add(new NotificationItem(
          string.IsNullOrWhiteSpace(message) ? ModelStateExtension.MessageWithoutText(key) : message,
          key));
      }
    }

    return notifications;
  }

  /// <summary>
  /// Converte as mensagens por campo de um problema de validação do ASP.NET Core em itens de notificação.
  /// </summary>
  /// <param name="problemDetails">O problema de validação.</param>
  /// <returns>Um item de notificação por mensagem.</returns>
  /// <exception cref="ArgumentNullException">Se o problema for nulo.</exception>
  /// <seealso cref="ToNotificationItems(IDictionary{string, string[]})"/>
  public static IReadOnlyList<NotificationItem> ToNotificationItems(this HttpValidationProblemDetails problemDetails)
  {
    ArgumentNullException.ThrowIfNull(problemDetails);

    return problemDetails.Errors.ToNotificationItems();
  }

  /// <summary>
  /// Converte itens de notificação nas mensagens por campo do ASP.NET Core.
  /// </summary>
  /// <remarks>
  /// Os itens são agrupados pela chave, na ordem em que cada chave aparece, com as mensagens traduzidas pelo
  /// localizador JSON do <c>Tooark.Extensions</c>, no idioma do fluxo de execução. O código de cada item se perde:
  /// o formato do ASP.NET Core não tem onde guardá-lo. Serve para quem precisa responder no formato do ASP.NET
  /// Core, como <c>Results.ValidationProblem(notification.ToValidationErrors())</c> com a conversão desligada.
  /// </remarks>
  /// <param name="notifications">Os itens de notificação.</param>
  /// <returns>As mensagens traduzidas, por chave.</returns>
  /// <exception cref="ArgumentNullException">Se a lista for nula.</exception>
  public static IDictionary<string, string[]> ToValidationErrors(this IEnumerable<NotificationItem> notifications)
  {
    ArgumentNullException.ThrowIfNull(notifications);

    // O agrupamento preserva a ordem da primeira ocorrência de cada chave
    return notifications
      .Where(notification => notification is not null)
      .GroupBy(notification => notification.Key, StringComparer.Ordinal)
      .ToDictionary(
        group => group.Key,
        group => group.Select(notification => Localizer[notification.Message].Value).ToArray(),
        StringComparer.Ordinal);
  }

  #endregion
}
