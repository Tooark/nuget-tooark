using System.Net;
using Tooark.Exceptions;
using Tooark.Notifications;

namespace Tooark.Tests.Exceptions;

public class PayloadTooLargeExceptionTests
{
  // Classe de teste para simular uma notificação.
  public class TestException : Notification
  {
    // Expoe o mutador protegido para uso nos testes.
    public new void AddNotification(NotificationItem notification) => base.AddNotification(notification);
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma única mensagem.
  [Fact]
  public void PayloadTooLargeException_ShouldReturnCorrectMessage_WithSingleMessage()
  {
    // Arrange
    var expectedMessage = "Payload Too Large Error";

    // Act
    var exception = new PayloadTooLargeException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma lista de mensagens.
  [Fact]
  public void PayloadTooLargeException_ShouldReturnCorrectMessage_WithListMessages()
  {
    // Arrange
    string[] expectedMessage = ["Payload Too Large Error", "Another Payload Too Large Error"];

    // Act
    var exception = new PayloadTooLargeException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage[0], exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetNotifications().Select(n => n.Message));
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de notificação.
  [Fact]
  public void PayloadTooLargeException_ShouldReturnCorrectMessage_WithNotification()
  {
    // Arrange
    string expectedMessage = "Payload Too Large Error";
    TestException testException = new();
    testException.AddNotification(expectedMessage);

    // Act
    var exception = new PayloadTooLargeException(testException);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com um parâmetro.
  [Fact]
  public void PayloadTooLargeException_ShouldReturnCorrectMessage_WithFormattedString_SingleParameter()
  {
    // Arrange
    var format = "Limite de upload excedido: {0} MB";
    var expectedMessage = "Limite de upload excedido: 10 MB";

    // Act
    var exception = new PayloadTooLargeException(format, 10);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com múltiplos parâmetros.
  [Fact]
  public void PayloadTooLargeException_ShouldReturnCorrectMessage_WithFormattedString_MultipleParameters()
  {
    // Arrange
    var format = "Arquivo {0} excedeu o limite de {1} MB";
    var expectedMessage = "Arquivo video.mp4 excedeu o limite de 10 MB";

    // Act
    var exception = new PayloadTooLargeException(format, "video.mp4", 10);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.GetStatusCode());
  }

  // Teste para garantir que a excecao interna e preservada.
  [Fact]
  public void PayloadTooLargeException_ShouldPreserveInnerException()
  {
    // Arrange
    var causa = new InvalidOperationException("Causa raiz");
    var message = "Falha ao processar";

    // Act
    var exception = new PayloadTooLargeException(message, causa);

    // Assert
    Assert.Same(causa, exception.InnerException);
    Assert.Equal(message, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(message, exception.GetErrorMessages()[0]);
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.GetStatusCode());
  }
}
