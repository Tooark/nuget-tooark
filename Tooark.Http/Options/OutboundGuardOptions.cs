using Tooark.Exceptions;
using Tooark.Http.Internal;

namespace Tooark.Http.Options;

/// <summary>
/// Classe que representa as opções da guarda de saída, que impede uma URL informada pelo usuário de alcançar a rede
/// interna (SSRF).
/// </summary>
/// <remarks>
/// A guarda confere o endereço IP no momento da conexão, depois da resolução de DNS, e não o texto da URL: um nome
/// público que resolve para um endereço interno é recusado do mesmo jeito. As listas abaixo abrem exceções.
/// </remarks>
public class OutboundGuardOptions
{
  #region Properties

  /// <summary>
  /// Indica se a guarda está ligada. Padrão: falso no cliente do <c>AddTooarkHttpClient</c>, que em geral chama
  /// serviços internos em endereços privados; verdadeiro no <c>AddTooarkOutboundGuard</c>.
  /// </summary>
  public bool Enabled { get; set; }

  /// <summary>
  /// Nomes de host liberados da conferência de endereço. Padrão: nenhum.
  /// </summary>
  /// <remarks>
  /// Um nome exato (<c>api.interna.exemplo.com</c>) ou um sufixo iniciado por ponto (<c>.interna.exemplo.com</c>),
  /// que libera os subdomínios e não o próprio domínio. A comparação ignora maiúsculas e minúsculas. Endereço IP
  /// não entra aqui: use <see cref="AllowedNetworks"/>.
  /// </remarks>
  public string[] AllowedHosts { get; set; } = [];

  /// <summary>
  /// Redes liberadas da lista de bloqueio, em notação CIDR, IPv4 ou IPv6 (<c>10.20.0.0/16</c>, <c>fd00:1::/64</c>).
  /// Padrão: nenhuma.
  /// </summary>
  /// <remarks>
  /// Um endereço único é escrito com <c>/32</c> (IPv4) ou <c>/128</c> (IPv6). O endereço base precisa ser o da rede:
  /// <c>10.20.0.1/16</c> é recusado.
  /// </remarks>
  public string[] AllowedNetworks { get; set; } = [];

  /// <summary>
  /// Com um proxy de saída, deixa passar o nome de host que a aplicação não consegue resolver. Padrão: falso.
  /// </summary>
  /// <remarks>
  /// Com um proxy, quem abre a conexão com o destino é o proxy, e a guarda confere o nome resolvendo-o pelo DNS da
  /// própria aplicação antes de enviar a requisição. Quando o nome não resolve, a guarda não tem o que conferir: por
  /// padrão a requisição falha como uma falha de conexão, sem chegar ao proxy. Ligue esta opção só em redes em que a
  /// aplicação não tem DNS para nomes externos e depende do proxy para resolvê-los; nesse caso os nomes que ela não
  /// resolve passam sem conferência, e o bloqueio das faixas internas fica por conta das regras do proxy.
  /// </remarks>
  public bool AllowUnresolvedHostsBehindProxy { get; set; }

  #endregion

  #region Validate

  /// <summary>
  /// Valida as listas de exceção da guarda.
  /// </summary>
  /// <exception cref="InternalServerErrorException">Quando um host ou uma rede da lista é inválido.</exception>
  public void Validate() => _ = OutboundGuardPolicy.Create(this);

  #endregion
}
