using System.Net;
using Tooark.Exceptions;
using Tooark.Notifications;

namespace Tooark.Tests.Exceptions;

public class TooarkExceptionTests
{
  // Classe de exceção de teste que herda de TooarkException.
  private class TestTooarkException : TooarkException
  {
    public TestTooarkException(string message) : base(message) { }
    public TestTooarkException(IList<string> errors) : base(errors) { }
    public TestTooarkException(Notification notification) : base(notification) { }
    public TestTooarkException(string messageFormat, params object[] args) : base(messageFormat, args) { }

    public override HttpStatusCode GetStatusCode() => HttpStatusCode.BadRequest;
  }

  // Classe de notificação de teste que herda de Notification.
  private class TestNotification : Notification
  {
    // Expoe o mutador protegido para uso nos testes.
    public new void AddNotification(NotificationItem notification) => base.AddNotification(notification);
  }

  // Teste de unidade para o construtor com uma única mensagem.
  [Fact]
  public void Constructor_WithSingleMessage_ShouldInitializeErrorList()
  {
    // Arrange
    var message = "Test error message";

    // Act
    var exception = new TestTooarkException(message);

    // Assert
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(message, exception.GetErrorMessages().First());
  }

  // Teste de unidade para o construtor com uma lista de mensagens.
  [Fact]
  public void Constructor_WithErrorList_ShouldInitializeErrorList()
  {
    // Arrange
    var errors = new List<string> { "Error 1", "Error 2" };

    // Act
    var exception = new TestTooarkException(errors);

    // Assert
    Assert.Equal(errors.Count, exception.GetErrorMessages().Count);
    Assert.Equal(errors, exception.GetErrorMessages());
  }

  // Teste de unidade para o construtor com uma notificação.
  [Fact]
  public void Constructor_WithNotification_ShouldInitializeNotification()
  {
    // Arrange
    string message = "Test error message";
    var notification = new TestNotification();
    notification.AddNotification(message);

    // Act
    var exception = new TestTooarkException(notification);

    // Assert
    Assert.Single(exception.GetNotifications());
    Assert.Equal(message, exception.GetErrorMessages().FirstOrDefault());
    Assert.Equal(message, exception.GetNotifications().FirstOrDefault()?.Message);
  }

  // Teste de unidade para a função GetStatusCode.
  [Fact]
  public void GetStatusCode_ShouldReturnBadRequest()
  {
    // Arrange
    var exception = new TestTooarkException("Test error message");

    // Act
    var statusCode = exception.GetStatusCode();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, statusCode);
  }

  // Teste de unidade para o construtor com formatação de string (um parâmetro).
  [Fact]
  public void Constructor_WithFormattedMessageSingleParameter_ShouldFormatAndInitialize()
  {
    // Arrange
    var format = "Test error with value: {0}";
    var value = "test123";
    var expectedMessage = "Test error with value: test123";

    // Act
    var exception = new TestTooarkException(format, value);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
  }

  // Teste de unidade para o construtor com formatação de string (múltiplos parâmetros).
  [Fact]
  public void Constructor_WithFormattedMessageMultipleParameters_ShouldFormatAndInitialize()
  {
    // Arrange
    var format = "Tenho {0} registros com problema {1} no sistema {2}";
    var expectedMessage = "Tenho 2 registros com problema algum erro no sistema prod";

    // Act
    var exception = new TestTooarkException(format, 2, "algum erro", "prod");

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
    Assert.Equal(expectedMessage, exception.GetNotifications().FirstOrDefault()?.Message);
  }

  // Teste de unidade para o construtor com formatação de string (com objetos complexos).
  [Fact]
  public void Constructor_WithFormattedMessageComplexObjects_ShouldFormatAndInitialize()
  {
    // Arrange
    var format = "Erro ao processar {0} de {1} items";
    var expectedMessage = "Erro ao processar 5 de 100 items";

    // Act
    var exception = new TestTooarkException(format, 5, 100);

    // Assert
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(expectedMessage, exception.GetErrorMessages().First());
  }
}
