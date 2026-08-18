using System.Net;
using Tooark.Exceptions;
using Tooark.Notifications;

namespace Tooark.Tests.Exceptions;

public class UnauthorizedExceptionTest
{
  // Classe de teste para simular uma exceção de teste.
  public class TestException : Notification
  {
    // Expoe o mutador protegido para uso nos testes.
    public new void AddNotification(NotificationItem notification) => base.AddNotification(notification);
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma única mensagem.
  [Fact]
  public void UnauthorizedException_ShouldReturnCorrectMessage_WithSingleMessage()
  {
    // Arrange
    var expectedMessage = "Unauthorized Error";

    // Act
    var exception = new UnauthorizedException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.Unauthorized, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de uma lista de mensagens.
  [Fact]
  public void UnauthorizedException_ShouldReturnCorrectMessage_WithListMessages()
  {
    // Arrange
    string[] expectedMessage = ["Unauthorized Error", "Another Unauthorized Error"];

    // Act
    var exception = new UnauthorizedException(expectedMessage);

    // Assert
    Assert.Equal(expectedMessage[0], exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetNotifications().Select(n => n.Message));
    Assert.Equal(HttpStatusCode.Unauthorized, exception.GetStatusCode());
  }

  // Teste para retornar a mensagem de erro correta com parâmetro de notificação.
  [Fact]
  public void UnauthorizedException_ShouldReturnCorrectMessage_WithNotification()
  {
    // Arrange
    string expectedMessage = "Unauthorized Error";
    TestException testException = new();
    testException.AddNotification(expectedMessage);

    // Act
    var exception = new UnauthorizedException(testException);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.Unauthorized, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com um parâmetro.
  [Fact]
  public void UnauthorizedException_ShouldReturnCorrectMessage_WithFormattedString_SingleParameter()
  {
    // Arrange
    var format = "Token {0} inválido ou expirado";
    var expectedMessage = "Token jwt inválido ou expirado";

    // Act
    var exception = new UnauthorizedException(format, "jwt");

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.Unauthorized, exception.GetStatusCode());
  }

  // Teste para formatação de mensagem com múltiplos parâmetros.
  [Fact]
  public void UnauthorizedException_ShouldReturnCorrectMessage_WithFormattedString_MultipleParameters()
  {
    // Arrange
    var format = "Usuário {0} não autenticado - {1}";
    var expectedMessage = "Usuário admin não autenticado - credenciais inválidas";

    // Act
    var exception = new UnauthorizedException(format, "admin", "credenciais inválidas");

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
    Assert.Equal(HttpStatusCode.Unauthorized, exception.GetStatusCode());
  }

  // Teste para garantir que a excecao interna e preservada.
  [Fact]
  public void UnauthorizedException_ShouldPreserveInnerException()
  {
    // Arrange
    var causa = new InvalidOperationException("Causa raiz");
    var message = "Falha ao processar";

    // Act
    var exception = new UnauthorizedException(message, causa);

    // Assert
    Assert.Same(causa, exception.InnerException);
    Assert.Equal(message, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(message, exception.GetErrorMessages()[0]);
    Assert.Equal(HttpStatusCode.Unauthorized, exception.GetStatusCode());
  }
}
