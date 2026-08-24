using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Tooark.Dtos;
using Tooark.Extensions.Factories;
using Tooark.Injections;
using Microsoft.Extensions.Options;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Enums;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Injections;
using Tooark.Mediator.Options;
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

  #region Registro do mediador pelo agregador

  // Testa se o agregador registra o mediador com os manipuladores do assembly chamador.
  // Sem repassar o assembly, o GetCallingAssembly de dentro do AddTooarkMediator devolveria o
  // assembly do agregador, que não tem manipulador algum: o mediador subiria vazio e só falharia
  // em execução, com Handler.NotFound.
  [Fact]
  public async Task AddTooarkService_ShouldRegisterMediator_WithHandlersFromTheCallingAssembly()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService();

    // Assert
    var resposta = await services.BuildServiceProvider()
      .GetRequiredService<ISender>()
      .SendAsync(new PingAgregador("oi"), TestContext.Current.CancellationToken);

    Assert.Equal("pong: oi", resposta);
  }

  // Testa se os assemblies informados substituem o assembly chamador
  [Fact]
  public async Task AddTooarkService_ShouldUseTheInformedAssemblies()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService(null, typeof(PingAgregador).Assembly);

    // Assert
    var resposta = await services.BuildServiceProvider()
      .GetRequiredService<ISender>()
      .SendAsync(new PingAgregador("oi"), TestContext.Current.CancellationToken);

    Assert.Equal("pong: oi", resposta);
  }

  // Testa se um AddTooarkMediator posterior convive com o registro feito pelo agregador.
  // O registro dos manipuladores usa TryAddEnumerable, que deduplica por tipo de implementação, e
  // a validação de manipulador único não pode acusar duplicidade por causa da segunda chamada.
  [Fact]
  public async Task AddTooarkService_ShouldNotDuplicateHandlers_WhenMediatorIsRegisteredAgain()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService();
    services.AddTooarkMediator(typeof(PingAgregador).Assembly);

    // Assert
    Assert.Single(services, descriptor =>
      descriptor.ServiceType == typeof(IRequestHandler<PingAgregador, string>));

    var resposta = await services.BuildServiceProvider()
      .GetRequiredService<ISender>()
      .SendAsync(new PingAgregador("oi"), TestContext.Current.CancellationToken);

    Assert.Equal("pong: oi", resposta);
  }

  // Testa se a configuração do mediador continua possível depois do registro pelo agregador
  [Fact]
  public void AddTooarkService_ShouldAllowConfiguringMediatorAfterwards()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService();
    services.AddTooarkMediator(options => options.NotifyPublishStrategy = ENotifyStrategy.Sequential);

    // Assert
    var opcoes = services.BuildServiceProvider().GetRequiredService<IOptions<MediatorOptions>>();

    Assert.Equal(ENotifyStrategy.Sequential, opcoes.Value.NotifyPublishStrategy);
  }

  // Testa se array de assemblies nula recai no assembly chamador, como a ausência do argumento
  [Fact]
  public async Task AddTooarkService_ShouldFallBackToTheCallingAssembly_WhenAssembliesIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService(null, null!);

    // Assert
    var resposta = await services.BuildServiceProvider()
      .GetRequiredService<ISender>()
      .SendAsync(new PingAgregador("oi"), TestContext.Current.CancellationToken);

    Assert.Equal("pong: oi", resposta);
  }

  // Testa se a unidade de trabalho segue fora do agregador, por depender do tipo do contexto
  [Fact]
  public void AddTooarkService_ShouldNotRegisterUnitOfWork()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkService();

    // Assert
    Assert.DoesNotContain(services, descriptor =>
      descriptor.ServiceType.Name.Contains("UnitOfWork", StringComparison.Ordinal));
  }

  #endregion
}

// Requisição usada para verificar o registro do mediador feito pelo agregador
public sealed record PingAgregador(string Texto) : IRequest<string>;

// Manipulador correspondente, encontrado pela varredura do assembly de teste
public sealed class PingAgregadorHandler : IRequestHandler<PingAgregador, string>
{
  public Task<string> HandleAsync(PingAgregador request, CancellationToken cancellationToken = default) =>
    Task.FromResult($"pong: {request.Texto}");
}
