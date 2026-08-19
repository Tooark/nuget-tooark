using System.ComponentModel.DataAnnotations;
using Tooark.Attributes;

namespace Tooark.Tests.Attributes;

public class EmailValidationTests
{
  // Instância do atributo de EmailValidation para ser testado
  private readonly EmailValidationAttribute _emailValidationAttribute = new();

  // Teste de email válido com email válido
  [Theory]
  [InlineData("teste@example.com")]
  [InlineData("te.te@example.com")]
  [InlineData("te-te@example.com")]
  [InlineData("te_te@example.com")]
  [InlineData("teste@exa-ple.com")]
  [InlineData("TESTE@EXAMPLE.COM")]
  [InlineData("TE.TE@EXAMPLE.COM")]
  [InlineData("TE-TE@EXAMPLE.COM")]
  [InlineData("TE_TE@EXAMPLE.COM")]
  [InlineData("TESTE@EXA-PLE.COM")]
  public void IsValid_ShouldBeValid_WhenGivenValidEmail(string? email)
  {
    // Arrange & Act
    var result = _emailValidationAttribute.IsValid(email);

    // Assert
    Assert.True(result);
  }

  // Teste de email inválido com email inválido
  [Theory]
  [InlineData(".test@example.com")]
  [InlineData("-test@example.com")]
  [InlineData("_test@example.com")]
  [InlineData("test.@example.com")]
  [InlineData("test-@example.com")]
  [InlineData("test_@example.com")]
  [InlineData("te!st@example.com")]
  [InlineData("te#st@example.com")]
  [InlineData("te$st@example.com")]
  [InlineData("teçst@example.com")]
  [InlineData("test@@example.com")]
  [InlineData(".@example.com")]
  [InlineData("teste@.")]
  [InlineData("test@example@.com")]
  [InlineData("test@.example.com")]
  [InlineData("test@-example.com")]
  [InlineData("test@_example.com")]
  [InlineData("test@example-.com")]
  [InlineData("test@example_.com")]
  [InlineData("teste@exa!ple.com")]
  [InlineData("teste@exa#ple.com")]
  [InlineData("teste@exa$ple.com")]
  [InlineData("teste@exaçple.com")]
  [InlineData("test@example.-com")]
  [InlineData("test@example._com")]
  [InlineData("test@example.com-")]
  [InlineData("test@example.com_")]
  [InlineData("test@example.com.")]
  [InlineData("test@exaple..com")]
  [InlineData("test@example")]
  [InlineData("test@.com")]
  [InlineData("test@")]
  public void IsValid_ShouldBeInvalid_WhenGivenInvalidEmail(string? email)
  {
    // Arrange & Act
    var result = _emailValidationAttribute.IsValid(email);

    // Assert
    Assert.False(result);
    Assert.Equal("Field.Invalid;Email", Mensagem(_emailValidationAttribute, email));
  }

  // Teste de email nulo ou vazio
  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void IsValid_NullEmail_SetsErrorMessage(string? email)
  {
    // Arrange & Act
    var result = _emailValidationAttribute.IsValid(email);

    // Assert
    Assert.False(result);
    Assert.Equal("Field.Required;Email", Mensagem(_emailValidationAttribute, email));
  }

  // A mensagem passou a vir no resultado da validacao, e nao do estado do atributo.
  private static string? Mensagem(ValidationAttribute atributo, object? valor) =>
    atributo.GetValidationResult(valor, new ValidationContext(new object()))?.ErrorMessage;
}
