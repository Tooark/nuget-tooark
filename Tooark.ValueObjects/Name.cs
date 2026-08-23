using Tooark.Exceptions;
using Tooark.Extensions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um nome válido.
/// </summary>
public sealed class Name : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do nome.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Name com o valor especificado.
  /// </summary>
  /// <param name="value">O valor do nome a ser validado.</param>
  public Name(string value)
  {
    // Adiciona as notificações de validação do nome
    AddNotifications(new Validation()
      .IsNotNullOrWhiteSpace(value, "Name", "Field.Invalid;Name")
    );

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o valor do nome sem espaços em branco no início e no final
      _value = value.Trim();
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor do nome.
  /// </summary>
  public string Value { get => _value; }

  /// <summary>
  /// Obtém o valor do nome normalizado.
  /// </summary>
  public string Normalized => _value.ToNormalize();

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do nome.
  /// </summary>
  /// <returns>Uma string que representa o valor do nome.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto Name para uma string.
  /// </summary>
  /// <param name="name">O objeto Name a ser convertido.</param>
  /// <returns>Uma string que representa o valor do Name.</returns>
  public static implicit operator string(Name name) =>
    name?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Name.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Name.</param>
  /// <returns>Um objeto Name criado a partir da string fornecida.</returns>
  public static implicit operator Name(string value) => new(value);

  #endregion
}
