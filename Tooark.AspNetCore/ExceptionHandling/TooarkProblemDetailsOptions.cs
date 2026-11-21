using Microsoft.Extensions.Logging;

namespace Tooark.AspNetCore.ExceptionHandling;

/// <summary>
/// Opções do Problem Details do Tooark: o nível do log de cada exceção respondida pelo
/// <see cref="TooarkExceptionHandler"/> e a conversão dos problemas de validação do ASP.NET Core.
/// </summary>
/// <remarks>
/// Configure com <c>services.Configure&lt;TooarkProblemDetailsOptions&gt;(...)</c>, antes ou depois do
/// <c>AddTooarkProblemDetails</c>. As opções são lidas do container da requisição a cada resposta, sem seção de
/// configuração.
/// <para>
/// O nível de um status vem de <see cref="StatusCodeLogLevels"/> quando o status está lá; senão, de
/// <see cref="ServerErrorLogLevel"/> para 5xx e de <see cref="ClientErrorLogLevel"/> para os demais.
/// <see cref="LogLevel.None"/> não registra nada para aquele status. Só o log de um 5xx leva a exceção, com a
/// pilha de chamadas: um erro do cliente é registrado com o status e a mensagem.
/// </para>
/// </remarks>
public sealed class TooarkProblemDetailsOptions
{
  #region Properties

  /// <summary>
  /// Nível do log de uma exceção respondida com status de erro do cliente (4xx).
  /// </summary>
  /// <value>
  /// Padrão: <see cref="LogLevel.Warning"/>, o nível de antes das opções. Um 404 ou um 409 que faz parte da
  /// operação normal pede <see cref="LogLevel.Information"/>, o padrão a partir da v5.
  /// </value>
  public LogLevel ClientErrorLogLevel { get; set; } = LogLevel.Warning;

  /// <summary>
  /// Nível do log de uma exceção respondida com status de erro do servidor (5xx).
  /// </summary>
  /// <value>Padrão: <see cref="LogLevel.Error"/>.</value>
  public LogLevel ServerErrorLogLevel { get; set; } = LogLevel.Error;

  /// <summary>
  /// Nível do log por status, que vence o nível da faixa do status.
  /// </summary>
  /// <remarks>
  /// Por exemplo, <c>[401] = LogLevel.Warning</c> para acompanhar falhas de autenticação, ou
  /// <c>[404] = LogLevel.Debug</c> para tirar os não encontrados do log de produção.
  /// </remarks>
  public IDictionary<int, LogLevel> StatusCodeLogLevels { get; } = new Dictionary<int, LogLevel>();

  /// <summary>
  /// Converte os problemas de validação do ASP.NET Core para o formato do Tooark.
  /// </summary>
  /// <remarks>
  /// O <c>ValidationProblemDetails</c> do ASP.NET Core traz <c>errors</c> como um objeto com as mensagens por
  /// campo (<c>{ "Email": ["..."] }</c>); o Problem Details do Tooark traz <c>errors</c> como uma lista de
  /// <c>code</c>, <c>key</c> e <c>message</c>. Ligada, a conversão responde as falhas de validação na lista do
  /// Tooark, uma entrada por mensagem, com o campo em <c>key</c>, o código padrão <c>T.ERR</c> e a mensagem
  /// traduzida, e o cliente passa a receber um único formato em <c>errors</c>.
  /// <para>
  /// Vale para a validação automática dos controllers com <c>[ApiController]</c> (sem o envelope do
  /// <c>AddTooarkModelStateEnvelope</c>, que responde com o <c>ResponseDto</c>), para o
  /// <c>ValidationProblem()</c> dos controllers e, no .NET 10, para a validação das minimal APIs e o
  /// <c>Results.ValidationProblem</c>. No .NET 8 o <c>Results.ValidationProblem</c> não passa pelo serviço de
  /// Problem Details e não é convertido: use o <c>TooarkResults.ValidationProblem</c>.
  /// </para>
  /// </remarks>
  /// <value>Padrão: <see langword="false"/>. A partir da v5 a conversão vem ligada.</value>
  public bool ConvertValidationProblems { get; set; }

  #endregion

  #region Methods

  /// <summary>
  /// Resolve o nível do log de um status, pela mesma regra que o <see cref="TooarkExceptionHandler"/> aplica.
  /// </summary>
  /// <remarks>
  /// Serve para conferir a configuração sem capturar o log: num teste da aplicação, por exemplo, que garante que
  /// um 404 fica em <see cref="LogLevel.Debug"/> e que um 500 continua em <see cref="LogLevel.Error"/>.
  /// </remarks>
  /// <param name="statusCode">Status da resposta.</param>
  /// <returns>O nível configurado para o status, ou o da faixa dele.</returns>
  public LogLevel GetLogLevel(int statusCode)
  {
    // O nível do status vence o da faixa
    if (StatusCodeLogLevels.TryGetValue(statusCode, out var logLevel))
    {
      return logLevel;
    }

    return statusCode >= 500 ? ServerErrorLogLevel : ClientErrorLogLevel;
  }

  #endregion
}
