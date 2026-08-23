using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um CNPJ.
/// </summary>
public sealed class Cnpj : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do número do CNPJ.
  /// </summary>
  private readonly string _number = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Cnpj com o número.
  /// </summary>
  /// <param name="number">O número do CNPJ a ser validado.</param>
  public Cnpj(string number)
  {
    // Valida documento do tipo CNPJ
    var document = new Document(number, EDocumentType.CNPJ);

    // Adiciona as notificações
    AddNotifications(document);

    // Verifica se é válido, então não existe notificação
    if (IsValid)
    {
      // Define o valor do número da CNPJ
      _number = document;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do número do CNPJ.
  /// </summary>
  public string Number { get => _number; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do CNPJ.
  /// </summary>
  /// <returns>O valor do CNPJ.</returns>
  public override string ToString() => _number;

  /// <summary>
  /// Define uma conversão implícita de um objeto Cnpj para uma string.
  /// </summary>
  /// <param name="document">O objeto Cnpj a ser convertido.</param>
  /// <returns>Uma string que representa o valor do CNPJ.</returns>
  public static implicit operator string(Cnpj document) =>
    document?._number ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Cnpj.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Cnpj.</param>
  /// <returns>Um objeto Cnpj criado a partir da string fornecida.</returns>
  public static implicit operator Cnpj(string value) => new(value);

  #endregion
}
