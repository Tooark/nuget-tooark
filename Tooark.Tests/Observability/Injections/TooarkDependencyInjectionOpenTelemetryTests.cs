using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Tooark.Exceptions;
using Tooark.Observability.Injections;
using Tooark.Observability.Options;

namespace Tooark.Tests.Observability.Injections;

public class TooarkDependencyInjectionOpenTelemetryTests
{
  #region Config & ServiceCollection Helpers

  /// <summary>
  /// Helper para construir IConfiguration a partir de um dicionário.
  /// </summary>
  /// <param name="values">Dicionário de pares chave-valor.</param>
  /// <returns>IConfiguration construído.</returns>
  private static IConfiguration BuildConfig(Dictionary<string, string?> values) => new ConfigurationBuilder()
    .AddInMemoryCollection(values)
    .Build();

  /// <summary>
  /// Helper para contar registros OpenTelemetry em IServiceCollection.
  /// </summary>
  /// <param name="services">Coleção de serviços onde contar os registros OpenTelemetry.</param>
  /// <returns>Número de registros OpenTelemetry encontrados.</returns>
  private static int CountOpenTelemetryRegistrations(IServiceCollection services) => services.Count(sd =>
    (sd.ServiceType?.FullName?.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase) ?? false) ||
    (sd.ImplementationType?.FullName?.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase) ?? false));

  #endregion

  #region AddTooarkOpenTelemetry - AddTooarkOpenTelemetry Tests

  // Teste para AddTooarkOpenTelemetry quando Observability está desabilitado.
  [Fact]
  public void AddTooarkOpenTelemetry_WhenDisabled_DoesNotRegisterOpenTelemetry()
  {
    // Arrange
    var services = new ServiceCollection();
    var config = BuildConfig(new Dictionary<string, string?>
    {
      [$"{ObservabilityOptions.Section}:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Tracing:Enabled"] = "true",
      [$"{ObservabilityOptions.Section}:Metrics:Enabled"] = "true",
      [$"{ObservabilityOptions.Section}:Logging:Enabled"] = "true"
    });

    // Act
    services.AddTooarkOpenTelemetry(config);
    using var provider = services.BuildServiceProvider();
    var opt = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

    // Assert
    Assert.False(opt.Enabled);
    Assert.Equal(0, CountOpenTelemetryRegistrations(services));
    Assert.DoesNotContain(services, s => s.ServiceType == typeof(TracerProvider));
    Assert.DoesNotContain(services, s => s.ServiceType == typeof(MeterProvider));
  }

  // Teste para AddTooarkOpenTelemetry quando Observability está habilitado.
  [Fact]
  public void AddTooarkOpenTelemetry_WhenEnabled_RegistersOpenTelemetry()
  {
    // Arrange
    var services = new ServiceCollection();
    var config = BuildConfig(new Dictionary<string, string?>
    {
      [$"{ObservabilityOptions.Section}:Enabled"] = "true",
      [$"{ObservabilityOptions.Section}:Tracing:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Metrics:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Logging:Enabled"] = "false"
    });

    // Act
    services.AddTooarkOpenTelemetry(config);

    // Assert
    Assert.True(CountOpenTelemetryRegistrations(services) > 0);
  }

  // Teste para AddTooarkOpenTelemetry quando apenas Tracing está habilitado.
  [Fact]
  public void AddTooarkOpenTelemetry_WhenTracingEnabled_RegistersTracerProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    var config = BuildConfig(new Dictionary<string, string?>
    {
      ["ASPNETCORE_ENVIRONMENT"] = "Development",
      [$"{ObservabilityOptions.Section}:Enabled"] = "true",
      [$"{ObservabilityOptions.Section}:Tracing:Enabled"] = "true",
      [$"{ObservabilityOptions.Section}:Metrics:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Logging:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Otlp:Enabled"] = "false"
    });

    // Act
    services.AddTooarkOpenTelemetry(config);

    // Assert
    Assert.Contains(services, s => s.ServiceType == typeof(TracerProvider));
    Assert.DoesNotContain(services, s => s.ServiceType == typeof(MeterProvider));
  }

  // Teste para AddTooarkOpenTelemetry quando apenas Metrics está habilitado.
  [Fact]
  public void AddTooarkOpenTelemetry_WhenMetricsEnabled_RegistersMeterProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    var config = BuildConfig(new Dictionary<string, string?>
    {
      ["ASPNETCORE_ENVIRONMENT"] = "Development",
      [$"{ObservabilityOptions.Section}:Enabled"] = "true",
      [$"{ObservabilityOptions.Section}:Tracing:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Metrics:Enabled"] = "true",
      [$"{ObservabilityOptions.Section}:Logging:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Otlp:Enabled"] = "false"
    });

    // Act
    services.AddTooarkOpenTelemetry(config);

    // Assert
    Assert.Contains(services, s => s.ServiceType == typeof(MeterProvider));
    Assert.DoesNotContain(services, s => s.ServiceType == typeof(TracerProvider));
  }

  // Teste para AddTooarkOpenTelemetry quando apenas Logging está habilitado.
  [Fact]
  public void AddTooarkOpenTelemetry_WhenLoggingEnabled_RegistersOpenTelemetryLoggerProvider()
  {
    // Arrange
    var services = new ServiceCollection();

    var config = BuildConfig(new Dictionary<string, string?>
    {
      ["ASPNETCORE_ENVIRONMENT"] = "Development",
      [$"{ObservabilityOptions.Section}:Enabled"] = "true",
      [$"{ObservabilityOptions.Section}:Tracing:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Metrics:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Logging:Enabled"] = "true",
      [$"{ObservabilityOptions.Section}:Otlp:Enabled"] = "false"
    });

    // Act
    services.AddTooarkOpenTelemetry(config);
    using var provider = services.BuildServiceProvider();
    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("test");
    logger.LogInformation("hello");
    var providers = provider.GetServices<ILoggerProvider>().ToList();

    // Assert
    Assert.Contains(providers, p => p.GetType().FullName?.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase) == true);
  }

  // Teste para AddTooarkOpenTelemetry quando o delegate de configuração sobrescreve a decisão de registro, mas não o IOptions.
  [Fact]
  public void AddTooarkOpenTelemetry_ConfigureDelegateOverridesRegistrationDecision_ButNotIOptions()
  {
    // Arrange
    var services = new ServiceCollection();

    // Config diz Enabled=false
    var config = BuildConfig(new Dictionary<string, string?>
    {
      [$"{ObservabilityOptions.Section}:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Tracing:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Metrics:Enabled"] = "false",
      [$"{ObservabilityOptions.Section}:Logging:Enabled"] = "false"
    });

    // Act
    services.AddTooarkOpenTelemetry(config, opt =>
    {
      opt.Enabled = true;
      opt.Tracing.Enabled = false;
      opt.Metrics.Enabled = false;
      opt.Logging.Enabled = false;
    });
    using var provider = services.BuildServiceProvider();
    var optionsFromContainer = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

    // Assert
    Assert.False(optionsFromContainer.Enabled);
    Assert.True(CountOpenTelemetryRegistrations(services) > 0);
  }

  #endregion

  #region AddTooarkOpenTelemetry - GetEnvironment Tests

  // Teste para GetEnvironment utilizando ASPNETCORE_ENVIRONMENT nas configurações.
  [Theory]
  [InlineData("Production")]
  [InlineData("Staging")]
  [InlineData("Development")]
  [InlineData("PRODUCTION")]
  [InlineData("PRD")]
  [InlineData("prd")]
  public void GetEnvironment_WhenAspNetCoreEnvironment_FromConfiguration(string envValue)
  {
    // Arrange
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = envValue
      })
      .Build();

    // Act
    var env = TooarkDependencyInjection.GetEnvironment(config);

    // Assert
    Assert.Equal(envValue, env);
  }

  // Teste para GetEnvironment utilizando DOTNET_ENVIRONMENT nas configurações.
  [Theory]
  [InlineData("Production")]
  [InlineData("Staging")]
  [InlineData("Development")]
  [InlineData("PRODUCTION")]
  [InlineData("PRD")]
  [InlineData("prd")]
  public void GetEnvironment_WhenDotNetEnvironment_FromConfiguration(string envValue)
  {
    // Arrange
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["DOTNET_ENVIRONMENT"] = envValue
      })
      .Build();

    // Act
    var env = TooarkDependencyInjection.GetEnvironment(config);

    // Assert
    Assert.Equal(envValue, env);
  }

  // Teste para GetEnvironment utilizando ASPNETCORE_ENVIRONMENT como variável de ambiente.
  [Theory]
  [InlineData("Production")]
  [InlineData("Staging")]
  [InlineData("Development")]
  [InlineData("PRODUCTION")]
  [InlineData("PRD")]
  [InlineData("prd")]
  public void GetEnvironment_WhenAspNetCoreEnvironment_FromEnvironmentVariable(string envValue)
  {
    // Arrange
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = envValue
      })
      .AddEnvironmentVariables()
      .Build();

    // Act
    var env = TooarkDependencyInjection.GetEnvironment(config);

    // Assert
    Assert.Equal(envValue, env);
  }

  // Teste para GetEnvironment utilizando DOTNET_ENVIRONMENT como variável de ambiente.
  [Theory]
  [InlineData("Production")]
  [InlineData("Staging")]
  [InlineData("Development")]
  [InlineData("PRODUCTION")]
  [InlineData("PRD")]
  [InlineData("prd")]
  public void GetEnvironment_WhenDotNetEnvironment_FromEnvironmentVariable(string envValue)
  {
    // Arrange
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["DOTNET_ENVIRONMENT"] = envValue
      })
      .AddEnvironmentVariables()
      .Build();

    // Act
    var env = TooarkDependencyInjection.GetEnvironment(config);

    // Assert
    Assert.Equal(envValue, env);
  }

  // Teste para GetEnvironment quando nenhuma variável de ambiente está definida.
  [Fact]
  public void GetEnvironment_WhenMissing_ReturnsUnknownEnvironment()
  {
    // Arrange
    var config = new ConfigurationBuilder().Build();

    // Act
    var env = TooarkDependencyInjection.GetEnvironment(config);

    // Assert
    Assert.Equal("unknown_environment", env);
  }

  #endregion

  #region AddTooarkOpenTelemetry - NormalizeAttributeKey Tests

  // Teste para NormalizeAttributeKey com chave nula ou em branco.
  [Theory]
  [InlineData(null, "")]
  [InlineData("", "")]
  [InlineData("   ", "")]
  public void NormalizeAttributeKey_WhenNullOrWhitespace_ReturnsEmpty(string? input, string expected)
  {
    // Arrange & Act
    var result = TooarkDependencyInjection.NormalizeAttributeKey(input!);

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para NormalizeAttributeKey convertendo maiúsculas para minúsculas e substituindo caracteres inválidos.
  [Theory]
  [InlineData("My Key", "my.key")]
  [InlineData("My Key!", "my.key_")]
  [InlineData("A@B#C", "a_b_c")]
  [InlineData("  Hello World  ", "hello.world")]
  [InlineData("a.b-c_d", "a.b-c_d")]
  public void NormalizeAttributeKey_NormalizesToLower_Dots_And_Underscores(string input, string expected)
  {
    // Arrange & Act
    var result = TooarkDependencyInjection.NormalizeAttributeKey(input);

    // Assert
    Assert.Equal(expected, result);
  }

  #endregion

  #region AddTooarkOpenTelemetry - IsDevelopmentEnvironment Tests

  // Teste para IsDevelopmentEnvironment com vários valores de ambiente.
  [Theory]
  [InlineData("Development", true)]
  [InlineData("Dev", true)]
  [InlineData("development", true)]
  [InlineData("DEV", true)]
  [InlineData("Production", false)]
  [InlineData("Staging", false)]
  public void IsDevelopmentEnvironment_Works(string envValue, bool expected)
  {
    // Arrange
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = envValue
      })
      .Build();

    // Act
    var isDev = TooarkDependencyInjection.IsDevelopmentEnvironment(config);

    // Assert
    Assert.Equal(expected, isDev);
  }

  #endregion

  #region AddTooarkOpenTelemetry - ConfigureResource Tests

  // Teste para ConfigureResource adicionando atributos principais e ambiente de implantação.
  [Fact]
  public void ConfigureResource_AddsCoreAttributes_AndDeploymentEnvironment()
  {
    // Arrange
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = "TestEnv"
      })
      .Build();
    var options = new ObservabilityOptions
    {
      ServiceName = "svc",
      ServiceVersion = "1.2.3",
      ServiceInstanceId = "instance-001"
    };

    // Act
    var rb = TooarkDependencyInjection.ConfigureResource(config, options);
    var resource = rb.Build();
    var attrs = resource.Attributes.ToDictionary(k => k.Key, v => v.Value);

    // Assert
    Assert.True(attrs.ContainsKey("service.name"));
    Assert.True(attrs.ContainsKey("service.version"));
    Assert.True(attrs.ContainsKey("service.instance.id"));
    Assert.True(attrs.ContainsKey("deployment.environment"));
    Assert.True(attrs.ContainsKey("host.name"));
    Assert.True(attrs.ContainsKey("process.pid"));
    Assert.True(attrs.ContainsKey("process.runtime.name"));
    Assert.True(attrs.ContainsKey("process.runtime.version"));
    Assert.True(attrs.ContainsKey("process.runtime.description"));
    Assert.Equal("svc", attrs["service.name"]);
    Assert.Equal("1.2.3", attrs["service.version"]);
    Assert.Equal("instance-001", attrs["service.instance.id"]);
    Assert.Equal("TestEnv", attrs["deployment.environment"]);
    Assert.Equal(Environment.MachineName, attrs["host.name"]);
    Assert.Equal(Environment.ProcessId.ToString(), attrs["process.pid"]);
    Assert.Equal(".NET", attrs["process.runtime.name"]);
    Assert.Equal(Environment.Version.ToString(), attrs["process.runtime.version"]);
    Assert.Equal(RuntimeInformation.FrameworkDescription, attrs["process.runtime.description"]);
  }

  // Teste para ConfigureResource adicionando atributos principais a partir de AssemblyInfo.
  [Fact]
  public void ConfigureResource_WithoutSetInformation()
  {
    // Arrange
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection()
      .Build();
    var options = new ObservabilityOptions();

    // Act
    var rb = TooarkDependencyInjection.ConfigureResource(config, options);
    var resource = rb.Build();
    var attrs = resource.Attributes.ToDictionary(k => k.Key, v => v.Value);
    var serviceInstanceId = attrs["service.instance.id"].ToString() ?? "";

    // Assert
    Assert.True(attrs.ContainsKey("service.name"));
    Assert.True(attrs.ContainsKey("service.version"));
    Assert.True(attrs.ContainsKey("service.instance.id"));
    Assert.Equal(Assembly.GetEntryAssembly()?.GetName().Name, attrs["service.name"]);
    Assert.Equal(Assembly.GetEntryAssembly()?.GetName().Version?.ToString(), attrs["service.version"]);
    Assert.Equal(32, serviceInstanceId.Length);
  }

  // Teste para ConfigureResource normalizando e adicionando atributos de recurso personalizados.
  [Fact]
  public void ConfigureResource_NormalizesAndAdds_CustomResourceAttributes()
  {
    // Arrange
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = "TestEnv"
      })
      .Build();
    var options = new ObservabilityOptions
    {
      ResourceAttributes = new Dictionary<string, string>
      {
        ["Tenant ID"] = "tenant-123",       // -> tenant.id
        ["provider@region"] = "us-east-1",  // -> provider_region
        [""] = "ignore-me",
        ["   "] = "ignore-me-too"
      }
    };

    // Act
    var rb = TooarkDependencyInjection.ConfigureResource(config, options);
    var resource = rb.Build();
    var attrs = resource.Attributes.ToDictionary(k => k.Key, v => v.Value);

    // Assert
    Assert.Equal("tenant-123", attrs["tenant.id"]);
    Assert.Equal("us-east-1", attrs["provider_region"]);
    Assert.False(attrs.ContainsKey(""));
  }

  #endregion

  #region AddTooarkOpenTelemetry - ConfigureOtlpExporter  Methods

  // Teste para ConfigureOtlpExporter com endpoint válido e valores padrão.
  [Fact]
  public void ConfigureOtlpExporter_WhenEndpointSet()
  {
    // Arrange
    var url = "http://collector.example.com:4317";
    var exporter = new OtlpExporterOptions();
    var opt = new OtlpOptions()
    {
      Endpoint = url
    };

    // Act
    TooarkDependencyInjection.ConfigureOtlpExporter(exporter, opt);

    // Assert
    Assert.Equal(OtlpExportProtocol.Grpc, exporter.Protocol);
    Assert.Equal(new Uri(url), exporter.Endpoint);
    Assert.Equal(ExportProcessorType.Batch, exporter.ExportProcessorType);
    Assert.NotNull(exporter.BatchExportProcessorOptions);
    Assert.Equal(2048, exporter.BatchExportProcessorOptions.MaxQueueSize);
    Assert.Equal(512, exporter.BatchExportProcessorOptions.MaxExportBatchSize);
    Assert.Equal(5000, exporter.BatchExportProcessorOptions.ScheduledDelayMilliseconds);
    Assert.Equal(30000, exporter.BatchExportProcessorOptions.ExporterTimeoutMilliseconds);
    Assert.Null(exporter.Headers);
  }

  // Teste para ConfigureOtlpExporter com protocolo HTTP e processador Simple.
  [Fact]
  public void ConfigureOtlpExporter_WhenProtocolAndProcessorTypeSet()
  {
    // Arrange
    var url = "http://collector.example.com:4318";
    var exporter = new OtlpExporterOptions();
    var opt = new OtlpOptions()
    {
      Endpoint = url,
      Protocol = "http",
      ExportProcessorType = "simple"
    };

    // Act
    TooarkDependencyInjection.ConfigureOtlpExporter(exporter, opt);

    // Assert
    Assert.Equal(OtlpExportProtocol.HttpProtobuf, exporter.Protocol);
    Assert.Equal(new Uri(url), exporter.Endpoint);
    Assert.Equal(ExportProcessorType.Simple, exporter.ExportProcessorType);
    Assert.Equal(2048, exporter.BatchExportProcessorOptions.MaxQueueSize); // Default do OpenTelemetry
    Assert.Equal(512, exporter.BatchExportProcessorOptions.MaxExportBatchSize); // Default do OpenTelemetry
    Assert.Equal(5000, exporter.BatchExportProcessorOptions.ScheduledDelayMilliseconds); // Default do OpenTelemetry
    Assert.Equal(30000, exporter.BatchExportProcessorOptions.ExporterTimeoutMilliseconds); // Default do OpenTelemetry
    Assert.Null(exporter.Headers);
  }

  // Teste para ConfigureOtlpExporter com valores personalizados de batch.
  [Fact]
  public void ConfigureOtlpExporter_WhenCustomBatchValues_DoesNotOverride()
  {
    // Arrange
    var maxQueueSize = 999;
    var maxExportBatchSize = 111;
    var scheduledDelayMilliseconds = 2222;
    var exporterTimeoutMilliseconds = 12345;
    var exporter = new OtlpExporterOptions();
    var opt = new OtlpOptions
    {
      Endpoint = "https://collector.example.com:4317",
      Batch = new OtlpBatchOptions
      {
        MaxQueueSize = maxQueueSize,
        MaxExportBatchSize = maxExportBatchSize,
        ScheduledDelayMilliseconds = scheduledDelayMilliseconds,
        ExporterTimeoutMilliseconds = exporterTimeoutMilliseconds
      }
    };

    // Act
    TooarkDependencyInjection.ConfigureOtlpExporter(exporter, opt);

    // Assert
    Assert.Equal(ExportProcessorType.Batch, exporter.ExportProcessorType);
    Assert.NotNull(exporter.BatchExportProcessorOptions);
    Assert.Equal(maxQueueSize, exporter.BatchExportProcessorOptions.MaxQueueSize);
    Assert.Equal(maxExportBatchSize, exporter.BatchExportProcessorOptions.MaxExportBatchSize);
    Assert.Equal(scheduledDelayMilliseconds, exporter.BatchExportProcessorOptions.ScheduledDelayMilliseconds);
    Assert.Equal(exporterTimeoutMilliseconds, exporter.BatchExportProcessorOptions.ExporterTimeoutMilliseconds);
  }

  // Teste para ConfigureOtlpExporter com configuração serverless otimizada.
  [Fact]
  public void ConfigureOtlpExporter_WhenServerlessAndBatchDefaults_AppliesOptimizedBatch()
  {
    // Arrange
    var exporter = new OtlpExporterOptions();
    var opt = new OtlpOptions
    {
      Endpoint = "https://collector.example.com:4317",
      ServerlessOptimized = true
    };

    // Act
    TooarkDependencyInjection.ConfigureOtlpExporter(exporter, opt);

    // Assert
    Assert.NotNull(exporter.BatchExportProcessorOptions);
    Assert.Equal(ExportProcessorType.Batch, exporter.ExportProcessorType);
    Assert.Equal(512, exporter.BatchExportProcessorOptions!.MaxQueueSize);
    Assert.Equal(128, exporter.BatchExportProcessorOptions.MaxExportBatchSize);
    Assert.Equal(1000, exporter.BatchExportProcessorOptions.ScheduledDelayMilliseconds);
    Assert.Equal(30000, exporter.BatchExportProcessorOptions.ExporterTimeoutMilliseconds);
    Assert.Null(exporter.Headers);
  }

  // Teste para ConfigureOtlpExporter com headers definidos.
  [Fact]
  public void ConfigureOtlpExporter_WhenSetHeaders()
  {
    // Arrange
    var exporter = new OtlpExporterOptions();
    var opt = new OtlpOptions
    {
      Endpoint = "https://collector.example.com:4317",
      Headers = "k1=v1, k2=v2"
    };

    // Act
    TooarkDependencyInjection.ConfigureOtlpExporter(exporter, opt);

    // Assert
    Assert.Equal(new Uri("https://collector.example.com:4317"), exporter.Endpoint);
    Assert.Equal("k1=v1,k2=v2", exporter.Headers);
  }

  // Teste para ConfigureOtlpExporter com endpoint inválido.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("not-a-uri")]
  [InlineData("http:/broken")]
  public void ConfigureOtlpExporter_WhenEndpointInvalid_Throws(string? endpoint)
  {
    // Arrange
    var exporter = new OtlpExporterOptions();
    var opt = new OtlpOptions { Endpoint = endpoint };

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => TooarkDependencyInjection.ConfigureOtlpExporter(exporter, opt));
    Assert.Contains("Options.Otlp.Endpoint.Invalid", ex.Message);
  }

  // Teste para resolver OTLP efetivo herdando do OTLP global.
  [Fact]
  public void ResolveOtlpOptions_WhenResourceOtlpIsNull_ReturnsCloneFromGlobalOptions()
  {
    // Arrange
    var options = new ObservabilityOptions
    {
      Otlp = new OtlpOptions
      {
        Enabled = true,
        Endpoint = "http://global-collector:4317",
        Protocol = "http",
        Headers = "x-api-key=123",
        ServerlessOptimized = true,
        Batch = new OtlpBatchOptions
        {
          MaxQueueSize = 333,
          MaxExportBatchSize = 111,
          ScheduledDelayMilliseconds = 2222,
          ExporterTimeoutMilliseconds = 4444
        }
      }
    };

    // Act
    var resolved = TooarkDependencyInjection.ResolveOtlpOptions(options, null);

    // Assert
    Assert.NotSame(options.Otlp, resolved);
    Assert.True(resolved.Enabled);
    Assert.Equal("http://global-collector:4317", resolved.Endpoint);
    Assert.Equal("http", resolved.Protocol);
    Assert.Equal("x-api-key=123", resolved.Headers);
    Assert.True(resolved.ServerlessOptimized);
    Assert.Equal(333, resolved.Batch.MaxQueueSize);
    Assert.Equal(111, resolved.Batch.MaxExportBatchSize);
    Assert.Equal(2222, resolved.Batch.ScheduledDelayMilliseconds);
    Assert.Equal(4444, resolved.Batch.ExporterTimeoutMilliseconds);
  }

  // Teste para resolver OTLP efetivo com override parcial do recurso.
  [Fact]
  public void ResolveOtlpOptions_WhenResourceOverridesSubset_MergesWithGlobalOptions()
  {
    // Arrange
    var options = new ObservabilityOptions
    {
      Otlp = new OtlpOptions
      {
        Enabled = true,
        Endpoint = "http://global-collector:4317",
        Headers = "authorization=Bearer abc",
        Batch = new OtlpBatchOptions
        {
          MaxQueueSize = 2048,
          MaxExportBatchSize = 512,
          ScheduledDelayMilliseconds = 5000,
          ExporterTimeoutMilliseconds = 30000
        }
      }
    };
    var resourceOtlp = new OtlpOptions
    {
      Endpoint = "http://metrics-collector:4318",
      Protocol = "http",
      Batch = new OtlpBatchOptions
      {
        ScheduledDelayMilliseconds = 1500
      }
    };

    // Act
    var resolved = TooarkDependencyInjection.ResolveOtlpOptions(options, resourceOtlp);

    // Assert
    Assert.True(resolved.Enabled);
    Assert.Equal("http://metrics-collector:4318", resolved.Endpoint);
    Assert.Equal("http", resolved.Protocol);
    Assert.Equal("authorization=Bearer abc", resolved.Headers);
    Assert.Equal(2048, resolved.Batch.MaxQueueSize);
    Assert.Equal(512, resolved.Batch.MaxExportBatchSize);
    Assert.Equal(1500, resolved.Batch.ScheduledDelayMilliseconds);
    Assert.Equal(30000, resolved.Batch.ExporterTimeoutMilliseconds);
  }

  // Teste para manter valores globais quando o recurso não informa overrides efetivos.
  [Fact]
  public void ResolveOtlpOptions_WhenResourceUsesDefaults_KeepsGlobalValues()
  {
    // Arrange
    var options = new ObservabilityOptions
    {
      Otlp = new OtlpOptions
      {
        Enabled = true,
        Endpoint = "http://global-collector:4317",
        Headers = "tenant-id=abc",
        ServerlessOptimized = true
      }
    };

    // Act
    var resolved = TooarkDependencyInjection.ResolveOtlpOptions(options, new OtlpOptions());

    // Assert
    Assert.True(resolved.Enabled);
    Assert.Equal("http://global-collector:4317", resolved.Endpoint);
    Assert.Equal("tenant-id=abc", resolved.Headers);
    Assert.True(resolved.ServerlessOptimized);
  }

  #endregion

  #region AddTooarkOpenTelemetry - ValidateAndNormalizeOtlpHeaders Tests

  // Teste para ValidateAndNormalizeOtlpHeaders com valor nulo ou em branco.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void ValidateAndNormalizeOtlpHeaders_WhenNullOrWhitespace_ReturnsNull(string? headers)
  {
    // Arrange & Act
    var result = TooarkDependencyInjection.ValidateAndNormalizeOtlpHeaders(headers);

    // Assert
    Assert.Null(result);
  }

  // Teste para ValidateAndNormalizeOtlpHeaders com pares chave=valor válidos.
  [Fact]
  public void ValidateAndNormalizeOtlpHeaders_TrimsAndNormalizes_CommaSeparatedPairs()
  {
    // Arrange
    var input = "k1=v1, k2=v2 ,k3 = v3";

    // Act
    var result = TooarkDependencyInjection.ValidateAndNormalizeOtlpHeaders(input);

    // Assert
    Assert.Equal("k1=v1,k2=v2,k3=v3", result);
  }

  // Teste para ValidateAndNormalizeOtlpHeaders com caracteres de nova linha.
  [Theory]
  [InlineData("k1\r\n=v1,k2=v2")]
  [InlineData("k1\n=v1,k2=v2")]
  [InlineData("k1\r=v1,k2=v2")]
  [InlineData("k1=v1\r\n,k2=v2")]
  [InlineData("k1=v1\n,k2=v2")]
  [InlineData("k1=v1\r,k2=v2")]
  public void ValidateAndNormalizeOtlpHeaders_WhenContainsCRLF_ThrowsSecurityRisk(string input)
  {
    // Arrange & Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => TooarkDependencyInjection.ValidateAndNormalizeOtlpHeaders(input));
    Assert.Contains("Options.Otlp.Headers.SecurityRisk", ex.Message);
  }

  // Teste para ValidateAndNormalizeOtlpHeaders com pares inválidos.
  [Theory]
  [InlineData("k1")]                // sem '='
  [InlineData("k1=")]               // sem valor
  [InlineData("=v1")]               // sem chave
  [InlineData("k1=v1,brokenpair")]  // item inválido no meio
  [InlineData("  =v1")]             // chave vazia com espaços
  public void ValidateAndNormalizeOtlpHeaders_WhenInvalidPair_Throws(string input)
  {
    // Arrange & Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => TooarkDependencyInjection.ValidateAndNormalizeOtlpHeaders(input));
    Assert.Contains("Options.Otlp.Headers.Invalid", ex.Message);
  }

  // Teste para ValidateAndNormalizeOtlpHeaders com chaves inválidas.
  [Theory]
  [InlineData("k 1=v1")]  // espaço na chave
  [InlineData("k:1=v1")]  // caractere não permitido
  [InlineData("k/1=v1")]  // caractere não permitido
  public void ValidateAndNormalizeOtlpHeaders_WhenInvalidKey_Throws(string input)
  {
    // Arrange & Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() => TooarkDependencyInjection.ValidateAndNormalizeOtlpHeaders(input));
    Assert.Contains("Options.Otlp.Headers.InvalidKey", ex.Message);
  }

  #endregion

  #region AddTooarkOpenTelemetry - Regex Patterns Tests

  // Teste para o padrão de regex usado na normalização de chaves de atributos.
  [Fact]
  public void AttributesRegex_Pattern_IsExpected()
  {
    // Arrange & Act
    Regex re = TooarkDependencyInjection.AttributesRegex();

    // Assert
    Assert.Equal(@"[^a-z0-9._\-]", re.ToString());
    Assert.Equal(RegexOptions.None, re.Options);
  }

  // Teste para AttributesRegex detectando caracteres inválidos.
  [Theory]
  [InlineData("abc", false)]
  [InlineData("abc123", false)]
  [InlineData("a.b_c-d", false)]
  [InlineData("ABC", true)]          // maiúsculas são inválidas aqui (esperado)
  [InlineData("a b", true)]          // espaço inválido
  [InlineData("a@b", true)]          // @ inválido
  [InlineData("a/b", true)]          // / inválido
  [InlineData("ç", true)]            // fora de a-z0-9._-
  public void AttributesRegex_DetectsInvalidCharacters(string input, bool shouldMatch)
  {
    // Arrange
    var re = TooarkDependencyInjection.AttributesRegex();

    // Act
    var match = re.IsMatch(input);

    // Assert
    Assert.Equal(shouldMatch, match);
  }

  // Teste para AttributesRegex substituindo caracteres inválidos por "_".
  [Theory]
  [InlineData("abcDEF", "abc___")]         // D,E,F são inválidos
  [InlineData("a b", "a_b")]               // espaço vira "_"
  [InlineData("a@b#c", "a_b_c")]           // @ e # viram "_"
  [InlineData("a.b-c_d", "a.b-c_d")]       // permitido
  public void AttributesRegex_Replace_InvalidChars_WithUnderscore(string input, string expected)
  {
    // Arrange
    var re = TooarkDependencyInjection.AttributesRegex();

    // Act
    var result = re.Replace(input, "_");

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para o padrão de regex usado na validação de chaves de header.
  [Fact]
  public void HeaderKeyRegex_Pattern_IsExpected()
  {
    // Arrange & Act
    var re = TooarkDependencyInjection.HeaderKeyRegex();

    // Assert
    Assert.Equal(@"^[a-zA-Z0-9._\-]+$", re.ToString());
    Assert.Equal(RegexOptions.None, re.Options);
  }

  // Teste para HeaderKeyRegex validando chaves de header.
  [Theory]
  [InlineData("authorization", true)]
  [InlineData("x-api-key", true)]
  [InlineData("tenant.id", true)]
  [InlineData("A_B-1.z", true)]
  [InlineData("k 1", false)]          // espaço não permitido
  [InlineData("k:1", false)]          // ':' não permitido
  [InlineData("k/1", false)]          // '/' não permitido
  [InlineData("", false)]             // exige + (1 ou mais)
  public void HeaderKeyRegex_ValidatesHeaderKey(string key, bool isValid)
  {
    // Arrange & Act
    var re = TooarkDependencyInjection.HeaderKeyRegex();

    // Assert
    Assert.Equal(isValid, re.IsMatch(key));
  }

  // Teste para o padrão de regex usado na substituição de sequências de espaços.
  [Fact]
  public void SpaceRegex_Pattern_IsExpected()
  {
    // Arrange & Act
    var re = TooarkDependencyInjection.SpaceRegex();

    // Assert
    Assert.Equal(@"\s+", re.ToString());
  }

  // Teste para SpaceRegex substituindo sequências de espaços por ".".
  [Theory]
  [InlineData("a b", "a.b")]
  [InlineData("a   b", "a.b")]
  [InlineData("a\tb", "a.b")]
  [InlineData("a\r\nb", "a.b")]
  [InlineData(" a \t b ", ".a.b.")] // aqui NÃO tem Trim; é replace direto no texto
  public void SpaceRegex_ReplacesAnyWhitespaceSequence(string input, string expected)
  {
    // Arrange
    var re = TooarkDependencyInjection.SpaceRegex();

    // Act
    var result = re.Replace(input, ".");

    // Assert
    Assert.Equal(expected, result);
  }

  #endregion
}
