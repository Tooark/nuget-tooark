using System.Net;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Storage.Dtos;
using Tooark.Storage.Enums;
using Tooark.Storage.Gcp.Options;
using StorageObject = Google.Apis.Storage.v1.Data.Object;

namespace Tooark.Storage.Gcp;

/// <summary>
/// Serviço de storage sobre o Google Cloud Storage.
/// </summary>
/// <remarks>
/// Recebe o <see cref="StorageClient"/> pronto: o registro do pacote monta o cliente a partir das opções, e a
/// aplicação que já registra o próprio cliente tem o dela usado. O <see cref="UrlSigner"/> só é criado na primeira
/// URL assinada, porque nem toda credencial assina (a do <c>gcloud auth application-default login</c>, por exemplo),
/// e isso não deve impedir o upload e o download.
/// </remarks>
public sealed class GcpStorageService : StorageServiceBase
{
  #region Private Fields

  /// <summary>
  /// O cliente do Google Cloud Storage.
  /// </summary>
  private readonly StorageClient _client;

  /// <summary>
  /// O assinador de URLs, criado no primeiro uso.
  /// </summary>
  private readonly Lazy<UrlSigner> _signer;

  #endregion

  #region Constructors

  /// <summary>
  /// Cria o serviço com o cliente e o assinador informados.
  /// </summary>
  /// <param name="client">O cliente do Google Cloud Storage.</param>
  /// <param name="signer">O assinador de URLs.</param>
  /// <param name="options">As opções do storage no Google Cloud.</param>
  public GcpStorageService(StorageClient client, UrlSigner signer, IOptions<GcpStorageOptions> options)
    : this(client, new Lazy<UrlSigner>(signer), options)
  {
  }

  /// <summary>
  /// Cria o serviço com o assinador criado no primeiro uso.
  /// </summary>
  /// <param name="client">O cliente do Google Cloud Storage.</param>
  /// <param name="signer">O assinador de URLs, criado no primeiro uso.</param>
  /// <param name="options">As opções do storage no Google Cloud.</param>
  internal GcpStorageService(StorageClient client, Lazy<UrlSigner> signer, IOptions<GcpStorageOptions> options)
    : base(options.Value)
  {
    _client = client;
    _signer = signer;
  }

  #endregion

  #region Protected Methods

  /// <inheritdoc/>
  protected override Task<StorageObjectDto> UploadCoreAsync(
    string bucket,
    string key,
    Stream content,
    string? contentType,
    CancellationToken cancellationToken
  ) => Execute(async () =>
  {
    var uploaded = await _client.UploadObjectAsync(bucket, key, contentType, content, cancellationToken: cancellationToken);

    return ToDto(bucket, key, uploaded);
  });

  /// <inheritdoc/>
  protected override Task<Stream> DownloadCoreAsync(string bucket, string key, CancellationToken cancellationToken) =>
    Execute<Stream>(async () =>
    {
      // O cliente escreve num stream de destino; o conteúdo fica em memória e é devolvido do início
      var content = new MemoryStream();

      try
      {
        await _client.DownloadObjectAsync(bucket, key, content, cancellationToken: cancellationToken);
      }
      catch
      {
        await content.DisposeAsync();
        throw;
      }

      content.Position = 0;

      return content;
    });

  /// <inheritdoc/>
  protected override Task<bool> DeleteCoreAsync(string bucket, string key, CancellationToken cancellationToken) =>
    Execute(async () =>
    {
      try
      {
        await _client.DeleteObjectAsync(bucket, key, cancellationToken: cancellationToken);

        return true;
      }
      catch (GoogleApiException e) when (IsObjectNotFound(e))
      {
        return false;
      }
    });

  /// <inheritdoc/>
  protected override Task<StorageObjectDto?> GetInfoCoreAsync(string bucket, string key, CancellationToken cancellationToken) =>
    Execute<StorageObjectDto?>(async () =>
    {
      try
      {
        return ToDto(bucket, key, await _client.GetObjectAsync(bucket, key, cancellationToken: cancellationToken));
      }
      catch (GoogleApiException e) when (IsObjectNotFound(e))
      {
        return null;
      }
    });

  /// <inheritdoc/>
  protected override Task<Uri> GetSignedUrlCoreAsync(
    string bucket,
    string key,
    TimeSpan expiration,
    ESignedUrlAccess access,
    CancellationToken cancellationToken
  ) => Execute(async () =>
  {
    UrlSigner signer;

    try
    {
      signer = _signer.Value;
    }
    catch (Exception e) when (e is InvalidOperationException or ArgumentException or NotSupportedException)
    {
      // A credencial resolvida não tem chave para assinar nem permissão para pedir a assinatura ao IAM
      throw new InternalServerErrorException("Storage.SigningNotSupported", e);
    }

    var method = access == ESignedUrlAccess.Write ? HttpMethod.Put : HttpMethod.Get;
    var url = await signer.SignAsync(bucket, key, expiration, method, SigningVersion.V4, cancellationToken);

    return new Uri(url);
  });

  #endregion

  #region Private Methods

  /// <summary>
  /// Monta os dados do objeto a partir da resposta do Google Cloud Storage.
  /// </summary>
  /// <param name="bucket">O bucket.</param>
  /// <param name="key">A chave do objeto.</param>
  /// <param name="source">O objeto devolvido pelo Google Cloud Storage.</param>
  /// <returns>Os dados do objeto.</returns>
  private static StorageObjectDto ToDto(string bucket, string key, StorageObject source) => new()
  {
    Bucket = bucket,
    Key = key,
    Size = source.Size is ulong size ? (long)size : null,
    ContentType = source.ContentType,
    ETag = NormalizeETag(source.ETag),
    LastModified = source.UpdatedDateTimeOffset
  };

  /// <summary>
  /// Indica se o erro é de objeto inexistente, e não de bucket inexistente.
  /// </summary>
  /// <remarks>
  /// O Google Cloud Storage responde 404 para os dois; só a mensagem os diferencia.
  /// </remarks>
  /// <param name="exception">O erro do Google Cloud Storage.</param>
  /// <returns>Verdadeiro quando o objeto não existe.</returns>
  private static bool IsObjectNotFound(GoogleApiException exception) =>
    exception.HttpStatusCode == HttpStatusCode.NotFound && !IsBucketNotFound(exception);

  /// <summary>
  /// Indica se o erro é de bucket inexistente.
  /// </summary>
  /// <param name="exception">O erro do Google Cloud Storage.</param>
  /// <returns>Verdadeiro quando o bucket não existe.</returns>
  private static bool IsBucketNotFound(GoogleApiException exception) =>
    exception.HttpStatusCode == HttpStatusCode.NotFound &&
    (exception.Error?.Message ?? exception.Message).Contains("bucket does not exist", StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// Executa uma operação no Google Cloud Storage, trocando as exceções do SDK pelas do Tooark.
  /// </summary>
  /// <typeparam name="T">O tipo do resultado.</typeparam>
  /// <param name="operation">A operação.</param>
  /// <returns>O resultado da operação.</returns>
  /// <exception cref="NotFoundException">Quando o objeto não existe.</exception>
  /// <exception cref="InternalServerErrorException">Quando o bucket não existe, o acesso é negado ou o serviço falha.</exception>
  private static async Task<T> Execute<T>(Func<Task<T>> operation)
  {
    try
    {
      return await operation();
    }
    catch (GoogleApiException e) when (IsBucketNotFound(e))
    {
      throw new InternalServerErrorException("Storage.BucketNotFound", e);
    }
    catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
    {
      throw new NotFoundException("Storage.ObjectNotFound", e);
    }
    catch (GoogleApiException e) when (e.HttpStatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
    {
      throw new InternalServerErrorException("Storage.AccessDenied", e);
    }
    catch (GoogleApiException e)
    {
      throw new InternalServerErrorException("Storage.OperationFailed", e);
    }
  }

  #endregion
}
