using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Attributes;
using Tooark.Attributes.Messages;

namespace Tooark.Tests.Attributes;

/// <summary>
/// Testes dos atributos aplicados a parâmetros: records posicionais e parâmetros de action, validados pelo MVC.
/// </summary>
public class ParameterValidationTests
{
  // Record posicional com o atributo no parâmetro do construtor, onde o MVC procura a validação
  private sealed record ComEmail([EmailValidation] string Email);

  // Record posicional com o campo anulável, que o MVC não marca como obrigatório por conta própria
  private sealed record ComEmailAnulavel([EmailValidation] string? Email);

  // Record posicional com o atributo na propriedade gerada, que o MVC rejeita
  private sealed record ComAtributoNaPropriedade([property: EmailValidation] string Email);

  // Controller com o atributo direto no parâmetro da action
  private sealed class ComParametroNaAction
  {
    public static void Buscar([EmailValidation] string email) => _ = email;
  }

  // Monta o MVC como a aplicação monta, com o ajuste de opções informado
  private static ServiceProvider CriarMvc(Action<MvcOptions>? configurar = null)
  {
    var services = new ServiceCollection();
    services.AddControllers();

    if (configurar is not null)
    {
      services.Configure(configurar);
    }

    return services.BuildServiceProvider();
  }

  // Contexto da action com um ModelState vazio
  private static ActionContext CriarContexto(IServiceProvider provider) =>
    new(new DefaultHttpContext { RequestServices = provider }, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

  // Valida o modelo pelo validador do MVC, o mesmo que roda depois do binding, e devolve o ModelState
  private static ModelStateDictionary ValidarNoMvc(object modelo, Action<MvcOptions>? configurar = null)
  {
    using var provider = CriarMvc(configurar);
    var contexto = CriarContexto(provider);

    provider.GetRequiredService<IObjectModelValidator>().Validate(contexto, null, string.Empty, modelo);

    return contexto.ModelState;
  }

  // Mensagens de erro registradas para um campo
  private static List<string> Erros(ModelStateDictionary modelState, string campo) =>
    modelState.TryGetValue(campo, out var entrada) ? [.. entrada.Errors.Select(e => e.ErrorMessage)] : [];

  // Atributos de validação concretos do pacote
  public static TheoryData<Type> Atributos() =>
    [.. typeof(TooarkValidationAttribute).Assembly.GetTypes()
      .Where(t => t.IsSubclassOf(typeof(TooarkValidationAttribute)) && !t.IsAbstract)];

  // Testa se cada atributo do pacote pode ser aplicado a parâmetro, como os atributos do DataAnnotations
  [Theory]
  [MemberData(nameof(Atributos))]
  public void AttributeUsage_ShouldAllowParameter(Type atributo)
  {
    // Arrange & Act
    var uso = atributo.GetCustomAttribute<AttributeUsageAttribute>();

    // Assert
    Assert.NotNull(uso);
    Assert.True(uso.ValidOn.HasFlag(AttributeTargets.Parameter));
    Assert.True(uso.ValidOn.HasFlag(AttributeTargets.Property));
    Assert.True(uso.ValidOn.HasFlag(AttributeTargets.Field));
  }

  // Testa se o MVC valida o atributo aplicado ao parâmetro de um record posicional
  [Fact]
  public void Record_ShouldReportInvalidKey_WhenParameterValueIsInvalid()
  {
    // Arrange & Act
    var modelState = ValidarNoMvc(new ComEmail("nao-e-email"));

    // Assert
    Assert.False(modelState.IsValid);
    Assert.Equal([$"{AttributeErrorMessages.FieldInvalid};Email"], Erros(modelState, "Email"));
  }

  // Testa se o record com valor válido passa pela validação do MVC
  [Fact]
  public void Record_ShouldBeValid_WhenParameterValueIsValid()
  {
    // Arrange & Act
    var modelState = ValidarNoMvc(new ComEmail("valido@teste.com"));

    // Assert
    Assert.True(modelState.IsValid);
  }

  // Testa por que o alvo de parâmetro é necessário: o MVC lança ao achar validação na propriedade de um record
  [Fact]
  public void Record_ShouldThrow_WhenAttributeTargetsGeneratedProperty()
  {
    // Arrange
    var modelo = new ComAtributoNaPropriedade("valido@teste.com");

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() => ValidarNoMvc(modelo));
  }

  // Testa se o MVC valida o atributo aplicado direto ao parâmetro da action
  [Fact]
  public void ActionParameter_ShouldReportInvalidKey_WhenValueIsInvalid()
  {
    // Arrange
    using var provider = CriarMvc();
    var contexto = CriarContexto(provider);
    var parametro = typeof(ComParametroNaAction).GetMethod(nameof(ComParametroNaAction.Buscar))!.GetParameters()[0];
    var metadata = ((ModelMetadataProvider)provider.GetRequiredService<IModelMetadataProvider>()).GetMetadataForParameter(parametro);
    var validador = (ObjectModelValidator)provider.GetRequiredService<IObjectModelValidator>();

    // Act
    validador.Validate(contexto, null, "email", "nao-e-email", metadata);

    // Assert
    Assert.Equal([$"{AttributeErrorMessages.FieldInvalid};Email"], Erros(contexto.ModelState, "email"));
  }

  // Testa se o campo ausente recebe também o Required implícito do MVC, que acompanha o do atributo
  [Fact]
  public void Record_ShouldReportImplicitRequiredToo_WhenNonNullableValueIsMissing()
  {
    // Arrange & Act
    var erros = Erros(ValidarNoMvc(new ComEmail(null!)), "Email");

    // Assert
    Assert.Equal(2, erros.Count);
    Assert.Single(erros, e => e == $"{AttributeErrorMessages.FieldRequired};Email");
    Assert.Single(erros, e => e == new RequiredAttribute().FormatErrorMessage("Email"));
  }

  // Testa se o campo anulável deixa só a chave do atributo para o valor ausente
  [Fact]
  public void Record_ShouldReportOnlyAttributeKey_WhenNullableValueIsMissing()
  {
    // Arrange & Act
    var erros = Erros(ValidarNoMvc(new ComEmailAnulavel(null)), "Email");

    // Assert
    Assert.Equal([$"{AttributeErrorMessages.FieldRequired};Email"], erros);
  }

  // Testa se suprimir o Required implícito deixa só a chave do atributo para o valor ausente
  [Fact]
  public void Record_ShouldReportOnlyAttributeKey_WhenImplicitRequiredIsSuppressed()
  {
    // Arrange & Act
    var erros = Erros(
      ValidarNoMvc(new ComEmail(null!), o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true),
      "Email"
    );

    // Assert
    Assert.Equal([$"{AttributeErrorMessages.FieldRequired};Email"], erros);
  }

  // Testa se o Validator do DataAnnotations ignora o atributo no parâmetro, porque só lê propriedades
  [Fact]
  public void Validator_ShouldIgnoreAttribute_WhenAppliedToRecordParameter()
  {
    // Arrange
    var modelo = new ComEmail("nao-e-email");
    var resultados = new List<ValidationResult>();

    // Act
    var valido = Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, true);

    // Assert
    Assert.True(valido);
    Assert.Empty(resultados);
  }
}
