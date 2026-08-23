using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa uma string de apenas números válido.
/// </summary>
public sealed class Numeric : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado da string de números.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Numeric com o valor especificado.
  /// </summary>
  /// <param name="value">O valor da string de números a ser validado.</param>
  public Numeric(string value)
  {
    // Adiciona as notificações de validação da string de números.
    AddNotifications(new Validation()
      .IsNumeric(value, "Numeric", "Field.Invalid;Numeric")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor da string de números
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor da string de números.
  /// </summary>
  public string Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor da string de números.
  /// </summary>
  /// <returns>Uma string que representa o valor da string de números.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto Numeric para uma string.
  /// </summary>
  /// <param name="numeric">O objeto Numeric a ser convertido.</param>
  /// <returns>Uma string que representa o valor do Numeric.</returns>
  public static implicit operator string(Numeric numeric) =>
    numeric?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Numeric.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Numeric.</param>
  /// <returns>Um objeto Numeric criado a partir da string fornecida.</returns>
  public static implicit operator Numeric(string value) => new(value);

  #endregion
}
