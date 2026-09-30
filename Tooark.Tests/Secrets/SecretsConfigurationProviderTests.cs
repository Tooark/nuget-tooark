using Microsoft.Extensions.Configuration;
using Tooark.Exceptions;
using Tooark.Secrets.Configuration;
using Tooark.Secrets.Options;

namespace Tooark.Tests.Secrets;

/// <summary>
/// Testes da fonte de configuração base dos segredos.
/// </summary>
public class SecretsConfigurationProviderTests
{
  // Fonte que devolve as entradas informadas, no lugar de um cofre
  private sealed class FonteFalsa(SecretsOptions options, Func<CancellationToken, Task<IReadOnlyList<SecretEntry>>> carga)
    : SecretsConfigurationProvider(options, "Falso"), IConfigurationSource
  {
    public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

    protected override Task<IReadOnlyList<SecretEntry>> LoadEntriesAsync(CancellationToken cancellationToken) => carga(cancellationToken);

    public static string? Relativo(string name, string prefix) => RelativeName(name, prefix);
  }

  // Monta a configuração só com a fonte falsa
  private static IConfigurationRoot Montar(SecretsOptions? opcoes, params SecretEntry[] entradas) =>
    new ConfigurationBuilder()
      .Add(new FonteFalsa(opcoes ?? new SecretsOptions(), _ => Task.FromResult<IReadOnlyList<SecretEntry>>(entradas)))
      .Build();

  // Testa se barra e sublinhado duplo viram níveis da chave
  [Theory]
  [InlineData("Storage/SecretKey", "Storage:SecretKey")]
  [InlineData("Jwt__Secret", "Jwt:Secret")]
  [InlineData("OpenId/Entra__ClientSecret", "OpenId:Entra:ClientSecret")]
  [InlineData("/Storage//Bucket/", "Storage:Bucket")]
  [InlineData("Observability:Otlp:Headers", "Observability:Otlp:Headers")]
  public void Load_ShouldMapNamesToConfigurationKeys(string nome, string chave)
  {
    // Arrange & Act
    var configuracao = Montar(null, new SecretEntry(nome, "valor"));

    // Assert
    Assert.Equal("valor", configuracao[chave]);
  }

  // Testa se o JSON fica como texto quando a expansão está desligada
  [Fact]
  public void Load_ShouldKeepJsonAsText_ByDefault()
  {
    // Arrange
    const string json = """{"type":"service_account","project_id":"tooark"}""";

    // Act
    var configuracao = Montar(null, new SecretEntry("Storage/CredentialsJson", json));

    // Assert
    Assert.Equal(json, configuracao["Storage:CredentialsJson"]);
  }

  // Testa se o objeto JSON vira chaves filhas quando a expansão está ligada
  [Fact]
  public void Load_ShouldExpandJsonObjects_WhenEnabled()
  {
    // Arrange
    const string json = """{"Jwt":{"Secret":"segredo","ExpirationTime":30},"Storage__Bucket":"arquivos","Ligado":true,"Nulo":null}""";

    // Act
    var configuracao = Montar(new SecretsOptions { ExpandJson = true }, new SecretEntry("app", json));

    // Assert
    Assert.Equal("segredo", configuracao["app:Jwt:Secret"]);
    Assert.Equal("30", configuracao["app:Jwt:ExpirationTime"]);
    Assert.Equal("arquivos", configuracao["app:Storage:Bucket"]);
    Assert.Equal("true", configuracao["app:Ligado"]);
    Assert.Null(configuracao["app:Nulo"]);
  }

  // Testa se o segredo na raiz do prefixo expande direto nas chaves de primeiro nível
  [Fact]
  public void Load_ShouldExpandRootEntry()
  {
    // Arrange & Act
    var configuracao = Montar(new SecretsOptions { ExpandJson = true }, new SecretEntry(string.Empty, """{"Jwt":{"Secret":"s"}}"""));

    // Assert
    Assert.Equal("s", configuracao["Jwt:Secret"]);
  }

  // Testa se o texto dentro do objeto não é lido de novo como JSON
  [Fact]
  public void Load_ShouldNotReparseStringMembers()
  {
    // Arrange
    var chave = """{"type":"service_account"}""";
    var json = System.Text.Json.JsonSerializer.Serialize(new { Storage = new { CredentialsJson = chave } });

    // Act
    var configuracao = Montar(new SecretsOptions { ExpandJson = true }, new SecretEntry(string.Empty, json));

    // Assert
    Assert.Equal(chave, configuracao["Storage:CredentialsJson"]);
  }

  // Testa se o que não é objeto JSON fica como texto mesmo com a expansão ligada
  [Theory]
  [InlineData("[1,2,3]")]
  [InlineData("{inválido")]
  [InlineData("texto simples")]
  public void Load_ShouldKeepNonObjectsAsText_WhenExpansionIsEnabled(string valor)
  {
    // Arrange & Act
    var configuracao = Montar(new SecretsOptions { ExpandJson = true }, new SecretEntry("Valor", valor));

    // Assert
    Assert.Equal(valor, configuracao["Valor"]);
  }

  // Testa se o valor na raiz sem objeto para expandir é ignorado
  [Fact]
  public void Load_ShouldIgnoreRootEntry_WhenNotExpanded()
  {
    // Arrange & Act
    var configuracao = Montar(null, new SecretEntry(string.Empty, "sem chave"));

    // Assert
    Assert.Empty(configuracao.AsEnumerable());
  }

  // Testa se uma lista expandida é lida na ordem certa, inclusive com mais de dez itens
  [Fact]
  public void Load_ShouldKeepArrayOrder_WhenBinding()
  {
    // Arrange
    var itens = Enumerable.Range(0, 12).Select(i => $"item{i}").ToArray();
    var json = $$"""{"Lista":{{System.Text.Json.JsonSerializer.Serialize(itens)}}}""";

    // Act
    var configuracao = Montar(new SecretsOptions { ExpandJson = true }, new SecretEntry(string.Empty, json));

    // Assert
    Assert.Equal(itens, configuracao.GetSection("Lista").Get<string[]>());
  }

  // Testa se o cofre, adicionado depois, vale por cima das outras fontes
  [Fact]
  public void Load_ShouldOverrideEarlierSources()
  {
    // Arrange
    var builder = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = "local", ["Jwt:Issuer"] = "tooark" });

    // Act
    var configuracao = builder
      .Add(new FonteFalsa(new SecretsOptions(), _ => Task.FromResult<IReadOnlyList<SecretEntry>>([new("Jwt/Secret", "cofre")])))
      .Build();

    // Assert
    Assert.Equal("cofre", configuracao["Jwt:Secret"]);
    Assert.Equal("tooark", configuracao["Jwt:Issuer"]);
  }

  // Testa se a falha do cofre impede a aplicação de subir, com a chave de erro e a exceção original
  [Fact]
  public void Load_ShouldThrow_WhenVaultFails()
  {
    // Arrange
    var erro = new InvalidOperationException("cofre fora do ar");
    var builder = new ConfigurationBuilder().Add(new FonteFalsa(new SecretsOptions(), _ => throw erro));

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => builder.Build());

    // Assert
    Assert.Contains("Secrets.LoadFailed;Falso", ex.GetErrorMessages());
    Assert.Same(erro, ex.InnerException);
  }

  // Testa se a fonte opcional ignora a falha e sobe sem as chaves do cofre
  [Fact]
  public void Load_ShouldBeEmpty_WhenOptionalSourceFails()
  {
    // Arrange
    var builder = new ConfigurationBuilder().Add(new FonteFalsa(new SecretsOptions { Optional = true }, _ => throw new TimeoutException()));

    // Act
    var configuracao = builder.Build();

    // Assert
    Assert.Empty(configuracao.AsEnumerable());
  }

  // Testa se o cofre que não responde no tempo limite falha a carga
  [Fact]
  public void Load_ShouldThrow_WhenTimeoutExpires()
  {
    // Arrange
    var builder = new ConfigurationBuilder().Add(new FonteFalsa(
      new SecretsOptions { TimeoutSeconds = 1 },
      async cancellationToken =>
      {
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        return [];
      }
    ));

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => builder.Build());

    // Assert
    Assert.Contains("Secrets.LoadFailed;Falso", ex.GetErrorMessages());
  }

  // Testa o limite de nível do prefixo
  [Theory]
  [InlineData("arkuest/prod", "arkuest/prod", "")]
  [InlineData("arkuest/prod/Jwt/Secret", "arkuest/prod", "Jwt/Secret")]
  [InlineData("arkuest/prod__Jwt__Secret", "arkuest/prod", "Jwt__Secret")]
  [InlineData("arkuest/prod/Jwt", "arkuest/prod/", "Jwt")]
  [InlineData("arkuest-prod__Jwt", "arkuest-prod__", "Jwt")]
  [InlineData("/arkuest/prod/Jwt", "/", "arkuest/prod/Jwt")]
  [InlineData("arkuest/production/Jwt", "arkuest/prod", null)]
  [InlineData("arkuest/prodJwt", "arkuest/prod", null)]
  [InlineData("outro/prod/Jwt", "arkuest/prod", null)]
  public void RelativeName_ShouldRespectPrefixBoundary(string nome, string prefixo, string? esperado)
  {
    // Arrange & Act
    var resultado = FonteFalsa.Relativo(nome, prefixo);

    // Assert
    Assert.Equal(esperado, resultado);
  }
}
