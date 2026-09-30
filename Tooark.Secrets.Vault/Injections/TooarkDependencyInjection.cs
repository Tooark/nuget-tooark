using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tooark.Secrets.Interfaces;
using Tooark.Secrets.Options;
using Tooark.Secrets.Vault.Options;

namespace Tooark.Secrets.Vault.Injections;

/// <summary>
/// Classe para adicionar os segredos do Vault ou do OpenBao ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o <see cref="ISecretService"/> sobre o KV v2 do Vault ou do OpenBao, com as opções lidas da seção
  /// <c>Secrets</c> e das variáveis <c>VAULT_*</c> ou <c>BAO_*</c>.
  /// </summary>
  /// <remarks>
  /// Para carregar os segredos na configuração, use o <c>AddTooarkSecretsVault</c> do <c>IConfigurationBuilder</c>.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o serviço de segredos adicionado.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkSecretsVault(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<VaultSecretsOptions>? configure = null
  )
  {
    // Carrega da configuração, completa com o ambiente e aplica os overrides programáticos por cima
    void Apply(VaultSecretsOptions options)
    {
      configuration.GetSection(SecretsOptions.Section).Bind(options);
      configure?.Invoke(options);
      options.ApplyEnvironment(Environment.GetEnvironmentVariable);
    }

    // Valida no startup, e não na primeira leitura
    var options = new VaultSecretsOptions();
    Apply(options);
    options.Validate();

    services.AddOptions<VaultSecretsOptions>().Configure(Apply);
    services.TryAddSingleton<ISecretService, VaultSecretService>();

    return services;
  }
}
