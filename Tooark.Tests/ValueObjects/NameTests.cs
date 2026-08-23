using Tooark.Extensions;
using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class NameTests
{
  // Teste se o valor é um nome válido
  [Theory]
  [InlineData("abcd123")]
  [InlineData("ABCD123")]
  [InlineData("abCD123")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  public void Name_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;
    var expectedNormalize = valueParam.ToNormalize();

    // Act
    var name = new Name(valueParam);

    // Assert
    Assert.True(name.IsValid);
    Assert.Equal(expectedValue, name.Value);
    Assert.Equal(expectedNormalize, name.Normalized);
  }

  // Teste se o valor é um nome inválido
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public void Name_ShouldBeInvalid_WhenGivenInvalidName(string? valueParam)
  {
    // Arrange & Act
    var name = new Name(valueParam!);

    // Assert
    Assert.False(name.IsValid);
    Assert.Equal(string.Empty, name.Value);
    Assert.Equal("", name.Normalized);
  }

  // Testa se o método ToString retorna um nome
  [Fact]
  public void Name_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();
    var name = new Name(value);

    // Act
    var nameString = name.ToString();

    // Assert
    Assert.Equal(expectedValue, nameString);
    Assert.Equal(expectedNormalize, name.Normalized);
  }

  // Testa se nome está sendo convertido para string implicitamente
  [Fact]
  public void Name_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();
    var name = new Name(value);

    // Act
    string nameString = name;

    // Assert
    Assert.Equal(expectedValue, nameString);
    Assert.Equal(expectedNormalize, name.Normalized);
  }

  // Testa se nome está sendo convertido de string implicitamente
  [Fact]
  public void Name_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();

    // Act
    Name name = value;

    // Assert
    Assert.True(name.IsValid);
    Assert.Equal(expectedValue, name.Value);
    Assert.Equal(expectedNormalize, name.Normalized);
  }
}
