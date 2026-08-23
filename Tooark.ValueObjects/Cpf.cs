using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um CPF.
/// </summary>
public sealed class Cpf : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do número do CPF.
  /// </summary>
  private readonly string _number = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Cpf com o número.
  /// </summary>
  /// <param name="number">O número do CPF a ser validado.</param>
  public Cpf(string number)
  {
    // Valida documento do tipo CPF
    var document = new Document(number, EDocumentType.CPF);

    // Adiciona as notificações
    AddNotifications(document);

    // Verifica se é válido, então não existe notificação
    if (IsValid)
    {
      // Define o valor do número da CPF
      _number = document;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do número do CPF.
  /// </summary>
  public string Number { get => _number; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do CPF.
  /// </summary>
  /// <returns>O valor do CPF.</returns>
  public override string ToString() => _number;

  /// <summary>
  /// Define uma conversão implícita de um objeto Cpf para uma string.
  /// </summary>
  /// <param name="document">O objeto Cpf a ser convertido.</param>
  /// <returns>Uma string que representa o valor do CPF.</returns>
  public static implicit operator string(Cpf document) =>
    document?._number ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Cpf.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Cpf.</param>
  /// <returns>Um objeto Cpf criado a partir da string fornecida.</returns>
  public static implicit operator Cpf(string value) => new(value);

  #endregion
}
