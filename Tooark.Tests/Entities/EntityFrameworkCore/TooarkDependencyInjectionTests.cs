using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Entities.EntityFrameworkCore.Extensions;
using Tooark.Entities.EntityFrameworkCore.Injections;
using Tooark.Entities.EntityFrameworkCore.Interceptors;
using Tooark.Entities.EntityFrameworkCore.Interfaces;
using Tooark.Entities.EntityFrameworkCore.Options;
using Tooark.Exceptions;
using Tooark.Extensions;

namespace Tooark.Tests.Entities.EntityFrameworkCore;

public class TooarkDependencyInjectionTests : EntityFrameworkCoreTestBase
{
  // Estado da requisição, de onde o usuário com escopo lê o identificador
  public sealed class RequestState
  {
    public Guid? UserId { get; set; }
  }

  // Usuário atual com escopo, como o que lê as claims de uma requisição
  public sealed class ScopedCurrentUser(RequestState state) : ICurrentUser
  {
    public Guid? Id => state.UserId;
  }

  // Usuário atual singleton sobre um valor de ambiente, como o que lê o IHttpContextAccessor
  public sealed class AmbientCurrentUser : ICurrentUser
  {
    private static readonly AsyncLocal<Guid?> Current = new();

    public Guid? Id => Current.Value;

    public static void Set(Guid? id) => Current.Value = id;
  }

  // Monta o container com a validação de escopos informada
  private ServiceProvider Build(Action<IServiceCollection> configure, bool validateScopes = true)
  {
    var services = new ServiceCollection();

    configure(services);

    return Track(services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = validateScopes, ValidateOnBuild = true }));
  }

  #region Registro

  // Testa se o registro adiciona as opções com os padrões e o interceptor, sem registrar o usuário atual
  [Fact]
  public void AddTooarkEntityFrameworkCore_ShouldRegisterOptionsAndInterceptor()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    var returned = services.AddTooarkEntityFrameworkCore();
    using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    var options = provider.GetRequiredService<IOptions<EntityAuditOptions>>().Value;

    // Assert
    Assert.Same(services, returned);
    Assert.Null(options.SystemUserId);
    Assert.True(options.StampAudit);
    Assert.True(options.SoftDelete);
    Assert.Equal(ServiceLifetime.Transient, Assert.Single(services, service => service.ServiceType == typeof(TooarkEntityInterceptor)).Lifetime);
    Assert.NotNull(provider.GetRequiredService<TooarkEntityInterceptor>());
    Assert.DoesNotContain(services, service => service.ServiceType == typeof(ICurrentUser));
  }

  // Testa se a ação de configuração altera as opções
  [Fact]
  public void AddTooarkEntityFrameworkCore_ShouldApplyConfigureAction()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkEntityFrameworkCore(options =>
    {
      options.SystemUserId = SystemUserId;
      options.StampAudit = false;
      options.SoftDelete = false;
    });

    using var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<IOptions<EntityAuditOptions>>().Value;

    // Assert
    Assert.Equal(SystemUserId, options.SystemUserId);
    Assert.False(options.StampAudit);
    Assert.False(options.SoftDelete);
  }

  // Testa se a versão genérica registra o usuário atual com escopo e repassa a configuração
  [Fact]
  public void AddTooarkEntityFrameworkCore_ShouldRegisterCurrentUserAsScoped_WhenTypeIsInformed()
  {
    // Arrange
    var services = new ServiceCollection();

    services.AddScoped<RequestState>();

    // Act
    services.AddTooarkEntityFrameworkCore<ScopedCurrentUser>(options => options.SystemUserId = SystemUserId);

    using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    using var scope = provider.CreateScope();
    var descriptor = Assert.Single(services, service => service.ServiceType == typeof(ICurrentUser));

    // Assert
    Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    Assert.IsType<ScopedCurrentUser>(scope.ServiceProvider.GetRequiredService<ICurrentUser>());
    Assert.Equal(SystemUserId, provider.GetRequiredService<IOptions<EntityAuditOptions>>().Value.SystemUserId);
    Assert.Contains(services, service => service.ServiceType == typeof(TooarkEntityInterceptor));
  }

  // Testa se o registro do usuário atual feito pela aplicação prevalece sobre o do pacote
  [Fact]
  public void AddTooarkEntityFrameworkCore_ShouldKeepApplicationCurrentUser_WhenAlreadyRegistered()
  {
    // Arrange
    var services = new ServiceCollection();

    services.AddScoped<RequestState>();
    services.AddSingleton<ICurrentUser, AmbientCurrentUser>();

    // Act
    services.AddTooarkEntityFrameworkCore<ScopedCurrentUser>();

    // Assert
    var descriptor = Assert.Single(services, service => service.ServiceType == typeof(ICurrentUser));

    Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    Assert.Equal(typeof(AmbientCurrentUser), descriptor.ImplementationType);
  }

  // Testa se a coleção de serviços nula é rejeitada
  [Fact]
  public void AddTooarkEntityFrameworkCore_ShouldThrow_WhenServiceCollectionIsNull()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => TooarkDependencyInjection.AddTooarkEntityFrameworkCore(null!));
    Assert.Throws<ArgumentNullException>(() => TooarkDependencyInjection.AddTooarkEntityFrameworkCore<ScopedCurrentUser>(null!));
  }

  #endregion

  #region AddTooarkInterceptors

  // Testa se a falta do registro é apontada com uma chave própria, e não com o erro do container
  [Fact]
  public void AddTooarkInterceptors_ShouldThrow_WhenInterceptorIsNotRegistered()
  {
    // Arrange
    using var provider = new ServiceCollection().BuildServiceProvider();
    var builder = new DbContextOptionsBuilder<AuditDbContext>();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => builder.AddTooarkInterceptors(provider));

    // Assert
    Assert.Contains("Entities.Audit.InterceptorNotRegistered", exception.Message);
  }

  // Testa se os argumentos nulos são rejeitados
  [Fact]
  public void AddTooarkInterceptors_ShouldThrow_WhenArgumentIsNull()
  {
    // Arrange
    using var provider = new ServiceCollection().BuildServiceProvider();

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => DbContextOptionsBuilderExtensions.AddTooarkInterceptors(null!, provider));
    Assert.Throws<ArgumentNullException>(() => new DbContextOptionsBuilder().AddTooarkInterceptors(null!));
  }

  // Testa se as duas sobrecargas adicionam o interceptor e se a genérica mantém o tipo do construtor
  [Fact]
  public void AddTooarkInterceptors_ShouldAddInterceptorAndKeepBuilderType()
  {
    // Arrange
    var provider = BuildInterceptorProvider();
    var typed = new DbContextOptionsBuilder<AuditDbContext>();
    var untyped = new DbContextOptionsBuilder();

    // Act
    DbContextOptionsBuilder<AuditDbContext> returnedTyped = typed.AddTooarkInterceptors(provider);
    var returnedUntyped = untyped.AddTooarkInterceptors(provider);

    // Assert
    Assert.Same(typed, returnedTyped);
    Assert.Same(untyped, returnedUntyped);
    Assert.IsType<TooarkEntityInterceptor>(Assert.Single(Interceptors(typed.Options)));
    Assert.IsType<TooarkEntityInterceptor>(Assert.Single(Interceptors(untyped.Options)));
  }

  // Lê os interceptores registrados nas opções do contexto
  private static IEnumerable<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor> Interceptors(DbContextOptions options) =>
    options.FindExtension<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>()?.Interceptors ?? [];

  #endregion

  #region Tempos de vida

  // Testa o AddDbContext com o usuário atual com escopo: cada requisição grava com o próprio usuário
  [Fact]
  public async Task AddDbContext_ShouldResolveScopedCurrentUser_WhenScopesAreValidated()
  {
    // Arrange
    var provider = Build(services =>
    {
      services.AddScoped<RequestState>();
      services.AddTooarkEntityFrameworkCore<ScopedCurrentUser>();
      services.AddDbContext<AuditDbContext>((sp, options) => options.UseSqlite(Connection).AddTooarkInterceptors(sp));
    });

    NewContext(provider).Database.EnsureCreated();

    // Act - duas requisições, cada uma com o seu usuário
    foreach (var userId in new[] { UserId, OtherUserId })
    {
      using var scope = provider.CreateScope();

      scope.ServiceProvider.GetRequiredService<RequestState>().UserId = userId;

      var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
      context.Articles.Add(new Article { Title = userId.ToString() });
      await context.SaveChangesAsync(Token);
    }

    // Assert
    var articles = await NewContext(provider).Articles.ToListAsync(Token);

    Assert.Equal(2, articles.Count);
    Assert.All(articles, article => Assert.Equal(article.Title, article.CreatedById.ToString()));
  }

  // Testa o AddDbContextPool com o usuário atual singleton, que é a configuração suportada para o pool
  [Fact]
  public async Task AddDbContextPool_ShouldResolveSingletonCurrentUser_WhenScopesAreValidated()
  {
    // Arrange
    var provider = Build(services =>
    {
      services.AddSingleton<ICurrentUser, AmbientCurrentUser>();
      services.AddTooarkEntityFrameworkCore();
      services.AddDbContextPool<AuditDbContext>((sp, options) => options.UseSqlite(Connection).AddTooarkInterceptors(sp));
    });

    NewContext(provider).Database.EnsureCreated();

    // Act - o mesmo interceptor atende todos os contextos do pool e lê o usuário a cada gravação
    foreach (var userId in new[] { UserId, OtherUserId })
    {
      AmbientCurrentUser.Set(userId);

      using var scope = provider.CreateScope();

      var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
      context.Articles.Add(new Article { Title = userId.ToString() });
      await context.SaveChangesAsync(Token);
    }

    AmbientCurrentUser.Set(null);

    // Assert
    var articles = await NewContext(provider).Articles.ToListAsync(Token);

    Assert.Equal(2, articles.Count);
    Assert.All(articles, article => Assert.Equal(article.Title, article.CreatedById.ToString()));
  }

  // Testa o AddDbContextPool só com o usuário de sistema, sem usuário atual registrado
  [Fact]
  public async Task AddDbContextPool_ShouldUseSystemUser_WhenCurrentUserIsNotRegistered()
  {
    // Arrange
    var provider = Build(services =>
    {
      services.AddTooarkEntityFrameworkCore(options => options.SystemUserId = SystemUserId);
      services.AddDbContextPool<AuditDbContext>((sp, options) => options.UseSqlite(Connection).AddTooarkInterceptors(sp));
    });

    var context = NewContext(provider);
    context.Database.EnsureCreated();

    // Act
    context.Articles.Add(new Article { Title = "Novidades" });
    await context.SaveChangesAsync(Token);

    // Assert
    Assert.Equal(SystemUserId, (await NewContext(provider).Articles.SingleAsync(Token)).CreatedById);
  }

  // Testa o AddDbContextPool com o usuário atual com escopo: o provedor raiz não o resolve, e a falha tem chave própria
  [Fact]
  public async Task AddDbContextPool_ShouldThrow_WhenCurrentUserIsScopedAndScopesAreValidated()
  {
    // Arrange
    var provider = Build(services =>
    {
      services.AddScoped<RequestState>();
      services.AddTooarkEntityFrameworkCore<ScopedCurrentUser>(options => options.SystemUserId = SystemUserId);
      services.AddDbContextPool<AuditDbContext>((sp, options) => options.UseSqlite(Connection).AddTooarkInterceptors(sp));
    });

    using var scope = provider.CreateScope();

    scope.ServiceProvider.GetRequiredService<RequestState>().UserId = UserId;

    var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    context.Database.EnsureCreated();
    context.Articles.Add(new Article { Title = "Novidades" });

    // Act
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(() => context.SaveChangesAsync(Token));

    // Assert - nem o usuário de sistema encobre a configuração errada
    Assert.Contains("Entities.Audit.CurrentUserNotResolved", exception.Message);
    Assert.IsType<InvalidOperationException>(exception.InnerException);
    Assert.Equal(0, await NewContext(provider).Articles.CountAsync(Token));
  }

  // Testa a lacuna conhecida do pool sem validação de escopos: o usuário com escopo não enxerga a requisição
  [Fact]
  public async Task AddDbContextPool_ShouldNotSeeRequestState_WhenCurrentUserIsScopedAndScopesAreNotValidated()
  {
    // Arrange
    var provider = Build(
      services =>
      {
        services.AddScoped<RequestState>();
        services.AddTooarkEntityFrameworkCore<ScopedCurrentUser>(options => options.SystemUserId = SystemUserId);
        services.AddDbContextPool<AuditDbContext>((sp, options) => options.UseSqlite(Connection).AddTooarkInterceptors(sp));
      },
      validateScopes: false);

    using var scope = provider.CreateScope();

    scope.ServiceProvider.GetRequiredService<RequestState>().UserId = UserId;

    var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    context.Database.EnsureCreated();

    // Act - o usuário com escopo é resolvido no provedor raiz e vira uma instância única da aplicação
    context.Articles.Add(new Article { Title = "Novidades" });
    await context.SaveChangesAsync(Token);

    // Assert - a gravação saiu com o usuário de sistema, e não com o usuário da requisição
    Assert.Equal(SystemUserId, (await NewContext(provider).Articles.SingleAsync(Token)).CreatedById);
  }

  // Testa o AddDbContextFactory, que também monta as opções no provedor raiz
  [Fact]
  public async Task AddDbContextFactory_ShouldResolveSingletonCurrentUser_WhenScopesAreValidated()
  {
    // Arrange
    var provider = Build(services =>
    {
      services.AddSingleton<ICurrentUser>(new TestCurrentUser(UserId));
      services.AddTooarkEntityFrameworkCore();
      services.AddDbContextFactory<AuditDbContext>((sp, options) => options.UseSqlite(Connection).AddTooarkInterceptors(sp));
    });

    var factory = provider.GetRequiredService<IDbContextFactory<AuditDbContext>>();

    await using var context = await factory.CreateDbContextAsync(Token);
    await context.Database.EnsureCreatedAsync(Token);

    // Act
    context.Articles.Add(new Article { Title = "Novidades" });
    await context.SaveChangesAsync(Token);

    // Assert
    await using var verification = await factory.CreateDbContextAsync(Token);

    Assert.Equal(UserId, (await verification.Articles.SingleAsync(Token)).CreatedById);
  }

  #endregion

  #region Mensagens

  // Testa se toda chave de mensagem emitida pelo pacote tem tradução nos três idiomas
  [Theory]
  [InlineData("en-US")]
  [InlineData("es-ES")]
  [InlineData("pt-BR")]
  public void EveryEmittedKey_ShouldHaveTranslation(string language)
  {
    // Arrange - lê o recurso embutido no assembly, que é o que chega ao consumidor
    var assembly = typeof(JsonStringLocalizerExtension).Assembly;

    using var resource = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.{language}.default.json")!;

    var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(resource)!;

    string[] keys =
    [
      "Entities.Audit.CurrentUserNotResolved",
      "Entities.Audit.CurrentUserRequired",
      "Entities.Audit.InterceptorNotRegistered",
      "Entities.Conventions.SoftDeleteRootNotSupported"
    ];

    // Act & Assert
    foreach (var key in keys)
    {
      Assert.True(translations.ContainsKey(key), $"{language} não tem a chave {key}");
      Assert.False(string.IsNullOrWhiteSpace(translations[key]));
    }
  }

  #endregion
}
