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
      var dataSensitive = options.Tracing.DataSensitive;

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
      var dataSensitive = options.Tracing.DataSensitive;

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
  internal static Func<HttpContext, bool> BuildAspNetCoreFilter(ObservabilityOptions options) => httpContext =>
  {
    var path = httpContext.Request.Path.Value ?? string.Empty;
    var prefix = options.Tracing.IgnorePathPrefix ?? string.Empty;

    // Normaliza o prefixo: adiciona '/' no início se necessário e remove '/' do inicio e final
    if (!string.IsNullOrWhiteSpace(prefix))
    {
      prefix = "/" + prefix.TrimStart('/').TrimEnd('/');
    }

    // Constrói os paths completos a serem ignorados
    var ignorePath = options.Tracing.IgnorePaths
      .Select(p => prefix + "/" + p.TrimStart('/').TrimEnd('/'))
      .ToArray();

    // Retorna true se o path NÃO estiver na lista de ignorados
    return ignorePath.Length > 0
      && !ignorePath.Any(ignorePath => path.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase));
  };

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
      if (options.DataSensitive)
      {
        return;
      }

      // Configura remoção de dados sensíveis de Tracing
      var dataSensitive = options.Tracing.DataSensitive;

      // Remove dados sensíveis dos atributos
      var safeTarget = request.Path.Value ?? string.Empty;
      if (dataSensitive.HideQueryParameters && !string.IsNullOrWhiteSpace(safeTarget))
      {
        // Se o path atual contém query, substitui pelo safeTarget
        var currentTarget = activity.GetTagItem("http.target") as string;
        if (string.IsNullOrWhiteSpace(currentTarget) || currentTarget.Contains('?', StringComparison.Ordinal))
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
      if (options.DataSensitive)
      {
        return;
      }

      // Configura remoção de dados sensíveis de Tracing
      var dataSensitive = options.Tracing.DataSensitive;

      // Remove dados sensíveis dos atributos
      var safeTarget = request.RequestUri?.AbsolutePath;
      if (dataSensitive.HideQueryParameters && !string.IsNullOrWhiteSpace(safeTarget))
      {
        // Se contém query, substitui pelo safeTarget
        var currentTarget = activity.GetTagItem("http.target") as string;
        if (string.IsNullOrWhiteSpace(currentTarget) || currentTarget.Contains('?', StringComparison.Ordinal))
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
