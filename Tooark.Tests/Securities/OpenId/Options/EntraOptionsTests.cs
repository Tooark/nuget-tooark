using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Tooark.Exceptions;
using Tooark.Securities.OpenId.Options;

namespace Tooark.Tests.Securities.OpenId.Options;

public class EntraOptionsTests
{
  #region Helpers

  /// <summary>
  /// Tenant de exemplo, no formato GUID que o emissor v1 exige.
  /// </summary>
  private const string TenantId = "11111111-2222-3333-4444-555555555555";

  /// <summary>
  /// Cria opções válidas para o preset do Entra.
  /// </summary>
  /// <returns>Instância de <see cref="EntraOptions"/> pronta para validar.</returns>
  private static EntraOptions CreateValidOptions() => new()
  {
    TenantId = TenantId,
    ClientId = "client-id",
    ClientSecret = "client-secret"
  };

  #endregion

  #region Defaults

  // Teste para verificar os padrões específicos do Entra
  [Fact]
  public void Defaults_ShouldMatchEntraStandard()
  {
    // Arrange & Act
    var options = new EntraOptions();

    // Assert
    Assert.Equal("Entra", EntraOptions.DefaultName);
    Assert.Equal("Microsoft Entra ID", options.DisplayName);
    Assert.Equal("https://login.microsoftonline.com/", options.Instance);
    Assert.Null(options.TenantId);
    Assert.Null(options.Authority);
    Assert.Equal("preferred_username", options.NameClaimType);
    Assert.Equal("roles", options.RoleClaimType);
    Assert.False(options.IsMultiTenant);
  }

  #endregion

  #region Authority / Instance / TenantId

  // Teste para verificar que a Authority é derivada do tenant
  [Fact]
  public void Authority_WhenNotConfigured_IsDerivedFromTenant()
  {
    // Arrange
    var options = new EntraOptions { TenantId = TenantId };

    // Act & Assert
    Assert.Equal($"https://login.microsoftonline.com/{TenantId}/v2.0", options.Authority);
  }

  // Teste para verificar que a Authority explícita tem prioridade sobre a derivada
  [Fact]
  public void Authority_WhenConfigured_TakesPrecedence()
  {
    // Arrange
    var options = new EntraOptions { TenantId = TenantId, Authority = "https://custom.example.com/v2.0" };

    // Act & Assert
    Assert.Equal("https://custom.example.com/v2.0", options.Authority);
  }

  // Teste para verificar que a instância soberana compõe a Authority
  [Fact]
  public void Authority_UsesConfiguredInstance()
  {
    // Arrange
    var options = new EntraOptions { TenantId = TenantId, Instance = "https://login.microsoftonline.us/" };

    // Act & Assert
    Assert.Equal($"https://login.microsoftonline.us/{TenantId}/v2.0", options.Authority);
  }

  // Teste para verificar que a instância ganha a barra final e cai no padrão quando vazia
  [Theory]
  [InlineData("https://login.microsoftonline.us", "https://login.microsoftonline.us/")]
  [InlineData("https://login.microsoftonline.us/", "https://login.microsoftonline.us/")]
  [InlineData("  https://login.microsoftonline.us  ", "https://login.microsoftonline.us/")]
  [InlineData("", "https://login.microsoftonline.com/")]
  [InlineData("   ", "https://login.microsoftonline.com/")]
  [InlineData(null, "https://login.microsoftonline.com/")]
  public void Instance_SetValue_NormalizesTrailingSlash(string? input, string expected)
  {
    // Arrange & Act
    var options = new EntraOptions { Instance = input! };

    // Assert
    Assert.Equal(expected, options.Instance);
  }

  // Teste para verificar que o tenant é limpo de espaços
  [Fact]
  public void TenantId_SetValue_Trims()
  {
    // Arrange & Act
    var options = new EntraOptions { TenantId = $"  {TenantId}  " };

    // Assert
    Assert.Equal(TenantId, options.TenantId);
  }

  // Teste para verificar a detecção dos apelidos multi-tenant
  [Theory]
  [InlineData("common", true)]
  [InlineData("Organizations", true)]
  [InlineData("CONSUMERS", true)]
  [InlineData(TenantId, false)]
  [InlineData("contoso.onmicrosoft.com", false)]
  public void IsMultiTenant_DetectsAliases(string tenantId, bool expected)
  {
    // Arrange
    var options = new EntraOptions { TenantId = tenantId };

    // Act & Assert
    Assert.Equal(expected, options.IsMultiTenant);
  }

  #endregion

  #region ResolveValidIssuers / ResolveValidAudiences

  // Teste para verificar que um tenant na nuvem pública aceita os emissores v2 e v1
  [Fact]
  public void ResolveValidIssuers_PublicCloud_ReturnsV2AndV1Issuers()
  {
    // Arrange
    var options = new EntraOptions { TenantId = TenantId };

    // Act
    var issuers = options.ResolveValidIssuers();

    // Assert
    Assert.Equal(
      [$"https://login.microsoftonline.com/{TenantId}/v2.0", $"https://sts.windows.net/{TenantId}/"],
      issuers!
    );
  }

  // Teste para verificar que em nuvem soberana só o emissor v2 é derivado
  [Fact]
  public void ResolveValidIssuers_SovereignCloud_ReturnsOnlyV2Issuer()
  {
    // Arrange
    var options = new EntraOptions { TenantId = TenantId, Instance = "https://login.microsoftonline.us/" };

    // Act
    var issuers = options.ResolveValidIssuers();

    // Assert
    Assert.Equal([$"https://login.microsoftonline.us/{TenantId}/v2.0"], issuers!);
  }

  // Teste para verificar que emissores configurados têm prioridade sobre os derivados
  [Fact]
  public void ResolveValidIssuers_WhenConfigured_ReturnsConfigured()
  {
    // Arrange
    var options = new EntraOptions { TenantId = TenantId, ValidIssuers = ["https://issuer-a"] };

    // Act & Assert
    Assert.Equal(["https://issuer-a"], options.ResolveValidIssuers()!);
  }

  // Teste para verificar que tenant multi-tenant ou ausente não deriva emissores
  [Theory]
  [InlineData("common")]
  [InlineData(null)]
  public void ResolveValidIssuers_MultiTenantOrMissing_ReturnsNull(string? tenantId)
  {
    // Arrange
    var options = new EntraOptions { TenantId = tenantId };

    // Act & Assert
    Assert.Null(options.ResolveValidIssuers());
  }

  // Teste para verificar que o ClientId e o Application ID URI padrão são aceitos como destinatário
  [Fact]
  public void ResolveValidAudiences_WhenNotConfigured_ReturnsClientIdAndApiUri()
  {
    // Arrange
    var options = new EntraOptions { ClientId = "client-id" };

    // Act & Assert
    Assert.Equal(["client-id", "api://client-id"], options.ResolveValidAudiences());
  }

  // Teste para verificar que destinatários configurados têm prioridade
  [Fact]
  public void ResolveValidAudiences_WhenConfigured_ReturnsConfigured()
  {
    // Arrange
    var options = new EntraOptions { ClientId = "client-id", ValidAudiences = ["api://custom"] };

    // Act & Assert
    Assert.Equal(["api://custom"], options.ResolveValidAudiences());
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

  // Teste para verificar que o tenant é obrigatório e reportado antes da Authority
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Validate_WithoutTenantId_ShouldThrow(string? tenantId)
  {
    // Arrange
    var options = CreateValidOptions();
    options.TenantId = tenantId;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.Entra.TenantIdNotConfigured");
  }

  // Teste para verificar que a instância precisa ser uma URL HTTPS
  [Theory]
  [InlineData("http://login.microsoftonline.com/")]
  [InlineData("login.microsoftonline.com")]
  public void Validate_WithInvalidInstance_ShouldThrow(string instance)
  {
    // Arrange
    var options = CreateValidOptions();
    options.Instance = instance;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message.StartsWith("Options.OpenId.Entra.Instance.Invalid;"));
  }

  // Teste para verificar que multi-tenant exige emissores explícitos
  [Theory]
  [InlineData("common")]
  [InlineData("organizations")]
  [InlineData("consumers")]
  public void Validate_MultiTenantWithoutValidIssuers_ShouldThrow(string tenantId)
  {
    // Arrange
    var options = CreateValidOptions();
    options.TenantId = tenantId;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(false));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == $"Options.OpenId.Entra.MultiTenantRequiresValidIssuers;{tenantId}");
  }

  // Teste para verificar que multi-tenant com emissores explícitos é aceito
  [Fact]
  public void Validate_MultiTenantWithValidIssuers_ShouldNotThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.TenantId = "organizations";
    options.ValidIssuers = [$"https://login.microsoftonline.com/{TenantId}/v2.0"];

    // Act & Assert
    options.Validate(true);
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

  // Teste para verificar que as validações comuns continuam valendo no preset
  [Fact]
  public void Validate_WithoutClientId_ShouldThrow()
  {
    // Arrange
    var options = CreateValidOptions();
    options.ClientId = null;

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => options.Validate(true));

    // Assert
    Assert.Contains(ex.GetErrorMessages(), message => message == "Options.OpenId.ClientIdNotConfigured");
  }

  #endregion

  #region ConfigureHandler

  // Teste para verificar que o handler interativo recebe a Authority derivada e os padrões do Entra
  [Fact]
  public void ConfigureHandler_OpenIdConnect_AppliesEntraDefaults()
  {
    // Arrange
    var options = CreateValidOptions();
    var target = new OpenIdConnectOptions();

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Equal($"https://login.microsoftonline.com/{TenantId}/v2.0", target.Authority);
    Assert.Equal("preferred_username", target.TokenValidationParameters.NameClaimType);
    Assert.Equal("roles", target.TokenValidationParameters.RoleClaimType);
    Assert.Equal(
      [$"https://login.microsoftonline.com/{TenantId}/v2.0", $"https://sts.windows.net/{TenantId}/"],
      target.TokenValidationParameters.ValidIssuers
    );
    Assert.Null(target.TokenValidationParameters.ValidAudiences);
  }

  // Teste para verificar que o handler de API aceita tokens v1 e v2
  [Fact]
  public void ConfigureHandler_JwtBearer_AcceptsV1AndV2Tokens()
  {
    // Arrange
    var options = CreateValidOptions();
    var target = new JwtBearerOptions();

    // Act
    options.ConfigureHandler(target);

    // Assert
    Assert.Equal($"https://login.microsoftonline.com/{TenantId}/v2.0", target.Authority);
    Assert.Equal(
      [$"https://login.microsoftonline.com/{TenantId}/v2.0", $"https://sts.windows.net/{TenantId}/"],
      target.TokenValidationParameters.ValidIssuers
    );
    Assert.Equal(["client-id", "api://client-id"], target.TokenValidationParameters.ValidAudiences);
  }

  #endregion
}
