using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class NumericTests
{
  // Teste se é uma string de apenas números a partir de uma string de apenas números
  [Theory]
  [InlineData("1234")]
  public void Numeric_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var numeric = new Numeric(valueParam);

    // Assert
    Assert.True(numeric.IsValid);
    Assert.Equal(expectedValue, numeric.Value);
  }

  // Teste se é uma string de apenas números a partir de uma string de números, letras e símbolos (inválida)
  [Theory]
  [InlineData("abAB")]
  [InlineData("!@#$")]
  [InlineData("-1-2")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void Numeric_ShouldBeInvalid_WhenGivenInvalidNumeric(string? valueParam)
  {
    // Arrange & Act
    var numeric = new Numeric(valueParam!);

    // Assert
    Assert.False(numeric.IsValid);
    Assert.Equal(string.Empty, numeric.Value);
  }

  // Testa se o método ToString retorna uma string de números
  [Fact]
  public void Numeric_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "123456";
    var expectedValue = value;
    var numeric = new Numeric(value);

    // Act
    var numericString = numeric.ToString();

    // Assert
    Assert.Equal(expectedValue, numericString);
  }

  // Testa se a string de números está sendo convertida para string implicitamente
  [Fact]
  public void Numeric_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "123456";
    var expectedValue = value;
    var numeric = new Numeric(value);

    // Act
    string numericString = numeric;

    // Assert
    Assert.Equal(expectedValue, numericString);
  }

  // Testa se a string de números está sendo convertida de string implicitamente
  [Fact]
  public void Numeric_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "123456";
    var expectedValue = value;

    // Act
    Numeric numeric = value;

    // Assert
    Assert.True(numeric.IsValid);
    Assert.Equal(expectedValue, numeric.Value);
  }
}
