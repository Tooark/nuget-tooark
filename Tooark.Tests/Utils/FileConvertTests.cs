using System.Net;
using Microsoft.AspNetCore.Http;
using Moq;
using Tooark.Exceptions;
using Tooark.Utils;

namespace Tooark.Tests.Utils;

public class FileConvertTests
{
  // Teste para o método ToMemoryStream(string) com uma string base64 válida.
  [Fact]
  public void ToMemoryStream_ValidBase64String_ReturnsMemoryStream()
  {
    // Arrange
    string base64String = "data:application/pdf;base64,JVBERi0xLjMKJcfs"; // Example base64 string

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(base64String);

    // Assert
    Assert.NotNull(result);
    Assert.True(result.Length > 0);
  }

  // Teste para o método ToMemoryStream(string) com uma string base64 inválida.
  [Fact]
  public void ToMemoryStream_InvalidBase64String_ReturnsNull()
  {
    // Arrange
    string invalidBase64String = "invalid base64, string";

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(invalidBase64String);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método ToMemoryStream(string) com uma string vazia.
  [Fact]
  public void ToMemoryStream_EmptyString_ReturnsNull()
  {
    // Arrange
    string emptyString = "";

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(emptyString);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método ToMemoryStream(IFormFile) com um upload binário, preservando os bytes originais.
  [Fact]
  public void ToMemoryStream_FromIFormFile_ReturnsSameBytes()
  {
    // Arrange
    byte[] content = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x33, 0x0A, 0x25, 0xC7, 0xEC, 0x8F, 0xA2];
    var mockFile = CreateFormFile(content, "test.pdf");

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(mockFile);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(0, result.Position);
    Assert.Equal(content, result.ToArray());
  }

  // Teste para o método ToMemoryStream(IFormFile) com um IFormFile vazio.
  [Fact]
  public void ToMemoryStream_EmptyIFormFile_ReturnsNull()
  {
    // Arrange
    var mockFile = CreateFormFile([], "test.pdf");

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(mockFile);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método ToMemoryStreamAsync(IFormFile) com um upload binário, preservando os bytes originais.
  [Fact]
  public async Task ToMemoryStreamAsync_FromIFormFile_ReturnsSameBytes()
  {
    // Arrange
    byte[] content = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    var mockFile = CreateFormFile(content, "test.jpg");

    // Act
    MemoryStream? result = await FileConvert.ToMemoryStreamAsync(mockFile, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(0, result.Position);
    Assert.Equal(content, result.ToArray());
  }

  // Teste para o método ToMemoryStreamAsync(IFormFile) com um IFormFile nulo.
  [Fact]
  public async Task ToMemoryStreamAsync_NullIFormFile_ReturnsNull()
  {
    // Arrange & Act
    MemoryStream? result = await FileConvert.ToMemoryStreamAsync(null, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método ToMemoryStream(IFormFile) com um IFormFile nulo.
  [Fact]
  public void ToMemoryStream_NullIFormFile_ReturnsNull()
  {
    // Arrange
    IFormFile? nullFile = null;

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(nullFile!);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método Extension(string) com uma string base64 válida.
  [Fact]
  public void Extension_ValidBase64String_ReturnsExtension()
  {
    // Arrange
    string base64String = "data:application/pdf;base64,JVBERi0xLjMKJcfs"; // Example base64 string

    // Act
    string? result = FileConvert.Extension(base64String);

    // Assert
    Assert.Equal("PDF", result);
  }

  // Teste para o método Extension(string) com uma string base64 inválida.
  [Fact]
  public void Extension_InvalidBase64String_ReturnsNull()
  {
    // Arrange
    string invalidBase64String = "invalid base64, string";

    // Act
    string? result = FileConvert.Extension(invalidBase64String);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método Extension(string) com uma string vazia.
  [Fact]
  public void Extension_EmptyString_ReturnsNull()
  {
    // Arrange
    string emptyString = "";

    // Act
    string? result = FileConvert.Extension(emptyString);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método Extension(IFormFile) com um IFormFile válido.
  [Fact]
  public void Extension_FromIFormFile_ReturnsExtension()
  {
    // Arrange
    var mockFile = new Mock<IFormFile>();
    mockFile.Setup(_ => _.FileName).Returns("test.pdf");

    // Act
    string? result = FileConvert.Extension(mockFile.Object);

    // Assert
    Assert.Equal("PDF", result);
  }

  // Teste para o método Extension(IFormFile) com um IFormFile nulo.
  [Fact]
  public void Extension_NullIFormFile_ReturnsNull()
  {
    // Arrange
    IFormFile? nullFile = null;

    // Act
    string? result = FileConvert.Extension(nullFile!);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método Extension(IFormFile) com um IFormFile sem extensão.
  [Fact]
  public void Extension_IFormFileWithoutExtension_ReturnsNull()
  {
    // Arrange
    var mockFile = new Mock<IFormFile>();
    var fileName = "testfile"; // No extension
    mockFile.Setup(_ => _.FileName).Returns(fileName);

    // Act
    string? result = FileConvert.Extension(mockFile.Object);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método Extension(string) com uma base64 sem o separador de tipo.
  [Theory]
  [InlineData("data:image;base64,QQ==")]
  [InlineData("data:;base64,QQ==")]
  [InlineData("data:application/;base64,QQ==")]
  public void Extension_Base64WithoutMediaType_ReturnsNull(string base64String)
  {
    // Arrange & Act
    string? result = FileConvert.Extension(base64String);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método Extension(IFormFile) com um nome de arquivo terminado em ponto.
  [Fact]
  public void Extension_IFormFileWithTrailingDot_ReturnsNull()
  {
    // Arrange
    var mockFile = new Mock<IFormFile>();
    mockFile.Setup(_ => _.FileName).Returns("testfile.");

    // Act
    string? result = FileConvert.Extension(mockFile.Object);

    // Assert
    Assert.Null(result);
  }

  // Monta um IFormFile com o conteúdo binário informado.
  private static IFormFile CreateFormFile(byte[] content, string fileName, long? declaredLength = null)
  {
    var mockFile = new Mock<IFormFile>();
    mockFile.Setup(_ => _.OpenReadStream()).Returns(() => new MemoryStream(content));
    mockFile.Setup(_ => _.FileName).Returns(fileName);
    mockFile.Setup(_ => _.Length).Returns(declaredLength ?? content.Length);

    return mockFile.Object;
  }

  // Teste para o método Extension(string) com tipo declarado que não tem forma de extensão.
  [Theory]
  [InlineData("data:image/..%5c..%5c..%5cweb.config;base64,QQ==")]
  [InlineData("data:a/b/c;base64,QQ==")]
  [InlineData("data:image/<script>;base64,QQ==")]
  [InlineData("data:image/../..;base64,QQ==")]
  [InlineData("data:image/svg+xml;base64,QQ==")]
  [InlineData("data:application/vnd.ms-excel;base64,QQ==")]
  public void Extension_MediaTypeNotShapedAsExtension_ReturnsNull(string base64String)
  {
    // Arrange
    base64String = base64String.Replace("%5c", "\\");

    // Act
    string? result = FileConvert.Extension(base64String);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método Extension(string) com tipo declarado maior que o comprimento aceito.
  [Fact]
  public void Extension_MediaTypeTooLong_ReturnsNull()
  {
    // Arrange
    var base64String = $"data:x/{new string('A', 300)};base64,QQ==";

    // Act
    string? result = FileConvert.Extension(base64String);

    // Assert
    Assert.Null(result);
  }

  // Teste para o método Extension(string) com parâmetro no tipo declarado.
  [Fact]
  public void Extension_MediaTypeWithParameter_ReturnsExtension()
  {
    // Arrange
    var base64String = "data:image/png;charset=utf-8;base64,QQ==";

    // Act
    string? result = FileConvert.Extension(base64String);

    // Assert
    Assert.Equal("PNG", result);
  }

  // Teste para o método Extension(IFormFile) com nome de arquivo hostil.
  [Theory]
  // O byte nulo fica antes do ultimo ponto, entao a extensao real e mesmo .aspx
  [InlineData("arquivo.png\u0000.aspx", "ASPX")]
  [InlineData("shell.aspx;.png", "PNG")]
  [InlineData("..\\..\\..\\web.config", "CONFIG")]
  [InlineData("foto.png", "PNG")]
  public void Extension_FromIFormFileWithHostileName_IsSanitized(string fileName, string? expected)
  {
    // Arrange
    var mockFile = new Mock<IFormFile>();
    mockFile.Setup(_ => _.FileName).Returns(fileName);

    // Act
    string? result = FileConvert.Extension(mockFile.Object);

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para o método Extension(IFormFile) com extensão maior que o comprimento aceito.
  [Fact]
  public void Extension_FromIFormFileWithLongExtension_ReturnsNull()
  {
    // Arrange
    var mockFile = new Mock<IFormFile>();
    mockFile.Setup(_ => _.FileName).Returns($"arquivo.{new string('A', 200)}");

    // Act
    string? result = FileConvert.Extension(mockFile.Object);

    // Assert
    Assert.Null(result);
  }

  // Teste do marcador de base64 exigido no formato completo.
  [Theory]
  [InlineData("xxbase64,QQ==")]
  [InlineData("nao-e-data-url base64,QQ==")]
  [InlineData("BASE64,QQ==")]
  public void ToMemoryStream_MarkerOutsideDataUrlShape_ReturnsNull(string stringFile)
  {
    // Arrange & Act
    MemoryStream? result = FileConvert.ToMemoryStream(stringFile);

    // Assert
    Assert.Null(result);
  }

  // Teste do conteúdo lido logo após o marcador, e não a partir da última vírgula.
  [Fact]
  public void ToMemoryStream_ContentWithExtraComma_ReadsRightAfterMarker()
  {
    // Arrange
    var stringFile = "data:text/plain;base64,QUJD,WFla"; // "ABC" seguido de "XYZ"

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(stringFile);

    // Assert
    Assert.Null(result);
  }

  // Teste do marcador aceito sem diferenciar maiúsculas de minúsculas.
  [Fact]
  public void ToMemoryStream_MarkerIsCaseInsensitive_ReturnsMemoryStream()
  {
    // Arrange
    var stringFile = "data:text/plain;BASE64,QUJD";

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(stringFile);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("ABC", System.Text.Encoding.UTF8.GetString(result.ToArray()));
  }

  // Teste do limite padrão aplicado à conversão de base64.
  [Fact]
  public void ToMemoryStream_Base64AboveDefaultLimit_Throws()
  {
    // Arrange
    var stringFile = "data:application/octet-stream;base64," + new string('A', (int)FileConvert.DefaultMaxBytes / 3 * 4 + 8);

    // Act & Assert
    var exception = Assert.Throws<PayloadTooLargeException>(() => FileConvert.ToMemoryStream(stringFile));

    // Assert
    Assert.Equal($"File.SizeExceeded;{FileConvert.DefaultMaxBytes}", exception.Message);
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.GetStatusCode());
  }

  // Teste do limite informado aplicado à conversão de base64.
  [Fact]
  public void ToMemoryStream_Base64AboveGivenLimit_Throws()
  {
    // Arrange
    var stringFile = "data:application/octet-stream;base64," + Convert.ToBase64String(new byte[64]);

    // Act & Assert
    var exception = Assert.Throws<PayloadTooLargeException>(() => FileConvert.ToMemoryStream(stringFile, 32));

    // Assert
    Assert.Equal("File.SizeExceeded;32", exception.Message);
  }

  // Teste do conteúdo no limite exato, que deve ser aceito.
  [Fact]
  public void ToMemoryStream_Base64AtGivenLimit_ReturnsMemoryStream()
  {
    // Arrange
    var stringFile = "data:application/octet-stream;base64," + Convert.ToBase64String(new byte[64]);

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(stringFile, 64);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(64, result.Length);
  }

  // Teste do limite aplicado ao tamanho declarado do upload.
  [Fact]
  public void ToMemoryStream_IFormFileAboveGivenLimit_Throws()
  {
    // Arrange
    var mockFile = CreateFormFile(new byte[128], "test.pdf");

    // Act & Assert
    var exception = Assert.Throws<PayloadTooLargeException>(() => FileConvert.ToMemoryStream(mockFile, 64));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
  }

  // Teste do limite aplicado durante a leitura, quando o tamanho declarado não corresponde ao conteúdo real.
  [Fact]
  public void ToMemoryStream_IFormFileLyingAboutLength_Throws()
  {
    // Arrange
    var mockFile = CreateFormFile(new byte[256 * 1024], "test.pdf", declaredLength: 32);

    // Act & Assert
    var exception = Assert.Throws<PayloadTooLargeException>(() => FileConvert.ToMemoryStream(mockFile, 64));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
  }

  // Teste do limite aplicado durante a leitura na versão assíncrona.
  [Fact]
  public async Task ToMemoryStreamAsync_IFormFileLyingAboutLength_Throws()
  {
    // Arrange
    var mockFile = CreateFormFile(new byte[256 * 1024], "test.pdf", declaredLength: 32);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
      FileConvert.ToMemoryStreamAsync(mockFile, 64, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
  }

  // Teste do limite aplicado ao tamanho declarado do upload na versão assíncrona.
  [Fact]
  public async Task ToMemoryStreamAsync_IFormFileAboveGivenLimit_Throws()
  {
    // Arrange
    var mockFile = CreateFormFile(new byte[128], "test.pdf");

    // Act & Assert
    var exception = await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
      FileConvert.ToMemoryStreamAsync(mockFile, 64, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("File.SizeExceeded;64", exception.Message);
  }

  // Teste do upload maior que o padrão, aceito quando o limite é ampliado.
  [Fact]
  public void ToMemoryStream_IFormFileAboveDefaultWithHigherLimit_ReturnsMemoryStream()
  {
    // Arrange
    var content = new byte[(int)FileConvert.DefaultMaxBytes + 1024];
    var mockFile = CreateFormFile(content, "test.pdf");

    // Act
    MemoryStream? result = FileConvert.ToMemoryStream(mockFile, FileConvert.DefaultMaxBytes * 2);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(content.Length, result.Length);
  }

  // Teste do upload acima do limite padrão, sem limite informado.
  [Fact]
  public void ToMemoryStream_IFormFileAboveDefaultLimit_Throws()
  {
    // Arrange
    var mockFile = CreateFormFile(new byte[(int)FileConvert.DefaultMaxBytes + 1], "test.pdf");

    // Act & Assert
    Assert.Throws<PayloadTooLargeException>(() => FileConvert.ToMemoryStream(mockFile));
  }
}
