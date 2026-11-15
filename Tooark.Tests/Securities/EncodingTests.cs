using Tooark.Securities;

namespace Tooark.Tests.Securities;

/// <summary>
/// Testes dos codificadores internos Base32 e Base64Url usados por SecureToken e Totp.
/// </summary>
public class EncodingTests
{
  // Testa os vetores do RFC 4648 (seção 10) para o Base32 sem preenchimento
  [Theory]
  [InlineData("", "")]
  [InlineData("f", "MY")]
  [InlineData("fo", "MZXQ")]
  [InlineData("foo", "MZXW6")]
  [InlineData("foob", "MZXW6YQ")]
  [InlineData("fooba", "MZXW6YTB")]
  [InlineData("foobar", "MZXW6YTBOI")]
  public void Base32_Encode_ShouldMatchRfc4648Vectors(string text, string expected)
  {
    // Act
    var encoded = Base32.Encode(System.Text.Encoding.ASCII.GetBytes(text));

    // Assert
    Assert.Equal(expected, encoded);
  }

  // Testa se o Base32 decodifica os vetores com e sem preenchimento, em qualquer caixa
  [Theory]
  [InlineData("MY", "f")]
  [InlineData("MY======", "f")]
  [InlineData("mzxw6", "foo")]
  [InlineData("MZXW 6YTB-OI", "foobar")]
  public void Base32_TryDecode_ShouldDecodeVectors(string encoded, string expected)
  {
    // Act
    var decoded = Base32.TryDecode(encoded, out var bytes);

    // Assert
    Assert.True(decoded);
    Assert.Equal(expected, System.Text.Encoding.ASCII.GetString(bytes));
  }

  // Testa se texto vazio, só preenchimento ou fora do alfabeto não decodifica
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("====")]
  [InlineData("MZXW6YTB0")]
  [InlineData("MZXW6YTB1")]
  [InlineData("MZXW6YTB8")]
  public void Base32_TryDecode_ShouldReturnFalse_WhenTextIsInvalid(string? encoded)
  {
    // Act
    var decoded = Base32.TryDecode(encoded, out var bytes);

    // Assert
    Assert.False(decoded);
    Assert.Empty(bytes);
  }

  // Testa se o Base32 faz a ida e volta de bytes aleatórios
  [Fact]
  public void Base32_ShouldRoundTrip_RandomBytes()
  {
    // Arrange
    var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(37);

    // Act
    var decoded = Base32.TryDecode(Base32.Encode(bytes), out var result);

    // Assert
    Assert.True(decoded);
    Assert.Equal(bytes, result);
  }

  // Testa se o Base64Url troca os caracteres de URL e remove o preenchimento
  [Fact]
  public void Base64Url_Encode_ShouldBeUrlSafe_WithoutPadding()
  {
    // Arrange: bytes cujo Base64 padrão tem '+', '/' e '='
    var bytes = new byte[] { 0xFB, 0xFF, 0xBF, 0x00 };

    // Act
    var encoded = Base64Url.Encode(bytes);

    // Assert
    Assert.Equal("+/+/AA==", Convert.ToBase64String(bytes));
    Assert.Equal("-_-_AA", encoded);
  }

  // Testa se o Base64Url decodifica com ou sem preenchimento e no alfabeto padrão
  [Theory]
  [InlineData("-_-_AA")]
  [InlineData("-_-_AA==")]
  [InlineData("+/+/AA==")]
  public void Base64Url_TryDecode_ShouldAcceptBothAlphabets(string encoded)
  {
    // Act
    var decoded = Base64Url.TryDecode(encoded, out var bytes);

    // Assert
    Assert.True(decoded);
    Assert.Equal(new byte[] { 0xFB, 0xFF, 0xBF, 0x00 }, bytes);
  }

  // Testa se texto vazio, com resto de um caractere ou fora do alfabeto não decodifica
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("A")]
  [InlineData("AAAAA")]
  [InlineData("not base64!!")]
  public void Base64Url_TryDecode_ShouldReturnFalse_WhenTextIsInvalid(string? encoded)
  {
    // Act
    var decoded = Base64Url.TryDecode(encoded, out var bytes);

    // Assert
    Assert.False(decoded);
    Assert.Empty(bytes);
  }
}
