using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um protocolo de Envio de Email. Protocolo SMTP.
/// </summary>
public sealed class ProtocolEmailSender : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do protocolo de Envio de Email.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe ProtocolEmailSender com o valor especificado.
  /// </summary>
  /// <param name="value">Valor do protocolo de Envio de Email.</param>
  public ProtocolEmailSender(string value)
  {
    // Adiciona as notificações de validação do protocolo de Envio de Email
    AddNotifications(new Validation()
      .IsProtocolEmailSender(value, "ProtocolEmailSender", "Field.Invalid;ProtocolEmailSender")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor do protocolo de Envio de Email
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do protocolo de Envio de Email.
  /// </summary>
  public string Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do protocolo de Envio de Email.
  /// </summary>
  /// <returns>O valor do protocolo de Envio de Email.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto ProtocolEmailSender para uma string.
  /// </summary>
  /// <param name="protocol">O objeto ProtocolEmailSender a ser convertido.</param>
  /// <returns>Uma string que representa o valor do protocolo de envio de email.</returns>
  public static implicit operator string(ProtocolEmailSender protocol) =>
    protocol?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto ProtocolEmailSender.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto ProtocolEmailSender.</param>
  /// <returns>Um objeto ProtocolEmailSender criado a partir da string fornecida.</returns>
  public static implicit operator ProtocolEmailSender(string value) => new(value);

  #endregion
}
