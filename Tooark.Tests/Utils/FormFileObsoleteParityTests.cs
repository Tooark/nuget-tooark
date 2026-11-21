using Microsoft.AspNetCore.Http;
using Moq;
using Tooark.AspNetCore.Files;
using Tooark.Utils;

#pragma warning disable CS0618 // Os métodos com IFormFile são obsoletos: os testes comparam o resultado deles com o dos substitutos

namespace Tooark.Tests.Utils;

// Só existe na 4.x: garante que os substitutos devolvem o mesmo que os métodos obsoletos, que saem na v5.
public class FormFileObsoleteParityTests
{
  // A cópia pelo método obsoleto, pela extensão do Tooark.AspNetCore e pelo Stream devolve os mesmos bytes.
  [Fact]
  public async Task Conversion_ShouldMatchTheObsoleteMethods()
  {
    // Arrange
    byte[] content = [1, 2, 3, 4, 5];
    var file = CreateFormFile(content, "relatorio.PDF");

    // Act
    using var legacy = FileConvert.ToMemoryStream(file);
    await using var legacyAsync = await FileConvert.ToMemoryStreamAsync(file, cancellationToken: TestContext.Current.CancellationToken);
    using var extension = file.ToMemoryStream();
    await using var extensionAsync = await file.ToMemoryStreamAsync(cancellationToken: TestContext.Current.CancellationToken);
    using var source = file.OpenReadStream();
    using var stream = FileConvert.CopyToMemoryStream(source);

    // Assert
    Assert.Equal(content, legacy!.ToArray());
    Assert.Equal(content, legacyAsync!.ToArray());
    Assert.Equal(content, extension!.ToArray());
    Assert.Equal(content, extensionAsync!.ToArray());
    Assert.Equal(content, stream!.ToArray());
  }

  // Um arquivo que declara tamanho e entrega um fluxo vazio resulta em um MemoryStream vazio pelos dois caminhos.
  [Fact]
  public void Conversion_ShouldMatchTheObsoleteMethod_WhenTheDeclaredLengthHasNoContent()
  {
    // Arrange
    var mock = new Mock<IFormFile>();
    mock.Setup(_ => _.OpenReadStream()).Returns(() => new MemoryStream());
    mock.Setup(_ => _.Length).Returns(32);

    // Act
    using var legacy = FileConvert.ToMemoryStream(mock.Object);
    using var extension = mock.Object.ToMemoryStream();

    // Assert
    Assert.NotNull(legacy);
    Assert.NotNull(extension);
    Assert.Equal(legacy.Length, extension.Length);
  }

  // A extensão do arquivo é a mesma pelos três caminhos.
  [Theory]
  [InlineData("relatorio.PDF")]
  [InlineData("foto.png")]
  [InlineData("sem-extensao")]
  [InlineData("..\\..\\web.config")]
  public void Extension_ShouldMatchTheObsoleteMethod(string fileName)
  {
    // Arrange
    var file = CreateFormFile([1], fileName);

    // Act
    var legacy = FileConvert.Extension(file);

    // Assert
    Assert.Equal(legacy, file.GetExtension());
    Assert.Equal(legacy, FileConvert.ExtensionFromFileName(fileName));
  }

  // A validação é a mesma pelos três caminhos, dentro e fora do limite de tamanho.
  [Theory]
  [InlineData("foto.png", 1024, 2048)]
  [InlineData("foto.png", 4096, 2048)]
  [InlineData("relatorio.pdf", 1024, 0)]
  [InlineData("video.mp4", 1024, 2048)]
  [InlineData("programa.exe", 1024, 2048)]
  [InlineData("vazio.pdf", 0, 2048)]
  public void Validation_ShouldMatchTheObsoleteMethods(string fileName, int length, long maxBytes)
  {
    // Arrange
    var file = CreateFormFile(new byte[length], fileName);
    string[] custom = ["pdf", ".png"];

    // Act & Assert
    Assert.Equal(FileValid.IsImage(file, maxBytes), file.IsImage(maxBytes));
    Assert.Equal(FileValid.IsImage(file, maxBytes), FileValid.IsImageFile(fileName, length, maxBytes));
    Assert.Equal(FileValid.IsDocument(file, maxBytes), file.IsDocument(maxBytes));
    Assert.Equal(FileValid.IsDocument(file, maxBytes), FileValid.IsDocumentFile(fileName, length, maxBytes));
    Assert.Equal(FileValid.IsVideo(file, maxBytes), file.IsVideo(maxBytes));
    Assert.Equal(FileValid.IsVideo(file, maxBytes), FileValid.IsVideoFile(fileName, length, maxBytes));
    Assert.Equal(FileValid.IsCustom(file, maxBytes, custom), file.IsCustom(maxBytes, custom));
    Assert.Equal(FileValid.IsCustom(file, maxBytes, custom), FileValid.IsCustomFile(fileName, length, maxBytes, custom));
  }

  // Monta um IFormFile com o conteúdo binário informado.
  private static IFormFile CreateFormFile(byte[] content, string fileName)
  {
    var mock = new Mock<IFormFile>();
    mock.Setup(_ => _.OpenReadStream()).Returns(() => new MemoryStream(content));
    mock.Setup(_ => _.FileName).Returns(fileName);
    mock.Setup(_ => _.Length).Returns(content.Length);

    return mock.Object;
  }
}
