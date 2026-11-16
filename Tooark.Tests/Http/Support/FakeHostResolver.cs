using System.Net;
using System.Net.Sockets;
using Tooark.Http.Internal;

namespace Tooark.Tests.Http.Support;

/// <summary>
/// DNS falso da guarda de saída: resolve os nomes para os endereços informados e conta as consultas.
/// </summary>
internal sealed class FakeHostResolver : IHostResolver
{
  private readonly Dictionary<string, IPAddress[]> _hosts = new(StringComparer.OrdinalIgnoreCase);
  private readonly List<string> _queries = [];

  // Nomes consultados, na ordem das consultas
  public IReadOnlyList<string> Queries
  {
    get
    {
      lock (_queries)
      {
        return [.. _queries];
      }
    }
  }

  // Define os endereços de um nome
  public FakeHostResolver Map(string host, params string[] addresses)
  {
    _hosts[host] = [.. addresses.Select(IPAddress.Parse)];

    return this;
  }

  public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
  {
    lock (_queries)
    {
      _queries.Add(host);
    }

    // Nome desconhecido falha como no DNS real
    return _hosts.TryGetValue(host, out var addresses)
      ? Task.FromResult(addresses)
      : Task.FromException<IPAddress[]>(new SocketException((int)SocketError.HostNotFound));
  }
}
