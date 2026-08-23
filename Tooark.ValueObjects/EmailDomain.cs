using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um domínio de email válido.
/// </summary>
public sealed class EmailDomain : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do domínio de email.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe EmailDomain com o valor especificado.
  /// </summary>
  /// <param name="value">O valor do domínio de email a ser validado.</param>
  public EmailDomain(string value)
  {
    // Adiciona as notificações de validação do domínio de email
    AddNotifications(new Validation()
      .IsEmailDomain(value, "EmailDomain", "Field.Invalid;EmailDomain")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor do domínio de email em minúsculas e sem espaços em branco no início e no final
      _value = value.ToLowerInvariant().Trim();
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor do domínio de email.
  /// </summary>
  public string Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do domínio de email.
  /// </summary>
  /// <returns>Uma string que representa o valor do domínio de email.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto EmailDomain para uma string.
  /// </summary>
  /// <param name="emailDomain">O objeto EmailDomain a ser convertido.</param>
  /// <returns>Uma string que representa o valor do domínio de email.</returns>
  public static implicit operator string(EmailDomain emailDomain) =>
    emailDomain?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto EmailDomain.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto EmailDomain.</param>
  /// <returns>Um objeto EmailDomain criado a partir da string fornecida.</returns>
  public static implicit operator EmailDomain(string value) => new(value);

  #endregion
}
