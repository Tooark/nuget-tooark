using System.ComponentModel.DataAnnotations;
using Tooark.Attributes;

namespace Tooark.Tests.Attributes;

public class ZipCodeValidationAttributeTests
{
  // Instância do atributo de ZipCodeValidation para ser testado
  private readonly ZipCodeValidationAttribute _zipCodeValidationAttribute = new();

  // Teste de código postal válido com código postal válido
  [Theory]
  [InlineData("12345-678")]
  [InlineData("12345")]
  public void IsValid_ShouldBeValid_WhenGivenValidZipCode(string? code)
  {
    // Arrange & Act
    var result = _zipCodeValidationAttribute.IsValid(code);

    // Assert
    Assert.True(result);
  }

  // Teste de código postal inválido com código postal inválido
  [Theory]
  [InlineData("1")]
  [InlineData("12")]
  [InlineData("123")]
  [InlineData("1234")]
  [InlineData("1234-123")]
  [InlineData("12345-")]
  [InlineData("12345-1")]
  [InlineData("12345-12")]
  [InlineData("12345-1234")]
  [InlineData("123456-123")]
  [InlineData("xxxxx-xxx")]
  public void IsValid_ShouldBeInvalid_WhenGivenInvalidZipCode(string? code)
  {
    // Arrange & Act
    var result = _zipCodeValidationAttribute.IsValid(code);

    // Assert
    Assert.False(result);
    Assert.Equal("Field.Invalid;ZipCode", Mensagem(_zipCodeValidationAttribute, code));
  }

  // Teste de código postal nulo ou vazio
  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void IsValid_NullZipCode_SetsErrorMessage(string? code)
  {
    // Arrange & Act
    var result = _zipCodeValidationAttribute.IsValid(code);

    // Assert
    Assert.False(result);
    Assert.Equal("Field.Required;ZipCode", Mensagem(_zipCodeValidationAttribute, code));
  }

  // A mensagem passou a vir no resultado da validacao, e nao do estado do atributo.
  private static string? Mensagem(ValidationAttribute atributo, object? valor) =>
    atributo.GetValidationResult(valor, new ValidationContext(new object()))?.ErrorMessage;
}
