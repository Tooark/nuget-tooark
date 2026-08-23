using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um protocolo FTP. Protocolos FTP e SFTP.
/// </summary>
public sealed class ProtocolFtp : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do protocolo FTP.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe ProtocolFtp com o valor especificado.
  /// </summary>
  /// <param name="value">Valor do protocolo FTP.</param>
  public ProtocolFtp(string value)
  {
    // Adiciona as notificações de validação do protocolo FTP
    AddNotifications(new Validation()
      .IsProtocolFtp(value, "ProtocolFtp", "Field.Invalid;ProtocolFtp")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor do protocolo FTP
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do protocolo FTP.
  /// </summary>
  public string Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do protocolo FTP.
  /// </summary>
  /// <returns>O valor do protocolo FTP.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto ProtocolFtp para uma string.
  /// </summary>
  /// <param name="protocol">O objeto ProtocolFtp a ser convertido.</param>
  /// <returns>Uma string que representa o valor do protocolo FTP.</returns>
  public static implicit operator string(ProtocolFtp protocol) =>
    protocol?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto ProtocolFtp.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto ProtocolFtp.</param>
  /// <returns>Um objeto ProtocolFtp criado a partir da string fornecida.</returns>
  public static implicit operator ProtocolFtp(string value) => new(value);

  #endregion
}
