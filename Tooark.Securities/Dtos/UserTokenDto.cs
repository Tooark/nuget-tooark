using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    : this(token.Claims)
  { }

  /// <summary>
  /// Construtor a partir de um token JWT validado (JsonWebToken, handler atual).
  /// </summary>
  /// <param name="token">Token JWT validado.</param>
  /// <remarks>
  /// Claims ausentes resultam em valores vazios, permitindo validar tokens
  /// emitidos por outros fluxos sem gerar erro interno.
  /// </remarks>
  public UserTokenDto(JsonWebToken token)
    : this(token.Claims)
  { }

  /// <summary>
  /// Construtor padrão utilizando um erro.
  /// </summary>
  /// <param name="error">Mensagem de erro.</param>
  public UserTokenDto(string error)
  {
    ErrorToken = error;
  }

  /// <summary>
  /// Construtor a partir das claims de um token validado.
  /// </summary>
  /// <param name="claims">Claims do token validado.</param>
  private UserTokenDto(IEnumerable<Claim> claims)
  {
    // Materializa uma única vez: as propriedades e os métodos de leitura consultam a mesma lista
    Claims = [.. claims];

    Id = GetClaim("id");
    Login = GetClaim("login");
    Security = GetClaim("security");
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

  /// <summary>
  /// Todas as claims do token validado.
  /// </summary>
  /// <remarks>
  /// Inclui as claims do usuário (id, login, security), as claims extras informadas na criação do token
  /// e as registradas pelo emissor (exp, iat, nbf, iss, aud). Uma claim com vários valores (um array no
  /// payload) aparece uma vez por valor, com o mesmo tipo. Vazia quando a validação falha.
  /// </remarks>
  public IReadOnlyList<Claim> Claims { get; private set; } = [];

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

  /// <summary>
  /// Retorna o valor de uma claim do token.
  /// </summary>
  /// <param name="type">Tipo (nome) da claim.</param>
  /// <returns>O primeiro valor da claim, ou vazio quando a claim não existe no token.</returns>
  public string GetClaim(string type) =>
    Claims.FirstOrDefault(x => x.Type == type)?.Value ?? string.Empty;

  /// <summary>
  /// Retorna o valor de uma claim do token convertido para o tipo informado.
  /// </summary>
  /// <typeparam name="T">Tipo de destino (ex: Guid, int, long, bool, decimal, DateTime).</typeparam>
  /// <param name="type">Tipo (nome) da claim.</param>
  /// <returns>
  /// O primeiro valor da claim convertido com a cultura invariante, ou <c>default</c> do tipo quando a
  /// claim não existe ou o valor não é conversível (ex: <see cref="Guid.Empty"/> e 0).
  /// </returns>
  public T? GetClaim<T>(string type) where T : IParsable<T> =>
    T.TryParse(GetClaim(type), CultureInfo.InvariantCulture, out var value) ? value : default;

  /// <summary>
  /// Retorna todos os valores de uma claim do token.
  /// </summary>
  /// <param name="type">Tipo (nome) da claim.</param>
  /// <returns>Os valores da claim, na ordem do token, ou uma lista vazia quando a claim não existe.</returns>
  /// <remarks>Útil para claims com vários valores, como papéis e permissões.</remarks>
  public IReadOnlyList<string> GetClaims(string type) =>
    [.. Claims.Where(x => x.Type == type).Select(x => x.Value)];

  /// <summary>
  /// Indica se o token possui uma claim do tipo informado.
  /// </summary>
  /// <param name="type">Tipo (nome) da claim.</param>
  /// <returns>Verdadeiro quando a claim existe no token, mesmo com valor vazio.</returns>
  public bool HasClaim(string type) =>
    Claims.Any(x => x.Type == type);

  #endregion
}
