using Moq;
using Tooark.Extensions;
using Tooark.Utils;

namespace Tooark.Tests.Extensions;

[Collection("CultureSensitive")]
public class JsonStringLocalizerExtensionTests
{
  // Construtor para inicializar as variáveis de teste
  public JsonStringLocalizerExtensionTests()
  {
    // Define a instância da linguagem como a implementação padrão
    Language.Instance = new Language.LanguageImplementation();
  }


  // Teste se this[string name] retorna uma string localizada utilizando apenas a key
  [Fact]
  public void LocalizedString_CurrentCulture_WithStringKeyOnly_ShouldReturnLocalizedString()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    string key = "a";
    string localizedValue = "Olá";

    // Act
    var result = localizer[key];

    // Assert
    Assert.Equal(localizedValue, result.Value);
  }

  // Teste se this[string name] retorna uma string localizada utilizando key e parâmetro
  [Fact]
  public void LocalizedString_CurrentCulture_WithStringKeyAndParam_ShouldReturnLocalizedString()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    string key = "b;Tooark";
    string localizedValue = "Olá, Tooark";

    // Act
    var result = localizer[key];

    // Assert
    Assert.Equal(localizedValue, result.Value);
  }

  // Teste se this[string name] retorna uma string localizada utilizando e múltiplos parâmetros
  [Fact]
  public void LocalizedString_CurrentCulture_WithStringKeyAndMultiParam_ShouldReturnLocalizedString()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    string key = "c;Package;Nuget;Tooark";
    string localizedValue = "Olá, Package Nuget Tooark";

    // Act
    var result = localizer[key];

    // Assert
    Assert.Equal(localizedValue, result.Value);
  }

  // Teste se this[string name] retorna a chave buscada por não encontrar a key utilizando apenas a key
  [Fact]
  public void LocalizedString_CurrentCulture_WithStringKeyNotFound_ShouldReturnKey()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    string key = "custom";
    string expectedValue = key.Split(';')[0];

    // Act
    var result = localizer[key];

    // Assert
    Assert.Equal(expectedValue, result.Value);
  }

  // Teste se this[string name] retorna a chave buscada por não encontrar a key utilizando key e parâmetro
  [Fact]
  public void LocalizedString_CurrentCulture_WithStringKeyNotFoundAndParam_ShouldReturnKey()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    string key = "custom;Tooark";
    string expectedValue = key;

    // Act
    var result = localizer[key];

    // Assert
    Assert.Equal(expectedValue, result.Value);
  }

  // Teste se this[string name] retorna uma string localizada utilizando key e lista de parâmetro
  [Fact]
  public void LocalizedString_CurrentCulture_WithStringKeyAndListParam_ShouldReturnLocalizedString()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    string key = "b";
    string[] parameters = ["Tooark"];
    string localizedValue = "Olá, Tooark";

    // Act
    var result = localizer[key, parameters];

    // Assert
    Assert.Equal(localizedValue, result.Value);
  }

  // Teste se this[string name] retorna uma string localizada utilizando key e lista de múltiplos parâmetros
  [Fact]
  public void LocalizedString_CurrentCulture_WithStringKeyAndListMultiParam_ShouldReturnLocalizedString()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    string key = "c";
    string[] parameters = ["Package", "Nuget", "Tooark"];
    string localizedValue = "Olá, Package Nuget Tooark";

    // Act
    var result = localizer[key, parameters];

    // Assert
    Assert.Equal(localizedValue, result.Value);
  }

  // Teste para retornar todas as strings do arquivo da cultura atual de recursos
  [Fact]
  public void GetAllStrings_CurrentCulture_ShouldReturnAllStrings()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    var expectedName = "a";
    var expectedValue = "Olá";

    // Act
    var result = localizer.GetAllStrings(false).ToList();

    // Assert
    Assert.Contains(result, r =>
      r.Name == expectedName &&
      r.Value == expectedValue);
  }

  // Teste para retornar todas as strings do arquivo de cultura padrão de recursos
  [Fact]
  public void GetAllStrings_DefaultCulture_ShouldReturnAllStrings()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    var expectedName = "z";
    var expectedValue = "Default";

    // Act
    var result = localizer.GetAllStrings(true).ToList();

    // Assert
    Assert.Contains(result, r =>
      r.Name == expectedName &&
      r.Value == expectedValue);
  }

  // Teste se this[string name] retorna uma string localizada utilizando apenas a key da cultura padrão
  [Fact]
  public void LocalizedString_DefaultCulture_WithStringKeyOnly_ShouldReturnLocalizedString()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    string key = "z";
    string localizedValue = "Default";

    // Act
    var result = localizer[key];

    // Assert
    Assert.Equal(localizedValue, result.Value);
  }

  // Teste se this[string name] retorna uma string localizada utilizando key e lista de parâmetro com quantidade de parâmetros diferente do esperado
  [Fact]
  public void LocalizedString_CurrentCulture_WithStringKeyAndWrongCountParam_ShouldReturnKey()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();
    string key = "c";
    string[] parameters = ["Tooark"];
    string expectedValue = "Olá, {0} {1} {2};Tooark";

    // Act
    var result = localizer[key, parameters];

    // Assert
    Assert.Equal(expectedValue, result.Value);
  }

  // Teste se this[string name] com chave vazia ou nula
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  public void LocalizedString_CurrentCulture_WithKeyEmpty_ShouldReturnKey(string? key)
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var localizer = new JsonStringLocalizerExtension();

    // Act
    var result = localizer[key!];

    // Assert
    Assert.Equal(string.Empty, result.Value);
  }

  // Teste se this[string name] com chave vazia ou nula
  [Fact]
  public void LocalizedString_AdditionalNotFound_ShouldReturnKey()
  {
    // Arrange
    Language.SetCulture("es-ES");
    var localizer = new JsonStringLocalizerExtension();
    string key = "custom";

    // Act
    var result = localizer[key];

    // Assert
    Assert.Equal(key, result.Value);
  }
}
