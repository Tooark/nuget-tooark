using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tooark.AspNetCore.ExceptionHandling;
using Tooark.AspNetCore.Injections;
using Tooark.Exceptions;
using Tooark.Utils;

namespace Tooark.Tests.AspNetCore.ExceptionHandling;

/// <summary>
/// Testes do que o pipeline registra no log quando uma exceção do Tooark é respondida.
/// </summary>
[Collection("CultureSensitive")]
public class TooarkExceptionHandlerLoggingTests
{
  // Categoria do logger do manipulador
  private static readonly string HandlerCategory = typeof(TooarkExceptionHandler).FullName!;

#if !NET10_0_OR_GREATER
  // Categoria do logger do UseExceptionHandler
  private const string MiddlewareCategory = "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware";
#endif

  // Monta o pipeline como a aplicação monta, com o UseExceptionHandler e um endpoint que lança a exceção
  private static async Task<(CapturingLoggerProvider Capture, HttpContext Context)> RunAsync(
    Exception exception,
    Action<IServiceCollection>? configure = null
  )
  {
    Language.SetCulture("en-US");

    var capture = new CapturingLoggerProvider();
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddProvider(capture).SetMinimumLevel(LogLevel.Trace));
    services.AddTooarkProblemDetails();
    services.AddSingleton(new DiagnosticListener("Tooark.Tests"));
    services.AddMetrics();
    configure?.Invoke(services);
    await using var provider = services.BuildServiceProvider();

    var app = new ApplicationBuilder(provider);
    app.UseExceptionHandler();
    app.Run(_ => throw exception);
    var pipeline = app.Build();

    var context = ProblemDetailsTestHelper.CreateContext(provider, "/users/1", "application/json");
    await pipeline(context);

    return (capture, context);
  }

  // Testa se um erro do cliente sai em Warning por padrão, o nível de antes das opções, sem a pilha
  [Theory]
  [InlineData(StatusCodes.Status404NotFound)]
  [InlineData(StatusCodes.Status409Conflict)]
  public async Task ClientError_ShouldBeLoggedAtWarning_ByDefault(int statusCode)
  {
    // Arrange
    TooarkException exception = statusCode == StatusCodes.Status404NotFound
      ? new NotFoundException("User.NotFound")
      : new ConflictException("Record.Exists");

    // Act
    var (capture, context) = await RunAsync(exception);

    // Assert
    Assert.Equal(statusCode, context.Response.StatusCode);

    var entry = Assert.Single(capture.Entries, e => e.Category == HandlerCategory);
    Assert.Equal(LogLevel.Warning, entry.Level);
    Assert.Null(entry.Exception);
    Assert.Contains($"status {statusCode}", entry.Message);
  }

  // Testa se um erro do servidor sai em Error, com a exceção anexada
  [Fact]
  public async Task ServerError_ShouldBeLoggedAtError_WithException()
  {
    // Arrange
    var exception = new ServiceUnavailableException("ServiceUnavailable");

    // Act
    var (capture, _) = await RunAsync(exception);

    // Assert
    var entry = Assert.Single(capture.Entries, e => e.Category == HandlerCategory);
    Assert.Equal(LogLevel.Error, entry.Level);
    Assert.Same(exception, entry.Exception);
  }

#if NET10_0_OR_GREATER
  // Testa se, no .NET 10, o UseExceptionHandler não registra outra entrada para a exceção tratada: o log do
  // manipulador é o único
  [Fact]
  public async Task HandledException_ShouldHaveNoOtherEntry()
  {
    // Act
    var (capture, _) = await RunAsync(new NotFoundException("User.NotFound"));

    // Assert
    var entry = Assert.Single(capture.Entries, e => e.Level >= LogLevel.Information);
    Assert.Equal(HandlerCategory, entry.Category);
  }
#else
  // Testa se, no .NET 8, o UseExceptionHandler registra a exceção como Error antes de chamar o manipulador,
  // independente do nível do Tooark
  [Fact]
  public async Task HandledException_ShouldAlsoBeLoggedByTheMiddleware()
  {
    // Act
    var (capture, _) = await RunAsync(new NotFoundException("User.NotFound"), services =>
      services.Configure<TooarkProblemDetailsOptions>(options => options.ClientErrorLogLevel = LogLevel.Information));

    // Assert
    var entry = Assert.Single(capture.Entries, e => e.Category != HandlerCategory && e.Level >= LogLevel.Information);
    Assert.Equal(MiddlewareCategory, entry.Category);
    Assert.Equal(LogLevel.Error, entry.Level);
  }
#endif

  // Testa se os níveis das faixas e o nível por status são configuráveis, e se None não registra nada
  [Theory]
  [InlineData(StatusCodes.Status404NotFound, LogLevel.Debug)]
  [InlineData(StatusCodes.Status401Unauthorized, LogLevel.Warning)]
  [InlineData(StatusCodes.Status409Conflict, LogLevel.Trace)]
  [InlineData(StatusCodes.Status503ServiceUnavailable, LogLevel.Critical)]
  public async Task Options_ShouldSetTheLogLevel(int statusCode, LogLevel expected)
  {
    // Arrange
    TooarkException exception = statusCode switch
    {
      StatusCodes.Status404NotFound => new NotFoundException("User.NotFound"),
      StatusCodes.Status401Unauthorized => new UnauthorizedException("Unauthorized"),
      StatusCodes.Status409Conflict => new ConflictException("Record.Exists"),
      _ => new ServiceUnavailableException("ServiceUnavailable")
    };

    // Act
    var (capture, _) = await RunAsync(exception, services => services.Configure<TooarkProblemDetailsOptions>(options =>
    {
      options.ClientErrorLogLevel = LogLevel.Trace;
      options.ServerErrorLogLevel = LogLevel.Critical;
      options.StatusCodeLogLevels[StatusCodes.Status404NotFound] = LogLevel.Debug;
      options.StatusCodeLogLevels[StatusCodes.Status401Unauthorized] = LogLevel.Warning;
    }));

    // Assert
    var entry = Assert.Single(capture.Entries, e => e.Category == HandlerCategory);
    Assert.Equal(expected, entry.Level);
  }

  // Testa se o nível None tira o status do log
  [Fact]
  public async Task Options_ShouldSkipTheLog_WhenLevelIsNone()
  {
    // Act
    var (capture, context) = await RunAsync(new NotFoundException("User.NotFound"), services =>
      services.Configure<TooarkProblemDetailsOptions>(options => options.StatusCodeLogLevels[StatusCodes.Status404NotFound] = LogLevel.None));

    // Assert
    Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    Assert.DoesNotContain(capture.Entries, e => e.Category == HandlerCategory);
  }

  // Testa se as opções padrão mantêm o comportamento de antes delas: Warning para 4xx e a conversão desligada
  [Fact]
  public void DefaultOptions_ShouldKeepThePreviousBehavior()
  {
    // Arrange
    var options = new TooarkProblemDetailsOptions();

    // Assert
    Assert.Equal(LogLevel.Warning, options.ClientErrorLogLevel);
    Assert.Equal(LogLevel.Error, options.ServerErrorLogLevel);
    Assert.Empty(options.StatusCodeLogLevels);
    Assert.False(options.ConvertValidationProblems);
  }
}
