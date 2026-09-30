using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Tooark.Secrets.Gcp.Clients;
using Tooark.Secrets.Gcp.Options;
using Tooark.Secrets.Interfaces;
using Tooark.Secrets.Options;

namespace Tooark.Secrets.Gcp.Injections;

/// <summary>
/// Classe para adicionar os segredos do Google Cloud ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o <see cref="ISecretService"/> sobre o Google Cloud Secret Manager, com as opções lidas da seção
  /// <c>Secrets</c>.
  /// </summary>
  /// <remarks>
  /// O cliente do Secret Manager é criado no primeiro uso, com o Application Default Credentials. Se a aplicação já
  /// registrou um <see cref="SecretManagerServiceClient"/>, o dela é usado. Para carregar segredos e parâmetros na
  /// configuração, use o <c>AddTooarkSecretsGcp</c> do <c>IConfigurationBuilder</c>.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o serviço de segredos adicionado.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkSecretsGcp(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<GcpSecretsOptions>? configure = null
  )
  {
    // Carrega da configuração e aplica os overrides programáticos por cima
    void Apply(GcpSecretsOptions options)
    {
      configuration.GetSection(SecretsOptions.Section).Bind(options);
      configure?.Invoke(options);
    }

    // Valida no startup, e não na primeira leitura
    var options = new GcpSecretsOptions();
    Apply(options);
    options.Validate();

    services.AddOptions<GcpSecretsOptions>().Configure(Apply);

    // Um cliente registrado pela aplicação vale; sem ele, o cliente é criado no primeiro uso
    services.TryAddSingleton<ISecretService>(provider => new GcpSecretService(
      new GcpSecretsClient(provider.GetService<SecretManagerServiceClient>()),
      provider.GetRequiredService<IOptions<GcpSecretsOptions>>(),
      provider.GetService<TimeProvider>()
    ));

    return services;
  }
}
