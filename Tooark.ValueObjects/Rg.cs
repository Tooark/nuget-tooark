using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um RG.
/// </summary>
public sealed class Rg : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do número do RG.
  /// </summary>
  private readonly string _number = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Rg com o número.
  /// </summary>
  /// <param name="number">O número do RG a ser validado.</param>
  public Rg(string number)
  {
    // Valida documento do tipo RG
    var document = new Document(number, EDocumentType.RG);

    // Adiciona as notificações
    AddNotifications(document);

    // Verifica se é válido, então não existe notificação
    if (IsValid)
    {
      // Define o valor do número da RG
      _number = document;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do número do RG.
  /// </summary>
  public string Number { get => _number; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do RG.
  /// </summary>
  /// <returns>O valor do RG.</returns>
  public override string ToString() => _number;

  /// <summary>
  /// Define uma conversão implícita de um objeto Rg para uma string.
  /// </summary>
  /// <param name="document">O objeto Rg a ser convertido.</param>
  /// <returns>Uma string que representa o valor do RG.</returns>
  public static implicit operator string(Rg document) =>
    document?._number ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Rg.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Rg.</param>
  /// <returns>Um objeto Rg criado a partir da string fornecida.</returns>
  public static implicit operator Rg(string value) => new(value);

  #endregion
}
