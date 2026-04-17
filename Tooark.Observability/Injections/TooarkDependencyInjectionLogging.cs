using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Tooark.Observability.Options;

namespace Tooark.Observability.Injections;

/// <summary>
/// Classe para adicionar configurações de Logging ao OpenTelemetryLoggerOptions.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Configura o OpenTelemetryLoggerOptions.
  /// </summary>
  /// <remarks>
  /// Adiciona configurações de exportadores com base nas opções fornecidas.
  /// </remarks>
  /// <param name="loggerOptions">OpenTelemetryLoggerOptions a ser configurado.</param>
  /// <param name="options">Opções de Observability.</param>
  /// <param name="resourceBuilder">ResourceBuilder configurado.</param>
  /// <param name="isDevelopment">Indica se está em ambiente de desenvolvimento.</param>
  internal static void ConfigureLogging(
    OpenTelemetryLoggerOptions loggerOptions,
    ObservabilityOptions options,
    ResourceBuilder resourceBuilder,
    bool isDevelopment)
  {
    // Configura o resource
    loggerOptions.SetResourceBuilder(resourceBuilder);

    // Configura opções de logging
    loggerOptions.IncludeFormattedMessage = options.Logging.IncludeFormattedMessage;
    loggerOptions.IncludeScopes = options.Logging.IncludeScopes;
    loggerOptions.ParseStateValues = options.Logging.ParseStateValues;

    // Configura exportadores
    ConfigureLoggingExporters(loggerOptions, options, isDevelopment);

    // Aplica configurações customizadas
    options.ConfigureLogging?.Invoke(loggerOptions);
  }

  /// <summary>
  /// Configura os exportadores de logging.
  /// </summary>
  /// <param name="loggerOptions">OpenTelemetryLoggerOptions a ser configurado.</param>
  /// <param name="options">Opções de Observability.</param>
  /// <param name="isDevelopment">Indica se está em ambiente de desenvolvimento.</param>
  internal static void ConfigureLoggingExporters(
    OpenTelemetryLoggerOptions loggerOptions,
    ObservabilityOptions options,
    bool isDevelopment)
  {
    var effectiveOtlpOptions = ResolveOtlpOptions(options, options.Logging.Otlp);

    // Rastreia se algum exportador foi configurado
    var hasOtlpExporter = false;

    // Configura OTLP exporter se habilitado
    if (effectiveOtlpOptions.Enabled)
    {
      // Configura OTLP exporter
      loggerOptions.AddOtlpExporter(exporterOptions =>
      {
        ConfigureOtlpExporter(exporterOptions, effectiveOtlpOptions);
      });

      hasOtlpExporter = true;
    }

    // Console exporter em desenvolvimento se não houver OTLP configurado
    if (isDevelopment && options.UseConsoleExporterInDevelopment && !hasOtlpExporter)
    {
      loggerOptions.AddConsoleExporter();
    }
  }
}
