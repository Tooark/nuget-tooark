using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class CpfRgTests
{
  // Teste se é um CPF ou RG a partir de um CPF ou RG valido
  [Theory]
  [InlineData("118.214.830-14")]
  [InlineData("475.750.550-70")]
  [InlineData("28.589.200-9")]
  [InlineData("24.758.544-0")]
  [InlineData("11.831.807-X")]
  [InlineData("11.831.807-x")]
  [InlineData("11.831.807")]
  public void CpfRg_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var document = new CpfRg(valueParam);

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }

  // Teste se é um CPF ou RG a partir de um CPF ou RG invalido
  [Theory]
  [InlineData("123.456.789-01")]
  [InlineData("000.000.000-00")]
  [InlineData("00.000.000-0")]
  [InlineData("00.000.000-X")]
  [InlineData("00.000.000-x")]
  [InlineData("00.000.000")]
  [InlineData("12.345.678-9")]
  [InlineData("12.345.678-X")]
  [InlineData("12.345.678-x")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void CpfRg_ShouldBeInvalid_WhenGivenInvalidCpfRg(string? valueParam)
  {
    // Arrange & Act
    var document = new CpfRg(valueParam!);

    // Assert
    Assert.False(document.IsValid);
    Assert.Equal(string.Empty, document.Number);
  }

  // Testa se o método ToString retorna um CPF ou RG
  [Fact]
  public void CpfRg_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "123.456.789-09";
    var expectedValue = value;
    var document = new CpfRg(value);

    // Act
    var letterString = document.ToString();

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CPF ou RG está sendo convertida para string implicitamente
  [Fact]
  public void CpfRg_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "123.456.789-09";
    var expectedValue = value;
    var document = new CpfRg(value);

    // Act
    string letterString = document;

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CPF ou RG está sendo convertida de string implicitamente
  [Fact]
  public void CpfRg_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "123.456.789-09";
    var expectedValue = value;

    // Act
    CpfRg document = value;

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }
}
