namespace Tooark.Mediator.Abstractions;

/// <summary>
/// Representa um tipo de retorno vazio para requisições que não precisam retornar um valor.
/// </summary>
/// <remarks>
/// O tipo Unit é usado para indicar que uma solicitação ou comando não retorna um valor específico.
/// Ele é útil para simplificar a definição de manipuladores que não precisam retornar dados, evitando
/// a necessidade de usar tipos como void ou criar tipos de resposta personalizados para casos simples.
/// </remarks>
public readonly struct Unit : IEquatable<Unit>
{
  #region Fields

  /// <summary>
  /// A única instância de Unit, representando o valor vazio.
  /// </summary>
  public static readonly Unit Value = new();

  #endregion

  #region Private Static Fields

  /// <summary>
  /// A tarefa concluída com o valor Unit, criada uma única vez.
  /// </summary>
  private static readonly Task<Unit> _task = System.Threading.Tasks.Task.FromResult(Value);

  #endregion

  #region Properties

  /// <summary>
  /// Retorna uma tarefa concluída com o valor Unit, útil para métodos assíncronos que não retornam um valor.
  /// </summary>
  /// <remarks>
  /// Esta propriedade é uma conveniência para evitar a necessidade de criar novas tarefas para cada chamada
  /// de métodos assíncronos que retornam Unit, permitindo o uso de uma tarefa pré-construída.
  /// </remarks>
  public static Task<Unit> Task => _task;

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Determina se o objeto especificado é igual à instância atual de Unit.
  /// </summary>
  /// <param name="obj">O objeto a ser comparado com a instância atual.</param>
  /// <returns>>true se o objeto especificado for igual à instância atual; caso contrário, false.</returns>
  public override bool Equals(object? obj)
  {
    return obj is Unit;
  }

  /// <summary>
  /// Determina se a instância atual de Unit é igual a outra instância de Unit.
  /// </summary>
  /// <param name="other">A instância de Unit a ser comparada com a instância atual.</param>
  /// <returns>true se as instâncias forem iguais; caso contrário, false.</returns>
  public bool Equals(Unit other)
  {
    return true;
  }

  /// <summary>
  /// Retorna um código hash para a instância atual de Unit.
  /// </summary>
  /// <returns>Um código hash para a instância atual.</returns>
  public override int GetHashCode()
  {
    return 0;
  }

  /// <summary>
  /// Determina se duas instâncias de Unit são iguais.
  /// </summary>
  /// <param name="left">A primeira instância de Unit a ser comparada.</param>
  /// <param name="right">A segunda instância de Unit a ser comparada.</param>
  /// <returns>>true se as instâncias forem iguais; caso contrário, false.</returns>
  public static bool operator ==(Unit left, Unit right)
  {
    return left.Equals(right);
  }

  /// <summary>
  /// Determina se duas instâncias de Unit são diferentes.
  /// </summary>
  /// <param name="left">A primeira instância de Unit a ser comparada.</param>
  /// <param name="right">A segunda instância de Unit a ser comparada.</param>
  /// <returns>true se as instâncias forem diferentes; caso contrário, false.</returns>
  public static bool operator !=(Unit left, Unit right)
  {
    return !left.Equals(right);
  }

  /// <summary>
  /// Retorna uma representação em string da instância de Unit.
  /// </summary>
  /// <returns>Uma string que representa a instância de Unit.</returns>
  public override string ToString()
  {
    return "()";
  }

  #endregion
}
