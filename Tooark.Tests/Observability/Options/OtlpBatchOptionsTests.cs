using Tooark.Observability.Options;

namespace Tooark.Tests.Observability.Options;

public class OtlpBatchOptionsTests
{
  #region Default Values

  // Teste para verificar os valores padrão.
  [Fact]
  public void OtlpBatchOptions_ShouldHaveCorrectDefaultValues()
  {
    // Arrange & Act
    OtlpBatchOptions options = new();

    // Assert
    Assert.Equal(2048, options.MaxQueueSize);
    Assert.Equal(5000, options.ScheduledDelayMilliseconds);
    Assert.Equal(30000, options.ExporterTimeoutMilliseconds);
    Assert.Equal(512, options.MaxExportBatchSize);
  }

  #endregion

  #region Property Setters

  // Teste para definir MaxQueueSize.
  [Theory]
  [InlineData(1)]
  [InlineData(1024)]
  [InlineData(2048)]
  [InlineData(4096)]
  public void OtlpBatchOptions_ShouldSetMaxQueueSize(int value)
  {
    // Arrange
    OtlpBatchOptions options = new();

    // Act
    options.MaxQueueSize = value;

    // Assert
    Assert.Equal(value, options.MaxQueueSize);
  }

  // Teste para definir ScheduledDelayMilliseconds.
  [Theory]
  [InlineData(0)]
  [InlineData(2500)]
  [InlineData(5000)]
  [InlineData(10000)]
  public void OtlpBatchOptions_ShouldSetScheduledDelayMilliseconds(int value)
  {
    // Arrange
    OtlpBatchOptions options = new();

    // Act
    options.ScheduledDelayMilliseconds = value;

    // Assert
    Assert.Equal(value, options.ScheduledDelayMilliseconds);
  }

  // Teste para definir ExporterTimeoutMilliseconds.
  [Theory]
  [InlineData(1)]
  [InlineData(15000)]
  [InlineData(30000)]
  [InlineData(60000)]
  public void OtlpBatchOptions_ShouldSetExporterTimeoutMilliseconds(int value)
  {
    // Arrange
    OtlpBatchOptions options = new();

    // Act
    options.ExporterTimeoutMilliseconds = value;

    // Assert
    Assert.Equal(value, options.ExporterTimeoutMilliseconds);
  }

  // Teste para definir MaxExportBatchSize.
  [Theory]
  [InlineData(1)]
  [InlineData(256)]
  [InlineData(512)]
  [InlineData(2048)]
  public void OtlpBatchOptions_ShouldSetMaxExportBatchSize(int value)
  {
    // Arrange
    OtlpBatchOptions options = new();

    // Act
    options.MaxExportBatchSize = value;

    // Assert
    Assert.Equal(value, options.MaxExportBatchSize);
  }


  // Teste para definir MaxQueueSize com valor inválido.
  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void OtlpBatchOptions_InvalidMaxQueueSize_ShouldSetDefault(int value)
  {
    // Arrange
    OtlpBatchOptions options = new();

    // Act
    options.MaxQueueSize = value;

    // Assert
    Assert.Equal(2048, options.MaxQueueSize);
  }

  // Teste para definir ScheduledDelayMilliseconds com valor inválido.
  [Theory]
  [InlineData(-1)]
  [InlineData(-2)]
  public void OtlpBatchOptions_InvalidScheduledDelayMilliseconds_ShouldSetDefault(int value)
  {
    // Arrange
    OtlpBatchOptions options = new();

    // Act
    options.ScheduledDelayMilliseconds = value;

    // Assert
    Assert.Equal(5000, options.ScheduledDelayMilliseconds);
  }

  // Teste para definir ExporterTimeoutMilliseconds com valor inválido.
  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void OtlpBatchOptions_IInvalidExporterTimeoutMilliseconds_ShouldSetDefault(int value)
  {
    // Arrange
    OtlpBatchOptions options = new();

    // Act
    options.ExporterTimeoutMilliseconds = value;

    // Assert
    Assert.Equal(30000, options.ExporterTimeoutMilliseconds);
  }

  // Teste para definir MaxExportBatchSize com valor inválido.
  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void OtlpBatchOptions_InvalidMaxExportBatchSize_ShouldSetDefault(int value)
  {
    // Arrange
    OtlpBatchOptions options = new();

    // Act
    options.MaxExportBatchSize = value;

    // Assert
    Assert.Equal(512, options.MaxExportBatchSize);
  }

  // Teste para definir MaxExportBatchSize com valor maior que MaxQueueSize.
  [Theory]
  [InlineData(2049)]
  [InlineData(5000)]
  public void OtlpBatchOptions_MaxExportBatchSizeBiggerThanMaxQueueSize_ShouldSetMaxQueueSize(int value)
  {
    // Arrange
    OtlpBatchOptions options = new();

    // Act
    options.MaxExportBatchSize = value;

    // Assert
    Assert.Equal(2048, options.MaxExportBatchSize);
  }

  #endregion

  #region Combined Configurations

  // Teste para configurar todas as opções.
  [Fact]
  public void OtlpBatchOptions_ShouldAllowFullConfiguration()
  {
    // Arrange & Act
    var options = new OtlpBatchOptions
    {
      MaxQueueSize = 4096,
      ScheduledDelayMilliseconds = 3000,
      ExporterTimeoutMilliseconds = 15000,
      MaxExportBatchSize = 256
    };

    // Assert
    Assert.Equal(4096, options.MaxQueueSize);
    Assert.Equal(3000, options.ScheduledDelayMilliseconds);
    Assert.Equal(15000, options.ExporterTimeoutMilliseconds);
    Assert.Equal(256, options.MaxExportBatchSize);
  }

  // Teste para configuração de alta performance.
  [Fact]
  public void OtlpBatchOptions_ShouldAllowHighPerformanceConfiguration()
  {
    // Arrange & Act
    var options = new OtlpBatchOptions
    {
      MaxQueueSize = 8192,
      ScheduledDelayMilliseconds = 1000,
      ExporterTimeoutMilliseconds = 5000,
      MaxExportBatchSize = 1024
    };

    // Assert
    Assert.Equal(8192, options.MaxQueueSize);
    Assert.Equal(1000, options.ScheduledDelayMilliseconds);
    Assert.Equal(5000, options.ExporterTimeoutMilliseconds);
    Assert.Equal(1024, options.MaxExportBatchSize);
  }

  // Teste para configuração de baixa latência.
  [Fact]
  public void OtlpBatchOptions_ShouldAllowLowLatencyConfiguration()
  {
    // Arrange & Act
    var options = new OtlpBatchOptions
    {
      MaxQueueSize = 512,
      ScheduledDelayMilliseconds = 500,
      ExporterTimeoutMilliseconds = 5000,
      MaxExportBatchSize = 64
    };

    // Assert
    Assert.Equal(512, options.MaxQueueSize);
    Assert.Equal(500, options.ScheduledDelayMilliseconds);
    Assert.Equal(5000, options.ExporterTimeoutMilliseconds);
    Assert.Equal(64, options.MaxExportBatchSize);
  }

  // Teste para configuração com MaxExportBatchSize maior que MaxQueueSize e MaxQueueSize com valor definido.
  [Fact]
  public void OtlpBatchOptions_WithMaxExportBatchSizeGreaterThanMaxQueueSize()
  {
    // Arrange & Act
    var options = new OtlpBatchOptions
    {
      MaxQueueSize = 512,
      MaxExportBatchSize = 5000
    };

    // Assert
    Assert.Equal(512, options.MaxQueueSize);
    Assert.Equal(5000, options.ScheduledDelayMilliseconds);
    Assert.Equal(30000, options.ExporterTimeoutMilliseconds);
    Assert.Equal(512, options.MaxExportBatchSize);
  }

  #endregion
}
