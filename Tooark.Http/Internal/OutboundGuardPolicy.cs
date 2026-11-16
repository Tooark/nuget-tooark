using System.Globalization;
using System.Net;
using Tooark.Exceptions;
using Tooark.Http.Options;

namespace Tooark.Http.Internal;

/// <summary>
/// Regra da guarda de saída montada a partir das opções: a lista de bloqueio com as exceções de host e de rede.
/// </summary>
/// <remarks>
/// As listas são interpretadas uma vez, no registro, e a regra não guarda estado por chamada.
/// </remarks>
internal sealed class OutboundGuardPolicy
{
  #region Private Fields

  /// <summary>
  /// Converte nomes internacionais para a forma ASCII, a mesma que a URI entrega em <see cref="Uri.IdnHost"/>.
  /// </summary>
  private static readonly IdnMapping Idn = new();

  /// <summary>
  /// Nomes de host liberados por comparação exata.
  /// </summary>
  private readonly HashSet<string> _hosts;

  /// <summary>
  /// Sufixos liberados, com o ponto inicial.
  /// </summary>
  private readonly string[] _suffixes;

  /// <summary>
  /// Redes liberadas da lista de bloqueio.
  /// </summary>
  private readonly IPNetwork[] _networks;

  #endregion

  #region Properties

  /// <summary>
  /// Indica se, com um proxy de saída, o nome que a aplicação não consegue resolver passa sem conferência.
  /// </summary>
  internal bool AllowUnresolvedHostsBehindProxy { get; }

  #endregion

  #region Constructor

  /// <summary>
  /// Cria a regra com as listas já interpretadas.
  /// </summary>
  /// <param name="hosts">Nomes de host liberados por comparação exata.</param>
  /// <param name="suffixes">Sufixos liberados, com o ponto inicial.</param>
  /// <param name="networks">Redes liberadas da lista de bloqueio.</param>
  /// <param name="allowUnresolvedHostsBehindProxy">Se o nome que não resolve passa quando há um proxy de saída.</param>
  private OutboundGuardPolicy(HashSet<string> hosts, string[] suffixes, IPNetwork[] networks, bool allowUnresolvedHostsBehindProxy)
  {
    _hosts = hosts;
    _suffixes = suffixes;
    _networks = networks;
    AllowUnresolvedHostsBehindProxy = allowUnresolvedHostsBehindProxy;
  }

  #endregion

  #region Internal Methods

  /// <summary>
  /// Interpreta as listas das opções e monta a regra.
  /// </summary>
  /// <param name="options">As opções da guarda.</param>
  /// <returns>A regra da guarda.</returns>
  /// <exception cref="InternalServerErrorException">Quando um host ou uma rede da lista é inválido.</exception>
  internal static OutboundGuardPolicy Create(OutboundGuardOptions options)
  {
    var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var suffixes = new List<string>();
    var networks = new List<IPNetwork>();

    foreach (var entry in options.AllowedHosts ?? [])
    {
      var host = ParseHost(entry, out var isSuffix);

      if (isSuffix)
      {
        suffixes.Add(host);
      }
      else
      {
        hosts.Add(host);
      }
    }

    foreach (var entry in options.AllowedNetworks ?? [])
    {
      if (!TryParseNetwork(entry, out var network))
      {
        throw new InternalServerErrorException($"Options.HttpClient.OutboundGuard.AllowedNetworkInvalid;{Printable(entry)}");
      }

      networks.Add(network);
    }

    return new OutboundGuardPolicy(hosts, [.. suffixes], [.. networks], options.AllowUnresolvedHostsBehindProxy);
  }

  /// <summary>
  /// Indica se o nome de host está liberado da conferência de endereço.
  /// </summary>
  /// <param name="host">O nome de host da conexão.</param>
  /// <returns>Verdadeiro quando o nome está na lista, por comparação exata ou por sufixo.</returns>
  internal bool IsHostAllowed(string host)
  {
    // O ponto final do nome absoluto (exemplo.com.) não muda o destino
    host = host.TrimEnd('.');

    if (_hosts.Contains(host))
    {
      return true;
    }

    foreach (var suffix in _suffixes)
    {
      // O sufixo guarda o ponto inicial, então .interna.exemplo.com não libera falsainterna.exemplo.com
      if (host.Length > suffix.Length && host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Indica se o endereço está bloqueado: pertence à lista de bloqueio e a nenhuma das redes liberadas.
  /// </summary>
  /// <remarks>
  /// A rede liberada vale para o endereço da conexão. Um endereço NAT64, 6to4 ou Teredo é bloqueado pelo IPv4 que
  /// carrega, mas não é liberado por ele: liberar <c>10.0.0.0/8</c> não libera um túnel que termina lá.
  /// </remarks>
  /// <param name="address">O endereço IP de destino.</param>
  /// <returns>Verdadeiro quando a conexão ao endereço deve ser recusada.</returns>
  internal bool IsBlocked(IPAddress address)
  {
    var normalized = OutboundGuard.Normalize(address);

    foreach (var network in _networks)
    {
      if (network.Contains(normalized))
      {
        return false;
      }
    }

    return OutboundGuard.IsBlocked(normalized);
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Interpreta uma entrada da lista de hosts.
  /// </summary>
  /// <param name="entry">A entrada da lista.</param>
  /// <param name="isSuffix">Verdadeiro quando a entrada é um sufixo, iniciado por ponto.</param>
  /// <returns>O nome em ASCII, com o ponto inicial quando é um sufixo.</returns>
  /// <exception cref="InternalServerErrorException">Quando a entrada não é um nome de host.</exception>
  private static string ParseHost(string? entry, out bool isSuffix)
  {
    var value = entry?.Trim() ?? string.Empty;
    isSuffix = value.StartsWith('.');

    var name = isSuffix ? value[1..] : value;

    // Só nomes: esquema, porta, caminho e curinga não fazem parte de um nome de host, e endereço IP tem lista própria
    if (Uri.CheckHostName(name) != UriHostNameType.Dns)
    {
      throw new InternalServerErrorException($"Options.HttpClient.OutboundGuard.AllowedHostInvalid;{Printable(entry)}");
    }

    try
    {
      name = Idn.GetAscii(name.TrimEnd('.'));
    }
    catch (ArgumentException)
    {
      throw new InternalServerErrorException($"Options.HttpClient.OutboundGuard.AllowedHostInvalid;{Printable(entry)}");
    }

    return isSuffix ? "." + name : name;
  }

  /// <summary>
  /// Interpreta uma entrada da lista de redes, em notação CIDR.
  /// </summary>
  /// <remarks>
  /// O endereço que não é o da rede (<c>10.20.0.1/16</c>) é recusado, para não liberar por engano uma faixa maior que
  /// a pretendida. A leitura é feita aqui, e não pelo <see cref="IPNetwork.TryParse(string, out IPNetwork)"/>, porque
  /// ele aceita esse endereço, zerando os bits depois do prefixo.
  /// </remarks>
  /// <param name="entry">A entrada da lista.</param>
  /// <param name="network">A rede interpretada.</param>
  /// <returns>Verdadeiro quando a entrada é um bloco CIDR com o endereço da rede.</returns>
  private static bool TryParseNetwork(string? entry, out IPNetwork network)
  {
    network = default;

    var value = (entry ?? string.Empty).AsSpan().Trim();
    var slash = value.IndexOf('/');

    if (slash <= 0
      || !IPAddress.TryParse(value[..slash], out var address)
      || !int.TryParse(value[(slash + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var prefix))
    {
      return false;
    }

    Span<byte> bytes = stackalloc byte[16];
    address.TryWriteBytes(bytes, out var length);

    if (prefix > length * 8)
    {
      return false;
    }

    // Nenhum bit ligado depois do prefixo
    for (var bit = prefix; bit < length * 8; bit++)
    {
      if ((bytes[bit / 8] & (0x80 >> (bit % 8))) != 0)
      {
        return false;
      }
    }

    network = new IPNetwork(address, prefix);

    return true;
  }

  /// <summary>
  /// Prepara a entrada inválida para ir como parâmetro da mensagem.
  /// </summary>
  /// <param name="entry">A entrada da lista.</param>
  /// <returns>A entrada sem o separador de parâmetros da mensagem.</returns>
  private static string Printable(string? entry) => (entry ?? string.Empty).Replace(';', ' ').Trim();

  #endregion
}
