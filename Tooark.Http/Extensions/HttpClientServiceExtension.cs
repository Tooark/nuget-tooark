using Tooark.Http.Exceptions;
using Tooark.Http.Interfaces;
using Tooark.Http.Internal;

namespace Tooark.Http.Extensions;

/// <summary>
/// Métodos por verbo do <see cref="IHttpClientService"/> com ajuste da requisição por chamada.
/// </summary>
/// <remarks>
/// Cada método faz o mesmo que o método de mesmo nome da interface (JSON na ida e na volta, status de erro
/// convertido em <see cref="HttpRequestFailedException"/>, falhas de transporte nas exceções do Tooark) e recebe uma
/// ação que ajusta a requisição antes do envio: um cabeçalho de autorização que muda a cada chamada, uma chave de
/// idempotência, um <c>Accept</c> diferente. A ação roda uma vez, antes da primeira tentativa, e as novas tentativas
/// reenviam a mesma requisição. Para um cabeçalho igual em todas as chamadas, prefira configurar o cliente
/// (<c>AddHttpClient(nome).ConfigureHttpClient(...)</c>).
/// </remarks>
public static class HttpClientServiceExtension
{
  #region Methods

  /// <summary>
  /// Envia um GET com a requisição ajustada por quem chama e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio, como a inclusão de cabeçalhos.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  public static Task<TResponse?> GetAsync<TResponse>(
    this IHttpClientService http,
    string requestUri,
    Action<HttpRequestMessage> configure,
    CancellationToken cancellationToken = default
  ) => SendAsync<TResponse>(http, HttpMethod.Get, requestUri, null, configure, readBody: true, cancellationToken);

  /// <summary>
  /// Envia um POST com o corpo em JSON e a requisição ajustada por quem chama, e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio, como a inclusão de cabeçalhos.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  public static Task<TResponse?> PostAsync<TRequest, TResponse>(
    this IHttpClientService http,
    string requestUri,
    TRequest body,
    Action<HttpRequestMessage> configure,
    CancellationToken cancellationToken = default
  ) => SendAsync<TResponse>(http, HttpMethod.Post, requestUri, Content(body), configure, readBody: true, cancellationToken);

  /// <summary>
  /// Envia um POST com o corpo em JSON e a requisição ajustada por quem chama, sem ler o corpo da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio, como a inclusão de cabeçalhos.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Uma tarefa que termina quando a resposta de sucesso chega.</returns>
  public static Task PostAsync<TRequest>(
    this IHttpClientService http,
    string requestUri,
    TRequest body,
    Action<HttpRequestMessage> configure,
    CancellationToken cancellationToken = default
  ) => SendAsync<object>(http, HttpMethod.Post, requestUri, Content(body), configure, readBody: false, cancellationToken);

  /// <summary>
  /// Envia um PUT com o corpo em JSON e a requisição ajustada por quem chama, e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio, como a inclusão de cabeçalhos.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  public static Task<TResponse?> PutAsync<TRequest, TResponse>(
    this IHttpClientService http,
    string requestUri,
    TRequest body,
    Action<HttpRequestMessage> configure,
    CancellationToken cancellationToken = default
  ) => SendAsync<TResponse>(http, HttpMethod.Put, requestUri, Content(body), configure, readBody: true, cancellationToken);

  /// <summary>
  /// Envia um PUT com o corpo em JSON e a requisição ajustada por quem chama, sem ler o corpo da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio, como a inclusão de cabeçalhos.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Uma tarefa que termina quando a resposta de sucesso chega.</returns>
  public static Task PutAsync<TRequest>(
    this IHttpClientService http,
    string requestUri,
    TRequest body,
    Action<HttpRequestMessage> configure,
    CancellationToken cancellationToken = default
  ) => SendAsync<object>(http, HttpMethod.Put, requestUri, Content(body), configure, readBody: false, cancellationToken);

  /// <summary>
  /// Envia um PATCH com o corpo em JSON e a requisição ajustada por quem chama, e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio, como a inclusão de cabeçalhos.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  public static Task<TResponse?> PatchAsync<TRequest, TResponse>(
    this IHttpClientService http,
    string requestUri,
    TRequest body,
    Action<HttpRequestMessage> configure,
    CancellationToken cancellationToken = default
  ) => SendAsync<TResponse>(http, HttpMethod.Patch, requestUri, Content(body), configure, readBody: true, cancellationToken);

  /// <summary>
  /// Envia um PATCH com o corpo em JSON e a requisição ajustada por quem chama, sem ler o corpo da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio, como a inclusão de cabeçalhos.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Uma tarefa que termina quando a resposta de sucesso chega.</returns>
  public static Task PatchAsync<TRequest>(
    this IHttpClientService http,
    string requestUri,
    TRequest body,
    Action<HttpRequestMessage> configure,
    CancellationToken cancellationToken = default
  ) => SendAsync<object>(http, HttpMethod.Patch, requestUri, Content(body), configure, readBody: false, cancellationToken);

  /// <summary>
  /// Envia um DELETE com a requisição ajustada por quem chama, sem ler o corpo da resposta.
  /// </summary>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio, como a inclusão de cabeçalhos.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Uma tarefa que termina quando a resposta de sucesso chega.</returns>
  public static Task DeleteAsync(
    this IHttpClientService http,
    string requestUri,
    Action<HttpRequestMessage> configure,
    CancellationToken cancellationToken = default
  ) => SendAsync<object>(http, HttpMethod.Delete, requestUri, null, configure, readBody: false, cancellationToken);

  /// <summary>
  /// Envia um DELETE com a requisição ajustada por quem chama e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio, como a inclusão de cabeçalhos.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  public static Task<TResponse?> DeleteAsync<TResponse>(
    this IHttpClientService http,
    string requestUri,
    Action<HttpRequestMessage> configure,
    CancellationToken cancellationToken = default
  ) => SendAsync<TResponse>(http, HttpMethod.Delete, requestUri, null, configure, readBody: true, cancellationToken);

  #endregion

  #region Private Methods

  /// <summary>
  /// Serializa o corpo da requisição em JSON, como os métodos por verbo da interface.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <param name="body">Corpo da requisição.</param>
  /// <returns>O conteúdo da requisição.</returns>
  private static ByteArrayContent Content<TRequest>(TRequest body) => HttpClientService.Serialize(body);

  /// <summary>
  /// Monta a requisição, aplica o ajuste de quem chama e a envia pelo caminho do JSON.
  /// </summary>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="http">O serviço de chamadas HTTP.</param>
  /// <param name="method">O verbo HTTP.</param>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="content">O corpo da requisição, quando houver.</param>
  /// <param name="configure">Ação que ajusta a requisição antes do envio.</param>
  /// <param name="readBody">Indica se o corpo da resposta de sucesso é lido.</param>
  /// <param name="cancellationToken">Token de cancelamento de quem chamou.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando não é lido, a resposta é 204 ou não tem corpo.</returns>
  private static async Task<TResponse?> SendAsync<TResponse>(
    IHttpClientService http,
    HttpMethod method,
    string requestUri,
    HttpContent? content,
    Action<HttpRequestMessage> configure,
    bool readBody,
    CancellationToken cancellationToken
  )
  {
    // O conteúdo é descartado junto com a requisição, inclusive quando um argumento é recusado
    using var request = new HttpRequestMessage(method, (Uri?)null) { Content = content };

    ArgumentNullException.ThrowIfNull(http);
    ArgumentNullException.ThrowIfNull(configure);

    request.RequestUri = HttpClientService.ParseRequestUri(requestUri);

    configure(request);

    // O serviço do pacote envia pelo próprio caminho dos métodos por verbo: o corpo de erro lido com limite de
    // tamanho, e a leitura do corpo dentro do tempo limite da chamada
    if (http is HttpClientService service)
    {
      return await service.SendJsonAsync<TResponse>(request, readBody, cancellationToken).ConfigureAwait(false);
    }

    // Outra implementação da interface (um decorador, um substituto de teste) só oferece o SendAsync: a resposta
    // volta com o corpo carregado e recebe o mesmo tratamento de erro e de JSON
    try
    {
      using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);

      return await HttpClientService.ReadResponseAsync<TResponse>(response, readBody, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (HttpErrorMapper.Map(exception, cancellationToken) is { } mapped)
    {
      throw mapped;
    }
  }

  #endregion
}
