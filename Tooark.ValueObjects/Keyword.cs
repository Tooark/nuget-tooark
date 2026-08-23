using Tooark.Exceptions;
using Tooark.Extensions;
using Tooark.Validations;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa uma palavra-chave válida.
/// </summary>
public sealed class Keyword : ValueObject
{
  #region Private Fields

  /// <summary>
  /// Valor privado da palavra-chave.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Keyword com o valor especificado.
  /// </summary>
  /// <param name="value">O valor da palavra-chave a ser validada.</param>
  public Keyword(string value)
  {
    // Adiciona as notificações de validação da palavra-chave
    AddNotifications(new Validation()
      .IsNotNullOrWhiteSpace(value, "Keyword", "Field.Invalid;Keyword")
    );

    // Verifica se é válido então não existe notificação
    if (IsValid)
    {
      // Define o valor da palavra-chave sem espaços em branco no início e no final
      _value = value.Trim();
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor da palavra-chave.
  /// </summary>
  public string Value { get => _value; }

  /// <summary>
  /// Obtém o valor da palavra-chave normalizado.
  /// </summary>
  public string Normalized => _value.ToNormalize();

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar o valor da palavra-chave.
  /// </summary>
  /// <returns>Uma string que representa o valor da palavra-chave.</returns>
  public override string ToString() => _value;

  /// <summary>
  /// Define uma conversão implícita de um objeto Keyword para uma string.
  /// </summary>
  /// <param name="keyword">O objeto Keyword a ser convertido.</param>
  /// <returns>Uma string que representa o valor do Keyword.</returns>
  public static implicit operator string(Keyword keyword) =>
    keyword?._value ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Keyword.
  /// </summary>
  /// <param name="value">A string a ser convertida em um objeto Keyword.</param>
  /// <returns>Um objeto Keyword criado a partir da string fornecida.</returns>
  public static implicit operator Keyword(string value) => new(value);

  #endregion
}
