using Tooark.Validations;

namespace Tooark.Tests.Validations;

public class EmailValidationTests
{
  // Teste para validar se o valor corresponde ao padrão e cria notificação, com valor que não corresponde
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
  public void IsEmail_ShouldAddNotification_WhenValueNotIsEmail(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = valueParam!;

    // Act
    validation.IsEmail(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor corresponde ao padrão e não cria notificação, com valor que corresponde
  [Theory]
  [InlineData("teste@example.com")] // Email válido
  [InlineData("te.te@example.com")] // Email válido
  [InlineData("te-te@example.com")] // Email válido
  [InlineData("te_te@example.com")] // Email válido
  [InlineData("teste@exa-ple.com")] // Email válido
  public void IsEmail_ShouldNotAddNotification_WhenValueIsEmail(string valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = valueParam;

    // Act
    validation.IsEmail(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor corresponde ao padrão e cria notificação, com valor que não corresponde
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
  public void IsEmailOrEmpty_ShouldAddNotification_WhenValueNotIsEmailOrEmpty(string valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = valueParam;

    // Act
    validation.IsEmailOrEmpty(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor corresponde ao padrão e não cria notificação, com valor que corresponde
  [Theory]
  [InlineData("teste@example.com")] // Email válido
  [InlineData("te.te@example.com")] // Email válido
  [InlineData("te-te@example.com")] // Email válido
  [InlineData("te_te@example.com")] // Email válido
  [InlineData("teste@exa-ple.com")] // Email válido
  [InlineData("")] // Email vazio
  [InlineData(null)] // Email nulo
  public void IsEmailOrEmpty_ShouldNotAddNotification_WhenValueIsEmailOrEmpty(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = valueParam!;

    // Act
    validation.IsEmailOrEmpty(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  // Teste para validar se o valor corresponde ao padrão e cria notificação, com valor que não corresponde
  [Theory]
  [InlineData("teste@example.com")] // Email válido
  [InlineData("teste@exa-ple.com")] // Email válido
  [InlineData("@@example.com")] // Email com mais de um @
  [InlineData(".@example.com")] // Email com apenas .
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
  [InlineData("@example.-com")] // Domínio iniciado com -
  [InlineData("@example._com")] // Domínio iniciado com _
  [InlineData("@example.com-")] // Domínio terminado com -
  [InlineData("@example.com_")] // Domínio terminado com _
  [InlineData("@example.com.")] // Domínio terminado com .
  [InlineData("@exaple..com")] // Domínio com dois .
  [InlineData("@example")] // Domínio sem extensão
  [InlineData("@.com")] // Domínio ausente apenas extensão
  [InlineData("@")] // Domínio ausente
  [InlineData("")] // Email vazio
  [InlineData(null)] // Email nulo
  public void IsEmailDomain_ShouldAddNotification_WhenValueNotIsEmailDomain(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = valueParam!;

    // Act
    validation.IsEmailDomain(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor corresponde ao padrão e não cria notificação, com valor que corresponde
  [Theory]
  [InlineData("@example.com")] // Email válido
  [InlineData("@exa-ple.com")] // Email válido
  public void IsEmailDomain_ShouldNotAddNotification_WhenValueIsEmailDomain(string valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = valueParam;

    // Act
    validation.IsEmailDomain(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }
  
  // Teste para validar se o valor corresponde ao padrão e cria notificação, com valor que não corresponde
  [Theory]
  [InlineData("teste@example.com")] // Email válido
  [InlineData("teste@exa-ple.com")] // Email válido
  [InlineData("@@example.com")] // Email com mais de um @
  [InlineData(".@example.com")] // Email com apenas .
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
  [InlineData("@example.-com")] // Domínio iniciado com -
  [InlineData("@example._com")] // Domínio iniciado com _
  [InlineData("@example.com-")] // Domínio terminado com -
  [InlineData("@example.com_")] // Domínio terminado com _
  [InlineData("@example.com.")] // Domínio terminado com .
  [InlineData("@exaple..com")] // Domínio com dois .
  [InlineData("@example")] // Domínio sem extensão
  [InlineData("@.com")] // Domínio ausente apenas extensão
  [InlineData("@")] // Domínio ausente
  public void IsEmailDomainOrEmpty_ShouldAddNotification_WhenValueNotIsEmailDomainOrEmpty(string valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = valueParam;

    // Act
    validation.IsEmailDomainOrEmpty(value, property);

    // Assert
    Assert.Single(validation.Notifications);
    Assert.Equal(property, validation.Notifications.First().Key);
  }

  // Teste para validar se o valor corresponde ao padrão e não cria notificação, com valor que corresponde
  [Theory]
  [InlineData("@example.com")] // Email válido
  [InlineData("@exa-ple.com")] // Email válido
  [InlineData("")] // Email vazio
  [InlineData(null)] // Email nulo
  public void IsEmailDomainOrEmpty_ShouldNotAddNotification_WhenValueIsEmailDomainOrEmpty(string? valueParam)
  {
    // Arrange
    var property = "TestProperty";
    var validation = new Validation();
    string value = valueParam!;

    // Act
    validation.IsEmailDomainOrEmpty(value, property);

    // Assert
    Assert.Empty(validation.Notifications);
  }

  #region Nick e domínio curtos, aceitos a partir da v4.0.0

  // Testa se endereços com uma única letra no nick ou no rótulo do domínio são aceitos.
  // O padrão anterior exigia dois caracteres em cada parte, e reprovava endereços legítimos e
  // existentes, como x@google.com.
  [Theory]
  [InlineData("x@google.com")]
  [InlineData("a@b.co")]
  [InlineData("ab@b.co")]
  [InlineData("a1@b2.c3")]
  public void IsEmail_ShouldAccept_WhenLocalPartOrDomainLabelHasASingleCharacter(string value)
  {
    // Arrange
    var validation = new Validation();

    // Act
    validation.IsEmail(value, "Email");

    // Assert
    Assert.True(validation.IsValid);
  }

  // Testa se o sinal de mais segue reprovado. É decisão do pacote: o sufixo com '+' é válido pelo
  // RFC 5322 e usado por Gmail e Outlook, mas o Tooark mantém o bloqueio.
  [Theory]
  [InlineData("nome+tag@exemplo.com")]
  [InlineData("nome!tag@exemplo.com")]
  [InlineData("nome#tag@exemplo.com")]
  [InlineData("nome tag@exemplo.com")]
  public void IsEmail_ShouldStillReject_WhenLocalPartHasSpecialCharacters(string value)
  {
    // Arrange
    var validation = new Validation();

    // Act
    validation.IsEmail(value, "Email");

    // Assert
    Assert.False(validation.IsValid);
  }

  // Testa se o domínio segue exigindo ao menos um ponto e rótulos bem formados
  [Theory]
  [InlineData("nome@exemplo")]
  [InlineData("nome@-exemplo.com")]
  [InlineData("nome@exemplo-.com")]
  [InlineData("nome@exemplo..com")]
  public void IsEmail_ShouldStillReject_WhenDomainIsMalformed(string value)
  {
    // Arrange
    var validation = new Validation();

    // Act
    validation.IsEmail(value, "Email");

    // Assert
    Assert.False(validation.IsValid);
  }

  // Testa se o padrão deixou de ser vulnerável a retrocesso catastrófico. A entrada hostil levava o
  // padrão anterior a estourar o tempo limite de 300 ms a partir de 96 caracteres; o novo resolve de
  // imediato, porque não tem classes quantificadas adjacentes com conjuntos sobrepostos.
  [Theory]
  [InlineData(96)]
  [InlineData(128)]
  [InlineData(256)]
  public void IsEmail_ShouldNotBacktrack_WhenInputIsHostile(int length)
  {
    // Arrange
    var value = new string('a', length) + "@" + new string('b', length);
    var validation = new Validation();
    var cronometro = System.Diagnostics.Stopwatch.StartNew();

    // Act
    validation.IsEmail(value, "Email");
    cronometro.Stop();

    // Assert
    Assert.False(validation.IsValid);
    Assert.True(cronometro.ElapsedMilliseconds < 100,
      $"A avaliação levou {cronometro.ElapsedMilliseconds} ms, sinal de retrocesso catastrófico.");
  }

  #endregion
}
