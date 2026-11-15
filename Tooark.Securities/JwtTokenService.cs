using System.Security.Claims;
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
  /// Chaves de assinatura e de validação carregadas das opções.
  /// </summary>
  private readonly JwtSigningKeys _keys;

  /// <summary>
  /// Relógio usado na emissão e na validação da vida útil do token.
  /// </summary>
  private readonly TimeProvider _timeProvider;

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor do serviço de token JWT, com o relógio do sistema.
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
  /// <exception cref="InternalServerErrorException">Quando nenhum conjunto de claims (legado ou padrão) está habilitado.</exception>
  public JwtTokenService(IOptions<JwtOptions> jwtOptions, ILogger<JwtTokenService>? logger = null)
    : this(jwtOptions, TimeProvider.System, logger)
  { }

  /// <summary>
  /// Construtor do serviço de token JWT com um relógio próprio, para testar emissão e expiração sem esperar o tempo passar.
  /// </summary>
  /// <param name="jwtOptions">Opções de configuração do JWT.</param>
  /// <param name="timeProvider">Relógio da emissão (<c>iat</c>, <c>nbf</c>, <c>exp</c>) e da validação da vida útil.
  /// Com <see cref="TimeProvider.System"/>, a validação fica com o handler.</param>
  /// <param name="logger">Logger do serviço. Opcional: quando não fornecido, usa um logger nulo interno,
  /// sem registrar fallbacks globais de logging no container.</param>
  /// <exception cref="ArgumentNullException">Quando o relógio é nulo.</exception>
  /// <exception cref="InternalServerErrorException">Quando as opções não estão configuradas.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave secreta não está configurada para algoritmos simétricos.</exception>
  /// <exception cref="InternalServerErrorException">Quando as chaves pública/privada não estão configuradas para algoritmos assimétricos.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave RSA tem tamanho inválido.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave ECDsa tem curva inválida.</exception>
  /// <exception cref="InternalServerErrorException">Quando a chave é inválida ou o algoritmo não é suportado.</exception>
  /// <exception cref="InternalServerErrorException">Quando nenhum conjunto de claims (legado ou padrão) está habilitado.</exception>
  public JwtTokenService(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider, ILogger<JwtTokenService>? logger = null)
  {
    ArgumentNullException.ThrowIfNull(timeProvider);

    // Configura o logger (fallback interno para NullLogger quando logging não está configurado)
    _logger = logger ?? NullLogger<JwtTokenService>.Instance;
    _timeProvider = timeProvider;

    // Valida se as opções foram configuradas corretamente
    _jwtOptions = jwtOptions.Value
      ?? throw new InternalServerErrorException("Options.NotConfigured");

    // Um token sem id nem sub não identifica ninguém: falha no startup
    if (!_jwtOptions.EmitLegacyClaims && !_jwtOptions.EmitStandardClaims)
    {
      throw new InternalServerErrorException("Options.Jwt.ClaimsNotConfigured");
    }

    // Carrega as chaves com as mesmas regras do esquema de autenticação (fail-fast)
    _keys = JwtSigningKeys.Load(_jwtOptions, _logger);
  }

  #endregion

  #region Methods

  /// <inheritdoc/>
  public string Create(JwtTokenDto data, string? audience = null, IEnumerable<Claim>? extraClaims = null)
  {
    // Valida se pode criar o token
    if (!_keys.CanCreate)
    {
      throw new InternalServerErrorException("Options.Jwt.KeyNotConfigured;PrivateKey");
    }

    // Configura propriedades do token
    var expiryTime = _jwtOptions.ExpirationTime;
    var baseClaims = data.GetClaims(_jwtOptions);

    // Adiciona claims extras, se fornecidos
    var claims = extraClaims is null
    ? baseClaims
    : baseClaims.Concat(extraClaims);

    // Todas as marcas de tempo saem do mesmo relógio, para que um relógio de teste controle a emissão inteira
    var now = _timeProvider.GetUtcNow().UtcDateTime;

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      IssuedAt = now,
      NotBefore = now,
      Expires = now.AddMinutes(expiryTime),
      SigningCredentials = new SigningCredentials(_keys.CreateKey, _keys.Algorithm),
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
    if (!_keys.CanValidate)
    {
      throw new InternalServerErrorException("Options.Jwt.KeyNotConfigured;PublicKey");
    }

    try
    {
      // Valida o token: o handler expõe a validação apenas de forma assíncrona
      var result = await _tokenHandler
        .ValidateTokenAsync(token, JwtValidation.BuildParameters(_jwtOptions, _keys.ValidationKey!, audience, _timeProvider))
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
