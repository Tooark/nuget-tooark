using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class ZipCodeTests
{
  // Teste se é um Código Postal a partir de um Código Postal válido
  [Theory]
  [InlineData("12345-678")]
  [InlineData("12345")]
  public void ZipCode_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var letterNumeric = new ZipCode(valueParam);

    // Assert
    Assert.True(letterNumeric.IsValid);
    Assert.Equal(expectedValue, letterNumeric.Value);
  }

  // Teste se é um Código Postal a partir de um Código Postal inválido
  [Theory]
  [InlineData("1")]
  [InlineData("12")]
  [InlineData("123")]
  [InlineData("1234")]
  [InlineData("1234-123")]
  [InlineData("12345-")]
  [InlineData("12345-1")]
  [InlineData("12345-12")]
  [InlineData("12345-1234")]
  [InlineData("123456-123")]
  [InlineData("xxxxx-xxx")]
  [InlineData("")]
  [InlineData(null)]
  public void ZipCode_ShouldBeInvalid_WhenGivenInvalidZipCode(string? valueParam)
  {
    // Arrange & Act
    var letterNumeric = new ZipCode(valueParam!);

    // Assert
    Assert.False(letterNumeric.IsValid);
    Assert.Equal(string.Empty, letterNumeric.Value);
  }

  // Testa se o método ToString retorna um Código Postal
  [Fact]
  public void ZipCode_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "12345-678";
    var expectedValue = value;
    var letterNumeric = new ZipCode(value);

    // Act
    var zipCodeString = letterNumeric.ToString();

    // Assert
    Assert.Equal(expectedValue, zipCodeString);
  }

  // Testa se  Código Postal está sendo convertida para string implicitamente
  [Fact]
  public void ZipCode_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "12345-678";
    var expectedValue = value;
    var letterNumeric = new ZipCode(value);

    // Act
    string zipCodeString = letterNumeric;

    // Assert
    Assert.Equal(expectedValue, zipCodeString);
  }

  // Testa se  Código Postal está sendo convertida de string implicitamente
  [Fact]
  public void ZipCode_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "12345-678";
    var expectedValue = value;

    // Act
    ZipCode letterNumeric = value;

    // Assert
    Assert.True(letterNumeric.IsValid);
    Assert.Equal(expectedValue, letterNumeric.Value);
  }
}
