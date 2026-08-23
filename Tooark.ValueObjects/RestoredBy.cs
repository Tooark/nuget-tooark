using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa um Guid do Restaurado Por.
/// </summary>
public sealed class RestoredBy : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do Guid do Restaurado Por.
  /// </summary>
  private readonly Guid _value = Guid.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe RestoredBy com o valor especificado.
  /// </summary>
  /// <param name="value">O valor do Guid do Restaurado Por a ser validado.</param>
  public RestoredBy(Guid value)
  {
    // Adiciona as notificações de validação do Guid do Restaurado Por.
    AddNotifications(new Validation()
      .IsNotEmpty(value, "RestoredBy", "Field.Invalid;RestoredBy")
    );

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o valor do Guid do Restaurado Por
      _value = value;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor do Guid do Restaurado Por.
  /// </summary>
  public Guid Value { get => _value; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Define uma conversão implícita de um objeto RestoredBy para uma Guid.
  /// </summary>
  /// <param name="restoredBy">O objeto RestoredBy a ser convertido.</param>
  /// <returns>Uma Guid que representa o valor do RestoredBy.</returns>
  public static implicit operator Guid(RestoredBy restoredBy) =>
    restoredBy?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma Guid para um objeto RestoredBy.
  /// </summary>
  /// <param name="value">A Guid a ser convertida em um objeto RestoredBy.</param>
  /// <returns>Um objeto RestoredBy criado a partir do Guid fornecida.</returns>
  public static implicit operator RestoredBy(Guid value) => new(value);

  #endregion
}
