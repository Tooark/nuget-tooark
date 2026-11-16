using System.Net;
using System.Text;

namespace Tooark.Tests.Http.Support;

/// <summary>
/// Requisição recebida pelo handler falso, com o corpo já lido.
/// </summary>
internal sealed record RecordedRequest(
  HttpMethod Method,
  Uri? RequestUri,
  string? Body,
  string? ContentType,
  Version Version,
  HttpVersionPolicy VersionPolicy);

/// <summary>
/// Handler primário falso: registra as requisições e responde pela função informada, sem rede.
/// </summary>
internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
  : HttpMessageHandler
{
  private readonly List<RecordedRequest> _requests = [];

  // Responde sempre com a mesma função síncrona
  public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    : this((request, _) => Task.FromResult(respond(request))) { }

  // Requisições recebidas, na ordem de chegada
  public IReadOnlyList<RecordedRequest> Requests
  {
    get
    {
      lock (_requests)
      {
        return [.. _requests];
      }
    }
  }

  // Quantidade de requisições recebidas, contando as novas tentativas
  public int Calls => Requests.Count;

  // Monta uma resposta com corpo JSON
  public static HttpResponseMessage Json(HttpStatusCode status, string json, string mediaType = "application/json") =>
    new(status) { Content = new StringContent(json, Encoding.UTF8, mediaType) };

  // Monta uma resposta sem corpo
  public static HttpResponseMessage Empty(HttpStatusCode status) => new(status);

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

    lock (_requests)
    {
      _requests.Add(new RecordedRequest(
        request.Method,
        request.RequestUri,
        body,
        request.Content?.Headers.ContentType?.ToString(),
        request.Version,
        request.VersionPolicy));
    }

    var response = await respond(request, cancellationToken);

    // O handler de conexão real sempre preenche a requisição da resposta
    response.RequestMessage ??= request;

    return response;
  }
}
