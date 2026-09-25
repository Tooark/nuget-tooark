using System.Text.RegularExpressions;
using Tooark.Validations.Documents;
using Tooark.Validations.Messages;
using Tooark.Validations.Patterns;

namespace Tooark.Validations;

/// <summary>
/// Classe de validação de Documento.
/// </summary>
/// <remarks>
/// CPF e CNPJ são validados por formato e por dígitos verificadores. RG e CNH não possuem dígito
/// verificador de padrão nacional e são validados apenas por formato.
/// </remarks>
public partial class Validation
{
  #region Validates
  /// <summary>
  /// Função para validar documento.
  /// </summary>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="isValid">Condição de validade do documento.</param>
  /// <returns>Validação.</returns>
  private Validation ValidateDocument(string property, string message, Func<bool> isValid)
  {
    // Se o documento não for válido, adiciona a notificação.
    if (!isValid())
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.DOC1");
    }

    // Retorna uma validação.
    return this;
  }

  /// <summary>
  /// Verifica se o valor corresponde ao padrão informado.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="pattern">Padrão a ser comparado.</param>
  /// <returns>True quando o valor corresponde ao padrão.</returns>
  private static bool HasFormat(string value, string pattern) =>
    MatchFunc(value, pattern, RegexOptions.None, DefaultTimeout);

  /// <summary>
  /// Verifica se o valor é um CPF válido, por formato e por dígitos verificadores.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <returns>True quando o valor é um CPF válido.</returns>
  private static bool IsValidCpf(string value) =>
    HasFormat(value, RegexPattern.Cpf) && DocumentDigit.IsCpf(value);

  /// <summary>
  /// Verifica se o valor é um CNPJ válido, por formato e por dígitos verificadores.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <returns>True quando o valor é um CNPJ válido.</returns>
  private static bool IsValidCnpj(string value) =>
    HasFormat(value, RegexPattern.Cnpj) && DocumentDigit.IsCnpj(value);

  /// <summary>
  /// Verifica se o valor é um RG válido, por formato e por dígito verificador.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <returns>True quando o valor é um RG válido.</returns>
  private static bool IsValidRg(string value) =>
    HasFormat(value, RegexPattern.Rg) && DocumentDigit.IsRg(value);

  /// <summary>
  /// Verifica se o valor é uma CNH válida, por formato e por dígitos verificadores.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <returns>True quando o valor é uma CNH válida.</returns>
  private static bool IsValidCnh(string value) =>
    HasFormat(value, RegexPattern.Cnh) && DocumentDigit.IsCnh(value);
  #endregion

  #region IsCpf
  /// <summary>
  /// Verifica se o valor corresponde ao formato de CPF. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsCpf(string value, string property) =>
    IsCpf(value, property, ValidationErrorMessages.IsDocument(property, ValidationErrorMessages.CpfFormatter));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de CPF.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsCpf(string value, string property, string message) =>
    ValidateDocument(property, message, () => IsValidCpf(value));
  #endregion

  #region IsRg
  /// <summary>
  /// Verifica se o valor corresponde ao formato de RG. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsRg(string value, string property) =>
    IsRg(value, property, ValidationErrorMessages.IsDocument(property, ValidationErrorMessages.RgFormatter));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de RG.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsRg(string value, string property, string message) =>
    ValidateDocument(property, message, () => IsValidRg(value));
  #endregion

  #region IsCnh
  /// <summary>
  /// Verifica se o valor corresponde ao formato de CNH. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsCnh(string value, string property) =>
    IsCnh(value, property, ValidationErrorMessages.IsDocument(property, ValidationErrorMessages.CnhFormatter));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de CNH.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsCnh(string value, string property, string message) =>
    ValidateDocument(property, message, () => IsValidCnh(value));
  #endregion

  #region IsCpfRg
  /// <summary>
  /// Verifica se o valor corresponde ao formato de CPF ou RG. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsCpfRg(string value, string property) =>
    IsCpfRg(value, property, ValidationErrorMessages.IsDocument(property, ValidationErrorMessages.CpfRgFormatter));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de CPF ou RG.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsCpfRg(string value, string property, string message) =>
    ValidateDocument(property, message, () => IsValidCpf(value) || IsValidRg(value));
  #endregion

  #region IsCpfRgCnh
  /// <summary>
  /// Verifica se o valor corresponde ao formato de CPF, RG ou CNH. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsCpfRgCnh(string value, string property) =>
    IsCpfRgCnh(value, property, ValidationErrorMessages.IsDocument(property, ValidationErrorMessages.CpfRgCnhFormatter));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de CPF, RG ou CNH.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsCpfRgCnh(string value, string property, string message) =>
    ValidateDocument(property, message, () => IsValidCpf(value) || IsValidRg(value) || IsValidCnh(value));
  #endregion

  #region IsCnpj
  /// <summary>
  /// Verifica se o valor corresponde ao formato de CNPJ. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsCnpj(string value, string property) =>
    IsCnpj(value, property, ValidationErrorMessages.IsDocument(property, ValidationErrorMessages.CnpjFormatter));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de CNPJ.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsCnpj(string value, string property, string message) =>
    ValidateDocument(property, message, () => IsValidCnpj(value));
  #endregion

  #region IsCpfCnpj
  /// <summary>
  /// Verifica se o valor corresponde ao formato de CPF ou CNPJ. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsCpfCnpj(string value, string property) =>
    IsCpfCnpj(value, property, ValidationErrorMessages.IsDocument(property, ValidationErrorMessages.CpfCnpjFormatter));

  /// <summary>
  /// Verifica se o valor corresponde ao formato de CPF ou CNPJ.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsCpfCnpj(string value, string property, string message) =>
    ValidateDocument(property, message, () => IsValidCpf(value) || IsValidCnpj(value));
  #endregion
}
