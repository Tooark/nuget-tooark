using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Tooark.Observability;

/// <summary>
/// Fonte de atividades e medidor já registrados no OpenTelemetry pelo <c>AddTooarkOpenTelemetry</c>, prontos
/// para a aplicação criar os próprios spans e métricas sem repetir o nome configurado.
/// </summary>
/// <remarks>
/// <para>
/// Registrado como singleton no container, inclusive quando a observabilidade está desabilitada: um
/// <see cref="System.Diagnostics.ActivitySource"/> sem ouvinte não cria atividade e um
/// <see cref="System.Diagnostics.Metrics.Meter"/> sem leitor não grava medição, então a injeção nunca falha.
/// </para>
/// <para>
/// Os nomes seguem a resolução de <c>Tracing.ActivitySourceName</c> e <c>Metrics.MeterName</c>: o valor
/// informado, senão o <c>service.name</c> resolvido, senão <see cref="DefaultName"/>. O nome
/// <see cref="DefaultName"/> continua sempre registrado como fonte e medidor adicionais, então um
/// <c>new ActivitySource("Tooark")</c> criado pela aplicação segue sendo capturado.
/// </para>
/// </remarks>
public sealed class TooarkInstrumentation : IDisposable
{
  #region Constants

  /// <summary>
  /// Nome usado quando nenhum outro é informado ou inferido, e sempre registrado como fonte e medidor adicionais.
  /// </summary>
  public const string DefaultName = "Tooark";

  #endregion

  #region Constructors

  /// <summary>
  /// Cria a instrumentação com os nomes resolvidos.
  /// </summary>
  /// <param name="activitySourceName">Nome do <see cref="System.Diagnostics.ActivitySource"/>.</param>
  /// <param name="meterName">Nome do <see cref="System.Diagnostics.Metrics.Meter"/>.</param>
  /// <param name="serviceName">Nome do serviço resolvido (<c>service.name</c>).</param>
  /// <param name="serviceVersion">Versão do serviço resolvida (<c>service.version</c>), usada como versão da fonte e do medidor.</param>
  /// <exception cref="ArgumentException">Quando um dos nomes é nulo, vazio ou só espaços.</exception>
  public TooarkInstrumentation(string activitySourceName, string meterName, string serviceName, string serviceVersion)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(activitySourceName);
    ArgumentException.ThrowIfNullOrWhiteSpace(meterName);
    ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
    ArgumentException.ThrowIfNullOrWhiteSpace(serviceVersion);

    ServiceName = serviceName;
    ServiceVersion = serviceVersion;
    ActivitySource = new ActivitySource(activitySourceName, serviceVersion);
    Meter = new Meter(meterName, serviceVersion);
  }

  #endregion

  #region Properties

  /// <summary>
  /// Fonte de atividades registrada no tracing, para criar spans com <c>StartActivity</c>.
  /// </summary>
  public ActivitySource ActivitySource { get; }

  /// <summary>
  /// Medidor registrado nas métricas, para criar contadores, histogramas e demais instrumentos.
  /// </summary>
  public Meter Meter { get; }

  /// <summary>
  /// Nome do serviço resolvido, o mesmo gravado em <c>service.name</c> no Resource.
  /// </summary>
  public string ServiceName { get; }

  /// <summary>
  /// Versão do serviço resolvida, a mesma gravada em <c>service.version</c> no Resource.
  /// </summary>
  public string ServiceVersion { get; }

  #endregion

  #region Methods

  /// <summary>
  /// Libera a fonte de atividades e o medidor.
  /// </summary>
  public void Dispose()
  {
    ActivitySource.Dispose();
    Meter.Dispose();
  }

  #endregion
}
