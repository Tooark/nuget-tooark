using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Behaviors;
using Tooark.Mediator.EntityFrameworkCore.Interfaces;

namespace Tooark.Mediator.EntityFrameworkCore.Behaviors;

/// <summary>
/// Behavior que executa o comando dentro de uma transação explícita.
/// </summary>
/// <remarks>
/// Alternativa ao <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/> para quando o manipulador persiste
/// mais de uma vez ou combina o contexto com outro recurso transacional. No caso comum, de uma única
/// persistência, a transação explícita é desnecessária.
/// A restrição a <see cref="ICommand{TResponse}"/> mantém as consultas fora da esteira, e comandos
/// aninhados participam da transação já aberta pelo comando mais externo.
/// </remarks>
/// <typeparam name="TRequest">O tipo de comando que o behavior processa.</typeparam>
/// <typeparam name="TResponse">O tipo de resposta do comando.</typeparam>
/// <param name="unitOfWork">A unidade de trabalho responsável pela transação e pela persistência.</param>
/// <param name="scope">O controle de aninhamento de comandos do escopo.</param>
internal sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork, UnitOfWorkScope scope)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : ICommand<TResponse>
{
  /// <inheritdoc/>
  public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
  {
    // Comando aninhado participa da transação aberta pelo comando mais externo
    var isOutermost = scope.Enter();

    try
    {
      if (!isOutermost)
      {
        return await next(cancellationToken);
      }

      return await unitOfWork.ExecuteInTransactionAsync(next.Invoke, cancellationToken);
    }
    finally
    {
      scope.Exit();
    }
  }
}
