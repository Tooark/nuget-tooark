using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Requests;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;
using Moq;
using Tooark.Exceptions;
using Tooark.Storage.Enums;
using Tooark.Storage.Gcp;
using Tooark.Storage.Gcp.Options;
using StorageObject = Google.Apis.Storage.v1.Data.Object;

namespace Tooark.Tests.Storage.Gcp;

/// <summary>
/// Testes do serviço de storage sobre o Google Cloud Storage.
/// </summary>
public class GcpStorageServiceTests
{
  // Bucket padrão das opções dos testes
  private const string Bucket = "tooark-testes";

  // Chave de conta de serviço falsa, com chave RSA gerada na hora: a assinatura de URL é local e não usa rede
  internal static string ContaDeServicoFalsa()
  {
    using var rsa = RSA.Create(2048);

    return JsonSerializer.Serialize(new Dictionary<string, string>
    {
      ["type"] = "service_account",
      ["project_id"] = "tooark-testes",
      ["private_key_id"] = "chave-teste",
      ["private_key"] = rsa.ExportPkcs8PrivateKeyPem(),
      ["client_email"] = "testes@tooark-testes.iam.gserviceaccount.com",
      ["client_id"] = "123456789",
      ["token_uri"] = "https://oauth2.googleapis.com/token"
    });
  }

  // Assinador de URLs com a conta de serviço falsa
  private static UrlSigner AssinadorFalso() =>
    UrlSigner.FromCredential(CredentialFactory.FromJson<ServiceAccountCredential>(ContaDeServicoFalsa()));

  // Cria o serviço sobre o cliente informado
  private static GcpStorageService Criar(StorageClient cliente, UrlSigner? assinador = null) =>
    new(cliente, assinador ?? AssinadorFalso(), Microsoft.Extensions.Options.Options.Create(new GcpStorageOptions { Bucket = Bucket }));

  // Monta a exceção do Google Cloud Storage com o status e a mensagem informados
  private static GoogleApiException ErroGcs(HttpStatusCode status, string mensagem = "falha") =>
    new("storage", mensagem) { HttpStatusCode = status, Error = new RequestError { Code = (int)status, Message = mensagem } };

  // Testa se o upload envia bucket, chave e tipo, e devolve os dados do objeto
  [Fact]
  public async Task UploadAsync_ShouldUploadAndMapObject()
  {
    // Arrange
    var alterado = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    var cliente = new Mock<StorageClient>();
    cliente
      .Setup(c => c.UploadObjectAsync(Bucket, "docs/a.pdf", "application/pdf", It.IsAny<Stream>(), It.IsAny<UploadObjectOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<IUploadProgress>>()))
      .ReturnsAsync(new StorageObject { Size = 3, ContentType = "application/pdf", ETag = "CJ7s9Z8CEAE=", UpdatedDateTimeOffset = alterado });
    using var conteudo = new MemoryStream([1, 2, 3]);

    // Act
    var resultado = await Criar(cliente.Object).UploadAsync("docs/a.pdf", conteudo, "application/pdf", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(Bucket, resultado.Bucket);
    Assert.Equal("docs/a.pdf", resultado.Key);
    Assert.Equal(3, resultado.Size);
    Assert.Equal("application/pdf", resultado.ContentType);
    Assert.Equal("CJ7s9Z8CEAE=", resultado.ETag);
    Assert.Equal(alterado, resultado.LastModified);
  }

  // Testa se o download devolve o conteúdo do início
  [Fact]
  public async Task DownloadAsync_ShouldReturnContentFromStart()
  {
    // Arrange
    var cliente = new Mock<StorageClient>();
    cliente
      .Setup(c => c.DownloadObjectAsync(Bucket, "a.txt", It.IsAny<Stream>(), It.IsAny<DownloadObjectOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<IDownloadProgress>>()))
      .Callback<string, string, Stream, DownloadObjectOptions, CancellationToken, IProgress<IDownloadProgress>>((_, _, destino, _, _, _) => destino.Write([7, 8, 9]))
      .ReturnsAsync(new StorageObject());

    // Act
    await using var resultado = await Criar(cliente.Object).DownloadAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken);
    using var leitor = new MemoryStream();
    await resultado.CopyToAsync(leitor, TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal([7, 8, 9], leitor.ToArray());
  }

  // Testa se as falhas do Google Cloud Storage viram as exceções do Tooark
  [Theory]
  [InlineData(HttpStatusCode.NotFound, "No such object: tooark-testes/a.txt", typeof(NotFoundException), "Storage.ObjectNotFound")]
  [InlineData(HttpStatusCode.NotFound, "The specified bucket does not exist.", typeof(InternalServerErrorException), "Storage.BucketNotFound")]
  [InlineData(HttpStatusCode.Forbidden, "Access denied.", typeof(InternalServerErrorException), "Storage.AccessDenied")]
  [InlineData(HttpStatusCode.Unauthorized, "Invalid credentials.", typeof(InternalServerErrorException), "Storage.AccessDenied")]
  [InlineData(HttpStatusCode.ServiceUnavailable, "Backend error.", typeof(InternalServerErrorException), "Storage.OperationFailed")]
  public async Task DownloadAsync_ShouldMapGcsErrors(HttpStatusCode status, string mensagem, Type tipo, string chave)
  {
    // Arrange
    var erro = ErroGcs(status, mensagem);
    var cliente = new Mock<StorageClient>();
    cliente
      .Setup(c => c.DownloadObjectAsync(Bucket, "a.txt", It.IsAny<Stream>(), It.IsAny<DownloadObjectOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<IDownloadProgress>>()))
      .ThrowsAsync(erro);

    // Act
    var ex = (TooarkException)await Assert.ThrowsAsync(tipo, () => Criar(cliente.Object).DownloadAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains(chave, ex.GetErrorMessages());
    Assert.Same(erro, ex.InnerException);
  }

  // Testa se os dados do objeto são lidos sem baixar o conteúdo
  [Fact]
  public async Task GetInfoAsync_ShouldMapObject()
  {
    // Arrange
    var cliente = new Mock<StorageClient>();
    cliente
      .Setup(c => c.GetObjectAsync(Bucket, "a.png", It.IsAny<GetObjectOptions>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new StorageObject { Size = 2048, ContentType = "image/png" });

    // Act
    var resultado = await Criar(cliente.Object).GetInfoAsync("a.png", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotNull(resultado);
    Assert.Equal(2048, resultado.Size);
    Assert.Equal("image/png", resultado.ContentType);
  }

  // Testa se o objeto inexistente devolve nulo nos dados e falso na existência
  [Fact]
  public async Task GetInfoAsync_ShouldReturnNull_WhenObjectIsMissing()
  {
    // Arrange
    var cliente = new Mock<StorageClient>();
    cliente
      .Setup(c => c.GetObjectAsync(Bucket, "a.txt", It.IsAny<GetObjectOptions>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(ErroGcs(HttpStatusCode.NotFound, "No such object: tooark-testes/a.txt"));
    var servico = Criar(cliente.Object);

    // Act & Assert
    Assert.Null(await servico.GetInfoAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));
    Assert.False(await servico.ExistsAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));
  }

  // Testa se o bucket inexistente não é tratado como objeto inexistente
  [Fact]
  public async Task GetInfoAsync_ShouldThrow_WhenBucketIsMissing()
  {
    // Arrange
    var cliente = new Mock<StorageClient>();
    cliente
      .Setup(c => c.GetObjectAsync(Bucket, "a.txt", It.IsAny<GetObjectOptions>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(ErroGcs(HttpStatusCode.NotFound, "The specified bucket does not exist."));

    // Act
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => Criar(cliente.Object).GetInfoAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Storage.BucketNotFound", ex.GetErrorMessages());
  }

  // Testa se a exclusão devolve verdadeiro quando exclui e falso quando o objeto não existia
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task DeleteAsync_ShouldReportWhetherObjectExisted(bool existia)
  {
    // Arrange
    var cliente = new Mock<StorageClient>();
    var exclusao = cliente.Setup(c => c.DeleteObjectAsync(Bucket, "a.txt", It.IsAny<DeleteObjectOptions>(), It.IsAny<CancellationToken>()));

    if (existia)
    {
      exclusao.Returns(Task.CompletedTask);
    }
    else
    {
      exclusao.ThrowsAsync(ErroGcs(HttpStatusCode.NotFound, "No such object: tooark-testes/a.txt"));
    }

    // Act
    var resultado = await Criar(cliente.Object).DeleteAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(existia, resultado);
  }

  // Testa se a URL assinada V4 é calculada localmente, com a validade pedida
  [Theory]
  [InlineData(ESignedUrlAccess.Read)]
  [InlineData(ESignedUrlAccess.Write)]
  public async Task GetSignedUrlAsync_ShouldSignLocallyWithV4(ESignedUrlAccess acesso)
  {
    // Arrange
    var servico = Criar(new Mock<StorageClient>().Object);

    // Act
    var url = await servico.GetSignedUrlAsync("pasta/a.txt", TimeSpan.FromMinutes(5), acesso, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("storage.googleapis.com", url.Host);
    Assert.Equal($"/{Bucket}/pasta/a.txt", url.AbsolutePath);
    Assert.Contains("X-Goog-Algorithm=GOOG4-RSA-SHA256", url.Query);
    Assert.Contains("X-Goog-Expires=300", url.Query);
    Assert.Contains("X-Goog-Signature=", url.Query);
  }

  // Testa se leitura e escrita geram assinaturas diferentes, porque o verbo entra na assinatura
  [Fact]
  public async Task GetSignedUrlAsync_ShouldSignVerb()
  {
    // Arrange
    var servico = Criar(new Mock<StorageClient>().Object);

    // Act
    var leitura = await servico.GetSignedUrlAsync("a.txt", TimeSpan.FromMinutes(5), ESignedUrlAccess.Read, cancellationToken: TestContext.Current.CancellationToken);
    var escrita = await servico.GetSignedUrlAsync("a.txt", TimeSpan.FromMinutes(5), ESignedUrlAccess.Write, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotEqual(leitura, escrita);
  }

  // Testa se a credencial que não assina vira erro de configuração só na URL assinada
  [Fact]
  public async Task GetSignedUrlAsync_ShouldThrow_WhenCredentialCannotSign()
  {
    // Arrange
    var cliente = new Mock<StorageClient>();
    cliente
      .Setup(c => c.GetObjectAsync(Bucket, "a.txt", It.IsAny<GetObjectOptions>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new StorageObject());
    var assinador = new Lazy<UrlSigner>(() => throw new InvalidOperationException("credencial de usuário não assina"));
    var servico = new GcpStorageService(cliente.Object, assinador, Microsoft.Extensions.Options.Options.Create(new GcpStorageOptions { Bucket = Bucket }));

    // Act
    var existe = await servico.ExistsAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken);
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => servico.GetSignedUrlAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));

    // Assert
    Assert.True(existe);
    Assert.Contains("Storage.SigningNotSupported", ex.GetErrorMessages());
  }
}
