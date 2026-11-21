using System.Net;
using System.Net.Http.Headers;
using Tooark.Exceptions;
using Tooark.Http.Exceptions;
using Tooark.Http.Extensions;
using Tooark.Http.Interfaces;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes dos métodos por verbo com ajuste da requisição por chamada (cabeçalhos próprios).
/// </summary>
public class HttpRequestExtensionTests
{
  // Corpo enviado nas requisições
  private sealed record Pedido(int Id, string NomeCliente);

  // Corpo lido das respostas
  private sealed record Recibo(string Protocolo);

  // Resposta JSON padrão dos testes
  private const string ReciboJson = """{"protocolo":"ABC-1"}""";

  // URI usada nas chamadas
  private const string Endereco = "https://api.exemplo.com/pedidos";

  // O que o serviço chamado recebeu em cada tentativa
  private sealed record Recebido(HttpMethod Verbo, string? Chave, string? Autorizacao);

  // Implementação da interface que só oferece o SendAsync, como um substituto de teste ou um decorador
  private sealed class ServicoSubstituto(Func<HttpRequestMessage, HttpResponseMessage> responder) : IHttpClientService
  {
    public List<(HttpMethod Verbo, string? Uri, string? Chave, string? Corpo)> Envios { get; } = [];

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
      var corpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

      Envios.Add((
        request.Method,
        request.RequestUri?.OriginalString,
        request.Headers.TryGetValues("X-Api-Key", out var chaves) ? chaves.Single() : null,
        corpo));

      return responder(request);
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

  // Handler que registra o verbo e os cabeçalhos de cada tentativa
  private static FakeHttpMessageHandler Registrador(List<Recebido> recebidos, Func<int, HttpResponseMessage> responder) =>
    new(request =>
    {
      lock (recebidos)
      {
        recebidos.Add(new Recebido(
          request.Method,
          request.Headers.TryGetValues("X-Api-Key", out var chaves) ? chaves.Single() : null,
          request.Headers.Authorization?.ToString()));

        return responder(recebidos.Count);
      }
    });

  // Testa se cada verbo com resposta envia os cabeçalhos da chamada, o corpo em JSON e lê a resposta
  [Theory]
  [InlineData("GET")]
  [InlineData("POST")]
  [InlineData("PUT")]
  [InlineData("PATCH")]
  [InlineData("DELETE")]
  public async Task VerbsWithResponse_ShouldSendTheHeadersOfTheCall(string verbo)
  {
    // Arrange
    List<Recebido> recebidos = [];
    var handler = Registrador(recebidos, _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler);
    var service = provider.Service();
    var pedido = new Pedido(7, "Ana");
    var token = TestContext.Current.CancellationToken;

    void Ajustar(HttpRequestMessage request)
    {
      request.Headers.Add("X-Api-Key", "chave-da-chamada");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token-da-chamada");
    }

    // Act
    var recibo = verbo switch
    {
      "GET" => await service.GetAsync<Recibo>(Endereco, Ajustar, token),
      "POST" => await service.PostAsync<Pedido, Recibo>(Endereco, pedido, Ajustar, token),
      "PUT" => await service.PutAsync<Pedido, Recibo>(Endereco, pedido, Ajustar, token),
      "PATCH" => await service.PatchAsync<Pedido, Recibo>(Endereco, pedido, Ajustar, token),
      _ => await service.DeleteAsync<Recibo>(Endereco, Ajustar, token)
    };

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);

    var recebido = Assert.Single(recebidos);
    Assert.Equal(verbo, recebido.Verbo.Method);
    Assert.Equal("chave-da-chamada", recebido.Chave);
    Assert.Equal("Bearer token-da-chamada", recebido.Autorizacao);

    var request = Assert.Single(handler.Requests);
    Assert.Equal(verbo is "GET" or "DELETE" ? null : """{"id":7,"nomeCliente":"Ana"}""", request.Body);
    Assert.Equal(verbo is "GET" or "DELETE" ? null : "application/json; charset=utf-8", request.ContentType);
  }

  // Testa se os verbos sem resposta enviam os cabeçalhos e não leem o corpo, mesmo quando ele não é JSON
  [Theory]
  [InlineData("POST")]
  [InlineData("PUT")]
  [InlineData("PATCH")]
  [InlineData("DELETE")]
  public async Task VerbsWithoutResponse_ShouldSendTheHeadersAndIgnoreTheBody(string verbo)
  {
    // Arrange
    List<Recebido> recebidos = [];
    var handler = Registrador(recebidos, _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, "recebido", "text/plain"));
    using var provider = HttpTestHost.WithHandler(handler);
    var service = provider.Service();
    var pedido = new Pedido(7, "Ana");
    var token = TestContext.Current.CancellationToken;

    static void Ajustar(HttpRequestMessage request) => request.Headers.Add("X-Api-Key", "chave-da-chamada");

    // Act
    var envio = verbo switch
    {
      "POST" => service.PostAsync(Endereco, pedido, Ajustar, token),
      "PUT" => service.PutAsync(Endereco, pedido, Ajustar, token),
      "PATCH" => service.PatchAsync(Endereco, pedido, Ajustar, token),
      _ => service.DeleteAsync(Endereco, Ajustar, token)
    };

    await envio;

    // Assert
    var recebido = Assert.Single(recebidos);
    Assert.Equal(verbo, recebido.Verbo.Method);
    Assert.Equal("chave-da-chamada", recebido.Chave);
  }

  // Testa se o ajuste roda uma vez e as novas tentativas reenviam a mesma requisição, com os mesmos cabeçalhos
  [Fact]
  public async Task Configure_ShouldRunOnce_AndRetriesResendTheSameHeaders()
  {
    // Arrange
    List<Recebido> recebidos = [];
    var handler = Registrador(recebidos, chamada => chamada <= 2
      ? FakeHttpMessageHandler.Empty(HttpStatusCode.ServiceUnavailable)
      : FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler);
    var ajustes = 0;

    // Act
    var recibo = await provider.Service().GetAsync<Recibo>(
      Endereco,
      request =>
      {
        ajustes++;
        request.Headers.Add("X-Api-Key", $"chave-{ajustes}");
      },
      TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);
    Assert.Equal(1, ajustes);
    Assert.Equal(3, recebidos.Count);
    Assert.All(recebidos, recebido => Assert.Equal("chave-1", recebido.Chave));
  }

  // Testa se o status de erro vira a mesma exceção dos métodos da interface, com os erros do serviço chamado
  [Fact]
  public async Task ErrorStatus_ShouldThrowHttpRequestFailed_WithUpstreamErrors()
  {
    // Arrange
    const string problema = """{"status":422,"errors":[{"code":"T.VLD.STR1","key":"Email","message":"O campo Email é obrigatório"}]}""";
    var handler = new FakeHttpMessageHandler(_ =>
      FakeHttpMessageHandler.Json(HttpStatusCode.UnprocessableEntity, problema, "application/problem+json"));
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    var ex = await Assert.ThrowsAsync<HttpRequestFailedException>(() => provider.Service().PostAsync<Pedido, Recibo>(
      Endereco,
      new Pedido(7, "Ana"),
      request => request.Headers.Add("X-Api-Key", "chave"),
      TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal(HttpStatusCode.UnprocessableEntity, ex.UpstreamStatusCode);
    Assert.Equal(HttpStatusCode.BadGateway, ex.GetStatusCode());

    var item = Assert.Single(ex.GetNotifications());
    Assert.Equal("Email", item.Key);
    Assert.Equal("T.VLD.STR1", item.Code);
    Assert.Equal(problema, ex.UpstreamBody);
  }

  // Testa se a URI relativa segue o endereço base e se a URI inválida é recusada, como nos métodos da interface
  [Fact]
  public async Task RequestUri_ShouldFollowTheBaseAddress_AndRejectInvalidUris()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler, options => options.BaseAddress = "https://api.exemplo.com/v1");
    var service = provider.Service();

    static void Ajustar(HttpRequestMessage request) => request.Headers.Add("X-Api-Key", "chave");

    // Act
    await service.GetAsync<Recibo>("recibos/1", Ajustar, TestContext.Current.CancellationToken);

    var invalida = await Assert.ThrowsAsync<BadRequestException>(() =>
      service.GetAsync<Recibo>("http://[", Ajustar, TestContext.Current.CancellationToken));
    var esquema = await Assert.ThrowsAsync<BadRequestException>(() =>
      service.GetAsync<Recibo>("ftp://arquivos.exemplo.com/a", Ajustar, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("https://api.exemplo.com/v1/recibos/1", Assert.Single(handler.Requests).RequestUri?.AbsoluteUri);
    Assert.Equal("Http.RequestUriInvalid", invalida.Message);
    Assert.Equal("Http.RequestUriInvalid", esquema.Message);
  }

  // Testa se o serviço e o ajuste nulos são recusados antes de qualquer envio
  [Fact]
  public async Task NullArguments_ShouldBeRejected()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler);
    var service = provider.Service();
    IHttpClientService? nulo = null;

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
      service.GetAsync<Recibo>(Endereco, (Action<HttpRequestMessage>)null!, TestContext.Current.CancellationToken));
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
      service.PostAsync(Endereco, new Pedido(1, "Ana"), (Action<HttpRequestMessage>)null!, TestContext.Current.CancellationToken));
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
      nulo!.GetAsync<Recibo>(Endereco, _ => { }, TestContext.Current.CancellationToken));
    Assert.Equal(0, handler.Calls);
  }

  // Testa se outra implementação da interface é atendida pelo SendAsync dela, com o mesmo JSON e os mesmos cabeçalhos
  [Fact]
  public async Task OtherImplementation_ShouldGoThroughItsSendAsync()
  {
    // Arrange
    var substituto = new ServicoSubstituto(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    IHttpClientService service = substituto;

    // Act
    var recibo = await service.PostAsync<Pedido, Recibo>(
      "pedidos",
      new Pedido(7, "Ana"),
      request => request.Headers.Add("X-Api-Key", "chave"),
      TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);

    var envio = Assert.Single(substituto.Envios);
    Assert.Equal(HttpMethod.Post, envio.Verbo);
    Assert.Equal("pedidos", envio.Uri);
    Assert.Equal("chave", envio.Chave);
    Assert.Equal("""{"id":7,"nomeCliente":"Ana"}""", envio.Corpo);
  }

  // Testa se, em outra implementação, o status de erro e o JSON inválido recebem o mesmo tratamento do serviço
  [Fact]
  public async Task OtherImplementation_ShouldGetTheSameErrorHandling()
  {
    // Arrange
    IHttpClientService comErro = new ServicoSubstituto(_ =>
      FakeHttpMessageHandler.Json(HttpStatusCode.PaymentRequired, """{"error":{"message":"sem crédito"}}"""));
    IHttpClientService comJsonInvalido = new ServicoSubstituto(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, "<html>"));
    IHttpClientService semConteudo = new ServicoSubstituto(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.NoContent));
    var token = TestContext.Current.CancellationToken;

    // Act
    var erro = await Assert.ThrowsAsync<HttpRequestFailedException>(() => comErro.GetAsync<Recibo>(Endereco, _ => { }, token));
    var json = await Assert.ThrowsAsync<BadGatewayException>(() => comJsonInvalido.GetAsync<Recibo>(Endereco, _ => { }, token));
    var vazio = await semConteudo.GetAsync<Recibo>(Endereco, _ => { }, token);

    // O verbo sem resposta não lê o corpo, então o corpo que não é JSON não incomoda
    await comJsonInvalido.DeleteAsync(Endereco, _ => { }, token);

    // Assert
    Assert.Equal(HttpStatusCode.PaymentRequired, erro.UpstreamStatusCode);
    Assert.Equal("Http.RequestFailed;402", erro.Message);
    Assert.Equal("""{"error":{"message":"sem crédito"}}""", erro.UpstreamBody);
    Assert.Equal("Http.InvalidResponse", json.Message);
    Assert.Null(vazio);
  }
}
