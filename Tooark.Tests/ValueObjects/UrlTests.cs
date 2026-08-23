using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class UrlTests
{
  // Teste se a URL é válido a partir de uma URL válida
  [Theory]
  [InlineData("ftp://example.com")]
  [InlineData("sftp://example.com")]
  [InlineData("http://example.com")]
  [InlineData("https://example.com")]
  [InlineData("imap://example.com")]
  [InlineData("pop3://example.com")]
  [InlineData("smtp://example.com")]
  [InlineData("ws://example.com")]
  [InlineData("wss://example.com")]
  public void Url_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var url = new Url(valueParam);

    // Assert
    Assert.True(url.IsValid);
    Assert.Equal(expectedValue, url.Value);
  }

  // Teste se a URL é inválido a partir de uma URL inválida
  [Theory]
  [InlineData("example.com")]
  [InlineData("")]
  [InlineData(null)]
  public void Url_ShouldBeInvalid_WhenGivenInvalidUrl(string? valueParam)
  {
    // Arrange & Act
    var url = new Url(valueParam!);

    // Assert
    Assert.False(url.IsValid);
    Assert.Equal(string.Empty, url.Value);
  }

  // Testa se o método ToString retorna a URL
  [Fact]
  public void Url_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "sftp://example.com";
    var expectedValue = value;
    var url = new Url(value);

    // Act
    var urlString = url.ToString();

    // Assert
    Assert.Equal(expectedValue, urlString);
  }

  // Testa se o endereço de url está sendo convertido para string implicitamente
  [Fact]
  public void Url_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "sftp://example.com";
    var expectedValue = value;
    var url = new Url(value);

    // Act
    string urlString = url;

    // Assert
    Assert.Equal(expectedValue, urlString);
  }

  // Testa se o endereço de url está sendo convertido de string implicitamente
  [Fact]
  public void Url_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "sftp://example.com";
    var expectedValue = value;

    // Act
    Url url = value;

    // Assert
    Assert.True(url.IsValid);
    Assert.Equal(expectedValue, url.Value);
  }

  // Testa se o protocolo de recebimento de email está sendo convertido para string implicitamente
  [Fact]
  public void Url_ShouldConvertToStringImplicitly_WhenProtocolEmailReceiver()
  {
    // Arrange
    var value = "imap://example.com";
    var expectedValue = value;
    var protocol = new ProtocolEmailReceiver(value);
    var url = new Url(protocol);

    // Act
    string urlString = url;

    // Assert
    Assert.Equal(expectedValue, urlString);
  }

  // Testa se o protocolo de envio de email está sendo convertido para string implicitamente
  [Fact]
  public void Url_ShouldConvertToStringImplicitly_WhenProtocolEmailSender()
  {
    // Arrange
    var value = "smtp://example.com";
    var expectedValue = value;
    var protocol = new ProtocolEmailSender(value);
    var url = new Url(protocol);

    // Act
    string urlString = url;

    // Assert
    Assert.Equal(expectedValue, urlString);
  }

  // Testa se o protocolo de FTP está sendo convertido para string implicitamente
  [Fact]
  public void Url_ShouldConvertToStringImplicitly_WhenProtocolFtp()
  {
    // Arrange
    var value = "sftp://example.com";
    var expectedValue = value;
    var protocol = new ProtocolFtp(value);
    var url = new Url(protocol);

    // Act
    string urlString = url;

    // Assert
    Assert.Equal(expectedValue, urlString);
  }

  // Testa se o protocolo de HTTP está sendo convertido para string implicitamente
  [Fact]
  public void Url_ShouldConvertToStringImplicitly_WhenProtocolHttp()
  {
    // Arrange
    var value = "https://example.com";
    var expectedValue = value;
    var protocol = new ProtocolHttp(value);
    var url = new Url(protocol);

    // Act
    string urlString = url;

    // Assert
    Assert.Equal(expectedValue, urlString);
  }

  // Testa se o protocolo de WebSocket está sendo convertido para string implicitamente
  [Fact]
  public void Url_ShouldConvertToStringImplicitly_WhenProtocolWs()
  {
    // Arrange
    var value = "wss://example.com";
    var expectedValue = value;
    var protocol = new ProtocolWs(value);
    var url = new Url(protocol);

    // Act
    string urlString = url;

    // Assert
    Assert.Equal(expectedValue, urlString);
  }
}
