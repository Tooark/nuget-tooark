using System.Net;
using System.Net.Sockets;

namespace Tooark.Http;

/// <summary>
/// Regra de bloqueio da guarda de saída: diz se um endereço IP pertence a uma faixa que uma URL informada pelo
/// usuário não pode alcançar.
/// </summary>
/// <remarks>
/// São bloqueados o loopback, as redes privadas, o link-local (onde fica o endereço de metadados das nuvens,
/// <c>169.254.169.254</c>), o CGNAT, as faixas reservadas, de documentação e de multicast, em IPv4 e IPv6. Um
/// endereço IPv6 que carrega um IPv4 (mapeado, NAT64, 6to4 e Teredo) é classificado pelo IPv4 que carrega, para que
/// <c>::ffff:127.0.0.1</c> não passe por ser IPv6.
/// </remarks>
public static class OutboundGuard
{
  #region Private Fields

  /// <summary>
  /// Faixas IPv4 bloqueadas.
  /// </summary>
  private static readonly IPNetwork[] BlockedIPv4 =
  [
    // "Esta rede" (RFC 791); 0.0.0.0 chega ao próprio host em vários sistemas
    IPNetwork.Parse("0.0.0.0/8"),
    // Redes privadas (RFC 1918)
    IPNetwork.Parse("10.0.0.0/8"),
    IPNetwork.Parse("172.16.0.0/12"),
    IPNetwork.Parse("192.168.0.0/16"),
    // CGNAT (RFC 6598), usado por provedores e por redes internas de nuvem
    IPNetwork.Parse("100.64.0.0/10"),
    // Loopback
    IPNetwork.Parse("127.0.0.0/8"),
    // Link-local (RFC 3927), que inclui o endereço de metadados 169.254.169.254
    IPNetwork.Parse("169.254.0.0/16"),
    // Atribuições de protocolo da IETF (RFC 6890)
    IPNetwork.Parse("192.0.0.0/24"),
    // Documentação (RFC 5737)
    IPNetwork.Parse("192.0.2.0/24"),
    IPNetwork.Parse("198.51.100.0/24"),
    IPNetwork.Parse("203.0.113.0/24"),
    // Testes de desempenho entre redes (RFC 2544)
    IPNetwork.Parse("198.18.0.0/15"),
    // Multicast
    IPNetwork.Parse("224.0.0.0/4"),
    // Reservado (RFC 1112), que inclui o broadcast 255.255.255.255
    IPNetwork.Parse("240.0.0.0/4")
  ];

  /// <summary>
  /// Faixas IPv6 bloqueadas por inteiro.
  /// </summary>
  private static readonly IPNetwork[] BlockedIPv6 =
  [
    // Não especificado (::), loopback (::1) e os IPv4-compatíveis, obsoletos (RFC 4291)
    IPNetwork.Parse("::/96"),
    // NAT64 de uso local (RFC 8215)
    IPNetwork.Parse("64:ff9b:1::/48"),
    // Descarte (RFC 6666)
    IPNetwork.Parse("100::/64"),
    // Documentação (RFC 3849)
    IPNetwork.Parse("2001:db8::/32"),
    // Endereços locais únicos (RFC 4193), o equivalente às redes privadas
    IPNetwork.Parse("fc00::/7"),
    // Link-local
    IPNetwork.Parse("fe80::/10"),
    // Site-local, obsoleto (RFC 3879)
    IPNetwork.Parse("fec0::/10"),
    // Multicast
    IPNetwork.Parse("ff00::/8")
  ];

  /// <summary>
  /// NAT64 (RFC 6052): os últimos 32 bits são o IPv4 de destino.
  /// </summary>
  private static readonly IPNetwork Nat64 = IPNetwork.Parse("64:ff9b::/96");

  /// <summary>
  /// 6to4 (RFC 3056): os bits 16 a 47 são o IPv4 do roteador de borda.
  /// </summary>
  private static readonly IPNetwork SixToFour = IPNetwork.Parse("2002::/16");

  /// <summary>
  /// Teredo (RFC 4380): os últimos 32 bits são o IPv4 do cliente, com os bits invertidos.
  /// </summary>
  private static readonly IPNetwork Teredo = IPNetwork.Parse("2001::/32");

  #endregion

  #region Methods

  /// <summary>
  /// Indica se o endereço pertence a uma faixa bloqueada.
  /// </summary>
  /// <param name="address">O endereço IP de destino.</param>
  /// <returns>Verdadeiro quando o endereço não pode ser alcançado por uma URL informada pelo usuário.</returns>
  /// <exception cref="ArgumentNullException">Quando o endereço é nulo.</exception>
  public static bool IsBlocked(IPAddress address)
  {
    ArgumentNullException.ThrowIfNull(address);

    address = Normalize(address);

    if (address.AddressFamily == AddressFamily.InterNetwork)
    {
      return Contains(BlockedIPv4, address);
    }

    // Uma família que não é IP não tem como ser conferida: fica bloqueada
    if (address.AddressFamily != AddressFamily.InterNetworkV6)
    {
      return true;
    }

    // Um IPv4 embutido leva ao mesmo destino que o IPv4, então vale a regra dele
    if (TryGetEmbeddedIPv4(address, out var embedded))
    {
      return Contains(BlockedIPv4, embedded);
    }

    return Contains(BlockedIPv6, address);
  }

  #endregion

  #region Internal Methods

  /// <summary>
  /// Devolve o IPv4 de um endereço IPv4 mapeado em IPv6 (<c>::ffff:a.b.c.d</c>), ou o próprio endereço.
  /// </summary>
  /// <remarks>
  /// O mapeado é o mesmo destino do IPv4 no socket, e por isso é normalizado antes de qualquer comparação, da lista
  /// de bloqueio e das redes liberadas.
  /// </remarks>
  /// <param name="address">O endereço IP.</param>
  /// <returns>O endereço normalizado.</returns>
  internal static IPAddress Normalize(IPAddress address) =>
    address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

  #endregion

  #region Private Methods

  /// <summary>
  /// Extrai o IPv4 embutido em um endereço NAT64, 6to4 ou Teredo.
  /// </summary>
  /// <param name="address">O endereço IPv6.</param>
  /// <param name="embedded">O IPv4 embutido, quando houver.</param>
  /// <returns>Verdadeiro quando o endereço carrega um IPv4.</returns>
  private static bool TryGetEmbeddedIPv4(IPAddress address, out IPAddress embedded)
  {
    Span<byte> bytes = stackalloc byte[16];
    address.TryWriteBytes(bytes, out _);

    if (Nat64.Contains(address))
    {
      embedded = new IPAddress(bytes[12..]);

      return true;
    }

    if (SixToFour.Contains(address))
    {
      embedded = new IPAddress(bytes.Slice(2, 4));

      return true;
    }

    if (Teredo.Contains(address))
    {
      Span<byte> client = [(byte)~bytes[12], (byte)~bytes[13], (byte)~bytes[14], (byte)~bytes[15]];
      embedded = new IPAddress(client);

      return true;
    }

    embedded = IPAddress.None;

    return false;
  }

  /// <summary>
  /// Indica se alguma das redes contém o endereço.
  /// </summary>
  /// <param name="networks">As redes.</param>
  /// <param name="address">O endereço IP, já normalizado.</param>
  /// <returns>Verdadeiro quando o endereço pertence a alguma das redes.</returns>
  private static bool Contains(IPNetwork[] networks, IPAddress address)
  {
    foreach (var network in networks)
    {
      if (network.Contains(address))
      {
        return true;
      }
    }

    return false;
  }

  #endregion
}
