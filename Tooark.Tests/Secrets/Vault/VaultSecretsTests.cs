using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Secrets.Interfaces;
using Tooark.Secrets.Vault;
using Tooark.Secrets.Vault.Clients;
using Tooark.Secrets.Vault.Injections;
using Tooark.Secrets.Vault.Options;

namespace Tooark.Tests.Secrets.Vault;

/// <summary>
/// Testes dos segredos no HashiCorp Vault e no OpenBao: cliente, fonte de configuração, serviço e registro.
/// </summary>
public class VaultSecretsTests
{
  // Endereço do cofre falso
  private const string Endereco = "http://cofre.local:8200";

  // Ambiente vazio, para as variáveis da máquina não interferirem
  private static string? SemAmbiente(string _) => null;

  // Cofre com os segredos de um ambiente e de outro, fora do caminho
  private static CofreFalso Cofre()
  {
    var cofre = new CofreFalso();
    cofre.Segredos["arkuest/prod"] = new JsonObject { ["Jwt__Secret"] = "segredo-jwt" };
    cofre.Segredos["arkuest/prod/Storage"] = new JsonObject
    {
      ["SecretKey"] = "chave-s3",
      ["CredentialsJson"] = """{"type":"service_account"}""",
      ["Porta"] = 9000
    };
    cofre.Segredos["arkuest/prod/Storage/Extra"] = new JsonObject { ["Bucket"] = "arquivos" };
    cofre.Segredos["arkuest/prod/OpenId/Entra"] = new JsonObject { ["ClientSecret"] = "segredo-entra" };
    cofre.Segredos["arkuest/homolog"] = new JsonObject { ["Jwt__Secret"] = "de outro ambiente" };

    return cofre;
  }

  // Opções com token e o caminho do ambiente de produção
  private static void ComToken(VaultSecretsOptions opcoes)
  {
    opcoes.Address = Endereco;
    opcoes.Token = "token-raiz";
    opcoes.Path = "arkuest/prod";
  }

  // Monta a configuração só com a fonte do cofre falso
  private static IConfigurationRoot Montar(CofreFalso cofre, Action<VaultSecretsOptions> configurar) =>
    TooarkConfiguration.AddTooarkSecretsVault(new ConfigurationBuilder(), configurar, SemAmbiente, cofre).Build();

  // Cria o serviço sobre o cofre falso
  private static VaultSecretService Servico(CofreFalso cofre, Action<VaultSecretsOptions> configurar)
  {
    var opcoes = new VaultSecretsOptions();
    configurar(opcoes);
    opcoes.Validate();

    return new VaultSecretService(new VaultClient(opcoes, cofre), Microsoft.Extensions.Options.Options.Create(opcoes));
  }

  // Testa se o caminho é percorrido e cada campo vira uma chave, com o JSON como texto
  [Fact]
  public void Configuration_ShouldWalkPathAndMapFields()
  {
    // Arrange
    var cofre = Cofre();

    // Act
    var configuracao = Montar(cofre, ComToken);

    // Assert
    Assert.Equal("segredo-jwt", configuracao["Jwt:Secret"]);
    Assert.Equal("chave-s3", configuracao["Storage:SecretKey"]);
    Assert.Equal("""{"type":"service_account"}""", configuracao["Storage:CredentialsJson"]);
    Assert.Equal("9000", configuracao["Storage:Porta"]);
    Assert.Equal("arquivos", configuracao["Storage:Extra:Bucket"]);
    Assert.Equal("segredo-entra", configuracao["OpenId:Entra:ClientSecret"]);
    Assert.DoesNotContain(configuracao.AsEnumerable(), par => par.Value == "de outro ambiente");
  }

  // Testa se o token e o namespace vão em todas as requisições
  [Fact]
  public void Configuration_ShouldSendTokenAndNamespace()
  {
    // Arrange
    var cofre = Cofre();

    // Act
    Montar(cofre, opcoes =>
    {
      ComToken(opcoes);
      opcoes.Namespace = "arkuest";
    });

    // Assert
    Assert.NotEmpty(cofre.Requisicoes);
    Assert.All(cofre.Requisicoes, requisicao =>
    {
      Assert.Equal("token-raiz", requisicao.Token);
      Assert.Equal("arkuest", requisicao.Namespace);
    });
  }

  // Testa se a falha do cofre impede a aplicação de subir
  [Fact]
  public void Configuration_ShouldThrow_WhenVaultFails()
  {
    // Arrange
    var cofre = Cofre();
    cofre.FalhaForcada = HttpStatusCode.ServiceUnavailable;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Montar(cofre, ComToken));

    // Assert
    Assert.Contains("Secrets.LoadFailed;Vault", ex.GetErrorMessages());
  }

  // Testa as validações das opções do cofre
  [Theory]
  [InlineData(null, "Token", "t", "arkuest/prod", "Options.Secrets.Vault.AddressNotConfigured")]
  [InlineData("cofre:8200", "Token", "t", "arkuest/prod", "Options.Secrets.Vault.AddressInvalid;cofre:8200")]
  [InlineData(Endereco, "Ldap", "t", "arkuest/prod", "Options.Secrets.Vault.AuthMethodInvalid;Ldap")]
  [InlineData(Endereco, "Token", null, "arkuest/prod", "Options.Secrets.Vault.TokenNotConfigured")]
  [InlineData(Endereco, "AppRole", null, "arkuest/prod", "Options.Secrets.Vault.AppRoleNotConfigured")]
  [InlineData(Endereco, "Kubernetes", null, "arkuest/prod", "Options.Secrets.Vault.KubernetesRoleNotConfigured")]
  [InlineData(Endereco, "Token", "t", null, "Options.Secrets.SourceNotConfigured")]
  public void AddTooarkSecretsVault_ShouldThrow_WhenOptionsAreInvalid(string? endereco, string metodo, string? token, string? caminho, string chave)
  {
    // Arrange
    void Configurar(VaultSecretsOptions opcoes)
    {
      opcoes.Address = endereco;
      opcoes.AuthMethod = metodo;
      opcoes.Token = token;
      opcoes.Path = caminho;
    }

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Montar(Cofre(), Configurar));

    // Assert
    Assert.Contains(chave, ex.GetErrorMessages());
  }

  // Testa se endereço, token e namespace ausentes vêm das variáveis do Vault ou do OpenBao, sem passar por cima do informado
  [Fact]
  public void ApplyEnvironment_ShouldFillMissingValues()
  {
    // Arrange
    var ambiente = new Dictionary<string, string?>
    {
      ["BAO_ADDR"] = "http://bao.local:8200",
      ["VAULT_TOKEN"] = "token-do-vault",
      ["BAO_TOKEN"] = "token-do-bao",
      ["VAULT_NAMESPACE"] = "ns-do-ambiente"
    };
    var opcoes = new VaultSecretsOptions { Namespace = "ns-informado" };

    // Act
    opcoes.ApplyEnvironment(nome => ambiente.GetValueOrDefault(nome));

    // Assert
    Assert.Equal("http://bao.local:8200", opcoes.Address);
    Assert.Equal("token-do-vault", opcoes.Token);
    Assert.Equal("ns-informado", opcoes.Namespace);
  }

  // Testa se o serviço lê os campos de um segredo como objeto JSON, e um campo dele
  [Fact]
  public async Task Service_ShouldReadSecretAndField()
  {
    // Arrange
    var servico = Servico(Cofre(), ComToken);

    // Act
    var segredo = await servico.GetAsync("arkuest/prod/OpenId/Entra", TestContext.Current.CancellationToken);
    var campo = await servico.GetFieldAsync("arkuest/prod/Storage", "SecretKey", TestContext.Current.CancellationToken);
    var ausente = await servico.GetAsync("arkuest/prod/Inexistente", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("""{"ClientSecret":"segredo-entra"}""", segredo);
    Assert.Equal("chave-s3", campo);
    Assert.Null(ausente);
  }

  // Testa se o AppRole faz login no primeiro uso e de novo quando o token é revogado
  [Fact]
  public async Task Service_ShouldLoginWithAppRole_AndLoginAgainWhenTokenIsRevoked()
  {
    // Arrange
    var cofre = Cofre();
    var servico = Servico(cofre, opcoes =>
    {
      opcoes.Address = Endereco;
      opcoes.AuthMethod = "AppRole";
      opcoes.RoleId = "role-arkuest";
      opcoes.SecretId = "secret-arkuest";
      opcoes.CacheMinutes = 0;
    });

    // Act
    var primeira = await servico.GetFieldAsync("arkuest/prod/Storage", "SecretKey", TestContext.Current.CancellationToken);
    cofre.TokensValidos.Remove("token-login-1");
    var segunda = await servico.GetFieldAsync("arkuest/prod/Storage", "SecretKey", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("chave-s3", primeira);
    Assert.Equal("chave-s3", segunda);
    Assert.Equal(2, cofre.Logins);
  }

  // Testa se o Kubernetes faz login com o token da conta de serviço do pod
  [Fact]
  public async Task Service_ShouldLoginWithKubernetesServiceAccountToken()
  {
    // Arrange
    var arquivo = Path.GetTempFileName();
    await File.WriteAllTextAsync(arquivo, "jwt-do-pod\n", TestContext.Current.CancellationToken);

    try
    {
      var cofre = Cofre();
      var servico = Servico(cofre, opcoes =>
      {
        opcoes.Address = Endereco;
        opcoes.AuthMethod = "kubernetes";
        opcoes.Role = "arkuest";
        opcoes.KubernetesTokenPath = arquivo;
      });

      // Act
      var valor = await servico.GetFieldAsync("arkuest/prod/Storage", "SecretKey", TestContext.Current.CancellationToken);

      // Assert
      Assert.Equal("chave-s3", valor);
      Assert.Equal(1, cofre.Logins);
    }
    finally
    {
      File.Delete(arquivo);
    }
  }

  // Testa as falhas de autenticação e de acesso
  [Theory]
  [InlineData("AppRole", "Secrets.Vault.LoginFailed")]
  [InlineData("Kubernetes", "Secrets.Vault.KubernetesTokenNotFound")]
  [InlineData("Token", "Secrets.AccessDenied")]
  public async Task Service_ShouldThrow_WhenAuthenticationFails(string metodo, string chave)
  {
    // Arrange
    var cofre = Cofre();
    var servico = Servico(cofre, opcoes =>
    {
      opcoes.Address = Endereco;
      opcoes.AuthMethod = metodo;
      opcoes.Token = "token-revogado";
      opcoes.RoleId = "role-arkuest";
      opcoes.SecretId = "secret-errado";
      opcoes.Role = "arkuest";
      opcoes.KubernetesTokenPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.token");
    });

    // Act
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => servico.GetAsync("arkuest/prod", TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), mensagem => mensagem.StartsWith(chave, StringComparison.Ordinal));
    Assert.Equal(0, cofre.Logins);
  }

  // Testa se a falha do cofre vira erro do Tooark
  [Fact]
  public async Task Service_ShouldThrow_WhenVaultFails()
  {
    // Arrange
    var cofre = Cofre();
    cofre.FalhaForcada = HttpStatusCode.InternalServerError;
    var servico = Servico(cofre, ComToken);

    // Act
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => servico.GetAsync("arkuest/prod", TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Secrets.OperationFailed", ex.GetErrorMessages());
  }

  // Testa se o registro monta o serviço com as opções da seção Secrets
  [Fact]
  public void AddTooarkSecretsVault_ShouldRegisterService()
  {
    // Arrange
    var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["Secrets:Address"] = Endereco,
      ["Secrets:Token"] = "token-raiz"
    }).Build();
    var services = new ServiceCollection();

    // Act
    services.AddTooarkSecretsVault(configuracao);
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.IsType<VaultSecretService>(provider.GetRequiredService<ISecretService>());
  }
}
