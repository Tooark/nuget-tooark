using System.Security.Claims;
using Tooark.Securities.Dtos;
using Tooark.Securities.Options;

namespace Tooark.Securities.Extensions;

/// <summary>
/// Extensão para adicionar os dados de autenticação do usuário.
/// </summary>
public static class RoleClaimsExtension
{
  /// <summary>
  /// Adiciona os dados de autenticação do usuário como claims, com os nomes legados (<c>id</c>, <c>login</c>, <c>security</c>).
  /// </summary>
  /// <param name="jwtToken">Dados de autenticação.</param>
  /// <returns>Uma coleção de claims.</returns>
  public static IEnumerable<Claim> GetClaims(this JwtTokenDto jwtToken) =>
    jwtToken.GetClaims(emitLegacyClaims: true, emitStandardClaims: false);

  /// <summary>
  /// Adiciona os dados de autenticação do usuário como claims, com os nomes escolhidos nas opções.
  /// </summary>
  /// <param name="jwtToken">Dados de autenticação.</param>
  /// <param name="options">Opções do JWT, de onde saem <see cref="JwtOptions.EmitLegacyClaims"/> e <see cref="JwtOptions.EmitStandardClaims"/>.</param>
  /// <returns>Uma coleção de claims.</returns>
  public static IEnumerable<Claim> GetClaims(this JwtTokenDto jwtToken, JwtOptions options) =>
    jwtToken.GetClaims(options.EmitLegacyClaims, options.EmitStandardClaims);

  /// <summary>
  /// Adiciona os dados de autenticação do usuário como claims, com os nomes legados, os padrão ou ambos.
  /// </summary>
  /// <param name="jwtToken">Dados de autenticação.</param>
  /// <param name="emitLegacyClaims">Emite <c>id</c> e <c>login</c>.</param>
  /// <param name="emitStandardClaims">Emite <c>sub</c> e <c>preferred_username</c>.</param>
  /// <returns>Uma coleção de claims. A claim <c>security</c> é emitida sempre.</returns>
  /// <remarks>Com os dois sinalizadores desligados, só a claim <c>security</c> é emitida.</remarks>
  public static IEnumerable<Claim> GetClaims(this JwtTokenDto jwtToken, bool emitLegacyClaims, bool emitStandardClaims)
  {
    var claims = new List<Claim>(5);

    // Nomes legados, os padrões da linha 4.x
    if (emitLegacyClaims)
    {
      claims.Add(new Claim(TooarkClaimNames.Id, jwtToken.Id));
      claims.Add(new Claim(TooarkClaimNames.Login, jwtToken.Login));
    }

    // Nomes padrão do JWT e do OpenID Connect
    if (emitStandardClaims)
    {
      claims.Add(new Claim(TooarkClaimNames.Subject, jwtToken.Id));
      claims.Add(new Claim(TooarkClaimNames.PreferredUsername, jwtToken.Login));
    }

    // A chave de segurança não tem equivalente padrão
    claims.Add(new Claim(TooarkClaimNames.Security, jwtToken.Security));

    return claims;
  }
}
