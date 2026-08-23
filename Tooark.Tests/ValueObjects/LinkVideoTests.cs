using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class LinkVideoTests
{
  // Testa se o link de vídeo é válido a partir de um link de vídeo válido
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
  public void LinkVideo_ShouldBeValid_WhenGivenValidLinkVideo(string linkParam)
  {
    // Arrange
    var expectedValue = linkParam;

    // Act
    LinkVideo linkVideo = new(linkParam);

    // Assert
    Assert.True(linkVideo.IsValid);
    Assert.Equal(expectedValue, linkVideo.Link);
  }

  // Testa se o link de vídeo é inválido a partir de um link de vídeo inválido
  [Theory]
  [InlineData("https://www.y0utube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1")]
  [InlineData("https://vime0.com/24812648")]
  [InlineData("https://www.dailym0tion.com/video/x9fbqre")]
  [InlineData("https://invalidlink.com")]
  [InlineData("")]
  [InlineData(null)]
  public void LinkVideo_ShouldBeInvalid_WhenGivenInvalidLinkVideo(string? linkParam)
  {
    // Arrange & Act
    var linkVideo = new LinkVideo(linkParam!);

    // Assert
    Assert.False(linkVideo.IsValid);
    Assert.Equal(string.Empty, linkVideo.Link);
  }

  // Testa se o link de vídeo é válido a partir de um link de vídeo válido para os parâmetros de validação
  [Theory]
  [InlineData("https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1", true, true, true)] // Todos os parâmetros ativados, considera regra padrão
  [InlineData("https://vimeo.com/24812648", true, true, true)] // Todos os parâmetros ativados, considera regra padrão
  [InlineData("https://www.dailymotion.com/video/x9fbqre", true, true, true)] // Todos os parâmetros ativados, considera regra padrão
  [InlineData("https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1", true, false, false)] // Só permite YouTube
  [InlineData("https://vimeo.com/24812648", false, true, false)] // Só permite Vimeo
  [InlineData("https://www.dailymotion.com/video/x9fbqre", false, false, true)] // Só permite Dailymotion
  public void LinkVideo_ShouldBeValid_WhenGivenParams(string link, bool youtube, bool vimeo, bool dailymotion)
  {
    // Arrange
    var expectedValue = link;

    // Act
    LinkVideo linkVideo = new(link, youtube, vimeo, dailymotion);

    // Assert
    Assert.True(linkVideo.IsValid);
    Assert.Equal(expectedValue, linkVideo.Link);
  }

  // Testa se o link de vídeo é inválido a partir de um link de vídeo inválido para os parâmetros de validação
  [Theory]
  [InlineData("https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1", false, false, false)] // Todos os parâmetros desativados
  [InlineData("https://vimeo.com/24812648", false, false, false)] // Todos os parâmetros desativados
  [InlineData("https://www.dailymotion.com/video/x9fbqre", false, false, false)] // Todos os parâmetros desativados
  [InlineData("https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1", false, true, true)] // Só permite Vimeo e Dailymotion
  [InlineData("https://vimeo.com/24812648", true, false, true)] // Só permite YouTube e Dailymotion
  [InlineData("https://www.dailymotion.com/video/x9fbqre", true, true, false)] // Só permite YouTube e Vimeo
  public void LinkVideo_ShouldBeInvalid_WhenGivenParams(string link, bool youtube, bool vimeo, bool dailymotion)
  {
    // Arrange & Act
    LinkVideo linkVideo = new(link, youtube, vimeo, dailymotion);

    // Assert
    Assert.False(linkVideo.IsValid);
    Assert.Equal(string.Empty, linkVideo.Link);
  }

  // Testa se o método ToString retorna o código do idioma
  [Fact]
  public void LinkVideo_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var linkVideoValue = "https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1";
    var expectedCode = linkVideoValue;
    var linkVideo = new LinkVideo(linkVideoValue);

    // Act
    var linkVideoString = linkVideo.ToString();

    // Assert
    Assert.Equal(expectedCode, linkVideoString);
  }

  // Testa se o endereço de linkVideo está sendo convertido para string implicitamente
  [Fact]
  public void LinkVideo_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var linkVideoValue = "https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1";
    var expectedCode = linkVideoValue;
    var linkVideo = new LinkVideo(linkVideoValue);

    // Act
    string linkVideoString = linkVideo;

    // Assert
    Assert.Equal(expectedCode, linkVideoString);
  }

  // Testa se o endereço de linkVideo está sendo convertido de string implicitamente
  [Fact]
  public void LinkVideo_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var linkVideoValue = "https://www.youtube.com/watch?v=b6-JNeXxN3s&list=RDMMb6-JNeXxN3s&start_radio=1";
    var expectedCode = linkVideoValue;

    // Act
    LinkVideo linkVideo = linkVideoValue;

    // Assert
    Assert.True(linkVideo.IsValid);
    Assert.Equal(expectedCode, linkVideo.Link);
  }
}
