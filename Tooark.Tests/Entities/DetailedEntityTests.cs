using Tooark.Entities;

namespace Tooark.Tests.Entities;

public class DetailedEntityTests
{
  // Uma classe concreta para testar DetailedEntity
  private class TestDetailedEntity : DetailedEntity
  {
    // Construtor padrão
    public TestDetailedEntity() { }

    // Construtor com parâmetros
    public TestDetailedEntity(Guid createdBy) : base(createdBy) { }
  }

  // Teste se o construtor atribui valores padrão
  [Fact]
  public void Constructor_ShouldAssignDefaultValues()
  {
    // Arrange
    var createdBy = Guid.Empty;

    // Act
    var entity = new TestDetailedEntity();

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.Equal(createdBy, entity.UpdatedById);
    Assert.True((DateTime.UtcNow - entity.UpdatedAt).TotalMinutes < 1);
  }

  // Teste se o construtor atribui valores padrão com um criador
  [Fact]
  public void Constructor_ShouldAssignValues_WithCreatedBy()
  {
    // Arrange
    var createdBy = Guid.NewGuid();

    // Act
    var entity = new TestDetailedEntity(createdBy);

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.Equal(createdBy, entity.UpdatedById);
    Assert.True((DateTime.UtcNow - entity.CreatedAt).TotalMinutes < 1);
  }

  // Testa se SetCreatedBy atribui um Guid válido
  [Fact]
  public void SetCreatedBy_WithValidGuid_ShouldAssignGuid()
  {
    // Arrange
    var entity = new TestDetailedEntity();
    var createdBy = Guid.NewGuid();

    // Act
    entity.SetCreatedBy(createdBy);

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.Equal(createdBy, entity.UpdatedById);
    Assert.True((DateTime.UtcNow - entity.CreatedAt).TotalMinutes < 1);
  }

  // Testa se SetUpdatedBy atualiza o atualizador e a data de atualização
  [Fact]
  public void SetUpdatedBy_WithValidGuid_ShouldUpdateUpdatedByAndUpdatedAt()
  {
    // Arrange
    var entity = new TestDetailedEntity();
    var updatedBy = Guid.NewGuid();

    // Act
    entity.SetUpdatedBy(updatedBy);

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(updatedBy, entity.UpdatedById);
    Assert.True((DateTime.UtcNow - entity.UpdatedAt).TotalMinutes < 1);
  }

  // Testa se SetCreatedBy gera uma notificação ao tentar atribuir um Guid vazio
  [Fact]
  public void SetCreatedBy_WithGuidEmpty_ShouldGenerateNotification()
  {
    // Arrange
    var entity = new TestDetailedEntity();
    // Act & Assert
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetCreatedBy(Guid.Empty));
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.Equal(Guid.Empty, entity.CreatedById);
    Assert.Equal(Guid.Empty, entity.UpdatedById);
    Assert.Contains("Field.Invalid;CreatedBy", ex.GetErrorMessages());
  }

  // Testa se SetCreatedBy gera uma notificação ao tentar alterar o criador
  [Fact]
  public void SetCreatedBy_WithNonEmptyGuid_ShouldGenerateNotification()
  {
    // Arrange
    var entity = new TestDetailedEntity();
    var createdBy = Guid.NewGuid();
    entity.SetCreatedBy(createdBy);

    // Act
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetCreatedBy(Guid.NewGuid()));

    // Assert
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.Equal(createdBy, entity.UpdatedById);
    Assert.Contains("Field.ChangeBlocked;CreatedBy", ex.GetErrorMessages());
  }

  // Testa se SetUpdatedBy gera uma notificação ao tentar atribuir um Guid vazio
  [Fact]
  public void SetUpdatedBy_WithGuidEmpty_ShouldGenerateNotification()
  {
    // Arrange
    var entity = new TestDetailedEntity();
    var createdBy = Guid.NewGuid();
    entity.SetCreatedBy(createdBy);

    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetUpdatedBy(Guid.Empty));

    // Assert
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.Equal(createdBy, entity.UpdatedById);
    Assert.Contains("Field.Invalid;UpdatedBy", ex.GetErrorMessages());
  }

  // Testa se SetUpdatedBy atualiza corretamente quando chamado várias vezes
  [Fact]
  public void SetUpdatedBy_MultipleCalls_ShouldUpdateCorrectly()
  {
    // Arrange
    var entity = new TestDetailedEntity();
    var firstUpdatedBy = Guid.NewGuid();
    var secondUpdatedBy = Guid.NewGuid();

    // Act
    entity.SetUpdatedBy(firstUpdatedBy);
    entity.SetUpdatedBy(secondUpdatedBy);

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(secondUpdatedBy, entity.UpdatedById);
    Assert.True((DateTime.UtcNow - entity.UpdatedAt).TotalMinutes < 1);
  }

  // Testa se SetCreatedBy não altera UpdatedById quando há erro de validação
  [Fact]
  public void SetCreatedBy_WithInvalidGuid_ShouldNotChangeUpdatedBy()
  {
    // Arrange
    var entity = new TestDetailedEntity();
    var validCreatedBy = Guid.NewGuid();
    entity.SetCreatedBy(validCreatedBy);

    // Act
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetCreatedBy(Guid.Empty));

    // Assert
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.Equal(validCreatedBy, entity.CreatedById);
    Assert.Equal(validCreatedBy, entity.UpdatedById);
    Assert.Contains("Field.ChangeBlocked;CreatedBy", ex.GetErrorMessages());
  }
}
