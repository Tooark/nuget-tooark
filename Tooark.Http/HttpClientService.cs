using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Http.Interfaces;
using Tooark.Http.Internal;
using Tooark.Http.Options;

namespace Tooark.Http;

/// <summary>
/// Serviço de chamadas HTTP de saída com JSON, sobre o <see cref="IHttpClientFactory"/>.
/// </summary>
/// <remarks>
/// É uma camada fina: o cliente nomeado <see cref="ClientName"/> é pedido à fábrica a cada chamada, e a resiliência
/// e a guarda de saída ficam no pipeline de handlers dele, e não aqui. O serviço cuida do JSON
/// (<see cref="JsonSerializerDefaults.Web"/>), do tempo limite da chamada inteira e da conversão das falhas nas
/// exceções do Tooark. Não guarda estado por chamada e é seguro para uso concorrente.
/// </remarks>
public sealed class HttpClientService : IHttpClientService
{
  #region Constants

  /// <summary>
  /// Nome do cliente HTTP do Tooark no <see cref="IHttpClientFactory"/>.
  /// </summary>
  public const string ClientName = "Tooark";

  #endregion

  #region Private Fields

  /// <summary>
  /// Opções de JSON das requisições e das respostas: nomes em camelCase e leitura sem diferenciar maiúsculas.
  /// </summary>
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  /// <summary>
  /// Fábrica de clientes HTTP.
  /// </summary>
  private readonly IHttpClientFactory _httpClientFactory;

  /// <summary>
  /// Tempo limite da chamada inteira.
  /// </summary>
  private readonly TimeSpan _totalTimeout;

  #endregion

  #region Constructor

  /// <summary>
  /// Cria o serviço a partir da fábrica de clientes e das opções do cliente HTTP.
  /// </summary>
  /// <param name="httpClientFactory">Fábrica de clientes HTTP.</param>
  /// <param name="options">Opções do cliente HTTP.</param>
  /// <exception cref="InternalServerErrorException">Quando as opções são inválidas.</exception>
  public HttpClientService(IHttpClientFactory httpClientFactory, IOptions<HttpClientOptions> options)
  {
    ArgumentNullException.ThrowIfNull(httpClientFactory);
    ArgumentNullException.ThrowIfNull(options);

    // Pelo registro as opções já chegam validadas; a conferência vale para quem cria o serviço à mão
    options.Value.Validate();

    _httpClientFactory = httpClientFactory;
    _totalTimeout = TimeSpan.FromSeconds(options.Value.TotalTimeoutSeconds);
  }

  #endregion

  #region Methods

  /// <inheritdoc />
  public Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default) =>
    SendAsync<TResponse>(HttpMethod.Get, requestUri, null, readBody: true, cancellationToken);

  /// <inheritdoc />
  public Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
    SendAsync<TResponse>(HttpMethod.Post, requestUri, Serialize(body), readBody: true, cancellationToken);

  /// <inheritdoc />
  public Task PostAsync<TRequest>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
    SendAsync<object>(HttpMethod.Post, requestUri, Serialize(body), readBody: false, cancellationToken);

  /// <inheritdoc />
  public Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
    SendAsync<TResponse>(HttpMethod.Put, requestUri, Serialize(body), readBody: true, cancellationToken);

  /// <inheritdoc />
  public Task PutAsync<TRequest>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
    SendAsync<object>(HttpMethod.Put, requestUri, Serialize(body), readBody: false, cancellationToken);

  /// <inheritdoc />
  public Task<TResponse?> PatchAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
    SendAsync<TResponse>(HttpMethod.Patch, requestUri, Serialize(body), readBody: true, cancellationToken);

  /// <inheritdoc />
  public Task PatchAsync<TRequest>(string requestUri, TRequest body, CancellationToken cancellationToken = default) =>
    SendAsync<object>(HttpMethod.Patch, requestUri, Serialize(body), readBody: false, cancellationToken);

  /// <inheritdoc />
  public Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default) =>
    SendAsync<object>(HttpMethod.Delete, requestUri, null, readBody: false, cancellationToken);

  /// <inheritdoc />
  public Task<TResponse?> DeleteAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default) =>
    SendAsync<TResponse>(HttpMethod.Delete, requestUri, null, readBody: true, cancellationToken);

  /// <inheritdoc />
  public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    using var timeout = CreateTimeout(cancellationToken);

    try
    {
      var client = CreateClient(request.RequestUri);

      // O corpo é carregado dentro do tempo limite: quem recebe a resposta não depende mais da conexão
      return await client
        .SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token)
        .ConfigureAwait(false);
    }
    catch (Exception exception) when (HttpErrorMapper.Map(exception, cancellationToken) is { } mapped)
    {
      throw mapped;
    }
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Envia a requisição, converte a resposta de erro em exceção e lê o corpo JSON da resposta de sucesso.
  /// </summary>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="method">O verbo HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="content">O corpo da requisição, quando houver.</param>
  /// <param name="readBody">Indica se o corpo da resposta de sucesso é lido.</param>
  /// <param name="cancellationToken">Token de cancelamento de quem chamou.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando não é lido, a resposta é 204 ou não tem corpo.</returns>
  private async Task<TResponse?> SendAsync<TResponse>(
    HttpMethod method,
    string requestUri,
    HttpContent? content,
    bool readBody,
    CancellationToken cancellationToken
  )
  {
    using var request = new HttpRequestMessage(method, ParseRequestUri(requestUri)) { Content = content };
    using var timeout = CreateTimeout(cancellationToken);

    try
    {
      var client = CreateClient(request.RequestUri);

      // Só os cabeçalhos são aguardados aqui: o corpo é lido abaixo, o de erro com limite de tamanho, e os dois
      // dentro do tempo limite da chamada, que os tempos limite do handler de resiliência não cobrem
      using var response = await client
        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
        .ConfigureAwait(false);

      if (!response.IsSuccessStatusCode)
      {
        throw await HttpErrorMapper.FromResponseAsync(response, timeout.Token).ConfigureAwait(false);
      }

      if (!readBody || response.StatusCode == HttpStatusCode.NoContent)
      {
        return default;
      }

      return await ReadAsync<TResponse>(response, timeout.Token).ConfigureAwait(false);
    }
    catch (Exception exception) when (HttpErrorMapper.Map(exception, cancellationToken) is { } mapped)
    {
      throw mapped;
    }
  }

  /// <summary>
  /// Cria o cliente nomeado do Tooark e confere se a URI da requisição pode ser enviada por ele.
  /// </summary>
  /// <param name="requestUri">A URI da requisição.</param>
  /// <returns>O cliente HTTP.</returns>
  /// <exception cref="InternalServerErrorException">Quando a URI é relativa e não há endereço base.</exception>
  /// <exception cref="BadRequestException">Quando a URI absoluta não é <c>http</c> nem <c>https</c>.</exception>
  private HttpClient CreateClient(Uri? requestUri)
  {
    var client = _httpClientFactory.CreateClient(ClientName);

    if (requestUri is null || !requestUri.IsAbsoluteUri)
    {
      // Erro de configuração, e não de quem chamou a aplicação: a URI relativa foi escrita no código
      return client.BaseAddress is null
        ? throw new InternalServerErrorException("Http.BaseAddressNotConfigured")
        : client;
    }

    // A URI absoluta pode ter vindo do usuário (um webhook, por exemplo)
    if (requestUri.Scheme != Uri.UriSchemeHttp && requestUri.Scheme != Uri.UriSchemeHttps)
    {
      throw new BadRequestException("Http.RequestUriInvalid");
    }

    return client;
  }

  /// <summary>
  /// Cria o token do tempo limite da chamada inteira, ligado ao token de quem chamou.
  /// </summary>
  /// <param name="cancellationToken">Token de cancelamento de quem chamou.</param>
  /// <returns>A fonte do token, que cancela no tempo limite ou quando quem chamou cancela.</returns>
  private CancellationTokenSource CreateTimeout(CancellationToken cancellationToken)
  {
    var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    timeout.CancelAfter(_totalTimeout);

    return timeout;
  }

  /// <summary>
  /// Interpreta a URI da requisição.
  /// </summary>
  /// <param name="requestUri">O texto da URI.</param>
  /// <returns>A URI, ou nulo quando o texto é vazio e a requisição vai para o endereço base.</returns>
  /// <exception cref="BadRequestException">Quando o texto não é uma URI.</exception>
  private static Uri? ParseRequestUri(string? requestUri)
  {
    if (string.IsNullOrEmpty(requestUri))
    {
      return null;
    }

    return Uri.TryCreate(requestUri, UriKind.RelativeOrAbsolute, out var uri)
      ? uri
      : throw new BadRequestException("Http.RequestUriInvalid");
  }

  /// <summary>
  /// Serializa o corpo da requisição em JSON.
  /// </summary>
  /// <remarks>
  /// O corpo é serializado antes do envio, para bytes: a requisição sai com <c>Content-Length</c> e pode ser
  /// reenviada pelas novas tentativas sem serializar de novo.
  /// </remarks>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <param name="body">Corpo da requisição.</param>
  /// <returns>O conteúdo da requisição.</returns>
  private static ByteArrayContent Serialize<TRequest>(TRequest body)
  {
    var content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions));

    content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

    return content;
  }

  /// <summary>
  /// Lê o corpo JSON de uma resposta de sucesso.
  /// </summary>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="response">A resposta de sucesso.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando não há corpo.</returns>
  /// <exception cref="JsonException">Quando o corpo não é um JSON válido para o tipo.</exception>
  private static async Task<TResponse?> ReadAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    var json = HttpErrorMapper.SkipByteOrderMark(payload);

    if (json.IsEmpty)
    {
      return default;
    }

    // Um corpo que não é o JSON esperado lança JsonException, que o HttpErrorMapper converte
    return JsonSerializer.Deserialize<TResponse>(json.Span, JsonOptions);
  }

  #endregion
}
