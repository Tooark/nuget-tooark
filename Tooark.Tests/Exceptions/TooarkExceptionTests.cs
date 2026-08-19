using System.Net;
using Tooark.Exceptions;
using Tooark.Exceptions.Messages;
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
    public TestTooarkException(string message, Exception innerException) : base(message, innerException) { }
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

  // Teste para garantir que lista nula registra a chave conhecida em vez de estourar.
  [Fact]
  public void Constructor_WithNullErrorList_ShouldRegisterKnownMessage()
  {
    // Act
    var exception = new TestTooarkException((IList<string>)null!);

    // Assert
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(ExceptionErrorMessages.ErrorsIsNullOrEmpty, exception.GetErrorMessages()[0]);
    Assert.Equal(ExceptionErrorMessages.ErrorsIsNullOrEmpty, exception.Message);
  }

  // Teste para garantir que lista vazia nao produz excecao sem erro.
  [Fact]
  public void Constructor_WithEmptyErrorList_ShouldRegisterKnownMessage()
  {
    // Act
    var exception = new TestTooarkException(new List<string>());

    // Assert
    Assert.Single(exception.GetErrorMessages());
    Assert.Single(exception.GetNotifications());
    Assert.Equal(ExceptionErrorMessages.ErrorsIsNullOrEmpty, exception.Message);
  }

  // Teste para garantir que notificacao nula registra a chave conhecida em vez de estourar.
  [Fact]
  public void Constructor_WithNullNotification_ShouldRegisterKnownMessage()
  {
    // Act
    var exception = new TestTooarkException((Notification)null!);

    // Assert
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(ExceptionErrorMessages.ErrorsIsNullOrEmpty, exception.GetErrorMessages()[0]);
  }

  // Teste para garantir que notificacao sem itens registra a chave conhecida.
  [Fact]
  public void Constructor_WithEmptyNotification_ShouldRegisterKnownMessage()
  {
    // Act
    var exception = new TestTooarkException(new TestNotification());

    // Assert
    Assert.Single(exception.GetErrorMessages());
    Assert.Equal(ExceptionErrorMessages.ErrorsIsNullOrEmpty, exception.GetErrorMessages()[0]);
  }

  // Teste para garantir que mensagem nula ou em branco registra a chave conhecida.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithBlankMessage_ShouldRegisterKnownMessage(string? message)
  {
    // Act
    var exception = new TestTooarkException(message!);

    // Assert
    Assert.Equal(ExceptionErrorMessages.MessageIsNullOrEmpty, exception.Message);
    Assert.Equal(ExceptionErrorMessages.MessageIsNullOrEmpty, exception.GetErrorMessages()[0]);
    Assert.Equal(ExceptionErrorMessages.MessageIsNullOrEmpty, exception.GetNotifications()[0].Message);
  }

  // Teste para garantir que item nulo na lista aparece igual nas duas leituras.
  [Fact]
  public void Constructor_WithNullItemInList_ShouldKeepBothReadsAligned()
  {
    // Act
    var exception = new TestTooarkException(new List<string> { null!, "Erro real" });

    // Assert
    Assert.Equal(2, exception.GetErrorMessages().Count);
    Assert.Equal(exception.GetErrorMessages().Count, exception.GetNotifications().Count);
    Assert.Equal(ExceptionErrorMessages.MessageIsNullOrEmpty, exception.GetErrorMessages()[0]);
    Assert.Equal(ExceptionErrorMessages.MessageIsNullOrEmpty, exception.GetNotifications()[0].Message);
    Assert.Equal("Erro real", exception.GetErrorMessages()[1]);
  }

  // Teste para garantir que as colecoes expostas nao permitem alteracao externa.
  [Fact]
  public void GetErrorMessages_ShouldNotBeMutableFromOutside()
  {
    // Arrange
    var exception = new TestTooarkException("Erro original");

    // Act
    var errors = exception.GetErrorMessages();
    var notifications = exception.GetNotifications();

    // Assert
    Assert.IsNotType<List<string>>(errors);
    Assert.IsNotType<List<NotificationItem>>(notifications);
    Assert.Throws<NotSupportedException>(() => ((IList<string>)errors).Clear());
    Assert.Throws<NotSupportedException>(() => ((IList<NotificationItem>)notifications).Clear());
    Assert.Single(exception.GetErrorMessages());
  }

  // Teste para garantir que a colecao somente leitura e reutilizada a cada acesso.
  [Fact]
  public void GetErrorMessages_ShouldReuseTheSameInstance()
  {
    // Arrange
    var exception = new TestTooarkException("Erro original");

    // Act & Assert
    Assert.Same(exception.GetErrorMessages(), exception.GetErrorMessages());
    Assert.Same(exception.GetNotifications(), exception.GetNotifications());
  }

  // Teste para garantir que a excecao interna e preservada.
  [Fact]
  public void Constructor_WithInnerException_ShouldPreserveCause()
  {
    // Arrange
    var causa = new InvalidOperationException("Causa raiz");

    // Act
    var exception = new TestTooarkException("Falha ao processar", causa);

    // Assert
    Assert.Same(causa, exception.InnerException);
    Assert.Equal("Falha ao processar", exception.Message);
    Assert.Single(exception.GetErrorMessages());
  }

  // Teste para garantir que formato incompativel mantem a mensagem em vez de lancar.
  [Fact]
  public void Constructor_WithIncompatibleFormat_ShouldKeepMessage()
  {
    // Arrange
    var format = "Payload invalido: {campo}";

    // Act
    var exception = new TestTooarkException(format, "valor");

    // Assert
    Assert.Equal(format, exception.Message);
    Assert.Equal(format, exception.GetErrorMessages()[0]);
  }

  // Teste para garantir que mensagem sem marcadores nao e alterada pelos parametros.
  [Fact]
  public void Constructor_WithFormatWithoutPlaceholders_ShouldKeepMessage()
  {
    // Arrange
    var format = "Mensagem sem marcador";

    // Act
    var exception = new TestTooarkException(format, "ignorado");

    // Assert
    Assert.Equal(format, exception.Message);
  }

  // Teste para garantir que formato em branco cai na chave conhecida, mesmo com parametros.
  [Fact]
  public void Constructor_WithBlankFormatAndArgs_ShouldRegisterKnownMessage()
  {
    // Act
    var exception = new TestTooarkException("   ", "ignorado");

    // Assert
    Assert.Equal(ExceptionErrorMessages.MessageIsNullOrEmpty, exception.Message);
  }

  // Teste para garantir que lista de parametros vazia mantem a mensagem intacta.
  [Fact]
  public void Constructor_WithEmptyArgs_ShouldKeepMessage()
  {
    // Arrange
    var format = "Corpo recebido: {\"id\":1}";

    // Act
    var exception = new TestTooarkException(format, []);

    // Assert
    Assert.Equal(format, exception.Message);
    Assert.Equal(format, exception.GetErrorMessages()[0]);
  }
}
