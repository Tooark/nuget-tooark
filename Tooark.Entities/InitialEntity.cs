using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tooark.Exceptions;
using Tooark.ValueObjects;

namespace Tooark.Entities;

/// <summary>
/// Classe base abstrata para entidades iniciais.
/// </summary>
/// <remarks>
/// Herda de <see cref="BaseEntity"/> para incluir informações de criação.
/// Esta classe é usada para representar entidades com informações de auditoria,
/// como quem criou a entidade, além de quando esse evento ocorreu.
/// </remarks>
public abstract class InitialEntity : BaseEntity
{
  #region Constructors

  /// <summary>
  /// Construtor vazio para a entidade InitialEntity.
  /// </summary>
  /// <remarks>
  /// Utilizado pelo Entity Framework.
  /// </remarks>
  protected InitialEntity() { }

  /// <summary>
  /// Cria uma nova instância da entidade inicial.
  /// </summary>
  /// <param name="createdById">O identificador do usuário que criou a entidade.</param>
  protected InitialEntity(CreatedBy createdById)
  {
    SetCreatedBy(createdById);
  }

  #endregion

  #region Properties

  /// <summary>
  /// Identificador do usuário que criou a entidade.
  /// </summary>
  /// <value>
  /// O identificador do criador é do tipo <see cref="Guid"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'created_by', é do tipo 'uuid' e é obrigatória.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("created_by", TypeName = "uuid")]
  [Required]
  public Guid CreatedById { get; private set; }

  /// <summary>
  /// Data e hora de criação da entidade.
  /// </summary>
  /// <value>
  /// A data e hora de criação é do tipo <see cref="DateTime"/> em UTC.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'created_at', é do tipo 'timestamp with time zone' e é obrigatória.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("created_at", TypeName = "timestamp with time zone")]
  [Required]
  public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

  #endregion

  #region Methods

  /// <summary>
  /// Define o identificador do criador da entidade e a data e hora de criação.
  /// </summary>
  /// <param name="createdById">O valor do identificador do criador a ser definido.</param>
  /// <exception cref="BadRequestException">
  /// Quando a criação já foi registrada, ou quando o identificador informado está ausente ou é inválido.
  /// </exception>
  public virtual void SetCreatedBy(CreatedBy createdById)
  {
    // A autoria da criação é registrada uma única vez
    if (CreatedById != Guid.Empty)
    {
      throw Failure("Field.ChangeBlocked;CreatedBy", "CreatedBy", "T.ENT.INI1");
    }

    // Valida o argumento sem acumular notificação na entidade
    EnsureValid(createdById, "CreatedBy");

    CreatedById = createdById;
    CreatedAt = DateTime.UtcNow;
  }

  #endregion
}
