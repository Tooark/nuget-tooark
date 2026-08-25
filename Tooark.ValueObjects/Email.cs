using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um endereço de email válido.
/// </summary>
public sealed class Email : ValueObject
{
  #region Constants

  /// <summary>
  /// Tamanho mínimo do email.
  /// </summary>
  public const int MinLength = 6;

  /// <summary>
  /// Tamanho máximo do email.
  /// </summary>
  public const int MaxLength = 255;

  #endregion

  #region Private Fields

  /// <summary>
  /// Valor privado do email.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Email com o valor especificado.
  /// </summary>
  /// <param name="value">O valor do email a ser validado.</param>
  public Email(string? value)
  {
    // Adiciona as notificações de validação do email
    AddNotifications(new Validation()
      .IsBetween(value ?? "", MinLength, MaxLength, "Email")
      .IsEmail(value ?? "", "Email", "Field.Invalid;Email")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor do email em minúsculas e sem espaços em branco no início e no final
      _value = value!.ToLowerInvariant().Trim();
    }
  }


  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor do email.
  /// </summary>
  public string Value { get => _value; }


  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do email.
  /// </summary>
  /// <returns>Uma string que representa o valor do email.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto Email para uma string.
  /// </summary>
  /// <param name="email">O objeto Email a ser convertido.</param>
  /// <returns>Uma string que representa o valor do Email.</returns>
  public static implicit operator string(Email email) =>
    email?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Email.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Email.</param>
  /// <returns>Um objeto Email criado a partir da string fornecida.</returns>
  public static implicit operator Email(string value) => new(value);

  #endregion
}
