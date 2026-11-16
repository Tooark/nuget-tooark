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
/// Testes do registro do cliente HTTP e da guarda de saída no container de injeção de dependência.
/// </summary>
public class HttpInjectionTests
{
  // Monta a configuração a partir dos pares informados
  private static IConfiguration Configuracao(Dictionary<string, string?> valores) =>
    new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

  // Testa se o serviço e o cliente nomeado são registrados sem configuração
  [Fact]
  public void AddTooarkHttpClient_ShouldRegisterServiceAndNamedClient_WithoutConfiguration()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkHttpClient();
    using var provider = services.BuildServiceProvider();
    var service = provider.GetRequiredService<IHttpClientService>();
    using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientService.ClientName);

    // Assert
    Assert.IsType<HttpClientService>(service);
    Assert.Same(service, provider.GetRequiredService<IHttpClientService>());
    Assert.Null(client.BaseAddress);
    // Com a resiliência, o tempo limite é o do pipeline e o do serviço, e não o do cliente
    Assert.Equal(Timeout.InfiniteTimeSpan, client.Timeout);
  }

  // Testa se as opções são lidas da seção HttpClient
  [Fact]
  public void AddTooarkHttpClient_ShouldBindOptionsFromConfiguration()
  {
    // Arrange
    var configuracao = Configuracao(new()
    {
      ["HttpClient:BaseAddress"] = "https://api.exemplo.com/v1",
      ["HttpClient:TotalTimeoutSeconds"] = "20",
      ["HttpClient:AttemptTimeoutSeconds"] = "5",
      ["HttpClient:RetryCount"] = "1",
      ["HttpClient:RetryDelayMilliseconds"] = "50",
      ["HttpClient:CircuitBreakerFailureRatio"] = "0.5",
      ["HttpClient:CircuitBreakerMinimumThroughput"] = "10",
      ["HttpClient:CircuitBreakerSamplingSeconds"] = "60",
      ["HttpClient:CircuitBreakerBreakSeconds"] = "15",
      ["HttpClient:Resilience"] = "false",
      ["HttpClient:OutboundGuard:Enabled"] = "true",
      ["HttpClient:OutboundGuard:AllowedHosts:0"] = ".interna.exemplo.com",
      ["HttpClient:OutboundGuard:AllowedNetworks:0"] = "10.20.0.0/16",
      ["HttpClient:OutboundGuard:AllowUnresolvedHostsBehindProxy"] = "true"
    });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkHttpClient(configuracao);
    using var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<IOptions<HttpClientOptions>>().Value;
    using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientService.ClientName);

    // Assert
    Assert.Equal("https://api.exemplo.com/v1", options.BaseAddress);
    Assert.Equal(20, options.TotalTimeoutSeconds);
    Assert.Equal(5, options.AttemptTimeoutSeconds);
    Assert.Equal(1, options.RetryCount);
    Assert.Equal(50, options.RetryDelayMilliseconds);
    Assert.Equal(0.5, options.CircuitBreakerFailureRatio);
    Assert.Equal(10, options.CircuitBreakerMinimumThroughput);
    Assert.Equal(60, options.CircuitBreakerSamplingSeconds);
    Assert.Equal(15, options.CircuitBreakerBreakSeconds);
    Assert.False(options.Resilience);
    Assert.True(options.OutboundGuard.Enabled);
    Assert.Equal(".interna.exemplo.com", Assert.Single(options.OutboundGuard.AllowedHosts));
    Assert.Equal("10.20.0.0/16", Assert.Single(options.OutboundGuard.AllowedNetworks));
    Assert.True(options.OutboundGuard.AllowUnresolvedHostsBehindProxy);

    // O endereço base ganha a barra final, e sem a resiliência o cliente aplica o tempo limite total
    Assert.Equal("https://api.exemplo.com/v1/", client.BaseAddress?.AbsoluteUri);
    Assert.Equal(TimeSpan.FromSeconds(20), client.Timeout);
  }

  // Testa se o ajuste programático vale por cima da configuração
  [Fact]
  public void AddTooarkHttpClient_ShouldApplyConfigureAfterConfiguration()
  {
    // Arrange
    var configuracao = Configuracao(new() { ["HttpClient:RetryCount"] = "1" });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkHttpClient(configuracao, options => options.RetryCount = 5);
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.Equal(5, provider.GetRequiredService<IOptions<HttpClientOptions>>().Value.RetryCount);
  }

  // Testa se a configuração inválida falha no registro, e não na primeira requisição
  [Theory]
  [InlineData("HttpClient:RetryCount", "99", "Options.HttpClient.RetryCountInvalid;0;10")]
  [InlineData("HttpClient:BaseAddress", "api.exemplo.com", "Options.HttpClient.BaseAddressInvalid")]
  [InlineData("HttpClient:AttemptTimeoutSeconds", "20", "Options.HttpClient.CircuitBreakerSamplingInvalid;40;86400")]
  [InlineData("HttpClient:OutboundGuard:AllowedNetworks:0", "10.0.0.0/40", "Options.HttpClient.OutboundGuard.AllowedNetworkInvalid;10.0.0.0/40")]
  public void AddTooarkHttpClient_ShouldThrowOnRegistration_WhenConfigurationIsInvalid(string chave, string valor, string mensagem)
  {
    // Arrange
    var configuracao = Configuracao(new() { [chave] = valor });
    var services = new ServiceCollection();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkHttpClient(configuracao));

    // Assert
    Assert.Contains(mensagem, ex.GetErrorMessages());
    Assert.Empty(services);
  }

  // Testa se o ajuste programático inválido também falha no registro
  [Fact]
  public void AddTooarkHttpClient_ShouldThrowOnRegistration_WhenConfigureIsInvalid()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() =>
      services.AddTooarkHttpClient(options => options.TotalTimeoutSeconds = 0));

    // Assert
    Assert.Contains("Options.HttpClient.TotalTimeoutInvalid;1;3600", ex.GetErrorMessages());
  }

  // Testa se o registro repetido não duplica o serviço nem empilha outro handler de resiliência
  [Fact]
  public async Task AddTooarkHttpClient_ShouldNotStackHandlers_WhenCalledTwice()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.ServiceUnavailable));
    var services = new ServiceCollection();

    // Act
    services.AddTooarkHttpClient(options => options.RetryDelayMilliseconds = 1);
    services.AddTooarkHttpClient(options => options.RetryDelayMilliseconds = 1);
    services.AddHttpClient(HttpClientService.ClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
    using var provider = services.BuildServiceProvider();

    await Assert.ThrowsAsync<HttpRequestFailedException>(() =>
      provider.Service().GetAsync<string>("https://api.exemplo.com/", TestContext.Current.CancellationToken));

    // Assert: uma chamada e três novas tentativas; dois handlers de resiliência dariam dezesseis
    Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IHttpClientService));
    Assert.Equal(4, handler.Calls);
  }

  // Testa se o segundo registro ainda valida as próprias opções
  [Fact]
  public void AddTooarkHttpClient_ShouldValidateOptions_WhenCalledTwice()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTooarkHttpClient();

    // Act & Assert
    Assert.Throws<InternalServerErrorException>(() => services.AddTooarkHttpClient(options => options.RetryCount = -1));
  }

  // Testa se a guarda em um cliente da aplicação falha no registro com uma rede liberada inválida
  [Fact]
  public void AddTooarkOutboundGuard_ShouldThrowOnRegistration_WhenNetworkIsInvalid()
  {
    // Arrange
    var builder = new ServiceCollection().AddHttpClient("webhooks");

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() =>
      builder.AddTooarkOutboundGuard(options => options.AllowedNetworks = ["rede-interna"]));

    // Assert
    Assert.Contains("Options.HttpClient.OutboundGuard.AllowedNetworkInvalid;rede-interna", ex.GetErrorMessages());
  }

  // Testa se a guarda em um cliente da aplicação entra ligada e troca o handler primário por um SocketsHttpHandler com o ConnectCallback
  [Fact]
  public void AddTooarkOutboundGuard_ShouldInstallConnectCallback()
  {
    // Arrange
    var services = new ServiceCollection();
    SocketsHttpHandler? primario = null;

    // Act
    services.AddHttpClient("webhooks")
      .AddTooarkOutboundGuard()
      .ConfigurePrimaryHttpMessageHandler((handler, _) => primario = handler as SocketsHttpHandler);
    using var provider = services.BuildServiceProvider();
    using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("webhooks");

    // Assert
    Assert.NotNull(primario?.ConnectCallback);
  }

  // Testa se um SocketsHttpHandler já configurado pela aplicação é mantido, só com o ConnectCallback da guarda
  [Fact]
  public void AddTooarkOutboundGuard_ShouldKeepConfiguredSocketsHttpHandler()
  {
    // Arrange
    var services = new ServiceCollection();
    var configurado = new SocketsHttpHandler { MaxConnectionsPerServer = 7, AllowAutoRedirect = false };
    HttpMessageHandler? primario = null;

    // Act
    services.AddHttpClient("webhooks")
      .ConfigurePrimaryHttpMessageHandler(() => configurado)
      .AddTooarkOutboundGuard()
      .ConfigurePrimaryHttpMessageHandler((handler, _) => primario = handler);
    using var provider = services.BuildServiceProvider();
    using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("webhooks");

    // Assert
    Assert.Same(configurado, primario);
    Assert.Equal(7, configurado.MaxConnectionsPerServer);
    Assert.False(configurado.AllowAutoRedirect);
    Assert.NotNull(configurado.ConnectCallback);
  }

  // Testa se um handler primário de outro tipo é substituído, porque só o SocketsHttpHandler tem o ConnectCallback
  [Fact]
  public void AddTooarkOutboundGuard_ShouldReplaceOtherPrimaryHandlerTypes()
  {
    // Arrange
    var services = new ServiceCollection();
    using var configurado = new HttpClientHandler();
    HttpMessageHandler? primario = null;

    // Act
    services.AddHttpClient("webhooks")
      .ConfigurePrimaryHttpMessageHandler(() => configurado)
      .AddTooarkOutboundGuard()
      .ConfigurePrimaryHttpMessageHandler((handler, _) => primario = handler);
    using var provider = services.BuildServiceProvider();
    using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("webhooks");

    // Assert
    Assert.NotNull(Assert.IsType<SocketsHttpHandler>(primario).ConnectCallback);
  }

  // Testa se a guarda desligada explicitamente não mexe no cliente
  [Fact]
  public void AddTooarkOutboundGuard_ShouldLeaveClientUntouched_WhenDisabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var builder = services.AddHttpClient("webhooks");
    var antes = services.Count;

    // Act
    builder.AddTooarkOutboundGuard(options => options.Enabled = false);

    // Assert
    Assert.Equal(antes, services.Count);
  }
}
