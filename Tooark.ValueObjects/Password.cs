using Tooark.Validations;
using Tooark.Validations.Patterns;

namespace Tooark.ValueObjects;

/// <summary>
/// Representa uma senha válida com complexidade especificada.
/// </summary>
/// <remarks>
/// O valor só é alcançável por <see cref="Value"/>. Não existe conversão implícita para texto, e
/// <see cref="ToString"/> devolve uma máscara: um tipo que guarda senha não pode vazar o conteúdo em um
/// log, em uma interpolação ou em uma serialização acidental.
/// </remarks>
public sealed class Password : ValueObject
{
  #region Constants

  /// <summary>
  /// Texto devolvido por <see cref="ToString"/> no lugar da senha.
  /// </summary>
  public const string Mask = "********";

  #endregion

  #region Private Fields

  /// <summary>
  /// O valor privado da senha.
  /// </summary>
  private readonly string _value = string.Empty;

  #endregion

  #region Constructor

  /// <summary>
  /// Inicializa uma nova instância da classe Password com os critérios de complexidade especificados.
  /// </summary>
  /// <remarks>
  /// Os critérios valem exatamente como informados: desabilitar todos significa exigir apenas o
  /// comprimento. A regra é a mesma aplicada pelo <c>PasswordValidationAttribute</c>.
  /// </remarks>
  /// <param name="value">O valor da senha a ser validado.</param>
  /// <param name="lowercase">Exige carácter minúsculo. Padrão: true.</param>
  /// <param name="uppercase">Exige carácter maiúsculo. Padrão: true.</param>
  /// <param name="number">Exige carácter numérico. Padrão: true.</param>
  /// <param name="symbol">Exige carácter especial. Padrão: true.</param>
  /// <param name="length">Comprimento mínimo da senha. Padrão: 8. Valor não positivo assume 1.</param>
  public Password(
    string? value,
    bool lowercase = true,
    bool uppercase = true,
    bool number = true,
    bool symbol = true,
    int length = PasswordPattern.DefaultLength)
  {
    // Define a expressão regular para validação da senha
    var pattern = PasswordPattern.Mount(lowercase, uppercase, number, symbol, length);

    // Adiciona as notificações de validação da senha
    AddNotifications(new Validation()
      .Match(value, pattern, "Password", "Field.Invalid;Password")
    );

    // Verifica é valido então não existe notificação
    if (IsValid)
    {
      // Define o valor da senha
      _value = value!;
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o valor da senha.
  /// </summary>
  /// <remarks>
  /// É o único caminho para o valor em texto puro. Senha inválida devolve string vazia.
  /// </remarks>
  public string Value { get => _value; }

  #endregion

  #region Methods

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/>, que devolve uma máscara.
  /// </summary>
  /// <remarks>
  /// Devolver a senha aqui faria qualquer interpolação, concatenação ou registro em log vazar o valor.
  /// Use <see cref="Value"/> quando precisar do conteúdo.
  /// </remarks>
  /// <returns>A máscara, ou string vazia quando a senha é inválida.</returns>
  public override string ToString() => _value.Length > 0 ? Mask : string.Empty;

  /// <summary>
  /// Define uma conversão implícita de uma string para um objeto Password.
  /// </summary>
  /// <remarks>
  /// A conversão existe apenas neste sentido. O caminho inverso levaria a senha para fora do objeto sem
  /// que ninguém pedisse, que é exatamente o que este tipo evita.
  /// </remarks>
  /// <param name="value">A string a ser convertida em um objeto Password.</param>
  /// <returns>O objeto Password criado a partir da string fornecida.</returns>
  public static implicit operator Password(string? value) => new(value);

  #endregion
}
