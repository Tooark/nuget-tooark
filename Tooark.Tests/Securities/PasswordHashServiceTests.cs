using Tooark.Exceptions;
using Tooark.Securities;
using Tooark.Securities.Options;
using MEOptions = Microsoft.Extensions.Options;

namespace Tooark.Tests.Securities;

public class PasswordHashServiceTests
{
  #region Test Helpers

  // Serviço com as opções padrão
  private static PasswordHashService CreateService(Action<PasswordHashOptions>? configure = null)
  {
    var options = new PasswordHashOptions();
    configure?.Invoke(options);

    return new PasswordHashService(MEOptions.Options.Create(options));
  }

  // Opções inválidas para cenários de erro
  private class NullOptions : MEOptions.IOptions<PasswordHashOptions>
  {
    public PasswordHashOptions Value => null!;
  }

  #endregion

  // Testa se o hash sai no formato PHC com os parâmetros padrão e Base64 sem preenchimento
  [Fact]
  public void Hash_ShouldReturnPhcString_WithDefaultParameters()
  {
    // Arrange
    var service = CreateService();

    // Act
    var hash = service.Hash("correct horse battery staple");

    // Assert
    Assert.Matches(@"^\$pbkdf2-sha256\$i=600000\$[A-Za-z0-9+/]{22}\$[A-Za-z0-9+/]{43}$", hash);
  }

  // Testa se cada hash usa um salt novo: a mesma senha nunca produz o mesmo texto
  [Fact]
  public void Hash_ShouldUseRandomSalt()
  {
    // Arrange
    var service = CreateService();

    // Act
    var first = service.Hash("senha");
    var second = service.Hash("senha");

    // Assert
    Assert.NotEqual(first, second);
  }

  // Testa se uma senha ausente é recusada com a chave de mensagem
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  public void Hash_ShouldThrowBadRequest_WhenPasswordIsMissing(string? password)
  {
    // Arrange
    var service = CreateService();

    // Act
    var exception = Assert.Throws<BadRequestException>(() => service.Hash(password!));

    // Assert
    Assert.Equal("PasswordHash.PasswordNotProvided", exception.Message);
  }

  // Testa se a senha confere com o próprio hash e outra senha não
  [Fact]
  public void Verify_ShouldReturnTrueForSamePassword_AndFalseForAnother()
  {
    // Arrange
    var service = CreateService();
    var hash = service.Hash("senha-correta");

    // Act & Assert
    Assert.True(service.Verify("senha-correta", hash));
    Assert.False(service.Verify("senha-errada", hash));
    Assert.False(service.Verify("Senha-correta", hash));
  }

  // Testa o vetor conhecido do PBKDF2-HMAC-SHA256 (senha "password", salt "salt", 1 iteração), montado no formato PHC
  [Fact]
  public void Verify_ShouldAcceptKnownPbkdf2Vector()
  {
    // Arrange
    var service = CreateService();
    var salt = Convert.ToBase64String("salt"u8.ToArray()).TrimEnd('=');
    var digest = Convert.ToBase64String(Convert.FromHexString("120fb6cffcf8b32c43e7225256c4f837a86548c92ccc35480805987cb70be17b")).TrimEnd('=');
    var hash = $"$pbkdf2-sha256$i=1${salt}${digest}";

    // Act & Assert
    Assert.True(service.Verify("password", hash));
    Assert.False(service.Verify("Password", hash));
  }

  // Testa se o hash sem preenchimento também é lido com o preenchimento do Base64 padrão
  [Fact]
  public void Verify_ShouldAcceptPaddedBase64()
  {
    // Arrange
    var service = CreateService();
    var salt = Convert.ToBase64String("salt"u8.ToArray());
    var digest = Convert.ToBase64String(Convert.FromHexString("120fb6cffcf8b32c43e7225256c4f837a86548c92ccc35480805987cb70be17b"));

    // Act & Assert
    Assert.True(service.Verify("password", $"$pbkdf2-sha256$i=1${salt}${digest}"));
  }

  // Testa se senha ou hash ausentes ou malformados devolvem falso sem lançar
  [Theory]
  [InlineData(null, "$pbkdf2-sha256$i=1$c2FsdA$Eg+2z/z4syxD5yJSVsT4N6hlSMkszDVIgAWYfLcL4Xs")]
  [InlineData("", "$pbkdf2-sha256$i=1$c2FsdA$Eg+2z/z4syxD5yJSVsT4N6hlSMkszDVIgAWYfLcL4Xs")]
  [InlineData("password", null)]
  [InlineData("password", "")]
  [InlineData("password", "plain-text")]
  [InlineData("password", "$argon2id$v=19$m=65536,t=3,p=4$c2FsdA$Eg+2z/z4syxD5yJSVsT4N6hlSMkszDVIgAWYfLcL4Xs")]
  [InlineData("password", "$pbkdf2-sha256$i=abc$c2FsdA$Eg+2z/z4syxD5yJSVsT4N6hlSMkszDVIgAWYfLcL4Xs")]
  [InlineData("password", "$pbkdf2-sha256$i=0$c2FsdA$Eg+2z/z4syxD5yJSVsT4N6hlSMkszDVIgAWYfLcL4Xs")]
  [InlineData("password", "$pbkdf2-sha256$i=1$c2FsdA$")]
  [InlineData("password", "$pbkdf2-sha256$i=1$c2FsdA$!!!")]
  [InlineData("password", "$pbkdf2-sha256$i=1$c2FsdA$Eg+2z/z4syxD5yJSVsT4N6hlSMkszDVIgAWYfLcL4Xs$extra")]
  public void Verify_ShouldReturnFalse_WhenPasswordOrHashIsInvalid(string? password, string? hash)
  {
    // Arrange
    var service = CreateService();

    // Act & Assert
    Assert.False(service.Verify(password, hash));
  }

  // Testa se um hash recém-gerado não precisa ser refeito
  [Fact]
  public void NeedsRehash_ShouldReturnFalse_ForCurrentParameters()
  {
    // Arrange
    var service = CreateService();
    var hash = service.Hash("senha");

    // Act & Assert
    Assert.False(service.NeedsRehash(hash));
  }

  // Testa se um hash com iterações antigas é apontado para ser refeito, mas continua validando
  [Fact]
  public void NeedsRehash_ShouldReturnTrue_WhenIterationsChanged()
  {
    // Arrange
    var oldService = CreateService();
    var newService = CreateService(options => options.Iterations = 700_000);
    var hash = oldService.Hash("senha");

    // Act & Assert
    Assert.True(newService.Verify("senha", hash));
    Assert.True(newService.NeedsRehash(hash));
    Assert.Contains("$i=700000$", newService.Hash("senha"));
  }

  // Testa se um hash com salt ou saída de tamanho diferente do configurado é apontado para ser refeito
  [Fact]
  public void NeedsRehash_ShouldReturnTrue_WhenSizesChanged()
  {
    // Arrange
    var service = CreateService();
    var bigger = CreateService(options =>
    {
      options.SaltSize = 32;
      options.HashSize = 64;
    });

    // Act & Assert
    Assert.True(service.NeedsRehash(bigger.Hash("senha")));
    Assert.True(bigger.NeedsRehash(service.Hash("senha")));
  }

  // Testa se um hash malformado ou de outro algoritmo também é apontado para ser refeito
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("plain-text")]
  [InlineData("$argon2id$v=19$m=65536,t=3,p=4$c2FsdA$Eg+2z/z4syxD5yJSVsT4N6hlSMkszDVIgAWYfLcL4Xs")]
  public void NeedsRehash_ShouldReturnTrue_WhenHashIsNotFromThisService(string? hash)
  {
    // Arrange
    var service = CreateService();

    // Act & Assert
    Assert.True(service.NeedsRehash(hash));
  }

  // Testa se opções nulas falham no construtor
  [Fact]
  public void Constructor_ShouldThrowInternalServerError_WhenOptionsAreNull()
  {
    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => new PasswordHashService(new NullOptions()));

    // Assert
    Assert.Equal("Options.NotConfigured", exception.Message);
  }

  // Testa se iterações abaixo do mínimo falham no construtor com a chave e o mínimo na mensagem
  [Fact]
  public void Constructor_ShouldThrowInternalServerError_WhenIterationsAreTooLow()
  {
    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => CreateService(options => options.Iterations = 50_000));

    // Assert
    Assert.Equal("Options.PasswordHash.IterationsTooLow;100000", exception.Message);
  }
}
