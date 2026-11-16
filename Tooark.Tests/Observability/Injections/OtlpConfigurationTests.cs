using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Tooark.Exceptions;
using Tooark.Observability;
using Tooark.Observability.Enums;
using Tooark.Observability.Injections;
using Tooark.Observability.Options;

namespace Tooark.Tests.Observability.Injections;

/// <summary>
/// O protocolo e o processador do OTLP lidos da configuração, e o endereço para onde cada sinal é exportado.
/// </summary>
/// <remarks>
/// Até a 4.8.0 o <c>"Protocol"</c> e o <c>"ExportProcessorType"</c> do <c>appsettings.json</c> eram ignorados em
/// silêncio (os tipos não tinham conversão a partir de texto para o binder), e o HTTP não recebia o caminho do
/// sinal. Os testes de rede fixam o que sai de fato pela conexão: conferir só as opções lidas não pega um
/// <c>POST</c> feito na raiz do coletor.
/// </remarks>
public class OtlpConfigurationTests
{
  #region Helpers

  // Monta a configuração da seção Observability a partir dos pares informados
  private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
    new ConfigurationBuilder()
      .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => pair.Value))
      .Build();

  // Registra a observabilidade e devolve as opções como o container as entrega
  private static ObservabilityOptions Register(IConfiguration configuration)
  {
    var services = new ServiceCollection();
    services.AddTooarkOpenTelemetry(configuration);

    using var provider = services.BuildServiceProvider();

    return provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
  }

  /// <summary>
  /// Coletor de mentira: aceita conexões em uma porta local e guarda a primeira linha de cada requisição.
  /// </summary>
  private sealed class FakeCollector : IDisposable
  {
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly List<string> _requestLines = [];

    public FakeCollector()
    {
      _listener.Start();
      Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
      _ = Task.Run(AcceptAsync);
    }

    public int Port { get; }

    public string[] RequestLines
    {
      get
      {
        lock (_requestLines)
        {
          return [.. _requestLines];
        }
      }
    }

    // Espera até as linhas recebidas satisfazerem a condição, ou até o tempo limite
    public async Task<string[]> WaitForAsync(Func<string[], bool> condition, CancellationToken cancellationToken)
    {
      for (var attempt = 0; attempt < 100 && !condition(RequestLines); attempt++)
      {
        await Task.Delay(100, cancellationToken);
      }

      return RequestLines;
    }

    private async Task AcceptAsync()
    {
      while (!_stop.IsCancellationRequested)
      {
        try
        {
          using var client = await _listener.AcceptTcpClientAsync(_stop.Token);
          var stream = client.GetStream();
          var buffer = new byte[4096];
          var read = await stream.ReadAsync(buffer, _stop.Token);
          var line = Encoding.ASCII.GetString(buffer, 0, read).Split('\n')[0].Trim();

          // Conexão aberta e fechada sem enviar nada não é uma requisição
          if (line.Length == 0)
          {
            continue;
          }

          lock (_requestLines)
          {
            _requestLines.Add(line);
          }

          // Responde só ao HTTP/1.1; a conexão gRPC (HTTP/2) é apenas registrada e fechada
          if (line.EndsWith("HTTP/1.1", StringComparison.Ordinal))
          {
            await stream.WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(), _stop.Token);
          }
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or SocketException or ObjectDisposedException)
        {
          // Conexão encerrada pelo exportador ou coletor parado: segue para a próxima
        }
      }
    }

    public void Dispose()
    {
      _stop.Cancel();
      _listener.Stop();
      _stop.Dispose();
    }
  }

  // Registra a observabilidade apontada para o coletor de mentira, gera um trace, uma métrica e um log e
  // devolve a primeira linha das requisições que chegaram
  private static async Task<string[]> ExportAsync(string protocol, string path, Func<string[], bool> expected)
  {
    using var collector = new FakeCollector();

    var services = new ServiceCollection();
    services.AddTooarkOpenTelemetry(Configuration(
      ("Observability:Enabled", "true"),
      ("Observability:ServiceName", "tests"),
      ("Observability:Otlp:Enabled", "true"),
      ("Observability:Otlp:Endpoint", $"http://127.0.0.1:{collector.Port}{path}"),
      ("Observability:Otlp:Protocol", protocol),
      ("Observability:Otlp:ExportProcessorType", "simple")
    ));

    using var provider = services.BuildServiceProvider();

    var tracerProvider = provider.GetRequiredService<TracerProvider>();
    var meterProvider = provider.GetRequiredService<MeterProvider>();
    var instrumentation = provider.GetRequiredService<TooarkInstrumentation>();

    using (var activity = instrumentation.ActivitySource.StartActivity("otlp-wire"))
    {
      activity?.SetTag("test", "wire");
    }

    instrumentation.Meter.CreateCounter<long>("otlp.wire").Add(1);
    provider.GetRequiredService<ILoggerFactory>().CreateLogger("otlp-wire").LogWarning("otlp wire");

    tracerProvider.ForceFlush(5000);
    meterProvider.ForceFlush(5000);

    return await collector.WaitForAsync(expected, TestContext.Current.CancellationToken);
  }

  #endregion

  #region Binding - Protocol and ExportProcessorType

  // O protocolo e o processador do OTLP global vêm da configuração
  [Theory]
  [InlineData("http", "simple")]
  [InlineData("grpc", "batch")]
  [InlineData("HTTP", "Simple")]
  [InlineData(" http ", " simple ")]
  public void AddTooarkOpenTelemetry_ShouldReadProtocolAndProcessor_FromConfiguration(string protocol, string processor)
  {
    // Arrange
    var configuration = Configuration(
      ("Observability:Enabled", "false"),
      ("Observability:Otlp:Protocol", protocol),
      ("Observability:Otlp:ExportProcessorType", processor)
    );

    // Act
    var options = Register(configuration);

    // Assert
    Assert.Same((EProtocolOtlp)protocol.Trim(), options.Otlp.Protocol);
    Assert.Same((EProcessorType)processor.Trim(), options.Otlp.ExportProcessorType);
  }

  // Sem as chaves, valem os padrões: gRPC e lote
  [Fact]
  public void AddTooarkOpenTelemetry_ShouldKeepDefaults_WhenProtocolAndProcessorAreNotSet()
  {
    // Arrange
    var configuration = Configuration(("Observability:Enabled", "false"));

    // Act
    var options = Register(configuration);

    // Assert
    Assert.Same(EProtocolOtlp.Grpc, options.Otlp.Protocol);
    Assert.Same(EProcessorType.Batch, options.Otlp.ExportProcessorType);
    Assert.Null(options.Tracing.Otlp?.Protocol);
  }

  // O nome do protocolo como o OpenTelemetry o escreve também é aceito
  [Fact]
  public void AddTooarkOpenTelemetry_ShouldAcceptHttpProtobuf_AsHttp()
  {
    // Arrange
    var configuration = Configuration(
      ("Observability:Enabled", "false"),
      ("Observability:Otlp:Protocol", "http/protobuf")
    );

    // Act
    var options = Register(configuration);

    // Assert
    Assert.Same(EProtocolOtlp.Http, options.Otlp.Protocol);
  }

  // O protocolo e o processador de um sinal vêm da configuração e valem só para ele
  [Fact]
  public void AddTooarkOpenTelemetry_ShouldReadSignalOverrides_FromConfiguration()
  {
    // Arrange
    var configuration = Configuration(
      ("Observability:Enabled", "false"),
      ("Observability:Otlp:Endpoint", "http://localhost:4317"),
      ("Observability:Tracing:Otlp:Protocol", "http"),
      ("Observability:Tracing:Otlp:Endpoint", "http://localhost:4318"),
      ("Observability:Metrics:Otlp:ExportProcessorType", "simple"),
      ("Observability:Logging:Otlp:Protocol", "grpc")
    );

    // Act
    var options = Register(configuration);
    var tracing = TooarkDependencyInjection.ResolveOtlpOptions(options, options.Tracing.Otlp);
    var metrics = TooarkDependencyInjection.ResolveOtlpOptions(options, options.Metrics.Otlp);
    var logging = TooarkDependencyInjection.ResolveOtlpOptions(options, options.Logging.Otlp);

    // Assert
    Assert.Same(EProtocolOtlp.Grpc, options.Otlp.Protocol);
    Assert.Same(EProtocolOtlp.Http, tracing.Protocol);
    Assert.Same(EProcessorType.Batch, tracing.ExportProcessorType);
    Assert.Same(EProtocolOtlp.Grpc, metrics.Protocol);
    Assert.Same(EProcessorType.Simple, metrics.ExportProcessorType);
    Assert.Same(EProtocolOtlp.Grpc, logging.Protocol);
  }

  // Um valor que não existe falha no registro com a exceção do Tooark, com ou sem a configuração estrita
  [Theory]
  [InlineData("Observability:Otlp:Protocol", "htp", null)]
  [InlineData("Observability:Otlp:Protocol", "htp", "false")]
  [InlineData("Observability:Otlp:Protocol", "htp", "true")]
  [InlineData("Observability:Otlp:Protocol", "HttpProtobuf", null)]
  [InlineData("Observability:Otlp:ExportProcessorType", "lote", null)]
  [InlineData("Observability:Otlp:ExportProcessorType", "lote", "true")]
  [InlineData("Observability:Tracing:Otlp:Protocol", "htp", null)]
  [InlineData("Observability:Tracing:Otlp:Protocol", "htp", "true")]
  [InlineData("Observability:Metrics:Otlp:ExportProcessorType", "lote", "false")]
  [InlineData("Observability:Logging:Otlp:Protocol", "htp", "true")]
  public void AddTooarkOpenTelemetry_ShouldFailWithTooarkException_WhenValueIsInvalid(string key, string value, string? strict)
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = Configuration(
      ("Tooark:StrictConfiguration", strict),
      ("Observability:Enabled", "false"),
      (key, value)
    );

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => services.AddTooarkOpenTelemetry(configuration));

    // Assert
    Assert.Equal($"Invalid.Parameter;{value}", exception.Message);
  }

  // O conversor que o binder usa só converte a partir de texto
  [Theory]
  [InlineData(typeof(EProtocolOtlp))]
  [InlineData(typeof(EProcessorType))]
  public void TypeConverter_ShouldConvertOnlyFromText(Type type)
  {
    // Arrange
    var converter = TypeDescriptor.GetConverter(type);

    // Act & Assert
    Assert.True(converter.CanConvertFrom(typeof(string)));
    Assert.False(converter.CanConvertFrom(typeof(int)));
    Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(1));
  }

  #endregion

  #region ResolveOtlpEndpoint

  // No HTTP o caminho do sinal é acrescentado ao endereço; o que já termina no caminho de um sinal não muda
  [Theory]
  [InlineData("http://collector:4318", "http://collector:4318/v1/traces")]
  [InlineData("http://collector:4318/", "http://collector:4318/v1/traces")]
  [InlineData("https://gateway.example.com/otlp", "https://gateway.example.com/otlp/v1/traces")]
  [InlineData("https://gateway.example.com/otlp/", "https://gateway.example.com/otlp/v1/traces")]
  [InlineData("https://gateway.example.com/otlp?tenant=a", "https://gateway.example.com/otlp/v1/traces?tenant=a")]
  [InlineData("http://collector:4318/v1/traces", "http://collector:4318/v1/traces")]
  [InlineData("http://collector:4318/v1/traces/", "http://collector:4318/v1/traces/")]
  [InlineData("http://collector:4318/V1/Traces", "http://collector:4318/V1/Traces")]
  [InlineData("http://collector:4318/v1/metrics", "http://collector:4318/v1/metrics")]
  [InlineData("http://collector:4318/api/v2/otlp/v1/logs", "http://collector:4318/api/v2/otlp/v1/logs")]
  public void ResolveOtlpEndpoint_ShouldCompleteSignalPath_WhenProtocolIsHttp(string endpoint, string expected)
  {
    // Arrange
    var options = new OtlpOptions { Endpoint = endpoint, Protocol = "http" };

    // Act
    var resolved = TooarkDependencyInjection.ResolveOtlpEndpoint(options, TooarkDependencyInjection.OtlpTracesPath);

    // Assert
    Assert.Equal(expected, resolved.AbsoluteUri);
    Assert.Equal(endpoint, options.Endpoint);
  }

  // Cada sinal recebe o seu caminho a partir do mesmo endereço global
  [Theory]
  [InlineData(TooarkDependencyInjection.OtlpTracesPath, "http://collector:4318/v1/traces")]
  [InlineData(TooarkDependencyInjection.OtlpMetricsPath, "http://collector:4318/v1/metrics")]
  [InlineData(TooarkDependencyInjection.OtlpLogsPath, "http://collector:4318/v1/logs")]
  public void ResolveOtlpEndpoint_ShouldUseThePathOfEachSignal(string signalPath, string expected)
  {
    // Arrange
    var options = new OtlpOptions { Endpoint = "http://collector:4318", Protocol = "http" };

    // Act
    var resolved = TooarkDependencyInjection.ResolveOtlpEndpoint(options, signalPath);

    // Assert
    Assert.Equal(expected, resolved.AbsoluteUri);
  }

  // No gRPC o endereço é usado como foi informado
  [Theory]
  [InlineData("http://collector:4317")]
  [InlineData("http://collector:4317/otlp")]
  public void ResolveOtlpEndpoint_ShouldKeepTheEndpoint_WhenProtocolIsGrpc(string endpoint)
  {
    // Arrange
    var options = new OtlpOptions { Endpoint = endpoint, Protocol = "grpc" };

    // Act
    var resolved = TooarkDependencyInjection.ResolveOtlpEndpoint(options, TooarkDependencyInjection.OtlpTracesPath);

    // Assert
    Assert.Equal(new Uri(endpoint), resolved);
  }

  // O endereço inválido continua recusado com a chave de sempre
  [Theory]
  [InlineData(null)]
  [InlineData("/v1/traces")]
  public void ResolveOtlpEndpoint_ShouldFail_WhenEndpointIsInvalid(string? endpoint)
  {
    // Arrange
    var options = new OtlpOptions { Endpoint = endpoint, Protocol = "http" };

    // Act
    var exception = Assert.Throws<InternalServerErrorException>(
      () => TooarkDependencyInjection.ResolveOtlpEndpoint(options, TooarkDependencyInjection.OtlpTracesPath));

    // Assert
    Assert.Equal("Options.Otlp.Endpoint.Invalid", exception.Message);
  }

  // O exportador de cada sinal recebe o endereço completo, e as opções resolvidas não são alteradas
  [Fact]
  public void ConfigureOtlpExporter_ShouldSetTheSignalEndpoint_WithoutChangingTheOptions()
  {
    // Arrange
    var options = new OtlpOptions { Endpoint = "http://collector:4318", Protocol = "http" };
    var traces = new OtlpExporterOptions();
    var metrics = new OtlpExporterOptions();
    var logs = new OtlpExporterOptions();

    // Act
    TooarkDependencyInjection.ConfigureOtlpExporter(traces, options, TooarkDependencyInjection.OtlpTracesPath);
    TooarkDependencyInjection.ConfigureOtlpExporter(metrics, options, TooarkDependencyInjection.OtlpMetricsPath);
    TooarkDependencyInjection.ConfigureOtlpExporter(logs, options, TooarkDependencyInjection.OtlpLogsPath);

    // Assert
    Assert.Equal("http://collector:4318/v1/traces", traces.Endpoint.AbsoluteUri);
    Assert.Equal("http://collector:4318/v1/metrics", metrics.Endpoint.AbsoluteUri);
    Assert.Equal("http://collector:4318/v1/logs", logs.Endpoint.AbsoluteUri);
    Assert.Equal(OtlpExportProtocol.HttpProtobuf, traces.Protocol);
    Assert.Equal("http://collector:4318", options.Endpoint);
  }

  #endregion

  #region Wire

  // Com "Protocol": "http" na configuração, cada sinal é enviado por HTTP para o seu caminho
  [Theory]
  [InlineData("", "/v1/traces", "/v1/metrics", "/v1/logs")]
  [InlineData("/otlp", "/otlp/v1/traces", "/otlp/v1/metrics", "/otlp/v1/logs")]
  public async Task Export_ShouldPostEachSignalToItsPath_WhenProtocolIsHttp(string path, string traces, string metrics, string logs)
  {
    // Arrange
    string[] expected = [$"POST {traces} HTTP/1.1", $"POST {metrics} HTTP/1.1", $"POST {logs} HTTP/1.1"];

    // Act
    var lines = await ExportAsync("http", path, received => expected.All(received.Contains));

    // Assert
    Assert.All(expected, line => Assert.Contains(line, lines));
    Assert.All(lines, line => Assert.Contains(line, expected));
  }

  // Com "Protocol": "grpc", ou sem a chave, a conexão é HTTP/2 (gRPC), e não um POST em HTTP/1.1
  [Fact]
  public async Task Export_ShouldUseHttp2_WhenProtocolIsGrpc()
  {
    // Act
    var lines = await ExportAsync("grpc", "", received => received.Length > 0);

    // Assert
    Assert.NotEmpty(lines);
    Assert.All(lines, line => Assert.Equal("PRI * HTTP/2.0", line));
  }

  #endregion
}
