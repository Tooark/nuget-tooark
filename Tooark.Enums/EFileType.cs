using Tooark.Exceptions;

namespace Tooark.Enums;

/// <summary>
/// Tipos de arquivo.
/// </summary>
public sealed class EFileType
{
  #region File Types

  /// <summary>
  /// Tipo de arquivo desconhecido. Seu id é 0 e seu tipo é "unknown".
  /// </summary>
  public static readonly EFileType Unknown = new(0, "Unknown");

  /// <summary>
  /// Tipo de arquivo documento. Seu id é 1 e seu tipo é "document".
  /// </summary>
  public static readonly EFileType Document = new(1, "Document");

  /// <summary>
  /// Tipo de arquivo imagem. Seu id é 2 e seu tipo é "image".
  /// </summary>
  public static readonly EFileType Image = new(2, "Image");

  /// <summary>
  /// Tipo de arquivo video. Seu id é 3 e seu tipo é "video".
  /// </summary>
  public static readonly EFileType Video = new(3, "Video");

  /// <summary>
  /// Tipo de arquivo audio. Seu id é 4 e seu tipo é "audio".
  /// </summary>
  public static readonly EFileType Audio = new(4, "Audio");

  #endregion

  #region Constructor

  /// <summary>
  /// Construtor privado da classe.
  /// </summary>
  /// <param name="id">Id do tipo.</param>
  /// <param name="description">Descrição do tipo.</param>
  /// <returns>Uma nova instância de <see cref="EFileType"/>.</returns>
  private EFileType(int id, string description)
  {
    Id = id;
    Description = description;
  }

  #endregion

  #region Private Properties

  /// <summary>
  /// Id do tipo.
  /// </summary>
  private int Id { get; }

  /// <summary>
  /// Descrição do tipo.
  /// </summary>
  private string Description { get; }

  #endregion

  #region Private Methods

  /// <summary>
  /// Função que retorna um tipo a partir de sua descrição.
  /// </summary>
  /// <remarks>
  /// A caixa e os espaços das extremidades são normalizados. Descrição desconhecida resulta em
  /// <see cref="Unknown"/>, que é o membro previsto para valores não reconhecidos.
  /// </remarks>
  /// <param name="description">Descrição do tipo.</param>
  /// <returns>Uma instância de <see cref="EFileType"/>.</returns>
  private static EFileType FromDescription(string description) => description?.Trim().ToLowerInvariant() switch
  {
    "document" => Document,
    "image" => Image,
    "video" => Video,
    "audio" => Audio,
    _ => Unknown
  };

  /// <summary>
  /// Função que retorna um tipo a partir de seu id.
  /// </summary>
  /// <param name="id">Id do tipo.</param>
  /// <returns>Uma instância de <see cref="EFileType"/>.</returns>
  private static EFileType FromId(int id) => id switch
  {
    1 => Document,
    2 => Image,
    3 => Video,
    4 => Audio,
    _ => Unknown
  };

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar a descrição do tipo.
  /// </summary>
  /// <returns>A descrição do tipo.</returns>
  public override string ToString() => Description;

  /// <summary>
  /// Método que retorna o id do tipo.
  /// </summary>
  /// <returns>O id do tipo.</returns>
  public int ToInt() => Id;

  /// <summary>
  /// Conversão implícita de <see cref="EFileType"/> para <see cref="int"/>.
  /// </summary>
  /// <param name="fileType">Instância de <see cref="EFileType"/>.</param>
  /// <returns>Id do tipo.</returns>
  /// <exception cref="InternalServerErrorException">Lançada quando a instância é nula.</exception>
  public static implicit operator int(EFileType fileType) =>
    fileType?.Id ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="EFileType"/> para <see cref="string"/>.
  /// </summary>
  /// <param name="fileType">Instância de <see cref="EFileType"/>.</param>
  /// <returns>Descrição do tipo.</returns>
  /// <exception cref="InternalServerErrorException">Lançada quando a instância é nula.</exception>
  public static implicit operator string(EFileType fileType) =>
    fileType?.Description ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="int"/> para <see cref="EFileType"/>.
  /// </summary>
  /// <param name="id">Id do tipo.</param>
  /// <returns>Uma instância de <see cref="EFileType"/>.</returns>
  public static implicit operator EFileType(int id) => FromId(id);

  /// <summary>
  /// Conversão implícita de <see cref="string"/> para <see cref="EFileType"/>.
  /// </summary>
  /// <param name="description">Descrição do tipo.</param>
  /// <returns>Uma instância de <see cref="EFileType"/>.</returns>
  public static implicit operator EFileType(string description) => FromDescription(description);

  #endregion
}
