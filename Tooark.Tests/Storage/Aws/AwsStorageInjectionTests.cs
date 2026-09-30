using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tooark.Exceptions;
using Tooark.Storage.Aws;
using Tooark.Storage.Aws.Injections;
using Tooark.Storage.Aws.Options;
using Tooark.Storage.Interfaces;

namespace Tooark.Tests.Storage.Aws;

/// <summary>
/// Testes do registro e das opções do storage na AWS.
/// </summary>
public class AwsStorageInjectionTests
{
  // Monta a configuração a partir dos pares informados
  private static IConfiguration Configuracao(Dictionary<string, string?> valores) =>
    new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

  // Testa se o serviço é registrado com o cliente montado a partir da seção Storage
  [Fact]
  public void AddTooarkStorageAws_ShouldRegisterServiceAndClient_FromConfiguration()
  {
    // Arrange
    var configuracao = Configuracao(new()
    {
      ["Storage:Bucket"] = "tooark",
      ["Storage:Region"] = "sa-east-1",
      ["Storage:AccessKey"] = "AKIAEXEMPLO",
      ["Storage:SecretKey"] = "segredo"
    });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkStorageAws(configuracao);
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.IsType<AwsStorageService>(provider.GetRequiredService<IStorageService>());
    var cliente = Assert.IsType<AmazonS3Client>(provider.GetRequiredService<IAmazonS3>());
    Assert.Equal(RegionEndpoint.SAEast1, cliente.Config.RegionEndpoint);
  }

  // Testa se o endereço de um serviço compatível e o estilo de caminho chegam ao cliente
  [Fact]
  public void AddTooarkStorageAws_ShouldConfigureCompatibleService()
  {
    // Arrange
    var configuracao = Configuracao(new()
    {
      ["Storage:Bucket"] = "tooark",
      ["Storage:ServiceUrl"] = "http://localhost:9000",
      ["Storage:ForcePathStyle"] = "true",
      ["Storage:AccessKey"] = "minio",
      ["Storage:SecretKey"] = "minio123"
    });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkStorageAws(configuracao);
    using var provider = services.BuildServiceProvider();
    var cliente = (AmazonS3Client)provider.GetRequiredService<IAmazonS3>();

    // Assert
    Assert.StartsWith("http://localhost:9000", cliente.Config.ServiceURL);
    Assert.True(((AmazonS3Config)cliente.Config).ForcePathStyle);
  }

  // Testa se a região informada junto com um serviço compatível vale para a assinatura
  [Fact]
  public void AddTooarkStorageAws_ShouldUseRegionForSigning_WithCompatibleService()
  {
    // Arrange
    var configuracao = Configuracao(new()
    {
      ["Storage:Bucket"] = "tooark",
      ["Storage:ServiceUrl"] = "https://minio.tooark.com",
      ["Storage:Region"] = "sa-east-1"
    });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkStorageAws(configuracao);
    using var provider = services.BuildServiceProvider();
    var cliente = (AmazonS3Client)provider.GetRequiredService<IAmazonS3>();

    // Assert
    Assert.Equal("sa-east-1", cliente.Config.AuthenticationRegion);
  }

  // Testa se o cliente já registrado pela aplicação é mantido
  [Fact]
  public void AddTooarkStorageAws_ShouldKeepClientRegisteredByApplication()
  {
    // Arrange
    var proprio = new Mock<IAmazonS3>().Object;
    var services = new ServiceCollection();
    services.AddSingleton(proprio);

    // Act
    services.AddTooarkStorageAws(Configuracao(new() { ["Storage:Bucket"] = "tooark" }));
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.Same(proprio, provider.GetRequiredService<IAmazonS3>());
  }

  // Testa se o ajuste programático vale por cima da configuração
  [Fact]
  public void AddTooarkStorageAws_ShouldApplyConfigureAfterConfiguration()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkStorageAws(Configuracao(new() { ["Storage:Bucket"] = "config" }), opcoes => opcoes.Bucket = "codigo");
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.Equal("codigo", provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AwsStorageOptions>>().Value.Bucket);
  }

  // Testa se credencial incompleta falha no registro
  [Theory]
  [InlineData("AKIAEXEMPLO", null, null)]
  [InlineData(null, "segredo", null)]
  [InlineData(null, null, "token")]
  public void AddTooarkStorageAws_ShouldThrow_WhenCredentialsAreIncomplete(string? acesso, string? segredo, string? token)
  {
    // Arrange
    var configuracao = Configuracao(new()
    {
      ["Storage:AccessKey"] = acesso,
      ["Storage:SecretKey"] = segredo,
      ["Storage:SessionToken"] = token
    });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkStorageAws(configuracao));

    // Assert
    Assert.Contains("Options.Storage.Aws.CredentialsIncomplete", ex.GetErrorMessages());
  }

  // Testa se o endereço de serviço inválido falha no registro
  [Theory]
  [InlineData("localhost:9000")]
  [InlineData("ftp://localhost")]
  [InlineData("/minio")]
  public void AddTooarkStorageAws_ShouldThrow_WhenServiceUrlIsInvalid(string endereco)
  {
    // Arrange
    var configuracao = Configuracao(new() { ["Storage:ServiceUrl"] = endereco });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkStorageAws(configuracao));

    // Assert
    Assert.Contains($"Options.Storage.Aws.ServiceUrlInvalid;{endereco}", ex.GetErrorMessages());
  }

  // Testa se a validade padrão fora do intervalo falha no registro
  [Fact]
  public void AddTooarkStorageAws_ShouldThrow_WhenSignedUrlExpirationIsInvalid()
  {
    // Arrange
    var configuracao = Configuracao(new() { ["Storage:SignedUrlExpirationMinutes"] = "0" });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkStorageAws(configuracao));

    // Assert
    Assert.Contains("Options.Storage.SignedUrlExpirationInvalid;10080", ex.GetErrorMessages());
  }

  // Testa se o registro repetido não duplica o serviço
  [Fact]
  public void AddTooarkStorageAws_ShouldNotDuplicateServices_WhenCalledTwice()
  {
    // Arrange
    var configuracao = Configuracao(new() { ["Storage:Bucket"] = "tooark" });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkStorageAws(configuracao);
    services.AddTooarkStorageAws(configuracao);

    // Assert
    Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStorageService));
    Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IAmazonS3));
  }
}
