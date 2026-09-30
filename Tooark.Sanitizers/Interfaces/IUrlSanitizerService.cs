namespace Tooark.Sanitizers.Interfaces;

/// <summary>
/// Interface do serviço que sanitiza URLs com lista de permissão de esquemas.
/// </summary>
public interface IUrlSanitizerService
{
  /// <summary>
  /// Sanitiza uma URL.
  /// </summary>
  /// <param name="url">A URL a ser sanitizada.</param>
  /// <returns>
  /// A URL sem caracteres de controle e espaços quando ela é aceita, ou texto vazio quando não é.
  /// </returns>
  string Sanitize(string? url);

  /// <summary>
  /// Indica se a URL é aceita.
  /// </summary>
  /// <param name="url">A URL a ser conferida.</param>
  /// <returns>Verdadeiro quando a URL é aceita.</returns>
  bool IsSafe(string? url);
}
