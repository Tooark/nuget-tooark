using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Tooark.AspNetCore.Extensions;
using Tooark.AspNetCore.ModelBinding;
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
  /// <para>
  /// Muda o corpo da resposta, não as mensagens que a validação registra. Com <c>Nullable</c> habilitado, um
  /// campo não anulável com atributo do <c>Tooark.Attributes</c> chega ausente com duas mensagens: a chave
  /// <c>Field.Required</c>, traduzida, e o texto do <see cref="RequiredAttribute"/> que o MVC infere, que não é
  /// chave de tradução e chega como o framework o escreve. Quem retira a segunda é o
  /// <see cref="AddTooarkValidationAttributes"/>, um registro à parte: a aplicação que valida os DTOs com os
  /// atributos do Tooark chama os dois.
  /// </para>
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <returns>A coleção de serviços com a resposta de validação configurada.</returns>
  /// <seealso cref="AddTooarkValidationAttributes"/>
  public static IServiceCollection AddTooarkModelStateEnvelope(this IServiceCollection services)
  {
    // Substitui a factory depois das configurações, inclusive a do AddControllers
    services.PostConfigure<ApiBehaviorOptions>(options =>
      options.InvalidModelStateResponseFactory = ModelStateEnvelope);

    // Retorna os serviços
    return services;
  }

  /// <summary>
  /// Faz o MVC deixar de inferir <see cref="RequiredAttribute"/> nos membros validados por um atributo do
  /// <c>Tooark.Attributes</c>, que já reporta o valor ausente.
  /// </summary>
  /// <remarks>
  /// Com <c>Nullable</c> habilitado, o MVC trata todo tipo de referência não anulável como se tivesse
  /// <see cref="RequiredAttribute"/>, com a mensagem do framework. Um campo com atributo do Tooark recebia então
  /// duas mensagens para o mesmo valor ausente: a chave <c>Field.Required</c> do atributo e o texto do framework,
  /// que não é chave de tradução. Com este registro, fica só a chave.
  /// <para>
  /// O alcance é o dos atributos do Tooark: um <c>[Required]</c> declarado no membro continua valendo, e os
  /// membros sem atributo do Tooark mantêm a inferência. É a diferença para
  /// <see cref="MvcOptions.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes"/>, que desliga a
  /// inferência em todos os DTOs.
  /// </para>
  /// <para>
  /// Aplicado depois de todas as configurações das opções, então a ordem em relação ao <c>AddControllers</c> não
  /// importa, e chamar mais de uma vez não repete o registro. Vale para a validação do MVC, com ou sem
  /// <see cref="ApiControllerAttribute"/>. As minimal APIs não passam pelas opções do MVC.
  /// </para>
  /// <para>
  /// Não depende do <see cref="AddTooarkModelStateEnvelope"/>, e nenhum dos dois liga o outro: este escolhe as
  /// mensagens do campo ausente, e o envelope, o corpo em que elas chegam ao cliente.
  /// </para>
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <returns>A coleção de serviços com a validação dos atributos configurada.</returns>
  /// <seealso cref="AddTooarkModelStateEnvelope"/>
  public static IServiceCollection AddTooarkValidationAttributes(this IServiceCollection services)
  {
    // Acrescenta o provedor depois das configurações, para rodar depois do provedor do DataAnnotations
    services.PostConfigure<MvcOptions>(options =>
    {
      // Um segundo registro encontra o provedor já na lista e não o repete
      if (!options.ModelMetadataDetailsProviders.OfType<ImplicitRequiredMetadataProvider>().Any())
      {
        options.ModelMetadataDetailsProviders.Add(new ImplicitRequiredMetadataProvider());
      }
    });

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
