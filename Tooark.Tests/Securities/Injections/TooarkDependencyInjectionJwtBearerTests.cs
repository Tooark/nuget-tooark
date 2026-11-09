using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Tooark.Exceptions;
using Tooark.Securities.Dtos;
using Tooark.Securities.Injections;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Injections;

public class TooarkDependencyInjectionJwtBearerTests
{
  #region Helpers

  // Coleção de serviços com logging, como a aplicação tem
  private static ServiceCollection CreateServices()
  {
    var services = new ServiceCollection();
    services.AddLogging();

    return services;
  }

  // Configuração simétrica da seção Jwt
  private static IConfiguration SymmetricConfiguration(Dictionary<string, string?>? extra = null)
  {
    var settings = new Dictionary<string, string?>
    {
      { $"{JwtOptions.Section}:Algorithm", "HS256" },
      { $"{JwtOptions.Section}:Secret", "mysupersecretkey1234567890ABCDEF" },
      { $"{JwtOptions.Section}:Issuer", "TestIssuer" },
      { $"{JwtOptions.Section}:Audience", "TestAudience" },
      { $"{JwtOptions.Section}:ExpirationTime", "10" }
    };

    foreach (var (key, value) in extra ?? [])
    {
      settings[key] = value;
    }

    return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
  }

  // Configuração ES256 só com a chave privada: emite, mas não valida
  private static IConfiguration PrivateOnlyConfiguration()
  {
    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      { $"{JwtOptions.Section}:Algorithm", "ES256" },
      { $"{JwtOptions.Section}:PrivateKey", Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey()) }
    }).Build();
  }

  #endregion

  // Testa se o esquema Bearer é registrado como padrão, com a chave e os parâmetros das opções Jwt
  [Fact]
  public async Task AddTooarkJwtBearer_ShouldRegisterBearerScheme_WithJwtOptions()
  {
    // Arrange
    var services = CreateServices();

    // Act
    services.AddTooarkJwtBearer(SymmetricConfiguration());
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    var bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

    // Assert
    var scheme = await schemes.GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme);
    Assert.NotNull(scheme);
    Assert.Equal(typeof(JwtBearerHandler), scheme.HandlerType);
    Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authentication.DefaultScheme);
    Assert.False(bearer.MapInboundClaims);
    Assert.NotNull(bearer.TokenValidationParameters.IssuerSigningKey);
    Assert.Equal("TestIssuer", bearer.TokenValidationParameters.ValidIssuer);
    Assert.Equal("TestAudience", bearer.TokenValidationParameters.ValidAudience);
    Assert.True(bearer.TokenValidationParameters.ValidateIssuer);
    Assert.True(bearer.TokenValidationParameters.ValidateAudience);
    Assert.True(bearer.TokenValidationParameters.ValidateLifetime);
    Assert.Equal(TimeSpan.Zero, bearer.TokenValidationParameters.ClockSkew);
  }

  // Testa se um token emitido pelo JwtTokenService passa nos parâmetros do handler, e um de outra chave não
  [Fact]
  public async Task AddTooarkJwtBearer_ShouldAcceptTokensFromJwtTokenService()
  {
    // Arrange
    var services = CreateServices();
    var configuration = SymmetricConfiguration();
    services.AddTooarkJwtToken(configuration);
    services.AddTooarkJwtBearer(configuration);
    using var provider = services.BuildServiceProvider();
    var tokenService = provider.GetRequiredService<IJwtTokenService>();
    var bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
    var handler = new JsonWebTokenHandler();

    var otherServices = CreateServices();
    otherServices.AddTooarkJwtToken(SymmetricConfiguration(new Dictionary<string, string?>
    {
      { $"{JwtOptions.Section}:Secret", "anothersecretkey1234567890ABCDEF" }
    }));
    using var otherProvider = otherServices.BuildServiceProvider();
    var otherToken = otherProvider.GetRequiredService<IJwtTokenService>().Create(new JwtTokenDto("1", "ana", "7"));

    // Act
    var valid = await handler.ValidateTokenAsync(tokenService.Create(new JwtTokenDto("1", "ana", "7")), bearer.TokenValidationParameters);
    var invalid = await handler.ValidateTokenAsync(otherToken, bearer.TokenValidationParameters);

    // Assert
    Assert.True(valid.IsValid);
    Assert.Equal("1", valid.Claims["sub"]);
    Assert.False(invalid.IsValid);
  }

  // Testa se um esquema padrão já definido pela aplicação é preservado
  [Fact]
  public void AddTooarkJwtBearer_ShouldKeepExistingDefaultScheme()
  {
    // Arrange
    var services = CreateServices();
    services.AddAuthentication("Custom");

    // Act
    services.AddTooarkJwtBearer(SymmetricConfiguration());
    using var provider = services.BuildServiceProvider();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

    // Assert
    Assert.Equal("Custom", authentication.DefaultScheme);
  }

  // Testa se o ajuste do consumidor é aplicado por último, inclusive sobre os padrões Tooark
  [Fact]
  public void AddTooarkJwtBearer_WithConfigure_ShouldApplyOverridesLast()
  {
    // Arrange
    var services = CreateServices();

    // Act
    services.AddTooarkJwtBearer(SymmetricConfiguration(), bearer =>
    {
      bearer.Challenge = "Bearer realm=\"tooark\"";
      bearer.MapInboundClaims = true;
    });
    using var provider = services.BuildServiceProvider();
    var bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

    // Assert
    Assert.Equal("Bearer realm=\"tooark\"", bearer.Challenge);
    Assert.True(bearer.MapInboundClaims);
  }

  // Testa se as opções Jwt ficam disponíveis no container, como em AddTooarkJwtToken
  [Fact]
  public void AddTooarkJwtBearer_ShouldRegisterJwtOptions()
  {
    // Arrange
    var services = CreateServices();

    // Act
    services.AddTooarkJwtBearer(SymmetricConfiguration());
    using var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<IOptions<JwtOptions>>().Value;

    // Assert
    Assert.Equal("TestIssuer", options.Issuer);
    Assert.Equal(10, options.ExpirationTime);
  }

  // Testa se a falta da chave de validação falha no registro, e não na primeira requisição
  [Fact]
  public void AddTooarkJwtBearer_ShouldThrowAtRegistration_WhenValidationKeyIsMissing()
  {
    // Arrange
    var services = CreateServices();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkJwtBearer(PrivateOnlyConfiguration()));

    // Assert
    Assert.Equal("Options.Jwt.KeyNotConfigured;PublicKey", exception.Message);
  }

  // Testa se um segredo curto falha no registro com a mesma regra do JwtTokenService
  [Fact]
  public void AddTooarkJwtBearer_ShouldThrowAtRegistration_WhenSecretIsTooShort()
  {
    // Arrange
    var services = CreateServices();
    var configuration = SymmetricConfiguration(new Dictionary<string, string?>
    {
      { $"{JwtOptions.Section}:Secret", "short" }
    });

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkJwtBearer(configuration));

    // Assert
    Assert.Equal("Options.Jwt.SecretTooShort;32", exception.Message);
  }

  // Testa se o método devolve a mesma coleção para encadeamento
  [Fact]
  public void AddTooarkJwtBearer_ShouldReturnSameServiceCollection()
  {
    // Arrange
    var services = CreateServices();

    // Act
    var result = services.AddTooarkJwtBearer(SymmetricConfiguration());

    // Assert
    Assert.Same(services, result);
  }
}
