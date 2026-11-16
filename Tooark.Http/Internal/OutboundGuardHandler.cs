using System.Net;
using Tooark.Http.Exceptions;

namespace Tooark.Http.Internal;

/// <summary>
/// Handler da guarda de saída que atua na requisição, antes da conexão.
/// </summary>
/// <remarks>
/// Faz três coisas que o <see cref="OutboundConnector"/> não alcança sozinho:
/// <list type="bullet">
/// <item>confere o IP literal do host da requisição, a única conferência possível quando há um proxy de saída: com
/// proxy, a conexão é aberta para o proxy, e é ele quem resolve o nome;</item>
/// <item>mantém a requisição em HTTP/1.1 ou HTTP/2, porque a conexão QUIC do HTTP/3 não passa pelo
/// <see cref="SocketsHttpHandler.ConnectCallback"/>;</item>
/// <item>devolve a exceção da guarda sem o embrulho do transporte. O handler de conexão entrega a recusa dentro de
/// uma <see cref="HttpRequestException"/>, que o handler de resiliência trata como falha transitória: repetiria a
/// requisição e contaria a recusa no circuito, e URLs bloqueadas informadas por um usuário abririam o circuito para
/// todos.</item>
/// </list>
/// </remarks>
/// <param name="policy">A regra da guarda.</param>
internal sealed class OutboundGuardHandler(OutboundGuardPolicy policy) : DelegatingHandler
{
  #region Overrides

  /// <inheritdoc />
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    Prepare(request);

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
    Prepare(request);

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
  /// Confere o host literal da requisição e limita a versão do HTTP.
  /// </summary>
  /// <param name="request">A requisição.</param>
  /// <exception cref="OutboundBlockedException">Quando o host é um IP literal bloqueado.</exception>
  private void Prepare(HttpRequestMessage request)
  {
    if (request.RequestUri is { IsAbsoluteUri: true } uri
      && IPAddress.TryParse(uri.IdnHost, out var literal)
      && policy.IsBlocked(literal))
    {
      throw new OutboundBlockedException(uri.IdnHost);
    }

    // RequestVersionOrHigher deixaria o handler subir para o HTTP/3 quando o servidor o anuncia
    if (request.Version.Major >= 3 || request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
    {
      request.Version = HttpVersion.Version20;
      request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }
  }

  #endregion
}
