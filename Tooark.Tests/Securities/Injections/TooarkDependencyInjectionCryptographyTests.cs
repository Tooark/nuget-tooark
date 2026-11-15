using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Securities;
using Tooark.Securities.Injections;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Injections;

public class TooarkDependencyInjectionCryptographyTests
{
  // Teste para verificar se o método AddTooarkCryptography adiciona o serviço corretamente.
  [Fact]
  public void AddTooarkCryptography_ShouldAddServices()
  {
    // Arrange
    var services = new ServiceCollection();
    var inMemorySettings = new Dictionary<string, string?>
    {
      { $"{CryptographyOptions.Section}:Algorithm", "GCM" },
      { $"{CryptographyOptions.Section}:SecretBase64", Tooark.Tests.Securities.CryptographyTestKeys.FromText("initialization-vector-test") }
    };

    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(inMemorySettings)
      .Build();

    // Act
    services.AddTooarkCryptography(configuration);
    var provider = services.BuildServiceProvider();
    var cryptographyService = provider.GetService<ICryptographyService>();
    var options = provider.GetService<Microsoft.Extensions.Options.IOptions<CryptographyOptions>>();

    // Assert
    Assert.NotNull(cryptographyService);
    Assert.NotNull(options);
    Assert.Equal("GCM", options.Value.Algorithm);
    Assert.Equal(CryptographyTestKeys.FromText("initialization-vector-test"), options.Value.SecretBase64);
  }

  // Testa se um Secret em texto esquecido na configuração, removido na v5, é ignorado e o serviço falha por
  // falta de chave, em vez de cifrar com uma chave que ninguém configurou
  [Fact]
  public void AddTooarkCryptography_WithOnlyTheRemovedTextSecret_ShouldFailForMissingKey()
  {
    // Arrange
    var services = new ServiceCollection();
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        { $"{CryptographyOptions.Section}:Secret", "initialization-vector-test" }
      })
      .Build();

    services.AddTooarkCryptography(configuration);
    using var provider = services.BuildServiceProvider();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => provider.GetRequiredService<ICryptographyService>());

    // Assert
    Assert.Equal("Options.Cryptography.SecretNotConfigured", exception.Message);
  }

  // Testa se um algoritmo digitado errado na configuração falha no registro, em vez de cair em GCM em silêncio
  [Fact]
  public void AddTooarkCryptography_WithUnknownAlgorithm_ShouldFailAtRegistration()
  {
    // Arrange
    var services = new ServiceCollection();
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        { $"{CryptographyOptions.Section}:Algorithm", "GMC" },
        { $"{CryptographyOptions.Section}:SecretBase64", CryptographyTestKeys.FromText("initialization-vector-test") }
      })
      .Build();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkCryptography(configuration));

    // Assert
    Assert.Equal("Options.Cryptography.AlgorithmNotSupported;GMC", exception.Message);
  }

  // Testa se sem chaves identificadas o serviço de chave única é registrado e o identificado não existe
  [Fact]
  public void AddTooarkCryptography_WithoutKeys_ShouldRegisterSingleKeyServiceOnly()
  {
    // Arrange
    var services = new ServiceCollection();
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        { $"{CryptographyOptions.Section}:SecretBase64", Tooark.Tests.Securities.CryptographyTestKeys.FromText("initialization-vector-test") }
      })
      .Build();

    // Act
    services.AddTooarkCryptography(configuration);
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.IsType<CryptographyService>(provider.GetRequiredService<ICryptographyService>());
    Assert.Null(provider.GetService<IKeyedCryptographyService>());
  }

  // Testa se com chaves identificadas um único serviço atende às duas interfaces e cifra com a chave corrente
  [Fact]
  public void AddTooarkCryptography_WithKeys_ShouldRegisterKeyedServiceForBothInterfaces()
  {
    // Arrange
    var services = new ServiceCollection();
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        { $"{CryptographyOptions.Section}:Algorithm", "GCM" },
        { $"{CryptographyOptions.Section}:CurrentKeyId", "k2" },
        { $"{CryptographyOptions.Section}:Keys:k1", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) },
        { $"{CryptographyOptions.Section}:Keys:k2", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }
      })
      .Build();

    // Act
    services.AddTooarkCryptography(configuration);
    using var provider = services.BuildServiceProvider();
    var cryptography = provider.GetRequiredService<ICryptographyService>();
    var keyed = provider.GetRequiredService<IKeyedCryptographyService>();

    // Assert
    Assert.IsType<KeyedCryptographyService>(cryptography);
    Assert.Same(cryptography, keyed);
    Assert.Equal("k2", keyed.CurrentKeyId);
    Assert.StartsWith("tk1.k2.", cryptography.Encrypt("texto"));
  }

  // Testa se chaves identificadas inválidas falham no registro, e não na primeira cifra
  [Fact]
  public void AddTooarkCryptography_WithInvalidKeys_ShouldThrowAtRegistration()
  {
    // Arrange
    var services = new ServiceCollection();
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        { $"{CryptographyOptions.Section}:Keys:k1", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }
      })
      .Build();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkCryptography(configuration));

    // Assert
    Assert.Equal("Options.Cryptography.CurrentKeyId.NotConfigured", exception.Message);
  }
}
