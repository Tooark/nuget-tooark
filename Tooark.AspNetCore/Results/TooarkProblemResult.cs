using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tooark.AspNetCore.Results;

/// <summary>
/// Resultado de minimal API que escreve um <see cref="ProblemDetails"/> do Tooark.
/// </summary>
/// <remarks>
/// Completa o <c>instance</c> com o caminho da requisição no momento da execução, quando ele ainda não foi
/// informado, e delega a escrita ao <c>TypedResults.Problem</c>, que aplica o <c>type</c> padrão do status e as
/// opções de JSON da aplicação. Assim o corpo é o mesmo que o <c>TooarkExceptionHandler</c> escreve.
/// </remarks>
/// <param name="problemDetails">Problem Details a escrever.</param>
internal sealed class TooarkProblemResult(ProblemDetails problemDetails)
  : IResult, IStatusCodeHttpResult, IContentTypeHttpResult, IValueHttpResult, IValueHttpResult<ProblemDetails>
{
  #region Properties

  /// <summary>
  /// Problem Details que será escrito na resposta.
  /// </summary>
  public ProblemDetails ProblemDetails { get; } = problemDetails;

  /// <inheritdoc />
  public int? StatusCode => ProblemDetails.Status;

  /// <inheritdoc />
  public string ContentType => "application/problem+json";

  /// <inheritdoc />
  object? IValueHttpResult.Value => ProblemDetails;

  /// <inheritdoc />
  ProblemDetails? IValueHttpResult<ProblemDetails>.Value => ProblemDetails;

  #endregion

  #region Methods

  /// <inheritdoc />
  public Task ExecuteAsync(HttpContext httpContext)
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    // O caminho só é conhecido na execução; um instance já informado é respeitado
    if (string.IsNullOrEmpty(ProblemDetails.Instance) && httpContext.Request.Path.HasValue)
    {
      ProblemDetails.Instance = httpContext.Request.Path;
    }

    return TypedResults.Problem(ProblemDetails).ExecuteAsync(httpContext);
  }

  #endregion
}
