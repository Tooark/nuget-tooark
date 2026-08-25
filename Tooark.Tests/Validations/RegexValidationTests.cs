using System.Text.RegularExpressions;
using Tooark.Validations;

namespace Tooark.Tests.Validations;

public class RegexValidationTests
{
  // Teste para validar se o valor corresponde ao padrão e cria notificação, com valor que não corresponde
  [Fact]
  public void Match_ShouldAddNotification_WhenValueNotMatchPattern()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "abc";
    string pattern = @"^[0-9]*$";

    // Act
    validation.Match(value, pattern, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor corresponde ao padrão e não cria notificação, com valor que corresponde
  [Fact]
  public void Match_ShouldNotAddNotification_WhenValueMatchPattern()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "abc";
    string pattern = @"^[a-z]*$";

    // Act
    validation.Match(value, pattern, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor não corresponde ao padrão e cria notificação, com valor que corresponde
  [Fact]
  public void NotMatch_ShouldAddNotification_WhenValueMatchPattern()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "abc";
    string pattern = @"^[a-z]*$";

    // Act
    validation.NotMatch(value, pattern, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor não corresponde ao padrão e não cria notificação, com valor que não corresponde
  [Fact]
  public void NotMatch_ShouldNotAddNotification_WhenValueNotMatchPattern()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "abc";
    string pattern = @"^[0-9]*$";

    // Act
    validation.NotMatch(value, pattern, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }  

  // Teste para validar se o valor corresponde ao padrão e cria notificação, com valor que não corresponde
  [Fact]
  public void Match_ShouldAddNotification_WhenValueNotMatchPattern_WithCaseSensitive()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "abc";
    string pattern = @"^[0-9]*$";
    RegexOptions options = RegexOptions.IgnoreCase;

    // Act
    validation.Match(value, pattern, property, options);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor corresponde ao padrão e não cria notificação, com valor que corresponde
  [Fact]
  public void Match_ShouldNotAddNotification_WhenValueMatchPattern_WithCaseSensitive()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string valueLower = "abc";
    string valueUpper = "ABC";
    string pattern = @"^[a-z]*$";
    RegexOptions options = RegexOptions.IgnoreCase;

    // Act
    validation
      .Match(valueLower, pattern, property, options)
      .Match(valueUpper, pattern, property, options);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor não corresponde ao padrão e cria notificação, com valor que corresponde
  [Fact]
  public void NotMatch_ShouldAddNotification_WhenValueMatchPattern_WithCaseSensitive()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string valueLower = "abc";
    string valueUpper = "ABC";
    string pattern = @"^[a-z]*$";
    RegexOptions options = RegexOptions.IgnoreCase;

    // Act
    validation
      .NotMatch(valueLower, pattern, property, options)
      .NotMatch(valueUpper, pattern, property, options);

    // Assert
    Assert.Equal(2, validation.Notifications.Count);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor não corresponde ao padrão e não cria notificação, com valor que não corresponde
  [Fact]
  public void NotMatch_ShouldNotAddNotification_WhenValueNotMatchPattern_WithCaseSensitive()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "abc";
    string pattern = @"^[0-9]*$";
    RegexOptions options = RegexOptions.IgnoreCase;

    // Act
    validation.NotMatch(value, pattern, property, options);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste se a constante pública do tempo limite padrão está disponível para consulta externa com o valor esperado
  [Fact]
  public void DefaultTimeout_ShouldBePubliclyConsultable()
  {
    // Assert
    Assert.Equal(300, Validation.DefaultTimeout);
  }
}
