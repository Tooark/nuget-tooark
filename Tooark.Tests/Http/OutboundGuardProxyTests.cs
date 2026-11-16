using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Http.Exceptions;
using Tooark.Http.Injections;
using Tooark.Http.Internal;
using Tooark.Http.Options;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes da guarda de saída quando a conexão não enxerga o destino: um proxy de saída, ou um handler primário que
/// não é o da guarda.
/// </summary>
/// <remarks>
/// Nesses casos o nome do destino é conferido na requisição, resolvido pelo DNS da aplicação. Os testes com o proxy
/// de verdade fixam o que chega (e o que não chega) à conexão do proxy: conferir só o que o handler decidiu não pega
/// um nome interno que saiu pela rede.
/// </remarks>
public class OutboundGuardProxyTests
{
  #region Helpers

  // Endereço público usado nos nomes que devem passar
  private const string Publico = "93.184.216.34";

  // Monta um cliente com a guarda e um handler primário que não é o dela: toda a conferência fica na requisição
  private static (ServiceProvider Provider, HttpClient Client) SemConexaoConferida(
    FakeHttpMessageHandler handler,
    FakeHostResolver resolver,
    Action<OutboundGuardOptions>? configure = null
  )
  {
    var services = new ServiceCollection();

    services.AddSingleton<IHostResolver>(resolver);
    services.AddHttpClient("proxy")
      .AddTooarkOutboundGuard(configure)
      .ConfigurePrimaryHttpMessageHandler(() => handler);

    var provider = services.BuildServiceProvider();

    return (provider, provider.GetRequiredService<IHttpClientFactory>().CreateClient("proxy"));
  }

  // Monta um cliente com a guarda e um proxy de saída de verdade, no loopback. O 127.0.0.1 do proxy é liberado
  // pela rede, como a documentação pede para um proxy interno informado por endereço
  private static (ServiceProvider Provider, HttpClient Client) ComProxy(LoopbackServer proxy, FakeHostResolver resolver, params string[] semProxy)
  {
    var services = new ServiceCollection();

    services.AddSingleton<IHostResolver>(resolver);
    services.AddHttpClient("proxy")
      .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
      {
        UseProxy = true,
        Proxy = new WebProxy($"http://127.0.0.1:{proxy.Port}") { BypassList = semProxy }
      })
      .AddTooarkOutboundGuard(options => options.AllowedNetworks = ["127.0.0.1/32"]);

    var provider = services.BuildServiceProvider();

    return (provider, provider.GetRequiredService<IHttpClientFactory>().CreateClient("proxy"));
  }

  // Resposta de um handler primário falso
  private static FakeHttpMessageHandler Ok() => new(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.OK));

  #endregion

  #region Request Check - Names

  // Testa se o nome que resolve para um endereço interno é recusado na requisição, sem chegar ao handler primário
  [Theory]
  [InlineData("10.0.0.5")]
  [InlineData("127.0.0.1")]
  [InlineData("169.254.169.254")]
  [InlineData("192.168.1.10")]
  [InlineData("::1")]
  [InlineData("::ffff:10.0.0.5")]
  [InlineData("fd00::1")]
  public async Task RequestCheck_ShouldBlockNameThatResolvesToInternalAddress(string endereco)
  {
    // Arrange
    var handler = Ok();
    var resolver = new FakeHostResolver().Map("interno.exemplo.com", endereco);
    var (provider, client) = SemConexaoConferida(handler, resolver);

    using (provider)
    using (client)
    {
      // Act
      var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
        client.GetAsync("http://interno.exemplo.com/segredo", TestContext.Current.CancellationToken));

      // Assert
      Assert.Equal("interno.exemplo.com", ex.Host);
      Assert.Equal("Http.OutboundBlocked;interno.exemplo.com", Assert.Single(ex.GetErrorMessages()));
      Assert.Equal(0, handler.Calls);
      Assert.Equal(["interno.exemplo.com"], resolver.Queries);
    }
  }

  // Testa se basta um endereço interno entre os do nome para recusar o host inteiro
  [Fact]
  public async Task RequestCheck_ShouldBlockName_WhenAnyAddressIsInternal()
  {
    // Arrange
    var handler = Ok();
    var resolver = new FakeHostResolver().Map("misto.exemplo.com", Publico, "10.0.0.5");
    var (provider, client) = SemConexaoConferida(handler, resolver);

    using (provider)
    using (client)
    {
      // Act
      await Assert.ThrowsAsync<OutboundBlockedException>(() =>
        client.GetAsync("https://misto.exemplo.com/", TestContext.Current.CancellationToken));

      // Assert
      Assert.Equal(0, handler.Calls);
    }
  }

  // Testa se o nome que resolve para um endereço público passa, com uma única consulta ao DNS
  [Theory]
  [InlineData("http://publico.exemplo.com/")]
  [InlineData("https://publico.exemplo.com:8443/caminho?x=1")]
  public async Task RequestCheck_ShouldLetNameThatResolvesToPublicAddressThrough(string url)
  {
    // Arrange
    var handler = Ok();
    var resolver = new FakeHostResolver().Map("publico.exemplo.com", Publico);
    var (provider, client) = SemConexaoConferida(handler, resolver);

    using (provider)
    using (client)
    {
      // Act
      using var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(1, handler.Calls);
      Assert.Equal(["publico.exemplo.com"], resolver.Queries);
    }
  }

  // Testa se o nome liberado em AllowedHosts não é consultado nem precisa resolver: é o caso do serviço interno
  // que só o proxy alcança
  [Theory]
  [InlineData("http://api.interna.exemplo.com/")]
  [InlineData("http://parceiro.exemplo.com/")]
  public async Task RequestCheck_ShouldNotResolveAllowedHosts(string url)
  {
    // Arrange: nenhum dos nomes está no DNS falso
    var handler = Ok();
    var resolver = new FakeHostResolver();
    var (provider, client) = SemConexaoConferida(handler, resolver, options =>
      options.AllowedHosts = [".interna.exemplo.com", "parceiro.exemplo.com"]);

    using (provider)
    using (client)
    {
      // Act
      using var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(1, handler.Calls);
      Assert.Empty(resolver.Queries);
    }
  }

  // Testa se a rede liberada vale também para o endereço que o nome resolve
  [Fact]
  public async Task RequestCheck_ShouldHonorAllowedNetworks_ForResolvedNames()
  {
    // Arrange
    var handler = Ok();
    var resolver = new FakeHostResolver().Map("servico.exemplo.com", "10.20.0.5");
    var (provider, client) = SemConexaoConferida(handler, resolver, options => options.AllowedNetworks = ["10.20.0.0/16"]);

    using (provider)
    using (client)
    {
      // Act
      using var response = await client.GetAsync("http://servico.exemplo.com/", TestContext.Current.CancellationToken);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(1, handler.Calls);
    }
  }

  #endregion

  #region Request Check - Unresolved Names

  // Testa se o nome que a aplicação não resolve falha como a falha de resolução de nome da conexão direta, sem
  // chegar ao handler primário: a guarda recusa o que não consegue conferir
  [Fact]
  public async Task RequestCheck_ShouldFailAsNameResolutionError_WhenNameDoesNotResolve()
  {
    // Arrange
    var handler = Ok();
    var resolver = new FakeHostResolver();
    var (provider, client) = SemConexaoConferida(handler, resolver);

    using (provider)
    using (client)
    {
      // Act
      var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
        client.GetAsync("http://nao-existe.exemplo.com/", TestContext.Current.CancellationToken));

      // Assert
      Assert.Equal(HttpRequestError.NameResolutionError, ex.HttpRequestError);
      Assert.IsType<System.Net.Sockets.SocketException>(ex.InnerException);
      Assert.Equal(0, handler.Calls);
    }
  }

  // Testa se, com a opção ligada, o nome que a aplicação não resolve passa sem conferência, e o que resolve para um
  // endereço interno continua recusado
  [Fact]
  public async Task RequestCheck_ShouldLetUnresolvedNameThrough_WhenAllowed()
  {
    // Arrange
    var handler = Ok();
    var resolver = new FakeHostResolver().Map("interno.exemplo.com", "10.0.0.5");
    var (provider, client) = SemConexaoConferida(handler, resolver, options => options.AllowUnresolvedHostsBehindProxy = true);

    using (provider)
    using (client)
    {
      // Act
      using var response = await client.GetAsync("http://so-o-proxy-resolve.exemplo.com/", TestContext.Current.CancellationToken);
      await Assert.ThrowsAsync<OutboundBlockedException>(() =>
        client.GetAsync("http://interno.exemplo.com/", TestContext.Current.CancellationToken));

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(1, handler.Calls);
    }
  }

  // Testa se a opção sai desligada e é lida da configuração do cliente do Tooark
  [Fact]
  public void AllowUnresolvedHostsBehindProxy_ShouldBeOffByDefault()
  {
    // Arrange & Act
    var options = new HttpClientOptions();

    // Assert
    Assert.False(options.OutboundGuard.AllowUnresolvedHostsBehindProxy);
    Assert.False(new OutboundGuardOptions().AllowUnresolvedHostsBehindProxy);
  }

  #endregion

  #region Request Check - Synchronous Send

  // Testa se o envio síncrono confere o nome do mesmo jeito
  [Fact]
  public void RequestCheck_ShouldBlockInternalName_OnSynchronousSend()
  {
    // Arrange
    var handler = Ok();
    var resolver = new FakeHostResolver().Map("interno.exemplo.com", "10.0.0.5");
    var (provider, client) = SemConexaoConferida(handler, resolver);

    using (provider)
    using (client)
    using (var request = new HttpRequestMessage(HttpMethod.Get, "http://interno.exemplo.com/"))
    {
      // Act
      var ex = Assert.Throws<OutboundBlockedException>(() => client.Send(request, TestContext.Current.CancellationToken));

      // Assert
      Assert.Equal("interno.exemplo.com", ex.Host);
      Assert.Equal(0, handler.Calls);
    }
  }

  // Testa se o envio síncrono trata o nome que não resolve como o assíncrono
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void RequestCheck_ShouldHandleUnresolvedName_OnSynchronousSend(bool permitido)
  {
    // Arrange
    var handler = Ok();
    var (provider, client) = SemConexaoConferida(handler, new FakeHostResolver(), options => options.AllowUnresolvedHostsBehindProxy = permitido);

    using (provider)
    using (client)
    using (var request = new HttpRequestMessage(HttpMethod.Get, "http://nao-existe.exemplo.com/"))
    {
      // Act
      var ex = Record.Exception(() => client.Send(request, TestContext.Current.CancellationToken));

      // Assert: recusado, falha na resolução; permitido, passa pela guarda e para no handler falso, que não tem
      // envio síncrono
      if (permitido)
      {
        Assert.IsType<NotSupportedException>(ex);
      }
      else
      {
        Assert.Equal(HttpRequestError.NameResolutionError, Assert.IsType<HttpRequestException>(ex).HttpRequestError);
      }
    }
  }

  #endregion

  #region Tooark Service

  // Testa se, pelo serviço do Tooark, o nome interno sai como destino recusado, sem nova tentativa
  [Fact]
  public async Task Service_ShouldBlockInternalName_WithoutRetry()
  {
    // Arrange
    var handler = Ok();
    var resolver = new FakeHostResolver().Map("interno.exemplo.com", "10.0.0.5");
    var services = new ServiceCollection();
    services.AddSingleton<IHostResolver>(resolver);
    services.AddTooarkHttpClient(options =>
    {
      options.RetryDelayMilliseconds = 1;
      options.OutboundGuard.Enabled = true;
    });
    services.AddHttpClient(Tooark.Http.HttpClientService.ClientName).ConfigurePrimaryHttpMessageHandler(() => handler);

    using var provider = services.BuildServiceProvider();

    // Act
    var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
      provider.Service().GetAsync<object>("http://interno.exemplo.com/", TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, ex.GetStatusCode());
    Assert.Equal(0, handler.Calls);
    Assert.Single(resolver.Queries);
  }

  // Testa se, pelo serviço do Tooark, o nome que não resolve sai como falha de conexão e é repetido pela
  // resiliência, como na conexão direta
  [Fact]
  public async Task Service_ShouldMapUnresolvedNameToConnectionFailed_WithRetry()
  {
    // Arrange
    var handler = Ok();
    var resolver = new FakeHostResolver();
    var services = new ServiceCollection();
    services.AddSingleton<IHostResolver>(resolver);
    services.AddTooarkHttpClient(options =>
    {
      options.RetryCount = 2;
      options.RetryDelayMilliseconds = 1;
      options.OutboundGuard.Enabled = true;
    });
    services.AddHttpClient(Tooark.Http.HttpClientService.ClientName).ConfigurePrimaryHttpMessageHandler(() => handler);

    using var provider = services.BuildServiceProvider();

    // Act
    var ex = await Assert.ThrowsAsync<BadGatewayException>(() =>
      provider.Service().GetAsync<object>("http://nao-existe.exemplo.com/", TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("Http.ConnectionFailed", Assert.Single(ex.GetErrorMessages()));
    Assert.Equal(0, handler.Calls);
    Assert.Equal(3, resolver.Queries.Count);
  }

  #endregion

  #region Real Proxy

  // Testa se o nome que resolve para um endereço interno nunca chega ao proxy, em http e em https (sem CONNECT)
  [Theory]
  [InlineData("http://interno.exemplo.com/segredo")]
  [InlineData("https://interno.exemplo.com/segredo")]
  public async Task Proxy_ShouldNotReceiveRequest_WhenNameResolvesToInternalAddress(string url)
  {
    // Arrange
    await using var proxy = new LoopbackServer(_ => LoopbackServer.Json("{}"));
    var resolver = new FakeHostResolver().Map("interno.exemplo.com", "10.0.0.5");
    var (provider, client) = ComProxy(proxy, resolver);

    using (provider)
    using (client)
    {
      // Act
      var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() => client.GetAsync(url, TestContext.Current.CancellationToken));

      // Assert
      Assert.Equal("interno.exemplo.com", ex.Host);
      Assert.Empty(proxy.Targets);
      Assert.Equal(["interno.exemplo.com"], resolver.Queries);
    }
  }

  // Testa se o nome que resolve para um endereço público segue para o proxy, que recebe a URL inteira
  [Fact]
  public async Task Proxy_ShouldReceiveRequest_WhenNameResolvesToPublicAddress()
  {
    // Arrange
    await using var proxy = new LoopbackServer(_ => LoopbackServer.Json("{}"));
    var resolver = new FakeHostResolver().Map("publico.exemplo.com", Publico);
    var (provider, client) = ComProxy(proxy, resolver);

    using (provider)
    using (client)
    {
      // Act
      using var response = await client.GetAsync("http://publico.exemplo.com/recurso", TestContext.Current.CancellationToken);

      // Assert: a conferência foi feita uma vez, na requisição; a conexão é com o proxy, um IP literal
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(["http://publico.exemplo.com/recurso"], proxy.Targets);
      Assert.Equal(["publico.exemplo.com"], resolver.Queries);
    }
  }

  // Testa se, em https, o proxy recebe o CONNECT do nome conferido
  [Fact]
  public async Task Proxy_ShouldReceiveConnect_WhenHttpsNameResolvesToPublicAddress()
  {
    // Arrange: o proxy de mentira recusa o túnel, e o que importa é o CONNECT ter chegado
    await using var proxy = new LoopbackServer(_ => "HTTP/1.1 502 Bad Gateway\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
    var resolver = new FakeHostResolver().Map("publico.exemplo.com", Publico);
    var (provider, client) = ComProxy(proxy, resolver);

    using (provider)
    using (client)
    {
      // Act
      await Assert.ThrowsAsync<HttpRequestException>(() =>
        client.GetAsync("https://publico.exemplo.com/recurso", TestContext.Current.CancellationToken));

      // Assert
      Assert.Equal(["publico.exemplo.com:443"], proxy.Targets);
    }
  }

  // Testa se o nome que a aplicação não resolve não chega ao proxy por padrão
  [Fact]
  public async Task Proxy_ShouldNotReceiveRequest_WhenNameDoesNotResolve()
  {
    // Arrange
    await using var proxy = new LoopbackServer(_ => LoopbackServer.Json("{}"));
    var (provider, client) = ComProxy(proxy, new FakeHostResolver());

    using (provider)
    using (client)
    {
      // Act
      var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
        client.GetAsync("http://nao-existe.exemplo.com/", TestContext.Current.CancellationToken));

      // Assert
      Assert.Equal(HttpRequestError.NameResolutionError, ex.HttpRequestError);
      Assert.Empty(proxy.Targets);
    }
  }

  // Testa se a URI que o proxy dispensa vai direto ao destino e é conferida uma vez só, na conexão
  [Fact]
  public async Task Proxy_ShouldResolveOnce_WhenUriBypassesProxy()
  {
    // Arrange
    await using var proxy = new LoopbackServer(_ => LoopbackServer.Json("{}"));
    await using var destino = new LoopbackServer(_ => LoopbackServer.Json("{}"));
    var resolver = new FakeHostResolver().Map("direto.exemplo.com", "127.0.0.1");
    var (provider, client) = ComProxy(proxy, resolver, @"direto\.exemplo\.com");

    using (provider)
    using (client)
    {
      // Act
      using var response = await client.GetAsync($"http://direto.exemplo.com:{destino.Port}/recurso", TestContext.Current.CancellationToken);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(["/recurso"], destino.Targets);
      Assert.Empty(proxy.Targets);
      Assert.Equal(["direto.exemplo.com"], resolver.Queries);
    }
  }

  // Testa se, sem proxy e com a conexão da guarda, o nome interno continua recusado na conexão, com uma consulta só
  [Fact]
  public async Task Direct_ShouldResolveOnce_WhenProxyIsOff()
  {
    // Arrange
    await using var destino = new LoopbackServer(_ => LoopbackServer.Json("{}"));
    var resolver = new FakeHostResolver().Map("interno.exemplo.com", "127.0.0.2").Map("liberado.exemplo.com", "127.0.0.1");
    var services = new ServiceCollection();
    services.AddSingleton<IHostResolver>(resolver);
    services.AddHttpClient("direto")
      .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseProxy = false })
      .AddTooarkOutboundGuard(options => options.AllowedNetworks = ["127.0.0.1/32"]);

    using var provider = services.BuildServiceProvider();
    using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("direto");

    // Act
    using var response = await client.GetAsync($"http://liberado.exemplo.com:{destino.Port}/recurso", TestContext.Current.CancellationToken);
    await Assert.ThrowsAsync<OutboundBlockedException>(() =>
      client.GetAsync($"http://interno.exemplo.com:{destino.Port}/segredo", TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(["/recurso"], destino.Targets);
    Assert.Equal(["liberado.exemplo.com", "interno.exemplo.com"], resolver.Queries);
  }

  #endregion
}
