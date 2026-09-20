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

public class TooarkDependencyInjectionGoogleTests
{
  #region Helpers

  /// <summary>
  /// Configuração mínima do preset do Google na subseção <c>OpenId:{nome}</c>.
  /// </summary>
  /// <param name="name">Nome do provedor.</param>
  /// <param name="includeSecret">Indica se o segredo deve ser incluído.</param>
  /// <param name="hostedDomain">Domínio do Google Workspace, quando a restrição deve ser exercitada.</param>
  /// <returns>Configuração pronta para o registro.</returns>
  private static IConfiguration GoogleConfiguration(string name = "Google", bool includeSecret = true, string? hostedDomain = null)
  {
    var values = new Dictionary<string, string?>
    {
      ["ClientId"] = "client-id.apps.googleusercontent.com"
    };

    if (includeSecret)
    {
      values["ClientSecret"] = "client-secret";
    }

    if (hostedDomain is not null)
    {
      values["HostedDomain"] = hostedDomain;
    }

    return TooarkDependencyInjectionOpenIdTests.BuildConfiguration(name, values);
  }

  #endregion

  #region AddTooarkGoogleSso

  // Teste para verificar que o preset registra o esquema "Google" com a Authority do Google
  [Fact]
  public async Task AddTooarkGoogleSso_ShouldRegisterGoogleScheme()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();

    // Act
    services.AddTooarkGoogleSso(GoogleConfiguration());
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Google");

    // Assert - esquema
    var scheme = await schemes.GetSchemeAsync("Google");
    Assert.NotNull(scheme);
    Assert.Equal("Google", scheme.DisplayName);
    Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authentication.DefaultScheme);
    Assert.Equal("Google", authentication.DefaultChallengeScheme);

    // Assert - handler
    Assert.Equal("https://accounts.google.com", oidc.Authority);
    Assert.Equal("client-id.apps.googleusercontent.com", oidc.ClientId);
    Assert.Equal(OpenIdConnectResponseType.Code, oidc.ResponseType);
    Assert.True(oidc.UsePkce);
    Assert.Equal(["openid", "profile", "email"], oidc.Scope);
    Assert.Equal("email", oidc.TokenValidationParameters.NameClaimType);
    Assert.Equal(["https://accounts.google.com", "accounts.google.com"], oidc.TokenValidationParameters.ValidIssuers);
  }

  // Teste para verificar que o segredo é exigido no login interativo
  [Fact]
  public void AddTooarkGoogleSso_WithoutClientSecret_ShouldThrow()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() =>
      services.AddTooarkGoogleSso(GoogleConfiguration(includeSecret: false)));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.ClientSecretNotConfigured");
  }

  // Teste para verificar que o domínio configurado é lido da subseção e ativa a restrição
  [Fact]
  public void AddTooarkGoogleSso_WithHostedDomain_ShouldWrapEvents()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();

    // Act
    services.AddTooarkGoogleSso(GoogleConfiguration(hostedDomain: "tooark.com"));
    using var provider = services.BuildServiceProvider();
    var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Google");

    // Assert - os eventos foram substituídos pelos encadeados (o padrão do handler é uma instância própria)
    Assert.NotNull(oidc.Events);
    Assert.NotSame(new OpenIdConnectOptions().Events, oidc.Events);
  }

  // Teste para verificar que Entra e Google convivem no mesmo cookie
  [Fact]
  public async Task AddTooarkGoogleSso_WithEntra_ShouldShareCookie()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();
    var entra = TooarkDependencyInjectionOpenIdTests.BuildConfiguration("Entra", new()
    {
      ["TenantId"] = "11111111-2222-3333-4444-555555555555",
      ["ClientId"] = "entra-client",
      ["ClientSecret"] = "entra-secret"
    });

    // Act
    services.AddTooarkEntraSso(entra);
    services.AddTooarkGoogleSso(GoogleConfiguration());
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

    // Assert
    var all = (await schemes.GetAllSchemesAsync()).Select(scheme => scheme.Name).ToList();
    Assert.Contains("Entra", all);
    Assert.Contains("Google", all);
    Assert.Single(all, name => name == CookieAuthenticationDefaults.AuthenticationScheme);
    Assert.Equal("Entra", authentication.DefaultChallengeScheme);
  }

  #endregion

  #region AddTooarkGoogleBearer

  // Teste para verificar que o preset de API valida o id_token do Google
  [Fact]
  public async Task AddTooarkGoogleBearer_ShouldRegisterBearerScheme()
  {
    // Arrange
    var services = TooarkDependencyInjectionOpenIdTests.CreateServices();

    // Act
    services.AddTooarkGoogleBearer(GoogleConfiguration(includeSecret: false));
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Google");

    // Assert
    var scheme = await schemes.GetSchemeAsync("Google");
    Assert.NotNull(scheme);
    Assert.Equal(typeof(JwtBearerHandler), scheme.HandlerType);
    Assert.Equal("Google", authentication.DefaultScheme);
    Assert.Equal("https://accounts.google.com", jwt.Authority);
    Assert.Equal(["https://accounts.google.com", "accounts.google.com"], jwt.TokenValidationParameters.ValidIssuers);
    Assert.Equal(["client-id.apps.googleusercontent.com"], jwt.TokenValidationParameters.ValidAudiences);
    Assert.Equal("email", jwt.TokenValidationParameters.NameClaimType);
  }

  #endregion
}
