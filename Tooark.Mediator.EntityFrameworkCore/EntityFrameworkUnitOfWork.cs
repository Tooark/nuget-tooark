using Microsoft.EntityFrameworkCore;
using Tooark.Mediator.EntityFrameworkCore.Interfaces;

namespace Tooark.Mediator.EntityFrameworkCore;

/// <summary>
/// Implementação da unidade de trabalho sobre um contexto do Entity Framework Core.
/// </summary>
/// <remarks>
/// Registrada fechada sobre o contexto informado em <c>AddTooarkMediatorUnitOfWork</c>, o que mantém o
/// behavior com os dois parâmetros genéricos exigidos pelo pipeline.
/// </remarks>
/// <typeparam name="TContext">O tipo do contexto do Entity Framework Core.</typeparam>
/// <param name="context">O contexto do Entity Framework Core.</param>
internal sealed class EntityFrameworkUnitOfWork<TContext>(TContext context) : IUnitOfWork
  where TContext : DbContext
{
  /// <inheritdoc/>
  public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    return context.SaveChangesAsync(cancellationToken);
  }

  /// <inheritdoc/>
  public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
  {
    // A estratégia de resiliência do provedor pode reexecutar a operação, então a transação explícita
    // precisa ser aberta dentro dela: abrir por fora lança quando há retentativa configurada.
    var strategy = context.Database.CreateExecutionStrategy();

    return strategy.ExecuteAsync(async token =>
    {
      await using var transaction = await context.Database.BeginTransactionAsync(token);

      var result = await operation(token);

      await context.SaveChangesAsync(token);
      await transaction.CommitAsync(token);

      return result;
    }, cancellationToken);
  }
}
