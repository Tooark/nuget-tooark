using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Sanitizers.Injections;
using Tooark.Sanitizers.Interfaces;

namespace Tooark.Tests.Sanitizers.Injections;

/// <summary>
/// Testes do registro dos sanitizadores no container de injeção de dependência.
/// </summary>
public class SanitizersInjectionTests
{
  // Monta a configuração a partir dos pares informados
  private static IConfiguration Configuracao(Dictionary<string, string?> valores) =>
    new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

  // Testa se os três sanitizadores são registrados sem configuração
  [Fact]
  public void AddTooarkSanitizers_ShouldRegisterServices_WithoutConfiguration()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkSanitizers();
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.Equal("<p>ok</p>", provider.GetRequiredService<IHtmlSanitizerService>().Sanitize("<p>ok</p><script>x</script>"));
    Assert.False(provider.GetRequiredService<IUrlSanitizerService>().IsSafe("javascript:alert(1)"));
    Assert.NotNull(provider.GetRequiredService<IWysiwygSanitizerService>().Sanitize("""{"type":"doc"}"""));
  }

  // Testa se as opções são lidas da seção Sanitizers
  [Fact]
  public void AddTooarkSanitizers_ShouldBindOptionsFromConfiguration()
  {
    // Arrange
    var configuracao = Configuracao(new()
    {
      ["Sanitizers:Url:AllowedSchemes:0"] = "https",
      ["Sanitizers:Url:AllowedSchemes:1"] = "mailto",
      ["Sanitizers:Url:AllowRelative"] = "true"
    });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkSanitizers(configuracao);
    using var provider = services.BuildServiceProvider();
    var url = provider.GetRequiredService<IUrlSanitizerService>();

    // Assert
    Assert.True(url.IsSafe("mailto:contato@tooark.com"));
    Assert.True(url.IsSafe("/pagina"));
    Assert.False(url.IsSafe("http://tooark.com"));
  }

  // Testa se o ajuste programático vale por cima da configuração
  [Fact]
  public void AddTooarkSanitizers_ShouldApplyConfigureAfterConfiguration()
  {
    // Arrange
    var configuracao = Configuracao(new() { ["Sanitizers:Url:AllowRelative"] = "true" });
    var services = new ServiceCollection();

    // Act
    services.AddTooarkSanitizers(configuracao, opcoes => opcoes.Url.AllowRelative = false);
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.False(provider.GetRequiredService<IUrlSanitizerService>().IsSafe("/pagina"));
  }

  // Testa se a configuração insegura falha no registro, e não no primeiro uso
  [Fact]
  public void AddTooarkSanitizers_ShouldThrowOnRegistration_WhenConfigurationIsUnsafe()
  {
    // Arrange
    var configuracao = Configuracao(new() { ["Sanitizers:Html:AllowedTags:0"] = "script" });
    var services = new ServiceCollection();

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkSanitizers(configuracao));

    // Assert
    Assert.Contains("Options.Sanitizers.Html.TagNotAllowed;script", ex.GetErrorMessages());
  }

  // Testa se o registro repetido não duplica os serviços
  [Fact]
  public void AddTooarkSanitizers_ShouldNotDuplicateServices_WhenCalledTwice()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkSanitizers();
    services.AddTooarkSanitizers();

    // Assert
    Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IHtmlSanitizerService));
    Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IUrlSanitizerService));
    Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IWysiwygSanitizerService));
  }
}
