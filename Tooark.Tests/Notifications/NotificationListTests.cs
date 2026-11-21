using System.Net;
using Tooark.Exceptions;
using Tooark.Notifications;

namespace Tooark.Tests.Notifications;

public class NotificationListTests
{
  // Os itens entram na ordem recebida, com a mensagem, a chave e o código de cada um.
  [Fact]
  public void Constructor_ShouldKeepItemsInOrder()
  {
    // Arrange
    NotificationItem[] items =
    [
      new("Email is required", "Email", "T.VLD.STR1"),
      new("Name is too short", "Name", "T.VLD.STR2")
    ];

    // Act
    var notification = new NotificationList(items);

    // Assert
    Assert.False(notification.IsValid);
    Assert.Equal(2, notification.Count);
    Assert.Equal(["Email", "Name"], notification.Keys);
    Assert.Equal(["T.VLD.STR1", "T.VLD.STR2"], notification.Codes);
    Assert.Equal(["Email is required", "Name is too short"], notification.Messages);
  }

  // Uma lista vazia gera uma notificação válida, sem itens.
  [Fact]
  public void Constructor_ShouldBeValid_WhenThereAreNoItems()
  {
    // Arrange & Act
    var notification = new NotificationList([]);

    // Assert
    Assert.True(notification.IsValid);
    Assert.Equal(0, notification.Count);
  }

  // Itens nulos ficam de fora, sem virar a notificação de argumento nulo.
  [Fact]
  public void Constructor_ShouldIgnoreNullItems()
  {
    // Arrange
    NotificationItem?[] items = [null, new("Email is required", "Email"), null];

    // Act
    var notification = new NotificationList(items!);

    // Assert
    var item = Assert.Single(notification.Notifications);
    Assert.Equal("Email", item.Key);
  }

  // A coleção nula é erro de quem chamou.
  [Fact]
  public void Constructor_ShouldThrow_WhenTheCollectionIsNull()
  {
    // Arrange, Act & Assert
    Assert.Throws<ArgumentNullException>(() => new NotificationList(null!));
  }

  // A sequência é percorrida uma única vez, então um iterador preguiçoso também serve.
  [Fact]
  public void Constructor_ShouldAcceptALazySequence()
  {
    // Arrange
    var enumerations = 0;

    IEnumerable<NotificationItem> Items()
    {
      enumerations++;

      yield return new NotificationItem("First", "A");
      yield return new NotificationItem("Second", "B");
    }

    // Act
    var notification = new NotificationList(Items());

    // Assert
    Assert.Equal(2, notification.Count);
    Assert.Equal(1, enumerations);
  }

  // A exceção do Tooark criada com a lista preserva a chave e o código de cada item.
  [Fact]
  public void Exception_ShouldKeepKeyAndCodeOfEachItem()
  {
    // Arrange
    NotificationItem[] items =
    [
      new("Email is required", "Email", "T.VLD.STR1"),
      new("Name is too short", "Name", "T.VLD.STR2")
    ];

    // Act
    var exception = new BadRequestException(new NotificationList(items));

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, exception.GetStatusCode());
    Assert.Equal(["Email is required", "Name is too short"], exception.GetErrorMessages());
    Assert.Collection(
      exception.GetNotifications(),
      first =>
      {
        Assert.Equal("Email", first.Key);
        Assert.Equal("T.VLD.STR1", first.Code);
      },
      second =>
      {
        Assert.Equal("Name", second.Key);
        Assert.Equal("T.VLD.STR2", second.Code);
      });
  }

  // A lista vazia segue a regra das exceções: sem erro algum, vale a chave conhecida.
  [Fact]
  public void Exception_ShouldUseTheKnownKey_WhenTheListIsEmpty()
  {
    // Arrange & Act
    var exception = new UnprocessableEntityException(new NotificationList([]));

    // Assert
    Assert.Equal("Exceptions.ErrorsNullOrEmpty", Assert.Single(exception.GetErrorMessages()));
  }

  // A lista também é agregada por outra notificação, como qualquer Notification.
  [Fact]
  public void AddNotifications_ShouldAggregateTheList()
  {
    // Arrange
    var target = new NotificationList([new NotificationItem("First", "A")]);
    var source = new NotificationList([new NotificationItem("Second", "B")]);

    // Act
    target.AddNotifications(source);

    // Assert
    Assert.Equal(["A", "B"], target.Keys);
  }
}
