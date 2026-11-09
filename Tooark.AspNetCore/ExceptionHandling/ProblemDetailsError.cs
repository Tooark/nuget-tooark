using System.Text.Json.Serialization;
using Tooark.Notifications;

namespace Tooark.AspNetCore.ExceptionHandling;

/// <summary>
/// Um erro da lista <c>errors</c> do Problem Details montado pelo Tooark.
/// </summary>
/// <remarks>
/// Cada item nasce de um <see cref="NotificationItem"/>: o código e a chave chegam como foram registrados, e a
/// mensagem já traduzida pelo localizador. Os nomes das propriedades no JSON são fixos e em minúsculas
/// (<c>code</c>, <c>key</c>, <c>message</c>), como os membros do próprio <c>ProblemDetails</c>, então não
/// dependem da política de nomes configurada na aplicação.
/// </remarks>
/// <param name="code">Código do erro, como <c>T.VLD.STR5</c> ou o padrão <c>T.ERR</c>.</param>
/// <param name="key">Chave do erro, em geral o nome do campo, ou o padrão <c>Unknown</c>.</param>
/// <param name="message">Mensagem do erro, já traduzida.</param>
public sealed class ProblemDetailsError(string code, string key, string message)
{
  #region Properties

  /// <summary>
  /// Código do erro, como <c>T.VLD.STR5</c> ou o padrão <c>T.ERR</c>.
  /// </summary>
  [JsonPropertyName("code")]
  public string Code { get; } = code;

  /// <summary>
  /// Chave do erro, em geral o nome do campo, ou o padrão <c>Unknown</c>.
  /// </summary>
  [JsonPropertyName("key")]
  public string Key { get; } = key;

  /// <summary>
  /// Mensagem do erro, já traduzida.
  /// </summary>
  [JsonPropertyName("message")]
  public string Message { get; } = message;

  #endregion
}
