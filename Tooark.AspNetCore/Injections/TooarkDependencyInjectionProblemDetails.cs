using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Tooark.AspNetCore.ExceptionHandling;
using Tooark.Exceptions;

namespace Tooark.AspNetCore.Injections;

/// <summary>
/// Classe de extensão para adicionar as integrações do ASP.NET Core ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Faz as exceções do Tooark responderem com Problem Details (RFC 9457), no status que cada exceção define,
  /// em controllers e minimal APIs.
  /// </summary>
  /// <remarks>
  /// Registra o <c>AddProblemDetails</c> do ASP.NET Core e o <see cref="TooarkExceptionHandler"/>. Uma
  /// <see cref="TooarkException"/> lançada em qualquer ponto da requisição responde com o status de
  /// <see cref="TooarkException.GetStatusCode"/> e um corpo <c>application/problem+json</c> com <c>status</c>,
  /// <c>title</c>, <c>detail</c>, <c>instance</c> e a lista <c>errors</c>, com código, chave e mensagem
  /// traduzida de cada item de notificação. As demais exceções seguem para o tratamento padrão do ASP.NET Core,
  /// que responde 500 sem detalhes da exceção.
  /// <para>
  /// O manipulador só roda com o <c>app.UseExceptionHandler()</c>, sem argumentos, no pipeline, antes dos
  /// middlewares que podem lançar (autenticação, autorização, endpoints). O <c>AddProblemDetails</c> é o que
  /// permite chamar o <c>UseExceptionHandler</c> sem um caminho de erro.
  /// </para>
  /// <para>
  /// Independe do <see cref="AddTooarkModelStateEnvelope"/>: a falha de validação dos controllers com
  /// <c>[ApiController]</c> continua respondendo o que o envelope define. Este registro cobre as exceções, as
  /// notificações devolvidas pelas minimal APIs com o <c>TooarkResults</c> e, por padrão, as falhas de validação
  /// do ASP.NET Core que não passam pelo envelope
  /// (<see cref="TooarkProblemDetailsOptions.ConvertValidationProblems"/>). Chamar mais de uma vez não repete o
  /// manipulador nem a conversão.
  /// </para>
  /// <para>
  /// As <see cref="TooarkProblemDetailsOptions"/>, configuradas com
  /// <c>services.Configure&lt;TooarkProblemDetailsOptions&gt;(...)</c>, definem o nível do log de cada exceção
  /// respondida (por padrão <c>Information</c> para 4xx e <c>Error</c> para 5xx) e se os problemas de validação
  /// do ASP.NET Core saem na lista <c>errors</c> do Tooark (ligado por padrão).
  /// </para>
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configure">
  /// Ação opcional sobre as <see cref="ProblemDetailsOptions"/> do ASP.NET Core, como o
  /// <c>CustomizeProblemDetails</c>, aplicada a todo Problem Details da aplicação.
  /// </param>
  /// <returns>A coleção de serviços com o Problem Details configurado.</returns>
  public static IServiceCollection AddTooarkProblemDetails(
    this IServiceCollection services,
    Action<ProblemDetailsOptions>? configure = null
  )
  {
    // O serviço de Problem Details do ASP.NET Core, com as opções da aplicação quando informadas
    if (configure is null)
    {
      services.AddProblemDetails();
    }
    else
    {
      services.AddProblemDetails(configure);
    }

    // As opções do Problem Details são lidas pelo escritor do ASP.NET Core; o host já as registra, mas um
    // container montado à mão não
    services.AddOptions();

    // O manipulador entra uma única vez: o AddExceptionHandler do ASP.NET Core repetiria o registro
    services.TryAddEnumerable(ServiceDescriptor.Singleton<IExceptionHandler, TooarkExceptionHandler>());

    // A conversão dos problemas de validação fica registrada e age enquanto a opção estiver ligada; o MVC escreve
    // os dele pelos formatadores, e os demais passam pelo serviço de Problem Details
    services.TryAddEnumerable(
      ServiceDescriptor.Singleton<IPostConfigureOptions<ProblemDetailsOptions>, ValidationProblemPostConfigure>());
    services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<MvcOptions>, ValidationProblemMvcSetup>());

    // Retorna os serviços
    return services;
  }
}
