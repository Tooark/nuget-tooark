using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tooark.Exceptions;
using Tooark.ValueObjects;

namespace Tooark.Entities;

/// <summary>
/// Classe base abstrata para entidades que suportam exclusão lógica.
/// </summary>
/// <remarks>
/// Herda de <see cref="DetailedEntity"/> para incluir informações para auditoria.
/// Esta classe é usada para representar entidades que podem ser excluídas logicamente.
/// </remarks>
public abstract class SoftDeletableEntity : DetailedEntity
{
  #region Constructors

  /// <summary>
  /// Construtor vazio para a entidade SoftDeletableEntity.
  /// </summary>
  /// <remarks>
  /// Utilizado pelo Entity Framework.
  /// </remarks>
  protected SoftDeletableEntity() { }

  /// <summary>
  /// Cria uma nova instância da entidade exclusão lógica.
  /// </summary>
  /// <param name="createdById">O identificador do usuário que criou a entidade.</param>
  protected SoftDeletableEntity(CreatedBy createdById) : base(createdById) { }

  #endregion

  #region Properties

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
      AddNotification("Record.Deleted", "Entity", "T.ENT.SOF1");
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
      throw Failure("Record.Deleted", "Entity", "T.ENT.SOF1");
    }
  }

  /// <summary>
  /// Marca a entidade como excluída logicamente.
  /// </summary>
  /// <param name="changedById">O identificador do usuário que excluiu a entidade.</param>
  /// <exception cref="BadRequestException">
  /// Quando o identificador informado está ausente ou é inválido.
  /// </exception>
  public void SetDeleted(UpdatedBy changedById)
  {
    // Valida o argumento sem acumular notificação na entidade
    EnsureValid(changedById, "UpdatedBy");

    // Atualiza apenas se não estiver deletada
    if (!Deleted)
    {
      Deleted = true;

      SetUpdatedBy(changedById);
    }
  }

  /// <summary>
  /// Marca a entidade como não excluída logicamente.
  /// </summary>
  /// <param name="changedById">O identificador do usuário que restaurou a entidade.</param>
  /// <exception cref="BadRequestException">
  /// Quando o identificador informado está ausente ou é inválido.
  /// </exception>
  public void SetRestored(UpdatedBy changedById)
  {
    // Valida o argumento sem acumular notificação na entidade
    EnsureValid(changedById, "UpdatedBy");

    // Atualiza apenas se estiver deletada
    if (Deleted)
    {
      Deleted = false;

      SetUpdatedBy(changedById);
    }
  }

  #endregion
}
