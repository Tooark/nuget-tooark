using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
  #region Constants

  /// <summary>
  /// Caminho do sinal de traces no OTLP sobre HTTP.
  /// </summary>
  internal const string OtlpTracesPath = "v1/traces";

  /// <summary>
  /// Caminho do sinal de métricas no OTLP sobre HTTP.
  /// </summary>
  internal const string OtlpMetricsPath = "v1/metrics";

  /// <summary>
  /// Caminho do sinal de logs no OTLP sobre HTTP.
  /// </summary>
  internal const string OtlpLogsPath = "v1/logs";

  /// <summary>
  /// Caminhos dos sinais no OTLP sobre HTTP.
  /// </summary>
  private static readonly string[] OtlpSignalPaths = [OtlpTracesPath, OtlpMetricsPath, OtlpLogsPath];

  #endregion

  /// <summary>
  /// Adiciona os serviços de OpenTelemetry ao container de injeção de dependência.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Resolve o <c>service.name</c> e o <c>service.version</c> (opções, variáveis <c>OTEL_SERVICE_NAME</c> e
  /// <c>OTEL_RESOURCE_ATTRIBUTES</c>, assembly de entrada) e os nomes da fonte de atividades e do medidor, e
  /// grava os valores resolvidos no <c>IOptions&lt;ObservabilityOptions&gt;</c> do container.
  /// </para>
  /// <para>
  /// Registra o <see cref="TooarkInstrumentation"/> como singleton, com a fonte e o medidor já registrados no
  /// OpenTelemetry, mesmo quando a observabilidade está desabilitada, para a injeção nunca falhar.
  /// </para>
  /// </remarks>
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
    StrictConfiguration.Bind(configuration, ObservabilityOptions.Section, options);

    // Aplica configurações programáticas
    configure?.Invoke(options);

    // Resolve a identidade do serviço e os nomes da fonte e do medidor uma única vez, antes de olhar o Enabled:
    // a instrumentação e o IOptions recebem os valores resolvidos mesmo com a observabilidade desligada
    var identity = ResolveServiceIdentity(options, Environment.GetEnvironmentVariable, Assembly.GetEntryAssembly());
    var activitySourceName = ResolveInstrumentationName(options.Tracing.ActivitySourceName, identity);
    var meterName = ResolveInstrumentationName(options.Metrics.MeterName, identity);

    // As opções locais passam a carregar os nomes resolvidos: o tracing e as métricas registram esses nomes
    options.Tracing.ActivitySourceName = activitySourceName;
    options.Metrics.MeterName = meterName;

    // Grava os valores resolvidos no IOptions do container, depois dos overrides programáticos: quem lê
    // options.Value.ServiceName recebe o nome inferido, e não null
    services.PostConfigure<ObservabilityOptions>(registered =>
    {
      if (string.IsNullOrWhiteSpace(registered.ServiceName))
      {
        registered.ServiceName = identity.Name;
      }

      if (string.IsNullOrWhiteSpace(registered.ServiceVersion))
      {
        registered.ServiceVersion = identity.Version;
      }

      registered.Tracing.ActivitySourceName = activitySourceName;
      registered.Metrics.MeterName = meterName;
    });

    // Fonte e medidor prontos para a aplicação, descartados junto com o container
    services.TryAddSingleton(_ => new TooarkInstrumentation(activitySourceName, meterName, identity.Name, identity.Version));

    // Se desabilitado, não registra nada
    if (!options.Enabled)
    {
      return services;
    }

    // Detecta o ambiente
    var isDevelopment = IsDevelopmentEnvironment(configuration);

    // Configura o resource (identificação do serviço)
    var resourceBuilder = ConfigureResource(configuration, options, identity);

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
  internal static string NormalizeAttributeKey(string? key)
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
  /// Resolve a identidade do serviço (<c>service.name</c> e <c>service.version</c>) a partir das opções, das
  /// variáveis de ambiente do OpenTelemetry e do assembly de entrada.
  /// </summary>
  /// <remarks>
  /// Nome: <c>ServiceName</c>, <c>OTEL_SERVICE_NAME</c>, nome do assembly de entrada, <c>unknown_service</c>.
  /// Versão: <c>ServiceVersion</c>, <c>service.version</c> em <c>OTEL_RESOURCE_ATTRIBUTES</c>,
  /// <c>AssemblyInformationalVersion</c> sem o sufixo <c>+commit</c>, <c>AssemblyVersion</c>,
  /// <c>unknown_version</c>. Valores vazios ou só com espaços passam para a próxima fonte.
  /// </remarks>
  /// <param name="options">Opções de Observability.</param>
  /// <param name="getEnvironmentVariable">Leitura das variáveis de ambiente, substituível nos testes.</param>
  /// <param name="entryAssembly">Assembly de entrada da aplicação, quando houver.</param>
  /// <returns>A identidade resolvida.</returns>
  internal static ServiceIdentity ResolveServiceIdentity(
    ObservabilityOptions options,
    Func<string, string?> getEnvironmentVariable,
    Assembly? entryAssembly
  )
  {
    // O nome vem da primeira fonte real preenchida; só sem nenhuma delas cai no fallback da especificação
    var serviceName = FirstNonBlank(
      options.ServiceName,
      getEnvironmentVariable("OTEL_SERVICE_NAME"),
      entryAssembly?.GetName().Name
    );

    // A versão informacional carrega o <Version> do projeto (2.3.0, 2.3.0-beta.1); a AssemblyVersion tem quatro partes
    var serviceVersion = FirstNonBlank(
      options.ServiceVersion,
      ResolveVersionFromResourceAttributes(getEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES")),
      ResolveInformationalVersion(entryAssembly),
      entryAssembly?.GetName().Version?.ToString()
    );

    return new ServiceIdentity(
      serviceName ?? "unknown_service",
      serviceName is not null,
      serviceVersion ?? "unknown_version"
    );
  }

  /// <summary>
  /// Lê a chave <c>service.version</c> do valor de <c>OTEL_RESOURCE_ATTRIBUTES</c> (<c>chave=valor,chave=valor</c>).
  /// </summary>
  /// <param name="resourceAttributes">Valor da variável de ambiente, quando houver.</param>
  /// <returns>A versão, ou null quando a chave não está presente ou está vazia.</returns>
  internal static string? ResolveVersionFromResourceAttributes(string? resourceAttributes)
  {
    // Sem a variável não há o que ler
    if (string.IsNullOrWhiteSpace(resourceAttributes))
    {
      return null;
    }

    // Cada par é chave=valor; o valor pode conter '=' e vir codificado como na especificação (W3C Baggage)
    foreach (var pair in resourceAttributes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      var separator = pair.IndexOf('=');

      if (separator <= 0)
      {
        continue;
      }

      // As chaves de atributo do OpenTelemetry diferenciam maiúsculas, como no detector do próprio SDK
      var key = pair[..separator].Trim();

      if (!key.Equals("service.version", StringComparison.Ordinal))
      {
        continue;
      }

      var value = Uri.UnescapeDataString(pair[(separator + 1)..].Trim());

      return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    return null;
  }

  /// <summary>
  /// Lê a <c>AssemblyInformationalVersion</c> do assembly, descartando o sufixo <c>+commit</c> que o SDK anexa.
  /// </summary>
  /// <param name="assembly">Assembly a consultar, quando houver.</param>
  /// <returns>A versão informacional sem o sufixo, ou null quando ausente ou vazia.</returns>
  internal static string? ResolveInformationalVersion(Assembly? assembly)
  {
    var informational = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    if (string.IsNullOrWhiteSpace(informational))
    {
      return null;
    }

    // A partir do .NET 8 o SDK anexa o commit (2.3.0+a1b2c3d); o trecho depois do '+' não é parte da versão
    var plus = informational.IndexOf('+');
    var version = (plus >= 0 ? informational[..plus] : informational).Trim();

    return version.Length == 0 ? null : version;
  }

  /// <summary>
  /// Resolve o nome da fonte de atividades ou do medidor principal.
  /// </summary>
  /// <remarks>
  /// O valor informado vence quando difere do padrão <see cref="TooarkInstrumentation.DefaultName"/>. Sem valor
  /// informado, usa o <c>service.name</c> quando ele veio de uma fonte real; com o nome desconhecido, fica no
  /// padrão, para nunca existir uma fonte chamada <c>unknown_service</c>.
  /// </remarks>
  /// <param name="configured">Nome configurado nas opções.</param>
  /// <param name="identity">Identidade do serviço resolvida.</param>
  /// <returns>O nome resolvido.</returns>
  internal static string ResolveInstrumentationName(string? configured, ServiceIdentity identity)
  {
    var informed = configured?.Trim();

    // Um nome diferente do padrão foi informado de propósito
    if (!string.IsNullOrEmpty(informed) && !informed.Equals(TooarkInstrumentation.DefaultName, StringComparison.Ordinal))
    {
      return informed;
    }

    return identity.IsNameKnown ? identity.Name : TooarkInstrumentation.DefaultName;
  }

  /// <summary>
  /// Devolve o primeiro valor que não é nulo, vazio ou só espaços, já sem os espaços das pontas.
  /// </summary>
  /// <param name="candidates">Valores na ordem de prioridade.</param>
  /// <returns>O primeiro valor preenchido, ou null quando nenhum está.</returns>
  private static string? FirstNonBlank(params string?[] candidates)
  {
    foreach (var candidate in candidates)
    {
      if (!string.IsNullOrWhiteSpace(candidate))
      {
        return candidate.Trim();
      }
    }

    return null;
  }

  /// <summary>
  /// Configura o ResourceBuilder com informações do serviço, resolvendo a identidade a partir das variáveis
  /// de ambiente do processo e do assembly de entrada.
  /// </summary>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="options">Opções de Observability.</param>
  /// <returns>ResourceBuilder configurado.</returns>
  internal static ResourceBuilder ConfigureResource(IConfiguration configuration, ObservabilityOptions options) =>
    ConfigureResource(
      configuration,
      options,
      ResolveServiceIdentity(options, Environment.GetEnvironmentVariable, Assembly.GetEntryAssembly())
    );

  /// <summary>
  /// Configura o ResourceBuilder com informações do serviço.
  /// </summary>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="options">Opções de Observability.</param>
  /// <param name="identity">Identidade do serviço já resolvida.</param>
  /// <returns>ResourceBuilder configurado.</returns>
  internal static ResourceBuilder ConfigureResource(IConfiguration configuration, ObservabilityOptions options, ServiceIdentity identity)
  {
    // Obtém a instância e o ambiente do serviço
    var serviceInstanceId = options.ServiceInstanceId
      ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_INSTANCE_ID")
      ?? Guid.NewGuid().ToString("N");
    var serviceEnvironment = GetEnvironment(configuration);

    // Cria o ResourceBuilder. O CreateDefault lê OTEL_SERVICE_NAME e OTEL_RESOURCE_ATTRIBUTES; como o AddService
    // vence na mesclagem, a identidade resolvida já considera as duas variáveis para os valores coincidirem
    var resourceBuilder = ResourceBuilder
      .CreateDefault()
      .AddService(
        serviceName: identity.Name,
        serviceVersion: identity.Version,
        serviceInstanceId: serviceInstanceId
      );

    // Atributos úteis e genéricos (não dependem do tipo de aplicação)
    // deployment.environment.name é a convenção semântica atual; deployment.environment é mantida para backends que ainda usam a chave antiga
    // process.pid é numérico na convenção semântica (o SDK guarda inteiros como long)
    resourceBuilder.AddAttributes(new Dictionary<string, object>
    {
      ["deployment.environment"] = serviceEnvironment,
      ["deployment.environment.name"] = serviceEnvironment,
      ["host.name"] = Environment.MachineName,
      ["process.pid"] = (long)Environment.ProcessId,
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

    // Callback da aplicação por último, como os demais callbacks do pacote: detectores de nuvem e de contêiner
    options.ConfigureResource?.Invoke(resourceBuilder);

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
  /// Resolve o endereço para onde um sinal é exportado.
  /// </summary>
  /// <remarks>
  /// <para>
  /// No gRPC o endereço é o do coletor, sem caminho. No HTTP cada sinal tem o seu caminho
  /// (<c>/v1/traces</c>, <c>/v1/metrics</c> e <c>/v1/logs</c>), e o SDK do OpenTelemetry não o acrescenta quando
  /// o endereço é definido por código: um <c>http://coletor:4318</c> faria o <c>POST</c> na raiz, que o coletor
  /// recusa. Por isso o caminho do sinal é acrescentado aqui.
  /// </para>
  /// <para>
  /// Um endereço que já termina no caminho de um sinal é usado como está: é o de quem já informava o endereço
  /// completo, e ele não muda.
  /// </para>
  /// </remarks>
  /// <param name="options">Opções de OTLP já resolvidas para o sinal.</param>
  /// <param name="signalPath">Caminho do sinal: <see cref="OtlpTracesPath"/>, <see cref="OtlpMetricsPath"/> ou <see cref="OtlpLogsPath"/>.</param>
  /// <returns>O endereço efetivo da exportação.</returns>
  /// <exception cref="InternalServerErrorException">Quando o endpoint é nulo ou não é uma URI absoluta válida.</exception>
  internal static Uri ResolveOtlpEndpoint(OtlpOptions options, string signalPath)
  {
    // Valida o endpoint
    ValidateOtlpEndpoint(options);

    // Não-nulo garantido por ValidateOtlpEndpoint
    var endpoint = new Uri(options.Endpoint!);

    if (options.Protocol.ToProtocol() != OtlpExportProtocol.HttpProtobuf)
    {
      return endpoint;
    }

    var path = endpoint.AbsolutePath.TrimEnd('/');

    // Quem já informa o endereço completo de um sinal continua com ele
    if (OtlpSignalPaths.Any(known => path.EndsWith($"/{known}", StringComparison.OrdinalIgnoreCase)))
    {
      return endpoint;
    }

    // O UriBuilder preserva a query string e a porta do endereço informado
    return new UriBuilder(endpoint) { Path = $"{path}/{signalPath}" }.Uri;
  }

  /// <summary>
  /// Configura as opções do exportador OTLP.
  /// </summary>
  /// <param name="otlpOptions">OtlpExporterOptions a ser configurado.</param>
  /// <param name="options">Opções de OTLP.</param>
  /// <param name="signalPath">Caminho do sinal no protocolo HTTP: <see cref="OtlpTracesPath"/>, <see cref="OtlpMetricsPath"/> ou <see cref="OtlpLogsPath"/>.</param>
  internal static void ConfigureOtlpExporter(OtlpExporterOptions otlpOptions, OtlpOptions options, string signalPath)
  {
    // Configura o endpoint e protocolo. O endereço efetivo fica só no exportador: gravá-lo de volta nas opções
    // levaria o caminho de um sinal para o seguinte
    otlpOptions.Endpoint = ResolveOtlpEndpoint(options, signalPath);
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
