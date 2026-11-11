using Tooark.Exceptions;
using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Options;

public class PasswordHashOptionsTests
{
  // Testa se os padrões são os recomendados: 600.000 iterações, salt de 16 bytes e hash de 32 bytes
  [Fact]
  public void Defaults_ShouldMatchOwaspRecommendation()
  {
    // Act
    var options = new PasswordHashOptions();

    // Assert
    Assert.Equal("PasswordHash", PasswordHashOptions.Section);
    Assert.Equal(600_000, options.Iterations);
    Assert.Equal(16, options.SaltSize);
    Assert.Equal(32, options.HashSize);
  }

  // Testa se as opções padrão passam na validação
  [Fact]
  public void Validate_ShouldNotThrow_WithDefaults()
  {
    // Arrange
    var options = new PasswordHashOptions();

    // Act & Assert
    options.Validate();
  }

  // Testa se o mínimo de iterações é aceito e um a menos é recusado
  [Fact]
  public void Validate_ShouldEnforceMinimumIterations()
  {
    // Arrange
    var atMinimum = new PasswordHashOptions { Iterations = 100_000 };
    var belowMinimum = new PasswordHashOptions { Iterations = 99_999 };

    // Act
    atMinimum.Validate();
    var exception = Assert.Throws<InternalServerErrorException>(belowMinimum.Validate);

    // Assert
    Assert.Equal("Options.PasswordHash.IterationsTooLow;100000", exception.Message);
  }

  // Testa se o salt abaixo de 16 bytes é recusado
  [Fact]
  public void Validate_ShouldThrow_WhenSaltIsTooSmall()
  {
    // Arrange
    var options = new PasswordHashOptions { SaltSize = 8 };

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert
    Assert.Equal("Options.PasswordHash.SaltSizeTooSmall;16", exception.Message);
  }

  // Testa se o hash abaixo de 32 bytes é recusado
  [Fact]
  public void Validate_ShouldThrow_WhenHashIsTooSmall()
  {
    // Arrange
    var options = new PasswordHashOptions { HashSize = 16 };

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert
    Assert.Equal("Options.PasswordHash.HashSizeTooSmall;32", exception.Message);
  }
}
