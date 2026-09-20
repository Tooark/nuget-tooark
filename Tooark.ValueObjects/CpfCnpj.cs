using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um CPF ou CNPJ.
/// </summary>
public sealed class CpfCnpj : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do número do CPF ou CNPJ.
  /// </summary>
  private readonly string _number = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe CpfCnpj com o número.
  /// </summary>
  /// <param name="number">O número do CPF ou CNPJ a ser validado.</param>
  public CpfCnpj(string number)
  {
    // Valida documento do tipo CPF ou CNPJ
    var document = new Document(number, EDocumentType.CPF_CNPJ);

    // Adiciona as notificações
    AddNotifications(document);

    // Verifica se é válido, então não existe notificação
    if (IsValid)
    {
      // Define o valor do número da CPF ou CNPJ
      _number = document;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do número do CPF ou CNPJ.
  /// </summary>
  public string Number { get => _number; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do CPF ou CNPJ.
  /// </summary>
  /// <returns>O valor do CPF ou CNPJ.</returns>
  public override string ToString() => _number;

  /// <summary>
  /// Define uma conversão implícita de um objeto CpfCnpj para uma string.
  /// </summary>
  /// <param name="document">O objeto CpfCnpj a ser convertido.</param>
  /// <returns>Uma string que representa o valor do CPF ou CNPJ.</returns>
  public static implicit operator string(CpfCnpj document) =>
    document?._number ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto CpfCnpj.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto CpfCnpj.</param>
  /// <returns>Um objeto CpfCnpj criado a partir da string fornecida.</returns>
  public static implicit operator CpfCnpj(string value) => new(value);

  #endregion
}
