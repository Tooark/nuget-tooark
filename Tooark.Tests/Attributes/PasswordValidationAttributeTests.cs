using System.ComponentModel.DataAnnotations;
using Tooark.Attributes;

namespace Tooark.Tests.Attributes;

public class PasswordValidationAttributeTests
{
  // Instância do atributo de PasswordValidation para ser testado
  private readonly PasswordValidationAttribute _passwordValidationAttribute = new();

  // Teste de senha válida com senha válida
  [Theory]
  [InlineData("zzZZ11##")]
  [InlineData("ZZzz11##")]
  [InlineData("11zzZZ##")]
  [InlineData("##zzZZ11")]
  public void IsValid_ShouldBeValid_WhenGivenValidPassword(string? password)
  {
    // Arrange & Act
    var result = _passwordValidationAttribute.IsValid(password);

    // Assert
    Assert.True(result);
  }

  // Teste de senha inválida com senha inválida
  [Theory]
  [InlineData("zzzzzzzz")]
  [InlineData("ZZZZZZZZ")]
  [InlineData("11111111")]
  [InlineData("!!!!!!!!")]
  [InlineData("zZzZzZzZ")]
  [InlineData("z1z1z1z1")]
  [InlineData("z!z!z!z!")]
  [InlineData("Z1Z1Z1Z1")]
  [InlineData("Z!Z!Z!Z!")]
  [InlineData("1!1!1!1!")]
  [InlineData("zZ1zZ1zZ")]
  [InlineData("zZ!zZ!zZ")]
  [InlineData("z1!z1!z1")]
  [InlineData("Z1!Z1!Z1")]
  [InlineData("zZ1!zZ1")]
  public void IsValid_ShouldBeInvalid_WhenGivenInvalidPassword(string? password)
  {
    // Arrange & Act
    var result = _passwordValidationAttribute.IsValid(password);

    // Assert
    Assert.False(result);
    Assert.Equal("Field.Invalid;Password", Mensagem(_passwordValidationAttribute, password));
  }

  // Teste de senha nula ou vazia
  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void IsValid_NullPassword_SetsErrorMessage(string? password)
  {
    // Arrange & Act
    var result = _passwordValidationAttribute.IsValid(password);

    // Assert
    Assert.False(result);
    Assert.Equal("Field.Required;Password", Mensagem(_passwordValidationAttribute, password));
  }

  // Teste de senha válida com senha válida para os parâmetros de validação
  [Theory]
  [InlineData("Senha@12", true, true, true, true, 8)]
  [InlineData("Senha123", true, true, true, false, 8)]
  [InlineData("Senha!@#", true, true, false, true, 8)]
  [InlineData("SenhaSen", true, true, false, false, 8)]
  [InlineData("senha@12", true, false, true, true, 8)]
  [InlineData("senha123", true, false, true, false, 8)]
  [InlineData("senha!@#", true, false, false, true, 8)]
  [InlineData("senhasen", true, false, false, false, 8)]
  [InlineData("SENHA@12", false, true, true, true, 8)]
  [InlineData("SENHA123", false, true, true, false, 8)]
  [InlineData("SENHA!@#", false, true, false, true, 8)]
  [InlineData("SENHASEN", false, true, false, false, 8)]
  [InlineData("1234!@#$", false, false, true, true, 8)]
  [InlineData("12341234", false, false, true, false, 8)]
  [InlineData("!@#$!@#$", false, false, false, true, 8)]
  [InlineData("Senha@12", false, false, false, false, 8)]
  [InlineData("Senha@123", true, true, true, true, 9)]
  [InlineData("Senha@12", true, true, true, true, 7)]
  [InlineData("Senha@1", true, true, true, true, 7)]
  public void IsValid_ShouldBeValid_WhenGivenParam(string? password, bool lower, bool upper, bool number, bool symbol, int length)
  {
    // Arrange
    PasswordValidationAttribute _passwordParam = new(lower, upper, number, symbol, length);

    // Act
    var result = _passwordParam.IsValid(password);

    // Assert
    Assert.True(result);
  }

  // Teste de senha inválida com senha inválida para os parâmetros de validação
  [Theory]
  [InlineData("!@#$!@#$", true, true, true, false, 8)]
  [InlineData("12341234", true, true, false, true, 8)]
  [InlineData("1234!@#$", true, true, false, false, 8)]
  [InlineData("SENHASEN", true, false, true, true, 8)]
  [InlineData("SENHA!@#", true, false, true, false, 8)]
  [InlineData("SENHA123", true, false, false, true, 8)]
  [InlineData("SENHA@12", true, false, false, false, 8)]
  [InlineData("senhasen", false, true, true, true, 8)]
  [InlineData("senha!@#", false, true, true, false, 8)]
  [InlineData("senha123", false, true, false, true, 8)]
  [InlineData("senha@12", false, true, false, false, 8)]
  [InlineData("senhaSEN", false, false, true, true, 8)]
  [InlineData("Senha!@#", false, false, true, false, 8)]
  [InlineData("Senha123", false, false, false, true, 8)]
  [InlineData("", false, false, false, false, 8)]
  [InlineData("Senha@12", true, true, true, true, 9)]
  public void IsValid_ShouldBeInvalid_WhenGivenParam(string? password, bool lower, bool upper, bool number, bool symbol, int length)
  {
    // Arrange
    PasswordValidationAttribute _passwordParam = new(lower, upper, number, symbol, length);

    // Act
    var result = _passwordParam.IsValid(password);

    // Assert
    Assert.False(result);
  }

  // A mensagem passou a vir no resultado da validacao, e nao do estado do atributo.
  private static string? Mensagem(ValidationAttribute atributo, object? valor) =>
    atributo.GetValidationResult(valor, new ValidationContext(new object()))?.ErrorMessage;
}
