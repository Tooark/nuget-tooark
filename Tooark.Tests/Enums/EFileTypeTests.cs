using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.Tests.Enums;

public class EFileTypeTests
{
  // Teste para o tipo de arquivo Unknown
  [Fact]
  public void TestUnknownFileType()
  {
    // Arrange
    var fileType = EFileType.Unknown;

    // Act & Assert
    Assert.Equal(0, fileType.ToInt());
    Assert.Equal("Unknown", fileType.ToString());
  }

  // Teste para o tipo de arquivo Document
  [Fact]
  public void TestDocumentFileType()
  {
    // Arrange
    var fileType = EFileType.Document;

    // Act & Assert
    Assert.Equal(1, fileType.ToInt());
    Assert.Equal("Document", fileType.ToString());
  }

  // Teste para o tipo de arquivo Image
  [Fact]
  public void TestImageFileType()
  {
    // Arrange
    var fileType = EFileType.Image;

    // Act & Assert
    Assert.Equal(2, fileType.ToInt());
    Assert.Equal("Image", fileType.ToString());
  }

  // Teste para o tipo de arquivo Video
  [Fact]
  public void TestVideoFileType()
  {
    // Arrange
    var fileType = EFileType.Video;

    // Act & Assert
    Assert.Equal(3, fileType.ToInt());
    Assert.Equal("Video", fileType.ToString());
  }

  // Teste para o tipo de arquivo Audio
  [Fact]
  public void TestAudioFileType()
  {
    // Arrange
    var fileType = EFileType.Audio;

    // Act & Assert
    Assert.Equal(4, fileType.ToInt());
    Assert.Equal("Audio", fileType.ToString());
  }

  // Teste para conversão implícita de string para tipo de arquivo
  [Fact]
  public void TestImplicitStringToEFileType()
  {
    // Arrange
    EFileType document = "Document";
    EFileType image = "Image";
    EFileType video = "Video";
    EFileType audio = "Audio";
    EFileType unknown = "Unknown";

    // Act & Assert
    Assert.Equal(EFileType.Document, document);
    Assert.Equal(EFileType.Image, image);
    Assert.Equal(EFileType.Video, video);
    Assert.Equal(EFileType.Audio, audio);
    Assert.Equal(EFileType.Unknown, unknown);
  }

  // Teste para conversão implícita de int para tipo de arquivo
  [Fact]
  public void TestImplicitIntToEFileType()
  {
    // Arrange
    EFileType document = 1;
    EFileType image = 2;
    EFileType video = 3;
    EFileType audio = 4;
    EFileType unknown = 0;

    // Act & Assert
    Assert.Equal(EFileType.Document, document);
    Assert.Equal(EFileType.Image, image);
    Assert.Equal(EFileType.Video, video);
    Assert.Equal(EFileType.Audio, audio);
    Assert.Equal(EFileType.Unknown, unknown);
  }

  // Teste para conversão implícita de tipo de arquivo para string
  [Fact]
  public void TestImplicitStringFromEFileType()
  {
    // Arrange
    string document = EFileType.Document;
    string image = EFileType.Image;
    string video = EFileType.Video;
    string audio = EFileType.Audio;
    string unknown = EFileType.Unknown;

    // Act & Assert
    Assert.Equal("Document", document);
    Assert.Equal("Image", image);
    Assert.Equal("Video", video);
    Assert.Equal("Audio", audio);
    Assert.Equal("Unknown", unknown);
  }

  // Teste para conversão implícita de tipo de arquivo para int
  [Fact]
  public void TestImplicitIntFromEFileType()
  {
    // Arrange
    int document = EFileType.Document;
    int image = EFileType.Image;
    int video = EFileType.Video;
    int audio = EFileType.Audio;
    int unknown = EFileType.Unknown;

    // Act & Assert
    Assert.Equal(1, document);
    Assert.Equal(2, image);
    Assert.Equal(3, video);
    Assert.Equal(4, audio);
    Assert.Equal(0, unknown);
  }

  // Teste para e uma descrição inválida para tipo de arquivo
  [Fact]
  public void TestInvalidDescription()
  {
    // Act
    var fileType = (EFileType)"InvalidDescription";

    // Assert
    Assert.Equal(EFileType.Unknown, fileType);
  }

  // Teste para e um ID inválido para tipo de arquivo
  [Fact]
  public void TestInvalidId()
  {
    // Act
    var fileType = (EFileType)999;

    // Assert
    Assert.Equal(EFileType.Unknown, fileType);
  }

  // Teste para garantir que a descricao nao diferencia caixa nem espacos.
  [Theory]
  [InlineData("image")]
  [InlineData("IMAGE")]
  [InlineData("Image")]
  [InlineData("  image  ")]
  public void EFileType_ShouldBeImage_WhenDescriptionVariesInCase(string description)
  {
    // Arrange & Act
    EFileType fileType = description;

    // Assert
    Assert.Equal(EFileType.Image, fileType);
  }

  // Teste para garantir que a conversao de instancia nula falha com erro claro.
  [Fact]
  public void EFileType_ShouldThrow_WhenInstanceIsNull()
  {
    // Arrange
    EFileType fileType = null!;

    // Act & Assert
    var toInt = Assert.Throws<InternalServerErrorException>(() => (int)fileType);
    var toString = Assert.Throws<InternalServerErrorException>(() => (string)fileType);

    Assert.Contains("Invalid.Parameter;null", toInt.GetErrorMessages());
    Assert.Contains("Invalid.Parameter;null", toString.GetErrorMessages());
  }
}
