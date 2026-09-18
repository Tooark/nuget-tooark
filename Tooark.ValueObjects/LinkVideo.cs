using System.Text.RegularExpressions;
using Tooark.Exceptions;
using Tooark.Validations;
using Tooark.Validations.Patterns;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um link de vídeo. Plataformas: YouTube, Vimeo e Dailymotion.
/// </summary>
public sealed class LinkVideo : ValueObject
{
  #region Private Fields

  /// <summary>
  /// O link privado do vídeo.
  /// </summary>
  private readonly string _link = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe LinkVideo com os parâmetros especificados.
  /// </summary>
  /// <param name="link">O valor do link a ser validado.</param>
  /// <param name="youtube">Validar link do YouTube. Padrão: true.</param>
  /// <param name="vimeo">Validar link do Vimeo. Padrão: true.</param>
  /// <param name="dailymotion">Validar link do Dailymotion. Padrão: true.</param>
  public LinkVideo(string? link, bool youtube = true, bool vimeo = true, bool dailymotion = true)
  {
    // Método de validação de expressão regular.
    static bool RegexValidation(string link, string pattern) => Regex.IsMatch(link, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(Validation.DefaultTimeout));

    // Link ausente é tratado como vazio, que nenhum provedor aceita
    var value = link ?? string.Empty;

    // Verifica se o link é válido.
    bool linkIsValid = !string.IsNullOrWhiteSpace(value) &&
    (
      (youtube && RegexValidation(value, RegexPattern.YouTube)) ||
      (vimeo && RegexValidation(value, RegexPattern.Vimeo)) ||
      (dailymotion && RegexValidation(value, RegexPattern.Dailymotion))
    );

    // Verifica se o link é válido.
    if (linkIsValid)
    {
      // Define o valor do link
      _link = value;
    }
    else
    {
      // Adiciona as notificação de validação do link
      AddNotification("Field.Invalid;LinkVideo", "LinkVideo", "T.VOJ.LVI1");
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o link do vídeo.
  /// </summary>
  public string Link { get => _link; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do link.
  /// </summary>
  /// <returns>O valor do link.</returns>
  public override string ToString() => _link;

  /// <summary>
  /// Define uma conversão implícita de um objeto LinkVideo para uma string.
  /// </summary>
  /// <param name="linkVideo">O objeto LinkVideo a ser convertido.</param>
  /// <returns>Uma string que representa o valor do link.</returns>
  public static implicit operator string(LinkVideo linkVideo) =>
    linkVideo?._link ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto LinkVideo.
  /// </summary>
  /// <param name="link">A string a ser convertida em um objeto LinkVideo.</param>
  /// <returns>O objeto LinkVideo criado a partir da string fornecida.</returns>
  public static implicit operator LinkVideo(string link) => new(link);

  #endregion
}
