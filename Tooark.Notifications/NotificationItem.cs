using Tooark.Notifications.Messages;

namespace Tooark.Notifications;

/// <summary>
/// Representa a estrutura de um item de notificação.
/// </summary>
/// <param name="message">Mensagem da notificação. Nula, vazia ou em branco assume 'Notifications.MessageNullEmpty'.</param>
/// <param name="key">Chave da notificação. Espaços em branco são removidos. Nula, vazia ou em branco assume 'Unknown'.</param>
/// <param name="code">Código de erro da notificação. Nulo, vazio ou em branco assume 'T.ERR'. T[ooark].ERR[or]</param>
public class NotificationItem(string message, string key = NotificationItem.DefaultKey, string code = NotificationItem.DefaultCode)
{
  #region Constants

  /// <summary>
  /// Chave utilizada quando nenhuma chave válida é informada.
  /// </summary>
  internal const string DefaultKey = "Unknown";

  /// <summary>
  /// Código de erro utilizado quando nenhum código válido é informado.
  /// </summary>
  internal const string DefaultCode = "T.ERR";

  #endregion

  #region Private Fields

  /// <summary>
  /// O código de erro privado da notificação.
  /// </summary>
  private readonly string _code = string.IsNullOrWhiteSpace(code) ? DefaultCode : code.Trim();

  /// <summary>
  /// A chave privada da notificação.
  /// </summary>
  private readonly string _key = SanitizeKey(key);

  /// <summary>
  /// A mensagem privada da notificação.
  /// </summary>
  private readonly string _message = string.IsNullOrWhiteSpace(message) ?
    NotificationErrorMessages.MessageIsNullOrEmpty :
    message.Trim();

  #endregion

  #region Properties

  /// <summary>
  /// O código de erro da notificação.
  /// </summary>
  public string Code { get => _code; }

  /// <summary>
  /// A chave da notificação.
  /// </summary>
  public string Key { get => _key; }

  /// <summary>
  /// A mensagem da notificação.
  /// </summary>
  public string Message { get => _message; }

  #endregion

  #region Private Methods

  /// <summary>
  /// Remove todos os espaços em branco da chave, incluindo tabulações e quebras de linha.
  /// </summary>
  /// <param name="key">Chave a ser tratada.</param>
  /// <returns>Chave sem espaços em branco ou 'Unknown' quando não há conteúdo.</returns>
  private static string SanitizeKey(string key) =>
    string.IsNullOrWhiteSpace(key) ?
      DefaultKey :
      string.Concat(key.Where(character => !char.IsWhiteSpace(character)));

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar a mensagem da notificação.
  /// </summary>
  /// <returns>Uma string que representa a notificação.</returns>
  public override string ToString() => _message;

  /// <summary>
  /// Define uma conversão implícita de uma notificação para uma string.
  /// </summary>
  /// <param name="notification">A notificação a ser convertida.</param>
  /// <returns>A mensagem da notificação ou uma string vazia quando a notificação é nula.</returns>
  public static implicit operator string(NotificationItem notification) => notification?._message ?? string.Empty;

  /// <summary>
  /// Define uma conversão implícita de uma string para uma notificação.
  /// </summary>
  /// <param name="message">A mensagem da notificação.</param>
  /// <returns>Uma nova instância de <see cref="NotificationItem"/> com a chave 'Unknown'.</returns>
  public static implicit operator NotificationItem(string message) => new(message);

  #endregion
}
