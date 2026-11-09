using Tooark.Observability;

namespace Tooark.Tests.Observability;

public class TooarkInstrumentationTests
{
  // Teste para a instrumentação expor a fonte e o medidor com os nomes e a versão recebidos.
  [Fact]
  public void Constructor_ShouldCreateActivitySourceAndMeter_WithResolvedNamesAndVersion()
  {
    // Arrange & Act
    using var instrumentation = new TooarkInstrumentation("Orders.Tracing", "Orders.Metrics", "orders-api", "2.3.0");

    // Assert
    Assert.Equal("Orders.Tracing", instrumentation.ActivitySource.Name);
    Assert.Equal("2.3.0", instrumentation.ActivitySource.Version);
    Assert.Equal("Orders.Metrics", instrumentation.Meter.Name);
    Assert.Equal("2.3.0", instrumentation.Meter.Version);
    Assert.Equal("orders-api", instrumentation.ServiceName);
    Assert.Equal("2.3.0", instrumentation.ServiceVersion);
  }

  // Teste para o nome padrão ser "Tooark", o mesmo valor padrão de ActivitySourceName e MeterName.
  [Fact]
  public void DefaultName_ShouldBeTooark()
  {
    // Arrange & Act & Assert
    Assert.Equal("Tooark", TooarkInstrumentation.DefaultName);
  }

  // Teste para um nome em branco falhar na construção: uma fonte sem nome não seria registrada em lugar nenhum.
  [Theory]
  [InlineData(null, "meter", "svc", "1.0")]
  [InlineData("", "meter", "svc", "1.0")]
  [InlineData("source", "  ", "svc", "1.0")]
  [InlineData("source", "meter", "", "1.0")]
  [InlineData("source", "meter", "svc", null)]
  public void Constructor_ShouldThrowArgumentException_WhenAnyNameIsBlank(string? source, string? meter, string? service, string? version)
  {
    // Arrange & Act & Assert
    Assert.ThrowsAny<ArgumentException>(() => new TooarkInstrumentation(source!, meter!, service!, version!));
  }

  // Teste para uma fonte sem ouvinte não criar atividade: a instrumentação registrada com a observabilidade
  // desligada é inofensiva.
  [Fact]
  public void ActivitySource_ShouldNotCreateActivity_WhenNoListenerIsRegistered()
  {
    // Arrange
    using var instrumentation = new TooarkInstrumentation($"Tooark.Tests.NoListener.{Guid.NewGuid():N}", "meter", "svc", "1.0");

    // Act
    using var activity = instrumentation.ActivitySource.StartActivity("Test");

    // Assert
    Assert.Null(activity);
  }

  // Teste para Dispose poder ser chamado mais de uma vez sem lançar exceção.
  [Fact]
  public void Dispose_ShouldNotThrow_WhenCalledTwice()
  {
    // Arrange
    var instrumentation = new TooarkInstrumentation("source", "meter", "svc", "1.0");

    // Act
    instrumentation.Dispose();
    var exception = Record.Exception(instrumentation.Dispose);

    // Assert
    Assert.Null(exception);
  }
}
