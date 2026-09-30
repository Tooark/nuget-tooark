using Tooark.Exceptions;
using Tooark.Sanitizers;
using Tooark.Sanitizers.Options;

namespace Tooark.Tests.Sanitizers;

/// <summary>
/// Testes do sanitizador de HTML.
/// </summary>
public class HtmlSanitizerServiceTests
{
  // Trechos que não podem sobreviver à sanitização, em qualquer caixa
  private static readonly string[] Proibidos =
    ["<script", "javascript:", "vbscript:", "onerror", "onload", "onclick", "expression(", "data:text"];

  // Cria o serviço com as opções de HTML informadas
  private static HtmlSanitizerService Criar(HtmlOptions? html = null) =>
    new(Microsoft.Extensions.Options.Options.Create(new SanitizerOptions { Html = html ?? new HtmlOptions() }));

  // Testa se os vetores de XSS conhecidos saem sem nenhum trecho executável
  [Theory]
  [InlineData("<script>alert(1)</script><p>ok</p>")]
  [InlineData("<SCRIPT SRC=//evil.com/xss.js></SCRIPT>")]
  [InlineData("<scr<script>ipt>alert(1)</scr</script>ipt>")]
  [InlineData("<img src=x onerror=alert(1)>")]
  [InlineData("<IMG SRC=x OnErRoR=alert(1)>")]
  [InlineData("<body onload=alert(1)>")]
  [InlineData("<svg onload=alert(1)>")]
  [InlineData("<svg><script>alert(1)</script></svg>")]
  [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
  [InlineData("<a href=\" JaVaScRiPt:alert(1)\">x</a>")]
  [InlineData("<a href=\"jav&#x09;ascript:alert(1)\">x</a>")]
  [InlineData("<a href=\"&#106;avascript:alert(1)\">x</a>")]
  [InlineData("<a href=\"&#x6A;&#x61;&#x76;&#x61;&#x73;&#x63;&#x72;&#x69;&#x70;&#x74;&#x3A;alert(1)\">x</a>")]
  [InlineData("<a href=\"vbscript:msgbox(1)\">x</a>")]
  [InlineData("<iframe src=\"data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==\"></iframe>")]
  [InlineData("<object data=\"javascript:alert(1)\"></object>")]
  [InlineData("<embed src=\"javascript:alert(1)\">")]
  [InlineData("<form action=\"javascript:alert(1)\"><button>x</button></form>")]
  [InlineData("<div style=\"background:url(javascript:alert(1))\">x</div>")]
  [InlineData("<div style=\"width: expression(alert(1))\">x</div>")]
  [InlineData("<noscript><p title=\"</noscript><img src=x onerror=alert(1)>\"></noscript>")]
  [InlineData("<math><mi xlink:href=\"javascript:alert(1)\">x</mi></math>")]
  [InlineData("<!--<img src=x onerror=alert(1)>-->")]
  [InlineData("<meta http-equiv=\"refresh\" content=\"0;url=javascript:alert(1)\">")]
  [InlineData("<p onclick=\"alert(1)\">x</p>")]
  public void Sanitize_ShouldRemoveExecutableContent_WhenGivenKnownXssVectors(string html)
  {
    // Arrange
    var sanitizador = Criar();

    // Act
    var resultado = sanitizador.Sanitize(html).ToLowerInvariant();

    // Assert
    Assert.All(Proibidos, trecho => Assert.DoesNotContain(trecho, resultado, StringComparison.Ordinal));
  }

  // Testa se o HTML de formatação comum passa sem alteração
  [Theory]
  [InlineData("<p><strong>Negrito</strong> e <em>itálico</em></p>")]
  [InlineData("<p><a href=\"https://tooark.com\">link</a></p>")]
  [InlineData("<ul><li>Item 1</li><li>Item 2</li></ul>")]
  public void Sanitize_ShouldKeepSafeHtml(string html)
  {
    // Arrange
    var sanitizador = Criar();

    // Act
    var resultado = sanitizador.Sanitize(html);

    // Assert
    Assert.Equal(html, resultado);
  }

  // Testa se entrada nula ou em branco vira texto vazio
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Sanitize_ShouldReturnEmpty_WhenInputIsNullOrWhiteSpace(string? html)
  {
    // Arrange
    var sanitizador = Criar();

    // Act
    var resultado = sanitizador.Sanitize(html);

    // Assert
    Assert.Equal(string.Empty, resultado);
  }

  // Testa se a lista de tags e atributos configurada substitui a padrão
  [Fact]
  public void Sanitize_ShouldApplyConfiguredAllowlist()
  {
    // Arrange
    var sanitizador = Criar(new HtmlOptions { AllowedTags = ["p", "a"], AllowedAttributes = ["href"] });

    // Act
    var resultado = sanitizador.Sanitize("<p class=\"x\"><em>fora</em><a href=\"https://a.com\" title=\"t\">link</a></p>");

    // Assert
    Assert.Equal("<p><a href=\"https://a.com\">link</a></p>", resultado);
  }

  // Testa se o conteúdo de uma tag removida fica quando configurado
  [Fact]
  public void Sanitize_ShouldKeepChildNodes_WhenConfigured()
  {
    // Arrange
    var sanitizador = Criar(new HtmlOptions { AllowedTags = ["p"], KeepChildNodes = true });

    // Act
    var resultado = sanitizador.Sanitize("<p><em>texto</em></p>");

    // Assert
    Assert.Equal("<p>texto</p>", resultado);
  }

  // Testa se os esquemas configurados valem nos atributos de URL
  [Fact]
  public void Sanitize_ShouldApplyConfiguredSchemes()
  {
    // Arrange
    var sanitizador = Criar(new HtmlOptions { AllowedSchemes = ["https", "mailto:"] });

    // Act
    var mailto = sanitizador.Sanitize("<a href=\"mailto:contato@tooark.com\">e-mail</a>");
    var http = sanitizador.Sanitize("<a href=\"http://tooark.com\">site</a>");

    // Assert
    Assert.Equal("<a href=\"mailto:contato@tooark.com\">e-mail</a>", mailto);
    Assert.Equal("<a>site</a>", http);
  }

  // Testa se só as classes configuradas ficam no atributo class
  [Fact]
  public void Sanitize_ShouldKeepOnlyConfiguredClasses()
  {
    // Arrange
    var sanitizador = Criar(new HtmlOptions { AllowedClasses = ["destaque"] });

    // Act
    var resultado = sanitizador.Sanitize("<p class=\"destaque perigosa\">x</p>");

    // Assert
    Assert.Equal("<p class=\"destaque\">x</p>", resultado);
  }

  // Testa se a configuração que libera uma tag executável é recusada
  [Theory]
  [InlineData("script")]
  [InlineData("SCRIPT")]
  [InlineData("object")]
  [InlineData("embed")]
  [InlineData("base")]
  [InlineData("meta")]
  public void Constructor_ShouldThrow_WhenBlockedTagIsAllowed(string tag)
  {
    // Arrange
    var html = new HtmlOptions { AllowedTags = ["p", tag] };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Criar(html));

    // Assert
    Assert.Contains($"Options.Sanitizers.Html.TagNotAllowed;{tag}", ex.GetErrorMessages());
  }

  // Testa se a configuração que libera um atributo de evento é recusada
  [Theory]
  [InlineData("onclick")]
  [InlineData("OnError")]
  public void Constructor_ShouldThrow_WhenEventAttributeIsAllowed(string attribute)
  {
    // Arrange
    var html = new HtmlOptions { AllowedAttributes = ["href", attribute] };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Criar(html));

    // Assert
    Assert.Contains($"Options.Sanitizers.Html.AttributeNotAllowed;{attribute}", ex.GetErrorMessages());
  }

  // Testa se a configuração que libera um esquema executável é recusada
  [Theory]
  [InlineData("javascript", "javascript")]
  [InlineData("JavaScript:", "JavaScript")]
  [InlineData("data", "data")]
  [InlineData("vbscript", "vbscript")]
  public void Constructor_ShouldThrow_WhenBlockedSchemeIsAllowed(string scheme, string reported)
  {
    // Arrange
    var html = new HtmlOptions { AllowedSchemes = ["https", scheme] };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Criar(html));

    // Assert
    Assert.Contains($"Options.Sanitizers.SchemeNotAllowed;{reported}", ex.GetErrorMessages());
  }

  // Testa se um esquema com sintaxe inválida é recusado
  [Theory]
  [InlineData("java script")]
  [InlineData("1http")]
  [InlineData("")]
  public void Constructor_ShouldThrow_WhenSchemeIsInvalid(string scheme)
  {
    // Arrange
    var html = new HtmlOptions { AllowedSchemes = [scheme] };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Criar(html));

    // Assert
    Assert.Contains($"Options.Sanitizers.SchemeInvalid;{scheme}", ex.GetErrorMessages());
  }
}
