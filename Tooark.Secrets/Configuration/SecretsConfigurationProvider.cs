using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Tooark.Exceptions;
using Tooark.Secrets.Options;

namespace Tooark.Secrets.Configuration;

/// <summary>
/// Classe base das fontes de configuração que leem segredos e parâmetros de um cofre.
/// </summary>
/// <remarks>
/// O provedor implementa só a leitura das entradas. A base as transforma em chaves de configuração: <c>/</c> e
/// <c>__</c> no nome viram <c>:</c>, e com <see cref="SecretsOptions.ExpandJson"/> um valor que é objeto JSON vira
/// chaves filhas. Assim, os pacotes que já leem as opções do <c>IConfiguration</c> (Storage, Securities,
/// Observability) recebem os valores do cofre sem mudar nada.
/// <para>
/// A leitura acontece uma vez, quando a configuração é montada. Rotacionar um segredo pede reinício da aplicação.
/// </para>
/// <para>
/// A base implementa o <see cref="IConfigurationProvider"/> direto, sem o pacote <c>Microsoft.Extensions.Configuration</c>:
/// ele traria a versão da linha do runtime em conflito com as dependências do .NET 8 que já usam a linha 10.
/// </para>
/// </remarks>
/// <param name="options">As opções dos segredos.</param>
/// <param name="source">O nome do cofre, usado na mensagem de erro, como <c>AWS</c>.</param>
public abstract class SecretsConfigurationProvider(SecretsOptions options, string source) : IConfigurationProvider
{
  #region Private Fields

  /// <summary>
  /// As chaves carregadas.
  /// </summary>
  private Dictionary<string, string?> _data = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Token de recarga que nunca dispara: a leitura acontece só no startup.
  /// </summary>
  private readonly IChangeToken _reloadToken = new CancellationChangeToken(CancellationToken.None);

  #endregion

  #region Methods

  /// <summary>
  /// Carrega as entradas do cofre para a configuração.
  /// </summary>
  /// <exception cref="InternalServerErrorException">
  /// Quando o cofre falha ou não responde no tempo limite, e a fonte não é opcional.
  /// </exception>
  public void Load()
  {
    IReadOnlyList<SecretEntry> entries;

    try
    {
      using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));

      // Load é síncrono; a leitura roda fora do contexto de quem chama para não travar num SynchronizationContext
      entries = Task.Run(() => LoadEntriesAsync(timeout.Token), timeout.Token).GetAwaiter().GetResult();
    }
    catch (Exception e)
    {
      if (options.Optional)
      {
        _data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        return;
      }

      throw new InternalServerErrorException($"Secrets.LoadFailed;{source}", e);
    }

    var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    // Ordem estável: com dois nomes que dão a mesma chave, vence sempre o mesmo
    foreach (var entry in entries.OrderBy(entry => entry.Name, StringComparer.Ordinal))
    {
      Add(data, ToKey(entry.Name), entry.Value);
    }

    _data = data;
  }

  /// <inheritdoc/>
  public bool TryGet(string key, out string? value) => _data.TryGetValue(key, out value);

  /// <inheritdoc/>
  public void Set(string key, string? value) => _data[key] = value;

  /// <inheritdoc/>
  public IChangeToken GetReloadToken() => _reloadToken;

  /// <inheritdoc/>
  public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
  {
    var prefix = parentPath is null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;
    var children = new List<string>();

    foreach (var key in _data.Keys)
    {
      if (key.Length > prefix.Length && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
      {
        var end = key.IndexOf(ConfigurationPath.KeyDelimiter, prefix.Length, StringComparison.Ordinal);
        children.Add(end < 0 ? key[prefix.Length..] : key[prefix.Length..end]);
      }
    }

    children.AddRange(earlierKeys);

    // A mesma ordem do .NET: índices numéricos em ordem numérica, para listas saírem na ordem certa
    children.Sort(CompareKeys);

    return children;
  }

  #endregion

  #region Protected Methods

  /// <summary>
  /// Lê do cofre as entradas que viram configuração.
  /// </summary>
  /// <param name="cancellationToken">Token cancelado no tempo limite das opções.</param>
  /// <returns>As entradas, com o nome relativo ao prefixo configurado.</returns>
  protected abstract Task<IReadOnlyList<SecretEntry>> LoadEntriesAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Calcula o nome relativo ao prefixo, respeitando o limite de um nível.
  /// </summary>
  /// <remarks>
  /// O prefixo <c>arkuest/prod</c> aceita <c>arkuest/prod</c> (a raiz), <c>arkuest/prod/Jwt</c> e
  /// <c>arkuest/prod__Jwt</c>, mas não <c>arkuest/production</c>.
  /// </remarks>
  /// <param name="name">O nome completo no cofre.</param>
  /// <param name="prefix">O prefixo configurado, com ou sem separador no fim.</param>
  /// <returns>O nome relativo, vazio para a raiz, ou nulo quando o nome está fora do prefixo.</returns>
  protected static string? RelativeName(string name, string prefix)
  {
    var normalized = prefix.TrimEnd('/');

    if (normalized.EndsWith("__", StringComparison.Ordinal))
    {
      normalized = normalized[..^2];
    }

    if (normalized.Length == 0)
    {
      return name.TrimStart('/');
    }

    if (name == normalized)
    {
      return string.Empty;
    }

    foreach (var separator in new[] { "/", "__" })
    {
      if (name.StartsWith(normalized + separator, StringComparison.Ordinal))
      {
        return name[(normalized.Length + separator.Length)..];
      }
    }

    return null;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Transforma um nome do cofre em chave de configuração.
  /// </summary>
  /// <param name="name">O nome, com <c>/</c>, <c>__</c> ou <c>:</c> como separador.</param>
  /// <returns>A chave, com <c>:</c> entre os níveis e sem níveis vazios.</returns>
  private static string ToKey(string name) =>
    string.Join(
      ConfigurationPath.KeyDelimiter,
      name.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal)
        .Replace("/", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal)
        .Split(ConfigurationPath.KeyDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    );

  /// <summary>
  /// Acrescenta uma entrada à configuração, expandindo o objeto JSON quando as opções pedem.
  /// </summary>
  /// <param name="data">A configuração em montagem.</param>
  /// <param name="key">A chave da entrada.</param>
  /// <param name="value">O valor da entrada.</param>
  private void Add(Dictionary<string, string?> data, string key, string? value)
  {
    if (options.ExpandJson && TryParseObject(value, out var document))
    {
      using (document)
      {
        Flatten(data, key, document.RootElement);
      }

      return;
    }

    // Um valor na raiz, sem objeto para expandir, não tem chave onde ficar
    if (key.Length > 0)
    {
      data[key] = value;
    }
  }

  /// <summary>
  /// Tenta ler o valor como um objeto JSON.
  /// </summary>
  /// <param name="value">O valor lido do cofre.</param>
  /// <param name="document">O documento, quando o valor é um objeto.</param>
  /// <returns>Verdadeiro quando o valor é um objeto JSON.</returns>
  private static bool TryParseObject(string? value, out JsonDocument document)
  {
    document = null!;

    if (value is null || !value.TrimStart().StartsWith('{'))
    {
      return false;
    }

    try
    {
      document = JsonDocument.Parse(value);

      if (document.RootElement.ValueKind == JsonValueKind.Object)
      {
        return true;
      }

      document.Dispose();
      return false;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  /// <summary>
  /// Transforma um elemento JSON em chaves de configuração.
  /// </summary>
  /// <remarks>
  /// Objetos viram níveis, listas viram índices, e texto fica como está, sem ser lido de novo como JSON.
  /// </remarks>
  /// <param name="data">A configuração em montagem.</param>
  /// <param name="key">A chave do elemento.</param>
  /// <param name="element">O elemento.</param>
  private static void Flatten(Dictionary<string, string?> data, string key, JsonElement element)
  {
    switch (element.ValueKind)
    {
      case JsonValueKind.Object:
        foreach (var property in element.EnumerateObject())
        {
          Flatten(data, Combine(key, ToKey(property.Name)), property.Value);
        }
        break;

      case JsonValueKind.Array:
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
          Flatten(data, Combine(key, index++.ToString(CultureInfo.InvariantCulture)), item);
        }
        break;

      default:
        if (key.Length > 0)
        {
          data[key] = element.ValueKind switch
          {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null => null,
            _ => element.GetRawText()
          };
        }
        break;
    }
  }

  /// <summary>
  /// Junta dois níveis de chave.
  /// </summary>
  /// <param name="parent">O nível de cima, vazio na raiz.</param>
  /// <param name="child">O nível de baixo.</param>
  /// <returns>A chave combinada.</returns>
  private static string Combine(string parent, string child) =>
    parent.Length == 0 ? child : ConfigurationPath.Combine(parent, child);

  /// <summary>
  /// Compara chaves de configuração como o .NET: nível a nível, números pelo valor e antes de texto, texto sem
  /// diferenciar caixa.
  /// </summary>
  /// <param name="left">A primeira chave.</param>
  /// <param name="right">A segunda chave.</param>
  /// <returns>Negativo, zero ou positivo, como em <see cref="IComparer{T}.Compare"/>.</returns>
  private static int CompareKeys(string left, string right)
  {
    var lefts = left.Split(ConfigurationPath.KeyDelimiter);
    var rights = right.Split(ConfigurationPath.KeyDelimiter);

    for (var i = 0; i < Math.Min(lefts.Length, rights.Length); i++)
    {
      var leftIsNumber = int.TryParse(lefts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var leftNumber);
      var rightIsNumber = int.TryParse(rights[i], NumberStyles.None, CultureInfo.InvariantCulture, out var rightNumber);

      var result = (leftIsNumber, rightIsNumber) switch
      {
        (true, true) => leftNumber.CompareTo(rightNumber),
        (true, false) => -1,
        (false, true) => 1,
        _ => string.Compare(lefts[i], rights[i], StringComparison.OrdinalIgnoreCase)
      };

      if (result != 0)
      {
        return result;
      }
    }

    return lefts.Length.CompareTo(rights.Length);
  }

  #endregion
}
