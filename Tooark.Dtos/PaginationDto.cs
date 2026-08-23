using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Tooark.Dtos;

/// <summary>
/// Classe para parâmetros de paginação.
/// </summary>
/// <remarks>
/// Os links gerados reaproveitam a query string da requisição, trocando apenas o índice da página, para que
/// os filtros do endpoint sigam valendo na navegação. Isso significa que **todo** parâmetro da requisição
/// aparece no corpo da resposta: não trafegue credenciais na query string.
/// </remarks>
public class PaginationDto
{
  #region Constants

  /// <summary>
  /// Chave para o parâmetro de busca.
  /// </summary>
  private const string SearchKey = "Search";

  /// <summary>
  /// Chave para o parâmetro de índice da página.
  /// </summary>
  private const string PageIndexKey = "PageIndex";

  /// <summary>
  /// Chave para o parâmetro de tamanho da página.
  /// </summary>
  private const string PageSizeKey = "PageSize";

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor com parâmetro de total de registros.
  /// </summary>
  /// <param name="total">Total de registros.</param>
  public PaginationDto(long total)
  {
    // Total de registros
    Total = total;
  }

  /// <summary>
  /// Construtor com parâmetro de requisição.
  /// </summary>
  /// <param name="request">Requisição HTTP atual.</param>
  /// <exception cref="ArgumentNullException">Se a requisição for nula.</exception>
  public PaginationDto(HttpRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);

    // Monta o link atual da requisição
    CurrentLink = BuildCurrentLink(request);
  }

  /// <summary>
  /// Construtor com parâmetros de total de registros e requisição.
  /// </summary>
  /// <remarks>
  /// O índice e o tamanho da página são lidos da query string da requisição.
  /// </remarks>
  /// <param name="total">Total de registros.</param>
  /// <param name="request">Requisição HTTP atual.</param>
  /// <exception cref="ArgumentNullException">Se a requisição for nula.</exception>
  public PaginationDto(long total, HttpRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);

    // Total de registros
    Total = total;

    // URL base da requisição
    var baseUrl = BuildBaseUrl(request);

    // URL atual da requisição. Base + QueryString
    CurrentLink = $"{baseUrl}{request.QueryString}";

    // Verifica se existem registros
    if (total > 0)
    {
      // QueryString da requisição
      var query = QueryHelpers.ParseQuery(request.QueryString.ToString());

      // Define o índice da página da requisição
      PageIndex = GetQueryValue(PageIndexKey, query);

      // Define o tamanho da página da requisição
      PageSize = GetQueryValue(PageSizeKey, query);

      // Monta a navegação a partir da página atual
      SetLinks(PageIndex, baseUrl, query);
    }
  }

  /// <summary>
  /// Construtor com parâmetros de total de registros, tamanho da página, índice da página, índice da página anterior, índice da página seguinte e requisição.
  /// </summary>
  /// <param name="total">Total de registros.</param>
  /// <param name="pageSize">Tamanho da página.</param>
  /// <param name="pageIndex">Índice da página.</param>
  /// <param name="previous">Índice da página anterior.</param>
  /// <param name="next">Índice da página seguinte.</param>
  /// <param name="request">Requisição HTTP atual.</param>
  /// <exception cref="ArgumentNullException">Se a requisição for nula.</exception>
  public PaginationDto(long total, long pageSize, long pageIndex, long previous, long next, HttpRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);

    // Atualiza os valores conforme os parâmetros
    Total = total;
    PageSize = pageSize;
    PageIndex = pageIndex;

    // URL base da requisição
    var baseUrl = BuildBaseUrl(request);

    // URL atual da requisição. Base + QueryString
    CurrentLink = $"{baseUrl}{request.QueryString}";

    // Verifica se existem registros e se a paginação faz sentido
    if (HasPages())
    {
      // QueryString da requisição
      var query = QueryHelpers.ParseQuery(request.QueryString.ToString());

      // Atualiza o tamanho da página na QueryString
      query[PageSizeKey] = PageSize.ToString();

      // Os índices vêm prontos do chamador, e não calculados a partir da página atual,
      // mas ainda precisam existir dentro do total de registros
      if (previous >= 1)
      {
        SetPrevious(previous, baseUrl, query);
      }

      if (next >= 1 && (next - 1) * PageSize < Total)
      {
        SetNext(next, baseUrl, query);
      }
    }
  }

  /// <summary>
  /// Construtor com parâmetros de total de registros, busca com paginação e requisição.
  /// </summary>
  /// <param name="total">Total de registros.</param>
  /// <param name="searchDto">Objeto de busca com paginação.</param>
  /// <param name="request">Requisição HTTP atual.</param>
  /// <exception cref="ArgumentNullException">Se a busca ou a requisição forem nulas.</exception>
  public PaginationDto(long total, SearchDto searchDto, HttpRequest request)
  {
    ArgumentNullException.ThrowIfNull(searchDto);
    ArgumentNullException.ThrowIfNull(request);

    // Atualiza os valores conforme os parâmetros
    Total = total;
    PageSize = searchDto.PageSize;
    PageIndex = searchDto.PageIndex;

    // URL base da requisição
    var baseUrl = BuildBaseUrl(request);

    // URL atual da requisição. Base + QueryString
    CurrentLink = $"{baseUrl}{request.QueryString}";

    // Verifica se existem registros e se a paginação faz sentido
    if (HasPages())
    {
      // QueryString da requisição
      var query = QueryHelpers.ParseQuery(request.QueryString.ToString());

      // Verifica se existe parâmetro de busca
      if (!string.IsNullOrEmpty(searchDto.Search))
      {
        // Atualiza parâmetro de busca na QueryString
        query[SearchKey] = searchDto.Search;
      }

      // Atualiza o tamanho da página na QueryString
      query[PageSizeKey] = PageSize.ToString();

      // Monta a navegação a partir da página atual
      SetLinks(PageIndex, baseUrl, query);
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Total de registros.
  /// </summary>
  /// <value>Valor padrão é: 0.</value>
  public long Total { get; private set; }

  /// <summary>
  /// Tamanho da página.
  /// </summary>
  /// <remarks>
  /// Nos construtores que leem a requisição, é o valor da query string, ou 0 quando ela não o traz.
  /// </remarks>
  /// <value>Valor padrão é: 10.</value>
  public long PageSize { get; private set; } = 10;

  /// <summary>
  /// Índice da página.
  /// </summary>
  /// <remarks>
  /// Nos construtores que leem a requisição, é o valor da query string, ou 0 quando ela não o traz.
  /// </remarks>
  /// <value>Valor padrão é: 1.</value>
  public long PageIndex { get; private set; } = 1;

  /// <summary>
  /// Índice da página anterior.
  /// </summary>
  /// <value>Valor padrão é: nulo.</value>
  public long? Previous { get; private set; }

  /// <summary>
  /// Índice da página seguinte.
  /// </summary>
  /// <value>Valor padrão é: nulo.</value>
  public long? Next { get; private set; }

  /// <summary>
  /// Link da página atual.
  /// </summary>
  /// <value>Valor padrão é: nulo.</value>
  public string? CurrentLink { get; private set; }

  /// <summary>
  /// Link da página anterior.
  /// </summary>
  /// <value>Valor padrão é: nulo.</value>
  public string? PreviousLink { get; private set; }

  /// <summary>
  /// Link da página seguinte.
  /// </summary>
  /// <value>Valor padrão é: nulo.</value>
  public string? NextLink { get; private set; }

  #endregion

  #region Private Methods

  /// <summary>
  /// Monta a URL base da requisição, sem a query string.
  /// </summary>
  /// <param name="request">Requisição HTTP atual.</param>
  /// <returns>Retorna a URL base da requisição.</returns>
  private static string BuildBaseUrl(HttpRequest request)
  {
    return $"{request.Scheme}://{request.Host}{request.Path}";
  }

  /// <summary>
  /// Helper para montar o link atual da requisição.
  /// </summary>
  /// <param name="request">Requisição HTTP atual.</param>
  /// <returns>Retorna o link completo da requisição.</returns>
  private static string BuildCurrentLink(HttpRequest request)
  {
    return $"{BuildBaseUrl(request)}{request.QueryString}";
  }

  /// <summary>
  /// Verifica se há registros suficientes para existir mais de uma página.
  /// </summary>
  /// <returns>Verdadeiro quando a navegação entre páginas faz sentido.</returns>
  private bool HasPages()
  {
    return Total > 0 && PageSize > 0 && PageIndex >= 1 && PageSize < Total;
  }

  /// <summary>
  /// Função para obter o valor de um parâmetro da QueryString.
  /// </summary>
  /// <param name="key">Chave do parâmetro.</param>
  /// <param name="query">Dicionário de parâmetros da QueryString.</param>
  /// <returns>Retorna o valor do parâmetro.</returns>
  private static long GetQueryValue(string key, Dictionary<string, StringValues> query)
  {
    // Se existir a chave no dicionário e for possível converter para long, retorna o valor. Senão retorna 0.
    return
      query.TryGetValue(key, out StringValues value) &&
      long.TryParse(value, out long result) ?
      result :
      0;
  }

  /// <summary>
  /// Função para gerar um link de paginação.
  /// </summary>
  /// <remarks>
  /// Trabalha sobre uma cópia da query string: alterar o dicionário recebido faria cada link montado
  /// interferir no seguinte.
  /// </remarks>
  /// <param name="baseUrl">URL base.</param>
  /// <param name="query">Dicionário de parâmetros da QueryString.</param>
  /// <param name="pageIndex">Índice da página.</param>
  /// <returns>Retorna o link de paginação.</returns>
  private static string GenerateLink(string baseUrl, Dictionary<string, StringValues> query, long pageIndex)
  {
    // Copia os parâmetros para não alterar os do chamador
    var parameters = new Dictionary<string, StringValues>(query)
    {
      // Define o índice da página no link
      [PageIndexKey] = pageIndex.ToString()
    };

    // Retorna o link de paginação.
    return $"{baseUrl}{QueryString.Create(parameters)}";
  }

  /// <summary>
  /// Define os índices e links das páginas anterior e seguinte a partir da página atual.
  /// </summary>
  /// <param name="index">Índice da página atual.</param>
  /// <param name="baseUrl">URL base.</param>
  /// <param name="query">Dicionário de parâmetros da QueryString.</param>
  private void SetLinks(long index, string baseUrl, Dictionary<string, StringValues> query)
  {
    // Verifica se a paginação faz sentido para o total de registros
    if (!HasPages())
    {
      return;
    }

    // A página anterior existe quando a atual não é a primeira
    if (index > 1)
    {
      SetPrevious(index - 1, baseUrl, query);
    }

    // A página seguinte existe quando a atual não esgota o total de registros
    if (index * PageSize < Total)
    {
      SetNext(index + 1, baseUrl, query);
    }
  }

  /// <summary>
  /// Função para definir o índice e link da página anterior.
  /// </summary>
  /// <param name="index">Índice da página anterior.</param>
  /// <param name="baseUrl">URL base.</param>
  /// <param name="query">Dicionário de parâmetros da QueryString.</param>
  private void SetPrevious(long index, string baseUrl, Dictionary<string, StringValues> query)
  {
    // Índice da página anterior
    Previous = index;

    // Link da página anterior
    PreviousLink = GenerateLink(baseUrl, query, index);
  }

  /// <summary>
  /// Função para definir o índice e link da página seguinte.
  /// </summary>
  /// <param name="index">Índice da página seguinte.</param>
  /// <param name="baseUrl">URL base.</param>
  /// <param name="query">Dicionário de parâmetros da QueryString.</param>
  private void SetNext(long index, string baseUrl, Dictionary<string, StringValues> query)
  {
    // Índice da página seguinte
    Next = index;

    // Link da página seguinte
    NextLink = GenerateLink(baseUrl, query, index);
  }

  #endregion
}
