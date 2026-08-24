using Tooark.Entities;

namespace Tooark.Tests.Entities;

public class SoftDeletableEntityTests
{
  // Uma classe concreta para testar SoftDeletableEntity
  private class TestSoftDeletableEntity : SoftDeletableEntity
  {
    // Construtor padrão
    public TestSoftDeletableEntity() { }

    // Construtor com parâmetros
    public TestSoftDeletableEntity(Guid createdBy) : base(createdBy) { }
  }

  // Testa se o construtor atribui valores padrão
  [Fact]
  public void Constructor_ShouldAssignDefaultValues()
  {
    // Arrange
    var createdBy = Guid.Empty;

    // Act
    var entity = new TestSoftDeletableEntity();

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.False(entity.Deleted);
  }

  // Teste se o construtor atribui valores padrão com um criador
  [Fact]
  public void Constructor_ShouldAssignValues_WithCreatedBy()
  {
    // Arrange
    var createdBy = Guid.NewGuid();

    // Act
    var entity = new TestSoftDeletableEntity(createdBy);

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(createdBy, entity.CreatedById);
    Assert.False(entity.Deleted);
  }

  // Teste para marcar a entidade como excluída
  [Fact]
  public void SetDeleted_ShouldMarkEntityAsDeleted()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var entity = new TestSoftDeletableEntity(userId);

    // Act
    entity.SetDeleted(userId);

    // Assert
    Assert.True(entity.IsValid);
    Assert.True(entity.Deleted);
    Assert.Equal(userId, entity.UpdatedById);
  }

  // Teste para não marcar a entidade como excluída quando o usuário é vazio
  [Fact]
  public void SetDeleted_ShouldNotMarkEntityAsDeleted_WhenUserIdIsEmpty()
  {
    // Arrange
    var entity = new TestSoftDeletableEntity();
    var userId = Guid.Empty;

    // Act & Assert
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetDeleted(userId));
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.False(entity.Deleted);
    Assert.Contains("Field.Invalid;UpdatedBy", ex.GetErrorMessages());
  }

  // Teste para marcar a entidade como restaurada
  [Fact]
  public void SetRestored_ShouldMarkEntityAsNotDeleted()
  {
    // Arrange
    var entity = new TestSoftDeletableEntity();
    var userId = Guid.NewGuid();
    entity.SetDeleted(userId);

    // Act
    entity.SetRestored(userId);

    // Assert
    Assert.True(entity.IsValid);
    Assert.False(entity.Deleted);
  }

  // Teste para não marcar a entidade como restaurada quando o usuário é vazio
  [Fact]
  public void SetRestored_ShouldNotMarkEntityAsNotDeleted_WhenUserIdIsEmpty()
  {
    // Arrange
    var entity = new TestSoftDeletableEntity();
    var userId = Guid.NewGuid();
    entity.SetDeleted(userId);

    // Act & Assert
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetRestored(Guid.Empty));
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.True(entity.Deleted);
    Assert.Contains("Field.Invalid;UpdatedBy", ex.GetErrorMessages());
  }

  // Teste para verificar se a alteração não é permitida quando a entidade está excluída
  [Fact]
  public void ValidateNotDeleted_ShouldAddNotification_WhenEntityIsDeleted()
  {
    // Arrange
    var entity = new TestSoftDeletableEntity();
    var userId = Guid.NewGuid();
    entity.SetDeleted(userId);

    // Act
    entity.ValidateNotDeleted();

    // Assert
    Assert.False(entity.IsValid);
    Assert.Equal("Entity", entity.Notifications.First().Key);
  }

  // Teste para verificar se ValidateNotDeleted não adiciona notificação quando a entidade não está excluída
  [Fact]
  public void ValidateNotDeleted_ShouldNotAddNotification_WhenEntityIsNotDeleted()
  {
    // Arrange
    var entity = new TestSoftDeletableEntity();

    // Act
    entity.ValidateNotDeleted();

    // Assert
    Assert.True(entity.IsValid);
    Assert.Empty(entity.Notifications);
  }

  // Teste para verificar se EnsureNotDeleted lança exceção quando a entidade está excluída
  [Fact]
  public void EnsureNotDeleted_ShouldThrowException_WhenEntityIsDeleted()
  {
    // Arrange
    var entity = new TestSoftDeletableEntity();
    var userId = Guid.NewGuid();
    entity.SetDeleted(userId);

    // Act & Assert
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.EnsureNotDeleted());

    // Assert
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.Contains("Record.Deleted", ex.GetErrorMessages());
  }

  // Teste para verificar se EnsureNotDeleted não lança exceção quando a entidade não está excluída
  [Fact]
  public void EnsureNotDeleted_ShouldNotThrowException_WhenEntityIsNotDeleted()
  {
    // Arrange
    var entity = new TestSoftDeletableEntity();

    // Act
    entity.EnsureNotDeleted();

    // Assert
    Assert.True(entity.IsValid);
    Assert.Empty(entity.Notifications);
  }
}
