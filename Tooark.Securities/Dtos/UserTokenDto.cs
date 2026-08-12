using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Tooark.Securities.Dtos;

/// <summary>
/// Data Transfer Object (DTO) para representar o conteúdo um usuário de uma validação de token.
/// </summary>
public class UserTokenDto
{
  #region Constructors

  /// <summary>
  /// Construtor a partir de um token JWT validado.
  /// </summary>
  /// <param name="token">Token JWT validado.</param>
  /// <remarks>
  /// Claims ausentes resultam em valores vazios, permitindo validar tokens
  /// emitidos por outros fluxos sem gerar erro interno.
  /// </remarks>
  public UserTokenDto(JwtSecurityToken token)
  {
    Id = token.Claims.FirstOrDefault(x => x.Type == "id")?.Value ?? string.Empty;
    Login = token.Claims.FirstOrDefault(x => x.Type == "login")?.Value ?? string.Empty;
    Security = token.Claims.FirstOrDefault(x => x.Type == "security")?.Value ?? string.Empty;
  }

  /// <summary>
  /// Construtor a partir de um token JWT validado (JsonWebToken, handler atual).
  /// </summary>
  /// <param name="token">Token JWT validado.</param>
  /// <remarks>
  /// Claims ausentes resultam em valores vazios, permitindo validar tokens
  /// emitidos por outros fluxos sem gerar erro interno.
  /// </remarks>
  public UserTokenDto(JsonWebToken token)
  {
    Id = token.Claims.FirstOrDefault(x => x.Type == "id")?.Value ?? string.Empty;
    Login = token.Claims.FirstOrDefault(x => x.Type == "login")?.Value ?? string.Empty;
    Security = token.Claims.FirstOrDefault(x => x.Type == "security")?.Value ?? string.Empty;
  }

  /// <summary>
  /// Construtor padrão utilizando um erro.
  /// </summary>
  /// <param name="error">Mensagem de erro.</param>
  public UserTokenDto(string error)
  {
    ErrorToken = error;
  }

  #endregion

  #region Properties

  /// <summary>
  /// Identificador do usuário.
  /// </summary>
  public string Id { get; private set; } = string.Empty;

  /// <summary>
  /// Login do usuário.
  /// </summary>
  public string Login { get; private set; } = string.Empty;

  /// <summary>
  /// Chave de segurança do usuário.
  /// </summary>
  public string Security { get; private set; } = string.Empty;

  /// <summary>
  /// Mensagem de erro do token.
  /// </summary>
  public string ErrorToken { get; private set; } = string.Empty;

  #endregion

  #region Methods

  /// <summary>
  /// Retorna o identificador do usuário como um Guid.
  /// </summary>
  public Guid GetGuidId => Guid.TryParse(Id, out var id) ? id : Guid.Empty;

  /// <summary>
  /// Retorna o identificador do usuário como um inteiro.
  /// </summary>
  public int GetIntId => int.TryParse(Id, out var id) ? id : 0;

  /// <summary>
  /// Retorna a chave de segurança do usuário como um Guid.
  /// </summary>
  public Guid GetGuidSecurity => Guid.TryParse(Security, out var security) ? security : Guid.Empty;

  /// <summary>
  /// Retorna a chave de segurança do usuário como um inteiro.
  /// </summary>
  public int GetIntSecurity => int.TryParse(Security, out var security) ? security : 0;

  #endregion
}
