using System.Net;
using System.Text;
using Tooark.Exceptions;
using Tooark.Http;
using Tooark.Http.Options;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes dos verbos, do JSON e do endereço base do serviço de chamadas HTTP.
/// </summary>
public class HttpClientServiceTests
{
  // Corpo enviado nas requisições
  private sealed record Pedido(int Id, string NomeCliente);

  // Corpo lido das respostas
  private sealed record Recibo(string Protocolo, decimal ValorTotal);

  // Resposta JSON padrão dos testes
  private const string ReciboJson = """{"protocolo":"ABC-1","valorTotal":10.5}""";

  // Fábrica que conta os clientes pedidos
  private sealed class FabricaContadora(HttpMessageHandler handler) : IHttpClientFactory
  {
    public List<string> Nomes { get; } = [];

    public HttpClient CreateClient(string name)
    {
      Nomes.Add(name);

      return new HttpClient(handler, disposeHandler: false);
    }
  }

  // Testa se o GET envia para a URI absoluta e lê o JSON em camelCase
  [Fact]
  public async Task GetAsync_ShouldSendGetAndReadJson()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    var recibo = await provider.Service().GetAsync<Recibo>("https://api.exemplo.com/recibos/1", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(new Recibo("ABC-1", 10.5m), recibo);

    var request = Assert.Single(handler.Requests);
    Assert.Equal(HttpMethod.Get, request.Method);
    Assert.Equal("https://api.exemplo.com/recibos/1", request.RequestUri?.AbsoluteUri);
    Assert.Null(request.Body);
  }

  // Testa se POST, PUT e PATCH enviam o corpo em JSON camelCase e leem a resposta
  [Theory]
  [InlineData("POST")]
  [InlineData("PUT")]
  [InlineData("PATCH")]
  public async Task BodyVerbs_ShouldSendJsonBodyAndReadJson(string verbo)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler);
    var service = provider.Service();
    var pedido = new Pedido(7, "Ana");
    var token = TestContext.Current.CancellationToken;

    // Act
    var recibo = verbo switch
    {
      "POST" => await service.PostAsync<Pedido, Recibo>("https://api.exemplo.com/pedidos", pedido, token),
      "PUT" => await service.PutAsync<Pedido, Recibo>("https://api.exemplo.com/pedidos", pedido, token),
      _ => await service.PatchAsync<Pedido, Recibo>("https://api.exemplo.com/pedidos", pedido, token)
    };

    // Assert
    Assert.Equal(new Recibo("ABC-1", 10.5m), recibo);

    var request = Assert.Single(handler.Requests);
    Assert.Equal(verbo, request.Method.Method);
    Assert.Equal("""{"id":7,"nomeCliente":"Ana"}""", request.Body);
    Assert.Equal("application/json; charset=utf-8", request.ContentType);
  }

  // Testa se POST, PUT e PATCH sem resposta enviam o corpo e não exigem JSON de volta
  [Theory]
  [InlineData("POST")]
  [InlineData("PUT")]
  [InlineData("PATCH")]
  public async Task BodyVerbs_ShouldNotReadResponse_WhenNoResponseTypeIsGiven(string verbo)
  {
    // Arrange: o corpo da resposta não é JSON, e mesmo assim a chamada conclui
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, "recebido", "text/plain"));
    using var provider = HttpTestHost.WithHandler(handler);
    var service = provider.Service();
    var pedido = new Pedido(7, "Ana");
    var token = TestContext.Current.CancellationToken;

    // Act
    var chamada = verbo switch
    {
      "POST" => service.PostAsync("https://api.exemplo.com/pedidos", pedido, token),
      "PUT" => service.PutAsync("https://api.exemplo.com/pedidos", pedido, token),
      _ => service.PatchAsync("https://api.exemplo.com/pedidos", pedido, token)
    };

    await chamada;

    // Assert
    var request = Assert.Single(handler.Requests);
    Assert.Equal(verbo, request.Method.Method);
    Assert.Equal("""{"id":7,"nomeCliente":"Ana"}""", request.Body);
  }

  // Testa se o DELETE sem resposta envia o verbo e não lê o corpo
  [Fact]
  public async Task DeleteAsync_ShouldSendDelete()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, "removido", "text/plain"));
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    await provider.Service().DeleteAsync("https://api.exemplo.com/pedidos/7", TestContext.Current.CancellationToken);

    // Assert
    var request = Assert.Single(handler.Requests);
    Assert.Equal(HttpMethod.Delete, request.Method);
    Assert.Null(request.Body);
  }

  // Testa se o DELETE com resposta lê o JSON
  [Fact]
  public async Task DeleteAsync_ShouldReadJson_WhenResponseTypeIsGiven()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    var recibo = await provider.Service().DeleteAsync<Recibo>("https://api.exemplo.com/pedidos/7", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);
    Assert.Equal(HttpMethod.Delete, Assert.Single(handler.Requests).Method);
  }

  // Testa se a URI relativa é combinada com o endereço base, com e sem barra final e com caminho
  [Theory]
  [InlineData("https://api.exemplo.com", "recibos/1", "https://api.exemplo.com/recibos/1")]
  [InlineData("https://api.exemplo.com/", "/recibos/1", "https://api.exemplo.com/recibos/1")]
  [InlineData("https://api.exemplo.com/v1", "recibos/1", "https://api.exemplo.com/v1/recibos/1")]
  [InlineData("https://api.exemplo.com/v1/", "recibos/1", "https://api.exemplo.com/v1/recibos/1")]
  [InlineData("https://api.exemplo.com/v1", "/recibos/1", "https://api.exemplo.com/recibos/1")]
  [InlineData("https://api.exemplo.com/v1", "", "https://api.exemplo.com/v1/")]
  [InlineData("https://api.exemplo.com/v1", "https://outro.exemplo.com/x", "https://outro.exemplo.com/x")]
  public async Task GetAsync_ShouldCombineRelativeUriWithBaseAddress(string baseAddress, string requestUri, string esperado)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler, options => options.BaseAddress = baseAddress);

    // Act
    await provider.Service().GetAsync<Recibo>(requestUri, TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(esperado, Assert.Single(handler.Requests).RequestUri?.AbsoluteUri);
  }

  // Testa se a URI relativa sem endereço base falha como erro de configuração, sem enviar
  [Theory]
  [InlineData("recibos/1")]
  [InlineData("")]
  public async Task GetAsync_ShouldThrowInternalServerError_WhenUriIsRelativeWithoutBaseAddress(string requestUri)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() =>
      provider.Service().GetAsync<Recibo>(requestUri, TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Http.BaseAddressNotConfigured", ex.GetErrorMessages());
    Assert.Equal(0, handler.Calls);
  }

  // Testa se uma URI que não é http nem https é recusada como entrada inválida, sem enviar
  [Theory]
  [InlineData("ftp://arquivos.exemplo.com/a.txt")]
  [InlineData("file:///etc/passwd")]
  [InlineData("http://")]
  public async Task GetAsync_ShouldThrowBadRequest_WhenUriIsNotHttp(string requestUri)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    using var provider = HttpTestHost.WithHandler(handler, options => options.BaseAddress = "https://api.exemplo.com");

    // Act
    var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
      provider.Service().GetAsync<Recibo>(requestUri, TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Http.RequestUriInvalid", ex.GetErrorMessages());
    Assert.Equal(0, handler.Calls);
  }

  // Testa se a resposta 204 devolve o valor padrão
  [Fact]
  public async Task GetAsync_ShouldReturnDefault_WhenResponseIsNoContent()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.NoContent));
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    var recibo = await provider.Service().GetAsync<Recibo>("https://api.exemplo.com/recibos/1", TestContext.Current.CancellationToken);
    var numero = await provider.Service().GetAsync<int>("https://api.exemplo.com/total", TestContext.Current.CancellationToken);

    // Assert
    Assert.Null(recibo);
    Assert.Equal(0, numero);
  }

  // Testa se a resposta de sucesso sem corpo devolve o valor padrão
  [Fact]
  public async Task GetAsync_ShouldReturnDefault_WhenBodyIsEmpty()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, string.Empty));
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    var recibo = await provider.Service().GetAsync<Recibo>("https://api.exemplo.com/recibos/1", TestContext.Current.CancellationToken);

    // Assert
    Assert.Null(recibo);
  }

  // Testa se a marca de ordem de bytes do UTF-8 no início do corpo não quebra a leitura
  [Fact]
  public async Task GetAsync_ShouldReadJson_WhenBodyStartsWithByteOrderMark()
  {
    // Arrange
    byte[] corpo = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(ReciboJson)];
    var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(corpo) });
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    var recibo = await provider.Service().GetAsync<Recibo>("https://api.exemplo.com/recibos/1", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);
  }

  // Testa se o SendAsync devolve a resposta de erro como veio, sem lançar
  [Fact]
  public async Task SendAsync_ShouldReturnRawResponse_WhenStatusIsError()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.NotFound, "nada aqui", "text/plain"));
    using var provider = HttpTestHost.WithHandler(handler);
    using var request = new HttpRequestMessage(HttpMethod.Head, "https://api.exemplo.com/recibos/1");
    request.Headers.Add("X-Origem", "teste");

    // Act
    using var response = await provider.Service().SendAsync(request, TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    Assert.Equal("nada aqui", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    Assert.Equal(HttpMethod.Head, Assert.Single(handler.Requests).Method);
  }

  // Testa se o SendAsync usa o endereço base e confere a URI como os demais métodos
  [Fact]
  public async Task SendAsync_ShouldUseBaseAddressAndValidateUri()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.OK));
    using var comBase = HttpTestHost.WithHandler(handler, options => options.BaseAddress = "https://api.exemplo.com/v1");
    using var semBase = HttpTestHost.WithHandler(handler);
    using var relativa = new HttpRequestMessage(HttpMethod.Get, "recibos/1");
    using var outraRelativa = new HttpRequestMessage(HttpMethod.Get, "recibos/1");
    using var arquivo = new HttpRequestMessage(HttpMethod.Get, "ftp://arquivos.exemplo.com/a.txt");

    // Act
    using var response = await comBase.Service().SendAsync(relativa, TestContext.Current.CancellationToken);
    var semEndereco = await Assert.ThrowsAsync<InternalServerErrorException>(() =>
      semBase.Service().SendAsync(outraRelativa, TestContext.Current.CancellationToken));
    var esquema = await Assert.ThrowsAsync<BadRequestException>(() =>
      semBase.Service().SendAsync(arquivo, TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("https://api.exemplo.com/v1/recibos/1", Assert.Single(handler.Requests).RequestUri?.AbsoluteUri);
    Assert.Contains("Http.BaseAddressNotConfigured", semEndereco.GetErrorMessages());
    Assert.Contains("Http.RequestUriInvalid", esquema.GetErrorMessages());
  }

  // Testa se o serviço pede o cliente nomeado à fábrica a cada chamada
  [Fact]
  public async Task Service_ShouldResolveNamedClientOnEveryCall()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    var fabrica = new FabricaContadora(handler);
    var service = new HttpClientService(fabrica, Microsoft.Extensions.Options.Options.Create(new HttpClientOptions()));

    // Act
    await service.GetAsync<Recibo>("https://api.exemplo.com/recibos/1", TestContext.Current.CancellationToken);
    await service.DeleteAsync("https://api.exemplo.com/recibos/1", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(new[] { "Tooark", "Tooark" }, fabrica.Nomes);
    Assert.Equal("Tooark", HttpClientService.ClientName);
  }

  // Testa se o serviço criado à mão recusa opções inválidas
  [Fact]
  public void Constructor_ShouldThrow_WhenOptionsAreInvalid()
  {
    // Arrange
    var fabrica = new FabricaContadora(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.OK)));
    var options = Microsoft.Extensions.Options.Options.Create(new HttpClientOptions { TotalTimeoutSeconds = 0 });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => new HttpClientService(fabrica, options));

    // Assert
    Assert.Contains("Options.HttpClient.TotalTimeoutInvalid;1;3600", ex.GetErrorMessages());
  }
}
