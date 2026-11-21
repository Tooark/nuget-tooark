using Microsoft.AspNetCore.Http;
using Tooark.Exceptions;
using Tooark.Utils;
using Tooark.Utils.Messages;

namespace Tooark.AspNetCore.Files;

/// <summary>
/// Conversão e validação de arquivos enviados (<see cref="IFormFile"/>).
/// </summary>
/// <remarks>
/// São as versões para <see cref="IFormFile"/> do <see cref="FileConvert"/> e do <see cref="FileValid"/> do
/// <c>Tooark.Utils</c>, com as mesmas regras de limite e de extensão. Ficam neste pacote porque o
/// <see cref="IFormFile"/> é um tipo do ASP.NET Core: os métodos equivalentes do <c>Tooark.Utils</c> estão obsoletos
/// e saem na v5, junto com a dependência dele no pacote <c>Microsoft.AspNetCore.Http</c> 2.x.
/// </remarks>
public static class FormFileExtension
{
  #region Methods

  /// <summary>
  /// Copia o conteúdo binário do arquivo para um <see cref="MemoryStream"/>, com limite de tamanho.
  /// </summary>
  /// <remarks>
  /// O limite é conferido no tamanho declarado, antes de qualquer alocação, e de novo durante a leitura, para valer
  /// mesmo quando o tamanho declarado não corresponde ao conteúdo. O <see cref="MemoryStream"/> retornado já vem
  /// posicionado no início.
  /// </remarks>
  /// <param name="file">Arquivo enviado.</param>
  /// <param name="maxBytes">
  /// Tamanho máximo do arquivo em bytes. Valor não positivo aplica <see cref="FileConvert.DefaultMaxBytes"/>.
  /// </param>
  /// <returns>
  /// Retorna um <see cref="MemoryStream"/>, ou nulo se o arquivo for nulo ou declarar tamanho zero.
  /// </returns>
  /// <exception cref="PayloadTooLargeException">Se o arquivo exceder o tamanho máximo permitido.</exception>
  public static MemoryStream? ToMemoryStream(this IFormFile? file, long maxBytes = 0)
  {
    if (!ShouldCopy(file, maxBytes))
    {
      return null;
    }

    using var stream = file!.OpenReadStream();

    // Um arquivo que declara tamanho e entrega um fluxo vazio continua resultando em um MemoryStream vazio
    return FileConvert.CopyToMemoryStream(stream, maxBytes) ?? new MemoryStream();
  }

  /// <summary>
  /// Copia o conteúdo binário do arquivo para um <see cref="MemoryStream"/> de forma assíncrona, com limite de
  /// tamanho.
  /// </summary>
  /// <remarks>
  /// O limite é conferido no tamanho declarado, antes de qualquer alocação, e de novo durante a leitura, para valer
  /// mesmo quando o tamanho declarado não corresponde ao conteúdo. O <see cref="MemoryStream"/> retornado já vem
  /// posicionado no início.
  /// </remarks>
  /// <param name="file">Arquivo enviado.</param>
  /// <param name="maxBytes">
  /// Tamanho máximo do arquivo em bytes. Valor não positivo aplica <see cref="FileConvert.DefaultMaxBytes"/>.
  /// </param>
  /// <param name="cancellationToken">Token de cancelamento da operação.</param>
  /// <returns>
  /// Retorna um <see cref="MemoryStream"/>, ou nulo se o arquivo for nulo ou declarar tamanho zero.
  /// </returns>
  /// <exception cref="PayloadTooLargeException">Se o arquivo exceder o tamanho máximo permitido.</exception>
  public static async Task<MemoryStream?> ToMemoryStreamAsync(
    this IFormFile? file,
    long maxBytes = 0,
    CancellationToken cancellationToken = default
  )
  {
    if (!ShouldCopy(file, maxBytes))
    {
      return null;
    }

    var stream = file!.OpenReadStream();

    await using (stream.ConfigureAwait(false))
    {
      // Um arquivo que declara tamanho e entrega um fluxo vazio continua resultando em um MemoryStream vazio
      return await FileConvert.CopyToMemoryStreamAsync(stream, maxBytes, cancellationToken).ConfigureAwait(false)
        ?? new MemoryStream();
    }
  }

  /// <summary>
  /// Extrai a extensão do nome do arquivo.
  /// </summary>
  /// <remarks>
  /// O nome é informado por quem envia o arquivo. São aceitas apenas extensões com letras e dígitos ASCII e no
  /// máximo dez caracteres; qualquer outra coisa resulta em nulo.
  /// </remarks>
  /// <param name="file">Arquivo enviado.</param>
  /// <returns>Retorna a extensão em maiúsculas, sem o ponto, ou nulo se não for possível extraí-la.</returns>
  public static string? GetExtension(this IFormFile? file) => FileConvert.ExtensionFromFileName(file?.FileName);

  /// <summary>
  /// Verifica se o arquivo é uma imagem válida, pela extensão do nome e pelo tamanho.
  /// </summary>
  /// <remarks>
  /// As extensões de imagem permitidas são: .JPG, .JPEG, .PNG, .GIF, .BMP, .SVG e .WEBP. O conteúdo não é
  /// inspecionado.
  /// </remarks>
  /// <param name="file">Arquivo enviado.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica o padrão de 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for uma imagem válida.</returns>
  public static bool IsImage(this IFormFile? file, long maxBytes = 0) =>
    file is not null && FileValid.IsImageFile(file.FileName, file.Length, maxBytes);

  /// <summary>
  /// Verifica se o arquivo é um documento válido, pela extensão do nome e pelo tamanho.
  /// </summary>
  /// <remarks>
  /// As extensões de documento permitidas são: .TXT, .CSV, .LOG, .PDF, .DOC, .DOCX, .XLS, .XLSX, .PPT e .PPTX. O
  /// conteúdo não é inspecionado.
  /// </remarks>
  /// <param name="file">Arquivo enviado.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica o padrão de 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um documento válido.</returns>
  public static bool IsDocument(this IFormFile? file, long maxBytes = 0) =>
    file is not null && FileValid.IsDocumentFile(file.FileName, file.Length, maxBytes);

  /// <summary>
  /// Verifica se o arquivo é um vídeo válido, pela extensão do nome e pelo tamanho.
  /// </summary>
  /// <remarks>
  /// As extensões de vídeo permitidas são: .AVI, .MP4, .MPG, .MPEG e .WMV. O conteúdo não é inspecionado.
  /// </remarks>
  /// <param name="file">Arquivo enviado.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica o padrão de 5242880 bytes (5MB).</param>
  /// <returns>Retorna verdadeiro se o arquivo for um vídeo válido.</returns>
  public static bool IsVideo(this IFormFile? file, long maxBytes = 0) =>
    file is not null && FileValid.IsVideoFile(file.FileName, file.Length, maxBytes);

  /// <summary>
  /// Verifica se o arquivo é válido para extensões personalizadas, pela extensão do nome e pelo tamanho.
  /// </summary>
  /// <remarks>
  /// As extensões são comparadas sem diferenciar maiúsculas de minúsculas e podem ser informadas com ou sem o
  /// ponto inicial: <c>"pdf"</c> e <c>".PDF"</c> têm o mesmo efeito. O conteúdo não é inspecionado.
  /// </remarks>
  /// <param name="file">Arquivo enviado.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica o padrão de 5242880 bytes (5MB).</param>
  /// <param name="permittedExtensions">
  /// Extensões permitidas. Nulo ou vazio aplica todas as extensões de imagem, documento e vídeo.
  /// </param>
  /// <returns>Retorna verdadeiro se o arquivo for válido.</returns>
  public static bool IsCustom(this IFormFile? file, long maxBytes = 0, string[]? permittedExtensions = null) =>
    file is not null && FileValid.IsCustomFile(file.FileName, file.Length, maxBytes, permittedExtensions);

  #endregion

  #region Private Methods

  /// <summary>
  /// Verifica se o arquivo tem conteúdo a copiar e se o tamanho declarado cabe no limite.
  /// </summary>
  /// <param name="file">Arquivo enviado.</param>
  /// <param name="maxBytes">Tamanho máximo solicitado, em bytes.</param>
  /// <returns>Verdadeiro se a cópia deve ser feita.</returns>
  /// <exception cref="PayloadTooLargeException">Se o tamanho declarado exceder o tamanho máximo permitido.</exception>
  private static bool ShouldCopy(IFormFile? file, long maxBytes)
  {
    if (file is null || file.Length <= 0)
    {
      return false;
    }

    // O mesmo limite que o FileConvert aplica: o padrão para valor não positivo, e nunca além de um MemoryStream
    var limit = Math.Min(maxBytes > 0 ? maxBytes : FileConvert.DefaultMaxBytes, int.MaxValue);

    // O tamanho declarado é recusado antes de abrir o arquivo; o conteúdo real é conferido na cópia
    if (file.Length > limit)
    {
      throw new PayloadTooLargeException($"{UtilErrorMessages.FileSizeExceeded};{limit}");
    }

    return true;
  }

  #endregion
}
