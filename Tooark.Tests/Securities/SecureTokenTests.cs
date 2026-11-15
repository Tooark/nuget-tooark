using System.Security.Cryptography;
using System.Text;
using Tooark.Exceptions;
using Tooark.Securities;

namespace Tooark.Tests.Securities;

public class SecureTokenTests
{
  // Testa se o token padrão tem 32 bytes em Base64Url: 43 caracteres, sem preenchimento nem caracteres de URL
  [Fact]
  public void Create_ShouldReturnUrlSafeToken_WithDefaultSize()
  {
    // Act
    var token = SecureToken.Create();

    // Assert
    Assert.Equal(43, token.Length);
    Assert.Matches("^[A-Za-z0-9_-]+$", token);
  }

  // Testa se o tamanho informado define o comprimento do token (16 bytes -> 22 caracteres)
  [Theory]
  [InlineData(16, 22)]
  [InlineData(32, 43)]
  [InlineData(64, 86)]
  public void Create_ShouldReturnExpectedLength_WhenSizeIsInformed(int sizeInBytes, int expectedLength)
  {
    // Act
    var token = SecureToken.Create(sizeInBytes);

    // Assert
    Assert.Equal(expectedLength, token.Length);
  }

  // Testa se dois tokens consecutivos são diferentes
  [Fact]
  public void Create_ShouldReturnDistinctTokens()
  {
    // Act
    var first = SecureToken.Create();
    var second = SecureToken.Create();

    // Assert
    Assert.NotEqual(first, second);
  }

  // Testa se um tamanho fora da faixa é recusado em vez de gerar um token fraco
  [Theory]
  [InlineData(0)]
  [InlineData(15)]
  [InlineData(1025)]
  public void Create_ShouldThrowArgumentOutOfRange_WhenSizeIsOutOfRange(int sizeInBytes)
  {
    // Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => SecureToken.Create(sizeInBytes));
  }

  // Testa se o hash é o SHA-256 do token em Base64Url, determinístico para permitir a busca pelo hash
  [Fact]
  public void Hash_ShouldReturnSha256InBase64Url()
  {
    // Arrange
    var token = "abc";
    var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
      .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // Act
    var hash = SecureToken.Hash(token);

    // Assert
    Assert.Equal(expected, hash);
    Assert.Equal(43, hash.Length);
    Assert.Equal(hash, SecureToken.Hash(token));
  }

  // Testa se um token vazio ou só com espaços é recusado no hash
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  public void Hash_ShouldThrowBadRequest_WhenTokenIsBlank(string? token)
  {
    // Act
    var exception = Assert.Throws<BadRequestException>(() => SecureToken.Hash(token!));

    // Assert
    Assert.Equal("SecureToken.TokenNotProvided", exception.Message);
  }

  // Testa se o token gerado confere com o próprio hash
  [Fact]
  public void Verify_ShouldReturnTrue_WhenTokenMatchesHash()
  {
    // Arrange
    var token = SecureToken.Create();
    var hash = SecureToken.Hash(token);

    // Act & Assert
    Assert.True(SecureToken.Verify(token, hash));
  }

  // Testa se outro token não confere com o hash guardado
  [Fact]
  public void Verify_ShouldReturnFalse_WhenTokenDiffers()
  {
    // Arrange
    var hash = SecureToken.Hash(SecureToken.Create());

    // Act & Assert
    Assert.False(SecureToken.Verify(SecureToken.Create(), hash));
  }

  // Testa se token ou hash ausentes ou malformados devolvem falso sem lançar
  [Theory]
  [InlineData(null, "unIrwb0J_HLPxmCSeLnkHGf5SdCeFXu_dWEfeAvWT8M")]
  [InlineData("", "unIrwb0J_HLPxmCSeLnkHGf5SdCeFXu_dWEfeAvWT8M")]
  [InlineData("abc", null)]
  [InlineData("abc", "")]
  [InlineData("abc", "not base64!!")]
  [InlineData("abc", "YWJj")]
  public void Verify_ShouldReturnFalse_WhenTokenOrHashIsInvalid(string? token, string? hash)
  {
    // Act & Assert
    Assert.False(SecureToken.Verify(token, hash));
  }

  // Testa se o hash também é aceito com o preenchimento do Base64 padrão, caso tenha sido guardado assim
  [Fact]
  public void Verify_ShouldAcceptHash_WithPadding()
  {
    // Arrange
    var token = "abc";
    var padded = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    // Act & Assert
    Assert.True(SecureToken.Verify(token, padded));
  }
}
