using Tooark.Validations.Messages;
using Tooark.Validations.Patterns;

namespace Tooark.Validations;

/// <summary>
/// Classe de validação de Protocol.
/// </summary>
public partial class Validation
{
  #region IsLinkVideo
  /// <summary>
  /// Verifica se o valor corresponde ao formato de um link de vídeo. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsLinkVideo(string value, string property) =>
    IsLinkVideo(value, property, ValidationErrorMessages.IsValid(property, "LinkVideo"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de um link de vídeo.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsLinkVideo(string value, string property, string message) =>
    Match(value, RegexPattern.LinkVideo, property, message);
  #endregion

  #region IsLinkVideoYouTube
  /// <summary>
  /// Verifica se o valor corresponde ao formato link de vídeo do YouTube. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsLinkVideoYouTube(string value, string property) =>
    IsLinkVideoYouTube(value, property, ValidationErrorMessages.IsValid(property, "LinkVideoYouTube"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato link de vídeo do YouTube.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsLinkVideoYouTube(string value, string property, string message) =>
    Match(value, RegexPattern.YouTube, property, message);
  #endregion

  #region IsLinkVideoVimeo
  /// <summary>
  /// Verifica se o valor corresponde ao formato link de vídeo do Vimeo. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsLinkVideoVimeo(string value, string property) =>
    IsLinkVideoVimeo(value, property, ValidationErrorMessages.IsValid(property, "LinkVideoVimeo"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato link de vídeo do Vimeo.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsLinkVideoVimeo(string value, string property, string message) =>
    Match(value, RegexPattern.Vimeo, property, message);
  #endregion

  #region IsLinkVideoDailymotion
  /// <summary>
  /// Verifica se o valor corresponde ao formato link de vídeo do Dailymotion. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsLinkVideoDailymotion(string value, string property) =>
    IsLinkVideoDailymotion(value, property, ValidationErrorMessages.IsValid(property, "LinkVideoDailymotion"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato link de vídeo do Dailymotion.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsLinkVideoDailymotion(string value, string property, string message) =>
    Match(value, RegexPattern.Dailymotion, property, message);
  #endregion
}
