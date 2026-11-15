using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Observability.Injections;
using Tooark.Sanitizers.Injections;
using Tooark.Secrets.Aws.Options;
using Tooark.Secrets.Configuration;
using Tooark.Securities.Injections;
using Tooark.Securities.OpenId.Injections;
using Tooark.Tests.Storage.Gcp;
using SecretsAws = Tooark.Secrets.Aws.Injections.TooarkDependencyInjection;
using SecretsGcp = Tooark.Secrets.Gcp.Injections.TooarkDependencyInjection;
using SecretsVault = Tooark.Secrets.Vault.Injections.TooarkDependencyInjection;
using StorageAws = Tooark.Storage.Aws.Injections.TooarkDependencyInjection;
using StorageGcp = Tooark.Storage.Gcp.Injections.TooarkDependencyInjection;

namespace Tooark.Tests.Configuration;

// Configuração estrita ("Tooark": { "StrictConfiguration": true }): cada registro que lê uma seção é exercitado
// com a opção desligada (chave desconhecida ignorada, como sempre), ligada com chave desconhecida (falha no
// registro, com a chave de mensagem) e ligada só com chaves conhecidas (não pode acusar falso positivo).
public class StrictConfigurationTests
{
  #region Helpers

  // Um registro que lê configuração: a seção, as chaves válidas mínimas, a chave desconhecida e o registro em si
  private sealed record Site(
    string Name,
    string Section,
    Dictionary<string, string?> Valid,
    string UnknownPath,
    string UnknownName,
    Action<IServiceCollection, IConfiguration> Register
  );

  // Chave AES-256 de teste em Base64
  private static readonly string AesKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

  // Nomes dos registros, para as teorias: o xunit serializa a linha, e o registro em si traz um delegate
  public static TheoryData<string> SiteNames() => [.. AllSites.Select(site => site.Name)];

  // Localiza o registro pelo nome
  private static Site Find(string name) => AllSites.Single(site => site.Name == name);

  // Todos os registros dos pacotes Tooark que leem uma seção de configuração
  private static readonly Site[] AllSites =
  [
    new Site(
      "Observability", "Observability",
      new()
      {
        ["Observability:Enabled"] = "false",
        ["Observability:ServiceName"] = "tests",
        ["Observability:Tracing:SamplingRatio"] = "0.5",
        ["Observability:ResourceAttributes:deployment.environment"] = "tests"
      },
      "Observability:Tracing:SampligRatio", "SampligRatio",
      (services, configuration) => services.AddTooarkOpenTelemetry(configuration)),

    new Site(
      "Sanitizers", "Sanitizers",
      new() { ["Sanitizers:Url:AllowedSchemes:0"] = "https" },
      "Sanitizers:Urls", "Urls",
      (services, configuration) => services.AddTooarkSanitizers(configuration)),

    new Site(
      "JwtToken", "Jwt",
      new()
      {
        ["Jwt:Algorithm"] = "HS256",
        ["Jwt:Secret"] = "mysupersecretkey1234567890ABCDEF",
        ["Jwt:Issuers:0"] = "tests",
        ["Jwt:EmitStandardClaims"] = "true"
      },
      "Jwt:Secert", "Secert",
      (services, configuration) => services.AddTooarkJwtToken(configuration)),

    new Site(
      "JwtBearer", "Jwt",
      new()
      {
        ["Jwt:Algorithm"] = "HS256",
        ["Jwt:Secret"] = "mysupersecretkey1234567890ABCDEF",
        ["Jwt:Audience"] = "tests"
      },
      "Jwt:Audince", "Audince",
      (services, configuration) => services.AddTooarkJwtBearer(configuration)),

    new Site(
      "Cryptography", "Cryptography",
      new()
      {
        ["Cryptography:Algorithm"] = "GCM",
        ["Cryptography:SecretBase64"] = AesKey,
        ["Cryptography:CurrentKeyId"] = "k1",
        ["Cryptography:Keys:k1"] = AesKey
      },
      "Cryptography:CurrentKey", "CurrentKey",
      (services, configuration) => services.AddTooarkCryptography(configuration)),

    new Site(
      "PasswordHash", "PasswordHash",
      new() { ["PasswordHash:Iterations"] = "600000" },
      "PasswordHash:Iteration", "Iteration",
      (services, configuration) => services.AddTooarkPasswordHash(configuration)),

    new Site(
      "DataProtection", "DataProtection",
      new()
      {
        ["DataProtection:ApplicationName"] = "tests",
        ["DataProtection:RequirePersistentKeyStorage"] = "false"
      },
      "DataProtection:KeyPath", "KeyPath",
      (services, configuration) => services.AddTooarkDataProtection(configuration)),

    new Site(
      "OpenId.Entra", "OpenId:Entra",
      new()
      {
        ["OpenId:Entra:TenantId"] = "11111111-2222-3333-4444-555555555555",
        ["OpenId:Entra:ClientId"] = "client-id",
        ["OpenId:Entra:ClientSecret"] = "client-secret"
      },
      "OpenId:Entra:ClientSecrett", "ClientSecrett",
      (services, configuration) => services.AddTooarkEntraSso(configuration)),

    new Site(
      "Storage.Aws", "Storage",
      new()
      {
        ["Storage:Bucket"] = "tooark",
        ["Storage:Region"] = "sa-east-1",
        ["Storage:AccessKey"] = "AKIAEXEMPLO",
        ["Storage:SecretKey"] = "segredo"
      },
      "Storage:Buckett", "Buckett",
      (services, configuration) => StorageAws.AddTooarkStorageAws(services, configuration)),

    new Site(
      "Storage.Gcp", "Storage",
      new()
      {
        ["Storage:Bucket"] = "tooark",
        ["Storage:CredentialsJson"] = GcpStorageServiceTests.ContaDeServicoFalsa()
      },
      "Storage:Buckett", "Buckett",
      (services, configuration) => StorageGcp.AddTooarkStorageGcp(services, configuration)),

    new Site(
      "Secrets.Aws", "Secrets",
      new() { ["Secrets:Region"] = "sa-east-1", ["Secrets:SecretsPrefix"] = "app" },
      "Secrets:SecretPrefix", "SecretPrefix",
      (services, configuration) => SecretsAws.AddTooarkSecretsAws(services, configuration)),

    new Site(
      "Secrets.Gcp", "Secrets",
      new() { ["Secrets:ProjectId"] = "projeto-de-testes", ["Secrets:SecretsPrefix"] = "app" },
      "Secrets:Project", "Project",
      (services, configuration) => SecretsGcp.AddTooarkSecretsGcp(services, configuration)),

    new Site(
      "Secrets.Vault", "Secrets",
      new() { ["Secrets:Address"] = "https://vault.exemplo.com", ["Secrets:Token"] = "token-raiz" },
      "Secrets:Adress", "Adress",
      (services, configuration) => SecretsVault.AddTooarkSecretsVault(services, configuration)),

    new Site(
      "Secrets.Reader", "Secrets",
      new() { ["Secrets:Region"] = "sa-east-1", ["Secrets:SecretsPrefix"] = "app" },
      "Secrets:SecretPrefix", "SecretPrefix",
      (_, configuration) => SecretsOptionsReader.Read<AwsSecretsOptions>(
        new ConfigurationBuilder().AddConfiguration(configuration), null))
  ];

  // Monta a configuração do registro, com a chave desconhecida e a opção estrita quando pedidas
  private static IConfiguration Build(Site site, bool withUnknownKey, string? strict)
  {
    var values = new Dictionary<string, string?>(site.Valid);

    if (withUnknownKey)
    {
      values[site.UnknownPath] = "valor";
    }

    if (strict is not null)
    {
      values["Tooark:StrictConfiguration"] = strict;
    }

    return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
  }

  #endregion

  // Testa se, sem a opção, uma chave desconhecida continua sendo ignorada: é o comportamento da linha 4.x
  [Theory]
  [MemberData(nameof(SiteNames))]
  public void Register_ShouldIgnoreUnknownKey_WhenStrictConfigurationIsNotSet(string name)
  {
    // Arrange
    var site = Find(name);
    var configuration = Build(site, withUnknownKey: true, strict: null);

    // Act
    var exception = Record.Exception(() => site.Register(new ServiceCollection(), configuration));

    // Assert
    Assert.Null(exception);
  }

  // Testa se a opção informada como false também mantém a chave desconhecida ignorada
  [Theory]
  [MemberData(nameof(SiteNames))]
  public void Register_ShouldIgnoreUnknownKey_WhenStrictConfigurationIsOff(string name)
  {
    // Arrange
    var site = Find(name);
    var configuration = Build(site, withUnknownKey: true, strict: "false");

    // Act
    var exception = Record.Exception(() => site.Register(new ServiceCollection(), configuration));

    // Assert
    Assert.Null(exception);
  }

  // Testa se, com a opção ligada, a chave desconhecida falha no registro, apontando a seção e a chave
  [Theory]
  [MemberData(nameof(SiteNames))]
  public void Register_ShouldFailAtRegistration_WhenStrictConfigurationFindsUnknownKey(string name)
  {
    // Arrange
    var site = Find(name);
    var configuration = Build(site, withUnknownKey: true, strict: "true");

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => site.Register(new ServiceCollection(), configuration));

    // Assert
    Assert.Equal($"Options.Configuration.UnknownKey;{site.Section};{site.UnknownName}", exception.Message);
  }

  // Testa se, com a opção ligada, uma seção só com chaves conhecidas é aceita: nenhum falso positivo
  [Theory]
  [MemberData(nameof(SiteNames))]
  public void Register_ShouldAcceptKnownKeys_WhenStrictConfigurationIsOn(string name)
  {
    // Arrange
    var site = Find(name);
    var configuration = Build(site, withUnknownKey: false, strict: "true");

    // Act
    var exception = Record.Exception(() => site.Register(new ServiceCollection(), configuration));

    // Assert
    Assert.Null(exception);
  }

  // Testa se a opção aceita maiúsculas e espaços em volta do valor
  [Theory]
  [InlineData("True")]
  [InlineData(" TRUE ")]
  public void Register_ShouldReadStrictConfiguration_IgnoringCaseAndSpaces(string value)
  {
    // Arrange
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["Tooark:StrictConfiguration"] = value,
      ["PasswordHash:Iteration"] = "600000"
    }).Build();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkPasswordHash(configuration));

    // Assert
    Assert.Equal("Options.Configuration.UnknownKey;PasswordHash;Iteration", exception.Message);
  }

  // Testa se um valor ilegível na própria opção falha, em vez de valer como desligada
  [Fact]
  public void Register_ShouldFail_WhenStrictConfigurationValueIsNotBoolean()
  {
    // Arrange
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["Tooark:StrictConfiguration"] = "talvez"
    }).Build();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkPasswordHash(configuration));

    // Assert
    Assert.Equal("Options.Configuration.StrictInvalid;talvez", exception.Message);
  }

  // Testa se a mensagem lista todas as chaves desconhecidas da seção
  [Fact]
  public void Register_ShouldListEveryUnknownKey()
  {
    // Arrange
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["Tooark:StrictConfiguration"] = "true",
      ["PasswordHash:Iteration"] = "600000",
      ["PasswordHash:Salt"] = "16"
    }).Build();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkPasswordHash(configuration));

    // Assert
    Assert.StartsWith("Options.Configuration.UnknownKey;PasswordHash;", exception.Message);
    Assert.Contains("Iteration", exception.Message);
    Assert.Contains("Salt", exception.Message);
    Assert.IsType<InvalidOperationException>(exception.InnerException);
  }

  // Testa se, com a opção ligada, o valor inválido de uma propriedade que valida no setter chega como a exceção
  // do Tooark no registro do serviço de token, e não embrulhado pelo binder
  [Fact]
  public void AddTooarkJwtToken_ShouldSurfaceInvalidAlgorithm_WhenStrictConfigurationIsOn()
  {
    // Arrange
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["Tooark:StrictConfiguration"] = "true",
      ["Jwt:Algorithm"] = "HS999"
    }).Build();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkJwtToken(configuration));

    // Assert
    Assert.Equal("Options.Jwt.AlgorithmNotSupported;HS999", exception.Message);
  }

  // Testa se uma chave extra em outra seção, que nenhum pacote Tooark lê, não é afetada pela opção
  [Fact]
  public void Register_ShouldNotLookAtSectionsOfOtherOwners()
  {
    // Arrange
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["Tooark:StrictConfiguration"] = "true",
      ["Logging:LogLevel:Default"] = "Information",
      ["MinhaAplicacao:Qualquer"] = "valor",
      ["PasswordHash:Iterations"] = "600000"
    }).Build();

    // Act
    var exception = Record.Exception(() => new ServiceCollection().AddTooarkPasswordHash(configuration));

    // Assert
    Assert.Null(exception);
  }
}
