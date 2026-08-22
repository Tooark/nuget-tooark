using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Tooark.Utils;

namespace Tooark.Extensions;

/// <summary>
/// Método de extensão para StringLocalizer que utiliza traduções em arquivos JSON.
/// </summary>
/// <remarks>
/// A chave pode trazer parâmetros no formato <c>Chave;parametro1;parametro2</c>. Os parâmetros substituem
/// os marcadores <c>{0}</c>, <c>{1}</c> e assim por diante do texto traduzido, e são eles próprios
/// traduzidos quando correspondem a uma chave existente.
/// <para>
/// As traduções de cada idioma são lidas do disco uma única vez por processo e ficam em memória, indexadas
/// por chave. Como consequência, arquivos de tradução alterados em disco só passam a valer no próximo
/// início do processo.
/// </para>
/// </remarks>
public class JsonStringLocalizerExtension : IStringLocalizer
{
  #region Private Fields

  /// <summary>
  /// Localizador interno de strings JSON.
  /// </summary>
  private readonly InternalJsonStringLocalizer _internalLocalizer = new();

  #endregion

  #region Properties

  /// <summary>
  /// Obtém a string localizada.
  /// </summary>
  /// <param name="name">Chave da string, com os parâmetros separados por ponto e vírgula.</param>
  /// <returns>
  /// A string localizada. Quando a chave não existe, <see cref="LocalizedString.ResourceNotFound"/> é
  /// verdadeiro e o valor é o texto recebido, inalterado.
  /// </returns>
  public LocalizedString this[string name]
  {
    get
    {
      // Verifica se a chave é nula ou vazia
      if (string.IsNullOrEmpty(name))
      {
        return new LocalizedString(string.Empty, string.Empty, true);
      }

      // Obtém a string localizada e se a chave foi encontrada
      var (value, found) = _internalLocalizer.GetLocalizedString(name);

      // Retorna a string localizada, sinalizando quando a chave não existe
      return new LocalizedString(name, value, !found);
    }
  }

  /// <summary>
  /// Obtém a string localizada formatada com os argumentos fornecidos.
  /// </summary>
  /// <param name="name">Chave da string.</param>
  /// <param name="arguments">Parâmetros da string.</param>
  /// <returns>A string localizada com os parâmetros substituídos.</returns>
  public LocalizedString this[string name, params object[] arguments]
  {
    get
    {
      // Verifica se a chave é nula ou vazia
      if (string.IsNullOrEmpty(name))
      {
        return new LocalizedString(string.Empty, string.Empty, true);
      }

      // Junta a chave e os parâmetros no mesmo formato aceito pela indexação por chave
      var composed = arguments is { Length: > 0 } ?
        string.Join(";", new[] { name }.Concat(arguments.Select(argument => argument?.ToString() ?? string.Empty))) :
        name;

      // Obtém a string localizada e se a chave foi encontrada
      var (value, found) = _internalLocalizer.GetLocalizedString(composed);

      // Retorna a string localizada, sinalizando quando a chave não existe
      return new LocalizedString(composed, value, !found);
    }
  }

  #endregion

  #region Methods

  /// <summary>
  /// Obtém todas as strings localizadas.
  /// </summary>
  /// <param name="includeParentCultures">Utilizado para avaliar se é a cultura padrão (True) ou a cultura atual (False).</param>
  /// <returns>Strings localizadas.</returns>
  public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
  {
    return _internalLocalizer.GetAllStrings(includeParentCultures);
  }

  #endregion
}

/// <summary>
/// Método interno de extensão para StringLocalizer que utiliza traduções em arquivos JSON.
/// </summary>
internal class InternalJsonStringLocalizer
{
  #region Private Static Fields

  /// <summary>
  /// Traduções já carregadas do disco, por idioma, compartilhadas por todas as instâncias.
  /// </summary>
  /// <remarks>
  /// O localizador é registrado como transitório, então uma instância nova é criada a cada resolução.
  /// Sem este cache, os arquivos JSON seriam lidos e interpretados de novo em cada uma delas. A busca por
  /// chave é uma consulta a dicionário, e por isso não há uma segunda camada de cache na frente dela.
  /// </remarks>
  private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> Translations = new();

  #endregion

  #region Internal Methods

  /// <summary>
  /// Obtém a string localizada.
  /// </summary>
  /// <param name="keyParameter">Chave da string localizada, com os parâmetros separados por ponto e vírgula.</param>
  /// <param name="cultureSelect">Código de idioma selecionado. Parâmetro opcional.</param>
  /// <returns>A string localizada e se a chave foi encontrada.</returns>
  internal (string Value, bool Found) GetLocalizedString(string keyParameter, string? cultureSelect = null)
  {
    // Separa a chave dos parâmetros
    var listInfo = keyParameter.Split(';');

    // Obtém o código de idioma selecionado ou o código de idioma atual
    var culture = cultureSelect ?? Language.Current;

    // Busca o texto da chave, ainda sem os parâmetros substituídos
    var (template, found) = GetTemplate(listInfo[0], culture);

    // Chave inexistente devolve o texto recebido inteiro, como faz o localizador do próprio framework:
    // truncar no ponto e vírgula descartaria parte de uma mensagem que não seja uma chave do Tooark
    if (!found)
    {
      return (keyParameter, false);
    }

    // Substitui a chave pelo texto traduzido
    listInfo[0] = template;

    // Traduz os parâmetros que também são chaves
    ReplaceParametersInList(listInfo, culture);

    // Retorna o texto com os parâmetros substituídos
    return (ReplaceParameters(listInfo), true);
  }

  /// <summary>
  /// Obtém todas as strings localizadas.
  /// </summary>
  /// <param name="includeParentCultures">Utilizado para avaliar se é a cultura padrão (True) ou a cultura atual (False).</param>
  /// <returns>Strings localizadas.</returns>
  internal IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
  {
    // Obtém o código de idioma atual
    string culture = includeParentCultures ? Language.Default : Language.Current;

    // Retorna todas as strings localizadas do idioma
    foreach (var translation in GetTranslations(culture))
    {
      // Retorna a string localizada
      yield return new LocalizedString(translation.Key, translation.Value, false);
    }
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Obtém o texto traduzido da chave, sem substituir os parâmetros.
  /// </summary>
  /// <param name="key">Chave da string localizada.</param>
  /// <param name="culture">Código de idioma.</param>
  /// <returns>O texto traduzido e se a chave foi encontrada.</returns>
  private static (string Template, bool Found) GetTemplate(string key, string culture)
  {
    // Busca o texto no idioma solicitado
    if (GetTranslations(culture).TryGetValue(key, out var value))
    {
      return (value, true);
    }

    // Sem tradução no idioma solicitado, tenta o idioma padrão da aplicação
    if (culture != Language.Default && GetTranslations(Language.Default).TryGetValue(key, out var fallback))
    {
      return (fallback, true);
    }

    // Chave inexistente devolve a própria chave
    return (key, false);
  }

  /// <summary>
  /// Traduz os parâmetros que também correspondem a uma chave.
  /// </summary>
  /// <param name="listInfo">Chave, já traduzida, seguida dos parâmetros.</param>
  /// <param name="culture">Código de idioma.</param>
  private static void ReplaceParametersInList(string[] listInfo, string culture)
  {
    // Itera sobre os parâmetros da string localizada
    for (int i = 1; i < listInfo.Length; i++)
    {
      // Busca o parâmetro como se fosse uma chave
      var (translated, found) = GetTemplate(listInfo[i], culture);

      // Substitui o parâmetro apenas quando ele corresponde a uma chave existente
      if (found)
      {
        listInfo[i] = translated;
      }
    }
  }

  /// <summary>
  /// Obtém as traduções do idioma, carregando os arquivos apenas na primeira vez.
  /// </summary>
  /// <param name="culture">Código de idioma.</param>
  /// <returns>Traduções do idioma, indexadas por chave.</returns>
  private static IReadOnlyDictionary<string, string> GetTranslations(string culture)
  {
    // Reaproveita as traduções já carregadas, ou carrega os arquivos do idioma
    return Translations.GetOrAdd(culture, LoadTranslations);
  }

  /// <summary>
  /// Carrega as traduções dos arquivos JSON do idioma.
  /// </summary>
  /// <remarks>
  /// O arquivo <c>{idioma}.json</c> do consumidor tem prioridade sobre o <c>{idioma}.default.json</c>
  /// distribuído com o pacote, chave a chave.
  /// </remarks>
  /// <param name="culture">Código de idioma.</param>
  /// <returns>Traduções do idioma, indexadas por chave.</returns>
  private static IReadOnlyDictionary<string, string> LoadTranslations(string culture)
  {
    // O arquivo do consumidor é lido por último, sobrescrevendo o que veio do pacote
    var translations = new Dictionary<string, string>(StringComparer.Ordinal);

    ReadJsonFile(GetFilePath(culture, true), translations);
    ReadJsonFile(GetFilePath(culture, false), translations);

    return translations;
  }

  /// <summary>
  /// Obtém o caminho do arquivo JSON do idioma.
  /// </summary>
  /// <param name="culture">Código de idioma.</param>
  /// <param name="defaultFile">Indica se é o arquivo JSON padrão.</param>
  /// <returns>Caminho do arquivo JSON.</returns>
  private static string GetFilePath(string culture, bool defaultFile = true)
  {
    // Define o complemento do arquivo JSON
    string complement = defaultFile ? ".default" : "";

    // Caminho relativo para o arquivo JSON
    string relativeFilePath = Path.Combine("Resources", $"{culture}{complement}.json");

    // Retorna o caminho completo do arquivo JSON
    return Path.Combine(AppContext.BaseDirectory, relativeFilePath);
  }

  /// <summary>
  /// Lê o arquivo JSON e acrescenta as traduções ao dicionário.
  /// </summary>
  /// <param name="filePath">Caminho do arquivo JSON.</param>
  /// <param name="translations">Dicionário que recebe as traduções.</param>
  private static void ReadJsonFile(string filePath, Dictionary<string, string> translations)
  {
    // Verifica se o arquivo existe
    if (!File.Exists(filePath))
    {
      // Sem arquivo não há o que acrescentar
      return;
    }

    try
    {
      // Lê e interpreta o arquivo JSON
      using var document = JsonDocument.Parse(File.ReadAllText(filePath));

      // Indexa cada tradução pela chave
      foreach (var property in document.RootElement.EnumerateObject())
      {
        // Apenas valores de texto são traduções
        if (property.Value.ValueKind == JsonValueKind.String)
        {
          translations[property.Name] = property.Value.GetString()!;
        }
      }
    }
    catch (JsonException)
    {
      // Arquivo de tradução malformado não pode derrubar a aplicação: o idioma fica sem essas traduções
    }
  }

  /// <summary>
  /// Substitui os parâmetros da string localizada.
  /// </summary>
  /// <param name="keyAndParameters">Texto traduzido seguido dos parâmetros.</param>
  /// <returns>String localizada com os parâmetros substituídos.</returns>
  private static string ReplaceParameters(string[] keyAndParameters)
  {
    // Se houver apenas um elemento retorna sem formatação
    if (keyAndParameters.Length == 1)
    {
      // Retorna a string localizada sem formatação
      return keyAndParameters[0];
    }

    try
    {
      // Retorna a string localizada com os parâmetros substituídos
      return string.Format(keyAndParameters[0], keyAndParameters[1..]);
    }
    catch (FormatException)
    {
      // Se houver um erro de formatação, retorna a string original sem formatação
      return string.Join(";", keyAndParameters);
    }
  }

  #endregion
}
