using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Tooark.Attributes;

namespace Tooark.AspNetCore.ModelBinding;

/// <summary>
/// Retira o <see cref="RequiredAttribute"/> que o MVC infere nos membros validados por um atributo do
/// <c>Tooark.Attributes</c>.
/// </summary>
/// <remarks>
/// Com <c>Nullable</c> habilitado, o MVC trata todo tipo de referência não anulável como se tivesse
/// <see cref="RequiredAttribute"/>. Os atributos do Tooark já reportam o valor ausente com a chave
/// <c>Field.Required</c>, então o campo ausente recebia duas mensagens, e a do framework não é chave de tradução.
/// <para>
/// Só sai o <see cref="RequiredAttribute"/> que o membro não declara. Um <c>[Required]</c> explícito é escolha de
/// quem escreveu o DTO e fica, e os membros sem atributo do Tooark mantêm a inferência.
/// </para>
/// <para>
/// O membro continua marcado como obrigatório (<see cref="ValidationMetadata.IsRequired"/>), o que é verdade: o
/// atributo recusa o valor ausente. É essa marca que faz o MVC validar um parâmetro de action que não chegou.
/// </para>
/// <para>
/// Precisa rodar depois do provedor do DataAnnotations, que é quem acrescenta o atributo inferido.
/// </para>
/// </remarks>
internal sealed class ImplicitRequiredMetadataProvider : IValidationMetadataProvider
{
  /// <summary>
  /// Retira o <see cref="RequiredAttribute"/> inferido quando o membro tem um atributo do Tooark.
  /// </summary>
  /// <param name="context">O contexto com os metadados de validação do membro.</param>
  public void CreateValidationMetadata(ValidationMetadataProviderContext context)
  {
    var validators = context.ValidationMetadata.ValidatorMetadata;

    // Sem atributo do Tooark, a inferência do MVC é a única garantia de obrigatoriedade e fica
    if (!validators.OfType<TooarkValidationAttribute>().Any())
    {
      return;
    }

    // Retira o RequiredAttribute que o membro não declara, de trás para frente para não pular índices
    for (var i = validators.Count - 1; i >= 0; i--)
    {
      if (validators[i] is RequiredAttribute required && !context.Attributes.Contains(required))
      {
        validators.RemoveAt(i);
      }
    }
  }
}
