using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tooark.Secrets.Aws.Options;
using Tooark.Secrets.Interfaces;
using Tooark.Secrets.Options;

namespace Tooark.Secrets.Aws.Injections;

/// <summary>
/// Classe para adicionar os segredos da AWS ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o <see cref="ISecretService"/> sobre o AWS Secrets Manager, com as opções lidas da seção
  /// <c>Secrets</c>.
  /// </summary>
  /// <remarks>
  /// O cliente é montado a partir das opções e registrado como <see cref="IAmazonSecretsManager"/>. Se a aplicação já
  /// registrou um, o dela é usado. Para carregar segredos e parâmetros na configuração, use o
  /// <c>AddTooarkSecretsAws</c> do <c>IConfigurationBuilder</c>.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o serviço de segredos adicionado.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkSecretsAws(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<AwsSecretsOptions>? configure = null
  )
  {
    // Carrega da configuração e aplica os overrides programáticos por cima
    void Apply(AwsSecretsOptions options)
    {
      configuration.GetSection(SecretsOptions.Section).Bind(options);
      configure?.Invoke(options);
    }

    // Valida no startup, e não na primeira leitura
    var options = new AwsSecretsOptions();
    Apply(options);
    options.Validate();

    services.AddOptions<AwsSecretsOptions>().Configure(Apply);

    // O cliente é thread-safe e caro de criar: uma instância atende a aplicação
    services.TryAddSingleton<IAmazonSecretsManager>(_ => AwsClients.CreateSecretsManager(options));
    services.TryAddSingleton<ISecretService, AwsSecretService>();

    return services;
  }
}
