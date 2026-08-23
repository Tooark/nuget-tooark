using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um Guid do Criado Por.
/// </summary>
public sealed class CreatedBy : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do Guid do Criado Por.
  /// </summary>
  private readonly Guid _value = Guid.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe CreatedBy com o valor especificado.
  /// </summary>
  /// <param name="value">O valor do Guid do Criado Por a ser validado.</param>
  public CreatedBy(Guid value)
  {
    // Adiciona as notificações de validação do Guid do Criado Por.
    AddNotifications(new Validation()
      .IsNotEmpty(value, "CreatedBy", "Field.Invalid;CreatedBy")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor do Guid do Criado Por
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor do Guid do Criado Por.
  /// </summary>
  public Guid Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Define uma conversão implícita de um objeto CreatedBy para uma Guid.
  /// </summary>
  /// <param name="createdBy">O objeto CreatedBy a ser convertido.</param>
  /// <returns>Uma Guid que representa o valor do CreatedBy.</returns>
  public static implicit operator Guid(CreatedBy createdBy) =>
    createdBy?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma Guid para um objeto CreatedBy.
  /// </summary>
  /// <param name="value">A Guid a ser convertida em um objeto CreatedBy.</param>
  /// <returns>Um objeto CreatedBy criado a partir do Guid fornecida.</returns>
  public static implicit operator CreatedBy(Guid value) => new(value);

  #endregion
}
