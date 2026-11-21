using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Tooark.Exceptions;
using Tooark.Http.Exceptions;
using Tooark.Http.Options;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes do handler de resiliência configurado pelas opções: novas tentativas, circuito e limites aceitos.
/// </summary>
public class HttpResilienceTests
{
  // Corpo lido das respostas
  private sealed record Recibo(string Protocolo);

  // URI usada nas chamadas
  private const string Endereco = "https://api.exemplo.com/recibos";

  // Responde 503 nas primeiras chamadas e 200 depois
  private static FakeHttpMessageHandler FalhaAte(int falhas)
  {
    var chamadas = 0;

    return new FakeHttpMessageHandler(_ => Interlocked.Increment(ref chamadas) <= falhas
      ? FakeHttpMessageHandler.Empty(HttpStatusCode.ServiceUnavailable)
      : FakeHttpMessageHandler.Json(HttpStatusCode.OK, """{"protocolo":"ABC-1"}"""));
  }

  // Testa se o 503 é tentado de novo até a resposta de sucesso
  [Fact]
  public async Task Retry_ShouldRetryOnServiceUnavailable()
  {
    // Arrange
    var handler = FalhaAte(2);
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    var recibo = await provider.Service().GetAsync<Recibo>(Endereco, TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);
    Assert.Equal(3, handler.Calls);
  }

  // Testa se a falha que persiste é devolvida depois da primeira chamada mais as novas tentativas configuradas
  [Theory]
  [InlineData(3, 4)]
  [InlineData(1, 2)]
  [InlineData(0, 1)]
  public async Task Retry_ShouldStopAfterConfiguredAttempts(int retryCount, int chamadasEsperadas)
  {
    // Arrange
    var handler = FalhaAte(int.MaxValue);
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryCount = retryCount);

    // Act
    var ex = await Assert.ThrowsAsync<HttpRequestFailedException>(() =>
      provider.Service().GetAsync<Recibo>(Endereco, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.UpstreamStatusCode);
    Assert.Equal(chamadasEsperadas, handler.Calls);
  }

  // Testa se o POST, com a nova tentativa dele ligada, é tentado de novo e reenvia o mesmo corpo
  [Fact]
  public async Task Retry_ShouldRetryPostWithTheSameBody()
  {
    // Arrange
    var handler = FalhaAte(1);
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryNonIdempotentMethods = true);

    // Act
    await provider.Service().PostAsync(Endereco, new Recibo("ABC-1"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(2, handler.Calls);
    Assert.All(handler.Requests, request => Assert.Equal("""{"protocolo":"ABC-1"}""", request.Body));
  }

  // Testa se um erro do cliente (4xx) não é tentado de novo
  [Fact]
  public async Task Retry_ShouldNotRetryClientErrors()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.NotFound));
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    await Assert.ThrowsAsync<HttpRequestFailedException>(() =>
      provider.Service().GetAsync<Recibo>(Endereco, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal(1, handler.Calls);
  }

  // Testa se a resiliência desligada envia uma única vez
  [Fact]
  public async Task Resilience_ShouldSendOnce_WhenDisabled()
  {
    // Arrange
    var handler = FalhaAte(int.MaxValue);
    using var provider = HttpTestHost.WithHandler(handler, options => options.Resilience = false);

    // Act
    await Assert.ThrowsAsync<HttpRequestFailedException>(() =>
      provider.Service().GetAsync<Recibo>(Endereco, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal(1, handler.Calls);
  }

  // Testa se o circuito abre pela taxa de falhas e passa a recusar sem chamar o serviço
  [Fact]
  public async Task CircuitBreaker_ShouldOpenAndRejectWithoutCalling()
  {
    // Arrange
    var handler = FalhaAte(int.MaxValue);
    using var provider = HttpTestHost.WithHandler(handler, options =>
    {
      options.RetryCount = 0;
      options.CircuitBreakerMinimumThroughput = 2;
      options.CircuitBreakerFailureRatio = 0.5;
    });
    var service = provider.Service();
    var token = TestContext.Current.CancellationToken;

    // Act: as duas primeiras falhas enchem a janela e abrem o circuito
    await Assert.ThrowsAsync<HttpRequestFailedException>(() => service.GetAsync<Recibo>(Endereco, token));
    await Assert.ThrowsAsync<HttpRequestFailedException>(() => service.GetAsync<Recibo>(Endereco, token));
    var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => service.GetAsync<Recibo>(Endereco, token));

    // Assert
    Assert.Contains("Http.CircuitOpen", ex.GetErrorMessages());
    Assert.IsType<BrokenCircuitException>(ex.InnerException);
    Assert.Equal(2, handler.Calls);
  }

  // Limites que o Validate aceita e que o handler padrão de resiliência também precisa aceitar
  public static TheoryData<string> LimitesAceitos =>
  [
    "padrao", "menores-tempos", "maiores-tempos", "sem-tentativas", "maximo-de-tentativas", "circuito-minimo", "circuito-maximo"
  ];

  // Aplica o conjunto de limites às opções
  private static void Aplicar(string limites, HttpClientOptions options)
  {
    switch (limites)
    {
      case "menores-tempos":
        options.TotalTimeoutSeconds = 1;
        options.AttemptTimeoutSeconds = 1;
        options.CircuitBreakerSamplingSeconds = 2;
        options.CircuitBreakerBreakSeconds = 1;
        break;
      case "maiores-tempos":
        options.TotalTimeoutSeconds = HttpClientOptions.MaximumTimeoutSeconds;
        options.AttemptTimeoutSeconds = HttpClientOptions.MaximumTimeoutSeconds;
        options.CircuitBreakerSamplingSeconds = HttpClientOptions.MaximumTimeoutSeconds * 2;
        break;
      case "sem-tentativas":
        options.RetryCount = 0;
        options.RetryDelayMilliseconds = 0;
        break;
      case "maximo-de-tentativas":
        options.RetryCount = HttpClientOptions.MaximumRetryCount;
        options.RetryDelayMilliseconds = HttpClientOptions.MaximumRetryDelayMilliseconds;
        break;
      case "circuito-minimo":
        options.CircuitBreakerFailureRatio = 0;
        options.CircuitBreakerMinimumThroughput = HttpClientOptions.MinimumCircuitBreakerThroughput;
        break;
      case "circuito-maximo":
        options.CircuitBreakerFailureRatio = 1;
        options.CircuitBreakerMinimumThroughput = int.MaxValue;
        options.CircuitBreakerSamplingSeconds = HttpClientOptions.MaximumCircuitBreakerSeconds;
        options.CircuitBreakerBreakSeconds = HttpClientOptions.MaximumCircuitBreakerSeconds;
        break;
    }
  }

  // Testa se cada limite aceito pelo Validate é aceito pelo handler padrão de resiliência e chega a ele com o mesmo valor
  [Theory]
  [MemberData(nameof(LimitesAceitos))]
  public async Task Options_ShouldBeAcceptedByTheStandardHandler_AtEveryAcceptedBound(string limites)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, """{"protocolo":"ABC-1"}"""));
    using var provider = HttpTestHost.WithHandler(handler, options => Aplicar(limites, options));
    var options = provider.GetRequiredService<IOptions<HttpClientOptions>>().Value;

    // Act: a leitura das opções nomeadas roda a validação do handler, a mesma que ele faz na primeira requisição, e
    // lançaria OptionsValidationException em uma combinação que o Validate deixou passar
    var resilience = provider.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>().Get("Tooark-standard");

    // Assert
    Assert.Equal(TimeSpan.FromSeconds(options.TotalTimeoutSeconds), resilience.TotalRequestTimeout.Timeout);
    Assert.Equal(TimeSpan.FromSeconds(options.AttemptTimeoutSeconds), resilience.AttemptTimeout.Timeout);
    Assert.Equal(TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds), resilience.Retry.Delay);
    Assert.Equal(options.CircuitBreakerFailureRatio, resilience.CircuitBreaker.FailureRatio);
    Assert.Equal(options.CircuitBreakerMinimumThroughput, resilience.CircuitBreaker.MinimumThroughput);
    Assert.Equal(TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds), resilience.CircuitBreaker.SamplingDuration);
    Assert.Equal(TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds), resilience.CircuitBreaker.BreakDuration);

    if (options.RetryCount > 0)
    {
      Assert.Equal(options.RetryCount, resilience.Retry.MaxRetryAttempts);
    }

    // Uma requisição pelo pipeline real, menos com o tempo limite de um segundo, que uma máquina de testes
    // carregada pode estourar na primeira chamada
    if (options.TotalTimeoutSeconds > 1)
    {
      var recibo = await provider.Service().GetAsync<Recibo>(Endereco, TestContext.Current.CancellationToken);

      Assert.Equal("ABC-1", recibo?.Protocolo);
    }
  }
}
