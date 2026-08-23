using Tooark.Extensions;
using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class KeywordTests
{
  // Teste se o valor é uma palavra-chave válida
  [Theory]
  [InlineData("abcd123")]
  [InlineData("ABCD123")]
  [InlineData("abCD123")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  public void Keyword_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;
    var expectedNormalize = valueParam.ToNormalize();

    // Act
    var keyword = new Keyword(valueParam);

    // Assert
    Assert.True(keyword.IsValid);
    Assert.Equal(expectedValue, keyword.Value);
    Assert.Equal(expectedNormalize, keyword.Normalized);
  }

  // Teste se o valor é uma palavra-chave inválida
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public void Keyword_ShouldBeInvalid_WhenGivenInvalidKeyword(string? valueParam)
  {
    // Arrange & Act
    var keyword = new Keyword(valueParam!);

    // Assert
    Assert.False(keyword.IsValid);
    Assert.Equal(string.Empty, keyword.Value);
    Assert.Equal("", keyword.Normalized);
  }

  // Testa se o método ToString retorna uma palavra-chave
  [Fact]
  public void Keyword_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();
    var keyword = new Keyword(value);

    // Act
    var keywordString = keyword.ToString();

    // Assert
    Assert.Equal(expectedValue, keywordString);
    Assert.Equal(expectedNormalize, keyword.Normalized);
  }

  // Testa se a palavra-chave está sendo convertida para string implicitamente
  [Fact]
  public void Keyword_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();
    var keyword = new Keyword(value);

    // Act
    string keywordString = keyword;

    // Assert
    Assert.Equal(expectedValue, keywordString);
    Assert.Equal(expectedNormalize, keyword.Normalized);
  }

  // Testa se a palavra-chave está sendo convertida de string implicitamente
  [Fact]
  public void Keyword_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();

    // Act
    Keyword keyword = value;

    // Assert
    Assert.True(keyword.IsValid);
    Assert.Equal(expectedValue, keyword.Value);
    Assert.Equal(expectedNormalize, keyword.Normalized);
  }
}
