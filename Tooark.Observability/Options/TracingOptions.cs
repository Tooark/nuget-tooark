namespace Tooark.Observability.Options;

/// <summary>
/// Opções de configuração para rastreamento (Tracing) OpenTelemetry.
/// </summary>
public class TracingOptions
{
  #region Private properties

  /// <summary>
  /// Taxa de amostragem interna para traces (0.0 a 1.0). Padrão: 1.0 (100% dos traces).
  /// </summary>
  private double _samplingRatio = 1.0;

  #endregion

  #region Properties

  /// <summary>
  /// Indica se o rastreamento está habilitado. Padrão: true.
  /// </summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Taxa de amostragem para traces (0.0 a 1.0). Padrão: 1.0 (100% dos traces).
  /// </summary>
  /// <remarks>
  /// Valores:
  /// - 1.0: captura 100% dos traces
  /// - 0.5: captura 50% dos traces
  /// - 0.0: não captura nenhum trace
  /// </remarks>
  public double SamplingRatio
  {
    get => _samplingRatio;
    set
    {
      // Garante que o valor seja no mínimo 0.0
      if (value < 0.0)
      {
        _samplingRatio = 0.0;
        return;
      }

      // Garante que o valor seja no máximo 1.0
      if (value > 1.0)
      {
        _samplingRatio = 1.0;
        return;
      }

      _samplingRatio = value;
    }
  }

  /// <summary>
  /// Prefixo para adicionar aos paths a serem ignorados no rastreamento de requisições HTTP.
  /// </summary>
  /// <remarks>
  /// Exemplo:
  /// - "/api" -> com IgnorePaths = ["/health"], irá ignorar "/api/health"
  /// - "/internal" -> com IgnorePaths = ["/metrics"], irá ignorar "/internal/metrics"
  /// </remarks>
  public string? IgnorePathPrefix { get; set; }
  
  /// <summary>
  /// Paths a serem ignorados no rastreamento de requisições HTTP.
  /// </summary>
  /// <remarks>
  /// Padrão: ["/health", "/healthz", "/ready", "/metrics", "/favicon.ico"]
  /// </remarks>
  public string[] IgnorePaths { get; set; } =
  [
    "/health",
    "/healthz",
    "/ready",
    "/traces",
    "/metrics",
    "/logs",
    "/favicon.ico"
  ];

  /// <summary>
  /// Nome padrão do ActivitySource para tracing.
  /// </summary>
  /// <remarks>
  /// Utilizado para criar uma 'assinatura/canal' de eventos personalizados de tracing.
  /// </remarks>
  public string ActivitySourceName { get; set; } = "Tooark";

  /// <summary>
  /// Nomes de ActivitySource adicionais a serem registrados para tracing.
  /// </summary>
  /// <remarks>
  /// Utilizado para adicionar outras fontes de tracing além da principal.
  /// Exemplo: 
  /// - "Tooark.MyComponent"
  /// - "orders-api"
  /// - "orders-api.billing"
  /// - "MyCompany.MyLib"
  /// </remarks>
  public string[] AdditionalSources { get; set; } = [];

  /// <summary>
  /// Configura coleta e exibição de dados sensíveis granulares para Tracing.
  /// </summary>
  public DataSensitiveOptions DataSensitive { get; set; } = new();  

  /// <summary>
  /// Opções de configuração para exportador OTLP (OpenTelemetry Protocol) usado para tracing.
  /// </summary>
  public OtlpOptions? Otlp { get; set; }

  #endregion
}
