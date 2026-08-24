using System.Text.Json;
using Tooark.Extensions;
using Tooark.Utils;

namespace Tooark.Tests.Extensions;

/// <summary>
/// Testes de cobertura das traduções e do comportamento do localizador entre idiomas.
/// </summary>
[Collection("CultureSensitive")]
public class JsonStringLocalizerCoverageTests
{
  // Idiomas distribuídos com o pacote
  private static readonly string[] Idiomas = ["en-US", "pt-BR", "es-ES"];

  // Constrói o localizador
  private static JsonStringLocalizerExtension Localizador() => new();

  // Lê o arquivo de recurso do idioma embutido no assembly, que é o que o consumidor recebe
  private static Dictionary<string, string> Recurso(string idioma)
  {
    using var conteudo = RecursoEmbutido(idioma)
      ?? throw new InvalidOperationException($"O idioma {idioma} não está embutido no assembly.");

    using var leitor = new StreamReader(conteudo);
    using var documento = JsonDocument.Parse(leitor.ReadToEnd());

    return documento.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
  }

  // Abre o recurso embutido do idioma, ou devolve nulo quando ele não é distribuído
  private static Stream? RecursoEmbutido(string idioma)
  {
    var assembly = typeof(JsonStringLocalizerExtension).Assembly;

    return assembly.GetManifestResourceStream(
      $"{assembly.GetName().Name}.Resources.{idioma}.default.json");
  }

  public JsonStringLocalizerCoverageTests()
  {
    // Define a instância da linguagem como a implementação padrão
    Language.Instance = new Language.LanguageImplementation();
  }

  // Testa se os três idiomas distribuídos têm exatamente o mesmo conjunto de chaves
  [Fact]
  public void Resources_ShouldHaveTheSameKeysInEveryLanguage()
  {
    // Arrange
    var referencia = Recurso("en-US").Keys.OrderBy(k => k).ToList();

    // Act & Assert
    foreach (var idioma in Idiomas)
    {
      Assert.Equal(referencia, Recurso(idioma).Keys.OrderBy(k => k).ToList());
    }
  }

  // Testa se o pt-PT deixou de ser distribuído
  [Fact]
  public void Resources_ShouldNotShipRemovedLanguages()
  {
    // Act & Assert
    Assert.Null(RecursoEmbutido("pt-PT"));
  }

  // Testa se nenhum idioma tem texto vazio
  [Fact]
  public void Resources_ShouldNotHaveEmptyText()
  {
    // Act & Assert
    foreach (var idioma in Idiomas)
    {
      var vazias = Recurso(idioma).Where(par => string.IsNullOrWhiteSpace(par.Value)).Select(par => par.Key);

      Assert.Empty(vazias);
    }
  }

  // Testa se os marcadores de parâmetro são os mesmos nos três idiomas
  [Fact]
  public void Resources_ShouldUseTheSamePlaceholdersInEveryLanguage()
  {
    // Arrange
    var referencia = Recurso("en-US");
    var divergentes = new List<string>();

    // Act
    foreach (var idioma in Idiomas.Where(i => i != "en-US"))
    {
      foreach (var (chave, texto) in Recurso(idioma))
      {
        // Conta os marcadores {0}, {1} e {2} de cada texto
        for (int indice = 0; indice < 3; indice++)
        {
          var marcador = $"{{{indice}}}";

          if (referencia[chave].Contains(marcador) != texto.Contains(marcador))
          {
            divergentes.Add($"{idioma}:{chave}:{marcador}");
          }
        }
      }
    }

    // Assert
    Assert.Empty(divergentes);
  }

  // Testa se as chaves emitidas pelos pacotes têm tradução nos três idiomas
  [Theory]
  [InlineData("Field.Required;Email")]
  [InlineData("Field.Invalid;Documento")]
  [InlineData("Validation.IsNotNullOrEmpty;Nome")]
  [InlineData("Validation.IsBetween;Idade;18;65")]
  [InlineData("Options.Jwt.SecretTooShort;32")]
  [InlineData("Handler.NotFound;MinhaRequisicao")]
  [InlineData("Attributes.DocumentTypeUnknown;CPFF")]
  [InlineData("Attributes.LinkVideoNoProvider;Video")]
  [InlineData("File.SizeExceeded;5242880")]
  [InlineData("Exceptions.MessageNullEmpty")]
  [InlineData("Notifications.NotificationNull")]
  [InlineData("UnitOfWork.StrategyNotSupported;Desconhecida")]
  [InlineData("Cryptography.InvalidCipherText")]
  [InlineData("Options.Otlp.Endpoint.Invalid")]
  [InlineData("Invalid.Parameter;null")]
  [InlineData("Record.Deleted")]
  public void LocalizedString_ShouldBeTranslatedInEveryLanguage(string chave)
  {
    // Act & Assert
    foreach (var idioma in Idiomas)
    {
      Language.SetCulture(idioma);
      var resultado = Localizador()[chave];

      // A chave existe, então não pode ser reportada como ausente nem devolver a si mesma
      Assert.False(resultado.ResourceNotFound, $"{idioma}: {chave}");
      Assert.NotEqual(chave, resultado.Value);
      Assert.DoesNotContain("{0}", resultado.Value, StringComparison.Ordinal);
    }
  }

  // Testa se o idioma sem arquivo próprio não fixa os parâmetros da primeira consulta
  [Fact]
  public void LocalizedString_FallbackShouldNotCacheFormattedText()
  {
    // Arrange
    Language.SetCulture("ja-JP");
    var localizador = Localizador();

    // Act
    var primeiro = localizador["Field.Required;Email"].Value;
    var segundo = localizador["Field.Required;Nome"].Value;
    var terceiro = localizador["Field.Required;Senha"].Value;

    // Assert
    Assert.Contains("Email", primeiro, StringComparison.Ordinal);
    Assert.Contains("Nome", segundo, StringComparison.Ordinal);
    Assert.Contains("Senha", terceiro, StringComparison.Ordinal);
  }

  // Testa se o mesmo vale para instâncias diferentes, já que as traduções são compartilhadas
  [Fact]
  public void LocalizedString_FallbackShouldNotLeakBetweenInstances()
  {
    // Arrange
    Language.SetCulture("ko-KR");

    // Act
    var primeiro = new JsonStringLocalizerExtension()["Field.Invalid;Email"].Value;
    var segundo = new JsonStringLocalizerExtension()["Field.Invalid;Cpf"].Value;

    // Assert
    Assert.Contains("Email", primeiro, StringComparison.Ordinal);
    Assert.Contains("Cpf", segundo, StringComparison.Ordinal);
  }

  // Testa se a chave inexistente é sinalizada, mantendo os parâmetros no nome
  [Fact]
  public void LocalizedString_ShouldFlagMissingKey()
  {
    // Arrange
    Language.SetCulture("pt-BR");

    // Act
    var resultado = Localizador()["ChaveInexistente;Parametro"];

    // Assert
    Assert.True(resultado.ResourceNotFound);
    Assert.Equal("ChaveInexistente;Parametro", resultado.Value);
    Assert.Equal("ChaveInexistente;Parametro", resultado.Name);
  }

  // Testa se um texto que não é chave do Tooark chega inteiro, sem truncar no ponto e vírgula
  [Fact]
  public void LocalizedString_ShouldNotTruncateForeignText()
  {
    // Arrange: mensagem no formato que o model binding do ASP.NET Core produz
    Language.SetCulture("pt-BR");
    var mensagem = "The value 'a;b' is not valid for Idade.";

    // Act
    var resultado = Localizador()[mensagem];

    // Assert
    Assert.True(resultado.ResourceNotFound);
    Assert.Equal(mensagem, resultado.Value);
  }

  // Testa se a chave existente não é sinalizada como ausente
  [Fact]
  public void LocalizedString_ShouldNotFlagExistingKey()
  {
    // Arrange
    Language.SetCulture("pt-BR");

    // Act
    var resultado = Localizador()["NotFound"];

    // Assert
    Assert.False(resultado.ResourceNotFound);
    Assert.Equal("Não encontrado", resultado.Value);
  }

  // Testa se o idioma removido passou a cair na cultura padrão
  [Fact]
  public void LocalizedString_RemovedLanguageShouldFallBackToDefault()
  {
    // Arrange
    Language.SetCulture("pt-PT");

    // Act
    var resultado = Localizador()["NotFound"];

    // Assert
    Assert.Equal("Not found", resultado.Value);
    Assert.False(resultado.ResourceNotFound);
  }
}
