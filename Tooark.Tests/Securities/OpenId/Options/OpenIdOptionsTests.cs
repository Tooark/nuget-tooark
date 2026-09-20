using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Tooark.Exceptions;
using Tooark.Securities.OpenId.Options;

namespace Tooark.Tests.Securities.OpenId.Options;

public class OpenIdOptionsTests
{
  #region Helpers

  /// <summary>
  /// Cria opções válidas para um provedor genérico.
  /// </summary>
  /// <returns>Instância de <see cref="OpenIdOptions"/> pronta para validar.</returns>
  private static OpenIdOptions CreateValidOptions() => new()
  {
    Authority = "https://idp.example.com/realms/tooark",
    ClientId = "client-id",
    ClientSecret = "client-secret"
  };

  #endregion

  #region Defaults

  // Teste para verificar os valores padrão das opções
  [Fact]
  public void Defaults_ShouldMatchTooarkStandard()
  {
    // Arrange & Act
    var options = new OpenIdOptions();

    // Assert
    Assert.Equal("OpenId", OpenIdOptions.Section);
    Assert.Null(options.Authority);
    Assert.Null(options.MetadataAddress);
    Assert.Null(options.ClientId);
    Assert.Null(options.ClientSecret);
    Assert.Null(options.Scopes);
    Assert.True(options.UsePkce);
    Assert.Equal("/signin-oidc", options.CallbackPath);
    Assert.Equal("/signout-callback-oidc", options.SignedOutCallbackPath);
    Assert.Null(options.SignInScheme);
    Assert.False(options.SaveTokens);
    Assert.False(options.GetClaimsFromUserInfoEndpoint);
    Assert.True(options.RequireHttpsMetadata);
    Assert.False(options.MapInboundClaims);
    Assert.Equal("name", options.NameClaimType);
    Assert.Equal("roles", options.RoleClaimType);
    Assert.Null(options.ValidIssuers);
    Assert.Null(options.ValidAudiences);
    Assert.Equal(300, options.ClockSkewSeconds);
    Assert.Null(options.Prompt);
    Assert.Null(options.DisplayName);
    Assert.Null(options.ConfigureOpenIdConnect);
    Assert.Null(options.ConfigureJwtBearer);
  }

  #endregion

  #region ResolveScopes

  // Teste para verificar que sem escopos configurados os padrão são usados
  [Fact]
  public void ResolveScopes_WhenNotConfigured_ReturnsDefaults()
  {
    // Arrange
    var options = new OpenIdOptions();

    // Act
    var scopes = options.ResolveScopes();

    // Assert
    Assert.Equal(["openid", "profile", "email"], scopes);
  }

  // Teste para verificar que o escopo openid é garantido mesmo quando omitido
  [Fact]
  public void ResolveScopes_WhenOpenIdMissing_PrependsOpenId()
  {
    // Arrange
    var options = new OpenIdOptions { Scopes = ["profile", "offline_access"] };

    // Act
    var scopes = options.ResolveScopes();

    // Assert
    Assert.Equal(["openid", "profile", "offline_access"], scopes);
  }

  // Teste para verificar que escopos vazios e repetidos são descartados
  [Fact]
  public void ResolveScopes_ShouldTrimAndRemoveDuplicates()
  {
    // Arrange
    var options = new OpenIdOptions { Scopes = [" openid ", "email", "", "  ", "email", "profile"] };

    // Act
    var scopes = options.ResolveScopes();

    // Assert
    Assert.Equal(["openid", "email", "profile"], scopes);
  }

  #endregion

  #region ResolveValidIssuers / ResolveValidAudiences

  // Teste para verificar que sem emissores configurados nada é derivado no provedor genérico
  [Fact]
  public void ResolveValidIssuers_WhenNotConfigured_ReturnsNull()
  {
    // Arrange
    var options = new OpenIdOptions();

    // Act & Assert
    Assert.Null(options.ResolveValidIssuers());
  }

  // Teste para verificar que os emissores configurados são devolvidos
  [Fact]
  public void ResolveValidIssuers_WhenConfigured_ReturnsConfigured()
  {
    // Arrange
    var options = new OpenIdOptions { ValidIssuers = ["https://a", "https://b"] };

    // Act & Assert
    Assert.Equal(["https://a", "https://b"], options.ResolveValidIssuers()!);
  }

  // Teste para verificar que sem destinatários configurados o ClientId é o único aceito
  [Fact]
  public void ResolveValidAudiences_WhenNotConfigured_ReturnsClientId()
  {
    // Arrange
    var options = new OpenIdOptions { ClientId = "client-id" };

    // Act & Assert
    Assert.Equal(["client-id"], options.ResolveValidAudiences());
  }

  // Teste para verificar que os destinatários configurados são devolvidos
  [Fact]
  public void ResolveValidAudiences_WhenConfigured_ReturnsConfigured()
  {
    // Arrange
    var options = new OpenIdOptions { ClientId = "client-id", ValidAudiences = ["api-a", "api-b"] };

    // Act & Assert
    Assert.Equal(["api-a", "api-b"], options.ResolveValidAudiences());
  }

  #endregion

  #region Validate

  // Teste para verificar que opções completas passam na validação dos dois handlers
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void Validate_WithValidOptions_ShouldNotThrow(bool interactive)
  {
    // Arrange
    var options = CreateValidOptions();

    // Act & Assert
    options.Validate(interactive);
  }

  // Teste para verificar que o ClientId é obrigatório
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Validate_WithoutClientId_ShouldThrow(string? clientId)
  {
    // Arrange
    var options = CreateValidOptions();
    options.ClientId = clientId;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.ClientIdNotConfigured");
  }

  // Teste para verificar que Authority ou MetadataAddress é obrigatório
  [Fact]
  public void Validate_WithoutAuthorityAndMetadataAddress_ShouldThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.Authority = null;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.AuthorityNotConfigured");
  }

  // Teste para verificar que MetadataAddress sozinho satisfaz a validação
  [Fact]
  public void Validate_WithMetadataAddressOnly_ShouldNotThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.Authority = null;
    options.MetadataAddress = "https://idp.example.com/custom/openid-configuration";

    // Act & Assert
    options.Validate(true);
  }

  // Teste para verificar que a Authority precisa ser uma URL absoluta e segura
  [Theory]
  [InlineData("idp.example.com")]
  [InlineData("/relative")]
  [InlineData("ftp://idp.example.com")]
  [InlineData("http://idp.example.com")]
  public void Validate_WithInvalidAuthority_ShouldThrow(string authority)
  {
    // Arrange
    var options = CreateValidOptions();
    options.Authority = authority;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == $"Options.OpenId.Authority.Invalid;{authority}");
  }

  // Teste para verificar que HTTP é aceito apenas com RequireHttpsMetadata desligado
  [Fact]
  public void Validate_WithHttpAuthority_WhenHttpsNotRequired_ShouldNotThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.Authority = "http://localhost:8080/realms/dev";
    options.RequireHttpsMetadata = false;

    // Act & Assert
    options.Validate(true);
  }

  // Teste para verificar que o MetadataAddress também é validado
  [Fact]
  public void Validate_WithInvalidMetadataAddress_ShouldThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.MetadataAddress = "not-a-url";

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.MetadataAddress.Invalid;not-a-url");
  }

  // Teste para verificar que a tolerância de relógio não pode ser negativa
  [Fact]
  public void Validate_WithNegativeClockSkew_ShouldThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.ClockSkewSeconds = -1;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(false));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.ClockSkew.Invalid;-1");
  }

  // Teste para verificar que o caminho de callback precisa começar com "/" no handler interativo
  [Theory]
  [InlineData("signin-oidc")]
  [InlineData("")]
  public void Validate_Interactive_WithInvalidCallbackPath_ShouldThrow(string callbackPath)
  {
    // Arrange
    var options = CreateValidOptions();
    options.CallbackPath = callbackPath;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == $"Options.OpenId.CallbackPath.Invalid;{callbackPath}");
  }

  // Teste para verificar que o caminho de callback de logout também é validado
  [Fact]
  public void Validate_Interactive_WithInvalidSignedOutCallbackPath_ShouldThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.SignedOutCallbackPath = "signout";

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.SignedOutCallbackPath.Invalid;signout");
  }

  // Teste para verificar que os caminhos de callback não importam para o handler de API
  [Fact]
  public void Validate_Bearer_IgnoresCallbackPaths()
  {
    // Arrange
    var options = CreateValidOptions();
    options.CallbackPath = "invalid";
    options.SignedOutCallbackPath = "invalid";

    // Act & Assert
    options.Validate(false);
  }

  // Teste para verificar que o provedor genérico aceita cliente público (sem segredo) no handler interativo
  [Fact]
  public void Validate_Interactive_WithoutClientSecret_ShouldNotThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.ClientSecret = null;

    // Act & Assert
    options.Validate(true);
  }

  #endregion

  #region ConfigureHandler (OpenIdConnect)

  // Teste para verificar que o handler interativo recebe os padrões Tooark
  [Fact]
  public void ConfigureHandler_OpenIdConnect_AppliesTooarkDefaults()
  {
    // Arrange
    var options = CreateValidOptions();
    var target = new OpenIdConnectOptions();

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Equal("https://idp.example.com/realms/tooark", target.Authority);
    Assert.Equal("client-id", target.ClientId);
    Assert.Equal("client-secret", target.ClientSecret);
    Assert.Equal(OpenIdConnectResponseType.Code, target.ResponseType);
    Assert.True(target.UsePkce);
    Assert.Equal("/signin-oidc", target.CallbackPath.Value);
    Assert.Equal("/signout-callback-oidc", target.SignedOutCallbackPath.Value);
    Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, target.SignInScheme);
    Assert.False(target.SaveTokens);
    Assert.False(target.GetClaimsFromUserInfoEndpoint);
    Assert.True(target.RequireHttpsMetadata);
    Assert.False(target.MapInboundClaims);
    Assert.Null(target.Prompt);
    Assert.Equal(["openid", "profile", "email"], target.Scope);
    Assert.Equal("name", target.TokenValidationParameters.NameClaimType);
    Assert.Equal("roles", target.TokenValidationParameters.RoleClaimType);
    Assert.True(target.TokenValidationParameters.ValidateIssuer);
    Assert.True(target.TokenValidationParameters.ValidateAudience);
    Assert.True(target.TokenValidationParameters.ValidateLifetime);
    Assert.Equal(TimeSpan.FromSeconds(300), target.TokenValidationParameters.ClockSkew);
    Assert.Null(target.TokenValidationParameters.ValidIssuers);
    Assert.Null(target.TokenValidationParameters.ValidAudiences);
  }

  // Teste para verificar que as opções configuradas chegam ao handler interativo
  [Fact]
  public void ConfigureHandler_OpenIdConnect_AppliesConfiguredValues()
  {
    // Arrange
    var options = CreateValidOptions();
    options.MetadataAddress = "https://idp.example.com/custom/openid-configuration";
    options.Scopes = ["openid", "offline_access"];
    options.UsePkce = false;
    options.CallbackPath = "/auth/callback";
    options.SignedOutCallbackPath = "/auth/signed-out";
    options.SignInScheme = "AppCookie";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = false;
    options.MapInboundClaims = true;
    options.NameClaimType = "preferred_username";
    options.RoleClaimType = "groups";
    options.ValidIssuers = ["https://issuer-a", "https://issuer-b"];
    options.ValidAudiences = ["aud-a"];
    options.ClockSkewSeconds = 60;
    options.Prompt = "select_account";
    var target = new OpenIdConnectOptions();

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Equal("https://idp.example.com/custom/openid-configuration", target.MetadataAddress);
    Assert.Equal(["openid", "offline_access"], target.Scope);
    Assert.False(target.UsePkce);
    Assert.Equal("/auth/callback", target.CallbackPath.Value);
    Assert.Equal("/auth/signed-out", target.SignedOutCallbackPath.Value);
    Assert.Equal("AppCookie", target.SignInScheme);
    Assert.True(target.SaveTokens);
    Assert.True(target.GetClaimsFromUserInfoEndpoint);
    Assert.False(target.RequireHttpsMetadata);
    Assert.True(target.MapInboundClaims);
    Assert.Equal("select_account", target.Prompt);
    Assert.Equal("preferred_username", target.TokenValidationParameters.NameClaimType);
    Assert.Equal("groups", target.TokenValidationParameters.RoleClaimType);
    Assert.Equal(["https://issuer-a", "https://issuer-b"], target.TokenValidationParameters.ValidIssuers);
    Assert.Equal(["aud-a"], target.TokenValidationParameters.ValidAudiences);
    Assert.Equal(TimeSpan.FromSeconds(60), target.TokenValidationParameters.ClockSkew);
  }

  // Teste para verificar que o callback do consumidor executa por último e pode sobrescrever os padrões
  [Fact]
  public void ConfigureHandler_OpenIdConnect_InvokesConsumerCallbackLast()
  {
    // Arrange
    var options = CreateValidOptions();
    OpenIdConnectOptions? received = null;
    options.ConfigureOpenIdConnect = target =>
    {
      received = target;
      target.ResponseMode = OpenIdConnectResponseMode.Query;
      target.Scope.Add("custom");
    };
    var target = new OpenIdConnectOptions();

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Same(target, received);
    Assert.Equal(OpenIdConnectResponseMode.Query, target.ResponseMode);
    Assert.Contains("custom", target.Scope);
    Assert.Equal(OpenIdConnectResponseType.Code, target.ResponseType);
  }

  #endregion

  #region ConfigureHandler (JwtBearer)

  // Teste para verificar que o handler de API recebe os padrões Tooark
  [Fact]
  public void ConfigureHandler_JwtBearer_AppliesTooarkDefaults()
  {
    // Arrange
    var options = CreateValidOptions();
    var target = new JwtBearerOptions();

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Equal("https://idp.example.com/realms/tooark", target.Authority);
    Assert.Null(target.MetadataAddress);
    Assert.True(target.RequireHttpsMetadata);
    Assert.False(target.SaveToken);
    Assert.False(target.MapInboundClaims);
    Assert.Equal("name", target.TokenValidationParameters.NameClaimType);
    Assert.Equal("roles", target.TokenValidationParameters.RoleClaimType);
    Assert.True(target.TokenValidationParameters.ValidateIssuer);
    Assert.True(target.TokenValidationParameters.ValidateAudience);
    Assert.True(target.TokenValidationParameters.ValidateLifetime);
    Assert.Equal(TimeSpan.FromSeconds(300), target.TokenValidationParameters.ClockSkew);
    Assert.Null(target.TokenValidationParameters.ValidIssuers);
    Assert.Equal(["client-id"], target.TokenValidationParameters.ValidAudiences);
  }

  // Teste para verificar que as opções configuradas chegam ao handler de API
  [Fact]
  public void ConfigureHandler_JwtBearer_AppliesConfiguredValues()
  {
    // Arrange
    var options = CreateValidOptions();
    options.MetadataAddress = "https://idp.example.com/custom/openid-configuration";
    options.SaveTokens = true;
    options.RequireHttpsMetadata = false;
    options.MapInboundClaims = true;
    options.NameClaimType = "sub";
    options.RoleClaimType = "role";
    options.ValidIssuers = ["https://issuer-a"];
    options.ValidAudiences = ["aud-a", "aud-b"];
    options.ClockSkewSeconds = 0;
    var target = new JwtBearerOptions();

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Equal("https://idp.example.com/custom/openid-configuration", target.MetadataAddress);
    Assert.True(target.SaveToken);
    Assert.False(target.RequireHttpsMetadata);
    Assert.True(target.MapInboundClaims);
    Assert.Equal("sub", target.TokenValidationParameters.NameClaimType);
    Assert.Equal("role", target.TokenValidationParameters.RoleClaimType);
    Assert.Equal(["https://issuer-a"], target.TokenValidationParameters.ValidIssuers);
    Assert.Equal(["aud-a", "aud-b"], target.TokenValidationParameters.ValidAudiences);
    Assert.Equal(TimeSpan.Zero, target.TokenValidationParameters.ClockSkew);
  }

  // Teste para verificar que o callback do consumidor executa por último no handler de API
  [Fact]
  public void ConfigureHandler_JwtBearer_InvokesConsumerCallbackLast()
  {
    // Arrange
    var options = CreateValidOptions();
    JwtBearerOptions? received = null;
    options.ConfigureJwtBearer = target =>
    {
      received = target;
      target.TokenValidationParameters.ValidateAudience = false;
    };
    var target = new JwtBearerOptions();

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Same(target, received);
    Assert.False(target.TokenValidationParameters.ValidateAudience);
  }

  #endregion
}
