using Tooark.Notifications;

namespace Tooark.Tests.Notifications;

public class NotificationTests
{
  // Cria uma classe de notificação para testes, expondo os mutadores protegidos
  public class TestNotification : Notification
  {
    public new void AddNotification(NotificationItem notification) => base.AddNotification(notification);

    public new void AddNotification(Type property, string message) => base.AddNotification(property, message);

    public new void AddNotification(Type property, string message, string code) => base.AddNotification(property, message, code);

    public new void AddNotification(string message, string key) => base.AddNotification(message, key);

    public new void AddNotification(string message, string key, string code) => base.AddNotification(message, key, code);

    public new void AddNotifications(ICollection<NotificationItem> notifications) => base.AddNotifications(notifications);

    public new void Clear() => base.Clear();
  }

  // Testa a adição de uma notificação com uma mensagem
  [Fact]
  public void Should_Add_Notification_With_Message()
  {
    // Arrange
    var message = "Tooark message";

    // Act
    var notification = new TestNotification();
    notification.AddNotification(message);

    // Assert
    Assert.Equal(message, notification.Notifications.First().Message);
    Assert.Single(notification.Notifications);
  }

  // Testa a adição de uma notificação com chave e mensagem
  [Fact]
  public void Should_Add_Notification_With_Key_And_Message()
  {
    // Arrange
    var message = "Tooark message";
    var key = "Tooark Key";
    var code = "ERR";

    // Act
    var notification = new TestNotification();
    notification.AddNotification(message, key, code);

    // Assert
    Assert.Equal(message, notification.Notifications.First().Message);
    Assert.Equal(key.Trim().Replace(" ", string.Empty), notification.Notifications.First().Key);
    Assert.Equal(code, notification.Notifications.First().Code);
    Assert.Single(notification.Notifications);
  }

  // Testa a adição de notificação com propriedade e mensagem
  [Fact]
  public void Should_Add_Notification_With_Property_And_Message()
  {
    // Arrange
    var notification = new TestNotification();
    var message = "Tooark message";
    var property = typeof(TestNotification);

    // Act
    notification.AddNotification(property, message);

    // Assert
    Assert.Equal(message, notification.Notifications.First().Message);
    Assert.Equal(property.Name, notification.Notifications.First().Key);
    Assert.Single(notification.Notifications);
  }

  // Testa a adição de uma notificação com uma instância de NotificationItem
  [Fact]
  public void Should_Add_Notification_With_NotificationItem()
  {
    // Arrange
    var message = "Tooark message";
    var notificationItem = new NotificationItem(message);

    // Act
    var notification = new TestNotification();
    notification.AddNotification(notificationItem);

    // Assert
    Assert.Equal(message, notification.Notifications.First().Message);
    Assert.Single(notification.Notifications);
  }

  // Testa a adição de múltiplas notificações a partir de uma lista de NotificationItem
  [Fact]
  public void Should_Add_Multiple_Notifications_With_ListNotificationItem()
  {
    // Arrange
    var listNotificationItem = new List<NotificationItem>
    {
      new("Message 1"),
      new("Message 2")
    };
    var testNotification = new TestNotification();
    testNotification.AddNotifications(listNotificationItem);

    // Act
    var notification = new TestNotification();
    notification.AddNotifications(testNotification);

    // Assert
    Assert.Equal(2, notification.Notifications.Count);
  }

  // Testa a adição de notificações de outra notificação
  [Fact]
  public void Should_Add_Multiple_Notifications_From_Another_Notification()
  {
    // Arrange
    var notification1 = new TestNotification();
    var notification2 = new TestNotification();
    notification1.AddNotification("Message 1");
    notification1.AddNotification("Message 2");

    // Act
    notification2.AddNotifications(notification1);

    // Assert
    Assert.Equal(2, notification2.Notifications.Count);
  }

  // Testa a adição de notificações utilizando outras Notification como parâmetros
  [Fact]
  public void Should_Add_Multiple_Notifications_With_Params_Notification()
  {
    // Arrange
    var notification1 = new TestNotification();
    notification1.AddNotification("Message 1");
    var notification2 = new TestNotification();
    notification2.AddNotification("Message 2");
    var notification = new TestNotification();

    // Act
    notification.AddNotifications(notification1, notification2);

    // Assert
    Assert.Equal(2, notification.Notifications.Count);
  }

  // Testa a limpeza das notificações
  [Fact]
  public void Should_Clear_Notifications()
  {
    // Arrange
    var notification = new TestNotification();
    notification.AddNotification("Message 1");

    // Act
    notification.Clear();

    // Assert
    Assert.Empty(notification.Notifications);
  }

  // Testa a validação de notificações
  [Fact]
  public void Should_Return_ValidAndInvalid()
  {
    // Arrange
    var validNotification = new TestNotification();
    var invalidNotification = new TestNotification();


    // Act
    invalidNotification.AddNotification("Tooark Message");

    // Assert
    Assert.True(validNotification.IsValid);
    Assert.False(invalidNotification.IsValid);
  }

  // Testa a contagem de notificações
  [Fact]
  public void Should_Return_Count()
  {
    // Arrange
    var notification1 = new TestNotification();
    notification1.AddNotification("Message 1");
    var notification2 = new TestNotification();
    notification2.AddNotification("Message 2");
    var notification = new TestNotification();

    // Act
    notification.AddNotifications(notification1, notification2);

    // Assert
    Assert.Equal(2, notification.Count);
  }

  // Testa a obtenção de códigos de erros
  [Fact]
  public void Should_Return_Code()
  {
    // Arrange
    var message = "Tooark message";
    var key = "Tooark Key";
    var code = "ERR";

    // Act
    var notification = new TestNotification();
    notification.AddNotification(message, key, code);

    // Assert
    Assert.Equal(code, notification.Codes[0]);
    Assert.Single(notification.Keys);
  }

  // Testa a obtenção de chaves
  [Fact]
  public void Should_Return_Key()
  {
    // Arrange
    var message = "Tooark message";
    var key = "Tooark Key";
    var code = "ERR";

    // Act
    var notification = new TestNotification();
    notification.AddNotification(message, key, code);

    // Assert
    Assert.Equal(key.Trim().Replace(" ", string.Empty), notification.Keys[0]);
    Assert.Single(notification.Keys);
  }

  // Testa a obtenção de mensagens
  [Fact]
  public void Should_Return_Message()
  {
    // Arrange
    var message = "Tooark message";
    var key = "Tooark Key";
    var code = "ERR";

    // Act
    var notification = new TestNotification();
    notification.AddNotification(message, key, code);

    // Assert
    Assert.Equal(message, notification.Messages[0]);
    Assert.Single(notification.Messages);
  }

  // Testa o retorno da mensagem padrão ao adicionar notificação nula
  [Fact]
  public void Should_ReturnMessageDefaultNotificationNull_WhenAddingNullNotificationItem()
  {
    // Arrange
    var notification = new TestNotification();

    // Act
    notification.AddNotification(null!);

    // Assert
    Assert.Equal("Notifications.NotificationNull", notification.Notifications.First().Message);
  }

  // Testa o retorno da mensagem padrão ao adicionar notificação com mensagem nula ou vazia com Type Property
  [Fact]
  public void Should_ReturnMessageDefaultMessageNullEmpty_WhenAddingNotificationWithNullOrEmptyMessageAndProperty()
  {
    // Arrange
    var notificationNull = new TestNotification();
    var notificationEmpty = new TestNotification();
    var property = typeof(TestNotification);

    // Act
    notificationNull.AddNotification(property, null!);
    notificationEmpty.AddNotification(property, "");

    // Assert
    Assert.Equal("Notifications.MessageNullEmpty", notificationNull.Notifications.First().Message);
    Assert.Equal("Notifications.MessageNullEmpty", notificationEmpty.Notifications.First().Message);
  }

  // Testa o retorno da mensagem padrão ao adicionar notificação com mensagem nula ou vazia com Type Property e Code
  [Fact]
  public void Should_ReturnMessageDefaultMessageNullEmpty_WhenAddingNotificationWithNullOrEmptyMessagePropertyAndCode()
  {
    // Arrange
    var notificationNull = new TestNotification();
    var notificationEmpty = new TestNotification();
    var property = typeof(TestNotification);
    var code = "ERR";

    // Act
    notificationNull.AddNotification(property, null!);
    notificationEmpty.AddNotification(property, "", code);

    // Assert
    Assert.Equal("Notifications.MessageNullEmpty", notificationNull.Notifications.First().Message);
    Assert.Equal("Notifications.MessageNullEmpty", notificationEmpty.Notifications.First().Message);
  }

  // Testa o retorno da chave padrão ao adicionar notificação com tipo nulo
  [Fact]
  public void Should_ReturnKeyDefault_WhenAddingNotificationWithNullProperty()
  {
    // Arrange
    var notification = new TestNotification();
    var notificationWithCode = new TestNotification();

    // Act
    notification.AddNotification((Type)null!, "Message 1");
    notificationWithCode.AddNotification((Type)null!, "Message 2", "ERR");

    // Assert
    Assert.Equal("Unknown", notification.Keys[0]);
    Assert.Equal("Unknown", notificationWithCode.Keys[0]);
    Assert.Equal("ERR", notificationWithCode.Codes[0]);
  }

  // Testa o retorno da mensagem padrão ao adicionar uma notificação nula
  [Fact]
  public void Should_ReturnMessageDefaultNotificationNull_WhenAddingNullNotification()
  {
    // Arrange
    var notification = new TestNotification();

    // Act
    notification.AddNotifications((Notification)null!);

    // Assert
    Assert.Equal("Notifications.NotificationNull", notification.Notifications.First().Message);
  }

  // Testa o retorno da mensagem padrão ao adicionar uma coleção de itens nula
  [Fact]
  public void Should_ReturnMessageDefaultNotificationNull_WhenAddingNullNotificationItemCollection()
  {
    // Arrange - antes lançava NullReferenceException ao iterar a coleção
    var notification = new TestNotification();

    // Act
    notification.AddNotifications((ICollection<NotificationItem>)null!);

    // Assert
    Assert.Single(notification.Notifications);
    Assert.Equal("Notifications.NotificationNull", notification.Notifications.First().Message);
  }

  // Testa o retorno da mensagem padrão ao adicionar uma coleção de notificações nula
  [Fact]
  public void Should_ReturnMessageDefaultNotificationNull_WhenAddingNullNotificationArray()
  {
    // Arrange - antes lançava NullReferenceException ao iterar a coleção
    var notification = new TestNotification();

    // Act
    notification.AddNotifications((Notification[])null!);

    // Assert
    Assert.Single(notification.Notifications);
    Assert.Equal("Notifications.NotificationNull", notification.Notifications.First().Message);
  }

  // Testa que itens nulos dentro da coleção de itens são ignorados
  [Fact]
  public void Should_IgnoreNullItems_WhenAddingNotificationItemCollection()
  {
    // Arrange - antes o item nulo entrava na lista e quebrava a leitura das notificações
    var notification = new TestNotification();
    var listNotificationItem = new List<NotificationItem>
    {
      new("Message 1"),
      null!,
      new("Message 2")
    };

    // Act
    notification.AddNotifications(listNotificationItem);

    // Assert
    Assert.Equal(2, notification.Count);
    Assert.Equal(["Message 1", "Message 2"], notification.Messages);
    Assert.Equal(["T.ERR", "T.ERR"], notification.Codes);
  }

  // Testa que notificações nulas dentro da coleção de notificações são ignoradas
  [Fact]
  public void Should_IgnoreNullItems_WhenAddingNotificationArray()
  {
    // Arrange
    var notification1 = new TestNotification();
    notification1.AddNotification("Message 1");
    var notification = new TestNotification();

    // Act
    notification.AddNotifications(notification1, null!);

    // Assert
    Assert.Single(notification.Notifications);
    Assert.Equal("Message 1", notification.Messages[0]);
  }

  // Testa que adicionar a própria instância não altera a lista de notificações
  [Fact]
  public void Should_NotChangeNotifications_WhenAddingItself()
  {
    // Arrange - a auto-adição duplicava as notificações da própria instância
    var notification = new TestNotification();
    notification.AddNotification("Message 1");

    // Act
    notification.AddNotifications(notification);

    // Assert
    Assert.Single(notification.Notifications);
    Assert.Equal("Message 1", notification.Messages[0]);
  }

  // Testa o retorno da mensagem padrão ao adicionar notificação com mensagem em branco
  [Fact]
  public void Should_ReturnMessageDefaultMessageNullEmpty_WhenAddingNotificationWithWhiteSpaceMessage()
  {
    // Arrange - antes a mensagem em branco gerava uma notificação com mensagem vazia
    var notification = new TestNotification();

    // Act
    notification.AddNotification("   ", "Key");

    // Assert
    Assert.Equal("Notifications.MessageNullEmpty", notification.Messages[0]);
  }

  // Testa que a lista de notificações não pode ser alterada externamente
  [Fact]
  public void Should_ReturnReadOnlyCollection_WhenAccessingNotifications()
  {
    // Arrange
    var notification = new TestNotification();
    notification.AddNotification("Message 1");

    // Act
    var notifications = notification.Notifications;

    // Assert
    Assert.Single(notifications);
    Assert.Same(notifications, notification.Notifications);
  }
}
