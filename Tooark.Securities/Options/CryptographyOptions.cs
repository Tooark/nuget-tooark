namespace Tooark.Securities.Options;

/// <summary>
/// Classe que representa as opções de configuração para criptografia.
/// </summary>
public class CryptographyOptions
{
  #region Section

  /// <summary>
  /// Seção de configuração das opções de criptografia.
  /// </summary>
  public const string Section = "Cryptography";

  #endregion

  #region Private Properties

  /// <summary>
  /// Algoritmo de criptografia a ser utilizado.
  /// </summary>
  private string _algorithm = null!;

  #endregion

  #region Properties

  /// <summary>
  /// Algoritmo de criptografia a ser utilizado.
  /// </summary>
  /// <remarks>
  /// Valores válidos (case insensitive):
  ///   - "CBC"  que representa o modo AES-256-CBC
  ///   - "GCM"  que representa o modo AES-256-GCM
  ///   - "CBCUnsafe" que representa o modo AES-256-CBC com IV zerado (legado)
  /// </remarks>
  public string Algorithm
  {
    get => _algorithm;
    set => _algorithm = value?.ToUpperInvariant() switch
    {
      "CBC" => "CBC",
      "GCM" => "GCM",
      "CBCUNSAFE" => "CBCZeroIv",
      _ => "GCM"
    } ?? "GCM";
  }

  /// <summary>
  /// Chave secreta para algoritmos simétricos.
  /// </summary>
  /// <remarks>
  /// É obrigatório informar <see cref="Secret"/> ou <see cref="SecretBase64"/>.
  /// Quando ambos são informados, <see cref="SecretBase64"/> tem prioridade.
  /// </remarks>
  public string? Secret { get; set; } = null;

  /// <summary>
  /// Chave secreta em Base64 para algoritmos simétricos.
  /// </summary>
  /// <remarks>
  /// Se informada, será usada diretamente como chave simétrica e deve representar
  /// exatamente 32 bytes (AES-256); valores inválidos falham no startup.
  /// É a opção recomendada: gere uma chave aleatória com
  /// <c>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))</c>.
  /// Quando <see cref="Secret"/> também é informado, esta chave tem prioridade.
  /// </remarks>
  public string? SecretBase64 { get; set; } = null;

  #endregion
}
