using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Storage.Aws.Options;
using Tooark.Storage.Dtos;
using Tooark.Storage.Enums;

namespace Tooark.Storage.Aws;

/// <summary>
/// Serviço de storage sobre o Amazon S3 e serviços compatíveis.
/// </summary>
/// <remarks>
/// Recebe o <see cref="IAmazonS3"/> pronto: o registro do pacote monta o cliente a partir das opções, e a aplicação
/// que já registra o próprio cliente tem o dela usado.
/// </remarks>
/// <param name="client">O cliente do S3.</param>
/// <param name="options">As opções do storage na AWS.</param>
public sealed class AwsStorageService(IAmazonS3 client, IOptions<AwsStorageOptions> options) : StorageServiceBase(options.Value)
{
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
    var request = new PutObjectRequest
    {
      BucketName = bucket,
      Key = key,
      InputStream = content,
      ContentType = contentType,
      // O stream é de quem chamou, e o contrato diz que o serviço não o fecha
      AutoCloseStream = false
    };

    var response = await client.PutObjectAsync(request, cancellationToken);

    return new StorageObjectDto
    {
      Bucket = bucket,
      Key = key,
      Size = content.CanSeek ? content.Length : null,
      ContentType = contentType,
      ETag = NormalizeETag(response.ETag)
    };
  });

  /// <inheritdoc/>
  protected override Task<Stream> DownloadCoreAsync(string bucket, string key, CancellationToken cancellationToken) =>
    Execute(async () =>
    {
      // Fechar o stream devolvido libera a resposta e a conexão
      var response = await client.GetObjectAsync(bucket, key, cancellationToken);

      return response.ResponseStream;
    });

  /// <inheritdoc/>
  protected override Task<bool> DeleteCoreAsync(string bucket, string key, CancellationToken cancellationToken) =>
    Execute(async () =>
    {
      // O S3 responde sucesso ao excluir um objeto que não existe: saber se ele existia custa uma consulta antes
      if (await GetInfoCoreAsync(bucket, key, cancellationToken) is null)
      {
        return false;
      }

      await client.DeleteObjectAsync(bucket, key, cancellationToken);

      return true;
    });

  /// <inheritdoc/>
  protected override Task<StorageObjectDto?> GetInfoCoreAsync(string bucket, string key, CancellationToken cancellationToken) =>
    Execute(async () =>
    {
      try
      {
        var response = await client.GetObjectMetadataAsync(bucket, key, cancellationToken);

        return new StorageObjectDto
        {
          Bucket = bucket,
          Key = key,
          Size = response.Headers.ContentLength,
          ContentType = response.Headers.ContentType,
          ETag = NormalizeETag(response.ETag),
          LastModified = response.LastModified is DateTime modified ? new DateTimeOffset(modified.ToUniversalTime()) : null
        };
      }
      catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
      {
        // A consulta de metadados não traz corpo, então objeto e bucket inexistentes chegam iguais
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
    var request = new GetPreSignedUrlRequest
    {
      BucketName = bucket,
      Key = key,
      Verb = access == ESignedUrlAccess.Write ? HttpVerb.PUT : HttpVerb.GET,
      Expires = DateTime.UtcNow.Add(expiration)
    };

    return new Uri(await client.GetPreSignedURLAsync(request));
  });

  #endregion

  #region Private Methods

  /// <summary>
  /// Executa uma operação no S3, trocando as exceções do SDK pelas do Tooark.
  /// </summary>
  /// <typeparam name="T">O tipo do resultado.</typeparam>
  /// <param name="operation">A operação.</param>
  /// <returns>O resultado da operação.</returns>
  /// <exception cref="NotFoundException">Quando o objeto não existe.</exception>
  /// <exception cref="InternalServerErrorException">Quando o bucket não existe, o acesso é negado ou o S3 falha.</exception>
  private static async Task<T> Execute<T>(Func<Task<T>> operation)
  {
    try
    {
      return await operation();
    }
    catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound && e.ErrorCode == "NoSuchBucket")
    {
      throw new InternalServerErrorException("Storage.BucketNotFound", e);
    }
    catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
    {
      throw new NotFoundException("Storage.ObjectNotFound", e);
    }
    catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.Forbidden)
    {
      throw new InternalServerErrorException("Storage.AccessDenied", e);
    }
    catch (Exception e) when (e is AmazonServiceException or AmazonClientException)
    {
      throw new InternalServerErrorException("Storage.OperationFailed", e);
    }
  }

  #endregion
}
