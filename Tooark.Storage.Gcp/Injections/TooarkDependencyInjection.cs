using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Storage.Gcp.Options;
using Tooark.Storage.Interfaces;
using Tooark.Storage.Options;

namespace Tooark.Storage.Gcp.Injections;

/// <summary>
/// Classe para adicionar o storage no Google Cloud ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o <see cref="IStorageService"/> sobre o Google Cloud Storage, com as opções lidas da seção
  /// <c>Storage</c>.
  /// </summary>
  /// <remarks>
  /// O cliente é montado a partir das opções e registrado como <see cref="StorageClient"/>. Se a aplicação já
  /// registrou um <see cref="StorageClient"/>, o dela é usado nas operações, e a credencial das opções vale só para
  /// assinar URLs. As opções são validadas aqui, no registro; a credencial é carregada no primeiro uso.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o storage adicionado.</returns>
  /// <exception cref="InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkStorageGcp(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<GcpStorageOptions>? configure = null
  )
  {
    // Carrega da configuração e aplica os overrides programáticos por cima
    void Apply(GcpStorageOptions options)
    {
      configuration.GetSection(StorageOptions.Section).Bind(options);
      configure?.Invoke(options);
    }

    // Valida no startup, e não na primeira operação
    var options = new GcpStorageOptions();
    Apply(options);
    options.Validate();

    services.AddOptions<GcpStorageOptions>().Configure(Apply);

    // A credencial é lida uma vez, no primeiro uso: o Application Default Credentials consulta o ambiente
    var credential = new Lazy<GoogleCredential>(() => CreateCredential(options));

    services.TryAddSingleton(_ => StorageClient.Create(credential.Value));
    services.TryAddSingleton<IStorageService>(provider => new GcpStorageService(
      provider.GetRequiredService<StorageClient>(),
      new Lazy<UrlSigner>(() => UrlSigner.FromCredential(credential.Value)),
      provider.GetRequiredService<IOptions<GcpStorageOptions>>()
    ));

    return services;
  }

  /// <summary>
  /// Carrega a credencial a partir das opções.
  /// </summary>
  /// <param name="options">As opções já validadas.</param>
  /// <returns>A credencial da conta de serviço, ou a do Application Default Credentials.</returns>
  /// <exception cref="InternalServerErrorException">
  /// Quando a chave informada não é de uma conta de serviço válida, ou quando não há chave nas opções nem credencial
  /// no ambiente.
  /// </exception>
  internal static GoogleCredential CreateCredential(GcpStorageOptions options)
  {
    // Sem chave nas opções, vale a credencial do ambiente
    if (string.IsNullOrWhiteSpace(options.CredentialsJson) && string.IsNullOrWhiteSpace(options.CredentialsPath))
    {
      try
      {
        return GoogleCredential.GetApplicationDefault();
      }
      catch (InvalidOperationException e)
      {
        throw new InternalServerErrorException("Options.Storage.Gcp.CredentialsUnavailable", e);
      }
    }

    try
    {
      // Só conta de serviço: carregar qualquer tipo de credencial de um JSON externo é o que o Google desaconselha
      var credential = string.IsNullOrWhiteSpace(options.CredentialsJson)
        ? CredentialFactory.FromFile<ServiceAccountCredential>(options.CredentialsPath!)
        : CredentialFactory.FromJson<ServiceAccountCredential>(options.CredentialsJson);

      return credential.ToGoogleCredential();
    }
    catch (Exception e)
    {
      // JSON malformado, tipo de credencial diferente ou chave privada ilegível: a biblioteca lança tipos variados,
      // inclusive do serializador que ela usa por dentro, e todos significam a mesma coisa aqui
      throw new InternalServerErrorException("Options.Storage.Gcp.CredentialsInvalid", e);
    }
  }
}
