namespace Tooark.Observability.Options;

/// <summary>
/// Opções do processador de exportação em lote (Batch) para OTLP.
/// </summary>
public class OtlpBatchOptions
{
  #region Constants

  /// <summary>
  /// Valor padrão de <see cref="MaxQueueSize"/>.
  /// </summary>
  internal const int DefaultMaxQueueSize = 2048;

  /// <summary>
  /// Valor padrão de <see cref="ScheduledDelayMilliseconds"/>.
  /// </summary>
  internal const int DefaultScheduledDelayMilliseconds = 5000;

  /// <summary>
  /// Valor padrão de <see cref="ExporterTimeoutMilliseconds"/>.
  /// </summary>
  internal const int DefaultExporterTimeoutMilliseconds = 30000;

  /// <summary>
  /// Valor padrão de <see cref="MaxExportBatchSize"/>.
  /// </summary>
  internal const int DefaultMaxExportBatchSize = 512;

  #endregion

  #region Private properties

  /// <summary>
  /// Valor customizado de <see cref="MaxQueueSize"/>. Null quando não informado (usa o padrão).
  /// </summary>
  private int? _maxQueueSize;

  /// <summary>
  /// Valor customizado de <see cref="ScheduledDelayMilliseconds"/>. Null quando não informado (usa o padrão).
  /// </summary>
  private int? _scheduledDelayMilliseconds;

  /// <summary>
  /// Valor customizado de <see cref="ExporterTimeoutMilliseconds"/>. Null quando não informado (usa o padrão).
  /// </summary>
  private int? _exporterTimeoutMilliseconds;

  /// <summary>
  /// Valor customizado de <see cref="MaxExportBatchSize"/>. Null quando não informado (usa o padrão).
  /// </summary>
  private int? _maxExportBatchSize;

  #endregion

  #region Internal properties

  /// <summary>
  /// Indica se <see cref="MaxQueueSize"/> foi customizado com um valor válido.
  /// </summary>
  internal bool HasCustomMaxQueueSize => _maxQueueSize.HasValue;

  /// <summary>
  /// Indica se <see cref="ScheduledDelayMilliseconds"/> foi customizado com um valor válido.
  /// </summary>
  internal bool HasCustomScheduledDelay => _scheduledDelayMilliseconds.HasValue;

  /// <summary>
  /// Indica se <see cref="MaxExportBatchSize"/> foi customizado com um valor válido.
  /// </summary>
  internal bool HasCustomMaxExportBatchSize => _maxExportBatchSize.HasValue;

  #endregion

  #region Properties

  /// <summary>
  /// Tamanho máximo da fila interna de itens aguardando exportação. Padrão: 2048.
  /// </summary>
  /// <remarks>
  /// Se o valor for menor ou igual a zero, será usado o valor padrão.
  /// </remarks>
  public int MaxQueueSize
  {
    get => _maxQueueSize ?? DefaultMaxQueueSize;
    set => _maxQueueSize = (value > 0) ? value : null;
  }

  /// <summary>
  /// Intervalo (em ms) entre envios em lote. Padrão: 5000.
  /// </summary>
  /// <remarks>
  /// Se o valor for menor que zero, será usado o valor padrão.
  /// </remarks>
  public int ScheduledDelayMilliseconds
  {
    get => _scheduledDelayMilliseconds ?? DefaultScheduledDelayMilliseconds;
    set => _scheduledDelayMilliseconds = (value >= 0) ? value : null;
  }

  /// <summary>
  /// Timeout (em ms) máximo por tentativa de exportação. Padrão: 30000.
  /// </summary>
  /// <remarks>
  /// Se o valor for menor ou igual a zero, será usado o valor padrão.
  /// </remarks>
  public int ExporterTimeoutMilliseconds
  {
    get => _exporterTimeoutMilliseconds ?? DefaultExporterTimeoutMilliseconds;
    set => _exporterTimeoutMilliseconds = (value > 0) ? value : null;
  }

  /// <summary>
  /// Tamanho máximo (em itens) de cada lote enviado. Padrão: 512.
  /// </summary>
  /// <remarks>
  /// Se o valor for maior que <see cref="MaxQueueSize"/>, será usado o valor de <see cref="MaxQueueSize"/>.
  /// Se o valor for menor ou igual a zero, será usado o valor padrão.
  /// O limite é aplicado na leitura, então independe da ordem em que as propriedades são definidas.
  /// </remarks>
  public int MaxExportBatchSize
  {
    get => Math.Min(_maxExportBatchSize ?? DefaultMaxExportBatchSize, MaxQueueSize);
    set => _maxExportBatchSize = (value > 0) ? value : null;
  }

  #endregion

  #region Internal Methods

  /// <summary>
  /// Cria uma cópia preservando a distinção entre valores customizados e padrões.
  /// </summary>
  /// <returns>Cópia das opções.</returns>
  internal OtlpBatchOptions Clone()
  {
    var clone = new OtlpBatchOptions
    {
      _maxQueueSize = _maxQueueSize,
      _scheduledDelayMilliseconds = _scheduledDelayMilliseconds,
      _exporterTimeoutMilliseconds = _exporterTimeoutMilliseconds,
      _maxExportBatchSize = _maxExportBatchSize
    };

    return clone;
  }

  #endregion
}
