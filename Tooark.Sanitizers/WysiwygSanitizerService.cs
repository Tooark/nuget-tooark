using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Microsoft.Extensions.Options;
using Tooark.Sanitizers.Interfaces;
using Tooark.Sanitizers.Options;
using Tooark.Sanitizers.Policies;

namespace Tooark.Sanitizers;

/// <summary>
/// Serviço que sanitiza o conteúdo do <c>@tooark/wysiwyg</c>: o JSON do Tiptap que o editor emite e o viewer exibe.
/// </summary>
/// <remarks>
/// O componente sanitiza o conteúdo no navegador, com <c>sanitizeWysiwygContent</c>. Este serviço aplica as mesmas
/// regras no servidor, onde o JSON pode chegar sem ter passado pelo editor:
/// <list type="bullet">
/// <item>nós e marcas fora do schema do componente caem, com os filhos;</item>
/// <item>links, imagens e vídeos só com URL em http(s), mailto, tel ou relativa (<c>/</c>, <c>#</c>, <c>?</c>,
/// <c>./</c> e <c>../</c>); o link sai da marca e a mídia sai do documento;</item>
/// <item>cores só em hex, nome CSS ou <c>rgb()</c>/<c>hsl()</c>; <c>textAlign</c> só nos quatro alinhamentos; o
/// <c>level</c> do título entre 1 e 4.</item>
/// </list>
/// O documento devolvido é novo e reconstruído só com o que a lista de permissão conhece: chaves e atributos
/// desconhecidos não são copiados. O schema segue o do <c>@tooark/wysiwyg</c> 1.4.
/// </remarks>
public sealed class WysiwygSanitizerService : IWysiwygSanitizerService
{
  #region Constants

  /// <summary>
  /// Profundidade máxima de nós. Um documento do editor fica bem abaixo disso; além dela, o nó cai.
  /// </summary>
  private const int MaxDepth = 64;

  /// <summary>
  /// Nós do schema do componente e os atributos que cada um aceita.
  /// </summary>
  private static readonly IReadOnlyDictionary<string, string[]> NodeAttributes = new Dictionary<string, string[]>(StringComparer.Ordinal)
  {
    ["doc"] = [],
    ["paragraph"] = ["textAlign"],
    ["heading"] = ["level", "textAlign"],
    ["text"] = [],
    ["blockquote"] = [],
    ["bulletList"] = [],
    ["orderedList"] = ["start", "type"],
    ["listItem"] = [],
    ["codeBlock"] = ["language"],
    ["hardBreak"] = [],
    ["horizontalRule"] = [],
    ["image"] = ["src", "alt", "title", "width", "height"],
    ["video"] = ["src", "poster", "title"]
  };

  /// <summary>
  /// Marcas do schema do componente e os atributos que cada uma aceita.
  /// </summary>
  private static readonly IReadOnlyDictionary<string, string[]> MarkAttributes = new Dictionary<string, string[]>(StringComparer.Ordinal)
  {
    ["bold"] = [],
    ["italic"] = [],
    ["underline"] = [],
    ["strike"] = [],
    ["code"] = [],
    ["link"] = ["href", "target", "rel", "title"],
    ["textStyle"] = ["color", "backgroundColor"],
    ["highlight"] = ["color"]
  };

  /// <summary>
  /// Alinhamentos de parágrafo e título do componente.
  /// </summary>
  private static readonly IReadOnlySet<string> Alignments = new HashSet<string>(["left", "center", "right", "justify"], StringComparer.Ordinal);

  /// <summary>
  /// Destinos de link aceitos.
  /// </summary>
  private static readonly IReadOnlySet<string> LinkTargets = new HashSet<string>(["_blank", "_self", "_parent", "_top"], StringComparer.Ordinal);

  /// <summary>
  /// Tipos de numeração de lista ordenada aceitos.
  /// </summary>
  private static readonly IReadOnlySet<string> ListTypes = new HashSet<string>(["1", "a", "A", "i", "I"], StringComparer.Ordinal);

  /// <summary>
  /// Tempo máximo de cada expressão regular.
  /// </summary>
  private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

  /// <summary>
  /// Cor aceita: a mesma regra do <c>isSafeColor</c> do componente, sem <c>;</c> para não injetar outra declaração.
  /// </summary>
  private static readonly Regex Color = new(
    @"^(#[0-9a-f]{3,8}|[a-z]{3,24}|(rgb|rgba|hsl|hsla)\([0-9.,%\s/]{1,40}\))$",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
    RegexTimeout
  );

  /// <summary>
  /// Linguagem de bloco de código, que vira classe CSS na renderização.
  /// </summary>
  private static readonly Regex Language = new(@"^[a-z0-9_+#.-]{1,32}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);

  /// <summary>
  /// Valor de <c>rel</c> do link: palavras separadas por um espaço.
  /// </summary>
  private static readonly Regex Rel = new(@"^[a-z]{1,20}( [a-z]{1,20}){0,4}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);

  /// <summary>
  /// Largura ou altura de imagem em texto: número, em pixels ou em porcentagem.
  /// </summary>
  private static readonly Regex Dimension = new(@"^\d{1,5}(\.\d{1,2})?(px|%)?$", RegexOptions.CultureInvariant, RegexTimeout);

  /// <summary>
  /// Regra de URL do componente: http(s), mailto, tel e relativa com prefixo.
  /// </summary>
  private static readonly UrlPolicy Urls = new(["http", "https", "mailto", "tel"], allowRelative: true, allowCredentials: true);

  /// <summary>
  /// Serialização do resultado: mantém letras acentuadas legíveis e escapa os caracteres sensíveis em HTML
  /// (<c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c>, aspas), para o JSON continuar seguro se for embutido numa página.
  /// </summary>
  private static readonly JsonSerializerOptions Output = new()
  {
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
  };

  #endregion

  #region Private Fields

  /// <summary>
  /// Opções do sanitizador do componente.
  /// </summary>
  private readonly WysiwygOptions _options;

  #endregion

  #region Constructor

  /// <summary>
  /// Cria o serviço a partir das opções dos sanitizadores.
  /// </summary>
  /// <param name="options">As opções dos sanitizadores.</param>
  public WysiwygSanitizerService(IOptions<SanitizerOptions> options)
  {
    _options = options.Value.Wysiwyg;
  }

  #endregion

  #region Methods

  /// <inheritdoc/>
  public JsonObject? Sanitize(JsonNode? content)
  {
    try
    {
      // Só um documento é conteúdo do editor; um parágrafo solto na raiz não abre
      if (content is not JsonObject root || TypeOf(root) != "doc")
      {
        return null;
      }

      var document = SanitizeNode(root, 0)!;

      // Um documento sem blocos não abre no ProseMirror: vira o documento vazio do componente
      if (document["content"] is null)
      {
        document["content"] = new JsonArray(new JsonObject { ["type"] = "paragraph" });
      }

      return document;
    }
    catch (ArgumentException)
    {
      // Chave repetida em um objeto JSON só aparece quando ele é lido, e o documento não é confiável
      return null;
    }
  }

  /// <inheritdoc/>
  public string? Sanitize(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return null;
    }

    JsonNode? content;

    try
    {
      content = JsonNode.Parse(json);
    }
    catch (JsonException)
    {
      return null;
    }

    return Sanitize(content)?.ToJsonString(Output);
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Reconstrói um nó com o que a lista de permissão aceita.
  /// </summary>
  /// <param name="node">O nó de entrada.</param>
  /// <param name="depth">A profundidade do nó no documento.</param>
  /// <returns>O nó sanitizado, ou nulo quando ele cai.</returns>
  private JsonObject? SanitizeNode(JsonNode? node, int depth)
  {
    if (node is not JsonObject source || depth > MaxDepth)
    {
      return null;
    }

    var type = TypeOf(source);

    // Nó fora do schema cai com os filhos
    if (type is null || !NodeAttributes.TryGetValue(type, out var allowed))
    {
      return null;
    }

    // Mídia desligada na configuração cai mesmo com URL válida
    if ((type == "image" && !_options.AllowImages) || (type == "video" && !_options.AllowVideos))
    {
      return null;
    }

    var result = new JsonObject { ["type"] = type };

    if (type == "text")
    {
      // O ProseMirror recusa nó de texto vazio, e o documento inteiro deixaria de abrir
      if (StringOf(source["text"]) is not { Length: > 0 } text)
      {
        return null;
      }

      result["text"] = text;
    }

    // Imagem e vídeo sem URL aceita caem inteiros
    var attributes = SanitizeNodeAttributes(type, source["attrs"] as JsonObject, allowed);

    if (attributes is null)
    {
      return null;
    }

    if (attributes.Count > 0)
    {
      result["attrs"] = attributes;
    }

    if (source["marks"] is JsonArray marks)
    {
      var sanitized = marks.Select(SanitizeMark).OfType<JsonObject>().ToArray<JsonNode?>();

      if (sanitized.Length > 0)
      {
        result["marks"] = new JsonArray(sanitized);
      }
    }

    // Texto não tem filhos; nos demais, cada filho passa pela mesma regra
    if (type != "text" && source["content"] is JsonArray content)
    {
      var children = content.Select(child => SanitizeNode(child, depth + 1)).OfType<JsonObject>().ToArray<JsonNode?>();

      if (children.Length > 0)
      {
        result["content"] = new JsonArray(children);
      }
    }

    return result;
  }

  /// <summary>
  /// Reconstrói os atributos de um nó.
  /// </summary>
  /// <param name="type">O tipo do nó.</param>
  /// <param name="source">Os atributos de entrada.</param>
  /// <param name="allowed">Os atributos que o nó aceita.</param>
  /// <returns>Os atributos sanitizados, ou nulo quando o nó é mídia sem URL aceita.</returns>
  private static JsonObject? SanitizeNodeAttributes(string type, JsonObject? source, string[] allowed)
  {
    var result = new JsonObject();

    if (type is "image" or "video")
    {
      if (Urls.Clean(StringOf(source?["src"])) is not { } src)
      {
        return null;
      }

      result["src"] = src;
    }

    // O componente sempre define o nível do título, mesmo quando ele não vem
    if (type == "heading")
    {
      result["level"] = HeadingLevel(source?["level"]);
    }

    foreach (var name in allowed.Where(name => name is not ("src" or "level")))
    {
      var value = source?[name];

      JsonNode? sanitized = name switch
      {
        "textAlign" => StringOf(value) is { } align && Alignments.Contains(align) ? align : null,
        "poster" => Urls.Clean(StringOf(value)),
        "alt" or "title" => StringOf(value),
        "width" or "height" => SizeOf(value),
        "start" => value is JsonValue number && number.TryGetValue(out int start) ? start : null,
        "type" => StringOf(value) is { } listType && ListTypes.Contains(listType) ? listType : null,
        "language" => StringOf(value) is { } language && Language.IsMatch(language) ? language : null,
        _ => null
      };

      if (sanitized is not null)
      {
        result[name] = sanitized;
      }
    }

    return result;
  }

  /// <summary>
  /// Reconstrói uma marca com o que a lista de permissão aceita.
  /// </summary>
  /// <param name="mark">A marca de entrada.</param>
  /// <returns>A marca sanitizada, ou nula quando ela cai.</returns>
  private static JsonObject? SanitizeMark(JsonNode? mark)
  {
    if (mark is not JsonObject source)
    {
      return null;
    }

    var type = TypeOf(source);

    if (type is null || !MarkAttributes.TryGetValue(type, out var allowed))
    {
      return null;
    }

    var result = new JsonObject { ["type"] = type };
    var attributes = source["attrs"] as JsonObject;
    var sanitized = new JsonObject();

    // Link sem URL aceita cai; o texto continua, sem o link
    if (type == "link")
    {
      if (Urls.Clean(StringOf(attributes?["href"])) is not { } href)
      {
        return null;
      }

      sanitized["href"] = href;
    }

    foreach (var name in allowed.Where(name => name != "href"))
    {
      var value = StringOf(attributes?[name]);

      JsonNode? accepted = name switch
      {
        "target" => value is not null && LinkTargets.Contains(value) ? value : null,
        "rel" => value is not null && Rel.IsMatch(value) ? value : null,
        "title" => value,
        "color" or "backgroundColor" => value is not null && Color.IsMatch(value.Trim()) ? value.Trim() : null,
        _ => null
      };

      if (accepted is not null)
      {
        sanitized[name] = accepted;
      }
    }

    if (sanitized.Count > 0)
    {
      result["attrs"] = sanitized;
    }

    return result;
  }

  /// <summary>
  /// Resolve o nível do título como o componente: inteiro limitado entre 1 e 4, e 1 para o que não é inteiro.
  /// </summary>
  /// <param name="value">O valor de entrada, número ou texto numérico.</param>
  /// <returns>O nível do título.</returns>
  private static int HeadingLevel(JsonNode? value)
  {
    double? number = value switch
    {
      JsonValue json when json.TryGetValue(out double parsed) => parsed,
      JsonValue json when json.TryGetValue(out string? text) &&
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
      _ => null
    };

    return number is double level && double.IsFinite(level) && level == Math.Floor(level)
      ? (int)Math.Clamp(level, 1, 4)
      : 1;
  }

  /// <summary>
  /// Resolve a largura ou a altura de uma imagem.
  /// </summary>
  /// <param name="value">O valor de entrada.</param>
  /// <returns>O número positivo, o texto em pixels ou porcentagem, ou nulo quando o valor não é aceito.</returns>
  private static JsonNode? SizeOf(JsonNode? value)
  {
    if (value is not JsonValue json)
    {
      return null;
    }

    if (json.TryGetValue(out double number))
    {
      return double.IsFinite(number) && number > 0 && number <= 100_000 ? JsonValue.Create(number) : null;
    }

    return json.TryGetValue(out string? text) && Dimension.IsMatch(text) ? JsonValue.Create(text) : null;
  }

  /// <summary>
  /// Lê o tipo de um nó ou de uma marca.
  /// </summary>
  /// <param name="source">O objeto de entrada.</param>
  /// <returns>O tipo, ou nulo quando ele não é texto.</returns>
  private static string? TypeOf(JsonObject source) => StringOf(source["type"]);

  /// <summary>
  /// Lê um valor de texto.
  /// </summary>
  /// <param name="value">O valor de entrada.</param>
  /// <returns>O texto, ou nulo quando o valor não é texto.</returns>
  private static string? StringOf(JsonNode? value) =>
    value is JsonValue json && json.TryGetValue(out string? text) ? text : null;

  #endregion
}
