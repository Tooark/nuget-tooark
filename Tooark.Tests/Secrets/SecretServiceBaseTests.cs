using Microsoft.Extensions.Configuration;
using Tooark.Exceptions;
using Tooark.Secrets;
using Tooark.Secrets.Configuration;
using Tooark.Secrets.Options;

namespace Tooark.Tests.Secrets;

/// <summary>
/// Testes do cache, da leitura de campo e das opções comuns dos segredos.
/// </summary>
public class SecretServiceBaseTests
{
  // Relógio controlado pelo teste
  private sealed class RelogioFalso : TimeProvider
  {
    public DateTimeOffset Agora { get; set; } = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Agora;
  }

  // Serviço com segredos em memória que conta as leituras no cofre
  private sealed class ServicoFalso(SecretsOptions options, TimeProvider relogio) : SecretServiceBase(options, relogio)
  {
    public Dictionary<string, string> Segredos { get; } = new();

    public int Leituras { get; private set; }

    protected override Task<string?> ReadAsync(string name, CancellationToken cancellationToken)
    {
      Leituras++;
      return Task.FromResult(Segredos.TryGetValue(name, out var valor) ? valor : null);
    }
  }

  // Cria o serviço com o cache informado
  private static (ServicoFalso Servico, RelogioFalso Relogio) Criar(int cacheMinutos = 5)
  {
    var relogio = new RelogioFalso();
    var servico = new ServicoFalso(new SecretsOptions { CacheMinutes = cacheMinutos }, relogio);
    servico.Segredos["banco"] = "senha";
    servico.Segredos["api"] = """{"Chave":"abc","Porta":8080,"Config":{"A":1},"Nulo":null}""";
    servico.Segredos["lista"] = "[1,2]";

    return (servico, relogio);
  }

  // Testa se a segunda leitura dentro da validade vem do cache
  [Fact]
  public async Task GetAsync_ShouldUseCache_WithinExpiration()
  {
    // Arrange
    var (servico, relogio) = Criar();

    // Act
    var primeira = await servico.GetAsync("banco", TestContext.Current.CancellationToken);
    relogio.Agora = relogio.Agora.AddMinutes(4);
    var segunda = await servico.GetAsync("banco", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("senha", primeira);
    Assert.Equal("senha", segunda);
    Assert.Equal(1, servico.Leituras);
  }

  // Testa se o valor é lido de novo depois da validade, trazendo o segredo rotacionado
  [Fact]
  public async Task GetAsync_ShouldReadAgain_AfterExpiration()
  {
    // Arrange
    var (servico, relogio) = Criar();
    await servico.GetAsync("banco", TestContext.Current.CancellationToken);
    servico.Segredos["banco"] = "nova-senha";

    // Act
    relogio.Agora = relogio.Agora.AddMinutes(5);
    var valor = await servico.GetAsync("banco", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("nova-senha", valor);
    Assert.Equal(2, servico.Leituras);
  }

  // Testa se o cache zerado lê o cofre toda vez
  [Fact]
  public async Task GetAsync_ShouldNotCache_WhenDisabled()
  {
    // Arrange
    var (servico, _) = Criar(cacheMinutos: 0);

    // Act
    await servico.GetAsync("banco", TestContext.Current.CancellationToken);
    await servico.GetAsync("banco", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(2, servico.Leituras);
  }

  // Testa se o segredo inexistente não entra no cache, para ser lido quando for criado
  [Fact]
  public async Task GetAsync_ShouldNotCacheMissingSecrets()
  {
    // Arrange
    var (servico, _) = Criar();

    // Act
    var antes = await servico.GetAsync("novo", TestContext.Current.CancellationToken);
    servico.Segredos["novo"] = "criado";
    var depois = await servico.GetAsync("novo", TestContext.Current.CancellationToken);

    // Assert
    Assert.Null(antes);
    Assert.Equal("criado", depois);
  }

  // Testa se o nome em branco é recusado antes de chegar ao cofre
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("  ")]
  public async Task GetAsync_ShouldThrow_WhenNameIsBlank(string? nome)
  {
    // Arrange
    var (servico, _) = Criar();

    // Act
    var ex = await Assert.ThrowsAsync<BadRequestException>(() => servico.GetAsync(nome!, TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Secrets.NameRequired", ex.GetErrorMessages());
    Assert.Equal(0, servico.Leituras);
  }

  // Testa a leitura de campos de um segredo JSON
  [Theory]
  [InlineData("Chave", "abc")]
  [InlineData("Porta", "8080")]
  [InlineData("Config", """{"A":1}""")]
  [InlineData("Nulo", null)]
  [InlineData("Ausente", null)]
  [InlineData("chave", null)]
  public async Task GetFieldAsync_ShouldReadFields(string campo, string? esperado)
  {
    // Arrange
    var (servico, _) = Criar();

    // Act
    var valor = await servico.GetFieldAsync("api", campo, TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(esperado, valor);
  }

  // Testa se o campo de um segredo inexistente é nulo
  [Fact]
  public async Task GetFieldAsync_ShouldReturnNull_WhenSecretIsMissing()
  {
    // Arrange
    var (servico, _) = Criar();

    // Act
    var valor = await servico.GetFieldAsync("inexistente", "Chave", TestContext.Current.CancellationToken);

    // Assert
    Assert.Null(valor);
  }

  // Testa se ler campo de um segredo que não é objeto JSON é erro
  [Theory]
  [InlineData("banco")]
  [InlineData("lista")]
  public async Task GetFieldAsync_ShouldThrow_WhenSecretIsNotJsonObject(string nome)
  {
    // Arrange
    var (servico, _) = Criar();

    // Act
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => servico.GetFieldAsync(nome, "Chave", TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains($"Secrets.NotJsonObject;{nome}", ex.GetErrorMessages());
  }

  // Testa se o campo em branco é recusado
  [Fact]
  public async Task GetFieldAsync_ShouldThrow_WhenFieldIsBlank()
  {
    // Arrange
    var (servico, _) = Criar();

    // Act
    var ex = await Assert.ThrowsAsync<BadRequestException>(() => servico.GetFieldAsync("api", " ", TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Secrets.FieldRequired", ex.GetErrorMessages());
  }

  // Testa as validações das opções comuns
  [Theory]
  [InlineData(0, 5, "Options.Secrets.TimeoutInvalid")]
  [InlineData(30, -1, "Options.Secrets.CacheMinutesInvalid")]
  public void SecretsOptions_Validate_ShouldThrow_WhenOutOfRange(int timeout, int cache, string chave)
  {
    // Arrange
    var opcoes = new SecretsOptions { TimeoutSeconds = timeout, CacheMinutes = cache };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(opcoes.Validate);

    // Assert
    Assert.Contains(chave, ex.GetErrorMessages());
  }

  // Testa se as opções são lidas da configuração já montada, com o ajuste do código por cima
  [Fact]
  public void SecretsOptionsReader_ShouldReadSectionAndApplyConfigure()
  {
    // Arrange
    var valores = new Dictionary<string, string?> { ["Secrets:ExpandJson"] = "true", ["Secrets:CacheMinutes"] = "10" };
    var gerenciador = new ConfigurationManager();
    gerenciador.AddInMemoryCollection(valores);
    var builder = new ConfigurationBuilder().AddInMemoryCollection(valores);

    // Act
    var doGerenciador = SecretsOptionsReader.Read<SecretsOptions>(gerenciador, opcoes => opcoes.CacheMinutes = 1);
    var doBuilder = SecretsOptionsReader.Read<SecretsOptions>(builder, null);

    // Assert
    Assert.True(doGerenciador.ExpandJson);
    Assert.Equal(1, doGerenciador.CacheMinutes);
    Assert.True(doBuilder.ExpandJson);
    Assert.Equal(10, doBuilder.CacheMinutes);
  }
}
