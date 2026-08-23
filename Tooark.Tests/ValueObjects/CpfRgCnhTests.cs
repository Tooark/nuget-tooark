using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class CpfRgCnhTests
{
  // Teste se é um CPF, RG ou CNH a partir de um CPF, RG ou CNH valido
  [Theory]
  [InlineData("118.214.830-14")]
  [InlineData("475.750.550-70")]
  [InlineData("28.589.200-9")]
  [InlineData("24.758.544-0")]
  [InlineData("11.831.807-X")]
  [InlineData("11.831.807-x")]
  [InlineData("11.831.807")]
  [InlineData("17932463758")]
  [InlineData("75314994670")]
  [InlineData("65161594873")]
  public void CpfRgCnh_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var document = new CpfRgCnh(valueParam);

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }

  // Teste se é um CPF, RG ou CNH a partir de um CPF, RG ou CNH invalido
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
  [InlineData("00000000000")]
  [InlineData("12345678901")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void CpfRgCnh_ShouldBeInvalid_WhenGivenInvalidCpfRgCnh(string? valueParam)
  {
    // Arrange & Act
    var document = new CpfRgCnh(valueParam!);

    // Assert
    Assert.False(document.IsValid);
    Assert.Equal(string.Empty, document.Number);
  }

  // Testa se o método ToString retorna um CPF, RG ou CNH
  [Fact]
  public void CpfRgCnh_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "123.456.789-09";
    var expectedValue = value;
    var document = new CpfRgCnh(value);

    // Act
    var letterString = document.ToString();

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CPF, RG ou CNH está sendo convertida para string implicitamente
  [Fact]
  public void CpfRgCnh_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "123.456.789-09";
    var expectedValue = value;
    var document = new CpfRgCnh(value);

    // Act
    string letterString = document;

    // Assert
    Assert.Equal(expectedValue, letterString);
  }

  // Testa se é um CPF, RG ou CNH está sendo convertida de string implicitamente
  [Fact]
  public void CpfRgCnh_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "123.456.789-09";
    var expectedValue = value;

    // Act
    CpfRgCnh document = value;

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(expectedValue, document.Number);
  }
}
