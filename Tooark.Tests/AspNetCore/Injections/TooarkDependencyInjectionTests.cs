using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tooark.AspNetCore.Injections;
using Tooark.Dtos;
using Tooark.Utils;

namespace Tooark.Tests.AspNetCore.Injections;

/// <summary>
/// Testes da resposta de validação registrada pelo AddTooarkModelStateEnvelope.
/// </summary>
[Collection("CultureSensitive")]
public class TooarkDependencyInjectionTests
{
  // Monta as opções como a aplicação monta e chama a factory com o ModelState informado
  private static IActionResult InvalidModelStateResponse(IServiceCollection services, ModelStateDictionary modelState)
  {
    using var provider = services.BuildServiceProvider();

    var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
    var context = new ActionContext(
      new DefaultHttpContext { RequestServices = provider },
      new RouteData(),
      new ActionDescriptor(),
      modelState
    );

    return options.InvalidModelStateResponseFactory(context);
  }

  // ModelState com uma mensagem de validador e outra do model binding, que não é chave de tradução
  private static ModelStateDictionary InvalidModelState()
  {
    var modelState = new ModelStateDictionary();
    modelState.AddModelError("Name", "Name is mandatory");
    modelState.AddModelError("Age", "The value 'abc' is not valid for Age.");

    return modelState;
  }

  // Testa se a falha de validação vira uma resposta 400 com o ResponseDto
  [Fact]
  public void AddTooarkModelStateEnvelope_ShouldRespondWithResponseDto_WhenModelStateIsInvalid()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddControllers();
    services.AddTooarkModelStateEnvelope();

    // Act
    var result = InvalidModelStateResponse(services, InvalidModelState());

    // Assert
    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    var response = Assert.IsType<ResponseDto<object>>(badRequest.Value);
    Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    Assert.Equal(2, response.Errors.Count);
    Assert.Contains("Name is mandatory", response.Errors);
    Assert.Contains("The value 'abc' is not valid for Age.", response.Errors);
    Assert.Null(response.Data);
    Assert.Null(response.Pagination);
    Assert.Empty(response.Metadata);
  }

  // Testa se a factory do pacote vence a do AddControllers mesmo quando registrada antes dele
  [Fact]
  public void AddTooarkModelStateEnvelope_ShouldReplaceDefaultFactory_WhenCalledBeforeAddControllers()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTooarkModelStateEnvelope();
    services.AddControllers();

    // Act
    var result = InvalidModelStateResponse(services, InvalidModelState());

    // Assert
    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    Assert.IsType<ResponseDto<object>>(badRequest.Value);
  }

  // Testa se as chaves de tradução e o erro sem texto chegam traduzidos na resposta
  [Fact]
  public void AddTooarkModelStateEnvelope_ShouldTranslateErrorKeys()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var services = new ServiceCollection();
    services.AddControllers();
    services.AddTooarkModelStateEnvelope();

    var modelState = new ModelStateDictionary();
    modelState.AddModelError("Email", "Field.Required;Email");
    modelState.TryAddModelException("Age", new FormatException("Internal detail"));

    // Act
    var result = InvalidModelStateResponse(services, modelState);

    // Assert
    var response = Assert.IsType<ResponseDto<object>>(Assert.IsType<BadRequestObjectResult>(result).Value);
    Assert.Equal(2, response.Errors.Count);
    Assert.Contains("O campo E-mail é obrigatório", response.Errors);
    Assert.Contains("O campo Age é inválido", response.Errors);
  }

  // Testa se chamar o registro duas vezes mantém uma única resposta
  [Fact]
  public void AddTooarkModelStateEnvelope_ShouldBeIdempotent()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddControllers();
    services.AddTooarkModelStateEnvelope();
    services.AddTooarkModelStateEnvelope();

    // Act
    var result = InvalidModelStateResponse(services, InvalidModelState());

    // Assert
    var response = Assert.IsType<ResponseDto<object>>(Assert.IsType<BadRequestObjectResult>(result).Value);
    Assert.Equal(2, response.Errors.Count);
  }
}
