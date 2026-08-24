using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tooark.Exceptions;
using Tooark.ValueObjects;

namespace Tooark.Entities;

/// <summary>
/// Classe base abstrata para entidades que precisam de auditoria completa.
/// </summary>
/// <remarks>
/// Herda de <see cref="DetailedEntity"/> para incluir informações para auditoria.
/// Esta classe é usada para representar entidades que precisam de auditoria completa.
/// Contém propriedades para armazenar informações sobre a versão da entidade, se foi excluída logicamente, e se foi restaurada.
/// </remarks>
public abstract class AuditableEntity : DetailedEntity
{
  #region Constructors

  /// <summary>
  /// Construtor vazio para a entidade AuditableEntity.
  /// </summary>
  /// <remarks>
  /// Utilizado pelo Entity Framework.
  /// </remarks>
  protected AuditableEntity() { }

  /// <summary>
  /// Cria uma nova instância da entidade auditoria.
  /// </summary>
  /// <param name="createdById">O identificador do usuário que criou a entidade.</param>
  protected AuditableEntity(CreatedBy createdById) : base(createdById) { }

  #endregion

  #region Properties

  /// <summary>
  /// Versão da entidade.
  /// </summary>
  /// <value>
  /// O valor é do tipo <see cref="long"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'version' é do tipo 'bigint' com valor padrão '1' e é obrigatória.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("version", TypeName = "bigint")]
  [DefaultValue(1)]
  [Required]
  public long Version { get; private set; } = 1;

  /// <summary>
  /// Indica se a entidade foi excluída logicamente.
  /// </summary>
  /// <value>
  /// O valor é do tipo <see cref="bool"/>. O valor padrão é 'false'.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'deleted' é do tipo 'bool' com valor padrão 'false' e é obrigatória.
  /// </remarks>
  [Column("deleted", TypeName = "bool")]
  [DefaultValue(false)]
  [Required]
  public bool Deleted { get; private set; } = false;

  /// <summary>
  /// Identificador do usuário que excluiu a entidade.
  /// </summary>
  /// <value>
  /// O identificador do excluidor é do tipo <see cref="Guid"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'deleted_by' é do tipo 'uuid'.
  /// Só possui valor quando <see cref="Deleted"/> é <c>true</c>.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("deleted_by", TypeName = "uuid")]
  public Guid? DeletedById { get; private set; }

  /// <summary>
  /// Data e hora da exclusão da entidade.
  /// </summary>
  /// <value>
  /// A data e hora da exclusão é do tipo <see cref="DateTime"/> em UTC.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'deleted_at' é do tipo 'timestamp with time zone' e é obrigatória.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("deleted_at", TypeName = "timestamp with time zone")]
  public DateTime? DeletedAt { get; private set; }

  /// <summary>
  /// Identificador do usuário que restaurou a entidade.
  /// </summary>
  /// <value>
  /// O identificador do restaurador é do tipo <see cref="Guid"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'restored_by' é do tipo 'uuid'.
  /// Só possui valor quando a entidade foi restaurada.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("restored_by", TypeName = "uuid")]
  public Guid? RestoredById { get; private set; }

  /// <summary>
  /// Data e hora da restauração da entidade.
  /// </summary>
  /// <value>
  /// A data e hora da restauração é do tipo <see cref="DateTime"/> em UTC.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'restored_at' é do tipo 'timestamp with time zone' e é obrigatória.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("restored_at", TypeName = "timestamp with time zone")]
  public DateTime? RestoredAt { get; private set; }

  #endregion

  #region Private Methods

  /// <summary>
  /// Incrementa a versão da entidade.
  /// </summary>
  private void IncrementVersion()
  {
    Version++;
  }

  #endregion

  #region Methods

  /// <summary>
  /// Valida se a entidade não foi excluída logicamente.
  /// </summary>
  /// <remarks>
  /// Adiciona uma notificação se a entidade foi excluída logicamente.
  /// </remarks>
  public void ValidateNotDeleted()
  {
    // Verifica se a entidade foi excluída logicamente.
    if (Deleted)
    {
      AddNotification("Record.Deleted", "Entity", "T.ENT.AUD1");
    }
  }

  /// <summary>
  /// Garante que a entidade não foi excluída logicamente.
  /// </summary>
  /// <exception cref="BadRequestException">Lançada quando a entidade foi excluída logicamente.</exception>
  public void EnsureNotDeleted()
  {
    // Verifica se a entidade foi excluída logicamente e lança uma exceção se for o caso.
    if (Deleted)
    {
      throw Failure("Record.Deleted", "Entity", "T.ENT.AUD1");
    }
  }

  /// <summary>
  /// Atualiza entidade e incrementa a versão.
  /// </summary>
  /// <param name="updatedById">O valor do identificador do atualizador a ser definido.</param>
  public override void SetUpdatedBy(UpdatedBy updatedById)
  {
    // Define o identificador do atualizador.
    base.SetUpdatedBy(updatedById);

    // Incrementa a versão apenas após atualização bem-sucedida
    IncrementVersion();
  }

  /// <summary>
  /// Marca a entidade como excluída.
  /// </summary>
  /// <param name="deletedById">O valor do identificador do excluidor a ser definido.</param>
  /// <exception cref="BadRequestException">
  /// Quando o identificador informado está ausente ou é inválido.
  /// </exception>
  public void SetDeleted(DeletedBy deletedById)
  {
    // Valida o argumento sem acumular notificação na entidade
    EnsureValid(deletedById, "DeletedBy");

    // Atualiza apenas se não estiver deletada
    if (!Deleted)
    {
      Deleted = true;
      DeletedById = deletedById;
      DeletedAt = DateTime.UtcNow;

      // Excluir é uma alteração: registra o autor e a data, e incrementa a versão uma única vez
      SetUpdatedBy(new UpdatedBy(deletedById));
    }
  }

  /// <summary>
  /// Marca a entidade como restaurada.
  /// </summary>
  /// <param name="restoredById">O valor do identificador do restaurador a ser definido.</param>
  /// <exception cref="BadRequestException">
  /// Quando o identificador informado está ausente ou é inválido.
  /// </exception>
  public void SetRestored(RestoredBy restoredById)
  {
    // Valida o argumento sem acumular notificação na entidade
    EnsureValid(restoredById, "RestoredBy");

    // Atualiza apenas se estiver deletada
    if (Deleted)
    {
      Deleted = false;
      RestoredById = restoredById;
      RestoredAt = DateTime.UtcNow;

      // Restaurar é uma alteração: registra o autor e a data, e incrementa a versão uma única vez
      SetUpdatedBy(new UpdatedBy(restoredById));
    }
  }

  #endregion
}
