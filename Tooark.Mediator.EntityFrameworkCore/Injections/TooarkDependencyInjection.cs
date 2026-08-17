using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tooark.Exceptions;
using Tooark.Mediator.EntityFrameworkCore.Behaviors;
using Tooark.Mediator.EntityFrameworkCore.Enums;
using Tooark.Mediator.EntityFrameworkCore.Interfaces;
using Tooark.Mediator.Injections;

namespace Tooark.Mediator.EntityFrameworkCore.Injections;

/// <summary>
/// Classe para adicionar a unidade de trabalho do Entity Framework Core ao pipeline do Mediator.
/// </summary>
public static class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona a unidade de trabalho ao pipeline de requisições do Mediator.
  /// </summary>
  /// <remarks>
  /// O behavior é registrado na posição em que este método é chamado, e os behaviors executam na ordem
  /// de registro. Registre a validação antes da unidade de trabalho: não faz sentido preparar a
  /// persistência para em seguida rejeitar a requisição.
  /// Aplica-se apenas a comandos; consultas não passam pela persistência.
  /// </remarks>
  /// <typeparam name="TContext">O tipo do contexto do Entity Framework Core.</typeparam>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="strategy">A estratégia de persistência. Padrão <see cref="EUnitOfWorkStrategy.SaveChanges"/>.</param>
  /// <returns>A coleção de serviços com a unidade de trabalho adicionada.</returns>
  /// <exception cref="InternalServerErrorException">Lançada quando a coleção de serviços é nula ou a estratégia não é suportada.</exception>
  public static IServiceCollection AddTooarkMediatorUnitOfWork<TContext>(
    this IServiceCollection services,
    EUnitOfWorkStrategy strategy = EUnitOfWorkStrategy.SaveChanges
  )
    where TContext : DbContext
  {
    // Verifica se a coleção de serviços é nula
    if (services == null)
    {
      throw new InternalServerErrorException("Mediator.Null.Service");
    }

    // Seleciona o behavior da estratégia, sem substituição silenciosa para valores desconhecidos
    var behaviorType = strategy switch
    {
      EUnitOfWorkStrategy.SaveChanges => typeof(UnitOfWorkBehavior<,>),
      EUnitOfWorkStrategy.Transaction => typeof(TransactionBehavior<,>),
      _ => throw new InternalServerErrorException($"UnitOfWork.StrategyNotSupported;{strategy}")
    };

    // Registra a unidade de trabalho fechada sobre o contexto informado, no mesmo escopo do contexto
    services.TryAddScoped<IUnitOfWork, EntityFrameworkUnitOfWork<TContext>>();

    // Registra o controle de aninhamento de comandos do escopo
    services.TryAddScoped<UnitOfWorkScope>();

    // Registra o behavior na posição atual da ordem de execução do pipeline
    return services.AddTooarkMediatorBehavior(behaviorType);
  }
}
