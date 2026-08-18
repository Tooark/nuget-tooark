namespace Tooark.Observability.Options;

/// <summary>
/// Opções de override do processador de exportação em lote (Batch) para um sinal específico.
/// </summary>
/// <remarks>
/// Todas as propriedades são anuláveis: valores não informados (null) herdam a configuração
/// OTLP global (<see cref="ObservabilityOptions.Otlp"/>). Os valores informados são validados
/// pelas regras de <see cref="OtlpBatchOptions"/> ao serem aplicados.
/// </remarks>
public class OtlpBatchOverrideOptions
{
  #region Properties

  /// <summary>
  /// Tamanho máximo da fila interna de itens aguardando exportação. Null herda do global.
  /// </summary>
  public int? MaxQueueSize { get; set; }

  /// <summary>
  /// Intervalo (em ms) entre envios em lote. Null herda do global.
  /// </summary>
  public int? ScheduledDelayMilliseconds { get; set; }

  /// <summary>
  /// Timeout (em ms) máximo por tentativa de exportação. Null herda do global.
  /// </summary>
  public int? ExporterTimeoutMilliseconds { get; set; }

  /// <summary>
  /// Tamanho máximo (em itens) de cada lote enviado. Null herda do global.
  /// </summary>
  public int? MaxExportBatchSize { get; set; }

  #endregion
}
