using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Http.Injections;
using Tooark.Http.Interfaces;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes do envio que devolve a resposta nos cabeçalhos, sem carregar o corpo em memória.
/// </summary>
public class HttpStreamingTests
{
  // URI usada nas chamadas
  private const string Endereco = "https://api.exemplo.com/eventos";

  // Conteúdo cuja leitura nunca termina: os cabeçalhos chegam, o corpo fica parado até o cancelamento
  private sealed class CorpoParado : HttpContent
  {
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
      SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
      Task.Delay(Timeout.Infinite, cancellationToken);

    protected override bool TryComputeLength(out long length)
    {
      length = 0;

      return false;
    }
  }

  // Implementação escrita antes do envio em streaming existir: só os membros originais da interface
  private sealed class ServicoAntigo(HttpResponseMessage resposta) : IHttpClientService
  {
    public int Envios { get; private set; }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
      Envios++;

      return Task.FromResult(resposta);
    }

    public Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public Task PostAsync<TRequest>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public Task PutAsync<TRequest>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public Task<TResponse?> PatchAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public Task PatchAsync<TRequest>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public Task<TResponse?> DeleteAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();
  }

  // Testa se a resposta volta nos cabeçalhos, mesmo com um corpo que nunca termina
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task SendStreamingAsync_ShouldReturnOnHeaders_WithoutWaitingForTheBody(bool resiliencia)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new CorpoParado() });
    using var provider = HttpTestHost.WithHandler(handler, options => options.Resilience = resiliencia);
    using var request = new HttpRequestMessage(HttpMethod.Get, Endereco);

    // Act
    using var response = await provider.Service().SendStreamingAsync(request, TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(1, handler.Calls);
  }

  // Testa se o corpo pode ser lido depois do tempo limite da chamada, que termina nos cabeçalhos
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task SendStreamingAsync_ShouldLetTheBodyOutliveTheTotalTimeout(bool resiliencia)
  {
    // Arrange: os cabeçalhos e o primeiro evento saem na hora; o segundo, depois do tempo limite de um segundo
    await using var server = new LoopbackServer(async (target, stream, cancellationToken) =>
    {
      if (target == "/aquecimento")
      {
        await stream.WriteAsync(
          Encoding.UTF8.GetBytes("HTTP/1.1 204 No Content\r\nConnection: close\r\n\r\n"),
          cancellationToken);

        return;
      }

      await stream.WriteAsync(
        Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/event-stream\r\nConnection: close\r\n\r\ndata: primeiro\n\n"),
        cancellationToken);
      await stream.FlushAsync(cancellationToken);
      await Task.Delay(TimeSpan.FromMilliseconds(1600), cancellationToken);
      await stream.WriteAsync(Encoding.UTF8.GetBytes("data: segundo\n\n"), cancellationToken);
    });

    var services = new ServiceCollection();

    services.AddTooarkHttpClient(options =>
    {
      options.Resilience = resiliencia;
      options.TotalTimeoutSeconds = 1;
      options.AttemptTimeoutSeconds = 1;
      options.CircuitBreakerSamplingSeconds = 2;
      options.RetryCount = 0;
    });

    using var provider = services.BuildServiceProvider();

    // A primeira chamada paga a montagem do pipeline, para o segundo de tempo limite valer só para os cabeçalhos;
    // o resultado dela não importa para o teste
    await Record.ExceptionAsync(() =>
      provider.Service().DeleteAsync($"http://127.0.0.1:{server.Port}/aquecimento", TestContext.Current.CancellationToken));

    using var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{server.Port}/eventos");
    var relogio = Stopwatch.StartNew();

    // Act
    using var response = await provider.Service().SendStreamingAsync(request, TestContext.Current.CancellationToken);

    await using var corpo = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
    using var reader = new StreamReader(corpo, Encoding.UTF8);
    var texto = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    Assert.Equal("data: primeiro\n\ndata: segundo\n\n", texto);
    // A chamada tem um segundo de tempo limite, e a leitura do corpo só termina depois dele
    Assert.True(relogio.Elapsed > TimeSpan.FromMilliseconds(1500), $"A leitura terminou em {relogio.Elapsed}.");
  }

  // Testa se o status de erro não vira exceção, como no SendAsync
  [Fact]
  public async Task SendStreamingAsync_ShouldReturnErrorStatusWithoutThrowing()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, """{"erro":"limite"}"""));
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryCount = 0);
    using var request = new HttpRequestMessage(HttpMethod.Post, Endereco) { Content = new StringContent("{}") };

    // Act
    using var response = await provider.Service().SendStreamingAsync(request, TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    Assert.Equal("""{"erro":"limite"}""", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
  }

  // Testa se as falhas até os cabeçalhos viram as exceções do Tooark
  [Fact]
  public async Task SendStreamingAsync_ShouldMapTransportFailures()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryCount = 0);
    var service = provider.Service();
    using var request = new HttpRequestMessage(HttpMethod.Get, Endereco);
    using var arquivo = new HttpRequestMessage(HttpMethod.Get, "file:///etc/passwd");

    // Act
    var conexao = await Assert.ThrowsAsync<BadGatewayException>(() =>
      service.SendStreamingAsync(request, TestContext.Current.CancellationToken));
    var esquema = await Assert.ThrowsAsync<BadRequestException>(() =>
      service.SendStreamingAsync(arquivo, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("Http.ConnectionFailed", conexao.Message);
    Assert.Equal("Http.RequestUriInvalid", esquema.Message);
    await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendStreamingAsync(null!, TestContext.Current.CancellationToken));
  }

  // Testa se o tempo limite vale enquanto os cabeçalhos não chegam
  [Fact]
  public async Task SendStreamingAsync_ShouldThrowGatewayTimeout_WhenHeadersDoNotArrive()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(async (_, cancellationToken) =>
    {
      await Task.Delay(Timeout.Infinite, cancellationToken);

      return FakeHttpMessageHandler.Empty(HttpStatusCode.OK);
    });
    using var provider = HttpTestHost.WithHandler(handler, options =>
    {
      options.Resilience = false;
      options.TotalTimeoutSeconds = 1;
      options.AttemptTimeoutSeconds = 1;
    });
    using var request = new HttpRequestMessage(HttpMethod.Get, Endereco);

    // Act
    var ex = await Assert.ThrowsAsync<GatewayTimeoutException>(() =>
      provider.Service().SendStreamingAsync(request, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("Http.RequestTimeout", ex.Message);
  }

  // Testa se o cancelamento de quem chamou segue como cancelamento, e não como tempo limite
  [Fact]
  public async Task SendStreamingAsync_ShouldPropagateCallerCancellation()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(async (_, cancellationToken) =>
    {
      await Task.Delay(Timeout.Infinite, cancellationToken);

      return FakeHttpMessageHandler.Empty(HttpStatusCode.OK);
    });
    using var provider = HttpTestHost.WithHandler(handler);
    using var request = new HttpRequestMessage(HttpMethod.Get, Endereco);
    using var cancelamento = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

    cancelamento.CancelAfter(TimeSpan.FromMilliseconds(100));

    // Act & Assert
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
      provider.Service().SendStreamingAsync(request, cancelamento.Token));
  }

  // Testa se uma implementação que só tem os membros originais da interface atende o envio em streaming pelo SendAsync
  [Fact]
  public async Task SendStreamingAsync_ShouldFallBackToSendAsync_OnAnImplementationWithoutIt()
  {
    // Arrange
    using var resposta = FakeHttpMessageHandler.Json(HttpStatusCode.OK, """{"ok":true}""");
    var antigo = new ServicoAntigo(resposta);
    IHttpClientService service = antigo;
    using var request = new HttpRequestMessage(HttpMethod.Get, Endereco);

    // Act
    var response = await service.SendStreamingAsync(request, TestContext.Current.CancellationToken);

    // Assert
    Assert.Same(resposta, response);
    Assert.Equal(1, antigo.Envios);
  }
}
