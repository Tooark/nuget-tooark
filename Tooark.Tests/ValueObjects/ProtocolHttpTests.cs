using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class ProtocolHttpTests
{
  // Teste se o protocolo HTTP é válido a partir de um protocolo HTTP válido
  [Theory]
  [InlineData("http://example.com")]
  [InlineData("https://example.com")]
  public void ProtocolHttp_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var protocolHttp = new ProtocolHttp(valueParam);

    // Assert
    Assert.True(protocolHttp.IsValid);
    Assert.Equal(expectedValue, protocolHttp.Value);
  }

  // Teste se o protocolo HTTP é inválido a partir de um protocolo HTTP inválido
  [Theory]
  [InlineData("ftp://example.com")]
  [InlineData("sftp://example.com")]
  [InlineData("imap://example.com")]
  [InlineData("pop3://example.com")]
  [InlineData("smtp://example.com")]
  [InlineData("ws://example.com")]
  [InlineData("wss://example.com")]
  [InlineData("example.com")]
  [InlineData("")]
  [InlineData(null)]
  public void ProtocolHttp_ShouldBeInvalid_WhenGivenInvalidProtocolHttp(string? valueParam)
  {
    // Arrange & Act
    var protocolHttp = new ProtocolHttp(valueParam!);

    // Assert
    Assert.False(protocolHttp.IsValid);
    Assert.Equal(string.Empty, protocolHttp.Value);
  }

  // Testa se o método ToString retorna o protocolo HTTP
  [Fact]
  public void ProtocolHttp_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "https://example.com";
    var expectedValue = value;
    var protocolHttp = new ProtocolHttp(value);

    // Act
    var protocolString = protocolHttp.ToString();

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolHttp está sendo convertido para string implicitamente
  [Fact]
  public void ProtocolHttp_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "https://example.com";
    var expectedValue = value;
    var protocolHttp = new ProtocolHttp(value);

    // Act
    string protocolString = protocolHttp;

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolHttp está sendo convertido de string implicitamente
  [Fact]
  public void ProtocolHttp_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "https://example.com";
    var expectedValue = value;

    // Act
    ProtocolHttp protocolHttp = value;

    // Assert
    Assert.True(protocolHttp.IsValid);
    Assert.Equal(expectedValue, protocolHttp.Value);
  }
}
