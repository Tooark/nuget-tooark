using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Tooark.Attributes.Messages;

namespace Tooark.Attributes;

/// <summary>
/// Classe base dos atributos de validação do Tooark.
/// </summary>
/// <remarks>
/// Concentra o que é comum aos atributos: valor ausente, montagem da mensagem e execução da expressão
/// regular com tempo limite.
/// <para>
/// A mensagem é devolvida por chamada, em um <see cref="ValidationResult"/>, e nunca gravada em
/// <see cref="ValidationAttribute.ErrorMessage"/>. O framework de validação reaproveita a mesma instância
/// do atributo em todas as validações daquele membro, inclusive concorrentes: escrever nela mistura a
/// mensagem de uma validação com a de outra e descarta a mensagem que o consumidor tenha configurado.
/// </para>
/// </remarks>
/// <param name="propertyName">Nome do campo usado na mensagem de erro.</param>
public abstract class TooarkValidationAttribute(string propertyName) : ValidationAttribute
{
  #region Constants

  /// <summary>
  /// Tempo máximo de execução de cada expressão regular, em milissegundos.
  /// </summary>
  private const int RegexTimeoutMilliseconds = 300;

  #endregion

  #region Properties

  /// <summary>
  /// Nome do campo usado na mensagem de erro.
  /// </summary>
  protected string PropertyName { get; } = string.IsNullOrWhiteSpace(propertyName) ? "Field" : propertyName.Trim();

  /// <summary>
  /// Indica se o consumidor configurou a própria mensagem de erro no atributo.
  /// </summary>
  private bool HasCustomMessage => !string.IsNullOrEmpty(ErrorMessage) || !string.IsNullOrEmpty(ErrorMessageResourceName);

  #endregion

  #region Methods

  /// <summary>
  /// Valida o valor e devolve o resultado, sem alterar o estado do atributo.
  /// </summary>
  /// <param name="value">O objeto a ser validado.</param>
  /// <param name="validationContext">O contexto da validação. Nulo quando chamado por <see cref="ValidationAttribute.IsValid(object?)"/>.</param>
  /// <returns><see cref="ValidationResult.Success"/> quando o valor é válido, ou o resultado com a mensagem de erro.</returns>
  protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
  {
    // Converte o valor para texto, aceitando qualquer tipo que saiba se representar
    var text = value?.ToString();

    // Valor ausente é reportado como campo obrigatório
    if (string.IsNullOrWhiteSpace(text))
    {
      return Failure(AttributeErrorMessages.FieldRequired, validationContext);
    }

    // Delega a regra específica do atributo
    return IsSatisfied(text) ?
      ValidationResult.Success :
      Failure(AttributeErrorMessages.FieldInvalid, validationContext);
  }

  /// <summary>
  /// Verifica se o valor satisfaz a regra do atributo.
  /// </summary>
  /// <param name="value">O valor a ser verificado, nunca nulo nem em branco.</param>
  /// <returns>Verdadeiro quando o valor satisfaz a regra.</returns>
  protected abstract bool IsSatisfied(string value);

  /// <summary>
  /// Aplica uma expressão regular ao valor, com tempo limite.
  /// </summary>
  /// <remarks>
  /// O tempo limite protege contra entradas que provocam retrocesso excessivo. Atingi-lo significa que o
  /// valor não corresponde ao padrão, e não uma exceção subindo de um atributo de validação.
  /// </remarks>
  /// <param name="value">O valor a ser verificado.</param>
  /// <param name="pattern">O padrão a ser aplicado.</param>
  /// <returns>Verdadeiro quando o valor corresponde ao padrão.</returns>
  protected static bool Matches(string value, string pattern)
  {
    try
    {
      return Regex.IsMatch(value, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(RegexTimeoutMilliseconds));
    }
    catch (RegexMatchTimeoutException)
    {
      // Entrada patológica não corresponde ao padrão, e não derruba a validação
      return false;
    }
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Monta o resultado da falha, respeitando a mensagem configurada pelo consumidor.
  /// </summary>
  /// <param name="key">A chave de tradução da mensagem padrão.</param>
  /// <param name="validationContext">O contexto da validação, quando disponível.</param>
  /// <returns>O resultado da validação com a mensagem de erro.</returns>
  private ValidationResult Failure(string key, ValidationContext? validationContext)
  {
    // O nome do membro alimenta a mensagem configurada pelo consumidor e o resultado da validação
    var member = validationContext?.MemberName;

    // A mensagem do consumidor tem precedência sobre a chave padrão
    var message = HasCustomMessage ?
      FormatErrorMessage(member ?? PropertyName) :
      $"{key};{PropertyName}";

    return member is null ? new ValidationResult(message) : new ValidationResult(message, [member]);
  }

  #endregion
}
