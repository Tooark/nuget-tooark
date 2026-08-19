using System.ComponentModel.DataAnnotations;
using Tooark.Attributes;

namespace Tooark.Tests.Attributes;

public class LinkVideoValidationAttributeTest
{
  // Instância do atributo de LinkVideoValidationAttributeTest para ser testado
  private readonly LinkVideoValidationAttribute _linkVideoValidationAttribute = new();

  // Testa se o link é válido
  [Theory]
  [InlineData("https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1")]
  [InlineData("https://youtu.be/b6-JNeXxN3s?si=Q3mm2FArjTqUf7I6")]
  [InlineData("https://youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1")]
  [InlineData("https://www.youtube.com/embed/b6-JNeXxN3s?si=9ON8XlL6BaqygVkW")]
  [InlineData("https://vimeo.com/24812648")]
  [InlineData("https://vimeo.com/24812648?share=copy")]
  [InlineData("https://player.vimeo.com/video/24812648?badge=0&autopause=0&player_id=0&app_id=58479")]
  [InlineData("https://www.dailymotion.com/video/x9fbqre")]
  [InlineData("https://dai.ly/x9fbqre")]
  [InlineData("https://geo.dailymotion.com/player.html?video=x9fbqre")]
  public void IsValid_ShouldBeValid_WhenGivenValidLinkVideo(string? link)
  {
    // Arrange & Act
    var result = _linkVideoValidationAttribute.IsValid(link);

    // Assert
    Assert.True(result);
  }

  // Testa se o link é inválido
  [Theory]
  [InlineData("https://www.y0utube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1")]
  [InlineData("https://vime0.com/24812648")]
  [InlineData("https://www.dailym0tion.com/video/x9fbqre")]
  [InlineData("https://invalidlink.com")]
  [InlineData("")]
  [InlineData(null)]
  public void IsValid_ShouldBeInvalid_WhenGivenInvalidLinkVideo(string? link)
  {
    // Arrange & Act
    var result = _linkVideoValidationAttribute.IsValid(link);

    // Assert
    Assert.False(result);
  }

  // Teste de link válido passando parâmetros
  [Theory]
  [InlineData("https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1", true, false, false)]
  [InlineData("https://vimeo.com/24812648", false, true, false)]
  [InlineData("https://www.dailymotion.com/video/x9fbqre", false, false, true)]
  public void IsValid_ShouldBeValid_WhenGivenParam(string? link, bool youtube, bool vimeo, bool dailymotion)
  {
    // Arrange
    string title = "TestTitle";
    LinkVideoValidationAttribute _linkVideoParam = new(title, youtube, vimeo, dailymotion);

    // Arrange & Act
    var result = _linkVideoParam.IsValid(link);

    // Assert
    Assert.True(result);
  }

  // Teste de link inválido passando parâmetros
  [Theory]
  [InlineData("https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1", false, true, true)]
  [InlineData("https://vimeo.com/24812648", true, false, true)]
  [InlineData("https://www.dailymotion.com/video/x9fbqre", true, true, false)]
  public void IsValid_ShouldBeInvalid_WhenGivenParam(string? link, bool youtube, bool vimeo, bool dailymotion)
  {
    // Arrange
    string title = "TestTitle";
    LinkVideoValidationAttribute _linkVideoParam = new(title, youtube, vimeo, dailymotion);

    // Arrange & Act
    var result = _linkVideoParam.IsValid(link);

    // Assert
    Assert.False(result);
    Assert.Equal($"Field.Invalid;{title}", Mensagem(_linkVideoParam, link));
  }

  // Teste de link inválido com link nulo ou vazio
  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void IsValid_ShouldBeInvalid_WhenGivenParamNullOrEmpty(string? link)
  {
    // Arrange & Act
    var result = _linkVideoValidationAttribute.IsValid(link);

    // Assert
    Assert.False(result);
    Assert.Equal("Field.Required;Link", Mensagem(_linkVideoValidationAttribute, link));
  }

  // A mensagem passou a vir no resultado da validacao, e nao do estado do atributo.
  private static string? Mensagem(ValidationAttribute atributo, object? valor) =>
    atributo.GetValidationResult(valor, new ValidationContext(new object()))?.ErrorMessage;
}
