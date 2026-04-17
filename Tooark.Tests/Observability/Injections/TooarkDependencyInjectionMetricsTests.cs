using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Tooark.Observability.Injections;
using Tooark.Observability.Options;

namespace Tooark.Tests.Observability.Injections;

public class TooarkDependencyInjectionMetricsTests
{
  #region Helpers

  /// <summary>
  /// Cria opções de observabilidade para testes de métricas.
  /// </summary>
  /// <param name="runtimeMetricsEnabled">Permite habilitar ou desabilitar métricas de runtime.</param>
  /// <param name="meterName">Nome do medidor principal.</param>
  /// <param name="additionalMeters">Medidores adicionais a serem configurados.</param>
  /// <param name="useConsoleExporterInDev">Indica se o console exporter deve ser usado em ambiente de desenvolvimento.</param>
  /// <param name="otlpEnabled">Indica se o exportador OTLP deve ser habilitado.</param>
  /// <param name="otlpEndpoint">Endpoint do exportador OTLP.</param>
  /// <returns>Instância de <see cref="ObservabilityOptions"/> configurada para testes.</returns>
  private static ObservabilityOptions CreateOptions(
    bool runtimeMetricsEnabled,
    string meterName = "Tooark",
    string[]? additionalMeters = null,
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
      Metrics = new MetricsOptions
      {
        Enabled = true,
        RuntimeMetricsEnabled = runtimeMetricsEnabled,
        MeterName = meterName,
        AdditionalMeters = additionalMeters ?? []
      }
    };
  }

  #endregion

  #region ConfigureMetrics Tests

  // Teste para RuntimeMetricsEnabled = true, AdditionalMeters com itens, e callback invocado
  [Fact]
  public void ConfigureMetrics_RuntimeEnabledTrue_WithAdditionalMeters_InvokesCallback_AndBuilds()
  {
    // Arrange
    var callbackInvoked = false;
    var options = CreateOptions(
      runtimeMetricsEnabled: true,
      meterName: "MainMeter",
      additionalMeters: ["Meter.A", "Meter.B"],
      useConsoleExporterInDev: false,
      otlpEnabled: false
    );
    options.ConfigureMetrics = _ => callbackInvoked = true;
    var rb = ResourceBuilder.CreateDefault().AddService("svc");
    var builder = Sdk.CreateMeterProviderBuilder();

    // Act
    TooarkDependencyInjection.ConfigureMetrics(builder, options, rb, isDevelopment: false);

    // Assert
    Assert.True(callbackInvoked);

    // Assert
    using var provider = builder.Build();
    Assert.NotNull(provider);
  }

  // Teste para RuntimeMetricsEnabled = false, AdditionalMeters vazio
  [Fact]
  public void ConfigureMetrics_RuntimeEnabledFalse_NoAdditionalMeters_Builds()
  {
    // Arrange
    var options = CreateOptions(
      runtimeMetricsEnabled: false,
      meterName: "MainMeter",
      additionalMeters: Array.Empty<string>(),
      useConsoleExporterInDev: false,
      otlpEnabled: false
    );
    var rb = ResourceBuilder.CreateDefault().AddService("svc");
    var builder = Sdk.CreateMeterProviderBuilder();

    // Act
    TooarkDependencyInjection.ConfigureMetrics(builder, options, rb, isDevelopment: false);
    using var provider = builder.Build();

    // Assert
    Assert.NotNull(provider);
  }

  #endregion

  #region ConfigureMetricsExporters Tests

  // Teste para configurar exportadores OTLP
  [Fact]
  public void ConfigureMetricsExporters_WhenOtlpEnabled_ExecutesConfigureOtlpExporter_OnBuild()
  {
    // Arrange
    var options = CreateOptions(
      runtimeMetricsEnabled: false,
      useConsoleExporterInDev: true,
      otlpEnabled: true,
      otlpEndpoint: "http://localhost:4317"
    );
    var builder = Sdk
      .CreateMeterProviderBuilder()
      .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("svc"));

    // Act
    TooarkDependencyInjection.ConfigureMetricsExporters(builder, options, isDevelopment: false);
    using var provider = builder.Build();

    // Assert
    Assert.NotNull(provider);
  }

  // Teste para garantir que o OTLP específico de metrics pode habilitar exportação mesmo com OTLP global desabilitado.
  [Fact]
  public void ConfigureMetricsExporters_WhenMetricsOtlpOverridesGlobalDisabled_BuildsSuccessfully()
  {
    // Arrange
    var options = CreateOptions(
      runtimeMetricsEnabled: false,
      useConsoleExporterInDev: false,
      otlpEnabled: false
    );
    options.Metrics.Otlp = new OtlpOptions
    {
      Enabled = true,
      Endpoint = "http://localhost:4318",
      Protocol = "http"
    };

    var builder = Sdk
      .CreateMeterProviderBuilder()
      .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("svc"));

    // Act
    TooarkDependencyInjection.ConfigureMetricsExporters(builder, options, isDevelopment: false);
    using var provider = builder.Build();

    // Assert
    Assert.NotNull(provider);
  }

  // Teste para configurar exportador de console em desenvolvimento
  [Fact]
  public void ConfigureMetricsExporters_WhenDevAndNoOtlp_AddsConsoleExporterBranch_AndBuilds()
  {
    // Arrange
    var options = CreateOptions(
      runtimeMetricsEnabled: false,
      useConsoleExporterInDev: true,
      otlpEnabled: false
    );
    var builder = Sdk
      .CreateMeterProviderBuilder()
      .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("svc"));

    // Act
    TooarkDependencyInjection.ConfigureMetricsExporters(builder, options, isDevelopment: true);
    using var provider = builder.Build();

    // Assert
    Assert.NotNull(provider);
  }

  // Teste para não adicionar exportador de console quando não está em desenvolvimento
  [Fact]
  public void ConfigureMetricsExporters_WhenNotDev_DoesNotAddConsoleExporterBranch_AndBuilds()
  {
    // Arrange
    var options = CreateOptions(
      runtimeMetricsEnabled: false,
      useConsoleExporterInDev: true,
      otlpEnabled: false
    );
    var builder = Sdk
      .CreateMeterProviderBuilder()
      .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("svc"));

    // Act
    TooarkDependencyInjection.ConfigureMetricsExporters(builder, options, isDevelopment: false);
    using var provider = builder.Build();

    // Assert
    Assert.NotNull(provider);
  }

  // Teste para não adicionar exportador de console quando UseConsoleExporterInDev é false
  [Fact]
  public void ConfigureMetricsExporters_WhenDevButUseConsoleFalse_DoesNotAddConsoleExporterBranch_AndBuilds()
  {
    // Arrange
    var options = CreateOptions(
      runtimeMetricsEnabled: false,
      useConsoleExporterInDev: false, // impede console exporter
      otlpEnabled: false
    );

    var builder = Sdk
      .CreateMeterProviderBuilder()
      .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("svc"));

    // Act
    TooarkDependencyInjection.ConfigureMetricsExporters(builder, options, isDevelopment: true);
    using var provider = builder.Build();

    // Assert
    Assert.NotNull(provider);
  }

  #endregion
}
