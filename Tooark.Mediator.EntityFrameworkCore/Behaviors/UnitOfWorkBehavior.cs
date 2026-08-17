using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Behaviors;
using Tooark.Mediator.EntityFrameworkCore.Interfaces;

namespace Tooark.Mediator.EntityFrameworkCore.Behaviors;

/// <summary>
/// Behavior que persiste as alterações do comando ao final do pipeline.
/// </summary>
/// <remarks>
/// A restrição a <see cref="ICommand{TResponse}"/> mantém as consultas fora da esteira: o container
/// ignora o behavior ao despachar requisições que não a satisfazem.
/// A persistência só ocorre quando o pipeline conclui sem exceção — a exceção do manipulador sobe antes,
/// deixando as alterações sem efeito. Um único <c>SaveChanges</c> já é atômico, pois o Entity Framework
/// Core envolve o lote em uma transação implícita.
/// </remarks>
/// <typeparam name="TRequest">O tipo de comando que o behavior processa.</typeparam>
/// <typeparam name="TResponse">O tipo de resposta do comando.</typeparam>
/// <param name="unitOfWork">A unidade de trabalho responsável pela persistência.</param>
/// <param name="scope">O controle de aninhamento de comandos do escopo.</param>
internal sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork, UnitOfWorkScope scope)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : ICommand<TResponse>
{
  /// <inheritdoc/>
  public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
  {
    // Comando aninhado participa da unidade de trabalho iniciada pelo comando mais externo
    var isOutermost = scope.Enter();

    try
    {
      var response = await next(cancellationToken);

      // Persiste apenas ao concluir o comando mais externo, mantendo a operação como uma unidade
      if (isOutermost)
      {
        await unitOfWork.SaveChangesAsync(cancellationToken);
      }

      return response;
    }
    finally
    {
      scope.Exit();
    }
  }
}
