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
  /// Opções de configuração para exportador OTLP (OpenTelemetry Protocol) usado para logs.
  /// </summary>
  public OtlpOptions? Otlp { get; set; }
}
