using Tooark.Securities.Options;

#pragma warning disable CS0618 // CryptographyOptions.Secret é obsoleto: os testes garantem que continua funcionando até a v5

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

  // Teste para verificar secret
  [Fact]
  public void Secret_SetAndGet_ReturnsSameValue()
  {
    // Arrange & Act
    var options = new CryptographyOptions
    {
      Secret = "mysecret-crypto"
    };

    // Assert
    Assert.Equal("mysecret-crypto", options.Secret);
  }

  // Testa se o Secret carrega o ObsoleteAttribute com a orientação de migração para a v5
  [Fact]
  public void Secret_ShouldBeMarkedObsolete()
  {
    // Arrange
    var property = typeof(CryptographyOptions).GetProperty(nameof(CryptographyOptions.Secret));

    // Act
    var attribute = property?.GetCustomAttributes(typeof(ObsoleteAttribute), false).OfType<ObsoleteAttribute>().SingleOrDefault();

    // Assert
    Assert.NotNull(attribute);
    Assert.Contains("SecretBase64", attribute.Message);
    Assert.Contains("v5", attribute.Message);
  }
}
