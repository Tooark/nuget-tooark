using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class RgTests
{
  // Teste se é um RG a partir de um RG valido
  [Theory]
  [InlineData("28.589.200-9")]
  [InlineData("24.758.544-0")]
  [InlineData("11.831.807-X")]
  [InlineData("11.831.807-x")]
  [InlineData("11.831.807")]
  public void Rg_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var document = new Rg(valueParam);

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }

  // Teste se é um RG a partir de um RG invalido
  [Theory]
  [InlineData("00.000.000-0")]
  [InlineData("00.000.000-X")]
  [InlineData("00.000.000-x")]
  [InlineData("00.000.000")]
  [InlineData("12.345.678-9")]
  [InlineData("12.345.678-X")]
  [InlineData("12.345.678-x")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void Rg_ShouldBeInvalid_WhenGivenInvalidRg(string? valueParam)
  {
    // Arrange & Act
    var document = new Rg(valueParam!);

    // Assert
    Assert.False(document.IsValid);
    Assert.Equal(string.Empty, document.Number);
  }

  // Testa se o método ToString retorna um RG
  [Fact]
  public void Rg_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "28.589.200-9";
    var expectedValue = value;
    var document = new Rg(value);

    // Act
    var letterString = document.ToString();

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um RG está sendo convertida para string implicitamente
  [Fact]
  public void Rg_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "28.589.200-9";
    var expectedValue = value;
    var document = new Rg(value);

    // Act
    string letterString = document;

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um RG está sendo convertida de string implicitamente
  [Fact]
  public void Rg_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "28.589.200-9";
    var expectedValue = value;

    // Act
    Rg document = value;

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }
}
