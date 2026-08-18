using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Tooark.Exceptions;
using Tooark.Securities;
using Tooark.Securities.Dtos;
using Tooark.Securities.Options;
using MEOptions = Microsoft.Extensions.Options;

namespace Tooark.Tests.Securities;

public class JwtTokenServiceTests
{
  // Opções de configuração do JWT para testes simétricos
  private static JwtOptions GetSymmetricOptions() => new()
  {
    Algorithm = "HS256",
    Secret = "mysupersecretkey1234567890ABCDEF",
    Issuer = "TestIssuer",
    Audience = "TestAudience",
    ExpirationTime = 10
  };

  // Opções de configuração do JWT para testes assimétricos RS256
  private static JwtOptions GetAsymmetricRSOptions()
  {
    // Gera um par de chaves RSA temporárias para uso nos testes e exporta
    // para PKCS#8 (private) e SubjectPublicKeyInfo (public), codificando em Base64.
    using var rsa = RSA.Create(2048);

    var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
    var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

    return new JwtOptions
    {
      Algorithm = "RS256",
      PrivateKey = Convert.ToBase64String(privateKeyBytes),
      PublicKey = Convert.ToBase64String(publicKeyBytes),
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpirationTime = 10
    };
  }

  // Opções de configuração do JWT para testes assimétricos PS256
  private static JwtOptions GetAsymmetricPSOptions()
  {
    // Gera um par de chaves RSA temporárias para uso nos testes e exporta
    // para PKCS#8 (private) e SubjectPublicKeyInfo (public), codificando em Base64.
    using var rsa = RSA.Create(2048);

    var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
    var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

    return new JwtOptions
    {
      Algorithm = "PS256",
      PrivateKey = Convert.ToBase64String(privateKeyBytes),
      PublicKey = Convert.ToBase64String(publicKeyBytes),
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpirationTime = 10
    };
  }

  // Opções de configuração do JWT para testes assimétricos ES256
  private static JwtOptions GetAsymmetricESOptions()
  {
    // Gera um par de chaves RSA temporárias para uso nos testes e exporta
    // para PKCS#8 (private) e SubjectPublicKeyInfo (public), codificando em Base64.
    using var ecdsa = ECDsa.Create();

    var privateKeyBytes = ecdsa.ExportPkcs8PrivateKey();
    var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();

    return new JwtOptions
    {
      Algorithm = "ES256",
      PrivateKey = Convert.ToBase64String(privateKeyBytes),
      PublicKey = Convert.ToBase64String(publicKeyBytes),
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpirationTime = 10
    };
  }

  // DTO de token JWT para testes
  private static JwtTokenDto GetTokenDto() => new(1, "user", 1);

  // Mock do logger para testes
  private static ILogger<JwtTokenService> GetMockLogger() => Mock.Of<ILogger<JwtTokenService>>();

  // Token JWT pré-gerado para testes de expiração
  private static readonly string JwtTokenExpired = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjEiLCJsb2dpbiI6InVzZXIiLCJzZWN1cml0eSI6IjEiLCJuYmYiOjE3NjE5Mjg0MDgsImV4cCI6MTc2MTkyODQ1MywiaWF0IjoxNzYxOTI4NDA4LCJpc3MiOiJUZXN0SXNzdWVyIiwiYXVkIjoiVGVzdEF1ZGllbmNlIn0.M1iGoekiVJ0RnCMgk52Kc8rWjYzkFa5P0xakILzXCsc";

  // Token JWT malformado para testes de erro
  private static readonly string MalformedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

  // Opções inválidas para cenários de erro
  private class NullOptions : MEOptions.IOptions<JwtOptions>
  {
    public JwtOptions Value => null!;
  }

  // Teste de criação e validação de token JWT simétrico
  [Fact]
  public void Create_And_Validate_Token_Symmetric_Should_Succeed()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var dto = GetTokenDto();

    // Act
    var token = service.Create(dto);
    var result = service.Validate(token);

    // Assert
    Assert.NotNull(token);
    Assert.NotNull(result);
    Assert.Equal("", result.ErrorToken);
    Assert.Equal("1", result.Id);
    Assert.Equal("user", result.Login);
    Assert.Equal("1", result.Security);
  }

  // Teste de criação e validação de token JWT assimétrico RS
  [Fact]
  public void Create_And_Validate_Token_Asymmetric_RS_Should_Succeed()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetAsymmetricRSOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var dto = GetTokenDto();

    // Act
    var token = service.Create(dto);
    var result = service.Validate(token);

    // Assert
    Assert.NotNull(token);
    Assert.NotNull(result);
    Assert.Equal("", result.ErrorToken);
    Assert.Equal("1", result.Id);
    Assert.Equal("user", result.Login);
    Assert.Equal("1", result.Security);
  }

  // Teste de criação e validação de token JWT assimétrico PS
  [Fact]
  public void Create_And_Validate_Token_Asymmetric_PS_Should_Succeed()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetAsymmetricPSOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var dto = GetTokenDto();

    // Act
    var token = service.Create(dto);
    var result = service.Validate(token);

    // Assert
    Assert.NotNull(token);
    Assert.NotNull(result);
    Assert.Equal("", result.ErrorToken);
    Assert.Equal("1", result.Id);
    Assert.Equal("user", result.Login);
    Assert.Equal("1", result.Security);
  }

  // Teste de criação e validação de token JWT assimétrico ES
  [Fact]
  public void Create_And_Validate_Token_Asymmetric_ES_Should_Succeed()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetAsymmetricESOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var dto = GetTokenDto();

    // Act
    var token = service.Create(dto);
    var result = service.Validate(token);

    // Assert
    Assert.NotNull(token);
    Assert.NotNull(result);
    Assert.Equal("", result.ErrorToken);
    Assert.Equal("1", result.Id);
    Assert.Equal("user", result.Login);
    Assert.Equal("1", result.Security);
  }

  // Teste de validação de token JWT com dados inválidos
  [Fact]
  public void Validate_Invalid_Token_Should_Return_Error()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var invalidToken = "invalid.token.value";

    // Act
    var result = service.Validate(invalidToken);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.ErrorToken);
  }

  // Teste de validação de token JWT com token expirado
  [Fact]
  public void Validate_Expired_Token_Should_Return_ExpiredError()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var token = JwtTokenExpired;

    // Act
    var result = service.Validate(token);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Token.Expired", result.ErrorToken);
  }

  // Teste de validação com audience incorreta (token inválido, não erro interno)
  [Fact]
  public void Validate_WithWrongAudience_ShouldReturnTokenInvalid()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var token = service.Create(GetTokenDto());

    // Act - valida exigindo um audience diferente do usado na criação
    var result = service.Validate(token, "OutroAudience");

    // Assert - antes caía no catch genérico e reportava InternalServerError
    Assert.Equal("Token.Invalid", result.ErrorToken);
  }

  // Teste de validação de token assinado corretamente mas sem as claims esperadas
  [Fact]
  public void Validate_TokenWithoutExpectedClaims_ShouldReturnEmptyFields()
  {
    // Arrange - token válido (mesma chave/issuer/audience) porém sem as claims id/login/security
    var symmetricOptions = GetSymmetricOptions();
    var handler = new JwtSecurityTokenHandler();
    var keyBytes = Encoding.UTF8.GetBytes(symmetricOptions.Secret!);
    var descriptor = new SecurityTokenDescriptor
    {
      Expires = DateTime.UtcNow.AddMinutes(10),
      SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), symmetricOptions.Algorithm),
      Issuer = symmetricOptions.Issuer,
      Audience = symmetricOptions.Audience
    };
    var token = handler.WriteToken(handler.CreateToken(descriptor));

    var options = MEOptions.Options.Create(symmetricOptions);
    var service = new JwtTokenService(options, GetMockLogger());

    // Act
    var result = service.Validate(token);

    // Assert - antes lançava internamente e reportava InternalServerError; agora retorna campos vazios
    Assert.Equal("", result.ErrorToken);
    Assert.Equal("", result.Id);
    Assert.Equal("", result.Login);
    Assert.Equal("", result.Security);
  }

  // Teste de criação e validação sem logger fornecido (fallback interno para NullLogger)
  [Fact]
  public void Create_And_Validate_WithoutLogger_ShouldWork()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options);

    // Act
    var token = service.Create(GetTokenDto());
    var result = service.Validate(token);

    // Assert
    Assert.Equal("", result.ErrorToken);
    Assert.Equal("1", result.Id);
  }

  // Teste de construtor sem Algorithm configurado (usa o padrão ES256 e falha com erro claro, sem NullReferenceException)
  [Fact]
  public void Constructor_WithoutAlgorithm_ShouldFailFastWithClearError()
  {
    // Arrange - apenas Secret configurado, sem Algorithm (padrão ES256, assimétrico sem chaves)
    var jwtOptions = new JwtOptions
    {
      Secret = "mysupersecretkey1234567890ABCDEF"
    };
    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert - antes: NullReferenceException; agora fail-fast no startup com erro de configuração
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.KeysNotConfigured", ex.GetErrorMessages());
  }

  // Teste de construtor ECDsa sem nenhuma chave configurada (fail-fast igual ao RSA)
  [Fact]
  public void Constructor_AsymmetricES_WithoutKeys_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var jwtOptions = new JwtOptions
    {
      Algorithm = "ES256"
    };
    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert - antes o construtor completava e a falha só aparecia no primeiro uso
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.KeysNotConfigured", ex.GetErrorMessages());
  }

  // Teste de secret simétrico abaixo do mínimo da RFC 7518 (falha no startup, sem erro obscuro em runtime)
  [Theory]
  [InlineData("HS256", 31)]
  [InlineData("HS384", 47)]
  [InlineData("HS512", 63)]
  public void Constructor_SymmetricWithShortSecret_ShouldThrowInternalServerErrorException(string algorithm, int secretLength)
  {
    // Arrange - secret um byte abaixo do mínimo exigido para o algoritmo
    var jwtOptions = new JwtOptions
    {
      Algorithm = algorithm,
      Secret = new string('x', secretLength)
    };
    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains(ex.GetErrorMessages(), message => message.StartsWith("Options.Jwt.SecretTooShort"));
  }

  // Teste de secret simétrico no tamanho mínimo exato da RFC 7518
  [Theory]
  [InlineData("HS256", 32)]
  [InlineData("HS384", 48)]
  [InlineData("HS512", 64)]
  public void Constructor_SymmetricWithMinimumSecret_ShouldCreateInstance(string algorithm, int secretLength)
  {
    // Arrange
    var jwtOptions = new JwtOptions
    {
      Algorithm = algorithm,
      Secret = new string('x', secretLength)
    };
    var options = MEOptions.Options.Create(jwtOptions);

    // Act
    var service = new JwtTokenService(options, GetMockLogger());

    // Assert
    Assert.NotNull(service);
  }

  // Teste de construtor com opções nulas
  [Fact]
  public void Constructor_WithNullOptions_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var options = new NullOptions();

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.NotConfigured", ex.GetErrorMessages());
  }

  // Teste de construtor simétrico sem secret configurado
  [Fact]
  public void Constructor_SymmetricAlgorithmWithoutSecret_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var jwtOptions = new JwtOptions
    {
      Algorithm = "HS256",
      Secret = null
    };

    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.SecretNotConfigured", ex.GetErrorMessages());
  }

  // Teste de construtor assimétrico sem chave pública
  [Fact]
  public void Constructor_AsymmetricWithoutPublicKey_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var jwtOptions = new JwtOptions
    {
      Algorithm = "RS256",
      PublicKey = null,
      PrivateKey = null
    };

    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.KeysNotConfigured", ex.GetErrorMessages());
  }

  // Teste de construtor assimétrico RSA com chave menor que 2048 bits
  [Fact]
  public void Constructor_AsymmetricRS_WithSmallKey_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    using var rsa = RSA.Create(1024);

    var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
    var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

    var jwtOptions = new JwtOptions
    {
      Algorithm = "RS256",
      PrivateKey = Convert.ToBase64String(privateKeyBytes),
      PublicKey = Convert.ToBase64String(publicKeyBytes)
    };

    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.PrivateKey.InvalidSize", ex.GetErrorMessages());
  }

  // Teste de construtor ECDsa com curva/tamanho incorreto
  [Fact]
  public void Constructor_AsymmetricES_WithWrongCurve_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    var privateKeyBytes = ecdsa.ExportPkcs8PrivateKey();
    var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();

    var jwtOptions = new JwtOptions
    {
      Algorithm = "ES384",
      PrivateKey = Convert.ToBase64String(privateKeyBytes),
      PublicKey = Convert.ToBase64String(publicKeyBytes)
    };

    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.PrivateKey.InvalidCurve", ex.GetErrorMessages());
  }

  // Teste de construtor RSA com chaves inválidas
  [Fact]
  public void Constructor_AsymmetricRS_WithInvalidKeys_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var invalidBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

    var jwtOptions = new JwtOptions
    {
      Algorithm = "RS256",
      PrivateKey = invalidBase64,
      PublicKey = invalidBase64
    };

    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.InvalidKey", ex.GetErrorMessages());
  }

  // Teste de criação de token com chave pública apenas (sem chave privada)
  [Fact]
  public void Create_AsymmetricPublicKeyOnly_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    using var rsa = RSA.Create(2048);

    var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

    var jwtOptions = new JwtOptions
    {
      Algorithm = "RS256",
      PublicKey = Convert.ToBase64String(publicKeyBytes),
      PrivateKey = null
    };

    var options = MEOptions.Options.Create(jwtOptions);
    var service = new JwtTokenService(options, GetMockLogger());

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => service.Create(GetTokenDto()));
    Assert.Contains("Options.Jwt.KeyNotConfigured;PrivateKey", ex.GetErrorMessages());
  }

  // Teste de validação com assinatura inválida
  [Fact]
  public void Validate_WithWrongSignature_ShouldReturnInvalidSignatureError()
  {
    // Arrange
    var optionsSigning = MEOptions.Options.Create(new JwtOptions
    {
      Algorithm = "HS256",
      Secret = "mysupersecretkey1234567890ABCDEF",
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpirationTime = 10
    });

    var signingService = new JwtTokenService(optionsSigning, GetMockLogger());
    var token = signingService.Create(GetTokenDto());
    var optionsValidation = MEOptions.Options.Create(new JwtOptions
    {
      Algorithm = "HS256",
      Secret = "anothersecretkey1234567890ABCDEF",
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpirationTime = 10
    });

    var validationService = new JwtTokenService(optionsValidation, GetMockLogger());

    // Act
    var result = validationService.Validate(token);

    // Assert
    Assert.Equal("Token.InvalidSignature", result.ErrorToken);
  }

  // Teste de construtor RSA com chave pública menor que 2048 bits
  [Fact]
  public void Constructor_AsymmetricRS_WithSmallPublicKey_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    using var rsaValid = RSA.Create(2048);
    using var rsaSmall = RSA.Create(1024);

    var privateKeyBytes = rsaValid.ExportPkcs8PrivateKey();
    var publicKeyBytes = rsaSmall.ExportSubjectPublicKeyInfo();

    var jwtOptions = new JwtOptions
    {
      Algorithm = "RS256",
      PrivateKey = Convert.ToBase64String(privateKeyBytes),
      PublicKey = Convert.ToBase64String(publicKeyBytes)
    };

    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.PublicKey.InvalidSize", ex.GetErrorMessages());
  }

  // Teste de construtor ECDsa com curva incorreta apenas na chave pública
  [Fact]
  public void Constructor_AsymmetricES_WithWrongPublicKeyCurve_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    using var ecdsaValid = ECDsa.Create(ECCurve.NamedCurves.nistP384);
    using var ecdsaWrong = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    var privateKeyBytes = ecdsaValid.ExportPkcs8PrivateKey();
    var publicKeyBytes = ecdsaWrong.ExportSubjectPublicKeyInfo();

    var jwtOptions = new JwtOptions
    {
      Algorithm = "ES384",
      PrivateKey = Convert.ToBase64String(privateKeyBytes),
      PublicKey = Convert.ToBase64String(publicKeyBytes)
    };

    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.PublicKey.InvalidCurve", ex.GetErrorMessages());
  }

  // Teste de construtor ECDsa com chaves inválidas
  [Fact]
  public void Constructor_AsymmetricES_WithInvalidKeys_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var invalidBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

    var jwtOptions = new JwtOptions
    {
      Algorithm = "ES256",
      PrivateKey = invalidBase64,
      PublicKey = invalidBase64
    };

    var options = MEOptions.Options.Create(jwtOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtTokenService(options, GetMockLogger()));
    Assert.Contains("Options.Jwt.InvalidKey", ex.GetErrorMessages());
  }

  // Teste de validação com apenas chave privada configurada
  [Fact]
  public void Validate_WithPrivateKeyOnly_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    using var rsa = RSA.Create(2048);
    var privateKeyBytes = rsa.ExportPkcs8PrivateKey();

    var jwtOptions = new JwtOptions
    {
      Algorithm = "RS256",
      PrivateKey = Convert.ToBase64String(privateKeyBytes)
    };

    var options = MEOptions.Options.Create(jwtOptions);
    var service = new JwtTokenService(options, GetMockLogger());

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => service.Validate("any.token.here"));
    Assert.Contains("Options.Jwt.KeyNotConfigured;PublicKey", ex.GetErrorMessages());
  }

  // Teste de validação que retorna Token.InvalidSignature
  [Fact]
  public void Validate_WithMalformedToken_ShouldReturnTokenInvalidSignature()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var malformedToken = MalformedToken;

    // Act
    var result = service.Validate(malformedToken);

    // Assert
    Assert.NotNull(result.ErrorToken);
    Assert.Equal("Token.InvalidSignature", result.ErrorToken);
  }

  // Teste de validação com token que causa ArgumentException
  [Fact]
  public void Validate_WithEmptyToken_ShouldReturnTokenInvalid()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());

    // Act
    var result = service.Validate("");

    // Assert
    Assert.Equal("Token.Invalid", result.ErrorToken);
  }

  // Teste de criação de token com audience customizado
  [Fact]
  public void Create_WithCustomAudience_ShouldSucceed()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var dto = GetTokenDto();

    // Act
    var token = service.Create(dto, audience: "CustomAudience");
    var result = service.Validate(token, audience: "CustomAudience");

    // Assert
    Assert.NotNull(token);
    Assert.Equal("", result.ErrorToken);
  }

  // Teste de criação de token com claims extras
  [Fact]
  public void Create_WithExtraClaims_ShouldSucceed()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var dto = GetTokenDto();
    var extraClaims = new[] { new System.Security.Claims.Claim("custom", "value") };

    // Act
    var token = service.Create(dto, extraClaims: extraClaims);
    var result = service.Validate(token);

    // Assert
    Assert.NotNull(token);
    Assert.Equal("", result.ErrorToken);
  }

  // Teste de criação e validação sem Issuer e Audience configurados
  [Fact]
  public void Create_And_Validate_WithoutIssuerAndAudience_ShouldSucceed()
  {
    // Arrange
    var jwtOptions = new JwtOptions
    {
      Algorithm = "HS256",
      Secret = "mysupersecretkey1234567890ABCDEF",
      ExpirationTime = 10
    };
    var options = MEOptions.Options.Create(jwtOptions);
    var service = new JwtTokenService(options, GetMockLogger());
    var dto = GetTokenDto();

    // Act
    var token = service.Create(dto);
    var result = service.Validate(token);

    // Assert
    Assert.NotNull(token);
    Assert.Equal("", result.ErrorToken);
  }

  // Teste de validação com Issuers array configurado
  [Fact]
  public void Validate_WithIssuersArray_ShouldSucceed()
  {
    // Arrange
    var jwtOptions = new JwtOptions
    {
      Algorithm = "HS256",
      Secret = "mysupersecretkey1234567890ABCDEF",
      Issuer = "TestIssuer",
      Issuers = ["TestIssuer", "AnotherIssuer"],
      Audience = "TestAudience",
      ExpirationTime = 10
    };
    var options = MEOptions.Options.Create(jwtOptions);
    var service = new JwtTokenService(options, GetMockLogger());
    var dto = GetTokenDto();

    // Act
    var token = service.Create(dto);
    var result = service.Validate(token);

    // Assert
    Assert.NotNull(token);
    Assert.Equal("", result.ErrorToken);
  }

  // Teste de validação com Audiences array configurado
  [Fact]
  public void Validate_WithAudiencesArray_ShouldSucceed()
  {
    // Arrange
    var jwtOptions = new JwtOptions
    {
      Algorithm = "HS256",
      Secret = "mysupersecretkey1234567890ABCDEF",
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      Audiences = ["TestAudience", "AnotherAudience"],
      ExpirationTime = 10
    };
    var options = MEOptions.Options.Create(jwtOptions);
    var service = new JwtTokenService(options, GetMockLogger());
    var dto = GetTokenDto();

    // Act
    var token = service.Create(dto);
    var result = service.Validate(token);

    // Assert
    Assert.NotNull(token);
    Assert.Equal("", result.ErrorToken);
  }

  // Teste de validação ECDsa com apenas chave pública (somente validação)
  [Fact]
  public void Validate_AsymmetricES_WithPublicKeyOnly_ShouldSucceed()
  {
    // Arrange
    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    var privateKeyBytes = ecdsa.ExportPkcs8PrivateKey();
    var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();

    var createOptions = MEOptions.Options.Create(new JwtOptions
    {
      Algorithm = "ES256",
      PrivateKey = Convert.ToBase64String(privateKeyBytes),
      PublicKey = Convert.ToBase64String(publicKeyBytes),
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpirationTime = 10
    });

    var createService = new JwtTokenService(createOptions, GetMockLogger());
    var token = createService.Create(GetTokenDto());

    var validateOptions = MEOptions.Options.Create(new JwtOptions
    {
      Algorithm = "ES256",
      PublicKey = Convert.ToBase64String(publicKeyBytes),
      PrivateKey = null,
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpirationTime = 10
    });

    var validateService = new JwtTokenService(validateOptions, GetMockLogger());

    // Act
    var result = validateService.Validate(token);

    // Assert
    Assert.Equal("", result.ErrorToken);
  }

  // Teste de validação assíncrona de token JWT válido
  [Fact]
  public async Task ValidateAsync_ValidToken_Should_Succeed()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var token = service.Create(GetTokenDto());

    // Act
    var result = await service.ValidateAsync(token);

    // Assert
    Assert.Equal("", result.ErrorToken);
    Assert.Equal("1", result.Id);
    Assert.Equal("user", result.Login);
    Assert.Equal("1", result.Security);
  }

  // Teste de validação assíncrona com token expirado
  [Fact]
  public async Task ValidateAsync_ExpiredToken_Should_Return_ExpiredError()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());

    // Act
    var result = await service.ValidateAsync(JwtTokenExpired);

    // Assert
    Assert.Equal("Token.Expired", result.ErrorToken);
  }

  // Teste de validação assíncrona com token de assinatura inválida
  [Fact]
  public async Task ValidateAsync_MalformedToken_Should_Return_TokenInvalidSignature()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());

    // Act
    var result = await service.ValidateAsync(MalformedToken);

    // Assert
    Assert.Equal("Token.InvalidSignature", result.ErrorToken);
  }

  // Teste de validação assíncrona com audience incorreta
  [Fact]
  public async Task ValidateAsync_WithWrongAudience_Should_Return_TokenInvalid()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetSymmetricOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var token = service.Create(GetTokenDto());

    // Act
    var result = await service.ValidateAsync(token, "OutroAudience");

    // Assert
    Assert.Equal("Token.Invalid", result.ErrorToken);
  }

  // Teste de validação assíncrona sem chave pública configurada
  [Fact]
  public async Task ValidateAsync_WithoutPublicKey_Should_Throw_InternalServerErrorException()
  {
    // Arrange
    using var rsa = RSA.Create(2048);

    var jwtOptions = new JwtOptions
    {
      Algorithm = "RS256",
      PrivateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey())
    };

    var options = MEOptions.Options.Create(jwtOptions);
    var service = new JwtTokenService(options, GetMockLogger());

    // Act & Assert
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => service.ValidateAsync("any.token.here"));
    Assert.Contains("Options.Jwt.KeyNotConfigured;PublicKey", ex.GetErrorMessages());
  }

  // Teste de paridade entre a validação síncrona e a assíncrona
  [Fact]
  public async Task Validate_And_ValidateAsync_Should_Return_SameResult()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetAsymmetricESOptions());
    var service = new JwtTokenService(options, GetMockLogger());
    var token = service.Create(GetTokenDto());

    // Act
    var syncResult = service.Validate(token);
    var asyncResult = await service.ValidateAsync(token);

    // Assert
    Assert.Equal(syncResult.ErrorToken, asyncResult.ErrorToken);
    Assert.Equal(syncResult.Id, asyncResult.Id);
    Assert.Equal(syncResult.Login, asyncResult.Login);
    Assert.Equal(syncResult.Security, asyncResult.Security);
  }
}
