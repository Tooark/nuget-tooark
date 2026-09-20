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
using Tooark.Securities.OpenId.Options;

namespace Tooark.Tests.Securities.OpenId.Injections;

public class TooarkDependencyInjectionOpenIdTests
{
  #region Helpers

  /// <summary>
  /// Monta a configuração em memória de um provedor na subseção <c>OpenId:{nome}</c>.
  /// </summary>
  /// <param name="name">Nome do provedor.</param>
  /// <param name="values">Chaves relativas à subseção e seus valores.</param>
  /// <returns>Configuração pronta para o registro.</returns>
  internal static IConfiguration BuildConfiguration(string name, Dictionary<string, string?> values)
  {
    var settings = values.ToDictionary(pair => $"OpenId:{name}:{pair.Key}", pair => pair.Value);

    return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
  }

  /// <summary>
  /// Configuração mínima de um provedor genérico.
  /// </summary>
  /// <param name="name">Nome do provedor.</param>
  /// <returns>Configuração com Authority, ClientId e ClientSecret.</returns>
  private static IConfiguration GenericConfiguration(string name = "Keycloak") => BuildConfiguration(name, new()
  {
    ["Authority"] = "https://idp.example.com/realms/tooark",
    ["ClientId"] = "client-id",
    ["ClientSecret"] = "client-secret"
  });

  /// <summary>
  /// Cria a coleção de serviços com o mínimo que os handlers exigem para resolver as opções.
  /// </summary>
  /// <returns>Coleção de serviços com logging.</returns>
  internal static ServiceCollection CreateServices()
  {
    var services = new ServiceCollection();
    services.AddLogging();

    return services;
  }

  #endregion

  #region SectionName / BindOptions

  // Teste para verificar o caminho da subseção do provedor
  [Fact]
  public void SectionName_ShouldPrefixWithOpenId()
  {
    // Act & Assert
    Assert.Equal("OpenId:Keycloak", TooarkDependencyInjection.SectionName("Keycloak"));
  }

  // Teste para verificar que o nome do provedor é obrigatório
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void BindOptions_WithoutName_ShouldThrow(string? name)
  {
    // Arrange
    var configuration = GenericConfiguration();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() =>
      TooarkDependencyInjection.BindOptions<OpenIdOptions>(configuration, name!, null, interactive: true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.NameNotConfigured");
  }

  // Teste para verificar que a configuração é lida da subseção e o override programático vem por cima
  [Fact]
  public void BindOptions_ShouldBindSectionAndApplyConfigure()
  {
    // Arrange
    var configuration = BuildConfiguration("Keycloak", new()
    {
      ["Authority"] = "https://idp.example.com/realms/tooark",
      ["ClientId"] = "from-configuration",
      ["Scopes:0"] = "openid",
      ["Scopes:1"] = "offline_access",
      ["ClockSkewSeconds"] = "30"
    });

    // Act
    var options = TooarkDependencyInjection.BindOptions<OpenIdOptions>(
      configuration,
      "Keycloak",
      configure: options => options.ClientId = "from-code",
      interactive: true
    );

    // Assert
    Assert.Equal("https://idp.example.com/realms/tooark", options.Authority);
    Assert.Equal("from-code", options.ClientId);
    Assert.Equal(["openid", "offline_access"], options.Scopes!);
    Assert.Equal(30, options.ClockSkewSeconds);
  }

  // Teste para verificar que a validação roda no registro, não no primeiro login
  [Fact]
  public void BindOptions_WithInvalidOptions_ShouldThrow()
  {
    // Arrange - sem ClientId
    var configuration = BuildConfiguration("Keycloak", new()
    {
      ["Authority"] = "https://idp.example.com/realms/tooark"
    });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() =>
      TooarkDependencyInjection.BindOptions<OpenIdOptions>(configuration, "Keycloak", null, interactive: true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.ClientIdNotConfigured");
  }

  #endregion

  #region AddTooarkOpenIdSso

  // Teste para verificar que o login interativo registra o esquema, o cookie e os padrões
  [Fact]
  public async Task AddTooarkOpenIdSso_ShouldRegisterSchemeCookieAndDefaults()
  {
    // Arrange
    var services = CreateServices();

    // Act
    services.AddTooarkOpenIdSso(GenericConfiguration(), "Keycloak");
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Keycloak");

    // Assert - esquemas
    var scheme = await schemes.GetSchemeAsync("Keycloak");
    Assert.NotNull(scheme);
    Assert.Equal("Keycloak", scheme.DisplayName);
    Assert.Equal(typeof(OpenIdConnectHandler), scheme.HandlerType);
    Assert.NotNull(await schemes.GetSchemeAsync(CookieAuthenticationDefaults.AuthenticationScheme));
    Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authentication.DefaultScheme);
    Assert.Equal("Keycloak", authentication.DefaultChallengeScheme);

    // Assert - handler
    Assert.Equal("https://idp.example.com/realms/tooark", oidc.Authority);
    Assert.Equal("client-id", oidc.ClientId);
    Assert.Equal("client-secret", oidc.ClientSecret);
    Assert.Equal(OpenIdConnectResponseType.Code, oidc.ResponseType);
    Assert.True(oidc.UsePkce);
    Assert.False(oidc.MapInboundClaims);
    Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, oidc.SignInScheme);
  }

  // Teste para verificar que o nome de exibição configurado é usado no esquema
  [Fact]
  public async Task AddTooarkOpenIdSso_WithDisplayName_ShouldUseIt()
  {
    // Arrange
    var services = CreateServices();

    // Act
    services.AddTooarkOpenIdSso(GenericConfiguration(), "Keycloak", options => options.DisplayName = "Keycloak Corporativo");
    using var provider = services.BuildServiceProvider();
    var scheme = await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync("Keycloak");

    // Assert
    Assert.NotNull(scheme);
    Assert.Equal("Keycloak Corporativo", scheme.DisplayName);
  }

  // Teste para verificar que dois provedores compartilham um único cookie e o primeiro define o desafio padrão
  [Fact]
  public async Task AddTooarkOpenIdSso_WithTwoProviders_ShouldRegisterCookieOnce()
  {
    // Arrange
    var services = CreateServices();

    // Act
    services.AddTooarkOpenIdSso(GenericConfiguration("Primeiro"), "Primeiro");
    services.AddTooarkOpenIdSso(GenericConfiguration("Segundo"), "Segundo");
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

    // Assert - um segundo AddCookie lançaria "Scheme already exists" ao construir as opções
    var all = (await schemes.GetAllSchemesAsync()).Select(scheme => scheme.Name).ToList();
    Assert.Contains("Primeiro", all);
    Assert.Contains("Segundo", all);
    Assert.Single(all, name => name == CookieAuthenticationDefaults.AuthenticationScheme);
    Assert.Equal("Primeiro", authentication.DefaultChallengeScheme);
  }

  // Teste para verificar que um SignInScheme próprio dispensa o cookie do pacote
  [Fact]
  public async Task AddTooarkOpenIdSso_WithSignInScheme_ShouldNotRegisterCookie()
  {
    // Arrange
    var services = CreateServices();
    services.AddAuthentication().AddCookie("AppCookie");

    // Act
    services.AddTooarkOpenIdSso(GenericConfiguration(), "Keycloak", options => options.SignInScheme = "AppCookie");
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Keycloak");

    // Assert
    Assert.Null(await schemes.GetSchemeAsync(CookieAuthenticationDefaults.AuthenticationScheme));
    Assert.NotNull(await schemes.GetSchemeAsync("AppCookie"));
    Assert.Equal("AppCookie", authentication.DefaultScheme);
    Assert.Equal("AppCookie", oidc.SignInScheme);
  }

  // Teste para verificar que os esquemas padrão definidos pela aplicação são preservados
  [Fact]
  public void AddTooarkOpenIdSso_WhenDefaultsAlreadySet_ShouldPreserveThem()
  {
    // Arrange
    var services = CreateServices();
    services.AddAuthentication(options =>
    {
      options.DefaultScheme = "Custom";
      options.DefaultChallengeScheme = "CustomChallenge";
    });

    // Act
    services.AddTooarkOpenIdSso(GenericConfiguration(), "Keycloak");
    using var provider = services.BuildServiceProvider();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

    // Assert
    Assert.Equal("Custom", authentication.DefaultScheme);
    Assert.Equal("CustomChallenge", authentication.DefaultChallengeScheme);
  }

  // Teste para verificar que configuração inválida falha no registro
  [Fact]
  public void AddTooarkOpenIdSso_WithInvalidConfiguration_ShouldThrowAtRegistration()
  {
    // Arrange
    var services = CreateServices();
    var configuration = BuildConfiguration("Keycloak", new() { ["ClientId"] = "client-id" });

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkOpenIdSso(configuration, "Keycloak"));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.AuthorityNotConfigured");
  }

  // Teste para verificar que uma subclasse de opções pode ser usada como preset próprio
  [Fact]
  public void AddTooarkOpenIdSso_WithCustomOptions_ShouldUseSubclass()
  {
    // Arrange
    var services = CreateServices();
    var configuration = BuildConfiguration("Custom", new()
    {
      ["Realm"] = "tooark",
      ["ClientId"] = "client-id"
    });

    // Act
    services.AddTooarkOpenIdSso<CustomProviderOptions>(configuration, "Custom");
    using var provider = services.BuildServiceProvider();
    var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Custom");

    // Assert
    Assert.Equal("https://custom.example.com/realms/tooark", oidc.Authority);
    Assert.Equal("Custom Provider", (oidc.Events as CustomProviderOptions.MarkerEvents)?.Origin);
  }

  #endregion

  #region AddTooarkOpenIdBearer

  // Teste para verificar que a validação de token registra o esquema como padrão e aplica os padrões
  [Fact]
  public async Task AddTooarkOpenIdBearer_ShouldRegisterSchemeAndDefaults()
  {
    // Arrange
    var services = CreateServices();

    // Act
    services.AddTooarkOpenIdBearer(GenericConfiguration(), "Keycloak");
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Keycloak");

    // Assert
    var scheme = await schemes.GetSchemeAsync("Keycloak");
    Assert.NotNull(scheme);
    Assert.Equal(typeof(JwtBearerHandler), scheme.HandlerType);
    Assert.Null(await schemes.GetSchemeAsync(CookieAuthenticationDefaults.AuthenticationScheme));
    Assert.Equal("Keycloak", authentication.DefaultScheme);
    Assert.Equal("https://idp.example.com/realms/tooark", jwt.Authority);
    Assert.False(jwt.MapInboundClaims);
    Assert.Equal(["client-id"], jwt.TokenValidationParameters.ValidAudiences);
  }

  // Teste para verificar que login interativo e validação de token convivem com nomes diferentes
  [Fact]
  public async Task AddTooarkOpenIdSsoAndBearer_WithDifferentNames_ShouldCoexist()
  {
    // Arrange
    var services = CreateServices();

    // Act
    services.AddTooarkOpenIdSso(GenericConfiguration("Web"), "Web");
    services.AddTooarkOpenIdBearer(GenericConfiguration("Api"), "Api");
    using var provider = services.BuildServiceProvider();
    var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

    // Assert - o primeiro registrado define os padrões
    Assert.NotNull(await schemes.GetSchemeAsync("Web"));
    Assert.NotNull(await schemes.GetSchemeAsync("Api"));
    Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authentication.DefaultScheme);
    Assert.Equal("Web", authentication.DefaultChallengeScheme);
  }

  // Teste para verificar que o override programático chega ao handler de API
  [Fact]
  public void AddTooarkOpenIdBearer_WithConfigure_ShouldApplyOverrides()
  {
    // Arrange
    var services = CreateServices();

    // Act
    services.AddTooarkOpenIdBearer(GenericConfiguration(), "Keycloak", options =>
    {
      options.ValidAudiences = ["api-a", "api-b"];
      options.ConfigureJwtBearer = jwt => jwt.Challenge = "Bearer realm=\"tooark\"";
    });
    using var provider = services.BuildServiceProvider();
    var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Keycloak");

    // Assert
    Assert.Equal(["api-a", "api-b"], jwt.TokenValidationParameters.ValidAudiences);
    Assert.Equal("Bearer realm=\"tooark\"", jwt.Challenge);
  }

  #endregion

  #region Custom Provider

  /// <summary>
  /// Preset de exemplo: deriva a Authority de um realm e marca os eventos para o teste reconhecer.
  /// </summary>
  public class CustomProviderOptions : OpenIdOptions
  {
    /// <summary>
    /// Eventos marcados com a origem, para o teste confirmar que o preset configurou o handler.
    /// </summary>
    public class MarkerEvents : OpenIdConnectEvents
    {
      /// <summary>
      /// Origem da configuração.
      /// </summary>
      public string Origin { get; init; } = string.Empty;
    }

    /// <summary>
    /// Realm do provedor.
    /// </summary>
    public string? Realm { get; set; }

    /// <summary>
    /// Authority derivada do realm.
    /// </summary>
    public override string? Authority
    {
      get => base.Authority ?? (Realm is null ? null : $"https://custom.example.com/realms/{Realm}");
      set => base.Authority = value;
    }

    /// <summary>
    /// Marca os eventos do handler.
    /// </summary>
    /// <param name="target">Opções do handler.</param>
    public override void ConfigureHandler(OpenIdConnectOptions target)
    {
      base.ConfigureHandler(target);
      target.Events = new MarkerEvents { Origin = "Custom Provider" };
    }
  }

  #endregion
}
