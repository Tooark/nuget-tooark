using System.Net;

namespace Tooark.Http.Internal;

/// <summary>
/// Resolve um nome de host para os endereços IP da conexão.
/// </summary>
/// <remarks>
/// Existe para que os testes troquem o DNS: a guarda precisa ser exercitada com um nome público que resolve para um
/// endereço interno, sem depender de rede.
/// </remarks>
internal interface IHostResolver
{
  /// <summary>
  /// Resolve o nome de host.
  /// </summary>
  /// <param name="host">O nome de host.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Os endereços IP do host, na ordem em que devem ser tentados.</returns>
  Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken);

  /// <summary>
  /// Resolve o nome de host de forma síncrona, para o envio síncrono do <see cref="HttpClient"/>.
  /// </summary>
  /// <param name="host">O nome de host.</param>
  /// <returns>Os endereços IP do host, na ordem em que devem ser tentados.</returns>
  IPAddress[] Resolve(string host);
}

/// <summary>
/// Resolve nomes de host pelo DNS do sistema.
/// </summary>
internal sealed class DnsHostResolver : IHostResolver
{
  /// <inheritdoc />
  public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken) =>
    Dns.GetHostAddressesAsync(host, cancellationToken);

  /// <inheritdoc />
  public IPAddress[] Resolve(string host) => Dns.GetHostAddresses(host);
}
