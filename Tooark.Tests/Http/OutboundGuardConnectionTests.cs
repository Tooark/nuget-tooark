using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Http.Exceptions;
using Tooark.Http.Injections;
using Tooark.Http.Internal;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes da guarda de saída na conexão real, contra um servidor no loopback e com o DNS trocado.
/// </summary>
public class OutboundGuardConnectionTests
{
  // Corpo lido das respostas
  private sealed record Recibo(string Protocolo);

  // Resposta JSON padrão do servidor
  private const string ReciboJson = """{"protocolo":"ABC-1"}""";

  // Inicia um servidor que responde sempre o mesmo JSON
  private static LoopbackServer Servidor() => new(_ => LoopbackServer.Json(ReciboJson));

  // Testa se o loopback é bloqueado por padrão, sem abrir conexão
  [Fact]
  public async Task Guard_ShouldBlockLoopbackLiteral_ByDefault()
  {
    // Arrange
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver();
    using var provider = HttpTestHost.WithGuard(resolver);

    // Act
    var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
      provider.Service().GetAsync<Recibo>($"http://127.0.0.1:{servidor.Port}/", TestContext.Current.CancellationToken));

    // Assert
    Assert.IsAssignableFrom<BadRequestException>(ex);
    Assert.Equal(HttpStatusCode.BadRequest, ex.GetStatusCode());
    Assert.Equal("127.0.0.1", ex.Host);
    Assert.Equal("Http.OutboundBlocked;127.0.0.1", Assert.Single(ex.GetErrorMessages()));
    Assert.Empty(servidor.Targets);
    Assert.Empty(resolver.Queries);
  }

  // Testa se o loopback fica alcançável quando a rede dele está liberada
  [Fact]
  public async Task Guard_ShouldReachLoopback_WhenNetworkIsAllowed()
  {
    // Arrange
    await using var servidor = Servidor();
    using var provider = HttpTestHost.WithGuard(new FakeHostResolver(), options => options.OutboundGuard.AllowedNetworks = ["127.0.0.0/8"]);

    // Act
    var recibo = await provider.Service().GetAsync<Recibo>($"http://127.0.0.1:{servidor.Port}/recibos", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);
    Assert.Equal("/recibos", Assert.Single(servidor.Targets));
  }

  // Testa se um nome público que resolve para um endereço interno é bloqueado, sem nova tentativa e sem segunda resolução
  [Fact]
  public async Task Guard_ShouldBlockNameResolvingToInternalAddress_WithoutRetry()
  {
    // Arrange
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver().Map("webhook.exemplo.com", "127.0.0.1");
    using var provider = HttpTestHost.WithGuard(resolver, options => options.RetryCount = 3);

    // Act
    var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
      provider.Service().GetAsync<Recibo>($"http://webhook.exemplo.com:{servidor.Port}/", TestContext.Current.CancellationToken));

    // Assert: o host da mensagem é o nome da URL, e não o endereço para o qual ele resolveu
    Assert.Equal("Http.OutboundBlocked;webhook.exemplo.com", Assert.Single(ex.GetErrorMessages()));
    Assert.DoesNotContain("127.0.0.1", ex.Message);
    // A exceção de transporte que embrulhava a recusa fica preservada para diagnóstico
    Assert.IsType<HttpRequestException>(ex.InnerException);
    // Uma única resolução: a recusa não foi tratada como falha transitória pelo handler de resiliência
    Assert.Equal("webhook.exemplo.com", Assert.Single(resolver.Queries));
    Assert.Empty(servidor.Targets);
  }

  // Testa se as recusas da guarda não contam como falha no circuito
  [Fact]
  public async Task Guard_ShouldNotOpenTheCircuit()
  {
    // Arrange
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver()
      .Map("interno.exemplo.com", "10.0.0.5")
      .Map("publico.exemplo.com", "127.0.0.1");
    using var provider = HttpTestHost.WithGuard(resolver, options =>
    {
      options.RetryCount = 0;
      options.CircuitBreakerMinimumThroughput = 2;
      options.CircuitBreakerFailureRatio = 0.1;
      options.OutboundGuard.AllowedHosts = ["publico.exemplo.com"];
    });
    var service = provider.Service();
    var token = TestContext.Current.CancellationToken;

    // Act: várias URLs bloqueadas informadas por um usuário
    for (int i = 0; i < 5; i++)
    {
      await Assert.ThrowsAsync<OutboundBlockedException>(() =>
        service.GetAsync<Recibo>($"http://interno.exemplo.com:{servidor.Port}/", token));
    }

    var recibo = await service.GetAsync<Recibo>($"http://publico.exemplo.com:{servidor.Port}/", token);

    // Assert: o circuito segue fechado para as demais chamadas
    Assert.Equal("ABC-1", recibo?.Protocolo);
  }

  // Testa a liberação por nome exato e por sufixo de host, e a recusa de um domínio parecido
  [Theory]
  [InlineData("api.interna.exemplo.com", "api.interna.exemplo.com", true)]
  [InlineData(".interna.exemplo.com", "api.interna.exemplo.com", true)]
  [InlineData(".interna.exemplo.com", "API.Interna.Exemplo.com", true)]
  [InlineData(".interna.exemplo.com", "falsainterna.exemplo.com", false)]
  [InlineData(".interna.exemplo.com", "interna.exemplo.com", false)]
  public async Task Guard_ShouldHonorAllowedHosts(string liberado, string host, bool alcanca)
  {
    // Arrange
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver().Map(host, "127.0.0.1");
    using var provider = HttpTestHost.WithGuard(resolver, options => options.OutboundGuard.AllowedHosts = [liberado]);
    var chamada = () => provider.Service().GetAsync<Recibo>($"http://{host}:{servidor.Port}/", TestContext.Current.CancellationToken);

    // Act & Assert
    if (alcanca)
    {
      Assert.Equal("ABC-1", (await chamada())?.Protocolo);
      Assert.Single(servidor.Targets);
    }
    else
    {
      await Assert.ThrowsAsync<OutboundBlockedException>(chamada);
      Assert.Empty(servidor.Targets);
    }
  }

  // Testa a liberação por rede CIDR para um nome resolvido, dentro e fora da rede
  [Theory]
  [InlineData("127.0.0.1/32", true)]
  [InlineData("127.0.0.0/8", true)]
  [InlineData("127.0.0.2/32", false)]
  [InlineData("10.0.0.0/8", false)]
  public async Task Guard_ShouldHonorAllowedNetworks(string rede, bool alcanca)
  {
    // Arrange
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver().Map("servico.exemplo.com", "127.0.0.1");
    using var provider = HttpTestHost.WithGuard(resolver, options => options.OutboundGuard.AllowedNetworks = [rede]);
    var chamada = () => provider.Service().GetAsync<Recibo>($"http://servico.exemplo.com:{servidor.Port}/", TestContext.Current.CancellationToken);

    // Act & Assert
    if (alcanca)
    {
      Assert.Equal("ABC-1", (await chamada())?.Protocolo);
    }
    else
    {
      await Assert.ThrowsAsync<OutboundBlockedException>(chamada);
      Assert.Empty(servidor.Targets);
    }
  }

  // Testa se o IPv4 mapeado em IPv6 é bloqueado como o IPv4 que carrega, vindo do DNS ou literal na URL
  [Fact]
  public async Task Guard_ShouldBlockIPv4MappedIPv6()
  {
    // Arrange
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver().Map("mapeado.exemplo.com", "::ffff:127.0.0.1");
    using var provider = HttpTestHost.WithGuard(resolver);
    var token = TestContext.Current.CancellationToken;

    // Act
    var peloDns = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
      provider.Service().GetAsync<Recibo>($"http://mapeado.exemplo.com:{servidor.Port}/", token));
    var literal = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
      provider.Service().GetAsync<Recibo>($"http://[::ffff:127.0.0.1]:{servidor.Port}/", token));

    // Assert
    Assert.Equal("mapeado.exemplo.com", peloDns.Host);
    Assert.Contains("Http.OutboundBlocked;", Assert.Single(literal.GetErrorMessages()));
    Assert.Empty(servidor.Targets);
  }

  // Testa se o IPv4 mapeado em IPv6 é liberado pela rede IPv4 e conectado como IPv4
  [Fact]
  public async Task Guard_ShouldConnectIPv4MappedIPv6_WhenIPv4NetworkIsAllowed()
  {
    // Arrange
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver().Map("mapeado.exemplo.com", "::ffff:127.0.0.1");
    using var provider = HttpTestHost.WithGuard(resolver, options => options.OutboundGuard.AllowedNetworks = ["127.0.0.0/8"]);

    // Act
    var recibo = await provider.Service().GetAsync<Recibo>($"http://mapeado.exemplo.com:{servidor.Port}/", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);
  }

  // Testa se basta um endereço bloqueado entre os resolvidos para o host inteiro ser recusado
  [Fact]
  public async Task Guard_ShouldBlockHost_WhenAnyResolvedAddressIsBlocked()
  {
    // Arrange: um nome com um endereço público e um interno é o desenho do ataque de rebinding
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver().Map("misto.exemplo.com", "93.184.216.34", "127.0.0.1");
    using var provider = HttpTestHost.WithGuard(resolver);

    // Act & Assert
    await Assert.ThrowsAsync<OutboundBlockedException>(() =>
      provider.Service().GetAsync<Recibo>($"http://misto.exemplo.com:{servidor.Port}/", TestContext.Current.CancellationToken));
    Assert.Empty(servidor.Targets);
  }

  // Testa se os endereços resolvidos são tentados em ordem, até um aceitar a conexão
  [Fact]
  public async Task Guard_ShouldTryResolvedAddressesInOrder()
  {
    // Arrange: nada escuta em 127.0.0.2, então a primeira tentativa falha e a segunda conecta
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver().Map("dois.exemplo.com", "127.0.0.2", "127.0.0.1");
    using var provider = HttpTestHost.WithGuard(resolver, options => options.OutboundGuard.AllowedNetworks = ["127.0.0.0/8"]);

    // Act
    var recibo = await provider.Service().GetAsync<Recibo>($"http://dois.exemplo.com:{servidor.Port}/", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);
    Assert.Single(resolver.Queries);
  }

  // Testa se a falha de conexão de um destino permitido continua sendo falha de conexão, e não bloqueio
  [Fact]
  public async Task Guard_ShouldMapConnectionFailure_WhenAllowedDestinationRefuses()
  {
    // Arrange: o servidor é encerrado para liberar a porta, e nada mais escuta nela
    int porta;

    await using (var servidor = Servidor())
    {
      porta = servidor.Port;
    }

    using var provider = HttpTestHost.WithGuard(new FakeHostResolver(), options =>
    {
      options.RetryCount = 0;
      options.OutboundGuard.AllowedNetworks = ["127.0.0.0/8"];
    });

    // Act
    var ex = await Assert.ThrowsAsync<BadGatewayException>(() =>
      provider.Service().GetAsync<Recibo>($"http://127.0.0.1:{porta}/", TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Http.ConnectionFailed", ex.GetErrorMessages());
  }

  // Testa se o salto de um redirecionamento para um endereço interno é bloqueado, porque abre a própria conexão
  [Fact]
  public async Task Guard_ShouldBlockRedirectToInternalAddress()
  {
    // Arrange: o destino liberado redireciona para um nome que resolve para um endereço interno
    LoopbackServer? servidor = null;
    servidor = new LoopbackServer(alvo => alvo == "/entrada"
      ? LoopbackServer.Redirect($"http://metadados.exemplo.net:{servidor!.Port}/segredo")
      : LoopbackServer.Json(ReciboJson));

    await using (servidor)
    {
      var resolver = new FakeHostResolver()
        .Map("webhook.exemplo.com", "127.0.0.1")
        .Map("metadados.exemplo.net", "127.0.0.1");
      using var provider = HttpTestHost.WithGuard(resolver, options => options.OutboundGuard.AllowedHosts = ["webhook.exemplo.com"]);

      // Act
      var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
        provider.Service().GetAsync<Recibo>($"http://webhook.exemplo.com:{servidor.Port}/entrada", TestContext.Current.CancellationToken));

      // Assert: só a primeira requisição chegou ao servidor
      Assert.Equal("metadados.exemplo.net", ex.Host);
      Assert.Equal("/entrada", Assert.Single(servidor.Targets));
      Assert.Equal(new[] { "webhook.exemplo.com", "metadados.exemplo.net" }, resolver.Queries);
    }
  }

  // Testa se o salto de um redirecionamento para um IP literal interno também é bloqueado
  [Fact]
  public async Task Guard_ShouldBlockRedirectToInternalLiteral()
  {
    // Arrange: só 127.0.0.1 está liberado, e o redirecionamento aponta para 127.0.0.2
    LoopbackServer? servidor = null;
    servidor = new LoopbackServer(_ => LoopbackServer.Redirect($"http://127.0.0.2:{servidor!.Port}/segredo"));

    await using (servidor)
    {
      using var provider = HttpTestHost.WithGuard(new FakeHostResolver(), options => options.OutboundGuard.AllowedNetworks = ["127.0.0.1/32"]);

      // Act
      var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
        provider.Service().GetAsync<Recibo>($"http://127.0.0.1:{servidor.Port}/entrada", TestContext.Current.CancellationToken));

      // Assert
      Assert.Equal("127.0.0.2", ex.Host);
      Assert.Single(servidor.Targets);
    }
  }

  // Testa se a guarda em um cliente da aplicação entrega a recusa como exceção do Tooark direto do HttpClient
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task AddTooarkOutboundGuard_ShouldBlockOnApplicationClient(bool comResiliencia)
  {
    // Arrange
    await using var servidor = Servidor();
    var resolver = new FakeHostResolver().Map("webhook.exemplo.com", "127.0.0.1");
    var services = new ServiceCollection();
    services.AddSingleton<IHostResolver>(resolver);

    var builder = services.AddHttpClient("webhooks");

    if (comResiliencia)
    {
      // A resiliência vem antes da guarda, como a documentação pede
      builder.AddStandardResilienceHandler();
    }

    builder.AddTooarkOutboundGuard();

    using var provider = services.BuildServiceProvider();
    using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("webhooks");

    // Act
    var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
      client.GetAsync($"http://webhook.exemplo.com:{servidor.Port}/", TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("Http.OutboundBlocked;webhook.exemplo.com", Assert.Single(ex.GetErrorMessages()));
    Assert.Single(resolver.Queries);
    Assert.Empty(servidor.Targets);
  }

  // Testa se a guarda em um cliente da aplicação respeita as listas informadas
  [Fact]
  public async Task AddTooarkOutboundGuard_ShouldHonorAllowLists()
  {
    // Arrange
    await using var servidor = Servidor();
    var services = new ServiceCollection();
    services.AddSingleton<IHostResolver>(new FakeHostResolver().Map("api.interna.exemplo.com", "127.0.0.1"));
    services.AddHttpClient("interno").AddTooarkOutboundGuard(options => options.AllowedHosts = [".interna.exemplo.com"]);

    using var provider = services.BuildServiceProvider();
    using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("interno");

    // Act
    using var response = await client.GetAsync($"http://api.interna.exemplo.com:{servidor.Port}/", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  // Monta um cliente com a guarda e um handler primário falso, como fica a conferência quando a conexão não passa
  // pelo ConnectCallback da guarda: sobra a conferência feita na requisição, com o DNS trocado
  private static (ServiceProvider Provider, HttpClient Client) ComProxy(FakeHttpMessageHandler handler, params string[] redes)
  {
    var services = new ServiceCollection();

    services.AddSingleton<IHostResolver>(new FakeHostResolver()
      .Map("nome-publico.exemplo.com", "93.184.216.34")
      .Map("webhook.exemplo.com", "93.184.216.34"));

    services.AddHttpClient("proxy")
      .AddTooarkOutboundGuard(options => options.AllowedNetworks = redes)
      .ConfigurePrimaryHttpMessageHandler(() => handler);

    var provider = services.BuildServiceProvider();

    return (provider, provider.GetRequiredService<IHttpClientFactory>().CreateClient("proxy"));
  }

  // Testa se o IP literal bloqueado no host da requisição é recusado antes de chegar à conexão
  [Theory]
  [InlineData("http://127.0.0.1/")]
  [InlineData("http://10.0.0.5:8080/admin")]
  [InlineData("http://169.254.169.254/latest/meta-data/")]
  [InlineData("http://[::1]/")]
  [InlineData("http://[::ffff:10.0.0.5]/")]
  [InlineData("http://[fd00::1]/")]
  // Formas alternativas de escrever 127.0.0.1, que a URI normaliza
  [InlineData("http://2130706433/")]
  [InlineData("http://0x7f.0.0.1/")]
  [InlineData("http://0177.0.0.1/")]
  public async Task RequestHostCheck_ShouldBlockInternalLiteral(string url)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.OK));
    var (provider, client) = ComProxy(handler);

    using (provider)
    using (client)
    {
      // Act
      var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() => client.GetAsync(url, TestContext.Current.CancellationToken));

      // Assert
      Assert.StartsWith("Http.OutboundBlocked;", Assert.Single(ex.GetErrorMessages()));
      Assert.Equal(0, handler.Calls);
    }
  }

  // Testa o que a conferência do host da requisição deixa passar: IP público, rede liberada e nome que resolve para
  // um endereço público
  [Theory]
  [InlineData("http://93.184.216.34/")]
  [InlineData("http://10.20.0.5/")]
  [InlineData("http://nome-publico.exemplo.com/")]
  public async Task RequestHostCheck_ShouldLetPublicLiteralAllowedNetworkAndPublicNamesThrough(string url)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.OK));
    var (provider, client) = ComProxy(handler, "10.20.0.0/16");

    using (provider)
    using (client)
    {
      // Act
      using var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(1, handler.Calls);
    }
  }

  // Testa se a guarda mantém a requisição fora do HTTP/3, cuja conexão QUIC não passa pela conferência
  [Theory]
  [InlineData(3, HttpVersionPolicy.RequestVersionExact, 2, HttpVersionPolicy.RequestVersionOrLower)]
  [InlineData(3, HttpVersionPolicy.RequestVersionOrLower, 2, HttpVersionPolicy.RequestVersionOrLower)]
  [InlineData(1, HttpVersionPolicy.RequestVersionOrHigher, 2, HttpVersionPolicy.RequestVersionOrLower)]
  [InlineData(2, HttpVersionPolicy.RequestVersionExact, 2, HttpVersionPolicy.RequestVersionExact)]
  [InlineData(1, HttpVersionPolicy.RequestVersionOrLower, 1, HttpVersionPolicy.RequestVersionOrLower)]
  public async Task RequestHostCheck_ShouldKeepRequestBelowHttp3(int versao, HttpVersionPolicy politica, int versaoEnviada, HttpVersionPolicy politicaEnviada)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.OK));
    var (provider, client) = ComProxy(handler);

    using (provider)
    using (client)
    {
      using var request = new HttpRequestMessage(HttpMethod.Get, "https://webhook.exemplo.com/")
      {
        Version = new Version(versao, versao == 1 ? 1 : 0),
        VersionPolicy = politica
      };

      // Act
      using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

      // Assert
      var enviada = Assert.Single(handler.Requests);
      Assert.Equal(versaoEnviada, enviada.Version.Major);
      Assert.Equal(politicaEnviada, enviada.VersionPolicy);
    }
  }
}
