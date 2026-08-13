using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Tooark.Observability.Enums;
using Tooark.Observability.Options;

namespace Tooark.Tests.Observability.Options;

public class ObservabilityOptionsTests
{
  #region Section

  // Teste para verificar o valor da seção de configuração.
  [Fact]
  public void ObservabilityOptions_Section_ShouldBeObservability()
  {
    // Assert
    Assert.Equal("Observability", ObservabilityOptions.Section);
  }

  #endregion

  #region Default Values

  // Teste para verificar os valores padrão.
  [Fact]
  public void ObservabilityOptions_ShouldHaveCorrectDefaultValues()
  {
    // Arrange & Act
    ObservabilityOptions options = new();

    // Assert
    Assert.True(options.Enabled);
    Assert.Null(options.ServiceName);
    Assert.Null(options.ServiceVersion);
    Assert.Null(options.ServiceInstanceId);
    Assert.NotNull(options.ResourceAttributes);
    Assert.Empty(options.ResourceAttributes);
    Assert.False(options.AllowSensitiveData);
    Assert.True(options.UseConsoleExporterInDevelopment);
  }

  // Teste para verificar os sub-options padrão.
  [Fact]
  public void ObservabilityOptions_ShouldHaveCorrectDefaultSubOptions()
  {
    // Arrange & Act
    ObservabilityOptions options = new();

    // Assert
    Assert.NotNull(options.Otlp);
    Assert.NotNull(options.Tracing);
    Assert.NotNull(options.Metrics);
    Assert.NotNull(options.Logging);
  }

  // Teste para verificar callbacks nulos por padrão.
  [Fact]
  public void ObservabilityOptions_ShouldHaveNullCallbacksByDefault()
  {
    // Arrange & Act
    ObservabilityOptions options = new();

    // Assert
    Assert.Null(options.ConfigureTracing);
    Assert.Null(options.ConfigureMetrics);
    Assert.Null(options.ConfigureLogging);
  }

  #endregion

  #region Property Setters

  // Teste para definir Enabled.
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void ObservabilityOptions_ShouldSetEnabled(bool value)
  {
    // Arrange
    ObservabilityOptions options = new();

    // Act
    options.Enabled = value;

    // Assert
    Assert.Equal(value, options.Enabled);
  }

  // Teste para definir ServiceName.
  [Theory]
  [InlineData("MyService")]
  [InlineData("my-api")]
  [InlineData("Company.Product.Service")]
  [InlineData(null)]
  public void ObservabilityOptions_ShouldSetServiceName(string? serviceName)
  {
    // Arrange
    ObservabilityOptions options = new();

    // Act
    options.ServiceName = serviceName;

    // Assert
    Assert.Equal(serviceName, options.ServiceName);
  }

  // Teste para definir ServiceVersion.
  [Theory]
  [InlineData("1.0.0")]
  [InlineData("2.1.3-beta")]
  [InlineData("v1.0.0")]
  [InlineData(null)]
  public void ObservabilityOptions_ShouldSetServiceVersion(string? serviceVersion)
  {
    // Arrange
    ObservabilityOptions options = new();

    // Act
    options.ServiceVersion = serviceVersion;

    // Assert
    Assert.Equal(serviceVersion, options.ServiceVersion);
  }

  // Teste para definir ServiceInstanceId.
  [Theory]
  [InlineData("instance-001")]
  [InlineData("abc123def456")]
  [InlineData(null)]
  public void ObservabilityOptions_ShouldSetServiceInstanceId(string? instanceId)
  {
    // Arrange
    ObservabilityOptions options = new();

    // Act
    options.ServiceInstanceId = instanceId;

    // Assert
    Assert.Equal(instanceId, options.ServiceInstanceId);
  }

  // Teste para definir ResourceAttributes.
  [Fact]
  public void ObservabilityOptions_ShouldSetResourceAttributes()
  {
    // Arrange
    ObservabilityOptions options = new();
    var attributes = new Dictionary<string, string>
    {
      ["provider.name"] = "aws",
      ["provider.region"] = "us-east-1",
      ["tenant.id"] = "tenant-123"
    };

    // Act
    options.ResourceAttributes = attributes;

    // Assert
    Assert.Equal(3, options.ResourceAttributes.Count);
    Assert.Equal("aws", options.ResourceAttributes["provider.name"]);
    Assert.Equal("us-east-1", options.ResourceAttributes["provider.region"]);
    Assert.Equal("tenant-123", options.ResourceAttributes["tenant.id"]);
  }

  // Teste para definir AllowSensitiveData.
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void ObservabilityOptions_ShouldSetAllowSensitiveData(bool value)
  {
    // Arrange
    ObservabilityOptions options = new();

    // Act
    options.AllowSensitiveData = value;

    // Assert
    Assert.Equal(value, options.AllowSensitiveData);
  }

  // Teste para definir UseConsoleExporterInDevelopment.
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void ObservabilityOptions_ShouldSetUseConsoleExporterInDevelopment(bool value)
  {
    // Arrange
    ObservabilityOptions options = new();

    // Act
    options.UseConsoleExporterInDevelopment = value;

    // Assert
    Assert.Equal(value, options.UseConsoleExporterInDevelopment);
  }

  #endregion

  #region Sub-Options Setters

  // Teste para definir OtlpOptions.
  [Fact]
  public void ObservabilityOptions_ShouldSetOtlpOptions()
  {
    // Arrange
    ObservabilityOptions options = new();
    var otlpOptions = new OtlpOptions
    {
      Enabled = true,
      Protocol = EProtocolOtlp.Grpc,
      Endpoint = "http://localhost:4317"
    };

    // Act
    options.Otlp = otlpOptions;

    // Assert
    Assert.True(options.Otlp.Enabled);
    Assert.Equal("http://localhost:4317", options.Otlp.Endpoint);
  }

  // Teste para definir TracingOptions.
  [Fact]
  public void ObservabilityOptions_ShouldSetTracingOptions()
  {
    // Arrange
    ObservabilityOptions options = new();
    var tracingOptions = new TracingOptions
    {
      Enabled = true,
      SamplingRatio = 0.5,
      ActivitySourceName = "MySource"
    };

    // Act
    options.Tracing = tracingOptions;

    // Assert
    Assert.True(options.Tracing.Enabled);
    Assert.Equal(0.5, options.Tracing.SamplingRatio);
    Assert.Equal("MySource", options.Tracing.ActivitySourceName);
  }

  // Teste para definir MetricsOptions.
  [Fact]
  public void ObservabilityOptions_ShouldSetMetricsOptions()
  {
    // Arrange
    ObservabilityOptions options = new();
    var metricsOptions = new MetricsOptions
    {
      Enabled = true,
      RuntimeMetricsEnabled = false,
      MeterName = "MyMeter"
    };

    // Act
    options.Metrics = metricsOptions;

    // Assert
    Assert.True(options.Metrics.Enabled);
    Assert.False(options.Metrics.RuntimeMetricsEnabled);
    Assert.Equal("MyMeter", options.Metrics.MeterName);
  }

  // Teste para definir LoggingOptions.
  [Fact]
  public void ObservabilityOptions_ShouldSetLoggingOptions()
  {
    // Arrange
    ObservabilityOptions options = new();
    var loggingOptions = new LoggingOptions
    {
      Enabled = true,
      IncludeFormattedMessage = false,
      IncludeScopes = false
    };

    // Act
    options.Logging = loggingOptions;

    // Assert
    Assert.True(options.Logging.Enabled);
    Assert.False(options.Logging.IncludeFormattedMessage);
    Assert.False(options.Logging.IncludeScopes);
  }

  #endregion

  #region Callbacks

  // Teste para definir ConfigureTracing callback.
  [Fact]
  public void ObservabilityOptions_ShouldSetConfigureTracingCallback()
  {
    // Arrange
    ObservabilityOptions options = new();
    var wasInvoked = false;
    void callback(TracerProviderBuilder _) => wasInvoked = true;

    // Act
    options.ConfigureTracing = callback;

    // Assert
    Assert.NotNull(options.ConfigureTracing);
    options.ConfigureTracing.Invoke(null!);
    Assert.True(wasInvoked);
  }

  // Teste para definir ConfigureMetrics callback.
  [Fact]
  public void ObservabilityOptions_ShouldSetConfigureMetricsCallback()
  {
    // Arrange
    ObservabilityOptions options = new();
    var wasInvoked = false;
    void callback(MeterProviderBuilder _) => wasInvoked = true;

    // Act
    options.ConfigureMetrics = callback;

    // Assert
    Assert.NotNull(options.ConfigureMetrics);
    options.ConfigureMetrics.Invoke(null!);
    Assert.True(wasInvoked);
  }

  // Teste para definir ConfigureLogging callback.
  [Fact]
  public void ObservabilityOptions_ShouldSetConfigureLoggingCallback()
  {
    // Arrange
    ObservabilityOptions options = new();
    var wasInvoked = false;
    void callback(OpenTelemetryLoggerOptions _) => wasInvoked = true;

    // Act
    options.ConfigureLogging = callback;

    // Assert
    Assert.NotNull(options.ConfigureLogging);
    options.ConfigureLogging.Invoke(null!);
    Assert.True(wasInvoked);
  }

  #endregion

  #region Combined Configurations

  // Teste para configuração completa.
  [Fact]
  public void ObservabilityOptions_ShouldAllowFullConfiguration()
  {
    // Arrange & Act
    var options = new ObservabilityOptions
    {
      Enabled = true,
      ServiceName = "TestService",
      ServiceVersion = "1.0.0",
      ServiceInstanceId = "instance-001",
      ResourceAttributes = new Dictionary<string, string>
      {
        ["cloud.provider"] = "aws",
        ["cloud.region"] = "us-east-1"
      },
      AllowSensitiveData = false,
      UseConsoleExporterInDevelopment = true,
      Otlp = new OtlpOptions
      {
        Enabled = true,
        Endpoint = "http://localhost:4317"
      },
      Tracing = new TracingOptions
      {
        Enabled = true,
        SamplingRatio = 0.5
      },
      Metrics = new MetricsOptions
      {
        Enabled = true
      },
      Logging = new LoggingOptions
      {
        Enabled = true
      }
    };

    // Assert
    Assert.True(options.Enabled);
    Assert.Equal("TestService", options.ServiceName);
    Assert.Equal("1.0.0", options.ServiceVersion);
    Assert.Equal("instance-001", options.ServiceInstanceId);
    Assert.Equal(2, options.ResourceAttributes.Count);
    Assert.False(options.AllowSensitiveData);
    Assert.True(options.UseConsoleExporterInDevelopment);
    Assert.True(options.Otlp.Enabled);
    Assert.True(options.Tracing.Enabled);
    Assert.True(options.Metrics.Enabled);
    Assert.True(options.Logging.Enabled);
  }

  // Teste para configuração desabilitada.
  [Fact]
  public void ObservabilityOptions_ShouldAllowDisabling()
  {
    // Arrange & Act
    var options = new ObservabilityOptions
    {
      Enabled = false
    };

    // Assert
    Assert.False(options.Enabled);
  }

  // Teste para configuração mínima.
  [Fact]
  public void ObservabilityOptions_ShouldAllowMinimalConfiguration()
  {
    // Arrange & Act
    var options = new ObservabilityOptions
    {
      Enabled = true,
      ServiceName = "MinimalService"
    };

    // Assert
    Assert.True(options.Enabled);
    Assert.Equal("MinimalService", options.ServiceName);
    // Verifica que os sub-options têm valores padrão válidos
    Assert.NotNull(options.Otlp);
    Assert.NotNull(options.Tracing);
    Assert.NotNull(options.Metrics);
    Assert.NotNull(options.Logging);
  }

  #endregion
}
