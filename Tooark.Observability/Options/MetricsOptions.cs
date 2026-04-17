namespace Tooark.Observability.Options;

/// <summary>
/// Opções de configuração para métricas (Metrics) OpenTelemetry.
/// </summary>
public class MetricsOptions
{
  /// <summary>
  /// Indica se a coleta de métricas está habilitada. Padrão: true.
  /// </summary>
  public bool Enabled { get; set; } = true;

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
  /// Opções de configuração para exportador OTLP (OpenTelemetry Protocol) usado para métricas.
  /// </summary>
  public OtlpOptions? Otlp { get; set; }
}
