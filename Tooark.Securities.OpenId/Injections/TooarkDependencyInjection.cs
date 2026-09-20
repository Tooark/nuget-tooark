using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Securities.OpenId.Options;

// Permite acesso interno para testes unitários
[assembly: InternalsVisibleTo("Tooark.Tests")]

namespace Tooark.Securities.OpenId.Injections;

/// <summary>
/// Classe para adicionar os serviços de OpenID Connect ao container de injeção de dependência.
/// </summary>
/// <remarks>
/// O pacote configura os handlers nativos do ASP.NET Core (<c>OpenIdConnect</c>, <c>JwtBearer</c> e cookie) com
/// os padrões Tooark — nenhum fluxo do protocolo é implementado aqui. Cada provedor é lido da subseção
/// <c>OpenId:{nome}</c> e registrado como esquema de autenticação com o mesmo nome.
/// </remarks>
public static partial class TooarkDependencyInjection
{
  #region Generic Provider

  /// <summary>
  /// Adiciona o login interativo (handler <c>OpenIdConnect</c> + cookie) de um provedor OpenID Connect genérico.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="name">Nome do provedor: subseção <c>OpenId:{nome}</c> e esquema de autenticação.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do provedor.</param>
  /// <returns>A coleção de serviços com o provedor adicionado.</returns>
  public static IServiceCollection AddTooarkOpenIdSso(
    this IServiceCollection services,
    IConfiguration configuration,
    string name,
    Action<OpenIdOptions>? configure = null
  ) => services.AddTooarkOpenIdSso<OpenIdOptions>(configuration, name, configure);

  /// <summary>
  /// Adiciona o login interativo (handler <c>OpenIdConnect</c> + cookie) de um provedor OpenID Connect com
  /// opções personalizadas — o caminho para criar um preset próprio.
  /// </summary>
  /// <typeparam name="TOptions">Tipo das opções do provedor, derivado de <see cref="OpenIdOptions"/>.</typeparam>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="name">Nome do provedor: subseção <c>OpenId:{nome}</c> e esquema de autenticação.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do provedor.</param>
  /// <returns>A coleção de serviços com o provedor adicionado.</returns>
  /// <remarks>
  /// O esquema padrão de autenticação passa a ser o cookie e o esquema padrão de desafio passa a ser este
  /// provedor, a menos que a aplicação já os tenha definido — o primeiro provedor registrado vence. Quando
  /// <see cref="OpenIdOptions.SignInScheme"/> não é informado, o cookie padrão é registrado uma única vez,
  /// compartilhado por todos os provedores.
  /// </remarks>
  /// <exception cref="InternalServerErrorException">Quando as opções do provedor são inválidas.</exception>
  public static IServiceCollection AddTooarkOpenIdSso<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    string name,
    Action<TOptions>? configure = null
  ) where TOptions : OpenIdOptions, new()
  {
    // Carrega e valida as opções (falha no startup, não no primeiro login)
    var options = BindOptions(configuration, name, configure, interactive: true);

    // Sessão: cookie padrão do pacote ou o esquema que a aplicação já registrou
    var signInScheme = options.SignInScheme ?? CookieAuthenticationDefaults.AuthenticationScheme;
    var builder = services.AddAuthentication();

    if (options.SignInScheme is null)
    {
      AddDefaultCookieOnce(services, builder);
    }

    // Esquemas padrão: só quando a aplicação ainda não definiu (o primeiro provedor registrado vence)
    services.Configure<AuthenticationOptions>(authentication =>
    {
      authentication.DefaultScheme ??= signInScheme;
      authentication.DefaultChallengeScheme ??= name;
    });

    // Handler interativo com os padrões Tooark
    builder.AddOpenIdConnect(name, options.DisplayName ?? name, options.ConfigureHandler);

    return services;
  }

  /// <summary>
  /// Adiciona a validação de token de API (handler <c>JwtBearer</c>) de um provedor OpenID Connect genérico.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="name">Nome do provedor: subseção <c>OpenId:{nome}</c> e esquema de autenticação.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do provedor.</param>
  /// <returns>A coleção de serviços com o provedor adicionado.</returns>
  public static IServiceCollection AddTooarkOpenIdBearer(
    this IServiceCollection services,
    IConfiguration configuration,
    string name,
    Action<OpenIdOptions>? configure = null
  ) => services.AddTooarkOpenIdBearer<OpenIdOptions>(configuration, name, configure);

  /// <summary>
  /// Adiciona a validação de token de API (handler <c>JwtBearer</c>) de um provedor OpenID Connect com opções
  /// personalizadas — o caminho para criar um preset próprio.
  /// </summary>
  /// <typeparam name="TOptions">Tipo das opções do provedor, derivado de <see cref="OpenIdOptions"/>.</typeparam>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="name">Nome do provedor: subseção <c>OpenId:{nome}</c> e esquema de autenticação.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções do provedor.</param>
  /// <returns>A coleção de serviços com o provedor adicionado.</returns>
  /// <remarks>
  /// O esquema padrão de autenticação passa a ser este provedor, a menos que a aplicação já o tenha definido —
  /// o primeiro provedor registrado vence.
  /// </remarks>
  /// <exception cref="InternalServerErrorException">Quando as opções do provedor são inválidas.</exception>
  public static IServiceCollection AddTooarkOpenIdBearer<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    string name,
    Action<TOptions>? configure = null
  ) where TOptions : OpenIdOptions, new()
  {
    // Carrega e valida as opções (falha no startup, não na primeira requisição)
    var options = BindOptions(configuration, name, configure, interactive: false);
    var builder = services.AddAuthentication();

    // Esquema padrão: só quando a aplicação ainda não definiu (o primeiro provedor registrado vence)
    services.Configure<AuthenticationOptions>(authentication =>
    {
      authentication.DefaultScheme ??= name;
    });

    // Handler de API com os padrões Tooark
    builder.AddJwtBearer(name, options.ConfigureHandler);

    return services;
  }

  #endregion

  #region Internal Methods

  /// <summary>
  /// Carrega as opções do provedor da subseção <c>OpenId:{nome}</c>, aplica os overrides programáticos e valida.
  /// </summary>
  /// <typeparam name="TOptions">Tipo das opções do provedor.</typeparam>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="name">Nome do provedor.</param>
  /// <param name="configure">Ação opcional de configuração programática.</param>
  /// <param name="interactive">Indica se as opções alimentam o handler interativo (true) ou o de API (false).</param>
  /// <returns>Opções carregadas e validadas.</returns>
  /// <exception cref="InternalServerErrorException">Quando o nome está vazio ou as opções são inválidas.</exception>
  internal static TOptions BindOptions<TOptions>(
    IConfiguration configuration,
    string name,
    Action<TOptions>? configure,
    bool interactive
  ) where TOptions : OpenIdOptions, new()
  {
    // O nome identifica a subseção e o esquema: sem ele não há como registrar
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new InternalServerErrorException("Options.OpenId.NameNotConfigured");
    }

    // Carrega da configuração e aplica os overrides programáticos por cima
    var options = new TOptions();
    configuration.GetSection(SectionName(name)).Bind(options);
    configure?.Invoke(options);

    // Valida com o handler em vista, para que a mensagem aponte o que falta
    options.Validate(interactive);

    return options;
  }

  /// <summary>
  /// Obtém o caminho da subseção de configuração do provedor.
  /// </summary>
  /// <param name="name">Nome do provedor.</param>
  /// <returns>Caminho no formato <c>OpenId:{nome}</c>.</returns>
  internal static string SectionName(string name) => $"{OpenIdOptions.Section}:{name}";

  /// <summary>
  /// Registra o esquema de cookie padrão uma única vez, para que vários provedores compartilhem a mesma sessão.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="builder">Builder de autenticação.</param>
  /// <remarks>
  /// Registrar o mesmo esquema duas vezes falha apenas quando as opções de autenticação são construídas — na
  /// primeira requisição, longe do ponto que causou o problema. O marcador evita a duplicidade no registro.
  /// </remarks>
  private static void AddDefaultCookieOnce(IServiceCollection services, AuthenticationBuilder builder)
  {
    // Já registrado por outro provedor deste pacote
    if (services.Any(descriptor => descriptor.ServiceType == typeof(OpenIdCookieSchemeMarker)))
    {
      return;
    }

    services.AddSingleton<OpenIdCookieSchemeMarker>();
    builder.AddCookie();
  }

  #endregion
}

/// <summary>
/// Marcador que indica que o esquema de cookie padrão já foi registrado por este pacote.
/// </summary>
internal sealed class OpenIdCookieSchemeMarker
{
}
