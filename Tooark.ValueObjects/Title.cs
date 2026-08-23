using Tooark.Exceptions;
using Tooark.Extensions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um título válido.
/// </summary>
public sealed class Title : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do título.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Title com o valor especificado.
  /// </summary>
  /// <param name="value">O valor do título a ser validado.</param>
  public Title(string value)
  {
    // Adiciona as notificações de validação do título
    AddNotifications(new Validation()
      .IsNotNullOrWhiteSpace(value, "Title", "Field.Invalid;Title")
    );

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o valor do título sem espaços em branco no início e no final
      _value = value.Trim();
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor do título.
  /// </summary>
  public string Value { get => _value; }

  /// <summary>
  /// Obtém o valor do título normalizado.
  /// </summary>
  public string Normalized => _value.ToNormalize();

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do título.
  /// </summary>
  /// <returns>Uma string que representa o valor do título.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto Title para uma string.
  /// </summary>
  /// <param name="title">O objeto Title a ser convertido.</param>
  /// <returns>Uma string que representa o valor do Title.</returns>
  public static implicit operator string(Title title) =>
    title?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Title.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Title.</param>
  /// <returns>Um objeto Title criado a partir da string fornecida.</returns>
  public static implicit operator Title(string value) => new(value);

  #endregion
}
