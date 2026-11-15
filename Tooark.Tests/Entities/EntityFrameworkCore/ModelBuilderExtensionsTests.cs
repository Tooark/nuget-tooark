using Microsoft.EntityFrameworkCore;
using Tooark.Entities;
using Tooark.Entities.EntityFrameworkCore.Extensions;
using Tooark.Exceptions;

namespace Tooark.Tests.Entities.EntityFrameworkCore;

public class ModelBuilderExtensionsTests : EntityFrameworkCoreTestBase
{
  #region Filtro de excluídos

  // Testa se o filtro esconde as linhas excluídas e se o IgnoreQueryFilters as traz de volta
  [Fact]
  public async Task ApplyTooarkConventions_ShouldHideDeletedRows_WhenEntityIsSoftDeletable()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    var removedCategory = new Category { Name = "Removida" };
    var removedOrder = new Order { Number = "P-2" };
    seed.Categories.AddRange(new Category { Name = "Ativa" }, removedCategory);
    seed.Orders.AddRange(new Order { Number = "P-1" }, removedOrder);
    await seed.SaveChangesAsync(Token);

    seed.Categories.Remove(removedCategory);
    seed.Orders.Remove(removedOrder);
    await seed.SaveChangesAsync(Token);

    // Act
    var context = NewContext(provider);
    var categories = await context.Categories.ToListAsync(Token);
    var orders = await context.Orders.ToListAsync(Token);

    // Assert
    Assert.Equal("Ativa", Assert.Single(categories).Name);
    Assert.Equal("P-1", Assert.Single(orders).Number);
    Assert.Equal(2, await context.Categories.IgnoreQueryFilters().CountAsync(Token));
    Assert.Equal(2, await context.Orders.IgnoreQueryFilters().CountAsync(Token));
  }

  // Testa se o filtro da própria aplicação e o de excluídos valem juntos
  [Fact]
  public async Task ApplyTooarkConventions_ShouldCombineWithApplicationFilter_WhenEntityAlreadyHasOne()
  {
    // Arrange
    var provider = BuildInterceptorProvider();
    var context = NewStandaloneContext<OwnFilterDbContext>(options => new OwnFilterDbContext(options), provider: provider);
    var removed = new Category { Name = "Removida" };

    context.Categories.AddRange(new Category { Name = "Ativa" }, new Category { Name = "Inativa", Active = false }, removed);
    await context.SaveChangesAsync(Token);

    context.Categories.Remove(removed);
    await context.SaveChangesAsync(Token);

    // Act
    var filtered = await context.Categories.AsNoTracking().ToListAsync(Token);
    var all = await context.Categories.AsNoTracking().IgnoreQueryFilters().ToListAsync(Token);

    // Assert - nem a inativa (filtro da aplicação) nem a removida (filtro de excluídos) aparecem
    Assert.Equal("Ativa", Assert.Single(filtered).Name);
    Assert.Equal(3, all.Count);
    Assert.True(all.Single(category => category.Name == "Removida").Deleted);
  }

  // Testa se o filtro definido depois das convenções substitui o de excluídos, motivo de chamá-las por último
  [Fact]
  public async Task ApplyTooarkConventions_ShouldBeReplaced_WhenApplicationSetsFilterAfterwards()
  {
    // Arrange
    var provider = BuildInterceptorProvider();
    var context = NewStandaloneContext<FilterAfterDbContext>(options => new FilterAfterDbContext(options), provider: provider);
    var removed = new Category { Name = "Removida" };

    context.Categories.AddRange(new Category { Name = "Inativa", Active = false }, removed);
    await context.SaveChangesAsync(Token);

    context.Categories.Remove(removed);
    await context.SaveChangesAsync(Token);

    // Act
    var filtered = await context.Categories.AsNoTracking().ToListAsync(Token);

    // Assert - só o filtro da aplicação sobrou: a linha removida volta a aparecer
    var category = Assert.Single(filtered);

    Assert.Equal("Removida", category.Name);
    Assert.True(category.Deleted);
  }

  // Testa se chamar o método duas vezes continua produzindo um modelo válido
  [Fact]
  public async Task ApplyTooarkConventions_ShouldKeepWorking_WhenCalledTwice()
  {
    // Arrange
    var provider = BuildInterceptorProvider();
    var context = NewStandaloneContext<AppliedTwiceDbContext>(options => new AppliedTwiceDbContext(options), provider: provider);
    var removed = new Category { Name = "Removida" };

    context.Categories.AddRange(new Category { Name = "Ativa" }, removed);
    await context.SaveChangesAsync(Token);

    context.Categories.Remove(removed);
    await context.SaveChangesAsync(Token);

    // Act
    var filtered = await context.Categories.AsNoTracking().ToListAsync(Token);

    // Assert
    Assert.Equal("Ativa", Assert.Single(filtered).Name);
  }

  // Testa se o construtor de modelo nulo é rejeitado
  [Fact]
  public void ApplyTooarkConventions_ShouldThrow_WhenModelBuilderIsNull()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => ModelBuilderExtensions.ApplyTooarkConventions(null!));
  }

  #endregion

  #region Herança

  // Testa se a hierarquia com raiz e derivados com exclusão lógica recebe o filtro na raiz
  [Fact]
  public async Task ApplyTooarkConventions_ShouldFilterHierarchy_WhenRootIsSoftDeletable()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    var removedCar = new Car { Plate = "AAA-0002", Doors = 2 };
    seed.Vehicles.AddRange(new Car { Plate = "AAA-0001", Doors = 4 }, removedCar, new Truck { Plate = "BBB-0001", Capacity = 10 });
    await seed.SaveChangesAsync(Token);

    seed.Vehicles.Remove(removedCar);
    await seed.SaveChangesAsync(Token);

    // Act
    var context = NewContext(provider);
    var vehicles = await context.Vehicles.ToListAsync(Token);
    var cars = await context.Cars.ToListAsync(Token);
    var allCars = await context.Cars.IgnoreQueryFilters().ToListAsync(Token);

    // Assert - o filtro da raiz vale para a consulta da raiz e para a do tipo derivado
    Assert.Equal(2, vehicles.Count);
    Assert.Equal("AAA-0001", Assert.Single(cars).Plate);
    Assert.Equal(2, allCars.Count);
    Assert.True(allCars.Single(car => car.Plate == "AAA-0002").Deleted);
  }

  // Testa se a entidade com exclusão lógica abaixo de uma raiz que não a suporta falha ao montar o modelo
  [Fact]
  public void ApplyTooarkConventions_ShouldThrow_WhenHierarchyRootIsNotSoftDeletable()
  {
    // Arrange - só acontece quando uma classe base do Tooark é mapeada como raiz da hierarquia
    var connection = OpenConnection();
    var options = new DbContextOptionsBuilder<InvalidRootDbContext>().UseSqlite(connection).Options;
    using var context = new InvalidRootDbContext(options);

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => context.Model);

    // Assert
    Assert.Contains("Entities.Conventions.SoftDeleteRootNotSupported;Category;DetailedEntity", exception.Message);
  }

  #endregion

  #region Concorrência

  // Testa se a versão vira token de concorrência nas entidades que a têm
  [Fact]
  public void ApplyTooarkConventions_ShouldMarkVersionAsConcurrencyToken_WhenEntityHasVersion()
  {
    // Arrange
    var model = NewContext(BuildProvider()).Model;

    // Act & Assert
    Assert.True(model.FindEntityType(typeof(Document))!.FindProperty(nameof(VersionedEntity.Version))!.IsConcurrencyToken);
    Assert.True(model.FindEntityType(typeof(Order))!.FindProperty(nameof(AuditableEntity.Version))!.IsConcurrencyToken);
    Assert.DoesNotContain(model.FindEntityType(typeof(Category))!.GetProperties(), property => property.IsConcurrencyToken);
    Assert.DoesNotContain(model.FindEntityType(typeof(Article))!.GetProperties(), property => property.IsConcurrencyToken);
  }

  // Testa se dois contextos atualizando a mesma linha fazem a segunda gravação falhar
  [Fact]
  public async Task SaveChanges_ShouldThrowConcurrency_WhenTwoContextsUpdateTheSameRow()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    seed.Orders.Add(new Order { Number = "P-1" });
    await seed.SaveChangesAsync(Token);

    var first = NewContext(provider);
    var second = NewContext(provider);

    (await first.Documents.SingleAsync(Token)).Title = "Primeiro";
    (await first.Orders.SingleAsync(Token)).Number = "P-2";

    var staleDocument = await second.Documents.SingleAsync(Token);
    var staleOrder = await second.Orders.SingleAsync(Token);

    // Act - a primeira gravação leva a versão para 2, e a segunda ainda parte da versão 1
    await first.SaveChangesAsync(Token);

    staleDocument.Title = "Segundo";
    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(Token));

    second.Entry(staleDocument).State = EntityState.Unchanged;
    staleOrder.Number = "P-3";
    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(Token));

    // Assert - valeu a primeira gravação
    var verification = NewContext(provider);

    Assert.Equal("Primeiro", (await verification.Documents.SingleAsync(Token)).Title);
    Assert.Equal("P-2", (await verification.Orders.SingleAsync(Token)).Number);
  }

  // Testa se a exclusão lógica de uma linha que outro contexto alterou também falha na concorrência
  [Fact]
  public async Task SaveChanges_ShouldThrowConcurrency_WhenSoftDeletingStaleRow()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Orders.Add(new Order { Number = "P-1" });
    await seed.SaveChangesAsync(Token);

    var first = NewContext(provider);
    var second = NewContext(provider);

    (await first.Orders.SingleAsync(Token)).Number = "P-2";
    second.Orders.Remove(await second.Orders.SingleAsync(Token));

    // Act
    await first.SaveChangesAsync(Token);

    // Assert
    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(Token));
    Assert.False((await NewContext(provider).Orders.SingleAsync(Token)).Deleted);
  }

  // Testa se, sem o carimbo, a versão não muda e a concorrência deixa de ser detectada
  [Fact]
  public async Task SaveChanges_ShouldNotDetectConcurrency_WhenNothingIncrementsTheVersion()
  {
    // Arrange
    var provider = BuildProvider(options => options.StampAudit = false);

    var seed = NewContext(provider);
    seed.Documents.Add(new Document(UserId) { Title = "Contrato" });
    await seed.SaveChangesAsync(Token);

    var first = NewContext(provider);
    var second = NewContext(provider);

    (await first.Documents.SingleAsync(Token)).Title = "Primeiro";
    (await second.Documents.SingleAsync(Token)).Title = "Segundo";

    // Act - nenhuma das gravações chama SetUpdatedBy, então a versão continua 1 nas duas
    await first.SaveChangesAsync(Token);
    await second.SaveChangesAsync(Token);

    // Assert - a segunda gravação sobrescreveu a primeira
    var reloaded = await NewContext(provider).Documents.SingleAsync(Token);

    Assert.Equal("Segundo", reloaded.Title);
    Assert.Equal(1, reloaded.Version);
  }

  #endregion

  #region Tipos owned e sem chave

  // Testa se os tipos owned do modelo principal não recebem filtro e não quebram as convenções
  [Fact]
  public void ApplyTooarkConventions_ShouldSkipOwnedTypes()
  {
    // Arrange
    var context = NewContext(BuildProvider());

    // Act
    var owned = context.Model.GetEntityTypes().Where(entityType => entityType.IsOwned()).ToList();
    var sql = context.Orders.ToQueryString();

    // Assert - o filtro do dono segue valendo, com as partes na mesma consulta
    Assert.Equal(2, owned.Count);
    Assert.Contains("NOT (\"o\".\"deleted\")", sql);
  }

  // Testa se os tipos sem chave não quebram as convenções: recebem o filtro, mas não o token de concorrência
  [Fact]
  public async Task ApplyTooarkConventions_ShouldFilterKeylessTypesWithoutConcurrencyToken()
  {
    // Arrange
    var context = NewStandaloneContext<KeylessDbContext>(options => new KeylessDbContext(options));

    // Act
    var sql = context.Orders.ToQueryString();
    var orders = await context.Orders.ToListAsync(Token);
    var settings = await context.Settings.ToListAsync(Token);

    // Assert
    Assert.Contains("NOT (\"o\".\"deleted\")", sql);
    Assert.Empty(orders);
    Assert.Empty(settings);
    Assert.False(context.Model.FindEntityType(typeof(Order))!.FindProperty(nameof(AuditableEntity.Version))!.IsConcurrencyToken);
  }

  #endregion

#if NET10_0_OR_GREATER
  #region Filtros nomeados (Entity Framework Core 10)

  // Testa o motivo do filtro nomeado: o Entity Framework Core 10 não aceita filtro nomeado e sem nome na mesma entidade
  [Fact]
  public void HasQueryFilter_ShouldThrow_WhenNamedAndUnnamedFiltersAreMixed()
  {
    // Arrange
    var connection = OpenConnection();
    var options = new DbContextOptionsBuilder<MixedFiltersDbContext>().UseSqlite(connection).Options;
    using var context = new MixedFiltersDbContext(options);

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() => context.Model);
  }

  // Testa se o filtro de excluídos entra como filtro nomeado quando a entidade já usa filtros nomeados
  [Fact]
  public async Task ApplyTooarkConventions_ShouldAddNamedFilter_WhenEntityUsesNamedFilters()
  {
    // Arrange
    var provider = BuildInterceptorProvider();
    var context = NewStandaloneContext<NamedFilterDbContext>(options => new NamedFilterDbContext(options), provider: provider);
    var removed = new Category { Name = "Removida" };

    context.Categories.AddRange(new Category { Name = "Ativa" }, new Category { Name = "Inativa", Active = false }, removed);
    await context.SaveChangesAsync(Token);

    context.Categories.Remove(removed);
    await context.SaveChangesAsync(Token);

    // Act
    var filtered = await context.Categories.AsNoTracking().ToListAsync(Token);
    var withDeleted = await context.Categories.AsNoTracking()
      .IgnoreQueryFilters([ModelBuilderExtensions.SoftDeleteFilterName])
      .ToListAsync(Token);
    var all = await context.Categories.AsNoTracking().IgnoreQueryFilters().ToListAsync(Token);

    // Assert - os dois filtros valem juntos, e o de excluídos pode ser desligado sozinho pelo nome
    Assert.Equal("Ativa", Assert.Single(filtered).Name);
    Assert.Equal(["Ativa", "Removida"], withDeleted.Select(category => category.Name).Order());
    Assert.Equal(3, all.Count);
  }

  #endregion
#endif
}

// Contexto em que a aplicação define o próprio filtro antes das convenções
public class OwnFilterDbContext(DbContextOptions<OwnFilterDbContext> options) : DbContext(options)
{
  public DbSet<Category> Categories => Set<Category>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Category>().HasQueryFilter(category => category.Active);

    modelBuilder.ApplyTooarkConventions();
  }
}

// Contexto em que a aplicação define o próprio filtro depois das convenções
public class FilterAfterDbContext(DbContextOptions<FilterAfterDbContext> options) : DbContext(options)
{
  public DbSet<Category> Categories => Set<Category>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyTooarkConventions();

    modelBuilder.Entity<Category>().HasQueryFilter(category => category.Active);
  }
}

// Contexto que aplica as convenções duas vezes
public class AppliedTwiceDbContext(DbContextOptions<AppliedTwiceDbContext> options) : DbContext(options)
{
  public DbSet<Category> Categories => Set<Category>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyTooarkConventions();
    modelBuilder.ApplyTooarkConventions();
  }
}

// Contexto que mapeia uma classe base do Tooark como raiz de uma hierarquia com um derivado com exclusão lógica
public class InvalidRootDbContext(DbContextOptions<InvalidRootDbContext> options) : DbContext(options)
{
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<DetailedEntity>();
    modelBuilder.Entity<Category>();

    modelBuilder.ApplyTooarkConventions();
  }
}

// Contexto com tipos sem chave, um deles com exclusão lógica e versão
public class KeylessDbContext(DbContextOptions<KeylessDbContext> options) : DbContext(options)
{
  public DbSet<Order> Orders => Set<Order>();

  public DbSet<Setting> Settings => Set<Setting>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Order>(order =>
    {
      order.HasNoKey();
      order.Ignore(o => o.Address);
      order.Ignore(o => o.Phones);
      order.Ignore(o => o.Items);
      order.Ignore(o => o.Logs);
      order.Ignore(o => o.Shipments);
    });

    modelBuilder.Entity<Setting>().HasNoKey();

    modelBuilder.ApplyTooarkConventions();
  }
}

#if NET10_0_OR_GREATER
// Contexto em que a aplicação usa filtros nomeados
public class NamedFilterDbContext(DbContextOptions<NamedFilterDbContext> options) : DbContext(options)
{
  public DbSet<Category> Categories => Set<Category>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Category>().HasQueryFilter("Active", category => category.Active);

    modelBuilder.ApplyTooarkConventions();
  }
}

// Contexto que mistura filtro nomeado e filtro sem nome, sem passar pelas convenções
public class MixedFiltersDbContext(DbContextOptions<MixedFiltersDbContext> options) : DbContext(options)
{
  public DbSet<Category> Categories => Set<Category>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Category>().HasQueryFilter("Active", category => category.Active);
    modelBuilder.Entity<Category>().HasQueryFilter(category => !category.Deleted);
  }
}
#endif
