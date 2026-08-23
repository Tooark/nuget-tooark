using Tooark.Validations.Patterns;

namespace Tooark.Attributes;

/// <summary>
/// Atributo de validação de senha com critérios específicos de complexidade.
/// </summary>
/// <remarks>
/// Os critérios são aplicados exatamente como configurados. Desabilitar todos significa exigir apenas o
/// comprimento mínimo, que é uma política legítima — senhas longas sem regra de composição. Valor ausente
/// é reportado como campo obrigatório.
/// </remarks>
/// <param name="lowercase">Exige carácter minúsculo. Padrão: true.</param>
/// <param name="uppercase">Exige carácter maiúsculo. Padrão: true.</param>
/// <param name="number">Exige carácter numérico. Padrão: true.</param>
/// <param name="symbol">Exige carácter especial. Padrão: true.</param>
/// <param name="length">Comprimento mínimo da senha. Padrão: 8. Valor não positivo assume 1.</param>
/// <param name="propertyName">Nome do campo usado na mensagem de erro. Padrão: "Password".</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class PasswordValidationAttribute(
  bool lowercase = true,
  bool uppercase = true,
  bool number = true,
  bool symbol = true,
  int length = PasswordPattern.DefaultLength,
  string propertyName = "Password"
) : TooarkValidationAttribute(propertyName)
{
  #region Private Fields

  /// <summary>
  /// Expressão regular montada a partir dos critérios configurados.
  /// </summary>
  private readonly string _pattern = PasswordPattern.Mount(lowercase, uppercase, number, symbol, length);

  #endregion

  #region Methods

  /// <summary>
  /// Verifica se o valor atende aos critérios de complexidade configurados.
  /// </summary>
  /// <param name="value">O valor a ser verificado.</param>
  /// <returns>Verdadeiro quando o valor atende aos critérios.</returns>
  protected override bool IsSatisfied(string value) => Matches(value, _pattern);

  #endregion
}
