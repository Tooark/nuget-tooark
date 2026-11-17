using Microsoft.AspNetCore.Http;
using Tooark.AspNetCore.Extensions;
using Tooark.Notifications;
using Tooark.Utils;

namespace Tooark.Tests.AspNetCore.Extensions;

[Collection("CultureSensitive")]
public class ValidationProblemExtensionTests
{
  // Testa se cada mensagem vira um item, com o campo como chave e o código padrão, na ordem do dicionário
  [Fact]
  public void ToNotificationItems_ShouldCreateOneItemPerMessage()
  {
    // Arrange
    var errors = new Dictionary<string, string[]>
    {
      ["Name"] = ["Name is mandatory", "Field.Invalid;Name"],
      ["Email"] = ["Field.Required;Email"]
    };

    // Act
    var items = errors.ToNotificationItems();

    // Assert
    Assert.Equal(3, items.Count);
    Assert.Equal(("Name", "Name is mandatory"), (items[0].Key, items[0].Message));
    Assert.Equal(("Name", "Field.Invalid;Name"), (items[1].Key, items[1].Message));
    Assert.Equal(("Email", "Field.Required;Email"), (items[2].Key, items[2].Message));
    Assert.All(items, item => Assert.Equal("T.ERR", item.Code));
  }

  // Testa se uma mensagem em branco vira a chave de campo inválido, ou de requisição inválida sem campo, e se um
  // campo sem mensagens não gera item
  [Fact]
  public void ToNotificationItems_ShouldHandleBlankMessagesAndKeys()
  {
    // Arrange
    var errors = new Dictionary<string, string[]>
    {
      ["Age"] = [" "],
      [string.Empty] = [string.Empty],
      ["Empty"] = [],
      ["Null"] = null!
    };

    // Act
    var items = errors.ToNotificationItems();

    // Assert
    Assert.Equal(2, items.Count);
    Assert.Equal(("Age", "Field.Invalid;Age"), (items[0].Key, items[0].Message));
    Assert.Equal(("Unknown", "BadRequest"), (items[1].Key, items[1].Message));
  }

  // Testa se o problema de validação do ASP.NET Core é convertido pelas mensagens dele
  [Fact]
  public void ToNotificationItems_FromProblemDetails_ShouldUseTheErrors()
  {
    // Arrange
    var problemDetails = new HttpValidationProblemDetails(new Dictionary<string, string[]> { ["Email"] = ["invalid"] });

    // Act
    var item = Assert.Single(problemDetails.ToNotificationItems());

    // Assert
    Assert.Equal(("Email", "invalid"), (item.Key, item.Message));
  }

  // Testa se os itens são agrupados pela chave, na ordem da primeira ocorrência, com as mensagens traduzidas e sem
  // os itens nulos
  [Fact]
  public void ToValidationErrors_ShouldGroupByKey_AndTranslate()
  {
    // Arrange
    Language.SetCulture("en-US");
    NotificationItem[] notifications =
    [
      new("Field.Required;Email", "Email", "T.VLD.STR1"),
      new("Name is mandatory", "Name"),
      null!,
      new("Field.Invalid;Email", "Email", "T.VLD.RGX1")
    ];

    // Act
    var errors = notifications.ToValidationErrors();

    // Assert
    Assert.Equal(["Email", "Name"], errors.Keys);
    Assert.Equal(["Field Email is required", "Field Email is invalid"], errors["Email"]);
    Assert.Equal(["Name is mandatory"], errors["Name"]);
  }

  // Testa se os argumentos nulos são recusados
  [Fact]
  public void Methods_ShouldThrow_WhenArgumentIsNull()
  {
    Assert.Throws<ArgumentNullException>(() => ((IDictionary<string, string[]>)null!).ToNotificationItems());
    Assert.Throws<ArgumentNullException>(() => ((HttpValidationProblemDetails)null!).ToNotificationItems());
    Assert.Throws<ArgumentNullException>(() => ((IEnumerable<NotificationItem>)null!).ToValidationErrors());
  }
}
