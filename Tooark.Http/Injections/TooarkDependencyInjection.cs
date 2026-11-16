using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
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
  ) => AddClient(services, configuration, configure);

  /// <summary>
  /// Adiciona o <see cref="IHttpClientService"/> e o cliente nomeado do Tooark, sem ler a configuração da aplicação.
  /// </summary>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o cliente HTTP adicionado.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkHttpClient(this IServiceCollection services, Action<HttpClientOptions>? configure = null) =>
    AddClient(services, null, configure);

  /// <summary>
  /// Adiciona a guarda de saída a um cliente HTTP nomeado ou tipado da aplicação.
  /// </summary>
  /// <remarks>
  /// Aqui a guarda entra ligada. O handler primário do cliente passa a ser um <see cref="SocketsHttpHandler"/> cujo
  /// <see cref="SocketsHttpHandler.ConnectCallback"/> resolve o nome, recusa os endereços bloqueados e conecta ao
  /// endereço conferido; um handler na requisição confere o IP literal do host. Um <see cref="SocketsHttpHandler"/>
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
  /// <param name="configuration">A configuração da aplicação, quando houver.</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o cliente HTTP adicionado.</returns>
  private static IServiceCollection AddClient(
    IServiceCollection services,
    IConfiguration? configuration,
    Action<HttpClientOptions>? configure
  )
  {
    // Carrega da configuração e aplica os overrides programáticos por cima
    void Apply(HttpClientOptions options)
    {
      if (configuration is not null)
      {
        StrictConfiguration.Bind(configuration, HttpClientOptions.Section, options);
      }

      configure?.Invoke(options);
    }

    // Valida no startup, e não na primeira requisição
    var options = new HttpClientOptions();
    Apply(options);
    options.Validate();

    // Os handlers do cliente nomeado se acumulam a cada registro: um segundo registro empilharia outro handler de
    // resiliência e multiplicaria as novas tentativas
    if (services.Any(descriptor => descriptor.ServiceType == typeof(ClientRegistration)))
    {
      return services;
    }

    services.AddSingleton<ClientRegistration>();
    services.AddOptions<HttpClientOptions>().Configure(Apply);

    options.TryGetBaseAddress(out var baseAddress);

    var builder = services.AddHttpClient(HttpClientService.ClientName, client =>
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
    services.TryAddSingleton<IHttpClientService, HttpClientService>();

    return services;
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
    }

    resilience.Retry.Delay = TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds);

    resilience.CircuitBreaker.FailureRatio = options.CircuitBreakerFailureRatio;
    resilience.CircuitBreaker.MinimumThroughput = options.CircuitBreakerMinimumThroughput;
    resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds);
    resilience.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds);
  }

  /// <summary>
  /// Registra os dois pontos da guarda no cliente: a conferência na conexão e a conferência na requisição.
  /// </summary>
  /// <param name="builder">O construtor do cliente HTTP.</param>
  /// <param name="policy">A regra da guarda.</param>
  /// <returns>O construtor do cliente HTTP com a guarda adicionada.</returns>
  private static IHttpClientBuilder AddOutboundGuard(IHttpClientBuilder builder, OutboundGuardPolicy policy)
  {
    builder.Services.TryAddSingleton<IHostResolver, DnsHostResolver>();

    // O handler primário padrão do .NET 8 é o HttpClientHandler, que não expõe o ConnectCallback; um
    // SocketsHttpHandler já configurado pela aplicação é mantido
    builder.UseSocketsHttpHandler((handler, provider) =>
      handler.ConnectCallback = new OutboundConnector(policy, provider.GetRequiredService<IHostResolver>()).ConnectAsync);

    builder.AddHttpMessageHandler(() => new OutboundGuardHandler(policy));

    return builder;
  }

  /// <summary>
  /// Marca que o cliente nomeado do Tooark já foi registrado na coleção de serviços.
  /// </summary>
  private sealed class ClientRegistration;
}
