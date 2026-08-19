using System.Globalization;
using Tooark.Utils;

namespace Tooark.Tests.Utils;

[Collection("CultureSensitive")]
public class LanguageTests
{
  public LanguageTests()
  {
    // Define a instância da linguagem como a implementação padrão
    Language.Instance = new Language.LanguageImplementation();
  }

  // Testa se o valor padrão da linguagem é "en-US"
  [Fact]
  public void DefaultLanguage_ShouldBeEnUS()
  {
    // Arrange & Act
    var defaultLanguage = Language.Default;

    // Assert
    Assert.Equal("en-US", defaultLanguage);
  }

  // Testa se a cultura atual é alterada corretamente
  [Theory]
  [InlineData("en-US")]
  [InlineData("es-ES")]
  [InlineData("jp-JP")]
  [InlineData("pt-BR")]
  [InlineData("pt-PT")]
  public void SetCulture_ShouldChangeCurrentCulture(string culture)
  {
    // Arrange & Act
    Language.SetCulture(culture);

    // Assert
    Assert.Equal(culture, Language.Current);
    Assert.Equal(culture, Language.CurrentCulture.Name);
  }

  // Testa se a cultura atual é alterada corretamente com um objeto CultureInfo
  [Theory]
  [InlineData("en-US")]
  [InlineData("es-ES")]
  [InlineData("jp-JP")]
  [InlineData("pt-BR")]
  [InlineData("pt-PT")]
  public void SetCulture_WithCultureInfo_SetsCurrentCulture(string newCulture)
  {
    // Arrange
    CultureInfo culture = new(newCulture);

    // Act
    Language.SetCulture(culture);

    // Assert
    Assert.Equal(newCulture, Language.Current);
    Assert.Equal(culture, Language.CurrentCulture);
  }

  // Testa se a cultura atual mantém o valor padrão quando a cultura informada é inválida
  [Theory]
  [InlineData("en")]
  [InlineData("es")]
  [InlineData("jp")]
  [InlineData("pt")]
  [InlineData("ptBR")]
  public void SetCulture_ShouldDefaultCulture_WhenCultureInvalid(string culture)
  {
    // Arrange & Act
    Language.SetCulture(culture);

    // Assert
    Assert.Equal(Language.Default, Language.Current);
    Assert.Equal(Language.Default, Language.CurrentCulture.Name);
  }

  // Testa se a cultura atual aceita o nome sem diferenciar maiúsculas de minúsculas
  [Theory]
  [InlineData("pt-br", "pt-BR")]
  [InlineData("PT-BR", "pt-BR")]
  [InlineData("Es-Es", "es-ES")]
  public void SetCulture_ShouldNormalizeCultureName(string culture, string expected)
  {
    // Arrange & Act
    Language.SetCulture(culture);

    // Assert
    Assert.Equal(expected, Language.Current);
  }

  // Testa se a cultura atual mantém o valor padrão quando o nome da cultura não é informado
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void SetCulture_ShouldDefaultCulture_WhenCultureNullOrEmpty(string? culture)
  {
    // Arrange
    Language.SetCulture("pt-BR");

    // Act
    Language.SetCulture(culture);

    // Assert
    Assert.Equal(Language.Default, Language.Current);
  }

  // Testa se a cultura atual mantém o valor padrão quando a cultura informada é nula
  [Fact]
  public void SetCulture_ShouldDefaultCulture_WhenCultureInfoNull()
  {
    // Arrange
    Language.SetCulture("pt-BR");

    // Act
    Language.SetCulture((CultureInfo?)null);

    // Assert
    Assert.Equal(Language.Default, Language.Current);
  }

  // Testa se a instância não aceita valor nulo
  [Fact]
  public void Instance_ShouldThrow_WhenNull()
  {
    // Arrange & Act & Assert
    Assert.Throws<ArgumentNullException>(() => Language.Instance = null!);
  }

  // Testa se o idioma de um fluxo de execução não vaza para outro
  [Fact]
  public async Task Current_ShouldNotLeakBetweenExecutionFlows()
  {
    // Arrange
    var firstApplied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var secondApplied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    // Act
    var first = Task.Run(async () =>
    {
      // Define a cultura do primeiro fluxo e espera o segundo definir a dele
      Language.SetCulture("pt-BR");
      firstApplied.SetResult();
      await secondApplied.Task;

      // Lê a cultura depois que o outro fluxo já alterou a própria
      return Language.Current;
    });

    var second = Task.Run(async () =>
    {
      // Espera o primeiro fluxo definir a cultura dele antes de definir a sua
      await firstApplied.Task;
      Language.SetCulture("ja-JP");
      secondApplied.SetResult();

      return Language.Current;
    });

    // Assert
    Assert.Equal("pt-BR", await first);
    Assert.Equal("ja-JP", await second);
  }

  // Testa se o idioma atual acompanha a cultura do ambiente de execução
  [Fact]
  public void Current_ShouldFollowCurrentCulture()
  {
    // Arrange
    Language.SetCulture("en-US");

    // Act
    CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

    // Assert
    Assert.Equal("pt-BR", Language.Current);
    Assert.Equal("pt-BR", Language.CurrentCulture.Name);
  }
}
