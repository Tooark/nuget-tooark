using Tooark.Exceptions;
using Tooark.Storage;
using Tooark.Storage.Dtos;
using Tooark.Storage.Enums;
using Tooark.Storage.Options;

namespace Tooark.Tests.Storage;

/// <summary>
/// Testes da validação comum aos provedores de storage.
/// </summary>
public class StorageServiceBaseTests
{
  // Provedor em memória que registra o que chegou do serviço base
  private sealed class ProvedorFalso(StorageOptions options) : StorageServiceBase(options)
  {
    public string? Bucket { get; private set; }
    public string? Chave { get; private set; }
    public TimeSpan? Validade { get; private set; }
    public ESignedUrlAccess? Acesso { get; private set; }
    public bool Existe { get; set; } = true;

    protected override Task<StorageObjectDto> UploadCoreAsync(string bucket, string key, Stream content, string? contentType, CancellationToken cancellationToken)
    {
      (Bucket, Chave) = (bucket, key);
      return Task.FromResult(new StorageObjectDto { Bucket = bucket, Key = key });
    }

    protected override Task<Stream> DownloadCoreAsync(string bucket, string key, CancellationToken cancellationToken)
    {
      (Bucket, Chave) = (bucket, key);
      return Task.FromResult<Stream>(new MemoryStream());
    }

    protected override Task<bool> DeleteCoreAsync(string bucket, string key, CancellationToken cancellationToken)
    {
      (Bucket, Chave) = (bucket, key);
      return Task.FromResult(true);
    }

    protected override Task<StorageObjectDto?> GetInfoCoreAsync(string bucket, string key, CancellationToken cancellationToken)
    {
      (Bucket, Chave) = (bucket, key);
      return Task.FromResult(Existe ? new StorageObjectDto { Bucket = bucket, Key = key } : null);
    }

    protected override Task<Uri> GetSignedUrlCoreAsync(string bucket, string key, TimeSpan expiration, ESignedUrlAccess access, CancellationToken cancellationToken)
    {
      (Bucket, Chave, Validade, Acesso) = (bucket, key, expiration, access);
      return Task.FromResult(new Uri($"https://storage/{bucket}/{key}"));
    }

    public static string? Normalizar(string? etag) => NormalizeETag(etag);
  }

  // Cria o provedor com o bucket padrão informado
  private static ProvedorFalso Criar(string? bucket = "padrao", int validade = 5) =>
    new(new StorageOptions { Bucket = bucket, SignedUrlExpirationMinutes = validade });

  // Testa se o bucket da chamada tem precedência sobre o das opções
  [Fact]
  public async Task Operations_ShouldUseCallBucket_OverOptionsBucket()
  {
    // Arrange
    var provedor = Criar();

    // Act
    await provedor.DownloadAsync("a.txt", "outro", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("outro", provedor.Bucket);
    Assert.Equal("a.txt", provedor.Chave);
  }

  // Testa se o bucket das opções vale quando a chamada não informa um
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("  ")]
  public async Task Operations_ShouldUseOptionsBucket_WhenCallBucketIsMissing(string? bucket)
  {
    // Arrange
    var provedor = Criar();

    // Act
    await provedor.DeleteAsync("a.txt", bucket, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("padrao", provedor.Bucket);
  }

  // Testa se a falta de bucket é erro de configuração
  [Fact]
  public async Task Operations_ShouldThrow_WhenNoBucketIsAvailable()
  {
    // Arrange
    var provedor = Criar(bucket: null);

    // Act
    var ex = await Assert.ThrowsAsync<InternalServerErrorException>(() => provedor.GetInfoAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Storage.BucketNotConfigured", ex.GetErrorMessages());
  }

  // Testa se a chave em branco é recusada antes de chegar ao provedor
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task Operations_ShouldThrow_WhenKeyIsBlank(string? chave)
  {
    // Arrange
    var provedor = Criar();

    // Act
    var ex = await Assert.ThrowsAsync<BadRequestException>(() => provedor.ExistsAsync(chave!, cancellationToken: TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Storage.KeyRequired", ex.GetErrorMessages());
    Assert.Null(provedor.Chave);
  }

  // Testa se a chave acima de 1024 bytes UTF-8 é recusada
  [Fact]
  public async Task Operations_ShouldThrow_WhenKeyIsTooLong()
  {
    // Arrange
    var provedor = Criar();
    var chave = new string('ç', 513); // 1026 bytes em UTF-8

    // Act
    var ex = await Assert.ThrowsAsync<BadRequestException>(() => provedor.UploadAsync(chave, new MemoryStream(), cancellationToken: TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Storage.KeyTooLong;1024", ex.GetErrorMessages());
  }

  // Testa se o upload sem conteúdo é erro de programação
  [Fact]
  public async Task UploadAsync_ShouldThrow_WhenContentIsNull()
  {
    // Arrange
    var provedor = Criar();

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => provedor.UploadAsync("a.txt", null!, cancellationToken: TestContext.Current.CancellationToken));
  }

  // Testa se a existência vem dos dados do objeto
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task ExistsAsync_ShouldFollowGetInfo(bool existe)
  {
    // Arrange
    var provedor = Criar();
    provedor.Existe = existe;

    // Act
    var resultado = await provedor.ExistsAsync("a.txt", cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(existe, resultado);
  }

  // Testa se a validade das opções vale quando a chamada não informa uma
  [Fact]
  public async Task GetSignedUrlAsync_ShouldUseOptionsExpiration_WhenNotInformed()
  {
    // Arrange
    var provedor = Criar(validade: 15);

    // Act
    await provedor.GetSignedUrlAsync("a.txt", access: ESignedUrlAccess.Write, cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(TimeSpan.FromMinutes(15), provedor.Validade);
    Assert.Equal(ESignedUrlAccess.Write, provedor.Acesso);
  }

  // Testa se a validade informada na chamada é repassada
  [Fact]
  public async Task GetSignedUrlAsync_ShouldUseCallExpiration()
  {
    // Arrange
    var provedor = Criar();

    // Act
    await provedor.GetSignedUrlAsync("a.txt", TimeSpan.FromHours(2), cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(TimeSpan.FromHours(2), provedor.Validade);
    Assert.Equal(ESignedUrlAccess.Read, provedor.Acesso);
  }

  // Testa se a validade fora do intervalo aceito é recusada
  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  [InlineData(10081)]
  public async Task GetSignedUrlAsync_ShouldThrow_WhenExpirationIsOutOfRange(int minutos)
  {
    // Arrange
    var provedor = Criar();

    // Act
    var ex = await Assert.ThrowsAsync<BadRequestException>(() => provedor.GetSignedUrlAsync("a.txt", TimeSpan.FromMinutes(minutos), cancellationToken: TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Storage.SignedUrlExpirationInvalid;10080", ex.GetErrorMessages());
  }

  // Testa se as aspas do ETag saem
  [Theory]
  [InlineData("\"abc123\"", "abc123")]
  [InlineData("CJ7s9Z8CEAE=", "CJ7s9Z8CEAE=")]
  [InlineData("", null)]
  [InlineData(null, null)]
  public void NormalizeETag_ShouldRemoveQuotes(string? etag, string? esperado)
  {
    // Arrange & Act
    var resultado = ProvedorFalso.Normalizar(etag);

    // Assert
    Assert.Equal(esperado, resultado);
  }

  // Testa se a validade padrão das opções fora do intervalo é recusada
  [Theory]
  [InlineData(0)]
  [InlineData(10081)]
  public void StorageOptions_Validate_ShouldThrow_WhenExpirationIsOutOfRange(int minutos)
  {
    // Arrange
    var opcoes = new StorageOptions { SignedUrlExpirationMinutes = minutos };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(opcoes.Validate);

    // Assert
    Assert.Contains("Options.Storage.SignedUrlExpirationInvalid;10080", ex.GetErrorMessages());
  }
}
