using System.Net;
using Tooark.Http;
using Tooark.Http.Internal;
using Tooark.Http.Options;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes da regra de bloqueio da guarda de saída e das listas de exceção.
/// </summary>
public class OutboundGuardTests
{
  // Monta a regra a partir das listas
  private static OutboundGuardPolicy Regra(string[]? hosts = null, string[]? redes = null) =>
    OutboundGuardPolicy.Create(new OutboundGuardOptions { AllowedHosts = hosts ?? [], AllowedNetworks = redes ?? [] });

  // Testa cada faixa IPv4 bloqueada, com o primeiro e o último endereço de cada uma
  [Theory]
  // "Esta rede" 0.0.0.0/8
  [InlineData("0.0.0.0")]
  [InlineData("0.255.255.255")]
  // Privada 10.0.0.0/8
  [InlineData("10.0.0.0")]
  [InlineData("10.255.255.255")]
  // CGNAT 100.64.0.0/10
  [InlineData("100.64.0.0")]
  [InlineData("100.127.255.255")]
  // Loopback 127.0.0.0/8
  [InlineData("127.0.0.0")]
  [InlineData("127.0.0.1")]
  [InlineData("127.255.255.255")]
  // Link-local 169.254.0.0/16, com o endereço de metadados das nuvens
  [InlineData("169.254.0.0")]
  [InlineData("169.254.169.254")]
  [InlineData("169.254.255.255")]
  // Privada 172.16.0.0/12
  [InlineData("172.16.0.0")]
  [InlineData("172.31.255.255")]
  // Atribuições de protocolo 192.0.0.0/24
  [InlineData("192.0.0.0")]
  [InlineData("192.0.0.255")]
  // Documentação 192.0.2.0/24, 198.51.100.0/24 e 203.0.113.0/24
  [InlineData("192.0.2.0")]
  [InlineData("192.0.2.255")]
  [InlineData("198.51.100.0")]
  [InlineData("198.51.100.255")]
  [InlineData("203.0.113.0")]
  [InlineData("203.0.113.255")]
  // Privada 192.168.0.0/16
  [InlineData("192.168.0.0")]
  [InlineData("192.168.255.255")]
  // Testes de desempenho 198.18.0.0/15
  [InlineData("198.18.0.0")]
  [InlineData("198.19.255.255")]
  // Multicast 224.0.0.0/4
  [InlineData("224.0.0.0")]
  [InlineData("239.255.255.255")]
  // Reservado 240.0.0.0/4 e broadcast
  [InlineData("240.0.0.0")]
  [InlineData("255.255.255.254")]
  [InlineData("255.255.255.255")]
  public void IsBlocked_ShouldBlockIPv4Ranges(string endereco)
  {
    // Act & Assert
    Assert.True(OutboundGuard.IsBlocked(IPAddress.Parse(endereco)));
  }

  // Testa o endereço público imediatamente antes e depois de cada faixa IPv4 bloqueada
  [Theory]
  [InlineData("1.0.0.0")]
  [InlineData("9.255.255.255")]
  [InlineData("11.0.0.0")]
  [InlineData("100.63.255.255")]
  [InlineData("100.128.0.0")]
  [InlineData("126.255.255.255")]
  [InlineData("128.0.0.0")]
  [InlineData("169.253.255.255")]
  [InlineData("169.255.0.0")]
  [InlineData("172.15.255.255")]
  [InlineData("172.32.0.0")]
  [InlineData("191.255.255.255")]
  [InlineData("192.0.1.0")]
  [InlineData("192.0.1.255")]
  [InlineData("192.0.3.0")]
  [InlineData("192.167.255.255")]
  [InlineData("192.169.0.0")]
  [InlineData("198.17.255.255")]
  [InlineData("198.20.0.0")]
  [InlineData("198.51.99.255")]
  [InlineData("198.51.101.0")]
  [InlineData("203.0.112.255")]
  [InlineData("203.0.114.0")]
  [InlineData("223.255.255.255")]
  [InlineData("8.8.8.8")]
  public void IsBlocked_ShouldAllowPublicIPv4(string endereco)
  {
    // Act & Assert
    Assert.False(OutboundGuard.IsBlocked(IPAddress.Parse(endereco)));
  }

  // Testa cada faixa IPv6 bloqueada, com o primeiro e o último endereço de cada uma
  [Theory]
  // Não especificado, loopback e IPv4-compatíveis ::/96
  [InlineData("::")]
  [InlineData("::1")]
  [InlineData("::8.8.8.8")]
  [InlineData("::ffff:ffff")]
  // NAT64 de uso local 64:ff9b:1::/48
  [InlineData("64:ff9b:1::")]
  [InlineData("64:ff9b:1:ffff:ffff:ffff:ffff:ffff")]
  // Descarte 100::/64
  [InlineData("100::")]
  [InlineData("100::ffff:ffff:ffff:ffff")]
  // Documentação 2001:db8::/32
  [InlineData("2001:db8::")]
  [InlineData("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff")]
  // Locais únicos fc00::/7
  [InlineData("fc00::")]
  [InlineData("fd12:3456:789a::1")]
  [InlineData("fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
  // Link-local fe80::/10
  [InlineData("fe80::")]
  [InlineData("fe80::1%1")]
  [InlineData("febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
  // Site-local fec0::/10
  [InlineData("fec0::")]
  [InlineData("feff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
  // Multicast ff00::/8
  [InlineData("ff00::")]
  [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
  public void IsBlocked_ShouldBlockIPv6Ranges(string endereco)
  {
    // Act & Assert
    Assert.True(OutboundGuard.IsBlocked(IPAddress.Parse(endereco)));
  }

  // Testa o endereço público imediatamente antes e depois de cada faixa IPv6 bloqueada
  [Theory]
  [InlineData("::1:0:0")]
  [InlineData("64:ff9b:0:ffff:ffff:ffff:ffff:ffff")]
  [InlineData("64:ff9b:2::")]
  [InlineData("ff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
  [InlineData("100:0:0:1::")]
  [InlineData("2001:db7:ffff:ffff:ffff:ffff:ffff:ffff")]
  [InlineData("2001:db9::")]
  [InlineData("fbff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
  [InlineData("fe00::")]
  [InlineData("fe7f:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
  [InlineData("2606:4700:4700::1111")]
  [InlineData("2a00:1450:4001:81b::200e")]
  public void IsBlocked_ShouldAllowPublicIPv6(string endereco)
  {
    // Act & Assert
    Assert.False(OutboundGuard.IsBlocked(IPAddress.Parse(endereco)));
  }

  // Testa se o IPv6 que carrega um IPv4 (mapeado, NAT64, 6to4 e Teredo) é classificado pelo IPv4 que carrega
  [Theory]
  // Mapeado ::ffff:a.b.c.d
  [InlineData("::ffff:127.0.0.1", true)]
  [InlineData("::ffff:10.0.0.1", true)]
  [InlineData("::ffff:169.254.169.254", true)]
  [InlineData("::ffff:192.168.1.1", true)]
  [InlineData("::ffff:8.8.8.8", false)]
  [InlineData("::ffff:11.0.0.0", false)]
  // NAT64 64:ff9b::/96
  [InlineData("64:ff9b::127.0.0.1", true)]
  [InlineData("64:ff9b::10.0.0.1", true)]
  [InlineData("64:ff9b::a9fe:a9fe", true)]
  [InlineData("64:ff9b::8.8.8.8", false)]
  [InlineData("64:ff9b::ffff:ffff", true)]
  [InlineData("64:ff9b::", true)]
  // 6to4 2002::/16, com o IPv4 nos bits 16 a 47
  [InlineData("2002:7f00:1::", true)]
  [InlineData("2002:a00:1::1", true)]
  [InlineData("2002:c0a8:101::", true)]
  [InlineData("2002:808:808::", false)]
  [InlineData("2002:ffff:ffff:ffff:ffff:ffff:ffff:ffff", true)]
  [InlineData("2001:ffff:ffff:ffff:ffff:ffff:ffff:ffff", false)]
  [InlineData("2003::", false)]
  // Teredo 2001::/32, com o IPv4 do cliente invertido nos últimos 32 bits
  [InlineData("2001:0:4136:e378:8000:63bf:80ff:fffe", true)]
  [InlineData("2001:0:4136:e378:8000:63bf:f5ff:fffe", true)]
  [InlineData("2001:0:4136:e378:8000:63bf:f7f7:f7f7", false)]
  [InlineData("2001:1::", false)]
  public void IsBlocked_ShouldClassifyEmbeddedIPv4(string endereco, bool bloqueado)
  {
    // Act & Assert
    Assert.Equal(bloqueado, OutboundGuard.IsBlocked(IPAddress.Parse(endereco)));
  }

  // Testa se o endereço nulo é erro de quem chama
  [Fact]
  public void IsBlocked_ShouldThrow_WhenAddressIsNull()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => OutboundGuard.IsBlocked(null!));
  }

  // Testa se uma rede liberada tira o endereço da lista de bloqueio, e só os endereços dela
  [Theory]
  [InlineData("127.0.0.0/8", "127.0.0.1", false)]
  [InlineData("127.0.0.0/8", "10.0.0.1", true)]
  [InlineData("10.20.0.0/16", "10.20.255.255", false)]
  [InlineData("10.20.0.0/16", "10.21.0.0", true)]
  [InlineData("10.20.0.0/16", "10.19.255.255", true)]
  [InlineData("192.168.1.7/32", "192.168.1.7", false)]
  [InlineData("192.168.1.7/32", "192.168.1.8", true)]
  [InlineData("fd00:1::/64", "fd00:1::abcd", false)]
  [InlineData("fd00:1::/64", "fd00:2::1", true)]
  [InlineData("::1/128", "::1", false)]
  [InlineData("::1/128", "::", true)]
  // O IPv4 mapeado em IPv6 é o mesmo destino, então a rede IPv4 liberada vale para ele
  [InlineData("127.0.0.0/8", "::ffff:127.0.0.1", false)]
  [InlineData("10.20.0.0/16", "::ffff:10.21.0.1", true)]
  // NAT64 e 6to4 são bloqueados pelo IPv4 que carregam, mas a rede IPv4 liberada não os libera
  [InlineData("10.0.0.0/8", "64:ff9b::10.0.0.1", true)]
  [InlineData("10.0.0.0/8", "2002:a00:1::1", true)]
  [InlineData("64:ff9b::/96", "64:ff9b::10.0.0.1", false)]
  public void Policy_ShouldExemptAllowedNetworks(string rede, string endereco, bool bloqueado)
  {
    // Arrange
    var regra = Regra(redes: [rede]);

    // Act & Assert
    Assert.Equal(bloqueado, regra.IsBlocked(IPAddress.Parse(endereco)));
  }

  // Testa se um endereço público continua liberado com ou sem redes na lista
  [Fact]
  public void Policy_ShouldNotBlockPublicAddress()
  {
    // Act & Assert
    Assert.False(Regra().IsBlocked(IPAddress.Parse("8.8.8.8")));
    Assert.False(Regra(redes: ["10.0.0.0/8"]).IsBlocked(IPAddress.Parse("8.8.8.8")));
  }

  // Testa a liberação de host por nome exato e por sufixo, sem diferenciar maiúsculas
  [Theory]
  [InlineData("api.interna.exemplo.com", "api.interna.exemplo.com", true)]
  [InlineData("api.interna.exemplo.com", "API.Interna.Exemplo.COM", true)]
  [InlineData("api.interna.exemplo.com", "api.interna.exemplo.com.", true)]
  [InlineData("api.interna.exemplo.com", "outra.interna.exemplo.com", false)]
  [InlineData("api.interna.exemplo.com", "x.api.interna.exemplo.com", false)]
  [InlineData(".interna.exemplo.com", "api.interna.exemplo.com", true)]
  [InlineData(".interna.exemplo.com", "a.b.interna.exemplo.com", true)]
  [InlineData(".INTERNA.exemplo.com", "api.interna.EXEMPLO.com", true)]
  // O sufixo libera os subdomínios, e não o próprio domínio
  [InlineData(".interna.exemplo.com", "interna.exemplo.com", false)]
  // O ponto do sufixo impede que outro domínio com o mesmo final passe
  [InlineData(".interna.exemplo.com", "falsainterna.exemplo.com", false)]
  [InlineData(".interna.exemplo.com", "interna.exemplo.com.atacante.net", false)]
  [InlineData("exemplo.com", "falsoexemplo.com", false)]
  // Nome internacional comparado na forma ASCII, a que a URI entrega
  [InlineData("münchen.exemplo.com", "xn--mnchen-3ya.exemplo.com", true)]
  public void Policy_ShouldMatchAllowedHosts(string liberado, string host, bool esperado)
  {
    // Arrange
    var regra = Regra(hosts: [liberado]);

    // Act & Assert
    Assert.Equal(esperado, regra.IsHostAllowed(host));
  }
}
