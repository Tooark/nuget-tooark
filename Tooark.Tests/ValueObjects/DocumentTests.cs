using Tooark.Enums;
using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class DocumentTests
{
  // Testa se o documento é válido a partir de um número e tipo de documento válidos
  [Theory]
  [InlineData("78.293.721/0001-24", "CNPJ")]
  [InlineData("46.935.447/0001-53", "CPF_CNPJ")]
  [InlineData("118.214.830-14", "CPF")]
  [InlineData("475.750.550-70", "CPF")]
  [InlineData("28.589.200-9", "RG")]
  [InlineData("24.758.544-0", "CPF_RG")]
  [InlineData("11.831.807-X", "CPF_RG_CNH")]
  [InlineData("11.831.807-x", "RG")]
  [InlineData("11.831.807", "RG")]
  [InlineData("17932463758", "CNH")]
  [InlineData("75314994670", "CNH")]
  [InlineData("65161594873", "CPF_RG_CNH")]
  public void Document_ShouldBeValid_WhenNumberAndTypeAreValid(string valueParam, string typeParam)
  {
    // Arrange
    var number = valueParam;
    EDocumentType type = typeParam;

    // Act
    var document = new Document(number, type);

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(number, document.Number);
    Assert.Equal(type, document.Type);
  }

  // Testa se o documento é inválido a partir de um número inválido
  [Theory]
  [InlineData("12.345.678/0001-12")]
  [InlineData("00.000.000/0000-00")]
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
  [InlineData("01234567890")]
  [InlineData("1234")]
  [InlineData("!@#$")]
  [InlineData("aB1234")]
  [InlineData("aB!@#$")]
  [InlineData("")]
  [InlineData(null)]
  public void Document_ShouldBeInvalid_WhenNumberIsInvalid(string? valueParam)
  {
    // Arrange
    var number = valueParam!;
    var type = EDocumentType.CPF;

    // Act
    var document = new Document(number, type);

    // Assert
    Assert.False(document.IsValid);
  }

  // Teste se o documento é válido a partir de um número válido e tipo de documento nulo
  [Fact]
  public void Document_ShouldSetTypeToNone_WhenTypeIsNotProvided()
  {
    // Arrange
    var number = "123456789";

    // Act
    var document = new Document(number);

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(number, document.Number);
    Assert.Equal(EDocumentType.None, document.Type);
  }

  // Testa se o método ToString retorna o número do documento
  [Fact]
  public void Document_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var number = "123456789";
    var document = new Document(number);

    // Act
    var documentString = document.ToString();

    // Assert
    Assert.Equal(number, documentString);
  }

  // Testa se o documento é válido para conversão implícita para uma string
  [Fact]
  public void Document_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var number = "123456789";
    var document = new Document(number);

    // Act
    string documentString = document;

    // Assert
    Assert.Equal(number, documentString);
  }

  // Testa se o documento é válido para conversão implícita de uma string
  [Fact]
  public void Document_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var number = "123456789";

    // Act
    Document document = number;

    // Assert
    Assert.True(document.IsValid);
    Assert.Equal(number, document.Number);
  }
}
