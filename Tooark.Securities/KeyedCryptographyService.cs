using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Securities;

/// <summary>
/// Serviço de criptografia AES-256 com várias chaves identificadas e rotação sem recifrar tudo de uma vez.
/// </summary>
/// <remarks>
/// Reutiliza o <see cref="CryptographyService"/>, um por chave, para o AES em si. Cada valor cifrado sai no
/// formato <c>tk1.&lt;id&gt;.&lt;payload&gt;</c>: o ponto não existe no alfabeto Base64, então um valor legado,
/// sem prefixo, nunca se confunde com um valor identificado. O algoritmo (<c>GCM</c> ou <c>CBC</c>) vale para todas
/// as chaves.
/// </remarks>
public partial class KeyedCryptographyService : IKeyedCryptographyService
{
  #region Constants

  /// <summary>
  /// Versão do formato dos valores cifrados com chave identificada.
  /// </summary>
  public const string FormatVersion = "tk1";

  /// <summary>
  /// Separador entre a versão, o identificador da chave e o payload.
  /// </summary>
  public const char Separator = '.';

  /// <summary>
  /// Tamanho da chave AES-256 em bytes.
  /// </summary>
  private const int AesKeySize = 32;

  #endregion

  #region Private Properties

  /// <summary>
  /// Serviços de criptografia por identificador de chave.
  /// </summary>
  private readonly Dictionary<string, CryptographyService> _services;

  /// <summary>
  /// Identificadores na ordem da configuração.
  /// </summary>
  private readonly string[] _keyIds;

  /// <summary>
  /// Serviço da chave legada, que abre os valores sem prefixo, quando configurada.
  /// </summary>
  private readonly CryptographyService? _legacy;

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor do serviço de criptografia com chaves identificadas.
  /// </summary>
  /// <param name="options">Opções de configuração da criptografia, com <see cref="CryptographyOptions.Keys"/> preenchido.</param>
  /// <exception cref="InternalServerErrorException">Quando as opções não estão configuradas ou as chaves são inválidas.</exception>
  public KeyedCryptographyService(IOptions<CryptographyOptions> options)
  {
    // Valida se as opções foram configuradas corretamente
    var configured = options.Value
      ?? throw new InternalServerErrorException("Options.NotConfigured");

    // Falha no startup: chave ausente, malformada ou corrente desconhecida nunca cai em outra chave
    ValidateOptions(configured);

    CurrentKeyId = configured.CurrentKeyId!;
    _keyIds = [.. configured.Keys.Keys];
    _services = new Dictionary<string, CryptographyService>(StringComparer.Ordinal);

    // Um serviço por chave, com o mesmo algoritmo
    foreach (var (keyId, key) in configured.Keys)
    {
      _services[keyId] = new CryptographyService(Microsoft.Extensions.Options.Options.Create(new CryptographyOptions
      {
        Algorithm = ConfiguredAlgorithm(configured.Algorithm),
        SecretBase64 = key
      }));
    }

    // A chave legada é opcional: só é necessária enquanto restarem valores sem prefixo
#pragma warning disable CS0618 // Secret é obsoleto: continua lido até a v5 como chave legada
    if (!string.IsNullOrWhiteSpace(configured.Secret) || !string.IsNullOrWhiteSpace(configured.SecretBase64))
    {
      _legacy = new CryptographyService(Microsoft.Extensions.Options.Options.Create(new CryptographyOptions
      {
        Algorithm = ConfiguredAlgorithm(configured.Algorithm),
        Secret = configured.Secret,
        SecretBase64 = configured.SecretBase64
      }));
    }
#pragma warning restore CS0618
  }

  #endregion

  #region Properties

  /// <inheritdoc/>
  public string CurrentKeyId { get; }

  /// <inheritdoc/>
  public IReadOnlyCollection<string> KeyIds => _keyIds;

  #endregion

  #region Methods

  /// <inheritdoc/>
  public string Encrypt(string plainText) => Encrypt(plainText, CurrentKeyId);

  /// <inheritdoc/>
  public string Encrypt(string plainText, string keyId)
  {
    // Valida o texto a criptografar
    if (plainText is null)
    {
      throw new BadRequestException("Cryptography.PlainTextNotProvided");
    }

    var service = Resolve(keyId);

    return string.Concat(FormatVersion, Separator, keyId, Separator, service.Encrypt(plainText));
  }

  /// <inheritdoc/>
  public string Decrypt(string cipherText)
  {
    // Valor identificado abre com a chave do prefixo
    if (TryParse(cipherText, out var keyId, out var payload))
    {
      return Resolve(keyId).Decrypt(payload);
    }

    // Valor legado abre com a chave única, enquanto ela estiver configurada
    if (_legacy is null)
    {
      throw new BadRequestException("Cryptography.LegacyKeyNotConfigured");
    }

    return _legacy.Decrypt(cipherText);
  }

  /// <inheritdoc/>
  public string? KeyIdOf(string cipherText) =>
    TryParse(cipherText, out var keyId, out _) ? keyId : null;

  /// <inheritdoc/>
  public bool IsCurrent(string cipherText) =>
    string.Equals(KeyIdOf(cipherText), CurrentKeyId, StringComparison.Ordinal);

  /// <inheritdoc/>
  public string ReEncrypt(string cipherText)
  {
    // Já está na chave corrente: recifrar só trocaria o nonce
    if (IsCurrent(cipherText))
    {
      return cipherText;
    }

    return Encrypt(Decrypt(cipherText), CurrentKeyId);
  }

  #endregion

  #region Internal Methods

  /// <summary>
  /// Valida as chaves identificadas das opções, para falhar no registro e não no primeiro uso.
  /// </summary>
  /// <param name="options">Opções de configuração da criptografia.</param>
  /// <exception cref="InternalServerErrorException">Quando não há chaves, um identificador ou uma chave é inválido, ou a chave corrente não existe.</exception>
  internal static void ValidateOptions(CryptographyOptions options)
  {
    // Sem chaves o serviço identificado não tem com o que cifrar
    if (options.Keys is not { Count: > 0 })
    {
      throw new InternalServerErrorException("Options.Cryptography.Keys.NotConfigured");
    }

    foreach (var (keyId, key) in options.Keys)
    {
      // O identificador vai no prefixo do valor: não pode conter o separador nem ficar vazio
      if (string.IsNullOrWhiteSpace(keyId) || !KeyIdRegex().IsMatch(keyId))
      {
        throw new InternalServerErrorException($"Options.Cryptography.KeyId.Invalid;{keyId}");
      }

      // Cada chave precisa ser AES-256: exatamente 32 bytes em Base64
      if (!IsAesKey(key))
      {
        throw new InternalServerErrorException($"Options.Cryptography.Keys.Invalid;{keyId}");
      }
    }

    // A chave corrente é obrigatória e precisa estar entre as configuradas
    if (string.IsNullOrWhiteSpace(options.CurrentKeyId))
    {
      throw new InternalServerErrorException("Options.Cryptography.CurrentKeyId.NotConfigured");
    }

    if (!options.Keys.ContainsKey(options.CurrentKeyId))
    {
      throw new InternalServerErrorException($"Options.Cryptography.CurrentKeyId.NotFound;{options.CurrentKeyId}");
    }
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Obtém o serviço de uma chave configurada.
  /// </summary>
  /// <param name="keyId">Identificador da chave.</param>
  /// <returns>O serviço da chave.</returns>
  /// <exception cref="BadRequestException">Quando a chave não está configurada.</exception>
  private CryptographyService Resolve(string keyId)
  {
    // Chave desconhecida: o valor foi cifrado com uma chave removida ou o identificador está errado
    if (string.IsNullOrWhiteSpace(keyId) || !_services.TryGetValue(keyId, out var service))
    {
      throw new BadRequestException($"Cryptography.KeyNotFound;{keyId}");
    }

    return service;
  }

  /// <summary>
  /// Separa a versão, o identificador da chave e o payload de um valor cifrado.
  /// </summary>
  /// <param name="cipherText">Texto criptografado.</param>
  /// <param name="keyId">Identificador da chave do prefixo.</param>
  /// <param name="payload">Texto cifrado em Base64, sem o prefixo.</param>
  /// <returns>Verdadeiro para um valor identificado; falso para um valor legado, sem prefixo.</returns>
  /// <exception cref="BadRequestException">Quando o texto não é fornecido ou tem um prefixo malformado.</exception>
  private static bool TryParse(string cipherText, out string keyId, out string payload)
  {
    keyId = string.Empty;
    payload = string.Empty;

    // Valida o texto a descriptografar
    if (cipherText is null)
    {
      throw new BadRequestException("Cryptography.CipherTextNotProvided");
    }

    // Sem ponto é um valor legado em Base64 puro
    if (!cipherText.Contains(Separator))
    {
      return false;
    }

    var parts = cipherText.Split(Separator);

    // Com ponto só pode ser o formato identificado, com exatamente três partes (o payload em Base64 não tem ponto);
    // qualquer outra coisa é um valor inválido
    if (parts.Length != 3 || parts[0] != FormatVersion || !KeyIdRegex().IsMatch(parts[1]) || parts[2].Length == 0)
    {
      throw new BadRequestException("Cryptography.InvalidCipherText");
    }

    keyId = parts[1];
    payload = parts[2];

    return true;
  }

  /// <summary>
  /// Indica se o texto é uma chave AES-256 válida em Base64.
  /// </summary>
  /// <param name="key">Chave em Base64.</param>
  /// <returns>Verdadeiro para um Base64 válido de exatamente 32 bytes.</returns>
  private static bool IsAesKey(string? key)
  {
    // Sem chave não há o que validar
    if (string.IsNullOrWhiteSpace(key))
    {
      return false;
    }

    try
    {
      return Convert.FromBase64String(key).Length == AesKeySize;
    }
    catch (FormatException)
    {
      return false;
    }
  }

  /// <summary>
  /// Converte o algoritmo já normalizado pelas opções no valor aceito pelo setter de <see cref="CryptographyOptions.Algorithm"/>.
  /// </summary>
  /// <param name="algorithm">Algoritmo normalizado (<c>GCM</c>, <c>CBC</c> ou <c>CBCZeroIv</c>).</param>
  /// <returns>O valor a atribuir ao setter.</returns>
  private static string ConfiguredAlgorithm(string algorithm) =>
    // O setter só reconhece o nome configurável do modo legado, não o nome interno
    algorithm == "CBCZeroIv" ? "CBCUnsafe" : algorithm;

  #endregion

  #region Regex Generated Methods

  /// <summary>
  /// Expressão do identificador de chave: letras, dígitos, hífen e sublinhado.
  /// </summary>
  [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.None, matchTimeoutMilliseconds: 250)]
  private static partial Regex KeyIdRegex();

  #endregion
}
