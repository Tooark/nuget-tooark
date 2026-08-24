using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tooark.ValueObjects;

namespace Tooark.Entities;

/// <summary>
/// Classe base abstrata para entidades que suportam versionamento.
/// </summary>
/// <remarks>
/// Herda de <see cref="DetailedEntity"/> para incluir informações para auditoria.
/// Esta classe é usada para representar entidades que suportam versionamento.
/// </remarks>
public abstract class VersionedEntity : DetailedEntity
{
  #region Constructors

  /// <summary>
  /// Construtor vazio para a entidade VersionedEntity.
  /// </summary>
  /// <remarks>
  /// Utilizado pelo Entity Framework.
  /// </remarks>
  protected VersionedEntity() { }

  /// <summary>
  /// Cria uma nova instância da entidade versionamento.
  /// </summary>
  /// <param name="createdById">O identificador do usuário que criou a entidade.</param>
  protected VersionedEntity(CreatedBy createdById) : base(createdById) { }

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
  /// Incrementa a versão da entidade.
  /// </summary>
  /// <remarks>
  /// Este método deve ser chamado sempre que a entidade for atualizada.
  /// </remarks>
  /// <param name="updatedById">O identificador do usuário que atualizou a entidade.</param>
  public override void SetUpdatedBy(UpdatedBy updatedById)
  {
    // Define o identificador do atualizador.
    base.SetUpdatedBy(updatedById);

    // Incrementa a versão apenas após atualização bem-sucedida
    IncrementVersion();
  }

  #endregion
}
