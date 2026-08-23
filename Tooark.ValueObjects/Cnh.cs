using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa uma CNH.
/// </summary>
public sealed class Cnh : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do número da CNH.
  /// </summary>
  private readonly string _number = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Cnh com o número.
  /// </summary>
  /// <param name="number">O número da CNH a ser validado.</param>
  public Cnh(string number)
  {
    // Valida documento do tipo CNH
    var document = new Document(number, EDocumentType.CNH);

    // Adiciona as notificações
    AddNotifications(document);

    // Verifica se é válido, então não existe notificação
    if (IsValid)
    {
      // Define o valor do número da CNH
      _number = document;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do número da CNH.
  /// </summary>
  public string Number { get => _number; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor da CNH.
  /// </summary>
  /// <returns>O valor da CNH.</returns>
  public override string ToString() => _number;

  /// <summary>
  /// Define uma conversão implícita de um objeto Cnh para uma string.
  /// </summary>
  /// <param name="document">O objeto Cnh a ser convertido.</param>
  /// <returns>Uma string que representa o valor da CNH.</returns>
  public static implicit operator string(Cnh document) =>
    document?._number ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Cnh.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Cnh.</param>
  /// <returns>Um objeto Cnh criado a partir da string fornecida.</returns>
  public static implicit operator Cnh(string value) => new(value);

  #endregion
}
