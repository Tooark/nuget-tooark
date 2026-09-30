using Tooark.Exceptions;

namespace Tooark.Sanitizers.Options;

/// <summary>
/// Classe que representa as opções do sanitizador de HTML.
/// </summary>
/// <remarks>
/// Cada lista substitui a lista padrão do <c>HtmlSanitizer</c> quando informada. Ausente ou vazia, vale o padrão da
/// biblioteca, que já é uma lista de permissão segura para HTML de formatação.
/// </remarks>
public class HtmlOptions
{
  #region Constants

  /// <summary>
  /// Tags que executam código, carregam conteúdo externo ou mudam o documento, e que nenhuma configuração libera.
  /// </summary>
  private static readonly IReadOnlySet<string> BlockedTags = new HashSet<string>(
    ["script", "object", "embed", "applet", "base", "meta", "link", "frame", "frameset"],
    StringComparer.OrdinalIgnoreCase
  );

  #endregion

  #region Properties

  /// <summary>
  /// Tags permitidas, como <c>p</c> e <c>a</c>. Padrão: a lista do <c>HtmlSanitizer</c>.
  /// </summary>
  public string[]? AllowedTags { get; set; }

  /// <summary>
  /// Atributos permitidos, como <c>href</c> e <c>alt</c>. Padrão: a lista do <c>HtmlSanitizer</c>.
  /// </summary>
  public string[]? AllowedAttributes { get; set; }

  /// <summary>
  /// Esquemas de URL permitidos nos atributos de URL, como <c>href</c> e <c>src</c>. Padrão: <c>http</c> e
  /// <c>https</c>.
  /// </summary>
  public string[]? AllowedSchemes { get; set; }

  /// <summary>
  /// Propriedades CSS permitidas no atributo <c>style</c>. Padrão: a lista do <c>HtmlSanitizer</c>.
  /// </summary>
  public string[]? AllowedCssProperties { get; set; }

  /// <summary>
  /// Classes CSS permitidas. Padrão: nenhuma, porque o atributo <c>class</c> não está na lista padrão.
  /// </summary>
  /// <remarks>
  /// Informar as classes libera o atributo <c>class</c> só com elas. Com <c>class</c> em
  /// <see cref="AllowedAttributes"/> e sem esta lista, toda classe passa.
  /// </remarks>
  public string[]? AllowedClasses { get; set; }

  /// <summary>
  /// Indica se o conteúdo de uma tag removida é mantido. Padrão: falso, a tag sai com o conteúdo.
  /// </summary>
  public bool KeepChildNodes { get; set; }

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções do sanitizador de HTML.
  /// </summary>
  /// <exception cref="InternalServerErrorException">
  /// Quando a configuração libera uma tag bloqueada, um atributo de evento ou um esquema inseguro.
  /// </exception>
  public void Validate()
  {
    // Tags como script e object executam código mesmo sem atributo algum
    foreach (var tag in AllowedTags ?? [])
    {
      if (BlockedTags.Contains(tag?.Trim() ?? string.Empty))
      {
        throw new InternalServerErrorException($"Options.Sanitizers.Html.TagNotAllowed;{tag}");
      }
    }

    // Atributos de evento (onclick, onerror...) executam código em qualquer tag
    foreach (var attribute in AllowedAttributes ?? [])
    {
      if (attribute?.Trim().StartsWith("on", StringComparison.OrdinalIgnoreCase) == true)
      {
        throw new InternalServerErrorException($"Options.Sanitizers.Html.AttributeNotAllowed;{attribute}");
      }
    }

    SchemeRules.Validate(AllowedSchemes);
  }

  #endregion
}
