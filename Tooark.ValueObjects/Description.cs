using Tooark.Exceptions;
using Tooark.Extensions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa uma descrição válida.
/// </summary>
public sealed class Description : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado da descrição.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Description com o valor especificado.
  /// </summary>
  /// <param name="value">O valor da descrição a ser validada.</param>
  public Description(string value)
  {
    // Adiciona as notificações de validação da descrição
    AddNotifications(new Validation()
      .IsNotNullOrWhiteSpace(value, "Description", "Field.Invalid;Description")
    );

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o valor da descrição sem espaços em branco no início e no final
      _value = value.Trim();
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor da descrição.
  /// </summary>
  public string Value { get => _value; }

  /// <summary>
  /// Obtém o valor da descrição normalizado.
  /// </summary>
  public string Normalized => _value.ToNormalize();

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor da descrição.
  /// </summary>
  /// <returns>Uma string que representa o valor da descrição.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto Description para uma string.
  /// </summary>
  /// <param name="description">O objeto Description a ser convertido.</param>
  /// <returns>Uma string que representa o valor do Description.</returns>
  public static implicit operator string(Description description) =>
    description?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Description.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Description.</param>
  /// <returns>Um objeto Description criado a partir da string fornecida.</returns>
  public static implicit operator Description(string value) => new(value);

  #endregion
}
