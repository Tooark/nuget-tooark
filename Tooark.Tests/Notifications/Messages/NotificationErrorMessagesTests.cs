using Tooark.Notifications.Messages;

namespace Tooark.Tests.Notifications.Messages;

public class NotificationErrorMessagesTests
{
  // Teste para as mensagens de erro quando a mensagem é nula ou vazia
  [Fact]
  public void MessageIsNullOrEmpty_ShouldReturnCorrectMessage()
  {
    // Arrange
    var expectedMessage = "Notifications.MessageNullEmpty";

    // Act
    var actualMessage = NotificationErrorMessages.MessageIsNullOrEmpty;

    // Assert
    Assert.Equal(expectedMessage, actualMessage);
  }

  // Teste para as mensagens de erro quando a notificação é nula
  [Fact]
  public void NotificationIsNull_ShouldReturnCorrectMessage()
  {
    // Arrange
    var expectedMessage = "Notifications.NotificationNull";

    // Act
    var actualMessage = NotificationErrorMessages.NotificationIsNull;

    // Assert
    Assert.Equal(expectedMessage, actualMessage);
  }
}
