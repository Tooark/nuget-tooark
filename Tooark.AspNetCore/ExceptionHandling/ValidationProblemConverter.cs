using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Tooark.AspNetCore.Extensions;
using Tooark.Extensions;

namespace Tooark.AspNetCore.ExceptionHandling;

/// <summary>
/// Converte o problema de validação do ASP.NET Core no Problem Details do Tooark, quando
/// <see cref="TooarkProblemDetailsOptions.ConvertValidationProblems"/> está ligada.
/// </summary>
internal static class ValidationProblemConverter
{
  #region Private Fields

  /// <summary>
  /// Localizador usado quando o container não tem um <see cref="IStringLocalizer"/>, como no manipulador.
  /// </summary>
  private static readonly IStringLocalizer DefaultLocalizer = new JsonStringLocalizerExtension();

  #endregion

  #region Internal Methods

  /// <summary>
  /// Indica se a conversão está ligada nas opções do container da requisição.
  /// </summary>
  /// <param name="httpContext">Contexto da requisição.</param>
  /// <returns>Verdadeiro quando a conversão está ligada.</returns>
  internal static bool IsEnabled(HttpContext httpContext) =>
    httpContext.RequestServices?.GetService<IOptionsMonitor<TooarkProblemDetailsOptions>>()?.CurrentValue
      .ConvertValidationProblems == true;

  /// <summary>
  /// Monta o Problem Details do Tooark a partir do problema de validação do ASP.NET Core.
  /// </summary>
  /// <remarks>
  /// O status, o <c>type</c> e as demais extensões, como o <c>traceId</c>, vêm do problema original; o título
  /// passa a ser a frase padrão do status, o <c>detail</c> a primeira mensagem e o <c>instance</c>, quando o
  /// original não tem, o caminho da requisição, como nas exceções do Tooark.
  /// </remarks>
  /// <param name="source">O problema de validação do ASP.NET Core.</param>
  /// <param name="httpContext">Contexto da requisição.</param>
  /// <returns>O Problem Details com a lista <c>errors</c> do Tooark.</returns>
  internal static ProblemDetails Convert(HttpValidationProblemDetails source, HttpContext httpContext)
  {
    var localizer = httpContext.RequestServices?.GetService<IStringLocalizer>() ?? DefaultLocalizer;

    // O problema de validação sempre é um erro do cliente; sem status, vale o da resposta ou o 400 padrão
    var statusCode = source.Status
      ?? (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest
        ? httpContext.Response.StatusCode
        : StatusCodes.Status400BadRequest);

    var problemDetails = TooarkProblemDetailsBuilder.Build(
      statusCode,
      source.ToNotificationItems(),
      localizer,
      source.Instance ?? httpContext.Request.Path.Value
    );

    problemDetails.Type = source.Type;

    // O errors do original é o objeto por campo, que a lista substitui
    foreach (var (key, value) in source.Extensions)
    {
      if (key != TooarkProblemDetailsBuilder.ErrorsExtension)
      {
        problemDetails.Extensions.TryAdd(key, value);
      }
    }

    return problemDetails;
  }

  #endregion
}

/// <summary>
/// Converte os problemas de validação escritos pelo <see cref="IProblemDetailsService"/>: a validação das minimal
/// APIs e o <c>Results.ValidationProblem</c>.
/// </summary>
/// <remarks>
/// Envolve o <c>CustomizeProblemDetails</c> da aplicação depois de todas as configurações. A personalização da
/// aplicação roda antes, sobre o problema do ASP.NET Core, e a conversão copia as extensões dela para o corpo
/// convertido. A ordem importa no MVC: a fábrica de Problem Details dos controllers também chama o
/// <c>CustomizeProblemDetails</c>, mas fica com o objeto original, que o <see cref="ValidationProblemResultFilter"/>
/// converte depois.
/// </remarks>
internal sealed class ValidationProblemPostConfigure : IPostConfigureOptions<ProblemDetailsOptions>
{
  /// <summary>
  /// Envolve o <c>CustomizeProblemDetails</c> com a conversão.
  /// </summary>
  /// <param name="name">Nome das opções.</param>
  /// <param name="options">Opções do Problem Details do ASP.NET Core.</param>
  public void PostConfigure(string? name, ProblemDetailsOptions options)
  {
    var customize = options.CustomizeProblemDetails;

    options.CustomizeProblemDetails = context =>
    {
      customize?.Invoke(context);

      if (context.ProblemDetails is HttpValidationProblemDetails validation
        && ValidationProblemConverter.IsEnabled(context.HttpContext))
      {
        context.ProblemDetails = ValidationProblemConverter.Convert(validation, context.HttpContext);
      }
    };
  }
}

/// <summary>
/// Converte os problemas de validação dos controllers, que o MVC escreve pelos formatadores de saída, sem passar
/// pelo <see cref="IProblemDetailsService"/>: a validação automática do <c>[ApiController]</c> e o
/// <c>ValidationProblem()</c>.
/// </summary>
internal sealed class ValidationProblemResultFilter : IAlwaysRunResultFilter
{
  /// <summary>
  /// Troca o problema de validação do resultado pelo Problem Details do Tooark.
  /// </summary>
  /// <param name="context">Contexto do resultado.</param>
  public void OnResultExecuting(ResultExecutingContext context)
  {
    if (context.Result is not ObjectResult { Value: HttpValidationProblemDetails validation } result
      || !ValidationProblemConverter.IsEnabled(context.HttpContext))
    {
      return;
    }

    result.Value = ValidationProblemConverter.Convert(validation, context.HttpContext);
    result.DeclaredType = typeof(ProblemDetails);
  }

  /// <summary>
  /// Não faz nada depois do resultado.
  /// </summary>
  /// <param name="context">Contexto do resultado executado.</param>
  public void OnResultExecuted(ResultExecutedContext context)
  {
  }
}

/// <summary>
/// Acrescenta o <see cref="ValidationProblemResultFilter"/> aos filtros globais do MVC.
/// </summary>
internal sealed class ValidationProblemMvcSetup : IConfigureOptions<MvcOptions>
{
  /// <summary>
  /// Acrescenta o filtro.
  /// </summary>
  /// <param name="options">Opções do MVC.</param>
  public void Configure(MvcOptions options) => options.Filters.Add(new ValidationProblemResultFilter());
}
