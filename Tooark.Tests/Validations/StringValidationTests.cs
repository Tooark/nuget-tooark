using Tooark.Validations;

namespace Tooark.Tests.Validations;

public class StringValidationTests
{
  // Teste para validar se o tamanho do valor é maior que o comparador e criar uma notificação, com comparador maior ou igual
  [Theory]
  [InlineData("Test Value", 10)]
  [InlineData("Test Value", 11)]
  public void IsGreater_ShouldAddNotification_WhenValueIsNotGreater(string value, int comparer)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsGreater(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o tamanho do valor é maior que o comparador e não criar uma notificação, com comparador menor
  [Theory]
  [InlineData("Test Value", 8)]
  [InlineData("Test Value", 9)]
  public void IsGreater_ShouldNotAddNotification_WhenValueIsGreater(string value, int comparer)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsGreater(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o tamanho do valor é maior ou igual que o comparador e criar uma notificação, com comparador maior
  [Theory]
  [InlineData("Test Value", 11)]
  [InlineData("Test Value", 12)]
  public void IsGreaterOrEquals_ShouldAddNotification_WhenValueIsNotGreaterOrEquals(string value, int comparer)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsGreaterOrEquals(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o tamanho do valor é maior ou igual que o comparador e não criar uma notificação, com comparador menor ou igual
  [Theory]
  [InlineData("Test Value", 9)]
  [InlineData("Test Value", 10)]
  public void IsGreaterOrEquals_ShouldNotAddNotification_WhenValueIsGreaterOrEquals(string value, int comparer)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsGreaterOrEquals(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o tamanho do valor é menor que o comparador e criar uma notificação, com comparador menor ou igual
  [Theory]
  [InlineData("Test Value", 9)]
  [InlineData("Test Value", 10)]
  public void IsLower_ShouldAddNotification_WhenValueIsNotGreater(string value, int comparer)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsLower(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o tamanho do valor é menor que o comparador e não criar uma notificação, com comparador maior
  [Theory]
  [InlineData("Test Value", 11)]
  [InlineData("Test Value", 12)]
  public void IsLower_ShouldNotAddNotification_WhenValueIsLower(string value, int comparer)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsLower(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o tamanho do valor é menor ou igual que o comparador e criar uma notificação, com comparador menor
  [Theory]
  [InlineData("Test Value", 8)]
  [InlineData("Test Value", 9)]
  public void IsLowerOrEquals_ShouldAddNotification_WhenValueIsNotLowerOrEquals(string value, int comparer)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsLowerOrEquals(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o tamanho do valor é menor ou igual que o comparador e não criar uma notificação, com comparador maior ou igual
  [Theory]
  [InlineData("Test Value", 10)]
  [InlineData("Test Value", 11)]
  public void IsLowerOrEquals_ShouldNotAddNotification_WhenValueIsLowerOrEquals(string value, int comparer)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsLowerOrEquals(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o tamanho do valor está entre o início e o fim e cria notificação, com valor menor e maior
  [Theory]
  [InlineData("Test Value", 5, 6)]
  [InlineData("Test Value", 15, 16)]
  public void IsBetween_ShouldAddNotification_WhenValueIsNotBetweenStartAndEnd(string value, int start, int end)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsBetween(value, start, end, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o tamanho do valor está entre o início e o fim e cria notificação, com valor menor e maior
  [Theory]
  [InlineData("Test Value", 5, 10)]
  [InlineData("Test Value", 10, 16)]
  public void IsBetween_ShouldNotAddNotification_WhenValueIsBetweenStartAndEnd(string value, int start, int end)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsBetween(value, start, end, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o tamanho do valor não está entre o início e o fim e cria notificação, com valor entre
  [Theory]
  [InlineData("Test Value", 5, 10)]
  [InlineData("Test Value", 10, 16)]
  public void IsNotBetween_ShouldAddNotification_WhenValueIsBetweenStartAndEnd(string value, int start, int end)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsNotBetween(value, start, end, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o tamanho do valor não está entre o início e o fim e não cria notificação, com valor menor e maior
  [Theory]
  [InlineData("Test Value", 5, 6)]
  [InlineData("Test Value", 15, 16)]
  public void IsNotBetween_ShouldNotAddNotification_WhenValueIsNotBetweenStartAndEnd(string value, int start, int end)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();

    // Act
    validation.IsNotBetween(value, start, end, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o tamanho do valor é igual ao comparador e cria notificação, com valores diferentes
  [Fact]
  public void AreEquals_ShouldAddNotification_WhenValueIsNotEqualLength()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Value";
    int comparer = 0;

    // Act
    validation.AreEquals(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o tamanho do valor é igual ao comparador e não cria notificação, com valores iguais
  [Fact]
  public void AreEquals_ShouldNotAddNotification_WhenValueIsEqualLength()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test";
    int comparer = 4;

    // Act
    validation.AreEquals(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor é igual ao comparador e cria notificação, com valores diferentes
  [Fact]
  public void AreEquals_ShouldAddNotification_WhenValueIsNotEqual()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Value";
    string comparer = "Comparer";

    // Act
    validation.AreEquals(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor é igual ao comparador e não cria notificação, com valores iguais
  [Fact]
  public void AreEquals_ShouldNotAddNotification_WhenValueIsEqual()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test";
    string comparer = "Test";

    // Act
    validation.AreEquals(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o tamanho do valor é não igual ao comparador e cria notificação, com valores iguais
  [Fact]
  public void AreNotEquals_ShouldAddNotification_WhenValueIsEqualLength()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test";
    int comparer = 4;

    // Act
    validation.AreNotEquals(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o tamanho do valor não é igual ao comparador e não cria notificação, com valores diferentes
  [Fact]
  public void AreNotEquals_ShouldNotAddNotification_WhenValueIsNotEqualLength()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Value";
    int comparer = 0;

    // Act
    validation.AreNotEquals(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor é não igual ao comparador e cria notificação, com valores iguais
  [Fact]
  public void AreNotEquals_ShouldAddNotification_WhenValueIsEqual()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test";
    string comparer = "Test";

    // Act
    validation.AreNotEquals(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor não é igual ao comparador e não cria notificação, com valores diferentes
  [Fact]
  public void AreNotEquals_ShouldNotAddNotification_WhenValueIsNotEqual()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Value";
    string comparer = "Comparer";

    // Act
    validation.AreNotEquals(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor contém no comparador e cria notificação, com valor não contido
  [Fact]
  public void Contains_ShouldAddNotification_WhenValueIsNotInComparer()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test Value";
    string comparer = "Comparer";

    // Act
    validation.Contains(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor contém no comparador e não cria notificação, com valor contido
  [Fact]
  public void Contains_ShouldNotAddNotification_WhenValueIsInComparer()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test Value";
    string comparer = "Test";

    // Act
    validation.Contains(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor contém na lista e cria notificação, com valor não contido
  [Fact]
  public void Contains_ShouldAddNotification_WhenValueIsNotInList()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Value";
    string[] comparer = ["Test", "Comparer"];

    // Act
    validation.Contains(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor contém na lista e não cria notificação, com valor contido
  [Fact]
  public void Contains_ShouldNotAddNotification_WhenValueIsInList()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test";
    string[] comparer = ["Test", "Comparer"];

    // Act
    validation.Contains(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor não contém no comparador e cria notificação, com valor contido
  [Fact]
  public void NotContains_ShouldAddNotification_WhenValueIsInComparer()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test Value";
    string comparer = "Test";

    // Act
    validation.NotContains(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor não contém no comparador e não cria notificação, com valor não contido
  [Fact]
  public void NotContains_ShouldNotAddNotification_WhenValueIsNotInComparer()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test Value";
    string comparer = "Comparer";

    // Act
    validation.NotContains(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor não contém na lista e cria notificação, com valor contido
  [Fact]
  public void NotContains_ShouldAddNotification_WhenValueIsInList()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test";
    string[] comparer = ["Test", "Comparer"];

    // Act
    validation.NotContains(value, comparer, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor não contém na lista e não cria notificação, com valor não contido
  [Fact]
  public void NotContains_ShouldNotAddNotification_WhenValueIsNotInList()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = "Test Value";
    string[] comparer = ["Test", "Comparer"];

    // Act
    validation.NotContains(value, comparer, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor é nulo e cria notificação, com valor não nulo
  [Fact]
  public void IsNull_ShouldAddNotification_WhenIsNotNull()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = "Value";

    // Act
    validation.IsNull(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor é nulo e não cria notificação, com valor nulo
  [Fact]
  public void IsNull_ShouldNotAddNotification_WhenIsNull()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = null;

    // Act
    validation.IsNull(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor não é nulo e cria notificação, com valor nulo
  [Fact]
  public void IsNotNull_ShouldAddNotification_WhenIsNull()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = null;

    // Act
    validation.IsNotNull(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor não é nulo e não cria notificação, com valor não nulo
  [Fact]
  public void IsNotNull_ShouldNotAddNotification_WhenIsNotNull()
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = "Value";

    // Act
    validation.IsNotNull(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor é nulo ou vazio e cria notificação, com valor não nulo ou vazio
  [Theory]
  [InlineData("Value")]
  [InlineData(" ")]
  public void IsNullOrEmpty_ShouldAddNotification_WhenIsNotNullOrEmpty(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = valueParam;

    // Act
    validation.IsNullOrEmpty(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor é nulo ou vazio e não cria notificação, com valor nulo ou vazio
  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void IsNullOrEmpty_ShouldNotAddNotification_WhenIsNullOrEmpty(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = valueParam;

    // Act
    validation.IsNullOrEmpty(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor não é nulo ou vazio e cria notificação, com valor nulo ou vazio
  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void IsNotNullOrEmpty_ShouldAddNotification_WhenIsNullOrEmpty(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = valueParam;

    // Act
    validation.IsNotNullOrEmpty(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor não é nulo ou vazio e não cria notificação, com valor não nulo ou vazio
  [Theory]
  [InlineData("Value")]
  [InlineData(" ")]
  public void IsNotNullOrEmpty_ShouldNotAddNotification_WhenIsNotNullOrEmpty(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = valueParam;

    // Act
    validation.IsNotNullOrEmpty(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor é nulo ou espaço em branco e cria notificação, com valor não nulo ou espaço em branco
  [Theory]
  [InlineData("Value")]
  [InlineData("Test")]
  public void IsNullOrWhiteSpace_ShouldAddNotification_WhenIsNotNullOrWhiteSpace(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = valueParam;

    // Act
    validation.IsNullOrWhiteSpace(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor é nulo ou espaço em branco e não cria notificação, com valor nulo ou espaço em branco
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public void IsNullOrWhiteSpace_ShouldNotAddNotification_WhenIsNullOrWhiteSpace(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = valueParam;

    // Act
    validation.IsNullOrWhiteSpace(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor não é nulo ou espaço em branco e cria notificação, com valor nulo ou espaço em branco
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData(null)]
  public void IsNotNullOrWhiteSpace_ShouldAddNotification_WhenIsNullOrWhiteSpace(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = valueParam;

    // Act
    validation.IsNotNullOrWhiteSpace(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor não é nulo ou espaço em branco e não cria notificação, com valor não nulo ou espaço em branco
  [Theory]
  [InlineData("Value")]
  [InlineData("Test")]
  public void IsNotNullOrWhiteSpace_ShouldNotAddNotification_WhenIsNotNullOrWhiteSpace(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string? value = valueParam;

    // Act
    validation.IsNotNullOrWhiteSpace(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  #region Nulo tratado como vazio nas comparações

  // Testa se as comparações com comparador textual tratam valor nulo como vazio. A sobrecarga que
  // compara texto não fazia esse tratamento, ao contrário da que compara tamanho: as duas
  // discordavam sobre a mesma entrada.
  [Theory]
  [InlineData(null, "", true)]
  [InlineData(null, "x", false)]
  [InlineData("", "", true)]
  public void AreEquals_ShouldTreatNullAsEmpty(string? value, string comparer, bool esperado)
  {
    // Arrange
    var validation = new Validation();

    // Act
    validation.AreEquals(value!, comparer, "TestProperty");

    // Assert
    Assert.Equal(esperado, validation.IsValid);
  }

  // Testa se o mesmo vale para as demais comparações textuais, sem lançar para valor ausente
  [Fact]
  public void TextComparisons_ShouldNotThrow_WhenValueIsNull()
  {
    // Arrange & Act & Assert
    Assert.False(new Validation().AreNotEquals(null!, "", "P").IsValid);
    Assert.False(new Validation().Contains(null!, "x", "P").IsValid);
    Assert.True(new Validation().NotContains(null!, "x", "P").IsValid);
  }

  #endregion
}
