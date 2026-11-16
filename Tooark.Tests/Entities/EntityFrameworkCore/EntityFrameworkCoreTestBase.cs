using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Entities.EntityFrameworkCore.Extensions;
using Tooark.Entities.EntityFrameworkCore.Injections;
using Tooark.Entities.EntityFrameworkCore.Interfaces;
using Tooark.Entities.EntityFrameworkCore.Options;

namespace Tooark.Tests.Entities.EntityFrameworkCore;

// Base dos testes: SQLite em memória, com SQL e checagem de concorrência reais
public abstract class EntityFrameworkCoreTestBase : IDisposable
{
  protected static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
  protected static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
  protected static readonly Guid SystemUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

  private readonly List<IDisposable> _disposables = [];

  protected EntityFrameworkCoreTestBase()
  {
    // SQLite em memória mantém o banco enquanto a conexão estiver aberta
    Connection = OpenConnection();
  }

  protected SqliteConnection Connection { get; }

  protected static CancellationToken Token => TestContext.Current.CancellationToken;

  public void Dispose()
  {
    // Os escopos e os provedores são descartados antes das conexões que eles usam
    for (var index = _disposables.Count - 1; index >= 0; index--)
    {
      _disposables[index].Dispose();
    }

    GC.SuppressFinalize(this);
  }

  // Abre um banco em memória à parte, para os contextos com outro modelo
  protected SqliteConnection OpenConnection()
  {
    var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();

    _disposables.Add(connection);

    return connection;
  }

  // Registra o que precisa ser descartado ao final do teste
  protected T Track<T>(T disposable) where T : IDisposable
  {
    _disposables.Add(disposable);

    return disposable;
  }

  // Monta o container com o contexto principal, o interceptor e o usuário atual informado
  protected ServiceProvider BuildProvider(ICurrentUser? user, Action<EntityAuditOptions>? configure = null)
  {
    var services = new ServiceCollection();

    if (user is not null)
    {
      services.AddSingleton(user);
    }

    services.AddTooarkEntityFrameworkCore(configure);
    services.AddDbContext<AuditDbContext>((sp, options) => options.UseSqlite(Connection).AddTooarkInterceptors(sp));

    var provider = Track(services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }));

    NewContext(provider).Database.EnsureCreated();

    return provider;
  }

  // Monta o container com o usuário atual padrão dos testes
  protected ServiceProvider BuildProvider(Action<EntityAuditOptions>? configure = null) =>
    BuildProvider(new TestCurrentUser(UserId), configure);

  // Abre um contexto novo, em um escopo próprio, como em uma requisição
  protected AuditDbContext NewContext(IServiceProvider provider) => NewContext<AuditDbContext>(provider);

  // Abre um contexto novo do tipo informado, em um escopo próprio
  protected TContext NewContext<TContext>(IServiceProvider provider) where TContext : DbContext =>
    Track(provider.CreateScope()).ServiceProvider.GetRequiredService<TContext>();

  // Monta um container só com o interceptor, para os contextos criados fora dele
  protected ServiceProvider BuildInterceptorProvider(Action<EntityAuditOptions>? configure = null)
  {
    var services = new ServiceCollection();

    services.AddSingleton<ICurrentUser>(new TestCurrentUser(UserId));
    services.AddTooarkEntityFrameworkCore(configure);

    return Track(services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }));
  }

  // Abre, fora do container, um contexto com outro modelo em um banco à parte, já com as tabelas criadas
  protected TContext NewStandaloneContext<TContext>(
    Func<DbContextOptions<TContext>, TContext> factory,
    SqliteConnection? connection = null,
    IServiceProvider? provider = null
  )
    where TContext : DbContext
  {
    var builder = new DbContextOptionsBuilder<TContext>().UseSqlite(connection ?? OpenConnection());

    if (provider is not null)
    {
      builder.AddTooarkInterceptors(provider);
    }

    var context = Track(factory(builder.Options));

    context.Database.EnsureCreated();

    return context;
  }

  // Salva pelo caminho síncrono ou pelo assíncrono
  protected static async Task<int> SaveAsync(DbContext context, bool async) =>
    async ? await context.SaveChangesAsync(Token) : context.SaveChanges();
}
