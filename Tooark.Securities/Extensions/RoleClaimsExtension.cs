using System.Security.Claims;
using Tooark.Securities.Dtos;

namespace Tooark.Securities.Extensions;

/// <summary>
/// Extensão para adicionar os dados de autenticação do usuário.
/// </summary>
public static class RoleClaimsExtension
{
  /// <summary>
  /// Adiciona os dados de autenticação do usuário como claims.
  /// </summary>
  /// <param name="jwtToken">Dados de autenticação.</param>
  /// <returns>Uma coleção de claims.</returns>
  public static IEnumerable<Claim> GetClaims(this JwtTokenDto jwtToken) =>
  [
    new("id", jwtToken.Id),
    new("login", jwtToken.Login),
    new("security", jwtToken.Security)
  ];
}
