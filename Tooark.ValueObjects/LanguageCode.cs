using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um código de um idioma válido.
/// </summary>
public sealed class LanguageCode : ValueObject
{
  #region Constants

  /// <summary>
  /// Tamanho do código do idioma, no formato xx-XX. Exemplo: pt-BR.
  /// </summary>
  public const int Length = 5;

  #endregion

  #region Private Fields

  /// <summary>
  /// Código do idioma privado.
  /// </summary>
  private readonly string _code = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe LanguageCode com o valor especificado.
  /// </summary>
  /// <param name="code">Código do idioma.</param>
  public LanguageCode(string code)
  {
    // Adiciona as notificações de validação código do idioma
    AddNotifications(new Validation()
      .IsCultureIgnoreCase(code, "LanguageCode", "Field.Invalid;LanguageCode")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Padroniza o código do idioma
      _code = code[..2].ToLowerInvariant() + "-" + code[3..].ToUpperInvariant();
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o código do idioma.
  /// </summary>
  public string Code { get => _code; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do código do idioma.
  /// </summary>
  /// <returns>O valor do código do idioma.</returns>
  public override string ToString() => _code;

  /// <summary>
  /// Define uma conversão implícita de um objeto LanguageCode para uma string.
  /// </summary>
  /// <param name="languageCode">O objeto LanguageCode a ser convertido.</param>
  /// <returns>Uma string que representa o valor do código do idioma.</returns>
  public static implicit operator string(LanguageCode languageCode) =>
    languageCode?._code ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto LanguageCode.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto LanguageCode.</param>
  /// <returns>Um objeto LanguageCode criado a partir da string fornecida.</returns>
  public static implicit operator LanguageCode(string value) => new(value);

  #endregion
}
