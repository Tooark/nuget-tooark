using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class LanguageCodeTests
{
  // Teste de o código do idioma é válido a partir de um código de idioma válido
  [Theory]
  [InlineData("en-us")]
  [InlineData("en-uS")]
  [InlineData("en-Us")]
  [InlineData("en-US")]
  [InlineData("eN-us")]
  [InlineData("eN-uS")]
  [InlineData("eN-Us")]
  [InlineData("eN-US")]
  [InlineData("En-us")]
  [InlineData("En-uS")]
  [InlineData("En-Us")]
  [InlineData("En-US")]
  [InlineData("EN-us")]
  [InlineData("EN-uS")]
  [InlineData("EN-Us")]
  [InlineData("EN-US")]
  public void LanguageCode_ShouldBeValid_WhenGivenValidCode(string codeParam)
  {
    // Arrange
    var expectedCode = codeParam[..2].ToLowerInvariant() + "-" + codeParam[3..].ToUpperInvariant();

    // Act
    var languageCode = new LanguageCode(codeParam);

    // Assert
    Assert.True(languageCode.IsValid);
    Assert.Equal(expectedCode, languageCode.Code);
  }

  // Teste de o código do idioma é inválido a partir de um código de idioma inválido
  [Theory]
  [InlineData("enus")]
  [InlineData("enus-")]
  [InlineData("enu-s")]
  [InlineData("e-nus")]
  [InlineData("-enus")]
  [InlineData("enus_")]
  [InlineData("enu_s")]
  [InlineData("en_us")]
  [InlineData("e_nus")]
  [InlineData("_enus")]
  [InlineData("en")]
  [InlineData("us")]
  [InlineData("")]
  [InlineData(null)]
  public void LanguageCode_ShouldBeInvalid_WhenGivenInvalidLanguageCode(string? codeParam)
  {
    // Arrange & Act
    var languageCode = new LanguageCode(codeParam!);

    // Assert
    Assert.False(languageCode.IsValid);
    Assert.Equal(string.Empty, languageCode.Code);
  }

  // Testa se o método ToString retorna o código do idioma
  [Fact]
  public void LanguageCode_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var code = "en-US";
    var expectedCode = code[..2].ToLowerInvariant() + "-" + code[3..].ToUpperInvariant();
    var languageCode = new LanguageCode(code);

    // Act
    var languageCodeString = languageCode.ToString();

    // Assert
    Assert.Equal(expectedCode, languageCodeString);
  }

  // Testa se o endereço de languageCode está sendo convertido para string implicitamente
  [Fact]
  public void LanguageCode_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var code = "en-US";
    var expectedCode = code[..2].ToLowerInvariant() + "-" + code[3..].ToUpperInvariant();
    var languageCode = new LanguageCode(code);

    // Act
    string languageCodeString = languageCode;

    // Assert
    Assert.Equal(expectedCode, languageCodeString);
  }

  // Testa se o endereço de languageCode está sendo convertido de string implicitamente
  [Fact]
  public void LanguageCode_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var code = "en-US";
    var expectedCode = code[..2].ToLowerInvariant() + "-" + code[3..].ToUpperInvariant();

    // Act
    LanguageCode languageCode = code;

    // Assert
    Assert.True(languageCode.IsValid);
    Assert.Equal(expectedCode, languageCode.Code);
  }

  // Teste se a constante pública de tamanho está disponível para consulta externa e condiz com o formato xx-XX
  [Fact]
  public void LanguageCode_Length_ShouldBePubliclyConsultable()
  {
    // Arrange
    var languageCode = new LanguageCode("pt-BR");

    // Assert
    Assert.Equal(5, LanguageCode.Length);
    Assert.Equal(LanguageCode.Length, languageCode.Code.Length);
  }
}
