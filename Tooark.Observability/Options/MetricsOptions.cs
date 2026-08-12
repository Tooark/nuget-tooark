namespace Tooark.Observability.Options;

/// <summary>
/// Opções de configuração para métricas (Metrics) OpenTelemetry.
/// </summary>
public class MetricsOptions
{
  #region Constants

  /// <summary>
  /// Intervalo padrão (em ms) de exportação de métricas.
  /// </summary>
  internal const int DefaultExportIntervalMilliseconds = 60000;

  /// <summary>
  /// Intervalo (em ms) de exportação de métricas aplicado quando o OTLP efetivo tem ServerlessOptimized habilitado.
  /// </summary>
  internal const int ServerlessExportIntervalMilliseconds = 5000;

  #endregion

  /// <summary>
  /// Indica se a coleta de métricas está habilitada. Padrão: true.
  /// </summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Intervalo (em ms) entre exportações de métricas via OTLP. Padrão: null (usa 60000).
  /// </summary>
  /// <remarks>
  /// Métricas usam um reader periódico (não o processador Batch), então as opções de Batch do OTLP não se aplicam.
  /// Quando null: usa 60000ms, ou 5000ms se o OTLP efetivo tiver ServerlessOptimized habilitado
  /// (reduz perda de métricas em ambientes com scale-to-zero).
  /// Valores menores ou iguais a zero são tratados como null.
  /// </remarks>
  public int? ExportIntervalMilliseconds { get; set; }

  /// <summary>
  /// Indica se as métricas de runtime .NET estão habilitadas. Padrão: true.
  /// </summary>
  public bool RuntimeMetricsEnabled { get; set; } = true;

  /// <summary>
  /// Indica se as métricas de processo estão habilitadas. Padrão: true.
  /// </summary>
  public bool ProcessMetricsEnabled { get; set; } = true;

  /// <summary>
  /// Nome padrão do Meter para métricas.
  /// </summary>
  /// <remarks>
  /// Utilizado para registrar métricas customizadas.
  /// </remarks>
  public string MeterName { get; set; } = "Tooark";

  /// <summary>
  /// Nomes de Meter adicionais a serem registrados para métricas.
  /// </summary>
  /// <remarks>
  /// Permite adicionar meters customizados além do meter principal.
  /// Exemplo:
  /// - "MyCompany.MyProduct"
  /// - "MyCompany.MyProduct.MyComponent"
  /// </remarks>
  public string[] AdditionalMeters { get; set; } = [];  

  /// <summary>
  /// Overrides de configuração do exportador OTLP para métricas. Valores não informados herdam do OTLP global.
  /// </summary>
  public OtlpOverrideOptions? Otlp { get; set; }
}
