using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class CpfCnpjTests
{
  // Teste se é um CPF ou CNPJ a partir de um CPF ou CNPJ valido
  [Theory]
  [InlineData("118.214.830-14")]
  [InlineData("78.293.721/0001-24")]
  public void CpfCnpj_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var document = new CpfCnpj(valueParam);

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }

  // Teste se é um CPF ou CNPJ a partir de um CPF ou CNPJ invalido
  [Theory]
  [InlineData("123.456.789-01")]
  [InlineData("12.345.678/9012-34")]
  [InlineData("000.000.000-00")]
  [InlineData("00.000.000/0000-00")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void CpfCnpj_ShouldBeInvalid_WhenGivenInvalidCpfCnpj(string? valueParam)
  {
    // Arrange & Act
    var document = new CpfCnpj(valueParam!);

    // Assert
    Assert.False(document.IsValid);
    Assert.Equal(string.Empty, document.Number);
  }

  // Testa se o método ToString retorna um CPF ou CNPJ
  [Fact]
  public void CpfCnpj_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "118.214.830-14";
    var expectedValue = value;
    var document = new CpfCnpj(value);

    // Act
    var letterString = document.ToString();

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CPF ou CNPJ está sendo convertida para string implicitamente
  [Fact]
  public void CpfCnpj_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "118.214.830-14";
    var expectedValue = value;
    var document = new CpfCnpj(value);

    // Act
    string letterString = document;

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CPF ou CNPJ está sendo convertida de string implicitamente
  [Fact]
  public void CpfCnpj_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "118.214.830-14";
    var expectedValue = value;

    // Act
    CpfCnpj document = value;

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }
}
