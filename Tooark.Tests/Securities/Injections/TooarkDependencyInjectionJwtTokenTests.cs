using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Securities.Injections;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Injections;

public class TooarkDependencyInjectionJwtTokenTests
{
  // Teste para verificar se o método AddTooarkJwtToken adiciona o serviço corretamente.
  [Fact]
  public void AddTooarkJwtToken_ShouldAddServices()
  {
    // Arrange
    var services = new ServiceCollection();
    var inMemorySettings = new Dictionary<string, string?>
    {
      { $"{JwtOptions.Section}:Algorithm", "HS256" },
      { $"{JwtOptions.Section}:Issuer", "issuer-test" },
      { $"{JwtOptions.Section}:Audience", "audience-test" },
      { $"{JwtOptions.Section}:Secret", "secret-test-1234567890-ABCDEFGHIJ" },
      { $"{JwtOptions.Section}:ExpirationTime", "60" }
    };

    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(inMemorySettings)
      .Build();

    // Act
    services.AddTooarkJwtToken(configuration);
    var provider = services.BuildServiceProvider();
    var jwtService = provider.GetService<IJwtTokenService>();
    var options = provider.GetService<Microsoft.Extensions.Options.IOptions<JwtOptions>>();

    // Assert
    Assert.NotNull(jwtService);
    Assert.NotNull(options);
    Assert.Equal("HS256", options.Value.Algorithm);
    Assert.Equal("issuer-test", options.Value.Issuer);
    Assert.Equal("audience-test", options.Value.Audience);
    Assert.Equal("secret-test-1234567890-ABCDEFGHIJ", options.Value.Secret);
    Assert.Equal(60, options.Value.ExpirationTime);
  }
}
