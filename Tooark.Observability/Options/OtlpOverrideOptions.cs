using OpenTelemetry;
using Tooark.Observability.Enums;

namespace Tooark.Observability.Options;

/// <summary>
/// Opções de override do exportador OTLP para um sinal específico (Tracing, Metrics ou Logging).
/// </summary>
/// <remarks>
/// Todas as propriedades são anuláveis: valores não informados (null) herdam a configuração
/// OTLP global (<see cref="ObservabilityOptions.Otlp"/>). Isso permite, por exemplo,
/// desabilitar o OTLP apenas para um sinal (<c>Enabled = false</c>) mesmo quando o global está habilitado.
/// </remarks>
public class OtlpOverrideOptions
{
  #region Properties

  /// <summary>
  /// Indica se o exportador OTLP está habilitado para o sinal. Null herda do global.
  /// </summary>
  public bool? Enabled { get; set; }

  /// <summary>
  /// Protocolo de comunicação com o coletor OTLP ("grpc" ou "http"). Null herda do global.
  /// </summary>
  public EProtocolOtlp? Protocol { get; set; }

  /// <summary>
  /// Endpoint do coletor OTLP. Null ou vazio herda do global.
  /// </summary>
  public string? Endpoint { get; set; }

  /// <summary>
  /// Tipo do processador de exportação ("simple" ou "batch"). Null herda do global.
  /// </summary>
  public EProcessorType? ExportProcessorType { get; set; }

  /// <summary>
  /// Opções de override do processador Batch (usadas apenas quando o tipo efetivo for <see cref="ExportProcessorType.Batch"/>).
  /// </summary>
  public OtlpBatchOverrideOptions Batch { get; set; } = new();

  /// <summary>
  /// Otimiza configurações para ambientes serverless. Null herda do global.
  /// </summary>
  public bool? ServerlessOptimized { get; set; }

  /// <summary>
  /// Headers adicionais para autenticação ou metadados. Null ou vazio herda do global.
  /// </summary>
  /// <remarks>
  /// Formato: "key1=value1,key2=value2"
  /// </remarks>
  public string? Headers { get; set; }

  #endregion
}
