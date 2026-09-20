using Tooark.Exceptions;

namespace Tooark.Securities.OpenId.Options;

/// <summary>
/// Classe que representa as opções de configuração do preset de SSO com Microsoft Entra ID.
/// </summary>
/// <remarks>
/// O preset deriva a <see cref="OpenIdOptions.Authority"/> do tenant (<c>{Instance}{TenantId}/v2.0</c>), aceita
/// os emissores v1 e v2 do tenant e os destinatários <c>{ClientId}</c> e <c>api://{ClientId}</c>, e usa
/// <c>preferred_username</c> como nome do usuário e <c>roles</c> (app roles) como papel. Usa apenas o handler
/// nativo do ASP.NET Core; recursos que exigem o SDK do provedor (Graph, cache de token distribuído,
/// on-behalf-of) ficam fora do escopo deste pacote.
/// </remarks>
public class EntraOptions : OpenIdOptions
{
  #region Constants

  /// <summary>
  /// Nome padrão do provedor: subseção <c>OpenId:Entra</c> e esquema de autenticação <c>Entra</c>.
  /// </summary>
  public const string DefaultName = "Entra";

  /// <summary>
  /// Instância padrão do Entra ID (nuvem pública).
  /// </summary>
  public const string DefaultInstance = "https://login.microsoftonline.com/";

  /// <summary>
  /// Emissor dos tokens v1 do Entra ID na nuvem pública. O tenant é sempre o GUID, nunca o domínio.
  /// </summary>
  public const string V1IssuerInstance = "https://sts.windows.net/";

  /// <summary>
  /// Claim usada como nome do usuário no Entra ID: o UPN ou e-mail de login.
  /// </summary>
  public const string PreferredUsernameClaimType = "preferred_username";

  /// <summary>
  /// Apelidos de tenant que aceitam contas de vários tenants (<c>common</c>, <c>organizations</c> e <c>consumers</c>).
  /// </summary>
  /// <remarks>
  /// Com esses apelidos o documento de descoberta publica um emissor com o marcador <c>{tenantid}</c>, que nunca
  /// confere com o emissor real do token — por isso exigem <see cref="OpenIdOptions.ValidIssuers"/> explícitos.
  /// </remarks>
  public static readonly string[] MultiTenantAliases = ["common", "organizations", "consumers"];

  #endregion

  #region Private Properties

  /// <summary>
  /// Instância do Entra ID, sempre terminada em "/".
  /// </summary>
  private string _instance = DefaultInstance;

  /// <summary>
  /// Tenant, sem espaços nas pontas.
  /// </summary>
  private string? _tenantId;

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor com os padrões do Entra ID.
  /// </summary>
  public EntraOptions()
  {
    DisplayName = "Microsoft Entra ID";
    NameClaimType = PreferredUsernameClaimType;
  }

  #endregion

  #region Properties

  /// <summary>
  /// Instância do Entra ID. Padrão: <see cref="DefaultInstance"/>.
  /// </summary>
  /// <remarks>
  /// Troque apenas para nuvens soberanas (ex.: <c>https://login.microsoftonline.us/</c>). A barra final é
  /// garantida ao atribuir.
  /// </remarks>
  public string Instance
  {
    get => _instance;
    set => _instance = value?.Trim() is { Length: > 0 } instance
      ? (instance.EndsWith('/') ? instance : $"{instance}/")
      : DefaultInstance;
  }

  /// <summary>
  /// Tenant do Entra ID: GUID ou domínio (ex.: <c>contoso.onmicrosoft.com</c>). Obrigatório.
  /// </summary>
  /// <remarks>
  /// Os apelidos <c>common</c>, <c>organizations</c> e <c>consumers</c> são aceitos, mas exigem
  /// <see cref="OpenIdOptions.ValidIssuers"/> com os emissores dos tenants permitidos.
  /// </remarks>
  public string? TenantId
  {
    get => _tenantId;
    set => _tenantId = value?.Trim();
  }

  /// <summary>
  /// Endereço do emissor. Quando não informado, é derivado como <c>{Instance}{TenantId}/v2.0</c>.
  /// </summary>
  public override string? Authority
  {
    get => base.Authority ?? (string.IsNullOrWhiteSpace(TenantId) ? null : $"{Instance}{TenantId}/v2.0");
    set => base.Authority = value;
  }

  /// <summary>
  /// Indica se o tenant é um apelido multi-tenant (<see cref="MultiTenantAliases"/>).
  /// </summary>
  public bool IsMultiTenant => TenantId is not null && MultiTenantAliases.Contains(TenantId, StringComparer.OrdinalIgnoreCase);

  #endregion

  #region Resolve Methods

  /// <summary>
  /// Obtém os emissores efetivos: os configurados ou, para um tenant específico, os emissores v2 e v1 do tenant.
  /// </summary>
  /// <returns>Lista de emissores, ou nulo quando o tenant é multi-tenant sem emissores configurados.</returns>
  /// <remarks>
  /// O emissor v1 (<c>https://sts.windows.net/{tenant}/</c>) só é acrescentado na nuvem pública e só confere
  /// quando o tenant é informado como GUID. Ele cobre APIs cujo registro ainda emite tokens v1
  /// (<c>requestedAccessTokenVersion</c> nulo), o padrão de registros novos.
  /// </remarks>
  public override string[]? ResolveValidIssuers()
  {
    // O que o consumidor configurou tem prioridade
    if (ValidIssuers is { Length: > 0 })
    {
      return ValidIssuers;
    }

    // Sem tenant específico não há emissor a derivar
    if (string.IsNullOrWhiteSpace(TenantId) || IsMultiTenant)
    {
      return null;
    }

    // Emissor v2, o mesmo do documento de descoberta
    var issuers = new List<string> { $"{Instance}{TenantId}/v2.0" };

    // Emissor v1, apenas na nuvem pública
    if (string.Equals(Instance, DefaultInstance, StringComparison.OrdinalIgnoreCase))
    {
      issuers.Add($"{V1IssuerInstance}{TenantId}/");
    }

    return [.. issuers];
  }

  /// <summary>
  /// Obtém os destinatários efetivos: os configurados ou <c>{ClientId}</c> e <c>api://{ClientId}</c>.
  /// </summary>
  /// <returns>Lista de destinatários aceitos na validação do token pelo handler de API.</returns>
  /// <remarks>
  /// Tokens v2 trazem <c>aud</c> igual ao ClientId; tokens v1 trazem o Application ID URI, cujo padrão é
  /// <c>api://{ClientId}</c>. Um Application ID URI personalizado precisa ser informado em
  /// <see cref="OpenIdOptions.ValidAudiences"/>.
  /// </remarks>
  public override string[] ResolveValidAudiences() => ValidAudiences is { Length: > 0 }
    ? ValidAudiences
    : [ClientId!, $"api://{ClientId}"];

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções do preset, além das validações comuns de <see cref="OpenIdOptions.Validate"/>.
  /// </summary>
  /// <param name="interactive">Indica se as opções alimentam o handler interativo (true) ou o de API (false).</param>
  /// <exception cref="InternalServerErrorException">Quando <see cref="TenantId"/> não está configurado.</exception>
  /// <exception cref="InternalServerErrorException">Quando <see cref="Instance"/> não é uma URL HTTPS absoluta.</exception>
  /// <exception cref="InternalServerErrorException">Quando o tenant é multi-tenant e <see cref="OpenIdOptions.ValidIssuers"/> não foi informado.</exception>
  /// <exception cref="InternalServerErrorException">Quando, no handler interativo, <see cref="OpenIdOptions.ClientSecret"/> não está configurado.</exception>
  public override void Validate(bool interactive)
  {
    // Sem tenant não há Authority a derivar — reportado antes da validação comum para a mensagem ser a certa
    if (string.IsNullOrWhiteSpace(TenantId))
    {
      throw new InternalServerErrorException("Options.OpenId.Entra.TenantIdNotConfigured");
    }

    // A instância compõe a Authority, então precisa ser válida por si só
    if (!Uri.TryCreate(Instance, UriKind.Absolute, out var instance) || instance.Scheme != Uri.UriSchemeHttps)
    {
      throw new InternalServerErrorException($"Options.OpenId.Entra.Instance.Invalid;{Instance}");
    }

    // Multi-tenant sem emissores explícitos aceitaria... nada: o emissor do documento é um marcador
    if (IsMultiTenant && ValidIssuers is not { Length: > 0 })
    {
      throw new InternalServerErrorException($"Options.OpenId.Entra.MultiTenantRequiresValidIssuers;{TenantId}");
    }

    base.Validate(interactive);

    // O Entra não troca o código de uma aplicação web sem o segredo (AADSTS7000218)
    if (interactive && string.IsNullOrWhiteSpace(ClientSecret))
    {
      throw new InternalServerErrorException("Options.OpenId.ClientSecretNotConfigured");
    }
  }

  #endregion
}
