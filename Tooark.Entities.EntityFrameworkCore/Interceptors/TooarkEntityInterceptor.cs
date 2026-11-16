using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Entities.EntityFrameworkCore.Interfaces;
using Tooark.Entities.EntityFrameworkCore.Options;
using Tooark.Exceptions;
using Tooark.ValueObjects;

namespace Tooark.Entities.EntityFrameworkCore.Interceptors;

/// <summary>
/// Interceptor que preenche os campos de auditoria e converte a remoção em exclusão lógica ao salvar.
/// </summary>
/// <remarks>
/// O interceptor não guarda estado entre gravações: o usuário é resolvido a cada <c>SaveChanges</c>, no
/// provedor de serviços em que o interceptor foi criado. Por isso a mesma instância pode atender todos os
/// contextos de um pool.
/// A decisão de carimbar parte do rastreamento de alterações: a entidade que a aplicação já carimbou nesta
/// unidade de trabalho não é carimbada de novo, o que evita o incremento duplo da versão.
/// </remarks>
/// <param name="serviceProvider">O provedor de serviços de onde o usuário atual é resolvido a cada gravação.</param>
/// <param name="options">As opções da auditoria e da exclusão lógica.</param>
public sealed class TooarkEntityInterceptor(IServiceProvider serviceProvider, IOptions<EntityAuditOptions> options) : SaveChangesInterceptor
{
  #region Private Fields

  /// <summary>
  /// Nome da propriedade que marca a exclusão lógica, igual nas duas classes base que a declaram.
  /// </summary>
  private const string DeletedProperty = nameof(SoftDeletableEntity.Deleted);

  /// <summary>
  /// Propriedades que o <c>SetUpdatedBy</c> altera, usadas para reconhecer a entidade já carimbada.
  /// </summary>
  private static readonly string[] StampProperties =
  [
    nameof(DetailedEntity.UpdatedAt),
    nameof(DetailedEntity.UpdatedById),
    nameof(VersionedEntity.Version)
  ];

  #endregion

  #region Methods

  /// <summary>
  /// Aplica a auditoria e a exclusão lógica antes da gravação síncrona.
  /// </summary>
  /// <param name="eventData">Os dados do evento, com o contexto que está sendo salvo.</param>
  /// <param name="result">O resultado da interceptação recebido dos interceptores anteriores.</param>
  /// <returns>O resultado da interceptação, sem alteração.</returns>
  /// <exception cref="InternalServerErrorException">
  /// Quando a gravação precisa de um autor e não há usuário atual nem usuário de sistema, ou quando o usuário
  /// atual não pode ser resolvido.
  /// </exception>
  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
  {
    Apply(eventData.Context);

    return base.SavingChanges(eventData, result);
  }

  /// <summary>
  /// Aplica a auditoria e a exclusão lógica antes da gravação assíncrona.
  /// </summary>
  /// <param name="eventData">Os dados do evento, com o contexto que está sendo salvo.</param>
  /// <param name="result">O resultado da interceptação recebido dos interceptores anteriores.</param>
  /// <param name="cancellationToken">O token de cancelamento.</param>
  /// <returns>O resultado da interceptação, sem alteração.</returns>
  /// <exception cref="InternalServerErrorException">
  /// Quando a gravação precisa de um autor e não há usuário atual nem usuário de sistema, ou quando o usuário
  /// atual não pode ser resolvido.
  /// </exception>
  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default
  )
  {
    Apply(eventData.Context);

    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Percorre as entradas rastreadas e aplica a exclusão lógica e os carimbos de auditoria.
  /// </summary>
  /// <param name="context">O contexto que está sendo salvo.</param>
  private void Apply(DbContext? context)
  {
    var settings = options.Value;

    // Sem contexto ou com os dois recursos desligados não há o que fazer
    if (context is null || (!settings.StampAudit && !settings.SoftDelete))
    {
      return;
    }

    // O SaveChanges aplica a cascata logo depois do interceptor, qualquer que seja o momento configurado no
    // contexto. Antecipá-la mostra ao interceptor os dependentes e os órfãos que seriam removidos, que de
    // outro modo passariam por ele ainda sem marca quando a cascata é adiada.
    if (settings.SoftDelete)
    {
      context.ChangeTracker.CascadeChanges();
    }

    // O interceptor roda antes da detecção de alterações do SaveChanges. Entries() dispara a detecção quando
    // ela é automática, e a lista é materializada porque o laço troca o estado das entradas.
    var entries = context.ChangeTracker.Entries()
      .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
      .ToList();

    // O usuário é resolvido uma vez por gravação e só quando alguma entrada precisa dele: salvar o que não
    // tem auditoria não exige usuário.
    Guid? resolved = null;
    Guid UserId() => resolved ??= ResolveUserId();

    foreach (var entry in entries)
    {
      // O estado é lido de novo a cada entrada: a conversão de um dono devolve as partes dele ao rastreio
      switch (entry.State)
      {
        case EntityState.Deleted when settings.SoftDelete:
          SoftDelete(entry, UserId);
          break;

        case EntityState.Added when settings.StampAudit:
          StampCreated(entry, UserId);
          break;

        case EntityState.Modified when settings.StampAudit:
          StampUpdated(entry, UserId);
          break;
      }
    }
  }

  /// <summary>
  /// Converte a remoção em exclusão lógica quando a entidade a suporta.
  /// </summary>
  /// <param name="entry">A entrada marcada para remoção.</param>
  /// <param name="userId">A função que resolve o autor.</param>
  private static void SoftDelete(EntityEntry entry, Func<Guid> userId)
  {
    // Só as entidades com o campo de exclusão mapeado podem ser excluídas logicamente
    if (entry.Entity is not (SoftDeletableEntity or AuditableEntity) || entry.Metadata.FindProperty(DeletedProperty) is null)
    {
      return;
    }

    // A linha que já está excluída no banco é removida de verdade. O valor original é o que decide: a
    // entidade que a aplicação marcou com SetDeleted nesta unidade de trabalho ainda não foi gravada como
    // excluída, e removê-la aqui apagaria a linha.
    if (entry.Property(DeletedProperty).OriginalValue is true)
    {
      return;
    }

    // Resolve o autor antes de mexer no rastreio, para que a falha não deixe a entrada pela metade
    var author = userId();

    // Voltar para Unchanged, e não para Modified, mantém o UPDATE restrito ao que mudou de fato: a detecção
    // logo abaixo compara os valores atuais com os originais, que a remoção não descartou.
    entry.State = EntityState.Unchanged;

    // O Remove() do dono também marcou as partes dele (owned types) para remoção
    RestoreOwned(entry);

    // Cada classe base recebe o autor em um objeto de valor diferente. As duas ignoram a chamada quando a
    // entidade já está marcada, então a versão sobe uma única vez.
    switch (entry.Entity)
    {
      case SoftDeletableEntity softDeletable:
        softDeletable.SetDeleted(new UpdatedBy(author));
        break;

      case AuditableEntity auditable:
        auditable.SetDeleted(new DeletedBy(author));
        break;
    }

    // Os métodos da entidade alteram as propriedades depois da detecção já ter rodado
    entry.DetectChanges();
  }

  /// <summary>
  /// Devolve ao rastreio as partes (owned types) que o Entity Framework marcou para remoção junto com o dono.
  /// </summary>
  /// <remarks>
  /// Sem isso, a exclusão lógica do dono gravaria nulo nas colunas das partes que dividem a tabela com ele,
  /// ou apagaria as linhas das partes que têm tabela própria.
  /// </remarks>
  /// <param name="owner">A entrada do dono que voltou ao rastreio.</param>
  private static void RestoreOwned(EntityEntry owner)
  {
    foreach (var navigation in owner.Navigations)
    {
      // Só interessam as navegações do dono para as partes dele
      if (navigation.Metadata is not INavigation { IsOnDependent: false, ForeignKey.IsOwnership: true })
      {
        continue;
      }

      switch (navigation)
      {
        case ReferenceEntry { TargetEntry: { } target }:
          RestoreOwnedEntry(target);
          break;

        case CollectionEntry { CurrentValue: { } items } collection:
          foreach (var item in items)
          {
            if (collection.FindEntry(item) is { } target)
            {
              RestoreOwnedEntry(target);
            }
          }

          break;
      }
    }
  }

  /// <summary>
  /// Devolve uma parte ao rastreio, junto com as partes aninhadas nela.
  /// </summary>
  /// <param name="owned">A entrada da parte.</param>
  private static void RestoreOwnedEntry(EntityEntry owned)
  {
    // A parte que não foi marcada para remoção não precisa de ajuste
    if (owned.State != EntityState.Deleted)
    {
      return;
    }

    owned.State = EntityState.Unchanged;

    RestoreOwned(owned);

    // Recupera as alterações que a aplicação fez na parte antes de remover o dono
    owned.DetectChanges();
  }

  /// <summary>
  /// Registra o autor da criação quando a aplicação ainda não o informou.
  /// </summary>
  /// <param name="entry">A entrada adicionada.</param>
  /// <param name="userId">A função que resolve o autor.</param>
  private static void StampCreated(EntityEntry entry, Func<Guid> userId)
  {
    // O SetCreatedBy lança quando a criação já foi registrada, então só é chamado com o autor ainda vazio.
    // A linha inserida recebe os valores atuais da entidade, sem depender de nova detecção.
    if (entry.Entity is InitialEntity { CreatedById: var createdById } initial && createdById == Guid.Empty)
    {
      initial.SetCreatedBy(new CreatedBy(userId()));
    }
  }

  /// <summary>
  /// Registra o autor da atualização quando a aplicação ainda não o fez nesta unidade de trabalho.
  /// </summary>
  /// <param name="entry">A entrada modificada.</param>
  /// <param name="userId">A função que resolve o autor.</param>
  private static void StampUpdated(EntityEntry entry, Func<Guid> userId)
  {
    // O SetUpdatedBy das entidades versionadas incrementa a versão: carimbar o que a aplicação já carimbou
    // contaria a mesma alteração duas vezes.
    if (entry.Entity is not DetailedEntity detailed || IsAlreadyStamped(entry))
    {
      return;
    }

    detailed.SetUpdatedBy(new UpdatedBy(userId()));

    // Os métodos da entidade alteram as propriedades depois da detecção já ter rodado
    entry.DetectChanges();
  }

  /// <summary>
  /// Verifica se a entidade já recebeu o carimbo de atualização desde que foi carregada ou salva.
  /// </summary>
  /// <remarks>
  /// A comparação é entre o valor atual e o original, e não pelo indicador de propriedade modificada: o
  /// <c>Update()</c> de uma entidade desconectada marca todas as propriedades como modificadas sem que a
  /// aplicação tenha carimbado nada.
  /// </remarks>
  /// <param name="entry">A entrada modificada.</param>
  /// <returns><c>true</c> quando algum campo do carimbo difere do valor original.</returns>
  private static bool IsAlreadyStamped(EntityEntry entry)
  {
    foreach (var name in StampProperties)
    {
      // A versão só existe em parte das entidades, e a aplicação pode ter tirado um campo do modelo
      if (entry.Metadata.FindProperty(name) is null)
      {
        continue;
      }

      var property = entry.Property(name);

      if (!Equals(property.CurrentValue, property.OriginalValue))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Resolve o identificador do autor da gravação.
  /// </summary>
  /// <returns>O identificador do usuário atual ou, na falta dele, o do usuário de sistema.</returns>
  /// <exception cref="InternalServerErrorException">
  /// Quando o usuário atual não pode ser resolvido, ou quando não há usuário atual nem usuário de sistema.
  /// </exception>
  private Guid ResolveUserId()
  {
    ICurrentUser? currentUser;

    try
    {
      // O registro é opcional: um worker pode trabalhar só com o usuário de sistema
      currentUser = serviceProvider.GetService<ICurrentUser>();
    }
    catch (InvalidOperationException exception)
    {
      // O caso comum é o usuário com escopo pedido ao provedor raiz, que é o que o AddDbContextPool e as
      // opções singleton entregam ao interceptor. A exceção original segue como causa.
      throw new InternalServerErrorException("Entities.Audit.CurrentUserNotResolved", exception);
    }

    var id = Normalize(currentUser?.Id) ?? Normalize(options.Value.SystemUserId);

    // Gravar a linha com autor vazio em silêncio é pior do que falhar
    return id ?? throw new InternalServerErrorException("Entities.Audit.CurrentUserRequired");
  }

  /// <summary>
  /// Trata o identificador vazio como ausente.
  /// </summary>
  /// <param name="id">O identificador informado.</param>
  /// <returns>O identificador, ou <c>null</c> quando ele é nulo ou vazio.</returns>
  private static Guid? Normalize(Guid? id) => id == Guid.Empty ? null : id;

  #endregion
}
