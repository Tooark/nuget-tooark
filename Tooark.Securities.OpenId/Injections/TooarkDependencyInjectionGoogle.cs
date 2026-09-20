using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Securities.OpenId.Options;

namespace Tooark.Securities.OpenId.Injections;

/// <summary>
/// Classe para adicionar o preset de SSO com Google ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o login interativo (handler <c>OpenIdConnect</c> + cookie) com Google.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="name">Nome do provedor: subseção <c>OpenId:{nome}</c> e esquema de autenticação. Padrão: <c>Google</c>.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do provedor.</param>
  /// <returns>A coleção de serviços com o provedor adicionado.</returns>
  /// <remarks>
  /// Preset sobre <see cref="AddTooarkOpenIdSso{TOptions}"/> com <see cref="GoogleOptions"/>: Authority do Google,
  /// as duas formas de emissor, <c>email</c> como nome e restrição opcional a um domínio do Google Workspace.
  /// Sem SDK do provedor.
  /// </remarks>
  public static IServiceCollection AddTooarkGoogleSso(
    this IServiceCollection services,
    IConfiguration configuration,
    string name = GoogleOptions.DefaultName,
    Action<GoogleOptions>? configure = null
  ) => services.AddTooarkOpenIdSso(configuration, name, configure);

  /// <summary>
  /// Adiciona a validação de token de API (handler <c>JwtBearer</c>) emitido pelo Google.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="name">Nome do provedor: subseção <c>OpenId:{nome}</c> e esquema de autenticação. Padrão: <c>Google</c>.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do provedor.</param>
  /// <returns>A coleção de serviços com o provedor adicionado.</returns>
  /// <remarks>
  /// Preset sobre <see cref="AddTooarkOpenIdBearer{TOptions}"/> com <see cref="GoogleOptions"/>. Valida o
  /// <c>id_token</c> do Google (JWT com <c>aud</c> igual ao ClientId); o access token do Google é opaco e não
  /// passa por este handler.
  /// </remarks>
  public static IServiceCollection AddTooarkGoogleBearer(
    this IServiceCollection services,
    IConfiguration configuration,
    string name = GoogleOptions.DefaultName,
    Action<GoogleOptions>? configure = null
  ) => services.AddTooarkOpenIdBearer(configuration, name, configure);
}
