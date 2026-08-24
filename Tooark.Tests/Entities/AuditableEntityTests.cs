using Tooark.Entities;

namespace Tooark.Tests.Entities;

public class AuditableEntityTests
{
  // Uma classe concreta para testar AuditableEntity
  private class TestAuditableEntity : AuditableEntity
  {
    // Construtor padrão
    public TestAuditableEntity() { }

    // Construtor com parâmetros
    public TestAuditableEntity(Guid createdBy) : base(createdBy) { }
  }

  // Teste se o construtor atribui valores padrão
  [Fact]
  public void Constructor_ShouldAssignDefaultValues()
  {
    // Arrange
    var userId = Guid.Empty;

    // Act
    var entity = new TestAuditableEntity();

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(userId, entity.CreatedById);
    Assert.Equal(userId, entity.UpdatedById);
    Assert.Equal(1, entity.Version);
    Assert.False(entity.Deleted);
    Assert.Null(entity.DeletedById);
    Assert.Null(entity.DeletedAt);
  }

  // Teste se o construtor atribui valores padrão com um criador
  [Fact]
  public void Constructor_ShouldAssignValues_WithCreatedBy()
  {
    // Arrange
    var userId = Guid.NewGuid();

    // Act
    var entity = new TestAuditableEntity(userId);

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(userId, entity.CreatedById);
    Assert.Equal(userId, entity.UpdatedById);
    Assert.Equal(1, entity.Version);
    Assert.False(entity.Deleted);
    Assert.Null(entity.DeletedById);
    Assert.Null(entity.DeletedAt);
  }

  // Teste para marcar a entidade como excluída
  [Fact]
  public void SetDeleted_ShouldSetDeletedByIdAndDeletedAt()
  {
    // Arrange
    var entity = new TestAuditableEntity();
    var userId = Guid.NewGuid();

    // Act
    entity.SetDeleted(userId);

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(2, entity.Version);
    Assert.True(entity.Deleted);
    Assert.Equal(userId, entity.DeletedById);
    Assert.NotNull(entity.DeletedAt);
  }

  // Teste se SetDeleted não atribui DeletedById quando o usuário é vazio
  [Fact]
  public void SetDeleted_ShouldNotSetDeletedById_WhenUserIdIsEmpty()
  {
    // Arrange
    var entity = new TestAuditableEntity();
    var userId = Guid.Empty;

    // Act
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetDeleted(userId));

    // Assert
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.Equal(userId, entity.UpdatedById);
    Assert.Equal(1, entity.Version);
    Assert.False(entity.Deleted);
    Assert.Null(entity.DeletedById);
    Assert.Null(entity.DeletedAt);
    Assert.Contains("Field.Invalid;DeletedBy", ex.GetErrorMessages());
  }

  // Teste se SetRestored atribui valores corretos
  [Fact]
  public void SetRestored_ShouldSetRestoredByIdAndRestoredAt()
  {
    // Arrange
    var entity = new TestAuditableEntity();
    var deletedBy = Guid.NewGuid();
    entity.SetDeleted(deletedBy);
    var userId = Guid.NewGuid();

    // Act
    entity.SetRestored(userId);

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(3, entity.Version);
    Assert.False(entity.Deleted);
    Assert.Equal(userId, entity.RestoredById);
    Assert.NotNull(entity.RestoredAt);
  }

  // Teste se SetRestored não atribui RestoredById quando o usuário é vazio
  [Fact]
  public void SetRestored_ShouldNotSetRestoredById_WhenUserIdIsEmpty()
  {
    // Arrange
    var entity = new TestAuditableEntity();
    var deletedBy = Guid.NewGuid();
    entity.SetDeleted(deletedBy);
    var userId = Guid.Empty;

    // Act
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetRestored(userId));

    // Assert
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.Equal(2, entity.Version);
    Assert.True(entity.Deleted);
    Assert.Null(entity.RestoredById);
    Assert.Null(entity.RestoredAt);
    Assert.Contains("Field.Invalid;RestoredBy", ex.GetErrorMessages());
  }

  // Teste para verificar se ValidateNotDeleted adiciona notificação quando a entidade está excluída
  [Fact]
  public void ValidateNotDeleted_ShouldAddNotification_WhenEntityIsDeleted()
  {
    // Arrange
    var entity = new TestAuditableEntity();
    var userId = Guid.NewGuid();
    entity.SetDeleted(userId);

    // Act
    entity.ValidateNotDeleted();

    // Assert
    Assert.False(entity.IsValid);
    Assert.Contains(entity.Notifications, n => n.Key == "Entity");
  }

  // Teste para verificar se ValidateNotDeleted não adiciona notificação quando a entidade não está excluída
  [Fact]
  public void ValidateNotDeleted_ShouldNotAddNotification_WhenEntityIsNotDeleted()
  {
    // Arrange
    var entity = new TestAuditableEntity();

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
    var entity = new TestAuditableEntity();
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
    var entity = new TestAuditableEntity();

    // Act
    entity.EnsureNotDeleted();

    // Assert
    Assert.True(entity.IsValid);
    Assert.Empty(entity.Notifications);
  }

  // Teste para SetUpdatedBy incrementando a versão e atualizando o UpdatedById
  [Fact]
  public void SetUpdatedBy_ShouldIncrementVersionAndSetUpdatedBy()
  {
    // Arrange
    var entity = new TestAuditableEntity();
    var userId = Guid.NewGuid();

    // Act
    entity.SetUpdatedBy(userId);

    // Assert
    Assert.True(entity.IsValid);
    Assert.Equal(userId, entity.UpdatedById);
    Assert.Equal(2, entity.Version);
  }

  // Teste para SetUpdatedBy não alterar a entidade quando o identificador é inválido
  [Fact]
  public void SetUpdatedBy_ShouldNotUpdate_WhenUserIdIsInvalid()
  {
    // Arrange
    var entity = new TestAuditableEntity();
    var userId = Guid.Empty;

    // Act
    var ex = Assert.Throws<Tooark.Exceptions.BadRequestException>(() => entity.SetUpdatedBy(userId));

    // Assert
    // A chamada recusada não deixa notificação na entidade: ela segue utilizável
    Assert.True(entity.IsValid);
    Assert.Equal(Guid.Empty, entity.UpdatedById);
    Assert.Equal(1, entity.Version);
    Assert.Contains("Field.Invalid;UpdatedBy", ex.GetErrorMessages());
  }
}
