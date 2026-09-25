using Tooark.Enums;
using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class FileStorageTests
{
  // Testa se os dados do arquivo do storage são válidos
  [Theory]
  [InlineData("http://example.com", "path/file")]
  [InlineData("https://example.com", "path/file.txt")]
  [InlineData("https://example.com", " ")]
  [InlineData("https://example.com", "")]
  [InlineData("https://example.com", null)]
  public void FileStorage_ShouldBeValid_WhenParamsAreValid(ProtocolHttp linkParam, string? nameParam)
  {
    // Arrange
    string expectedLink = linkParam;
    string expectedName = string.IsNullOrWhiteSpace(nameParam) ? expectedLink : nameParam;

    // Act
    var linkStorage = new FileStorage(linkParam, nameParam);

    // Assert
    Assert.True(linkStorage.IsValid);
    Assert.Equal(expectedLink, linkStorage.Link);
    Assert.Equal(expectedName, linkStorage.Name);
  }

  // Testa se os dados do arquivo do storage são inválido
  [Theory]
  [InlineData("example.com")]
  [InlineData(" ")]
  [InlineData("")]
  [InlineData(null)]
  public void FileStorage_ShouldBeInvalid_WhenParamIsInvalid(string? valueParam)
  {
    // Arrange & Act
    var linkStorage = new FileStorage(valueParam!);

    // Assert
    Assert.False(linkStorage.IsValid);
  }

  // Testa se o método ToString retorna o link do arquivo do storage
  [Fact]
  public void FileStorage_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var link = "https://example.com";
    var name = "path/file";
    var linkStorage = new FileStorage(link, name);

    // Act
    var FileStorageString = linkStorage.ToString();

    // Assert
    Assert.Equal(link, FileStorageString);
  }

  // Testa se os dados do arquivo do storage são válido para conversão implícita para uma string
  [Fact]
  public void FileStorage_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var link = "https://example.com";
    var name = "path/file";
    var linkStorage = new FileStorage(link, name);

    // Act
    string FileStorageString = linkStorage;

    // Assert
    Assert.Equal(link, FileStorageString);
  }

  // Testa se os dados do arquivo do storage são válido para conversão implícita de uma string
  [Fact]
  public void FileStorage_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var link = "https://example.com";

    // Act
    FileStorage linkStorage = link;

    // Assert
    Assert.True(linkStorage.IsValid);
    Assert.Equal(link, linkStorage.Link);
    Assert.Equal(link, linkStorage.Name);
  }
}
