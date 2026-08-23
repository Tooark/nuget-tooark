using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class ProtocolEmailSenderTests
{
  // Teste se o protocolo EmailSender é válido a partir de um protocolo EmailSender válido
  [Theory]
  [InlineData("smtp://example.com")]
  public void ProtocolEmailSender_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var protocolEmailSender = new ProtocolEmailSender(valueParam);

    // Assert
    Assert.True(protocolEmailSender.IsValid);
    Assert.Equal(expectedValue, protocolEmailSender.Value);
  }

  // Teste se o protocolo EmailSender é inválido a partir de um protocolo EmailSender inválido
  [Theory]
  [InlineData("http://example.com")]
  [InlineData("https://example.com")]
  [InlineData("ftp://example.com")]
  [InlineData("sftp://example.com")]
  [InlineData("imap://example.com")]
  [InlineData("pop3://example.com")]
  [InlineData("ws://example.com")]
  [InlineData("wss://example.com")]
  [InlineData("example.com")]
  [InlineData("")]
  [InlineData(null)]
  public void ProtocolEmailSender_ShouldBeInvalid_WhenGivenInvalidProtocolEmailSender(string? valueParam)
  {
    // Arrange & Act
    var protocolEmailSender = new ProtocolEmailSender(valueParam!);

    // Assert
    Assert.False(protocolEmailSender.IsValid);
    Assert.Equal(string.Empty, protocolEmailSender.Value);
  }

  // Testa se o método ToString retorna o protocolo EmailSender
  [Fact]
  public void ProtocolEmailSender_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "smtp://example.com";
    var expectedValue = value;
    var protocolEmailSender = new ProtocolEmailSender(value);

    // Act
    var protocolString = protocolEmailSender.ToString();

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolEmailSender está sendo convertido para string implicitamente
  [Fact]
  public void ProtocolEmailSender_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "smtp://example.com";
    var expectedValue = value;
    var protocolEmailSender = new ProtocolEmailSender(value);

    // Act
    string protocolString = protocolEmailSender;

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolEmailSender está sendo convertido de string implicitamente
  [Fact]
  public void ProtocolEmailSender_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "smtp://example.com";
    var expectedValue = value;

    // Act
    ProtocolEmailSender protocolEmailSender = value;

    // Assert
    Assert.True(protocolEmailSender.IsValid);
    Assert.Equal(expectedValue, protocolEmailSender.Value);
  }
}
