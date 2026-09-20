using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Securities;

/// <summary>
/// Serviço de criptografia para criptografar e descriptografar dados sensíveis.
/// </summary>
public class CryptographyService : ICryptographyService
{
  #region Private Properties

  /// <summary>
  /// Opções de configuração de criptografia.
  /// </summary>
  private readonly string _algorithm;

  /// <summary>
  /// Segredo para derivação da chave (usado quando SecretBase64 não é informado).
  /// </summary>
  private readonly string? _secret;

  /// <summary>
  /// Chave AES binária opcional (quando fornecida em Base64 na configuração).
  /// </summary>
  private readonly byte[]? _aesKey;

  /// <summary>
  /// Tamanho da chave AES-256 em bytes.
  /// </summary>
  private const int AesKeySize = 32; // 256 bits

  /// <summary>
  /// Tamanho do vetor de inicialização (IV) para CBC.
  /// </summary>
  private const int IvSize = 16; // 128 bits

  /// <summary>
  /// Tamanho do vetor de inicialização (Nonce/IV) para GCM.
  /// </summary>
  private const int GcmNonceSize = 12; // 96 bits (recomendado)

  /// <summary>
  /// Tamanho da tag de autenticação para GCM.
  /// </summary>
  private const int GcmTagSize = 16;   // 128 bits

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor do serviço de criptografia.
  /// </summary>
  /// <param name="options">Opções de configuração de criptografia.</param>
  /// <exception cref="InternalServerErrorException">Quando as opções não estão configuradas.</exception>
  /// <exception cref="InternalServerErrorException">Quando nem Secret nem SecretBase64 estão configurados.</exception>
  /// <exception cref="InternalServerErrorException">Quando SecretBase64 não é um Base64 válido.</exception>
  /// <exception cref="InternalServerErrorException">Quando SecretBase64 não representa uma chave de 32 bytes (AES-256).</exception>
  public CryptographyService(IOptions<CryptographyOptions> options)
  {
    // Valida se as opções foram configuradas corretamente
    var _options = options.Value
      ?? throw new InternalServerErrorException("Options.NotConfigured");

    _algorithm = _options.Algorithm;
    _secret = _options.Secret;

    // SecretBase64 tem prioridade quando informado; validado no startup (fail-fast, nunca troca de chave silenciosamente)
    if (!string.IsNullOrWhiteSpace(_options.SecretBase64))
    {
      byte[] keyBytes;

      try
      {
        keyBytes = Convert.FromBase64String(_options.SecretBase64);
      }
      catch (FormatException)
      {
        throw new InternalServerErrorException("Options.Cryptography.SecretBase64Invalid");
      }

      // AES-256 exige chave de exatamente 32 bytes
      if (keyBytes.Length != AesKeySize)
      {
        throw new InternalServerErrorException("Options.Cryptography.SecretBase64InvalidSize");
      }

      _aesKey = keyBytes;
    }
    else if (string.IsNullOrWhiteSpace(_secret))
    {
      // Exige ao menos uma das chaves: Secret ou SecretBase64
      throw new InternalServerErrorException("Options.Cryptography.SecretNotConfigured");
    }
  }

  #endregion

  #region  Methods

  /// <summary>
  /// Criptografa um texto usando o algoritmo configurado.
  /// </summary>
  /// <remarks>
  /// Suporta os algoritmos:
  /// - AES-256-CBC (Opção "CBC")
  /// - AES-256-GCM (Opção "GCM", padrão)
  /// </remarks>
  /// <param name="plainText">Texto plano para criptografar.</param>
  /// <returns>Texto criptografado em Base64.</returns>
  /// <exception cref="BadRequestException">Quando o texto plano não é fornecido.</exception>
  /// <exception cref="InternalServerErrorException">Quando o algoritmo configurado é apenas para descriptografia (CBCUnsafe).</exception>
  public string Encrypt(string plainText)
  {
    // Valida o texto a criptografar
    if (plainText is null)
    {
      throw new BadRequestException("Cryptography.PlainTextNotProvided");
    }

    // Seleciona o algoritmo de criptografia
    // CBCZeroIv (CBCUnsafe) é apenas para leitura de dados legados: criptografar com IV zero seria inseguro
    return _algorithm switch
    {
      "CBC" => EncryptCbc(plainText),
      "GCM" => EncryptGcm(plainText),
      "CBCZeroIv" => throw new InternalServerErrorException("Options.Cryptography.AlgorithmDecryptOnly;CBCUnsafe"),
      _ => EncryptGcm(plainText)
    };
  }

  /// <summary>
  /// Descriptografa um texto usando o algoritmo configurado.
  /// </summary>
  /// <remarks>
  /// Suporta os algoritmos:
  /// - AES-256-CBC (Opção "CBC")
  /// - AES-256-GCM (Opção "GCM", padrão)
  /// </remarks>
  /// <param name="cipherText">Texto criptografado em Base64.</param>
  /// <returns>Texto plano descriptografado.</returns>
  /// <exception cref="BadRequestException">Quando o texto criptografado não é fornecido.</exception>
  /// <exception cref="BadRequestException">Quando o texto criptografado é inválido.</exception>
  public string Decrypt(string cipherText)
  {
    // Valida o texto a descriptografar
    if (cipherText is null)
    {
      throw new BadRequestException("Cryptography.CipherTextNotProvided");
    }

    try
    {
      // Seleciona o algoritmo de descriptografia
      return _algorithm switch
      {
        "CBC" => DecryptCbc(cipherText),
        "GCM" => DecryptGcm(cipherText),
        "CBCZeroIv" => DecryptCbcWithZeroIv(cipherText),
        _ => DecryptGcm(cipherText)
      };
    }
    catch (FormatException)
    {
      // Base64 inválido: uniformiza como texto criptografado inválido
      throw new BadRequestException("Cryptography.InvalidCipherText");
    }
    catch (CryptographicException)
    {
      // Padding/tag inválidos: mensagem única evita distinção de erros (superfície de padding oracle)
      throw new BadRequestException("Cryptography.InvalidCipherText");
    }
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Criptografa um texto usando AES-256-CBC e retorna em Base64.
  /// </summary>
  /// <remarks>
  /// Formato do payload (bytes antes de ser convertido para Base64):
  ///   [16 bytes IV][N bytes CipherText]
  /// </remarks>
  /// <param name="plainText">Texto plano para criptografar.</param>
  /// <returns>Texto criptografado em Base64.</returns>
  private string EncryptCbc(string plainText)
  {
    // Cria e configura o objeto AES para criptografia
    using var aes = Aes.Create();
    aes.KeySize = 256;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    aes.Key = GetSymmetricKey();
    aes.GenerateIV();

    // Gera um vetor de inicialização (IV) aleatório para cada criptografia
    using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

    // Converte o texto plano em bytes
    var plainBytes = Encoding.UTF8.GetBytes(plainText);

    // Realiza a criptografia
    var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

    // concatena IV + dados criptografados para poder usar IV dinâmico
    var result = new byte[aes.IV.Length + cipherBytes.Length];
    Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
    Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

    // Converte o resultado para Base64
    return Convert.ToBase64String(result);
  }

  /// <summary>
  /// Criptografa um texto usando AES-256-GCM (criptografia autenticada) e retorna em Base64.
  /// </summary>
  /// <remarks>
  /// Formato do payload (bytes antes de ser convertido para Base64):
  ///   [12 bytes Nonce][16 bytes Tag][N bytes CipherText]
  /// </remarks>
  /// <param name="plainText">Texto plano para criptografar.</param>
  /// <returns>Texto criptografado em Base64.</returns>
  private string EncryptGcm(string plainText)
  {
    // Gera um nonce (vetor de inicialização) aleatório, tag de autenticação, chave a partir do segredo
    var nonce = RandomNumberGenerator.GetBytes(GcmNonceSize);
    var tag = new byte[GcmTagSize];
    var key = GetSymmetricKey();

    // Converte o texto plano em bytes e prepara o buffer para os dados criptografados
    var plainBytes = Encoding.UTF8.GetBytes(plainText);
    var cipherBytes = new byte[plainBytes.Length];

    // Realiza a criptografia usando AES-GCM
    using (var aesGcm = new AesGcm(key, GcmTagSize))
    {
      aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag, associatedData: null);
    }

    // Concatena Nonce + Tag + Dados criptografados
    var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];

    // Copia os componentes para o array resultante, tag de autenticação e dados criptografados
    Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
    Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
    Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

    // Converte o resultado para Base64
    return Convert.ToBase64String(result);
  }

  /// <summary>
  /// Descriptografa um texto em Base64 usando AES-256-CBC.
  /// </summary>
  /// <param name="cipherText">Texto criptografado em Base64.</param>
  /// <returns>Texto plano descriptografado.</returns>
  /// <exception cref="BadRequestException">Lançada quando o texto criptografado é inválido.</exception>
  private string DecryptCbc(string cipherText)
  {
    // Converte o texto Base64 de volta para bytes
    var allBytes = Convert.FromBase64String(cipherText);

    // Valida o tamanho do array de bytes
    if (allBytes.Length <= IvSize)
    {
      throw new BadRequestException("Cryptography.InvalidCipherText");
    }

    // Extrai o IV e os dados criptografados
    var iv = new byte[IvSize];
    var cipherBytes = new byte[allBytes.Length - IvSize];

    // Copia o IV e os dados criptografados para os arrays correspondentes
    Buffer.BlockCopy(allBytes, 0, iv, 0, IvSize);
    Buffer.BlockCopy(allBytes, IvSize, cipherBytes, 0, cipherBytes.Length);

    // Cria e configura o objeto AES para descriptografia
    using var aes = Aes.Create();
    aes.KeySize = 256;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    aes.Key = GetSymmetricKey();
    aes.IV = iv;

    // Cria o descriptografador e realiza a descriptografia
    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
    var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

    // Converte os bytes descriptografados de volta para string
    return Encoding.UTF8.GetString(plainBytes);
  }

  /// <summary>
  /// Descriptografa um texto em Base64 usando AES-256-GCM.
  /// </summary>
  /// <param name="cipherText">Texto criptografado em Base64.</param>
  /// <returns>Texto plano descriptografado.</returns>
  /// <exception cref="BadRequestException">Lançada quando o texto criptografado é inválido.</exception>
  private string DecryptGcm(string cipherText)
  {
    // Converte o texto Base64 de volta para bytes
    var allBytes = Convert.FromBase64String(cipherText);

    // Valida o tamanho do array de bytes
    if (allBytes.Length < GcmNonceSize + GcmTagSize)
    {
      throw new BadRequestException("Cryptography.InvalidCipherText");
    }

    // Extrai o nonce, a tag de autenticação e os dados criptografados
    var nonce = new byte[GcmNonceSize];
    var tag = new byte[GcmTagSize];
    var key = GetSymmetricKey();
    var cipherBytes = new byte[allBytes.Length - GcmNonceSize - GcmTagSize];

    // Copia os componentes para os arrays correspondentes
    Buffer.BlockCopy(allBytes, 0, nonce, 0, GcmNonceSize);
    Buffer.BlockCopy(allBytes, GcmNonceSize, tag, 0, GcmTagSize);
    Buffer.BlockCopy(allBytes, GcmNonceSize + GcmTagSize, cipherBytes, 0, cipherBytes.Length);

    // Prepara o buffer para os dados descriptografados
    var plainBytes = new byte[cipherBytes.Length];

    // Realiza a descriptografia usando AES-GCM
    using (var aesGcm = new AesGcm(key, GcmTagSize))
    {
      aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes, associatedData: null);
    }

    // Converte os bytes descriptografados de volta para string
    return Encoding.UTF8.GetString(plainBytes);
  }

  /// <summary>
  /// Descriptografa um texto em Base64 usando AES-256-CBC com IV zero (legado).
  /// </summary>
  /// <remarks>
  /// Este método existe apenas para compatibilidade com dados legados
  /// criptografados em AES-256-CBC usando IV fixo igual a zero e sem
  /// prefixar o IV no payload. Para novos dados, utilize <see cref="Decrypt"/>
  /// com Algorithm = "CBC".
  /// </remarks>
  /// <param name="cipherText">Texto criptografado em Base64 (sem IV prefixado).</param>
  /// <returns>Texto plano descriptografado.</returns>
  private string DecryptCbcWithZeroIv(string cipherText)
  {
    // Converte o texto Base64 legado diretamente para bytes criptografados
    var cipherBytes = Convert.FromBase64String(cipherText);

    // Cria e configura o objeto AES para descriptografia com IV zero
    using var aes = Aes.Create();
    aes.KeySize = 256;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    aes.Key = GetSymmetricKey();
    aes.IV = new byte[IvSize]; // IV zero (dados legados)

    // Cria o descriptografador e realiza a descriptografia
    using var ms = new MemoryStream(cipherBytes);
    using var cs = new CryptoStream(ms, aes.CreateDecryptor(aes.Key, aes.IV), CryptoStreamMode.Read);
    using var sr = new StreamReader(cs, Encoding.UTF8);

    // Lê e retorna o texto plano descriptografado
    return sr.ReadToEnd();
  }

  /// <summary>
  /// Deriva uma chave de 32 bytes (256 bits) a partir do segredo fornecido.
  /// </summary>
  /// <param name="secret">Segredo para derivação da chave.</param>
  /// <returns>Chave derivada de 32 bytes.</returns>
  private static byte[] DeriveKey(string secret)
  {
    // Garante 32 bytes (256 bits) a partir do secret usando SHA256.
    return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
  }

  /// <summary>
  /// Obtém a chave simétrica a ser usada (AES-256).
  /// </summary>
  /// <remarks>
  /// A chave Base64 (validada no construtor) tem prioridade; caso contrário,
  /// a chave é derivada a partir do segredo usando SHA256. O construtor garante
  /// que ao menos uma das duas fontes de chave está configurada.
  /// </remarks>
  private byte[] GetSymmetricKey()
  {
    return _aesKey ?? DeriveKey(_secret!);
  }

  #endregion
}
