namespace Tooark.Sanitizers.Options;

/// <summary>
/// Classe que representa as opções de configuração dos sanitizadores.
/// </summary>
/// <remarks>
/// Todas as opções têm padrão seguro: a seção de configuração é opcional, e sem ela os três sanitizadores
/// funcionam com as regras descritas em cada grupo.
/// </remarks>
public class SanitizerOptions
{
  #region Section

  /// <summary>
  /// Seção de configuração dos sanitizadores.
  /// </summary>
  public const string Section = "Sanitizers";

  #endregion

  #region Properties

  /// <summary>
  /// Opções do sanitizador de HTML.
  /// </summary>
  public HtmlOptions Html { get; set; } = new();

  /// <summary>
  /// Opções do sanitizador de URL.
  /// </summary>
  public UrlOptions Url { get; set; } = new();

  /// <summary>
  /// Opções do sanitizador do conteúdo do <c>@tooark/wysiwyg</c>.
  /// </summary>
  public WysiwygOptions Wysiwyg { get; set; } = new();

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções de todos os sanitizadores.
  /// </summary>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando alguma opção libera algo inseguro.</exception>
  public void Validate()
  {
    Html.Validate();
    Url.Validate();
  }

  #endregion
}
