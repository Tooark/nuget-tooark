namespace Tooark.Sanitizers.Interfaces;

/// <summary>
/// Interface do serviço que sanitiza HTML com lista de permissão.
/// </summary>
public interface IHtmlSanitizerService
{
  /// <summary>
  /// Sanitiza um fragmento de HTML, mantendo só as tags, os atributos, os esquemas e o CSS permitidos.
  /// </summary>
  /// <param name="html">O HTML a ser sanitizado.</param>
  /// <returns>O HTML sanitizado, ou texto vazio quando a entrada é nula ou em branco.</returns>
  string Sanitize(string? html);
}
