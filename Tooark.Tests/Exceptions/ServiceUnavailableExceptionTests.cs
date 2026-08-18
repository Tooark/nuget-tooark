using System.Net;
using Tooark.Exceptions;
using Tooark.Notifications;

namespace Tooark.Tests.Exceptions;

public class ServiceUnavailableExceptionTests
{
  // Classe de teste para simular uma exceção de teste.
  public class TestException : Notification
  {
    // Expoe o mutador protegido para uso nos testes.
    public new void AddNotification(NotificationItem notification) => base.AddNotification(notification);
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma única mensagem.
  [Fact]
  public void ServiceUnavailableException_ShouldReturnCorrectMessage_WithSingleMessage()
  {
    // Arrange
    var expectedMessage = "Service Unavailable Error";

    // Act
    var exception = new ServiceUnavailableException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma lista de mensagens.
  [Fact]
  public void ServiceUnavailableException_ShouldReturnCorrectMessage_WithListMessages()
  {
    // Arrange
    string[] expectedMessage = ["Service Unavailable Error", "Another Service Unavailable Error"];

    // Act
    var exception = new ServiceUnavailableException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage[0], exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetNotifications().Select(n => n.Message));
    Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de notificação.
  [Fact]
  public void ServiceUnavailableException_ShouldReturnCorrectMessage_WithNotification()
  {
    // Arrange
    string expectedMessage = "Service Unavailable Error";
    TestException testException = new();
    testException.AddNotification(expectedMessage);

    // Act
    var exception = new ServiceUnavailableException(testException);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com um parâmetro.
  [Fact]
  public void ServiceUnavailableException_ShouldReturnCorrectMessage_WithFormattedString_SingleParameter()
  {
    // Arrange
    var format = "Serviço {0} indisponível";
    var expectedMessage = "Serviço autenticacion indisponível";

    // Act
    var exception = new ServiceUnavailableException(format, "autenticacion");

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com múltiplos parâmetros.
  [Fact]
  public void ServiceUnavailableException_ShouldReturnCorrectMessage_WithFormattedString_MultipleParameters()
  {
    // Arrange
    var format = "Serviço {0} indisponível em {1}";
    var expectedMessage = "Serviço banco de dados indisponível em prod";

    // Act
    var exception = new ServiceUnavailableException(format, "banco de dados", "prod");

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.GetStatusCode());
  }

  // Teste para garantir que a excecao interna e preservada.
  [Fact]
  public void ServiceUnavailableException_ShouldPreserveInnerException()
  {
    // Arrange
    var causa = new InvalidOperationException("Causa raiz");
    var message = "Falha ao processar";

    // Act
    var exception = new ServiceUnavailableException(message, causa);

    // Assert
    Assert.Same(causa, exception.InnerException);
    Assert.Equal(message, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(message, exception.GetErrorMessages()[0]);
    Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.GetStatusCode());
  }
}
