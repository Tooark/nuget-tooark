namespace Tooark.Observability.Options;

/// <summary>
/// Opções de configuração para logs (Logging) OpenTelemetry.
/// </summary>
public class LoggingOptions
{
  /// <summary>
  /// Indica se o logging OpenTelemetry está habilitado. Padrão: true.
  /// </summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Indica se deve incluir a mensagem formatada nos logs. Padrão: true.
  /// </summary>
  public bool IncludeFormattedMessage { get; set; } = true;

  /// <summary>
  /// Indica se deve incluir os scopes nos logs. Padrão: true.
  /// </summary>
  public bool IncludeScopes { get; set; } = true;

  /// <summary>
  /// Indica se deve fazer parse dos valores de estado nos logs. Padrão: true.
  /// </summary>
  public bool ParseStateValues { get; set; } = true;

  /// <summary>
  /// Overrides de configuração do exportador OTLP para logs. Valores não informados herdam do OTLP global.
  /// </summary>
  public OtlpOverrideOptions? Otlp { get; set; }
}
