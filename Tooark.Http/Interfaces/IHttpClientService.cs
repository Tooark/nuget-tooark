using Tooark.Exceptions;
using Tooark.Http.Exceptions;

namespace Tooark.Http.Interfaces;

/// <summary>
/// Interface do serviço de chamadas HTTP de saída com JSON.
/// </summary>
/// <remarks>
/// Todos os métodos enviam pelo cliente nomeado do Tooark, criado pelo <see cref="IHttpClientFactory"/>, e por isso
/// passam pela resiliência e pela guarda de saída configuradas. As falhas chegam como exceções do Tooark:
/// <list type="bullet">
/// <item><see cref="HttpRequestFailedException"/> (502) quando o serviço chamado responde com um status de erro;</item>
/// <item><see cref="GatewayTimeoutException"/> (504) no tempo limite;</item>
/// <item><see cref="ServiceUnavailableException"/> (503) com o circuito aberto;</item>
/// <item><see cref="BadGatewayException"/> (502) na falha de conexão ou no corpo que não é o JSON esperado;</item>
/// <item><see cref="OutboundBlockedException"/> (400) quando a guarda de saída recusa o destino.</item>
/// </list>
/// O cancelamento pelo token de quem chamou segue como <see cref="OperationCanceledException"/>.
/// </remarks>
public interface IHttpClientService
{
  /// <summary>
  /// Envia um GET e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);

  /// <summary>
  /// Envia um POST com o corpo em JSON e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

  /// <summary>
  /// Envia um POST com o corpo em JSON, sem ler o corpo da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Uma tarefa que termina quando a resposta de sucesso chega.</returns>
  Task PostAsync<TRequest>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

  /// <summary>
  /// Envia um PUT com o corpo em JSON e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

  /// <summary>
  /// Envia um PUT com o corpo em JSON, sem ler o corpo da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Uma tarefa que termina quando a resposta de sucesso chega.</returns>
  Task PutAsync<TRequest>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

  /// <summary>
  /// Envia um PATCH com o corpo em JSON e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  Task<TResponse?> PatchAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

  /// <summary>
  /// Envia um PATCH com o corpo em JSON, sem ler o corpo da resposta.
  /// </summary>
  /// <typeparam name="TRequest">Tipo do corpo da requisição.</typeparam>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="body">Corpo da requisição.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Uma tarefa que termina quando a resposta de sucesso chega.</returns>
  Task PatchAsync<TRequest>(string requestUri, TRequest body, CancellationToken cancellationToken = default);

  /// <summary>
  /// Envia um DELETE, sem ler o corpo da resposta.
  /// </summary>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Uma tarefa que termina quando a resposta de sucesso chega.</returns>
  Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);

  /// <summary>
  /// Envia um DELETE e lê o corpo JSON da resposta.
  /// </summary>
  /// <typeparam name="TResponse">Tipo do corpo da resposta.</typeparam>
  /// <param name="requestUri">URI da requisição, absoluta ou relativa ao endereço base.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo da resposta, ou o valor padrão quando a resposta é 204 ou não tem corpo.</returns>
  Task<TResponse?> DeleteAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);

  /// <summary>
  /// Envia uma requisição montada por quem chama e devolve a resposta como veio.
  /// </summary>
  /// <remarks>
  /// É a saída para o que os métodos por verbo não cobrem: cabeçalhos próprios, corpo que não é JSON, leitura do
  /// status. A requisição passa pelo mesmo cliente, com a resiliência e a guarda de saída, e a resposta volta com o
  /// corpo já carregado em memória. Um status de erro <b>não</b> vira exceção aqui; as falhas de transporte (tempo
  /// limite, circuito aberto, conexão, destino bloqueado) viram, como nos demais métodos. Quem chama descarta a
  /// resposta.
  /// </remarks>
  /// <param name="request">A requisição.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>A resposta do serviço chamado, com qualquer status.</returns>
  Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}
