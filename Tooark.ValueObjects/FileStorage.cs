using Tooark.Exceptions;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa dados de um arquivo do storage.
/// </summary>
public sealed class FileStorage : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado do link do arquivo do storage.
  /// </summary>
  private readonly string _link = string.Empty;

  /// <summary>
  /// Valor privado do nome do arquivo do storage.
  /// </summary>
  private readonly string _name = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe FileStorage com o link e o nome do arquivo.
  /// </summary>
  /// <param name="link">O número do arquivo do storage a ser validado.</param>
  /// <param name="name">O tipo do arquivo do storage a ser validado. Parâmetro opcional.</param>
  public FileStorage(ProtocolHttp link, string? name = null)
  {
    // Adiciona as notificações de validação do link do arquivo do storage
    AddNotifications(link);

    // Se o nome do arquivo do storage não for informado, define o nome como o link
    name = string.IsNullOrWhiteSpace(name) ? link : name;

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o link e o nome do arquivo do storage
      _link = link;
      _name = name!;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor do link do arquivo do storage.
  /// </summary>
  public string Link { get => _link; }

  /// <summary>
  /// Valor do nome do arquivo do storage.
  /// </summary>
  public string Name { get => _name; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o link do arquivo do storage.
  /// </summary>
  /// <returns>O link do arquivo do storage.</returns>
  public override string ToString() => _link;

  /// <summary>
  /// Define uma conversão implícita de um objeto FileStorage para uma string.
  /// </summary>
  /// <param name="document">O objeto FileStorage a ser convertido.</param>
  /// <returns>Uma string que representa o link do arquivo do storage.</returns>
  public static implicit operator string(FileStorage document) =>
    document?._link ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto FileStorage.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto FileStorage.</param>
  /// <returns>Um objeto FileStorage criado a partir da string fornecida.</returns>
  public static implicit operator FileStorage(string value) => new(value);

  #endregion
}
