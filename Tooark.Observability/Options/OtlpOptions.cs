using Tooark.Observability.Enums;

namespace Tooark.Observability.Options;

/// <summary>
/// Opções de configuração para exportador OTLP (OpenTelemetry Protocol).
/// </summary>
public class OtlpOptions
{
  #region Constants

  /// <summary>
  /// Valor de <see cref="OtlpBatchOptions.MaxQueueSize"/> aplicado quando <see cref="ServerlessOptimized"/> está habilitado.
  /// </summary>
  internal const int ServerlessMaxQueueSize = 512;

  /// <summary>
  /// Valor de <see cref="OtlpBatchOptions.MaxExportBatchSize"/> aplicado quando <see cref="ServerlessOptimized"/> está habilitado.
  /// </summary>
  internal const int ServerlessMaxExportBatchSize = 128;

  /// <summary>
  /// Valor de <see cref="OtlpBatchOptions.ScheduledDelayMilliseconds"/> aplicado quando <see cref="ServerlessOptimized"/> está habilitado.
  /// </summary>
  internal const int ServerlessScheduledDelayMilliseconds = 1000;

  #endregion

  #region Properties

  /// <summary>
  /// Indica se o exportador OTLP está habilitado. Padrão: false.
  /// </summary>
  public bool Enabled { get; set; } = false;

  /// <summary>
  /// Protocolo de comunicação com o coletor OTLP. Padrão: "grpc".
  /// </summary>
  /// <remarks>
  /// Valores possíveis:
  /// - "grpc": usa gRPC para comunicação.
  /// - "http": usa HTTP com payload em protobuf.
  /// </remarks>
  public EProtocolOtlp Protocol { get; set; } = "grpc";

  /// <summary>
  /// Endpoint do coletor OTLP.
  /// </summary>
  /// <remarks>
  /// Exemplo: 
  /// - "http://localhost:4317" para gRPC
  /// - "http://localhost:4318" para HTTP/protobuf.
  /// </remarks>
  public string? Endpoint { get; set; }

  /// <summary>
  /// Tipo do processador de exportação (Simple ou Batch).
  /// </summary>
  /// <remarks>
  /// - Simple: exporta item a item (mais simples, porém pode impactar performance).
  /// - Batch: exporta em lote (recomendado em produção).
  /// </remarks>
  public EProcessorType ExportProcessorType { get; set; } = "batch";

  /// <summary>
  /// Opções do processador Batch (usadas apenas quando <see cref="ExportProcessorType"/> = Batch).
  /// </summary>
  public OtlpBatchOptions Batch { get; set; } = new();

  /// <summary>
  /// Otimiza configurações para ambientes serverless (AWS ECS, GCP Cloud Run, AWS Lambda, Azure Container Apps).
  /// </summary>
  /// <remarks>
  /// Quando habilitado, aplica automaticamente configurações de batch otimizadas para minimizar
  /// perda de dados durante shutdown rápido de containers:
  /// - ScheduledDelayMilliseconds: 1000ms (1 segundo)
  /// - MaxExportBatchSize: 128
  /// - MaxQueueSize: 512
  /// Essas configurações garantem flush mais frequente dos dados de telemetria,
  /// reduzindo o risco de perda em ambientes com scale-to-zero.
  /// </remarks>
  public bool ServerlessOptimized { get; set; } = false;

  /// <summary>
  /// Headers adicionais para autenticação ou metadados.
  /// </summary>
  /// <remarks>
  /// Formato: "key1=value1,key2=value2"
  /// Exemplo: "authorization=Bearer token123,tenant.id=tenant-123"
  /// </remarks>
  public string? Headers { get; set; }

  #endregion
}
