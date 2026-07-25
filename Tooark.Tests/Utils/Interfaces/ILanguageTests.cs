using System.Globalization;
using Moq;
using Tooark.Utils.Interfaces;

namespace Tooark.Tests.Utils.Interfaces;

[Collection("CultureSensitive")]
public class ILanguageTests
{
  // Testa se o valor padrão da linguagem é "en-US"
  [Fact]
  public void DefaultLanguage_ShouldReturnCorrectValue()
  {
    // Arrange
    var mockLanguage = new Mock<ILanguage>();
    mockLanguage.Setup(l => l.DefaultLanguage).Returns("en-US");

    // Act
    var defaultLanguage = mockLanguage.Object.DefaultLanguage;

    // Assert
    Assert.Equal("en-US", defaultLanguage);
  }

  // Testa se a cultura atual é alterada corretamente
  [Fact]
  public void CurrentLanguage_ShouldReturnCorrectValue()
  {
    // Arrange
    var mockLanguage = new Mock<ILanguage>();
    mockLanguage.Setup(l => l.CurrentLanguage).Returns("pt-BR");

    // Act
    var currentLanguage = mockLanguage.Object.CurrentLanguage;

    // Assert
    Assert.Equal("pt-BR", currentLanguage);
  }

  // Testa se a cultura atual é alterada corretamente
  [Fact]
  public void CurrentCulture_ShouldReturnCorrectValue()
  {
    // Arrange
    var mockLanguage = new Mock<ILanguage>();
    var cultureInfo = new CultureInfo("fr-FR");
    mockLanguage.Setup(l => l.CurrentCultureInfo).Returns(cultureInfo);

    // Act
    var currentCulture = mockLanguage.Object.CurrentCultureInfo;

    // Assert
    Assert.Equal(cultureInfo, currentCulture);
  }

  // Testa se a cultura atual é alterada corretamente
  [Fact]
  public void SetCulture_WithString_ShouldSetCorrectCulture()
  {
    // Arrange
    var mockLanguage = new Mock<ILanguage>();
    var cultureInfo = new CultureInfo("es-ES");

    // Act
    mockLanguage.Object.SetCultureInfo("es-ES");

    // Assert
    mockLanguage.Verify(l => l.SetCultureInfo(It.Is<string>(s => s == "es-ES")), Times.Once);
  }

  // Testa se a cultura atual é alterada corretamente com um objeto CultureInfo
  [Fact]
  public void SetCulture_WithCultureInfo_ShouldSetCorrectCulture()
  {
    // Arrange
    var mockLanguage = new Mock<ILanguage>();
    var cultureInfo = new CultureInfo("de-DE");

    // Act
    mockLanguage.Object.SetCultureInfo(cultureInfo);

    // Assert
    mockLanguage.Verify(l => l.SetCultureInfo(It.Is<CultureInfo>(c => c.Equals(cultureInfo))), Times.Once);
  }
}
