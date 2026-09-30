using System.Text;
using Tooark.Exceptions;
using Tooark.Storage.Dtos;
using Tooark.Storage.Enums;
using Tooark.Storage.Interfaces;
using Tooark.Storage.Options;

namespace Tooark.Storage;

/// <summary>
/// Classe base dos provedores de storage.
/// </summary>
/// <remarks>
/// Concentra o que não depende do provedor: a validação da chave, a resolução do bucket e da validade da URL
/// assinada. Cada operação pública valida os argumentos e chama o método protegido do provedor já com o bucket e a
/// chave resolvidos.
/// </remarks>
/// <param name="options">As opções do storage.</param>
public abstract class StorageServiceBase(StorageOptions options) : IStorageService
{
  #region Constants

  /// <summary>
  /// Tamanho máximo da chave em bytes UTF-8: o limite da AWS e do Google Cloud.
  /// </summary>
  public const int MaxKeyBytes = 1024;

  #endregion

  #region Methods

  /// <inheritdoc/>
  public Task<StorageObjectDto> UploadAsync(
    string key,
    Stream content,
    string? contentType = null,
    string? bucket = null,
    CancellationToken cancellationToken = default
  )
  {
    ArgumentNullException.ThrowIfNull(content);

    return UploadCoreAsync(ResolveBucket(bucket), ValidateKey(key), content, contentType, cancellationToken);
  }

  /// <inheritdoc/>
  public Task<Stream> DownloadAsync(string key, string? bucket = null, CancellationToken cancellationToken = default) =>
    DownloadCoreAsync(ResolveBucket(bucket), ValidateKey(key), cancellationToken);

  /// <inheritdoc/>
  public Task<bool> DeleteAsync(string key, string? bucket = null, CancellationToken cancellationToken = default) =>
    DeleteCoreAsync(ResolveBucket(bucket), ValidateKey(key), cancellationToken);

  /// <inheritdoc/>
  public async Task<bool> ExistsAsync(string key, string? bucket = null, CancellationToken cancellationToken = default) =>
    await GetInfoCoreAsync(ResolveBucket(bucket), ValidateKey(key), cancellationToken) is not null;

  /// <inheritdoc/>
  public Task<StorageObjectDto?> GetInfoAsync(string key, string? bucket = null, CancellationToken cancellationToken = default) =>
    GetInfoCoreAsync(ResolveBucket(bucket), ValidateKey(key), cancellationToken);

  /// <inheritdoc/>
  public Task<Uri> GetSignedUrlAsync(
    string key,
    TimeSpan? expiration = null,
    ESignedUrlAccess access = ESignedUrlAccess.Read,
    string? bucket = null,
    CancellationToken cancellationToken = default
  ) => GetSignedUrlCoreAsync(ResolveBucket(bucket), ValidateKey(key), ResolveExpiration(expiration), access, cancellationToken);

  #endregion

  #region Protected Methods

  /// <summary>
  /// Envia o objeto ao provedor.
  /// </summary>
  /// <param name="bucket">O bucket já resolvido.</param>
  /// <param name="key">A chave já validada.</param>
  /// <param name="content">O conteúdo do objeto.</param>
  /// <param name="contentType">O tipo do conteúdo, quando informado.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Os dados do objeto enviado.</returns>
  protected abstract Task<StorageObjectDto> UploadCoreAsync(
    string bucket,
    string key,
    Stream content,
    string? contentType,
    CancellationToken cancellationToken
  );

  /// <summary>
  /// Baixa o objeto do provedor.
  /// </summary>
  /// <param name="bucket">O bucket já resolvido.</param>
  /// <param name="key">A chave já validada.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O conteúdo do objeto.</returns>
  protected abstract Task<Stream> DownloadCoreAsync(string bucket, string key, CancellationToken cancellationToken);

  /// <summary>
  /// Exclui o objeto no provedor.
  /// </summary>
  /// <param name="bucket">O bucket já resolvido.</param>
  /// <param name="key">A chave já validada.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Verdadeiro quando o objeto foi excluído; falso quando ele não existia.</returns>
  protected abstract Task<bool> DeleteCoreAsync(string bucket, string key, CancellationToken cancellationToken);

  /// <summary>
  /// Obtém os dados do objeto no provedor.
  /// </summary>
  /// <param name="bucket">O bucket já resolvido.</param>
  /// <param name="key">A chave já validada.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Os dados do objeto, ou nulo quando ele não existe.</returns>
  protected abstract Task<StorageObjectDto?> GetInfoCoreAsync(string bucket, string key, CancellationToken cancellationToken);

  /// <summary>
  /// Gera a URL assinada no provedor.
  /// </summary>
  /// <param name="bucket">O bucket já resolvido.</param>
  /// <param name="key">A chave já validada.</param>
  /// <param name="expiration">A validade já conferida.</param>
  /// <param name="access">O acesso concedido.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>A URL assinada.</returns>
  protected abstract Task<Uri> GetSignedUrlCoreAsync(
    string bucket,
    string key,
    TimeSpan expiration,
    ESignedUrlAccess access,
    CancellationToken cancellationToken
  );

  /// <summary>
  /// Remove as aspas que o provedor devolve em volta do ETag.
  /// </summary>
  /// <param name="etag">O ETag do provedor.</param>
  /// <returns>O ETag sem aspas, ou nulo.</returns>
  protected static string? NormalizeETag(string? etag) => string.IsNullOrEmpty(etag) ? null : etag.Trim('"');

  #endregion

  #region Private Methods

  /// <summary>
  /// Resolve o bucket da operação.
  /// </summary>
  /// <param name="bucket">O bucket informado na chamada.</param>
  /// <returns>O bucket informado, ou o das opções.</returns>
  /// <exception cref="InternalServerErrorException">Quando nenhum bucket foi informado nem configurado.</exception>
  private string ResolveBucket(string? bucket)
  {
    var resolved = string.IsNullOrWhiteSpace(bucket) ? options.Bucket : bucket;

    return string.IsNullOrWhiteSpace(resolved)
      ? throw new InternalServerErrorException("Storage.BucketNotConfigured")
      : resolved.Trim();
  }

  /// <summary>
  /// Valida a chave do objeto.
  /// </summary>
  /// <param name="key">A chave informada.</param>
  /// <returns>A mesma chave.</returns>
  /// <exception cref="BadRequestException">Quando a chave está em branco ou passa do tamanho máximo.</exception>
  private static string ValidateKey(string key)
  {
    if (string.IsNullOrWhiteSpace(key))
    {
      throw new BadRequestException("Storage.KeyRequired");
    }

    if (Encoding.UTF8.GetByteCount(key) > MaxKeyBytes)
    {
      throw new BadRequestException($"Storage.KeyTooLong;{MaxKeyBytes}");
    }

    return key;
  }

  /// <summary>
  /// Resolve a validade da URL assinada.
  /// </summary>
  /// <param name="expiration">A validade informada na chamada.</param>
  /// <returns>A validade informada, ou a das opções.</returns>
  /// <exception cref="BadRequestException">Quando a validade não é positiva ou passa de 7 dias.</exception>
  private TimeSpan ResolveExpiration(TimeSpan? expiration)
  {
    var resolved = expiration ?? TimeSpan.FromMinutes(options.SignedUrlExpirationMinutes);

    if (resolved <= TimeSpan.Zero || resolved > TimeSpan.FromMinutes(StorageOptions.MaxSignedUrlExpirationMinutes))
    {
      throw new BadRequestException($"Storage.SignedUrlExpirationInvalid;{StorageOptions.MaxSignedUrlExpirationMinutes}");
    }

    return resolved;
  }

  #endregion
}
