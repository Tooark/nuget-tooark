namespace Tooark.Securities;

/// <summary>
/// Nomes das claims que o <see cref="JwtTokenService"/> emite e o <see cref="Dtos.UserTokenDto"/> lê.
/// </summary>
/// <remarks>
/// As claims padrão (<see cref="Subject"/>, <see cref="PreferredUsername"/>) seguem o JWT (RFC 7519) e o OpenID
/// Connect e são as emitidas por padrão (<see cref="Options.JwtOptions.EmitStandardClaims"/>). As legadas
/// (<see cref="Id"/>, <see cref="Login"/>), que eram o padrão da linha 4.x, são opt-in por
/// <see cref="Options.JwtOptions.EmitLegacyClaims"/>.
/// </remarks>
public static class TooarkClaimNames
{
  /// <summary>
  /// Identificador do usuário (claim legada).
  /// </summary>
  public const string Id = "id";

  /// <summary>
  /// Login do usuário (claim legada).
  /// </summary>
  public const string Login = "login";

  /// <summary>
  /// Chave de segurança do usuário. Não tem equivalente padrão e é emitida sempre.
  /// </summary>
  public const string Security = "security";

  /// <summary>
  /// Identificador do usuário (claim padrão <c>sub</c>, RFC 7519).
  /// </summary>
  public const string Subject = "sub";

  /// <summary>
  /// Login do usuário (claim padrão <c>preferred_username</c>, OpenID Connect).
  /// </summary>
  public const string PreferredUsername = "preferred_username";
}
