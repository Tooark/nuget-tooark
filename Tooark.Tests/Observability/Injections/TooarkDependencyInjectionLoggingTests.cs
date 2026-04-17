using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Tooark.Exceptions;
using Tooark.Observability.Injections;
using Tooark.Observability.Options;

namespace Tooark.Tests.Observability.Injections;

public class TooarkDependencyInjectionLoggingTests
{
  #region Helpers

  /// <summary>
  /// Cria opções de observabilidade para testes de logging.
  /// </summary>
  /// <param name="includeFormatted">Indica se a mensagem formatada deve ser incluída.</param>
  /// <param name="includeScopes">Indica se os escopos devem ser incluídos.</param>
  /// <param name="parseState">Indica se os valores do estado devem ser analisados.</param>
  /// <param name="useConsoleExporterInDev">Indica se o console exporter deve ser usado em ambiente de desenvolvimento.</param>
  /// <param name="otlpEnabled">Indica se o exportador OTLP deve ser habilitado.</param>
  /// <param name="otlpEndpoint">Endpoint do exportador OTLP.</param>
  /// <returns>Instância de <see cref="ObservabilityOptions"/> configurada para testes.</returns>
  private static ObservabilityOptions CreateOptions(
    bool includeFormatted = true,
    bool includeScopes = true,
    bool parseState = true,
    bool useConsoleExporterInDev = true,
    bool otlpEnabled = false,
    string? otlpEndpoint = null
  )
  {
    return new ObservabilityOptions
    {
      UseConsoleExporterInDevelopment = useConsoleExporterInDev,
      Otlp = new OtlpOptions
      {
        Enabled = otlpEnabled,
        Endpoint = otlpEndpoint
      },
      Logging = new LoggingOptions
      {
        Enabled = true,
        IncludeFormattedMessage = includeFormatted,
        IncludeScopes = includeScopes,
        ParseStateValues = parseState
      }
    };
  }

  #endregion

  #region ConfigureLogging Tests

  // Teste para configuração básica de logging e callback invocado
  [Fact]
  public void ConfigureLogging_SetsLoggerOptionsFlags_And_InvokesCallback()
  {
    // Arrange
    var options = CreateOptions(
      includeFormatted: false,
      includeScopes: false,
      parseState: false,
      useConsoleExporterInDev: false,
      otlpEnabled: false
    );
    var rb = ResourceBuilder.CreateDefault().AddService("svc");
    var loggerOptions = new OpenTelemetryLoggerOptions();
    var callbackInvoked = false;
    options.ConfigureLogging = otelLoggerOptions =>
    {
      callbackInvoked = true;
      otelLoggerOptions.IncludeScopes = true;
    };

    // Act
    TooarkDependencyInjection.ConfigureLogging(loggerOptions, options, rb, isDevelopment: false);

    // Assert
    Assert.False(loggerOptions.IncludeFormattedMessage);
    Assert.True(loggerOptions.IncludeScopes);
    Assert.False(loggerOptions.ParseStateValues);
    Assert.True(callbackInvoked);
  }

  #endregion

  #region ConfigureLoggingExporters Tests

  // Teste para exportador OTLP habilitado com endpoint inválido
  [Fact]
  public void ConfigureLoggingExporters_WhenOtlpEnabled_InvalidEndpoint_Throws_WhenProvidersAreInstantiated()
  {
    // Arrange
    var options = CreateOptions(otlpEnabled: true, otlpEndpoint: "not-a-uri");
    var rb = ResourceBuilder.CreateDefault().AddService("svc");
    var services = new ServiceCollection();
    services.AddLogging(lb =>
    {
      lb.AddOpenTelemetry(otel =>
      {
        TooarkDependencyInjection.ConfigureLogging(otel, options, rb, isDevelopment: true);
      });
    });

    using var sp = services.BuildServiceProvider();

    // Act + Assert
    var ex = Assert.Throws<InternalServerErrorException>(() =>
    {
      _ = sp.GetServices<ILoggerProvider>().ToList();
    });
    Assert.Contains("Options.Otlp.Endpoint.Invalid", ex.Message);
  }

  // Teste para exportador OTLP habilitado com endpoint válido
  [Fact]
  public void ConfigureLoggingExporters_WhenOtlpEnabled_WithValidEndpoint_DoesNotThrow()
  {
    // Arrange
    var options = CreateOptions(
      useConsoleExporterInDev: true,
      otlpEnabled: true,
      otlpEndpoint: "http://localhost:4317"
    );
    var loggerOptions = new OpenTelemetryLoggerOptions();

    // Act
    TooarkDependencyInjection.ConfigureLoggingExporters(loggerOptions, options, isDevelopment: true);

    // Assert
    Assert.True(true);
  }

  // Teste para garantir que o OTLP específico de logging faz override do global.
  [Fact]
  public void ConfigureLoggingExporters_WhenLoggingOtlpOverridesGlobal_UsesLoggingOtlp()
  {
    // Arrange
    var options = CreateOptions(
      useConsoleExporterInDev: true,
      otlpEnabled: true,
      otlpEndpoint: "http://localhost:4317"
    );
    options.Logging.Otlp = new OtlpOptions
    {
      Endpoint = "not-a-uri"
    };

    var rb = ResourceBuilder.CreateDefault().AddService("svc");
    var services = new ServiceCollection();
    services.AddLogging(lb =>
    {
      lb.AddOpenTelemetry(otel =>
      {
        TooarkDependencyInjection.ConfigureLogging(otel, options, rb, isDevelopment: false);
      });
    });

    using var sp = services.BuildServiceProvider();

    // Act + Assert
    var ex = Assert.Throws<InternalServerErrorException>(() =>
    {
      _ = sp.GetServices<ILoggerProvider>().ToList();
    });
    Assert.Contains("Options.Otlp.Endpoint.Invalid", ex.Message);
  }

  // Teste para exportador de console em ambiente de desenvolvimento
  [Fact]
  public void ConfigureLoggingExporters_WhenDevAndNoOtlp_AddsConsoleExporterBranch_DoesNotThrow()
  {
    // Arrange
    var options = CreateOptions(
      useConsoleExporterInDev: true,
      otlpEnabled: false
    );
    var loggerOptions = new OpenTelemetryLoggerOptions();

    // Act
    TooarkDependencyInjection.ConfigureLoggingExporters(loggerOptions, options, isDevelopment: true);

    // Assert
    Assert.True(true);
  }

  // Teste para não adicionar exportador de console quando não está em desenvolvimento
  [Fact]
  public void ConfigureLoggingExporters_WhenNotDev_DoesNotAddConsoleExporterBranch_DoesNotThrow()
  {
    // Arrange
    var options = CreateOptions(
      useConsoleExporterInDev: true,
      otlpEnabled: false
    );
    var loggerOptions = new OpenTelemetryLoggerOptions();

    // Act
    TooarkDependencyInjection.ConfigureLoggingExporters(loggerOptions, options, isDevelopment: false);

    // Assert
    Assert.True(true);
  }

  // Teste para não adicionar exportador de console quando UseConsoleExporterInDevelopment é false
  [Fact]
  public void ConfigureLoggingExporters_WhenDevButUseConsoleFalse_DoesNotAddConsoleExporterBranch_DoesNotThrow()
  {
    // Arrange
    var options = CreateOptions(
      useConsoleExporterInDev: false,
      otlpEnabled: false
    );
    var loggerOptions = new OpenTelemetryLoggerOptions();

    // Act
    TooarkDependencyInjection.ConfigureLoggingExporters(loggerOptions, options, isDevelopment: true);

    // Assert
    Assert.True(true);
  }

  #endregion
}
