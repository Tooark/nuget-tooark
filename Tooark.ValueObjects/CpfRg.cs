using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um CPF ou RG.
/// </summary>
public sealed class CpfRg : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do número do CPF ou RG.
  /// </summary>
  private readonly string _number = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe CpfRg com o número.
  /// </summary>
  /// <param name="number">O número do CPF ou RG a ser validado.</param>
  public CpfRg(string number)
  {
    // Valida documento do tipo CPF ou RG
    var document = new Document(number, EDocumentType.CPF_RG);

    // Adiciona as notificações
    AddNotifications(document);

    // Verifica se é válido, então não existe notificação
    if (IsValid)
    {
      // Define o valor do número da CPF ou RG
      _number = document;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do número do CPF ou RG.
  /// </summary>
  public string Number { get => _number; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do CPF ou RG.
  /// </summary>
  /// <returns>O valor do CPF ou RG.</returns>
  public override string ToString() => _number;

  /// <summary>
  /// Define uma conversão implícita de um objeto CpfRg para uma string.
  /// </summary>
  /// <param name="document">O objeto CpfRg a ser convertido.</param>
  /// <returns>Uma string que representa o valor do CPF ou RG.</returns>
  public static implicit operator string(CpfRg document) =>
    document?._number ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto CpfRg.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto CpfRg.</param>
  /// <returns>Um objeto CpfRg criado a partir da string fornecida.</returns>
  public static implicit operator CpfRg(string value) => new(value);

  #endregion
}
