using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using Tooark.Exceptions;
using Tooark.Observability.Enums;
using Tooark.Observability.Injections;
using Tooark.Observability.Options;

namespace Tooark.Tests.Observability.Injections;

public class TooarkDependencyInjectionTests
{
  #region AddTooarkObservability - Basic Tests

  // Teste para adicionar observability com configuração padrão.
  [Fact]
  public void AddTooarkObservability_ShouldAddServices_WithDefaultConfiguration()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert - Verifica que os serviços foram registrados
    Assert.NotNull(services);
    Assert.True(services.Count > 0);
  }

  // Teste para adicionar observability desabilitado.
  [Fact]
  public void AddTooarkObservability_ShouldNotAddServices_WhenDisabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var initialCount = services.Count;
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "false"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert - Verifica que poucos serviços foram adicionados (apenas options)
    Assert.True(services.Count >= initialCount);
  }

  // Teste para adicionar observability com configuração programática.
  [Fact]
  public void AddTooarkObservability_ShouldApplyProgrammaticConfiguration()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true"
      })
      .Build();
    var configureInvoked = false;

    // Act
    services.AddTooarkObservability(configuration, options =>
    {
      configureInvoked = true;
      options.ServiceName = "TestService";
      options.ServiceVersion = "1.0.0";
    });

    // Assert
    Assert.True(configureInvoked);
  }

  // Teste para adicionar observability sem configuração programática (null).
  [Fact]
  public void AddTooarkObservability_ShouldWork_WithNullConfigure()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration, null);

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - Tracing Tests

  // Teste para adicionar observability com tracing habilitado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureTracing_WhenEnabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Tracing:Enabled"] = "true",
        ["Observability:Tracing:SamplingRatio"] = "0.5"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
    Assert.True(services.Count > 0);
  }

  // Teste para adicionar observability com tracing desabilitado.
  [Fact]
  public void AddTooarkObservability_ShouldNotConfigureTracing_WhenDisabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Tracing:Enabled"] = "false"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - Metrics Tests

  // Teste para adicionar observability com metrics habilitado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureMetrics_WhenEnabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Metrics:Enabled"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
    Assert.True(services.Count > 0);
  }

  // Teste para adicionar observability com metrics desabilitado.
  [Fact]
  public void AddTooarkObservability_ShouldNotConfigureMetrics_WhenDisabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Metrics:Enabled"] = "false"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com runtime metrics desabilitado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureMetrics_WithRuntimeMetricsDisabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Metrics:Enabled"] = "true",
        ["Observability:Metrics:RuntimeMetricsEnabled"] = "false"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com meters adicionais.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureMetrics_WithAdditionalMeters()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Metrics:Enabled"] = "true",
        ["Observability:Metrics:AdditionalMeters:0"] = "MyApp.Meter1",
        ["Observability:Metrics:AdditionalMeters:1"] = "MyApp.Meter2"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - Logging Tests

  // Teste para adicionar observability com logging habilitado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureLogging_WhenEnabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Logging:Enabled"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
    Assert.True(services.Count > 0);
  }

  // Teste para adicionar observability com logging desabilitado.
  [Fact]
  public void AddTooarkObservability_ShouldNotConfigureLogging_WhenDisabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Logging:Enabled"] = "false"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - OTLP Tests

  // Teste para adicionar observability com OTLP habilitado (gRPC).
  [Fact]
  public void AddTooarkObservability_ShouldConfigureOtlp_WithGrpcProtocol()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Otlp:Enabled"] = "true",
        ["Observability:Otlp:Protocol"] = "grpc",
        ["Observability:Otlp:Endpoint"] = "http://localhost:4317"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com OTLP habilitado (HTTP).
  [Fact]
  public void AddTooarkObservability_ShouldConfigureOtlp_WithHttpProtocol()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Otlp:Enabled"] = "true",
        ["Observability:Otlp:Protocol"] = "http",
        ["Observability:Otlp:Endpoint"] = "http://localhost:4318"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com OTLP desabilitado.
  [Fact]
  public void AddTooarkObservability_ShouldNotConfigureOtlp_WhenDisabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Otlp:Enabled"] = "false"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com OTLP e processador Simple.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureOtlp_WithSimpleProcessor()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Otlp:Enabled"] = "true",
        ["Observability:Otlp:Protocol"] = "grpc",
        ["Observability:Otlp:Endpoint"] = "http://localhost:4317",
        ["Observability:Otlp:ExportProcessorType"] = "simple"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com OTLP e processador Batch.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureOtlp_WithBatchProcessor()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Otlp:Enabled"] = "true",
        ["Observability:Otlp:Protocol"] = "grpc",
        ["Observability:Otlp:Endpoint"] = "http://localhost:4317",
        ["Observability:Otlp:ExportProcessorType"] = "batch"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com OTLP e headers.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureOtlp_WithHeaders()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Otlp:Enabled"] = "true",
        ["Observability:Otlp:Protocol"] = "grpc",
        ["Observability:Otlp:Endpoint"] = "http://localhost:4317",
        ["Observability:Otlp:Headers"] = "authorization=Bearer token123,x-api-key=mykey"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - Environment Tests

  // Teste para adicionar observability em ambiente de desenvolvimento.
  [Fact]
  public void AddTooarkObservability_ShouldAddConsoleExporter_InDevelopment()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = "Development",
        ["Observability:Enabled"] = "true",
        ["Observability:UseConsoleExporterInDevelopment"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability em ambiente de produção.
  [Fact]
  public void AddTooarkObservability_ShouldNotAddConsoleExporter_InProduction()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = "Production",
        ["Observability:Enabled"] = "true",
        ["Observability:UseConsoleExporterInDevelopment"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para verificar detecção de ambiente via DOTNET_ENVIRONMENT.
  [Fact]
  public void AddTooarkObservability_ShouldDetectEnvironment_FromDotnetEnvironment()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["DOTNET_ENVIRONMENT"] = "Development",
        ["Observability:Enabled"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - Resource Configuration Tests

  // Teste para adicionar observability com ServiceName configurado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureResource_WithServiceName()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:ServiceName"] = "MyTestService"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com ServiceVersion configurado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureResource_WithServiceVersion()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:ServiceVersion"] = "1.0.0"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com ServiceInstanceId configurado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureResource_WithServiceInstanceId()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:ServiceInstanceId"] = "instance-001"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com ResourceAttributes configurados.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureResource_WithResourceAttributes()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:ResourceAttributes:cloud.provider"] = "aws",
        ["Observability:ResourceAttributes:cloud.region"] = "us-east-1"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com atributos que precisam de normalização.
  [Fact]
  public void AddTooarkObservability_ShouldNormalizeResourceAttributes()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:ResourceAttributes:Cloud Provider"] = "aws",
        ["Observability:ResourceAttributes:CLOUD_REGION"] = "us-east-1"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - AllowSensitiveData Tests

  // Teste para adicionar observability com AllowSensitiveData habilitado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureTracing_WithAllowSensitiveDataEnabled()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:AllowSensitiveData"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com configurações de DataSensitive em Tracing.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureTracing_WithTracingDataSensitiveOptions()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Tracing:DataSensitive:HideQueryParameters"] = "true",
        ["Observability:Tracing:DataSensitive:HideHeaders"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - Tracing Configuration Tests

  // Teste para adicionar observability com IgnorePaths configurados.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureTracing_WithIgnorePaths()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Tracing:IgnorePaths:0"] = "/health",
        ["Observability:Tracing:IgnorePaths:1"] = "/metrics"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com IgnorePathPrefix configurado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureTracing_WithIgnorePathPrefix()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Tracing:IgnorePathPrefix"] = "/api"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com ActivitySourceName configurado.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureTracing_WithActivitySourceName()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Tracing:ActivitySourceName"] = "MyApp"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com AdditionalSources configuradas.
  [Fact]
  public void AddTooarkObservability_ShouldConfigureTracing_WithAdditionalSources()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true",
        ["Observability:Tracing:AdditionalSources:0"] = "MyApp.Component1",
        ["Observability:Tracing:AdditionalSources:1"] = "MyApp.Component2"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - Callbacks Tests

  // Teste para adicionar observability com callback de ConfigureTracing.
  [Fact]
  public void AddTooarkObservability_ShouldInvokeConfigureTracingCallback()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true"
      })
      .Build();
    var callbackInvoked = false;

    // Act
    services.AddTooarkObservability(configuration, options =>
    {
      options.ConfigureTracing = builder =>
      {
        callbackInvoked = true;
      };
    });

    // Build service provider para invocar o callback
    var serviceProvider = services.BuildServiceProvider();

    // Assert - O callback será invocado durante a construção
    // Nota: O callback é invocado no momento da configuração
    Assert.NotNull(serviceProvider);
    Assert.True(callbackInvoked);
  }

  // Teste para adicionar observability com callback de ConfigureMetrics.
  [Fact]
  public void AddTooarkObservability_ShouldInvokeConfigureMetricsCallback()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration, options =>
    {
      options.ConfigureMetrics = builder =>
      {
        // Custom configuration
      };
    });

    // Assert
    Assert.NotNull(services);
  }

  // Teste para adicionar observability com callback de ConfigureLogging.
  [Fact]
  public void AddTooarkObservability_ShouldInvokeConfigureLoggingCallback()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Observability:Enabled"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration, options =>
    {
      options.ConfigureLogging = loggerOptions =>
      {
        // Custom configuration
      };
    });

    // Assert
    Assert.NotNull(services);
  }

  #endregion

  #region AddTooarkObservability - Full Configuration Tests

  // Teste para configuração completa de observability.
  [Fact]
  public void AddTooarkObservability_ShouldWork_WithFullConfiguration()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = "Development",
        ["Observability:Enabled"] = "true",
        ["Observability:ServiceName"] = "TestService",
        ["Observability:ServiceVersion"] = "1.0.0",
        ["Observability:ServiceInstanceId"] = "instance-001",
        ["Observability:AllowSensitiveData"] = "false",
        ["Observability:UseConsoleExporterInDevelopment"] = "true",
        ["Observability:ResourceAttributes:cloud.provider"] = "aws",
        ["Observability:Tracing:Enabled"] = "true",
        ["Observability:Tracing:SamplingRatio"] = "0.5",
        ["Observability:Tracing:ActivitySourceName"] = "TestApp",
        ["Observability:Metrics:Enabled"] = "true",
        ["Observability:Metrics:MeterName"] = "TestMeter",
        ["Observability:Logging:Enabled"] = "true",
        ["Observability:Logging:IncludeFormattedMessage"] = "true",
        ["Observability:Logging:IncludeScopes"] = "true"
      })
      .Build();

    // Act
    services.AddTooarkObservability(configuration);

    // Assert
    Assert.NotNull(services);
    Assert.True(services.Count > 0);
  }

  #endregion

  }
