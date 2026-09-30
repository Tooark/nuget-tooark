using Tooark.Exceptions;
using Tooark.Sanitizers;
using Tooark.Sanitizers.Options;

namespace Tooark.Tests.Sanitizers;

/// <summary>
/// Testes do sanitizador de URL.
/// </summary>
public class UrlSanitizerServiceTests
{
  // Cria o serviço com as opções de URL informadas
  private static UrlSanitizerService Criar(UrlOptions? url = null) =>
    new(Microsoft.Extensions.Options.Options.Create(new SanitizerOptions { Url = url ?? new UrlOptions() }));

  // Testa se URLs http e https passam, só sem os caracteres de controle e espaços
  [Theory]
  [InlineData("https://tooark.com", "https://tooark.com")]
  [InlineData("http://tooark.com/pagina?a=1&b=2#topo", "http://tooark.com/pagina?a=1&b=2#topo")]
  [InlineData("HTTPS://TOOARK.COM", "HTTPS://TOOARK.COM")]
  [InlineData("  https://tooark.com  ", "https://tooark.com")]
  [InlineData("https://too\nark.com", "https://tooark.com")]
  public void Sanitize_ShouldKeepHttpUrls(string url, string esperado)
  {
    // Arrange
    var sanitizador = Criar();

    // Act
    var resultado = sanitizador.Sanitize(url);

    // Assert
    Assert.Equal(esperado, resultado);
    Assert.True(sanitizador.IsSafe(url));
  }

  // Testa se esquemas executáveis e truques de ofuscação são recusados
  [Theory]
  [InlineData("javascript:alert(1)")]
  [InlineData("JaVaScRiPt:alert(1)")]
  [InlineData(" javascript:alert(1)")]
  [InlineData("java\tscript:alert(1)")]
  [InlineData("java\nscript:alert(1)")]
  [InlineData("java\rscript:alert(1)")]
  [InlineData("\u0000javascript:alert(1)")]
  [InlineData("java\u0000script:alert(1)")]
  [InlineData(" javascript:alert(1)")]
  [InlineData("﻿javascript:alert(1)")]
  [InlineData("vbscript:msgbox(1)")]
  [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
  [InlineData("DATA:image/png;base64,iVBORw0KGgo=")]
  [InlineData("blob:https://tooark.com/uuid")]
  [InlineData("file:///etc/passwd")]
  [InlineData("ftp://tooark.com/arquivo")]
  [InlineData("mailto:contato@tooark.com")]
  [InlineData("&#106;avascript:alert(1)")]
  [InlineData("javascript&colon;alert(1)")]
  public void Sanitize_ShouldRejectUnsafeSchemes(string url)
  {
    // Arrange
    var sanitizador = Criar();

    // Act
    var resultado = sanitizador.Sanitize(url);

    // Assert
    Assert.Equal(string.Empty, resultado);
    Assert.False(sanitizador.IsSafe(url));
  }

  // Testa se URL relativa é recusada no padrão
  [Theory]
  [InlineData("/pagina")]
  [InlineData("#topo")]
  [InlineData("?busca=1")]
  [InlineData("./arquivo")]
  [InlineData("../arquivo")]
  public void Sanitize_ShouldRejectRelativeUrls_ByDefault(string url)
  {
    // Arrange
    var sanitizador = Criar();

    // Act & Assert
    Assert.False(sanitizador.IsSafe(url));
  }

  // Testa se URL relativa com prefixo é aceita quando liberada
  [Theory]
  [InlineData("/pagina")]
  [InlineData("#topo")]
  [InlineData("?busca=1")]
  [InlineData("./arquivo")]
  [InlineData("../arquivo")]
  public void Sanitize_ShouldAcceptRelativeUrls_WhenAllowed(string url)
  {
    // Arrange
    var sanitizador = Criar(new UrlOptions { AllowRelative = true });

    // Act
    var resultado = sanitizador.Sanitize(url);

    // Assert
    Assert.Equal(url, resultado);
  }

  // Testa se caminho sem prefixo é recusado mesmo com relativas liberadas, como no @tooark/wysiwyg
  [Theory]
  [InlineData("uploads/foto.png")]
  [InlineData("pagina")]
  public void Sanitize_ShouldRejectPathWithoutPrefix_WhenRelativeIsAllowed(string url)
  {
    // Arrange
    var sanitizador = Criar(new UrlOptions { AllowRelative = true });

    // Act & Assert
    Assert.False(sanitizador.IsSafe(url));
  }

  // Testa se URL com usuário e senha é recusada no padrão e aceita quando liberada
  [Fact]
  public void Sanitize_ShouldHandleCredentialsByOption()
  {
    // Arrange
    const string url = "https://banco.com@golpe.com/login";
    var padrao = Criar();
    var liberado = Criar(new UrlOptions { AllowCredentials = true });

    // Act & Assert
    Assert.False(padrao.IsSafe(url));
    Assert.True(liberado.IsSafe(url));
  }

  // Testa se os esquemas configurados substituem os padrões
  [Fact]
  public void Sanitize_ShouldApplyConfiguredSchemes()
  {
    // Arrange
    var sanitizador = Criar(new UrlOptions { AllowedSchemes = ["https", "mailto", "TEL:"] });

    // Act & Assert
    Assert.True(sanitizador.IsSafe("mailto:contato@tooark.com"));
    Assert.True(sanitizador.IsSafe("tel:+5511999999999"));
    Assert.True(sanitizador.IsSafe("https://tooark.com"));
    Assert.False(sanitizador.IsSafe("http://tooark.com"));
  }

  // Testa se entrada nula ou em branco é recusada
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData(" \t\n ")]
  public void Sanitize_ShouldReturnEmpty_WhenInputIsNullOrWhiteSpace(string? url)
  {
    // Arrange
    var sanitizador = Criar();

    // Act & Assert
    Assert.Equal(string.Empty, sanitizador.Sanitize(url));
    Assert.False(sanitizador.IsSafe(url));
  }

  // Testa se a configuração que libera um esquema executável é recusada
  [Theory]
  [InlineData("javascript")]
  [InlineData("data")]
  [InlineData("vbscript")]
  public void Constructor_ShouldThrow_WhenBlockedSchemeIsAllowed(string scheme)
  {
    // Arrange
    var url = new UrlOptions { AllowedSchemes = [scheme] };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Criar(url));

    // Assert
    Assert.Contains($"Options.Sanitizers.SchemeNotAllowed;{scheme}", ex.GetErrorMessages());
  }
}
