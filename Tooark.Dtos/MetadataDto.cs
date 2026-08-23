namespace Tooark.Dtos;

/// <summary>
/// Classe para metadados.
/// </summary>
/// <remarks>
/// Chave e valor são independentes: informar um deles como nulo resulta em string vazia, sem descartar o
/// outro.
/// </remarks>
/// <param name="key">Chave do metadado.</param>
/// <param name="value">Valor do metadado.</param>
public class MetadataDto(string? key, string? value)
{
  #region Properties

  /// <summary>
  /// Chave do metadado.
  /// </summary>
  public string Key { get; private set; } = key ?? string.Empty;

  /// <summary>
  /// Valor do metadado.
  /// </summary>
  public string Value { get; private set; } = value ?? string.Empty;

  #endregion
}
