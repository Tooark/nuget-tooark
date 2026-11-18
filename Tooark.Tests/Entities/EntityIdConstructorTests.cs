using Tooark.Entities;
using Tooark.Enums;
using Tooark.Exceptions;
using Tooark.ValueObjects;

namespace Tooark.Tests.Entities;

/// <summary>
/// Testes dos construtores que recebem o identificador nas entidades com auditoria.
/// </summary>
public class EntityIdConstructorTests
{
  #region Entidades de teste

  private sealed class Inicial : InitialEntity
  {
    public Inicial(Guid id, Guid createdBy) : base(id, createdBy) { }
  }

  private sealed class Detalhada : DetailedEntity
  {
    public Detalhada(Guid id, Guid createdBy) : base(id, createdBy) { }
  }

  private sealed class Versionada : VersionedEntity
  {
    public Versionada(Guid id, Guid createdBy) : base(id, createdBy) { }
  }

  private sealed class Excluivel : SoftDeletableEntity
  {
    public Excluivel(Guid id, Guid createdBy) : base(id, createdBy) { }
  }

  private sealed class Auditavel : AuditableEntity
  {
    public Auditavel(Guid id, Guid createdBy) : base(id, createdBy) { }
  }

  private sealed class Arquivo : FileEntity
  {
    public Arquivo(Guid id, FileStorage file, string title, Guid createdBy) : base(id, file, title, createdBy) { }

    public Arquivo(Guid id, FileStorage file, string title, string fileFormat, EFileType type, long size, Guid createdBy)
      : base(id, file, title, fileFormat, type, size, createdBy) { }
  }

  // O caso que motivou os construtores: sem eles, a derivada só podia chamar o SetId depois do construtor
  // sem parâmetros da base, que já gerou um identificador
  private sealed class DetalhadaComSetId : DetailedEntity
  {
    public DetalhadaComSetId(Guid id, Guid createdBy) : base(createdBy)
    {
      SetId(id);
    }
  }

  // A derivada que mantém o construtor de um argumento com Guid continua repassando o criador
  private sealed class InicialSoComCriador : InitialEntity
  {
    public InicialSoComCriador(Guid createdBy) : base(createdBy) { }
  }

  #endregion

  // Testa se cada entidade com auditoria guarda o identificador e o criador informados
  [Fact]
  public void Constructor_WithId_ShouldAssignIdAndCreator()
  {
    // Arrange
    var id = Guid.NewGuid();
    var createdBy = Guid.NewGuid();

    // Act
    InitialEntity[] entities =
    [
      new Inicial(id, createdBy),
      new Detalhada(id, createdBy),
      new Versionada(id, createdBy),
      new Excluivel(id, createdBy),
      new Auditavel(id, createdBy)
    ];

    // Assert
    Assert.All(entities, entity =>
    {
      Assert.Equal(id, entity.Id);
      Assert.Equal(createdBy, entity.CreatedById);
      Assert.True(entity.IsValid);
    });
  }

  // Testa se a criação com identificador também nasce como a primeira atualização, uma vez só
  [Fact]
  public void Constructor_WithId_ShouldMarkCreationAsFirstUpdateWithoutIncrementingVersion()
  {
    // Arrange
    var id = Guid.NewGuid();
    var createdBy = Guid.NewGuid();

    // Act
    var detalhada = new Detalhada(id, createdBy);
    var versionada = new Versionada(id, createdBy);
    var auditavel = new Auditavel(id, createdBy);

    // Assert
    Assert.All(new DetailedEntity[] { detalhada, versionada, auditavel }, entity =>
    {
      Assert.Equal(createdBy, entity.UpdatedById);
      Assert.Equal(entity.CreatedAt, entity.UpdatedAt);
    });
    Assert.Equal(1, versionada.Version);
    Assert.Equal(1, auditavel.Version);
    Assert.False(auditavel.Deleted);
  }

  // Testa se os construtores de arquivo com identificador guardam o identificador e os dados do arquivo
  [Fact]
  public void FileEntity_ConstructorsWithId_ShouldAssignIdAndFile()
  {
    // Arrange
    var id = Guid.NewGuid();
    var createdBy = Guid.NewGuid();
    var file = new FileStorage("https://example.com/path/file.txt", "path/file.txt");

    // Act
    var simples = new Arquivo(id, file, "Contrato", createdBy);
    var completo = new Arquivo(id, file, "Contrato", "txt", EFileType.Unknown, 10, createdBy);

    // Assert
    Assert.Equal(id, simples.Id);
    Assert.Equal(createdBy, simples.CreatedById);
    Assert.Equal(file.Name, simples.FileName);
    Assert.Equal("Contrato", simples.Title);
    Assert.Equal(id, completo.Id);
    Assert.Equal("txt", completo.FileFormat);
    Assert.Equal(10, completo.Size);
  }

  // Testa se o identificador vazio é recusado com o código do identificador vazio
  [Fact]
  public void Constructor_WithEmptyId_ShouldThrowBadRequest()
  {
    // Act
    var ex = Assert.Throws<BadRequestException>(() => new Auditavel(Guid.Empty, Guid.NewGuid()));

    // Assert
    Assert.Equal("T.ENT.BAS1", Assert.Single(ex.GetNotifications()).Code);
  }

  // Testa se o criador vazio continua recusado no construtor com identificador
  [Fact]
  public void Constructor_WithIdAndEmptyCreator_ShouldThrowBadRequest()
  {
    // Act
    var ex = Assert.Throws<BadRequestException>(() => new Detalhada(Guid.NewGuid(), Guid.Empty));

    // Assert
    Assert.Contains("Field.Invalid;CreatedBy", ex.GetErrorMessages());
  }

  // Testa se o SetId depois do construtor sem identificador da base lança, em vez de manter o id gerado em silêncio
  [Fact]
  public void SetId_AfterBaseGeneratedId_ShouldThrowChangeBlocked()
  {
    // Act
    var ex = Assert.Throws<BadRequestException>(() => new DetalhadaComSetId(Guid.NewGuid(), Guid.NewGuid()));

    // Assert
    var notification = Assert.Single(ex.GetNotifications());
    Assert.Equal("T.ENT.BAS2", notification.Code);
    Assert.Equal("Field.ChangeBlocked;Id", notification.Message);
  }

  // Testa se o construtor de um argumento com Guid continua sendo o do criador, e não o do identificador
  [Fact]
  public void Constructor_WithSingleGuid_ShouldStillBindToCreator()
  {
    // Arrange
    var createdBy = Guid.NewGuid();

    // Act
    var entity = new InicialSoComCriador(createdBy);

    // Assert
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.NotEqual(createdBy, entity.Id);
    Assert.NotEqual(Guid.Empty, entity.Id);
  }
}
