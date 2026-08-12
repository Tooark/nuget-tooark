using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Securities.Injections;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Injections;

public class TooarkDependencyInjectionSecuritiesTests
{
  // Teste para verificar se o método AddTooarkSecurities adiciona ambos os serviços quando ambas as configurações existem.
  [Fact]
  public void AddTooarkSecurities_WithBothConfigurations_ShouldAddBothServices()
  {
    // Arrange
    var services = new ServiceCollection();
    var inMemorySettings = new Dictionary<string, string?>
    {
      // Configurações do JWT
      { $"{JwtOptions.Section}:Algorithm", "HS256" },
      { $"{JwtOptions.Section}:Issuer", "issuer-test" },
      { $"{JwtOptions.Section}:Audience", "audience-test" },
      { $"{JwtOptions.Section}:Secret", "secret-test-with-minimum-length!" },
      { $"{JwtOptions.Section}:ExpirationTime", "60" },
      // Configurações de Criptografia
      { $"{CryptographyOptions.Section}:Algorithm", "GCM" },
      { $"{CryptographyOptions.Section}:Secret", "initialization-vector-test" }
    };

    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(inMemorySettings)
      .Build();

    // Act
    services.AddTooarkSecurities(configuration);
    var provider = services.BuildServiceProvider();
    var jwtService = provider.GetService<IJwtTokenService>();
    var cryptographyService = provider.GetService<ICryptographyService>();

    // Assert
    Assert.NotNull(jwtService);
    Assert.NotNull(cryptographyService);
  }

  // Teste para verificar se o método AddTooarkSecurities adiciona apenas o serviço JWT quando só a configuração JWT existe.
  [Fact]
  public void AddTooarkSecurities_WithOnlyJwtConfiguration_ShouldAddOnlyJwtService()
  {
    // Arrange
    var services = new ServiceCollection();
    var inMemorySettings = new Dictionary<string, string?>
    {
      { $"{JwtOptions.Section}:Algorithm", "HS256" },
      { $"{JwtOptions.Section}:Issuer", "issuer-test" },
      { $"{JwtOptions.Section}:Audience", "audience-test" },
      { $"{JwtOptions.Section}:Secret", "secret-test-with-minimum-length!" },
      { $"{JwtOptions.Section}:ExpirationTime", "60" }
    };

    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(inMemorySettings)
      .Build();

    // Act
    services.AddTooarkSecurities(configuration);
    var provider = services.BuildServiceProvider();
    var jwtService = provider.GetService<IJwtTokenService>();
    var cryptographyService = provider.GetService<ICryptographyService>();

    // Assert
    Assert.NotNull(jwtService);
    Assert.Null(cryptographyService);
  }

  // Teste para verificar se o método AddTooarkSecurities adiciona apenas o serviço de Criptografia quando só a configuração de Criptografia existe.
  [Fact]
  public void AddTooarkSecurities_WithOnlyCryptographyConfiguration_ShouldAddOnlyCryptographyService()
  {
    // Arrange
    var services = new ServiceCollection();
    var inMemorySettings = new Dictionary<string, string?>
    {
      { $"{CryptographyOptions.Section}:Algorithm", "GCM" },
      { $"{CryptographyOptions.Section}:Secret", "initialization-vector-test" }
    };

    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(inMemorySettings)
      .Build();

    // Act
    services.AddTooarkSecurities(configuration);
    var provider = services.BuildServiceProvider();
    var jwtService = provider.GetService<IJwtTokenService>();
    var cryptographyService = provider.GetService<ICryptographyService>();

    // Assert
    Assert.Null(jwtService);
    Assert.NotNull(cryptographyService);
  }

  // Teste para verificar se o método AddTooarkSecurities não adiciona nenhum serviço quando nenhuma configuração existe.
  [Fact]
  public void AddTooarkSecurities_WithNoConfiguration_ShouldNotAddAnyService()
  {
    // Arrange
    var services = new ServiceCollection();
    var inMemorySettings = new Dictionary<string, string?>();

    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(inMemorySettings)
      .Build();

    // Act
    services.AddTooarkSecurities(configuration);
    var provider = services.BuildServiceProvider();
    var jwtService = provider.GetService<IJwtTokenService>();
    var cryptographyService = provider.GetService<ICryptographyService>();

    // Assert
    Assert.Null(jwtService);
    Assert.Null(cryptographyService);
  }

  // Teste para verificar se o método AddTooarkSecurities retorna a mesma instância de IServiceCollection.
  [Fact]
  public void AddTooarkSecurities_ShouldReturnSameServiceCollection()
  {
    // Arrange
    var services = new ServiceCollection();
    var inMemorySettings = new Dictionary<string, string?>();

    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(inMemorySettings)
      .Build();

    // Act
    var result = services.AddTooarkSecurities(configuration);

    // Assert
    Assert.Same(services, result);
  }
}
