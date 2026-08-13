namespace Tooark.Mediator.Enums;

/// <summary>
/// Enumeração para definir a estratégia de publicação de notificações no mediador.
/// </summary>
public enum ENotifyStrategy
{
  /// <summary>
  /// Publica as notificações em paralelo, aguardando todas as tarefas concluírem.
  /// </summary>
  ParallelWhenAll = 0,

  /// <summary>
  /// Publica as notificações de forma sequencial, aguardando cada tarefa concluir antes de iniciar a próxima.
  /// </summary>
  Sequential = 1
}
