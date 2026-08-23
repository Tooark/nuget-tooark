using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class ProtocolEmailReceiverTests
{
  // Teste se o protocolo EmailReceiver é válido a partir de um protocolo EmailReceiver válido
  [Theory]
  [InlineData("imap://example.com")]
  [InlineData("pop3://example.com")]
  public void ProtocolEmailReceiver_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var protocolEmailReceiver = new ProtocolEmailReceiver(valueParam);

    // Assert
    Assert.True(protocolEmailReceiver.IsValid);
    Assert.Equal(expectedValue, protocolEmailReceiver.Value);
  }

  // Teste se o protocolo EmailReceiver é inválido a partir de um protocolo EmailReceiver inválido
  [Theory]
  [InlineData("http://example.com")]
  [InlineData("https://example.com")]
  [InlineData("ftp://example.com")]
  [InlineData("sftp://example.com")]
  [InlineData("smtp://example.com")]
  [InlineData("ws://example.com")]
  [InlineData("wss://example.com")]
  [InlineData("example.com")]
  [InlineData("")]
  [InlineData(null)]
  public void ProtocolEmailReceiver_ShouldBeInvalid_WhenGivenInvalidProtocolEmailReceiver(string? valueParam)
  {
    // Arrange & Act
    var protocolEmailReceiver = new ProtocolEmailReceiver(valueParam!);

    // Assert
    Assert.False(protocolEmailReceiver.IsValid);
    Assert.Equal(string.Empty, protocolEmailReceiver.Value);
  }

  // Testa se o método ToString retorna o protocolo EmailReceiver
  [Fact]
  public void ProtocolEmailReceiver_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "imap://example.com";
    var expectedValue = value;
    var protocolEmailReceiver = new ProtocolEmailReceiver(value);

    // Act
    var protocolString = protocolEmailReceiver.ToString();

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolEmailReceiver está sendo convertido para string implicitamente
  [Fact]
  public void ProtocolEmailReceiver_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "imap://example.com";
    var expectedValue = value;
    var protocolEmailReceiver = new ProtocolEmailReceiver(value);

    // Act
    string protocolString = protocolEmailReceiver;

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolEmailReceiver está sendo convertido de string implicitamente
  [Fact]
  public void ProtocolEmailReceiver_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "imap://example.com";
    var expectedValue = value;

    // Act
    ProtocolEmailReceiver protocolEmailReceiver = value;

    // Assert
    Assert.True(protocolEmailReceiver.IsValid);
    Assert.Equal(expectedValue, protocolEmailReceiver.Value);
  }
}
