using System.Net;
using Microsoft.Extensions.Configuration;
using Tooark.Exceptions;
using Tooark.Http.Exceptions;
using Tooark.Http.Extensions;
using Tooark.Http.Interfaces;
using Tooark.Http.Options;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes das novas tentativas por verbo: POST e PATCH seguem a opção, os verbos idempotentes são sempre tentados.
/// </summary>
/// <remarks>
/// Cada teste informa a opção de forma explícita, porque o padrão dela muda entre a 4.x e a v5.
/// </remarks>
public class HttpRetryByVerbTests
{
  // Corpo enviado e lido nas chamadas
  private sealed record Recibo(string Protocolo);

  // URI usada nas chamadas
  private const string Endereco = "https://api.exemplo.com/recibos";

  // Corpo padrão das chamadas
  private static readonly Recibo Corpo = new("ABC-1");

  // Responde 503 nas primeiras chamadas e 200 depois
  private static FakeHttpMessageHandler FalhaAte(int falhas)
  {
    var chamadas = 0;

    return new FakeHttpMessageHandler(_ => Interlocked.Increment(ref chamadas) <= falhas
      ? FakeHttpMessageHandler.Empty(HttpStatusCode.ServiceUnavailable)
      : FakeHttpMessageHandler.Json(HttpStatusCode.OK, """{"protocolo":"ABC-1"}"""));
  }

  // Falha na conexão nas primeiras chamadas e responde 200 depois
  private static FakeHttpMessageHandler ConexaoFalhaAte(int falhas)
  {
    var chamadas = 0;

    return new FakeHttpMessageHandler(_ => Interlocked.Increment(ref chamadas) <= falhas
      ? throw new HttpRequestException("Connection refused")
      : FakeHttpMessageHandler.Json(HttpStatusCode.OK, """{"protocolo":"ABC-1"}"""));
  }

  // Envia pelo verbo informado, pelos métodos da interface
  private static Task EnviarAsync(IHttpClientService service, string verbo) => verbo switch
  {
    "GET" => service.GetAsync<Recibo>(Endereco, TestContext.Current.CancellationToken),
    "POST" => service.PostAsync(Endereco, Corpo, TestContext.Current.CancellationToken),
    "PUT" => service.PutAsync(Endereco, Corpo, TestContext.Current.CancellationToken),
    "PATCH" => service.PatchAsync(Endereco, Corpo, TestContext.Current.CancellationToken),
    _ => service.DeleteAsync(Endereco, TestContext.Current.CancellationToken)
  };

  // Testa se, com a opção desligada, POST e PATCH não são tentados de novo em uma resposta 5xx
  [Theory]
  [InlineData("POST")]
  [InlineData("PATCH")]
  public async Task NonIdempotentVerbs_ShouldNotBeRetriedOnServerError_WhenOptionIsOff(string verbo)
  {
    // Arrange
    var handler = FalhaAte(1);
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryNonIdempotentMethods = false);

    // Act
    var ex = await Assert.ThrowsAsync<HttpRequestFailedException>(() => EnviarAsync(provider.Service(), verbo));

    // Assert
    Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.UpstreamStatusCode);
    Assert.Equal(1, handler.Calls);
  }

  // Testa se, com a opção desligada, POST e PATCH não são tentados de novo em uma falha de conexão, que não tem resposta
  [Theory]
  [InlineData("POST")]
  [InlineData("PATCH")]
  public async Task NonIdempotentVerbs_ShouldNotBeRetriedOnConnectionFailure_WhenOptionIsOff(string verbo)
  {
    // Arrange
    var handler = ConexaoFalhaAte(1);
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryNonIdempotentMethods = false);

    // Act
    var ex = await Assert.ThrowsAsync<BadGatewayException>(() => EnviarAsync(provider.Service(), verbo));

    // Assert
    Assert.Equal("Http.ConnectionFailed", ex.Message);
    Assert.Equal(1, handler.Calls);
  }

  // Testa se, com a opção desligada, os verbos idempotentes continuam sendo tentados de novo, nas duas falhas
  [Theory]
  [InlineData("GET", true)]
  [InlineData("PUT", true)]
  [InlineData("DELETE", true)]
  [InlineData("GET", false)]
  [InlineData("PUT", false)]
  [InlineData("DELETE", false)]
  public async Task IdempotentVerbs_ShouldStillBeRetried_WhenOptionIsOff(string verbo, bool falhaDeConexao)
  {
    // Arrange
    var handler = falhaDeConexao ? ConexaoFalhaAte(2) : FalhaAte(2);
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryNonIdempotentMethods = false);

    // Act
    await EnviarAsync(provider.Service(), verbo);

    // Assert
    Assert.Equal(3, handler.Calls);
  }

  // Testa se, com a opção ligada, POST e PATCH são tentados de novo e reenviam o mesmo corpo
  [Theory]
  [InlineData("POST")]
  [InlineData("PATCH")]
  public async Task NonIdempotentVerbs_ShouldBeRetried_WhenOptionIsOn(string verbo)
  {
    // Arrange
    var handler = FalhaAte(2);
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryNonIdempotentMethods = true);

    // Act
    await EnviarAsync(provider.Service(), verbo);

    // Assert
    Assert.Equal(3, handler.Calls);
    Assert.All(handler.Requests, request => Assert.Equal("""{"protocolo":"ABC-1"}""", request.Body));
  }

  // Testa se a opção vale também para a requisição montada por quem chama e para os métodos com cabeçalho por chamada
  [Fact]
  public async Task Option_ShouldApplyToSendAsyncAndToTheExtensionMethods()
  {
    // Arrange
    var montada = FalhaAte(1);
    var extensao = FalhaAte(1);
    using var comMontada = HttpTestHost.WithHandler(montada, options => options.RetryNonIdempotentMethods = false);
    using var comExtensao = HttpTestHost.WithHandler(extensao, options => options.RetryNonIdempotentMethods = false);
    using var request = new HttpRequestMessage(HttpMethod.Post, Endereco) { Content = new StringContent("{}") };

    // Act
    using var response = await comMontada.Service().SendAsync(request, TestContext.Current.CancellationToken);

    await Assert.ThrowsAsync<HttpRequestFailedException>(() => comExtensao.Service().PostAsync(
      Endereco,
      Corpo,
      mensagem => mensagem.Headers.Add("Idempotency-Key", "abc"),
      TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    Assert.Equal(1, montada.Calls);
    Assert.Equal(1, extensao.Calls);
  }

  // Testa se zero novas tentativas continua desligando tudo, com a opção em qualquer valor
  [Theory]
  [InlineData("GET", true)]
  [InlineData("GET", false)]
  [InlineData("POST", true)]
  [InlineData("POST", false)]
  public async Task RetryCountZero_ShouldDisableRetriesForEveryVerb(string verbo, bool opcao)
  {
    // Arrange
    var handler = FalhaAte(1);
    using var provider = HttpTestHost.WithHandler(handler, options =>
    {
      options.RetryCount = 0;
      options.RetryNonIdempotentMethods = opcao;
    });

    // Act
    await Assert.ThrowsAsync<HttpRequestFailedException>(() => EnviarAsync(provider.Service(), verbo));

    // Assert
    Assert.Equal(1, handler.Calls);
  }

  // Testa se a falha do POST que não é tentado de novo continua contando para o circuito
  [Fact]
  public async Task NonIdempotentFailures_ShouldStillCountInTheCircuit()
  {
    // Arrange
    var handler = FalhaAte(int.MaxValue);
    using var provider = HttpTestHost.WithHandler(handler, options =>
    {
      options.RetryNonIdempotentMethods = false;
      options.CircuitBreakerMinimumThroughput = 2;
      options.CircuitBreakerFailureRatio = 0.5;
      options.CircuitBreakerBreakSeconds = 30;
    });
    var service = provider.Service();

    // Act: as falhas enchem a janela até o circuito abrir
    Exception? ultima = null;

    for (var tentativa = 0; tentativa < 10 && ultima is not ServiceUnavailableException; tentativa++)
    {
      ultima = await Record.ExceptionAsync(() => EnviarAsync(service, "POST"));
    }

    // Assert
    var aberto = Assert.IsType<ServiceUnavailableException>(ultima);
    Assert.Equal("Http.CircuitOpen", aberto.Message);
  }

  // Testa se a opção informada na configuração é lida, nos dois valores
  [Theory]
  [InlineData("true", true)]
  [InlineData("false", false)]
  public void Option_ShouldBindFromConfiguration(string valor, bool esperado)
  {
    // Arrange
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?> { ["HttpClient:RetryNonIdempotentMethods"] = valor })
      .Build();
    var options = new HttpClientOptions();

    // Act
    configuration.GetSection(HttpClientOptions.Section).Bind(options);

    // Assert
    Assert.Equal(esperado, options.RetryNonIdempotentMethods);
  }
}
