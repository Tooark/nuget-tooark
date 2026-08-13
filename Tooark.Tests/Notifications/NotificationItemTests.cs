using Tooark.Notifications;

namespace Tooark.Tests.Notifications;

public class NotificationItemTests
{
  // Teste para construtor da classe NotificationItem com parâmetros padrão.
  [Fact]
  public void Constructor_ShouldNotificationItem_WhenDefaultValues()
  {
    // Arrange
    var message = "Tooark message";

    // Act
    var notification = new NotificationItem(message);

    // Assert
    Assert.Equal(message, notification.Message);
    Assert.Equal("Unknown", notification.Key);
    Assert.Equal("T.ERR", notification.Code);
  }

  // Teste para construtor da classe NotificationItem com parâmetro key.
  [Fact]
  public void Constructor_ShouldNotificationItem_WhenKeyParam()
  {
    // Arrange
    var message = "Tooark message";
    var key = "Tooark Key";
    var code = "ERR";

    // Act
    var notification = new NotificationItem(message, key, code);

    // Assert
    Assert.Equal(message, notification.Message);
    Assert.Equal(key.Trim().Replace(" ", string.Empty), notification.Key);
    Assert.Equal(code, notification.Code);
  }

  // Teste para método ToString da classe NotificationItem.
  [Fact]
  public void ToString_ShouldReturnMessage()
  {
    // Arrange
    var message = "Tooark message";
    var notification = new NotificationItem(message);

    // Act
    var result = notification.ToString();

    // Assert
    Assert.Equal(message, result);
  }

  // Teste para operador implícito de conversão de string para NotificationItem.
  [Fact]
  public void ImplicitConversion_FromString_ShouldCreateNotification()
  {
    // Arrange
    var message = "Tooark message";

    // Act
    NotificationItem notification = message;

    // Assert
    Assert.Equal(message, notification.Message);
    Assert.Equal("Unknown", notification.Key);
    Assert.Equal("T.ERR", notification.Code);
  }

  // Teste para operador implícito de conversão de NotificationItem para string.
  [Fact]
  public void ImplicitConversion_ToString_ShouldReturnMessage()
  {
    // Arrange
    var message = "Tooark message";
    var notification = new NotificationItem(message);

    // Act
    string result = notification;

    // Assert
    Assert.Equal(message, result);
    Assert.Equal("Unknown", notification.Key);
    Assert.Equal("T.ERR", notification.Code);
  }

  // Teste para construtor da classe NotificationItem com mensagem nula, vazia ou em branco.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("\t\n")]
  public void Constructor_ShouldMessageNullEmpty_WhenMessageNullOrWhiteSpace(string? message)
  {
    // Act - antes mensagem nula gerava 'Notifications.MessageUnknown' e vazia ou em branco gerava string.Empty
    var notification = new NotificationItem(message!);

    // Assert
    Assert.Equal("Notifications.MessageNullEmpty", notification.Message);
    Assert.Equal("Unknown", notification.Key);
    Assert.Equal("T.ERR", notification.Code);
  }

  // Teste para construtor da classe NotificationItem com chave nula retornando 'Unknown'.
  [Fact]
  public void Constructor_ShouldKeyUnknown_WhenKeyNull()
  {
    // Arrange
    string message = "Tooark Message";
    string key = null!;

    // Act
    var notification = new NotificationItem(message, key);

    // Assert
    Assert.Equal(message, notification.Message);
    Assert.Equal("Unknown", notification.Key);
    Assert.Equal("T.ERR", notification.Code);
  }

  // Teste para construtor da classe NotificationItem com chave vazia ou em branco retornando 'Unknown'.
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("\t\n")]
  public void Constructor_ShouldKeyUnknown_WhenKeyEmptyOrWhiteSpace(string key)
  {
    // Arrange
    string message = "Tooark Message";

    // Act
    var notification = new NotificationItem(message, key);

    // Assert
    Assert.Equal(message, notification.Message);
    Assert.Equal("Unknown", notification.Key);
    Assert.Equal("T.ERR", notification.Code);
  }

  // Teste para construtor da classe NotificationItem removendo todos os espaços em branco da chave.
  [Theory]
  [InlineData("Tooark Key", "TooarkKey")]
  [InlineData("Tooark\tKey", "TooarkKey")]
  [InlineData("Tooark\r\nKey", "TooarkKey")]
  [InlineData("  Tooark  Key  ", "TooarkKey")]
  public void Constructor_ShouldRemoveAllWhiteSpace_WhenKeyHasWhiteSpace(string key, string expected)
  {
    // Act - antes apenas o espaço literal era removido, mantendo tabulação e quebra de linha
    var notification = new NotificationItem("Tooark Message", key);

    // Assert
    Assert.Equal(expected, notification.Key);
  }

  // Teste para construtor da classe NotificationItem com código nulo, vazio ou em branco retornando 'T.ERR'.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_ShouldCodeDefault_WhenCodeNullOrWhiteSpace(string? code)
  {
    // Act
    var notification = new NotificationItem("Tooark Message", "Key", code!);

    // Assert
    Assert.Equal("T.ERR", notification.Code);
  }

  // Teste para operador implícito de conversão de NotificationItem nulo para string.
  [Fact]
  public void ImplicitConversion_ToString_ShouldReturnEmpty_WhenNotificationNull()
  {
    // Arrange - antes a conversão de uma instância nula lançava NullReferenceException
    NotificationItem notification = null!;

    // Act
    string result = notification;

    // Assert
    Assert.Equal(string.Empty, result);
  }

  // Teste para operador implícito de conversão de string nula para NotificationItem.
  [Fact]
  public void ImplicitConversion_FromString_ShouldMessageNullEmpty_WhenStringNull()
  {
    // Arrange
    string message = null!;

    // Act
    NotificationItem notification = message;

    // Assert
    Assert.Equal("Notifications.MessageNullEmpty", notification.Message);
    Assert.Equal("Unknown", notification.Key);
    Assert.Equal("T.ERR", notification.Code);
  }
}
