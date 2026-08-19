using System.Collections.Concurrent;
using System.Reflection;
using Tooark.Exceptions;

namespace Tooark.Utils;

/// <summary>
/// Classe estática que fornece métodos buscar campo name, title e description.
/// </summary>
public static class GetInfo
{
  #region Methods

  /// <summary>
  /// Obtém o nome localizado de uma lista de objetos.
  /// </summary>
  /// <remarks>
  /// O método tenta obter o nome no idioma solicitado. Se não encontrar, tenta obter no idioma padrão da aplicação.
  /// Se não encontrar, retorna o primeiro item da lista.
  /// </remarks>
  /// <param name="list">A lista de objetos.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>O nome localizado, ou uma string vazia se a lista for nula ou vazia.</returns>
  /// <exception cref="GetInfoException">Se a propriedade 'LanguageCode' ou 'Name' não existir no tipo T.</exception>
  public static string Name<T>(IList<T>? list, string? languageCode = null)
  {
    return InternalGetInfo.Name(list, languageCode);
  }

  /// <summary>
  /// Obtém o título localizado de uma lista de objetos.
  /// </summary>
  /// <remarks>
  /// O método tenta obter o nome no idioma solicitado. Se não encontrar, tenta obter no idioma padrão da aplicação.
  /// Se não encontrar, retorna o primeiro item da lista.
  /// </remarks>
  /// <param name="list">A lista de objetos.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>O título localizado, ou uma string vazia se a lista for nula ou vazia.</returns>
  /// <exception cref="GetInfoException">Se a propriedade 'LanguageCode' ou 'Title' não existir no tipo T.</exception>
  public static string Title<T>(IList<T>? list, string? languageCode = null)
  {
    return InternalGetInfo.Title(list, languageCode);
  }

  /// <summary>
  /// Obtém a descrição localizado de uma lista de objetos.
  /// </summary>
  /// <remarks>
  /// O método tenta obter o nome no idioma solicitado. Se não encontrar, tenta obter no idioma padrão da aplicação.
  /// Se não encontrar, retorna o primeiro item da lista.
  /// </remarks>
  /// <param name="list">A lista de objetos.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>A descrição localizada, ou uma string vazia se a lista for nula ou vazia.</returns>
  /// <exception cref="GetInfoException">Se a propriedade 'LanguageCode' ou 'Description' não existir no tipo T.</exception>
  public static string Description<T>(IList<T>? list, string? languageCode = null)
  {
    return InternalGetInfo.Description(list, languageCode);
  }

  /// <summary>
  /// Obtém as palavras-chave localizadas de uma lista de objetos.
  /// </summary>
  /// <remarks>
  /// O método tenta obter as palavras-chave no idioma solicitado. Se não encontrar, tenta obter no idioma padrão da aplicação.
  /// Se não encontrar, retorna o primeiro item da lista.
  /// </remarks>
  /// <param name="list">A lista de objetos.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>As palavras-chave localizadas, ou uma string vazia se a lista for nula ou vazia.</returns>
  /// <exception cref="GetInfoException">Se a propriedade 'LanguageCode' ou 'Keywords' não existir no tipo T.</exception>
  public static string Keywords<T>(IList<T>? list, string? languageCode = null)
  {
    return InternalGetInfo.Keywords(list, languageCode);
  }

  /// <summary>
  /// Obtém um valor localizado de uma propriedade em uma lista de objetos.
  /// </summary>
  /// <typeparam name="T">O tipo de objeto na lista.</typeparam>
  /// <param name="list">A lista de objetos do tipo T.</param>
  /// <param name="property">O nome da propriedade a ser obtida do objeto.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>
  /// O valor da propriedade localizada como uma string. Se o item correspondente ao idioma solicitado não for
  /// encontrado, tenta retornar o valor no idioma padrão. Se nenhum item for encontrado, retorna uma string vazia.
  /// </returns>
  /// <exception cref="GetInfoException">
  /// Lança uma exceção se a propriedade 'LanguageCode' ou a propriedade especificada não existir no tipo T.
  /// </exception>
  public static string Custom<T>(IList<T>? list, string property, string? languageCode = null)
  {
    return InternalGetInfo.GetLanguageCode(list, property, languageCode);
  }

  #endregion
}

/// <summary>
/// Classe estática interna que fornece métodos buscar campo name, title e description.
/// </summary>
internal static class InternalGetInfo
{
  #region Private Static Fields

  /// <summary>
  /// Cache das propriedades já resolvidas por reflexão, evitando repetir a busca a cada chamada.
  /// </summary>
  private static readonly ConcurrentDictionary<(Type Type, string Property), PropertyInfo?> PropertyCache = new();

  #endregion

  #region Internal Methods

  /// <summary>
  /// Obtém o nome localizado de uma lista de objetos.
  /// </summary>
  /// <remarks>
  /// O método tenta obter o nome no idioma solicitado. Se não encontrar, tenta obter no idioma padrão da aplicação.
  /// Se não encontrar, retorna o primeiro item da lista.
  /// </remarks>
  /// <param name="list">A lista de objetos.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>O nome localizado.</returns>
  internal static string Name<T>(IList<T>? list, string? languageCode = null)
  {
    // Retorna o nome localizado no idioma solicitado.
    return GetLanguageCode(list, "Name", languageCode);
  }

  /// <summary>
  /// Obtém o título localizado de uma lista de objetos.
  /// </summary>
  /// <remarks>
  /// O método tenta obter o título no idioma solicitado. Se não encontrar, tenta obter no idioma padrão da aplicação.
  /// Se não encontrar, retorna o primeiro item da lista.
  /// </remarks>
  /// <param name="list">A lista de objetos.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>O título localizado.</returns>
  internal static string Title<T>(IList<T>? list, string? languageCode = null)
  {
    // Retorna o título localizado no idioma solicitado.
    return GetLanguageCode(list, "Title", languageCode);
  }

  /// <summary>
  /// Obtém a descrição localizado de uma lista de objetos.
  /// </summary>
  /// <remarks>
  /// O método tenta obter a descrição no idioma solicitado. Se não encontrar, tenta obter no idioma padrão da aplicação.
  /// Se não encontrar, retorna o primeiro item da lista.
  /// </remarks>
  /// <param name="list">A lista de objetos.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>A descrição localizada.</returns>
  internal static string Description<T>(IList<T>? list, string? languageCode = null)
  {
    // Retorna a descrição localizada no idioma solicitado.
    return GetLanguageCode(list, "Description", languageCode);
  }

  /// <summary>
  /// Obtém as palavras-chave localizadas de uma lista de objetos.
  /// </summary>
  /// <remarks>
  /// O método tenta obter as palavras-chave no idioma solicitado. Se não encontrar, tenta obter no idioma padrão da aplicação.
  /// Se não encontrar, retorna o primeiro item da lista.
  /// </remarks>
  /// <param name="list">A lista de objetos.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>As palavras-chave localizadas.</returns>
  internal static string Keywords<T>(IList<T>? list, string? languageCode = null)
  {
    // Retorna as palavras-chave localizadas no idioma solicitado.
    return GetLanguageCode(list, "Keywords", languageCode);
  }

  /// <summary>
  /// Obtém um valor de uma propriedade localizado de uma lista de objetos.
  /// </summary>
  /// <remarks>
  /// O método tenta obter no idioma solicitado.
  /// Se não encontrar, tenta obter no idioma padrão da aplicação.
  /// Se não encontrar, tenta obter o primeiro item da lista.
  /// Se não encontrar, retorna uma string vazia.
  /// </remarks>
  /// <typeparam name="T">O tipo de objeto na lista.</typeparam>
  /// <param name="list">A lista de objetos do tipo T.</param>
  /// <param name="nameProperty">O nome da propriedade a ser obtida do objeto.</param>
  /// <param name="languageCode">O código de idioma. Parâmetro opcional. Padrão é o idioma atual.</param>
  /// <returns>O valor da propriedade localizada como uma string.</returns>
  /// <exception cref="GetInfoException">
  /// Lança uma exceção se a propriedade 'LanguageCode' ou a propriedade especificada não existir no tipo T.
  /// </exception>
  internal static string GetLanguageCode<T>(IList<T>? list, string nameProperty, string? languageCode = null)
  {
    // Se o nome da propriedade não for informado, lança uma exceção.
    if (string.IsNullOrWhiteSpace(nameProperty))
    {
      // Lança uma exceção se o nome da propriedade não for informado.
      throw new GetInfoException("NotFound.Property;null");
    }

    // Obtém as propriedades 'LanguageCode'.
    var languageCodeProperty = GetProperty<T>("LanguageCode");

    // Se a propriedade 'LanguageCode' não existir, lança uma exceção.
    if (languageCodeProperty == null)
    {
      // Lança uma exceção se a propriedade 'LanguageCode' não existir.
      throw new GetInfoException("NotFound.Property;LanguageCode");
    }

    // Obtém a propriedade especificada.
    var property = GetProperty<T>(nameProperty);

    // Se a propriedade especificada não existir, lança uma exceção.
    if (property == null)
    {
      // Lança uma exceção se a propriedade especificada não existir.
      throw new GetInfoException($"NotFound.Property;{nameProperty}");
    }

    // Se a lista não tiver itens, retorna uma string vazia.
    if (list == null || list.Count == 0)
    {
      // Retorna uma string vazia se não houver o que localizar.
      return string.Empty;
    }

    // Se o código de idioma for nulo, usa o idioma atual.
    languageCode ??= Language.Current;

    // Código de idioma padrão.
    var defaultLanguageCode = Language.Default;

    // Obtém o item atual com base no código de idioma.
    var item =
      list.FirstOrDefault(x => ReadValue(languageCodeProperty, x) == languageCode) ??
      list.FirstOrDefault(x => ReadValue(languageCodeProperty, x) == defaultLanguageCode) ??
      list.FirstOrDefault();

    // Retorna o valor da propriedade especificada. Se não encontrar, retorna uma string vazia.
    return EqualityComparer<T>.Default.Equals(item, default) ?
      string.Empty :
      ReadValue(property, item);
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Obtém a propriedade do tipo T, reaproveitando o resultado já resolvido por reflexão.
  /// </summary>
  /// <typeparam name="T">O tipo de objeto na lista.</typeparam>
  /// <param name="nameProperty">O nome da propriedade a ser obtida.</param>
  /// <returns>A propriedade encontrada, ou nulo se ela não existir no tipo T.</returns>
  private static PropertyInfo? GetProperty<T>(string nameProperty)
  {
    // Busca a propriedade no cache, resolvendo por reflexão apenas na primeira vez.
    return PropertyCache.GetOrAdd(
      (typeof(T), nameProperty),
      key => key.Type.GetProperty(key.Property));
  }

  /// <summary>
  /// Lê o valor de uma propriedade como texto, sem assumir que o tipo dela é string.
  /// </summary>
  /// <param name="property">A propriedade a ser lida.</param>
  /// <param name="item">O objeto de onde o valor será lido.</param>
  /// <returns>O valor da propriedade como texto, ou uma string vazia se o valor for nulo.</returns>
  private static string ReadValue(PropertyInfo property, object? item)
  {
    // Converte o valor para texto, aceitando propriedades que não sejam string.
    return property.GetValue(item)?.ToString() ?? string.Empty;
  }

  #endregion
}
