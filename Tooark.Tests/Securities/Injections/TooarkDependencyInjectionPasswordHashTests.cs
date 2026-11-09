using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Securities;
using Tooark.Securities.Injections;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Injections;

public class TooarkDependencyInjectionPasswordHashTests
{
  // Monta a configuração em memória da seção PasswordHash
  private static IConfiguration Configuration(Dictionary<string, string?>? settings = null) =>
    new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();

  // Testa se o serviço é registrado com os padrões quando não há configuração nem seção
  [Fact]
  public void AddTooarkPasswordHash_ShouldRegisterService_WithDefaults()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkPasswordHash();
    using var provider = services.BuildServiceProvider();
    var service = provider.GetRequiredService<IPasswordHashService>();

    // Assert
    Assert.IsType<PasswordHashService>(service);
    Assert.Contains("$i=600000$", service.Hash("senha"));
  }

  // Testa se a seção PasswordHash é lida da configuração
  [Fact]
  public void AddTooarkPasswordHash_ShouldBindSection_FromConfiguration()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = Configuration(new Dictionary<string, string?>
    {
      { $"{PasswordHashOptions.Section}:Iterations", "650000" },
      { $"{PasswordHashOptions.Section}:SaltSize", "24" }
    });

    // Act
    services.AddTooarkPasswordHash(configuration);
    using var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<IOptions<PasswordHashOptions>>().Value;
    var hash = provider.GetRequiredService<IPasswordHashService>().Hash("senha");

    // Assert
    Assert.Equal(650_000, options.Iterations);
    Assert.Equal(24, options.SaltSize);
    Assert.Contains("$i=650000$", hash);
  }

  // Testa se o override programático vence a configuração
  [Fact]
  public void AddTooarkPasswordHash_ShouldApplyConfigure_OverConfiguration()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = Configuration(new Dictionary<string, string?>
    {
      { $"{PasswordHashOptions.Section}:Iterations", "650000" }
    });

    // Act
    services.AddTooarkPasswordHash(configuration, options => options.Iterations = 700_000);
    using var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<IOptions<PasswordHashOptions>>().Value;

    // Assert
    Assert.Equal(700_000, options.Iterations);
  }

  // Testa se opções fracas falham no registro, e não no primeiro hash
  [Fact]
  public void AddTooarkPasswordHash_ShouldThrowAtRegistration_WhenIterationsAreTooLow()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = Configuration(new Dictionary<string, string?>
    {
      { $"{PasswordHashOptions.Section}:Iterations", "1000" }
    });

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkPasswordHash(configuration));

    // Assert
    Assert.Equal("Options.PasswordHash.IterationsTooLow;100000", exception.Message);
  }

  // Testa se chamar duas vezes não duplica o registro do serviço
  [Fact]
  public void AddTooarkPasswordHash_ShouldRegisterServiceOnce_WhenCalledTwice()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkPasswordHash();
    services.AddTooarkPasswordHash();

    // Assert
    Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IPasswordHashService));
  }

  // Testa se o método devolve a mesma coleção para encadeamento
  [Fact]
  public void AddTooarkPasswordHash_ShouldReturnSameServiceCollection()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    var result = services.AddTooarkPasswordHash();

    // Assert
    Assert.Same(services, result);
  }
}
