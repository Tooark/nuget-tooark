using System.ComponentModel.DataAnnotations;
using Tooark.Validations.Patterns;

namespace Tooark.Attributes;

/// <summary>
/// Atributo de validação de endereço de email.
/// </summary>
/// <remarks>
/// O endereço de email é validado utilizando uma expressão regular.
/// Valor ausente é reportado como campo obrigatório: para tornar o campo opcional, aplique o atributo
/// apenas quando houver valor, ou valide o campo fora do atributo.
/// </remarks>
/// <param name="propertyName">Nome do campo usado na mensagem de erro. Padrão: "Email".</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class EmailValidationAttribute(string propertyName = "Email") : TooarkValidationAttribute(propertyName)
{
  #region Methods

  /// <summary>
  /// Verifica se o valor é um email válido.
  /// </summary>
  /// <param name="value">O valor a ser verificado.</param>
  /// <returns>Verdadeiro quando o valor é um email válido.</returns>
  protected override bool IsSatisfied(string value) => Matches(value, RegexPattern.Email);

  #endregion
}
