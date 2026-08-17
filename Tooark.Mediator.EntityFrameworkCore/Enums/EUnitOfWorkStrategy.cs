namespace Tooark.Mediator.EntityFrameworkCore.Enums;

/// <summary>
/// Enumeração para definir a estratégia de persistência da unidade de trabalho no pipeline.
/// </summary>
public enum EUnitOfWorkStrategy
{
  /// <summary>
  /// Persiste as alterações ao final do comando, com a transação implícita do Entity Framework Core.
  /// </summary>
  SaveChanges = 0,

  /// <summary>
  /// Executa o comando dentro de uma transação explícita, persistindo antes de confirmar.
  /// </summary>
  Transaction = 1
}
