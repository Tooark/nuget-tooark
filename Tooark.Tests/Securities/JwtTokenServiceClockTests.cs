using Microsoft.IdentityModel.JsonWebTokens;
using Tooark.Exceptions;
using Tooark.Securities;
using Tooark.Securities.Dtos;
using Tooark.Securities.Options;
using Tooark.Tests.Moq.Util;
using MEOptions = Microsoft.Extensions.Options;

namespace Tooark.Tests.Securities;

/// <summary>
/// Testes do relógio próprio e dos nomes de claims do JwtTokenService.
/// </summary>
public class JwtTokenServiceClockTests
{
  #region Test Helpers

  // Instante fixo de emissão dos tokens
  private static readonly DateTimeOffset IssuedAt = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

  // Opções simétricas com expiração de 10 minutos
  private static JwtOptions GetOptions(Action<JwtOptions>? configure = null)
  {
    var options = new JwtOptions
    {
      Algorithm = "HS256",
      Secret = "mysupersecretkey1234567890ABCDEF",
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpirationTime = 10
    };

    configure?.Invoke(options);

    return options;
  }

  // Serviço com o relógio de teste
  private static JwtTokenService CreateService(FakeTimeProvider clock, Action<JwtOptions>? configure = null) =>
    new(MEOptions.Options.Create(GetOptions(configure)), clock);

  // DTO de um usuário
  private static JwtTokenDto User() => new("42", "ana", "7");

  #endregion

  #region TimeProvider

  // Testa se iat, nbf e exp saem do relógio informado, e não do relógio do sistema
  [Fact]
  public void Create_ShouldStampTimesFromTimeProvider()
  {
    // Arrange
    var clock = new FakeTimeProvider(IssuedAt);
    var service = CreateService(clock);

    // Act
    var token = new JsonWebToken(service.Create(User()));

    // Assert
    Assert.Equal(IssuedAt.UtcDateTime, token.IssuedAt);
    Assert.Equal(IssuedAt.UtcDateTime, token.ValidFrom);
    Assert.Equal(IssuedAt.AddMinutes(10).UtcDateTime, token.ValidTo);
  }

  // Testa se o token é válido no instante da emissão e dentro da vida útil
  [Fact]
  public void Validate_ShouldAcceptToken_WithinLifetime()
  {
    // Arrange
    var clock = new FakeTimeProvider(IssuedAt);
    var service = CreateService(clock);
    var token = service.Create(User());

    // Act
    clock.Advance(TimeSpan.FromMinutes(9));
    var result = service.Validate(token);

    // Assert
    Assert.Equal(string.Empty, result.ErrorToken);
    Assert.Equal("42", result.Id);
  }

  // Testa se a expiração é detectada ao avançar o relógio, sem esperar o tempo passar
  [Fact]
  public void Validate_ShouldReturnExpired_WhenClockPassesExpiration()
  {
    // Arrange
    var clock = new FakeTimeProvider(IssuedAt);
    var service = CreateService(clock);
    var token = service.Create(User());

    // Act
    clock.Advance(TimeSpan.FromMinutes(11));
    var result = service.Validate(token);

    // Assert
    Assert.Equal("Token.Expired", result.ErrorToken);
  }

  // Testa se um token emitido no futuro (nbf à frente do relógio) é inválido, como no handler
  [Fact]
  public void Validate_ShouldReturnInvalid_WhenTokenIsNotYetValid()
  {
    // Arrange
    var clock = new FakeTimeProvider(IssuedAt);
    var service = CreateService(clock);
    var token = service.Create(User());

    // Act
    clock.Advance(TimeSpan.FromMinutes(-1));
    var result = service.Validate(token);

    // Assert
    Assert.Equal("Token.Invalid", result.ErrorToken);
  }

  // Testa se o construtor original continua emitindo e validando com o relógio do sistema
  [Fact]
  public void Validate_ShouldAcceptToken_WithSystemClockConstructor()
  {
    // Arrange
    var service = new JwtTokenService(MEOptions.Options.Create(GetOptions()));

    // Act
    var result = service.Validate(service.Create(User()));

    // Assert
    Assert.Equal(string.Empty, result.ErrorToken);
    Assert.Equal("ana", result.Login);
  }

  // Testa se o relógio nulo é recusado
  [Fact]
  public void Constructor_ShouldThrowArgumentNull_WhenTimeProviderIsNull()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new JwtTokenService(MEOptions.Options.Create(GetOptions()), (TimeProvider)null!));
  }

  #endregion

  #region Claim Names

  // Testa se o padrão da 4.x emite só as claims legadas (id, login) e a security
  [Fact]
  public void Create_ShouldEmitLegacyClaimsOnly_ByDefault()
  {
    // Arrange
    var service = CreateService(new FakeTimeProvider(IssuedAt));

    // Act
    var token = new JsonWebToken(service.Create(User()));

    // Assert
    Assert.Equal("42", token.GetClaim(TooarkClaimNames.Id).Value);
    Assert.Equal("ana", token.GetClaim(TooarkClaimNames.Login).Value);
    Assert.Equal("7", token.GetClaim(TooarkClaimNames.Security).Value);
    Assert.False(token.TryGetClaim(TooarkClaimNames.Subject, out _));
    Assert.False(token.TryGetClaim(TooarkClaimNames.PreferredUsername, out _));
  }

  // Testa se só as claims padrão (sub, preferred_username) saem quando as legadas são desligadas, e o DTO as lê
  [Fact]
  public void Create_ShouldEmitStandardClaimsOnly_WhenLegacyIsOff()
  {
    // Arrange
    var clock = new FakeTimeProvider(IssuedAt);
    var service = CreateService(clock, options =>
    {
      options.EmitLegacyClaims = false;
      options.EmitStandardClaims = true;
    });

    // Act
    var jwt = service.Create(User());
    var token = new JsonWebToken(jwt);
    var user = service.Validate(jwt);

    // Assert
    Assert.Equal("42", token.Subject);
    Assert.Equal("ana", token.GetClaim(TooarkClaimNames.PreferredUsername).Value);
    Assert.Equal("7", token.GetClaim(TooarkClaimNames.Security).Value);
    Assert.False(token.TryGetClaim(TooarkClaimNames.Id, out _));
    Assert.False(token.TryGetClaim(TooarkClaimNames.Login, out _));
    Assert.Equal("42", user.Id);
    Assert.Equal("ana", user.Login);
    Assert.Equal("7", user.Security);
  }

  // Testa o caminho de migração: os dois conjuntos no mesmo token
  [Fact]
  public void Create_ShouldEmitBothClaimSets_WhenBothAreOn()
  {
    // Arrange
    var service = CreateService(new FakeTimeProvider(IssuedAt), options => options.EmitStandardClaims = true);

    // Act
    var token = new JsonWebToken(service.Create(User()));

    // Assert
    Assert.Equal("42", token.GetClaim(TooarkClaimNames.Id).Value);
    Assert.Equal("42", token.Subject);
    Assert.Equal("ana", token.GetClaim(TooarkClaimNames.Login).Value);
    Assert.Equal("ana", token.GetClaim(TooarkClaimNames.PreferredUsername).Value);
  }

  // Testa se desligar os dois conjuntos falha no startup: o token não identificaria ninguém
  [Fact]
  public void Constructor_ShouldThrowInternalServerError_WhenNoClaimSetIsEnabled()
  {
    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => CreateService(new FakeTimeProvider(IssuedAt), options =>
    {
      options.EmitLegacyClaims = false;
      options.EmitStandardClaims = false;
    }));

    // Assert
    Assert.Equal("Options.Jwt.ClaimsNotConfigured", exception.Message);
  }

  #endregion
}
