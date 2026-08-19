using Microsoft.AspNetCore.Http;

namespace Tooark.Utils;

/// <summary>
/// Classe estática que fornece métodos para verificar a validade de arquivos.
/// </summary>
/// <remarks>
/// A verificação considera o tamanho e a extensão declarada no nome do arquivo. Ela não inspeciona o conteúdo,
/// portanto não substitui a checagem do tipo real do arquivo quando o conteúdo enviado não é confiável.
/// </remarks>
public static class FileValid
{
  #region Methods

  /// <summary>
  /// Verifica se o arquivo é uma imagem válida.
  /// </summary>
  /// <remarks>
  /// As extensões de imagem permitidas são: .JPG, .JPEG, .PNG, .GIF, .BMP, .SVG e .WEBP.
  /// </remarks>
  /// <param name="file">Arquivo a ser verificado.</param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for uma imagem válida.</returns>
  public static bool IsImage(IFormFile? file, long fileSize = 0)
  {
    return InternalFileValid.IsImage(file, fileSize);
  }

  /// <summary>
  /// Verifica se o arquivo é um documento válido.
  /// </summary>
  /// <remarks>
  /// As extensões de documento permitidas são: .TXT, .CSV, .LOG, .PDF, .DOC, .DOCX, .XLS, .XLSX, .PPT e .PPTX.
  /// </remarks>
  /// <param name="file">Arquivo a ser verificado.</param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um documento válido.</returns>
  public static bool IsDocument(IFormFile? file, long fileSize = 0)
  {
    return InternalFileValid.IsDocument(file, fileSize);
  }

  /// <summary>
  /// Verifica se o arquivo é um video válido.
  /// </summary>
  /// <remarks>
  /// As extensões de video permitidas são: .AVI, .MP4, .MPG, .MPEG e .WMV.
  /// </remarks>
  /// <param name="file">Arquivo a ser verificado.</param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um video válido.</returns>
  public static bool IsVideo(IFormFile? file, long fileSize = 0)
  {
    return InternalFileValid.IsVideo(file, fileSize);
  }

  /// <summary>
  /// Verifica se o arquivo é valido para extensões personalizadas.
  /// </summary>
  /// <remarks>
  /// As extensões são comparadas sem diferenciar maiúsculas de minúsculas e podem ser informadas com ou sem o
  /// ponto inicial: <c>"pdf"</c> e <c>".PDF"</c> têm o mesmo efeito.
  /// </remarks>
  /// <param name="file">Arquivo a ser verificado.</param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <param name="permittedExtensions">Extensões permitidas. Parâmetro opcional. Valor padrão é todas as extensões de imagem, documento e vídeo.</param>
  /// <returns>Retorna verdadeiro se o arquivo for válido.</returns>
  public static bool IsCustom(IFormFile? file, long fileSize = 0, string[]? permittedExtensions = null)
  {
    return InternalFileValid.IsCustom(file, fileSize, permittedExtensions);
  }

  #endregion
}

/// <summary>
/// Classe estática interna que fornece métodos para verificar a validade de arquivos.
/// </summary>
internal static class InternalFileValid
{
  #region Private Static Fields

  /// <summary>
  /// Tamanho máximo padrão do arquivo, em bytes.
  /// </summary>
  private static readonly long DefaultFileSize = 5242880; // 5MB

  /// <summary>
  /// Extensões de imagem permitidas por padrão.
  /// </summary>
  private static readonly string[] DefaultImageExtensions = [".JPG", ".JPEG", ".PNG", ".GIF", ".BMP", ".SVG", ".WEBP"];

  /// <summary>
  /// Extensões de documento permitidas por padrão.
  /// </summary>
  private static readonly string[] DefaultDocumentExtensions = [".TXT", ".CSV", ".LOG", ".PDF", ".DOC", ".DOCX", ".XLS", ".XLSX", ".PPT", ".PPTX"];

  /// <summary>
  /// Extensões de vídeo permitidas por padrão.
  /// </summary>
  private static readonly string[] DefaultVideoExtensions = [".AVI", ".MP4", ".MPG", ".MPEG", ".WMV"];

  #endregion

  #region Internal Methods

  /// <summary>
  /// Verifica se o arquivo é uma imagem válida.
  /// </summary>
  /// <param name="file">Arquivo a ser verificado. Formato de <see cref="IFormFile"/></param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for uma imagem válida.</returns>
  internal static bool IsImage(IFormFile? file, long fileSize = 0) => IsValid(file, fileSize, DefaultImageExtensions);

  /// <summary>
  /// Verifica se o arquivo é um documento válido.
  /// </summary>
  /// <param name="file">Arquivo a ser verificado. Formato de <see cref="IFormFile"/></param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um documento válido.</returns>
  internal static bool IsDocument(IFormFile? file, long fileSize = 0) => IsValid(file, fileSize, DefaultDocumentExtensions);

  /// <summary>
  /// Verifica se o arquivo é um video válido.
  /// </summary>
  /// <param name="file">Arquivo a ser verificado. Formato de <see cref="IFormFile"/></param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um video válido.</returns>
  internal static bool IsVideo(IFormFile? file, long fileSize = 0) => IsValid(file, fileSize, DefaultVideoExtensions);

  /// <summary>
  /// Verifica se o arquivo é valido para extensões personalizadas.
  /// </summary>
  /// <param name="file">Arquivo a ser verificado. Formato de <see cref="IFormFile"/></param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <param name="permittedExtensions">Extensões permitidas. Parâmetro opcional. Valor padrão é todas as extensões de imagem, documento e vídeo.</param>
  /// <returns>Retorna verdadeiro se o arquivo for válido.</returns>
  internal static bool IsCustom(IFormFile? file, long fileSize = 0, string[]? permittedExtensions = null)
  {
    // Normaliza as extensões informadas, aceitando-as com ou sem o ponto inicial
    var extensions = NormalizeExtensions(permittedExtensions);

    // Se nenhuma extensão válida foi informada, as extensões padrão serão usadas
    if (extensions.Length == 0)
    {
      // Usa todas as extensões de imagem, documento e vídeo
      extensions = [.. DefaultImageExtensions, .. DefaultDocumentExtensions, .. DefaultVideoExtensions];
    }

    // Verifica se o arquivo é válido
    return IsValid(file, fileSize, extensions);
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Normaliza as extensões informadas para o formato comparável: em maiúsculas e com o ponto inicial.
  /// </summary>
  /// <param name="extensions">Extensões a serem normalizadas.</param>
  /// <returns>As extensões normalizadas, sem entradas vazias.</returns>
  private static string[] NormalizeExtensions(string[]? extensions)
  {
    // Verifica se as extensões foram informadas
    if (extensions == null || extensions.Length == 0)
    {
      // Retorna um conjunto vazio caso nenhuma extensão tenha sido informada
      return [];
    }

    // Descarta as entradas vazias, converte para maiúsculas e garante o ponto inicial
    return
    [
      .. extensions
        .Where(extension => !string.IsNullOrWhiteSpace(extension))
        .Select(extension => extension.Trim().ToUpperInvariant())
        .Select(extension => extension.StartsWith('.') ? extension : $".{extension}")
        .Where(extension => extension.Length > 1)
        .Distinct()
    ];
  }

  /// <summary>
  /// Verifica se o tamanho do arquivo é válido.
  /// </summary>
  /// <param name="file">Arquivo a ser verificado.</param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o tamanho do arquivo for válido.</returns>
  private static bool FileSize(IFormFile file, long fileSize = 0)
  {
    // Se o tamanho do arquivo não for especificado, o tamanho padrão será 5MB
    fileSize = fileSize > 0 ? fileSize : DefaultFileSize;

    // Verifica o tamanho do arquivo
    return file.Length > 0 && file.Length <= fileSize;
  }

  /// <summary>
  /// Verifica se o arquivo é válido.
  /// </summary>
  /// <param name="file">Arquivo a ser verificado. Formato de <see cref="IFormFile"/></param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes.</param>
  /// <param name="permittedExtensions">Extensões permitidas, já normalizadas.</param>
  /// <returns>Retorna verdadeiro se o arquivo for válido.</returns>
  private static bool IsValid(IFormFile? file, long fileSize, string[] permittedExtensions)
  {
    // Verifica se o arquivo não é nulo e se o tamanho do arquivo é válido
    if (file != null && FileSize(file, fileSize))
    {
      // Obtém a extensão do arquivo
      var ext = Path.GetExtension(file.FileName ?? string.Empty).ToUpperInvariant();

      // Verifica a extensão declarada no nome do arquivo contra a lista de extensões permitidas
      if (!string.IsNullOrEmpty(ext) && permittedExtensions.Contains(ext))
      {
        // Verifica se a extensão do arquivo é válida
        return true;
      }
    }

    // Retorna falso se o arquivo não for válido
    return false;
  }

  #endregion
}
