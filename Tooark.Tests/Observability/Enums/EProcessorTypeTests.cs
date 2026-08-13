using OpenTelemetry;
using Tooark.Exceptions;
using Tooark.Observability.Enums;

namespace Tooark.Tests.Observability.Enums;

public class EProcessorTypeTests
{
  #region Implicit Conversions - From Int

  // Teste para converter de int para EProcessorType e retornar o int, string e ExportProcessorType corretos.
  [Theory]
  [InlineData(0, "simple", ExportProcessorType.Simple)]
  [InlineData(1, "batch", ExportProcessorType.Batch)]
  public void EProcessorType_ShouldBeValid_WhenGivenId(int id, string description, ExportProcessorType type)
  {
    // Arrange
    EProcessorType processorType = id;

    // Act
    int processorTypeId = processorType;
    string processorTypeDescription = processorType;
    ExportProcessorType processorTypeType = processorType;

    // Assert
    Assert.Equal(id, processorTypeId);
    Assert.Equal(description, processorTypeDescription);
    Assert.Equal(type, processorTypeType);
  }

  // Teste para lançar exceção quando o id é inválido.
  [Theory]
  [InlineData(-1)]
  [InlineData(2)]
  public void EProcessorType_ShouldThrowException_WhenIdIsInvalid(int id)
  {
    // Arrange
    int invalidId = id;

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() =>
    {
      EProcessorType processorType = invalidId;
    });
    Assert.Equal($"Invalid.Parameter;{invalidId}", ex.Message);
  }

  #endregion

  #region Implicit Conversions - From String

  // Teste para converter de string para EProcessorType e retornar a string, o int e o ExportProcessorType corretos.
  [Theory]
  [InlineData("simple", 0, "simple", ExportProcessorType.Simple)]
  [InlineData("Simple", 0, "simple", ExportProcessorType.Simple)]
  [InlineData("SIMPLE", 0, "simple", ExportProcessorType.Simple)]
  [InlineData("batch", 1, "batch", ExportProcessorType.Batch)]
  [InlineData("Batch", 1, "batch", ExportProcessorType.Batch)]
  [InlineData("BATCH", 1, "batch", ExportProcessorType.Batch)]
  public void EProcessorType_ShouldBeValid_WhenGivenDescription(string description, int id, string descriptionExpected, ExportProcessorType type)
  {
    // Arrange
    EProcessorType processorType = description;

    // Act
    string processorTypeDescription = processorType;
    int processorTypeId = processorType;
    ExportProcessorType processorTypeType = processorType;

    // Assert
    Assert.Equal(descriptionExpected, processorTypeDescription);
    Assert.Equal(id, processorTypeId);
    Assert.Equal(type, processorTypeType);
  }

  // Teste para lançar exceção quando a descrição é inválida.
  [Theory]
  [InlineData("invalid")]
  [InlineData("")]
  [InlineData(null)]
  public void EProcessorType_ShouldThrowException_WhenDescriptionIsInvalid(string? description)
  {
    // Arrange
    string? invalidDescription = description;

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() =>
    {
      EProcessorType processorType = invalidDescription!;
    });
    Assert.Equal($"Invalid.Parameter;{invalidDescription}", ex.Message);
  }

  #endregion

  #region Implicit Conversions - From ExportProcessorType

  // Teste para converter de ExportProcessorType para EProcessorType.
  [Theory]
  [InlineData(ExportProcessorType.Simple, 0, "simple")]
  [InlineData(ExportProcessorType.Batch, 1, "batch")]
  public void EProcessorType_ShouldBeValid_WhenGivenExportProcessorType(ExportProcessorType type, int expectedId, string expectedDescription)
  {
    // Arrange
    EProcessorType processorType = type;

    // Act
    int processorTypeId = processorType;
    string processorTypeDescription = processorType;
    ExportProcessorType processorTypeType = processorType;

    // Assert
    Assert.Equal(expectedId, processorTypeId);
    Assert.Equal(expectedDescription, processorTypeDescription);
    Assert.Equal(type, processorTypeType);
  }

  // Teste para lançar exceção quando ExportProcessorType é inválido (valor não definido).
  [Fact]
  public void EProcessorType_ShouldThrowException_WhenExportProcessorTypeIsInvalid()
  {
    // Arrange
    ExportProcessorType invalidType = (ExportProcessorType)99;

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() =>
    {
      EProcessorType processorType = invalidType;
    });
    Assert.Equal($"Invalid.Parameter;{invalidType}", ex.Message);
  }

  #endregion

  #region Implicit Conversions - To Types

  // Teste para converter de EProcessorType para int.
  [Fact]
  public void EProcessorType_ShouldImplicitConversionToInt()
  {
    // Arrange & Act
    int simple = EProcessorType.Simple;
    int batch = EProcessorType.Batch;

    // Assert
    Assert.Equal(0, simple);
    Assert.Equal(1, batch);
  }

  // Teste para converter de EProcessorType para string.
  [Fact]
  public void EProcessorType_ShouldImplicitConversionToString()
  {
    // Arrange & Act
    string simple = EProcessorType.Simple;
    string batch = EProcessorType.Batch;

    // Assert
    Assert.Equal("simple", simple);
    Assert.Equal("batch", batch);
  }

  // Teste para converter de EProcessorType para ExportProcessorType.
  [Fact]
  public void EProcessorType_ShouldImplicitConversionToExportProcessorType()
  {
    // Arrange & Act
    ExportProcessorType simple = EProcessorType.Simple;
    ExportProcessorType batch = EProcessorType.Batch;

    // Assert
    Assert.Equal(ExportProcessorType.Simple, simple);
    Assert.Equal(ExportProcessorType.Batch, batch);
  }

  #endregion

  #region Methods

  // Teste para converter de EProcessorType com ToInt.
  [Fact]
  public void EProcessorType_ShouldConvertWithToInt()
  {
    // Arrange & Act
    int simple = EProcessorType.Simple.ToInt();
    int batch = EProcessorType.Batch.ToInt();

    // Assert
    Assert.Equal(0, simple);
    Assert.Equal(1, batch);
  }

  // Teste para converter de EProcessorType com ToString.
  [Fact]
  public void EProcessorType_ShouldConvertWithToString()
  {
    // Arrange & Act
    string simple = EProcessorType.Simple.ToString();
    string batch = EProcessorType.Batch.ToString();

    // Assert
    Assert.Equal("simple", simple);
    Assert.Equal("batch", batch);
  }

  // Teste para converter de EProcessorType com ToType.
  [Fact]
  public void EProcessorType_ShouldConvertWithToType()
  {
    // Arrange & Act
    ExportProcessorType simple = EProcessorType.Simple.ToType();
    ExportProcessorType batch = EProcessorType.Batch.ToType();

    // Assert
    Assert.Equal(ExportProcessorType.Simple, simple);
    Assert.Equal(ExportProcessorType.Batch, batch);
  }

  #endregion

  #region Static Instances

  // Teste para verificar as instâncias estáticas.
  [Fact]
  public void EProcessorType_StaticInstances_ShouldHaveCorrectValues()
  {
    // Assert - Simple
    Assert.Equal(0, EProcessorType.Simple.ToInt());
    Assert.Equal("simple", EProcessorType.Simple.ToString());
    Assert.Equal(ExportProcessorType.Simple, EProcessorType.Simple.ToType());

    // Assert - Batch
    Assert.Equal(1, EProcessorType.Batch.ToInt());
    Assert.Equal("batch", EProcessorType.Batch.ToString());
    Assert.Equal(ExportProcessorType.Batch, EProcessorType.Batch.ToType());
  }

  #endregion

  #region Null Conversions

  // Teste para verificar que a conversão implícita de instância nula falha com erro identificável.
  [Fact]
  public void EProcessorType_ImplicitConversionFromNull_ShouldThrowInternalServerErrorException()
  {
    // Arrange - as propriedades de override são anuláveis por design, então a conversão de nulo é alcançável
    EProcessorType type = null!;

    // Act & Assert - antes cada conversão estourava NullReferenceException
    var toInt = Assert.Throws<InternalServerErrorException>(() => { int _ = type; });
    var toString = Assert.Throws<InternalServerErrorException>(() => { string _ = type; });
    var toType = Assert.Throws<InternalServerErrorException>(() => { ExportProcessorType _ = type; });

    Assert.Contains("Invalid.Parameter;null", toInt.GetErrorMessages());
    Assert.Contains("Invalid.Parameter;null", toString.GetErrorMessages());
    Assert.Contains("Invalid.Parameter;null", toType.GetErrorMessages());
  }

  #endregion
}
