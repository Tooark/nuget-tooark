using Tooark.Exceptions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa uma string delimitada por ponto e vírgula.
/// </summary>
/// <remarks>
/// Conversão entre listas e strings delimitadas por ponto e vírgula.
/// Esta classe é utilizada para validar e manipular strings que contêm múltiplos valores separados por ponto e vírgula.
/// </remarks>
public class DelimitedString : ValueObject
{
  #region Constants

  /// <summary>
  /// Delimitador padrão utilizado para separar os valores.
  /// </summary>
  private const char DefaultDelimiter = ';';

  #endregion

  #region Private Fields

  /// <summary>
  /// Valor privado da string delimitada.
  /// </summary>
  private readonly string _value = string.Empty;

  /// <summary>
  /// Lista de valores privados da string delimitada.
  /// </summary>
  private readonly string[] _values = [];

  #endregion

  #region Constructors

  /// <summary>
  /// Inicializa uma nova instância da classe DelimitedString com o valor especificado.
  /// </summary>
  /// <param name="value">O valor da string delimitada por ponto e vírgula a ser validada.</param>
  public DelimitedString(string? value)
  {
    // Adiciona as notificações de validação da string delimitada
    AddNotifications(new Validation()
      .IsNotNullOrWhiteSpace(value, nameof(DelimitedString), "Field.Invalid;DelimitedString")
    );

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o valor e a lista de valores da string delimitada
      _value = value!.Trim();
      _values = [.. _value.Split(DefaultDelimiter, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim())];
    }
  }

  /// <summary>
  /// Inicializa uma nova instância da classe DelimitedString com os valores especificados.
  /// </summary>
  /// <param name="values">Os valores a serem concatenados na string delimitada.</param>
  public DelimitedString(params string[]? values)
  {
    // Adiciona as notificações de validação da array de strings
    AddNotifications(new Validation()
      .IsGreater(values?.Length ?? 0, 0, nameof(DelimitedString), "Field.Invalid;DelimitedString")
    );

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o valor e a lista de valores da string delimitada, copiando o que veio do chamador
      _value = string.Join(DefaultDelimiter, values!);
      _values = [.. values!];
    }
  }

  /// <summary>
  /// Inicializa uma nova instância da classe DelimitedString com os valores especificados.
  /// </summary>
  /// <param name="values">Os valores a serem concatenados na string delimitada.</param>
  public DelimitedString(List<string>? values)
  {
    // Adiciona as notificações de validação da lista de strings
    AddNotifications(new Validation()
      .IsGreater(values?.Count ?? 0, 0, nameof(DelimitedString), "Field.Invalid;DelimitedString")
    );

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o valor e a lista de valores da string delimitada
      _value = string.Join(DefaultDelimiter, values!);
      _values = [.. values!];
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Valor da string delimitada.
  /// </summary>
  public string Value { get => _value; }

  /// <summary>
  /// Lista de valores da string delimitada.
  /// </summary>
  /// <remarks>
  /// Cada leitura devolve uma cópia: alterar o resultado não altera o objeto.
  /// </remarks>
  public string[] Values { get => [.. _values]; }

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescreve o método <see cref="object.ToString"/> para retornar a string delimitada.
  /// </summary>
  /// <returns>String delimitada.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Converte a string delimitada em uma array de strings.
  /// </summary>
  /// <returns>Array de strings, em uma cópia.</returns>
  public string[] ToArray() => [.. _values];

  /// <summary>
  /// Converte a string delimitada em uma lista de strings.
  /// </summary>
  /// <returns>Lista de strings.</returns>
  public List<string> ToList() => [.. _values];

  /// <summary>
  /// Conversão implícita de DelimitedString para string.
  /// </summary>
  /// <param name="delimitedString">Instância de DelimitedString.</param>
  /// <returns>String delimitada.</returns>
  public static implicit operator string(DelimitedString delimitedString) =>
    delimitedString?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de DelimitedString para array de strings.
  /// </summary>
  /// <param name="delimitedString">Instância de DelimitedString.</param>
  /// <returns>Array de strings, em uma cópia.</returns>
  public static implicit operator string[](DelimitedString delimitedString) =>
    delimitedString is null ?
      throw new InternalServerErrorException("Invalid.Parameter;null") :
      [.. delimitedString._values];

  /// <summary>
  /// Conversão implícita de DelimitedString para lista de strings.
  /// </summary>
  /// <param name="delimitedString">Instância de DelimitedString.</param>
  /// <returns>Lista de strings.</returns>
  public static implicit operator List<string>(DelimitedString delimitedString) =>
    delimitedString is null ?
      throw new InternalServerErrorException("Invalid.Parameter;null") :
      [.. delimitedString._values];

  /// <summary>
  /// Conversão implícita de string para DelimitedString.
  /// </summary>
  /// <param name="value">String delimitada.</param>
  /// <returns>Instância de DelimitedString.</returns>
  public static implicit operator DelimitedString(string value) => new(value);

  /// <summary>
  /// Conversão implícita de array de strings para DelimitedString.
  /// </summary>
  /// <param name="values">Array de strings.</param>
  /// <returns>Instância de DelimitedString.</returns>
  public static implicit operator DelimitedString(string[] values) => new(values);

  /// <summary>
  /// Conversão implícita de lista de strings para DelimitedString.
  /// </summary>
  /// <param name="values">Lista de strings.</param>
  /// <returns>Instância de DelimitedString.</returns>
  public static implicit operator DelimitedString(List<string> values) => new(values);

  #endregion
}
