using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Tooark.Exceptions;

namespace Tooark.Securities.OpenId.Options;

/// <summary>
/// Classe que representa as opções de configuração do preset de SSO com Google.
/// </summary>
/// <remarks>
/// O preset fixa a <see cref="OpenIdOptions.Authority"/> em <see cref="GoogleAuthority"/>, aceita as duas
/// formas de emissor que o Google usa e usa <c>email</c> como nome do usuário. Com <see cref="HostedDomain"/>,
/// restringe o login a um domínio do Google Workspace: o domínio é sugerido na tela de login e conferido na
/// claim <c>hd</c> do token — a sugestão sozinha não restringe nada.
/// </remarks>
public class GoogleOptions : OpenIdOptions
{
  #region Constants

  /// <summary>
  /// Nome padrão do provedor: subseção <c>OpenId:Google</c> e esquema de autenticação <c>Google</c>.
  /// </summary>
  public const string DefaultName = "Google";

  /// <summary>
  /// Endereço do emissor do Google.
  /// </summary>
  public const string GoogleAuthority = "https://accounts.google.com";

  /// <summary>
  /// Emissores que o Google usa nos tokens: com e sem o esquema.
  /// </summary>
  public static readonly string[] GoogleIssuers = ["https://accounts.google.com", "accounts.google.com"];

  /// <summary>
  /// Claim usada como nome do usuário no Google: o e-mail da conta.
  /// </summary>
  public const string EmailClaimType = "email";

  /// <summary>
  /// Parâmetro da requisição de autorização e claim do token que carregam o domínio do Google Workspace.
  /// </summary>
  public const string HostedDomainClaimType = "hd";

  /// <summary>
  /// Mensagem de rejeição do token quando a conta não pertence a <see cref="HostedDomain"/>.
  /// </summary>
  public const string HostedDomainInvalidMessage = "OpenId.HostedDomain.Invalid";

  #endregion

  #region Private Properties

  /// <summary>
  /// Domínio do Google Workspace, sem espaços nas pontas.
  /// </summary>
  private string? _hostedDomain;

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor com os padrões do Google.
  /// </summary>
  public GoogleOptions()
  {
    DisplayName = "Google";
    Authority = GoogleAuthority;
    NameClaimType = EmailClaimType;
  }

  #endregion

  #region Properties

  /// <summary>
  /// Domínio do Google Workspace ao qual o login fica restrito (ex.: <c>tooark.com</c>).
  /// </summary>
  /// <remarks>
  /// Quando informado, é enviado como parâmetro <c>hd</c> na tela de login e conferido na claim <c>hd</c> do
  /// token; contas de outro domínio, e contas pessoais (que não têm a claim), são rejeitadas.
  /// </remarks>
  public string? HostedDomain
  {
    get => _hostedDomain;
    set => _hostedDomain = value?.Trim() is { Length: > 0 } domain ? domain : null;
  }

  #endregion

  #region Resolve Methods

  /// <summary>
  /// Obtém os emissores efetivos: os configurados ou as duas formas usadas pelo Google.
  /// </summary>
  /// <returns>Lista de emissores aceitos na validação do token.</returns>
  public override string[]? ResolveValidIssuers() => ValidIssuers is { Length: > 0 } ? ValidIssuers : GoogleIssuers;

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções do preset, além das validações comuns de <see cref="OpenIdOptions.Validate"/>.
  /// </summary>
  /// <param name="interactive">Indica se as opções alimentam o handler interativo (true) ou o de API (false).</param>
  /// <exception cref="InternalServerErrorException">Quando, no handler interativo, <see cref="OpenIdOptions.ClientSecret"/> não está configurado.</exception>
  public override void Validate(bool interactive)
  {
    base.Validate(interactive);

    // O Google não troca o código de uma aplicação web sem o segredo
    if (interactive && string.IsNullOrWhiteSpace(ClientSecret))
    {
      throw new InternalServerErrorException("Options.OpenId.ClientSecretNotConfigured");
    }
  }

  #endregion

  #region Configure Handlers

  /// <summary>
  /// Aplica as opções ao handler interativo e, com <see cref="HostedDomain"/>, sugere o domínio na tela de login
  /// e rejeita tokens de outro domínio.
  /// </summary>
  /// <param name="target">Opções do handler <c>OpenIdConnect</c> a configurar.</param>
  /// <remarks>
  /// A conferência do domínio é encadeada após o callback do consumidor, então sobrevive a
  /// <see cref="OpenIdOptions.ConfigureOpenIdConnect"/> — inclusive quando ele substitui os eventos.
  /// </remarks>
  public override void ConfigureHandler(OpenIdConnectOptions target)
  {
    base.ConfigureHandler(target);

    // Sem domínio não há o que restringir
    if (HostedDomain is null)
    {
      return;
    }

    var hostedDomain = HostedDomain;
    var events = target.Events ?? new OpenIdConnectEvents();

    // Sugere o domínio na tela de login (só afeta a experiência; a restrição é na validação abaixo)
    var onRedirectToIdentityProvider = events.OnRedirectToIdentityProvider;

    events.OnRedirectToIdentityProvider = context =>
    {
      context.ProtocolMessage.SetParameter(HostedDomainClaimType, hostedDomain);

      return onRedirectToIdentityProvider(context);
    };

    // Rejeita o token antes de qualquer handler do consumidor
    var onTokenValidated = events.OnTokenValidated;

    events.OnTokenValidated = context =>
    {
      if (!IsHostedDomainValid(context.Principal, hostedDomain))
      {
        context.Fail(HostedDomainInvalidMessage);

        return Task.CompletedTask;
      }

      return onTokenValidated(context);
    };

    target.Events = events;
  }

  /// <summary>
  /// Aplica as opções ao handler de API e, com <see cref="HostedDomain"/>, rejeita tokens de outro domínio.
  /// </summary>
  /// <param name="target">Opções do handler <c>JwtBearer</c> a configurar.</param>
  /// <remarks>
  /// A conferência do domínio é encadeada após o callback do consumidor, então sobrevive a
  /// <see cref="OpenIdOptions.ConfigureJwtBearer"/> — inclusive quando ele substitui os eventos.
  /// </remarks>
  public override void ConfigureHandler(JwtBearerOptions target)
  {
    base.ConfigureHandler(target);

    // Sem domínio não há o que restringir
    if (HostedDomain is null)
    {
      return;
    }

    var hostedDomain = HostedDomain;
    var events = target.Events ?? new JwtBearerEvents();

    // Rejeita o token antes de qualquer handler do consumidor
    var onTokenValidated = events.OnTokenValidated;

    events.OnTokenValidated = context =>
    {
      if (!IsHostedDomainValid(context.Principal, hostedDomain))
      {
        context.Fail(HostedDomainInvalidMessage);

        return Task.CompletedTask;
      }

      return onTokenValidated(context);
    };

    target.Events = events;
  }

  /// <summary>
  /// Verifica se a claim <c>hd</c> do usuário corresponde ao domínio configurado.
  /// </summary>
  /// <param name="principal">Usuário autenticado pelo token.</param>
  /// <param name="hostedDomain">Domínio configurado.</param>
  /// <returns>True quando a claim existe e confere, ignorando maiúsculas e minúsculas.</returns>
  internal static bool IsHostedDomainValid(ClaimsPrincipal? principal, string hostedDomain)
  {
    var claim = principal?.FindFirst(HostedDomainClaimType)?.Value;

    return string.Equals(claim, hostedDomain, StringComparison.OrdinalIgnoreCase);
  }

  #endregion
}
