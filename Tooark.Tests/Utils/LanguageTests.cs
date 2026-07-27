using System.Globalization;
using Tooark.Utils;

namespace Tooark.Tests.Utils;

[Collection("CultureSensitive")]
public class LanguageTests
{
  public LanguageTests()
  {
    // Define a instância da linguagem como a implementação padrão
    Language.Instance = new Language.LanguageImplementation();
  }

  // Testa se o valor padrão da linguagem é "en-US"
  [Fact]
  public void DefaultLanguage_ShouldBeEnUS()
  {
    // Arrange & Act
    var defaultLanguage = Language.Default;

    // Assert
    Assert.Equal("en-US", defaultLanguage);
  }

  // Testa se a cultura atual é alterada corretamente
  [Theory]
  [InlineData("en-US")]
  [InlineData("es-ES")]
  [InlineData("jp-JP")]
  [InlineData("pt-BR")]
  [InlineData("pt-PT")]
  public void SetCulture_ShouldChangeCurrentCulture(string culture)
  {
    // Arrange & Act
    Language.SetCulture(culture);

    // Assert
    Assert.Equal(culture, Language.Current);
    Assert.Equal(culture, Language.CurrentCulture.Name);
  }

  // Testa se a cultura atual é alterada corretamente com um objeto CultureInfo
  [Theory]
  [InlineData("en-US")]
  [InlineData("es-ES")]
  [InlineData("jp-JP")]
  [InlineData("pt-BR")]
  [InlineData("pt-PT")]
  public void SetCulture_WithCultureInfo_SetsCurrentCulture(string newCulture)
  {
    // Arrange
    CultureInfo culture = new(newCulture);

    // Act
    Language.SetCulture(culture);

    // Assert
    Assert.Equal(newCulture, Language.Current);
    Assert.Equal(culture, Language.CurrentCulture);
  }

  // Testa se a cultura atual mantém o valor padrão quando a cultura informada é inválida
  [Theory]
  [InlineData("en")]
  [InlineData("es")]
  [InlineData("jp")]
  [InlineData("pt")]
  [InlineData("ptBR")]
  public void SetCulture_ShouldDefaultCulture_WhenCultureInvalid(string culture)
  {
    // Arrange & Act
    Language.SetCulture(culture);

    // Assert
    Assert.Equal(Language.Default, Language.Current);
    Assert.Equal(Language.Default, Language.CurrentCulture.Name);
  }
}
