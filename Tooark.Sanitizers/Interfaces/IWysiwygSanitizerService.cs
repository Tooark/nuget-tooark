using System.Text.Json.Nodes;

namespace Tooark.Sanitizers.Interfaces;

/// <summary>
/// Interface do serviço que sanitiza o conteúdo do <c>@tooark/wysiwyg</c>: o JSON do Tiptap que o editor emite.
/// </summary>
public interface IWysiwygSanitizerService
{
  /// <summary>
  /// Sanitiza um documento do Tiptap.
  /// </summary>
  /// <param name="content">O documento, com <c>type</c> igual a <c>doc</c> na raiz.</param>
  /// <returns>
  /// Um documento novo, sanitizado, ou nulo quando a entrada é nula ou a raiz não é um documento.
  /// </returns>
  JsonObject? Sanitize(JsonNode? content);

  /// <summary>
  /// Sanitiza um documento do Tiptap recebido como texto JSON.
  /// </summary>
  /// <param name="json">O documento em JSON.</param>
  /// <returns>
  /// O documento sanitizado em JSON, ou nulo quando a entrada é nula, não é JSON válido ou a raiz não é um
  /// documento.
  /// </returns>
  string? Sanitize(string? json);
}
