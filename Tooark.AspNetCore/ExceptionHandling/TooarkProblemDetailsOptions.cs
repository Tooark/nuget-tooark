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
  /// Padrão: <see cref="LogLevel.Information"/>, porque um 404 ou um 409 faz parte da operação normal. Até a 4.x o
  /// padrão era <see cref="LogLevel.Warning"/>.
  /// </value>
  public LogLevel ClientErrorLogLevel { get; set; } = LogLevel.Information;

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
  /// <c>ValidationProblem()</c> dos controllers, para a validação das minimal APIs e para o
  /// <c>Results.ValidationProblem</c>.
  /// </para>
  /// </remarks>
  /// <value>
  /// Padrão: <see langword="true"/>. Na 4.x, onde a opção surgiu (v4.10.0), o padrão era
  /// <see langword="false"/>.
  /// </value>
  public bool ConvertValidationProblems { get; set; } = true;

  #endregion

  #region Internal Methods

  /// <summary>
  /// Resolve o nível do log de um status.
  /// </summary>
  /// <param name="statusCode">Status da resposta.</param>
  /// <returns>O nível configurado para o status, ou o da faixa dele.</returns>
  internal LogLevel GetLogLevel(int statusCode)
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
