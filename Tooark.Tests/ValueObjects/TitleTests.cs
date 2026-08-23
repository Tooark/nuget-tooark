using Tooark.Extensions;
using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class TitleTests
{
  // Teste se o valor é um titulo válido
  [Theory]
  [InlineData("abcd123")]
  [InlineData("ABCD123")]
  [InlineData("abCD123")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  public void Title_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;
    var expectedNormalize = valueParam.ToNormalize();

    // Act
    var title = new Title(valueParam);

    // Assert
    Assert.True(title.IsValid);
    Assert.Equal(expectedValue, title.Value);
    Assert.Equal(expectedNormalize, title.Normalized);
  }

  // Teste se o valor é um titulo inválido
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public void Title_ShouldBeInvalid_WhenGivenInvalidTitle(string? valueParam)
  {
    // Arrange & Act
    var title = new Title(valueParam!);

    // Assert
    Assert.False(title.IsValid);
    Assert.Equal(string.Empty, title.Value);
    Assert.Equal("", title.Normalized);
  }

  // Testa se o método ToString retorna um titulo
  [Fact]
  public void Title_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();
    var title = new Title(value);

    // Act
    var titleString = title.ToString();

    // Assert
    Assert.Equal(expectedValue, titleString);
    Assert.Equal(expectedNormalize, title.Normalized);
  }

  // Testa se titulo está sendo convertido para string implicitamente
  [Fact]
  public void Title_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();
    var title = new Title(value);

    // Act
    string titleString = title;

    // Assert
    Assert.Equal(expectedValue, titleString);
    Assert.Equal(expectedNormalize, title.Normalized);
  }

  // Testa se titulo está sendo convertido de string implicitamente
  [Fact]
  public void Title_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();

    // Act
    Title title = value;

    // Assert
    Assert.True(title.IsValid);
    Assert.Equal(expectedValue, title.Value);
    Assert.Equal(expectedNormalize, title.Normalized);
  }
}
