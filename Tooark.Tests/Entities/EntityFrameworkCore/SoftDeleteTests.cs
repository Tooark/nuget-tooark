using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Tooark.Entities.EntityFrameworkCore.Extensions;
using Tooark.Exceptions;

namespace Tooark.Tests.Entities.EntityFrameworkCore;

public class SoftDeleteTests : EntityFrameworkCoreTestBase
{
  // Testa se o Remove de uma SoftDeletableEntity vira atualização, pelos caminhos síncrono e assíncrono
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Remove_ShouldSoftDelete_WhenEntityIsSoftDeletable(bool async)
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);
    var category = new Category { Name = "Papelaria" };

    var seed = NewContext(provider);
    seed.Categories.Add(category);
    await SaveAsync(seed, async);

    // Act - outro usuário remove a categoria
    user.Id = OtherUserId;

    var context = NewContext(provider);
    context.Categories.Remove(await context.Categories.SingleAsync(Token));
    await SaveAsync(context, async);

    // Assert - a linha continua no banco, marcada como excluída por quem removeu
    var verification = NewContext(provider);
    var reloaded = await verification.Categories.IgnoreQueryFilters().SingleAsync(Token);

    Assert.True(reloaded.Deleted);
    Assert.Equal(UserId, reloaded.CreatedById);
    Assert.Equal(OtherUserId, reloaded.UpdatedById);
    Assert.True(reloaded.UpdatedAt > reloaded.CreatedAt);
    Assert.Equal("Papelaria", reloaded.Name);
    Assert.Empty(await verification.Categories.ToListAsync(Token));
  }

  // Testa se o Remove de uma AuditableEntity registra quem excluiu, quando, e incrementa a versão uma vez
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Remove_ShouldSoftDeleteWithDeletionFields_WhenEntityIsAuditable(bool async)
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Orders.Add(new Order { Number = "P-1" });
    await SaveAsync(seed, async);

    // Act
    user.Id = OtherUserId;

    var context = NewContext(provider);
    context.Orders.Remove(await context.Orders.SingleAsync(Token));
    await SaveAsync(context, async);

    // Assert
    var verification = NewContext(provider);
    var reloaded = await verification.Orders.IgnoreQueryFilters().SingleAsync(Token);

    Assert.True(reloaded.Deleted);
    Assert.Equal(OtherUserId, reloaded.DeletedById);
    Assert.NotNull(reloaded.DeletedAt);
    Assert.Equal(OtherUserId, reloaded.UpdatedById);
    Assert.Equal(2, reloaded.Version);
    Assert.Null(reloaded.RestoredById);
    Assert.Empty(await verification.Orders.ToListAsync(Token));
  }

  // Testa se a entidade sem exclusão lógica é apagada de verdade
  [Fact]
  public async Task Remove_ShouldDeleteRow_WhenEntityIsNotSoftDeletable()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    seed.Notes.Add(new Note { Text = "Lembrete" });
    await seed.SaveChangesAsync(Token);

    // Act
    var context = NewContext(provider);
    context.Documents.Remove(await context.Documents.SingleAsync(Token));
    context.Notes.Remove(await context.Notes.SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);

    Assert.Equal(0, await verification.Documents.IgnoreQueryFilters().CountAsync(Token));
    Assert.Equal(0, await verification.Notes.IgnoreQueryFilters().CountAsync(Token));
  }

  // Testa se remover de novo a entidade que já está excluída no banco apaga a linha
  [Fact]
  public async Task Remove_ShouldDeleteRow_WhenEntityIsAlreadySoftDeleted()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Categories.Add(new Category { Name = "Papelaria" });
    seed.Orders.Add(new Order { Number = "P-1" });
    await seed.SaveChangesAsync(Token);

    var first = NewContext(provider);
    first.Categories.Remove(await first.Categories.SingleAsync(Token));
    first.Orders.Remove(await first.Orders.SingleAsync(Token));
    await first.SaveChangesAsync(Token);

    // Act - a segunda remoção parte da linha lida já como excluída
    var second = NewContext(provider);
    second.Categories.Remove(await second.Categories.IgnoreQueryFilters().SingleAsync(Token));
    second.Orders.Remove(await second.Orders.IgnoreQueryFilters().SingleAsync(Token));
    await second.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);

    Assert.Equal(0, await verification.Categories.IgnoreQueryFilters().CountAsync(Token));
    Assert.Equal(0, await verification.Orders.IgnoreQueryFilters().CountAsync(Token));
  }

  // Testa se a entidade que o handler marcou com SetDeleted e depois removeu continua sendo exclusão lógica
  [Fact]
  public async Task Remove_ShouldSoftDeleteOnce_WhenHandlerAlreadyCalledSetDeleted()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order { Number = "P-1" });
    await seed.SaveChangesAsync(Token);

    // Act - a decisão de apagar a linha vem do valor gravado, não do valor em memória
    var context = NewContext(provider);
    var order = await context.Orders.SingleAsync(Token);
    order.SetDeleted(OtherUserId);
    context.Orders.Remove(order);
    await context.SaveChangesAsync(Token);

    // Assert - a linha fica, com o autor que o handler informou e um único incremento de versão
    var verification = NewContext(provider);
    var reloaded = await verification.Orders.IgnoreQueryFilters().SingleAsync(Token);

    Assert.True(reloaded.Deleted);
    Assert.Equal(OtherUserId, reloaded.DeletedById);
    Assert.Equal(2, reloaded.Version);
  }

  // Testa se a alteração feita antes do Remove é gravada junto com a exclusão lógica
  [Fact]
  public async Task Remove_ShouldKeepPendingChanges_WhenEntityWasModifiedBeforeRemove()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Categories.Add(new Category { Name = "Papelaria" });
    await seed.SaveChangesAsync(Token);

    // Act
    var context = NewContext(provider);
    var category = await context.Categories.SingleAsync(Token);
    category.Name = "Escritório";
    context.Categories.Remove(category);
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);
    var reloaded = await verification.Categories.IgnoreQueryFilters().SingleAsync(Token);

    Assert.True(reloaded.Deleted);
    Assert.Equal("Escritório", reloaded.Name);
  }

  // Testa se a exclusão lógica chega ao UPDATE mesmo com a detecção automática de alterações desligada
  [Fact]
  public async Task Remove_ShouldSoftDelete_WhenAutoDetectChangesIsDisabled()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order { Number = "P-1" });
    await seed.SaveChangesAsync(Token);

    // Act - sem a detecção automática, o SaveChanges não enxerga o que o SetDeleted alterou
    var context = NewContext(provider);
    context.ChangeTracker.AutoDetectChangesEnabled = false;
    context.Orders.Remove(await context.Orders.SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var reloaded = await NewContext(provider).Orders.IgnoreQueryFilters().SingleAsync(Token);

    Assert.True(reloaded.Deleted);
    Assert.Equal(UserId, reloaded.DeletedById);
    Assert.Equal(2, reloaded.Version);
  }

  // Testa se a exclusão lógica desligada apaga a linha
  [Fact]
  public async Task Remove_ShouldDeleteRow_WhenSoftDeleteIsDisabled()
  {
    // Arrange
    var provider = BuildProvider(options => options.SoftDelete = false);

    var seed = NewContext(provider);
    seed.Categories.Add(new Category { Name = "Papelaria" });
    seed.Orders.Add(new Order { Number = "P-1" });
    await seed.SaveChangesAsync(Token);

    // Act
    var context = NewContext(provider);
    context.Categories.Remove(await context.Categories.SingleAsync(Token));
    context.Orders.Remove(await context.Orders.SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);

    Assert.Equal(0, await verification.Categories.IgnoreQueryFilters().CountAsync(Token));
    Assert.Equal(0, await verification.Orders.IgnoreQueryFilters().CountAsync(Token));
  }

  // Testa se a exclusão lógica registra o autor mesmo com o carimbo de auditoria desligado
  [Fact]
  public async Task Remove_ShouldSoftDeleteWithAuthor_WhenStampAuditIsDisabled()
  {
    // Arrange
    var provider = BuildProvider(options => options.StampAudit = false);

    var seed = NewContext(provider);
    seed.Categories.Add(new Category(UserId) { Name = "Papelaria" });
    await seed.SaveChangesAsync(Token);

    // Act
    var context = NewContext(provider);
    context.Categories.Remove(await context.Categories.SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);
    var reloaded = await verification.Categories.IgnoreQueryFilters().SingleAsync(Token);

    Assert.True(reloaded.Deleted);
    Assert.Equal(UserId, reloaded.UpdatedById);
  }

  // Testa se a exclusão lógica sem usuário falha e não altera a linha
  [Fact]
  public async Task Remove_ShouldThrow_WhenThereIsNoUser()
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Categories.Add(new Category { Name = "Papelaria" });
    await seed.SaveChangesAsync(Token);

    // Act
    user.Id = null;

    var context = NewContext(provider);
    context.Categories.Remove(await context.Categories.SingleAsync(Token));

    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(() => context.SaveChangesAsync(Token));

    // Assert - nada foi gravado, e a entrada continua marcada para remoção
    Assert.Contains("Entities.Audit.CurrentUserRequired", exception.Message);
    Assert.Equal(EntityState.Deleted, context.ChangeTracker.Entries<Category>().Single().State);

    var verification = NewContext(provider);

    Assert.False((await verification.Categories.SingleAsync(Token)).Deleted);
  }

  // Testa a cascata: dependentes com exclusão lógica são excluídos logicamente e os demais são apagados
  [Fact]
  public async Task Remove_ShouldSoftDeleteSoftDeletableDependentsAndDeleteTheOthers_WhenPrincipalIsRemoved()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order
    {
      Number = "P-1",
      Items = [new OrderItem { Name = "Caneta" }, new OrderItem { Name = "Caderno" }],
      Logs = [new OrderLog { Text = "Criado" }]
    });
    await seed.SaveChangesAsync(Token);

    // Act - com os dependentes carregados, o Entity Framework já os marcou para remoção no Remove do principal
    var context = NewContext(provider);
    var order = await context.Orders.Include(o => o.Items).Include(o => o.Logs).SingleAsync(Token);
    context.Orders.Remove(order);
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);
    var items = await verification.OrderItems.IgnoreQueryFilters().ToListAsync(Token);

    Assert.True((await verification.Orders.IgnoreQueryFilters().SingleAsync(Token)).Deleted);
    Assert.Equal(2, items.Count);
    Assert.All(items, item => Assert.True(item.Deleted));
    Assert.All(items, item => Assert.Equal(order.Id, item.OrderId));
    Assert.Equal(0, await verification.OrderLogs.CountAsync(Token));
  }

  // Testa se os dependentes que não foram carregados continuam ativos: a cascata do banco não dispara
  [Fact]
  public async Task Remove_ShouldLeaveUnloadedDependentsUntouched_WhenPrincipalIsSoftDeleted()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order
    {
      Number = "P-1",
      Items = [new OrderItem { Name = "Caneta" }],
      Logs = [new OrderLog { Text = "Criado" }]
    });
    await seed.SaveChangesAsync(Token);

    // Act - sem Include, só o principal está rastreado
    var context = NewContext(provider);
    context.Orders.Remove(await context.Orders.SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);

    Assert.True((await verification.Orders.IgnoreQueryFilters().SingleAsync(Token)).Deleted);
    Assert.False((await verification.OrderItems.IgnoreQueryFilters().SingleAsync(Token)).Deleted);
    Assert.Equal(1, await verification.OrderLogs.CountAsync(Token));
  }

  // Testa se o dependente opcional carregado perde a referência ao principal, que o Entity Framework anula no Remove
  [Fact]
  public async Task Remove_ShouldNullOptionalForeignKeyOfLoadedDependents_WhenPrincipalIsSoftDeleted()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order { Number = "P-1", Shipments = [new Shipment { Code = "E-1" }] });
    await seed.SaveChangesAsync(Token);

    // Act
    var context = NewContext(provider);
    context.Orders.Remove(await context.Orders.Include(o => o.Shipments).SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert - a linha do principal continua no banco, mas o dependente foi desligado dele
    var verification = NewContext(provider);

    Assert.True((await verification.Orders.IgnoreQueryFilters().SingleAsync(Token)).Deleted);
    Assert.Null((await verification.Shipments.SingleAsync(Token)).OrderId);
  }

  // Testa se o dependente obrigatório com exclusão lógica é apagado junto com o principal apagado de verdade
  [Fact]
  public async Task Remove_ShouldReallyDeleteSoftDeletableDependents_WhenPrincipalIsReallyDeleted()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Articles.Add(new Article { Title = "Novidades", Comments = [new Comment { Text = "Ótimo" }] });
    await seed.SaveChangesAsync(Token);

    // Act - o Entity Framework reaplica a cascata ao salvar, depois do interceptor
    var context = NewContext(provider);
    context.Articles.Remove(await context.Articles.Include(a => a.Comments).SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);

    Assert.Equal(0, await verification.Articles.CountAsync(Token));
    Assert.Equal(0, await verification.Comments.IgnoreQueryFilters().CountAsync(Token));
  }

  // Testa o mesmo caso sem ON DELETE CASCADE no banco: quem apaga o dependente é o Entity Framework
  [Fact]
  public async Task Remove_ShouldReallyDeleteSoftDeletableDependents_WhenPrincipalIsReallyDeletedWithoutDatabaseCascade()
  {
    // Arrange
    var provider = BuildInterceptorProvider();
    var connection = OpenConnection();
    var seed = NewStandaloneContext<ClientCascadeDbContext>(options => new ClientCascadeDbContext(options), connection, provider);

    seed.Articles.Add(new Article { Title = "Novidades", Comments = [new Comment { Text = "Ótimo" }] });
    await seed.SaveChangesAsync(Token);

    // Act - se a linha ficasse só marcada como excluída, a chave estrangeira barraria a remoção do principal
    var context = NewStandaloneContext<ClientCascadeDbContext>(options => new ClientCascadeDbContext(options), connection, provider);
    context.Articles.Remove(await context.Articles.Include(a => a.Comments).SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewStandaloneContext<ClientCascadeDbContext>(options => new ClientCascadeDbContext(options), connection, provider);

    Assert.Equal(0, await verification.Articles.CountAsync(Token));
    Assert.Equal(0, await verification.Comments.IgnoreQueryFilters().CountAsync(Token));
  }

  // Testa se remover de novo um principal já excluído apaga também os dependentes carregados
  [Fact]
  public async Task Remove_ShouldReallyDeleteLoadedDependents_WhenPrincipalIsAlreadySoftDeleted()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order { Number = "P-1", Items = [new OrderItem { Name = "Caneta" }] });
    await seed.SaveChangesAsync(Token);

    var first = NewContext(provider);
    first.Orders.Remove(await first.Orders.Include(o => o.Items).SingleAsync(Token));
    await first.SaveChangesAsync(Token);

    // Act
    var second = NewContext(provider);
    second.Orders.Remove(await second.Orders.IgnoreQueryFilters().Include(o => o.Items).SingleAsync(Token));
    await second.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);

    Assert.Equal(0, await verification.Orders.IgnoreQueryFilters().CountAsync(Token));
    Assert.Equal(0, await verification.OrderItems.IgnoreQueryFilters().CountAsync(Token));
  }

  // Testa se a cascata adiada para o SaveChanges também vira exclusão lógica nos dependentes
  [Theory]
  [InlineData(CascadeTiming.OnSaveChanges)]
  [InlineData(CascadeTiming.Never)]
  public async Task Remove_ShouldSoftDeleteDependents_WhenCascadeTimingIsDeferred(CascadeTiming timing)
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order
    {
      Number = "P-1",
      Items = [new OrderItem { Name = "Caneta" }],
      Logs = [new OrderLog { Text = "Criado" }]
    });
    await seed.SaveChangesAsync(Token);

    // Act - o Remove não marca os dependentes: a cascata só acontece ao salvar
    var context = NewContext(provider);
    context.ChangeTracker.CascadeDeleteTiming = timing;
    context.Orders.Remove(await context.Orders.Include(o => o.Items).Include(o => o.Logs).SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);

    Assert.True((await verification.Orders.IgnoreQueryFilters().SingleAsync(Token)).Deleted);
    Assert.True((await verification.OrderItems.IgnoreQueryFilters().SingleAsync(Token)).Deleted);
    Assert.Equal(0, await verification.OrderLogs.CountAsync(Token));
  }

  // Testa se o dependente obrigatório tirado da coleção do principal é excluído logicamente, sem perder o vínculo
  [Fact]
  public async Task Remove_ShouldSoftDeleteOrphan_WhenDependentIsRemovedFromCollection()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order { Number = "P-1", Items = [new OrderItem { Name = "Caneta" }, new OrderItem { Name = "Caderno" }] });
    await seed.SaveChangesAsync(Token);

    // Act - o Entity Framework marca o órfão de uma relação obrigatória para remoção
    var context = NewContext(provider);
    var order = await context.Orders.Include(o => o.Items).SingleAsync(Token);
    order.Items.Remove(order.Items.Single(item => item.Name == "Caneta"));
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);
    var items = await verification.OrderItems.IgnoreQueryFilters().ToListAsync(Token);
    var removed = items.Single(item => item.Name == "Caneta");

    Assert.Equal(2, items.Count);
    Assert.True(removed.Deleted);
    Assert.Equal(order.Id, removed.OrderId);
    Assert.False(items.Single(item => item.Name == "Caderno").Deleted);
    Assert.False((await verification.Orders.SingleAsync(Token)).Deleted);
  }

  // Testa se as partes (owned types) do principal sobrevivem à exclusão lógica
  [Fact]
  public async Task Remove_ShouldKeepOwnedTypes_WhenOwnerIsSoftDeleted()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order
    {
      Number = "P-1",
      Address = new Address { Street = "Rua A", City = "Recife" },
      Phones = [new Phone { Number = "1111" }, new Phone { Number = "2222" }]
    });
    await seed.SaveChangesAsync(Token);

    // Act - o Remove do dono marca as partes para remoção junto com ele
    var context = NewContext(provider);
    context.Orders.Remove(await context.Orders.SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);
    var reloaded = await verification.Orders.IgnoreQueryFilters().SingleAsync(Token);

    Assert.True(reloaded.Deleted);
    Assert.NotNull(reloaded.Address);
    Assert.Equal("Rua A", reloaded.Address.Street);
    Assert.Equal("Recife", reloaded.Address.City);
    Assert.Equal(["1111", "2222"], reloaded.Phones.Select(phone => phone.Number).Order());
  }

  // Testa se a restauração com SetRestored é gravada como uma atualização comum
  [Fact]
  public async Task SetRestored_ShouldBeSavedAsRegularUpdate_WhenEntityWasSoftDeleted()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Categories.Add(new Category { Name = "Papelaria" });
    seed.Orders.Add(new Order { Number = "P-1" });
    await seed.SaveChangesAsync(Token);

    var removal = NewContext(provider);
    removal.Categories.Remove(await removal.Categories.SingleAsync(Token));
    removal.Orders.Remove(await removal.Orders.SingleAsync(Token));
    await removal.SaveChangesAsync(Token);

    // Act - a linha excluída só é lida sem os filtros
    var context = NewContext(provider);
    (await context.Categories.IgnoreQueryFilters().SingleAsync(Token)).SetRestored(OtherUserId);
    (await context.Orders.IgnoreQueryFilters().SingleAsync(Token)).SetRestored(OtherUserId);
    await context.SaveChangesAsync(Token);

    // Assert - o SetRestored já carimbou a entidade, então o interceptor não incrementa a versão de novo
    var verification = NewContext(provider);
    var category = await verification.Categories.SingleAsync(Token);
    var order = await verification.Orders.SingleAsync(Token);

    Assert.False(category.Deleted);
    Assert.Equal(OtherUserId, category.UpdatedById);
    Assert.False(order.Deleted);
    Assert.Equal(OtherUserId, order.RestoredById);
    Assert.NotNull(order.RestoredAt);
    Assert.Equal(OtherUserId, order.UpdatedById);
    Assert.Equal(3, order.Version);
  }
}

// Contexto em que a cascata do principal apagado de verdade é feita só pelo Entity Framework, sem ON DELETE CASCADE
public class ClientCascadeDbContext(DbContextOptions<ClientCascadeDbContext> options) : DbContext(options)
{
  public DbSet<Article> Articles => Set<Article>();

  public DbSet<Comment> Comments => Set<Comment>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Article>()
      .HasMany(article => article.Comments)
      .WithOne()
      .HasForeignKey(comment => comment.ArticleId)
      .OnDelete(DeleteBehavior.ClientCascade);

    modelBuilder.ApplyTooarkConventions();
  }
}
