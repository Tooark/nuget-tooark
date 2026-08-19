using Microsoft.AspNetCore.Http;
using Moq;
using Tooark.Utils;

namespace Tooark.Tests.Utils;

public class FileValidTests
{
  // Teste de arquivo para validar imagem
  [Theory]
  [InlineData("test.jpg", true)]
  [InlineData("test.jpeg", true)]
  [InlineData("test.png", true)]
  [InlineData("test.gif", true)]
  [InlineData("test.bmp", true)]
  [InlineData("test.svg", true)]
  [InlineData("test.webp", true)]
  [InlineData("test.txt", false)]
  [InlineData("test.csv", false)]
  [InlineData("test.log", false)]
  [InlineData("test.pdf", false)]
  [InlineData("test.doc", false)]
  [InlineData("test.docx", false)]
  [InlineData("test.xls", false)]
  [InlineData("test.xlsx", false)]
  [InlineData("test.ppt", false)]
  [InlineData("test.pptx", false)]
  [InlineData("test.avi", false)]
  [InlineData("test.mp4", false)]
  [InlineData("test.mpg", false)]
  [InlineData("test.mpeg", false)]
  [InlineData("test.wmv", false)]
  [InlineData("test.tiff", false)]
  public void IsImage_ShouldReturnIfImage(string fileName, bool expected)
  {
    // Arrange
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns(fileName);

    // Act
    var result = FileValid.IsImage(fileMock.Object, 2048);

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste de arquivo para validar documento
  [Theory]
  [InlineData("test.jpg", false)]
  [InlineData("test.jpeg", false)]
  [InlineData("test.png", false)]
  [InlineData("test.gif", false)]
  [InlineData("test.bmp", false)]
  [InlineData("test.svg", false)]
  [InlineData("test.webp", false)]
  [InlineData("test.txt", true)]
  [InlineData("test.csv", true)]
  [InlineData("test.log", true)]
  [InlineData("test.pdf", true)]
  [InlineData("test.doc", true)]
  [InlineData("test.docx", true)]
  [InlineData("test.xls", true)]
  [InlineData("test.xlsx", true)]
  [InlineData("test.ppt", true)]
  [InlineData("test.pptx", true)]
  [InlineData("test.avi", false)]
  [InlineData("test.mp4", false)]
  [InlineData("test.mpg", false)]
  [InlineData("test.mpeg", false)]
  [InlineData("test.wmv", false)]
  [InlineData("test.tiff", false)]
  public void IsDocument_ShouldReturnIfDocument(string fileName, bool expected)
  {
    // Arrange
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns(fileName);

    // Act
    var result = FileValid.IsDocument(fileMock.Object, 2048);

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste de arquivo para validar video
  [Theory]
  [InlineData("test.jpg", false)]
  [InlineData("test.jpeg", false)]
  [InlineData("test.png", false)]
  [InlineData("test.gif", false)]
  [InlineData("test.bmp", false)]
  [InlineData("test.svg", false)]
  [InlineData("test.webp", false)]
  [InlineData("test.txt", false)]
  [InlineData("test.csv", false)]
  [InlineData("test.log", false)]
  [InlineData("test.pdf", false)]
  [InlineData("test.doc", false)]
  [InlineData("test.docx", false)]
  [InlineData("test.xls", false)]
  [InlineData("test.xlsx", false)]
  [InlineData("test.ppt", false)]
  [InlineData("test.pptx", false)]
  [InlineData("test.avi", true)]
  [InlineData("test.mp4", true)]
  [InlineData("test.mpg", true)]
  [InlineData("test.mpeg", true)]
  [InlineData("test.wmv", true)]
  [InlineData("test.tiff", false)]
  public void IsVideo_ShouldReturnIfVideo(string fileName, bool expected)
  {
    // Arrange
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns(fileName);

    // Act
    var result = FileValid.IsVideo(fileMock.Object, 2048);

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste de arquivo para validar tipo customizado, sem extensões personalizadas
  [Theory]
  [InlineData("test.jpg", true)]
  [InlineData("test.jpeg", true)]
  [InlineData("test.png", true)]
  [InlineData("test.gif", true)]
  [InlineData("test.bmp", true)]
  [InlineData("test.svg", true)]
  [InlineData("test.webp", true)]
  [InlineData("test.txt", true)]
  [InlineData("test.csv", true)]
  [InlineData("test.log", true)]
  [InlineData("test.pdf", true)]
  [InlineData("test.doc", true)]
  [InlineData("test.docx", true)]
  [InlineData("test.xls", true)]
  [InlineData("test.xlsx", true)]
  [InlineData("test.ppt", true)]
  [InlineData("test.pptx", true)]
  [InlineData("test.avi", true)]
  [InlineData("test.mp4", true)]
  [InlineData("test.mpg", true)]
  [InlineData("test.mpeg", true)]
  [InlineData("test.wmv", true)]
  [InlineData("test.tiff", false)]
  public void IsCustom_ShouldReturnIfCustom_WhenExtensionsDefault(string fileName, bool expected)
  {
    // Arrange
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns(fileName);

    // Act
    var result = FileValid.IsCustom(fileMock.Object, 2048);

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste de arquivo para validar tipo customizado, com extensões personalizadas
  [Theory]
  [InlineData("test.jpg", false)]
  [InlineData("test.jpeg", false)]
  [InlineData("test.png", false)]
  [InlineData("test.gif", false)]
  [InlineData("test.bmp", false)]
  [InlineData("test.svg", false)]
  [InlineData("test.webp", false)]
  [InlineData("test.txt", false)]
  [InlineData("test.csv", false)]
  [InlineData("test.log", false)]
  [InlineData("test.pdf", false)]
  [InlineData("test.doc", false)]
  [InlineData("test.docx", false)]
  [InlineData("test.xls", false)]
  [InlineData("test.xlsx", false)]
  [InlineData("test.ppt", false)]
  [InlineData("test.pptx", false)]
  [InlineData("test.avi", false)]
  [InlineData("test.mp4", false)]
  [InlineData("test.mpg", false)]
  [InlineData("test.mpeg", false)]
  [InlineData("test.wmv", false)]
  [InlineData("test.tiff", true)]
  public void IsCustom_ShouldReturnIfCustom_WhenExtensionsCustom(string fileName, bool expected)
  {
    // Arrange
    string[] extensions = [".tiff"];
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns(fileName);

    // Act
    var result = FileValid.IsCustom(fileMock.Object, 2048, extensions);

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste de arquivo de imagem para validar tamanho
  [Fact]
  public void IsImage_ShouldReturnFalse_WhenFileBigger()
  {
    // Arrange
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns("test.jpg");

    // Act
    var result = FileValid.IsImage(fileMock.Object, 512);

    // Assert
    Assert.False(result);
  }

  // Teste de arquivo de documento para validar tamanho
  [Fact]
  public void IsDocument_ShouldReturnFalse_WhenFileBigger()
  {
    // Arrange
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns("test.doc");

    // Act
    var result = FileValid.IsDocument(fileMock.Object, 512);

    // Assert
    Assert.False(result);
  }

  // Teste de arquivo de video para validar tamanho
  [Fact]
  public void IsVideo_ShouldReturnFalse_WhenFileBigger()
  {
    // Arrange
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns("test.avi");

    // Act
    var result = FileValid.IsVideo(fileMock.Object, 512);

    // Assert
    Assert.False(result);
  }

  // Teste de arquivo tipo customizado para validar tamanho
  [Fact]
  public void IsCustom_ShouldReturnFalse_WhenFileBigger()
  {
    // Arrange
    string[] extensions = [".tiff"];
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns("test.tiff");

    // Act
    var result = FileValid.IsCustom(fileMock.Object, 512, extensions);

    // Assert
    Assert.False(result);
  }

  // Teste de extensões personalizadas informadas com e sem o ponto inicial
  [Theory]
  [InlineData("pdf", true)]
  [InlineData(".pdf", true)]
  [InlineData("PDF", true)]
  [InlineData(".PDF", true)]
  [InlineData(" pdf ", true)]
  [InlineData("txt", false)]
  public void IsCustom_ShouldAcceptExtensionsWithOrWithoutDot(string extension, bool expected)
  {
    // Arrange
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns("test.pdf");

    // Act
    var result = FileValid.IsCustom(fileMock.Object, 2048, [extension]);

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste de extensões personalizadas com entradas vazias, que caem nas extensões padrão
  [Theory]
  [InlineData("test.pdf", true)]
  [InlineData("test.exe", false)]
  public void IsCustom_ShouldUseDefaultExtensions_WhenExtensionsAreEmpty(string fileName, bool expected)
  {
    // Arrange
    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(1024);
    fileMock.Setup(f => f.FileName).Returns(fileName);

    // Act
    var result = FileValid.IsCustom(fileMock.Object, 2048, ["", "   ", "."]);

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste de arquivo nulo
  [Fact]
  public void IsCustom_ShouldReturnFalse_WhenFileIsNull()
  {
    // Arrange & Act
    var result = FileValid.IsCustom(null, 2048, [".pdf"]);

    // Assert
    Assert.False(result);
  }
}
