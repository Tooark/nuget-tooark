using Tooark.Dtos;

namespace Tooark.Tests.Dtos;

public class MetadataDtoTests
{
  // Teste se atributos são inicializados corretamente
  [Fact]
  public void MetadataDto_ShouldInitializeProperties()
  {
    // Arrange
    var key = "TestKey";
    var value = "TestValue";

    // Act
    var metadata = new MetadataDto(key, value);

    // Assert
    Assert.Equal(key, metadata.Key);
    Assert.Equal(value, metadata.Value);
  }

  // Testa se quando a chave é nula, a propriedade Key e Value são inicializadas com string vazia
  [Fact]
  public void MetadataDto_ShouldHandleNullKey()
  {
    // Arrange
    string key = null!;
    var value = "TestValue";

    // Act
    var metadata = new MetadataDto(key, value);

    // Assert
    Assert.Equal(string.Empty, metadata.Key);
    Assert.Equal(value, metadata.Value);
  }

  // Testa se quando o valor é nulo, o Value é inicializado com string vazia
  [Fact]
  public void MetadataDto_ShouldHandleNullValue()
  {
    // Arrange
    var key = "TestKey";
    string value = null!;

    // Act
    var metadata = new MetadataDto(key, value);

    // Assert
    Assert.Equal(key, metadata.Key);
    Assert.Equal(string.Empty, metadata.Value);
  }
}
