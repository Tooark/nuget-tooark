using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tooark.Exceptions;

namespace Tooark.Entities.EntityFrameworkCore.Extensions;

/// <summary>
/// Classe com as extensões que aplicam as convenções das entidades do Tooark ao modelo.
/// </summary>
public static class ModelBuilderExtensions
{
  #region Constants

  /// <summary>
  /// Nome do filtro de excluídos quando a entidade usa filtros nomeados.
  /// </summary>
  /// <remarks>
  /// Filtros nomeados existem a partir do Entity Framework Core 10, que não aceita um filtro sem nome na
  /// mesma entidade. Só nesse caso o filtro de excluídos recebe este nome, e pode ser desligado sozinho com
  /// <c>IgnoreQueryFilters([ModelBuilderExtensions.SoftDeleteFilterName])</c>.
  /// </remarks>
  public const string SoftDeleteFilterName = "Tooark.SoftDelete";

  #endregion

  #region Private Fields

  /// <summary>
  /// Nome da propriedade que marca a exclusão lógica, igual nas duas classes base que a declaram.
  /// </summary>
  private const string DeletedProperty = nameof(SoftDeletableEntity.Deleted);

  /// <summary>
  /// Nome da propriedade de versão, igual nas duas classes base que a declaram.
  /// </summary>
  private const string VersionProperty = nameof(VersionedEntity.Version);

  #endregion

  #region Methods

  /// <summary>
  /// Aplica o filtro de excluídos e o token de concorrência às entidades do Tooark que já estão no modelo.
  /// </summary>
  /// <remarks>
  /// Chame por último no <c>OnModelCreating</c>: o método só enxerga as entidades já adicionadas ao modelo, e
  /// um <c>HasQueryFilter</c> chamado depois substitui o filtro de excluídos. O filtro que a entidade já tem
  /// é preservado e combinado com o de excluídos.
  /// O filtro vale para as descendentes de <see cref="SoftDeletableEntity"/> e de
  /// <see cref="AuditableEntity"/>, e o token de concorrência para as de <see cref="VersionedEntity"/> e de
  /// <see cref="AuditableEntity"/>. Tipos owned ficam de fora, e tipos sem chave recebem só o filtro.
  /// </remarks>
  /// <param name="modelBuilder">O construtor do modelo.</param>
  /// <returns>O construtor do modelo, com as convenções aplicadas.</returns>
  /// <exception cref="ArgumentNullException">Quando o construtor do modelo é nulo.</exception>
  /// <exception cref="InternalServerErrorException">
  /// Quando uma entidade com exclusão lógica faz parte de uma hierarquia cuja raiz não a suporta.
  /// </exception>
  public static ModelBuilder ApplyTooarkConventions(this ModelBuilder modelBuilder)
  {
    ArgumentNullException.ThrowIfNull(modelBuilder);

    // O filtro pertence à raiz da hierarquia, que é visitada uma vez para cada tipo derivado
    var filteredRoots = new HashSet<IMutableEntityType>();

    // A lista é copiada porque alterar o modelo durante a enumeração não é seguro
    foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
    {
      // Um tipo owned não tem filtro próprio nem concorrência própria: ele segue o dono
      if (entityType.IsOwned())
      {
        continue;
      }

      var clrType = entityType.ClrType;

      // O token só faz sentido onde há linha a atualizar, o que exclui os tipos sem chave
      if (IsVersioned(clrType) && entityType.FindPrimaryKey() is not null && entityType.FindProperty(VersionProperty) is { } version)
      {
        version.IsConcurrencyToken = true;
      }

      if (IsSoftDeletable(clrType))
      {
        ApplySoftDeleteFilter(entityType, filteredRoots);
      }
    }

    return modelBuilder;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Verifica se o tipo descende de uma classe base com exclusão lógica.
  /// </summary>
  /// <param name="type">O tipo CLR da entidade.</param>
  /// <returns><c>true</c> quando o tipo suporta exclusão lógica.</returns>
  private static bool IsSoftDeletable(Type type) =>
    typeof(SoftDeletableEntity).IsAssignableFrom(type) || typeof(AuditableEntity).IsAssignableFrom(type);

  /// <summary>
  /// Verifica se o tipo descende de uma classe base com versão.
  /// </summary>
  /// <param name="type">O tipo CLR da entidade.</param>
  /// <returns><c>true</c> quando o tipo tem versão.</returns>
  private static bool IsVersioned(Type type) =>
    typeof(VersionedEntity).IsAssignableFrom(type) || typeof(AuditableEntity).IsAssignableFrom(type);

  /// <summary>
  /// Aplica o filtro de excluídos na raiz da hierarquia da entidade.
  /// </summary>
  /// <param name="entityType">A entidade com exclusão lógica.</param>
  /// <param name="filteredRoots">As raízes que já receberam o filtro.</param>
  /// <exception cref="InternalServerErrorException">Quando a raiz da hierarquia não suporta exclusão lógica.</exception>
  private static void ApplySoftDeleteFilter(IMutableEntityType entityType, HashSet<IMutableEntityType> filteredRoots)
  {
    var root = entityType.GetRootType();

    // O Entity Framework só aceita filtro na raiz da hierarquia. Quando a raiz não tem o campo de exclusão,
    // não há onde aplicá-lo, e seguir em silêncio devolveria as linhas excluídas nas consultas.
    if (!IsSoftDeletable(root.ClrType))
    {
      throw new InternalServerErrorException(
        $"Entities.Conventions.SoftDeleteRootNotSupported;{entityType.DisplayName()};{root.DisplayName()}");
    }

    // A raiz já filtrada cobre os tipos derivados, e sem o campo mapeado não há o que filtrar
    if (!filteredRoots.Add(root) || root.FindProperty(DeletedProperty) is null)
    {
      return;
    }

    var parameter = Expression.Parameter(root.ClrType, "entity");
    var notDeleted = Expression.Not(Expression.Property(parameter, DeletedProperty));

    var declared = root.GetDeclaredQueryFilters();

    // O Entity Framework Core 10 não aceita filtro nomeado e filtro sem nome na mesma entidade: quando a
    // aplicação usa filtros nomeados, o de excluídos entra como mais um deles.
    if (declared.Any(filter => !filter.IsAnonymous))
    {
      root.SetQueryFilter(SoftDeleteFilterName, Expression.Lambda(notDeleted, parameter));

      return;
    }

    var existing = declared.FirstOrDefault(filter => filter.IsAnonymous)?.Expression;

    // Uma entidade tem um único filtro sem nome: definir outro apagaria o da aplicação, então os dois são
    // combinados em uma só expressão.
    var body = existing is null
      ? (Expression)notDeleted
      : Expression.AndAlso(new ParameterReplacer(existing.Parameters[0], parameter).Visit(existing.Body), notDeleted);

    root.SetQueryFilter(Expression.Lambda(body, parameter));
  }

  /// <summary>
  /// Troca o parâmetro de uma expressão, para que dois filtros usem o mesmo parâmetro ao serem combinados.
  /// </summary>
  /// <param name="source">O parâmetro a ser trocado.</param>
  /// <param name="target">O parâmetro que entra no lugar.</param>
  private sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
  {
    /// <summary>
    /// Devolve o parâmetro novo no lugar do antigo.
    /// </summary>
    /// <param name="node">O parâmetro visitado.</param>
    /// <returns>O parâmetro novo, ou o próprio parâmetro quando não é o que deve ser trocado.</returns>
    protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : base.VisitParameter(node);
  }

  #endregion
}
