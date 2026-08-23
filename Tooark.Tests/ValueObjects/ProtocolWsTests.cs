using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class ProtocolWsTests
{
  // Teste se o protocolo WebSocket é válido a partir de um protocolo WebSocket válido
  [Theory]
  [InlineData("ws://example.com")]
  [InlineData("wss://example.com")]
  public void ProtocolWs_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var protocolFtp = new ProtocolWs(valueParam);

    // Assert
    Assert.True(protocolFtp.IsValid);
    Assert.Equal(expectedValue, protocolFtp.Value);
  }

  // Teste se o protocolo WebSocket é inválido a partir de um protocolo WebSocket inválido
  [Theory]
  [InlineData("ftp://example.com")]
  [InlineData("sftp://example.com")]
  [InlineData("http://example.com")]
  [InlineData("https://example.com")]
  [InlineData("imap://example.com")]
  [InlineData("pop3://example.com")]
  [InlineData("smtp://example.com")]
  [InlineData("example.com")]
  [InlineData("")]
  [InlineData(null)]
  public void ProtocolWs_ShouldBeInvalid_WhenGivenInvalidProtocolWs(string? valueParam)
  {
    // Arrange & Act
    var protocolFtp = new ProtocolWs(valueParam!);

    // Assert
    Assert.False(protocolFtp.IsValid);
    Assert.Equal(string.Empty, protocolFtp.Value);
  }

  // Testa se o método ToString retorna o protocolo WebSocket
  [Fact]
  public void ProtocolWs_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "wss://example.com";
    var expectedValue = value;
    var protocolFtp = new ProtocolWs(value);

    // Act
    var protocolString = protocolFtp.ToString();

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolFtp está sendo convertido para string implicitamente
  [Fact]
  public void ProtocolWs_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "wss://example.com";
    var expectedValue = value;
    var protocolFtp = new ProtocolWs(value);

    // Act
    string protocolString = protocolFtp;

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolFtp está sendo convertido de string implicitamente
  [Fact]
  public void ProtocolWs_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "wss://example.com";
    var expectedValue = value;

    // Act
    ProtocolWs protocolFtp = value;

    // Assert
    Assert.True(protocolFtp.IsValid);
    Assert.Equal(expectedValue, protocolFtp.Value);
  }
}
