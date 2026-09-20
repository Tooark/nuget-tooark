using Tooark.Validations.Messages;
using Tooark.Validations.Patterns;

namespace Tooark.Validations;

/// <summary>
/// Classe de validação de Protocol.
/// </summary>
public partial class Validation
{
  #region IsUrl
  /// <summary>
  /// Verifica se o valor corresponde ao formato de URL. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsUrl(string value, string property) =>
    IsUrl(value, property, ValidationErrorMessages.IsValid(property, "Url"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de URL.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsUrl(string value, string property, string message) =>
    Match(value, RegexPattern.Url, property, message);
  #endregion

  #region IsFtp
  /// <summary>
  /// Verifica se o valor corresponde ao formato de FTP. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsFtp(string value, string property) =>
    IsFtp(value, property, ValidationErrorMessages.IsValid(property, "Ftp"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de FTP.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsFtp(string value, string property, string message) =>
    Match(value, RegexPattern.Ftp, property, message);
  #endregion

  #region IsSftp
  /// <summary>
  /// Verifica se o valor corresponde ao formato de SFTP. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsSftp(string value, string property) =>
    IsSftp(value, property, ValidationErrorMessages.IsValid(property, "Sftp"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de SFTP.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsSftp(string value, string property, string message) =>
    Match(value, RegexPattern.Sftp, property, message);
  #endregion

  #region IsProtocolFtp
  /// <summary>
  /// Verifica se o valor corresponde ao formato de Protocolo FTP. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolFtp(string value, string property) =>
    IsProtocolFtp(value, property, ValidationErrorMessages.IsValid(property, "ProtocolFtp"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de Protocolo FTP.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolFtp(string value, string property, string message) =>
    Match(value, RegexPattern.ProtocolFtp, property, message);
  #endregion

  #region IsHttp
  /// <summary>
  /// Verifica se o valor corresponde ao formato de HTTP. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsHttp(string value, string property) =>
    IsHttp(value, property, ValidationErrorMessages.IsValid(property, "Http"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de HTTP.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsHttp(string value, string property, string message) =>
    Match(value, RegexPattern.Http, property, message);
  #endregion

  #region IsHttps
  /// <summary>
  /// Verifica se o valor corresponde ao formato de HTTPS. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsHttps(string value, string property) =>
    IsHttps(value, property, ValidationErrorMessages.IsValid(property, "Https"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de HTTPS.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsHttps(string value, string property, string message) =>
    Match(value, RegexPattern.Https, property, message);
  #endregion

  #region IsProtocolHttp
  /// <summary>
  /// Verifica se o valor corresponde ao formato de Protocolo HTTP. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolHttp(string value, string property) =>
    IsProtocolHttp(value, property, ValidationErrorMessages.IsValid(property, "ProtocolHttp"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de Protocolo HTTP.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolHttp(string value, string property, string message) =>
    Match(value, RegexPattern.ProtocolHttp, property, message);
  #endregion

  #region IsImap
  /// <summary>
  /// Verifica se o valor corresponde ao formato de IMAP. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsImap(string value, string property) =>
    IsImap(value, property, ValidationErrorMessages.IsValid(property, "Imap"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de IMAP.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsImap(string value, string property, string message) =>
    Match(value, RegexPattern.Imap, property, message);
  #endregion

  #region IsPop3
  /// <summary>
  /// Verifica se o valor corresponde ao formato POP3. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsPop3(string value, string property) =>
    IsPop3(value, property, ValidationErrorMessages.IsValid(property, "Pop3"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato POP3.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsPop3(string value, string property, string message) =>
    Match(value, RegexPattern.Pop3, property, message);
  #endregion

  #region IsProtocolEmailReceiver
  /// <summary>
  /// Verifica se o valor corresponde ao formato de Protocolo de Recebimento de Email. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolEmailReceiver(string value, string property) =>
    IsProtocolEmailReceiver(value, property, ValidationErrorMessages.IsValid(property, "ProtocolEmailReceiver"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de ProtocolEmailReceiver.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolEmailReceiver(string value, string property, string message) =>
    Match(value, RegexPattern.ProtocolEmailReceiver, property, message);
  #endregion

  #region IsSmtp
  /// <summary>
  /// Verifica se o valor corresponde ao formato de SMTP. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsSmtp(string value, string property) =>
    IsSmtp(value, property, ValidationErrorMessages.IsValid(property, "Smtp"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de SMTP.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsSmtp(string value, string property, string message) =>
    Match(value, RegexPattern.Smtp, property, message);
  #endregion

  #region IsProtocolEmailSender
  /// <summary>
  /// Verifica se o valor corresponde ao formato de Protocolo de Envio de Email. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolEmailSender(string value, string property) =>
    IsProtocolEmailSender(value, property, ValidationErrorMessages.IsValid(property, "ProtocolEmailSender"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de Protocolo de Envio de Email.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolEmailSender(string value, string property, string message) =>
    Match(value, RegexPattern.ProtocolEmailSender, property, message);
  #endregion

  #region IsWs
  /// <summary>
  /// Verifica se o valor corresponde ao formato de WS. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsWs(string value, string property) =>
    IsWs(value, property, ValidationErrorMessages.IsValid(property, "Ws"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de WS.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsWs(string value, string property, string message) =>
    Match(value, RegexPattern.Ws, property, message);
  #endregion

  #region IsWss
  /// <summary>
  /// Verifica se o valor corresponde ao formato de WSS. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsWss(string value, string property) =>
    IsWss(value, property, ValidationErrorMessages.IsValid(property, "Wss"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de WSS.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsWss(string value, string property, string message) =>
    Match(value, RegexPattern.Wss, property, message);
  #endregion

  #region IsProtocolWebSocket
  /// <summary>
  /// Verifica se o valor corresponde ao formato de Protocolo Web Socket. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolWebSocket(string value, string property) =>
    IsProtocolWebSocket(value, property, ValidationErrorMessages.IsValid(property, "ProtocolWebSocket"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de Protocolo Web Socket.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsProtocolWebSocket(string value, string property, string message) =>
    Match(value, RegexPattern.ProtocolWebSocket, property, message);
  #endregion
}
