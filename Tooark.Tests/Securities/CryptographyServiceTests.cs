using System.Security.Cryptography;
using Tooark.Exceptions;
using Tooark.Securities;
using Tooark.Securities.Options;
using MEOptions = Microsoft.Extensions.Options;

namespace Tooark.Tests.Securities;

public class CryptographyServiceTests
{
  #region Test Helpers

  // Opções padrão para testes com GCM (padrão)
  private static CryptographyOptions GetGcmOptions() => new()
  {
    Algorithm = "GCM",
    Secret = "mysupersecretkey1234567890ABCDEF"
  };

  // Opções para testes com CBC
  private static CryptographyOptions GetCbcOptions() => new()
  {
    Algorithm = "CBC",
    Secret = "mysupersecretkey1234567890ABCDEF"
  };

  // Opções para testes com CBCZeroIv (legado)
  private static CryptographyOptions GetCbcZeroIvOptions() => new()
  {
    Algorithm = "CBCUnsafe",
    Secret = "mysupersecretkey1234567890ABCDEF"
  };

  // Opções com SecretBase64 configurado (chave AES de 32 bytes)
  private static CryptographyOptions GetOptionsWithBase64Key()
  {
    var key = RandomNumberGenerator.GetBytes(32);
    return new CryptographyOptions
    {
      Algorithm = "GCM",
      Secret = "fallback-secret",
      SecretBase64 = Convert.ToBase64String(key)
    };
  }

  // Opções com SecretBase64 de tamanho inválido (não 32 bytes)
  private static CryptographyOptions GetOptionsWithInvalidBase64Key()
  {
    var key = RandomNumberGenerator.GetBytes(16); // 16 bytes em vez de 32
    return new CryptographyOptions
    {
      Algorithm = "GCM",
      Secret = "mysupersecretkey1234567890ABCDEF",
      SecretBase64 = Convert.ToBase64String(key)
    };
  }

  // Opções inválidas para cenários de erro
  private class NullOptions : MEOptions.IOptions<CryptographyOptions>
  {
    public CryptographyOptions Value => null!;
  }

  #endregion

  #region Constructor Tests

  // Testa se o construtor lança exceções apropriadas para opções inválidas
  [Fact]
  public void Constructor_WithNullOptions_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var options = new NullOptions();

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new CryptographyService(options));
    Assert.Contains("Options.NotConfigured", ex.GetErrorMessages());
  }

  // Testa se o construtor lança exceção quando o segredo não está configurado
  [Fact]
  public void Constructor_WithoutSecret_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var cryptoOptions = new CryptographyOptions
    {
      Algorithm = "GCM",
      Secret = null
    };
    var options = MEOptions.Options.Create(cryptoOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new CryptographyService(options));
    Assert.Contains("Options.Cryptography.SecretNotConfigured", ex.GetErrorMessages());
  }

  // Testa se o construtor cria uma instância corretamente com opções válidas
  [Fact]
  public void Constructor_WithValidOptions_ShouldCreateInstance()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());

    // Act
    var service = new CryptographyService(options);

    // Assert
    Assert.NotNull(service);
  }

  // Testa se o construtor cria uma instância corretamente com SecretBase64
  [Fact]
  public void Constructor_WithSecretBase64_ShouldCreateInstance()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetOptionsWithBase64Key());

    // Act
    var service = new CryptographyService(options);

    // Assert
    Assert.NotNull(service);
  }

  #endregion

  #region Encrypt Tests - GCM

  // Testa a criptografia com o algoritmo GCM
  [Fact]
  public void Encrypt_GCM_WithValidText_ShouldReturnEncryptedBase64()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);
    var plainText = "Hello, World!";

    // Act
    var encrypted = service.Encrypt(plainText);

    // Assert
    Assert.NotNull(encrypted);
    Assert.NotEmpty(encrypted);
    Assert.NotEqual(plainText, encrypted);
  }

  // Testa a criptografia com texto nulo
  [Fact]
  public void Encrypt_GCM_WithNullText_ShouldThrowBadRequestException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);

    // Act & Assert
    var ex = Assert.Throws<BadRequestException>(() => service.Encrypt(null!));
    Assert.Contains("Cryptography.PlainTextNotProvided", ex.GetErrorMessages());
  }

  // Testa a criptografia com texto vazio
  [Fact]
  public void Encrypt_GCM_WithEmptyText_ShouldReturnEncryptedBase64()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);
    var plainText = "";

    // Act
    var encrypted = service.Encrypt(plainText);

    // Assert
    Assert.NotNull(encrypted);
    Assert.NotEmpty(encrypted);
  }

  // Testa a criptografia do mesmo texto duas vezes para garantir resultados diferentes
  [Fact]
  public void Encrypt_GCM_SameTextTwice_ShouldReturnDifferentResults()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);
    var plainText = "Hello, World!";

    // Act
    var encrypted1 = service.Encrypt(plainText);
    var encrypted2 = service.Encrypt(plainText);

    // Assert - Due to random nonce, results should differ
    Assert.NotEqual(encrypted1, encrypted2);
  }

  #endregion

  #region Encrypt Tests - CBC

  // Testa a criptografia com o algoritmo CBC
  [Fact]
  public void Encrypt_CBC_WithValidText_ShouldReturnEncryptedBase64()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);
    var plainText = "Hello, World!";

    // Act
    var encrypted = service.Encrypt(plainText);

    // Assert
    Assert.NotNull(encrypted);
    Assert.NotEmpty(encrypted);
    Assert.NotEqual(plainText, encrypted);
  }

  // Testa a criptografia com texto nulo
  [Fact]
  public void Encrypt_CBC_WithNullText_ShouldThrowBadRequestException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);

    // Act & Assert
    var ex = Assert.Throws<BadRequestException>(() => service.Encrypt(null!));
    Assert.Contains("Cryptography.PlainTextNotProvided", ex.GetErrorMessages());
  }

  // Testa a criptografia com texto vazio
  [Fact]
  public void Encrypt_CBC_WithEmptyText_ShouldReturnEncryptedBase64()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);
    var plainText = "";

    // Act
    var encrypted = service.Encrypt(plainText);

    // Assert
    Assert.NotNull(encrypted);
    Assert.NotEmpty(encrypted);
  }

  // Testa a criptografia do mesmo texto duas vezes para garantir resultados diferentes
  [Fact]
  public void Encrypt_CBC_SameTextTwice_ShouldReturnDifferentResults()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);
    var plainText = "Hello, World!";

    // Act
    var encrypted1 = service.Encrypt(plainText);
    var encrypted2 = service.Encrypt(plainText);

    // Assert - Due to random IV, results should differ
    Assert.NotEqual(encrypted1, encrypted2);
  }

  #endregion

  #region Encrypt Tests - Default Algorithm

  // Testa a criptografia com algoritmo desconhecido (deve usar GCM por padrão)
  [Fact]
  public void Encrypt_DefaultAlgorithm_ShouldUseGCM()
  {
    // Arrange
    var cryptoOptions = new CryptographyOptions
    {
      Algorithm = "UNKNOWN",
      Secret = "mysupersecretkey1234567890ABCDEF"
    };
    var options = MEOptions.Options.Create(cryptoOptions);
    var service = new CryptographyService(options);
    var plainText = "Hello, World!";

    // Act
    var encrypted = service.Encrypt(plainText);

    // Assert
    Assert.NotNull(encrypted);
    Assert.NotEmpty(encrypted);
  }

  #endregion

  #region Decrypt Tests - GCM

  // Testa a descriptografia com o algoritmo GCM
  [Fact]
  public void Decrypt_GCM_WithValidCipherText_ShouldReturnOriginalText()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);
    var plainText = "Hello, World!";
    var encrypted = service.Encrypt(plainText);

    // Act
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  // Testa a descriptografia com texto cifrado nulo
  [Fact]
  public void Decrypt_GCM_WithNullCipherText_ShouldThrowBadRequestException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);

    // Act & Assert
    var ex = Assert.Throws<BadRequestException>(() => service.Decrypt(null!));
    Assert.Contains("Cryptography.CipherTextNotProvided", ex.GetErrorMessages());
  }

  // Testa a descriptografia com texto cifrado muito curto
  [Fact]
  public void Decrypt_GCM_WithTooShortCipherText_ShouldThrowBadRequestException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);
    // GCM requires at least 12 (nonce) + 16 (tag) = 28 bytes
    var shortData = Convert.ToBase64String(new byte[20]);

    // Act & Assert
    var ex = Assert.Throws<BadRequestException>(() => service.Decrypt(shortData));
    Assert.Contains("Cryptography.InvalidCipherText", ex.GetErrorMessages());
  }

  // Testa a descriptografia com texto cifrado vazio
  [Fact]
  public void Decrypt_GCM_WithEmptyEncrypted_ShouldReturnEmptyString()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);
    var plainText = "";
    var encrypted = service.Encrypt(plainText);

    // Act
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  // Testa a descriptografia com dados adulterados
  [Fact]
  public void Decrypt_GCM_WithTamperedData_ShouldThrowException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);
    var plainText = "Hello, World!";
    var encrypted = service.Encrypt(plainText);

    // Tamper with the encrypted data
    var bytes = Convert.FromBase64String(encrypted);
    bytes[^1] ^= 0xFF; // Flip bits in the last byte
    var tampered = Convert.ToBase64String(bytes);

    // Act & Assert - dados adulterados retornam sempre a mesma exceção (sem distinção de causa)
    var ex = Assert.Throws<BadRequestException>(() => service.Decrypt(tampered));
    Assert.Contains("Cryptography.InvalidCipherText", ex.GetErrorMessages());
  }

  #endregion

  #region Decrypt Tests - CBC

  // Testa a descriptografia com o algoritmo CBC
  [Fact]
  public void Decrypt_CBC_WithValidCipherText_ShouldReturnOriginalText()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);
    var plainText = "Hello, World!";
    var encrypted = service.Encrypt(plainText);

    // Act
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  // Testa a descriptografia com texto cifrado nulo
  [Fact]
  public void Decrypt_CBC_WithNullCipherText_ShouldThrowBadRequestException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);

    // Act & Assert
    var ex = Assert.Throws<BadRequestException>(() => service.Decrypt(null!));
    Assert.Contains("Cryptography.CipherTextNotProvided", ex.GetErrorMessages());
  }

  // Testa a descriptografia com texto cifrado muito curto
  [Fact]
  public void Decrypt_CBC_WithTooShortCipherText_ShouldThrowBadRequestException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);
    // CBC requires more than 16 bytes (IV size)
    var shortData = Convert.ToBase64String(new byte[16]);

    // Act & Assert
    var ex = Assert.Throws<BadRequestException>(() => service.Decrypt(shortData));
    Assert.Contains("Cryptography.InvalidCipherText", ex.GetErrorMessages());
  }

  // Testa a descriptografia com texto cifrado vazio
  [Fact]
  public void Decrypt_CBC_WithEmptyEncrypted_ShouldReturnEmptyString()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);
    var plainText = "";
    var encrypted = service.Encrypt(plainText);

    // Act
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  // Testa a descriptografia com dados adulterados
  [Fact]
  public void Decrypt_CBC_WithTamperedData_ShouldThrowException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);
    var plainText = "Hello, World!";
    var encrypted = service.Encrypt(plainText);

    // Tamper with the encrypted data
    var bytes = Convert.FromBase64String(encrypted);
    bytes[^1] ^= 0xFF; // Flip bits in the last byte
    var tampered = Convert.ToBase64String(bytes);

    // Act & Assert - dados adulterados retornam sempre a mesma exceção (sem distinção de causa)
    var ex = Assert.Throws<BadRequestException>(() => service.Decrypt(tampered));
    Assert.Contains("Cryptography.InvalidCipherText", ex.GetErrorMessages());
  }  

  #endregion

  #region Decrypt Tests - CBCZeroIv (Legacy)

  // Testa a descriptografia de dados legados criptografados com CBC e IV zero
  [Fact]
  public void Decrypt_CBCZeroIv_WithValidLegacyData_ShouldReturnOriginalText()
  {
    // Arrange - Create legacy encrypted data using CBC with zero IV
    var secret = "mysupersecretkey1234567890ABCDEF";
    var plainText = "Hello, Legacy World!";

    // Encrypt using zero IV (simulating legacy data)
    using var aes = Aes.Create();
    aes.KeySize = 256;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    aes.Key = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
    aes.IV = new byte[16]; // Zero IV

    using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
    var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
    var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    var legacyEncrypted = Convert.ToBase64String(cipherBytes);

    // Setup service with CBCZeroIv - must use exact same algorithm
    var cryptoOptions = new CryptographyOptions
    {
      Algorithm = "CBCUnsafe",
      Secret = secret
    };
    var options = MEOptions.Options.Create(cryptoOptions);
    var service = new CryptographyService(options);

    // Act
    var decrypted = service.Decrypt(legacyEncrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  // Testa a descriptografia com texto cifrado nulo
  [Fact]
  public void Decrypt_CBCZeroIv_WithNullCipherText_ShouldThrowBadRequestException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcZeroIvOptions());
    var service = new CryptographyService(options);

    // Act & Assert
    var ex = Assert.Throws<BadRequestException>(() => service.Decrypt(null!));
    Assert.Contains("Cryptography.CipherTextNotProvided", ex.GetErrorMessages());
  }

  // Testa que criptografar com CBCUnsafe lança exceção (modo apenas para descriptografia de dados legados)
  [Fact]
  public void Encrypt_CBCZeroIv_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcZeroIvOptions());
    var service = new CryptographyService(options);

    // Act & Assert - antes caía silenciosamente em GCM, quebrando o round-trip com o Decrypt legado
    var ex = Assert.Throws<InternalServerErrorException>(() => service.Encrypt("any text"));
    Assert.Contains("Options.Cryptography.AlgorithmDecryptOnly;CBCUnsafe", ex.GetErrorMessages());
  }

  #endregion

  #region Decrypt Tests - Default Algorithm

  // Testa a descriptografia com algoritmo desconhecido (deve usar GCM por padrão)
  [Fact]
  public void Decrypt_DefaultAlgorithm_ShouldUseGCM()
  {
    // Arrange - encrypt with GCM
    var gcmOptions = MEOptions.Options.Create(GetGcmOptions());
    var gcmService = new CryptographyService(gcmOptions);
    var plainText = "Hello, World!";
    var encrypted = gcmService.Encrypt(plainText);

    // Create service with unknown algorithm (should default to GCM)
    var unknownOptions = new CryptographyOptions
    {
      Algorithm = "UNKNOWN",
      Secret = "mysupersecretkey1234567890ABCDEF"
    };
    var options = MEOptions.Options.Create(unknownOptions);
    var service = new CryptographyService(options);

    // Act
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  #endregion

  #region SecretBase64 Key Tests

  // Testa a criptografia e descriptografia com SecretBase64
  [Fact]
  public void Encrypt_Decrypt_WithSecretBase64_ShouldWork()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetOptionsWithBase64Key());
    var service = new CryptographyService(options);
    var plainText = "Hello with Base64 Key!";

    // Act
    var encrypted = service.Encrypt(plainText);
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  // Testa que SecretBase64 de tamanho inválido falha no startup (nunca troca de chave silenciosamente)
  [Fact]
  public void Constructor_WithInvalidSizeBase64Key_ShouldThrowInternalServerErrorException()
  {
    // Arrange - chave de 16 bytes em vez dos 32 exigidos pelo AES-256
    var options = MEOptions.Options.Create(GetOptionsWithInvalidBase64Key());

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new CryptographyService(options));
    Assert.Contains("Options.Cryptography.SecretBase64InvalidSize", ex.GetErrorMessages());
  }

  // Testa que SecretBase64 com Base64 inválido falha no startup
  [Fact]
  public void Constructor_WithMalformedBase64Key_ShouldThrowInternalServerErrorException()
  {
    // Arrange
    var cryptoOptions = new CryptographyOptions
    {
      Algorithm = "GCM",
      SecretBase64 = "not-valid-base64!!!"
    };
    var options = MEOptions.Options.Create(cryptoOptions);

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => new CryptographyService(options));
    Assert.Contains("Options.Cryptography.SecretBase64Invalid", ex.GetErrorMessages());
  }

  // Testa que SecretBase64 sozinho (sem Secret) é uma configuração válida
  [Fact]
  public void Encrypt_Decrypt_WithSecretBase64Only_ShouldWork()
  {
    // Arrange - apenas SecretBase64, sem Secret
    var key = RandomNumberGenerator.GetBytes(32);
    var cryptoOptions = new CryptographyOptions
    {
      Algorithm = "GCM",
      SecretBase64 = Convert.ToBase64String(key)
    };
    var options = MEOptions.Options.Create(cryptoOptions);
    var service = new CryptographyService(options);
    var plainText = "Hello with Base64-only key!";

    // Act
    var encrypted = service.Encrypt(plainText);
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  // Testa que SecretBase64 tem prioridade sobre Secret quando ambos são informados
  [Fact]
  public void Decrypt_WithBase64Priority_ShouldUseBase64Key()
  {
    // Arrange - dois serviços com o mesmo SecretBase64 e Secrets diferentes: devem interoperar
    var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var service1 = new CryptographyService(MEOptions.Options.Create(new CryptographyOptions
    {
      Algorithm = "GCM",
      Secret = "secret-one",
      SecretBase64 = key
    }));
    var service2 = new CryptographyService(MEOptions.Options.Create(new CryptographyOptions
    {
      Algorithm = "GCM",
      Secret = "secret-two",
      SecretBase64 = key
    }));
    var plainText = "Base64 key has priority!";

    // Act
    var decrypted = service2.Decrypt(service1.Encrypt(plainText));

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  #endregion

  #region Round-Trip Tests

  // Testa a criptografia GCM e descriptografia em um ciclo completo para vários textos
  [Theory]
  [InlineData("")]
  [InlineData("a")]
  [InlineData("Hello, World!")]
  [InlineData("Special chars: áéíóú ñ © ® ™")]
  [InlineData("Unicode: 你好世界 🌍🚀")]
  [InlineData("Long text with multiple lines\nLine 2\nLine 3")]
  public void Encrypt_Decrypt_GCM_RoundTrip_ShouldReturnOriginal(string plainText)
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);

    // Act
    var encrypted = service.Encrypt(plainText);
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  // Testa a criptografia CBC e descriptografia em um ciclo completo para vários textos
  [Theory]
  [InlineData("")]
  [InlineData("a")]
  [InlineData("Hello, World!")]
  [InlineData("Special chars: áéíóú ñ © ® ™")]
  [InlineData("Unicode: 你好世界 🌍🚀")]
  [InlineData("Long text with multiple lines\nLine 2\nLine 3")]
  public void Encrypt_Decrypt_CBC_RoundTrip_ShouldReturnOriginal(string plainText)
  {
    // Arrange
    var options = MEOptions.Options.Create(GetCbcOptions());
    var service = new CryptographyService(options);

    // Act
    var encrypted = service.Encrypt(plainText);
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  // Testa a criptografia e descriptografia de um texto muito grande
  [Fact]
  public void Encrypt_Decrypt_LargeText_ShouldWork()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);
    var plainText = new string('A', 100000); // 100KB of text

    // Act
    var encrypted = service.Encrypt(plainText);
    var decrypted = service.Decrypt(encrypted);

    // Assert
    Assert.Equal(plainText, decrypted);
  }

  #endregion

  #region Cross-Algorithm Tests

  // Testa a descriptografia de dados criptografados com GCM usando CBC (deve falhar)
  [Fact]
  public void Decrypt_CBC_WithGCMEncrypted_ShouldFail()
  {
    // Arrange
    var gcmOptions = MEOptions.Options.Create(GetGcmOptions());
    var gcmService = new CryptographyService(gcmOptions);
    var plainText = "Hello, World!";
    var encrypted = gcmService.Encrypt(plainText);

    var cbcOptions = MEOptions.Options.Create(GetCbcOptions());
    var cbcService = new CryptographyService(cbcOptions);

    // Act & Assert - should fail because formats are different
    Assert.Throws<BadRequestException>(() => cbcService.Decrypt(encrypted));
  }

  // Testa a descriptografia de dados criptografados com CBC usando GCM (deve falhar)
  [Fact]
  public void Decrypt_GCM_WithCBCEncrypted_ShouldFail()
  {
    // Arrange
    var cbcOptions = MEOptions.Options.Create(GetCbcOptions());
    var cbcService = new CryptographyService(cbcOptions);
    var plainText = "Hello, World!";
    var encrypted = cbcService.Encrypt(plainText);

    var gcmOptions = MEOptions.Options.Create(GetGcmOptions());
    var gcmService = new CryptographyService(gcmOptions);

    // Act & Assert - should fail because formats are different
    Assert.Throws<BadRequestException>(() => gcmService.Decrypt(encrypted));
  }

  #endregion

  #region Different Secret Tests

  // Testa a descriptografia com um segredo diferente (deve falhar)
  [Fact]
  public void Decrypt_WithDifferentSecret_ShouldFail()
  {
    // Arrange
    var options1 = MEOptions.Options.Create(new CryptographyOptions
    {
      Algorithm = "GCM",
      Secret = "mysupersecretkey1234567890ABCDEF"
    });
    var service1 = new CryptographyService(options1);
    var plainText = "Hello, World!";
    var encrypted = service1.Encrypt(plainText);

    var options2 = MEOptions.Options.Create(new CryptographyOptions
    {
      Algorithm = "GCM",
      Secret = "differentsecretek1234567890ABCDE"
    });
    var service2 = new CryptographyService(options2);

    // Act & Assert - should fail because secrets are different
    Assert.Throws<BadRequestException>(() => service2.Decrypt(encrypted));
  }

  #endregion

  #region Invalid Base64 Tests

  // Testa a descriptografia com uma string Base64 inválida (uniformizada como texto criptografado inválido)
  [Fact]
  public void Decrypt_WithInvalidBase64_ShouldThrowBadRequestException()
  {
    // Arrange
    var options = MEOptions.Options.Create(GetGcmOptions());
    var service = new CryptographyService(options);
    var invalidBase64 = "not-valid-base64!!!";

    // Act & Assert
    var ex = Assert.Throws<BadRequestException>(() => service.Decrypt(invalidBase64));
    Assert.Contains("Cryptography.InvalidCipherText", ex.GetErrorMessages());
  }

  #endregion
}
