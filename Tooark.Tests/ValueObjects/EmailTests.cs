using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class EmailTests
{
  // Testa se o endereço de email é válido a partir de um endereço de email válido
  [Theory]
  [InlineData("teste@example.com")] // Email válido
  [InlineData("te.te@example.com")] // Email válido
  [InlineData("te-te@example.com")] // Email válido
  [InlineData("te_te@example.com")] // Email válido
  [InlineData("teste@exa-ple.com")] // Email válido
  [InlineData("TESTE@EXAMPLE.COM")] // Email válido
  [InlineData("TE.TE@EXAMPLE.COM")] // Email válido
  [InlineData("TE-TE@EXAMPLE.COM")] // Email válido
  [InlineData("TE_TE@EXAMPLE.COM")] // Email válido
  [InlineData("TESTE@EXA-PLE.COM")] // Email válido
  public void Email_ShouldBeValid_WhenGivenValidEmail(string emailParam)
  {
    // Arrange & Act
    Email email = new(emailParam);

    // Assert
    Assert.True(email.IsValid);
    Assert.Equal(emailParam.ToLowerInvariant().Trim(), email.Value);
  }

  // Testa se o endereço de email é inválido a partir de um endereço de email inválido
  [Theory]
  [InlineData(".test@example.com")] // Email iniciado com .
  [InlineData("-test@example.com")] // Email iniciado com -
  [InlineData("_test@example.com")] // Email iniciado com _
  [InlineData("test.@example.com")] // Email terminado com .
  [InlineData("test-@example.com")] // Email terminado com -
  [InlineData("test_@example.com")] // Email terminado com _
  [InlineData("te!st@example.com")] // Email que carácter especial !
  [InlineData("te#st@example.com")] // Email que carácter especial #
  [InlineData("te$st@example.com")] // Email que carácter especial $
  [InlineData("teçst@example.com")] // Email que carácter especial ç
  [InlineData("test@@example.com")] // Email com mais de um @
  [InlineData(".@example.com")] // Email com apenas .
  [InlineData("teste@.")] // Domínio com apenas .
  [InlineData("test@example@.com")] // Domínio com @
  [InlineData("test@.example.com")] // Domínio iniciado com .
  [InlineData("test@-example.com")] // Domínio iniciado com -
  [InlineData("test@_example.com")] // Domínio iniciado com _
  [InlineData("test@example-.com")] // Domínio terminado com -
  [InlineData("test@example_.com")] // Domínio terminado com _
  [InlineData("teste@exa!ple.com")] // Domínio que carácter especial !
  [InlineData("teste@exa#ple.com")] // Domínio que carácter especial #
  [InlineData("teste@exa$ple.com")] // Domínio que carácter especial $
  [InlineData("teste@exaçple.com")] // Domínio que carácter especial ç
  [InlineData("test@example.-com")] // Domínio iniciado com -
  [InlineData("test@example._com")] // Domínio iniciado com _
  [InlineData("test@example.com-")] // Domínio terminado com -
  [InlineData("test@example.com_")] // Domínio terminado com _
  [InlineData("test@example.com.")] // Domínio terminado com .
  [InlineData("test@exaple..com")] // Domínio com dois .
  [InlineData("test@example")] // Domínio sem extensão
  [InlineData("test@.com")] // Domínio ausente apenas extensão
  [InlineData("test@")] // Domínio ausente
  [InlineData("")] // Email vazio
  [InlineData(null)] // Email nulo
  public void Email_ShouldBeInvalid_WhenGivenInvalidEmail(string? emailParam)
  {
    // Arrange & Act
    var email = new Email(emailParam!);

    // Assert
    Assert.False(email.IsValid);
    Assert.Equal(string.Empty, email.Value);
  }

  // Testa se o método ToString retorna o endereço de email
  [Fact]
  public void Email_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var validEmail = "test@example.com";
    var email = new Email(validEmail);

    // Act
    var emailString = email.ToString();

    // Assert
    Assert.Equal(validEmail.ToLowerInvariant().Trim(), emailString);
  }

  // Testa se o endereço de email está sendo convertido para string implicitamente
  [Fact]
  public void Email_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var validEmail = "test@example.com";
    var email = new Email(validEmail);

    // Act
    string emailString = email;

    // Assert
    Assert.Equal(validEmail.ToLowerInvariant().Trim(), emailString);
  }

  // Testa se o endereço de email está sendo convertido de string implicitamente
  [Fact]
  public void Email_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var validEmail = "test@example.com";

    // Act
    Email email = validEmail;

    // Assert
    Assert.True(email.IsValid);
    Assert.Equal(validEmail.ToLowerInvariant().Trim(), email.Value);
  }
}
