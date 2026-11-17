using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Extensions;

namespace Tooark.AspNetCore.ExceptionHandling;

/// <summary>
/// Manipulador de exceções do ASP.NET Core que responde as exceções do Tooark com Problem Details
/// (RFC 9457), no status que a própria exceção define.
/// </summary>
/// <remarks>
/// Trata só as exceções que herdam de <see cref="TooarkException"/>: o status vem de
/// <see cref="TooarkException.GetStatusCode"/> e a lista <c>errors</c> do corpo, de
/// <see cref="TooarkException.GetNotifications"/>, com as mensagens traduzidas pelo localizador. Qualquer outra
/// exceção é devolvida ao ASP.NET Core, que responde o 500 padrão do <c>AddProblemDetails</c>, sem detalhes da
/// exceção.
/// <para>
/// Cada exceção respondida é registrada no log no nível de <see cref="TooarkProblemDetailsOptions"/>, lidas
/// do container da requisição: por padrão <c>Warning</c> para 4xx e <c>Error</c>, com a pilha de chamadas, para
/// 5xx. No .NET 10 o <c>UseExceptionHandler</c> não registra a exceção que um <see cref="IExceptionHandler"/>
/// tratou, então este é o único registro dela; no .NET 8 o middleware registra a exceção como <c>Error</c>
/// antes de chamar o manipulador.
/// </para>
/// <para>
/// O corpo é escrito pelo <see cref="IProblemDetailsService"/>, que aplica o <c>type</c> padrão do status e o
/// <c>CustomizeProblemDetails</c> da aplicação. Quando o serviço recusa a escrita, por exemplo por um
/// <c>Accept</c> que não aceita JSON, o corpo é escrito mesmo assim em <c>application/problem+json</c>: uma
/// exceção do Tooark nunca vira um 500 sem corpo.
/// </para>
/// <para>
/// Registrado por <c>AddTooarkProblemDetails</c> e acionado pelo <c>UseExceptionHandler()</c> do pipeline.
/// </para>
/// </remarks>
public sealed class TooarkExceptionHandler : IExceptionHandler
{
  #region Private Fields

  /// <summary>
  /// Logger das exceções tratadas.
  /// </summary>
  private readonly ILogger<TooarkExceptionHandler> _logger;

  /// <summary>
  /// Serviço que escreve o Problem Details, quando registrado.
  /// </summary>
  private readonly IProblemDetailsService? _problemDetailsService;

  /// <summary>
  /// Localizador que traduz as mensagens dos erros.
  /// </summary>
  private readonly IStringLocalizer _localizer;

  /// <summary>
  /// Opções padrão, usadas quando o container da requisição não tem as opções do manipulador.
  /// </summary>
  private static readonly TooarkProblemDetailsOptions DefaultOptions = new();

  #endregion

  #region Constructors

  /// <summary>
  /// Cria o manipulador com os serviços do container, todos opcionais.
  /// </summary>
  /// <param name="logger">Logger; sem registro, as exceções tratadas não são registradas.</param>
  /// <param name="problemDetailsService">
  /// Serviço de Problem Details do ASP.NET Core; sem ele, o corpo é escrito direto na resposta.
  /// </param>
  /// <param name="localizer">
  /// Localizador das mensagens; sem registro, usa o localizador JSON do <c>Tooark.Extensions</c>.
  /// </param>
  /// <remarks>
  /// As <see cref="TooarkProblemDetailsOptions"/> não entram pelo construtor: são lidas do container da
  /// requisição a cada exceção, e sem registro valem os padrões.
  /// </remarks>
  public TooarkExceptionHandler(
    ILogger<TooarkExceptionHandler>? logger = null,
    IProblemDetailsService? problemDetailsService = null,
    IStringLocalizer? localizer = null
  )
  {
    _logger = logger ?? NullLogger<TooarkExceptionHandler>.Instance;
    _problemDetailsService = problemDetailsService;
    _localizer = localizer ?? new JsonStringLocalizerExtension();
  }

  #endregion

  #region Methods

  /// <summary>
  /// Tenta responder a exceção com Problem Details.
  /// </summary>
  /// <param name="httpContext">Contexto da requisição.</param>
  /// <param name="exception">Exceção capturada pelo pipeline.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>
  /// Verdadeiro quando a exceção é do Tooark e a resposta foi escrita; falso para as demais exceções, que
  /// seguem para o tratamento padrão do ASP.NET Core.
  /// </returns>
  public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
  {
    // Só as exceções do Tooark carregam o status e os itens de notificação
    if (exception is not TooarkException tooarkException)
    {
      return false;
    }

    var statusCode = (int)tooarkException.GetStatusCode();

    Log(httpContext, statusCode, exception);

    var problemDetails = TooarkProblemDetailsBuilder.Build(
      statusCode,
      tooarkException.GetNotifications(),
      _localizer,
      httpContext.Request.Path
    );

    httpContext.Response.StatusCode = statusCode;

    // O serviço aplica o type padrão do status e o CustomizeProblemDetails da aplicação
    if (_problemDetailsService is not null)
    {
      var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
      {
        HttpContext = httpContext,
        ProblemDetails = problemDetails,
        Exception = exception
      }).ConfigureAwait(false);

      if (written)
      {
        return true;
      }
    }

    // O serviço recusou (Accept sem JSON) ou não existe: o corpo sai mesmo assim, com os mesmos padrões
    await TypedResults.Problem(problemDetails).ExecuteAsync(httpContext).ConfigureAwait(false);

    return true;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Registra a exceção respondida no nível configurado para o status.
  /// </summary>
  /// <param name="httpContext">Contexto da requisição, de onde vêm as opções.</param>
  /// <param name="statusCode">Status da resposta.</param>
  /// <param name="exception">Exceção respondida.</param>
  private void Log(HttpContext httpContext, int statusCode, Exception exception)
  {
    // O monitor reflete a configuração atual; um manipulador criado fora do container usa os padrões
    var options = httpContext.RequestServices?.GetService<IOptionsMonitor<TooarkProblemDetailsOptions>>()?.CurrentValue;
    var logLevel = (options ?? DefaultOptions).GetLogLevel(statusCode);

    if (!_logger.IsEnabled(logLevel))
    {
      return;
    }

    // Só um erro do servidor leva a pilha de chamadas; o de cliente é esperado e basta a mensagem
    var loggedException = statusCode >= StatusCodes.Status500InternalServerError ? exception : null;

    _logger.Log(logLevel, loggedException, "Tooark exception answered with status {StatusCode}: {Message}", statusCode, exception.Message);
  }

  #endregion
}
