using OpenTelemetry.Exporter;
using Tooark.Exceptions;

namespace Tooark.Observability.Enums;

/// <summary>
/// Enumeração para os protocolos de comunicação OTLP.
/// </summary>
public sealed class EProtocolOtlp
{
  #region Protocolos OTLP

  /// <summary>
  /// Protocolo gRPC.
  /// </summary>
  public static readonly EProtocolOtlp Grpc = new(0, "grpc", OtlpExportProtocol.Grpc);

  /// <summary>
  /// Protocolo HTTP/protobuf.
  /// </summary>
  public static readonly EProtocolOtlp Http = new(1, "http", OtlpExportProtocol.HttpProtobuf);

  #endregion

  #region Constructor

  /// <summary>
  /// Construtor privado da classe.
  /// </summary>
  /// <param name="id">Identificador do protocolo OTLP.</param>
  /// <param name="description">Descrição do protocolo OTLP.</param>
  /// <param name="protocol">Protocolo OTLP do OpenTelemetry.</param>
  /// <returns>Uma instância da classe <see cref="EProtocolOtlp"/>.</returns>
  private EProtocolOtlp(int id, string description, OtlpExportProtocol protocol)
  {
    Id = id;
    Description = description;
    Protocol = protocol;
  }

  #endregion

  #region Private Properties

  /// <summary>
  /// Id do protocolo OTLP.
  /// </summary>
  private int Id { get; }

  /// <summary>
  /// Descrição do protocolo OTLP.
  /// </summary>
  private string Description { get; }

  /// <summary>
  /// Protocolo OTLP do OpenTelemetry.
  /// </summary>
  private OtlpExportProtocol Protocol { get; }

  #endregion

  #region Private Methods

  /// <summary>
  /// Função que retorna um enumerador protocolo OTLP a partir de sua descrição.
  /// </summary>
  /// <param name="description">Descrição do protocolo OTLP.</param>
  /// <returns>Uma instância de <see cref="EProtocolOtlp"/>.</returns>
  private static EProtocolOtlp FromDescription(string description) => description?.ToLowerInvariant() switch
  {
    "http" => Http,
    "grpc" => Grpc,
    _ => throw new InternalServerErrorException($"Invalid.Parameter;{description}")
  };

  /// <summary>
  /// Função que retorna um enumerador protocolo OTLP a partir de seu id.
  /// </summary>
  /// <param name="id">Id do protocolo OTLP.</param>
  /// <returns>Uma instância de <see cref="EProtocolOtlp"/>.</returns>
  private static EProtocolOtlp FromId(int id) => id switch
  {
    0 => Grpc,
    1 => Http,
    _ => throw new InternalServerErrorException($"Invalid.Parameter;{id}")
  };

  /// <summary>
  /// Função que retorna um enumerador protocolo OTLP a partir de seu id.
  /// </summary>
  /// <param name="protocol">Protocolo OTLP.</param>
  /// <returns>Uma instância de <see cref="EProtocolOtlp"/>.</returns>
  private static EProtocolOtlp FromProtocol(OtlpExportProtocol protocol) => protocol switch
  {
    OtlpExportProtocol.HttpProtobuf => Http,
    OtlpExportProtocol.Grpc => Grpc,
    _ => throw new InternalServerErrorException($"Invalid.Parameter;{protocol}")
  };

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar a descrição do protocolo OTLP.
  /// </summary>
  /// <returns>A descrição do protocolo OTLP.</returns>
  public override string ToString() => Description;

  /// <summary>
  /// Método que retorna o id do protocolo OTLP.
  /// </summary>
  /// <returns>O id do protocolo OTLP.</returns>
  public int ToInt() => Id;

  /// <summary>
  /// Método que retorna o protocolo OTLP do OpenTelemetry.
  /// </summary>
  /// <returns>O protocolo OTLP do OpenTelemetry.</returns>
  public OtlpExportProtocol ToProtocol() => Protocol;

  /// <summary>
  /// Conversão implícita de <see cref="EProtocolOtlp"/> para <see cref="int"/>.
  /// </summary>
  /// <param name="protocol">Instância de <see cref="EProtocolOtlp"/>.</param>
  /// <returns>Id do protocolo OTLP.</returns>
  public static implicit operator int(EProtocolOtlp protocol) => protocol?.Id ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="EProtocolOtlp"/> para <see cref="string"/>.
  /// </summary>
  /// <param name="protocol">Instância de <see cref="EProtocolOtlp"/>.</param>
  /// <returns>Descrição do protocolo OTLP.</returns>
  public static implicit operator string(EProtocolOtlp protocol) => protocol?.Description ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="EProtocolOtlp"/> para <see cref="OtlpExportProtocol"/>.
  /// </summary>
  /// <param name="protocol">Instância de <see cref="EProtocolOtlp"/>.</param>
  /// <returns>Protocolo OTLP do OpenTelemetry.</returns>
  public static implicit operator OtlpExportProtocol(EProtocolOtlp protocol) => protocol?.Protocol ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="int"/> para <see cref="EProtocolOtlp"/>.
  /// </summary>
  /// <param name="id">Id do protocolo OTLP.</param>
  /// <returns>Uma instância de <see cref="EProtocolOtlp"/>.</returns>
  public static implicit operator EProtocolOtlp(int id) => FromId(id);

  /// <summary>
  /// Conversão implícita de <see cref="string"/> para <see cref="EProtocolOtlp"/>.
  /// </summary>
  /// <param name="description">Descrição do protocolo OTLP.</param>
  /// <returns>Uma instância de <see cref="EProtocolOtlp"/>.</returns>
  public static implicit operator EProtocolOtlp(string description) => FromDescription(description);

  /// <summary>
  /// Conversão implícita de <see cref="OtlpExportProtocol"/> para <see cref="EProtocolOtlp"/>.
  /// </summary>
  /// <param name="protocol">Protocolo OTLP do OpenTelemetry.</param>
  /// <returns>Uma instância de <see cref="EProtocolOtlp"/>.</returns>
  public static implicit operator EProtocolOtlp(OtlpExportProtocol protocol) => FromProtocol(protocol);

  #endregion
}
