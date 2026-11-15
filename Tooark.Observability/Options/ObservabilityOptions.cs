using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
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
  /// Nome do serviço para identificação nos traces/metrics/logs (<c>service.name</c>).
  /// </summary>
  /// <remarks>
  /// <para>
  /// Ordem de resolução: este valor, a variável de ambiente <c>OTEL_SERVICE_NAME</c>, o nome do assembly de
  /// entrada e, por fim, <c>unknown_service</c>. Valores vazios ou só com espaços passam para a próxima fonte.
  /// </para>
  /// <para>
  /// Depois do registro, o <c>IOptions&lt;ObservabilityOptions&gt;</c> do container carrega o valor resolvido,
  /// e não mais <c>null</c>.
  /// </para>
  /// </remarks>
  public string? ServiceName { get; set; }

  /// <summary>
  /// Versão do serviço para identificação nos traces/metrics/logs (<c>service.version</c>).
  /// </summary>
  /// <remarks>
  /// <para>
  /// Ordem de resolução: este valor, a chave <c>service.version</c> da variável de ambiente
  /// <c>OTEL_RESOURCE_ATTRIBUTES</c>, a <c>AssemblyInformationalVersion</c> do assembly de entrada sem o sufixo
  /// <c>+commit</c> (o <c>&lt;Version&gt;</c> do projeto, como <c>2.3.0</c> ou <c>2.3.0-beta.1</c>), a
  /// <c>AssemblyVersion</c> e, por fim, <c>unknown_version</c>. Valores vazios ou só com espaços passam para a
  /// próxima fonte.
  /// </para>
  /// <para>
  /// Depois do registro, o <c>IOptions&lt;ObservabilityOptions&gt;</c> do container carrega o valor resolvido,
  /// e não mais <c>null</c>.
  /// </para>
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
  /// <para>
  /// Útil para atributos como nuvem, região, cluster e tenant. Prefira as chaves das convenções semânticas do
  /// OpenTelemetry, que os backends reconhecem. Exemplo:
  /// {
  ///   "cloud.provider": "aws",
  ///   "cloud.region": "us-east-1",
  ///   "k8s.cluster.name": "cluster-a",
  ///   "tenant.id": "tenant-123"
  /// }
  /// </para>
  /// <para>
  /// A variável de ambiente <c>OTEL_RESOURCE_ATTRIBUTES</c> (<c>chave=valor,chave=valor</c>) também entra no
  /// Resource, sem código. Quando a mesma chave vem das duas fontes, este dicionário vence.
  /// </para>
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

  /// <summary>
  /// Callback para configuração adicional do <see cref="ResourceBuilder"/>, executado por último, depois dos
  /// atributos do Tooark e de <see cref="ResourceAttributes"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Uso típico: instalar os detectores de recurso dos pacotes contrib do OpenTelemetry
  /// (<c>OpenTelemetry.Resources.AWS</c>, <c>.Azure</c>, <c>.Gcp</c>, <c>.Container</c>, <c>.Host</c>,
  /// <c>.Process</c>), que consultam o ambiente na inicialização e preenchem atributos como
  /// <c>cloud.provider</c> e <c>container.id</c>.
  /// </para>
  /// <para>
  /// Opcional. O Tooark não depende desses pacotes e não garante que os atributos que eles adicionam sejam
  /// aplicados: isso depende da versão instalada e do ambiente em que a aplicação roda.
  /// </para>
  /// </remarks>
  public Action<ResourceBuilder>? ConfigureResource { get; set; }

  #endregion
}
