using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um Guid do Atualizado Por.
/// </summary>
public sealed class UpdatedBy : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do Guid do Atualizado Por.
  /// </summary>
  private readonly Guid _value = Guid.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe UpdatedBy com o valor especificado.
  /// </summary>
  /// <param name="value">O valor do Guid do Atualizado Por a ser validado.</param>
  public UpdatedBy(Guid value)
  {
    // Adiciona as notificações de validação do Guid do Atualizado Por.
    AddNotifications(new Validation()
      .IsNotEmpty(value, "UpdatedBy", "Field.Invalid;UpdatedBy")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor do Guid do Atualizado Por
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor do Guid do Atualizado Por.
  /// </summary>
  public Guid Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Define uma conversão implícita de um objeto UpdatedBy para uma Guid.
  /// </summary>
  /// <param name="updatedBy">O objeto UpdatedBy a ser convertido.</param>
  /// <returns>Uma Guid que representa o valor do UpdatedBy.</returns>
  public static implicit operator Guid(UpdatedBy updatedBy) =>
    updatedBy?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma Guid para um objeto UpdatedBy.
  /// </summary>
  /// <param name="value">A Guid a ser convertida em um objeto UpdatedBy.</param>
  /// <returns>Um objeto UpdatedBy criado a partir do Guid fornecida.</returns>
  public static implicit operator UpdatedBy(Guid value) => new(value);

  #endregion
}
