namespace Tooark.Mediator.EntityFrameworkCore.Interfaces;

/// <summary>
/// Interface para a unidade de trabalho do pipeline de requisições.
/// </summary>
/// <remarks>
/// Representa a persistência das alterações acumuladas durante o processamento de um comando.
/// A interface não expõe tipos do Entity Framework, permitindo substituir a implementação em testes.
/// </remarks>
public interface IUnitOfWork
{
  /// <summary>
  /// Persiste as alterações pendentes.
  /// </summary>
  /// <param name="cancellationToken">O token de cancelamento para a operação assíncrona. Opcional.</param>
  /// <returns>A quantidade de registros afetados.</returns>
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Executa a operação dentro de uma transação explícita, persistindo as alterações antes de confirmar.
  /// </summary>
  /// <remarks>
  /// A transação é revertida quando a operação lança. A execução respeita a estratégia de resiliência do
  /// provedor, então a operação pode ser executada mais de uma vez em caso de falha transitória — ela
  /// precisa ser idempotente.
  /// </remarks>
  /// <typeparam name="TResult">O tipo de resultado da operação.</typeparam>
  /// <param name="operation">A operação a ser executada dentro da transação.</param>
  /// <param name="cancellationToken">O token de cancelamento para a operação assíncrona. Opcional.</param>
  /// <returns>O resultado da operação.</returns>
  Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
}
