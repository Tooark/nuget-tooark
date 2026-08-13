using OpenTelemetry.Exporter;
using Tooark.Exceptions;
using Tooark.Observability.Enums;

namespace Tooark.Tests.Observability.Enums;

public class EProtocolOtlpTests
{
  #region Implicit Conversions - From Int

  // Teste para converter de int para EProtocolOtlp e retornar o int, a string e o OtlpExportProtocol corretos.
  [Theory]
  [InlineData(0, "grpc", OtlpExportProtocol.Grpc)]
  [InlineData(1, "http", OtlpExportProtocol.HttpProtobuf)]
  public void EProtocolOtlp_ShouldBeValid_WhenGivenId(int id, string description, OtlpExportProtocol exportProtocol)
  {
    // Arrange
    EProtocolOtlp protocol = id;

    // Act
    int protocolId = protocol;
    string protocolDescription = protocol;
    OtlpExportProtocol protocolExport = protocol;

    // Assert
    Assert.Equal(id, protocolId);
    Assert.Equal(description, protocolDescription);
    Assert.Equal(exportProtocol, protocolExport);
  }

  // Teste para lançar exceção quando o id é inválido.
  [Theory]
  [InlineData(-1)]
  [InlineData(2)]
  public void EProtocolOtlp_ShouldThrowException_WhenIdIsInvalid(int invalidId)
  {
    // Act & Assert
    Assert.Throws<InternalServerErrorException>(() =>
    {
      EProtocolOtlp protocol = invalidId;
    });
  }

  #endregion

  #region Implicit Conversions - From String

  // Teste para converter de string para EProtocolOtlp e retornar a string, o int e o OtlpExportProtocol corretos.
  [Theory]
  [InlineData("grpc", 0, "grpc", OtlpExportProtocol.Grpc)]
  [InlineData("Grpc", 0, "grpc", OtlpExportProtocol.Grpc)]
  [InlineData("GRPC", 0, "grpc", OtlpExportProtocol.Grpc)]
  [InlineData("http", 1, "http", OtlpExportProtocol.HttpProtobuf)]
  [InlineData("Http", 1, "http", OtlpExportProtocol.HttpProtobuf)]
  [InlineData("HTTP", 1, "http", OtlpExportProtocol.HttpProtobuf)]
  public void EProtocolOtlp_ShouldBeValid_WhenGivenDescription(string description, int id, string descriptionExpected, OtlpExportProtocol exportProtocol)
  {
    // Arrange
    EProtocolOtlp protocol = description;

    // Act
    string protocolDescription = protocol;
    int protocolId = protocol;
    OtlpExportProtocol protocolExport = protocol;

    // Assert
    Assert.Equal(descriptionExpected, protocolDescription);
    Assert.Equal(id, protocolId);
    Assert.Equal(exportProtocol, protocolExport);
  }

  // Teste para lançar exceção quando a descrição é inválida.
  [Theory]
  [InlineData("invalid")]
  [InlineData("tcp")]
  [InlineData("")]
  [InlineData(null)]
  public void EProtocolOtlp_ShouldThrowException_WhenDescriptionIsInvalid(string? description)
  {
    // Arrange
    string? invalidDescription = description;

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() =>
    {
      EProtocolOtlp protocol = invalidDescription!;
    });
    Assert.Equal($"Invalid.Parameter;{invalidDescription}", ex.Message);
  }

  #endregion

  #region Implicit Conversions - From OtlpExportProtocol

  // Teste para converter de OtlpExportProtocol para EProtocolOtlp.
  [Theory]
  [InlineData(OtlpExportProtocol.Grpc, 0, "grpc")]
  [InlineData(OtlpExportProtocol.HttpProtobuf, 1, "http")]
  public void EProtocolOtlp_ShouldBeValid_WhenGivenOtlpExportProtocol(OtlpExportProtocol otlpProtocol, int expectedId, string expectedDescription)
  {
    // Arrange
    EProtocolOtlp protocol = otlpProtocol;

    // Act
    int protocolId = protocol;
    string protocolDescription = protocol;
    OtlpExportProtocol exportProtocol = protocol;

    // Assert
    Assert.Equal(expectedId, protocolId);
    Assert.Equal(expectedDescription, protocolDescription);
    Assert.Equal(otlpProtocol, exportProtocol);
  }

  // Teste para lançar exceção quando OtlpExportProtocol é inválido (valor não definido).
  [Fact]
  public void EProtocolOtlp_ShouldThrowException_WhenOtlpExportProtocolIsInvalid()
  {
    // Arrange
    OtlpExportProtocol invalidProtocol = (OtlpExportProtocol)99;

    // Act & Assert
    var ex = Assert.Throws<InternalServerErrorException>(() =>
    {
      EProtocolOtlp protocol = invalidProtocol;
    });
    Assert.Equal($"Invalid.Parameter;{invalidProtocol}", ex.Message);
  }

  #endregion

  #region Implicit Conversions - To Types

  // Teste para converter de EProtocolOtlp para int.
  [Fact]
  public void EProtocolOtlp_ShouldImplicitConversionToInt()
  {
    // Arrange & Act
    int grpc = EProtocolOtlp.Grpc;
    int http = EProtocolOtlp.Http;

    // Assert
    Assert.Equal(0, grpc);
    Assert.Equal(1, http);
  }

  // Teste para converter de EProtocolOtlp para string.
  [Fact]
  public void EProtocolOtlp_ShouldImplicitConversionToString()
  {
    // Arrange & Act
    string grpc = EProtocolOtlp.Grpc;
    string http = EProtocolOtlp.Http;

    // Assert
    Assert.Equal("grpc", grpc);
    Assert.Equal("http", http);
  }

  // Teste para converter de EProtocolOtlp para OtlpExportProtocol.
  [Fact]
  public void EProtocolOtlp_ShouldImplicitConversionToOtlpExportProtocol()
  {
    // Arrange & Act
    OtlpExportProtocol http = EProtocolOtlp.Http;
    OtlpExportProtocol grpc = EProtocolOtlp.Grpc;

    // Assert
    Assert.Equal(OtlpExportProtocol.HttpProtobuf, http);
    Assert.Equal(OtlpExportProtocol.Grpc, grpc);
  }

  #endregion

  #region Methods

  // Teste para converter de EProtocolOtlp com ToInt.
  [Fact]
  public void EProtocolOtlp_ShouldConvertWithToInt()
  {
    // Arrange & Act
    int grpc = EProtocolOtlp.Grpc.ToInt();
    int http = EProtocolOtlp.Http.ToInt();

    // Assert
    Assert.Equal(0, grpc);
    Assert.Equal(1, http);
  }

  // Teste para converter de EProtocolOtlp com ToString.
  [Fact]
  public void EProtocolOtlp_ShouldConvertWithToString()
  {
    // Arrange & Act
    string http = EProtocolOtlp.Http.ToString();
    string grpc = EProtocolOtlp.Grpc.ToString();

    // Assert
    Assert.Equal("http", http);
    Assert.Equal("grpc", grpc);
  }

  // Teste para converter de EProtocolOtlp com ToProtocol.
  [Fact]
  public void EProtocolOtlp_ShouldConvertWithToProtocol()
  {
    // Arrange & Act
    OtlpExportProtocol http = EProtocolOtlp.Http.ToProtocol();
    OtlpExportProtocol grpc = EProtocolOtlp.Grpc.ToProtocol();

    // Assert
    Assert.Equal(OtlpExportProtocol.HttpProtobuf, http);
    Assert.Equal(OtlpExportProtocol.Grpc, grpc);
  }

  #endregion

  #region Static Instances

  // Teste para verificar as instâncias estáticas.
  [Fact]
  public void EProtocolOtlp_StaticInstances_ShouldHaveCorrectValues()
  {
    // Assert - Grpc
    Assert.Equal(0, EProtocolOtlp.Grpc.ToInt());
    Assert.Equal("grpc", EProtocolOtlp.Grpc.ToString());
    Assert.Equal(OtlpExportProtocol.Grpc, EProtocolOtlp.Grpc.ToProtocol());

    // Assert - Http
    Assert.Equal(1, EProtocolOtlp.Http.ToInt());
    Assert.Equal("http", EProtocolOtlp.Http.ToString());
    Assert.Equal(OtlpExportProtocol.HttpProtobuf, EProtocolOtlp.Http.ToProtocol());
  }

  #endregion

  #region Null Conversions

  // Teste para verificar que a conversão implícita de instância nula falha com erro identificável.
  [Fact]
  public void EProtocolOtlp_ImplicitConversionFromNull_ShouldThrowInternalServerErrorException()
  {
    // Arrange - as propriedades de override são anuláveis por design, então a conversão de nulo é alcançável
    EProtocolOtlp protocol = null!;

    // Act & Assert - antes cada conversão estourava NullReferenceException
    var toInt = Assert.Throws<InternalServerErrorException>(() => { int _ = protocol; });
    var toString = Assert.Throws<InternalServerErrorException>(() => { string _ = protocol; });
    var toProtocol = Assert.Throws<InternalServerErrorException>(() => { OtlpExportProtocol _ = protocol; });

    Assert.Contains("Invalid.Parameter;null", toInt.GetErrorMessages());
    Assert.Contains("Invalid.Parameter;null", toString.GetErrorMessages());
    Assert.Contains("Invalid.Parameter;null", toProtocol.GetErrorMessages());
  }

  #endregion
}
