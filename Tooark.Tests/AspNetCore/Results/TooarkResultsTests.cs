using Microsoft.AspNetCore.Http;
using Tooark.AspNetCore.Results;
using Tooark.Exceptions;
using Tooark.Notifications;
using Tooark.Tests.Moq.Notifications;
using Tooark.Utils;

namespace Tooark.Tests.AspNetCore.Results;

/// <summary>
/// Testes dos resultados de minimal API com o Problem Details do Tooark.
/// </summary>
[Collection("CultureSensitive")]
public class TooarkResultsTests
{
  // Monta uma notificação inválida com dois itens
  private static TestException InvalidNotification()
  {
    var notification = new TestException();
    notification.AddNotification(new("Field.Required;Nome", "Nome", "T.VLD.STR5"));
    notification.AddNotification(new("Field.Invalid;Email", "Email", "T.VLD.STR9"));

    return notification;
  }

  // Testa se a exceção vira um resultado no status dela, com o corpo em application/problem+json
  [Fact]
  public async Task Problem_ShouldWriteStatusOfException()
  {
    // Arrange
    Language.SetCulture("en-US");
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users/1");

    // Act
    var result = TooarkResults.Problem(new NotFoundException("User.NotFound"));
    await result.ExecuteAsync(context);

    // Assert
    Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    Assert.StartsWith("application/problem+json", context.Response.ContentType);

    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    var root = body.RootElement;
    Assert.Equal(404, root.GetProperty("status").GetInt32());
    Assert.Equal("Not Found", root.GetProperty("title").GetString());
    Assert.Equal("User.NotFound", root.GetProperty("detail").GetString());
    Assert.Equal("/users/1", root.GetProperty("instance").GetString());
    Assert.Contains("rfc9110", root.GetProperty("type").GetString());

    var error = Assert.Single(root.GetProperty("errors").EnumerateArray());
    Assert.Equal("T.ERR", error.GetProperty("code").GetString());
    Assert.Equal("Unknown", error.GetProperty("key").GetString());
    Assert.Equal("User.NotFound", error.GetProperty("message").GetString());
  }

  // Testa se os itens da notificação viram a lista de erros, traduzidos no idioma corrente
  [Fact]
  public async Task ValidationProblem_ShouldWriteErrorsOfNotification()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users");

    // Act
    var result = TooarkResults.ValidationProblem(InvalidNotification());
    await result.ExecuteAsync(context);

    // Assert
    Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    var root = body.RootElement;
    Assert.Equal(400, root.GetProperty("status").GetInt32());
    Assert.Equal("Bad Request", root.GetProperty("title").GetString());
    Assert.Equal("O campo Nome é obrigatório", root.GetProperty("detail").GetString());
    Assert.Equal("/users", root.GetProperty("instance").GetString());

    var errors = root.GetProperty("errors").EnumerateArray().ToList();
    Assert.Equal(2, errors.Count);
    Assert.Equal("T.VLD.STR5", errors[0].GetProperty("code").GetString());
    Assert.Equal("Nome", errors[0].GetProperty("key").GetString());
    Assert.Equal("O campo Nome é obrigatório", errors[0].GetProperty("message").GetString());
    Assert.Equal("Email", errors[1].GetProperty("key").GetString());
    Assert.Equal("O campo E-mail é inválido", errors[1].GetProperty("message").GetString());
  }

  // Testa se o status informado é respeitado quando a validação pede outro código, como o 422
  [Fact]
  public async Task ValidationProblem_ShouldUseStatusCode_WhenInformed()
  {
    // Arrange
    Language.SetCulture("en-US");
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users");

    // Act
    var result = TooarkResults.ValidationProblem(InvalidNotification(), StatusCodes.Status422UnprocessableEntity);
    await result.ExecuteAsync(context);

    // Assert
    Assert.Equal(StatusCodes.Status422UnprocessableEntity, context.Response.StatusCode);

    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    Assert.Equal(422, body.RootElement.GetProperty("status").GetInt32());
    Assert.Equal("Unprocessable Entity", body.RootElement.GetProperty("title").GetString());
  }

  // Testa se a sobrecarga com a lista de itens escreve o mesmo corpo da sobrecarga com a notificação
  [Fact]
  public async Task ValidationProblem_ShouldAcceptNotificationItems()
  {
    // Arrange
    Language.SetCulture("en-US");
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users");
    var items = new List<NotificationItem> { new("Field.Required;Nome", "Nome", "T.VLD.STR5") };

    // Act
    var result = TooarkResults.ValidationProblem(items);
    await result.ExecuteAsync(context);

    // Assert
    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    var error = Assert.Single(body.RootElement.GetProperty("errors").EnumerateArray());
    Assert.Equal("Field Nome is required", error.GetProperty("message").GetString());
  }

  // Testa se uma notificação válida gera o problema sem erros, deixando a checagem de IsValid com quem chama
  [Fact]
  public async Task ValidationProblem_ShouldWriteEmptyErrors_WhenNotificationIsValid()
  {
    // Arrange
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users");

    // Act
    var result = TooarkResults.ValidationProblem(new TestException());
    await result.ExecuteAsync(context);

    // Assert
    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    Assert.Empty(body.RootElement.GetProperty("errors").EnumerateArray());
    Assert.False(body.RootElement.TryGetProperty("detail", out _));
  }

  // Testa se o resultado expõe o status e o content type antes da execução, para filtros e testes de endpoint
  [Fact]
  public void ValidationProblem_ShouldExposeStatusCodeAndContentType()
  {
    // Arrange & Act
    var result = TooarkResults.ValidationProblem(InvalidNotification(), StatusCodes.Status409Conflict);

    // Assert
    Assert.Equal(StatusCodes.Status409Conflict, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
    Assert.Equal("application/problem+json", Assert.IsAssignableFrom<IContentTypeHttpResult>(result).ContentType);
    Assert.NotNull(Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
  }

  // Testa se um status que não é de erro é recusado: um Problem Details 200 não faz sentido
  [Theory]
  [InlineData(StatusCodes.Status200OK)]
  [InlineData(399)]
  [InlineData(600)]
  public void ValidationProblem_ShouldThrow_WhenStatusIsNotError(int statusCode)
  {
    // Arrange & Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => TooarkResults.ValidationProblem(InvalidNotification(), statusCode));
  }

  // Testa se os argumentos nulos são recusados em vez de gerar um problema vazio
  [Fact]
  public void Results_ShouldThrow_WhenArgumentIsNull()
  {
    // Arrange & Act & Assert
    Assert.Throws<ArgumentNullException>(() => TooarkResults.Problem(null!));
    Assert.Throws<ArgumentNullException>(() => TooarkResults.ValidationProblem((Notification)null!));
    Assert.Throws<ArgumentNullException>(() => TooarkResults.ValidationProblem((IEnumerable<NotificationItem>)null!));
  }
}
