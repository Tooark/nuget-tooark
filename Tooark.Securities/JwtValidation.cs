using Microsoft.IdentityModel.Tokens;
using Tooark.Securities.Options;

namespace Tooark.Securities;

/// <summary>
/// Parâmetros de validação do JWT montados a partir das opções.
/// </summary>
/// <remarks>
/// Compartilhado pelo <see cref="JwtTokenService"/> e pelo esquema de autenticação <c>AddTooarkJwtBearer</c>,
/// para que um token aceito por um seja aceito pelo outro.
/// </remarks>
internal static class JwtValidation
{
  #region Methods

  /// <summary>
  /// Monta os parâmetros de validação do token a partir das opções e do destinatário informado.
  /// </summary>
  /// <param name="options">Opções de configuração do JWT.</param>
  /// <param name="validationKey">Chave de validação da assinatura.</param>
  /// <param name="audience">Destinatário que sobrescreve o destinatário padrão das opções, opcional.</param>
  /// <param name="timeProvider">Relógio da validação de vida útil; nulo ou o relógio do sistema usam a validação padrão do handler.</param>
  /// <returns>Parâmetros de validação do token.</returns>
  public static TokenValidationParameters BuildParameters(
    JwtOptions options,
    SecurityKey validationKey,
    string? audience = null,
    TimeProvider? timeProvider = null
  )
  {
    // Define issuer e audiences efetivos com base nas opções e parâmetros
    var effectiveIssuer = options.Issuer;
    var effectiveIssuers = options.Issuers;

    // Se um audience for informado no parâmetro, ele tem prioridade sobre as opções
    var effectiveAudience = !string.IsNullOrWhiteSpace(audience)
      ? audience
      : options.Audience;
    var effectiveAudiences = !string.IsNullOrWhiteSpace(audience)
      ? [audience]
      : options.Audiences;

    // Verifica se deve validar issuer/audience com base em qualquer configuração disponível
    var issuers = !string.IsNullOrWhiteSpace(effectiveIssuer) || effectiveIssuers?.Length > 0;
    var audiences = !string.IsNullOrWhiteSpace(effectiveAudience) || effectiveAudiences?.Length > 0;

    var parameters = new TokenValidationParameters
    {
      ClockSkew = TimeSpan.Zero,
      IssuerSigningKey = validationKey,
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

    // Um relógio próprio substitui a validação de vida útil do handler, que lê o relógio do sistema
    if (timeProvider is not null && !ReferenceEquals(timeProvider, TimeProvider.System))
    {
      parameters.LifetimeValidator = CreateLifetimeValidator(timeProvider);
    }

    return parameters;
  }

  /// <summary>
  /// Cria a validação de vida útil sobre um relógio próprio, com as mesmas regras e exceções do handler.
  /// </summary>
  /// <param name="timeProvider">Relógio da validação.</param>
  /// <returns>O validador de vida útil.</returns>
  public static LifetimeValidator CreateLifetimeValidator(TimeProvider timeProvider) =>
    (notBefore, expires, securityToken, validationParameters) =>
    {
      // A expiração é obrigatória: um token sem exp valeria para sempre
      if (validationParameters.RequireExpirationTime && !expires.HasValue)
      {
        throw new SecurityTokenNoExpirationException("IDX10225: The token has no expiration time.");
      }

      // Um token que expira antes de valer é inválido por construção
      if (notBefore.HasValue && expires.HasValue && notBefore.Value > expires.Value)
      {
        throw new SecurityTokenInvalidLifetimeException("IDX10224: The token NotBefore is after its Expires.");
      }

      var now = timeProvider.GetUtcNow().UtcDateTime;
      var skew = validationParameters.ClockSkew;

      if (notBefore.HasValue && notBefore.Value > now.Add(skew))
      {
        throw new SecurityTokenNotYetValidException("IDX10222: The token is not valid yet.")
        {
          NotBefore = notBefore.Value
        };
      }

      if (expires.HasValue && expires.Value < now.Subtract(skew))
      {
        throw new SecurityTokenExpiredException("IDX10223: The token has expired.")
        {
          Expires = expires.Value
        };
      }

      return true;
    };

  #endregion
}
