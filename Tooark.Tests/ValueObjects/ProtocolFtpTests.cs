using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class ProtocolFtpTests
{
  // Teste se o protocolo FTP é válido a partir de um protocolo FTP válido
  [Theory]
  [InlineData("ftp://example.com")]
  [InlineData("sftp://example.com")]
  public void ProtocolFtp_ShouldBeValid_WhenGivenValidValue(string valueParam)
  {
    // Arrange
    var expectedValue = valueParam;

    // Act
    var protocolFtp = new ProtocolFtp(valueParam);

    // Assert
    Assert.True(protocolFtp.IsValid);
    Assert.Equal(expectedValue, protocolFtp.Value);
  }

  // Teste se o protocolo FTP é inválido a partir de um protocolo FTP inválido
  [Theory]
  [InlineData("http://example.com")]
  [InlineData("https://example.com")]
  [InlineData("imap://example.com")]
  [InlineData("pop3://example.com")]
  [InlineData("smtp://example.com")]
  [InlineData("ws://example.com")]
  [InlineData("wss://example.com")]
  [InlineData("example.com")]
  [InlineData("")]
  [InlineData(null)]
  public void ProtocolFtp_ShouldBeInvalid_WhenGivenInvalidProtocolFtp(string? valueParam)
  {
    // Arrange & Act
    var protocolFtp = new ProtocolFtp(valueParam!);

    // Assert
    Assert.False(protocolFtp.IsValid);
    Assert.Equal(string.Empty, protocolFtp.Value);
  }

  // Testa se o método ToString retorna o protocolo FTP
  [Fact]
  public void ProtocolFtp_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var value = "sftp://example.com";
    var expectedValue = value;
    var protocolFtp = new ProtocolFtp(value);

    // Act
    var protocolString = protocolFtp.ToString();

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolFtp está sendo convertido para string implicitamente
  [Fact]
  public void ProtocolFtp_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var value = "sftp://example.com";
    var expectedValue = value;
    var protocolFtp = new ProtocolFtp(value);

    // Act
    string protocolString = protocolFtp;

    // Assert
    Assert.Equal(expectedValue, protocolString);
  }

  // Testa se o endereço de protocolFtp está sendo convertido de string implicitamente
  [Fact]
  public void ProtocolFtp_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var value = "sftp://example.com";
    var expectedValue = value;

    // Act
    ProtocolFtp protocolFtp = value;

    // Assert
    Assert.True(protocolFtp.IsValid);
    Assert.Equal(expectedValue, protocolFtp.Value);
  }
}
