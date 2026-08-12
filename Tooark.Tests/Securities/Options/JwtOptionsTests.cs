using Tooark.Exceptions;
using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Options;

public class JwtOptionsTests
{
  // Teste para verificar se o algoritmo é mapeado corretamente
  [Theory]
  [InlineData("HS256", "HS256")]
  [InlineData("hs384", "HS384")]
  [InlineData("HS512", "HS512")]
  [InlineData("RS256", "RS256")]
  [InlineData("rs384", "RS384")]
  [InlineData("rS512", "RS512")]
  [InlineData("ps384", "PS384")]
  [InlineData("PS512", "PS512")]
  [InlineData("ES256", "ES256")]
  [InlineData("eS384", "ES384")]
  [InlineData("es512", "ES512")]
  public void Algorithm_SetValue_MapsToExpectedAlgorithm(string? input, string expected)
  {
    // Arrange & Act
    var options = new JwtOptions
    {
      Algorithm = input!
    };

    // Assert
    Assert.Contains(expected, options.Algorithm, System.StringComparison.OrdinalIgnoreCase);
  }

  // Teste para verificar que algoritmo desconhecido lança exceção (sem fallback silencioso)
  [Theory]
  [InlineData("invalid")]
  [InlineData("HS-256")]
  [InlineData("")]
  [InlineData(null)]
  public void Algorithm_SetInvalidValue_ShouldThrowInternalServerErrorException(string? input)
  {
    // Arrange & Act & Assert - em configuração de segurança, valor inválido nunca troca de algoritmo silenciosamente
    var ex = Assert.Throws<InternalServerErrorException>(() => new JwtOptions
    {
      Algorithm = input!
    });
    Assert.Contains(ex.GetErrorMessages(), message => message.StartsWith("Options.Jwt.AlgorithmNotSupported"));
  }

  // Teste para verificar o algoritmo padrão quando não configurado
  [Fact]
  public void Algorithm_DefaultValue_IsES256()
  {
    // Arrange & Act - sem setar Algorithm (antes: campo nulo causava NullReferenceException no serviço)
    var options = new JwtOptions();

    // Assert
    Assert.Equal("ES256", options.Algorithm);
  }

  // Teste para verificar secret
  [Fact]
  public void Secret_SetAndGet_ReturnsSameValue()
  {
    // Arrange & Act
    var options = new JwtOptions
    {
      Secret = "mysecret"
    };

    // Assert
    Assert.Equal("mysecret", options.Secret);
  }

  // Teste para verificar se a chave privada é limpa de cabeçalhos PEM e espaços em branco
  [Fact]
  public void PrivateKey_SetValue_StripsPemHeadersAndWhitespace()
  {
    // Arrange
    var pem = "-----BEGIN PRIVATE KEY-----\nABCDEF123456\n-----END PRIVATE KEY-----\r\n";

    // Act
    var options = new JwtOptions
    {
      PrivateKey = pem
    };

    // Assert
    Assert.Equal("ABCDEF123456", options.PrivateKey);
  }

  // Teste para verificar se a chave pública é limpa de cabeçalhos PEM e espaços em branco
  [Fact]
  public void PublicKey_SetValue_StripsPemHeadersAndWhitespace()
  {
    // Arrange
    var pem = "-----BEGIN PUBLIC KEY-----\nXYZ987654\n-----END PUBLIC KEY-----\r\n";

    // Act
    var options = new JwtOptions
    {
      PublicKey = pem
    };

    // Assert
    Assert.Equal("XYZ987654", options.PublicKey);
  }

  // Teste para verificar que PublicKey nula resulta em vazio (sem fallback para a chave privada)
  [Fact]
  public void PublicKey_SetNull_ShouldBeEmpty_WithoutPrivateKeyFallback()
  {
    // Arrange & Act - antes: PublicKey nula copiava a chave privada (semanticamente inválido e dependente de ordem)
    var options = new JwtOptions
    {
      PrivateKey = "PRIVATEKEYCONTENT",
      PublicKey = null
    };

    // Assert
    Assert.Equal("", options.PublicKey);
  }

  // Teste para verificar emissor
  [Fact]
  public void Issuer_SetAndGet_ReturnsSameValue()
  {
    // Arrange & Act
    var options = new JwtOptions
    {
      Issuer = "issuer"
    };

    // Assert
    Assert.Equal("issuer", options.Issuer);
  }

  // Teste para verificar destinatário
  [Fact]
  public void Audience_SetAndGet_ReturnsSameValue()
  {
    // Arrange & Act
    var options = new JwtOptions
    {
      Audience = "audience"
    };

    // Assert
    Assert.Equal("audience", options.Audience);
  }

  // Teste para verificar arrays de emissores
  [Fact]
  public void Issuers_SetAndGet_ReturnsSameArray()
  {
    // Arrange
    var arr = new[] { "issuer1", "issuer2" };

    // Act
    var options = new JwtOptions
    {
      Issuers = arr
    };

    // Assert
    Assert.Equal(arr, options.Issuers);
  }

  // Teste para verificar arrays de destinatários
  [Fact]
  public void Audiences_SetAndGet_ReturnsSameArray()
  {
    // Arrange
    var arr = new[] { "aud1", "aud2" };

    // Act
    var options = new JwtOptions
    {
      Audiences = arr
    };

    // Assert
    Assert.Equal(arr, options.Audiences);
  }

  // Teste para verificar o tempo de expiração padrão
  [Fact]
  public void ExpirationTime_DefaultValue_IsFive()
  {
    // Arrange & Act
    var options = new JwtOptions();

    // Assert
    Assert.Equal(5, options.ExpirationTime);
  }

  // Teste para verificar se o tempo de expiração pode ser definido
  [Fact]
  public void ExpirationTime_SetAndGet_ReturnsSetValue()
  {
    // Arrange & Act
    var options = new JwtOptions
    {
      ExpirationTime = 10
    };

    // Assert
    Assert.Equal(10, options.ExpirationTime);
  }
}
