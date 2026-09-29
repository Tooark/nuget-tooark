using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Tooark.AspNetCore.Extensions;
using Tooark.Dtos;

namespace Tooark.AspNetCore.Injections;

/// <summary>
/// Classe de extensão para adicionar as integrações do ASP.NET Core ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Faz a falha de validação dos controllers com <see cref="ApiControllerAttribute"/> responder com o
  /// <see cref="ResponseDto{T}"/>, no lugar do <see cref="ValidationProblemDetails"/> padrão do ASP.NET Core.
  /// </summary>
  /// <remarks>
  /// Substitui o <see cref="ApiBehaviorOptions.InvalidModelStateResponseFactory"/> por uma resposta 400 com um
  /// <c>ResponseDto&lt;object&gt;</c>, cujos erros são os de
  /// <see cref="ModelStateExtension.GetErrors(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary)"/>
  /// já traduzidos, e <c>Data</c> nulo.
  /// <para>
  /// A factory é atribuída depois de todas as configurações das opções, então a ordem em relação ao
  /// <c>AddControllers</c> não importa: o <c>AddControllers</c> atribui a factory padrão ao montar as opções e,
  /// sem isso, venceria quando chamado depois.
  /// </para>
  /// <para>
  /// Só vale onde o ASP.NET Core usa a factory: actions de controllers com <see cref="ApiControllerAttribute"/>,
  /// sem <see cref="ApiBehaviorOptions.SuppressModelStateInvalidFilter"/>. Controllers sem o atributo e as
  /// minimal APIs não passam por ela.
  /// </para>
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <returns>A coleção de serviços com a resposta de validação configurada.</returns>
  public static IServiceCollection AddTooarkModelStateEnvelope(this IServiceCollection services)
  {
    // Substitui a factory depois das configurações, inclusive a do AddControllers
    services.PostConfigure<ApiBehaviorOptions>(options =>
      options.InvalidModelStateResponseFactory = ModelStateEnvelope);

    // Retorna os serviços
    return services;
  }

  /// <summary>
  /// Monta a resposta de uma falha de validação.
  /// </summary>
  /// <param name="context">Contexto da action com o ModelState inválido.</param>
  /// <returns>Uma resposta 400 com os erros do ModelState.</returns>
  private static BadRequestObjectResult ModelStateEnvelope(ActionContext context) =>
    new(new ResponseDto<object>(context.ModelState.GetErrors()));
}
