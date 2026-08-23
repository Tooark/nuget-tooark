using Tooark.Enums;
using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um documento.
/// </summary>
public sealed class Document : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do número do documento.
  /// </summary>
  private readonly string _number = string.Empty;

  /// <summary>
  /// Valor privado do tipo do documento.
  /// </summary>
  private readonly EDocumentType _type = EDocumentType.None;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Document com o número e o tipo especificados.
  /// </summary>
  /// <param name="number">O número do documento a ser validado.</param>
  /// <param name="type">O tipo do documento a ser validado. Parâmetro opcional.</param>
  public Document(string number, EDocumentType? type = null)
  {
    // Define o tipo do documento como "None" se não for informado
    type ??= EDocumentType.None;

    // Adiciona as notificações de validação do documento
    AddNotifications(new Validation()
      .Match(number, type.ToRegex(), "Document.Number", "Field.Invalid;Document")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Verifica se o número do Documento é válido
      if (type.IsValid(number))
      {
        // Define o valor do número do documento e o tipo do documento
        _number = number;
        _type = type!;
      }
      else
      {
        // Adiciona uma notificação de validação do Documento
        AddNotification($"Field.Invalid;Document.{type}", "Document.Type", "T.VOJ.DOC1");
      }
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do número do documento.
  /// </summary>
  public string Number { get => _number; }

  /// <summary>
  /// Valor do tipo do documento.
  /// </summary>
  public EDocumentType Type { get => _type; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor do documento.
  /// </summary>
  /// <returns>O valor do documento.</returns>
  public override string ToString() => _number;

  /// <summary>
  /// Define uma conversão implícita de um objeto Document para uma string.
  /// </summary>
  /// <param name="document">O objeto Document a ser convertido.</param>
  /// <returns>Uma string que representa o valor do documento.</returns>
  public static implicit operator string(Document document) =>
    document?._number ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Document.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Document.</param>
  /// <returns>Um objeto Document criado a partir da string fornecida.</returns>
  public static implicit operator Document(string value) => new(value);

  #endregion
}
