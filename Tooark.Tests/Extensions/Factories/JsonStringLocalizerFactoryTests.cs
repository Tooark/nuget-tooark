using Tooark.Extensions;
using Tooark.Extensions.Factories;

namespace Tooark.Tests.Extensions.Factories;

public class JsonStringLocalizerFactoryTests
{
  // Teste para verificar se o método Create retorna um JsonStringLocalizerExtension
  [Fact]
  public void Create_WithTypeResourceSource_ReturnsJsonStringLocalizer()
  {
    // Arrange
    JsonStringLocalizerFactory factory = new();
    var resourceSource = typeof(JsonStringLocalizerFactoryTests);

    // Act
    var localizer = factory.Create(resourceSource);

    // Assert
    Assert.NotNull(localizer);
    Assert.IsType<JsonStringLocalizerExtension>(localizer);
  }

  // Teste para verificar se o método Create retorna um JsonStringLocalizerExtension
  [Fact]
  public void Create_WithBaseNameAndLocation_ReturnsJsonStringLocalizer()
  {
    // Arrange
    JsonStringLocalizerFactory factory = new();
    var baseName = "BaseName";
    var location = "Location";

    // Act
    var localizer = factory.Create(baseName, location);

    // Assert
    Assert.NotNull(localizer);
    Assert.IsType<JsonStringLocalizerExtension>(localizer);
  }
}
