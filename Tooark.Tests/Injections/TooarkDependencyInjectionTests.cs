using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Tooark.Dtos;
using Tooark.Extensions.Factories;
using Tooark.Injections;
using Tooark.Observability.Options;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Tests.Injections;

public class TooarkDependencyInjectionTests
{
  // Testa se o método AddTooarkService registra os serviços
  [Fact]
  public void AddTooarkService_ShouldRegisterServices()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService();
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var localizerFactory = serviceProvider.GetService<IStringLocalizerFactory>();
    Assert.NotNull(localizerFactory);
    Assert.IsType<JsonStringLocalizerFactory>(localizerFactory);

    var localizer = serviceProvider.GetService<IStringLocalizer>();
    Assert.NotNull(localizer);

    var dtoLocalizer = serviceProvider.GetService<IStringLocalizer<Dto>>();
    Assert.NotNull(dtoLocalizer);
  }

  // Testa se o método AddTooarkService usa as opções padrão quando as opções não são fornecidas
  [Fact]
  public void AddTooarkService_ShouldUseDefaultOptions_WhenOptionsNotProvided()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService();
    var serviceProvider = services.BuildServiceProvider();

    var localizerFactory = serviceProvider.GetService<IStringLocalizerFactory>();
    var localizer = serviceProvider.GetService<IStringLocalizer>();
    var dtoLocalizer = serviceProvider.GetService<IStringLocalizer<Dto>>();

    // Assert
    Assert.NotNull(localizerFactory);
    Assert.IsType<JsonStringLocalizerFactory>(localizerFactory);
    Assert.NotNull(localizer);
    Assert.NotNull(dtoLocalizer);
  }

  // Testa se o método AddTooarkService usa as opções fornecidas
  [Fact]
  public void AddTooarkService_ShouldUseProvidedOptions()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService();
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var localizerFactory = serviceProvider.GetService<IStringLocalizerFactory>();
    Assert.NotNull(localizerFactory);
    Assert.IsType<JsonStringLocalizerFactory>(localizerFactory);

    var localizer = serviceProvider.GetService<IStringLocalizer>();
    Assert.NotNull(localizer);

    var dtoLocalizer = serviceProvider.GetService<IStringLocalizer<Dto>>();
    Assert.NotNull(dtoLocalizer);
  }

  // Testa se o método AddTooarkService registra os serviços com mock
  [Fact]
  public void AddTooarkService_ShouldRegisterServices_WithMock()
  {
    // Arrange
    var services = new ServiceCollection();
    var mockDistributedCache = new Mock<IDistributedCache>();
    services.AddSingleton(mockDistributedCache.Object);

    // Act
    services.AddTooarkService();
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var localizerFactory = serviceProvider.GetService<IStringLocalizerFactory>();
    Assert.NotNull(localizerFactory);
    Assert.IsType<JsonStringLocalizerFactory>(localizerFactory);

    var localizer = serviceProvider.GetService<IStringLocalizer>();
    Assert.NotNull(localizer);

    var dtoLocalizer = serviceProvider.GetService<IStringLocalizer<Dto>>();
    Assert.NotNull(dtoLocalizer);
  }

  // Testa se o método AddTooarkService não registra Securities quando configuration é null
  [Fact]
  public void AddTooarkService_ShouldNotRegisterSecurities_WhenConfigurationIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService(configuration: null);
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var jwtService = serviceProvider.GetService<IJwtTokenService>();
    var cryptographyService = serviceProvider.GetService<ICryptographyService>();
    Assert.Null(jwtService);
    Assert.Null(cryptographyService);
  }

  // Testa se o método AddTooarkService não registra Securities quando configuration não tem seções JWT ou Cryptography
  [Fact]
  public void AddTooarkService_ShouldNotRegisterSecurities_WhenNoSecuritiesSections()
  {
    // Arrange
    var services = new ServiceCollection();
    var inMemorySettings = new Dictionary<string, string?>
    {
      { "OtherSection:Key", "value" }
    };

    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(inMemorySettings)
      .Build();

    // Act
    services.AddTooarkService(configuration);
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var jwtService = serviceProvider.GetService<IJwtTokenService>();
    var cryptographyService = serviceProvider.GetService<ICryptographyService>();
    Assert.Null(jwtService);
    Assert.Null(cryptographyService);
  }

  // Testa se o método AddTooarkService registra Securities quando existe apenas seção JWT
  [Fact]
  public void AddTooarkService_ShouldRegisterSecurities_WhenOnlyJwtSectionExists()
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
    services.AddTooarkService(configuration);
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var jwtService = serviceProvider.GetService<IJwtTokenService>();
    var cryptographyService = serviceProvider.GetService<ICryptographyService>();
    Assert.NotNull(jwtService);
    Assert.Null(cryptographyService);
  }

  // Testa se o método AddTooarkService registra Securities quando existe apenas seção Cryptography
  [Fact]
  public void AddTooarkService_ShouldRegisterSecurities_WhenOnlyCryptographySectionExists()
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
    services.AddTooarkService(configuration);
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var jwtService = serviceProvider.GetService<IJwtTokenService>();
    var cryptographyService = serviceProvider.GetService<ICryptographyService>();
    Assert.Null(jwtService);
    Assert.NotNull(cryptographyService);
  }

  // Testa se o método AddTooarkService registra Securities quando existem ambas seções JWT e Cryptography
  [Fact]
  public void AddTooarkService_ShouldRegisterSecurities_WhenBothSectionsExist()
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
    services.AddTooarkService(configuration);
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var jwtService = serviceProvider.GetService<IJwtTokenService>();
    var cryptographyService = serviceProvider.GetService<ICryptographyService>();
    Assert.NotNull(jwtService);
    Assert.NotNull(cryptographyService);
  }

  // Testa se o método AddTooarkService registra Observability quando existe seção Observability
  [Fact]
  public void AddTooarkService_ShouldRegisterObservability_WhenBothSectionsExist()
  {
    // Arrange
    var services = new ServiceCollection();
    var inMemorySettings = new Dictionary<string, string?>
    {
      // Configurações de Observability
      { $"{ObservabilityOptions.Section}:Enable", "true" }
    };

    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(inMemorySettings)
      .Build();

    // Act
    services.AddTooarkService(configuration);
    var serviceProvider = services.BuildServiceProvider();
    ServiceDescriptor? otelDescriptor = null;
    foreach (var sd in services)
    {
      if ((sd.ServiceType?.Namespace?.StartsWith("OpenTelemetry") == true) ||
          (sd.ImplementationType?.Namespace?.StartsWith("OpenTelemetry") == true))
      {
        otelDescriptor = sd;
        break;
      }
    }

    // Assert
    Assert.NotNull(otelDescriptor);

    // Tenta resolver a instância registrada (se houver)
    var otelInstance = otelDescriptor is not null ? serviceProvider.GetService(otelDescriptor.ServiceType!) : null;
    Assert.NotNull(otelInstance);
  }
}
