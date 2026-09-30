namespace Tooark.Storage.Dtos;

/// <summary>
/// Classe que representa um objeto guardado no storage.
/// </summary>
public sealed class StorageObjectDto
{
  #region Properties

  /// <summary>
  /// Bucket onde o objeto está.
  /// </summary>
  public required string Bucket { get; init; }

  /// <summary>
  /// Chave (nome) do objeto no bucket.
  /// </summary>
  public required string Key { get; init; }

  /// <summary>
  /// Tamanho do objeto em bytes, quando conhecido.
  /// </summary>
  /// <remarks>
  /// No upload de um stream que não permite busca, o tamanho não é conhecido sem uma consulta a mais, e fica nulo.
  /// </remarks>
  public long? Size { get; init; }

  /// <summary>
  /// Tipo do conteúdo (MIME), quando informado.
  /// </summary>
  public string? ContentType { get; init; }

  /// <summary>
  /// Identificador da versão do conteúdo (ETag), sem aspas. O formato varia entre provedores.
  /// </summary>
  public string? ETag { get; init; }

  /// <summary>
  /// Data da última alteração do objeto, quando o provedor a informa.
  /// </summary>
  public DateTimeOffset? LastModified { get; init; }

  #endregion
}
