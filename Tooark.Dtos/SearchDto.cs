using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tooark.Extensions;

namespace Tooark.Dtos;

/// <summary>
/// Classe para parâmetros de busca.
/// </summary>
/// <remarks>
/// O <see cref="PageSize"/> é limitado por <see cref="PageSizeMax"/>, que existe para que uma requisição
/// não possa pedir a base inteira de uma vez. Um DTO que precise de páginas maiores sobrescreve o limite.
/// </remarks>
public class SearchDto : Dto
{
  #region Constants

  /// <summary>
  /// Tamanho máximo padrão da página.
  /// </summary>
  public const long DefaultPageSizeMax = 100;

  #endregion

  #region Private Properties

  /// <summary>
  /// Informação privada a ser procurada.
  /// </summary>
  private string? _search;

  /// <summary>
  /// Cache para a informação normalizada.
  /// </summary>
  private string? _searchNormalized;

  /// <summary>
  /// Índice privado da paginação.
  /// </summary>
  private long _pageIndex = 1;

  /// <summary>
  /// Tamanho privado da paginação.
  /// </summary>
  private long _pageSize = 10;

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor padrão.
  /// </summary>
  public SearchDto()
  { }

  /// <summary>
  /// Construtor com parâmetro de busca.
  /// </summary>
  /// <param name="search">Informação a ser procurada.</param>
  public SearchDto(string? search)
  {
    Search = search;
  }

  /// <summary>
  /// Construtor com parâmetros de paginação.
  /// </summary>
  /// <param name="pageIndex">Índice da paginação.</param>
  /// <param name="pageSize">Tamanho da paginação.</param>
  public SearchDto(long pageIndex, long pageSize)
  {
    PageIndex = pageIndex;
    PageSize = pageSize;
  }

  /// <summary>
  /// Construtor com parâmetros de busca e paginação.
  /// </summary>
  /// <param name="search">Informação a ser procurada.</param>
  /// <param name="pageIndex">Índice da paginação.</param>
  /// <param name="pageSize">Tamanho da paginação.</param>
  public SearchDto(string? search, long pageIndex, long pageSize)
  {
    Search = search;
    PageIndex = pageIndex;
    PageSize = pageSize;
  }

  #endregion

  #region Properties

  /// <summary>
  /// Tamanho máximo aceito para a página.
  /// </summary>
  /// <remarks>
  /// Sobrescreva em um DTO próprio para permitir páginas maiores em um endpoint específico. Sem um teto,
  /// uma única requisição poderia pedir todos os registros.
  /// <para>
  /// Use a forma de expressão, <c>protected override long PageSizeMax =&gt; 5000;</c>. Uma propriedade
  /// automática com inicializador não serve: o limite é consultado pelo construtor da classe base, que roda
  /// antes dos inicializadores da classe derivada, e o valor seria zero nesse momento.
  /// </para>
  /// </remarks>
  /// <value>Valor padrão é <see cref="DefaultPageSizeMax"/>.</value>
  protected virtual long PageSizeMax => DefaultPageSizeMax;

  /// <summary>
  /// Informação a ser procurada.
  /// </summary>
  public string? Search
  {
    get => _search;
    set
    {
      _search = value;

      // Invalida o cache ao alterar Search
      _searchNormalized = null;
    }
  }

  /// <summary>
  /// Informação a ser procurada normalizada.
  /// </summary>
  [BindNever]
  [JsonIgnore]
  public string? SearchNormalized
  {
    get
    {
      // Normaliza uma única vez por valor de busca
      _searchNormalized ??= _search?.ToNormalize();

      return _searchNormalized;
    }
  }

  /// <summary>
  /// Índice da paginação.
  /// </summary>
  /// <remarks>
  /// O menor índice é 1. Valor menor assume 1.
  /// </remarks>
  /// <value>Parâmetro padrão é 1.</value>
  public long PageIndex
  {
    get => _pageIndex;
    set => _pageIndex = value < 1 ? 1 : value;
  }

  /// <summary>
  /// Índice lógico da paginação.
  /// </summary>
  /// <remarks>
  /// É o <see cref="PageIndex"/> menos um, para uso direto em <c>Skip</c>.
  /// </remarks>
  /// <value>Parâmetro padrão é 0.</value>
  [BindNever]
  [JsonIgnore]
  public long PageIndexLogical
  {
    get => _pageIndex - 1;
  }

  /// <summary>
  /// Tamanho da paginação.
  /// </summary>
  /// <remarks>
  /// Valor negativo assume 0, que significa ignorar o tamanho. Valor acima de <see cref="PageSizeMax"/>
  /// assume o próprio limite.
  /// </remarks>
  /// <value>Parâmetro padrão é 10.</value>
  public long PageSize
  {
    get => _pageSize;
    set => _pageSize =
      value < 0 ? 0 :
      value > PageSizeMax ? PageSizeMax :
      value;
  }

  #endregion
}
