using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

  // Teste para verificar se o método AddTooarkSecurities registra o Data Protection quando a seção existe.
  [Fact]
  public void AddTooarkSecurities_WithDataProtectionConfiguration_ShouldAddDataProtection()
  {
    // Arrange
    var services = new ServiceCollection();
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        { $"{KeyRingOptions.Section}:ApplicationName", "tooark-app" }
      })
      .Build();

    // Act
    services.AddTooarkSecurities(configuration);
    using var provider = services.BuildServiceProvider();
    var dataProtection = provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

    // Assert
    Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDataProtectionProvider));
    Assert.Equal("tooark-app", dataProtection.ApplicationDiscriminator);
  }

  // Teste para verificar se o método AddTooarkSecurities não registra o Data Protection quando a seção não existe.
  [Fact]
  public void AddTooarkSecurities_WithoutDataProtectionConfiguration_ShouldNotAddDataProtection()
  {
    // Arrange
    var services = new ServiceCollection();
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>())
      .Build();

    // Act
    services.AddTooarkSecurities(configuration);

    // Assert
    Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IDataProtectionProvider));
  }

  // Teste para verificar se uma chamada explícita a AddTooarkDataProtection tem prioridade sobre a seção.
  [Fact]
  public void AddTooarkSecurities_AfterAddTooarkDataProtection_ShouldKeepExplicitOptions()
  {
    // Arrange
    var services = new ServiceCollection();
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        { $"{KeyRingOptions.Section}:ApplicationName", "from-section" }
      })
      .Build();

    // Act
    services.AddTooarkDataProtection(configuration, options => options.ApplicationName = "from-code");
    services.AddTooarkSecurities(configuration);
    using var provider = services.BuildServiceProvider();
    var dataProtection = provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

    // Assert
    Assert.Equal("from-code", dataProtection.ApplicationDiscriminator);
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
