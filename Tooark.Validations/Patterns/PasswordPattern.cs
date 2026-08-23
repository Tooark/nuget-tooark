using System.Globalization;
using System.Text;

namespace Tooark.Validations.Patterns;

/// <summary>
/// Monta a expressão regular de validação de senha a partir dos critérios de complexidade.
/// </summary>
/// <remarks>
/// Existe para que o atributo <c>PasswordValidationAttribute</c> e o value object <c>Password</c> apliquem
/// exatamente a mesma regra. Antes cada um tinha a própria cópia, e as duas divergiram: a mesma senha era
/// aceita por um e recusada pelo outro.
/// </remarks>
public static class PasswordPattern
{
  #region Constants

  /// <summary>
  /// Comprimento mínimo padrão da senha.
  /// </summary>
  public const int DefaultLength = 8;

  #endregion

  #region Methods

  /// <summary>
  /// Monta a expressão regular da senha.
  /// </summary>
  /// <remarks>
  /// Os critérios valem exatamente como informados. Desabilitar todos significa exigir apenas o
  /// comprimento, que é uma política legítima: senhas longas sem regra de composição.
  /// </remarks>
  /// <param name="lowercase">Exige carácter minúsculo.</param>
  /// <param name="uppercase">Exige carácter maiúsculo.</param>
  /// <param name="number">Exige carácter numérico.</param>
  /// <param name="symbol">Exige carácter especial.</param>
  /// <param name="length">Comprimento mínimo da senha. Valor não positivo assume 1.</param>
  /// <returns>Expressão regular da senha.</returns>
  public static string Mount(
    bool lowercase = true,
    bool uppercase = true,
    bool number = true,
    bool symbol = true,
    int length = DefaultLength)
  {
    // Comprimento não positivo não teria sentido em uma expressão regular
    var minimum = length > 0 ? length : 1;

    // Cada critério vira uma verificação antecipada, ancorada uma única vez no início
    var pattern = new StringBuilder("^");

    // Letras minúsculas
    if (lowercase)
    {
      pattern.Append(RegexPattern.PassLower[1..]);
    }

    // Letras maiúsculas
    if (uppercase)
    {
      pattern.Append(RegexPattern.PassUpper[1..]);
    }

    // Dígitos numéricos
    if (number)
    {
      pattern.Append(RegexPattern.PassNumber[1..]);
    }

    // Símbolos especiais
    if (symbol)
    {
      pattern.Append(RegexPattern.PassSymbol[1..]);
    }

    // Sem nenhum critério, resta apenas a exigência de comprimento
    pattern.Append(CultureInfo.InvariantCulture, $".{{{minimum},}}");

    return pattern.ToString();
  }

  #endregion
}
