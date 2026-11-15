using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Entities.EntityFrameworkCore.Interceptors;
using Tooark.Exceptions;

namespace Tooark.Entities.EntityFrameworkCore.Extensions;

/// <summary>
/// Classe com as extensões que ligam o interceptor das entidades às opções do contexto.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
  /// <summary>
  /// Adiciona o interceptor das entidades ao contexto.
  /// </summary>
  /// <remarks>
  /// O interceptor é resolvido no provedor informado e passa a resolver o usuário atual nele, a cada gravação.
  /// No <c>AddDbContext((sp, options) => ...)</c> esse provedor é o escopo da requisição. No
  /// <c>AddDbContextPool</c>, no <c>AddDbContextFactory</c> e nas opções singleton ele é o provedor raiz, que
  /// só entrega um usuário atual registrado como singleton.
  /// </remarks>
  /// <param name="builder">O construtor das opções do contexto.</param>
  /// <param name="serviceProvider">O provedor de serviços da aplicação.</param>
  /// <returns>O construtor das opções, com o interceptor adicionado.</returns>
  /// <exception cref="ArgumentNullException">Quando o construtor ou o provedor é nulo.</exception>
  /// <exception cref="InternalServerErrorException">
  /// Quando o interceptor não foi registrado com <c>AddTooarkEntityFrameworkCore</c>.
  /// </exception>
  public static DbContextOptionsBuilder AddTooarkInterceptors(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider)
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(serviceProvider);

    // Sem o registro, o erro do container não diria qual chamada faltou
    var interceptor = serviceProvider.GetService<TooarkEntityInterceptor>()
      ?? throw new InternalServerErrorException("Entities.Audit.InterceptorNotRegistered");

    return builder.AddInterceptors(interceptor);
  }

  /// <summary>
  /// Adiciona o interceptor das entidades ao contexto, mantendo o tipo do construtor na cadeia fluente.
  /// </summary>
  /// <typeparam name="TContext">O tipo do contexto do Entity Framework Core.</typeparam>
  /// <param name="builder">O construtor das opções do contexto.</param>
  /// <param name="serviceProvider">O provedor de serviços da aplicação.</param>
  /// <returns>O construtor das opções, com o interceptor adicionado.</returns>
  /// <exception cref="ArgumentNullException">Quando o construtor ou o provedor é nulo.</exception>
  /// <exception cref="InternalServerErrorException">
  /// Quando o interceptor não foi registrado com <c>AddTooarkEntityFrameworkCore</c>.
  /// </exception>
  public static DbContextOptionsBuilder<TContext> AddTooarkInterceptors<TContext>(
    this DbContextOptionsBuilder<TContext> builder,
    IServiceProvider serviceProvider
  )
    where TContext : DbContext
  {
    ((DbContextOptionsBuilder)builder).AddTooarkInterceptors(serviceProvider);

    return builder;
  }
}
