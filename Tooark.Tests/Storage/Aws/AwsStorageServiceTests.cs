using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using Tooark.Exceptions;
using Tooark.Storage.Aws;
using Tooark.Storage.Aws.Options;
using Tooark.Storage.Enums;

namespace Tooark.Tests.Storage.Aws;

/// <summary>
/// Testes do serviço de storage sobre o Amazon S3.
/// </summary>
public class AwsStorageServiceTests
{
  // Bucket padrão das opções dos testes
  private const string Bucket = "tooark-testes";

  // Cria o serviço sobre o cliente informado
  private static AwsStorageService Criar(IAmazonS3 cliente) =>
    new(cliente, Microsoft.Extensions.Options.Options.Create(new AwsStorageOptions { Bucket = Bucket }));

  // Monta a exceção do S3 com o status e o código informados
  private static AmazonS3Exception ErroS3(HttpStatusCode status, string codigo = "") =>
    new("falha") { StatusCode = status, ErrorCode = codigo };

  // Testa se o upload envia bucket, chave e tipo, sem fechar o stream de quem chamou
  [Fact]
  public async Task UploadAsync_ShouldSendRequest_AndReturnObject()
  {
    // Arrange
    PutObjectRequest? enviado = null;
    var cliente = new Mock<IAmazonS3>();
    cliente
      .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
      .Callback<PutObjectRequest, CancellationToken>((request, _) => enviado = request)
      .ReturnsAsync(new PutObjectResponse { ETag = "\"etag-1\"" });
    using var conteudo = new MemoryStream([1, 2, 3]);

    // Act
    var resultado = await Criar(cliente.Object).UploadAsync("docs/a.pdf", conteudo, "application/pdf", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotNull(enviado);
    Assert.Equal(Bucket, enviado.BucketName);
    Assert.Equal("docs/a.pdf", enviado.Key);
    Assert.Equal("application/pdf", enviado.ContentType);
    Assert.False(enviado.AutoCloseStream);
    Assert.Same(conteudo, enviado.InputStream);
    Assert.Equal(Bucket, resultado.Bucket);
    Assert.Equal("docs/a.pdf", resultado.Key);
    Assert.Equal(3, resultado.Size);
    Assert.Equal("etag-1", resultado.ETag);
  }

  // Testa se o download devolve o stream da resposta
  [Fact]
  public async Task DownloadAsync_ShouldReturnResponseStream()
  {
    // Arrange
    var conteudo = new MemoryStream([9]);
    var cliente = new Mock<IAmazonS3>();
    cliente
      .Setup(c => c.GetObjectAsync(Bucket, "a.txt", It.IsAny<CancellationToken>()))
      .ReturnsAsync(new GetObjectResponse { ResponseStream = conteudo });

    // Act
    var resultado = await Criar(cliente.Object).DownloadAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Same(conteudo, resultado);
  }

  // Testa se as falhas do S3 viram as exceções do Tooark
  [Theory]
  [InlineData(HttpStatusCode.NotFound, "NoSuchKey", typeof(NotFoundException), "Storage.ObjectNotFound")]
  [InlineData(HttpStatusCode.NotFound, "NoSuchBucket", typeof(InternalServerErrorException), "Storage.BucketNotFound")]
  [InlineData(HttpStatusCode.Forbidden, "AccessDenied", typeof(InternalServerErrorException), "Storage.AccessDenied")]
  [InlineData(HttpStatusCode.InternalServerError, "InternalError", typeof(InternalServerErrorException), "Storage.OperationFailed")]
  public async Task DownloadAsync_ShouldMapS3Errors(HttpStatusCode status, string codigo, Type tipo, string chave)
  {
    // Arrange
    var erro = ErroS3(status, codigo);
    var cliente = new Mock<IAmazonS3>();
    cliente.Setup(c => c.GetObjectAsync(Bucket, "a.txt", It.IsAny<CancellationToken>())).ThrowsAsync(erro);

    // Act
    var ex = (TooarkException)await Assert.ThrowsAsync(tipo, () => Criar(cliente.Object).DownloadAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains(chave, ex.GetErrorMessages());
    Assert.Same(erro, ex.InnerException);
  }

  // Testa se a falha do lado do cliente do SDK também vira exceção do Tooark
  [Fact]
  public async Task DownloadAsync_ShouldMapClientErrors()
  {
    // Arrange
    var cliente = new Mock<IAmazonS3>();
    cliente
      .Setup(c => c.GetObjectAsync(Bucket, "a.txt", It.IsAny<CancellationToken>()))
      .ThrowsAsync(new AmazonClientException("sem credenciais"));

    // Act
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => Criar(cliente.Object).DownloadAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Storage.OperationFailed", ex.GetErrorMessages());
  }

  // Testa se o cancelamento não é trocado por exceção do Tooark
  [Fact]
  public async Task DownloadAsync_ShouldPropagateCancellation()
  {
    // Arrange
    var cliente = new Mock<IAmazonS3>();
    cliente
      .Setup(c => c.GetObjectAsync(Bucket, "a.txt", It.IsAny<CancellationToken>()))
      .ThrowsAsync(new OperationCanceledException());

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(() => Criar(cliente.Object).DownloadAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));
  }

  // Testa se os metadados viram os dados do objeto
  [Fact]
  public async Task GetInfoAsync_ShouldMapMetadata()
  {
    // Arrange
    var alterado = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
    var resposta = new GetObjectMetadataResponse { ETag = "\"etag-2\"", LastModified = alterado };
    resposta.Headers.ContentLength = 2048;
    resposta.Headers.ContentType = "image/png";
    var cliente = new Mock<IAmazonS3>();
    cliente.Setup(c => c.GetObjectMetadataAsync(Bucket, "a.png", It.IsAny<CancellationToken>())).ReturnsAsync(resposta);

    // Act
    var resultado = await Criar(cliente.Object).GetInfoAsync("a.png", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotNull(resultado);
    Assert.Equal(2048, resultado.Size);
    Assert.Equal("image/png", resultado.ContentType);
    Assert.Equal("etag-2", resultado.ETag);
    Assert.Equal(new DateTimeOffset(alterado), resultado.LastModified);
  }

  // Testa se o objeto inexistente devolve nulo nos metadados e falso na existência
  [Fact]
  public async Task GetInfoAsync_ShouldReturnNull_WhenObjectIsMissing()
  {
    // Arrange
    var cliente = new Mock<IAmazonS3>();
    cliente
      .Setup(c => c.GetObjectMetadataAsync(Bucket, "a.txt", It.IsAny<CancellationToken>()))
      .ThrowsAsync(ErroS3(HttpStatusCode.NotFound, "NotFound"));
    var servico = Criar(cliente.Object);

    // Act & Assert
    Assert.Null(await servico.GetInfoAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));
    Assert.False(await servico.ExistsAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));
  }

  // Testa se a exclusão de objeto existente o exclui e devolve verdadeiro
  [Fact]
  public async Task DeleteAsync_ShouldDelete_WhenObjectExists()
  {
    // Arrange
    var cliente = new Mock<IAmazonS3>();
    cliente
      .Setup(c => c.GetObjectMetadataAsync(Bucket, "a.txt", It.IsAny<CancellationToken>()))
      .ReturnsAsync(new GetObjectMetadataResponse());
    cliente
      .Setup(c => c.DeleteObjectAsync(Bucket, "a.txt", It.IsAny<CancellationToken>()))
      .ReturnsAsync(new DeleteObjectResponse());

    // Act
    var resultado = await Criar(cliente.Object).DeleteAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.True(resultado);
    cliente.Verify(c => c.DeleteObjectAsync(Bucket, "a.txt", It.IsAny<CancellationToken>()), Times.Once);
  }

  // Testa se a exclusão de objeto inexistente devolve falso sem chamar a exclusão
  [Fact]
  public async Task DeleteAsync_ShouldReturnFalse_WhenObjectIsMissing()
  {
    // Arrange
    var cliente = new Mock<IAmazonS3>();
    cliente
      .Setup(c => c.GetObjectMetadataAsync(Bucket, "a.txt", It.IsAny<CancellationToken>()))
      .ThrowsAsync(ErroS3(HttpStatusCode.NotFound));

    // Act
    var resultado = await Criar(cliente.Object).DeleteAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.False(resultado);
    cliente.Verify(c => c.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  // Testa se a URL assinada pede o verbo e a validade corretos
  [Theory]
  [InlineData(ESignedUrlAccess.Read, "GET")]
  [InlineData(ESignedUrlAccess.Write, "PUT")]
  public async Task GetSignedUrlAsync_ShouldRequestVerbAndExpiration(ESignedUrlAccess acesso, string verbo)
  {
    // Arrange
    GetPreSignedUrlRequest? pedido = null;
    var cliente = new Mock<IAmazonS3>();
    cliente
      .Setup(c => c.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
      .Callback<GetPreSignedUrlRequest>(request => pedido = request)
      .ReturnsAsync("https://tooark-testes.s3.amazonaws.com/a.txt?X-Amz-Signature=x");
    var antes = DateTime.UtcNow;

    // Act
    var url = await Criar(cliente.Object).GetSignedUrlAsync("a.txt", TimeSpan.FromMinutes(10), acesso, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotNull(pedido);
    Assert.Equal(verbo, pedido.Verb.ToString());
    Assert.Equal(Bucket, pedido.BucketName);
    Assert.Equal("a.txt", pedido.Key);
    Assert.InRange(pedido.Expires!.Value, antes.AddMinutes(10), DateTime.UtcNow.AddMinutes(10));
    Assert.Equal("tooark-testes.s3.amazonaws.com", url.Host);
  }

  // Testa se a URL assinada é calculada localmente, com credencial fixa e sem acesso à rede
  [Fact]
  public async Task GetSignedUrlAsync_ShouldSignLocally_WithRealClient()
  {
    // Arrange
    using var cliente = new AmazonS3Client(new BasicAWSCredentials("AKIAEXEMPLO", "segredo"), RegionEndpoint.SAEast1);

    // Act
    var url = await Criar(cliente).GetSignedUrlAsync("pasta/a b.txt", TimeSpan.FromMinutes(5), cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(Uri.UriSchemeHttps, url.Scheme);
    Assert.Contains(Bucket, url.Host + url.AbsolutePath);
    Assert.Contains("X-Amz-Expires=300", url.Query);
    Assert.Contains("X-Amz-Signature=", url.Query);
    Assert.Contains("AKIAEXEMPLO", url.Query);
  }
}
