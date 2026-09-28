using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Securities.Injections;
using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Injections;

public class TooarkDependencyInjectionDataProtectionTests : IDisposable
{
  #region Helpers

  /// <summary>
  /// Senha dos certificados gerados nos testes.
  /// </summary>
  internal const string CertificatePassword = "tooark-tests";

  /// <summary>
  /// Diretório temporário do teste, removido ao final.
  /// </summary>
  private readonly string _directory = CreateTempDirectory();

  /// <summary>
  /// Remove o diretório temporário do teste.
  /// </summary>
  public void Dispose()
  {
    DeleteTempDirectory(_directory);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Cria um diretório temporário exclusivo.
  /// </summary>
  /// <returns>Caminho do diretório criado.</returns>
  internal static string CreateTempDirectory()
  {
    var path = Path.Combine(Path.GetTempPath(), $"tooark-dataprotection-{Guid.NewGuid():N}");
    Directory.CreateDirectory(path);

    return path;
  }

  /// <summary>
  /// Remove um diretório temporário, se ainda existir.
  /// </summary>
  /// <param name="path">Caminho do diretório.</param>
  internal static void DeleteTempDirectory(string path)
  {
    if (Directory.Exists(path))
    {
      Directory.Delete(path, recursive: true);
    }
  }

  /// <summary>
  /// Gera um certificado autoassinado RSA e o grava como PKCS#12 no diretório informado.
  /// </summary>
  /// <param name="directory">Diretório de destino.</param>
  /// <param name="withPrivateKey">Indica se o arquivo leva a chave privada.</param>
  /// <returns>Caminho do arquivo <c>.pfx</c> gerado.</returns>
  internal static string CreateCertificateFile(string directory, bool withPrivateKey = true)
  {
    var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
    var notAfter = DateTimeOffset.UtcNow.AddDays(30);

    // Emissor autoassinado, com chave privada
    using var issuerKey = RSA.Create(2048);
    var issuerRequest = new CertificateRequest("CN=Tooark Tests CA", issuerKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    issuerRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
    using var issuer = issuerRequest.CreateSelfSigned(notBefore, notAfter);

    // Certificado emitido pelo emissor: sai sem a chave privada, o caso de um .pfx exportado só com o público
    using var leafKey = RSA.Create(2048);
    var leafRequest = new CertificateRequest("CN=Tooark Tests", leafKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    using var leaf = leafRequest.Create(issuer, notBefore.AddHours(1), notAfter.AddHours(-1), [1, 2, 3, 4]);
    using var certificate = withPrivateKey ? leaf.CopyWithPrivateKey(leafKey) : leaf;

    var path = Path.Combine(directory, $"{Guid.NewGuid():N}.pfx");
    File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, CertificatePassword));

    return path;
  }

  /// <summary>
  /// Monta a configuração com a seção <c>DataProtection</c>.
  /// </summary>
  /// <param name="values">Valores da seção, sem o prefixo.</param>
  /// <returns>Configuração pronta para o registro.</returns>
  internal static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
  {
    var settings = values.ToDictionary(pair => $"{KeyRingOptions.Section}:{pair.Key}", pair => pair.Value);

    return new ConfigurationBuilder()
      .AddInMemoryCollection(settings)
      .Build();
  }

  /// <summary>
  /// Registra o Data Protection a partir da configuração e monta o provider.
  /// </summary>
  /// <param name="configuration">Configuração da aplicação.</param>
  /// <param name="configure">Ação opcional de configuração programática.</param>
  /// <returns>Provider de serviços.</returns>
  private static ServiceProvider BuildProvider(IConfiguration configuration, Action<KeyRingOptions>? configure = null)
  {
    var services = new ServiceCollection();
    services.AddTooarkDataProtection(configuration, configure);

    return services.BuildServiceProvider();
  }

  #endregion

  #region Registration

  // Teste para verificar que, sem a seção, o Data Protection é registrado com os padrões do ASP.NET Core
  [Fact]
  public void AddTooarkDataProtection_WithoutSection_ShouldRegisterDefaults()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = BuildConfiguration([]);

    // Act
    var result = services.AddTooarkDataProtection(configuration);

    // Assert
    Assert.Same(services, result);
    Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDataProtectionProvider));
  }

  // Teste para verificar que as opções da seção chegam ao Data Protection nativo
  [Fact]
  public void AddTooarkDataProtection_WithSection_ShouldApplyOptions()
  {
    // Arrange
    var configuration = BuildConfiguration(new()
    {
      ["ApplicationName"] = "tooark-app",
      ["KeysPath"] = _directory,
      ["KeyLifetimeDays"] = "30",
      ["DisableAutomaticKeyGeneration"] = "true"
    });

    // Act
    using var provider = BuildProvider(configuration);
    var dataProtection = provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
    var keyManagement = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

    // Assert
    Assert.Equal("tooark-app", dataProtection.ApplicationDiscriminator);
    Assert.Equal(TimeSpan.FromDays(30), keyManagement.NewKeyLifetime);
    Assert.False(keyManagement.AutoGenerateKeys);
    var repository = Assert.IsType<FileSystemXmlRepository>(keyManagement.XmlRepository);
    Assert.Equal(new DirectoryInfo(_directory).FullName, repository.Directory.FullName);
  }

  // Teste para verificar que o override programático vence a seção
  [Fact]
  public void AddTooarkDataProtection_WithConfigure_ShouldOverrideSection()
  {
    // Arrange
    var overridden = Path.Combine(_directory, "overridden");
    var configuration = BuildConfiguration(new()
    {
      ["ApplicationName"] = "from-section",
      ["KeysPath"] = _directory
    });

    // Act
    using var provider = BuildProvider(configuration, options =>
    {
      options.ApplicationName = "from-code";
      options.KeysPath = overridden;
    });
    var dataProtection = provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
    var keyManagement = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

    // Assert
    Assert.Equal("from-code", dataProtection.ApplicationDiscriminator);
    var repository = Assert.IsType<FileSystemXmlRepository>(keyManagement.XmlRepository);
    Assert.Equal(new DirectoryInfo(overridden).FullName, repository.Directory.FullName);
  }

  // Teste para verificar que o callback do builder nativo roda por último e sobrescreve as opções
  [Fact]
  public void AddTooarkDataProtection_WithConfigureDataProtection_ShouldRunLast()
  {
    // Arrange
    var fromCallback = Path.Combine(_directory, "callback");
    var configuration = BuildConfiguration(new()
    {
      ["KeysPath"] = _directory
    });

    // Act
    using var provider = BuildProvider(configuration, options =>
      options.ConfigureDataProtection = builder => builder.PersistKeysToFileSystem(new DirectoryInfo(fromCallback)));
    var keyManagement = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

    // Assert
    var repository = Assert.IsType<FileSystemXmlRepository>(keyManagement.XmlRepository);
    Assert.Equal(new DirectoryInfo(fromCallback).FullName, repository.Directory.FullName);
  }

  // Teste para verificar que opções inválidas falham no registro, e não no primeiro uso
  [Fact]
  public void AddTooarkDataProtection_WithInvalidOptions_ShouldThrowOnRegistration()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = BuildConfiguration(new()
    {
      ["KeyLifetimeDays"] = "3"
    });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkDataProtection(configuration));

    // Assert
    Assert.Contains("Options.DataProtection.KeyLifetimeTooShort;7", ex.GetErrorMessages());
  }

  #endregion

  #region Key Ring

  // Teste para verificar que as chaves são gravadas protegidas pelo certificado
  [Fact]
  public void AddTooarkDataProtection_WithCertificate_ShouldEncryptKeysAtRest()
  {
    // Arrange
    var keysPath = Path.Combine(_directory, "keys");
    var configuration = BuildConfiguration(new()
    {
      ["ApplicationName"] = "tooark-app",
      ["KeysPath"] = keysPath,
      ["CertificatePath"] = CreateCertificateFile(_directory),
      ["CertificatePassword"] = CertificatePassword
    });

    // Act
    using var provider = BuildProvider(configuration);
    provider.GetDataProtector("tests").Protect("payload");
    var keyManagement = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
    var keyFile = Assert.Single(Directory.GetFiles(keysPath, "*.xml"));

    // Assert
    Assert.IsType<CertificateXmlEncryptor>(keyManagement.XmlEncryptor);
    Assert.Contains("encryptedSecret", File.ReadAllText(keyFile));
  }

  // Teste para verificar que outra instância, com o mesmo key ring e certificado, abre o payload
  [Fact]
  public void AddTooarkDataProtection_WithSharedKeyRing_ShouldUnprotectAcrossInstances()
  {
    // Arrange
    var configuration = BuildConfiguration(new()
    {
      ["ApplicationName"] = "tooark-app",
      ["KeysPath"] = Path.Combine(_directory, "keys"),
      ["CertificatePath"] = CreateCertificateFile(_directory),
      ["CertificatePassword"] = CertificatePassword
    });

    using var first = BuildProvider(configuration);
    var payload = first.GetDataProtector("tests").Protect("payload");

    // Act
    using var second = BuildProvider(configuration);
    var result = second.GetDataProtector("tests").Unprotect(payload);

    // Assert
    Assert.Equal("payload", result);
  }

  // Teste para verificar que aplicações com nomes diferentes não abrem os payloads umas das outras
  [Fact]
  public void AddTooarkDataProtection_WithDifferentApplicationName_ShouldNotUnprotect()
  {
    // Arrange
    var keysPath = Path.Combine(_directory, "keys");
    using var first = BuildProvider(BuildConfiguration(new()
    {
      ["ApplicationName"] = "app-a",
      ["KeysPath"] = keysPath
    }));
    var payload = first.GetDataProtector("tests").Protect("payload");

    using var second = BuildProvider(BuildConfiguration(new()
    {
      ["ApplicationName"] = "app-b",
      ["KeysPath"] = keysPath
    }));
    var protector = second.GetDataProtector("tests");

    // Act & Assert
    Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(payload));
  }

  #endregion

  #region Key Storage

  /// <summary>
  /// Repositório de chaves em memória, no papel de um armazenamento externo (banco, Redis, storage).
  /// </summary>
  private sealed class InMemoryXmlRepository : IXmlRepository
  {
    /// <summary>
    /// Chaves gravadas.
    /// </summary>
    private readonly List<XElement> _elements = [];

    /// <inheritdoc/>
    public IReadOnlyCollection<XElement> GetAllElements() => [.. _elements];

    /// <inheritdoc/>
    public void StoreElement(XElement element, string friendlyName) => _elements.Add(element);
  }

  // Teste para verificar que, com a trava ligada e sem armazenamento, o host não sobe
  [Fact]
  public void AddTooarkDataProtection_WithRequiredStorageWithoutStorage_ShouldThrowOnStart()
  {
    // Arrange
    using var provider = BuildProvider(BuildConfiguration(new()
    {
      ["RequirePersistentKeyStorage"] = "true"
    }));
    var validator = provider.GetRequiredService<IStartupValidator>();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(validator.Validate);

    // Assert
    Assert.Contains("Options.DataProtection.KeyStorageNotConfigured", ex.GetErrorMessages());
  }

  // Teste para verificar que, sem host, a trava barra o primeiro uso do Data Protection
  [Fact]
  public void AddTooarkDataProtection_WithRequiredStorageWithoutStorage_ShouldThrowOnFirstUse()
  {
    // Arrange
    using var provider = BuildProvider(BuildConfiguration(new()
    {
      ["RequirePersistentKeyStorage"] = "true"
    }));

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => provider.GetDataProtector("tests").Protect("payload"));

    // Assert
    Assert.Contains("Options.DataProtection.KeyStorageNotConfigured", ex.GetErrorMessages());
  }

  // Teste para verificar que KeysPath satisfaz a trava
  [Fact]
  public void AddTooarkDataProtection_WithRequiredStorageAndKeysPath_ShouldStart()
  {
    // Arrange
    using var provider = BuildProvider(BuildConfiguration(new()
    {
      ["RequirePersistentKeyStorage"] = "true",
      ["KeysPath"] = _directory
    }));
    var validator = provider.GetRequiredService<IStartupValidator>();

    // Act
    var ex = Record.Exception(validator.Validate);

    // Assert
    Assert.Null(ex);
    Assert.Equal("payload", provider.GetDataProtector("tests").Unprotect(provider.GetDataProtector("tests").Protect("payload")));
  }

  // Teste para verificar que um armazenamento próprio, passado em ConfigureDataProtection, satisfaz a trava
  [Fact]
  public void AddTooarkDataProtection_WithRequiredStorageAndCustomRepository_ShouldStoreKeysThere()
  {
    // Arrange
    var repository = new InMemoryXmlRepository();
    using var provider = BuildProvider(BuildConfiguration(new()
    {
      ["RequirePersistentKeyStorage"] = "true"
    }), options => options.ConfigureDataProtection = builder =>
      builder.Services.Configure<KeyManagementOptions>(keyManagement => keyManagement.XmlRepository = repository));
    var validator = provider.GetRequiredService<IStartupValidator>();

    // Act
    var ex = Record.Exception(validator.Validate);
    provider.GetDataProtector("tests").Protect("payload");

    // Assert
    Assert.Null(ex);
    Assert.Single(repository.GetAllElements());
  }

  // Teste para verificar que o armazenamento registrado depois, direto no builder nativo, também satisfaz a trava
  [Fact]
  public void AddTooarkDataProtection_WithRequiredStorageAndStorageRegisteredLater_ShouldStart()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTooarkDataProtection(BuildConfiguration(new()
    {
      ["RequirePersistentKeyStorage"] = "true"
    }));
    services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(_directory));
    using var provider = services.BuildServiceProvider();
    var validator = provider.GetRequiredService<IStartupValidator>();

    // Act
    var ex = Record.Exception(validator.Validate);

    // Assert
    Assert.Null(ex);
  }

  // Teste para verificar que, sem a trava, o Data Protection segue o padrão do ASP.NET Core
  [Theory]
  [InlineData(null)]
  [InlineData("false")]
  public void AddTooarkDataProtection_WithoutRequiredStorage_ShouldKeepAspNetCoreDefaults(string? required)
  {
    // Arrange
    var values = new Dictionary<string, string?>();

    if (required is not null)
    {
      values["RequirePersistentKeyStorage"] = required;
    }

    // Act
    using var provider = BuildProvider(BuildConfiguration(values));
    var keyManagement = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

    // Assert
    Assert.Null(provider.GetService<IStartupValidator>());
    Assert.Null(keyManagement.XmlRepository);
  }

  // Teste para verificar que a última chamada define a trava, inclusive para desligá-la
  [Fact]
  public void AddTooarkDataProtection_WithRequiredStorageDisabledByLaterCall_ShouldStart()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = BuildConfiguration([]);
    services.AddTooarkDataProtection(configuration, options => options.RequirePersistentKeyStorage = true);
    services.AddTooarkDataProtection(configuration, options => options.RequirePersistentKeyStorage = false);
    using var provider = services.BuildServiceProvider();
    var validator = provider.GetRequiredService<IStartupValidator>();

    // Act
    var ex = Record.Exception(validator.Validate);

    // Assert
    Assert.Null(ex);
  }

  #endregion

  #region Key Callbacks

  /// <summary>
  /// Armazenamento próprio da aplicação, no papel de uma tabela: nome único e XML de cada chave.
  /// </summary>
  private sealed class KeyTable
  {
    /// <summary>
    /// Chaves gravadas, por nome.
    /// </summary>
    public Dictionary<string, string> Rows { get; } = [];
  }

  /// <summary>
  /// Serviço com escopo que acessa a tabela, no papel de um <c>DbContext</c>.
  /// </summary>
  /// <param name="table">Tabela compartilhada.</param>
  private sealed class KeyTableContext(KeyTable table)
  {
    /// <summary>
    /// Tabela compartilhada.
    /// </summary>
    public KeyTable Table { get; } = table;
  }

  /// <summary>
  /// Monta um provider com o Data Protection guardando as chaves na tabela pelos callbacks.
  /// </summary>
  /// <param name="table">Tabela compartilhada entre as instâncias.</param>
  /// <param name="values">Valores adicionais da seção.</param>
  /// <returns>Provider de serviços, com validação de escopo ligada.</returns>
  private static ServiceProvider BuildProviderWithCallbacks(KeyTable table, Dictionary<string, string?>? values = null)
  {
    var services = new ServiceCollection();
    services.AddSingleton(table);
    services.AddScoped<KeyTableContext>();
    services.AddTooarkDataProtection(BuildConfiguration(values ?? new() { ["ApplicationName"] = "tooark-app" }), options =>
    {
      options.ReadKeys = async provider =>
      {
        await Task.Yield();

        return [.. provider.GetRequiredService<KeyTableContext>().Table.Rows.Values];
      };
      options.WriteKey = async (provider, name, xml) =>
      {
        await Task.Yield();

        provider.GetRequiredService<KeyTableContext>().Table.Rows.Add(name, xml);
      };
    });

    return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
  }

  // Teste para verificar que a chave nova é gravada pelo callback, com nome único e o XML da chave
  [Fact]
  public void AddTooarkDataProtection_WithKeyCallbacks_ShouldWriteKeyThroughCallback()
  {
    // Arrange
    var table = new KeyTable();
    using var provider = BuildProviderWithCallbacks(table);

    // Act
    provider.GetDataProtector("tests").Protect("payload");

    // Assert
    var row = Assert.Single(table.Rows);
    Assert.StartsWith("key-", row.Key);
    Assert.StartsWith("<key ", row.Value);
  }

  // Teste para verificar que outra instância lê a chave pelo callback e abre o payload
  [Fact]
  public void AddTooarkDataProtection_WithKeyCallbacks_ShouldUnprotectAcrossInstances()
  {
    // Arrange
    var table = new KeyTable();
    using var first = BuildProviderWithCallbacks(table);
    var payload = first.GetDataProtector("tests").Protect("payload");

    // Act
    using var second = BuildProviderWithCallbacks(table);
    var result = second.GetDataProtector("tests").Unprotect(payload);

    // Assert
    Assert.Equal("payload", result);
    Assert.Single(table.Rows);
  }

  // Teste para verificar que, com certificado, o callback recebe a chave já cifrada
  [Fact]
  public void AddTooarkDataProtection_WithKeyCallbacksAndCertificate_ShouldWriteEncryptedKey()
  {
    // Arrange
    var table = new KeyTable();
    using var provider = BuildProviderWithCallbacks(table, new()
    {
      ["ApplicationName"] = "tooark-app",
      ["CertificatePath"] = CreateCertificateFile(_directory),
      ["CertificatePassword"] = CertificatePassword
    });

    // Act
    provider.GetDataProtector("tests").Protect("payload");

    // Assert
    var row = Assert.Single(table.Rows);
    Assert.Contains("encryptedSecret", row.Value);
  }

  // Teste para verificar que os callbacks satisfazem a trava do armazenamento de chaves
  [Fact]
  public void AddTooarkDataProtection_WithRequiredStorageAndKeyCallbacks_ShouldStart()
  {
    // Arrange
    using var provider = BuildProviderWithCallbacks(new KeyTable(), new()
    {
      ["RequirePersistentKeyStorage"] = "true"
    });
    var validator = provider.GetRequiredService<IStartupValidator>();

    // Act
    var ex = Record.Exception(validator.Validate);

    // Assert
    Assert.Null(ex);
  }

  // Teste para verificar que uma falha no callback de gravação chega a quem protege, e não é engolida
  [Fact]
  public void AddTooarkDataProtection_WithFailingWriteKey_ShouldThrow()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTooarkDataProtection(BuildConfiguration([]), options =>
    {
      options.ReadKeys = _ => Task.FromResult<IEnumerable<string>>([]);
      options.WriteKey = (_, _, _) => throw new InvalidOperationException("storage offline");
    });
    using var provider = services.BuildServiceProvider();

    // Act
    var ex = Record.Exception(() => provider.GetDataProtector("tests").Protect("payload"));

    // Assert
    Assert.NotNull(ex);
    Assert.Contains("storage offline", ex.ToString());
  }

  #endregion
}
