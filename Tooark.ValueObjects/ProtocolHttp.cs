using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um protocolo HTTP. Protocolos HTTP e HTTPS.
/// </summary>
public sealed class ProtocolHttp : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do protocolo HTTP.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe ProtocolHttp com o valor especificado.
  /// </summary>
  /// <param name="value">Valor do protocolo HTTP.</param>
  public ProtocolHttp(string value)
  {
    // Adiciona as notificações de validação do protocolo HTTP
    AddNotifications(new Validation()
      .IsProtocolHttp(value, "ProtocolHttp", "Field.Invalid;ProtocolHttp")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor do protocolo HTTP
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do protocolo HTTP.
  /// </summary>
  public string Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do protocolo HTTP.
  /// </summary>
  /// <returns>O valor do protocolo HTTP.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto ProtocolHttp para uma string.
  /// </summary>
  /// <param name="protocol">O objeto ProtocolHttp a ser convertido.</param>
  /// <returns>Uma string que representa o valor do protocolo HTTP.</returns>
  public static implicit operator string(ProtocolHttp protocol) =>
    protocol?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto ProtocolHttp.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto ProtocolHttp.</param>
  /// <returns>Um objeto ProtocolHttp criado a partir da string fornecida.</returns>
  public static implicit operator ProtocolHttp(string value) => new(value);

  #endregion
}
