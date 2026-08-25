using Tooark.Validations.Messages;
using Tooark.Validations.Patterns;

namespace Tooark.Validations;

/// <summary>
/// Classe de validação de Email.
/// </summary>
public partial class Validation
{
  #region IsEmail
  /// <summary>
  /// Verifica se o valor corresponde ao formato de Email. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmail(string value, string property) =>
    IsEmail(value, property, ValidationErrorMessages.IsValid(property, "Email"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de Email.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmail(string value, string property, string message) =>
    Match(value, RegexPattern.Email, property, message);
  #endregion

  #region IsEmailOrEmpty
  /// <summary>
  /// Verifica se o valor corresponde ao formato de Email ou vazio. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmailOrEmpty(string? value, string property) =>
    IsEmailOrEmpty(value, property, ValidationErrorMessages.IsValid(property, "EmailOrEmpty"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de Email ou vazio.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmailOrEmpty(string? value, string property, string message) =>
    string.IsNullOrEmpty(value) ? this : IsEmail(value, property, message);
  #endregion

  #region IsEmailDomain
  /// <summary>
  /// Verifica se o valor corresponde ao formato de Domínio de Email. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmailDomain(string value, string property) =>
    IsEmailDomain(value, property, ValidationErrorMessages.IsValid(property, "EmailDomain"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de Domínio de Email.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmailDomain(string value, string property, string message) =>
    Match(value, RegexPattern.EmailDomain, property, message);
  #endregion

  #region IsEmailDomainOrEmpty
  /// <summary>
  /// Verifica se o valor corresponde ao formato de Domínio de Email ou vazio. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmailDomainOrEmpty(string? value, string property) =>
    IsEmailDomainOrEmpty(value, property, ValidationErrorMessages.IsValid(property, "EmailDomainOrEmpty"));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de Domínio de Email ou vazio.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmailDomainOrEmpty(string? value, string property, string message) =>
    string.IsNullOrEmpty(value) ? this : IsEmailDomain(value, property, message);
  #endregion
}
