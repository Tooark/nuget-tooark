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
  /// É obrigatório informar <see cref="Secret"/> ou <see cref="SecretBase64"/>, a menos que <see cref="Keys"/>
  /// esteja preenchido. Quando ambos são informados, <see cref="SecretBase64"/> tem prioridade.
  /// Com <see cref="Keys"/>, esta chave passa a ser a chave legada: só abre os valores gravados sem prefixo de
  /// chave, e pode ser removida quando não restar nenhum deles.
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
  /// Com <see cref="Keys"/>, vale o mesmo papel de chave legada descrito em <see cref="Secret"/>.
  /// </remarks>
  public string? SecretBase64 { get; set; } = null;

  /// <summary>
  /// Identificador da chave corrente em <see cref="Keys"/>, usada para cifrar os valores novos.
  /// </summary>
  /// <remarks>
  /// Obrigatório quando <see cref="Keys"/> está preenchido, e precisa ser um dos identificadores da coleção.
  /// A rotação de chave é trocar este valor: os valores antigos continuam abrindo com a chave que os cifrou.
  /// </remarks>
  public string? CurrentKeyId { get; set; } = null;

  /// <summary>
  /// Chaves de criptografia por identificador, para cifrar com várias chaves e rotacionar sem recifrar tudo de uma vez.
  /// </summary>
  /// <remarks>
  /// Cada entrada mapeia um identificador (letras, dígitos, hífen e sublinhado, sem ponto) para uma chave AES-256
  /// em Base64, de exatamente 32 bytes. Quando preenchido, o pacote registra o
  /// <see cref="Interfaces.IKeyedCryptographyService"/>, que grava os valores no formato
  /// <c>tk1.&lt;id&gt;.&lt;payload&gt;</c>; vazio, vale o <see cref="Interfaces.ICryptographyService"/> de chave
  /// única com <see cref="Secret"/> ou <see cref="SecretBase64"/>. Remover uma chave torna ilegíveis os valores
  /// cifrados com ela.
  /// </remarks>
  public Dictionary<string, string> Keys { get; set; } = [];

  #endregion
}
