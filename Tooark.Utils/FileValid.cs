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
  [Obsolete("Use the IFormFile extension methods of Tooark.AspNetCore (namespace Tooark.AspNetCore.Files) or FileValid.IsImageFile. Will be removed in v5.")]
  public static bool IsImage(IFormFile? file, long fileSize = 0)
  {
    return InternalFileValid.IsImage(file, fileSize);
  }

  /// <summary>
  /// Verifica, pelo nome e pelo tamanho, se o arquivo é uma imagem válida.
  /// </summary>
  /// <remarks>
  /// As extensões de imagem permitidas são: .JPG, .JPEG, .PNG, .GIF, .BMP, .SVG e .WEBP.
  /// </remarks>
  /// <param name="fileName">Nome do arquivo, de onde sai a extensão.</param>
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica o padrão de 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for uma imagem válida.</returns>
  public static bool IsImageFile(string? fileName, long length, long maxBytes = 0)
  {
    return InternalFileValid.IsImage(fileName, length, maxBytes);
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
  [Obsolete("Use the IFormFile extension methods of Tooark.AspNetCore (namespace Tooark.AspNetCore.Files) or FileValid.IsDocumentFile. Will be removed in v5.")]
  public static bool IsDocument(IFormFile? file, long fileSize = 0)
  {
    return InternalFileValid.IsDocument(file, fileSize);
  }

  /// <summary>
  /// Verifica, pelo nome e pelo tamanho, se o arquivo é um documento válido.
  /// </summary>
  /// <remarks>
  /// As extensões de documento permitidas são: .TXT, .CSV, .LOG, .PDF, .DOC, .DOCX, .XLS, .XLSX, .PPT e .PPTX.
  /// </remarks>
  /// <param name="fileName">Nome do arquivo, de onde sai a extensão.</param>
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica o padrão de 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um documento válido.</returns>
  public static bool IsDocumentFile(string? fileName, long length, long maxBytes = 0)
  {
    return InternalFileValid.IsDocument(fileName, length, maxBytes);
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
  [Obsolete("Use the IFormFile extension methods of Tooark.AspNetCore (namespace Tooark.AspNetCore.Files) or FileValid.IsVideoFile. Will be removed in v5.")]
  public static bool IsVideo(IFormFile? file, long fileSize = 0)
  {
    return InternalFileValid.IsVideo(file, fileSize);
  }

  /// <summary>
  /// Verifica, pelo nome e pelo tamanho, se o arquivo é um vídeo válido.
  /// </summary>
  /// <remarks>
  /// As extensões de vídeo permitidas são: .AVI, .MP4, .MPG, .MPEG e .WMV.
  /// </remarks>
  /// <param name="fileName">Nome do arquivo, de onde sai a extensão.</param>
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica o padrão de 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um vídeo válido.</returns>
  public static bool IsVideoFile(string? fileName, long length, long maxBytes = 0)
  {
    return InternalFileValid.IsVideo(fileName, length, maxBytes);
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
  [Obsolete("Use the IFormFile extension methods of Tooark.AspNetCore (namespace Tooark.AspNetCore.Files) or FileValid.IsCustomFile. Will be removed in v5.")]
  public static bool IsCustom(IFormFile? file, long fileSize = 0, string[]? permittedExtensions = null)
  {
    return InternalFileValid.IsCustom(file, fileSize, permittedExtensions);
  }

  /// <summary>
  /// Verifica, pelo nome e pelo tamanho, se o arquivo é válido para extensões personalizadas.
  /// </summary>
  /// <remarks>
  /// As extensões são comparadas sem diferenciar maiúsculas de minúsculas e podem ser informadas com ou sem o
  /// ponto inicial: <c>"pdf"</c> e <c>".PDF"</c> têm o mesmo efeito.
  /// </remarks>
  /// <param name="fileName">Nome do arquivo, de onde sai a extensão.</param>
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica o padrão de 5242880 bytes (5MB).</param>
  /// <param name="permittedExtensions">Extensões permitidas. Nulo ou vazio aplica todas as extensões de imagem, documento e vídeo.</param>
  /// <returns>Retorna verdadeiro se o arquivo for válido.</returns>
  public static bool IsCustomFile(string? fileName, long length, long maxBytes = 0, string[]? permittedExtensions = null)
  {
    return InternalFileValid.IsCustom(fileName, length, maxBytes, permittedExtensions);
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
  /// Verifica, pelo nome e pelo tamanho, se o arquivo é uma imagem válida.
  /// </summary>
  /// <param name="fileName">Nome do arquivo.</param>
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes.</param>
  /// <returns>Retorna verdadeiro se o arquivo for uma imagem válida.</returns>
  internal static bool IsImage(string? fileName, long length, long maxBytes = 0) => IsValid(fileName, length, maxBytes, DefaultImageExtensions);

  /// <summary>
  /// Verifica se o arquivo é um documento válido.
  /// </summary>
  /// <param name="file">Arquivo a ser verificado. Formato de <see cref="IFormFile"/></param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um documento válido.</returns>
  internal static bool IsDocument(IFormFile? file, long fileSize = 0) => IsValid(file, fileSize, DefaultDocumentExtensions);

  /// <summary>
  /// Verifica, pelo nome e pelo tamanho, se o arquivo é um documento válido.
  /// </summary>
  /// <param name="fileName">Nome do arquivo.</param>
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes.</param>
  /// <returns>Retorna verdadeiro se o arquivo for um documento válido.</returns>
  internal static bool IsDocument(string? fileName, long length, long maxBytes = 0) => IsValid(fileName, length, maxBytes, DefaultDocumentExtensions);

  /// <summary>
  /// Verifica se o arquivo é um video válido.
  /// </summary>
  /// <param name="file">Arquivo a ser verificado. Formato de <see cref="IFormFile"/></param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um video válido.</returns>
  internal static bool IsVideo(IFormFile? file, long fileSize = 0) => IsValid(file, fileSize, DefaultVideoExtensions);

  /// <summary>
  /// Verifica, pelo nome e pelo tamanho, se o arquivo é um vídeo válido.
  /// </summary>
  /// <param name="fileName">Nome do arquivo.</param>
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes.</param>
  /// <returns>Retorna verdadeiro se o arquivo for um vídeo válido.</returns>
  internal static bool IsVideo(string? fileName, long length, long maxBytes = 0) => IsValid(fileName, length, maxBytes, DefaultVideoExtensions);

  /// <summary>
  /// Verifica se o arquivo é valido para extensões personalizadas.
  /// </summary>
  /// <param name="file">Arquivo a ser verificado. Formato de <see cref="IFormFile"/></param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Parâmetro opcional. Valor padrão é 5242880 bytes (5MB).</param>
  /// <param name="permittedExtensions">Extensões permitidas. Parâmetro opcional. Valor padrão é todas as extensões de imagem, documento e vídeo.</param>
  /// <returns>Retorna verdadeiro se o arquivo for válido.</returns>
  internal static bool IsCustom(IFormFile? file, long fileSize = 0, string[]? permittedExtensions = null)
  {
    // Verifica se o arquivo é válido, com as extensões informadas ou as padrão
    return IsValid(file, fileSize, ResolveExtensions(permittedExtensions));
  }

  /// <summary>
  /// Verifica, pelo nome e pelo tamanho, se o arquivo é valido para extensões personalizadas.
  /// </summary>
  /// <param name="fileName">Nome do arquivo.</param>
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes.</param>
  /// <param name="permittedExtensions">Extensões permitidas. Nulo ou vazio aplica todas as extensões de imagem, documento e vídeo.</param>
  /// <returns>Retorna verdadeiro se o arquivo for válido.</returns>
  internal static bool IsCustom(string? fileName, long length, long maxBytes = 0, string[]? permittedExtensions = null)
  {
    // Verifica se o arquivo é válido, com as extensões informadas ou as padrão
    return IsValid(fileName, length, maxBytes, ResolveExtensions(permittedExtensions));
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Resolve as extensões aplicáveis: as informadas, já normalizadas, ou todas as padrão.
  /// </summary>
  /// <param name="permittedExtensions">Extensões informadas por quem chamou.</param>
  /// <returns>As extensões a conferir.</returns>
  private static string[] ResolveExtensions(string[]? permittedExtensions)
  {
    // Normaliza as extensões informadas, aceitando-as com ou sem o ponto inicial
    var extensions = NormalizeExtensions(permittedExtensions);

    // Se nenhuma extensão válida foi informada, usa todas as extensões de imagem, documento e vídeo
    return extensions.Length == 0
      ? [.. DefaultImageExtensions, .. DefaultDocumentExtensions, .. DefaultVideoExtensions]
      : extensions;
  }

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
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes. Valor padrão é 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o tamanho do arquivo for válido.</returns>
  private static bool FileSize(long length, long fileSize = 0)
  {
    // Se o tamanho do arquivo não for especificado, o tamanho padrão será 5MB
    fileSize = fileSize > 0 ? fileSize : DefaultFileSize;

    // Verifica o tamanho do arquivo
    return length > 0 && length <= fileSize;
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
    // Sem arquivo não há o que validar; com arquivo, valem o nome e o tamanho declarados nele
    return file != null && IsValid(file.FileName, file.Length, fileSize, permittedExtensions);
  }

  /// <summary>
  /// Verifica, pelo nome e pelo tamanho, se o arquivo é válido.
  /// </summary>
  /// <param name="fileName">Nome do arquivo.</param>
  /// <param name="length">Tamanho do arquivo em bytes.</param>
  /// <param name="fileSize">Tamanho máximo do arquivo em bytes.</param>
  /// <param name="permittedExtensions">Extensões permitidas, já normalizadas.</param>
  /// <returns>Retorna verdadeiro se o arquivo for válido.</returns>
  private static bool IsValid(string? fileName, long length, long fileSize, string[] permittedExtensions)
  {
    // Verifica se o tamanho do arquivo é válido
    if (FileSize(length, fileSize))
    {
      // Obtém a extensão do arquivo
      var ext = Path.GetExtension(fileName ?? string.Empty).ToUpperInvariant();

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
