using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Options;

public class CryptographyOptionsTests
{
  // Teste para verificar se o algoritmo é mapeado corretamente
  [Theory]
  [InlineData("CBC", "CBC")]
  [InlineData("GCM", "GCM")]
  [InlineData("CBCUnsafe", "CBCZeroIv")]
  [InlineData("invalid", "GCM")]
  [InlineData(null, "GCM")]
  public void Algorithm_SetValue_MapsToExpectedAlgorithm(string? input, string expected)
  {
    // Arrange & Act
    var options = new CryptographyOptions
    {
      Algorithm = input!
    };

    // Assert
    Assert.Contains(expected, options.Algorithm, StringComparison.OrdinalIgnoreCase);
  }

  // Teste para verificar a chave em Base64
  [Fact]
  public void SecretBase64_SetAndGet_ReturnsSameValue()
  {
    // Arrange
    var key = CryptographyTestKeys.FromText("mysecret-crypto");

    // Act
    var options = new CryptographyOptions
    {
      SecretBase64 = key
    };

    // Assert
    Assert.Equal(key, options.SecretBase64);
  }
}
