using Tooark.Exceptions;
using Tooark.Http.Options;

namespace Tooark.Tests.Http.Options;

/// <summary>
/// Testes da validação das opções do cliente HTTP e da guarda de saída.
/// </summary>
public class HttpClientOptionsTests
{
  // Testa se as opções padrão são válidas e têm os valores documentados
  [Fact]
  public void Defaults_ShouldBeValid()
  {
    // Arrange
    var options = new HttpClientOptions();

    // Act
    options.Validate();

    // Assert
    Assert.Equal("HttpClient", HttpClientOptions.Section);
    Assert.Equal("HttpClients", HttpClientOptions.NamedSection);
    Assert.Null(options.BaseAddress);
    Assert.Equal(30, options.TotalTimeoutSeconds);
    Assert.Equal(10, options.AttemptTimeoutSeconds);
    Assert.Equal(3, options.RetryCount);
    // Na v5 POST e PATCH não são tentados de novo por padrão; na 4.x eram
    Assert.False(options.RetryNonIdempotentMethods);
    Assert.Equal(2000, options.RetryDelayMilliseconds);
    Assert.Equal(0.1, options.CircuitBreakerFailureRatio);
    Assert.Equal(100, options.CircuitBreakerMinimumThroughput);
    Assert.Equal(30, options.CircuitBreakerSamplingSeconds);
    Assert.Equal(5, options.CircuitBreakerBreakSeconds);
    Assert.True(options.Resilience);
    Assert.False(options.OutboundGuard.Enabled);
    Assert.Empty(options.OutboundGuard.AllowedHosts);
    Assert.Empty(options.OutboundGuard.AllowedNetworks);
  }

  // Testa se o endereço base precisa ser uma URL absoluta http ou https, sem credenciais, consulta ou fragmento
  [Theory]
  [InlineData("api.exemplo.com")]
  [InlineData("/v1")]
  [InlineData("ftp://api.exemplo.com")]
  [InlineData("https://usuario:senha@api.exemplo.com")]
  [InlineData("https://api.exemplo.com/v1?chave=1")]
  [InlineData("https://api.exemplo.com/v1#topo")]
  public void Validate_ShouldRejectInvalidBaseAddress(string baseAddress)
  {
    // Arrange
    var options = new HttpClientOptions { BaseAddress = baseAddress };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert: o valor não vai na mensagem, porque pode carregar uma credencial
    Assert.Equal("Options.HttpClient.BaseAddressInvalid", Assert.Single(ex.GetErrorMessages()));
  }

  // Testa se o endereço base em branco equivale a nenhum e se o válido é aceito
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("http://localhost:8080")]
  [InlineData("https://api.exemplo.com/v1/")]
  public void Validate_ShouldAcceptBlankOrAbsoluteBaseAddress(string? baseAddress)
  {
    // Arrange
    var options = new HttpClientOptions { BaseAddress = baseAddress };

    // Act & Assert
    options.Validate();
  }

  // Combinações inválidas e a chave que cada uma lança
  public static TheoryData<Action<HttpClientOptions>, string> Invalidas => new()
  {
    { o => o.TotalTimeoutSeconds = 0, "Options.HttpClient.TotalTimeoutInvalid;1;3600" },
    { o => o.TotalTimeoutSeconds = 3601, "Options.HttpClient.TotalTimeoutInvalid;1;3600" },
    { o => o.AttemptTimeoutSeconds = 0, "Options.HttpClient.AttemptTimeoutInvalid;1;30" },
    // Tentativa mais longa que a chamada inteira, que o handler de resiliência recusa
    { o => o.AttemptTimeoutSeconds = 31, "Options.HttpClient.AttemptTimeoutInvalid;1;30" },
    { o => { o.TotalTimeoutSeconds = 5; o.AttemptTimeoutSeconds = 6; }, "Options.HttpClient.AttemptTimeoutInvalid;1;5" },
    { o => o.RetryCount = -1, "Options.HttpClient.RetryCountInvalid;0;10" },
    { o => o.RetryCount = 11, "Options.HttpClient.RetryCountInvalid;0;10" },
    { o => o.RetryDelayMilliseconds = -1, "Options.HttpClient.RetryDelayInvalid;0;60000" },
    { o => o.RetryDelayMilliseconds = 60001, "Options.HttpClient.RetryDelayInvalid;0;60000" },
    { o => o.CircuitBreakerFailureRatio = -0.01, "Options.HttpClient.CircuitBreakerFailureRatioInvalid" },
    { o => o.CircuitBreakerFailureRatio = 1.01, "Options.HttpClient.CircuitBreakerFailureRatioInvalid" },
    { o => o.CircuitBreakerFailureRatio = double.NaN, "Options.HttpClient.CircuitBreakerFailureRatioInvalid" },
    { o => o.CircuitBreakerMinimumThroughput = 1, "Options.HttpClient.CircuitBreakerMinimumThroughputInvalid;2" },
    // Janela menor que o dobro do tempo limite por tentativa, que o handler de resiliência recusa
    { o => o.CircuitBreakerSamplingSeconds = 19, "Options.HttpClient.CircuitBreakerSamplingInvalid;20;86400" },
    { o => { o.AttemptTimeoutSeconds = 20; }, "Options.HttpClient.CircuitBreakerSamplingInvalid;40;86400" },
    { o => o.CircuitBreakerSamplingSeconds = 86401, "Options.HttpClient.CircuitBreakerSamplingInvalid;20;86400" },
    { o => o.CircuitBreakerBreakSeconds = 0, "Options.HttpClient.CircuitBreakerBreakInvalid;1;86400" },
    { o => o.CircuitBreakerBreakSeconds = 86401, "Options.HttpClient.CircuitBreakerBreakInvalid;1;86400" },
    { o => o.OutboundGuard.AllowedNetworks = ["10.0.0.0/33"], "Options.HttpClient.OutboundGuard.AllowedNetworkInvalid;10.0.0.0/33" },
    { o => o.OutboundGuard.AllowedHosts = ["*.exemplo.com"], "Options.HttpClient.OutboundGuard.AllowedHostInvalid;*.exemplo.com" }
  };

  // Testa se cada valor fora da faixa ou combinação recusada pelo handler de resiliência falha com a chave própria
  [Theory]
  [MemberData(nameof(Invalidas))]
  public void Validate_ShouldThrowWithKey_WhenOptionIsInvalid(Action<HttpClientOptions> alterar, string chave)
  {
    // Arrange
    var options = new HttpClientOptions();
    alterar(options);

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert
    Assert.Equal(chave, Assert.Single(ex.GetErrorMessages()));
  }

  // Testa se a tentativa pode durar o mesmo que a chamada inteira, o que o handler de resiliência aceita
  [Fact]
  public void Validate_ShouldAcceptAttemptTimeoutEqualToTotal()
  {
    // Arrange
    var options = new HttpClientOptions { TotalTimeoutSeconds = 15, AttemptTimeoutSeconds = 15 };

    // Act & Assert
    options.Validate();
  }

  // Testa se uma rede liberada precisa ser um bloco CIDR com o endereço da rede
  [Theory]
  [InlineData("")]
  [InlineData("  ")]
  [InlineData("10.0.0.1")]
  [InlineData("10.0.0.1/8")]
  [InlineData("10.0.0.0/33")]
  [InlineData("fd00::/129")]
  [InlineData("interna.exemplo.com/24")]
  [InlineData("10.0.0.0/8;192.168.0.0/16")]
  public void OutboundGuardValidate_ShouldRejectInvalidNetwork(string rede)
  {
    // Arrange
    var options = new OutboundGuardOptions { AllowedNetworks = [rede] };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert
    Assert.StartsWith("Options.HttpClient.OutboundGuard.AllowedNetworkInvalid;", Assert.Single(ex.GetErrorMessages()));
  }

  // Testa se um host liberado precisa ser um nome, sem esquema, porta, caminho, curinga ou endereço IP
  [Theory]
  [InlineData("")]
  [InlineData(".")]
  [InlineData("https://interna.exemplo.com")]
  [InlineData("interna.exemplo.com:8080")]
  [InlineData("interna.exemplo.com/caminho")]
  [InlineData("*.exemplo.com")]
  [InlineData("10.0.0.1")]
  [InlineData("::1")]
  [InlineData("nome com espaço")]
  public void OutboundGuardValidate_ShouldRejectInvalidHost(string host)
  {
    // Arrange
    var options = new OutboundGuardOptions { AllowedHosts = [host] };

    // Act
    var ex = Assert.Throws<InternalServerErrorException>(options.Validate);

    // Assert
    Assert.StartsWith("Options.HttpClient.OutboundGuard.AllowedHostInvalid;", Assert.Single(ex.GetErrorMessages()));
  }

  // Testa se as listas válidas da guarda são aceitas
  [Fact]
  public void OutboundGuardValidate_ShouldAcceptValidLists()
  {
    // Arrange
    var options = new OutboundGuardOptions
    {
      AllowedHosts = ["api.interna.exemplo.com", ".interna.exemplo.com", "LOCALHOST", " servico "],
      AllowedNetworks = ["10.20.0.0/16", "192.168.1.7/32", "fd00:1::/64", "::1/128", " 127.0.0.0/8 "]
    };

    // Act & Assert
    options.Validate();
  }
}
