using System.Net;
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
}
