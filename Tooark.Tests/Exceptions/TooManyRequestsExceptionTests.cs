using System.Net;
using Tooark.Exceptions;
using Tooark.Notifications;

namespace Tooark.Tests.Exceptions;

public class TooManyRequestsExceptionTests
{
  // Classe de teste para simular uma notificação.
  public class TestException : Notification
  {
    // Expoe o mutador protegido para uso nos testes.
    public new void AddNotification(NotificationItem notification) => base.AddNotification(notification);
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma única mensagem.
  [Fact]
  public void TooManyRequestsException_ShouldReturnCorrectMessage_WithSingleMessage()
  {
    // Arrange
    var expectedMessage = "Too Many Requests Error";

    // Act
    var exception = new TooManyRequestsException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.TooManyRequests, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma lista de mensagens.
  [Fact]
  public void TooManyRequestsException_ShouldReturnCorrectMessage_WithListMessages()
  {
    // Arrange
    string[] expectedMessage = ["Too Many Requests Error", "Another Too Many Requests Error"];

    // Act
    var exception = new TooManyRequestsException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage[0], exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetNotifications().Select(n => n.Message));
    Assert.Equal(HttpStatusCode.TooManyRequests, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de notificação.
  [Fact]
  public void TooManyRequestsException_ShouldReturnCorrectMessage_WithNotification()
  {
    // Arrange
    string expectedMessage = "Too Many Requests Error";
    TestException testException = new();
    testException.AddNotification(expectedMessage);

    // Act
    var exception = new TooManyRequestsException(testException);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.TooManyRequests, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com um parâmetro.
  [Fact]
  public void TooManyRequestsException_ShouldReturnCorrectMessage_WithFormattedString_SingleParameter()
  {
    // Arrange
    var format = "Limite de {0} requisições excedido";
    var expectedMessage = "Limite de 100 requisições excedido";

    // Act
    var exception = new TooManyRequestsException(format, 100);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.TooManyRequests, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com múltiplos parâmetros.
  [Fact]
  public void TooManyRequestsException_ShouldReturnCorrectMessage_WithFormattedString_MultipleParameters()
  {
    // Arrange
    var format = "Cliente {0} excedeu o limite de {1} requisições";
    var expectedMessage = "Cliente app-mobile excedeu o limite de 100 requisições";

    // Act
    var exception = new TooManyRequestsException(format, "app-mobile", 100);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.TooManyRequests, exception.GetStatusCode());
  }

  // Teste para garantir que a excecao interna e preservada.
  [Fact]
  public void TooManyRequestsException_ShouldPreserveInnerException()
  {
    // Arrange
    var causa = new InvalidOperationException("Causa raiz");
    var message = "Falha ao processar";

    // Act
    var exception = new TooManyRequestsException(message, causa);

    // Assert
    Assert.Same(causa, exception.InnerException);
    Assert.Equal(message, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(message, exception.GetErrorMessages()[0]);
    Assert.Equal(HttpStatusCode.TooManyRequests, exception.GetStatusCode());
  }
}
