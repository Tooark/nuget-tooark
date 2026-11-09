using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tooark.AspNetCore.ExceptionHandling;
using Tooark.AspNetCore.Injections;
using Tooark.Exceptions;
using Tooark.Tests.Moq.Notifications;
using Tooark.Utils;

namespace Tooark.Tests.AspNetCore.ExceptionHandling;

/// <summary>
/// Testes do manipulador que responde as exceções do Tooark com Problem Details.
/// </summary>
[Collection("CultureSensitive")]
public class TooarkExceptionHandlerTests
{
  // Resolve o manipulador registrado pelo AddTooarkProblemDetails
  private static IExceptionHandler ResolveHandler(IServiceProvider provider) =>
    Assert.Single(provider.GetServices<IExceptionHandler>().OfType<TooarkExceptionHandler>());

  // Testa se uma exceção que não é do Tooark é devolvida ao tratamento padrão do ASP.NET Core
  [Fact]
  public async Task TryHandleAsync_ShouldReturnFalse_WhenExceptionIsNotTooark()
  {
    // Arrange
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider);
    var handler = ResolveHandler(provider);

    // Act
    var handled = await handler.TryHandleAsync(context, new InvalidOperationException("x"), TestContext.Current.CancellationToken);

    // Assert
    Assert.False(handled);
    Assert.Equal(0, context.Response.Body.Length);
  }

  // Testa se a exceção de não encontrado vira um 404 em application/problem+json com a lista de erros
  [Fact]
  public async Task TryHandleAsync_ShouldWriteProblemDetails_WhenNotFoundException()
  {
    // Arrange
    Language.SetCulture("en-US");
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users/1", "application/json");
    var handler = ResolveHandler(provider);

    // Act
    var handled = await handler.TryHandleAsync(context, new NotFoundException("User.NotFound"), TestContext.Current.CancellationToken);

    // Assert
    Assert.True(handled);
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

  // Testa se o código e a chave de cada item de notificação chegam ao corpo com a mensagem traduzida
  [Fact]
  public async Task TryHandleAsync_ShouldTranslateMessages_WhenExceptionCarriesNotification()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users", "application/json");
    var handler = ResolveHandler(provider);

    var notification = new TestException();
    notification.AddNotification(new("Field.Required;Nome", "Nome", "T.VLD.STR5"));
    notification.AddNotification(new("Field.Invalid;Email", "Email", "T.VLD.STR9"));

    // Act
    var handled = await handler.TryHandleAsync(context, new BadRequestException(notification), TestContext.Current.CancellationToken);

    // Assert
    Assert.True(handled);
    Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    var root = body.RootElement;
    Assert.Equal("O campo Nome é obrigatório", root.GetProperty("detail").GetString());

    var errors = root.GetProperty("errors").EnumerateArray().ToList();
    Assert.Equal(2, errors.Count);
    Assert.Equal("T.VLD.STR5", errors[0].GetProperty("code").GetString());
    Assert.Equal("Nome", errors[0].GetProperty("key").GetString());
    Assert.Equal("O campo Nome é obrigatório", errors[0].GetProperty("message").GetString());
    Assert.Equal("T.VLD.STR9", errors[1].GetProperty("code").GetString());
    Assert.Equal("Email", errors[1].GetProperty("key").GetString());
    Assert.Equal("O campo E-mail é inválido", errors[1].GetProperty("message").GetString());
  }

  // Testa se o corpo sai mesmo quando o Accept não aceita JSON e o IProblemDetailsService recusa a escrita
  [Fact]
  public async Task TryHandleAsync_ShouldWriteBody_WhenAcceptDoesNotAllowJson()
  {
    // Arrange
    Language.SetCulture("en-US");
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/orders/7", "text/html");
    var handler = ResolveHandler(provider);

    // Act
    var handled = await handler.TryHandleAsync(context, new ConflictException("Record.Exists"), TestContext.Current.CancellationToken);

    // Assert
    Assert.True(handled);
    Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    Assert.StartsWith("application/problem+json", context.Response.ContentType);

    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    var root = body.RootElement;
    Assert.Equal(409, root.GetProperty("status").GetInt32());
    Assert.Equal("Conflict", root.GetProperty("title").GetString());
    Assert.Equal("Record already exists.", root.GetProperty("detail").GetString());
    Assert.Equal("/orders/7", root.GetProperty("instance").GetString());
    Assert.Contains("rfc9110", root.GetProperty("type").GetString());
    Assert.Single(root.GetProperty("errors").EnumerateArray());
  }

  // Testa se a requisição sem header Accept recebe o corpo: o cliente mais comum de API não envia o header
  [Fact]
  public async Task TryHandleAsync_ShouldWriteBody_WhenAcceptHeaderIsMissing()
  {
    // Arrange
    Language.SetCulture("en-US");
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users/1");
    var handler = ResolveHandler(provider);

    // Act
    var handled = await handler.TryHandleAsync(context, new NotFoundException("NotFound.Data"), TestContext.Current.CancellationToken);

    // Assert
    Assert.True(handled);
    Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    Assert.Equal("Data not found", body.RootElement.GetProperty("detail").GetString());
  }

  // Testa se o manipulador funciona sem o IProblemDetailsService e sem o localizador no container
  [Fact]
  public async Task TryHandleAsync_ShouldWriteBody_WhenProblemDetailsServiceIsMissing()
  {
    // Arrange
    Language.SetCulture("en-US");
    var services = new ServiceCollection();
    services.AddLogging();
    using var provider = services.BuildServiceProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users/1", "application/json");
    var handler = new TooarkExceptionHandler();

    // Act
    var handled = await handler.TryHandleAsync(context, new UnauthorizedException("Unauthorized"), TestContext.Current.CancellationToken);

    // Assert
    Assert.True(handled);
    Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    Assert.StartsWith("application/problem+json", context.Response.ContentType);

    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    var root = body.RootElement;
    Assert.Equal("Unauthorized", root.GetProperty("title").GetString());
    Assert.Equal("Not authenticated", root.GetProperty("detail").GetString());
  }

  // Testa se uma exceção do Tooark de servidor também responde com o corpo, no status da exceção
  [Fact]
  public async Task TryHandleAsync_ShouldWriteStatusOfException_WhenServerError()
  {
    // Arrange
    Language.SetCulture("en-US");
    using var provider = ProblemDetailsTestHelper.CreateProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/health", "application/json");
    var handler = ResolveHandler(provider);

    // Act
    var handled = await handler.TryHandleAsync(context, new ServiceUnavailableException("ServiceUnavailable"), TestContext.Current.CancellationToken);

    // Assert
    Assert.True(handled);
    Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);

    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    Assert.Equal(503, body.RootElement.GetProperty("status").GetInt32());
    Assert.Equal("Service Unavailable", body.RootElement.GetProperty("title").GetString());
  }

  // Testa se o CustomizeProblemDetails da aplicação é aplicado ao corpo escrito pelo manipulador
  [Fact]
  public async Task TryHandleAsync_ShouldApplyCustomizeProblemDetails()
  {
    // Arrange
    Language.SetCulture("en-US");
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddTooarkProblemDetails(options =>
      options.CustomizeProblemDetails = problem => problem.ProblemDetails.Extensions["application"] = "orders-api");
    using var provider = services.BuildServiceProvider();
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users/1", "application/json");
    var handler = ResolveHandler(provider);

    // Act
    await handler.TryHandleAsync(context, new NotFoundException("NotFound"), TestContext.Current.CancellationToken);

    // Assert
    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);
    Assert.Equal("orders-api", body.RootElement.GetProperty("application").GetString());
    Assert.Equal(JsonValueKind.Array, body.RootElement.GetProperty("errors").ValueKind);
  }
}
