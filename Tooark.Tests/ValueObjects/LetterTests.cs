using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class LetterTests
{
  // Teste se é uma string de apenas letras a partir de uma string de apenas letras
  [Theory]
  [InlineData("abcd")]
  [InlineData("ABCD")]
  [InlineData("abCD")]
  public void Letter_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var letter = new Letter(valueParam);

    // Assert
    Assert.True(letter.IsValid);
    Assert.Equal(expectedValue, letter.Value);
  }

  // Teste se é uma string de apenas letras a partir de uma string de letras, números e símbolos (inválida)
  [Theory]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void Letter_ShouldBeInvalid_WhenGivenInvalidLetter(string? valueParam)
  {
    // Arrange & Act
    var letter = new Letter(valueParam!);

    // Assert
    Assert.False(letter.IsValid);
    Assert.Equal(string.Empty, letter.Value);
  }

  // Testa se o método ToString retorna uma string de letras
  [Fact]
  public void Letter_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var letter = new Letter(value);

    // Act
    var letterString = letter.ToString();

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se a string de letras está sendo convertida para string implicitamente
  [Fact]
  public void Letter_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var letter = new Letter(value);

    // Act
    string letterString = letter;

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se a string de letras está sendo convertida de string implicitamente
  [Fact]
  public void Letter_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;

    // Act
    Letter letter = value;

    // Assert
    Assert.True(letter.IsValid);
    Assert.Equal(expectedValue, letter.Value);
  }
}
