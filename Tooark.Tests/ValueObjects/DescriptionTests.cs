using Tooark.Extensions;
using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class DescriptionTests
{
  // Teste se o valor é uma descrição válida
  [Theory]
  [InlineData("abcd123")]
  [InlineData("ABCD123")]
  [InlineData("abCD123")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  public void Description_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;
    var expectedNormalize = valueParam.ToNormalize();

    // Act
    var description = new Description(valueParam);

    // Assert
    Assert.True(description.IsValid);
    Assert.Equal(expectedValue, description.Value);
    Assert.Equal(expectedNormalize, description.Normalized);
  }

  // Teste se o valor é uma descrição inválida
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public void Description_ShouldBeInvalid_WhenGivenInvalidDescription(string? valueParam)
  {
    // Arrange & Act
    var description = new Description(valueParam!);

    // Assert
    Assert.False(description.IsValid);
    Assert.Equal(string.Empty, description.Value);
    Assert.Equal("", description.Normalized);
  }

  // Testa se o método ToString retorna uma descrição
  [Fact]
  public void Description_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();
    var description = new Description(value);

    // Act
    var descriptionString = description.ToString();

    // Assert
    Assert.Equal(expectedValue, descriptionString);
    Assert.Equal(expectedNormalize, description.Normalized);
  }

  // Testa se a descrição está sendo convertida para string implicitamente
  [Fact]
  public void Description_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();
    var description = new Description(value);

    // Act
    string descriptionString = description;

    // Assert
    Assert.Equal(expectedValue, descriptionString);
    Assert.Equal(expectedNormalize, description.Normalized);
  }

  // Testa se a descrição está sendo convertida de string implicitamente
  [Fact]
  public void Description_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "abcABC";
    var expectedValue = value;
    var expectedNormalize = value.ToNormalize();

    // Act
    Description description = value;

    // Assert
    Assert.True(description.IsValid);
    Assert.Equal(expectedValue, description.Value);
    Assert.Equal(expectedNormalize, description.Normalized);
  }
}
