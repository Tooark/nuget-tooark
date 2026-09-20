using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Tooark.Exceptions;

namespace Tooark.Securities.OpenId.Options;

/// <summary>
/// Classe que representa as opções de configuração de um provedor OpenID Connect.
/// </summary>
/// <remarks>
/// Cada provedor é lido da subseção <c>OpenId:{nome}</c> da configuração, e o nome também identifica o
/// esquema de autenticação registrado no ASP.NET Core. As mesmas opções alimentam o handler interativo
/// (<c>OpenIdConnect</c>, login pelo navegador) e o handler de API (<c>JwtBearer</c>, validação de token).
/// O pacote apenas configura os handlers nativos do ASP.NET Core: nenhum fluxo do protocolo é implementado aqui.
/// Para um provedor com convenções próprias, herde desta classe e sobrescreva os membros virtuais, como fazem
/// <see cref="EntraOptions"/> e <see cref="GoogleOptions"/>.
/// </remarks>
public class OpenIdOptions
{
  #region Section

  /// <summary>
  /// Seção raiz de configuração dos provedores OpenID Connect. Cada provedor fica em <c>OpenId:{nome}</c>.
  /// </summary>
  public const string Section = "OpenId";

  #endregion

  #region Constants

  /// <summary>
  /// Escopo obrigatório do OpenID Connect, garantido em toda requisição de autorização.
  /// </summary>
  public const string OpenIdScope = "openid";

  /// <summary>
  /// Escopos padrão quando <see cref="Scopes"/> não é informado: <c>openid</c>, <c>profile</c> e <c>email</c>.
  /// </summary>
  public static readonly string[] DefaultScopes = ["openid", "profile", "email"];

  /// <summary>
  /// Caminho padrão em que o provedor devolve o código de autorização.
  /// </summary>
  public const string DefaultCallbackPath = "/signin-oidc";

  /// <summary>
  /// Caminho padrão em que o provedor devolve o navegador após o logout.
  /// </summary>
  public const string DefaultSignedOutCallbackPath = "/signout-callback-oidc";

  /// <summary>
  /// Claim padrão usada como nome do usuário (<c>User.Identity.Name</c>).
  /// </summary>
  public const string DefaultNameClaimType = "name";

  /// <summary>
  /// Claim padrão usada como papel do usuário (<c>User.IsInRole</c>).
  /// </summary>
  public const string DefaultRoleClaimType = "roles";

  /// <summary>
  /// Tolerância padrão de relógio, em segundos, na validação de expiração do token.
  /// </summary>
  public const int DefaultClockSkewSeconds = 300;

  #endregion

  #region Properties

  /// <summary>
  /// Nome de exibição do provedor, usado nas listas de provedores externos de login. Padrão: o nome do esquema.
  /// </summary>
  public string? DisplayName { get; set; }

  /// <summary>
  /// Endereço do emissor (issuer) do provedor. O documento de descoberta é lido de
  /// <c>{Authority}/.well-known/openid-configuration</c>.
  /// </summary>
  /// <remarks>
  /// Obrigatório no provedor genérico, salvo quando <see cref="MetadataAddress"/> é informado.
  /// Os presets derivam o valor quando ele não é informado.
  /// </remarks>
  public virtual string? Authority { get; set; }

  /// <summary>
  /// Endereço explícito do documento de descoberta, para provedores que não o publicam no caminho padrão.
  /// </summary>
  public string? MetadataAddress { get; set; }

  /// <summary>
  /// Identificador da aplicação registrado no provedor. Obrigatório.
  /// </summary>
  public string? ClientId { get; set; }

  /// <summary>
  /// Segredo da aplicação registrado no provedor.
  /// </summary>
  /// <remarks>
  /// Usado apenas pelo handler interativo, na troca do código de autorização. No provedor genérico é opcional,
  /// para clientes públicos que dependem somente do PKCE; os presets de Entra e Google o exigem, porque esses
  /// provedores não trocam o código de uma aplicação web sem o segredo.
  /// </remarks>
  public string? ClientSecret { get; set; }

  /// <summary>
  /// Escopos solicitados ao provedor. Padrão: <see cref="DefaultScopes"/>.
  /// </summary>
  /// <remarks>
  /// O escopo <c>openid</c> é sempre incluído, mesmo quando omitido aqui. Use <see cref="ResolveScopes"/>
  /// para obter a lista efetiva.
  /// </remarks>
  public string[]? Scopes { get; set; }

  /// <summary>
  /// Indica se o handler interativo usa PKCE na troca do código de autorização. Padrão: true.
  /// </summary>
  /// <remarks>
  /// O fluxo é sempre <c>code</c> (authorization code); PKCE é a proteção recomendada pelo OAuth 2.1 e
  /// deve ficar desligado apenas para provedores que ainda não o suportam.
  /// </remarks>
  public bool UsePkce { get; set; } = true;

  /// <summary>
  /// Caminho em que o provedor devolve o código de autorização. Padrão: <see cref="DefaultCallbackPath"/>.
  /// </summary>
  /// <remarks>
  /// Deve ser registrado no provedor como redirect URI, na forma <c>https://{host}{CallbackPath}</c>.
  /// </remarks>
  public string CallbackPath { get; set; } = DefaultCallbackPath;

  /// <summary>
  /// Caminho em que o provedor devolve o navegador após o logout. Padrão: <see cref="DefaultSignedOutCallbackPath"/>.
  /// </summary>
  public string SignedOutCallbackPath { get; set; } = DefaultSignedOutCallbackPath;

  /// <summary>
  /// Esquema em que a sessão é persistida após o login interativo.
  /// </summary>
  /// <remarks>
  /// Quando não informado, o pacote registra o esquema de cookie padrão do ASP.NET Core (<c>Cookies</c>) uma única
  /// vez, compartilhado por todos os provedores. Informe o nome de um esquema já registrado pela aplicação para
  /// reutilizá-lo — por exemplo, um cookie com <c>LoginPath</c> ou expiração personalizados.
  /// </remarks>
  public string? SignInScheme { get; set; }

  /// <summary>
  /// Indica se os tokens recebidos do provedor são guardados na sessão. Padrão: false.
  /// </summary>
  /// <remarks>
  /// Ligue apenas quando a aplicação precisa do access token para chamar outras APIs em nome do usuário.
  /// No handler interativo os tokens vão para o cookie, o que aumenta seu tamanho.
  /// </remarks>
  public bool SaveTokens { get; set; } = false;

  /// <summary>
  /// Indica se o handler interativo consulta o endpoint de userinfo para complementar as claims. Padrão: false.
  /// </summary>
  public bool GetClaimsFromUserInfoEndpoint { get; set; } = false;

  /// <summary>
  /// Exige HTTPS no endereço do documento de descoberta. Padrão: true.
  /// </summary>
  /// <remarks>
  /// Desligue somente em desenvolvimento, contra um provedor local. Quando ligado, <see cref="Authority"/> e
  /// <see cref="MetadataAddress"/> com <c>http://</c> são rejeitados no startup.
  /// </remarks>
  public bool RequireHttpsMetadata { get; set; } = true;

  /// <summary>
  /// Indica se as claims do token são renomeadas para os tipos legados do .NET (ex.: <c>sub</c> vira
  /// <c>ClaimTypes.NameIdentifier</c>). Padrão: false.
  /// </summary>
  /// <remarks>
  /// Desligado, as claims mantêm os nomes do OpenID Connect (<c>sub</c>, <c>email</c>, <c>name</c>...), que é o
  /// padrão Tooark. Ligue apenas para código legado que consulta os tipos <c>ClaimTypes.*</c>.
  /// </remarks>
  public bool MapInboundClaims { get; set; } = false;

  /// <summary>
  /// Claim usada como nome do usuário (<c>User.Identity.Name</c>). Padrão: <see cref="DefaultNameClaimType"/>.
  /// </summary>
  public string NameClaimType { get; set; } = DefaultNameClaimType;

  /// <summary>
  /// Claim usada como papel do usuário (<c>User.IsInRole</c>). Padrão: <see cref="DefaultRoleClaimType"/>.
  /// </summary>
  public string RoleClaimType { get; set; } = DefaultRoleClaimType;

  /// <summary>
  /// Emissores aceitos na validação do token, além do emissor publicado no documento de descoberta.
  /// </summary>
  /// <remarks>
  /// Quando não informado, apenas o emissor do documento de descoberta é aceito. Os presets podem derivar
  /// emissores adicionais; use <see cref="ResolveValidIssuers"/> para obter a lista efetiva.
  /// </remarks>
  public string[]? ValidIssuers { get; set; }

  /// <summary>
  /// Destinatários (audiences) aceitos na validação do token.
  /// </summary>
  /// <remarks>
  /// No handler interativo o destinatário é sempre o <see cref="ClientId"/>; esta lista apenas o complementa.
  /// No handler de API, quando não informada, apenas o <see cref="ClientId"/> é aceito. Os presets podem
  /// derivar destinatários adicionais; use <see cref="ResolveValidAudiences"/> para obter a lista efetiva.
  /// </remarks>
  public string[]? ValidAudiences { get; set; }

  /// <summary>
  /// Tolerância de relógio, em segundos, na validação de expiração do token. Padrão: <see cref="DefaultClockSkewSeconds"/>.
  /// </summary>
  public int ClockSkewSeconds { get; set; } = DefaultClockSkewSeconds;

  /// <summary>
  /// Valor do parâmetro <c>prompt</c> enviado ao provedor no login interativo (ex.: <c>login</c>, <c>select_account</c>).
  /// </summary>
  public string? Prompt { get; set; }

  #endregion

  #region Callbacks

  /// <summary>
  /// Callback para configuração adicional do handler interativo, executado após os padrões Tooark.
  /// </summary>
  /// <remarks>
  /// Ao substituir <see cref="OpenIdConnectOptions.Events"/> por completo, componha com os handlers já
  /// registrados — os presets podem depender deles.
  /// </remarks>
  public Action<OpenIdConnectOptions>? ConfigureOpenIdConnect { get; set; }

  /// <summary>
  /// Callback para configuração adicional do handler de API, executado após os padrões Tooark.
  /// </summary>
  /// <remarks>
  /// Ao substituir <see cref="JwtBearerOptions.Events"/> por completo, componha com os handlers já
  /// registrados — os presets podem depender deles.
  /// </remarks>
  public Action<JwtBearerOptions>? ConfigureJwtBearer { get; set; }

  #endregion

  #region Resolve Methods

  /// <summary>
  /// Obtém os escopos efetivos: os configurados ou os padrão, sem repetição e sempre com <c>openid</c>.
  /// </summary>
  /// <returns>Lista de escopos solicitados ao provedor.</returns>
  public string[] ResolveScopes()
  {
    // Usa os escopos configurados, ou os padrão quando nada foi informado
    var source = Scopes is { Length: > 0 } ? Scopes : DefaultScopes;

    // Normaliza: remove vazios, espaços nas pontas e repetições
    var scopes = source
      .Where(scope => !string.IsNullOrWhiteSpace(scope))
      .Select(scope => scope.Trim())
      .Distinct(StringComparer.Ordinal)
      .ToList();

    // O escopo openid é o que caracteriza uma requisição OpenID Connect
    if (!scopes.Contains(OpenIdScope, StringComparer.Ordinal))
    {
      scopes.Insert(0, OpenIdScope);
    }

    return [.. scopes];
  }

  /// <summary>
  /// Obtém os emissores efetivos aceitos na validação do token, além do publicado no documento de descoberta.
  /// </summary>
  /// <returns>Lista de emissores, ou nulo para aceitar apenas o emissor do documento de descoberta.</returns>
  public virtual string[]? ResolveValidIssuers() => ValidIssuers is { Length: > 0 } ? ValidIssuers : null;

  /// <summary>
  /// Obtém os destinatários efetivos aceitos na validação do token pelo handler de API.
  /// </summary>
  /// <returns>Lista de destinatários; por padrão, apenas o <see cref="ClientId"/>.</returns>
  public virtual string[] ResolveValidAudiences() => ValidAudiences is { Length: > 0 } ? ValidAudiences : [ClientId!];

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções no startup, para que uma configuração incompleta falhe antes do primeiro login.
  /// </summary>
  /// <param name="interactive">Indica se as opções alimentam o handler interativo (true) ou o de API (false).</param>
  /// <exception cref="InternalServerErrorException">Quando <see cref="ClientId"/> não está configurado.</exception>
  /// <exception cref="InternalServerErrorException">Quando nem <see cref="Authority"/> nem <see cref="MetadataAddress"/> estão configurados.</exception>
  /// <exception cref="InternalServerErrorException">Quando <see cref="Authority"/> ou <see cref="MetadataAddress"/> não é uma URL absoluta, ou usa HTTP com <see cref="RequireHttpsMetadata"/> ligado.</exception>
  /// <exception cref="InternalServerErrorException">Quando <see cref="ClockSkewSeconds"/> é negativo.</exception>
  /// <exception cref="InternalServerErrorException">Quando, no handler interativo, <see cref="CallbackPath"/> ou <see cref="SignedOutCallbackPath"/> não começa com "/".</exception>
  public virtual void Validate(bool interactive)
  {
    // Sem o ClientId nenhum dos handlers consegue identificar a aplicação
    if (string.IsNullOrWhiteSpace(ClientId))
    {
      throw new InternalServerErrorException("Options.OpenId.ClientIdNotConfigured");
    }

    // O documento de descoberta vem da Authority ou de um endereço explícito
    if (string.IsNullOrWhiteSpace(Authority) && string.IsNullOrWhiteSpace(MetadataAddress))
    {
      throw new InternalServerErrorException("Options.OpenId.AuthorityNotConfigured");
    }

    // Endereços precisam ser absolutos e, salvo em desenvolvimento, HTTPS
    if (!string.IsNullOrWhiteSpace(Authority) && !IsValidEndpoint(Authority))
    {
      throw new InternalServerErrorException($"Options.OpenId.Authority.Invalid;{Authority}");
    }

    if (!string.IsNullOrWhiteSpace(MetadataAddress) && !IsValidEndpoint(MetadataAddress))
    {
      throw new InternalServerErrorException($"Options.OpenId.MetadataAddress.Invalid;{MetadataAddress}");
    }

    // Tolerância negativa não tem significado na validação de expiração
    if (ClockSkewSeconds < 0)
    {
      throw new InternalServerErrorException($"Options.OpenId.ClockSkew.Invalid;{ClockSkewSeconds}");
    }

    // Os caminhos de callback só existem no handler interativo
    if (interactive)
    {
      if (!IsValidPath(CallbackPath))
      {
        throw new InternalServerErrorException($"Options.OpenId.CallbackPath.Invalid;{CallbackPath}");
      }

      if (!IsValidPath(SignedOutCallbackPath))
      {
        throw new InternalServerErrorException($"Options.OpenId.SignedOutCallbackPath.Invalid;{SignedOutCallbackPath}");
      }
    }
  }

  /// <summary>
  /// Verifica se o endereço é uma URL absoluta com esquema permitido por <see cref="RequireHttpsMetadata"/>.
  /// </summary>
  /// <param name="endpoint">Endereço a verificar.</param>
  /// <returns>True quando o endereço é aceitável.</returns>
  protected bool IsValidEndpoint(string endpoint)
  {
    // Precisa ser uma URL absoluta
    if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
    {
      return false;
    }

    // HTTPS sempre; HTTP apenas quando explicitamente liberado
    return uri.Scheme == Uri.UriSchemeHttps
      || (!RequireHttpsMetadata && uri.Scheme == Uri.UriSchemeHttp);
  }

  /// <summary>
  /// Verifica se o caminho é relativo à raiz da aplicação.
  /// </summary>
  /// <param name="path">Caminho a verificar.</param>
  /// <returns>True quando o caminho começa com "/".</returns>
  private static bool IsValidPath(string? path) => !string.IsNullOrWhiteSpace(path) && path.StartsWith('/');

  #endregion

  #region Configure Handlers

  /// <summary>
  /// Aplica as opções ao handler interativo (<c>OpenIdConnect</c>) com os padrões Tooark:
  /// fluxo <c>code</c> com PKCE, claims com os nomes do OpenID Connect e validação de emissor e destinatário.
  /// </summary>
  /// <param name="target">Opções do handler <c>OpenIdConnect</c> a configurar.</param>
  /// <remarks>
  /// O callback <see cref="ConfigureOpenIdConnect"/> é executado por último, para que o consumidor possa
  /// sobrescrever qualquer padrão. Os presets sobrescrevem este método para acrescentar comportamento.
  /// </remarks>
  public virtual void ConfigureHandler(OpenIdConnectOptions target)
  {
    // Identificação da aplicação e do provedor
    target.Authority = Authority;
    target.MetadataAddress = MetadataAddress;
    target.ClientId = ClientId;
    target.ClientSecret = ClientSecret;
    target.RequireHttpsMetadata = RequireHttpsMetadata;

    // Fluxo: authorization code com PKCE — o handler nasce em id_token (implícito), que o OAuth 2.1 abandonou
    target.ResponseType = OpenIdConnectResponseType.Code;
    target.UsePkce = UsePkce;

    // Caminhos de retorno e sessão
    target.CallbackPath = CallbackPath;
    target.SignedOutCallbackPath = SignedOutCallbackPath;
    target.SignInScheme = SignInScheme ?? CookieAuthenticationDefaults.AuthenticationScheme;
    target.SaveTokens = SaveTokens;
    target.GetClaimsFromUserInfoEndpoint = GetClaimsFromUserInfoEndpoint;
    target.Prompt = Prompt;

    // Escopos: substitui os padrão do handler pelos efetivos
    target.Scope.Clear();

    foreach (var scope in ResolveScopes())
    {
      target.Scope.Add(scope);
    }

    // Claims com os nomes do OpenID Connect
    target.MapInboundClaims = MapInboundClaims;
    target.TokenValidationParameters.NameClaimType = NameClaimType;
    target.TokenValidationParameters.RoleClaimType = RoleClaimType;

    // Validação do token: o emissor do documento de descoberta é somado pelo handler aos informados aqui
    target.TokenValidationParameters.ValidateIssuer = true;
    target.TokenValidationParameters.ValidateAudience = true;
    target.TokenValidationParameters.ValidateLifetime = true;
    target.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(ClockSkewSeconds);

    var issuers = ResolveValidIssuers();

    if (issuers is not null)
    {
      target.TokenValidationParameters.ValidIssuers = issuers;
    }

    // O destinatário do id_token é o ClientId, que o handler já aplica; aqui só entra o que o consumidor somou
    if (ValidAudiences is { Length: > 0 })
    {
      target.TokenValidationParameters.ValidAudiences = ValidAudiences;
    }

    // O consumidor fala por último
    ConfigureOpenIdConnect?.Invoke(target);
  }

  /// <summary>
  /// Aplica as opções ao handler de API (<c>JwtBearer</c>) com os padrões Tooark:
  /// claims com os nomes do OpenID Connect e validação de emissor, destinatário e expiração.
  /// </summary>
  /// <param name="target">Opções do handler <c>JwtBearer</c> a configurar.</param>
  /// <remarks>
  /// O callback <see cref="ConfigureJwtBearer"/> é executado por último, para que o consumidor possa
  /// sobrescrever qualquer padrão. Os presets sobrescrevem este método para acrescentar comportamento.
  /// </remarks>
  public virtual void ConfigureHandler(JwtBearerOptions target)
  {
    // Identificação do provedor
    target.Authority = Authority;
    target.MetadataAddress = MetadataAddress!;
    target.RequireHttpsMetadata = RequireHttpsMetadata;
    target.SaveToken = SaveTokens;

    // Claims com os nomes do OpenID Connect
    target.MapInboundClaims = MapInboundClaims;
    target.TokenValidationParameters.NameClaimType = NameClaimType;
    target.TokenValidationParameters.RoleClaimType = RoleClaimType;

    // Validação do token: o emissor do documento de descoberta é somado pelo handler aos informados aqui
    target.TokenValidationParameters.ValidateIssuer = true;
    target.TokenValidationParameters.ValidateAudience = true;
    target.TokenValidationParameters.ValidateLifetime = true;
    target.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(ClockSkewSeconds);
    target.TokenValidationParameters.ValidAudiences = ResolveValidAudiences();

    var issuers = ResolveValidIssuers();

    if (issuers is not null)
    {
      target.TokenValidationParameters.ValidIssuers = issuers;
    }

    // O consumidor fala por último
    ConfigureJwtBearer?.Invoke(target);
  }

  #endregion
}
