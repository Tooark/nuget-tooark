using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tooark.Exceptions;
using Tooark.Securities.Dtos;
using Tooark.Securities.Extensions;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Securities;

/// <summary>
/// Serviço para manipulação de tokens JWT.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
  #region Private Static Properties

  /// <summary>
  /// Handler de tokens JWT (JsonWebTokenHandler, thread-safe e reutilizável).
  /// </summary>
  private static readonly JsonWebTokenHandler _tokenHandler = new();

  #endregion

  #region Private Properties

  /// <summary>
  /// Logger do serviço.
  /// </summary>
  private readonly ILogger<JwtTokenService> _logger;

  /// <summary>
  /// Opções de configuração do JWT.
  /// </summary>
  private readonly JwtOptions _jwtOptions;

  /// <summary>
  /// Algoritmo de assinatura utilizado (ex: HS256, RS256).
  /// </summary>
  private readonly string _algorithm;

  /// <summary>
  /// Chave de segurança para criação de tokens.
  /// </summary>
  private readonly SecurityKey? _createKey;

  /// <summary>
  /// Chave de segurança para validação de tokens.
  /// </summary>
  private readonly SecurityKey? _validationKey;

  /// <summary>
  /// Indica se o token pode ser criado.
  /// </summary>
  private readonly bool _createToken = false;

  /// <summary>
  /// Indica se o token pode ser validado.
  /// </summary>
  private readonly bool _validateToken = false;

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor do serviço de token JWT.
  /// </summary>
  /// <param name="jwtOptions">Opções de configuração do JWT.</param>
  /// <param name="logger">Logger do serviço. Opcional: quando não fornecido, usa um logger nulo interno,
  /// sem registrar fallbacks globais de logging no container.</param>
  /// <exception cref="InternalServerErrorException">Quando as opções não estão configuradas.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave secreta não está configurada para algoritmos simétricos.</exception>
  /// <exception cref="InternalServerErrorException">Quando as chaves pública/privada não estão configuradas para algoritmos assimétricos.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave RSA tem tamanho inválido.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave ECDsa tem curva inválida.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave é inválida ou o algoritmo não é suportado.</exception>
  public JwtTokenService(IOptions<JwtOptions> jwtOptions, ILogger<JwtTokenService>? logger = null)
  {
    // Configura o logger (fallback interno para NullLogger quando logging não está configurado)
    _logger = logger ?? NullLogger<JwtTokenService>.Instance;

    // Valida se as opções foram configuradas corretamente
    _jwtOptions = jwtOptions.Value
      ?? throw new InternalServerErrorException("Options.NotConfigured");

    // Suporte a algoritmo e chave simétrica ou assimétrica
    _algorithm = _jwtOptions.Algorithm;

    // Verifica se o algoritmo é simétrico ou assimétrico
    if (_algorithm.StartsWith("HS")) // HMAC (simétrico)
    {
      var secret = _jwtOptions.Secret ?? throw new InternalServerErrorException("Options.Jwt.SecretNotConfigured");
      var keyBytes = Encoding.UTF8.GetBytes(secret);

      // RFC 7518: a chave HMAC deve ter ao menos o tamanho da saída do hash (HS256: 32, HS384: 48, HS512: 64 bytes)
      var minKeyBytes = _algorithm switch
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

      _createKey = new SymmetricSecurityKey(keyBytes);
      _validationKey = _createKey;

      _createToken = true;
      _validateToken = true;
    }
    else
    {
      // Verifica se a chave privada existe para permitir criação de token
      if (!string.IsNullOrWhiteSpace(_jwtOptions.PrivateKey))
      {
        _createToken = true;
      }

      // Valida se a chave pública existe para permitir validação de token
      if (!string.IsNullOrWhiteSpace(_jwtOptions.PublicKey))
      {
        _validateToken = true;
      }

      // Verifica se ao menos uma chave está configurada (fail-fast para todas as famílias assimétricas: RSA, PSS e ECDsa)
      if (!_createToken && !_validateToken)
      {
        throw new InternalServerErrorException("Options.Jwt.KeysNotConfigured");
      }

      // Carrega chaves pública e privada, caso não exista a privada, usa a pública
      var privateKeyBytes = _createToken ? Convert.FromBase64String(_jwtOptions.PrivateKey!) : null;
      var publicKeyBytes = _validateToken ? Convert.FromBase64String(_jwtOptions.PublicKey!) : null;

      // Configura chaves de assinatura e validação de chave assimétrica
      if (_algorithm.StartsWith("RS") || _algorithm.StartsWith("PS")) // RSA ou PSS (assimétrico)
      {
        try
        {
          // Verifica se deve criar a chave de criação
          if (_createToken && privateKeyBytes != null)
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
            _createKey = new RsaSecurityKey(privateRsa);
          }

          // Verifica se deve criar a chave de validação
          if (_validateToken && publicKeyBytes != null)
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
            _validationKey = new RsaSecurityKey(publicRsa);
          }
        }
        catch (CryptographicException ex)
        {
          _logger.LogError("Error loading RSA key into JWT.\n{exception}", ex);

          throw new InternalServerErrorException("Options.Jwt.InvalidKey");
        }
      }
      else if (_algorithm.StartsWith("ES")) // ECDsa (assimétrico)
      {
        try
        {
          // Resolve o tamanho da chave ECDsa conforme o algoritmo (ES256/ES384/ES512)
          var keySize = int.TryParse(_algorithm.Split("ES")[1], out int ks) ? ks : 0;

          // Verifica se deve criar a chave de criação
          if (_createToken && privateKeyBytes != null)
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
            _createKey = new ECDsaSecurityKey(privateEcdsa);
          }

          // Verifica se deve criar a chave de validação
          if (_validateToken && publicKeyBytes != null)
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
            _validationKey = new ECDsaSecurityKey(publicEcdsa);
          }
        }
        catch (CryptographicException ex)
        {
          _logger.LogError("Error loading ECDsa key into JWT.\n{exception}", ex);

          throw new InternalServerErrorException("Options.Jwt.InvalidKey");
        }
      }
      else
      {
        throw new InternalServerErrorException("Options.Jwt.AlgorithmNotSupported");
      }
    }
  }

  #endregion

  #region Methods

  /// <inheritdoc/>
  public string Create(JwtTokenDto data, string? audience = null, IEnumerable<Claim>? extraClaims = null)
  {
    // Valida se pode criar o token
    if (!_createToken || _createKey == null)
    {
      throw new InternalServerErrorException("Options.Jwt.KeyNotConfigured;PrivateKey");
    }

    // Configura propriedades do token
    var expiryTime = _jwtOptions.ExpirationTime;
    var baseClaims = data.GetClaims();

    // Adiciona claims extras, se fornecidos
    var claims = extraClaims is null
    ? baseClaims
    : baseClaims.Concat(extraClaims);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Expires = DateTime.UtcNow.AddMinutes(expiryTime),
      SigningCredentials = new SigningCredentials(_createKey, _algorithm),
      Subject = new ClaimsIdentity(claims)
    };

    // Define o público do token, se fornecido, caso contrário usa o padrão do Options
    var targetAudience = !string.IsNullOrWhiteSpace(audience)
    ? audience
    : _jwtOptions.Audience;

    // Define o público, se configurados
    if (!string.IsNullOrWhiteSpace(targetAudience))
    {
      tokenDescriptor.Audience = targetAudience;
    }

    // Define o emissor, se configurado
    if (!string.IsNullOrWhiteSpace(_jwtOptions.Issuer))
    {
      tokenDescriptor.Issuer = _jwtOptions.Issuer;
    }

    // Cria e retorna o token JWT (JsonWebTokenHandler retorna a string diretamente)
    return _tokenHandler.CreateToken(tokenDescriptor);
  }

  /// <inheritdoc/>
  public async Task<UserTokenDto> ValidateAsync(string token, string? audience = null)
  {
    // Valida se pode validar o token
    if (!_validateToken || _validationKey == null)
    {
      throw new InternalServerErrorException("Options.Jwt.KeyNotConfigured;PublicKey");
    }

    try
    {
      // Valida o token: o handler expõe a validação apenas de forma assíncrona
      var result = await _tokenHandler
        .ValidateTokenAsync(token, BuildValidationParameters(audience))
        .ConfigureAwait(false);

      return MapValidationResult(result);
    }
    catch (Exception ex)
    {
      _logger.LogError("Error validating JWT token.\nException: {exception}", ex);

      return new UserTokenDto("InternalServerError");
    }
  }

  /// <inheritdoc/>
  public UserTokenDto Validate(string token, string? audience = null)
  {
    // Espera bloqueante sobre o caminho assíncrono: com chave de assinatura estática a validação é CPU-bound
    // e a tarefa já vem concluída. Em fluxos assíncronos, prefira ValidateAsync.
    return ValidateAsync(token, audience).GetAwaiter().GetResult();
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Monta os parâmetros de validação do token a partir das opções e do destinatário informado.
  /// </summary>
  /// <param name="audience">Destinatário do token que sobrescreve o destinatário padrão do Options.</param>
  /// <returns>Parâmetros de validação do token.</returns>
  private TokenValidationParameters BuildValidationParameters(string? audience)
  {
    // Define issuer e audiences efetivos com base nas opções e parâmetros
    var effectiveIssuer = _jwtOptions.Issuer;
    var effectiveIssuers = _jwtOptions.Issuers;

    // Se um audience for informado no parâmetro, ele tem prioridade sobre as opções
    var effectiveAudience = !string.IsNullOrWhiteSpace(audience)
      ? audience
      : _jwtOptions.Audience;
    var effectiveAudiences = !string.IsNullOrWhiteSpace(audience)
      ? [audience]
      : _jwtOptions.Audiences;

    // Verifica se deve validar issuer/audience com base em qualquer configuração disponível
    var issuers = !string.IsNullOrWhiteSpace(effectiveIssuer) || effectiveIssuers?.Length > 0;
    var audiences = !string.IsNullOrWhiteSpace(effectiveAudience) || effectiveAudiences?.Length > 0;

    return new TokenValidationParameters
    {
      ClockSkew = TimeSpan.Zero,
      IssuerSigningKey = _validationKey,
      RequireExpirationTime = true,
      RequireSignedTokens = true,
      ValidateIssuerSigningKey = true,
      ValidateLifetime = true,
      // Issuer
      ValidIssuer = effectiveIssuer,
      ValidIssuers = effectiveIssuers,
      // Audience
      ValidAudience = effectiveAudience,
      ValidAudiences = effectiveAudiences,
      ValidateIssuer = issuers,
      ValidateAudience = audiences,
      RequireAudience = audiences
    };
  }

  /// <summary>
  /// Converte o resultado da validação do handler nos dados do usuário ou no erro correspondente.
  /// </summary>
  /// <param name="result">Resultado devolvido pelo handler de tokens.</param>
  /// <returns>Dados do usuário quando o token é válido, ou o erro correspondente à falha.</returns>
  private UserTokenDto MapValidationResult(TokenValidationResult result)
  {
    // Retorna os dados do usuário quando o token é válido
    if (result.IsValid && result.SecurityToken is JsonWebToken jsonWebToken)
    {
      return new UserTokenDto(jsonWebToken);
    }

    // Mapeia a falha de validação para o erro correspondente (JsonWebTokenHandler reporta via resultado, não exceção)
    switch (result.Exception)
    {
      case SecurityTokenExpiredException:
        return new UserTokenDto("Token.Expired");

      case SecurityTokenInvalidSignatureException signatureException:
        _logger.LogError("Invalid JWT signature detected.\nException: {exception}", signatureException);

        return new UserTokenDto("Token.InvalidSignature");

      case SecurityTokenException:
      case ArgumentException:
        // Demais falhas de validação (audience/issuer inválidos, token malformado, etc.) são token inválido, não erro interno
        return new UserTokenDto("Token.Invalid");

      default:
        _logger.LogError("Error validating JWT token.\nException: {exception}", result.Exception);

        return new UserTokenDto("InternalServerError");
    }
  }

  #endregion
}
