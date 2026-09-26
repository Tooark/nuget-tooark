using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Securities.Options;
using Tooark.Tests.Securities.Injections;

namespace Tooark.Tests.Securities.Options;

public class KeyRingOptionsTests : IDisposable
{
  #region Helpers

  /// <summary>
  /// Diretório temporário do teste, removido ao final.
  /// </summary>
  private readonly string _directory = TooarkDependencyInjectionDataProtectionTests.CreateTempDirectory();

  /// <summary>
  /// Remove o diretório temporário do teste.
  /// </summary>
  public void Dispose()
  {
    TooarkDependencyInjectionDataProtectionTests.DeleteTempDirectory(_directory);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Aplica as opções a um builder novo, como faz o registro.
  /// </summary>
  /// <param name="options">Opções do key ring.</param>
  /// <returns>Opções de gerenciamento de chaves resultantes.</returns>
  private static KeyManagementOptions Apply(KeyRingOptions options)
  {
    var services = new ServiceCollection();
    options.ConfigureBuilder(services.AddDataProtection());

    using var provider = services.BuildServiceProvider();

    return provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
  }

  #endregion

  #region Defaults

  // Teste para verificar que nenhuma opção é obrigatória
  [Fact]
  public void Defaults_ShouldBeEmptyAndValid()
  {
    // Arrange
    var options = new KeyRingOptions();

    // Act
    var ex = Record.Exception(options.Validate);

    // Assert
    Assert.Null(ex);
    Assert.Equal("DataProtection", KeyRingOptions.Section);
    Assert.Null(options.ApplicationName);
    Assert.Null(options.KeysPath);
    Assert.Null(options.KeyLifetimeDays);
    Assert.False(options.DisableAutomaticKeyGeneration);
    Assert.Null(options.CertificatePath);
    Assert.Null(options.CertificatePassword);
    Assert.Null(options.CertificateThumbprint);
    Assert.Null(options.ConfigureDataProtection);
  }

  // Teste para verificar que, sem opções, o builder segue os padrões do ASP.NET Core
  [Fact]
  public void ConfigureBuilder_WithDefaults_ShouldKeepAspNetCoreDefaults()
  {
    // Arrange
    var options = new KeyRingOptions();

    // Act
    var keyManagement = Apply(options);

    // Assert
    Assert.Equal(TimeSpan.FromDays(90), keyManagement.NewKeyLifetime);
    Assert.True(keyManagement.AutoGenerateKeys);
  }

  #endregion

  #region Validate

  // Teste para verificar que a vida útil abaixo do mínimo falha
  [Theory]
  [InlineData(-1)]
  [InlineData(0)]
  [InlineData(6)]
  public void Validate_WithKeyLifetimeTooShort_ShouldThrow(int days)
  {
    // Arrange
    var options = new KeyRingOptions { KeyLifetimeDays = days };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert
    Assert.Contains("Options.DataProtection.KeyLifetimeTooShort;7", ex.GetErrorMessages());
  }

  // Teste para verificar que a vida útil mínima é aceita e aplicada
  [Fact]
  public void Validate_WithMinimumKeyLifetime_ShouldBeApplied()
  {
    // Arrange
    var options = new KeyRingOptions { KeyLifetimeDays = KeyRingOptions.MinimumKeyLifetimeDays };

    // Act
    options.Validate();
    var keyManagement = Apply(options);

    // Assert
    Assert.Equal(TimeSpan.FromDays(7), keyManagement.NewKeyLifetime);
  }

  // Teste para verificar que caminho e thumbprint juntos falham
  [Fact]
  public void Validate_WithCertificatePathAndThumbprint_ShouldThrow()
  {
    // Arrange
    var options = new KeyRingOptions
    {
      CertificatePath = TooarkDependencyInjectionDataProtectionTests.CreateCertificateFile(_directory),
      CertificateThumbprint = "0123456789ABCDEF0123456789ABCDEF01234567"
    };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert
    Assert.Contains("Options.DataProtection.CertificateAmbiguous", ex.GetErrorMessages());
  }

  // Teste para verificar que a senha sem o caminho do certificado falha
  [Fact]
  public void Validate_WithPasswordWithoutPath_ShouldThrow()
  {
    // Arrange
    var options = new KeyRingOptions { CertificatePassword = "secret" };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert
    Assert.Contains("Options.DataProtection.CertificatePathNotConfigured", ex.GetErrorMessages());
  }

  // Teste para verificar que um arquivo de certificado inexistente falha
  [Fact]
  public void Validate_WithMissingCertificateFile_ShouldThrow()
  {
    // Arrange
    var path = Path.Combine(_directory, "missing.pfx");
    var options = new KeyRingOptions { CertificatePath = path };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert
    Assert.Contains($"Options.DataProtection.CertificateNotFound;{path}", ex.GetErrorMessages());
  }

  #endregion

  #region Certificate

  // Teste para verificar que uma senha errada falha ao carregar o certificado
  [Fact]
  public void ConfigureBuilder_WithWrongCertificatePassword_ShouldThrow()
  {
    // Arrange
    var options = new KeyRingOptions
    {
      CertificatePath = TooarkDependencyInjectionDataProtectionTests.CreateCertificateFile(_directory),
      CertificatePassword = "wrong-password"
    };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Apply(options));

    // Assert
    Assert.Contains("Options.DataProtection.CertificateInvalid", ex.GetErrorMessages());
  }

  // Teste para verificar que um certificado sem chave privada falha
  [Fact]
  public void ConfigureBuilder_WithCertificateWithoutPrivateKey_ShouldThrow()
  {
    // Arrange
    var options = new KeyRingOptions
    {
      CertificatePath = TooarkDependencyInjectionDataProtectionTests.CreateCertificateFile(_directory, withPrivateKey: false),
      CertificatePassword = TooarkDependencyInjectionDataProtectionTests.CertificatePassword
    };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Apply(options));

    // Assert
    Assert.Contains("Options.DataProtection.CertificateWithoutPrivateKey", ex.GetErrorMessages());
  }

  // Teste para verificar que um certificado sem chave RSA falha, em vez de falhar ao gravar a primeira chave
  [Fact]
  public void ConfigureBuilder_WithNonRsaCertificate_ShouldThrow()
  {
    // Arrange
    using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    var request = new CertificateRequest("CN=Tooark Tests", key, HashAlgorithmName.SHA256);
    using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    var path = Path.Combine(_directory, "ecdsa.pfx");
    File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, TooarkDependencyInjectionDataProtectionTests.CertificatePassword));

    var options = new KeyRingOptions
    {
      CertificatePath = path,
      CertificatePassword = TooarkDependencyInjectionDataProtectionTests.CertificatePassword
    };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Apply(options));

    // Assert
    Assert.Contains("Options.DataProtection.CertificateNotRsa", ex.GetErrorMessages());
  }

  // Teste para verificar que um thumbprint ausente do repositório do sistema falha
  [Fact]
  public void ConfigureBuilder_WithUnknownThumbprint_ShouldThrow()
  {
    // Arrange
    const string thumbprint = "0123456789ABCDEF0123456789ABCDEF01234567";
    var options = new KeyRingOptions { CertificateThumbprint = thumbprint };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => Apply(options));

    // Assert
    Assert.Contains($"Options.DataProtection.CertificateNotFound;{thumbprint}", ex.GetErrorMessages());
  }

  #endregion
}
