using Tooark.Entities;
using Tooark.Enums;
using Tooark.ValueObjects;

namespace Tooark.Tests.Entities;

public class FileEntityTests
{
  // Uma classe concreta para testar FileEntity
  private class TestFileEntity : FileEntity
  {
    // Construtor padrão
    public TestFileEntity() { }

    // Construtor com parâmetros base
    public TestFileEntity(FileStorage file, string title, Guid createdBy) :
    base(file, title, createdBy)
    { }

    // Construtor com parâmetros, URL pública, formato e tipo
    public TestFileEntity(FileStorage file, string title, string fileFormat, EFileType type, long size, Guid createdBy) :
    base(file, title, fileFormat, type, size, createdBy)
    { }
  }

  // Teste se o construtor atribui valores padrão às propriedades
  [Fact]
  public void Constructor_ShouldAssignDefaultValues()
  {
    // Arrange & Act
    var fileEntity = new TestFileEntity();

    // Assert
    Assert.True(fileEntity.IsValid);
    Assert.Equal(string.Empty, fileEntity.FileName);
    Assert.Equal(string.Empty, fileEntity.Title);
    Assert.Equal(string.Empty, fileEntity.Link);
    Assert.Equal(Guid.Empty, fileEntity.CreatedById);
  }

  // Teste se o construtor atribui valores padrão às propriedades com parâmetros base
  [Fact]
  public void Constructor_ShouldAssignValues_WhenValidParameters()
  {
    // Arrange
    var file = "https://example.com/file";
    var title = "Test File";
    var createdBy = Guid.NewGuid();

    // Act
    var fileEntity = new TestFileEntity(file, title, createdBy);

    // Assert
    Assert.True(fileEntity.IsValid);
    Assert.Equal(file, fileEntity.FileName);
    Assert.Equal(title, fileEntity.Title);
    Assert.Equal(file, fileEntity.Link);
    Assert.Equal(createdBy, fileEntity.CreatedById);
  }

  // Teste se o construtor atribui valores padrão às propriedades com parâmetros e URL pública
  [Fact]
  public void Constructor_ShouldAssignValues_WhenValidParametersWithPublicUrl()
  {
    // Arrange
    var file = new FileStorage("https://example.com/path/file.txt", "path/file.txt");
    var title = "Test File";
    var createdBy = Guid.NewGuid();

    // Act
    var fileEntity = new TestFileEntity(file, title, createdBy);

    // Assert
    Assert.True(fileEntity.IsValid);
    Assert.Equal(file.Name, fileEntity.FileName);
    Assert.Equal(title, fileEntity.Title);
    Assert.Equal(file.Link, fileEntity.Link);
    Assert.Equal(createdBy, fileEntity.CreatedById);
  }

  // Teste se o construtor atribui valores padrão às propriedades com parâmetros, URL pública, formato e tipo
  [Fact]
  public void Constructor_ShouldAssignValues_WhenValidParametersWithExtensionAndType()
  {
    // Arrange
    var file = new FileStorage("https://example.com/path/file.txt", "path/file.txt");
    var title = "Test File";
    var fileFormat = ".txt";
    var type = EFileType.Unknown;
    var size = 1024;
    var createdBy = Guid.NewGuid();

    // Act
    var fileEntity = new TestFileEntity(file, title, fileFormat, type, size, createdBy);

    // Assert
    Assert.True(fileEntity.IsValid);
    Assert.Equal(file.Name, fileEntity.FileName);
    Assert.Equal(title, fileEntity.Title);
    Assert.Equal(file.Link, fileEntity.Link);
    Assert.Equal(fileFormat, fileEntity.FileFormat);
    Assert.Equal(type, fileEntity.Type);
    Assert.Equal(size, fileEntity.Size);
    Assert.Equal(createdBy, fileEntity.CreatedById);
  }

  // Teste se o construtor atribui valores padrão às propriedades com parâmetros, URL pública, formato e tipo
  [Fact]
  public void Constructor_ShouldGenerateNotification_WhenInvalidFileUrl()
  {
    // Arrange
    var file = "invalid-url";
    var title = "Test File";
    var createdBy = Guid.NewGuid();

    // Act & Assert
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => new TestFileEntity(file, title, createdBy));
    Assert.Contains("Field.Invalid;ProtocolHttp", ex.GetErrorMessages());
  }

  // Teste se o construtor atribui valores padrão quando o nome é inválido
  [Fact]
  public void Constructor_ShouldGenerateNotification_WhenNameIsEmpty()
  {
    // Arrange
    var file = "https://example.com/file";
    var title = "";
    var createdBy = Guid.NewGuid();

    // Act & Assert
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => new TestFileEntity(file, title, createdBy));
    Assert.Contains("Field.Invalid;Title", ex.GetErrorMessages());

  }

  // Teste se o construtor atribui valores padrão quando o formato do arquivo é inválido
  [Fact]
  public void Constructor_ShouldGenerateNotification_WhenFileFormatIsInvalid()
  {
    // Arrange
    var file = new FileStorage("https://example.com/path/file.txt", "path/file.txt");
    var title = "Test File";
    var fileFormat = "";
    var type = EFileType.Unknown;
    var size = 1024;
    var createdBy = Guid.NewGuid();

    // Act & Assert
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => new TestFileEntity(file, title, fileFormat, type, size, createdBy));
    Assert.Contains("Field.Required;FileFormat", ex.GetErrorMessages());
  }

  // Teste se o construtor gera notificação quando o tamanho do arquivo é inválido
  [Fact]
  public void Constructor_ShouldGenerateNotification_WhenFileSizeIsInvalid()
  {
    // Arrange
    var file = new FileStorage("https://example.com/path/file.txt", "path/file.txt");
    var title = "Test File";
    var fileFormat = ".txt";
    var type = EFileType.Unknown;
    var size = -1; // Tamanho inválido
    var createdBy = Guid.NewGuid();

    // Act & Assert
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => new TestFileEntity(file, title, fileFormat, type, size, createdBy));
    Assert.Contains("Field.Invalid;Size", ex.GetErrorMessages());
  }
}
