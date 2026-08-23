namespace Tooark.Dtos;

/// <summary>
/// Classe para parâmetros de busca com parâmetro de ordenação.
/// </summary>
public class SearchOrderDto : SearchDto
{
  #region Properties

  /// <summary>
  /// Referência a ser ordenada.
  /// </summary>
  /// <remarks>
  /// O nome da coluna da tabela para ser ordenada. Sem valor informado, cabe a quem consome decidir a
  /// ordenação padrão.
  /// </remarks>
  public string? OrderBy { get; set; }

  /// <summary>
  /// Sentido da ordenação.Crescente=true ou Decrescente=false. Por padrão a ordenação é crescente.
  /// </summary>
  /// <value>Valor padrão é <c>true</c>.</value>
  /// <remarks>
  /// Para ordenação crescente atribuir <c>true</c> ou decrescente atribuir <c>false</c>.
  /// </remarks>
  public bool OrderAsc { get; set; } = true;

  #endregion
}
