using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;

namespace Tooark.Securities.Options;

/// <summary>
/// Classe que representa as opções de configuração do key ring do Data Protection do ASP.NET Core.
/// </summary>
/// <remarks>
/// O Data Protection protege payloads de vida curta — cookie de autenticação, cookies de correlação e o
/// <c>state</c> do OpenID Connect, antiforgery, TempData — com chaves que giram sozinhas. Estas opções dizem
/// onde o key ring fica, como as chaves são protegidas em repouso e qual aplicação ele isola; nenhuma é
/// obrigatória, e o que não for informado segue o padrão do ASP.NET Core. Para dados que ficam gravados por
/// tempo indeterminado (colunas criptografadas no banco), use <c>ICryptographyService</c>: se o key ring se
/// perder, tudo o que ele protegeu fica ilegível. O nome da classe evita colidir com
/// <see cref="DataProtectionOptions"/>, do próprio ASP.NET Core.
/// </remarks>
public class KeyRingOptions
{
  #region Section

  /// <summary>
  /// Seção de configuração das opções do Data Protection.
  /// </summary>
  public const string Section = "DataProtection";

  #endregion

  #region Constants

  /// <summary>
  /// Vida útil mínima de uma chave, em dias, aceita pelo ASP.NET Core.
  /// </summary>
  public const int MinimumKeyLifetimeDays = 7;

  #endregion

  #region Properties

  /// <summary>
  /// Nome que isola o key ring da aplicação.
  /// </summary>
  /// <remarks>
  /// Instâncias com o mesmo nome e o mesmo armazenamento de chaves abrem os payloads umas das outras. Quando
  /// não é informado, o ASP.NET Core usa o caminho da aplicação, que pode variar entre instâncias.
  /// </remarks>
  public string? ApplicationName { get; set; } = null;

  /// <summary>
  /// Diretório em que as chaves são gravadas.
  /// </summary>
  /// <remarks>
  /// Em contêiner, aponte para um volume persistente e compartilhado entre as instâncias. Quando não é
  /// informado, vale o local padrão do ASP.NET Core — ou o armazenamento que a aplicação configurar em
  /// <see cref="ConfigureDataProtection"/> (Redis, Azure Blob, banco de dados).
  /// </remarks>
  public string? KeysPath { get; set; } = null;

  /// <summary>
  /// Vida útil de cada chave, em dias. Nulo mantém o padrão do ASP.NET Core (90 dias).
  /// </summary>
  public int? KeyLifetimeDays { get; set; } = null;

  /// <summary>
  /// Indica se esta aplicação deixa de gerar chaves novas, apenas lendo as existentes.
  /// </summary>
  /// <remarks>
  /// Útil quando várias aplicações compartilham o key ring e apenas uma delas deve criar e girar as chaves.
  /// </remarks>
  public bool DisableAutomaticKeyGeneration { get; set; } = false;

  /// <summary>
  /// Indica se a aplicação exige um armazenamento de chaves configurado, e falha no startup sem ele.
  /// </summary>
  /// <remarks>
  /// Sem armazenamento configurado, o ASP.NET Core grava as chaves no perfil do usuário ou apenas em memória e só
  /// registra um aviso. Em contêiner, o perfil fica dentro da instância, e as chaves morrem com ela: usuários
  /// deslogados e tudo o que foi protegido ilegível. Com <c>true</c>, a aplicação não sobe sem um armazenamento,
  /// seja <see cref="KeysPath"/>, os callbacks <see cref="ReadKeys"/> e <see cref="WriteKey"/>, um
  /// <c>PersistKeysTo*</c> em <see cref="ConfigureDataProtection"/> ou um encadeado ao
  /// <c>services.AddDataProtection()</c>. A trava confere que o armazenamento existe, não que ele sobrevive à
  /// instância: <see cref="KeysPath"/> precisa apontar para um volume persistente e compartilhado, e os callbacks,
  /// para um armazenamento fora da instância.
  /// Nulo mantém o padrão, hoje desligado; informe <c>false</c> para mantê-la desligada mesmo que o padrão mude.
  /// </remarks>
  public bool? RequirePersistentKeyStorage { get; set; } = null;

  /// <summary>
  /// Caminho do certificado PKCS#12 (<c>.pfx</c>) que protege as chaves em repouso.
  /// </summary>
  /// <remarks>
  /// O certificado precisa da chave privada, usada para abrir as chaves. Não pode ser informado junto com
  /// <see cref="CertificateThumbprint"/>.
  /// </remarks>
  public string? CertificatePath { get; set; } = null;

  /// <summary>
  /// Senha do certificado informado em <see cref="CertificatePath"/>.
  /// </summary>
  public string? CertificatePassword { get; set; } = null;

  /// <summary>
  /// Thumbprint do certificado, no repositório do sistema, que protege as chaves em repouso.
  /// </summary>
  /// <remarks>
  /// O certificado é procurado nos repositórios <c>My</c> do usuário atual e da máquina. Não pode ser
  /// informado junto com <see cref="CertificatePath"/>.
  /// </remarks>
  public string? CertificateThumbprint { get; set; } = null;

  #endregion

  #region Callbacks

  /// <summary>
  /// Callback que lê todas as chaves do armazenamento próprio da aplicação (banco, storage, vault).
  /// </summary>
  /// <remarks>
  /// Informado junto com <see cref="WriteKey"/>, no lugar de <see cref="KeysPath"/>, guarda o key ring onde a
  /// aplicação quiser, sem pacote de provedor. Devolve o XML de todas as chaves gravadas, inclusive as expiradas,
  /// que continuam abrindo o que protegeram. Recebe o provedor de serviços de um escopo criado para a chamada:
  /// serviços com escopo, como o <c>DbContext</c>, são resolvidos direto. O ASP.NET Core lê o armazenamento de
  /// forma síncrona e o Tooark aguarda a tarefa; as leituras são raras, ao carregar o key ring (no startup e a
  /// cada 24 horas) e depois de criar uma chave. Só em código.
  /// </remarks>
  public Func<IServiceProvider, Task<IEnumerable<string>>>? ReadKeys { get; set; }

  /// <summary>
  /// Callback que grava uma chave nova no armazenamento próprio da aplicação (banco, storage, vault).
  /// </summary>
  /// <remarks>
  /// Informado junto com <see cref="ReadKeys"/>. Recebe o provedor de serviços do escopo da chamada, o nome da
  /// chave (<c>key-{guid}</c>, único) e o XML da chave, já cifrado quando há certificado configurado. A gravação
  /// só insere: uma chave nunca é alterada nem apagada, e sobrescrever ou descartar chaves antigas torna ilegível o
  /// que elas protegeram. É chamado a cada chave nova, na primeira execução e a cada rotação (90 dias por padrão).
  /// Só em código.
  /// </remarks>
  public Func<IServiceProvider, string, string, Task>? WriteKey { get; set; }

  /// <summary>
  /// Callback para configuração adicional do Data Protection, executado após os padrões Tooark.
  /// </summary>
  /// <remarks>
  /// Dá acesso ao builder nativo para o que as opções não cobrem: outros armazenamentos
  /// (<c>PersistKeysToStackExchangeRedis</c>, <c>PersistKeysToAzureBlobStorage</c>,
  /// <c>PersistKeysToDbContext</c>), proteção por Azure Key Vault ou certificados antigos
  /// (<c>UnprotectKeysWithAnyCertificate</c>). Por rodar por último, sobrescreve o que as opções configuraram.
  /// </remarks>
  public Action<IDataProtectionBuilder>? ConfigureDataProtection { get; set; }

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções no startup, para que uma configuração inconsistente falhe antes da primeira chave gravada.
  /// </summary>
  /// <exception cref="InternalServerErrorException">Quando <see cref="KeyLifetimeDays"/> é menor que <see cref="MinimumKeyLifetimeDays"/>.</exception>
  /// <exception cref="InternalServerErrorException">Quando <see cref="CertificatePath"/> e <see cref="CertificateThumbprint"/> são informados juntos.</exception>
  /// <exception cref="InternalServerErrorException">Quando <see cref="CertificatePassword"/> é informado sem <see cref="CertificatePath"/>.</exception>
  /// <exception cref="InternalServerErrorException">Quando o arquivo de <see cref="CertificatePath"/> não existe.</exception>
  /// <exception cref="InternalServerErrorException">Quando apenas um entre <see cref="ReadKeys"/> e <see cref="WriteKey"/> é informado.</exception>
  /// <exception cref="InternalServerErrorException">Quando <see cref="KeysPath"/> e os callbacks de armazenamento são informados juntos.</exception>
  public void Validate()
  {
    // O ASP.NET Core recusa vidas úteis menores, mas só ao montar as opções, longe do ponto de configuração
    if (KeyLifetimeDays < MinimumKeyLifetimeDays)
    {
      throw new InternalServerErrorException($"Options.DataProtection.KeyLifetimeTooShort;{MinimumKeyLifetimeDays}");
    }

    // Leitura sem gravação perde toda chave nova; gravação sem leitura gera uma chave nova a cada início
    if ((ReadKeys is null) != (WriteKey is null))
    {
      throw new InternalServerErrorException("Options.DataProtection.KeyCallbacksIncomplete");
    }

    // Dois armazenamentos: nunca escolhe um deles silenciosamente
    if (ReadKeys is not null && !string.IsNullOrWhiteSpace(KeysPath))
    {
      throw new InternalServerErrorException("Options.DataProtection.KeyStorageAmbiguous");
    }

    // Duas fontes de certificado: nunca escolhe uma delas silenciosamente
    if (HasCertificatePath && HasCertificateThumbprint)
    {
      throw new InternalServerErrorException("Options.DataProtection.CertificateAmbiguous");
    }

    // Senha sem arquivo indica um caminho que ficou de fora, e as chaves seriam gravadas sem proteção
    if (!HasCertificatePath && !string.IsNullOrEmpty(CertificatePassword))
    {
      throw new InternalServerErrorException("Options.DataProtection.CertificatePathNotConfigured");
    }

    // Arquivo ausente: falha aqui, e não ao gravar a primeira chave
    if (HasCertificatePath && !File.Exists(CertificatePath))
    {
      throw new InternalServerErrorException($"Options.DataProtection.CertificateNotFound;{CertificatePath}");
    }
  }

  #endregion

  #region Configure Builder

  /// <summary>
  /// Aplica as opções ao builder nativo do Data Protection.
  /// </summary>
  /// <remarks>
  /// Apenas o que foi informado é aplicado; o restante segue o padrão do ASP.NET Core. O callback
  /// <see cref="ConfigureDataProtection"/> é executado por último, para que o consumidor possa sobrescrever
  /// qualquer padrão. Chame <see cref="Validate"/> antes.
  /// </remarks>
  /// <param name="builder">Builder do Data Protection.</param>
  /// <exception cref="InternalServerErrorException">Quando o certificado de <see cref="CertificatePath"/> não pode ser carregado.</exception>
  /// <exception cref="InternalServerErrorException">Quando o certificado de <see cref="CertificatePath"/> não tem chave privada.</exception>
  /// <exception cref="InternalServerErrorException">Quando o certificado de <see cref="CertificatePath"/> não usa chave RSA.</exception>
  /// <exception cref="InternalServerErrorException">Quando o certificado de <see cref="CertificateThumbprint"/> não é encontrado.</exception>
  public void ConfigureBuilder(IDataProtectionBuilder builder)
  {
    // Isolamento entre aplicações que compartilham o armazenamento de chaves
    if (!string.IsNullOrWhiteSpace(ApplicationName))
    {
      builder.SetApplicationName(ApplicationName);
    }

    // Armazenamento das chaves: diretório ou callbacks da aplicação
    if (!string.IsNullOrWhiteSpace(KeysPath))
    {
      builder.PersistKeysToFileSystem(new DirectoryInfo(KeysPath));
    }
    else if (ReadKeys is not null && WriteKey is not null)
    {
      PersistKeysToCallbacks(builder, ReadKeys, WriteKey);
    }

    // Proteção das chaves em repouso
    if (HasCertificatePath)
    {
      builder.ProtectKeysWithCertificate(LoadCertificate(CertificatePath, CertificatePassword));
    }
    else if (HasCertificateThumbprint)
    {
      ProtectKeysWithThumbprint(builder, CertificateThumbprint);
    }

    // Rotação das chaves
    if (KeyLifetimeDays is int days)
    {
      builder.SetDefaultKeyLifetime(TimeSpan.FromDays(days));
    }

    if (DisableAutomaticKeyGeneration)
    {
      builder.DisableAutomaticKeyGeneration();
    }

    // O consumidor fala por último
    ConfigureDataProtection?.Invoke(builder);
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Guarda as chaves pelos callbacks de leitura e gravação da aplicação.
  /// </summary>
  /// <remarks>
  /// O repositório precisa do provedor de serviços, que só existe depois do registro: por isso entra por um
  /// <see cref="IConfigureOptions{TOptions}"/>, o mesmo caminho do <c>PersistKeysToDbContext</c>.
  /// </remarks>
  /// <param name="builder">Builder do Data Protection.</param>
  /// <param name="read">Callback de leitura de todas as chaves.</param>
  /// <param name="write">Callback de gravação de uma chave nova.</param>
  private static void PersistKeysToCallbacks(
    IDataProtectionBuilder builder,
    Func<IServiceProvider, Task<IEnumerable<string>>> read,
    Func<IServiceProvider, string, string, Task> write
  )
  {
    builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
      new ConfigureOptions<KeyManagementOptions>(options =>
        options.XmlRepository = new CallbackXmlRepository(services, read, write)));
  }

  /// <summary>
  /// Indica se <see cref="CertificatePath"/> foi informado.
  /// </summary>
  [MemberNotNullWhen(true, nameof(CertificatePath))]
  private bool HasCertificatePath => !string.IsNullOrWhiteSpace(CertificatePath);

  /// <summary>
  /// Indica se <see cref="CertificateThumbprint"/> foi informado.
  /// </summary>
  [MemberNotNullWhen(true, nameof(CertificateThumbprint))]
  private bool HasCertificateThumbprint => !string.IsNullOrWhiteSpace(CertificateThumbprint);

  /// <summary>
  /// Carrega o certificado PKCS#12 informado em <see cref="CertificatePath"/>.
  /// </summary>
  /// <param name="path">Caminho do arquivo do certificado.</param>
  /// <param name="password">Senha do certificado.</param>
  /// <returns>Certificado com chave privada.</returns>
  /// <exception cref="InternalServerErrorException">Quando o arquivo não é um PKCS#12 válido ou a senha está errada.</exception>
  /// <exception cref="InternalServerErrorException">Quando o certificado não tem chave privada.</exception>
  /// <exception cref="InternalServerErrorException">Quando o certificado não usa chave RSA.</exception>
  private static X509Certificate2 LoadCertificate(string path, string? password)
  {
    X509Certificate2 certificate;

    try
    {
#if NET9_0_OR_GREATER
      certificate = X509CertificateLoader.LoadPkcs12FromFile(path, password);
#else
      certificate = new X509Certificate2(path, password);
#endif
    }
    catch (CryptographicException)
    {
      // Arquivo corrompido, formato errado ou senha incorreta: a mensagem não distingue os casos
      throw new InternalServerErrorException("Options.DataProtection.CertificateInvalid");
    }

    // Sem a chave privada as chaves seriam gravadas, mas nunca mais abertas
    if (!certificate.HasPrivateKey)
    {
      certificate.Dispose();

      throw new InternalServerErrorException("Options.DataProtection.CertificateWithoutPrivateKey");
    }

    // A criptografia XML das chaves só aceita RSA: outro algoritmo falharia apenas ao gravar a primeira chave
    using (var rsa = certificate.GetRSAPublicKey())
    {
      if (rsa is null)
      {
        certificate.Dispose();

        throw new InternalServerErrorException("Options.DataProtection.CertificateNotRsa");
      }
    }

    return certificate;
  }

  /// <summary>
  /// Protege as chaves com o certificado do repositório do sistema informado em <see cref="CertificateThumbprint"/>.
  /// </summary>
  /// <param name="builder">Builder do Data Protection.</param>
  /// <param name="thumbprint">Thumbprint do certificado.</param>
  /// <exception cref="InternalServerErrorException">Quando o certificado não é encontrado.</exception>
  private static void ProtectKeysWithThumbprint(IDataProtectionBuilder builder, string thumbprint)
  {
    try
    {
      builder.ProtectKeysWithCertificate(thumbprint);
    }
    catch (InvalidOperationException)
    {
      // O ASP.NET Core procura o certificado no registro e lança quando ele não existe
      throw new InternalServerErrorException($"Options.DataProtection.CertificateNotFound;{thumbprint}");
    }
  }

  #endregion
}

/// <summary>
/// Repositório de chaves que delega a leitura e a gravação aos callbacks da aplicação.
/// </summary>
/// <param name="services">Provedor de serviços raiz, usado para criar um escopo por chamada.</param>
/// <param name="read">Callback de leitura de todas as chaves.</param>
/// <param name="write">Callback de gravação de uma chave nova.</param>
internal sealed class CallbackXmlRepository(
  IServiceProvider services,
  Func<IServiceProvider, Task<IEnumerable<string>>> read,
  Func<IServiceProvider, string, string, Task> write
) : IXmlRepository
{
  /// <summary>
  /// Lê todas as chaves pelo callback de leitura.
  /// </summary>
  /// <returns>Elementos XML das chaves gravadas.</returns>
  public IReadOnlyCollection<XElement> GetAllElements()
  {
    var keys = Run(read);

    return [.. keys.Select(key => XElement.Parse(key))];
  }

  /// <summary>
  /// Grava uma chave nova pelo callback de gravação.
  /// </summary>
  /// <param name="element">Elemento XML da chave, já cifrado quando há certificado configurado.</param>
  /// <param name="friendlyName">Nome único da chave (<c>key-{guid}</c>).</param>
  public void StoreElement(XElement element, string friendlyName)
  {
    var xml = element.ToString(SaveOptions.DisableFormatting);

    Run(async provider =>
    {
      await write(provider, friendlyName, xml).ConfigureAwait(false);

      return true;
    });
  }

  /// <summary>
  /// Executa um callback num escopo próprio e aguarda o resultado.
  /// </summary>
  /// <remarks>
  /// O <see cref="IXmlRepository"/> é síncrono. A tarefa roda no pool de threads para que a espera não trave num
  /// contexto de sincronização da aplicação, e cada chamada ganha um escopo para os serviços com escopo.
  /// </remarks>
  /// <typeparam name="T">Tipo do resultado do callback.</typeparam>
  /// <param name="callback">Callback a executar.</param>
  /// <returns>Resultado do callback.</returns>
  private T Run<T>(Func<IServiceProvider, Task<T>> callback) =>
    Task.Run(async () =>
    {
      await using var scope = services.CreateAsyncScope();

      return await callback(scope.ServiceProvider).ConfigureAwait(false);
    }).GetAwaiter().GetResult();
}
