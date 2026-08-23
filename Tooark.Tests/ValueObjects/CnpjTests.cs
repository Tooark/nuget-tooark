using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class CnpjTests
{
  // Teste se é um CNPJ a partir de um CNPJ valido
  [Theory]
  [InlineData("78.293.721/0001-24")]
  [InlineData("46.935.447/0001-53")]
  public void Cnpj_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var document = new Cnpj(valueParam);

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }

  // Teste se é um CNPJ a partir de um CNPJ invalido
  [Theory]
  [InlineData("12.345.678/0001-12")]
  [InlineData("00.000.000/0000-00")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void Cnpj_ShouldBeInvalid_WhenGivenInvalidCnpj(string? valueParam)
  {
    // Arrange & Act
    var document = new Cnpj(valueParam!);

    // Assert
    Assert.False(document.IsValid);
    Assert.Equal(string.Empty, document.Number);
  }

  // Testa se o método ToString retorna um CNPJ
  [Fact]
  public void Cnpj_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "78.293.721/0001-24";
    var expectedValue = value;
    var document = new Cnpj(value);

    // Act
    var letterString = document.ToString();

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CNPJ está sendo convertida para string implicitamente
  [Fact]
  public void Cnpj_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "78.293.721/0001-24";
    var expectedValue = value;
    var document = new Cnpj(value);

    // Act
    string letterString = document;

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CNPJ está sendo convertida de string implicitamente
  [Fact]
  public void Cnpj_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "78.293.721/0001-24";
    var expectedValue = value;

    // Act
    Cnpj document = value;

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }
}
