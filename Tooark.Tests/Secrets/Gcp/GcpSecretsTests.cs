using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tooark.Exceptions;
using Tooark.Secrets.Gcp;
using Tooark.Secrets.Gcp.Clients;
using Tooark.Secrets.Gcp.Configuration;
using Tooark.Secrets.Gcp.Injections;
using Tooark.Secrets.Gcp.Options;
using Tooark.Secrets.Interfaces;

namespace Tooark.Tests.Secrets.Gcp;

/// <summary>
/// Testes dos segredos no Google Cloud: fonte de configuração, serviço e registro.
/// </summary>
public class GcpSecretsTests
{
  // Projeto dos testes
  private const string Projeto = "tooark-testes";

  // Enumera os identificadores informados de forma assíncrona, como o SDK
  private static async IAsyncEnumerable<string> Ids(params string[] ids)
  {
    foreach (var id in ids)
    {
      await Task.Yield();
      yield return id;
    }
  }

  // Cliente com os segredos e os parâmetros informados; valor nulo simula um item sem versão habilitada
  private static Mock<IGcpSecretsClient> Cliente(Dictionary<string, string?>? segredos = null, Dictionary<string, string?>? parametros = null)
  {
    segredos ??= [];
    parametros ??= [];
    var cliente = new Mock<IGcpSecretsClient>();

    cliente.Setup(c => c.ListSecretIdsAsync(Projeto, It.IsAny<CancellationToken>())).Returns(() => Ids([.. segredos.Keys]));
    cliente.Setup(c => c.AccessLatestSecretAsync(Projeto, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((string _, string id, CancellationToken _) => segredos.GetValueOrDefault(id));
    cliente.Setup(c => c.ListParameterIdsAsync(Projeto, It.IsAny<CancellationToken>())).Returns(() => Ids([.. parametros.Keys]));
    cliente.Setup(c => c.RenderLatestParameterAsync(Projeto, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((string _, string id, CancellationToken _) => parametros.GetValueOrDefault(id));

    return cliente;
  }

  // Monta a configuração só com a fonte do Google Cloud
  private static IConfigurationRoot Montar(GcpSecretsOptions opcoes, IGcpSecretsClient cliente) =>
    new ConfigurationBuilder().Add(new GcpSecretsConfigurationSource(opcoes, () => cliente)).Build();

  // Testa se os segredos do prefixo viram chaves, um por chave, com o sublinhado duplo como nível e o JSON como texto
  [Fact]
  public void Configuration_ShouldLoadOneSecretPerKey()
  {
    // Arrange
    var cliente = Cliente(
      segredos: new()
      {
        ["arkuest-prod__Storage__CredentialsJson"] = """{"type":"service_account"}""",
        ["arkuest-prod__Jwt__Secret"] = "segredo",
        ["arkuest-production__Jwt__Secret"] = "de outro ambiente",
        ["arkuest-prod__SemVersao"] = null
      },
      parametros: new() { ["arkuest-prod__Jwt__Secret"] = "do-parametro", ["arkuest-prod__Storage__Bucket"] = "arquivos" });
    var opcoes = new GcpSecretsOptions { ProjectId = Projeto, SecretsPrefix = "arkuest-prod", ParametersPrefix = "arkuest-prod" };

    // Act
    var configuracao = Montar(opcoes, cliente.Object);

    // Assert
    Assert.Equal("""{"type":"service_account"}""", configuracao["Storage:CredentialsJson"]);
    Assert.Equal("arquivos", configuracao["Storage:Bucket"]);
    Assert.Equal("segredo", configuracao["Jwt:Secret"]);
    Assert.Null(configuracao["SemVersao"]);
    Assert.DoesNotContain(configuracao.AsEnumerable(), par => par.Value == "de outro ambiente");
  }

  // Testa se um parâmetro JSON na raiz do prefixo vira várias chaves com a expansão ligada
  [Fact]
  public void Configuration_ShouldExpandJsonDocument_WhenEnabled()
  {
    // Arrange
    var cliente = Cliente(parametros: new() { ["arkuest-prod"] = """{"Storage":{"Bucket":"arquivos"},"Jwt":{"Issuer":"tooark"}}""" });
    var opcoes = new GcpSecretsOptions { ProjectId = Projeto, ParametersPrefix = "arkuest-prod", ExpandJson = true };

    // Act
    var configuracao = Montar(opcoes, cliente.Object);

    // Assert
    Assert.Equal("arquivos", configuracao["Storage:Bucket"]);
    Assert.Equal("tooark", configuracao["Jwt:Issuer"]);
  }

  // Testa que, com a expansão ligada, todo valor que é objeto JSON vira chaves, inclusive uma chave de conta de serviço
  [Fact]
  public void Configuration_ShouldExpandEveryJsonObject_WhenEnabled()
  {
    // Arrange
    var cliente = Cliente(segredos: new() { ["arkuest-prod__Storage__CredentialsJson"] = """{"type":"service_account"}""" });
    var opcoes = new GcpSecretsOptions { ProjectId = Projeto, SecretsPrefix = "arkuest-prod", ExpandJson = true };

    // Act
    var configuracao = Montar(opcoes, cliente.Object);

    // Assert
    Assert.Null(configuracao["Storage:CredentialsJson"]);
    Assert.Equal("service_account", configuracao["Storage:CredentialsJson:type"]);
  }

  // Testa se só o que tem prefixo configurado é consultado
  [Fact]
  public void Configuration_ShouldOnlyQueryConfiguredSources()
  {
    // Arrange
    var cliente = Cliente(segredos: new() { ["app__Chave"] = "valor" });

    // Act
    var configuracao = Montar(new GcpSecretsOptions { ProjectId = Projeto, SecretsPrefix = "app" }, cliente.Object);

    // Assert
    Assert.Equal("valor", configuracao["Chave"]);
    cliente.Verify(c => c.ListParameterIdsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  // Testa se a falha do Google Cloud impede a aplicação de subir
  [Fact]
  public void Configuration_ShouldThrow_WhenGoogleFails()
  {
    // Arrange
    var cliente = new Mock<IGcpSecretsClient>();
    cliente.Setup(c => c.ListSecretIdsAsync(Projeto, It.IsAny<CancellationToken>()))
      .Throws(new RpcException(new Status(StatusCode.PermissionDenied, "sem permissão")));

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() =>
      Montar(new GcpSecretsOptions { ProjectId = Projeto, SecretsPrefix = "app" }, cliente.Object));

    // Assert
    Assert.Contains("Secrets.LoadFailed;Google Cloud", ex.GetErrorMessages());
  }

  // Testa as validações da fonte do Google Cloud
  [Theory]
  [InlineData(null, "app", "Options.Secrets.Gcp.ProjectIdNotConfigured")]
  [InlineData(Projeto, null, "Options.Secrets.SourceNotConfigured")]
  public void AddTooarkSecretsGcp_ShouldThrow_WhenOptionsAreInvalid(string? projeto, string? prefixo, string chave)
  {
    // Arrange
    var builder = new ConfigurationBuilder();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => builder.AddTooarkSecretsGcp(opcoes =>
    {
      opcoes.ProjectId = projeto;
      opcoes.SecretsPrefix = prefixo;
    }));

    // Assert
    Assert.Contains(chave, ex.GetErrorMessages());
  }

  // Cria o serviço sobre o cliente informado
  private static GcpSecretService Servico(IGcpSecretsClient cliente) =>
    new(cliente, Microsoft.Extensions.Options.Options.Create(new GcpSecretsOptions { ProjectId = Projeto }));

  // Testa se o serviço lê a versão latest do segredo no projeto das opções
  [Fact]
  public async Task Service_ShouldReadLatestVersion()
  {
    // Arrange
    var cliente = Cliente(segredos: new() { ["tenant-acme"] = "chave-acme" });

    // Act
    var valor = await Servico(cliente.Object).GetAsync("tenant-acme", TestContext.Current.CancellationToken);
    var ausente = await Servico(cliente.Object).GetAsync("tenant-outro", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("chave-acme", valor);
    Assert.Null(ausente);
  }

  // Testa se as falhas do Google Cloud viram as exceções do Tooark
  [Theory]
  [InlineData(StatusCode.PermissionDenied, "Secrets.AccessDenied")]
  [InlineData(StatusCode.Unauthenticated, "Secrets.AccessDenied")]
  [InlineData(StatusCode.Unavailable, "Secrets.OperationFailed")]
  public async Task Service_ShouldMapGoogleErrors(StatusCode status, string chave)
  {
    // Arrange
    var erro = new RpcException(new Status(status, "falha"));
    var cliente = new Mock<IGcpSecretsClient>();
    cliente.Setup(c => c.AccessLatestSecretAsync(Projeto, "app", It.IsAny<CancellationToken>())).ThrowsAsync(erro);

    // Act
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => Servico(cliente.Object).GetAsync("app", TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains(chave, ex.GetErrorMessages());
    Assert.Same(erro, ex.InnerException);
  }

  // Testa se o registro monta o serviço sem criar o cliente, que só é criado no primeiro uso
  [Fact]
  public void AddTooarkSecretsGcp_ShouldRegisterService_WithoutCreatingClient()
  {
    // Arrange
    var configuracao = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?> { ["Secrets:ProjectId"] = Projeto })
      .Build();
    var services = new ServiceCollection();

    // Act
    services.AddTooarkSecretsGcp(configuracao);
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.IsType<GcpSecretService>(provider.GetRequiredService<ISecretService>());
  }

  // Testa se o registro recusa a falta do projeto
  [Fact]
  public void AddTooarkSecretsGcp_ShouldThrow_WhenProjectIsMissing()
  {
    // Arrange & Act
    var ex = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkSecretsGcp(new ConfigurationBuilder().Build()));

    // Assert
    Assert.Contains("Options.Secrets.Gcp.ProjectIdNotConfigured", ex.GetErrorMessages());
  }
}
