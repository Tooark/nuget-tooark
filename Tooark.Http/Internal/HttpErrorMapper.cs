using System.Text;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;
using Tooark.Exceptions;
using Tooark.Http.Exceptions;
using Tooark.Notifications;

namespace Tooark.Http.Internal;

/// <summary>
/// Converte as falhas de uma chamada HTTP nos padrões de retorno do Tooark.
/// </summary>
/// <remarks>
/// É o único ponto do pacote que decide qual exceção e qual chave cada falha vira: resposta de erro, tempo limite,
/// circuito aberto, falha de conexão, destino bloqueado e corpo de resposta inválido.
/// </remarks>
internal static class HttpErrorMapper
{
  #region Constants

  /// <summary>
  /// Tipo de mídia do Problem Details (RFC 9457).
  /// </summary>
  internal const string ProblemMediaType = "application/problem+json";

  /// <summary>
  /// Maior corpo de erro lido do serviço chamado, em bytes. Acima disso o corpo é ignorado.
  /// </summary>
  internal const int MaximumErrorBodyBytes = 64 * 1024;

  /// <summary>
  /// Maior quantidade de erros aproveitada de um Problem Details.
  /// </summary>
  internal const int MaximumErrors = 50;

  /// <summary>
  /// Maior tamanho aproveitado de cada texto de um erro (código, chave e mensagem).
  /// </summary>
  internal const int MaximumErrorTextLength = 1024;

  #endregion

  #region Private Fields

  /// <summary>
  /// Marca de ordem de bytes do UTF-8.
  /// </summary>
  private static ReadOnlySpan<byte> ByteOrderMark => [0xEF, 0xBB, 0xBF];

  #endregion

  #region Internal Methods

  /// <summary>
  /// Converte uma exceção da chamada na exceção do Tooark equivalente.
  /// </summary>
  /// <param name="exception">A exceção capturada.</param>
  /// <param name="callerToken">O token de quem chamou o serviço, sem o tempo limite do pacote.</param>
  /// <returns>
  /// A exceção do Tooark, ou nulo quando a exceção deve seguir como está: o cancelamento de quem chamou, uma
  /// exceção que já é do Tooark ou uma falha que não é da chamada HTTP.
  /// </returns>
  internal static TooarkException? Map(Exception exception, CancellationToken callerToken)
  {
    switch (exception)
    {
      // Já está no padrão: a resposta de erro mapeada, a recusa da guarda, o corpo inválido
      case TooarkException:
        return null;

      // O cancelamento de quem chamou não é um tempo limite: segue como OperationCanceledException
      case OperationCanceledException when callerToken.IsCancellationRequested:
        return null;
    }

    // Sem o handler da guarda no caminho (um cliente com o ConnectCallback configurado à mão), a recusa chega
    // embrulhada pelo transporte
    if (OutboundGuardHandler.FindBlocked(exception) is { } blocked)
    {
      return new OutboundBlockedException(blocked.Host, exception);
    }

    return exception switch
    {
      // Tempo limite do Polly (por tentativa ou total), do HttpClient ou o da chamada inteira, do serviço
      TimeoutRejectedException or OperationCanceledException =>
        new GatewayTimeoutException("Http.RequestTimeout", exception),
      BrokenCircuitException =>
        new ServiceUnavailableException("Http.CircuitOpen", exception),
      // O handler padrão limita as requisições simultâneas e recusa a que passa do limite
      RateLimiterRejectedException =>
        new ServiceUnavailableException("Http.ConcurrencyLimitReached", exception),
      // Sem resposta: conexão recusada, nome não resolvido, falha de TLS, conexão caída no meio do corpo
      HttpRequestException or IOException =>
        new BadGatewayException("Http.ConnectionFailed", exception),
      // Resposta de sucesso com um corpo que não é o contrato: a falha é do serviço chamado, e não de quem chamou
      JsonException =>
        new BadGatewayException("Http.InvalidResponse", exception),
      _ => null
    };
  }

  /// <summary>
  /// Monta a exceção de uma resposta de erro do serviço chamado.
  /// </summary>
  /// <remarks>
  /// Quando o corpo é o Problem Details do Tooark (<c>application/problem+json</c> com a lista <c>errors</c> de
  /// <c>code</c>, <c>key</c> e <c>message</c>), cada erro vira um item de notificação. Qualquer outro corpo fica
  /// fora da mensagem e das notificações. O corpo, em qualquer formato, vai como texto para
  /// <see cref="HttpRequestFailedException.UpstreamBody"/>, dentro do limite de <see cref="MaximumErrorBodyBytes"/>.
  /// </remarks>
  /// <param name="response">A resposta de erro.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>A exceção com o status, os erros e o corpo do serviço chamado.</returns>
  internal static async Task<HttpRequestFailedException> FromResponseAsync(
    HttpResponseMessage response,
    CancellationToken cancellationToken
  )
  {
    var requestUri = response.RequestMessage?.RequestUri;
    List<NotificationItem> notifications = [];
    string? upstreamBody = null;

    try
    {
      var body = await ReadErrorBodyAsync(response, cancellationToken).ConfigureAwait(false);

      if (!body.IsEmpty)
      {
        var contentType = response.Content.Headers.ContentType;

        // Só o Problem Details vira notificação; os demais formatos seguem apenas como texto
        if (string.Equals(contentType?.MediaType, ProblemMediaType, StringComparison.OrdinalIgnoreCase))
        {
          notifications = ParseProblem(body);
        }

        upstreamBody = DecodeBody(body, contentType?.CharSet);
      }
    }
    catch (Exception exception) when (exception is HttpRequestException or IOException)
    {
      // A conexão caiu no meio do corpo de erro: o status já chegou, e é ele que importa para quem chamou
    }

    return new HttpRequestFailedException(response.StatusCode, requestUri, notifications, upstreamBody);
  }

  /// <summary>
  /// Interpreta o corpo de um Problem Details do Tooark.
  /// </summary>
  /// <param name="body">O corpo da resposta.</param>
  /// <returns>Os erros aproveitáveis; vazio quando o corpo não é o Problem Details do Tooark.</returns>
  internal static List<NotificationItem> ParseProblem(ReadOnlyMemory<byte> body)
  {
    var notifications = new List<NotificationItem>();

    try
    {
      using var document = JsonDocument.Parse(SkipByteOrderMark(body));

      // No Problem Details de validação do ASP.NET Core, errors é um objeto, e não a lista do Tooark
      if (document.RootElement.ValueKind != JsonValueKind.Object
        || !document.RootElement.TryGetProperty("errors", out var errors)
        || errors.ValueKind != JsonValueKind.Array)
      {
        return notifications;
      }

      foreach (var error in errors.EnumerateArray())
      {
        if (notifications.Count == MaximumErrors)
        {
          break;
        }

        // Sem mensagem não há o que repassar
        if (error.ValueKind != JsonValueKind.Object || GetText(error, "message") is not { } message)
        {
          continue;
        }

        notifications.Add(new NotificationItem(message, GetText(error, "key"), GetText(error, "code")));
      }
    }
    catch (JsonException)
    {
      // Corpo que não é JSON válido: a resposta de erro segue com a mensagem padrão
      notifications.Clear();
    }

    return notifications;
  }

  /// <summary>
  /// Descarta a marca de ordem de bytes do UTF-8, que o leitor de JSON não aceita.
  /// </summary>
  /// <param name="body">O corpo da resposta.</param>
  /// <returns>O corpo sem a marca inicial.</returns>
  internal static ReadOnlyMemory<byte> SkipByteOrderMark(ReadOnlyMemory<byte> body) =>
    body.Span.StartsWith(ByteOrderMark) ? body[ByteOrderMark.Length..] : body;

  #endregion

  #region Private Methods

  /// <summary>
  /// Lê o corpo da resposta de erro, limitando o tamanho lido.
  /// </summary>
  /// <param name="response">A resposta de erro.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O corpo; vazio quando a resposta não tem corpo ou quando ele passa do limite.</returns>
  private static async Task<ReadOnlyMemory<byte>> ReadErrorBodyAsync(
    HttpResponseMessage response,
    CancellationToken cancellationToken
  )
  {
    // O tamanho declarado já resolve os dois extremos sem ler nada: sem corpo, ou acima do limite
    if (response.Content.Headers.ContentLength is 0 or > MaximumErrorBodyBytes)
    {
      return ReadOnlyMemory<byte>.Empty;
    }

    // Um byte além do limite basta para saber que o corpo passou dele, sem ler o resto
    var buffer = new byte[MaximumErrorBodyBytes + 1];
    var length = 0;

    var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

    await using (stream.ConfigureAwait(false))
    {
      while (length < buffer.Length)
      {
        var read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken).ConfigureAwait(false);

        if (read == 0)
        {
          break;
        }

        length += read;
      }
    }

    return length > MaximumErrorBodyBytes ? ReadOnlyMemory<byte>.Empty : buffer.AsMemory(0, length);
  }

  /// <summary>
  /// Converte o corpo da resposta de erro em texto.
  /// </summary>
  /// <param name="body">O corpo da resposta.</param>
  /// <param name="charSet">O conjunto de caracteres declarado no <c>Content-Type</c>, quando houver.</param>
  /// <returns>O texto, ou nulo quando o corpo está em branco.</returns>
  private static string? DecodeBody(ReadOnlyMemory<byte> body, string? charSet)
  {
    var encoding = Encoding.UTF8;

    if (!string.IsNullOrWhiteSpace(charSet))
    {
      try
      {
        // O valor pode vir entre aspas: charset="utf-8"
        encoding = Encoding.GetEncoding(charSet.Trim().Trim('"'));
      }
      catch (ArgumentException)
      {
        // Conjunto de caracteres desconhecido para o runtime: segue como UTF-8, o padrão do JSON
      }
    }

    var text = encoding.GetString(SkipByteOrderMark(body).Span);

    return string.IsNullOrWhiteSpace(text) ? null : text;
  }

  /// <summary>
  /// Lê uma propriedade de texto de um erro, limitando o tamanho.
  /// </summary>
  /// <param name="error">O erro.</param>
  /// <param name="name">O nome da propriedade.</param>
  /// <returns>O texto, ou nulo quando a propriedade não existe, não é texto ou está em branco.</returns>
  private static string? GetText(JsonElement error, string name)
  {
    if (!error.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
    {
      return null;
    }

    var text = property.GetString();

    if (string.IsNullOrWhiteSpace(text))
    {
      return null;
    }

    return text.Length > MaximumErrorTextLength ? text[..MaximumErrorTextLength] : text;
  }

  #endregion
}
