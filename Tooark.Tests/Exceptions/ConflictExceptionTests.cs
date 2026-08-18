using System.Net;
using Tooark.Exceptions;
using Tooark.Notifications;

namespace Tooark.Tests.Exceptions;

public class ConflictExceptionTests
{
  // Classe de teste para simular uma notificação.
  public class TestException : Notification
  {
    // Expoe o mutador protegido para uso nos testes.
    public new void AddNotification(NotificationItem notification) => base.AddNotification(notification);
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma única mensagem.
  [Fact]
  public void ConflictException_ShouldReturnCorrectMessage_WithSingleMessage()
  {
    // Arrange
    var expectedMessage = "Conflict Error";

    // Act
    var exception = new ConflictException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.Conflict, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma lista de mensagens.
  [Fact]
  public void ConflictException_ShouldReturnCorrectMessage_WithListMessages()
  {
    // Arrange
    string[] expectedMessage = ["Conflict Error", "Another Conflict Error"];

    // Act
    var exception = new ConflictException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage[0], exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetNotifications().Select(n => n.Message));
    Assert.Equal(HttpStatusCode.Conflict, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de notificação.
  [Fact]
  public void ConflictException_ShouldReturnCorrectMessage_WithNotification()
  {
    // Arrange
    string expectedMessage = "Conflict Error";
    TestException testException = new();
    testException.AddNotification(expectedMessage);

    // Act
    var exception = new ConflictException(testException);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.Conflict, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com um parâmetro.
  [Fact]
  public void ConflictException_ShouldReturnCorrectMessage_WithFormattedString_SingleParameter()
  {
    // Arrange
    var format = "Conflito no recurso: {0}";
    var expectedMessage = "Conflito no recurso: Usuário";

    // Act
    var exception = new ConflictException(format, "Usuário");

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.Conflict, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com múltiplos parâmetros.
  [Fact]
  public void ConflictException_ShouldReturnCorrectMessage_WithFormattedString_MultipleParameters()
  {
    // Arrange
    var format = "Conflito no recurso {0} para o ID {1}";
    var expectedMessage = "Conflito no recurso Usuário para o ID 10";

    // Act
    var exception = new ConflictException(format, "Usuário", 10);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.Conflict, exception.GetStatusCode());
  }

  // Teste para garantir que a excecao interna e preservada.
  [Fact]
  public void ConflictException_ShouldPreserveInnerException()
  {
    // Arrange
    var causa = new InvalidOperationException("Causa raiz");
    var message = "Falha ao processar";

    // Act
    var exception = new ConflictException(message, causa);

    // Assert
    Assert.Same(causa, exception.InnerException);
    Assert.Equal(message, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(message, exception.GetErrorMessages()[0]);
    Assert.Equal(HttpStatusCode.Conflict, exception.GetStatusCode());
  }
}
