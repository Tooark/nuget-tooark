using Microsoft.AspNetCore.Http;
using Tooark.Exceptions;
using Tooark.Utils.Messages;

namespace Tooark.Utils;

/// <summary>
/// Classe estática que fornece métodos para conversão de arquivos e extração de extensões.
/// </summary>
/// <remarks>
/// A conversão mantém o conteúdo em memória e por isso é limitada por tamanho: acima do limite é lançada
/// uma <see cref="PayloadTooLargeException"/>, em vez de o processo alocar o que o cliente pedir.
/// A extensão é extraída do que o cliente declarou — o nome do arquivo ou o tipo da data URL — e por isso
/// é validada como extensão, mas não prova qual é o conteúdo real do arquivo.
/// </remarks>
public static class FileConvert
{
  #region Constants

  /// <summary>
  /// Tamanho máximo padrão do conteúdo convertido, em bytes. Padrão 5242880 bytes (5MB).
  /// </summary>
  public const long DefaultMaxBytes = 5242880;

  #endregion

  #region Methods

  /// <summary>
  /// Converte uma string base64 para <see cref="MemoryStream"/>.
  /// </summary>
  /// <remarks>
  /// A string precisa conter o marcador <c>;base64,</c>, e o conteúdo é lido logo depois dele.
  /// O limite é conferido pelo comprimento do trecho base64, antes de qualquer alocação.
  /// </remarks>
  /// <param name="stringFile">String de arquivo no formato <c>data:tipo/extensao;base64,conteudo</c>.</param>
  /// <param name="maxBytes">Tamanho máximo do conteúdo em bytes. Valor não positivo aplica <see cref="DefaultMaxBytes"/>.</param>
  /// <returns>Retorna um <see cref="MemoryStream"/>, ou nulo se a string não for um base64 válido.</returns>
  /// <exception cref="PayloadTooLargeException">Se o conteúdo exceder o tamanho máximo permitido.</exception>
  public static MemoryStream? ToMemoryStream(string? stringFile, long maxBytes = 0)
  {
    return InternalFileConvert.ToMemoryStream(stringFile, maxBytes);
  }

  /// <summary>
  /// Converte uma IFormFile para <see cref="MemoryStream"/>.
  /// </summary>
  /// <remarks>
  /// Copia o conteúdo binário do arquivo. O <see cref="MemoryStream"/> retornado já vem posicionado no início.
  /// </remarks>
  /// <param name="fromFile">Arquivo em formato de <see cref="IFormFile"/>.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica <see cref="DefaultMaxBytes"/>.</param>
  /// <returns>Retorna um <see cref="MemoryStream"/>, ou nulo se o arquivo for nulo ou vazio.</returns>
  /// <exception cref="PayloadTooLargeException">Se o arquivo exceder o tamanho máximo permitido.</exception>
  public static MemoryStream? ToMemoryStream(IFormFile? fromFile, long maxBytes = 0)
  {
    return InternalFileConvert.ToMemoryStream(fromFile, maxBytes);
  }

  /// <summary>
  /// Converte uma IFormFile para <see cref="MemoryStream"/> de forma assíncrona.
  /// </summary>
  /// <remarks>
  /// Copia o conteúdo binário do arquivo. O <see cref="MemoryStream"/> retornado já vem posicionado no início.
  /// </remarks>
  /// <param name="fromFile">Arquivo em formato de <see cref="IFormFile"/>.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes. Valor não positivo aplica <see cref="DefaultMaxBytes"/>.</param>
  /// <param name="cancellationToken">Token de cancelamento da operação.</param>
  /// <returns>Retorna um <see cref="MemoryStream"/>, ou nulo se o arquivo for nulo ou vazio.</returns>
  /// <exception cref="PayloadTooLargeException">Se o arquivo exceder o tamanho máximo permitido.</exception>
  public static Task<MemoryStream?> ToMemoryStreamAsync(IFormFile? fromFile, long maxBytes = 0, CancellationToken cancellationToken = default)
  {
    return InternalFileConvert.ToMemoryStreamAsync(fromFile, maxBytes, cancellationToken);
  }

  /// <summary>
  /// Extrai a extensão do arquivo.
  /// </summary>
  /// <remarks>
  /// A extensão vem do tipo declarado na data URL, que é informado por quem envia o arquivo. São aceitas
  /// apenas extensões com letras e dígitos ASCII e no máximo dez caracteres; qualquer outra coisa resulta
  /// em nulo, para que o valor não possa carregar caminho, marcação ou texto arbitrário.
  /// </remarks>
  /// <param name="stringFile">String de arquivo no formato <c>data:tipo/extensao;base64,conteudo</c>.</param>
  /// <returns>Retorna a extensão do arquivo em maiúsculas, ou nulo se não for possível extraí-la.</returns>
  public static string? Extension(string? stringFile)
  {
    return InternalFileConvert.Extension(stringFile);
  }

  /// <summary>
  /// Extrai a extensão do arquivo.
  /// </summary>
  /// <remarks>
  /// A extensão vem do nome do arquivo, que é informado por quem envia o arquivo. São aceitas apenas
  /// extensões com letras e dígitos ASCII e no máximo dez caracteres; qualquer outra coisa resulta em nulo.
  /// </remarks>
  /// <param name="fromFile">Arquivo em formato de <see cref="IFormFile"/>.</param>
  /// <returns>Retorna a extensão do arquivo em maiúsculas, sem o ponto, ou nulo se não for possível extraí-la.</returns>
  public static string? Extension(IFormFile? fromFile)
  {
    return InternalFileConvert.Extension(fromFile);
  }

  #endregion
}

/// <summary>
/// Classe estática interna que fornece métodos para conversão de arquivos e extração de extensões.
/// </summary>
internal static class InternalFileConvert
{
  #region Constants

  /// <summary>
  /// Marcador que separa os metadados do conteúdo em uma data URL.
  /// </summary>
  private const string Base64Marker = ";base64,";

  /// <summary>
  /// Comprimento máximo aceito para uma extensão de arquivo.
  /// </summary>
  private const int MaxExtensionLength = 10;

  /// <summary>
  /// Tamanho do bloco usado na cópia do arquivo, igual ao adotado por <see cref="Stream.CopyTo(Stream)"/>.
  /// </summary>
  private const int CopyBufferSize = 81920;

  #endregion

  #region Internal Methods

  /// <summary>
  /// Converte uma string base64 para <see cref="MemoryStream"/>.
  /// </summary>
  /// <param name="file">String de arquivo em base64.</param>
  /// <param name="maxBytes">Tamanho máximo do conteúdo em bytes.</param>
  /// <returns>Retorna um <see cref="MemoryStream"/>, ou nulo se a string não for um base64 válido.</returns>
  /// <exception cref="PayloadTooLargeException">Se o conteúdo exceder o tamanho máximo permitido.</exception>
  internal static MemoryStream? ToMemoryStream(string? file, long maxBytes = 0)
  {
    // Localiza o conteúdo depois do marcador, sem copiar a string
    var content = Content(file);

    // Verifica se a string traz o marcador e algum conteúdo depois dele
    if (content.IsEmpty)
    {
      // Retorna nulo caso o arquivo seja inválido
      return null;
    }

    // Resolve o limite aplicável
    var limit = ResolveMaxBytes(maxBytes);

    // Deriva o tamanho decodificado do comprimento do trecho, permitindo reprovar o excesso antes de alocar
    var estimated = DecodedLength(content);

    // Verifica o tamanho antes de reservar memória, para que um envio grande não escolha quanto o processo aloca
    if (estimated > limit)
    {
      // Lança a exceção de carga muito grande com o limite aplicado
      throw TooLarge(limit);
    }

    // Reserva apenas o necessário para o conteúdo declarado
    var buffer = new byte[(int)estimated];

    // Decodifica direto do trecho, sem a cópia intermediária da string
    if (!Convert.TryFromBase64Chars(content, buffer, out var written))
    {
      // Retorna nulo caso o conteúdo não seja um base64 válido
      return null;
    }

    // Retorna o arquivo em MemoryStream, limitado ao que foi realmente decodificado
    return new MemoryStream(buffer, 0, written, true, false);
  }

  /// <summary>
  /// Converte uma IFormFile para <see cref="MemoryStream"/>.
  /// </summary>
  /// <param name="fromFile">Arquivo em formato de <see cref="IFormFile"/>.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes.</param>
  /// <returns>Retorna um <see cref="MemoryStream"/>, ou nulo se o arquivo for nulo ou vazio.</returns>
  /// <exception cref="PayloadTooLargeException">Se o arquivo exceder o tamanho máximo permitido.</exception>
  internal static MemoryStream? ToMemoryStream(IFormFile? fromFile, long maxBytes = 0)
  {
    // Resolve o limite aplicável
    var limit = ResolveMaxBytes(maxBytes);

    // Verifica se há conteúdo a copiar e se o tamanho declarado cabe no limite
    if (!ShouldCopy(fromFile, limit))
    {
      // Retorna nulo caso o arquivo seja inválido
      return null;
    }

    // Cria o destino já com o tamanho declarado, evitando o redimensionamento durante a cópia
    var destination = new MemoryStream((int)fromFile!.Length);

    try
    {
      // Copia o conteúdo binário do arquivo, sem interpretá-lo como texto
      using var stream = fromFile.OpenReadStream();

      // Aloca o bloco de cópia
      var buffer = new byte[CopyBufferSize];

      // Acumula o total copiado para conferir o limite também durante a leitura
      long total = 0;
      int read;

      // Copia bloco a bloco, para que o limite valha mesmo se o tamanho declarado não corresponder ao real
      while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
      {
        total += read;

        // Verifica o limite a cada bloco lido
        if (total > limit)
        {
          // Lança a exceção de carga muito grande com o limite aplicado
          throw TooLarge(limit);
        }

        destination.Write(buffer, 0, read);
      }
    }
    catch
    {
      // Descarta o destino parcial antes de propagar o erro
      destination.Dispose();

      throw;
    }

    // Posiciona o fluxo no início para que o consumidor possa lê-lo
    destination.Position = 0;

    // Retorna o arquivo em MemoryStream
    return destination;
  }

  /// <summary>
  /// Converte uma IFormFile para <see cref="MemoryStream"/> de forma assíncrona.
  /// </summary>
  /// <param name="fromFile">Arquivo em formato de <see cref="IFormFile"/>.</param>
  /// <param name="maxBytes">Tamanho máximo do arquivo em bytes.</param>
  /// <param name="cancellationToken">Token de cancelamento da operação.</param>
  /// <returns>Retorna um <see cref="MemoryStream"/>, ou nulo se o arquivo for nulo ou vazio.</returns>
  /// <exception cref="PayloadTooLargeException">Se o arquivo exceder o tamanho máximo permitido.</exception>
  internal static async Task<MemoryStream?> ToMemoryStreamAsync(IFormFile? fromFile, long maxBytes = 0, CancellationToken cancellationToken = default)
  {
    // Resolve o limite aplicável
    var limit = ResolveMaxBytes(maxBytes);

    // Verifica se há conteúdo a copiar e se o tamanho declarado cabe no limite
    if (!ShouldCopy(fromFile, limit))
    {
      // Retorna nulo caso o arquivo seja inválido
      return null;
    }

    // Cria o destino já com o tamanho declarado, evitando o redimensionamento durante a cópia
    var destination = new MemoryStream((int)fromFile!.Length);

    try
    {
      // Copia o conteúdo binário do arquivo, sem interpretá-lo como texto
      await using var stream = fromFile.OpenReadStream();

      // Aloca o bloco de cópia
      var buffer = new byte[CopyBufferSize];

      // Acumula o total copiado para conferir o limite também durante a leitura
      long total = 0;
      int read;

      // Copia bloco a bloco, para que o limite valha mesmo se o tamanho declarado não corresponder ao real
      while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
      {
        total += read;

        // Verifica o limite a cada bloco lido
        if (total > limit)
        {
          // Lança a exceção de carga muito grande com o limite aplicado
          throw TooLarge(limit);
        }

        await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
      }
    }
    catch
    {
      // Descarta o destino parcial antes de propagar o erro
      await destination.DisposeAsync().ConfigureAwait(false);

      throw;
    }

    // Posiciona o fluxo no início para que o consumidor possa lê-lo
    destination.Position = 0;

    // Retorna o arquivo em MemoryStream
    return destination;
  }

  /// <summary>
  /// Extrai a extensão do arquivo.
  /// </summary>
  /// <param name="file">String de arquivo em base64.</param>
  /// <returns>Retorna a extensão do arquivo em maiúsculas, ou nulo se não for possível extraí-la.</returns>
  internal static string? Extension(string? file)
  {
    // Localiza o marcador que encerra os metadados
    var marker = MarkerIndex(file);

    // Verifica se a string traz o marcador
    if (marker < 0)
    {
      // Retorna nulo caso o arquivo seja inválido
      return null;
    }

    // Pega a posição da barra que separa o tipo da extensão, que precisa vir antes do marcador
    var separator = file!.IndexOf('/');

    // Verifica se a barra existe dentro dos metadados
    if (separator < 0 || separator >= marker)
    {
      // Retorna nulo caso o arquivo não declare o tipo no formato "tipo/extensao"
      return null;
    }

    // O tipo termina no primeiro ponto e vírgula depois da barra, que no limite é o próprio marcador
    var end = file.IndexOf(';', separator + 1);

    // Retorna a extensão do arquivo, se ela for uma extensão válida
    return SanitizeExtension(file.AsSpan(separator + 1, end - separator - 1));
  }

  /// <summary>
  /// Extrai a extensão do arquivo.
  /// </summary>
  /// <param name="fromFile">Arquivo em formato de <see cref="IFormFile"/>.</param>
  /// <returns>Retorna a extensão do arquivo em maiúsculas, sem o ponto, ou nulo se não for possível extraí-la.</returns>
  internal static string? Extension(IFormFile? fromFile)
  {
    // Verifica se o arquivo é nulo ou se não possui nome
    if (string.IsNullOrEmpty(fromFile?.FileName))
    {
      // Retorna nulo caso o arquivo seja inválido
      return null;
    }

    // Pega a extensão do arquivo, que já descarta o que vier antes de um separador de diretório
    var extension = Path.GetExtension(fromFile.FileName);

    // Verifica se a extensão não é nula ou vazia
    if (string.IsNullOrEmpty(extension))
    {
      // Retorna nulo caso a extensão seja nula ou vazia
      return null;
    }

    // Remove o ponto da extensão e retorna, se ela for uma extensão válida
    return SanitizeExtension(extension.AsSpan(1));
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Localiza a posição do marcador de base64 na string.
  /// </summary>
  /// <param name="file">String de arquivo em base64.</param>
  /// <returns>A posição do marcador, ou -1 se ele não existir.</returns>
  private static int MarkerIndex(string? file)
  {
    // Exige o marcador completo, e não apenas o texto "base64," em qualquer posição
    return string.IsNullOrEmpty(file) ? -1 : file.IndexOf(Base64Marker, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Obtém o trecho da string que vem depois do marcador de base64.
  /// </summary>
  /// <param name="file">String de arquivo em base64.</param>
  /// <returns>O trecho com o conteúdo, ou um trecho vazio se o marcador não existir.</returns>
  private static ReadOnlySpan<char> Content(string? file)
  {
    // Localiza o marcador que separa os metadados do conteúdo
    var marker = MarkerIndex(file);

    // O conteúdo começa logo depois do marcador, e não a partir da última vírgula da string
    return marker < 0 ? default : file.AsSpan(marker + Base64Marker.Length);
  }

  /// <summary>
  /// Calcula quantos bytes o trecho base64 produz ao ser decodificado.
  /// </summary>
  /// <remarks>
  /// Cada quatro caracteres viram três bytes, descontados os caracteres de preenchimento do fim. Espaços em
  /// branco entram na conta e apenas aumentam o resultado, que é sempre um limite superior do tamanho real.
  /// </remarks>
  /// <param name="content">O trecho com o conteúdo em base64.</param>
  /// <returns>O tamanho em bytes do conteúdo decodificado.</returns>
  private static long DecodedLength(ReadOnlySpan<char> content)
  {
    // Desconsidera espaços em branco no fim, como o próprio decodificador faz
    var trimmed = content.TrimEnd();

    // Conta os caracteres de preenchimento, que não produzem bytes
    long padding = 0;

    // O preenchimento ocupa no máximo as duas últimas posições
    if (!trimmed.IsEmpty && trimmed[^1] == '=')
    {
      padding++;

      if (trimmed.Length > 1 && trimmed[^2] == '=')
      {
        padding++;
      }
    }

    // Calcula o tamanho decodificado, sem permitir resultado negativo em trechos malformados
    var length = (content.Length / 4L * 3L) - padding;

    return length > 0 ? length : 0;
  }

  /// <summary>
  /// Valida o trecho como extensão de arquivo, aceitando apenas letras e dígitos ASCII.
  /// </summary>
  /// <remarks>
  /// A extensão é informada por quem envia o arquivo. Sem a validação, o retorno poderia carregar
  /// separadores de diretório, marcação ou texto de tamanho arbitrário para dentro de quem a consome.
  /// </remarks>
  /// <param name="extension">O trecho a ser validado.</param>
  /// <returns>A extensão em maiúsculas, ou nulo se o trecho não for uma extensão válida.</returns>
  private static string? SanitizeExtension(ReadOnlySpan<char> extension)
  {
    // Verifica se há conteúdo e se ele cabe no comprimento aceito
    if (extension.IsEmpty || extension.Length > MaxExtensionLength)
    {
      // Retorna nulo caso o trecho não tenha forma de extensão
      return null;
    }

    // Verifica cada caractere do trecho
    foreach (var character in extension)
    {
      // Rejeita qualquer caractere fora de letras e dígitos ASCII
      if (!char.IsAsciiLetterOrDigit(character))
      {
        // Retorna nulo caso o trecho contenha caractere não permitido
        return null;
      }
    }

    // Retorna a extensão em maiúsculas
    return extension.ToString().ToUpperInvariant();
  }

  /// <summary>
  /// Resolve o limite de tamanho aplicável à conversão.
  /// </summary>
  /// <param name="maxBytes">Tamanho máximo solicitado, em bytes.</param>
  /// <returns>O limite a ser aplicado, nunca acima do que um <see cref="MemoryStream"/> comporta.</returns>
  private static long ResolveMaxBytes(long maxBytes)
  {
    // Valor não positivo aplica o padrão, seguindo a mesma convenção do FileValid
    var limit = maxBytes > 0 ? maxBytes : FileConvert.DefaultMaxBytes;

    // Um MemoryStream não vai além de int.MaxValue, então esse é o teto real
    return limit > int.MaxValue ? int.MaxValue : limit;
  }

  /// <summary>
  /// Verifica se o arquivo tem conteúdo a copiar e se o tamanho declarado cabe no limite.
  /// </summary>
  /// <param name="fromFile">Arquivo em formato de <see cref="IFormFile"/>.</param>
  /// <param name="limit">O limite a ser aplicado, em bytes.</param>
  /// <returns>Verdadeiro se a cópia deve ser feita.</returns>
  /// <exception cref="PayloadTooLargeException">Se o arquivo exceder o tamanho máximo permitido.</exception>
  private static bool ShouldCopy(IFormFile? fromFile, long limit)
  {
    // Verifica se o arquivo é nulo ou vazio
    if (fromFile == null || fromFile.Length <= 0)
    {
      // Retorna falso caso o arquivo seja inválido
      return false;
    }

    // Verifica o tamanho declarado antes de reservar memória
    if (fromFile.Length > limit)
    {
      // Lança a exceção de carga muito grande com o limite aplicado
      throw TooLarge(limit);
    }

    // Retorna verdadeiro para que a cópia seja feita
    return true;
  }

  /// <summary>
  /// Monta a exceção de carga muito grande com o limite aplicado.
  /// </summary>
  /// <param name="limit">O limite aplicado, em bytes.</param>
  /// <returns>A exceção a ser lançada.</returns>
  private static PayloadTooLargeException TooLarge(long limit)
  {
    // Monta a chave de tradução com o limite como parâmetro
    return new PayloadTooLargeException($"{UtilErrorMessages.FileSizeExceeded};{limit}");
  }

  #endregion
}
