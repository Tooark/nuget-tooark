using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.EntityFrameworkCore.Enums;
using Tooark.Mediator.EntityFrameworkCore.Injections;
using Tooark.Mediator.EntityFrameworkCore.Interfaces;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Injections;

namespace Tooark.Tests.Mediator.EntityFrameworkCore;

public class UnitOfWorkBehaviorTests : IDisposable
{
  private readonly SqliteConnection _connection;

  public UnitOfWorkBehaviorTests()
  {
    // SQLite em memória mantém o banco enquanto a conexão estiver aberta
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();
  }

  public void Dispose()
  {
    _connection.Dispose();

    GC.SuppressFinalize(this);
  }

  // Monta o container com o contexto, o mediador e a unidade de trabalho na estratégia informada
  private ServiceProvider BuildProvider(EUnitOfWorkStrategy strategy = EUnitOfWorkStrategy.SaveChanges)
  {
    var services = new ServiceCollection();

    services.AddDbContext<TestDbContext>(options => options.UseSqlite(_connection));
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<CreateProduct, Guid>, CreateProductHandler>();
    services.AddTransient<IRequestHandler<FailingCommand, Guid>, FailingCommandHandler>();
    services.AddTransient<IRequestHandler<CountProducts, int>, CountProductsHandler>();
    services.AddTransient<IRequestHandler<CreateTwoProducts, Guid>, CreateTwoProductsHandler>();
    services.AddTooarkMediatorUnitOfWork<TestDbContext>(strategy);

    var provider = services.BuildServiceProvider();

    using var scope = provider.CreateScope();
    scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();

    return provider;
  }

  // Teste para garantir que o comando persiste sem o handler chamar SaveChanges.
  [Fact]
  public async Task SendAsync_ShouldPersist_WhenHandlerDoesNotSaveChanges()
  {
    // Arrange
    using var provider = BuildProvider();
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    // Act
    var id = await mediator.SendAsync(new CreateProduct("Caneta"), TestContext.Current.CancellationToken);

    // Assert - a persistência aconteceu no behavior, não no handler
    using var verification = provider.CreateScope();
    var context = verification.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.Equal(1, await context.Products.CountAsync(TestContext.Current.CancellationToken));
    Assert.Equal("Caneta", (await context.Products.SingleAsync(TestContext.Current.CancellationToken)).Name);
    Assert.NotEqual(Guid.Empty, id);
  }

  // Teste para garantir que a exceção do handler impede a persistência.
  [Fact]
  public async Task SendAsync_ShouldNotPersist_WhenHandlerThrows()
  {
    // Arrange
    using var provider = BuildProvider();
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    // Act
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => mediator.SendAsync(new FailingCommand(), TestContext.Current.CancellationToken));

    // Assert - o produto adicionado ao contexto antes da falha não foi gravado
    using var verification = provider.CreateScope();
    var context = verification.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.Equal(0, await context.Products.CountAsync(TestContext.Current.CancellationToken));
  }

  // Teste para garantir que consultas não passam pela unidade de trabalho.
  [Fact]
  public async Task SendAsync_ShouldNotApplyUnitOfWork_ToQueries()
  {
    // Arrange - a restrição a ICommand mantém a consulta fora da esteira
    using var provider = BuildProvider();
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

    // Deixa uma alteração pendente no contexto que a consulta não pode persistir
    context.Products.Add(new Product { Id = Guid.NewGuid(), Name = "Pendente" });

    // Act
    var total = await mediator.SendAsync(new CountProducts(), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(0, total);

    using var verification = provider.CreateScope();
    var verificationContext = verification.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.Equal(0, await verificationContext.Products.CountAsync(TestContext.Current.CancellationToken));
  }

  // Teste para garantir que o comando aninhado não persiste antes do comando mais externo concluir.
  [Fact]
  public async Task SendAsync_ShouldPersistOnce_WhenCommandIsNested()
  {
    // Arrange - o comando externo despacha outro comando e falha depois
    using var provider = BuildProvider();
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    // Act
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => mediator.SendAsync(new CreateTwoProducts(Fail: true), TestContext.Current.CancellationToken));

    // Assert - nada foi gravado: o comando interno não persistiu no meio da operação do externo
    using var verification = provider.CreateScope();
    var context = verification.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.Equal(0, await context.Products.CountAsync(TestContext.Current.CancellationToken));
  }

  // Teste para garantir que o comando aninhado bem-sucedido persiste junto com o comando mais externo.
  [Fact]
  public async Task SendAsync_ShouldPersistAllTogether_WhenNestedCommandSucceeds()
  {
    // Arrange
    using var provider = BuildProvider();
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    // Act
    await mediator.SendAsync(new CreateTwoProducts(Fail: false), TestContext.Current.CancellationToken);

    // Assert
    using var verification = provider.CreateScope();
    var context = verification.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.Equal(2, await context.Products.CountAsync(TestContext.Current.CancellationToken));
  }

  // Teste para garantir que a estratégia de transação persiste o comando.
  [Fact]
  public async Task SendAsync_ShouldPersist_WhenTransactionStrategy()
  {
    // Arrange
    using var provider = BuildProvider(EUnitOfWorkStrategy.Transaction);
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    // Act
    await mediator.SendAsync(new CreateProduct("Caderno"), TestContext.Current.CancellationToken);

    // Assert
    using var verification = provider.CreateScope();
    var context = verification.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.Equal(1, await context.Products.CountAsync(TestContext.Current.CancellationToken));
  }

  // Teste para garantir que a transação é revertida quando o handler falha.
  [Fact]
  public async Task SendAsync_ShouldRollback_WhenHandlerThrows_InTransactionStrategy()
  {
    // Arrange
    using var provider = BuildProvider(EUnitOfWorkStrategy.Transaction);
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    // Act
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => mediator.SendAsync(new FailingCommand(), TestContext.Current.CancellationToken));

    // Assert
    using var verification = provider.CreateScope();
    var context = verification.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.Equal(0, await context.Products.CountAsync(TestContext.Current.CancellationToken));
  }

  // Teste para garantir que o comando aninhado participa da transação do comando mais externo.
  [Fact]
  public async Task SendAsync_ShouldJoinOuterTransaction_WhenCommandIsNested_InTransactionStrategy()
  {
    // Arrange - o comando aninhado não pode abrir uma segunda transação nem confirmar sozinho
    using var provider = BuildProvider(EUnitOfWorkStrategy.Transaction);
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    // Act
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => mediator.SendAsync(new CreateTwoProducts(Fail: true), TestContext.Current.CancellationToken));

    // Assert - a transação do comando externo foi revertida por completo
    using var verification = provider.CreateScope();
    var context = verification.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.Equal(0, await context.Products.CountAsync(TestContext.Current.CancellationToken));
  }

  // Teste para garantir que comandos aninhados bem-sucedidos confirmam juntos na estratégia de transação.
  [Fact]
  public async Task SendAsync_ShouldCommitAllTogether_WhenNestedCommandSucceeds_InTransactionStrategy()
  {
    // Arrange
    using var provider = BuildProvider(EUnitOfWorkStrategy.Transaction);
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    // Act
    await mediator.SendAsync(new CreateTwoProducts(Fail: false), TestContext.Current.CancellationToken);

    // Assert
    using var verification = provider.CreateScope();
    var context = verification.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.Equal(2, await context.Products.CountAsync(TestContext.Current.CancellationToken));
  }

  // Teste para garantir que a estratégia desconhecida falha no registro.
  [Fact]
  public void AddTooarkMediatorUnitOfWork_ShouldThrow_WhenStrategyIsNotSupported()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert - valor inválido nunca troca de estratégia silenciosamente
    var exception = Assert.Throws<InternalServerErrorException>(
      () => services.AddTooarkMediatorUnitOfWork<TestDbContext>((EUnitOfWorkStrategy)99));

    Assert.Contains("UnitOfWork.StrategyNotSupported", exception.Message);
  }

  // Teste para garantir que a coleção de serviços nula falha no registro.
  [Fact]
  public void AddTooarkMediatorUnitOfWork_ShouldThrow_WhenServiceCollectionIsNull()
  {
    // Act & Assert
    var exception = Assert.Throws<InternalServerErrorException>(
      () => global::Tooark.Mediator.EntityFrameworkCore.Injections.TooarkDependencyInjection
        .AddTooarkMediatorUnitOfWork<TestDbContext>(null!));

    Assert.Contains("Mediator.Null.Service", exception.Message);
  }

  // Teste para garantir que a unidade de trabalho é registrada no escopo do contexto.
  [Fact]
  public void AddTooarkMediatorUnitOfWork_ShouldRegisterUnitOfWorkAsScoped()
  {
    // Arrange
    using var provider = BuildProvider();

    // Act
    using var scope = provider.CreateScope();
    var first = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    var second = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    // Assert
    Assert.NotNull(first);
    Assert.Same(first, second);
  }

  #region Fixtures

  public sealed class Product
  {
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
  }

  public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
  {
    public DbSet<Product> Products => Set<Product>();
  }

  public sealed record CreateProduct(string Name) : ICommand<Guid>;

  public sealed class CreateProductHandler(TestDbContext context) : IRequestHandler<CreateProduct, Guid>
  {
    public Task<Guid> HandleAsync(CreateProduct request, CancellationToken cancellationToken = default)
    {
      var product = new Product { Id = Guid.NewGuid(), Name = request.Name };

      context.Products.Add(product);

      // Sem SaveChanges: a persistência é responsabilidade do behavior
      return Task.FromResult(product.Id);
    }
  }

  public sealed record FailingCommand : ICommand<Guid>;

  public sealed class FailingCommandHandler(TestDbContext context) : IRequestHandler<FailingCommand, Guid>
  {
    public Task<Guid> HandleAsync(FailingCommand request, CancellationToken cancellationToken = default)
    {
      context.Products.Add(new Product { Id = Guid.NewGuid(), Name = "Não deve persistir" });

      throw new InvalidOperationException("falha no manipulador");
    }
  }

  public sealed record CreateTwoProducts(bool Fail) : ICommand<Guid>;

  public sealed class CreateTwoProductsHandler(TestDbContext context, IMediator mediator)
    : IRequestHandler<CreateTwoProducts, Guid>
  {
    public async Task<Guid> HandleAsync(CreateTwoProducts request, CancellationToken cancellationToken = default)
    {
      // Comando aninhado: não pode persistir no meio da operação do comando externo
      await mediator.SendAsync(new CreateProduct("Interno"), cancellationToken);

      context.Products.Add(new Product { Id = Guid.NewGuid(), Name = "Externo" });

      if (request.Fail)
      {
        throw new InvalidOperationException("falha após o comando aninhado");
      }

      return Guid.NewGuid();
    }
  }

  public sealed record CountProducts : IQuery<int>;

  public sealed class CountProductsHandler(TestDbContext context) : IRequestHandler<CountProducts, int>
  {
    public Task<int> HandleAsync(CountProducts request, CancellationToken cancellationToken = default)
    {
      return context.Products.CountAsync(cancellationToken);
    }
  }

  #endregion
}
