using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa uma string de apenas letras e números válido.
/// </summary>
public sealed class LetterNumeric : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado da string de letras e números.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe LetterNumeric com o valor especificado.
  /// </summary>
  /// <param name="value">O valor da string de letras e números a ser validado.</param>
  public LetterNumeric(string value)
  {
    // Adiciona as notificações de validação da string de letras e números.
    AddNotifications(new Validation()
      .IsLetterNumeric(value, "LetterNumeric", "Field.Invalid;LetterNumeric")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor da string de letras e números
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor da string de letras e números.
  /// </summary>
  public string Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor da string de letras e números.
  /// </summary>
  /// <returns>Uma string que representa o valor da string de letras e números.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto LetterNumeric para uma string.
  /// </summary>
  /// <param name="letterNumeric">O objeto LetterNumeric a ser convertido.</param>
  /// <returns>Uma string que representa o valor do LetterNumeric.</returns>
  public static implicit operator string(LetterNumeric letterNumeric) =>
    letterNumeric?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto LetterNumeric.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto LetterNumeric.</param>
  /// <returns>Um objeto LetterNumeric criado a partir da string fornecida.</returns>
  public static implicit operator LetterNumeric(string value) => new(value);

  #endregion
}
