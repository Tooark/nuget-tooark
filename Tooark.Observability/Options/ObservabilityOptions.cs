using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Tooark.Observability.Options;

/// <summary>
/// Opções principais de configuração para observabilidade.
/// </summary>
public class ObservabilityOptions
{
  #region Section

  /// <summary>
  /// Seção de configuração no appsettings.json.
  /// </summary>
  public const string Section = "Observability";

  #endregion

  #region Properties

  /// <summary>
  /// Indica se a observabilidade está habilitada. Padrão: true.
  /// </summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Nome do serviço para identificação nos traces/metrics/logs.
  /// </summary>
  /// <remarks>
  /// Se não informado, será inferido do AssemblyName ou "unknown_service".
  /// </remarks>
  public string? ServiceName { get; set; }

  /// <summary>
  /// Versão do serviço para identificação nos traces/metrics/logs.
  /// </summary>
  /// <remarks>
  /// Se não informado, será inferido do AssemblyVersion ou "unknown_version".
  /// </remarks>
  public string? ServiceVersion { get; set; }

  /// <summary>
  /// Identificador único da instância do serviço.
  /// </summary>
  /// <remarks>
  /// Se não informado, será inferido da variável OTEL_SERVICE_INSTANCE_ID ou um novo GUID.
  /// </remarks>
  public string? ServiceInstanceId { get; set; }

  /// <summary>
  /// Atributos adicionais a serem adicionados ao Resource do OpenTelemetry.
  /// </summary>
  /// <remarks>
  /// Útil para tags como region, cluster, tenant, etc.
  /// Exemplo:
  /// {
  ///   "provider.name": "aws",
  ///   "provider.region": "us-east-1",
  ///   "provider.cluster": "cluster-a",
  ///   "tenant.id": "tenant-123"
  /// }
  /// </remarks>
  public Dictionary<string, string> ResourceAttributes { get; set; } = [];

  /// <summary>
  /// Indica se dados sensíveis podem ser coletados e exibidos sem sanitização. Padrão: false (sanitiza).
  /// </summary>
  /// <remarks>
  /// Quando true, desliga globalmente o mascaramento e a remoção de dados sensíveis, ignorando as
  /// opções granulares de <see cref="TracingOptions.DataSensitive"/>.
  /// </remarks>
  public bool AllowSensitiveData { get; set; } = false;

  /// <summary>
  /// Indica se deve usar o Console exporter em ambiente de desenvolvimento. Padrão: true.
  /// </summary>
  /// <remarks>
  /// Quando true, em ambiente de desenvolvimento o Console exporter será adicionado.
  /// O 'Otlp.Enabled' deve estar false para que o Console exporter seja usado exclusivamente.
  /// </remarks>
  public bool UseConsoleExporterInDevelopment { get; set; } = true;

  #endregion

  #region Sub-Options

  /// <summary>
  /// Opções de configuração para exportador OTLP.
  /// </summary>
  public OtlpOptions Otlp { get; set; } = new();

  /// <summary>
  /// Opções de configuração para rastreamento (Tracing) OpenTelemetry.
  /// </summary>
  public TracingOptions Tracing { get; set; } = new();

  /// <summary>
  /// Opções de configuração para métricas (Metrics) OpenTelemetry.
  /// </summary>
  public MetricsOptions Metrics { get; set; } = new();

  /// <summary>
  /// Opções de configuração para logs (Logging) OpenTelemetry.
  /// </summary>
  public LoggingOptions Logging { get; set; } = new();

  #endregion

  #region Callbacks

  /// <summary>
  /// Callback para configuração adicional do TracerProviderBuilder.
  /// </summary>
  public Action<TracerProviderBuilder>? ConfigureTracing { get; set; }

  /// <summary>
  /// Callback para configuração adicional do MeterProviderBuilder.
  /// </summary>
  public Action<MeterProviderBuilder>? ConfigureMetrics { get; set; }

  /// <summary>
  /// Callback para configuração adicional do OpenTelemetryLoggerOptions.
  /// </summary>
  public Action<OpenTelemetryLoggerOptions>? ConfigureLogging { get; set; }

  #endregion
}
