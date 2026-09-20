using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Securities.OpenId.Options;

namespace Tooark.Securities.OpenId.Injections;

/// <summary>
/// Classe para adicionar o preset de SSO com Microsoft Entra ID ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o login interativo (handler <c>OpenIdConnect</c> + cookie) com Microsoft Entra ID.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="name">Nome do provedor: subseção <c>OpenId:{nome}</c> e esquema de autenticação. Padrão: <c>Entra</c>.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do provedor.</param>
  /// <returns>A coleção de serviços com o provedor adicionado.</returns>
  /// <remarks>
  /// Preset sobre <see cref="AddTooarkOpenIdSso{TOptions}"/> com <see cref="EntraOptions"/>: Authority derivada
  /// do tenant, emissores v1 e v2, <c>preferred_username</c> como nome e <c>roles</c> como papel. Sem SDK do
  /// provedor.
  /// </remarks>
  public static IServiceCollection AddTooarkEntraSso(
    this IServiceCollection services,
    IConfiguration configuration,
    string name = EntraOptions.DefaultName,
    Action<EntraOptions>? configure = null
  ) => services.AddTooarkOpenIdSso(configuration, name, configure);

  /// <summary>
  /// Adiciona a validação de token de API (handler <c>JwtBearer</c>) emitido pelo Microsoft Entra ID.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="name">Nome do provedor: subseção <c>OpenId:{nome}</c> e esquema de autenticação. Padrão: <c>Entra</c>.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do provedor.</param>
  /// <returns>A coleção de serviços com o provedor adicionado.</returns>
  /// <remarks>
  /// Preset sobre <see cref="AddTooarkOpenIdBearer{TOptions}"/> com <see cref="EntraOptions"/>: aceita tokens v1 e
  /// v2 do tenant com destinatário <c>{ClientId}</c> ou <c>api://{ClientId}</c>.
  /// </remarks>
  public static IServiceCollection AddTooarkEntraBearer(
    this IServiceCollection services,
    IConfiguration configuration,
    string name = EntraOptions.DefaultName,
    Action<EntraOptions>? configure = null
  ) => services.AddTooarkOpenIdBearer(configuration, name, configure);
}
