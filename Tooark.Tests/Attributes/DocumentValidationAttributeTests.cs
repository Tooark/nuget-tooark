using System.ComponentModel.DataAnnotations;
using Tooark.Attributes;
using Tooark.Enums;

namespace Tooark.Tests.Attributes;

public class DocumentValidationAttributeTests
{
  // Teste de documento válido com documento válido
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
  public void IsValid_ShouldBeValid_WhenGivenValidDocument(string? document, string type)
  {
    // Arrange
    DocumentValidationAttribute _documentValidationAttribute = new(type);

    // Act
    var result = _documentValidationAttribute.IsValid(document);

    // Assert
    Assert.True(result);
  }

  // Teste de documento inválido com documento inválido
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
  public void IsValid_ShouldBeInvalid_WhenGivenInvalidDocument(string? document)
  {
    // Arrange
    var type = EDocumentType.CPF;
    DocumentValidationAttribute _documentValidationAttribute = new(type);

    // Act
    var result = _documentValidationAttribute.IsValid(document);

    // Assert
    Assert.False(result);
    Assert.Equal("Field.Invalid;Document", Mensagem(_documentValidationAttribute, document));
  }

  // Teste de documento nulo ou vazio
  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void IsValid_NullDocument_SetsErrorMessage(string? document)
  {
    // Arrange
    var type = EDocumentType.CPF;
    DocumentValidationAttribute _documentValidationAttribute = new(type);

    // Act
    var result = _documentValidationAttribute.IsValid(document);

    // Assert
    Assert.False(result);
    Assert.Equal("Field.Required;Document", Mensagem(_documentValidationAttribute, document));
  }

  // A mensagem passou a vir no resultado da validacao, e nao do estado do atributo.
  private static string? Mensagem(ValidationAttribute atributo, object? valor) =>
    atributo.GetValidationResult(valor, new ValidationContext(new object()))?.ErrorMessage;

  // Testa se tipo de documento ausente é tratado como desconhecido, e não como None legítimo.
  // O atributo aceita o tipo como texto, então nada impede [DocumentValidation(null!)].
  [Fact]
  public void Resolve_ShouldThrow_WhenTypeIsMissing()
  {
    // Arrange
    var atributo = new DocumentValidationAttribute(null!);

    // Act & Assert
    var excecao = Assert.Throws<Tooark.Exceptions.InternalServerErrorException>(
      () => atributo.IsValid("529.982.247-25"));

    Assert.Contains("Attributes.DocumentTypeUnknown;", excecao.Message);
  }
}
