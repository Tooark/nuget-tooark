using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um Guid do Deletado Por.
/// </summary>
public sealed class DeletedBy : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do Guid do Deletado Por.
  /// </summary>
  private readonly Guid _value = Guid.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe DeletedBy com o valor especificado.
  /// </summary>
  /// <param name="value">O valor do Guid do Deletado Por a ser validado.</param>
  public DeletedBy(Guid value)
  {
    // Adiciona as notificações de validação do Guid do Deletado Por.
    AddNotifications(new Validation()
      .IsNotEmpty(value, "DeletedBy", "Field.Invalid;DeletedBy")
    );

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o valor do Guid do Deletado Por
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor do Guid do Deletado Por.
  /// </summary>
  public Guid Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Define uma conversão implícita de um objeto DeletedBy para uma Guid.
  /// </summary>
  /// <param name="deletedBy">O objeto DeletedBy a ser convertido.</param>
  /// <returns>Uma Guid que representa o valor do DeletedBy.</returns>
  public static implicit operator Guid(DeletedBy deletedBy) =>
    deletedBy?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma Guid para um objeto DeletedBy.
  /// </summary>
  /// <param name="value">A Guid a ser convertida em um objeto DeletedBy.</param>
  /// <returns>Um objeto DeletedBy criado a partir do Guid fornecida.</returns>
  public static implicit operator DeletedBy(Guid value) => new(value);

  #endregion
}
