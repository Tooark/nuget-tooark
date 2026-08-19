namespace Tooark.Securities.Dtos;

/// <summary>
/// Data Transfer Object (DTO) para representar o conteúdo de um token JWT.
/// </summary>
/// <param name="id">Identificador do usuário.</param>
/// <param name="login">Login do usuário.</param>
/// <param name="security">Identificador de segurança do usuário.</param>
public class JwtTokenDto(string id, string login, string security)
{
  #region Constructors

  /// <summary>
  /// Construtor padrão utilizando parâmetros individuais.
  /// </summary>
  /// <param name="id">Identificador do usuário.</param>
  /// <param name="login">Login do usuário.</param>
  /// <param name="security">Identificador de segurança do usuário.</param>
  public JwtTokenDto(Guid id, Guid login, Guid security)
    : this(id.ToString(), login.ToString(), security.ToString())
  { }

  /// <summary>
  /// Construtor padrão utilizando parâmetros individuais.
  /// </summary>
  /// <param name="id">Identificador do usuário.</param>
  /// <param name="login">Login do usuário.</param>
  /// <param name="security">Identificador de segurança do usuário.</param>
  public JwtTokenDto(int id, int login, int security)
    : this(id.ToString(), login.ToString(), security.ToString())
  { }

  /// <summary>
  /// Construtor padrão utilizando parâmetros individuais.
  /// </summary>
  /// <param name="id">Identificador do usuário.</param>
  /// <param name="login">Login do usuário.</param>
  /// <param name="security">Identificador de segurança do usuário.</param>
  public JwtTokenDto(Guid id, string login, Guid security)
    : this(id.ToString(), login, security.ToString())
  { }

  /// <summary>
  /// Construtor padrão utilizando parâmetros individuais.
  /// </summary>
  /// <param name="id">Identificador do usuário.</param>
  /// <param name="login">Login do usuário.</param>
  /// <param name="security">Identificador de segurança do usuário.</param>
  public JwtTokenDto(Guid id, string login, int security)
    : this(id.ToString(), login, security.ToString())
  { }

  /// <summary>
  /// Construtor padrão utilizando parâmetros individuais.
  /// </summary>
  /// <param name="id">Identificador do usuário.</param>
  /// <param name="login">Login do usuário.</param>
  /// <param name="security">Identificador de segurança do usuário.</param>
  public JwtTokenDto(int id, string login, Guid security)
    : this(id.ToString(), login, security.ToString())
  { }

  /// <summary>
  /// Construtor padrão utilizando parâmetros individuais.
  /// </summary>
  /// <param name="id">Identificador do usuário.</param>
  /// <param name="login">Login do usuário.</param>
  /// <param name="security">Identificador de segurança do usuário.</param>
  public JwtTokenDto(int id, string login, int security)
    : this(id.ToString(), login, security.ToString())
  { }

  #endregion

  #region Properties

  /// <summary>
  /// Identificador do usuário.
  /// </summary>
  public string Id { get; set; } = id;

  /// <summary>
  /// Login do usuário.
  /// </summary>
  public string Login { get; set; } = login;

  /// <summary>
  /// Chave de segurança do usuário.
  /// </summary>
  public string Security { get; set; } = security;

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
