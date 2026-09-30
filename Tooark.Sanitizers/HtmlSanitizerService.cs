using Ganss.Xss;
using Microsoft.Extensions.Options;
using Tooark.Sanitizers.Interfaces;
using Tooark.Sanitizers.Options;

namespace Tooark.Sanitizers;

/// <summary>
/// Serviço que sanitiza HTML com lista de permissão, sobre o <c>HtmlSanitizer</c>.
/// </summary>
/// <remarks>
/// A sanitização de HTML não é escrita à mão: encodings, tags aninhadas e atributos de evento dão uma superfície de
/// bypass grande demais. O serviço configura uma instância do <c>HtmlSanitizer</c> uma vez, no construtor, e a
/// reaproveita: a biblioteca é segura para uso concorrente enquanto a configuração não muda.
/// </remarks>
public sealed class HtmlSanitizerService : IHtmlSanitizerService
{
  #region Private Fields

  /// <summary>
  /// Instância configurada do <c>HtmlSanitizer</c>.
  /// </summary>
  private readonly HtmlSanitizer _sanitizer;

  #endregion

  #region Constructor

  /// <summary>
  /// Cria o serviço a partir das opções dos sanitizadores.
  /// </summary>
  /// <param name="options">As opções dos sanitizadores.</param>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções de HTML liberam algo inseguro.</exception>
  public HtmlSanitizerService(IOptions<SanitizerOptions> options)
  {
    var html = options.Value.Html;
    html.Validate();

    _sanitizer = new HtmlSanitizer
    {
      KeepChildNodes = html.KeepChildNodes
    };

    // Cada lista informada substitui a padrão da biblioteca; ausente ou vazia, a padrão fica
    if (html.AllowedTags is { Length: > 0 })
    {
      Replace(_sanitizer.AllowedTags, html.AllowedTags);
    }

    if (html.AllowedAttributes is { Length: > 0 })
    {
      Replace(_sanitizer.AllowedAttributes, html.AllowedAttributes);
    }

    if (html.AllowedSchemes is { Length: > 0 })
    {
      Replace(_sanitizer.AllowedSchemes, SchemeRules.Normalize(html.AllowedSchemes));
    }

    if (html.AllowedCssProperties is { Length: > 0 })
    {
      Replace(_sanitizer.AllowedCssProperties, html.AllowedCssProperties);
    }

    // O atributo class não está na lista padrão da biblioteca: informar as classes é o que o libera
    if (html.AllowedClasses is { Length: > 0 })
    {
      Replace(_sanitizer.AllowedClasses, html.AllowedClasses);
      _sanitizer.AllowedAttributes.Add("class");
    }
  }

  #endregion

  #region Methods

  /// <inheritdoc/>
  public string Sanitize(string? html) => string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);

  #endregion

  #region Private Methods

  /// <summary>
  /// Substitui o conteúdo de uma lista da biblioteca pelos valores informados, sem espaços nas pontas e sem itens
  /// vazios.
  /// </summary>
  /// <param name="target">A lista da biblioteca.</param>
  /// <param name="values">Os valores informados.</param>
  private static void Replace(ISet<string> target, IEnumerable<string> values)
  {
    target.Clear();
    target.UnionWith(values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
  }

  #endregion
}
