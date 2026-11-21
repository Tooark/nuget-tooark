using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Tooark.Http.Exceptions;
using Tooark.Http.Internal;
using Tooark.Notifications;
using Tooark.Tests.Http.Support;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes do corpo da resposta de erro do serviço chamado, exposto na exceção sem entrar na mensagem.
/// </summary>
public class HttpUpstreamBodyTests
{
  // Corpo lido das respostas
  private sealed record Recibo(string Protocolo);

  // URI usada nas chamadas
  private const string Endereco = "https://api.exemplo.com/modelos/1";

  // Erro no formato de um provedor, que não é o Problem Details do Tooark
  private const string ErroDoProvedor = """{"error":{"type":"invalid_request_error","message":"model: modelo-inexistente"}}""";

  // Conteúdo cuja leitura falha no meio, como uma conexão que cai depois dos cabeçalhos
  private sealed class CorpoQuebrado : HttpContent
  {
    public CorpoQuebrado() => Headers.ContentType = new MediaTypeHeaderValue("application/json");

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
      throw new IOException("Connection reset");

    protected override bool TryComputeLength(out long length)
    {
      length = 0;

      return false;
    }
  }

  // Conteúdo sem tamanho declarado, entregue aos poucos: o limite só aparece durante a leitura
  private sealed class CorpoSemTamanho(byte[] bytes, string mediaType) : HttpContent
  {
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
      Headers.ContentType ??= new MediaTypeHeaderValue(mediaType);

      await stream.WriteAsync(bytes);
    }

    protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new MemoryStream(bytes));

    protected override bool TryComputeLength(out long length)
    {
      length = 0;

      return false;
    }
  }

  // Envia um GET que recebe a resposta de erro informada e devolve a exceção
  private static async Task<HttpRequestFailedException> FalharAsync(Func<HttpResponseMessage> resposta)
  {
    var handler = new FakeHttpMessageHandler(_ => resposta());
    using var provider = HttpTestHost.WithHandler(handler, options => options.RetryCount = 0);

    return await Assert.ThrowsAsync<HttpRequestFailedException>(() =>
      provider.Service().GetAsync<Recibo>(Endereco, TestContext.Current.CancellationToken));
  }

  // Testa se o erro de um provedor fica disponível como texto, fora da mensagem e das notificações
  [Fact]
  public async Task UpstreamBody_ShouldExposeTheProviderError_OutsideTheMessage()
  {
    // Arrange & Act
    var ex = await FalharAsync(() => FakeHttpMessageHandler.Json(HttpStatusCode.NotFound, ErroDoProvedor));

    // Assert
    Assert.Equal(ErroDoProvedor, ex.UpstreamBody);
    Assert.Equal(HttpStatusCode.NotFound, ex.UpstreamStatusCode);
    Assert.Equal("Http.RequestFailed;404", ex.Message);
    Assert.Equal(["Http.RequestFailed;404"], ex.GetErrorMessages());

    var item = Assert.Single(ex.GetNotifications());
    Assert.Equal("Http.RequestFailed;404", item.Message);
    Assert.DoesNotContain("modelo-inexistente", ex.ToString());
  }

  // Testa se o Problem Details do Tooark continua virando notificações, e o corpo também fica disponível
  [Fact]
  public async Task UpstreamBody_ShouldComeAlongWithTheTooarkProblemDetails()
  {
    // Arrange
    const string problema = """{"status":400,"errors":[{"code":"T.VLD.STR1","key":"Email","message":"O campo Email é obrigatório"}]}""";

    // Act
    var ex = await FalharAsync(() => FakeHttpMessageHandler.Json(HttpStatusCode.BadRequest, problema, "application/problem+json"));

    // Assert
    var item = Assert.Single(ex.GetNotifications());
    Assert.Equal("O campo Email é obrigatório", item.Message);
    Assert.Equal("Email", item.Key);
    Assert.Equal("T.VLD.STR1", item.Code);
    Assert.Equal(problema, ex.UpstreamBody);
  }

  // Testa se o texto é lido no conjunto de caracteres declarado, sem a marca de ordem de bytes
  [Fact]
  public async Task UpstreamBody_ShouldDecodeWithTheDeclaredCharset()
  {
    // Arrange
    const string mensagem = "Crédito insuficiente";
    var latin1 = Encoding.Latin1.GetBytes(mensagem);
    byte[] utf8ComMarca = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(mensagem)];

    HttpResponseMessage Resposta(byte[] bytes, string contentType)
    {
      var content = new ByteArrayContent(bytes);
      content.Headers.TryAddWithoutValidation("Content-Type", contentType);

      return new HttpResponseMessage(HttpStatusCode.PaymentRequired) { Content = content };
    }

    // Act
    var declarado = await FalharAsync(() => Resposta(latin1, "text/plain; charset=iso-8859-1"));
    var entreAspas = await FalharAsync(() => Resposta(latin1, "text/plain; charset=\"iso-8859-1\""));
    var comMarca = await FalharAsync(() => Resposta(utf8ComMarca, "text/plain"));
    var desconhecido = await FalharAsync(() => Resposta(Encoding.UTF8.GetBytes(mensagem), "text/plain; charset=nao-existe"));
    var semTipo = await FalharAsync(() =>
      new HttpResponseMessage(HttpStatusCode.PaymentRequired) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes(mensagem)) });

    // Assert
    Assert.Equal(mensagem, declarado.UpstreamBody);
    Assert.Equal(mensagem, entreAspas.UpstreamBody);
    Assert.Equal(mensagem, comMarca.UpstreamBody);
    Assert.Equal(mensagem, desconhecido.UpstreamBody);
    Assert.Equal(mensagem, semTipo.UpstreamBody);
  }

  // Testa se a resposta sem corpo, ou só com espaços, deixa o corpo nulo
  [Theory]
  [InlineData("")]
  [InlineData("   \n")]
  public async Task UpstreamBody_ShouldBeNull_WhenTheBodyIsBlank(string corpo)
  {
    // Arrange & Act
    var comCorpo = await FalharAsync(() => FakeHttpMessageHandler.Json(HttpStatusCode.InternalServerError, corpo));
    var semCorpo = await FalharAsync(() => FakeHttpMessageHandler.Empty(HttpStatusCode.InternalServerError));

    // Assert
    Assert.Null(comCorpo.UpstreamBody);
    Assert.Null(semCorpo.UpstreamBody);
  }

  // Testa se o corpo acima do limite é descartado inteiro, com e sem o tamanho declarado
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task UpstreamBody_ShouldBeNull_WhenAboveTheLimit(bool tamanhoDeclarado)
  {
    // Arrange
    var bytes = Encoding.UTF8.GetBytes(new string('a', HttpErrorMapper.MaximumErrorBodyBytes + 1));

    HttpContent Conteudo() => tamanhoDeclarado
      ? new ByteArrayContent(bytes) { Headers = { ContentType = new MediaTypeHeaderValue("text/plain") } }
      : new CorpoSemTamanho(bytes, "text/plain");

    // Act
    var ex = await FalharAsync(() => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = Conteudo() });

    // Assert
    Assert.Null(ex.UpstreamBody);
    Assert.Equal("Http.RequestFailed;502", ex.Message);
  }

  // Testa se o corpo exatamente no limite ainda é lido
  [Fact]
  public async Task UpstreamBody_ShouldBeRead_WhenExactlyAtTheLimit()
  {
    // Arrange
    var texto = new string('a', HttpErrorMapper.MaximumErrorBodyBytes);

    // Act
    var ex = await FalharAsync(() => new HttpResponseMessage(HttpStatusCode.BadGateway)
    {
      Content = new CorpoSemTamanho(Encoding.UTF8.GetBytes(texto), "text/plain")
    });

    // Assert
    Assert.Equal(texto, ex.UpstreamBody);
  }

  // Testa se a falha na leitura do corpo mantém o status e deixa o corpo nulo
  [Fact]
  public async Task UpstreamBody_ShouldBeNull_WhenTheBodyCannotBeRead()
  {
    // Arrange & Act
    var ex = await FalharAsync(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new CorpoQuebrado() });

    // Assert
    Assert.Null(ex.UpstreamBody);
    Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.UpstreamStatusCode);
    Assert.Equal("Http.RequestFailed;503", ex.Message);
  }

  // Testa os construtores: o corpo é guardado como veio, o corpo em branco vira nulo e os demais não o informam
  [Fact]
  public void Constructors_ShouldKeepTheBodyOnlyWhenGiven()
  {
    // Arrange
    var uri = new Uri(Endereco);
    NotificationItem[] itens = [new("Sem crédito", "Conta", "P.402")];

    // Act
    var comCorpo = new HttpRequestFailedException(HttpStatusCode.PaymentRequired, uri, itens, ErroDoProvedor);
    var emBranco = new HttpRequestFailedException(HttpStatusCode.PaymentRequired, uri, null, "  ");
    var semCorpo = new HttpRequestFailedException(HttpStatusCode.PaymentRequired, uri, itens);
    var padrao = new HttpRequestFailedException(HttpStatusCode.PaymentRequired);

    // Assert
    Assert.Equal(ErroDoProvedor, comCorpo.UpstreamBody);
    Assert.Equal("Sem crédito", comCorpo.Message);
    Assert.Equal("P.402", Assert.Single(comCorpo.GetNotifications()).Code);
    Assert.Same(uri, comCorpo.RequestUri);

    Assert.Null(emBranco.UpstreamBody);
    Assert.Equal("Http.RequestFailed;402", emBranco.Message);
    Assert.Null(semCorpo.UpstreamBody);
    Assert.Null(padrao.UpstreamBody);
  }
}
