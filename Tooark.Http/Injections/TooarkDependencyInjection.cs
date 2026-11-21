using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Tooark.Http.Interfaces;
using Tooark.Http.Internal;
using Tooark.Http.Options;

namespace Tooark.Http.Injections;

/// <summary>
/// Classe para adicionar o cliente HTTP do Tooark e a guarda de saída ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o <see cref="IHttpClientService"/> e o cliente nomeado do Tooark, com as opções lidas da seção
  /// <c>HttpClient</c>.
  /// </summary>
  /// <remarks>
  /// A seção é opcional. O cliente é registrado no <see cref="IHttpClientFactory"/> com o endereço base, o handler
  /// padrão de resiliência do <c>Microsoft.Extensions.Http.Resilience</c> configurado pelas opções e, quando ligada,
  /// a guarda de saída. As opções são validadas aqui, no registro, para que uma configuração inválida falhe no
  /// startup e não na primeira requisição. Só o primeiro registro vale: chamar de novo não empilha outro handler de
  /// resiliência.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o cliente HTTP adicionado.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkHttpClient(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<HttpClientOptions>? configure = null
  ) => AddClient(services, null, configuration, configure);

  /// <summary>
  /// Adiciona o <see cref="IHttpClientService"/> e o cliente nomeado do Tooark, sem ler a configuração da aplicação.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o cliente HTTP adicionado.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkHttpClient(this IServiceCollection services, Action<HttpClientOptions>? configure = null) =>
    AddClient(services, null, null, configure);

  /// <summary>
  /// Adiciona um cliente HTTP com nome, com as próprias opções lidas da seção <c>HttpClients:{nome}</c>, e o
  /// <see cref="IHttpClientService"/> dele como serviço com chave.
  /// </summary>
  /// <remarks>
  /// É o registro para quem chama mais de um destino com necessidades diferentes: cada nome tem o próprio endereço
  /// base, os próprios tempos limite, as próprias novas tentativas, o próprio circuito e a própria guarda de saída.
  /// O cliente padrão (<see cref="AddTooarkHttpClient(IServiceCollection, IConfiguration, Action{HttpClientOptions}?)"/>)
  /// não é alterado, e não precisa estar registrado. Resolva o serviço pela chave:
  /// <c>[FromKeyedServices("nome")] IHttpClientService http</c>, ou
  /// <c>provider.GetRequiredKeyedService&lt;IHttpClientService&gt;("nome")</c>.
  /// <para>
  /// O nome é também o do cliente no <see cref="IHttpClientFactory"/>: o que mais ele precisar (um cabeçalho fixo,
  /// um handler de autenticação) entra por <c>AddHttpClient("nome")</c>, depois deste registro. Por isso o nome não
  /// deve ser o de um cliente que a aplicação já registra com outra finalidade. Só o primeiro registro de cada nome
  /// vale: chamar de novo não empilha outro handler de resiliência.
  /// </para>
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="name">Nome do cliente, que não pode ser o do cliente padrão (<see cref="HttpClientService.ClientName"/>).</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o cliente HTTP adicionado.</returns>
  /// <exception cref="ArgumentException">Quando o nome é vazio, só espaços, tem <c>:</c> ou é o do cliente padrão.</exception>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkNamedHttpClient(
    this IServiceCollection services,
    string name,
    IConfiguration configuration,
    Action<HttpClientOptions>? configure = null
  ) => AddClient(services, ValidateName(name), configuration, configure);

  /// <summary>
  /// Adiciona um cliente HTTP com nome e o <see cref="IHttpClientService"/> dele como serviço com chave, sem ler a
  /// configuração da aplicação.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="name">Nome do cliente, que não pode ser o do cliente padrão (<see cref="HttpClientService.ClientName"/>).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o cliente HTTP adicionado.</returns>
  /// <exception cref="ArgumentException">Quando o nome é vazio, só espaços, tem <c>:</c> ou é o do cliente padrão.</exception>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkNamedHttpClient(
    this IServiceCollection services,
    string name,
    Action<HttpClientOptions>? configure = null
  ) => AddClient(services, ValidateName(name), null, configure);

  /// <summary>
  /// Adiciona a guarda de saída a um cliente HTTP nomeado ou tipado da aplicação.
  /// </summary>
  /// <remarks>
  /// Aqui a guarda entra ligada. O handler primário do cliente passa a ser um <see cref="SocketsHttpHandler"/> cujo
  /// <see cref="SocketsHttpHandler.ConnectCallback"/> resolve o nome, recusa os endereços bloqueados e conecta ao
  /// endereço conferido; um handler na requisição confere o IP literal do host e, quando a conexão não vai
  /// enxergar o destino (um proxy de saída, ou um handler primário que não é o da guarda), o próprio nome, resolvido
  /// pelo DNS da aplicação. Um <see cref="SocketsHttpHandler"/>
  /// já configurado é mantido, com o <c>ConnectCallback</c> trocado; um handler primário de outro tipo, como o
  /// <see cref="HttpClientHandler"/>, é substituído, e a configuração dele se perde. Chame este método <b>depois</b> do
  /// handler de resiliência e de qualquer configuração do handler primário: assim a recusa não é repetida nem
  /// contada no circuito, e nada substitui o handler da guarda.
  /// </remarks>
  /// <param name="builder">O construtor do cliente HTTP.</param>
  /// <param name="configure">Ação opcional para liberar hosts e redes.</param>
  /// <returns>O construtor do cliente HTTP com a guarda adicionada.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando um host ou uma rede liberada é inválido.</exception>
  public static IHttpClientBuilder AddTooarkOutboundGuard(this IHttpClientBuilder builder, Action<OutboundGuardOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(builder);

    var options = new OutboundGuardOptions { Enabled = true };
    configure?.Invoke(options);

    // As listas são interpretadas aqui, uma vez, e uma entrada inválida falha no startup
    var policy = OutboundGuardPolicy.Create(options);

    // Desligar explicitamente continua possível, para a configuração de um ambiente sem a guarda
    return options.Enabled ? AddOutboundGuard(builder, policy) : builder;
  }

  /// <summary>
  /// Valida as opções e registra o cliente nomeado e o serviço.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="name">O nome do cliente, ou nulo para o cliente padrão.</param>
  /// <param name="configuration">A configuração da aplicação, quando houver.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o cliente HTTP adicionado.</returns>
  private static IServiceCollection AddClient(
    IServiceCollection services,
    string? name,
    IConfiguration? configuration,
    Action<HttpClientOptions>? configure
  )
  {
    ArgumentNullException.ThrowIfNull(services);

    // O cliente padrão lê a seção HttpClient; um cliente com nome, a própria seção em HttpClients
    var section = name is null ? HttpClientOptions.Section : $"{HttpClientOptions.NamedSection}:{name}";
    var clientName = name ?? HttpClientService.ClientName;

    // Carrega da configuração e aplica os overrides programáticos por cima
    void Apply(HttpClientOptions options)
    {
      if (configuration is not null)
      {
        StrictConfiguration.Bind(configuration, section, options);
      }

      configure?.Invoke(options);
    }

    // Valida no startup, e não na primeira requisição
    var options = new HttpClientOptions();
    Apply(options);
    options.Validate();

    // Os handlers do cliente nomeado se acumulam a cada registro: um segundo registro empilharia outro handler de
    // resiliência e multiplicaria as novas tentativas. O marcador do cliente padrão não tem chave; o de um cliente
    // com nome tem o nome como chave
    if (services.Any(descriptor => descriptor.ServiceType == typeof(ClientRegistration) && Equals(descriptor.ServiceKey, name)))
    {
      return services;
    }

    if (name is null)
    {
      services.AddSingleton<ClientRegistration>();
      services.AddOptions<HttpClientOptions>().Configure(Apply);
    }
    else
    {
      services.AddKeyedSingleton<ClientRegistration>(name);
      services.AddOptions<HttpClientOptions>(name).Configure(Apply);
    }

    options.TryGetBaseAddress(out var baseAddress);

    var builder = services.AddHttpClient(clientName, client =>
    {
      client.BaseAddress = baseAddress;

      // Com a resiliência, os tempos limite são os do pipeline e o do serviço, que cobre a chamada inteira; sem
      // ela, o cliente aplica o total também a quem o usa direto pela fábrica
      client.Timeout = options.Resilience
        ? Timeout.InfiniteTimeSpan
        : TimeSpan.FromSeconds(options.TotalTimeoutSeconds);
    });

    if (options.Resilience)
    {
      builder.AddStandardResilienceHandler(resilience => Configure(resilience, options));
    }

    // Depois da resiliência, para ficar mais perto da conexão: a recusa da guarda chega ao handler de resiliência
    // como a exceção do Tooark, que ele não repete nem conta no circuito
    if (options.OutboundGuard.Enabled)
    {
      AddOutboundGuard(builder, OutboundGuardPolicy.Create(options.OutboundGuard));
    }

    // O serviço não guarda estado por chamada e pede o cliente à fábrica a cada uso
    if (name is null)
    {
      services.TryAddSingleton<IHttpClientService, HttpClientService>();
    }
    else
    {
      services.TryAddKeyedSingleton<IHttpClientService>(name, (provider, _) => new HttpClientService(
        provider.GetRequiredService<IHttpClientFactory>(),
        provider.GetRequiredService<IOptionsMonitor<HttpClientOptions>>().Get(name),
        name));
    }

    return services;
  }

  /// <summary>
  /// Confere o nome de um cliente com nome.
  /// </summary>
  /// <param name="name">O nome informado.</param>
  /// <returns>O nome, sem alteração.</returns>
  /// <exception cref="ArgumentException">Quando o nome é vazio, só espaços, tem <c>:</c> ou é o do cliente padrão.</exception>
  private static string ValidateName(string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);

    // O nome vira um segmento da seção de configuração, onde os dois-pontos separam os níveis
    if (name.Contains(':'))
    {
      throw new ArgumentException("The client name cannot contain ':'.", nameof(name));
    }

    // O cliente padrão tem o próprio registro e a própria seção
    if (string.Equals(name, HttpClientService.ClientName, StringComparison.Ordinal))
    {
      throw new ArgumentException(
        $"'{HttpClientService.ClientName}' is the default client: register it with AddTooarkHttpClient.",
        nameof(name));
    }

    return name;
  }

  /// <summary>
  /// Passa as opções do Tooark para o handler padrão de resiliência.
  /// </summary>
  /// <param name="resilience">As opções do handler padrão.</param>
  /// <param name="options">As opções já validadas.</param>
  private static void Configure(HttpStandardResilienceOptions resilience, HttpClientOptions options)
  {
    resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds);
    resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds);

    if (options.RetryCount == 0)
    {
      // O Polly não aceita zero tentativas: a estratégia fica no pipeline sem tratar resultado algum
      resilience.Retry.ShouldHandle = static _ => PredicateResult.False();
    }
    else
    {
      resilience.Retry.MaxRetryAttempts = options.RetryCount;

      if (!options.RetryNonIdempotentMethods)
      {
        // O verbo sai do contexto, e não da resposta: uma falha de conexão ou um tempo limite não tem resposta.
        // O resultado segue contando no circuito, que fica antes desta decisão
        var shouldHandle = resilience.Retry.ShouldHandle;

        resilience.Retry.ShouldHandle = arguments =>
          IsNonIdempotent(arguments.Context.GetRequestMessage()?.Method ?? arguments.Outcome.Result?.RequestMessage?.Method)
            ? PredicateResult.False()
            : shouldHandle(arguments);
      }
    }

    resilience.Retry.Delay = TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds);

    resilience.CircuitBreaker.FailureRatio = options.CircuitBreakerFailureRatio;
    resilience.CircuitBreaker.MinimumThroughput = options.CircuitBreakerMinimumThroughput;
    resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds);
    resilience.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds);
  }

  /// <summary>
  /// Indica se o verbo não é idempotente: repetir a requisição pode repetir o efeito dela no serviço chamado.
  /// </summary>
  /// <param name="method">O verbo da requisição, quando conhecido.</param>
  /// <returns>Verdadeiro para POST e PATCH.</returns>
  private static bool IsNonIdempotent(HttpMethod? method) => method == HttpMethod.Post || method == HttpMethod.Patch;

  /// <summary>
  /// Registra os dois pontos da guarda no cliente: a conferência na conexão e a conferência na requisição.
  /// </summary>
  /// <param name="builder">O construtor do cliente HTTP.</param>
  /// <param name="policy">A regra da guarda.</param>
  /// <returns>O construtor do cliente HTTP com a guarda adicionada.</returns>
  private static IHttpClientBuilder AddOutboundGuard(IHttpClientBuilder builder, OutboundGuardPolicy policy)
  {
    builder.Services.TryAddSingleton<IHostResolver, DnsHostResolver>();

    // O handler primário pode ser um HttpClientHandler, que não expõe o ConnectCallback; um
    // SocketsHttpHandler já configurado pela aplicação é mantido
    builder.UseSocketsHttpHandler((handler, provider) =>
      handler.ConnectCallback = new OutboundConnector(policy, provider.GetRequiredService<IHostResolver>()).ConnectAsync);

    // O handler da requisição usa a mesma conferência do nome quando a conexão não vai enxergar o destino
    builder.AddHttpMessageHandler(provider =>
      new OutboundGuardHandler(policy, new OutboundConnector(policy, provider.GetRequiredService<IHostResolver>())));

    return builder;
  }

  /// <summary>
  /// Marca que um cliente do Tooark já foi registrado na coleção de serviços: sem chave para o cliente padrão, com
  /// o nome como chave para um cliente com nome.
  /// </summary>
  private sealed class ClientRegistration;
}
