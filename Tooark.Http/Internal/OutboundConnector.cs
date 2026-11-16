using System.Net;
using System.Net.Sockets;
using Tooark.Http.Exceptions;

namespace Tooark.Http.Internal;

/// <summary>
/// Abre a conexão TCP do <see cref="SocketsHttpHandler"/> conferindo o endereço de destino.
/// </summary>
/// <remarks>
/// É o <see cref="SocketsHttpHandler.ConnectCallback"/> da guarda de saída. O nome é resolvido uma única vez, os
/// endereços são conferidos e a conexão é aberta para o endereço conferido, e não para o nome: resolver de novo na
/// hora de conectar deixaria o DNS responder outro endereço entre a conferência e a conexão (DNS rebinding). Cada
/// salto de um redirecionamento abre a própria conexão e passa por aqui.
/// </remarks>
/// <param name="policy">A regra da guarda.</param>
/// <param name="resolver">O resolvedor de nomes.</param>
internal sealed class OutboundConnector(OutboundGuardPolicy policy, IHostResolver resolver)
{
  #region Internal Methods

  /// <summary>
  /// Resolve o host, confere os endereços e conecta ao primeiro que aceitar a conexão.
  /// </summary>
  /// <param name="context">O contexto da conexão, com o host e a porta de destino.</param>
  /// <param name="cancellationToken">Token de cancelamento, que carrega o tempo limite de conexão do handler.</param>
  /// <returns>O stream da conexão aberta.</returns>
  /// <exception cref="OutboundBlockedException">Quando o destino está bloqueado.</exception>
  internal async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
  {
    var endPoint = context.DnsEndPoint;
    var addresses = await ResolveAsync(endPoint.Host, cancellationToken).ConfigureAwait(false);

    SocketException? failure = null;

    // Tenta os endereços na ordem da resolução, como faz o handler quando recebe o nome
    foreach (var address in addresses)
    {
      Socket? socket = null;

      try
      {
        // Um socket por tentativa: depois de uma conexão recusada, o mesmo socket não pode ser reutilizado em
        // todas as plataformas
        socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        await socket.ConnectAsync(new IPEndPoint(address, endPoint.Port), cancellationToken).ConfigureAwait(false);

        return new NetworkStream(socket, ownsSocket: true);
      }
      catch (SocketException exception)
      {
        socket?.Dispose();
        failure = exception;
      }
      catch
      {
        socket?.Dispose();

        throw;
      }
    }

    throw failure ?? new SocketException((int)SocketError.HostNotFound);
  }

  /// <summary>
  /// Resolve o host e confere os endereços contra a regra da guarda.
  /// </summary>
  /// <param name="host">O host da conexão: um nome ou um endereço IP literal.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Os endereços conferidos, na ordem em que devem ser tentados.</returns>
  /// <exception cref="OutboundBlockedException">Quando o destino está bloqueado.</exception>
  internal async ValueTask<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
  {
    // Um IP literal na URL não passa pelo DNS, mas passa pela mesma regra
    if (IPAddress.TryParse(host, out var literal))
    {
      return policy.IsBlocked(literal) ? throw new OutboundBlockedException(host) : [OutboundGuard.Normalize(literal)];
    }

    var addresses = await resolver.ResolveAsync(host, cancellationToken).ConfigureAwait(false);

    // O IPv4 mapeado em IPv6 é conferido e conectado como IPv4, o destino que ele de fato tem
    addresses = Array.ConvertAll(addresses, OutboundGuard.Normalize);

    // Basta um endereço bloqueado para recusar o host inteiro: um nome que mistura endereço público e interno é o
    // desenho de um ataque de rebinding, e tentar só os públicos deixaria a escolha com quem controla o DNS
    if (!policy.IsHostAllowed(host) && Array.Exists(addresses, policy.IsBlocked))
    {
      throw new OutboundBlockedException(host);
    }

    return addresses;
  }

  #endregion
}
