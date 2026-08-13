using OpenTelemetry;
using Tooark.Exceptions;

namespace Tooark.Observability.Enums;

/// <summary>
/// Enumeração para os tipos de processador de exportação OTLP.
/// </summary>
public sealed class EProcessorType
{
  #region Processor Types OTLP

  /// <summary>
  /// Tipo Simple.
  /// </summary>
  /// <remarks>
  /// Utiliza o processador de exportação simples, que exporta os dados imediatamente após serem coletados.
  /// </remarks>
  public static readonly EProcessorType Simple = new(0, "simple", ExportProcessorType.Simple);

  /// <summary>
  /// Tipo Batch.
  /// </summary>
  /// <remarks>
  /// Utiliza o processador de exportação em lote, que agrupa os dados antes de exportá-los, melhorando a performance.
  /// </remarks>
  public static readonly EProcessorType Batch = new(1, "batch", ExportProcessorType.Batch);

  #endregion

  #region Constructor

  /// <summary>
  /// Construtor privado da classe.
  /// </summary>
  /// <param name="id">Identificador do tipo de processador OTLP.</param>
  /// <param name="description">Descrição do tipo de processador OTLP.</param>
  /// <param name="type">Tipo do processador OTLP do OpenTelemetry.</param>
  /// <returns>Uma instância da classe <see cref="EProcessorType"/>.</returns>
  private EProcessorType(int id, string description, ExportProcessorType type)
  {
    Id = id;
    Description = description;
    Type = type;
  }

  #endregion

  #region Private Properties

  /// <summary>
  /// Id do tipo de processador OTLP.
  /// </summary>
  private int Id { get; }

  /// <summary>
  /// Descrição do tipo de processador OTLP.
  /// </summary>
  private string Description { get; }

  /// <summary>
  /// Tipo do processador de exportação.
  /// </summary>
  private ExportProcessorType Type { get; }

  #endregion

  #region Private Methods

  /// <summary>
  /// Função que retorna um enumerador tipo de processador OTLP a partir de sua descrição.
  /// </summary>
  /// <param name="description">Descrição do tipo de processador OTLP.</param>
  /// <returns>Uma instância de <see cref="EProcessorType"/>.</returns>
  private static EProcessorType FromDescription(string description) => description?.ToLowerInvariant() switch
  {
    "simple" => Simple,
    "batch" => Batch,
    _ => throw new InternalServerErrorException($"Invalid.Parameter;{description}")
  };

  /// <summary>
  /// Função que retorna um enumerador tipo de processador OTLP a partir de seu id.
  /// </summary>
  /// <param name="id">Id do tipo de processador OTLP.</param>
  /// <returns>Uma instância de <see cref="EProcessorType"/>.</returns>
  private static EProcessorType FromId(int id) => id switch
  {
    0 => Simple,
    1 => Batch,
    _ => throw new InternalServerErrorException($"Invalid.Parameter;{id}")
  };

  /// <summary>
  /// Função que retorna um enumerador tipo de processador OTLP a partir de seu id.
  /// </summary>
  /// <param name="type">Tipo de processador OTLP.</param>
  /// <returns>Uma instância de <see cref="EProcessorType"/>.</returns>
  private static EProcessorType FromType(ExportProcessorType type) => type switch
  {
    ExportProcessorType.Simple => Simple,
    ExportProcessorType.Batch => Batch,
    _ => throw new InternalServerErrorException($"Invalid.Parameter;{type}")
  };

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar a descrição tipo de processador OTLP.
  /// </summary>
  /// <returns>A descrição do tipo de processador OTLP.</returns>
  public override string ToString() => Description;

  /// <summary>
  /// Método que retorna o id do tipo de processador OTLP.
  /// </summary>
  /// <returns>O id do tipo de processador OTLP.</returns>
  public int ToInt() => Id;

  /// <summary>
  /// Método que retorna o tipo de processador OTLP do OpenTelemetry.
  /// </summary>
  /// <returns>O tipo de processador OTLP do OpenTelemetry.</returns>
  public ExportProcessorType ToType() => Type;

  /// <summary>
  /// Conversão implícita de <see cref="EProcessorType"/> para <see cref="int"/>.
  /// </summary>
  /// <param name="type">Instância de <see cref="EProcessorType"/>.</param>
  /// <returns>Id do tipo de processador OTLP.</returns>
  public static implicit operator int(EProcessorType type) => type?.Id ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="EProcessorType"/> para <see cref="string"/>.
  /// </summary>
  /// <param name="type">Instância de <see cref="EProcessorType"/>.</param>
  /// <returns>Descrição do tipo de processador OTLP.</returns>
  public static implicit operator string(EProcessorType type) => type?.Description ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="EProcessorType"/> para <see cref="ExportProcessorType"/>.
  /// </summary>
  /// <param name="type">Instância de <see cref="EProcessorType"/>.</param>
  /// <returns>Tipo de processador OTLP do OpenTelemetry.</returns>
  public static implicit operator ExportProcessorType(EProcessorType type) => type?.Type ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="int"/> para <see cref="EProcessorType"/>.
  /// </summary>
  /// <param name="id">Id do tipo de processador OTLP.</param>
  /// <returns>Uma instância de <see cref="EProcessorType"/>.</returns>
  public static implicit operator EProcessorType(int id) => FromId(id);

  /// <summary>
  /// Conversão implícita de <see cref="string"/> para <see cref="EProcessorType"/>.
  /// </summary>
  /// <param name="description">Descrição do tipo de processador OTLP.</param>
  /// <returns>Uma instância de <see cref="EProcessorType"/>.</returns>
  public static implicit operator EProcessorType(string description) => FromDescription(description);

  /// <summary>
  /// Conversão implícita de <see cref="ExportProcessorType"/> para <see cref="EProcessorType"/>.
  /// </summary>
  /// <param name="type">Tipo de processador OTLP do OpenTelemetry.</param>
  /// <returns>Uma instância de <see cref="EProcessorType"/>.</returns>
  public static implicit operator EProcessorType(ExportProcessorType type) => FromType(type);

  #endregion
}
