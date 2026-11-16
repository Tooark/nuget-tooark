using System.Net;
using System.Net.Sockets;
using Tooark.Http.Exceptions;

namespace Tooark.Http.Internal;

/// <summary>
/// Handler da guarda de saída que atua na requisição, antes da conexão.
/// </summary>
/// <remarks>
/// Faz quatro coisas que o <see cref="OutboundConnector"/> não alcança sozinho:
/// <list type="bullet">
/// <item>confere o IP literal do host da requisição;</item>
/// <item>confere o nome do host, resolvendo-o pelo DNS da aplicação, quando a conexão não vai enxergar o destino.
/// Com um proxy de saída a conexão é aberta para o proxy, e é ele quem resolve o nome; com um handler primário que
/// não é o da guarda, não existe conferência na conexão. Sem proxy e com o handler da guarda, o nome é conferido uma
/// vez só, na conexão, e aqui não há consulta ao DNS;</item>
/// <item>mantém a requisição em HTTP/1.1 ou HTTP/2, porque a conexão QUIC do HTTP/3 não passa pelo
/// <see cref="SocketsHttpHandler.ConnectCallback"/>;</item>
/// <item>devolve a exceção da guarda sem o embrulho do transporte. O handler de conexão entrega a recusa dentro de
/// uma <see cref="HttpRequestException"/>, que o handler de resiliência trata como falha transitória: repetiria a
/// requisição e contaria a recusa no circuito, e URLs bloqueadas informadas por um usuário abririam o circuito para
/// todos.</item>
/// </list>
/// A conferência do nome feita aqui é um filtro, e não uma garantia: o proxy resolve o nome de novo, e um DNS
/// controlado por quem ataca pode responder um endereço para a aplicação e outro para o proxy. Os saltos de um
/// redirecionamento seguido pelo handler primário também não passam por aqui.
/// </remarks>
/// <param name="policy">A regra da guarda.</param>
/// <param name="connector">A conferência do nome, a mesma usada na conexão.</param>
internal sealed class OutboundGuardHandler(OutboundGuardPolicy policy, OutboundConnector connector) : DelegatingHandler
{
  #region Overrides

  /// <inheritdoc />
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    if (Prepare(request) is { } host)
    {
      try
      {
        await connector.ResolveAsync(host, cancellationToken).ConfigureAwait(false);
      }
      catch (SocketException exception)
      {
        ThrowUnlessUnresolvedIsAllowed(exception);
      }
    }

    try
    {
      return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (FindBlocked(exception) is { } blocked)
    {
      throw new OutboundBlockedException(blocked.Host, exception);
    }
  }

  /// <inheritdoc />
  protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    if (Prepare(request) is { } host)
    {
      try
      {
        connector.Resolve(host);
      }
      catch (SocketException exception)
      {
        ThrowUnlessUnresolvedIsAllowed(exception);
      }
    }

    try
    {
      return base.Send(request, cancellationToken);
    }
    catch (Exception exception) when (FindBlocked(exception) is { } blocked)
    {
      throw new OutboundBlockedException(blocked.Host, exception);
    }
  }

  #endregion

  #region Internal Methods

  /// <summary>
  /// Procura a recusa da guarda dentro de uma exceção de transporte.
  /// </summary>
  /// <param name="exception">A exceção recebida do handler interno.</param>
  /// <returns>A recusa da guarda, ou nulo quando a exceção não a carrega ou já é a própria recusa.</returns>
  internal static OutboundBlockedException? FindBlocked(Exception exception)
  {
    for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
    {
      if (inner is OutboundBlockedException blocked)
      {
        return blocked;
      }
    }

    return null;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Confere o host literal da requisição, limita a versão do HTTP e diz se o nome precisa ser conferido aqui.
  /// </summary>
  /// <param name="request">A requisição.</param>
  /// <returns>O nome de host a resolver e conferir antes do envio, ou nulo quando não há o que conferir aqui.</returns>
  /// <exception cref="OutboundBlockedException">Quando o host é um IP literal bloqueado.</exception>
  private string? Prepare(HttpRequestMessage request)
  {
    // RequestVersionOrHigher deixaria o handler subir para o HTTP/3 quando o servidor o anuncia
    if (request.Version.Major >= 3 || request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
    {
      request.Version = HttpVersion.Version20;
      request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }

    if (request.RequestUri is not { IsAbsoluteUri: true } uri)
    {
      return null;
    }

    if (IPAddress.TryParse(uri.IdnHost, out var literal))
    {
      return policy.IsBlocked(literal) ? throw new OutboundBlockedException(uri.IdnHost) : null;
    }

    // Um nome liberado é de confiança, resolva para onde resolver: não é consultado, nem precisa resolver aqui
    return policy.IsHostAllowed(uri.IdnHost) || ConnectionSeesDestination(uri) ? null : uri.IdnHost;
  }

  /// <summary>
  /// Indica se a conexão desta requisição será aberta pela guarda direto para o destino.
  /// </summary>
  /// <remarks>
  /// Só responde verdadeiro quando há certeza: o handler primário é o <see cref="SocketsHttpHandler"/> com o
  /// <see cref="SocketsHttpHandler.ConnectCallback"/> da guarda e a requisição não vai por um proxy. Um engano para
  /// o lado do "não" custa uma consulta a mais ao DNS; para o lado do "sim", deixaria o nome sem conferência.
  /// A leitura é feita a cada requisição, e não no construtor: a fábrica liga os handlers depois de criá-los, e a
  /// decisão do proxy depende da URI.
  /// </remarks>
  /// <param name="uri">A URI da requisição.</param>
  /// <returns>Verdadeiro quando a conferência do nome fica com a conexão.</returns>
  private bool ConnectionSeesDestination(Uri uri)
  {
    var primary = InnerHandler;

    while (primary is DelegatingHandler delegating)
    {
      primary = delegating.InnerHandler;
    }

    if (primary is not SocketsHttpHandler { ConnectCallback.Target: OutboundConnector } sockets)
    {
      return false;
    }

    if (!sockets.UseProxy)
    {
      return true;
    }

    try
    {
      // A mesma pergunta que o handler faz ao proxy para decidir o caminho da requisição
      var proxy = sockets.Proxy ?? HttpClient.DefaultProxy;

      return proxy.IsBypassed(uri) || proxy.GetProxy(uri) is null;
    }
    catch (Exception exception) when (exception is not OutOfMemoryException)
    {
      // Um proxy que falha ao responder não dá certeza de nada: o nome é conferido aqui
      return false;
    }
  }

  /// <summary>
  /// Trata o nome que a aplicação não conseguiu resolver.
  /// </summary>
  /// <remarks>
  /// Por padrão a guarda recusa o que não consegue conferir, e a recusa sai como a mesma falha de resolução de nome
  /// que a conexão direta daria: é transitória para o handler de resiliência, e não conta como destino bloqueado.
  /// Com <c>AllowUnresolvedHostsBehindProxy</c>, a requisição segue sem a conferência do nome.
  /// </remarks>
  /// <param name="exception">A falha de resolução do DNS.</param>
  /// <exception cref="HttpRequestException">Quando o nome que não resolve não pode passar.</exception>
  private void ThrowUnlessUnresolvedIsAllowed(SocketException exception)
  {
    if (!policy.AllowUnresolvedHostsBehindProxy)
    {
      throw new HttpRequestException(HttpRequestError.NameResolutionError, exception.Message, exception);
    }
  }

  #endregion
}
