using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Tooark.Exceptions;
using Tooark.Securities.OpenId.Options;

namespace Tooark.Tests.Securities.OpenId.Options;

public class GoogleOptionsTests
{
  #region Helpers

  /// <summary>
  /// Cria opções válidas para o preset do Google.
  /// </summary>
  /// <param name="hostedDomain">Domínio do Google Workspace, quando a restrição deve ser exercitada.</param>
  /// <returns>Instância de <see cref="GoogleOptions"/> pronta para validar.</returns>
  private static GoogleOptions CreateValidOptions(string? hostedDomain = null) => new()
  {
    ClientId = "client-id",
    ClientSecret = "client-secret",
    HostedDomain = hostedDomain
  };

  /// <summary>
  /// Cria um usuário autenticado com a claim <c>hd</c> informada.
  /// </summary>
  /// <param name="hostedDomain">Valor da claim <c>hd</c>, ou nulo para uma conta sem a claim.</param>
  /// <returns>Usuário autenticado.</returns>
  private static ClaimsPrincipal CreatePrincipal(string? hostedDomain)
  {
    var claims = new List<Claim> { new("sub", "123"), new("email", "user@tooark.com") };

    if (hostedDomain is not null)
    {
      claims.Add(new Claim("hd", hostedDomain));
    }

    return new ClaimsPrincipal(new ClaimsIdentity(claims, "Google"));
  }

  /// <summary>
  /// Cria o contexto de redirecionamento do handler interativo.
  /// </summary>
  /// <param name="options">Opções do handler.</param>
  /// <returns>Contexto com uma mensagem de protocolo vazia.</returns>
  private static RedirectContext CreateRedirectContext(OpenIdConnectOptions options) => new(
    new DefaultHttpContext(),
    new AuthenticationScheme("Google", "Google", typeof(OpenIdConnectHandler)),
    options,
    new AuthenticationProperties()
  )
  {
    ProtocolMessage = new OpenIdConnectMessage()
  };

  /// <summary>
  /// Cria o contexto de token validado do handler interativo.
  /// </summary>
  /// <param name="options">Opções do handler.</param>
  /// <param name="principal">Usuário autenticado.</param>
  /// <returns>Contexto de token validado.</returns>
  private static Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext CreateOidcTokenValidatedContext(
    OpenIdConnectOptions options,
    ClaimsPrincipal principal
  ) => new(
    new DefaultHttpContext(),
    new AuthenticationScheme("Google", "Google", typeof(OpenIdConnectHandler)),
    options,
    principal,
    new AuthenticationProperties()
  );

  /// <summary>
  /// Cria o contexto de token validado do handler de API.
  /// </summary>
  /// <param name="options">Opções do handler.</param>
  /// <param name="principal">Usuário autenticado.</param>
  /// <returns>Contexto de token validado.</returns>
  private static Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext CreateBearerTokenValidatedContext(
    JwtBearerOptions options,
    ClaimsPrincipal principal
  ) => new(
    new DefaultHttpContext(),
    new AuthenticationScheme("Google", "Google", typeof(JwtBearerHandler)),
    options
  )
  {
    Principal = principal
  };

  #endregion

  #region Defaults

  // Teste para verificar os padrões específicos do Google
  [Fact]
  public void Defaults_ShouldMatchGoogleStandard()
  {
    // Arrange & Act
    var options = new GoogleOptions();

    // Assert
    Assert.Equal("Google", GoogleOptions.DefaultName);
    Assert.Equal("Google", options.DisplayName);
    Assert.Equal("https://accounts.google.com", options.Authority);
    Assert.Equal("email", options.NameClaimType);
    Assert.Null(options.HostedDomain);
  }

  // Teste para verificar que o domínio é limpo de espaços e vazio vira nulo
  [Theory]
  [InlineData("tooark.com", "tooark.com")]
  [InlineData("  tooark.com  ", "tooark.com")]
  [InlineData("", null)]
  [InlineData("   ", null)]
  [InlineData(null, null)]
  public void HostedDomain_SetValue_TrimsAndNormalizesEmpty(string? input, string? expected)
  {
    // Arrange & Act
    var options = new GoogleOptions { HostedDomain = input };

    // Assert
    Assert.Equal(expected, options.HostedDomain);
  }

  #endregion

  #region ResolveValidIssuers

  // Teste para verificar que as duas formas de emissor do Google são aceitas
  [Fact]
  public void ResolveValidIssuers_WhenNotConfigured_ReturnsBothGoogleIssuers()
  {
    // Arrange
    var options = new GoogleOptions();

    // Act & Assert
    Assert.Equal(["https://accounts.google.com", "accounts.google.com"], options.ResolveValidIssuers()!);
  }

  // Teste para verificar que emissores configurados têm prioridade
  [Fact]
  public void ResolveValidIssuers_WhenConfigured_ReturnsConfigured()
  {
    // Arrange
    var options = new GoogleOptions { ValidIssuers = ["https://issuer-a"] };

    // Act & Assert
    Assert.Equal(["https://issuer-a"], options.ResolveValidIssuers()!);
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

  // Teste para verificar que o handler interativo exige o segredo
  [Fact]
  public void Validate_Interactive_WithoutClientSecret_ShouldThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.ClientSecret = null;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.ClientSecretNotConfigured");
  }

  // Teste para verificar que o handler de API dispensa o segredo
  [Fact]
  public void Validate_Bearer_WithoutClientSecret_ShouldNotThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.ClientSecret = null;

    // Act & Assert
    options.Validate(false);
  }

  #endregion

  #region ConfigureHandler (OpenIdConnect)

  // Teste para verificar que sem domínio os eventos do handler não são alterados
  [Fact]
  public void ConfigureHandler_OpenIdConnect_WithoutHostedDomain_KeepsEvents()
  {
    // Arrange
    var options = CreateValidOptions();
    var target = new OpenIdConnectOptions();
    var events = target.Events;

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Same(events, target.Events);
    Assert.Equal("https://accounts.google.com", target.Authority);
    Assert.Equal("email", target.TokenValidationParameters.NameClaimType);
    Assert.Equal(["https://accounts.google.com", "accounts.google.com"], target.TokenValidationParameters.ValidIssuers);
  }

  // Teste para verificar que o domínio é sugerido na tela de login
  [Fact]
  public async Task ConfigureHandler_OpenIdConnect_WithHostedDomain_SetsHdParameter()
  {
    // Arrange
    var options = CreateValidOptions("tooark.com");
    var target = new OpenIdConnectOptions();
    options.ConfigureHandler(target);
    var context = CreateRedirectContext(target);

    // Act
    await target.Events.OnRedirectToIdentityProvider(context);

    // Assert
    Assert.Equal("tooark.com", context.ProtocolMessage.GetParameter("hd"));
  }

  // Teste para verificar que o token de outro domínio, ou sem domínio, é rejeitado
  [Theory]
  [InlineData("outro.com")]
  [InlineData(null)]
  public async Task ConfigureHandler_OpenIdConnect_WithHostedDomain_RejectsOtherDomains(string? claim)
  {
    // Arrange
    var options = CreateValidOptions("tooark.com");
    var target = new OpenIdConnectOptions();
    options.ConfigureHandler(target);
    var context = CreateOidcTokenValidatedContext(target, CreatePrincipal(claim));

    // Act
    await target.Events.OnTokenValidated(context);

    // Assert
    Assert.NotNull(context.Result?.Failure);
    Assert.Equal("OpenId.HostedDomain.Invalid", context.Result.Failure.Message);
  }

  // Teste para verificar que o token do domínio configurado é aceito, ignorando maiúsculas
  [Theory]
  [InlineData("tooark.com")]
  [InlineData("TOOARK.COM")]
  public async Task ConfigureHandler_OpenIdConnect_WithHostedDomain_AcceptsConfiguredDomain(string claim)
  {
    // Arrange
    var options = CreateValidOptions("tooark.com");
    var target = new OpenIdConnectOptions();
    options.ConfigureHandler(target);
    var context = CreateOidcTokenValidatedContext(target, CreatePrincipal(claim));

    // Act
    await target.Events.OnTokenValidated(context);

    // Assert
    Assert.Null(context.Result);
  }

  // Teste para verificar que a restrição sobrevive ao consumidor substituir os eventos por completo
  [Fact]
  public async Task ConfigureHandler_OpenIdConnect_WithHostedDomain_WrapsConsumerEvents()
  {
    // Arrange
    var consumerInvocations = new List<string>();
    var options = CreateValidOptions("tooark.com");
    options.ConfigureOpenIdConnect = target => target.Events = new OpenIdConnectEvents
    {
      OnTokenValidated = context =>
      {
        consumerInvocations.Add(context.Principal!.FindFirst("hd")?.Value ?? "none");

        return Task.CompletedTask;
      }
    };
    var target = new OpenIdConnectOptions();
    options.ConfigureHandler(target);

    // Act
    await target.Events.OnTokenValidated(CreateOidcTokenValidatedContext(target, CreatePrincipal("outro.com")));
    await target.Events.OnTokenValidated(CreateOidcTokenValidatedContext(target, CreatePrincipal("tooark.com")));

    // Assert - o handler do consumidor só roda para o domínio aceito
    Assert.Equal(["tooark.com"], consumerInvocations);
  }

  #endregion

  #region ConfigureHandler (JwtBearer)

  // Teste para verificar que sem domínio os eventos do handler de API não são alterados
  [Fact]
  public void ConfigureHandler_JwtBearer_WithoutHostedDomain_KeepsEvents()
  {
    // Arrange
    var options = CreateValidOptions();
    var target = new JwtBearerOptions();
    var events = target.Events;

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Same(events, target.Events);
    Assert.Equal(["https://accounts.google.com", "accounts.google.com"], target.TokenValidationParameters.ValidIssuers);
    Assert.Equal(["client-id"], target.TokenValidationParameters.ValidAudiences);
  }

  // Teste para verificar que o handler de API rejeita tokens de outro domínio
  [Theory]
  [InlineData("outro.com")]
  [InlineData(null)]
  public async Task ConfigureHandler_JwtBearer_WithHostedDomain_RejectsOtherDomains(string? claim)
  {
    // Arrange
    var options = CreateValidOptions("tooark.com");
    var target = new JwtBearerOptions();
    options.ConfigureHandler(target);
    var context = CreateBearerTokenValidatedContext(target, CreatePrincipal(claim));

    // Act
    await target.Events.OnTokenValidated(context);

    // Assert
    Assert.NotNull(context.Result?.Failure);
    Assert.Equal("OpenId.HostedDomain.Invalid", context.Result.Failure.Message);
  }

  // Teste para verificar que o handler de API aceita o domínio configurado e chama o consumidor
  [Fact]
  public async Task ConfigureHandler_JwtBearer_WithHostedDomain_AcceptsConfiguredDomainAndInvokesConsumer()
  {
    // Arrange
    var consumerInvoked = false;
    var options = CreateValidOptions("tooark.com");
    options.ConfigureJwtBearer = target => target.Events = new JwtBearerEvents
    {
      OnTokenValidated = _ =>
      {
        consumerInvoked = true;

        return Task.CompletedTask;
      }
    };
    var target = new JwtBearerOptions();
    options.ConfigureHandler(target);
    var context = CreateBearerTokenValidatedContext(target, CreatePrincipal("tooark.com"));

    // Act
    await target.Events.OnTokenValidated(context);

    // Assert
    Assert.Null(context.Result);
    Assert.True(consumerInvoked);
  }

  #endregion
}
