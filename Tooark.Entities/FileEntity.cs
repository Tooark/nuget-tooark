using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tooark.Enums;
using Tooark.Exceptions;
using Tooark.Validations;
using Tooark.ValueObjects;

namespace Tooark.Entities;

/// <summary>
/// Entidade para gerenciar arquivos dentro do bucket.
/// </summary>
/// <remarks>
/// Herda de <see cref="InitialEntity"/> para incluir informações iniciais.
/// Esta classe é usada para representar arquivos armazenados no bucket.
/// </remarks>
public abstract class FileEntity : InitialEntity
{
  #region Constructors

  /// <summary>
  /// Construtor vazio para a entidade FileEntity.
  /// </summary>
  /// <remarks>
  /// Utilizado pelo Entity Framework.
  /// </remarks>
  protected FileEntity() { }

  /// <summary>
  /// Construtor da classe FileEntity.
  /// </summary>
  /// <param name="file">O nome e link do arquivo.</param>
  /// <param name="title">O título do arquivo.</param>
  /// <param name="createdById">O identificador do usuário que criou o arquivo.</param>
  protected FileEntity(FileStorage file, Title title, CreatedBy createdById) : base(createdById)
  {
    // Valida os argumentos sem acumular notificação na entidade
    EnsureValid(file, "FileStorage");
    EnsureValid(title, "Title");

    FileName = file.Name;
    Title = title;
    Link = file.Link;
  }

  /// <summary>
  /// Construtor da classe FileEntity.
  /// </summary>
  /// <param name="file">O nome e link do arquivo.</param>
  /// <param name="title">O título do arquivo.</param>
  /// <param name="fileFormat">O formato do arquivo.</param>
  /// <param name="type">O tipo do arquivo.</param>
  /// <param name="size">O tamanho do arquivo.</param>
  /// <param name="createdById">O identificador do usuário que criou o arquivo.</param>
  protected FileEntity(FileStorage file, Title title, string fileFormat, EFileType type, long size, CreatedBy createdById) : base(createdById)
  {
    // Valida os argumentos sem acumular notificação na entidade
    EnsureValid(file, "FileStorage");
    EnsureValid(title, "Title");
    EnsureValid(
      new Validation()
        .IsNotNullOrEmpty(fileFormat, "FileFormat", "Field.Required;FileFormat")
        .IsGreaterOrEquals(size, 0, "Size", "Field.Invalid;Size"),
      "File");

    FileName = file.Name;
    Title = title;
    Link = file.Link;
    FileFormat = fileFormat;
    Type = type;
    Size = size;
  }

  #endregion

  #region Properties

  /// <summary>
  /// Nome do arquivo incluindo path do bucket.
  /// </summary>
  /// <value>
  /// O nome do arquivo é do tipo <see cref="string"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'file_name' é do tipo 'text' e é obrigatório.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("file_name", TypeName = "text")]
  [Required]
  public string FileName { get; private set; } = string.Empty;

  /// <summary>
  /// Título do arquivo para exibição.
  /// </summary>
  /// <value>
  /// O título do arquivo é do tipo <see cref="string"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'title' é do tipo 'varchar(255)' e é obrigatório.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("title", TypeName = "varchar(255)")]
  [Required]
  public string Title { get; private set; } = string.Empty;

  /// <summary>
  /// Link do arquivo para utilização em páginas web.
  /// </summary>
  /// <value>
  /// O link público do arquivo é do tipo <see cref="string"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'link' é do tipo 'text' e é obrigatório.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("link", TypeName = "text")]
  [Required]
  public string Link { get; private set; } = string.Empty;

  /// <summary>
  /// Formato do arquivo.
  /// </summary>
  /// <value>
  /// O formato do arquivo é do tipo <see cref="string"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'file_format' é do tipo 'varchar(10)'.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("file_format", TypeName = "varchar(10)")]
  public string? FileFormat { get; private set; }

  /// <summary>
  /// Tipo do arquivo.
  /// </summary>
  /// <value>
  /// O tipo do arquivo é do tipo <see cref="int"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'type' é do tipo 'int'.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("type", TypeName = "int")]
  [Required]
  public EFileType Type { get; private set; } = EFileType.Unknown;

  /// <summary>
  /// Tamanho do arquivo.
  /// </summary>
  /// <value>
  /// O tamanho do arquivo é do tipo <see cref="long"/>.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'size' é do tipo 'bigint'.
  /// </remarks>
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("size", TypeName = "bigint")]
  [Required]
  public long Size { get; private set; } = 0;

  #endregion
}
