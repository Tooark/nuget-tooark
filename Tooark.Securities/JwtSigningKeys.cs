using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Tooark.Exceptions;
using Tooark.Securities.Options;

namespace Tooark.Securities;

/// <summary>
/// Chaves de assinatura e de validação carregadas das opções do JWT.
/// </summary>
/// <remarks>
/// Centraliza a leitura das chaves para que o <see cref="JwtTokenService"/> e o esquema de autenticação
/// <c>AddTooarkJwtBearer</c> apliquem as mesmas regras: algoritmo, tamanho mínimo e curva.
/// </remarks>
internal sealed class JwtSigningKeys
{
  #region Constructors

  /// <summary>
  /// Construtor das chaves carregadas.
  /// </summary>
  /// <param name="algorithm">Algoritmo de assinatura.</param>
  /// <param name="createKey">Chave de assinatura, quando configurada.</param>
  /// <param name="validationKey">Chave de validação, quando configurada.</param>
  private JwtSigningKeys(string algorithm, SecurityKey? createKey, SecurityKey? validationKey)
  {
    Algorithm = algorithm;
    CreateKey = createKey;
    ValidationKey = validationKey;
  }

  #endregion

  #region Properties

  /// <summary>
  /// Algoritmo de assinatura (ex: HS256, RS256).
  /// </summary>
  public string Algorithm { get; }

  /// <summary>
  /// Chave de segurança para criação de tokens, ou nula quando a chave privada não foi configurada.
  /// </summary>
  public SecurityKey? CreateKey { get; }

  /// <summary>
  /// Chave de segurança para validação de tokens, ou nula quando a chave pública não foi configurada.
  /// </summary>
  public SecurityKey? ValidationKey { get; }

  /// <summary>
  /// Indica se o token pode ser criado.
  /// </summary>
  public bool CanCreate => CreateKey is not null;

  /// <summary>
  /// Indica se o token pode ser validado.
  /// </summary>
  public bool CanValidate => ValidationKey is not null;

  #endregion

  #region Methods

  /// <summary>
  /// Carrega as chaves das opções, falhando no startup quando a configuração é inválida.
  /// </summary>
  /// <param name="options">Opções de configuração do JWT.</param>
  /// <param name="logger">Logger para registrar a falha de carga de chave, opcional.</param>
  /// <returns>As chaves carregadas.</returns>
  /// <exception cref="InternalServerErrorException">Quando a chave secreta não está configurada para algoritmos simétricos.</exception>
  /// <exception cref="InternalServerErrorException">Quando as chaves pública/privada não estão configuradas para algoritmos assimétricos.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave RSA tem tamanho inválido.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave ECDsa tem curva inválida.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave é inválida ou o algoritmo não é suportado.</exception>
  public static JwtSigningKeys Load(JwtOptions options, ILogger? logger = null)
  {
    // Suporte a algoritmo e chave simétrica ou assimétrica
    var algorithm = options.Algorithm;

    // Verifica se o algoritmo é simétrico ou assimétrico
    if (algorithm.StartsWith("HS")) // HMAC (simétrico)
    {
      var secret = options.Secret ?? throw new InternalServerErrorException("Options.Jwt.SecretNotConfigured");
      var keyBytes = Encoding.UTF8.GetBytes(secret);

      // RFC 7518: a chave HMAC deve ter ao menos o tamanho da saída do hash (HS256: 32, HS384: 48, HS512: 64 bytes)
      var minKeyBytes = algorithm switch
      {
        "HS384" => 48,
        "HS512" => 64,
        _ => 32
      };

      // Valida o tamanho mínimo no startup (secrets curtos tornam a chave HMAC forçável e falhariam em runtime com erro obscuro)
      if (keyBytes.Length < minKeyBytes)
      {
        throw new InternalServerErrorException($"Options.Jwt.SecretTooShort;{minKeyBytes}");
      }

      var symmetricKey = new SymmetricSecurityKey(keyBytes);

      return new JwtSigningKeys(algorithm, symmetricKey, symmetricKey);
    }

    // Verifica se a chave privada existe para permitir criação de token
    var createToken = !string.IsNullOrWhiteSpace(options.PrivateKey);

    // Valida se a chave pública existe para permitir validação de token
    var validateToken = !string.IsNullOrWhiteSpace(options.PublicKey);

    // Verifica se ao menos uma chave está configurada (fail-fast para todas as famílias assimétricas: RSA, PSS e ECDsa)
    if (!createToken && !validateToken)
    {
      throw new InternalServerErrorException("Options.Jwt.KeysNotConfigured");
    }

    // Carrega chaves pública e privada, caso não exista a privada, usa a pública
    var privateKeyBytes = createToken ? Convert.FromBase64String(options.PrivateKey!) : null;
    var publicKeyBytes = validateToken ? Convert.FromBase64String(options.PublicKey!) : null;

    SecurityKey? createKey = null;
    SecurityKey? validationKey = null;

    // Configura chaves de assinatura e validação de chave assimétrica
    if (algorithm.StartsWith("RS") || algorithm.StartsWith("PS")) // RSA ou PSS (assimétrico)
    {
      try
      {
        // Verifica se deve criar a chave de criação
        if (privateKeyBytes != null)
        {
          // Carrega chave privada
          var privateRsa = RSA.Create();
          privateRsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

          // Valida tamanho mínimo da chave RSA
          if (privateRsa.KeySize < 2048)
          {
            throw new InternalServerErrorException("Options.Jwt.PrivateKey.InvalidSize");
          }

          // Configura chave de criação
          createKey = new RsaSecurityKey(privateRsa);
        }

        // Verifica se deve criar a chave de validação
        if (publicKeyBytes != null)
        {
          // Carrega chave pública
          var publicRsa = RSA.Create();
          publicRsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

          // Valida tamanho mínimo da chave RSA
          if (publicRsa.KeySize < 2048)
          {
            throw new InternalServerErrorException("Options.Jwt.PublicKey.InvalidSize");
          }

          // Configura chave de validação
          validationKey = new RsaSecurityKey(publicRsa);
        }
      }
      catch (CryptographicException ex)
      {
        logger?.LogError("Error loading RSA key into JWT.\n{exception}", ex);

        throw new InternalServerErrorException("Options.Jwt.InvalidKey");
      }
    }
    else if (algorithm.StartsWith("ES")) // ECDsa (assimétrico)
    {
      try
      {
        // Resolve o tamanho da chave ECDsa conforme o algoritmo (ES256/ES384/ES512)
        var keySize = int.TryParse(algorithm.Split("ES")[1], out int ks) ? ks : 0;

        // Verifica se deve criar a chave de criação
        if (privateKeyBytes != null)
        {
          // Carrega chave privada
          var privateEcdsa = ECDsa.Create();
          privateEcdsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

          // Valida curva da chave ECDsa conforme o algoritmo
          if (privateEcdsa.KeySize < keySize)
          {
            throw new InternalServerErrorException("Options.Jwt.PrivateKey.InvalidCurve");
          }

          // Configura chave de criação
          createKey = new ECDsaSecurityKey(privateEcdsa);
        }

        // Verifica se deve criar a chave de validação
        if (publicKeyBytes != null)
        {
          // Carrega chave pública
          var publicEcdsa = ECDsa.Create();
          publicEcdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

          // Valida curva da chave ECDsa conforme o algoritmo
          if (publicEcdsa.KeySize < keySize)
          {
            throw new InternalServerErrorException("Options.Jwt.PublicKey.InvalidCurve");
          }

          // Configura chave de validação
          validationKey = new ECDsaSecurityKey(publicEcdsa);
        }
      }
      catch (CryptographicException ex)
      {
        logger?.LogError("Error loading ECDsa key into JWT.\n{exception}", ex);

        throw new InternalServerErrorException("Options.Jwt.InvalidKey");
      }
    }
    else
    {
      throw new InternalServerErrorException("Options.Jwt.AlgorithmNotSupported");
    }

    return new JwtSigningKeys(algorithm, createKey, validationKey);
  }

  #endregion
}
