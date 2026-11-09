using System.Security.Cryptography;
using Tooark.Exceptions;
using Tooark.Securities;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;
using MEOptions = Microsoft.Extensions.Options;

namespace Tooark.Tests.Securities;

public class KeyedCryptographyServiceTests
{
  #region Test Helpers

  // Gera uma chave AES-256 em Base64
  private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

  // Opções com duas chaves identificadas e a segunda como corrente
  private static CryptographyOptions GetKeyedOptions(string algorithm = "GCM", string? legacySecret = null) => new()
  {
    Algorithm = algorithm,
    Secret = legacySecret,
    CurrentKeyId = "k2",
    Keys = new Dictionary<string, string>
    {
      ["k1"] = NewKey(),
      ["k2"] = NewKey()
    }
  };

  // Cria o serviço identificado a partir das opções
  private static KeyedCryptographyService CreateService(CryptographyOptions options) =>
    new(MEOptions.Options.Create(options));

  // Opções inválidas para cenários de erro
  private class NullOptions : MEOptions.IOptions<CryptographyOptions>
  {
    public CryptographyOptions Value => null!;
  }

  #endregion

  #region Encrypt and Decrypt

  // Testa se o valor cifrado sai com o prefixo da versão e da chave corrente, e volta ao texto original
  [Theory]
  [InlineData("GCM")]
  [InlineData("CBC")]
  public void Encrypt_ShouldPrefixCurrentKey_AndDecryptRoundTrip(string algorithm)
  {
    // Arrange
    var service = CreateService(GetKeyedOptions(algorithm));

    // Act
    var cipherText = service.Encrypt("123.456.789-00");

    // Assert
    Assert.StartsWith("tk1.k2.", cipherText);
    Assert.Equal("123.456.789-00", service.Decrypt(cipherText));
  }

  // Testa se a cifra pode ser feita com uma chave específica, e é lida pela chave do prefixo
  [Fact]
  public void Encrypt_WithKeyId_ShouldUseInformedKey()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());

    // Act
    var cipherText = service.Encrypt("texto", "k1");

    // Assert
    Assert.StartsWith("tk1.k1.", cipherText);
    Assert.Equal("k1", service.KeyIdOf(cipherText));
    Assert.Equal("texto", service.Decrypt(cipherText));
  }

  // Testa se cifrar com uma chave desconhecida é recusado com a chave de mensagem
  [Fact]
  public void Encrypt_ShouldThrowBadRequest_WhenKeyIsUnknown()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());

    // Act
    var exception = Assert.Throws<BadRequestException>(() => service.Encrypt("texto", "k9"));

    // Assert
    Assert.Equal("Cryptography.KeyNotFound;k9", exception.Message);
  }

  // Testa se texto nulo é recusado como no serviço de chave única
  [Fact]
  public void Encrypt_ShouldThrowBadRequest_WhenPlainTextIsNull()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());

    // Act
    var exception = Assert.Throws<BadRequestException>(() => service.Encrypt(null!));

    // Assert
    Assert.Equal("Cryptography.PlainTextNotProvided", exception.Message);
  }

  // Testa se cada valor cifrado é diferente (nonce aleatório), mas todos decifram
  [Fact]
  public void Encrypt_ShouldProduceDifferentCipherTexts_ForSamePlainText()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());

    // Act
    var first = service.Encrypt("texto");
    var second = service.Encrypt("texto");

    // Assert
    Assert.NotEqual(first, second);
    Assert.Equal("texto", service.Decrypt(first));
    Assert.Equal("texto", service.Decrypt(second));
  }

  // Testa se um valor cifrado com uma chave que saiu da configuração é recusado com a chave de mensagem
  [Fact]
  public void Decrypt_ShouldThrowBadRequest_WhenKeyWasRemoved()
  {
    // Arrange
    var options = GetKeyedOptions();
    var cipherText = CreateService(options).Encrypt("texto", "k1");
    options.Keys.Remove("k1");
    var service = CreateService(options);

    // Act
    var exception = Assert.Throws<BadRequestException>(() => service.Decrypt(cipherText));

    // Assert
    Assert.Equal("Cryptography.KeyNotFound;k1", exception.Message);
  }

  // Testa se um valor legado, sem prefixo, é aberto com a chave única configurada em Secret
  [Fact]
  public void Decrypt_ShouldOpenLegacyValue_WithLegacySecret()
  {
    // Arrange
    var legacy = new CryptographyService(MEOptions.Options.Create(new CryptographyOptions
    {
      Algorithm = "GCM",
      Secret = "mysupersecretkey1234567890ABCDEF"
    }));
    var legacyCipherText = legacy.Encrypt("valor antigo");
    var service = CreateService(GetKeyedOptions(legacySecret: "mysupersecretkey1234567890ABCDEF"));

    // Act & Assert
    Assert.Equal("valor antigo", service.Decrypt(legacyCipherText));
    Assert.Null(service.KeyIdOf(legacyCipherText));
    Assert.False(service.IsCurrent(legacyCipherText));
  }

  // Testa se um valor legado é recusado quando a chave única não está mais configurada
  [Fact]
  public void Decrypt_ShouldThrowBadRequest_WhenLegacyValueHasNoLegacyKey()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());
    var legacyLike = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    // Act
    var exception = Assert.Throws<BadRequestException>(() => service.Decrypt(legacyLike));

    // Assert
    Assert.Equal("Cryptography.LegacyKeyNotConfigured", exception.Message);
  }

  // Testa se um prefixo malformado (outra versão, id inválido ou payload vazio) é texto cifrado inválido
  [Theory]
  [InlineData("tk2.k1.AbCd")]
  [InlineData("tk1.k.1.AbCd")]
  [InlineData("tk1.k1.")]
  [InlineData("tk1.k1")]
  [InlineData("abc.def")]
  public void Decrypt_ShouldThrowBadRequest_WhenPrefixIsMalformed(string cipherText)
  {
    // Arrange
    var service = CreateService(GetKeyedOptions(legacySecret: "mysupersecretkey1234567890ABCDEF"));

    // Act
    var exception = Assert.Throws<BadRequestException>(() => service.Decrypt(cipherText));

    // Assert
    Assert.Equal("Cryptography.InvalidCipherText", exception.Message);
  }

  // Testa se texto nulo é recusado na leitura
  [Fact]
  public void Decrypt_ShouldThrowBadRequest_WhenCipherTextIsNull()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());

    // Act & Assert
    Assert.Equal("Cryptography.CipherTextNotProvided", Assert.Throws<BadRequestException>(() => service.Decrypt(null!)).Message);
    Assert.Equal("Cryptography.CipherTextNotProvided", Assert.Throws<BadRequestException>(() => service.KeyIdOf(null!)).Message);
  }

  // Testa se um payload adulterado é recusado como texto cifrado inválido, sem revelar detalhes
  [Fact]
  public void Decrypt_ShouldThrowBadRequest_WhenPayloadIsTampered()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());
    var cipherText = service.Encrypt("texto");
    var tampered = cipherText[..^4] + (cipherText.EndsWith("AAAA") ? "BBBB" : "AAAA");

    // Act
    var exception = Assert.Throws<BadRequestException>(() => service.Decrypt(tampered));

    // Assert
    Assert.Equal("Cryptography.InvalidCipherText", exception.Message);
  }

  #endregion

  #region Rotation

  // Testa se a chave corrente e a lista de chaves refletem a configuração, na ordem
  [Fact]
  public void CurrentKeyIdAndKeyIds_ShouldReflectOptions()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());

    // Act & Assert
    Assert.Equal("k2", service.CurrentKeyId);
    Assert.Equal(["k1", "k2"], service.KeyIds);
  }

  // Testa se IsCurrent distingue a chave corrente das demais
  [Fact]
  public void IsCurrent_ShouldBeTrueOnlyForCurrentKey()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());

    // Act & Assert
    Assert.True(service.IsCurrent(service.Encrypt("texto")));
    Assert.False(service.IsCurrent(service.Encrypt("texto", "k1")));
  }

  // Testa se ReEncrypt leva um valor da chave antiga para a corrente, mantendo o texto
  [Fact]
  public void ReEncrypt_ShouldMoveValueToCurrentKey()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());
    var old = service.Encrypt("texto", "k1");

    // Act
    var renewed = service.ReEncrypt(old);

    // Assert
    Assert.StartsWith("tk1.k2.", renewed);
    Assert.True(service.IsCurrent(renewed));
    Assert.Equal("texto", service.Decrypt(renewed));
  }

  // Testa se ReEncrypt devolve o próprio valor quando ele já está na chave corrente
  [Fact]
  public void ReEncrypt_ShouldReturnSameValue_WhenAlreadyCurrent()
  {
    // Arrange
    var service = CreateService(GetKeyedOptions());
    var current = service.Encrypt("texto");

    // Act & Assert
    Assert.Same(current, service.ReEncrypt(current));
  }

  // Testa se ReEncrypt também migra um valor legado para a chave corrente
  [Fact]
  public void ReEncrypt_ShouldMigrateLegacyValue()
  {
    // Arrange
    var legacy = new CryptographyService(MEOptions.Options.Create(new CryptographyOptions
    {
      Algorithm = "GCM",
      Secret = "mysupersecretkey1234567890ABCDEF"
    }));
    var service = CreateService(GetKeyedOptions(legacySecret: "mysupersecretkey1234567890ABCDEF"));

    // Act
    var renewed = service.ReEncrypt(legacy.Encrypt("valor antigo"));

    // Assert
    Assert.Equal("k2", service.KeyIdOf(renewed));
    Assert.Equal("valor antigo", service.Decrypt(renewed));
  }

  // Testa a rotação completa: a chave nova vira corrente, os valores antigos seguem legíveis e são recifrados
  [Fact]
  public void Rotation_ShouldKeepOldValuesReadable_UntilReEncrypted()
  {
    // Arrange
    var options = GetKeyedOptions();
    options.CurrentKeyId = "k1";
    var before = CreateService(options);
    var stored = before.Encrypt("texto");

    // Act: rotaciona para k2
    options.CurrentKeyId = "k2";
    var after = CreateService(options);
    var migrated = after.ReEncrypt(stored);

    // Assert
    Assert.Equal("texto", after.Decrypt(stored));
    Assert.False(after.IsCurrent(stored));
    Assert.True(after.IsCurrent(migrated));
    Assert.Equal("texto", after.Decrypt(migrated));
  }

  // Testa se o serviço identificado atende à interface de chave única
  [Fact]
  public void Service_ShouldImplementICryptographyService()
  {
    // Arrange
    ICryptographyService service = CreateService(GetKeyedOptions());

    // Act & Assert
    Assert.Equal("texto", service.Decrypt(service.Encrypt("texto")));
  }

  #endregion

  #region Options Validation

  // Testa se opções nulas falham no construtor
  [Fact]
  public void Constructor_ShouldThrowInternalServerError_WhenOptionsAreNull()
  {
    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => new KeyedCryptographyService(new NullOptions()));

    // Assert
    Assert.Equal("Options.NotConfigured", exception.Message);
  }

  // Testa se a falta de chaves identificadas falha no startup
  [Fact]
  public void Constructor_ShouldThrow_WhenKeysAreEmpty()
  {
    // Arrange
    var options = new CryptographyOptions { Algorithm = "GCM", CurrentKeyId = "k1" };

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => CreateService(options));

    // Assert
    Assert.Equal("Options.Cryptography.Keys.NotConfigured", exception.Message);
  }

  // Testa se a chave corrente ausente falha no startup
  [Fact]
  public void Constructor_ShouldThrow_WhenCurrentKeyIdIsMissing()
  {
    // Arrange
    var options = GetKeyedOptions();
    options.CurrentKeyId = null;

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => CreateService(options));

    // Assert
    Assert.Equal("Options.Cryptography.CurrentKeyId.NotConfigured", exception.Message);
  }

  // Testa se a chave corrente fora da coleção falha no startup, apontando o identificador
  [Fact]
  public void Constructor_ShouldThrow_WhenCurrentKeyIdIsNotAmongKeys()
  {
    // Arrange
    var options = GetKeyedOptions();
    options.CurrentKeyId = "k9";

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => CreateService(options));

    // Assert
    Assert.Equal("Options.Cryptography.CurrentKeyId.NotFound;k9", exception.Message);
  }

  // Testa se um identificador com ponto ou espaço é recusado, porque iria para o prefixo do valor
  [Theory]
  [InlineData("k.1")]
  [InlineData("k 1")]
  [InlineData("k/1")]
  public void Constructor_ShouldThrow_WhenKeyIdIsInvalid(string keyId)
  {
    // Arrange
    var options = GetKeyedOptions();
    options.Keys[keyId] = NewKey();

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => CreateService(options));

    // Assert
    Assert.Equal($"Options.Cryptography.KeyId.Invalid;{keyId}", exception.Message);
  }

  // Testa se uma chave que não é Base64 de 32 bytes é recusada, apontando o identificador
  [Theory]
  [InlineData("not-base64!!")]
  [InlineData("c2hvcnQ=")]
  [InlineData("")]
  public void Constructor_ShouldThrow_WhenKeyIsInvalid(string key)
  {
    // Arrange
    var options = GetKeyedOptions();
    options.Keys["k3"] = key;

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => CreateService(options));

    // Assert
    Assert.Equal("Options.Cryptography.Keys.Invalid;k3", exception.Message);
  }

  // Testa se a validação estática usada no registro aplica as mesmas regras
  [Fact]
  public void ValidateOptions_ShouldPass_ForValidOptions()
  {
    // Arrange
    var options = GetKeyedOptions();

    // Act & Assert
    KeyedCryptographyService.ValidateOptions(options);
  }

  #endregion
}
