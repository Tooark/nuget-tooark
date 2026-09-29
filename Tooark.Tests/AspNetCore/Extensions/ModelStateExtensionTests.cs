using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tooark.AspNetCore.Extensions;

namespace Tooark.Tests.AspNetCore.Extensions;

public class ModelStateExtensionTests
{
  // Testes do método sem erros
  [Fact]
  public void GetErrors_ReturnsEmptyList_WhenModelStateIsValid()
  {
    // Arrange
    var modelState = new ModelStateDictionary();

    // Act
    var result = modelState.GetErrors();

    // Assert
    Assert.Empty(result);
  }

  // Testes do método com erros, que devolve uma mensagem por erro registrado
  [Fact]
  public void GetErrors_ReturnsErrors_WhenModelStateHasErrors()
  {
    // Arrange
    var modelState = new ModelStateDictionary();
    modelState.AddModelError("Key1", "Error message 1");
    modelState.AddModelError("Key2", "Error message 2");

    // Act
    var result = modelState.GetErrors();

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Contains("Error message 1", result);
    Assert.Contains("Error message 2", result);
  }

  // Testes da ordem: vem das chaves, não da ordem de registro, com os erros de cada campo juntos e em sequência
  [Fact]
  public void GetErrors_ReturnsErrorsGroupedByField_RegardlessOfRecordingOrder()
  {
    // Arrange
    var nameFirst = new ModelStateDictionary();
    nameFirst.AddModelError("Name", "Field.Required;Name");
    nameFirst.AddModelError("Email", "Field.Required;Email");
    nameFirst.AddModelError("Email", "Field.Invalid;Email");

    var emailFirst = new ModelStateDictionary();
    emailFirst.AddModelError("Email", "Field.Required;Email");
    emailFirst.AddModelError("Name", "Field.Required;Name");
    emailFirst.AddModelError("Email", "Field.Invalid;Email");

    // Act
    var result = nameFirst.GetErrors();

    // Assert
    Assert.Equal(result, emailFirst.GetErrors());
    Assert.Equal(3, result.Count);

    var email = result.IndexOf("Field.Required;Email");
    Assert.Equal("Field.Invalid;Email", result[email + 1]);
  }

  // Testes do erro registrado só com a exceção, que vira a chave de campo inválido sem expor a exceção
  [Fact]
  public void GetErrors_ReturnsInvalidFieldKey_WhenErrorHasOnlyException()
  {
    // Arrange
    var modelState = new ModelStateDictionary();
    modelState.TryAddModelException("Age", new InvalidOperationException("Internal detail"));

    // Act
    var result = modelState.GetErrors();

    // Assert
    Assert.Equal(["Field.Invalid;Age"], result);
  }

  // Testes da mensagem só com espaços, que o ResponseDto descartaria como vazia
  [Fact]
  public void GetErrors_ReturnsInvalidFieldKey_WhenErrorMessageIsWhiteSpace()
  {
    // Arrange
    var modelState = new ModelStateDictionary();
    modelState.AddModelError("Name", " ");

    // Act
    var result = modelState.GetErrors();

    // Assert
    Assert.Equal(["Field.Invalid;Name"], result);
  }

  // Testes do erro sem texto e sem campo, como o registrado quando o limite de erros é atingido
  [Fact]
  public void GetErrors_ReturnsBadRequestKey_WhenErrorHasNoTextAndNoField()
  {
    // Arrange
    var modelState = new ModelStateDictionary(maxAllowedErrors: 2);
    modelState.AddModelError("Name", "Field.Required;Name");
    modelState.AddModelError("Email", "Field.Required;Email");

    // Act
    var result = modelState.GetErrors();

    // Assert
    Assert.Equal(["BadRequest", "Field.Required;Name"], result);
  }
}
