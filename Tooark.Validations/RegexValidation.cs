using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Tooark.Validations.Messages;

namespace Tooark.Validations;

/// <summary>
/// Classe de validação de Regex.
/// </summary>
public partial class Validation
{
  #region Constants
  /// <summary>
  /// Tempo limite padrão para validação por expressão regular. Em milissegundos.
  /// </summary>
  internal const int DefaultTimeout = 300;
  #endregion

  #region Private Static Fields
  /// <summary>
  /// Cache de expressões regulares por padrão, opções e tempo limite.
  /// </summary>
  private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options, int Timeout), Regex> _regexCache = new();
  #endregion

  #region Validates
  /// <summary>
  /// Verifica se o valor corresponde ao padrão.
  /// </summary>
  /// <remarks>
  /// O tempo limite protege contra padrões suscetíveis a backtracking excessivo. Ao ser atingido, o valor
  /// é tratado como não correspondente: uma validação reprova a entrada, não interrompe o fluxo com exceção.
  /// </remarks>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="pattern">Padrão a ser comparado.</param>
  /// <param name="options">Opções de regex para Case Sensitive.</param>
  /// <param name="timeout">Tempo limite para validação. Em milissegundos.</param>
  /// <returns>True quando o valor corresponde ao padrão.</returns>
  private static bool MatchFunc(string value, string pattern, RegexOptions options, int timeout)
  {
    try
    {
      return GetRegex(pattern, options, timeout).IsMatch(value ?? "");
    }
    catch (RegexMatchTimeoutException)
    {
      // Entrada patológica não corresponde ao padrão
      return false;
    }
  }

  /// <summary>
  /// Obtém a expressão regular do cache, criando-a na primeira ocorrência de cada combinação.
  /// </summary>
  /// <remarks>
  /// O cache interno do <see cref="Regex"/> estático guarda apenas 15 combinações, enquanto o pacote
  /// oferece dezenas de padrões: em uso real as entradas se expulsam entre si e o padrão é recompilado a
  /// cada validação. O cache próprio elimina a recompilação e vale também para padrões da aplicação.
  /// </remarks>
  /// <param name="pattern">Padrão a ser comparado.</param>
  /// <param name="options">Opções de regex para Case Sensitive.</param>
  /// <param name="timeout">Tempo limite para validação. Em milissegundos.</param>
  /// <returns>Expressão regular correspondente à combinação.</returns>
  private static Regex GetRegex(string pattern, RegexOptions options, int timeout) =>
    _regexCache.GetOrAdd(
      (pattern, options, timeout),
      static key => new Regex(key.Pattern, key.Options, TimeSpan.FromMilliseconds(key.Timeout)));

  /// <summary>
  /// Função para validar condição.
  /// </summary>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="condition">Condição a ser validada.</param>
  /// <returns>Validação.</returns>
  private Validation Validate(string property, string message, Func<bool> condition)
  {
    // Se a condição for verdadeira, adicione a notificação.
    if (condition())
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.RGX1");
    }

    // Retorna uma validação.
    return this;
  }
  #endregion


  #region Match
  /// <summary>
  /// Verifica se o valor corresponde ao padrão. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="pattern">Padrão a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation Match(string value, string pattern, string property) =>
    Match(value, pattern, property, ValidationErrorMessages.Match(property, value));

  /// <summary>
  /// Verifica se o valor corresponde ao padrão. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="pattern">Padrão a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="options">Opções de regex para Case Sensitive.</param>
  /// <returns>Validação.</returns>
  public Validation Match(string value, string pattern, string property, RegexOptions options) =>
    Match(value, pattern, property, ValidationErrorMessages.Match(property, value), options);

  /// <summary>
  /// Verifica se o valor corresponde ao padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="pattern">Padrão a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="options">Opções de regex para Case Sensitive. Parâmetro opcional. Padrão RegexOptions.None.</param>
  /// <param name="timeout">Tempo limite para validação. Em milissegundos. Parâmetro opcional. Padrão 300.</param>
  /// <returns>Validação.</returns>
  public Validation Match(
    string value,
    string pattern,
    string property,
    string message,
    RegexOptions options = RegexOptions.None,
    int timeout = DefaultTimeout
  ) => Validate(property, message, () => !MatchFunc(value, pattern, options, timeout));
  #endregion

  #region NotMatch
  /// <summary>
  /// Verifica se o valor não corresponde ao padrão. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="pattern">Padrão a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation NotMatch(string value, string pattern, string property) =>
    NotMatch(value, pattern, property, ValidationErrorMessages.NotMatch(property, value));

  /// <summary>
  /// Verifica se o valor não corresponde ao padrão. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="pattern">Padrão a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="options">Opções de regex para Case Sensitive.</param>
  /// <returns>Validação.</returns>
  public Validation NotMatch(string value, string pattern, string property, RegexOptions options) =>
    NotMatch(value, pattern, property, ValidationErrorMessages.NotMatch(property, value), options);

  /// <summary>
  /// Verifica se o valor não corresponde ao padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="pattern">Padrão a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="options">Opções de regex para Case Sensitive. Parâmetro opcional. Padrão RegexOptions.None.</param>
  /// <param name="timeout">Tempo limite para validação. Em milissegundos. Parâmetro opcional. Padrão 300.</param>
  /// <returns>Validação.</returns>
  public Validation NotMatch(
    string value,
    string pattern,
    string property,
    string message,
    RegexOptions options = RegexOptions.None,
    int timeout = DefaultTimeout
  ) => Validate(property, message, () => MatchFunc(value, pattern, options, timeout));
  #endregion
}
