using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um CPF, RG ou CNH.
/// </summary>
public sealed class CpfRgCnh : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do número do CPF, RG ou CNH.
  /// </summary>
  private readonly string _number = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe CpfRgCnh com o número.
  /// </summary>
  /// <param name="number">O número do CPF, RG ou CNH a ser validado.</param>
  public CpfRgCnh(string number)
  {
    // Valida documento do tipo CPF, RG OU CNH
    var document = new Document(number, EDocumentType.CPF_RG_CNH);

    // Adiciona as notificações
    AddNotifications(document);

    // Verifica se é válido, então não existe notificação
    if (IsValid)
    {
      // Define o valor do número da CPF, RG OU CNH
      _number = document;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do número do CPF, RG ou CNH.
  /// </summary>
  public string Number { get => _number; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do CPF, RG ou CNH.
  /// </summary>
  /// <returns>O valor do CPF, RG ou CNH.</returns>
  public override string ToString() => _number;

  /// <summary>
  /// Define uma conversão implícita de um objeto CpfRgCnh para uma string.
  /// </summary>
  /// <param name="document">O objeto CpfRgCnh a ser convertido.</param>
  /// <returns>Uma string que representa o valor do CPF, RG ou CNH.</returns>
  public static implicit operator string(CpfRgCnh document) =>
    document?._number ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto CpfRgCnh.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto CpfRgCnh.</param>
  /// <returns>Um objeto CpfRgCnh criado a partir da string fornecida.</returns>
  public static implicit operator CpfRgCnh(string value) => new(value);

  #endregion
}
