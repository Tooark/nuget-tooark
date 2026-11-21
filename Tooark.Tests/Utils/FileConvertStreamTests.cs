using System.Net;
using Tooark.Exceptions;
using Tooark.Utils;

namespace Tooark.Tests.Utils;

public class FileConvertStreamTests
{
  // O conteúdo é copiado byte a byte, com o destino posicionado no início e a origem aberta.
  [Fact]
  public void CopyToMemoryStream_ShouldReturnTheSameBytes()
  {
    // Arrange
    byte[] content = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x33];
    using var source = new MemoryStream(content);

    // Act
    using var result = FileConvert.CopyToMemoryStream(source);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(0, result.Position);
    Assert.Equal(content, result.ToArray());
    Assert.True(source.CanRead);
  }

  // A versão assíncrona copia os mesmos bytes.
  [Fact]
  public async Task CopyToMemoryStreamAsync_ShouldReturnTheSameBytes()
  {
    // Arrange
    byte[] content = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    using var source = new MemoryStream(content);

    // Act
    await using var result = await FileConvert.CopyToMemoryStreamAsync(source, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(0, result.Position);
    Assert.Equal(content, result.ToArray());
    Assert.True(source.CanRead);
  }

  // A cópia começa na posição atual da origem, e não no início dela.
  [Fact]
  public void CopyToMemoryStream_ShouldStartAtTheCurrentPosition()
  {
    // Arrange
    using var source = new MemoryStream([1, 2, 3, 4, 5]) { Position = 2 };

    // Act
    using var result = FileConvert.CopyToMemoryStream(source);

    // Assert
    Assert.Equal([3, 4, 5], result!.ToArray());
  }

  // Sem fluxo, com o fluxo vazio ou já lido até o fim não há conteúdo: o retorno é nulo.
  [Fact]
  public async Task CopyToMemoryStream_ShouldReturnNull_WhenThereIsNoContent()
  {
    // Arrange
    using var empty = new MemoryStream();
    using var consumed = new MemoryStream([1, 2, 3]) { Position = 3 };
    using var unseekableEmpty = new UnseekableStream([]);

    // Act & Assert
    Assert.Null(FileConvert.CopyToMemoryStream(null));
    Assert.Null(FileConvert.CopyToMemoryStream(empty));
    Assert.Null(FileConvert.CopyToMemoryStream(consumed));
    Assert.Null(FileConvert.CopyToMemoryStream(unseekableEmpty));
    Assert.Null(await FileConvert.CopyToMemoryStreamAsync(null, cancellationToken: TestContext.Current.CancellationToken));
    Assert.Null(await FileConvert.CopyToMemoryStreamAsync(empty, cancellationToken: TestContext.Current.CancellationToken));
  }

  // Um fluxo que informa o tamanho é recusado antes da leitura quando passa do limite.
  [Fact]
  public void CopyToMemoryStream_ShouldThrowBeforeReading_WhenTheLengthIsAboveTheLimit()
  {
    // Arrange
    using var source = new MemoryStream(new byte[128]);

    // Act
    var exception = Assert.Throws<PayloadTooLargeException>(() => FileConvert.CopyToMemoryStream(source, 64));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.GetStatusCode());
    Assert.Equal(0, source.Position);
  }

  // Um fluxo que não informa o tamanho tem o limite conferido bloco a bloco, durante a cópia.
  [Fact]
  public async Task CopyToMemoryStream_ShouldThrowWhileReading_WhenTheStreamIsUnseekable()
  {
    // Arrange
    using var source = new UnseekableStream(new byte[256 * 1024]);
    using var asyncSource = new UnseekableStream(new byte[256 * 1024]);

    // Act
    var exception = Assert.Throws<PayloadTooLargeException>(() => FileConvert.CopyToMemoryStream(source, 64));
    var asyncException = await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
      FileConvert.CopyToMemoryStreamAsync(asyncSource, 64, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
    Assert.Equal("File.SizeExceeded;64", asyncException.Message);
  }

  // Um fluxo que não informa o tamanho é copiado inteiro quando cabe no limite.
  [Fact]
  public async Task CopyToMemoryStream_ShouldCopyAnUnseekableStream()
  {
    // Arrange
    byte[] content = [.. Enumerable.Range(0, 200_000).Select(index => (byte)index)];
    using var source = new UnseekableStream(content);
    using var asyncSource = new UnseekableStream(content);

    // Act
    using var result = FileConvert.CopyToMemoryStream(source);
    await using var asyncResult = await FileConvert.CopyToMemoryStreamAsync(asyncSource, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(content, result!.ToArray());
    Assert.Equal(content, asyncResult!.ToArray());
  }

  // O conteúdo exatamente no limite é aceito; um byte a mais é recusado.
  [Fact]
  public void CopyToMemoryStream_ShouldAcceptContentExactlyAtTheLimit()
  {
    // Arrange
    using var atLimit = new MemoryStream(new byte[64]);
    using var aboveLimit = new MemoryStream(new byte[65]);

    // Act
    using var result = FileConvert.CopyToMemoryStream(atLimit, 64);

    // Assert
    Assert.Equal(64, result!.Length);
    Assert.Throws<PayloadTooLargeException>(() => FileConvert.CopyToMemoryStream(aboveLimit, 64));
  }

  // Sem limite informado, ou com um valor não positivo, vale o padrão.
  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void CopyToMemoryStream_ShouldApplyTheDefaultLimit(long maxBytes)
  {
    // Arrange
    using var source = new MemoryStream(new byte[(int)FileConvert.DefaultMaxBytes + 1]);

    // Act
    var exception = Assert.Throws<PayloadTooLargeException>(() => FileConvert.CopyToMemoryStream(source, maxBytes));

    // Assert
    Assert.Equal($"File.SizeExceeded;{FileConvert.DefaultMaxBytes}", exception.Message);
  }

  // A versão assíncrona respeita o cancelamento.
  [Fact]
  public async Task CopyToMemoryStreamAsync_ShouldHonorCancellation()
  {
    // Arrange
    using var source = new UnseekableStream(new byte[1024]);
    using var cancellation = new CancellationTokenSource();
    await cancellation.CancelAsync();

    // Act & Assert
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
      FileConvert.CopyToMemoryStreamAsync(source, cancellationToken: cancellation.Token));
  }

  // A extensão sai do nome do arquivo, em maiúsculas e sem o ponto, descartando o caminho.
  [Theory]
  [InlineData("test.pdf", "PDF")]
  [InlineData("foto.JPeG", "JPEG")]
  [InlineData("pasta/sub/arquivo.docx", "DOCX")]
  [InlineData("arquivo.tar.gz", "GZ")]
  // O byte nulo fica antes do último ponto, então a extensão real é mesmo .aspx
  [InlineData("arquivo.png\u0000.aspx", "ASPX")]
  [InlineData("shell.aspx;.png", "PNG")]
  [InlineData("..\\..\\..\\web.config", "CONFIG")]
  public void ExtensionFromFileName_ShouldReturnTheSanitizedExtension(string fileName, string expected)
  {
    // Arrange & Act
    var result = FileConvert.ExtensionFromFileName(fileName);

    // Assert
    Assert.Equal(expected, result);
  }

  // Sem nome, sem extensão, com caractere fora de letras e dígitos ou com mais de dez caracteres o retorno é nulo.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("testfile")]
  [InlineData("testfile.")]
  [InlineData("arquivo.p-f")]
  [InlineData("arquivo.ABCDEFGHIJK")]
  public void ExtensionFromFileName_ShouldReturnNull_WhenThereIsNoValidExtension(string? fileName)
  {
    // Arrange & Act
    var result = FileConvert.ExtensionFromFileName(fileName);

    // Assert
    Assert.Null(result);
  }

  // Fluxo que só lê para a frente, como o corpo de uma requisição: não informa tamanho nem posição.
  private sealed class UnseekableStream(byte[] content) : MemoryStream(content)
  {
    public override bool CanSeek => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
      get => throw new NotSupportedException();
      set => throw new NotSupportedException();
    }
  }
}
