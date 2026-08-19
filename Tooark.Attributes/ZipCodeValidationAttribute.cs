using System.ComponentModel.DataAnnotations;
using Tooark.Validations.Patterns;

namespace Tooark.Attributes;

/// <summary>
/// Atributo de validação de código postal.
/// </summary>
/// <remarks>
/// O código postal é validado utilizando uma expressão regular.
/// Valor ausente é reportado como campo obrigatório.
/// </remarks>
/// <param name="propertyName">Nome do campo usado na mensagem de erro. Padrão: "ZipCode".</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ZipCodeValidationAttribute(string propertyName = "ZipCode") : TooarkValidationAttribute(propertyName)
{
  #region Methods

  /// <summary>
  /// Verifica se o valor é um código postal válido.
  /// </summary>
  /// <param name="value">O valor a ser verificado.</param>
  /// <returns>Verdadeiro quando o valor é um código postal válido.</returns>
  protected override bool IsSatisfied(string value) => Matches(value, RegexPattern.ZipCode);

  #endregion
}
