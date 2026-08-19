using System.ComponentModel.DataAnnotations;
using Tooark.Validations.Patterns;

namespace Tooark.Attributes;

/// <summary>
/// Atributo de validação de URL.
/// </summary>
/// <remarks>
/// A URL é aceita nos protocolos de email (envio e recebimento), FTP, HTTP e WebSocket.
/// Valor ausente é reportado como campo obrigatório.
/// </remarks>
/// <param name="propertyName">Nome do campo usado na mensagem de erro. Padrão: "Url".</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class UrlValidationAttribute(string propertyName = "Url") : TooarkValidationAttribute(propertyName)
{
  #region Methods

  /// <summary>
  /// Verifica se o valor é uma URL válida em algum dos protocolos aceitos.
  /// </summary>
  /// <param name="value">O valor a ser verificado.</param>
  /// <returns>Verdadeiro quando o valor é uma URL válida.</returns>
  protected override bool IsSatisfied(string value) =>
    Matches(value, RegexPattern.ProtocolEmailReceiver) ||
    Matches(value, RegexPattern.ProtocolEmailSender) ||
    Matches(value, RegexPattern.ProtocolFtp) ||
    Matches(value, RegexPattern.ProtocolHttp) ||
    Matches(value, RegexPattern.ProtocolWebSocket);

  #endregion
}
