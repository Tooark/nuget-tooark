using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Tooark.Exceptions;
using Tooark.Securities.OpenId.Injections;

namespace Tooark.Tests.Securities.OpenId.Injections;

public class TooarkDependencyInjectionEntraTests
{
  #region Helpers

  /// <summary>
  /// Tenant de exemplo, no formato GUID.
  /// </summary>
  private const string TenantId = "11111111-2222-3333-4444-555555555555";

  /// <summary>
  /// Configuração mínima do preset do Entra na subseção <c>OpenId:{nome}</c>.
  /// </summary>
  /// <param name="name">Nome do provedor.</param>
  /// <param name="includeSecret">Indica se o segredo deve ser incluído.</param>
  /// <returns>Configuração pronta para o registro.</returns>
  private static IConfiguration EntraConfiguration(string name = "Entra", bool includeSecret = true)
  {
    var values = new Dictionary<string, string?>
    {
      ["TenantId"] = TenantId,
      ["ClientId"] = "client-id"
    };

    if (includeSecret)
    {
      values["ClientSecret"] = "client-secret";
    }

    return TooarkDependencyInjectionOpenIdTests.BuildConfiguration(name, values);
  }

  #endregion

  #region AddTooarkEntraSso

  // Teste para verificar que o preset registra o esquema "Entra" com a Authority derivada do tenant
  [Fact]
  public async Task AddTooarkEntraSso_ShouldRegisterEntraScheme()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();

    // Act
    services.AddTooarkEntraSso(EntraConfiguration());
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Entra");

    // Assert - esquema
    var scheme = await schemes.GetSchemeAsync("Entra");
    Assert.NotNull(scheme);
    Assert.Equal("Microsoft Entra ID", scheme.DisplayName);
    Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authentication.DefaultScheme);
    Assert.Equal("Entra", authentication.DefaultChallengeScheme);

    // Assert - handler
    Assert.Equal($"https://login.microsoftonline.com/{TenantId}/v2.0", oidc.Authority);
    Assert.Equal("client-id", oidc.ClientId);
    Assert.Equal("client-secret", oidc.ClientSecret);
    Assert.Equal(OpenIdConnectResponseType.Code, oidc.ResponseType);
    Assert.True(oidc.UsePkce);
    Assert.Equal(["openid", "profile", "email"], oidc.Scope);
    Assert.Equal("preferred_username", oidc.TokenValidationParameters.NameClaimType);
    Assert.Equal("roles", oidc.TokenValidationParameters.RoleClaimType);
    Assert.Contains($"https://sts.windows.net/{TenantId}/", oidc.TokenValidationParameters.ValidIssuers!);
  }

  // Teste para verificar que o nome pode ser trocado, mudando a subseção e o esquema
  [Fact]
  public async Task AddTooarkEntraSso_WithCustomName_ShouldUseItAsSectionAndScheme()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();

    // Act
    services.AddTooarkEntraSso(EntraConfiguration("EntraCorp"), "EntraCorp");
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("EntraCorp");

    // Assert
    Assert.NotNull(await schemes.GetSchemeAsync("EntraCorp"));
    Assert.Null(await schemes.GetSchemeAsync("Entra"));
    Assert.Equal("client-id", oidc.ClientId);
  }

  // Teste para verificar que o segredo é exigido no login interativo
  [Fact]
  public void AddTooarkEntraSso_WithoutClientSecret_ShouldThrow()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() =>
      services.AddTooarkEntraSso(EntraConfiguration(includeSecret: false)));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.ClientSecretNotConfigured");
  }

  // Teste para verificar que o tenant é exigido
  [Fact]
  public void AddTooarkEntraSso_WithoutTenantId_ShouldThrow()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();
    var configuration = TooarkDependencyInjectionOpenIdTests.BuildConfiguration("Entra", new()
    {
      ["ClientId"] = "client-id",
      ["ClientSecret"] = "client-secret"
    });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkEntraSso(configuration));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.Entra.TenantIdNotConfigured");
  }

  // Teste para verificar que o override programático vem por cima da configuração
  [Fact]
  public void AddTooarkEntraSso_WithConfigure_ShouldApplyOverrides()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();

    // Act
    services.AddTooarkEntraSso(EntraConfiguration(), configure: options =>
    {
      options.Scopes = ["openid", "profile", "User.Read"];
      options.Prompt = "select_account";
    });
    using var provider = services.BuildServiceProvider();
    var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Entra");

    // Assert
    Assert.Equal(["openid", "profile", "User.Read"], oidc.Scope);
    Assert.Equal("select_account", oidc.Prompt);
  }

  #endregion

  #region AddTooarkEntraBearer

  // Teste para verificar que o preset de API aceita tokens v1 e v2 e dispensa o segredo
  [Fact]
  public async Task AddTooarkEntraBearer_ShouldRegisterBearerSchemeWithV1AndV2Support()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();

    // Act
    services.AddTooarkEntraBearer(EntraConfiguration(includeSecret: false));
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Entra");

    // Assert
    var scheme = await schemes.GetSchemeAsync("Entra");
    Assert.NotNull(scheme);
    Assert.Equal(typeof(JwtBearerHandler), scheme.HandlerType);
    Assert.Equal("Entra", authentication.DefaultScheme);
    Assert.Equal($"https://login.microsoftonline.com/{TenantId}/v2.0", jwt.Authority);
    Assert.Equal(
      [$"https://login.microsoftonline.com/{TenantId}/v2.0", $"https://sts.windows.net/{TenantId}/"],
      jwt.TokenValidationParameters.ValidIssuers
    );
    Assert.Equal(["client-id", "api://client-id"], jwt.TokenValidationParameters.ValidAudiences);
    Assert.Equal("preferred_username", jwt.TokenValidationParameters.NameClaimType);
  }

  // Teste para verificar que multi-tenant sem emissores falha no registro
  [Fact]
  public void AddTooarkEntraBearer_MultiTenantWithoutValidIssuers_ShouldThrow()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();
    var configuration = TooarkDependencyInjectionOpenIdTests.BuildConfiguration("Entra", new()
    {
      ["TenantId"] = "organizations",
      ["ClientId"] = "client-id"
    });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkEntraBearer(configuration));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.Entra.MultiTenantRequiresValidIssuers;organizations");
  }

  // Teste para verificar que multi-tenant com emissores explícitos usa exatamente esses emissores
  [Fact]
  public void AddTooarkEntraBearer_MultiTenantWithValidIssuers_ShouldUseConfiguredIssuers()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();
    var configuration = TooarkDependencyInjectionOpenIdTests.BuildConfiguration("Entra", new()
    {
      ["TenantId"] = "organizations",
      ["ClientId"] = "client-id",
      ["ValidIssuers:0"] = $"https://login.microsoftonline.com/{TenantId}/v2.0"
    });

    // Act
    services.AddTooarkEntraBearer(configuration);
    using var provider = services.BuildServiceProvider();
    var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Entra");

    // Assert
    Assert.Equal("https://login.microsoftonline.com/organizations/v2.0", jwt.Authority);
    Assert.Equal([$"https://login.microsoftonline.com/{TenantId}/v2.0"], jwt.TokenValidationParameters.ValidIssuers);
  }

  #endregion
}
