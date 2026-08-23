using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa uma URL. Protocolos FTP, SFTP, HTTP, HTTPS, IMAP, POP3, SMTP, WS e WSS.
/// </summary>
public class Url : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado da URL.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Url com o valor especificado.
  /// </summary>
  /// <param name="value">Valor da URL.</param>
  public Url(string value)
  {
    // Adiciona as notificações de validação da URL
    AddNotifications(new Validation()
      .IsUrl(value, "Url", "Field.Invalid;Url")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor da URL
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor da URL.
  /// </summary>
  public string Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor da URL.
  /// </summary>
  /// <returns>O valor da URL.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto Url para uma string.
  /// </summary>
  /// <param name="url">O objeto Url a ser convertido.</param>
  /// <returns>Uma string que representa o valor da URL.</returns>
  public static implicit operator string(Url url) =>
    url?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Url.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Url.</param>
  /// <returns>Um objeto Url criado a partir da string fornecida.</returns>
  public static implicit operator Url(string value) => new(value);

  #endregion
}
