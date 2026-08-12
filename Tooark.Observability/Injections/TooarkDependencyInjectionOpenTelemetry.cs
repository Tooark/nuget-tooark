using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using Tooark.Exceptions;
using Tooark.Observability.Options;

// Permite acesso interno para testes unitários
// (necessário para testar métodos internos como NormalizeAttributeKey, ValidateAndNormalizeOtlpHeaders, etc.)
[assembly: InternalsVisibleTo("Tooark.Tests")]

namespace Tooark.Observability.Injections;

/// <summary>
/// Classe para adicionar os serviços de OpenTelemetry ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona os serviços de OpenTelemetry ao container de injeção de dependência.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções de Observability.</param>
  /// <returns>A coleção de serviços com OpenTelemetry configurado.</returns>
  public static IServiceCollection AddTooarkOpenTelemetry(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<ObservabilityOptions>? configure = null
  )
  {
    // Registra as opções do Observability a partir da configuração
    services.Configure<ObservabilityOptions>(configuration.GetSection(ObservabilityOptions.Section));

    // Aplica os overrides programáticos também ao IOptions do container
    if (configure is not null)
    {
      services.PostConfigure(configure);
    }

    // Carrega as opções da configuração
    var options = new ObservabilityOptions();
    configuration.GetSection(ObservabilityOptions.Section).Bind(options);

    // Aplica configurações programáticas
    configure?.Invoke(options);

    // Se desabilitado, não registra nada
    if (!options.Enabled)
    {
      return services;
    }

    // Detecta o ambiente
    var isDevelopment = IsDevelopmentEnvironment(configuration);

    // Configura o resource (identificação do serviço)
    var resourceBuilder = ConfigureResource(configuration, options);

    // Configura OpenTelemetry
    var otelBuilder = services.AddOpenTelemetry();

    // Configura Tracing
    if (options.Tracing.Enabled)
    {
      otelBuilder.WithTracing(builder => ConfigureTracing(builder, options, resourceBuilder, isDevelopment));
    }

    // Configura Metrics
    if (options.Metrics.Enabled)
    {
      otelBuilder.WithMetrics(builder => ConfigureMetrics(builder, options, resourceBuilder, isDevelopment));
    }

    // Configura Logging
    if (options.Logging.Enabled)
    {
      services.AddLogging(logging =>
      {
        logging.AddOpenTelemetry(loggerOptions =>
        {
          ConfigureLogging(loggerOptions, options, resourceBuilder, isDevelopment);
        });
      });
    }

    return services;
  }

  #region Internal Methods

  /// <summary>
  /// Obtém o ambiente de execução.
  /// </summary>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <returns>Nome do ambiente de execução.</returns>
  internal static string GetEnvironment(IConfiguration configuration)
  {
    return configuration["ASPNETCORE_ENVIRONMENT"]
        ?? configuration["DOTNET_ENVIRONMENT"]
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? "unknown_environment";
  }

  /// <summary>
  /// Normaliza uma chave de atributo para o formato aceito pelo OpenTelemetry.
  /// </summary>
  /// <param name="key">Chave do atributo.</param>
  /// <returns>Chave normalizada.</returns>
  internal static string NormalizeAttributeKey(string key)
  {
    // Se a chave for nula ou vazia, retorna como está
    if (string.IsNullOrEmpty(key))
    {
      return key ?? string.Empty;
    }

    // Converte para minúsculas, substitui espaços por pontos e remove caracteres inválidos
    var lower = SpaceRegex().Replace(key.Trim().ToLowerInvariant(), ".");

    // Remove caracteres inválidos
    return AttributesRegex().Replace(lower, "_");
  }

  /// <summary>
  /// Verifica se está em ambiente de desenvolvimento.
  /// </summary>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <returns>True se estiver em ambiente de desenvolvimento, caso contrário, false.</returns>
  internal static bool IsDevelopmentEnvironment(IConfiguration configuration)
  {
    // Verifica as variáveis de ambiente padrão
    var environment = GetEnvironment(configuration);

    // Considera desenvolvimento se o ambiente for "Development"
    return environment.Equals("Development", StringComparison.OrdinalIgnoreCase) == true ||
           environment.Equals("Dev", StringComparison.OrdinalIgnoreCase) == true;
  }

  /// <summary>
  /// Configura o ResourceBuilder com informações do serviço.
  /// </summary>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="options">Opções de Observability.</param>
  /// <returns>ResourceBuilder configurado.</returns>
  internal static ResourceBuilder ConfigureResource(IConfiguration configuration, ObservabilityOptions options)
  {
    // Obtém o nome, versão e ambiente do serviço
    var serviceName = options.ServiceName
      ?? Assembly.GetEntryAssembly()?.GetName().Name
      ?? "unknown_service";
    var serviceVersion = options.ServiceVersion
      ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
      ?? "unknown_version";
    var serviceInstanceId = options.ServiceInstanceId
      ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_INSTANCE_ID")
      ?? Guid.NewGuid().ToString("N");
    var serviceEnvironment = GetEnvironment(configuration);

    // Cria o ResourceBuilder
    var resourceBuilder = ResourceBuilder
      .CreateDefault()
      .AddService(
        serviceName: serviceName,
        serviceVersion: serviceVersion,
        serviceInstanceId: serviceInstanceId
      );

    // Atributos úteis e genéricos (não dependem do tipo de aplicação)
    // deployment.environment.name é a convenção semântica atual; deployment.environment é mantida para backends que ainda usam a chave antiga
    resourceBuilder.AddAttributes(new Dictionary<string, object>
    {
      ["deployment.environment"] = serviceEnvironment,
      ["deployment.environment.name"] = serviceEnvironment,
      ["host.name"] = Environment.MachineName,
      ["process.pid"] = Environment.ProcessId.ToString(),
      ["process.runtime.name"] = ".NET",
      ["process.runtime.version"] = Environment.Version.ToString(),
      ["process.runtime.description"] = RuntimeInformation.FrameworkDescription
    });

    // Atributos adicionais configuráveis (chaves que normalizam para o mesmo valor: a última vence)
    if (options.ResourceAttributes.Count > 0)
    {
      var customAttributes = new Dictionary<string, object>();

      foreach (var (key, value) in options.ResourceAttributes)
      {
        if (string.IsNullOrWhiteSpace(key))
        {
          continue;
        }

        customAttributes[NormalizeAttributeKey(key)] = value;
      }

      resourceBuilder.AddAttributes(customAttributes);
    }

    return resourceBuilder;
  }

  /// <summary>
  /// Valida o endpoint OTLP, lançando exceção quando inválido.
  /// </summary>
  /// <remarks>
  /// Deve ser invocado de forma eager (durante a configuração), e não somente dentro do callback
  /// do <c>AddOtlpExporter</c>, pois o momento de execução desse callback varia entre versões do
  /// OpenTelemetry e pode não disparar na instanciação dos provedores.
  /// </remarks>
  /// <param name="options">Opções de OTLP.</param>
  /// <exception cref="InternalServerErrorException">Quando o endpoint é nulo ou não é uma URI absoluta válida.</exception>
  internal static void ValidateOtlpEndpoint(OtlpOptions options)
  {
    if (options.Endpoint == null || !Uri.IsWellFormedUriString(options.Endpoint, UriKind.Absolute))
    {
      throw new InternalServerErrorException("Options.Otlp.Endpoint.Invalid");
    }
  }

  /// <summary>
  /// Configura as opções do exportador OTLP.
  /// </summary>
  /// <param name="otlpOptions">OtlpExporterOptions a ser configurado.</param>
  /// <param name="options">Opções de OTLP.</param>
  internal static void ConfigureOtlpExporter(OtlpExporterOptions otlpOptions, OtlpOptions options)
  {
    // Valida o endpoint
    ValidateOtlpEndpoint(options);

    // Configura o endpoint e protocolo (não-nulo garantido por ValidateOtlpEndpoint)
    otlpOptions.Endpoint = new Uri(options.Endpoint!);
    otlpOptions.Protocol = options.Protocol;

    // Configura o tipo de processador de exportação
    otlpOptions.ExportProcessorType = options.ExportProcessorType;

    // Configura opções do Batch, quando aplicável
    if (options.ExportProcessorType == ExportProcessorType.Batch)
    {
      var batch = options.Batch;
      var serverless = options.ServerlessOptimized;

      // Defaults serverless aplicam-se apenas aos valores não customizados pelo usuário
      var maxQueueSize = serverless && !batch.HasCustomMaxQueueSize
        ? OtlpOptions.ServerlessMaxQueueSize
        : batch.MaxQueueSize;
      var maxExportBatchSize = serverless && !batch.HasCustomMaxExportBatchSize
        ? OtlpOptions.ServerlessMaxExportBatchSize
        : batch.MaxExportBatchSize;
      var scheduledDelayMilliseconds = serverless && !batch.HasCustomScheduledDelay
        ? OtlpOptions.ServerlessScheduledDelayMilliseconds
        : batch.ScheduledDelayMilliseconds;

      // Configura as opções do processador de exportação em lote (lote nunca maior que a fila)
      otlpOptions.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
      {
        MaxQueueSize = maxQueueSize,
        MaxExportBatchSize = Math.Min(maxExportBatchSize, maxQueueSize),
        ScheduledDelayMilliseconds = scheduledDelayMilliseconds,
        ExporterTimeoutMilliseconds = batch.ExporterTimeoutMilliseconds
      };
    }

    // Configura headers
    otlpOptions.Headers = ValidateAndNormalizeOtlpHeaders(options.Headers);
  }

  /// <summary>
  /// Resolve a configuração efetiva de OTLP para um sinal específico,
  /// herdando do OTLP global e aplicando overrides do sinal quando informados.
  /// </summary>
  /// <param name="options">Opções globais de observabilidade.</param>
  /// <param name="resourceOtlp">Overrides OTLP específicos do sinal.</param>
  /// <returns>Configuração OTLP efetiva.</returns>
  internal static OtlpOptions ResolveOtlpOptions(ObservabilityOptions options, OtlpOverrideOptions? resourceOtlp)
  {
    // Começa com uma cópia das opções globais para permitir composição segura de overrides
    var resolved = CloneOtlpOptions(options.Otlp);

    // Se não houver overrides específicos para o sinal, usa as opções globais
    if (resourceOtlp is null)
    {
      return resolved;
    }

    // Propriedades não informadas (null) herdam o valor global
    resolved.Enabled = resourceOtlp.Enabled ?? resolved.Enabled;
    resolved.Protocol = resourceOtlp.Protocol ?? resolved.Protocol;
    resolved.ExportProcessorType = resourceOtlp.ExportProcessorType ?? resolved.ExportProcessorType;
    resolved.ServerlessOptimized = resourceOtlp.ServerlessOptimized ?? resolved.ServerlessOptimized;

    // Endpoint aplica override quando não nulo ou vazio
    if (!string.IsNullOrWhiteSpace(resourceOtlp.Endpoint))
    {
      resolved.Endpoint = resourceOtlp.Endpoint;
    }

    // Headers aplica override quando não nulo ou vazio
    if (!string.IsNullOrWhiteSpace(resourceOtlp.Headers))
    {
      resolved.Headers = resourceOtlp.Headers;
    }

    // MaxQueueSize aplicam override quando é um valor inteiro válido
    if (resourceOtlp.Batch.MaxQueueSize is int maxQueueSize)
    {
      resolved.Batch.MaxQueueSize = maxQueueSize;
    }

    // MaxExportBatchSize aplicam override quando é um valor inteiro válido
    if (resourceOtlp.Batch.MaxExportBatchSize is int maxExportBatchSize)
    {
      resolved.Batch.MaxExportBatchSize = maxExportBatchSize;
    }

    // ScheduledDelayMilliseconds aplicam override quando é um valor inteiro válido
    if (resourceOtlp.Batch.ScheduledDelayMilliseconds is int scheduledDelayMilliseconds)
    {
      resolved.Batch.ScheduledDelayMilliseconds = scheduledDelayMilliseconds;
    }

    // ExporterTimeoutMilliseconds aplicam override quando é um valor inteiro válido
    if (resourceOtlp.Batch.ExporterTimeoutMilliseconds is int exporterTimeoutMilliseconds)
    {
      resolved.Batch.ExporterTimeoutMilliseconds = exporterTimeoutMilliseconds;
    }

    return resolved;
  }

  /// <summary>
  /// Cria uma cópia de <see cref="OtlpOptions"/> para permitir composição segura de overrides.
  /// </summary>
  /// <param name="options">Opções de origem.</param>
  /// <returns>Cópia das opções.</returns>
  internal static OtlpOptions CloneOtlpOptions(OtlpOptions options)
  {
    return new OtlpOptions
    {
      Enabled = options.Enabled,
      Protocol = options.Protocol,
      Endpoint = options.Endpoint,
      ExportProcessorType = options.ExportProcessorType,
      ServerlessOptimized = options.ServerlessOptimized,
      Headers = options.Headers,
      // Clone dedicado preserva a distinção entre valores customizados e padrões
      Batch = options.Batch.Clone()
    };
  }

  /// <summary>
  /// Valida e normaliza headers OTLP.
  /// </summary>
  /// <param name="headers">Headers em formato "k1=v1,k2=v2".</param>
  /// <returns>Headers normalizados ou null.</returns>
  /// <exception cref="InternalServerErrorException">Lançada se os headers representarem um risco de segurança.</exception>
  /// <exception cref="InternalServerErrorException">Lançada se os headers forem inválidos.</exception>
  /// <remarks>
  /// Esta função valida os headers para evitar injeção de CRLF e garante que cada
  /// par chave=valor esteja no formato correto. Retorna os headers normalizados ou null
  /// se não houver headers válidos.
  /// </remarks>
  internal static string? ValidateAndNormalizeOtlpHeaders(string? headers)
  {
    // Se nulo ou vazio, retorna null
    if (string.IsNullOrWhiteSpace(headers))
    {
      return null;
    }

    // Bloqueia CRLF para evitar injeção de headers
    if (headers.Contains('\r') || headers.Contains('\n'))
    {
      throw new InternalServerErrorException("Options.Otlp.Headers.SecurityRisk");
    }

    // Separa e valida cada par chave=valor
    var parts = headers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // Lista para armazenar os headers normalizados
    var normalized = new List<string>();

    // Valida cada parte
    foreach (var part in parts)
    {
      // Separa chave e valor
      var param = part.Split('=', StringSplitOptions.TrimEntries);
      if (param.Length != 2 || string.IsNullOrWhiteSpace(param[0]) || string.IsNullOrWhiteSpace(param[1]))
      {
        throw new InternalServerErrorException($"Options.Otlp.Headers.Invalid;{part}");
      }

      // Valida chave
      var key = param[0];
      var value = param[1];
      if (!HeaderKeyRegex().IsMatch(key))
      {
        throw new InternalServerErrorException($"Options.Otlp.Headers.InvalidKey;{key}");
      }

      // Adiciona ao resultado normalizado
      normalized.Add($"{key}={value}");
    }

    return string.Join(',', normalized);
  }

  #endregion

  #region Regex Generated Methods

  /// <summary>
  /// Regex para normalizar chaves de atributos.
  /// </summary>
  /// <returns>Regex compilada.</returns>
  [GeneratedRegex(@"[^a-z0-9._\-]", RegexOptions.None, matchTimeoutMilliseconds: 250)]
  internal static partial Regex AttributesRegex();

  /// <summary>
  /// Regex para validar a chave do header (formato simples e seguro).
  /// </summary>
  /// <returns>Regex compilada.</returns>
  [GeneratedRegex(@"^[a-zA-Z0-9._\-]+$", RegexOptions.None, matchTimeoutMilliseconds: 250)]
  internal static partial Regex HeaderKeyRegex();

  /// <summary>
  /// Regex para substituir espaços por pontos na normalização de chaves.
  /// </summary>
  /// <returns>Regex compilada.</returns>
  [GeneratedRegex(@"\s+")]
  internal static partial Regex SpaceRegex();

  #endregion
}
