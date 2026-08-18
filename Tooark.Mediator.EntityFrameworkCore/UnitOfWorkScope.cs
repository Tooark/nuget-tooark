namespace Tooark.Mediator.EntityFrameworkCore;

/// <summary>
/// Controla o aninhamento de comandos dentro de um mesmo escopo de injeção de dependência.
/// </summary>
/// <remarks>
/// Um comando despachado de dentro de outro comando participa da unidade de trabalho já iniciada:
/// sem esse controle, o comando interno persistiria no meio da operação do externo, que persistiria
/// novamente ao final. Registrada por escopo, acompanha o tempo de vida do contexto.
/// O contexto do Entity Framework Core não é seguro para uso concorrente, então o controle assume
/// despacho sequencial dentro do escopo.
/// </remarks>
internal sealed class UnitOfWorkScope
{
  #region Private Fields

  /// <summary>
  /// Profundidade atual de comandos em processamento no escopo.
  /// </summary>
  private int _depth;

  #endregion

  #region Methods

  /// <summary>
  /// Entra em um nível de processamento.
  /// </summary>
  /// <returns>True quando este é o comando mais externo do escopo.</returns>
  public bool Enter()
  {
    return ++_depth == 1;
  }

  /// <summary>
  /// Sai do nível de processamento atual.
  /// </summary>
  public void Exit()
  {
    _depth--;
  }

  #endregion
}
