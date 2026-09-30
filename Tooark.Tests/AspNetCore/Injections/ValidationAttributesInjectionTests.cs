using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.AspNetCore.Injections;
using Tooark.Attributes;
using Tooark.Attributes.Messages;

namespace Tooark.Tests.AspNetCore.Injections;

/// <summary>
/// Testes da validação dos atributos do Tooark registrada pelo AddTooarkValidationAttributes.
/// </summary>
public class ValidationAttributesInjectionTests
{
  // Chave que o atributo reporta para o e-mail ausente
  private const string EmailRequired = $"{AttributeErrorMessages.FieldRequired};Email";

  // Record com um campo validado pelo Tooark e outro sem atributo, os dois não anuláveis
  private sealed record ComEmailENome([EmailValidation] string Email, string Nome);

  // Record com o [Required] declarado ao lado do atributo do Tooark
  private sealed record ComRequiredExplicito([Required][EmailValidation] string Email);

  // Classe com o atributo na propriedade
  private sealed class ComPropriedade
  {
    [EmailValidation]
    public string Email { get; set; } = null!;
  }

  // Controller com o atributo direto no parâmetro da action
  private sealed class ComParametroNaAction
  {
    public static void Buscar([EmailValidation] string email) => _ = email;
  }

  // Monta o MVC com o registro antes ou depois do AddControllers
  private static ServiceProvider CriarMvc(bool registrarAntes = false)
  {
    var services = new ServiceCollection();

    if (registrarAntes)
    {
      services.AddTooarkValidationAttributes();
      services.AddControllers();
    }
    else
    {
      services.AddControllers();
      services.AddTooarkValidationAttributes();
    }

    return services.BuildServiceProvider();
  }

  // Contexto da action com um ModelState vazio
  private static ActionContext CriarContexto(IServiceProvider provider) =>
    new(new DefaultHttpContext { RequestServices = provider }, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

  // Valida o modelo pelo validador do MVC, o mesmo que roda depois do binding, e devolve o ModelState
  private static ModelStateDictionary ValidarNoMvc(object modelo, bool registrarAntes = false)
  {
    using var provider = CriarMvc(registrarAntes);
    var contexto = CriarContexto(provider);

    provider.GetRequiredService<IObjectModelValidator>().Validate(contexto, null, string.Empty, modelo);

    return contexto.ModelState;
  }

  // Mensagens de erro registradas para um campo
  private static List<string> Erros(ModelStateDictionary modelState, string campo) =>
    modelState.TryGetValue(campo, out var entrada) ? [.. entrada.Errors.Select(e => e.ErrorMessage)] : [];

  // Mensagem do [Required] do framework para o campo
  private static string RequiredDoFramework(string campo) => new RequiredAttribute().FormatErrorMessage(campo);

  // Testa se o campo ausente com atributo do Tooark fica só com a chave, com o registro antes ou depois do MVC
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void AddTooarkValidationAttributes_ShouldReportOnlyAttributeKey_WhenValueIsMissing(bool registrarAntes)
  {
    // Arrange & Act
    var modelState = ValidarNoMvc(new ComEmailENome(null!, "Ana"), registrarAntes);

    // Assert
    Assert.Equal([EmailRequired], Erros(modelState, "Email"));
  }

  // Testa se o campo sem atributo do Tooark mantém o [Required] que o MVC infere
  [Fact]
  public void AddTooarkValidationAttributes_ShouldKeepInferredRequired_WhenMemberHasNoTooarkAttribute()
  {
    // Arrange & Act
    var modelState = ValidarNoMvc(new ComEmailENome("valido@teste.com", null!));

    // Assert
    Assert.Equal([RequiredDoFramework("Nome")], Erros(modelState, "Nome"));
  }

  // Testa se o [Required] declarado no membro continua valendo ao lado do atributo do Tooark
  [Fact]
  public void AddTooarkValidationAttributes_ShouldKeepExplicitRequired()
  {
    // Arrange & Act
    var erros = Erros(ValidarNoMvc(new ComRequiredExplicito(null!)), "Email");

    // Assert
    Assert.Equal(2, erros.Count);
    Assert.Contains(EmailRequired, erros);
    Assert.Contains(RequiredDoFramework("Email"), erros);
  }

  // Testa se a propriedade de uma classe também fica só com a chave do atributo
  [Fact]
  public void AddTooarkValidationAttributes_ShouldReportOnlyAttributeKey_WhenClassPropertyIsMissing()
  {
    // Arrange & Act
    var modelState = ValidarNoMvc(new ComPropriedade());

    // Assert
    Assert.Equal([EmailRequired], Erros(modelState, "Email"));
  }

  // Testa se o parâmetro de action ausente é validado e fica só com a chave do atributo
  [Fact]
  public void AddTooarkValidationAttributes_ShouldReportOnlyAttributeKey_WhenActionParameterIsMissing()
  {
    // Arrange
    using var provider = CriarMvc();
    var contexto = CriarContexto(provider);
    var parametro = typeof(ComParametroNaAction).GetMethod(nameof(ComParametroNaAction.Buscar))!.GetParameters()[0];
    var metadata = ((ModelMetadataProvider)provider.GetRequiredService<IModelMetadataProvider>()).GetMetadataForParameter(parametro);
    var validador = (ObjectModelValidator)provider.GetRequiredService<IObjectModelValidator>();

    // Act
    validador.Validate(contexto, null, "email", null, metadata);

    // Assert
    Assert.Equal([EmailRequired], Erros(contexto.ModelState, "email"));
  }

  // Testa se o valor inválido continua reprovado pelo atributo
  [Fact]
  public void AddTooarkValidationAttributes_ShouldKeepAttributeValidation_WhenValueIsInvalid()
  {
    // Arrange & Act
    var modelState = ValidarNoMvc(new ComEmailENome("nao-e-email", "Ana"));

    // Assert
    Assert.Equal([$"{AttributeErrorMessages.FieldInvalid};Email"], Erros(modelState, "Email"));
  }

  // Testa se chamar o registro duas vezes acrescenta o provedor uma vez só
  [Fact]
  public void AddTooarkValidationAttributes_ShouldAddProviderOnce_WhenCalledTwice()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddControllers();
    services.AddTooarkValidationAttributes();
    services.AddTooarkValidationAttributes();
    using var provider = services.BuildServiceProvider();

    // Act
    var options = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
    var doPacote = options.ModelMetadataDetailsProviders
      .Count(p => p.GetType().Assembly == typeof(TooarkDependencyInjection).Assembly);

    // Assert
    Assert.Equal(1, doPacote);
  }
}
