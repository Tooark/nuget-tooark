using Tooark.Exceptions;

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
  ///   - "CBCUnsafe" que representa o modo AES-256-CBC com IV zerado (legado, somente descriptografia)
  /// Ausente, vazio ou só com espaços vale "GCM". Qualquer outro valor lança exceção: em configuração de
  /// segurança, um algoritmo digitado errado nunca deve ser substituído em silêncio por outro. Na linha 4.x um
  /// valor desconhecido caía em "GCM".
  /// </remarks>
  /// <exception cref="InternalServerErrorException">Quando o valor não corresponde a um algoritmo suportado.</exception>
  public string Algorithm
  {
    get => _algorithm;
    set => _algorithm = value?.Trim().ToUpperInvariant() switch
    {
      null or "" => "GCM",
      "CBC" => "CBC",
      "GCM" => "GCM",
      "CBCUNSAFE" => "CBCZeroIv",
      _ => throw new InternalServerErrorException($"Options.Cryptography.AlgorithmNotSupported;{value}")
    };
  }

  /// <summary>
  /// Chave secreta em Base64 para algoritmos simétricos.
  /// </summary>
  /// <remarks>
  /// Obrigatória, a menos que <see cref="Keys"/> esteja preenchido. É usada diretamente como chave simétrica e
  /// deve representar exatamente 32 bytes (AES-256); valores inválidos falham no startup. Gere uma chave
  /// aleatória com <c>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))</c>.
  /// Com <see cref="Keys"/>, esta chave passa a ser a chave legada: só abre os valores gravados sem prefixo de
  /// chave, e pode ser removida quando não restar nenhum deles.
  /// <para>
  /// Na linha 4.x existia também o <c>Secret</c> em texto, removido na v5. A chave que ele derivava é o SHA-256
  /// do texto: <c>Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret)))</c> informado aqui abre
  /// os mesmos valores, sem recifrar nada.
  /// </para>
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
  /// única com <see cref="SecretBase64"/>. Remover uma chave torna ilegíveis os valores
  /// cifrados com ela.
  /// </remarks>
  public Dictionary<string, string> Keys { get; set; } = [];

  #endregion
}
