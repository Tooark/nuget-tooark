using System.Net;
using Microsoft.AspNetCore.Http;
using Moq;
using Tooark.AspNetCore.Files;
using Tooark.Exceptions;
using Tooark.Utils;

namespace Tooark.Tests.AspNetCore.Files;

// Os mesmos casos dos métodos com IFormFile do FileConvert, agora pelas extensões do Tooark.AspNetCore.
public class FormFileConversionTests
{
  // Um upload binário é copiado byte a byte, com o fluxo posicionado no início.
  [Fact]
  public void ToMemoryStream_ShouldReturnTheSameBytes()
  {
    // Arrange
    byte[] content = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x33, 0x0A, 0x25, 0xC7, 0xEC, 0x8F, 0xA2];
    var file = CreateFormFile(content, "test.pdf");

    // Act
    var result = file.ToMemoryStream();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(0, result.Position);
    Assert.Equal(content, result.ToArray());
  }

  // Um arquivo vazio não tem o que copiar.
  [Fact]
  public void ToMemoryStream_ShouldReturnNull_WhenTheFileIsEmpty()
  {
    // Arrange
    var file = CreateFormFile([], "test.pdf");

    // Act
    var result = file.ToMemoryStream();

    // Assert
    Assert.Null(result);
  }

  // Um arquivo que declara tamanho e entrega um fluxo vazio resulta em um MemoryStream vazio, e não em nulo.
  [Fact]
  public async Task ToMemoryStream_ShouldReturnAnEmptyStream_WhenTheDeclaredLengthHasNoContent()
  {
    // Arrange
    var file = CreateFormFile([], "test.pdf", declaredLength: 32);

    // Act
    using var result = file.ToMemoryStream();
    await using var asyncResult = await file.ToMemoryStreamAsync(cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(0, result.Length);
    Assert.NotNull(asyncResult);
    Assert.Equal(0, asyncResult.Length);
  }

  // Um arquivo nulo resulta em nulo, na versão síncrona e na assíncrona.
  [Fact]
  public async Task ToMemoryStream_ShouldReturnNull_WhenTheFileIsNull()
  {
    // Arrange
    IFormFile? file = null;

    // Act
    var result = file.ToMemoryStream();
    var asyncResult = await file.ToMemoryStreamAsync(cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Null(result);
    Assert.Null(asyncResult);
  }

  // A versão assíncrona copia os mesmos bytes.
  [Fact]
  public async Task ToMemoryStreamAsync_ShouldReturnTheSameBytes()
  {
    // Arrange
    byte[] content = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    var file = CreateFormFile(content, "test.jpg");

    // Act
    var result = await file.ToMemoryStreamAsync(cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(0, result.Position);
    Assert.Equal(content, result.ToArray());
  }

  // O limite vale para o tamanho declarado do upload, com a mesma mensagem e o mesmo status do FileConvert.
  [Fact]
  public void ToMemoryStream_ShouldThrow_WhenAboveTheGivenLimit()
  {
    // Arrange
    var file = CreateFormFile(new byte[128], "test.pdf");

    // Act
    var exception = Assert.Throws<PayloadTooLargeException>(() => file.ToMemoryStream(64));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.GetStatusCode());
  }

  // O tamanho declarado acima do limite é recusado antes de o arquivo ser aberto.
  [Fact]
  public void ToMemoryStream_ShouldThrowBeforeOpeningTheFile_WhenTheDeclaredLengthIsAboveTheLimit()
  {
    // Arrange
    var mock = new Mock<IFormFile>();
    mock.Setup(_ => _.Length).Returns(128);

    // Act
    Assert.Throws<PayloadTooLargeException>(() => mock.Object.ToMemoryStream(64));

    // Assert
    mock.Verify(_ => _.OpenReadStream(), Times.Never);
  }

  // O limite vale durante a leitura quando o tamanho declarado não corresponde ao conteúdo real.
  [Fact]
  public void ToMemoryStream_ShouldThrow_WhenTheFileLiesAboutItsLength()
  {
    // Arrange
    var file = CreateFormFile(new byte[256 * 1024], "test.pdf", declaredLength: 32);

    // Act
    var exception = Assert.Throws<PayloadTooLargeException>(() => file.ToMemoryStream(64));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
  }

  // O mesmo limite durante a leitura, com um fluxo que não informa o tamanho.
  [Fact]
  public void ToMemoryStream_ShouldThrow_WhenAnUnseekableFileLiesAboutItsLength()
  {
    // Arrange
    var file = CreateFormFile(new byte[256 * 1024], "test.pdf", declaredLength: 32, seekable: false);

    // Act
    var exception = Assert.Throws<PayloadTooLargeException>(() => file.ToMemoryStream(64));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
  }

  // A versão assíncrona aplica o limite durante a leitura.
  [Fact]
  public async Task ToMemoryStreamAsync_ShouldThrow_WhenTheFileLiesAboutItsLength()
  {
    // Arrange
    var file = CreateFormFile(new byte[256 * 1024], "test.pdf", declaredLength: 32);

    // Act
    var exception = await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
      file.ToMemoryStreamAsync(64, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
  }

  // A versão assíncrona aplica o limite ao tamanho declarado.
  [Fact]
  public async Task ToMemoryStreamAsync_ShouldThrow_WhenAboveTheGivenLimit()
  {
    // Arrange
    var file = CreateFormFile(new byte[128], "test.pdf");

    // Act
    var exception = await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
      file.ToMemoryStreamAsync(64, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
  }

  // Um upload maior que o padrão é aceito quando o limite é ampliado.
  [Fact]
  public void ToMemoryStream_ShouldAcceptAboveTheDefault_WhenTheLimitIsHigher()
  {
    // Arrange
    var content = new byte[(int)FileConvert.DefaultMaxBytes + 1024];
    var file = CreateFormFile(content, "test.pdf");

    // Act
    var result = file.ToMemoryStream(FileConvert.DefaultMaxBytes * 2);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(content.Length, result.Length);
  }

  // Sem limite informado vale o padrão do FileConvert.
  [Fact]
  public void ToMemoryStream_ShouldThrow_WhenAboveTheDefaultLimit()
  {
    // Arrange
    var file = CreateFormFile(new byte[(int)FileConvert.DefaultMaxBytes + 1], "test.pdf");

    // Act & Assert
    var exception = Assert.Throws<PayloadTooLargeException>(() => file.ToMemoryStream());

    Assert.Equal($"File.SizeExceeded;{FileConvert.DefaultMaxBytes}", exception.Message);
  }

  // O arquivo aberto para a cópia é fechado ao final, nas duas versões.
  [Fact]
  public async Task ToMemoryStream_ShouldCloseTheFileStream()
  {
    // Arrange
    var first = new MemoryStream([1, 2, 3]);
    var second = new MemoryStream([1, 2, 3]);
    var mock = new Mock<IFormFile>();
    mock.Setup(_ => _.Length).Returns(3);
    mock.SetupSequence(_ => _.OpenReadStream()).Returns(first).Returns(second);

    // Act
    using var result = mock.Object.ToMemoryStream();
    await using var asyncResult = await mock.Object.ToMemoryStreamAsync(cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.False(first.CanRead);
    Assert.False(second.CanRead);
    Assert.Equal([1, 2, 3], result!.ToArray());
    Assert.Equal([1, 2, 3], asyncResult!.ToArray());
  }

  // A extensão sai do nome do arquivo, em maiúsculas e sem o ponto.
  [Theory]
  [InlineData("test.pdf", "PDF")]
  [InlineData("foto.png", "PNG")]
  // O byte nulo fica antes do último ponto, então a extensão real é mesmo .aspx
  [InlineData("arquivo.png\u0000.aspx", "ASPX")]
  [InlineData("shell.aspx;.png", "PNG")]
  [InlineData("..\\..\\..\\web.config", "CONFIG")]
  public void GetExtension_ShouldReturnTheSanitizedExtension(string fileName, string expected)
  {
    // Arrange
    var mock = new Mock<IFormFile>();
    mock.Setup(_ => _.FileName).Returns(fileName);

    // Act
    var result = mock.Object.GetExtension();

    // Assert
    Assert.Equal(expected, result);
  }

  // Sem extensão, com o nome terminado em ponto ou com uma extensão longa demais não há o que devolver.
  [Theory]
  [InlineData("testfile")]
  [InlineData("testfile.")]
  [InlineData("")]
  [InlineData("arquivo.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
  public void GetExtension_ShouldReturnNull_WhenThereIsNoValidExtension(string fileName)
  {
    // Arrange
    var mock = new Mock<IFormFile>();
    mock.Setup(_ => _.FileName).Returns(fileName);

    // Act
    var result = mock.Object.GetExtension();

    // Assert
    Assert.Null(result);
  }

  // Um arquivo nulo não tem extensão.
  [Fact]
  public void GetExtension_ShouldReturnNull_WhenTheFileIsNull()
  {
    // Arrange
    IFormFile? file = null;

    // Act
    var result = file.GetExtension();

    // Assert
    Assert.Null(result);
  }

  // Monta um IFormFile com o conteúdo binário informado.
  private static IFormFile CreateFormFile(byte[] content, string fileName, long? declaredLength = null, bool seekable = true)
  {
    var mock = new Mock<IFormFile>();
    mock.Setup(_ => _.OpenReadStream()).Returns(() => seekable ? new MemoryStream(content) : new UnseekableStream(content));
    mock.Setup(_ => _.FileName).Returns(fileName);
    mock.Setup(_ => _.Length).Returns(declaredLength ?? content.Length);

    return mock.Object;
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
