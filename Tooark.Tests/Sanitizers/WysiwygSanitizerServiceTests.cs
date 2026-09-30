using System.Text.Json.Nodes;
using Tooark.Sanitizers;
using Tooark.Sanitizers.Options;

namespace Tooark.Tests.Sanitizers;

/// <summary>
/// Testes do sanitizador do conteúdo do @tooark/wysiwyg (JSON do Tiptap).
/// </summary>
public class WysiwygSanitizerServiceTests
{
  // Cria o serviço com as opções do componente informadas
  private static WysiwygSanitizerService Criar(WysiwygOptions? wysiwyg = null) =>
    new(Microsoft.Extensions.Options.Options.Create(new SanitizerOptions { Wysiwyg = wysiwyg ?? new WysiwygOptions() }));

  // Monta um documento com os blocos informados
  private static JsonObject Documento(params JsonNode[] blocos) =>
    new() { ["type"] = "doc", ["content"] = new JsonArray(blocos) };

  // Monta um parágrafo com um texto e as marcas informadas
  private static JsonObject Paragrafo(string texto, params JsonNode[] marcas)
  {
    var noTexto = new JsonObject { ["type"] = "text", ["text"] = texto };

    if (marcas.Length > 0)
    {
      noTexto["marks"] = new JsonArray(marcas);
    }

    return new JsonObject { ["type"] = "paragraph", ["content"] = new JsonArray(noTexto) };
  }

  // Sanitiza e devolve o primeiro bloco do documento
  private static JsonObject? PrimeiroBloco(JsonObject documento, WysiwygOptions? opcoes = null) =>
    Criar(opcoes).Sanitize(documento)!["content"]![0] as JsonObject;

  // Sanitiza um parágrafo com uma marca e devolve as marcas do texto
  private static JsonArray? Marcas(JsonNode marca) =>
    PrimeiroBloco(Documento(Paragrafo("texto", marca)))!["content"]![0]!["marks"] as JsonArray;

  // Testa se um documento com todo o schema do componente passa sem alteração
  [Fact]
  public void Sanitize_ShouldKeepDocumentWithTheWholeSchema()
  {
    // Arrange
    const string json = """
      {"type":"doc","content":[
        {"type":"heading","attrs":{"level":2,"textAlign":"center"},"content":[{"type":"text","text":"Título"}]},
        {"type":"paragraph","attrs":{"textAlign":"justify"},"content":[
          {"type":"text","marks":[{"type":"bold"},{"type":"italic"},{"type":"underline"},{"type":"strike"}],"text":"formatado"},
          {"type":"hardBreak"},
          {"type":"text","marks":[{"type":"link","attrs":{"href":"https://tooark.com","target":"_blank","rel":"noopener noreferrer nofollow"}}],"text":"link"},
          {"type":"text","marks":[{"type":"textStyle","attrs":{"color":"#ff0000"}},{"type":"highlight","attrs":{"color":"rgb(255, 240, 0)"}}],"text":"cor"},
          {"type":"text","marks":[{"type":"code"}],"text":"código"}
        ]},
        {"type":"bulletList","content":[{"type":"listItem","content":[{"type":"paragraph","content":[{"type":"text","text":"item"}]}]}]},
        {"type":"orderedList","attrs":{"start":3,"type":"a"},"content":[{"type":"listItem","content":[{"type":"paragraph","content":[{"type":"text","text":"item"}]}]}]},
        {"type":"blockquote","content":[{"type":"paragraph","content":[{"type":"text","text":"citação"}]}]},
        {"type":"codeBlock","attrs":{"language":"csharp"},"content":[{"type":"text","text":"var x = 1;"}]},
        {"type":"horizontalRule"},
        {"type":"image","attrs":{"src":"https://cdn.tooark.com/foto.png","alt":"Foto","title":"Legenda","width":320}},
        {"type":"video","attrs":{"src":"/midias/video.mp4","poster":"/midias/capa.png","title":"Vídeo"}}
      ]}
      """;
    var entrada = JsonNode.Parse(json);

    // Act
    var resultado = Criar().Sanitize(entrada);

    // Assert
    Assert.True(JsonNode.DeepEquals(entrada, resultado), resultado?.ToJsonString());
  }

  // Testa se nós fora do schema caem com os filhos
  [Theory]
  [InlineData("script")]
  [InlineData("iframe")]
  [InlineData("table")]
  [InlineData("html")]
  [InlineData("")]
  public void Sanitize_ShouldDropUnknownNodes(string tipo)
  {
    // Arrange
    var desconhecido = new JsonObject { ["type"] = tipo, ["content"] = new JsonArray(Paragrafo("filho")) };

    // Act
    var resultado = Criar().Sanitize(Documento(desconhecido, Paragrafo("fica")));

    // Assert
    Assert.Single(resultado!["content"]!.AsArray());
    Assert.Equal("fica", resultado["content"]![0]!["content"]![0]!["text"]!.GetValue<string>());
  }

  // Testa se marcas fora do schema caem e o texto fica
  [Fact]
  public void Sanitize_ShouldDropUnknownMarks()
  {
    // Arrange
    var marca = new JsonObject { ["type"] = "script" };

    // Act
    var marcas = Marcas(marca);

    // Assert
    Assert.Null(marcas);
  }

  // Testa se o link com esquema inseguro sai da marca, mantendo o texto
  [Theory]
  [InlineData("javascript:alert(1)")]
  [InlineData("JAVASCRIPT:alert(1)")]
  [InlineData("java\tscript:alert(1)")]
  [InlineData(" javascript:alert(1)")]
  [InlineData("data:text/html,<script>alert(1)</script>")]
  [InlineData("vbscript:msgbox(1)")]
  [InlineData("blob:https://tooark.com/uuid")]
  [InlineData("uploads/arquivo.pdf")]
  [InlineData("")]
  public void Sanitize_ShouldDropLink_WhenHrefIsUnsafe(string href)
  {
    // Arrange
    var link = new JsonObject { ["type"] = "link", ["attrs"] = new JsonObject { ["href"] = href } };

    // Act
    var bloco = PrimeiroBloco(Documento(Paragrafo("texto", link)));

    // Assert
    Assert.Null(bloco!["content"]![0]!["marks"]);
    Assert.Equal("texto", bloco["content"]![0]!["text"]!.GetValue<string>());
  }

  // Testa se o link aceito pelo componente fica, sem caracteres de controle
  [Theory]
  [InlineData("https://tooark.com", "https://tooark.com")]
  [InlineData("mailto:contato@tooark.com", "mailto:contato@tooark.com")]
  [InlineData("tel:+5511999999999", "tel:+5511999999999")]
  [InlineData("/pagina", "/pagina")]
  [InlineData("#secao", "#secao")]
  [InlineData("../acima", "../acima")]
  [InlineData("https://too\nark.com", "https://tooark.com")]
  public void Sanitize_ShouldKeepLink_WhenHrefIsAccepted(string href, string esperado)
  {
    // Arrange
    var link = new JsonObject { ["type"] = "link", ["attrs"] = new JsonObject { ["href"] = href } };

    // Act
    var marcas = Marcas(link);

    // Assert
    Assert.Equal(esperado, marcas![0]!["attrs"]!["href"]!.GetValue<string>());
  }

  // Testa se atributos do link fora da regra caem e o href fica
  [Fact]
  public void Sanitize_ShouldDropInvalidLinkAttributes()
  {
    // Arrange
    var link = new JsonObject
    {
      ["type"] = "link",
      ["attrs"] = new JsonObject
      {
        ["href"] = "https://tooark.com",
        ["target"] = "javascript:alert(1)",
        ["rel"] = "\" onclick=\"alert(1)",
        ["class"] = "classe",
        ["onclick"] = "alert(1)"
      }
    };

    // Act
    var atributos = Marcas(link)![0]!["attrs"]!.AsObject();

    // Assert
    Assert.Equal(["href"], atributos.Select(par => par.Key));
  }

  // Testa se imagem e vídeo com src inseguro caem inteiros
  [Theory]
  [InlineData("image", "data:image/png;base64,iVBORw0KGgo=")]
  [InlineData("image", "javascript:alert(1)")]
  [InlineData("image", "blob:https://tooark.com/uuid")]
  [InlineData("image", "foto.png")]
  [InlineData("video", "data:video/mp4;base64,AAAA")]
  [InlineData("video", "vbscript:msgbox(1)")]
  public void Sanitize_ShouldDropMedia_WhenSrcIsUnsafe(string tipo, string src)
  {
    // Arrange
    var midia = new JsonObject { ["type"] = tipo, ["attrs"] = new JsonObject { ["src"] = src } };

    // Act
    var resultado = Criar().Sanitize(Documento(midia, Paragrafo("fica")));

    // Assert
    Assert.Single(resultado!["content"]!.AsArray());
  }

  // Testa se a mídia sem src cai
  [Fact]
  public void Sanitize_ShouldDropMedia_WhenSrcIsMissing()
  {
    // Arrange
    var imagem = new JsonObject { ["type"] = "image", ["attrs"] = new JsonObject { ["alt"] = "sem src" } };

    // Act
    var resultado = Criar().Sanitize(Documento(imagem, Paragrafo("fica")));

    // Assert
    Assert.Single(resultado!["content"]!.AsArray());
  }

  // Testa se o poster inseguro sai e o vídeo fica
  [Fact]
  public void Sanitize_ShouldDropUnsafePoster_AndKeepVideo()
  {
    // Arrange
    var video = new JsonObject
    {
      ["type"] = "video",
      ["attrs"] = new JsonObject { ["src"] = "https://cdn.tooark.com/video.mp4", ["poster"] = "javascript:alert(1)" }
    };

    // Act
    var bloco = PrimeiroBloco(Documento(video));

    // Assert
    Assert.Equal("video", bloco!["type"]!.GetValue<string>());
    Assert.Equal(["src"], bloco["attrs"]!.AsObject().Select(par => par.Key));
  }

  // Testa se a mídia desligada na configuração cai mesmo com URL válida
  [Theory]
  [InlineData("image")]
  [InlineData("video")]
  public void Sanitize_ShouldDropMedia_WhenDisabledByOptions(string tipo)
  {
    // Arrange
    var midia = new JsonObject { ["type"] = tipo, ["attrs"] = new JsonObject { ["src"] = "https://cdn.tooark.com/a" } };
    var opcoes = new WysiwygOptions { AllowImages = false, AllowVideos = false };

    // Act
    var resultado = Criar(opcoes).Sanitize(Documento(midia, Paragrafo("fica")));

    // Assert
    Assert.Single(resultado!["content"]!.AsArray());
  }

  // Testa se as cores seguem a regra do componente
  [Theory]
  [InlineData("#f00", true)]
  [InlineData("#FF000080", true)]
  [InlineData("red", true)]
  [InlineData("rebeccapurple", true)]
  [InlineData("rgb(1, 2, 3)", true)]
  [InlineData("hsla(120, 50%, 50%, 0.5)", true)]
  [InlineData("red; background: url(https://golpe.com)", false)]
  [InlineData("expression(alert(1))", false)]
  [InlineData("url(javascript:alert(1))", false)]
  [InlineData("var(--cor)", false)]
  [InlineData("oklch(70% 0.1 200)", false)]
  public void Sanitize_ShouldValidateColors(string cor, bool aceita)
  {
    // Arrange
    var estilo = new JsonObject { ["type"] = "textStyle", ["attrs"] = new JsonObject { ["color"] = cor } };

    // Act
    var marca = Marcas(estilo)![0]!;

    // Assert
    Assert.Equal(aceita, marca["attrs"]?["color"] is not null);
  }

  // Testa se o alinhamento fora dos quatro previstos cai
  [Theory]
  [InlineData("left", true)]
  [InlineData("justify", true)]
  [InlineData("start", false)]
  [InlineData("center; color: red", false)]
  public void Sanitize_ShouldValidateTextAlign(string alinhamento, bool aceito)
  {
    // Arrange
    var paragrafo = Paragrafo("texto");
    paragrafo["attrs"] = new JsonObject { ["textAlign"] = alinhamento };

    // Act
    var bloco = PrimeiroBloco(Documento(paragrafo));

    // Assert
    Assert.Equal(aceito, bloco!["attrs"]?["textAlign"] is not null);
  }

  // Testa se o nível do título fica entre 1 e 4, como no componente
  [Theory]
  [InlineData("7", 4)]
  [InlineData("0", 1)]
  [InlineData("-3", 1)]
  [InlineData("2.5", 1)]
  [InlineData("\"3\"", 3)]
  [InlineData("\"x\"", 1)]
  [InlineData("true", 1)]
  [InlineData("null", 1)]
  public void Sanitize_ShouldClampHeadingLevel(string nivel, int esperado)
  {
    // Arrange
    var json = $$"""{"type":"doc","content":[{"type":"heading","attrs":{"level":{{nivel}}},"content":[{"type":"text","text":"T"}]}]}""";

    // Act
    var bloco = PrimeiroBloco(JsonNode.Parse(json)!.AsObject());

    // Assert
    Assert.Equal(esperado, bloco!["attrs"]!["level"]!.GetValue<int>());
  }

  // Testa se o título sem nível recebe o nível 1
  [Fact]
  public void Sanitize_ShouldSetHeadingLevel_WhenMissing()
  {
    // Arrange
    var titulo = new JsonObject { ["type"] = "heading", ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = "T" }) };

    // Act
    var bloco = PrimeiroBloco(Documento(titulo));

    // Assert
    Assert.Equal(1, bloco!["attrs"]!["level"]!.GetValue<int>());
  }

  // Testa se chaves e atributos desconhecidos não são copiados
  [Fact]
  public void Sanitize_ShouldDropUnknownKeysAndAttributes()
  {
    // Arrange
    const string json = """
      {"type":"doc","html":"<script>alert(1)</script>","content":[
        {"type":"paragraph","attrs":{"onclick":"alert(1)","style":"color:red","textAlign":"left"},"extra":1,
         "content":[{"type":"text","text":"texto","innerHTML":"<img src=x onerror=alert(1)>"}]}
      ]}
      """;

    // Act
    var resultado = Criar().Sanitize(JsonNode.Parse(json));

    // Assert
    const string esperado = """{"type":"doc","content":[{"type":"paragraph","attrs":{"textAlign":"left"},"content":[{"type":"text","text":"texto"}]}]}""";
    Assert.True(JsonNode.DeepEquals(JsonNode.Parse(esperado), resultado), resultado?.ToJsonString());
  }

  // Testa se o nó de texto vazio ou sem texto cai, porque o ProseMirror o recusa
  [Theory]
  [InlineData("{\"type\":\"text\",\"text\":\"\"}")]
  [InlineData("{\"type\":\"text\"}")]
  [InlineData("{\"type\":\"text\",\"text\":123}")]
  public void Sanitize_ShouldDropInvalidTextNodes(string texto)
  {
    // Arrange
    var json = $$"""{"type":"doc","content":[{"type":"paragraph","content":[{{texto}},{"type":"text","text":"fica"}]}]}""";

    // Act
    var bloco = PrimeiroBloco(JsonNode.Parse(json)!.AsObject());

    // Assert
    Assert.Single(bloco!["content"]!.AsArray());
  }

  // Testa se o documento que perde todos os blocos vira o documento vazio do componente
  [Fact]
  public void Sanitize_ShouldReturnEmptyDocument_WhenAllBlocksAreDropped()
  {
    // Arrange
    var documento = Documento(new JsonObject { ["type"] = "script" });

    // Act
    var resultado = Criar().Sanitize(documento);

    // Assert
    Assert.Equal("""{"type":"doc","content":[{"type":"paragraph"}]}""", resultado!.ToJsonString());
  }

  // Testa se entrada que não é documento devolve nulo
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("não é json")]
  [InlineData("{\"type\":\"doc\"")]
  [InlineData("[]")]
  [InlineData("\"doc\"")]
  [InlineData("{\"type\":\"paragraph\"}")]
  [InlineData("{\"content\":[]}")]
  public void Sanitize_ShouldReturnNull_WhenInputIsNotADocument(string? json)
  {
    // Arrange
    var sanitizador = Criar();

    // Act
    var resultado = sanitizador.Sanitize(json);

    // Assert
    Assert.Null(resultado);
  }

  // Testa se o JSON com chave repetida é recusado em vez de lançar
  [Fact]
  public void Sanitize_ShouldReturnNull_WhenJsonHasDuplicateKeys()
  {
    // Arrange
    const string json = """{"type":"doc","type":"doc","content":[]}""";

    // Act
    var resultado = Criar().Sanitize(json);

    // Assert
    Assert.Null(resultado);
  }

  // Testa se o texto JSON devolvido mantém acentos e escapa os caracteres sensíveis em HTML
  [Fact]
  public void Sanitize_ShouldSerializeWithReadableAccentsAndEscapedHtml()
  {
    // Arrange
    const string json = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"ação </script><b>"}]}]}""";

    // Act
    var resultado = Criar().Sanitize(json);

    // Assert
    Assert.NotNull(resultado);
    Assert.Contains("ação", resultado);
    Assert.DoesNotContain("<", resultado);
    Assert.DoesNotContain(">", resultado);
    Assert.Equal("ação </script><b>", JsonNode.Parse(resultado)!["content"]![0]!["content"]![0]!["text"]!.GetValue<string>());
  }

  // Testa se aninhamento profundo demais é cortado sem estourar a pilha
  [Fact]
  public void Sanitize_ShouldCutDeepNesting()
  {
    // Arrange
    JsonObject atual = Paragrafo("fundo");

    for (int i = 0; i < 500; i++)
    {
      atual = new JsonObject { ["type"] = "blockquote", ["content"] = new JsonArray(atual) };
    }

    // Act
    var resultado = Criar().Sanitize(Documento(atual));

    // Assert
    Assert.NotNull(resultado);
    Assert.DoesNotContain("fundo", resultado.ToJsonString());
  }

  // Testa se o documento devolvido é novo e a entrada não muda
  [Fact]
  public void Sanitize_ShouldNotChangeInput()
  {
    // Arrange
    var link = new JsonObject { ["type"] = "link", ["attrs"] = new JsonObject { ["href"] = "javascript:alert(1)" } };
    var entrada = Documento(Paragrafo("texto", link));
    var antes = entrada.ToJsonString();

    // Act
    var resultado = Criar().Sanitize(entrada);
    resultado!["content"]!.AsArray().Add(Paragrafo("novo"));

    // Assert
    Assert.Equal(antes, entrada.ToJsonString());
  }
}
