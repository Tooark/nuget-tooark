using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.AspNetCore.ExceptionHandling;
using Tooark.AspNetCore.Injections;

namespace Tooark.Tests.AspNetCore.Injections;

/// <summary>
/// Testes do registro do Problem Details pelo AddTooarkProblemDetails.
/// </summary>
public class ProblemDetailsInjectionTests
{
  // Testa se o registro traz o serviço de Problem Details do ASP.NET Core e o manipulador do Tooark
  [Fact]
  public void AddTooarkProblemDetails_ShouldRegisterServiceAndHandler()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddTooarkProblemDetails();
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.NotNull(provider.GetService<IProblemDetailsService>());
    Assert.Single(provider.GetServices<IExceptionHandler>().OfType<TooarkExceptionHandler>());
  }

  // Testa se chamar o registro duas vezes mantém um único manipulador: dois iguais responderiam a mesma exceção
  [Fact]
  public void AddTooarkProblemDetails_ShouldBeIdempotent()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddTooarkProblemDetails();
    services.AddTooarkProblemDetails();

    // Assert
    Assert.Single(services, descriptor =>
      descriptor.ServiceType == typeof(IExceptionHandler) && descriptor.ImplementationType == typeof(TooarkExceptionHandler));
  }

  // Testa se a configuração das opções do ASP.NET Core chega ao container
  [Fact]
  public void AddTooarkProblemDetails_ShouldApplyConfigure()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddTooarkProblemDetails(options => options.CustomizeProblemDetails = problem => problem.ProblemDetails.Title = "x");
    using var provider = services.BuildServiceProvider();

    // Assert
    Assert.NotNull(provider.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value.CustomizeProblemDetails);
  }

  // Testa se o manipulador é construído sem o logging e sem o localizador no container: todos são opcionais
  [Fact]
  public void AddTooarkProblemDetails_ShouldResolveHandler_WithoutOptionalServices()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTooarkProblemDetails();
    using var provider = services.BuildServiceProvider();

    // Act
    var handlers = provider.GetServices<IExceptionHandler>();

    // Assert
    Assert.Single(handlers.OfType<TooarkExceptionHandler>());
  }
}
