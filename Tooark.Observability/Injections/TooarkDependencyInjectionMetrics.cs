using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Tooark.Observability.Options;

namespace Tooark.Observability.Injections;

/// <summary>
/// Classe para adicionar configurações de Metrics ao MeterProviderBuilder.
/// </summary>
public static partial class TooarkDependencyInjection
{
  #region Configure Metrics

  /// <summary>
  /// Configura o MeterProviderBuilder.
  /// </summary>
  /// <remarks>
  /// Adiciona instrumentations, meters e exportadores conforme as opções fornecidas.
  /// </remarks>
  /// <param name="builder">MeterProviderBuilder a ser configurado.</param>
  /// <param name="options">Opções de Observability.</param>
  /// <param name="resourceBuilder">ResourceBuilder configurado.</param>
  /// <param name="isDevelopment">Indica se está em ambiente de desenvolvimento.</param>
  internal static void ConfigureMetrics(
    MeterProviderBuilder builder,
    ObservabilityOptions options,
    ResourceBuilder resourceBuilder,
    bool isDevelopment
  )
  {
    // Configura o resource
    builder.SetResourceBuilder(resourceBuilder);

    // Adiciona instrumentation padrão
    builder.AddAspNetCoreInstrumentation();
    builder.AddHttpClientInstrumentation();

    // Adiciona métricas de runtime se habilitado
    if (options.Metrics.RuntimeMetricsEnabled)
    {
      builder.AddRuntimeInstrumentation();
    }

    // Adiciona métricas de processo se habilitado
    if (options.Metrics.ProcessMetricsEnabled)
    {
      builder.AddProcessInstrumentation();
    }

    // Adiciona meters padrão e customizados
    builder.AddMeter(options.Metrics.MeterName);
    foreach (var meter in options.Metrics.AdditionalMeters)
    {
      builder.AddMeter(meter);
    }

    // Configura exportadores
    ConfigureMetricsExporters(builder, options, isDevelopment);

    // Aplica configurações customizadas
    options.ConfigureMetrics?.Invoke(builder);
  }

  #endregion

  #region Configure OTLP Exporter

  /// <summary>
  /// Configura os exportadores de metrics.
  /// </summary>
  /// <param name="builder">MeterProviderBuilder a ser configurado.</param>
  /// <param name="options">Opções de Observability.</param>
  /// <param name="isDevelopment">Indica se está em ambiente de desenvolvimento.</param>
  internal static void ConfigureMetricsExporters(
    MeterProviderBuilder builder,
    ObservabilityOptions options,
    bool isDevelopment
  )
  {
    var effectiveOtlpOptions = ResolveOtlpOptions(options, options.Metrics.Otlp);

    // Rastreia se algum exportador foi configurado
    var hasOtlpExporter = false;

    // Configura OTLP exporter se habilitado
    if (effectiveOtlpOptions.Enabled)
    {
      // Configura OTLP exporter
      builder.AddOtlpExporter(exporterOptions =>
      {
        ConfigureOtlpExporter(exporterOptions, effectiveOtlpOptions);
      });

      hasOtlpExporter = true;
    }

    // Console exporter em desenvolvimento se não houver OTLP configurado
    if (isDevelopment && options.UseConsoleExporterInDevelopment && !hasOtlpExporter)
    {
      builder.AddConsoleExporter();
    }
  }

  #endregion
}
