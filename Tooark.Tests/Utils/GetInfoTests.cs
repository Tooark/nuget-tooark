using System.Globalization;
using Tooark.Exceptions;
using Tooark.Tests.Moq.Entities.Language;
using Tooark.Utils;

namespace Tooark.Tests.Utils;

[Collection("CultureSensitive")]
public class GetInfoTests
{
  // Lista de idiomas para testes
  private readonly List<MLanguage> _testItems =
  [
    new()
    {
      LanguageCode = "en-US",
      Name = "Name in English",
      Title = "Title in English",
      Description = "Description in English",
      Keywords = "Keywords in English",
      Other = "Other in en-US"
    },
    new()
    {
      LanguageCode = "es-ES",
      Name = "Nombre en Español",
      Title = "Título en Español",
      Description = "Descripción en Español",
      Keywords = "Palabras clave en Español", 
      Other = "Otros en es-ES"
    },
    new()
    {
      LanguageCode = "fr-FR",
      Name = "Nom en Français",
      Title = "Titre en Français",
      Description = "Description en Français",
      Keywords = "Mots-clés en Français",
      Other = "Autre en fr-FR"
    },
    new()
    {
      LanguageCode = "jp-JP",
      Name = "日本語での名前",
      Title = "日本語タイトル",
      Description = "日本語での説明",
      Keywords = "日本語のキーワード",
      Other = "その他 日本語"
    },
    new()
    {
      LanguageCode = "pt-BR",
      Name = "Nome em Português",
      Title = "Titulo em Português",
      Description = "Descrição em Português",
      Keywords = "Palavra-Chave em Português",
      Other = "Outro em pt-BR"
     }
  ];

  // Testa se o método Name retorna o nome no idioma correspondente ao idioma do parâmetro
  [Theory]
  [InlineData("en-US", "Name in English")]
  [InlineData("es-ES", "Nombre en Español")]
  [InlineData("fr-FR", "Nom en Français")]
  [InlineData("jp-JP", "日本語での名前")]
  [InlineData("pt-BR", "Nome em Português")]
  public void Name_ShouldReturnName_BasedOnLanguageCode(string languageCode, string expectedName)
  {
    // Arrange & Act
    var result = GetInfo.Name(_testItems, languageCode);

    // Assert
    Assert.Equal(expectedName, result);
  }

  // Testa se o método Title retorna o titulo no idioma correspondente ao idioma do parâmetro
  [Theory]
  [InlineData("en-US", "Title in English")]
  [InlineData("es-ES", "Título en Español")]
  [InlineData("fr-FR", "Titre en Français")]
  [InlineData("jp-JP", "日本語タイトル")]
  [InlineData("pt-BR", "Titulo em Português")]
  public void Title_ShouldReturnTitle_BasedOnLanguageCode(string languageCode, string expectedName)
  {
    // Arrange & Act
    var result = GetInfo.Title(_testItems, languageCode);

    // Assert
    Assert.Equal(expectedName, result);
  }

  // Testa se o método Description retorna o descrição no idioma correspondente ao idioma do parâmetro
  [Theory]
  [InlineData("en-US", "Description in English")]
  [InlineData("es-ES", "Descripción en Español")]
  [InlineData("fr-FR", "Description en Français")]
  [InlineData("jp-JP", "日本語での説明")]
  [InlineData("pt-BR", "Descrição em Português")]
  public void Description_ShouldReturnDescription_BasedOnLanguageCode(string languageCode, string expectedName)
  {
    // Arrange & Act
    var result = GetInfo.Description(_testItems, languageCode);

    // Assert
    Assert.Equal(expectedName, result);
  }

  // Testa se o método Keywords retorna as palavras-chave no idioma correspondente ao idioma do parâmetro
  [Theory]
  [InlineData("en-US", "Keywords in English")]
  [InlineData("es-ES", "Palabras clave en Español")]
  [InlineData("fr-FR", "Mots-clés en Français")]
  [InlineData("jp-JP", "日本語のキーワード")]
  [InlineData("pt-BR", "Palavra-Chave em Português")]
  public void Keywords_ShouldReturnKeywords_BasedOnLanguageCode(string languageCode, string expectedName)
  {
    // Arrange & Act
    var result = GetInfo.Keywords(_testItems, languageCode);

    // Assert
    Assert.Equal(expectedName, result);
  }

  // Testa se o método Custom retorna a propriedade correspondente ao idioma do parâmetro
  [Theory]
  [InlineData("en-US", "Other in en-US")]
  [InlineData("es-ES", "Otros en es-ES")]
  [InlineData("fr-FR", "Autre en fr-FR")]
  [InlineData("jp-JP", "その他 日本語")]
  [InlineData("pt-BR", "Outro em pt-BR")]
  public void Custom_ShouldReturnProperty_BasedOnLanguageCode(string languageCode, string expectedName)
  {
    // Arrange & Act
    var languageCodeResult = GetInfo.Custom(_testItems, "Other", languageCode);

    // Assert
    Assert.Equal(expectedName, languageCodeResult);
  }

  // Testa se o método Name retorna o nome no idioma correspondente ao cultura definida
  [Theory]
  [InlineData("en-US", "Name in English")]
  [InlineData("es-ES", "Nombre en Español")]
  [InlineData("fr-FR", "Nom en Français")]
  [InlineData("jp-JP", "日本語での名前")]
  [InlineData("pt-BR", "Nome em Português")]
  public void Name_ShouldReturnName_SetCurrentLanguageCode(string languageCode, string expectedName)
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture(languageCode);

    try
    {
      // Act
      var result = GetInfo.Name(_testItems);

      // Assert
      Assert.Equal(expectedName, result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Title retorna o titulo no idioma correspondente ao cultura definida
  [Theory]
  [InlineData("en-US", "Title in English")]
  [InlineData("es-ES", "Título en Español")]
  [InlineData("fr-FR", "Titre en Français")]
  [InlineData("jp-JP", "日本語タイトル")]
  [InlineData("pt-BR", "Titulo em Português")]
  public void Title_ShouldReturnTitle_SetCurrentLanguageCode(string languageCode, string expectedName)
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture(languageCode);

    try
    {
      // Act
      var result = GetInfo.Title(_testItems);

      // Assert
      Assert.Equal(expectedName, result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Description retorna o descrição no idioma correspondente ao cultura definida
  [Theory]
  [InlineData("en-US", "Description in English")]
  [InlineData("es-ES", "Descripción en Español")]
  [InlineData("fr-FR", "Description en Français")]
  [InlineData("jp-JP", "日本語での説明")]
  [InlineData("pt-BR", "Descrição em Português")]
  public void Description_ShouldReturnDescription_SetCurrentLanguageCode(string languageCode, string expectedName)
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture(languageCode);

    try
    {
      // Act
      var result = GetInfo.Description(_testItems);

      // Assert
      Assert.Equal(expectedName, result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Keywords retorna as palavras-chave no idioma correspondente ao cultura definida
  [Theory]
  [InlineData("en-US", "Keywords in English")]
  [InlineData("es-ES", "Palabras clave en Español")]
  [InlineData("fr-FR", "Mots-clés en Français")]
  [InlineData("jp-JP", "日本語のキーワード")]
  [InlineData("pt-BR", "Palavra-Chave em Português")]
  public void Keywords_ShouldReturnKeywords_SetCurrentLanguageCode(string languageCode, string expectedName)
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture(languageCode);

    try
    {
      // Act
      var result = GetInfo.Keywords(_testItems);

      // Assert
      Assert.Equal(expectedName, result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Custom retorna a propriedade correspondente ao cultura definida
  [Theory]
  [InlineData("en-US", "Other in en-US")]
  [InlineData("es-ES", "Otros en es-ES")]
  [InlineData("fr-FR", "Autre en fr-FR")]
  [InlineData("jp-JP", "その他 日本語")]
  [InlineData("pt-BR", "Outro em pt-BR")]
  public void Custom_ShouldReturnProperty_SetCurrentLanguageCode(string languageCode, string expectedName)
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture(languageCode);

    try
    {
      // Act
      var result = GetInfo.Custom(_testItems, "Other");

      // Assert
      Assert.Equal(expectedName, result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Name retorna o nome no idioma padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Name_ShouldReturnNameDefault_WhenParameterNotInList()
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture("de-DE");

    try
    {
      // Act
      var result = GetInfo.Name(_testItems);

      // Assert
      Assert.Equal("Name in English", result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Title retorna o nome no idioma padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Title_ShouldReturnTitleDefault_WhenParameterNotInList()
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture("de-DE");

    try
    {
      // Act
      var result = GetInfo.Title(_testItems);

      // Assert
      Assert.Equal("Title in English", result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Description retorna o nome no idioma padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Description_ShouldReturnDescriptionDefault_WhenParameterNotInList()
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture("de-DE");

    try
    {
      // Act
      var result = GetInfo.Description(_testItems);

      // Assert
      Assert.Equal("Description in English", result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Keywords retorna as palavras-chave no padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Keywords_ShouldReturnKeywordsDefault_WhenParameterNotInList()
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture("de-DE");

    try
    {
      // Act
      var result = GetInfo.Keywords(_testItems);

      // Assert
      Assert.Equal("Keywords in English", result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Custom retorna o nome no idioma padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Custom_ShouldReturnNameDefault_WhenParameterNotInList()
  {
    // Arrange
    var originalCulture = CultureInfo.CurrentCulture;
    Language.SetCulture("de-DE");

    try
    {
      // Act
      var result = GetInfo.Custom(_testItems, "Other");

      // Assert
      Assert.Equal("Other in en-US", result);
    }
    finally
    {
      // Cleanup
      Language.SetCulture(originalCulture);
    }
  }

  // Testa se o método Name retorna o nome no idioma padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Name_ShouldReturnNameDefault_WhenCultureInList()
  {
    // Arrange & Act
    var result = GetInfo.Name(_testItems, "de-DE");

    // Assert
    Assert.Equal("Name in English", result);
  }

  // Testa se o método Title retorna o nome no idioma padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Title_ShouldReturnTitleDefault_WhenCultureInList()
  {
    // Arrange & Act
    var result = GetInfo.Title(_testItems, "de-DE");

    // Assert
    Assert.Equal("Title in English", result);
  }

  // Testa se o método Description retorna o nome no idioma padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Description_ShouldReturnDescriptionDefault_WhenCultureInList()
  {
    // Arrange & Act
    var result = GetInfo.Description(_testItems, "de-DE");

    // Assert
    Assert.Equal("Description in English", result);
  }

  // Testa se o método Keywords retorna as palavras-chave no idioma padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Keywords_ShouldReturnKeywordsDefault_WhenCultureInList()
  {
    // Arrange & Act
    var result = GetInfo.Keywords(_testItems, "de-DE");

    // Assert
    Assert.Equal("Keywords in English", result);
  }

  // Testa se o método Custom retorna o nome no idioma padrão (en-US) idioma do parâmetro não está na lista
  [Fact]
  public void Custom_ShouldReturnNameDefault_WhenCultureInList()
  {
    // Arrange & Act
    var result = GetInfo.Custom(_testItems, "Other", "de-DE");

    // Assert
    Assert.Equal("Other in en-US", result);
  }

  // Testa se o método Name retorna uma string vazia quando a lista de linguagens está vazia
  [Fact]
  public void Name_ReturnsEmptyStringWhenListIsEmpty()
  {
    // Arrange
    var list = new List<MLanguage>();

    // Act
    var result = GetInfo.Name(list);

    // Assert
    Assert.Equal(string.Empty, result);
  }

  // Testa se o método Title retorna uma string vazia quando a lista de linguagens está vazia
  [Fact]
  public void Title_ReturnsEmptyStringWhenListIsEmpty()
  {
    // Arrange
    var list = new List<MLanguage>();

    // Act
    var result = GetInfo.Title(list);

    // Assert
    Assert.Equal(string.Empty, result);
  }

  // Testa se o método Description retorna uma string vazia quando a lista de linguagens está vazia
  [Fact]
  public void Description_ReturnsEmptyStringWhenListIsEmpty()
  {
    // Arrange
    var list = new List<MLanguage>();

    // Act
    var result = GetInfo.Description(list);

    // Assert
    Assert.Equal(string.Empty, result);
  }

  // Testa se o método Keywords retorna uma string vazia quando a lista de linguagens está vazia
  [Fact]
  public void Keywords_ReturnsEmptyStringWhenListIsEmpty()
  {
    // Arrange
    var list = new List<MLanguage>();

    // Act
    var result = GetInfo.Keywords(list);

    // Assert
    Assert.Equal(string.Empty, result);
  }

  // Testa se o método Custom retorna uma string vazia quando a lista de linguagens está vazia
  [Fact]
  public void Custom_ReturnsEmptyStringWhenListIsEmpty()
  {
    // Arrange
    var list = new List<MLanguage>();

    // Act
    var result = GetInfo.Custom(list, "Name");

    // Assert
    Assert.Equal(string.Empty, result);
  }

  // Testa se o método Custom lança uma exceção quando a propriedade LanguageCode não existe na lista de linguagens
  [Fact]
  public void Custom_ThrowsExceptionWhenLanguageCodePropertyDoesNotExist()
  {
    // Arrange
    List<MLanguageOnlyName> list = [ new() { Name = "Name in English" } ];

    // Act
    var exception = Assert.Throws<GetInfoException>(() => GetInfo.Custom(list, "Name"));

    // Assert
    Assert.Equal("NotFound.Property;LanguageCode", exception.Message);
  }

  // Testa se o método Custom lança uma exceção quando a propriedade LanguageCode não existe na lista de linguagens
  [Fact]
  public void Custom_ThrowsExceptionWhenLanguageCodePropertyOnlyExist()
  {
    // Arrange
    List<MLanguageOnlyLanguageCode> list = [ new() { LanguageCode = "pt-BR" } ];

    // Act
    var exception = Assert.Throws<GetInfoException>(() => GetInfo.Name(list));

    // Assert
    Assert.Equal("NotFound.Property;Name", exception.Message);
  }

  // Testa se o método Custom lança uma exceção quando a propriedade Name não existe na lista de linguagens
  [Fact]
  public void Custom_ThrowsExceptionWhenNamePropertyDoesNotExist()
  {
    // Act
    var exception = Assert.Throws<GetInfoException>(() => GetInfo.Custom(_testItems, "Person"));

    // Assert
    Assert.Equal("NotFound.Property;Person", exception.Message);
  }

  // Testa se retorna string vazia quando a lista é nula
  [Fact]
  public void Name_ShouldReturnEmpty_WhenListIsNull()
  {
    // Arrange & Act
    var result = GetInfo.Name<MLanguage>(null);

    // Assert
    Assert.Equal(string.Empty, result);
  }

  // Testa se retorna string vazia quando a lista está vazia
  [Fact]
  public void Name_ShouldReturnEmpty_WhenListIsEmpty()
  {
    // Arrange & Act
    var result = GetInfo.Name<MLanguage>([]);

    // Assert
    Assert.Equal(string.Empty, result);
  }

  // Testa se a validação das propriedades acontece mesmo com a lista nula
  [Fact]
  public void Name_ShouldThrow_WhenListIsNullAndPropertyMissing()
  {
    // Arrange & Act & Assert
    var exception = Assert.Throws<GetInfoException>(() => GetInfo.Name<MLanguageOnlyLanguageCode>(null));

    // Assert
    Assert.Equal("NotFound.Property;Name", exception.Message);
  }

  // Testa se o nome da propriedade personalizada é obrigatório
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Custom_ShouldThrow_WhenPropertyNameIsEmpty(string property)
  {
    // Arrange & Act & Assert
    var exception = Assert.Throws<GetInfoException>(() => GetInfo.Custom(_testItems, property));

    // Assert
    Assert.Equal("NotFound.Property;null", exception.Message);
  }

  // Testa se lê propriedades que não são string
  [Fact]
  public void Custom_ShouldReadNonStringProperty()
  {
    // Arrange
    List<MLanguageNonStringProperty> list =
    [
      new() { LanguageCode = "en-US", Name = 10 },
      new() { LanguageCode = "pt-BR", Name = 20 }
    ];

    // Act
    var result = GetInfo.Custom(list, "Name", "pt-BR");

    // Assert
    Assert.Equal("20", result);
  }

  // Testa se retorna string vazia quando o valor da propriedade é nulo
  [Fact]
  public void Custom_ShouldReturnEmpty_WhenPropertyValueIsNull()
  {
    // Arrange
    List<MLanguageNonStringProperty> list =
    [
      new() { LanguageCode = "pt-BR", Name = 1, Title = null }
    ];

    // Act
    var result = GetInfo.Custom(list, "Title", "pt-BR");

    // Assert
    Assert.Equal(string.Empty, result);
  }
}
