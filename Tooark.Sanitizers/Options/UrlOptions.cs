namespace Tooark.Sanitizers.Options;

/// <summary>
/// Classe que representa as opções do sanitizador de URL.
/// </summary>
public class UrlOptions
{
  #region Constants

  /// <summary>
  /// Esquemas permitidos quando a configuração não informa nenhum.
  /// </summary>
  internal static readonly string[] DefaultSchemes = ["http", "https"];

  #endregion

  #region Properties

  /// <summary>
  /// Esquemas permitidos em uma URL absoluta, como <c>https</c> e <c>mailto</c>. Padrão: <c>http</c> e
  /// <c>https</c>. <c>javascript</c>, <c>vbscript</c> e <c>data</c> não podem ser liberados.
  /// </summary>
  public string[]? AllowedSchemes { get; set; }

  /// <summary>
  /// Indica se URLs relativas são aceitas. Padrão: falso.
  /// </summary>
  /// <remarks>
  /// Relativa é a URL que começa com <c>/</c>, <c>#</c>, <c>?</c>, <c>./</c> ou <c>../</c>, a mesma regra do
  /// <c>@tooark/wysiwyg</c>. Um caminho sem prefixo, como <c>uploads/x.png</c>, é recusado.
  /// </remarks>
  public bool AllowRelative { get; set; }

  /// <summary>
  /// Indica se URLs com usuário e senha (<c>https://usuario:senha@host</c>) são aceitas. Padrão: falso.
  /// </summary>
  /// <remarks>
  /// A forma <c>https://banco.com@golpe.com</c> leva ao host <c>golpe.com</c> enquanto exibe outro nome, um
  /// recurso comum de phishing.
  /// </remarks>
  public bool AllowCredentials { get; set; }

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções do sanitizador de URL.
  /// </summary>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando um esquema é inválido ou está bloqueado.</exception>
  public void Validate() => SchemeRules.Validate(AllowedSchemes);

  #endregion
}
