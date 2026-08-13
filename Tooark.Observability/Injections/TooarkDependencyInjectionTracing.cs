using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tooark.Observability.Options;

namespace Tooark.Observability.Injections;

/// <summary>
/// Classe para adicionar configurações de Tracing ao TracerProviderBuilder.
/// </summary>
public static partial class TooarkDependencyInjection
{
  #region Configure Tracing

  /// <summary>
  /// Configura o TracerProviderBuilder.
  /// </summary>
  /// <remarks>
  /// Adiciona instrumentations, sources, sampling e exportadores conforme as opções fornecidas.
  /// </remarks>
  /// <param name="builder">TracerProviderBuilder a ser configurado.</param>
  /// <param name="options">Opções de Observability.</param>
  /// /// <param name="resourceBuilder">ResourceBuilder configurado.</param>
  /// <param name="isDevelopment">Indica se está em ambiente de desenvolvimento.</param>
  internal static void ConfigureTracing(
    TracerProviderBuilder builder,
    ObservabilityOptions options,
    ResourceBuilder resourceBuilder,
    bool isDevelopment
  )
  {
    // Configura o resource
    builder.SetResourceBuilder(resourceBuilder);

    // Adiciona instrumentação padrão
    builder.AddAspNetCoreInstrumentation(aspNetOptions =>
    {
      // Registra exceções como evento + tags no span
      aspNetOptions.RecordException = true;

      // Habilita suporte a SignalR e Blazor
      aspNetOptions.EnableAspNetCoreSignalRSupport = true;
      aspNetOptions.EnableRazorComponentsSupport = true;

      // Filtra paths conforme configuração
      aspNetOptions.Filter = BuildAspNetCoreFilter(options);

      // Remove dados sensíveis dos atributos
      aspNetOptions.EnrichWithHttpRequest = BuildAspNetCoreEnricher(options);
    });

    // Adiciona instrumentação HTTP Client
    builder.AddHttpClientInstrumentation(httpClientOptions =>
    {
      // Registra exceções como evento + tags no span
      httpClientOptions.RecordException = true;

      // Remove dados sensíveis dos atributos
      httpClientOptions.EnrichWithHttpRequestMessage = BuildHttpClientEnricher(options);
    });

    // Adiciona sources padrão e customizadas
    builder.AddSource(options.Tracing.ActivitySourceName);
    foreach (var source in options.Tracing.AdditionalSources)
    {
      builder.AddSource(source);
    }

    // Configura sampling
    builder.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.Tracing.SamplingRatio)));

    // Configura exportadores
    ConfigureTracingExporters(builder, options, isDevelopment);

    // Aplica configurações customizadas
    options.ConfigureTracing?.Invoke(builder);
  }

  #endregion

  #region Configure OTLP Exporter

  /// <summary>
  /// /// Configura os exportadores de tracing.
  /// </summary>
  /// /// <param name="builder">TracerProviderBuilder a ser configurado.</param>
  /// <param name="options">Opções de Observability.</param>
  /// <param name="isDevelopment">Indica se está em ambiente de desenvolvimento.</param>
  internal static void ConfigureTracingExporters(
    TracerProviderBuilder builder,
    ObservabilityOptions options,
    bool isDevelopment
  )
  {
    var effectiveOtlpOptions = ResolveOtlpOptions(options, options.Tracing.Otlp);

    // Rastreia se algum exportador foi configurado
    var hasOtlpExporter = false;

    // Configura OTLP exporter se habilitado
    if (effectiveOtlpOptions.Enabled)
    {
      // Valida o endpoint de forma eager (independente do timing do callback do OpenTelemetry)
      ValidateOtlpEndpoint(effectiveOtlpOptions);

      // Configura OTLP exporter
      builder.AddOtlpExporter(exporterOptions =>
      {
        ConfigureOtlpExporter(exporterOptions, effectiveOtlpOptions);
      });

      hasOtlpExporter = true;
    }

    // Console exporter em desenvolvimento se não houver OTLP configurado
    if (isDevelopment && options.UseConsoleExporterInDevelopment && !hasOtlpExporter)
    {
      builder.AddConsoleExporter();
    }
  }

  #endregion

  #region Filters and Enrichers Builders

  /// <summary>
  /// Constrói o filtro para ASP.NET Core.
  /// </summary>
  /// <param name="options">Opções de Observability.</param>
  /// <returns>Função de filtro.</returns>
  internal static Func<HttpContext, bool> BuildAspNetCoreFilter(ObservabilityOptions options)
  {
    var prefix = options.Tracing.IgnorePathPrefix ?? string.Empty;

    // Normaliza o prefixo: adiciona '/' no início se necessário e remove '/' do inicio e final
    if (!string.IsNullOrWhiteSpace(prefix))
    {
      prefix = "/" + prefix.TrimStart('/').TrimEnd('/');
    }

    // Pré-computa os paths completos a serem ignorados (uma única vez, não a cada requisição)
    var ignorePaths = options.Tracing.IgnorePaths
      .Select(p => prefix + "/" + p.TrimStart('/').TrimEnd('/'))
      .ToArray();

    // Sem paths a ignorar, rastreia todas as requisições
    if (ignorePaths.Length == 0)
    {
      return _ => true;
    }

    return httpContext =>
    {
      var path = httpContext.Request.Path.Value ?? string.Empty;

      // Retorna true se o path NÃO estiver na lista de ignorados
      return !ignorePaths.Any(ignored => path.StartsWith(ignored, StringComparison.OrdinalIgnoreCase));
    };
  }

  /// <summary>
  /// Constrói o enricher para ASP.NET Core.
  /// </summary>
  /// <param name="options">Opções de Observability.</param>
  /// <returns>Função de enricher.</returns>
  internal static Action<Activity, HttpRequest> BuildAspNetCoreEnricher(ObservabilityOptions options)
  {
    return (activity, request) =>
    {
      // Se estiver configurado para tratar dados sensíveis, não adiciona nada
      if (options.AllowSensitiveData)
      {
        return;
      }

      // Configura remoção de dados sensíveis de Tracing
      var dataSensitive = options.Tracing.DataSensitive;

      // Remove a query string dos atributos do span
      if (dataSensitive.HideQueryParameters)
      {
        // Convenção semântica atual: a query fica no atributo url.query (SetTag com null remove a tag)
        if (activity.GetTagItem("url.query") is not null)
        {
          activity.SetTag("url.query", null);
        }

        // Convenção legada: reescreve http.target quando presente com query
        var safeTarget = request.Path.Value;
        if (!string.IsNullOrWhiteSpace(safeTarget)
          && activity.GetTagItem("http.target") is string currentTarget
          && currentTarget.Contains('?', StringComparison.Ordinal))
        {
          activity.SetTag("http.target", safeTarget);
        }
      }

      // Mascara headers sensíveis (não copia valores reais)
      if (dataSensitive.HideHeaders)
      {
        // Mascara todos os headers sensíveis configurados
        foreach (var headerName in dataSensitive.SensitiveRequestHeaders)
        {
          // Ignora nomes inválidos
          if (string.IsNullOrWhiteSpace(headerName))
          {
            continue;
          }

          // Remove espaços extras
          var trimmed = headerName.Trim();

          // Se o header existir, mascara o valor
          if (request.Headers.ContainsKey(trimmed))
          {
            activity.SetTag($"http.request.header.{trimmed.ToLowerInvariant()}", "[REDACTED]");
          }
        }
      }
    };
  }

  /// <summary>
  /// Constrói o enricher para HttpClient.
  /// </summary>
  /// <param name="options">Opções de Observability.</param>
  /// <returns>Função de enricher.</returns>
  internal static Action<Activity, HttpRequestMessage> BuildHttpClientEnricher(ObservabilityOptions options)
  {
    return (activity, request) =>
    {
      // Se estiver configurado para tratar dados sensíveis, não adiciona nada
      if (options.AllowSensitiveData)
      {
        return;
      }

      // Configura remoção de dados sensíveis de Tracing
      var dataSensitive = options.Tracing.DataSensitive;

      // Remove a query string dos atributos do span
      if (dataSensitive.HideQueryParameters)
      {
        // Convenção semântica atual: url.full carrega a URL completa, incluindo a query
        if (activity.GetTagItem("url.full") is string fullUrl
          && fullUrl.IndexOf('?', StringComparison.Ordinal) is int queryIndex
          && queryIndex >= 0)
        {
          activity.SetTag("url.full", fullUrl[..queryIndex]);
        }

        // Convenção legada: reescreve http.target quando presente com query
        var safeTarget = request.RequestUri?.AbsolutePath;
        if (!string.IsNullOrWhiteSpace(safeTarget)
          && activity.GetTagItem("http.target") is string currentTarget
          && currentTarget.Contains('?', StringComparison.Ordinal))
        {
          activity.SetTag("http.target", safeTarget);
        }
      }

      // Mascara headers sensíveis (não copia valores reais)
      if (dataSensitive.HideHeaders)
      {
        // Mascara todos os headers sensíveis configurados
        foreach (var headerName in dataSensitive.SensitiveRequestHeaders)
        {
          // Ignora nomes inválidos
          if (string.IsNullOrWhiteSpace(headerName))
          {
            continue;
          }

          // Remove espaços extras
          var trimmed = headerName.Trim();

          // Verifica se o header existe na requisição
          if (request.Headers.TryGetValues(trimmed, out _) ||
              (request.Content?.Headers?.TryGetValues(trimmed, out _) == true))
          {
            activity.SetTag($"http.request.header.{trimmed.ToLowerInvariant()}", "[REDACTED]");
          }
        }
      }
    };
  }

  #endregion
}
