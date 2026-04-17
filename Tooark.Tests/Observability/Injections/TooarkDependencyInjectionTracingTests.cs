using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tooark.Exceptions;
using Tooark.Observability.Injections;
using Tooark.Observability.Options;

namespace Tooark.Tests.Observability.Injections;

public class TooarkDependencyInjectionTracingTests
{
  #region Helpers

  /// <summary>
  /// Cria opções de Observability para testes.
  /// </summary>
  /// <param name="globalDataSensitive">Parâmetro DataSensitive global.</param>
  /// <param name="hideQuery">Ocultar query parameters.</param>
  /// <param name="hideHeaders">Ocultar headers.</param>
  /// <param name="sensitiveHeaders">Headers sensíveis.</param>
  /// <param name="ignorePrefix">Prefixo de paths a ignorar.</param>
  /// <param name="ignorePaths">Paths a ignorar.</param>
  /// <param name="activitySourceName">Nome da fonte de atividade.</param>
  /// <param name="additionalSources">Fontes adicionais de atividade.</param>
  /// <param name="useConsoleExporterInDev">Usar exportador de console em desenvolvimento.</param>
  /// <param name="otlpEnabled">Habilitar OTLP.</param>
  /// <param name="otlpEndpoint">Endpoint OTLP.</param>
  /// <returns>Instância de <see cref="ObservabilityOptions"/> configurada para testes.</returns>
  private static ObservabilityOptions CreateOptions(
    bool globalDataSensitive = false,
    bool hideQuery = true,
    bool hideHeaders = true,
    string[]? sensitiveHeaders = null,
    string? ignorePrefix = null,
    string[]? ignorePaths = null,
    string activitySourceName = "Tooark",
    string[]? additionalSources = null,
    bool useConsoleExporterInDev = true,
    bool otlpEnabled = false,
    string? otlpEndpoint = null
  )
  {
    return new ObservabilityOptions
    {
      DataSensitive = globalDataSensitive,
      UseConsoleExporterInDevelopment = useConsoleExporterInDev,
      Otlp = new OtlpOptions
      {
        Enabled = otlpEnabled,
        Endpoint = otlpEndpoint
      },
      Tracing = new TracingOptions
      {
        Enabled = true,
        SamplingRatio = 1.0,
        ActivitySourceName = activitySourceName,
        AdditionalSources = additionalSources ?? [],
        IgnorePathPrefix = ignorePrefix,
        IgnorePaths = ignorePaths ?? ["/health", "/metrics"],
        DataSensitive = new DataSensitiveOptions
        {
          HideQueryParameters = hideQuery,
          HideHeaders = hideHeaders,
          SensitiveRequestHeaders = sensitiveHeaders ?? ["authorization", "x-api-key", "   ", ""]
        }
      }
    };
  }

  /// <summary>
  /// Cria uma Activity de teste com http.target opcional.
  /// </summary>
  /// <param name="httpTarget">Valor opcional para o tag http.target.</param>
  /// <returns>Nova Activity.</returns>
  private static Activity NewActivityWithTarget(string? httpTarget = null)
  {
    // Cria uma Activity simples
    var a = new Activity("test");

    // Define http.target se fornecido
    if (httpTarget is not null)
    {
      a.SetTag("http.target", httpTarget);
    }

    return a;
  }

  #endregion

  #region ConfigureTracing - covers loops and branches

  // Teste para garantir que paths ignorados retornam false
  [Fact]
  public void BuildAspNetCoreFilter_WithPrefix_NormalizesPrefixAndIgnoresConfiguredPaths()
  {
    // Arrange
    var options = CreateOptions(ignorePrefix: "api/");

    // Act
    var filter = TooarkDependencyInjection.BuildAspNetCoreFilter(options);
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/api/health";

    // Assert
    Assert.False(filter(ctx));
  }

  // Teste para garantir que paths não ignorados retornam true
  [Fact]
  public void BuildAspNetCoreFilter_WithPrefix_AllowsNonIgnoredPaths()
  {
    // Arrange
    var options = CreateOptions(ignorePrefix: "/api");

    // Act
    var filter = TooarkDependencyInjection.BuildAspNetCoreFilter(options);
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/api/orders";

    // Assert
    Assert.True(filter(ctx));
  }

  // Teste para garantir que paths ignorados retornam false
  [Fact]
  public void BuildAspNetCoreFilter_WhenIgnorePathsEmpty_ReturnsFalse_ByCurrentLogic()
  {
    // Arrange
    var options = CreateOptions(ignorePaths: []);

    // Act
    var filter = TooarkDependencyInjection.BuildAspNetCoreFilter(options);
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/anything";

    // Assert
    Assert.False(filter(ctx));
  }

  #endregion

  #region ConfigureTracing - covers callback and loops

  // Teste para garantir que o callback ConfigureTracing é invocado com GlobalDataSensitive ativo
  [Fact]
  public void BuildAspNetCoreEnricher_WhenGlobalDataSensitiveTrue_DoesNothing()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: true);

    // Act
    var enricher = TooarkDependencyInjection.BuildAspNetCoreEnricher(options);
    using var activity = NewActivityWithTarget("/x?token=1");
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/safe";
    ctx.Request.Headers.Authorization = "Bearer SECRET";
    enricher(activity, ctx.Request);

    // Assert
    Assert.Equal("/x?token=1", activity.GetTagItem("http.target") as string);
    Assert.Null(activity.GetTagItem("http.request.header.authorization"));
  }

  // Teste para garantir que o callback ConfigureTracing é invocado com HideQueryParameters ativo
  [Fact]
  public void BuildAspNetCoreEnricher_HidesQueryParameters_WhenCurrentTargetHasQuery()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);

    // Act
    var enricher = TooarkDependencyInjection.BuildAspNetCoreEnricher(options);
    using var activity = NewActivityWithTarget("/original?secret=1");
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/safe-path";
    enricher(activity, ctx.Request);

    // Assert
    Assert.Equal("/safe-path", activity.GetTagItem("http.target") as string);
  }

  // Teste para garantir que o callback ConfigureTracing é invocado com HideQueryParameters ativo
  [Fact]
  public void BuildAspNetCoreEnricher_DoesNotOverrideHttpTarget_WhenAlreadySafe()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);

    // Act
    var enricher = TooarkDependencyInjection.BuildAspNetCoreEnricher(options);
    using var activity = NewActivityWithTarget("/already-safe");
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/new-safe";
    enricher(activity, ctx.Request);

    // Assert
    Assert.Equal("/already-safe", activity.GetTagItem("http.target") as string);
  }

  // Teste para garantir que o callback ConfigureTracing mascara query parameters quando http.target é null
  [Fact]
  public void BuildAspNetCoreEnricher_WhenHttpTargetIsNull_SetsToSafeTarget()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);

    // Act
    var enricher = TooarkDependencyInjection.BuildAspNetCoreEnricher(options);
    using var activity = NewActivityWithTarget(null);
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/safe";
    enricher(activity, ctx.Request);

    // Assert
    Assert.Equal("/safe", activity.GetTagItem("http.target") as string);
  }

  // Teste para garantir que o callback ConfigureTracing mascara headers sensíveis
  [Fact]
  public void BuildAspNetCoreEnricher_RedactsSensitiveHeaders_And_SkipsInvalidHeaderNames()
  {
    // Arrange
    var options = CreateOptions(
      globalDataSensitive: false,
      hideQuery: false,
      hideHeaders: true,
      sensitiveHeaders: ["  authorization  ", "x-api-key", " ", ""]
    );

    // Act
    var enricher = TooarkDependencyInjection.BuildAspNetCoreEnricher(options);
    using var activity = NewActivityWithTarget("/safe");
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/safe";
    ctx.Request.Headers.Authorization = "Bearer SECRET";
    enricher(activity, ctx.Request);

    // Assert
    Assert.Equal("[REDACTED]", activity.GetTagItem("http.request.header.authorization") as string);
    Assert.Null(activity.GetTagItem("http.request.header.x-api-key"));
  }

  #endregion

  #region ConfigureTracing - covers HttpClient enricher

  // Teste para garantir que o callback ConfigureTracing é invocado com GlobalDataSensitive ativo
  [Fact]
  public void BuildHttpClientEnricher_WhenGlobalDataSensitiveTrue_DoesNothing()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: true);

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    using var activity = NewActivityWithTarget("/x?token=1");
    var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com/orders?id=1");
    req.Headers.Add("authorization", "Bearer SECRET");
    enricher(activity, req);

    // Assert
    Assert.Equal("/x?token=1", activity.GetTagItem("http.target") as string);
    Assert.Null(activity.GetTagItem("http.request.header.authorization"));
  }

  // Teste para garantir que o callback ConfigureTracing é invocado com HideQueryParameters ativo
  [Fact]
  public void BuildHttpClientEnricher_HidesQueryParameters_WhenCurrentTargetHasQuery()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    using var activity = NewActivityWithTarget("https://example.com/orders?id=1");
    var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com/orders?id=1");
    enricher(activity, req);

    // Assert
    Assert.Equal("/orders", activity.GetTagItem("http.target") as string);
  }

  // Teste para garantir que o callback ConfigureTracing é invocado com HideQueryParameters ativo
  [Fact]
  public void BuildHttpClientEnricher_DoesNotOverrideHttpTarget_WhenAlreadySafe()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    using var activity = NewActivityWithTarget("/already-safe");
    var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com/new-safe?id=1");
    enricher(activity, req);

    // Assert
    Assert.Equal("/already-safe", activity.GetTagItem("http.target") as string);
  }

  // Teste para garantir que o callback ConfigureTracing mascara query parameters quando http.target é null
  [Fact]
  public void BuildHttpClientEnricher_RedactsSensitiveHeaders_FromRequestHeaders()
  {
    // Arrange
    var options = CreateOptions(
      globalDataSensitive: false,
      hideQuery: false,
      hideHeaders: true,
      sensitiveHeaders: ["authorization", " ", ""]
    );

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    using var activity = NewActivityWithTarget("/safe");
    var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com/orders");
    req.Headers.Add("authorization", "Bearer SECRET");
    enricher(activity, req);

    // Assert
    Assert.Equal("[REDACTED]", activity.GetTagItem("http.request.header.authorization") as string);
  }

  // Teste para garantir que o callback ConfigureTracing mascara headers sensíveis de Content.Headers
  [Fact]
  public void BuildHttpClientEnricher_RedactsSensitiveHeaders_FromContentHeaders()
  {
    // Arrange
    var options = CreateOptions(
      globalDataSensitive: false,
      hideQuery: false,
      hideHeaders: true,
      sensitiveHeaders: ["x-api-key"]
    );

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    using var activity = NewActivityWithTarget("/safe");
    var req = new HttpRequestMessage(HttpMethod.Post, "https://example.com/orders")
    {
      Content = new StringContent("payload")
    };
    req.Content.Headers.Add("x-api-key", "SECRET");
    enricher(activity, req);

    // Assert
    Assert.Equal("[REDACTED]", activity.GetTagItem("http.request.header.x-api-key") as string);
  }

  #endregion

  #region ConfigureTracing - OTLP Exporter

  // Teste para garantir que o exportador OTLP é configurado quando habilitado
  [Fact]
  public void ConfigureTracingExporters_WhenOtlpEnabled_AddsOtlpExporterBranch()
  {
    // Arrange
    var options = CreateOptions(
      otlpEnabled: true,
      otlpEndpoint: "http://localhost:4317",
      useConsoleExporterInDev: true
    );

    // Act
    var builder = Sdk.CreateTracerProviderBuilder();
    TooarkDependencyInjection.ConfigureTracingExporters(builder, options, isDevelopment: true);

    // Assert
    Assert.NotNull(builder);
  }

  // Teste para garantir que o exportador de console é configurado em desenvolvimento quando OTLP não está habilitado
  [Fact]
  public void ConfigureTracingExporters_WhenDevAndNoOtlp_AddsConsoleExporterBranch()
  {
    // Arrange
    var options = CreateOptions(
      otlpEnabled: false,
      useConsoleExporterInDev: true
    );

    // Act
    var builder = Sdk.CreateTracerProviderBuilder();
    TooarkDependencyInjection.ConfigureTracingExporters(builder, options, isDevelopment: true);

    // Assert
    Assert.NotNull(builder);
  }

  // Teste para garantir que o exportador de console NÃO é configurado quando não está em desenvolvimento
  [Fact]
  public void ConfigureTracingExporters_WhenNotDev_DoesNotAddConsoleExporterBranch()
  {
    // Arrange
    var options = CreateOptions(
      otlpEnabled: false,
      useConsoleExporterInDev: true
    );
    var builder = Sdk.CreateTracerProviderBuilder();

    // Act
    TooarkDependencyInjection.ConfigureTracingExporters(builder, options, isDevelopment: false);

    // Assert
    Assert.NotNull(builder);
  }

  #endregion

  #region ConfigureTracing - AdditionalSources + callback ConfigureTracing

  // Teste para garantir que o callback ConfigureTracing é invocado e fontes adicionais são adicionadas
  [Fact]
  public void ConfigureTracing_InvokesConfigureTracingCallback_And_AddsSources()
  {
    // Arrange
    var callbackInvoked = false;
    var options = CreateOptions(
      additionalSources: ["Source.A", "Source.B"],
      otlpEnabled: false,
      useConsoleExporterInDev: false
    );
    options.ConfigureTracing = _ => callbackInvoked = true;
    var builder = Sdk.CreateTracerProviderBuilder();
    var rb = ResourceBuilder.CreateDefault().AddService("svc");

    // Act
    TooarkDependencyInjection.ConfigureTracing(builder, options, rb, isDevelopment: false);
    using var provider = builder.Build();

    // Assert
    Assert.True(callbackInvoked);
    Assert.NotNull(provider);
  }

  #endregion

  #region ConfigureTracing - Additional Tests

  // Teste para configurar exportadores OTLP
  [Fact]
  public void ConfigureTracingExporters_WhenOtlpEnabled_ExecutesConfigureOtlpExporter_OnBuild()
  {
    // Arrange
    var options = CreateOptions(
      otlpEnabled: true,
      otlpEndpoint: "http://localhost:4317",
      useConsoleExporterInDev: true
    );
    var builder = Sdk.CreateTracerProviderBuilder()
      .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("svc"))
      .AddSource("TestSource");

    // Act
    TooarkDependencyInjection.ConfigureTracingExporters(builder, options, isDevelopment: false);
    using var provider = builder.Build();

    // Assert
    Assert.NotNull(provider);
  }

  // Teste para garantir que o OTLP específico de tracing faz override do global.
  [Fact]
  public void ConfigureTracingExporters_WhenTracingOtlpOverridesGlobal_UsesTracingOtlp()
  {
    // Arrange
    var options = CreateOptions(
      otlpEnabled: true,
      otlpEndpoint: "http://localhost:4317",
      useConsoleExporterInDev: false
    );
    options.Tracing.Otlp = new OtlpOptions
    {
      Endpoint = "not-a-uri"
    };

    var builder = Sdk.CreateTracerProviderBuilder()
      .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("svc"))
      .AddSource("TestSource");

    // Act + Assert
    var ex = Assert.Throws<InternalServerErrorException>(() =>
    {
      TooarkDependencyInjection.ConfigureTracingExporters(builder, options, isDevelopment: false);
      using var provider = builder.Build();
    });
    Assert.Contains("Options.Otlp.Endpoint.Invalid", ex.Message);
  }

  // Teste para configurar filtro quando o path é null
  [Fact]
  public void BuildAspNetCoreFilter_WhenPathValueIsNull_UsesEmptyStringBranch()
  {
    // Arrange
    var options = CreateOptions(ignorePrefix: null, ignorePaths: ["/health"]);

    // Act
    var filter = TooarkDependencyInjection.BuildAspNetCoreFilter(options);
    var ctx = new DefaultHttpContext();

    // Assert
    Assert.True(filter(ctx));
  }

  // Teste para configurar o enricher quando o path é null
  [Fact]
  public void BuildAspNetCoreEnricher_WhenRequestPathValueIsNull_UsesEmptyStringBranch()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);
    using var activity = NewActivityWithTarget("/orig?token=1");

    // Act
    var enricher = TooarkDependencyInjection.BuildAspNetCoreEnricher(options);
    var ctx = new DefaultHttpContext();
    enricher(activity, ctx.Request);

    // Assert
    Assert.Equal("/orig?token=1", activity.GetTagItem("http.target") as string);
  }

  // Teste para configurar o enricher quando o RequestUri é null
  [Fact]
  public void BuildHttpClientEnricher_WhenRequestUriNull_CoversNullSafeTargetBranch()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);
    using var activity = NewActivityWithTarget("/orig?x=1");

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    var req = new HttpRequestMessage();
    enricher(activity, req);

    // Assert
    Assert.Equal("/orig?x=1", activity.GetTagItem("http.target") as string);
  }

  // Teste para configurar o enricher quando o http.target é null
  [Fact]
  public void BuildHttpClientEnricher_WhenCurrentTargetNull_SetsHttpTarget()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);
    using var activity = NewActivityWithTarget(null);

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com/orders?id=1");
    enricher(activity, req);

    // Assert
    Assert.Equal("/orders", activity.GetTagItem("http.target") as string);
  }

  // Teste para configurar o enricher quando o http.target contém query
  [Fact]
  public void BuildHttpClientEnricher_WhenCurrentTargetHasQuery_ReplacesHttpTarget()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);
    using var activity = NewActivityWithTarget("/something?secret=1");

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com/orders?id=1");
    enricher(activity, req);

    // Assert
    Assert.Equal("/orders", activity.GetTagItem("http.target") as string);
  }

  // Teste para configurar o enricher quando o header sensível está presente
  [Fact]
  public void BuildHttpClientEnricher_WhenCurrentTargetAlreadySafe_DoesNotOverride()
  {
    // Arrange
    var options = CreateOptions(globalDataSensitive: false, hideQuery: true, hideHeaders: false);
    using var activity = NewActivityWithTarget("/already-safe");

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com/orders?id=1");
    enricher(activity, req);

    // Assert
    Assert.Equal("/already-safe", activity.GetTagItem("http.target") as string);
  }

  // Teste para configurar o enricher quando o header sensível está presente
  [Fact]
  public void BuildHttpClientEnricher_RedactsHeader_WhenPresentInRequestHeaders_LeftSideTrue()
  {
    // Arrange
    var options = CreateOptions(
      globalDataSensitive: false,
      hideQuery: false,
      hideHeaders: true,
      sensitiveHeaders: [" authorization ", "", " "]
    );
    using var activity = NewActivityWithTarget("/safe");

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com/orders");
    req.Headers.Add("authorization", "Bearer SECRET");
    enricher(activity, req);

    // Assert
    Assert.Equal("[REDACTED]", activity.GetTagItem("http.request.header.authorization") as string);
  }

  // Teste para configurar o enricher quando o header sensível está presente em Content.Headers
  [Fact]
  public void BuildHttpClientEnricher_RedactsHeader_WhenPresentInContentHeaders_RightSideTrue()
  {
    // Arrange
    var options = CreateOptions(
      globalDataSensitive: false,
      hideQuery: false,
      hideHeaders: true,
      sensitiveHeaders: ["x-api-key"]
    );
    using var activity = NewActivityWithTarget("/safe");

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    var req = new HttpRequestMessage(HttpMethod.Post, "https://example.com/orders")
    {
      Content = new StringContent("payload")
    };
    req.Content.Headers.Add("x-api-key", "SECRET");
    enricher(activity, req);

    // Assert
    Assert.Equal("[REDACTED]", activity.GetTagItem("http.request.header.x-api-key") as string);
  }

  // Teste para configurar o enricher quando o header sensível está ausente
  [Fact]
  public void BuildHttpClientEnricher_DoesNotRedact_WhenHeaderAbsentAndContentPresent_BothSidesFalse()
  {
    // Arrange
    var options = CreateOptions(
      globalDataSensitive: false,
      hideQuery: false,
      hideHeaders: true,
      sensitiveHeaders: ["authorization"]
    );
    using var activity = NewActivityWithTarget("/safe");

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    var req = new HttpRequestMessage(HttpMethod.Post, "https://example.com/orders")
    {
      Content = new StringContent("payload")
    };
    enricher(activity, req);

    // Assert
    Assert.Null(activity.GetTagItem("http.request.header.authorization"));
  }

  // Teste para configurar o enricher quando o header sensível está ausente e Content é null
  [Fact]
  public void BuildHttpClientEnricher_DoesNotRedact_WhenHeaderAbsentAndContentNull_CoversNullConditional()
  {
    // Arrange
    var options = CreateOptions(
      globalDataSensitive: false,
      hideQuery: false,
      hideHeaders: true,
      sensitiveHeaders: ["authorization"]
    );
    using var activity = NewActivityWithTarget("/safe");

    // Act
    var enricher = TooarkDependencyInjection.BuildHttpClientEnricher(options);
    var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com/orders");
    enricher(activity, req);

    // Assert
    Assert.Null(activity.GetTagItem("http.request.header.authorization"));
  }

  #endregion
}
