using Tooark.Entities;

namespace Tooark.Tests.Entities;

public class VersionedEntityTests
{
  // Uma classe concreta para testar VersionedEntity
  private class TestVersionedEntity : VersionedEntity
  {
    // Construtor padrão
    public TestVersionedEntity() { }

    // Construtor com parâmetros
    public TestVersionedEntity(Guid createdBy) : base(createdBy) { }
  }

  // Teste se o construtor atribui valores padrão
  [Fact]
  public void Constructor_ShouldAssignDefaultValues()
  {
    // Arrange
    var createdBy = Guid.Empty;
    var entity = new TestVersionedEntity();

    // Act
    var version = entity.Version;

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.Equal(1, version);
  }

  // Teste se o construtor atribui valores padrão com um criador
  [Fact]
  public void Constructor_ShouldAssignValues_WithCreatedBy()
  {
    // Arrange
    var createdBy = Guid.NewGuid();

    // Act
    var entity = new TestVersionedEntity(createdBy);
    var version = entity.Version;

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.Equal(1, version);
  }

  // Teste se a versão é incrementada ao chamar SetUpdatedBy
  [Fact]
  public void SetUpdatedBy_ShouldIncrementVersion_WhenCalled()
  {
    // Arrange
    var entity = new TestVersionedEntity();
    var initialVersion = entity.Version;
    var updatedBy = Guid.NewGuid();

    // Act
    entity.SetUpdatedBy(updatedBy);
    var updatedVersion = entity.Version;

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(initialVersion + 1, updatedVersion);
  }

  // Teste se a versão não é incrementada ao chamar SetUpdatedBy quando a entidade é inválida
  [Fact]
  public void SetUpdatedBy_ShouldNotIncrementVersion_WhenEntityIsInvalid()
  {
    // Arrange
    var entity = new TestVersionedEntity();
    var updatedBy = Guid.NewGuid();
    entity.SetUpdatedBy(updatedBy);
    var version = entity.Version;

    // Act
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetUpdatedBy(Guid.Empty));
    var updatedVersion = entity.Version;

    // Assert
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.Contains("Field.Invalid;UpdatedBy", ex.GetErrorMessages());
    Assert.Equal(version, updatedVersion);
  }

  // Teste se o construtor sem parâmetros inicializa a versão corretamente
  [Fact]
  public void Constructor_WithoutParameters_ShouldInitializeVersionCorrectly()
  {
    // Arrange & Act
    var entity = new TestVersionedEntity();

    // Assert
    Assert.Equal(1, entity.Version);
  }
}
