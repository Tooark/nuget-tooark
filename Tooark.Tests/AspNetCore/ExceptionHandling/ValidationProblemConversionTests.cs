using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.AspNetCore.ExceptionHandling;
using Tooark.AspNetCore.Injections;
using Tooark.Dtos;
using Tooark.Utils;

namespace Tooark.Tests.AspNetCore.ExceptionHandling;

[Collection("CultureSensitive")]
public class ValidationProblemConversionTests
{
  // Controller mínimo para o ValidationProblem() do MVC
  private sealed class UsersController : ControllerBase
  {
  }

  // Monta o container como a aplicação monta; sem informar a conversão, vale o padrão
  private static ServiceProvider CreateProvider(bool? convert, Action<IServiceCollection>? configure = null)
  {
    Language.SetCulture("en-US");

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddControllers();
    services.AddTooarkProblemDetails();

    if (convert is { } value)
    {
      services.Configure<TooarkProblemDetailsOptions>(options => options.ConvertValidationProblems = value);
    }

    configure?.Invoke(services);

    return services.BuildServiceProvider();
  }

  // ModelState com dois erros no mesmo campo, um erro sem texto (o ASP.NET Core o troca pelo texto padrão dele)
  // e um erro sem campo
  private static ModelStateDictionary InvalidModelState()
  {
    var modelState = new ModelStateDictionary();
    modelState.AddModelError("Name", "Name is mandatory");
    modelState.AddModelError("Name", "Field.Invalid;Name");
    modelState.AddModelError("Age", string.Empty);
    modelState.AddModelError(string.Empty, "The request body is invalid.");

    return modelState;
  }

  // Personalização da aplicação, que precisa chegar ao corpo convertido
  private static void AddAppCustomization(IServiceCollection services) =>
    services.AddTooarkProblemDetails(options =>
      options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["application"] = "orders-api");

  // Contexto da action com o ModelState informado
  private static ActionContext CreateActionContext(IServiceProvider provider, ModelStateDictionary modelState)
  {
    var httpContext = ProblemDetailsTestHelper.CreateContext(provider, "/users");

    return new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), modelState);
  }

  // Roda os filtros de resultado globais, como o MVC faz, e escreve o resultado pelos formatadores
  private static async Task<JsonDocument> ExecuteAsync(IServiceProvider provider, ActionContext actionContext, IActionResult result)
  {
    var filters = provider.GetRequiredService<IOptions<MvcOptions>>().Value.Filters.OfType<IAlwaysRunResultFilter>().ToList();
    var resultContext = new ResultExecutingContext(actionContext, [.. filters], result, new object());

    foreach (var filter in filters)
    {
      filter.OnResultExecuting(resultContext);
    }

    await resultContext.Result.ExecuteResultAsync(actionContext);

    return await ProblemDetailsTestHelper.ReadBodyAsync(actionContext.HttpContext);
  }

  // Escreve um problema de validação pelo serviço de Problem Details, como a validação das minimal APIs
  private static async Task<JsonDocument> WriteWithServiceAsync(IServiceProvider provider)
  {
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users");
    context.Response.StatusCode = StatusCodes.Status400BadRequest;

    await provider.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
    {
      HttpContext = context,
      ProblemDetails = new HttpValidationProblemDetails(new Dictionary<string, string[]>
      {
        ["Email"] = ["The Email field is required."]
      })
    });

    return await ProblemDetailsTestHelper.ReadBodyAsync(context);
  }

  // Confere a lista do Tooark gerada pelo InvalidModelState
  private static void AssertTooarkList(JsonElement root)
  {
    var errors = root.GetProperty("errors");
    Assert.Equal(JsonValueKind.Array, errors.ValueKind);

    var items = errors.EnumerateArray()
      .Select(item => (
        Code: item.GetProperty("code").GetString(),
        Key: item.GetProperty("key").GetString(),
        Message: item.GetProperty("message").GetString()))
      .ToList();

    Assert.Equal(4, items.Count);
    Assert.All(items, item => Assert.Equal("T.ERR", item.Code));
    Assert.Contains(("T.ERR", "Name", "Name is mandatory"), items);
    Assert.Contains(("T.ERR", "Name", "Field Name is invalid"), items);
    Assert.Contains(("T.ERR", "Age", "The input was not valid."), items);
    Assert.Contains(("T.ERR", "Unknown", "The request body is invalid."), items);
  }

  // Testa se a validação automática do [ApiController] responde a lista do Tooark por padrão, mantendo o type, o
  // traceId e a personalização da aplicação
  [Fact]
  public async Task AutomaticValidation_ShouldRespondWithTheTooarkList_ByDefault()
  {
    // Arrange
    await using var provider = CreateProvider(convert: null, AddAppCustomization);
    var actionContext = CreateActionContext(provider, InvalidModelState());
    var result = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value.InvalidModelStateResponseFactory(actionContext);

    // Act
    using var body = await ExecuteAsync(provider, actionContext, result);

    // Assert
    var root = body.RootElement;
    Assert.Equal(StatusCodes.Status400BadRequest, actionContext.HttpContext.Response.StatusCode);
    Assert.Equal(400, root.GetProperty("status").GetInt32());
    Assert.Equal("Bad Request", root.GetProperty("title").GetString());
    Assert.Equal("/users", root.GetProperty("instance").GetString());
    Assert.Contains("rfc9110", root.GetProperty("type").GetString());
    Assert.True(root.TryGetProperty("traceId", out _));
    Assert.Equal("orders-api", root.GetProperty("application").GetString());
    Assert.Equal(root.GetProperty("errors")[0].GetProperty("message").GetString(), root.GetProperty("detail").GetString());
    AssertTooarkList(root);
  }

  // Testa se, com a conversão desligada, a validação automática mantém o formato do ASP.NET Core
  [Fact]
  public async Task AutomaticValidation_ShouldKeepTheAspNetFormat_WhenDisabled()
  {
    // Arrange
    await using var provider = CreateProvider(convert: false);
    var actionContext = CreateActionContext(provider, InvalidModelState());
    var result = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value.InvalidModelStateResponseFactory(actionContext);

    // Act
    using var body = await ExecuteAsync(provider, actionContext, result);

    // Assert
    var errors = body.RootElement.GetProperty("errors");
    Assert.Equal(JsonValueKind.Object, errors.ValueKind);
    Assert.Equal(2, errors.GetProperty("Name").GetArrayLength());
  }

  // Testa se o ValidationProblem() de um controller é convertido, com a personalização da aplicação
  [Fact]
  public async Task ControllerValidationProblem_ShouldBeConverted_WhenEnabled()
  {
    // Arrange
    await using var provider = CreateProvider(convert: true, AddAppCustomization);
    var actionContext = CreateActionContext(provider, InvalidModelState());
    var controller = new UsersController { ControllerContext = new ControllerContext(actionContext) };

    // Act
    using var body = await ExecuteAsync(provider, actionContext, controller.ValidationProblem(actionContext.ModelState));

    // Assert
    AssertTooarkList(body.RootElement);
    Assert.Equal("orders-api", body.RootElement.GetProperty("application").GetString());
  }

  // Testa se o envelope continua respondendo a validação automática com o ResponseDto, com a conversão ligada
  [Fact]
  public async Task AutomaticValidation_ShouldKeepTheEnvelope_WhenBothAreRegistered()
  {
    // Arrange
    await using var provider = CreateProvider(convert: true, services => services.AddTooarkModelStateEnvelope());
    var actionContext = CreateActionContext(provider, InvalidModelState());
    var result = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value.InvalidModelStateResponseFactory(actionContext);

    // Act
    using var body = await ExecuteAsync(provider, actionContext, result);

    // Assert
    Assert.IsType<ResponseDto<object>>(Assert.IsType<BadRequestObjectResult>(result).Value);
    var errors = body.RootElement.GetProperty("errors");
    Assert.Equal(JsonValueKind.Array, errors.ValueKind);
    Assert.Equal(JsonValueKind.String, errors[0].ValueKind);
  }

  // Testa se o problema de validação escrito pelo serviço de Problem Details (validação das minimal APIs) é
  // convertido, e se o CustomizeProblemDetails da aplicação vale para o corpo convertido
  [Fact]
  public async Task ProblemDetailsService_ShouldConvert_AndKeepTheAppCustomization()
  {
    // Arrange
    await using var provider = CreateProvider(convert: true, AddAppCustomization);

    // Act
    using var body = await WriteWithServiceAsync(provider);

    // Assert
    var root = body.RootElement;
    Assert.Equal("orders-api", root.GetProperty("application").GetString());
    Assert.Equal("/users", root.GetProperty("instance").GetString());

    var error = Assert.Single(root.GetProperty("errors").EnumerateArray());
    Assert.Equal("T.ERR", error.GetProperty("code").GetString());
    Assert.Equal("Email", error.GetProperty("key").GetString());
    Assert.Equal("The Email field is required.", error.GetProperty("message").GetString());
  }

  // Testa se, com a conversão desligada, o serviço de Problem Details mantém o formato do ASP.NET Core
  [Fact]
  public async Task ProblemDetailsService_ShouldKeepTheAspNetFormat_WhenDisabled()
  {
    // Arrange
    await using var provider = CreateProvider(convert: false);

    // Act
    using var body = await WriteWithServiceAsync(provider);

    // Assert
    Assert.Equal(JsonValueKind.Object, body.RootElement.GetProperty("errors").ValueKind);
  }

  // Testa se a conversão registrada duas vezes não roda duas vezes nem repete o filtro
  [Fact]
  public async Task AddTooarkProblemDetails_Twice_ShouldRegisterTheConversionOnce()
  {
    // Arrange
    await using var provider = CreateProvider(convert: true, services => services.AddTooarkProblemDetails());

    // Act
    var filters = provider.GetRequiredService<IOptions<MvcOptions>>().Value.Filters.OfType<IAlwaysRunResultFilter>();
    using var body = await WriteWithServiceAsync(provider);

    // Assert
    Assert.Single(filters);
    Assert.Single(body.RootElement.GetProperty("errors").EnumerateArray());
  }

  // Testa se o Results.ValidationProblem das minimal APIs, que passa pelo serviço de Problem Details, é convertido
  [Fact]
  public async Task ResultsValidationProblem_ShouldBeConverted()
  {
    // Arrange
    await using var provider = CreateProvider(convert: true);
    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users");
    var result = TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["Email"] = ["Field.Required;Email"] });

    // Act
    await result.ExecuteAsync(context);
    using var body = await ProblemDetailsTestHelper.ReadBodyAsync(context);

    // Assert
    var error = Assert.Single(body.RootElement.GetProperty("errors").EnumerateArray());
    Assert.Equal("Field Email is required", error.GetProperty("message").GetString());
  }
}
