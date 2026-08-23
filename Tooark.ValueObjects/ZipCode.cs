using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um código postal válido.
/// </summary>
public class ZipCode : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do código postal.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe ZipCode com o valor especificado.
  /// </summary>
  /// <param name="value">O valor do código postal a ser validado.</param>
  public ZipCode(string value)
  {
    // Adiciona as notificações de validação do código postal.
    AddNotifications(new Validation()
      .IsZipCode(value, "ZipCode", "Field.Invalid;ZipCode")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor do código postal
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor do código postal.
  /// </summary>
  public string Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do código postal.
  /// </summary>
  /// <returns>Uma string que representa o valor do código postal.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto ZipCode para uma string.
  /// </summary>
  /// <param name="zipCode">O objeto ZipCode a ser convertido.</param>
  /// <returns>Uma string que representa o valor do ZipCode.</returns>
  public static implicit operator string(ZipCode zipCode) =>
    zipCode?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto ZipCode.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto ZipCode.</param>
  /// <returns>Um objeto ZipCode criado a partir da string fornecida.</returns>
  public static implicit operator ZipCode(string value) => new(value);

  #endregion
}
