using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class EmailDomainTests
{
  // Testa se o domínio de email é válido a partir de um domínio de email válido
  [Theory]
  [InlineData("@example.com")] // EmailDomain válido
  [InlineData("@exa-ple.com")] // EmailDomain válido
  [InlineData("@EXAMPLE.COM")] // EmailDomain válido
  [InlineData("@EXA-PLE.COM")] // EmailDomain válido
  public void EmailDomain_ShouldBeValid_WhenGivenValidEmailDomain(string domainParam)
  {
    // Arrange & Act
    EmailDomain domain = new(domainParam);

    // Assert
    Assert.True(domain.IsValid);
    Assert.Equal(domainParam.ToLowerInvariant().Trim(), domain.Value);
  }

  // Testa se o domínio de email é inválido a partir de um domínio de email inválido
  [Theory]
  [InlineData("test@example.com")] // Endereço de email
  [InlineData("@.")] // Domínio com apenas .
  [InlineData("@example@.com")] // Domínio com @
  [InlineData("@.example.com")] // Domínio iniciado com .
  [InlineData("@-example.com")] // Domínio iniciado com -
  [InlineData("@_example.com")] // Domínio iniciado com _
  [InlineData("@example-.com")] // Domínio terminado com -
  [InlineData("@example_.com")] // Domínio terminado com _
  [InlineData("@exa!ple.com")] // Domínio que carácter especial !
  [InlineData("@exa#ple.com")] // Domínio que carácter especial #
  [InlineData("@exa$ple.com")] // Domínio que carácter especial $
  [InlineData("@exaçple.com")] // Domínio que carácter especial ç
  [InlineData("@example.-com")] // Extensão iniciado com -
  [InlineData("@example._com")] // Extensão iniciado com _
  [InlineData("@example.com-")] // Extensão terminado com -
  [InlineData("@example.com_")] // Extensão terminado com _
  [InlineData("@example.com.")] // Extensão terminado com .
  [InlineData("@exaple..com")] // Domínio com dois .
  [InlineData("@example")] // Domínio sem extensão
  [InlineData("@.com")] // Domínio ausente apenas extensão
  [InlineData("@")] // Domínio e extensão ausentes
  [InlineData("")] // EmailDomain vazio
  [InlineData(null)] // EmailDomain nulo
  public void EmailDomain_ShouldBeInvalid_WhenGivenInvalidEmailDomain(string? domainParam)
  {
    // Arrange & Act
    var domain = new EmailDomain(domainParam!);

    // Assert
    Assert.False(domain.IsValid);
    Assert.Equal(string.Empty, domain.Value);
  }

  // Testa se o método ToString retorna o domínio de email
  [Fact]
  public void EmailDomain_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var validEmailDomain = "@example.com";
    var domain = new EmailDomain(validEmailDomain);

    // Act
    var domainString = domain.ToString();

    // Assert
    Assert.Equal(validEmailDomain.ToLowerInvariant().Trim(), domainString);
  }

  // Testa se o domínio de email está sendo convertido para string implicitamente
  [Fact]
  public void EmailDomain_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var validEmailDomain = "@example.com";
    var domain = new EmailDomain(validEmailDomain);

    // Act
    string domainString = domain;

    // Assert
    Assert.Equal(validEmailDomain.ToLowerInvariant().Trim(), domainString);
  }

  // Testa se o domínio de email está sendo convertido de string implicitamente
  [Fact]
  public void EmailDomain_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var validEmailDomain = "@example.com";

    // Act
    EmailDomain domain = validEmailDomain;

    // Assert
    Assert.True(domain.IsValid);
    Assert.Equal(validEmailDomain.ToLowerInvariant().Trim(), domain.Value);
  }
}
