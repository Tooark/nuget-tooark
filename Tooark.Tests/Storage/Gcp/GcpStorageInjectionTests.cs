using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tooark.Exceptions;
using Tooark.Storage.Enums;
using Tooark.Storage.Gcp;
using Tooark.Storage.Gcp.Injections;
using Tooark.Storage.Interfaces;

namespace Tooark.Tests.Storage.Gcp;

/// <summary>
/// Testes do registro e das opções do storage no Google Cloud.
/// </summary>
public class GcpStorageInjectionTests
{
  // Monta a configuração a partir dos pares informados
  private static IConfiguration Configuracao(Dictionary<string, string?> valores) =>
    new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

  // Testa se o serviço é registrado com a chave da conta de serviço em JSON e assina URLs
  [Fact]
  public async Task AddTooarkStorageGcp_ShouldRegisterService_WithCredentialsJson()
  {
    // Arrange
    var configuracao = Configuracao(new()
    {
      ["Storage:Bucket"] = "tooark",
      ["Storage:CredentialsJson"] = GcpStorageServiceTests.ContaDeServicoFalsa()
    });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkStorageGcp(configuracao);
    using var provider = services.BuildServiceProvider();
    var servico = provider.GetRequiredService<IStorageService>();
    var url = await servico.GetSignedUrlAsync("a.txt", access: ESignedUrlAccess.Write, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.IsType<GcpStorageService>(servico);
    Assert.NotNull(provider.GetRequiredService<StorageClient>());
    Assert.Contains("X-Goog-Signature=", url.Query);
  }

  // Testa se a chave em arquivo também é aceita
  [Fact]
  public void AddTooarkStorageGcp_ShouldRegisterService_WithCredentialsPath()
  {
    // Arrange
    var caminho = Path.GetTempFileName();
    File.WriteAllText(caminho, GcpStorageServiceTests.ContaDeServicoFalsa());

    try
    {
      var services = new ServiceCollection();

      // Act
      services.AddTooarkStorageGcp(Configuracao(new() { ["Storage:Bucket"] = "tooark", ["Storage:CredentialsPath"] = caminho }));
      using var provider = services.BuildServiceProvider();

      // Assert
      Assert.IsType<GcpStorageService>(provider.GetRequiredService<IStorageService>());
    }
    finally
    {
      File.Delete(caminho);
    }
  }

  // Testa se o cliente já registrado pela aplicação é mantido
  [Fact]
  public void AddTooarkStorageGcp_ShouldKeepClientRegisteredByApplication()
  {
    // Arrange
    var proprio = new Mock<StorageClient>().Object;
    var services = new ServiceCollection();
    services.AddSingleton(proprio);

    // Act
    services.AddTooarkStorageGcp(Configuracao(new() { ["Storage:Bucket"] = "tooark" }));
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.Same(proprio, provider.GetRequiredService<StorageClient>());
    Assert.IsType<GcpStorageService>(provider.GetRequiredService<IStorageService>());
  }

  // Testa se as duas formas de credencial juntas falham no registro
  [Fact]
  public void AddTooarkStorageGcp_ShouldThrow_WhenCredentialsAreAmbiguous()
  {
    // Arrange
    var configuracao = Configuracao(new() { ["Storage:CredentialsJson"] = "{}", ["Storage:CredentialsPath"] = "/chave.json" });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkStorageGcp(configuracao));

    // Assert
    Assert.Contains("Options.Storage.Gcp.CredentialsAmbiguous", ex.GetErrorMessages());
  }

  // Testa se o arquivo de chave inexistente falha no registro
  [Fact]
  public void AddTooarkStorageGcp_ShouldThrow_WhenCredentialsFileIsMissing()
  {
    // Arrange
    var caminho = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
    var configuracao = Configuracao(new() { ["Storage:CredentialsPath"] = caminho });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkStorageGcp(configuracao));

    // Assert
    Assert.Contains($"Options.Storage.Gcp.CredentialsNotFound;{caminho}", ex.GetErrorMessages());
  }

  // Testa se a chave inválida falha no primeiro uso, com a chave de erro própria
  [Theory]
  [InlineData("não é json")]
  [InlineData("{\"type\":\"authorized_user\",\"client_id\":\"x\",\"client_secret\":\"y\",\"refresh_token\":\"z\"}")]
  [InlineData("{\"type\":\"service_account\",\"client_email\":\"a@b.com\",\"private_key\":\"invalida\"}")]
  public void AddTooarkStorageGcp_ShouldThrowOnFirstUse_WhenCredentialsAreInvalid(string json)
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTooarkStorageGcp(Configuracao(new() { ["Storage:Bucket"] = "tooark", ["Storage:CredentialsJson"] = json }));
    using var provider = services.BuildServiceProvider();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => provider.GetRequiredService<IStorageService>());

    // Assert
    Assert.Contains("Options.Storage.Gcp.CredentialsInvalid", ex.GetErrorMessages());
  }

  // Testa se o registro repetido não duplica o serviço
  [Fact]
  public void AddTooarkStorageGcp_ShouldNotDuplicateServices_WhenCalledTwice()
  {
    // Arrange
    var configuracao = Configuracao(new() { ["Storage:Bucket"] = "tooark" });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkStorageGcp(configuracao);
    services.AddTooarkStorageGcp(configuracao);

    // Assert
    Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStorageService));
    Assert.Single(services, descriptor => descriptor.ServiceType == typeof(StorageClient));
  }
}
