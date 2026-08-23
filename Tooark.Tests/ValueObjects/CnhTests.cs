using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class CnhTests
{
  // Teste se é um CNH a partir de um CNH valido
  [Theory]
  [InlineData("17932463758")]
  [InlineData("75314994670")]
  [InlineData("65161594873")]
  public void Cnh_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var document = new Cnh(valueParam);

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }

  // Teste se é um CNH a partir de um CNH invalido
  [Theory]
  [InlineData("00000000000")]
  [InlineData("01234567890")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void Cnh_ShouldBeInvalid_WhenGivenInvalidCnh(string? valueParam)
  {
    // Arrange & Act
    var document = new Cnh(valueParam!);

    // Assert
    Assert.False(document.IsValid);
    Assert.Equal(string.Empty, document.Number);
  }

  // Testa se o método ToString retorna um CNH
  [Fact]
  public void Cnh_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "17932463758";
    var expectedValue = value;
    var document = new Cnh(value);

    // Act
    var letterString = document.ToString();

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CNH está sendo convertida para string implicitamente
  [Fact]
  public void Cnh_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "17932463758";
    var expectedValue = value;
    var document = new Cnh(value);

    // Act
    string letterString = document;

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CNH está sendo convertida de string implicitamente
  [Fact]
  public void Cnh_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "17932463758";
    var expectedValue = value;

    // Act
    Cnh document = value;

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }
}
