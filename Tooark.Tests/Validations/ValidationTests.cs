using Tooark.Validations;

namespace Tooark.Tests.Validations;

public class ValidationTests
{
  // Teste de Join com notificações nulas
  [Fact]
  public void Join_WithNullNotifications_ReturnsSameInstance()
  {
    // Arrange
    var validation = new Validation();

    // Act
    validation.Join(null!);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste de Join com notificações válidas
  [Fact]
  public void Join_WithValidNotifications_DoesNotAddNotifications()
  {
    // Arrange
    var testValidation = new Validation();
    testValidation.IsTrue(true, "TestProperty");

    var validation = new Validation();

    // Act
    validation.Join(testValidation);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste de Join com notificações inválidas
  [Fact]
  public void Join_WithInvalidNotifications_AddsNotifications()
  {
    // Arrange
    var testValidation = new Validation();
    testValidation.IsTrue(false, "TestProperty");

    var validation = new Validation();

    // Act
    validation.Join(testValidation);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Same(testValidation.Notifications.First(), validation.Notifications.First());
  }

  // Teste de Join com notificações que já existem
  [Fact]
  public void Join_WithExitsNotifications_AddsNotifications()
  {
    // Arrange
    var testValidation = new Validation();
    testValidation.IsTrue(false, "TestProperty");

    var validation = new Validation();
    validation.IsTrue(false, "TestProperty");

    // Act
    validation.Join(testValidation);

    // Assert
    Assert.Equal(2, validation.Notifications.Count);
    Assert.Contains(testValidation.Notifications.First(), validation.Notifications);
  }

  // Teste de Join com notificações válidas e inválidas
  [Fact]
  public void Join_WithMixedNotifications_AddsOnlyInvalidNotifications()
  {
    // Arrange
    var validNotification = new Validation();
    validNotification.IsTrue(true, "TestProperty");

    var invalidNotification = new Validation();
    invalidNotification.IsTrue(false, "TestProperty");

    var validation = new Validation();

    // Act
    validation.Join(validNotification, invalidNotification);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Same(invalidNotification.Notifications.First(), validation.Notifications.First());
  }

  // Teste de Join com múltiplas notificações inválidas
  [Fact]
  public void Join_WithMultipleInvalidNotifications_AddsAllInvalidNotifications()
  {
    // Arrange
    var invalidNotification1 = new Validation();
    invalidNotification1.IsTrue(false, "TestProperty");

    var invalidNotification2 = new Validation();
    invalidNotification2.IsTrue(false, "TestProperty");

    var validation = new Validation();

    // Act
    validation.Join(invalidNotification1, invalidNotification2);

    // Assert
    Assert.Equal(2, validation.Notifications.Count);
    Assert.Contains(invalidNotification1.Notifications.First(), validation.Notifications);
    Assert.Contains(invalidNotification2.Notifications.First(), validation.Notifications);
  }

  // Teste de Join sem notificações
  [Fact]
  public void Join_WithNoNotifications_DoesNotAddAnyNotifications()
  {
    // Arrange
    var validation = new Validation();

    // Act
    validation.Join();

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste de Join com todas as notificações válidas
  [Fact]
  public void Join_WithAllValidNotifications_DoesNotAddAnyNotifications()
  {
    // Arrange
    var validNotification1 = new Validation();
    validNotification1.IsTrue(true, "TestProperty");

    var validNotification2 = new Validation();
    validNotification2.IsTrue(true, "TestProperty");

    var validation = new Validation();

    // Act
    validation.Join(validNotification1, validNotification2);

    // Assert
    Assert.Empty(validation.Notifications);
  }
}
