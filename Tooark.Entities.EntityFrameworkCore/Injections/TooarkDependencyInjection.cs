using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tooark.Entities.EntityFrameworkCore.Interceptors;
using Tooark.Entities.EntityFrameworkCore.Interfaces;
using Tooark.Entities.EntityFrameworkCore.Options;

namespace Tooark.Entities.EntityFrameworkCore.Injections;

/// <summary>
/// Classe para adicionar a integração das entidades com o Entity Framework Core ao container de injeção de
/// dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona as opções da auditoria e o interceptor das entidades.
  /// </summary>
  /// <remarks>
  /// Este método não registra o <see cref="ICurrentUser"/>: sem ele, o autor das gravações é o
  /// <see cref="EntityAuditOptions.SystemUserId"/>, que é a configuração de um worker. O interceptor só passa
  /// a atuar no contexto que o recebe com <c>AddTooarkInterceptors</c>.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configure">Ação opcional para configurar as opções.</param>
  /// <returns>A coleção de serviços com a integração adicionada.</returns>
  /// <exception cref="ArgumentNullException">Quando a coleção de serviços é nula.</exception>
  public static IServiceCollection AddTooarkEntityFrameworkCore(this IServiceCollection services, Action<EntityAuditOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(services);

    var builder = services.AddOptions<EntityAuditOptions>();

    if (configure is not null)
    {
      builder.Configure(configure);
    }

    // Transitório para nascer no provedor que o pede: o escopo da requisição no AddDbContext e o provedor
    // raiz no AddDbContextPool. É desse provedor que o interceptor resolve o usuário a cada gravação.
    services.TryAddTransient<TooarkEntityInterceptor>();

    return services;
  }

  /// <summary>
  /// Adiciona as opções da auditoria, o interceptor das entidades e o usuário atual.
  /// </summary>
  /// <remarks>
  /// O usuário atual é registrado com escopo, e só quando a aplicação ainda não registrou o dela. Com
  /// <c>AddDbContextPool</c> as opções do contexto nascem no provedor raiz, onde um serviço com escopo não
  /// pode ser resolvido: registre o usuário atual como singleton antes de chamar este método.
  /// </remarks>
  /// <typeparam name="TCurrentUser">O tipo que informa o usuário atual.</typeparam>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configure">Ação opcional para configurar as opções.</param>
  /// <returns>A coleção de serviços com a integração adicionada.</returns>
  /// <exception cref="ArgumentNullException">Quando a coleção de serviços é nula.</exception>
  public static IServiceCollection AddTooarkEntityFrameworkCore<TCurrentUser>(
    this IServiceCollection services,
    Action<EntityAuditOptions>? configure = null
  )
    where TCurrentUser : class, ICurrentUser
  {
    ArgumentNullException.ThrowIfNull(services);

    // O registro da própria aplicação prevalece, qualquer que seja o tempo de vida que ela escolheu
    services.TryAddScoped<ICurrentUser, TCurrentUser>();

    return services.AddTooarkEntityFrameworkCore(configure);
  }
}
