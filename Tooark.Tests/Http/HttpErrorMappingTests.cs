using System.Net;
using System.Text;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;
using Tooark.Exceptions;
using Tooark.Http.Exceptions;
using Tooark.Http.Internal;
using Tooark.Notifications;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes da conversão das falhas de uma chamada HTTP nas exceções do Tooark.
/// </summary>
public class HttpErrorMappingTests
{
  // Corpo lido das respostas
  private sealed record Recibo(string Protocolo);

  // URI usada nas chamadas
  private const string Endereco = "https://api.exemplo.com/recibos/1";

  // Problem Details do Tooark com dois erros
  private const string ProblemaComDoisErros = """
    {
      "status": 400,
      "title": "Bad Request",
      "detail": "O campo Email é obrigatório",
      "errors": [
        { "code": "T.VLD.STR1", "key": "Email", "message": "O campo Email é obrigatório" },
        { "code": "T.VLD.NUM3", "key": "Idade", "message": "O campo Idade deve ser maior que 18" }
      ]
    }
    """;

  // Conteúdo cuja leitura nunca termina: os cabeçalhos chegam, o corpo fica parado até o cancelamento
  private sealed class CorpoParado : HttpContent
  {
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
      SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
      Task.Delay(Timeout.Infinite, cancellationToken);

    protected override bool TryComputeLength(out long length)
    {
      length = 0;

      return false;
    }
  }

  // Conteúdo cuja leitura falha no meio, como uma conexão que cai depois dos cabeçalhos
  private sealed class CorpoQuebrado : HttpContent
  {
    public CorpoQuebrado() => Headers.ContentType = new("application/problem+json");

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
      throw new IOException("conexão encerrada");

    protected override bool TryComputeLength(out long length)
    {
      length = 0;

      return false;
    }
  }

  // Envia um GET pelo serviço montado com o handler falso
  private static async Task<Exception> FalhaAsync(FakeHttpMessageHandler handler, Action<Tooark.Http.Options.HttpClientOptions>? configure = null)
  {
    using var provider = HttpTestHost.WithHandler(handler, configure);

    return await Assert.ThrowsAnyAsync<Exception>(() =>
      provider.Service().GetAsync<Recibo>(Endereco, TestContext.Current.CancellationToken));
  }

  // Testa se um status de erro vira HttpRequestFailedException com o status original e a resposta da aplicação em 502
  [Theory]
  [InlineData(HttpStatusCode.BadRequest)]
  [InlineData(HttpStatusCode.Unauthorized)]
  [InlineData(HttpStatusCode.NotFound)]
  [InlineData(HttpStatusCode.Conflict)]
  public async Task ErrorStatus_ShouldThrowHttpRequestFailed_WithUpstreamStatus(HttpStatusCode status)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(status, "segredo interno do serviço", "text/plain"));

    // Act
    var ex = Assert.IsType<HttpRequestFailedException>(await FalhaAsync(handler));

    // Assert
    Assert.IsAssignableFrom<BadGatewayException>(ex);
    Assert.Equal(HttpStatusCode.BadGateway, ex.GetStatusCode());
    Assert.Equal(status, ex.UpstreamStatusCode);
    Assert.Equal(Endereco, ex.RequestUri?.AbsoluteUri);

    var item = Assert.Single(ex.GetNotifications());
    Assert.Equal($"Http.RequestFailed;{(int)status}", item.Message);
    Assert.DoesNotContain("segredo", ex.Message);
    Assert.Equal(1, handler.Calls);
  }

  // Testa se o Problem Details do Tooark é repassado, um item de notificação por erro, com código e chave originais
  [Fact]
  public async Task ErrorStatus_ShouldPassThroughTooarkProblemDetails()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ =>
      FakeHttpMessageHandler.Json(HttpStatusCode.BadRequest, ProblemaComDoisErros, "application/problem+json"));

    // Act
    var ex = Assert.IsType<HttpRequestFailedException>(await FalhaAsync(handler));

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, ex.UpstreamStatusCode);
    Assert.Equal(HttpStatusCode.BadGateway, ex.GetStatusCode());

    var itens = ex.GetNotifications();
    Assert.Equal(2, itens.Count);
    Assert.Equal(("T.VLD.STR1", "Email", "O campo Email é obrigatório"), (itens[0].Code, itens[0].Key, itens[0].Message));
    Assert.Equal(("T.VLD.NUM3", "Idade", "O campo Idade deve ser maior que 18"), (itens[1].Code, itens[1].Key, itens[1].Message));
    Assert.Equal(new[] { "O campo Email é obrigatório", "O campo Idade deve ser maior que 18" }, ex.GetErrorMessages());
  }

  // Testa se um corpo que não é o Problem Details do Tooark cai na mensagem padrão
  [Theory]
  // Lista de erros com o tipo de mídia errado
  [InlineData("application/json", """{"errors":[{"code":"C","key":"K","message":"M"}]}""")]
  // Problem Details de validação do ASP.NET Core: errors é um objeto
  [InlineData("application/problem+json", """{"errors":{"Email":["obrigatório"]}}""")]
  // Lista vazia
  [InlineData("application/problem+json", """{"errors":[]}""")]
  // Itens sem mensagem
  [InlineData("application/problem+json", """{"errors":[{"code":"C","key":"K"},{"message":"  "},"texto",5]}""")]
  // Sem a lista
  [InlineData("application/problem+json", """{"title":"Erro","detail":"segredo"}""")]
  // JSON inválido
  [InlineData("application/problem+json", """{"errors":[{"message":"M"}""")]
  // Raiz que não é objeto
  [InlineData("application/problem+json", """["errors"]""")]
  // Sem corpo
  [InlineData("application/problem+json", "")]
  public async Task ErrorStatus_ShouldFallBackToDefaultMessage_WhenBodyIsNotTooarkProblemDetails(string mediaType, string corpo)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.UnprocessableEntity, corpo, mediaType));

    // Act
    var ex = Assert.IsType<HttpRequestFailedException>(await FalhaAsync(handler));

    // Assert
    Assert.Equal("Http.RequestFailed;422", Assert.Single(ex.GetNotifications()).Message);
  }

  // Testa se um corpo de erro acima do limite é ignorado, em vez de lido por inteiro
  [Fact]
  public async Task ErrorStatus_ShouldIgnoreBodyAboveTheLimit()
  {
    // Arrange: um Problem Details válido, mas maior que o limite de leitura
    var mensagem = new string('x', HttpErrorMapper.MaximumErrorBodyBytes);
    var corpo = $$"""{"errors":[{"code":"C","key":"K","message":"{{mensagem}}"}]}""";
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.BadRequest, corpo, "application/problem+json"));

    // Act
    var ex = Assert.IsType<HttpRequestFailedException>(await FalhaAsync(handler));

    // Assert
    Assert.Equal("Http.RequestFailed;400", Assert.Single(ex.GetNotifications()).Message);
  }

  // Testa se a conexão que cai durante a leitura do corpo de erro não esconde o status que já chegou
  [Fact]
  public async Task ErrorStatus_ShouldKeepUpstreamStatus_WhenErrorBodyCannotBeRead()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new CorpoQuebrado() });

    // Act
    var ex = Assert.IsType<HttpRequestFailedException>(await FalhaAsync(handler));

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, ex.UpstreamStatusCode);
    Assert.Equal("Http.RequestFailed;403", Assert.Single(ex.GetNotifications()).Message);
  }

  // Testa se a quantidade de erros e o tamanho de cada texto repassado têm limite
  [Fact]
  public void ParseProblem_ShouldCapEntriesAndTextLength()
  {
    // Arrange
    var longa = new string('m', HttpErrorMapper.MaximumErrorTextLength + 50);
    var erros = string.Join(",", Enumerable.Range(0, HttpErrorMapper.MaximumErrors + 10).Select(i => $$"""{"key":"K{{i}}","message":"M{{i}}"}"""));
    var corpo = Encoding.UTF8.GetBytes($$"""{"errors":[{"message":"{{longa}}"},{{erros}}]}""");

    // Act
    var itens = HttpErrorMapper.ParseProblem(corpo);

    // Assert
    Assert.Equal(HttpErrorMapper.MaximumErrors, itens.Count);
    Assert.Equal(HttpErrorMapper.MaximumErrorTextLength, itens[0].Message.Length);
    // Sem código e sem chave, valem os padrões do item de notificação
    Assert.Equal(("T.ERR", "Unknown"), (itens[0].Code, itens[0].Key));
    Assert.Equal(("T.ERR", "K0", "M0"), (itens[1].Code, itens[1].Key, itens[1].Message));
  }

  // Testa se os construtores públicos da exceção montam a mensagem padrão e preservam os itens
  [Fact]
  public void HttpRequestFailedException_ShouldBuildDefaultMessageAndKeepItems()
  {
    // Arrange
    var uri = new Uri(Endereco);
    NotificationItem[] itens = [new("Mensagem", "Campo", "T.X")];

    // Act
    var padrao = new HttpRequestFailedException(HttpStatusCode.ServiceUnavailable);
    var vazia = new HttpRequestFailedException(HttpStatusCode.NotFound, uri, []);
    var comItens = new HttpRequestFailedException(HttpStatusCode.Conflict, uri, itens);

    // Assert
    Assert.Equal("Http.RequestFailed;503", padrao.Message);
    Assert.Null(padrao.RequestUri);
    Assert.Equal("Http.RequestFailed;404", Assert.Single(vazia.GetNotifications()).Message);
    Assert.Same(itens[0], Assert.Single(comItens.GetNotifications()));
    Assert.Equal(HttpStatusCode.Conflict, comItens.UpstreamStatusCode);
    Assert.Same(uri, comItens.RequestUri);
  }

  // Testa se o tempo limite do Polly e o cancelamento que não é de quem chamou viram 504
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task Timeout_ShouldThrowGatewayTimeout(bool rejeitadoPeloPolly)
  {
    // Arrange
    Exception falha = rejeitadoPeloPolly ? new TimeoutRejectedException() : new TaskCanceledException();
    var handler = new FakeHttpMessageHandler(_ => throw falha);

    // Act
    var ex = Assert.IsType<GatewayTimeoutException>(await FalhaAsync(handler));

    // Assert
    Assert.Contains("Http.RequestTimeout", ex.GetErrorMessages());
    Assert.Same(falha, ex.InnerException);
  }

  // Testa se a tentativa que não responde é cortada pelo tempo limite por tentativa
  [Fact]
  public async Task Timeout_ShouldThrowGatewayTimeout_WhenAttemptDoesNotRespond()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(async (_, token) =>
    {
      await Task.Delay(Timeout.Infinite, token);

      return FakeHttpMessageHandler.Empty(HttpStatusCode.OK);
    });

    // Act
    var ex = await FalhaAsync(handler, options =>
    {
      options.TotalTimeoutSeconds = 30;
      options.AttemptTimeoutSeconds = 1;
      options.CircuitBreakerSamplingSeconds = 30;
      options.RetryCount = 0;
    });

    // Assert
    var timeout = Assert.IsType<GatewayTimeoutException>(ex);
    Assert.Contains("Http.RequestTimeout", timeout.GetErrorMessages());
    Assert.IsType<TimeoutRejectedException>(timeout.InnerException);
  }

  // Testa se o corpo que para de chegar depois dos cabeçalhos é cortado pelo tempo limite total, com e sem resiliência
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task Timeout_ShouldThrowGatewayTimeout_WhenBodyStalls(bool resiliencia)
  {
    // Arrange: os tempos limite do handler de resiliência terminam nos cabeçalhos; o corpo é do serviço
    var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new CorpoParado() });

    // Act
    var ex = await FalhaAsync(handler, options =>
    {
      options.Resilience = resiliencia;
      options.TotalTimeoutSeconds = 1;
      options.AttemptTimeoutSeconds = 1;
    });

    // Assert
    Assert.Contains("Http.RequestTimeout", Assert.IsType<GatewayTimeoutException>(ex).GetErrorMessages());
  }

  // Testa se o cancelamento de quem chamou segue como OperationCanceledException, sem virar exceção do Tooark
  [Fact]
  public async Task CallerCancellation_ShouldPropagateUntouched()
  {
    // Arrange
    using var cancelamento = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
    var chegou = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var handler = new FakeHttpMessageHandler(async (_, token) =>
    {
      chegou.SetResult();
      await Task.Delay(Timeout.Infinite, token);

      return FakeHttpMessageHandler.Empty(HttpStatusCode.OK);
    });
    using var provider = HttpTestHost.WithHandler(handler);

    // Act
    var chamada = provider.Service().GetAsync<Recibo>(Endereco, cancelamento.Token);
    await chegou.Task;
    await cancelamento.CancelAsync();
    var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => chamada);

    // Assert
    Assert.IsNotAssignableFrom<TooarkException>((object)ex);
    Assert.Equal(1, handler.Calls);
  }

  // Testa se o token já cancelado de quem chamou também segue como OperationCanceledException
  [Fact]
  public async Task CallerCancellation_ShouldPropagate_WhenTokenIsAlreadyCanceled()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Empty(HttpStatusCode.OK));
    using var provider = HttpTestHost.WithHandler(handler);

    // Act & Assert
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
      provider.Service().GetAsync<Recibo>(Endereco, new CancellationToken(canceled: true)));
  }

  // Testa se o circuito aberto vira 503
  [Fact]
  public async Task OpenCircuit_ShouldThrowServiceUnavailable()
  {
    // Arrange
    var falha = new BrokenCircuitException();
    var handler = new FakeHttpMessageHandler(_ => throw falha);

    // Act
    var ex = Assert.IsType<ServiceUnavailableException>(await FalhaAsync(handler));

    // Assert
    Assert.Contains("Http.CircuitOpen", ex.GetErrorMessages());
    Assert.Same(falha, ex.InnerException);
  }

  // Testa se a recusa do limite de requisições simultâneas vira 503 com chave própria
  [Fact]
  public async Task ConcurrencyLimit_ShouldThrowServiceUnavailable()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => throw new RateLimiterRejectedException());

    // Act
    var ex = Assert.IsType<ServiceUnavailableException>(await FalhaAsync(handler));

    // Assert
    Assert.Contains("Http.ConcurrencyLimitReached", ex.GetErrorMessages());
  }

  // Testa se a falha de conexão, sem resposta, vira 502 com a chave de conexão
  [Fact]
  public async Task ConnectionFailure_ShouldThrowBadGateway()
  {
    // Arrange
    var falha = new HttpRequestException("Connection refused (10.0.0.9:443)");
    var handler = new FakeHttpMessageHandler(_ => throw falha);

    // Act
    var ex = await FalhaAsync(handler, options => options.RetryCount = 0);

    // Assert
    var gateway = Assert.IsType<BadGatewayException>(ex);
    Assert.Contains("Http.ConnectionFailed", gateway.GetErrorMessages());
    Assert.DoesNotContain("10.0.0.9", gateway.Message);
    Assert.Same(falha, gateway.InnerException);
  }

  // Testa se a conexão que cai durante a leitura do corpo também vira a chave de conexão
  [Fact]
  public void Map_ShouldMapIOExceptionToConnectionFailed()
  {
    // Act
    var ex = HttpErrorMapper.Map(new IOException("fim inesperado"), CancellationToken.None);

    // Assert
    Assert.Contains("Http.ConnectionFailed", Assert.IsType<BadGatewayException>(ex).GetErrorMessages());
  }

  // Testa se o que não é falha da chamada HTTP segue sem conversão
  [Fact]
  public void Map_ShouldLeaveUnrelatedExceptionsUntouched()
  {
    // Act & Assert
    Assert.Null(HttpErrorMapper.Map(new InvalidOperationException(), CancellationToken.None));
    Assert.Null(HttpErrorMapper.Map(new BadRequestException("Qualquer"), CancellationToken.None));
    Assert.Null(HttpErrorMapper.Map(new OperationCanceledException(), new CancellationToken(canceled: true)));
  }

  // Testa se a recusa da guarda embrulhada pelo transporte volta a ser a exceção da guarda
  [Fact]
  public void Map_ShouldUnwrapOutboundBlocked()
  {
    // Arrange: é assim que o handler de conexão entrega a exceção lançada dentro do ConnectCallback
    var transporte = new HttpRequestException("bloqueado (interno.exemplo.com:80)", new OutboundBlockedException("interno.exemplo.com"));

    // Act
    var ex = HttpErrorMapper.Map(transporte, CancellationToken.None);

    // Assert
    var bloqueio = Assert.IsType<OutboundBlockedException>(ex);
    Assert.Equal("interno.exemplo.com", bloqueio.Host);
    Assert.Equal(HttpStatusCode.BadRequest, bloqueio.GetStatusCode());
    Assert.Contains("Http.OutboundBlocked;interno.exemplo.com", bloqueio.GetErrorMessages());
  }

  // Testa se o corpo de sucesso que não é o JSON esperado vira 502 com a chave de resposta inválida
  [Theory]
  [InlineData("<html>erro</html>")]
  [InlineData("""{"protocolo":""")]
  [InlineData("""["lista"]""")]
  [InlineData("   ")]
  public async Task InvalidJson_ShouldThrowBadGateway(string corpo)
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, corpo));

    // Act
    var ex = Assert.IsType<BadGatewayException>(await FalhaAsync(handler));

    // Assert
    Assert.Contains("Http.InvalidResponse", ex.GetErrorMessages());
    Assert.DoesNotContain("html", ex.Message);
  }

  // Testa se o SendAsync também converte as falhas de transporte
  [Fact]
  public async Task SendAsync_ShouldMapTransportFailures()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("sem rota"));
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryCount = 0);
    using var request = new HttpRequestMessage(HttpMethod.Get, Endereco);

    // Act
    var ex = await Assert.ThrowsAsync<BadGatewayException>(() =>
      provider.Service().SendAsync(request, TestContext.Current.CancellationToken));

    // Assert
    Assert.Contains("Http.ConnectionFailed", ex.GetErrorMessages());
  }
}
