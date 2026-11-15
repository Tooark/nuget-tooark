using Tooark.Validations.Messages;

namespace Tooark.Tests.Validations.Messages;

public class ValidationErrorMessagesTests
{
  // Testa se a mensagem correta é retornada quando a validação de BooleanIsFalse falha
  [Theory]
  [InlineData("property", "Validation.IsNotFalse;property")]
  public void BooleanIsFalse_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.BooleanIsFalse(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de BooleanIsTrue falha
  [Theory]
  [InlineData("property", "Validation.IsNotTrue;property")]
  public void BooleanIsTrue_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.BooleanIsTrue(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsGreater falha
  [Theory]
  [InlineData("property", "comparer", "Validation.IsNotGreater;property;comparer")]
  public void IsGreater_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsGreater(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsGreaterOrEquals falha
  [Theory]
  [InlineData("property", "comparer", "Validation.IsNotGreaterOrEquals;property;comparer")]
  public void IsGreaterOrEquals_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsGreaterOrEquals(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsLower falha
  [Theory]
  [InlineData("property", "comparer", "Validation.IsNotLower;property;comparer")]
  public void IsLower_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsLower(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsLowerOrEquals falha
  [Theory]
  [InlineData("property", "comparer", "Validation.IsNotLowerOrEquals;property;comparer")]
  public void IsLowerOrEquals_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsLowerOrEquals(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsBetween falha
  [Theory]
  [InlineData("property", "start", "end", "Validation.IsNotBetween;property;start;end")]
  public void IsBetween_ShouldReturnCorrectMessage(string property, string start, string end, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsBetween(property, start, end);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNotBetween falha
  [Theory]
  [InlineData("property", "start", "end", "Validation.IsBetween;property;start;end")]
  public void IsNotBetween_ShouldReturnCorrectMessage(string property, string start, string end, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNotBetween(property, start, end);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsMin falha
  [Theory]
  [InlineData("property", "comparer", "Validation.IsNotMin;property;comparer")]
  public void IsMin_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsMin(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNotMin falha
  [Theory]
  [InlineData("property", "comparer", "Validation.IsMin;property;comparer")]
  public void IsNotMin_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNotMin(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsMax falha
  [Theory]
  [InlineData("property", "comparer", "Validation.IsNotMax;property;comparer")]
  public void IsMax_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsMax(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNotMax falha
  [Theory]
  [InlineData("property", "comparer", "Validation.IsMax;property;comparer")]
  public void IsNotMax_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNotMax(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de AreEquals falha
  [Theory]
  [InlineData("property", "comparer", "Validation.AreNotEquals;property;comparer")]
  public void AreEquals_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.AreEquals(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de AreNotEquals falha
  [Theory]
  [InlineData("property", "comparer", "Validation.AreEquals;property;comparer")]
  public void AreNotEquals_ShouldReturnCorrectMessage(string property, string comparer, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.AreNotEquals(property, comparer);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de Contains falha
  [Theory]
  [InlineData("property", "value", "Validation.NotContains;property;value")]
  public void Contains_ShouldReturnCorrectMessage(string property, string value, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.Contains(property, value);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de NotContains falha
  [Theory]
  [InlineData("property", "value", "Validation.Contains;property;value")]
  public void NotContains_ShouldReturnCorrectMessage(string property, string value, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.NotContains(property, value);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de All falha
  [Theory]
  [InlineData("property", "value", "Validation.NotAll;property;value")]
  public void All_ShouldReturnCorrectMessage(string property, string value, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.All(property, value);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de NotAll falha
  [Theory]
  [InlineData("property", "value", "Validation.All;property;value")]
  public void NotAll_ShouldReturnCorrectMessage(string property, string value, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.NotAll(property, value);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNull falha
  [Theory]
  [InlineData("property", "Validation.IsNotNull;property")]
  public void IsNull_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNull(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNotNull falha
  [Theory]
  [InlineData("property", "Validation.IsNull;property")]
  public void IsNotNull_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNotNull(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsEmpty falha
  [Theory]
  [InlineData("property", "Validation.IsNotEmpty;property")]
  public void IsEmpty_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsEmpty(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNotEmpty falha
  [Theory]
  [InlineData("property", "Validation.IsEmpty;property")]
  public void IsNotEmpty_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNotEmpty(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de Match falha
  [Theory]
  [InlineData("property", "value", "Validation.NotMatch;property;value")]
  public void Match_ShouldReturnCorrectMessage(string property, string value, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.Match(property, value);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de NotMatch falha
  [Theory]
  [InlineData("property", "value", "Validation.Match;property;value")]
  public void NotMatch_ShouldReturnCorrectMessage(string property, string value, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.NotMatch(property, value);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNullOrEmpty falha
  [Theory]
  [InlineData("property", "Validation.IsNotNullOrEmpty;property")]
  public void IsNullOrEmpty_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNullOrEmpty(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNotNullOrEmpty falha
  [Theory]
  [InlineData("property", "Validation.IsNullOrEmpty;property")]
  public void IsNotNullOrEmpty_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNotNullOrEmpty(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNullOrWhiteSpace falha
  [Theory]
  [InlineData("property", "Validation.IsNotNullOrWhiteSpace;property")]
  public void IsNullOrWhiteSpace_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNullOrWhiteSpace(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsNotNullOrWhiteSpace falha
  [Theory]
  [InlineData("property", "Validation.IsNullOrWhiteSpace;property")]
  public void IsNotNullOrWhiteSpace_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsNotNullOrWhiteSpace(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsDocument falha
  [Theory]
  [InlineData("property", "formatter", "Validation.IsNotDocument;property;formatter")]
  public void IsDocument_ShouldReturnCorrectMessage(string property, string formatter, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsDocument(property, formatter);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se a mensagem correta é retornada quando a validação de IsValid falha
  [Theory]
  [InlineData("property", "type", "Validation.IsNotValid;property;type")]
  public void IsValid_ShouldReturnCorrectMessage(string property, string type, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.IsValid(property, type);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se FieldInvalid monta a chave Field.Invalid;{Propriedade}, idêntica à que os value objects emitiam à mão
  [Theory]
  [InlineData("Email", "Field.Invalid;Email")]
  [InlineData(" Email ", "Field.Invalid;Email")]
  [InlineData("Document.Cpf", "Field.Invalid;Document.Cpf")]
  [InlineData("my property", "Field.Invalid;myproperty")]
  public void FieldInvalid_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.FieldInvalid(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se FieldRequired monta a chave Field.Required;{Propriedade}
  [Theory]
  [InlineData("Email", "Field.Required;Email")]
  [InlineData(" File Format ", "Field.Required;FileFormat")]
  public void FieldRequired_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.FieldRequired(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se FieldEmpty monta a chave Field.Empty;{Propriedade}
  [Theory]
  [InlineData("Id", "Field.Empty;Id")]
  public void FieldEmpty_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.FieldEmpty(property);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se FieldChangeBlocked monta a chave Field.ChangeBlocked;{Propriedade}
  [Theory]
  [InlineData("CreatedBy", "Field.ChangeBlocked;CreatedBy")]
  public void FieldChangeBlocked_ShouldReturnCorrectMessage(string property, string expected)
  {
    // Arrange & Act
    var result = ValidationErrorMessages.FieldChangeBlocked(property);

    // Assert
    Assert.Equal(expected, result);
  }
}
