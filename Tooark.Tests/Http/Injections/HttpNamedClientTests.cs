using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Http;
using Tooark.Http.Exceptions;
using Tooark.Http.Injections;
using Tooark.Http.Interfaces;
using Tooark.Http.Options;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http.Injections;

/// <summary>
/// Testes dos clientes HTTP com nome: registro, opções próprias por destino e resolução pela chave.
/// </summary>
public class HttpNamedClientTests
{
  // Corpo lido das respostas
  private sealed record Recibo(string Protocolo);

  // Resposta JSON padrão dos testes
  private const string ReciboJson = """{"protocolo":"ABC-1"}""";

  // Monta a configuração a partir de pares chave e valor
  private static IConfiguration Configuracao(Dictionary<string, string?> valores) =>
    new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

  // Testa se o cliente com nome é resolvido pela chave e envia pelo cliente de mesmo nome da fábrica
  [Fact]
  public async Task AddTooarkNamedHttpClient_ShouldRegisterKeyedServiceAndNamedClient()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    var services = new ServiceCollection();

    services.AddTooarkNamedHttpClient("pagamentos", options => options.BaseAddress = "https://pagamentos.exemplo.com/v2");
    services.AddHttpClient("pagamentos").ConfigurePrimaryHttpMessageHandler(() => handler);

    using var provider = services.BuildServiceProvider();

    // Act
    var service = provider.GetRequiredKeyedService<IHttpClientService>("pagamentos");
    var recibo = await service.GetAsync<Recibo>("recibos/1", TestContext.Current.CancellationToken);

    // Assert
    Assert.IsType<HttpClientService>(service);
    Assert.Equal("ABC-1", recibo?.Protocolo);
    Assert.Equal("https://pagamentos.exemplo.com/v2/recibos/1", Assert.Single(handler.Requests).RequestUri?.AbsoluteUri);

    // O cliente padrão não é registrado por tabela
    Assert.Null(provider.GetService<IHttpClientService>());
  }

  // Testa se cada cliente tem o próprio endereço base e o próprio tempo limite, sem alterar o cliente padrão
  [Fact]
  public void AddTooarkNamedHttpClient_ShouldKeepOptionsPerClient()
  {
    // Arrange
    var services = new ServiceCollection();

    services.AddTooarkHttpClient(options => options.BaseAddress = "https://api.exemplo.com");
    services.AddTooarkNamedHttpClient("modelos", options =>
    {
      options.BaseAddress = "https://modelos.exemplo.com";
      options.TotalTimeoutSeconds = 600;
      options.AttemptTimeoutSeconds = 600;
      options.CircuitBreakerSamplingSeconds = 1200;
      options.RetryNonIdempotentMethods = false;
    });
    services.AddTooarkNamedHttpClient("pagamentos", options => options.Resilience = false);

    using var provider = services.BuildServiceProvider();
    var monitor = provider.GetRequiredService<IOptionsMonitor<HttpClientOptions>>();
    var factory = provider.GetRequiredService<IHttpClientFactory>();

    // Act
    using var padrao = factory.CreateClient(HttpClientService.ClientName);
    using var modelos = factory.CreateClient("modelos");
    using var pagamentos = factory.CreateClient("pagamentos");

    // Assert
    Assert.Equal("https://api.exemplo.com/", padrao.BaseAddress?.AbsoluteUri);
    Assert.Equal("https://modelos.exemplo.com/", modelos.BaseAddress?.AbsoluteUri);
    Assert.Null(pagamentos.BaseAddress);

    Assert.Equal(30, monitor.CurrentValue.TotalTimeoutSeconds);
    Assert.Equal(600, monitor.Get("modelos").TotalTimeoutSeconds);
    Assert.False(monitor.Get("modelos").RetryNonIdempotentMethods);
    Assert.Equal(30, monitor.Get("pagamentos").TotalTimeoutSeconds);

    // Sem a resiliência, o tempo limite total vai para o próprio cliente
    Assert.Equal(Timeout.InfiniteTimeSpan, modelos.Timeout);
    Assert.Equal(TimeSpan.FromSeconds(30), pagamentos.Timeout);

    Assert.NotSame(
      provider.GetRequiredKeyedService<IHttpClientService>("modelos"),
      provider.GetRequiredKeyedService<IHttpClientService>("pagamentos"));
    Assert.NotSame(
      provider.GetRequiredService<IHttpClientService>(),
      provider.GetRequiredKeyedService<IHttpClientService>("modelos"));
  }

  // Testa se o cliente com nome lê a própria seção, com o ajuste do código por cima
  [Fact]
  public void AddTooarkNamedHttpClient_ShouldBindItsOwnSection()
  {
    // Arrange
    var configuration = Configuracao(new()
    {
      ["HttpClient:TotalTimeoutSeconds"] = "20",
      ["HttpClients:modelos:BaseAddress"] = "https://modelos.exemplo.com/v1",
      ["HttpClients:modelos:TotalTimeoutSeconds"] = "300",
      ["HttpClients:modelos:AttemptTimeoutSeconds"] = "300",
      ["HttpClients:modelos:CircuitBreakerSamplingSeconds"] = "600",
      ["HttpClients:modelos:RetryNonIdempotentMethods"] = "false",
      ["HttpClients:pagamentos:RetryCount"] = "1"
    });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkHttpClient(configuration);
    services.AddTooarkNamedHttpClient("modelos", configuration, options => options.RetryCount = 0);
    services.AddTooarkNamedHttpClient("pagamentos", configuration);

    using var provider = services.BuildServiceProvider();
    var monitor = provider.GetRequiredService<IOptionsMonitor<HttpClientOptions>>();

    // Assert
    Assert.Equal(20, monitor.CurrentValue.TotalTimeoutSeconds);

    var modelos = monitor.Get("modelos");
    Assert.Equal("https://modelos.exemplo.com/v1", modelos.BaseAddress);
    Assert.Equal(300, modelos.TotalTimeoutSeconds);
    Assert.False(modelos.RetryNonIdempotentMethods);
    Assert.Equal(0, modelos.RetryCount);

    var pagamentos = monitor.Get("pagamentos");
    Assert.Equal(1, pagamentos.RetryCount);
    Assert.Equal(30, pagamentos.TotalTimeoutSeconds);
  }

  // Testa se uma opção inválida do cliente com nome falha no registro, e não na primeira requisição
  [Fact]
  public void AddTooarkNamedHttpClient_ShouldThrowOnRegistration_WhenOptionsAreInvalid()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = Configuracao(new() { ["HttpClients:modelos:TotalTimeoutSeconds"] = "0" });

    // Act
    var daConfiguracao = Assert.Throws<InternalServerErrorException>(() =>
      services.AddTooarkNamedHttpClient("modelos", configuration));
    var doCodigo = Assert.Throws<InternalServerErrorException>(() =>
      services.AddTooarkNamedHttpClient("modelos", options => options.RetryCount = -1));

    // Assert
    Assert.Contains("Options.HttpClient.TotalTimeoutInvalid;1;3600", daConfiguracao.GetErrorMessages());
    Assert.Contains("Options.HttpClient.RetryCountInvalid;0;10", doCodigo.GetErrorMessages());
  }

  // Testa se a configuração estrita aponta a seção do cliente com nome e a chave desconhecida
  [Fact]
  public void AddTooarkNamedHttpClient_ShouldRejectUnknownKeys_WhenStrictConfigurationIsOn()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = Configuracao(new()
    {
      ["Tooark:StrictConfiguration"] = "true",
      ["HttpClients:modelos:TotalTimeout"] = "300"
    });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkNamedHttpClient("modelos", configuration));

    // Assert
    Assert.Equal("Options.Configuration.UnknownKey;HttpClients:modelos;TotalTimeout", ex.Message);
  }

  // Testa se o nome vazio, com dois-pontos ou igual ao do cliente padrão é recusado
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("pagamentos:v2")]
  [InlineData(HttpClientService.ClientName)]
  public void AddTooarkNamedHttpClient_ShouldRejectInvalidNames(string nome)
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    Assert.ThrowsAny<ArgumentException>(() => services.AddTooarkNamedHttpClient(nome));
    Assert.ThrowsAny<ArgumentException>(() => services.AddTooarkNamedHttpClient(nome, Configuracao(new())));
    Assert.Empty(services);
  }

  // Testa se o registro repetido do mesmo nome não empilha outro handler de resiliência, e se nomes diferentes
  // têm pipelines independentes
  [Fact]
  public async Task AddTooarkNamedHttpClient_ShouldNotStackHandlers_WhenCalledTwice()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.ServiceUnavailable));
    var outro = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.ServiceUnavailable));
    var services = new ServiceCollection();

    // Act
    services.AddTooarkNamedHttpClient("pagamentos", options => options.RetryDelayMilliseconds = 1);
    services.AddTooarkNamedHttpClient("pagamentos", options => options.RetryDelayMilliseconds = 1);
    services.AddTooarkNamedHttpClient("modelos", options =>
    {
      options.RetryDelayMilliseconds = 1;
      options.RetryCount = 1;
    });
    services.AddHttpClient("pagamentos").ConfigurePrimaryHttpMessageHandler(() => handler);
    services.AddHttpClient("modelos").ConfigurePrimaryHttpMessageHandler(() => outro);
    using var provider = services.BuildServiceProvider();

    await Assert.ThrowsAsync<HttpRequestFailedException>(() => provider
      .GetRequiredKeyedService<IHttpClientService>("pagamentos")
      .GetAsync<string>("https://api.exemplo.com/", TestContext.Current.CancellationToken));
    await Assert.ThrowsAsync<HttpRequestFailedException>(() => provider
      .GetRequiredKeyedService<IHttpClientService>("modelos")
      .GetAsync<string>("https://api.exemplo.com/", TestContext.Current.CancellationToken));

    // Assert: uma chamada e três novas tentativas no primeiro; uma chamada e uma nova tentativa no segundo
    Assert.Equal(4, handler.Calls);
    Assert.Equal(2, outro.Calls);
    Assert.Equal(2, services.Count(descriptor => descriptor.ServiceType == typeof(IHttpClientService)));
  }

  // Testa se o registro de um cliente com nome não impede nem substitui o registro do cliente padrão, em qualquer ordem
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void AddTooarkNamedHttpClient_ShouldCoexistWithTheDefaultClient(bool padraoPrimeiro)
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    if (padraoPrimeiro)
    {
      services.AddTooarkHttpClient(options => options.RetryCount = 5);
      services.AddTooarkNamedHttpClient("modelos", options => options.RetryCount = 1);
    }
    else
    {
      services.AddTooarkNamedHttpClient("modelos", options => options.RetryCount = 1);
      services.AddTooarkHttpClient(options => options.RetryCount = 5);
    }

    using var provider = services.BuildServiceProvider();
    var monitor = provider.GetRequiredService<IOptionsMonitor<HttpClientOptions>>();

    // Assert
    Assert.Equal(5, monitor.CurrentValue.RetryCount);
    Assert.Equal(5, provider.GetRequiredService<IOptions<HttpClientOptions>>().Value.RetryCount);
    Assert.Equal(1, monitor.Get("modelos").RetryCount);
    Assert.NotNull(provider.GetRequiredService<IHttpClientService>());
    Assert.NotNull(provider.GetRequiredKeyedService<IHttpClientService>("modelos"));
  }

  // Testa se a guarda de saída vale só para o cliente em que foi ligada
  [Fact]
  public async Task AddTooarkNamedHttpClient_ShouldApplyTheOutboundGuardPerClient()
  {
    // Arrange
    var services = new ServiceCollection();

    services.AddSingleton<Tooark.Http.Internal.IHostResolver>(new FakeHostResolver());
    services.AddTooarkNamedHttpClient("webhooks", options =>
    {
      options.OutboundGuard.Enabled = true;
      options.RetryCount = 0;
    });

    using var provider = services.BuildServiceProvider();
    var service = provider.GetRequiredKeyedService<IHttpClientService>("webhooks");

    // Act
    var ex = await Assert.ThrowsAsync<OutboundBlockedException>(() =>
      service.GetAsync<string>("http://169.254.169.254/latest/meta-data", TestContext.Current.CancellationToken));

    // Assert
    Assert.Equal("Http.OutboundBlocked;169.254.169.254", ex.Message);
  }

  // Testa se o serviço criado à mão recusa o nome vazio e pede à fábrica o cliente do nome informado
  [Fact]
  public async Task HttpClientService_ShouldUseTheGivenClientName()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, ReciboJson));
    var services = new ServiceCollection();

    services.AddHttpClient("manual").ConfigurePrimaryHttpMessageHandler(() => handler);

    using var provider = services.BuildServiceProvider();
    var factory = provider.GetRequiredService<IHttpClientFactory>();

    // Act
    var service = new HttpClientService(factory, new HttpClientOptions(), "manual");
    var recibo = await service.GetAsync<Recibo>("https://api.exemplo.com/recibos/1", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("ABC-1", recibo?.Protocolo);
    Assert.Equal(1, handler.Calls);
    Assert.ThrowsAny<ArgumentException>(() => new HttpClientService(factory, new HttpClientOptions(), " "));
    Assert.Throws<ArgumentNullException>(() => new HttpClientService(factory, (HttpClientOptions)null!, "manual"));
    Assert.Throws<InternalServerErrorException>(() =>
      new HttpClientService(factory, new HttpClientOptions { TotalTimeoutSeconds = 0 }, "manual"));
  }
}
