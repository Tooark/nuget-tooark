using System.Net;
using System.Text;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tooark.Exceptions;
using Tooark.Secrets.Aws;
using Tooark.Secrets.Aws.Configuration;
using Tooark.Secrets.Aws.Injections;
using Tooark.Secrets.Aws.Options;
using Tooark.Secrets.Interfaces;
using InternalServerErrorException = Tooark.Exceptions.InternalServerErrorException;
using Parameter = Amazon.SimpleSystemsManagement.Model.Parameter;

namespace Tooark.Tests.Secrets.Aws;

/// <summary>
/// Testes dos segredos na AWS: fonte de configuração, serviço e registro.
/// </summary>
public class AwsSecretsTests
{
  // Cliente do Secrets Manager com os segredos informados, listados em duas páginas
  private static Mock<IAmazonSecretsManager> SecretsManager(Dictionary<string, string> segredos)
  {
    var nomes = segredos.Keys.ToList();
    var cliente = new Mock<IAmazonSecretsManager>();

    cliente
      .Setup(c => c.ListSecretsAsync(It.Is<ListSecretsRequest>(r => r.NextToken == null), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListSecretsResponse { SecretList = [.. nomes.Take(1).Select(n => new SecretListEntry { Name = n })], NextToken = "pagina-2" });
    cliente
      .Setup(c => c.ListSecretsAsync(It.Is<ListSecretsRequest>(r => r.NextToken == "pagina-2"), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListSecretsResponse { SecretList = [.. nomes.Skip(1).Select(n => new SecretListEntry { Name = n })] });
    cliente
      .Setup(c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((GetSecretValueRequest r, CancellationToken _) => segredos.TryGetValue(r.SecretId, out var valor)
        ? new GetSecretValueResponse { Name = r.SecretId, SecretString = valor }
        : throw new Amazon.SecretsManager.Model.ResourceNotFoundException("não existe"));

    return cliente;
  }

  // Cliente do Parameter Store com os parâmetros informados, em duas páginas
  private static Mock<IAmazonSimpleSystemsManagement> ParameterStore(Dictionary<string, string> parametros)
  {
    var lista = parametros.Select(p => new Parameter { Name = p.Key, Value = p.Value }).ToList();
    var cliente = new Mock<IAmazonSimpleSystemsManagement>();

    cliente
      .Setup(c => c.GetParametersByPathAsync(It.Is<GetParametersByPathRequest>(r => r.NextToken == null), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new GetParametersByPathResponse { Parameters = [.. lista.Take(1)], NextToken = "pagina-2" });
    cliente
      .Setup(c => c.GetParametersByPathAsync(It.Is<GetParametersByPathRequest>(r => r.NextToken == "pagina-2"), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new GetParametersByPathResponse { Parameters = [.. lista.Skip(1)] });

    return cliente;
  }

  // Monta a configuração só com a fonte da AWS
  private static IConfigurationRoot Montar(AwsSecretsOptions opcoes, IAmazonSecretsManager? sm = null, IAmazonSimpleSystemsManagement? ps = null) =>
    new ConfigurationBuilder()
      .Add(new AwsSecretsConfigurationSource(
        opcoes,
        () => sm ?? throw new InvalidOperationException("Secrets Manager não deveria ser usado"),
        () => ps ?? throw new InvalidOperationException("Parameter Store não deveria ser usado")))
      .Build();

  // Testa se os parâmetros abaixo do caminho viram chaves, lidos recursivamente e descriptografados
  [Fact]
  public void Configuration_ShouldLoadParametersUnderPath()
  {
    // Arrange
    var ps = ParameterStore(new()
    {
      ["/arkuest/prod/Storage/Bucket"] = "arquivos",
      ["/arkuest/prod/Jwt/Secret"] = "segredo"
    });

    // Act
    var configuracao = Montar(new AwsSecretsOptions { ParametersPath = "arkuest/prod/" }, ps: ps.Object);

    // Assert
    Assert.Equal("arquivos", configuracao["Storage:Bucket"]);
    Assert.Equal("segredo", configuracao["Jwt:Secret"]);
    ps.Verify(c => c.GetParametersByPathAsync(
      It.Is<GetParametersByPathRequest>(r => r.Path == "/arkuest/prod" && r.Recursive == true && r.WithDecryption == true),
      It.IsAny<CancellationToken>()), Times.Exactly(2));
  }

  // Testa se os segredos do prefixo viram chaves, respeitando o limite de nível e expandindo o JSON da raiz
  [Fact]
  public void Configuration_ShouldLoadSecretsUnderPrefix()
  {
    // Arrange
    var sm = SecretsManager(new()
    {
      ["arkuest/prod"] = """{"Jwt":{"Issuer":"tooark"}}""",
      ["arkuest/prod/Storage/SecretKey"] = "chave-s3",
      ["arkuest/production/Storage/SecretKey"] = "de outro ambiente"
    });

    // Act
    var configuracao = Montar(new AwsSecretsOptions { SecretsPrefix = "arkuest/prod", ExpandJson = true }, sm: sm.Object);

    // Assert
    Assert.Equal("tooark", configuracao["Jwt:Issuer"]);
    Assert.Equal("chave-s3", configuracao["Storage:SecretKey"]);
    Assert.DoesNotContain(configuracao.AsEnumerable(), par => par.Value == "de outro ambiente");
    sm.Verify(c => c.ListSecretsAsync(
      It.Is<ListSecretsRequest>(r => r.Filters[0].Key == FilterNameStringType.Name && r.Filters[0].Values[0] == "arkuest/prod"),
      It.IsAny<CancellationToken>()), Times.Exactly(2));
  }

  // Testa se o segredo vence o parâmetro com o mesmo nome
  [Fact]
  public void Configuration_ShouldPreferSecretsOverParameters()
  {
    // Arrange
    var sm = SecretsManager(new() { ["arkuest/prod/Jwt/Secret"] = "do-secrets-manager" });
    var ps = ParameterStore(new() { ["/arkuest/prod/Jwt/Secret"] = "do-parameter-store" });

    // Act
    var configuracao = Montar(new AwsSecretsOptions { SecretsPrefix = "arkuest/prod", ParametersPath = "/arkuest/prod" }, sm.Object, ps.Object);

    // Assert
    Assert.Equal("do-secrets-manager", configuracao["Jwt:Secret"]);
  }

  // Testa se o segredo excluído entre a listagem e a leitura é ignorado
  [Fact]
  public void Configuration_ShouldSkipSecretDeletedAfterListing()
  {
    // Arrange
    var sm = new Mock<IAmazonSecretsManager>();
    sm.Setup(c => c.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListSecretsResponse { SecretList = [new() { Name = "app/Excluido" }, new() { Name = "app/Existe" }] });
    sm.Setup(c => c.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "app/Excluido"), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new Amazon.SecretsManager.Model.ResourceNotFoundException("excluído"));
    sm.Setup(c => c.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "app/Existe"), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new GetSecretValueResponse { SecretBinary = new MemoryStream(Encoding.UTF8.GetBytes("binário")) });

    // Act
    var configuracao = Montar(new AwsSecretsOptions { SecretsPrefix = "app" }, sm: sm.Object);

    // Assert
    Assert.Null(configuracao["Excluido"]);
    Assert.Equal("binário", configuracao["Existe"]);
  }

  // Testa se a falha da AWS impede a aplicação de subir, e a fonte opcional sobe vazia
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void Configuration_ShouldHandleFailure(bool opcional)
  {
    // Arrange
    var sm = new Mock<IAmazonSecretsManager>();
    sm.Setup(c => c.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new AmazonSecretsManagerException("sem permissão") { StatusCode = HttpStatusCode.Forbidden });
    var opcoes = new AwsSecretsOptions { SecretsPrefix = "app", Optional = opcional };

    // Act
    var acao = () => Montar(opcoes, sm: sm.Object);

    // Assert
    if (opcional)
    {
      Assert.Empty(acao().AsEnumerable());
    }
    else
    {
      Assert.Contains("Secrets.LoadFailed;AWS", Assert.Throws<InternalServerErrorException>(acao).GetErrorMessages());
    }
  }

  // Testa se a fonte sem prefixo nem caminho é recusada ao ser adicionada
  [Fact]
  public void AddTooarkSecretsAws_ShouldThrow_WhenNoSourceIsConfigured()
  {
    // Arrange
    var builder = new ConfigurationBuilder();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => builder.AddTooarkSecretsAws(opcoes => opcoes.Region = "sa-east-1"));

    // Assert
    Assert.Contains("Options.Secrets.SourceNotConfigured", ex.GetErrorMessages());
  }

  // Testa se o endereço de serviço inválido é recusado
  [Fact]
  public void AddTooarkSecretsAws_ShouldThrow_WhenServiceUrlIsInvalid()
  {
    // Arrange
    var builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["Secrets:SecretsPrefix"] = "app",
      ["Secrets:ServiceUrl"] = "localhost:4566"
    });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => builder.AddTooarkSecretsAws());

    // Assert
    Assert.Contains("Options.Secrets.Aws.ServiceUrlInvalid;localhost:4566", ex.GetErrorMessages());
  }

  // Cria o serviço sobre o cliente informado
  private static AwsSecretService Servico(IAmazonSecretsManager cliente) =>
    new(cliente, Microsoft.Extensions.Options.Options.Create(new AwsSecretsOptions()));

  // Testa se o serviço lê o segredo e o campo, com cache
  [Fact]
  public async Task Service_ShouldReadSecretAndField_WithCache()
  {
    // Arrange
    var sm = SecretsManager(new() { ["arkuest/tenants/acme"] = """{"ApiKey":"chave-acme"}""" });
    var servico = Servico(sm.Object);

    // Act
    var valor = await servico.GetAsync("arkuest/tenants/acme", TestContext.Current.CancellationToken);
    var campo = await servico.GetFieldAsync("arkuest/tenants/acme", "ApiKey", TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("""{"ApiKey":"chave-acme"}""", valor);
    Assert.Equal("chave-acme", campo);
    sm.Verify(c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  // Testa se o segredo inexistente devolve nulo
  [Fact]
  public async Task Service_ShouldReturnNull_WhenSecretIsMissing()
  {
    // Arrange
    var servico = Servico(SecretsManager([]).Object);

    // Act
    var valor = await servico.GetAsync("inexistente", TestContext.Current.CancellationToken);

    // Assert
    Assert.Null(valor);
  }

  // Testa se as falhas da AWS viram as exceções do Tooark
  [Theory]
  [InlineData(HttpStatusCode.BadRequest, "AccessDeniedException", "Secrets.AccessDenied")]
  [InlineData(HttpStatusCode.Forbidden, "Forbidden", "Secrets.AccessDenied")]
  [InlineData(HttpStatusCode.InternalServerError, "InternalServiceError", "Secrets.OperationFailed")]
  public async Task Service_ShouldMapAwsErrors(HttpStatusCode status, string codigo, string chave)
  {
    // Arrange
    var erro = new AmazonSecretsManagerException("falha") { StatusCode = status, ErrorCode = codigo };
    var sm = new Mock<IAmazonSecretsManager>();
    sm.Setup(c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(erro);

    // Act
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => Servico(sm.Object).GetAsync("app", TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains(chave, ex.GetErrorMessages());
    Assert.Same(erro, ex.InnerException);
  }

  // Testa se o registro monta o serviço e mantém o cliente já registrado pela aplicação
  [Fact]
  public void AddTooarkSecretsAws_ShouldRegisterService_KeepingApplicationClient()
  {
    // Arrange
    var proprio = new Mock<IAmazonSecretsManager>().Object;
    var services = new ServiceCollection();
    services.AddSingleton(proprio);

    // Act
    services.AddTooarkSecretsAws(new ConfigurationBuilder().Build());
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.IsType<AwsSecretService>(provider.GetRequiredService<ISecretService>());
    Assert.Same(proprio, provider.GetRequiredService<IAmazonSecretsManager>());
  }

  // Testa se o registro recusa opções inválidas
  [Fact]
  public void AddTooarkSecretsAws_ShouldThrow_WhenOptionsAreInvalid()
  {
    // Arrange
    var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Secrets:CacheMinutes"] = "-1" }).Build();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => new ServiceCollection().AddTooarkSecretsAws(configuracao));

    // Assert
    Assert.Contains("Options.Secrets.CacheMinutesInvalid", ex.GetErrorMessages());
  }
}
