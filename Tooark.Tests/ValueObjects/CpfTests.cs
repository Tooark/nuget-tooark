using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class CpfTests
{
  // Teste se é um CPF a partir de um CPF valido
  [Theory]
  [InlineData("118.214.830-14")]
  [InlineData("475.750.550-70")]
  public void Cpf_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var document = new Cpf(valueParam);

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }

  // Teste se é um CPF a partir de um CPF invalido
  [Theory]
  [InlineData("123.456.789-01")]
  [InlineData("000.000.000-00")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void Cpf_ShouldBeInvalid_WhenGivenInvalidCpf(string? valueParam)
  {
    // Arrange & Act
    var document = new Cpf(valueParam!);

    // Assert
    Assert.False(document.IsValid);
    Assert.Equal(string.Empty, document.Number);
  }

  // Testa se o método ToString retorna um CPF
  [Fact]
  public void Cpf_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "118.214.830-14";
    var expectedValue = value;
    var document = new Cpf(value);

    // Act
    var letterString = document.ToString();

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CPF está sendo convertida para string implicitamente
  [Fact]
  public void Cpf_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "118.214.830-14";
    var expectedValue = value;
    var document = new Cpf(value);

    // Act
    string letterString = document;

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CPF está sendo convertida de string implicitamente
  [Fact]
  public void Cpf_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "118.214.830-14";
    var expectedValue = value;

    // Act
    Cpf document = value;

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }
}
