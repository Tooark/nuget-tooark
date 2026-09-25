using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class LetterNumericNumericTests
{
  // Teste se é uma string de apenas letras e números a partir de uma string de apenas letras e números
  [Theory]
  [InlineData("abcd")]
  [InlineData("ABCD")]
  [InlineData("abCD")]
  [InlineData("1234")]
  [InlineData("aB1234")]
  public void LetterNumeric_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var letterNumeric = new LetterNumeric(valueParam);

    // Assert
    Assert.True(letterNumeric.IsValid);
    Assert.Equal(expectedValue, letterNumeric.Value);
  }

  // Teste se é uma string de apenas letras e números a partir de uma string de letras, números e símbolos (inválida)
  [Theory]
  [InlineData("-1-2")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void LetterNumeric_ShouldBeInvalid_WhenGivenInvalidLetterNumeric(string? valueParam)
  {
    // Arrange & Act
    var letterNumeric = new LetterNumeric(valueParam!);

    // Assert
    Assert.False(letterNumeric.IsValid);
    Assert.Equal(string.Empty, letterNumeric.Value);
  }

  // Testa se o método ToString retorna uma string de letras e números
  [Fact]
  public void LetterNumeric_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "abc123";
    var expectedValue = value;
    var letterNumeric = new LetterNumeric(value);

    // Act
    var letterNumericString = letterNumeric.ToString();

    // Assert
    Assert.Equal(expectedValue, letterNumericString);
  }

  // Testa se a string de letras e números está sendo convertida para string implicitamente
  [Fact]
  public void LetterNumeric_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "abc123";
    var expectedValue = value;
    var letterNumeric = new LetterNumeric(value);

    // Act
    string letterNumericString = letterNumeric;

    // Assert
    Assert.Equal(expectedValue, letterNumericString);
  }

  // Testa se a string de letras e números está sendo convertida de string implicitamente
  [Fact]
  public void LetterNumeric_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "abc123";
    var expectedValue = value;

    // Act
    LetterNumeric letterNumeric = value;

    // Assert
    Assert.True(letterNumeric.IsValid);
    Assert.Equal(expectedValue, letterNumeric.Value);
  }
}
