using Tooark.Exceptions;
using Tooark.Validations.Documents;
using Tooark.Validations.Patterns;

namespace Tooark.Enums;

/// <summary>
/// Tipos de documento.
/// </summary>
public sealed class EDocumentType
{
  #region Document Types

  /// <summary>
  /// Documento do tipo "None".
  /// </summary>
  public static readonly EDocumentType None = new(0, "None", @"^[a-zA-Z0-9.-]*$", _ => true);

  /// <summary>
  /// Documento do tipo "CPF".
  /// </summary>
  public static readonly EDocumentType CPF = new(1, "CPF", RegexPattern.Cpf, ValidateCpf);

  /// <summary>
  /// Documento do tipo "RG".
  /// </summary>
  public static readonly EDocumentType RG = new(2, "RG", RegexPattern.Rg, ValidateRg);

  /// <summary>
  /// Documento do tipo "CNH".
  /// </summary>
  public static readonly EDocumentType CNH = new(3, "CNH", RegexPattern.Cnh, ValidateCnh);

  /// <summary>
  /// Documento do tipo "CNPJ".
  /// </summary>
  public static readonly EDocumentType CNPJ = new(4, "CNPJ", RegexPattern.Cnpj, ValidateCnpj);

  /// <summary>
  /// Documento do tipo "CPF" ou "CNPJ".
  /// </summary>
  public static readonly EDocumentType CPF_CNPJ = new(5, "CPF_CNPJ", RegexPattern.CpfCnpj, value => ValidateCpf(value) || ValidateCnpj(value));

  /// <summary>
  /// Documento do tipo "CPF" ou "RG".
  /// </summary>
  public static readonly EDocumentType CPF_RG = new(6, "CPF_RG", RegexPattern.CpfRg, value => ValidateCpf(value) || ValidateRg(value));

  /// <summary>
  /// Documento do tipo "CPF", "RG" ou "CNH".
  /// </summary>
  public static readonly EDocumentType CPF_RG_CNH = new(7, "CPF_RG_CNH", RegexPattern.CpfRgCnh, value => ValidateCpf(value) || ValidateRg(value) || ValidateCnh(value));

  #endregion

  #region Constructor

  /// <summary>
  /// Construtor privado da classe.
  /// </summary>
  /// <param name="id">Id do tipo de documento.</param>
  /// <param name="description">Descrição do tipo de documento.</param>
  /// <param name="patternRegex">Padrão de regex do tipo de documento.</param>
  /// <param name="validator">Função de validação do tipo de documento.</param>
  /// <returns>Uma nova instância de <see cref="EDocumentType"/>.</returns>
  private EDocumentType(int id, string description, string patternRegex, Func<string, bool> validator)
  {
    Id = id;
    Description = description;
    PatternRegex = patternRegex;
    Validator = validator;
  }

  #endregion

  #region Private Properties

  /// <summary>
  /// Id do tipo de documento.
  /// </summary>
  private int Id { get; }

  /// <summary>
  /// Descrição do tipo de documento.
  /// </summary>
  private string Description { get; }

  /// <summary>
  /// Padrão de regex do tipo de documento.
  /// </summary>
  private string PatternRegex { get; }

  /// <summary>
  /// Função de validação do tipo de documento.
  /// </summary>
  private Func<string, bool> Validator { get; }

  #endregion

  #region Private Methods

  /// <summary>
  /// Função que retorna um tipo de documento a partir de sua descrição.
  /// </summary>
  /// <remarks>
  /// A caixa e os espaços das extremidades são normalizados. Descrição desconhecida resulta em
  /// <see cref="None"/>.
  /// </remarks>
  /// <param name="description">Descrição do tipo de documento.</param>
  /// <returns>Uma instância de <see cref="EDocumentType"/>.</returns>
  private static EDocumentType FromDescription(string description) => description?.Trim().ToUpperInvariant() switch
  {
    "CPF" => CPF,
    "RG" => RG,
    "CNH" => CNH,
    "CNPJ" => CNPJ,
    "CPF_CNPJ" => CPF_CNPJ,
    "CPF_RG" => CPF_RG,
    "CPF_RG_CNH" => CPF_RG_CNH,
    _ => None
  };

  /// <summary>
  /// Função que retorna um tipo de documento a partir de seu id.
  /// </summary>
  /// <param name="id">Id do tipo de documento.</param>
  /// <returns>Uma instância de <see cref="EDocumentType"/>.</returns>
  private static EDocumentType FromId(int id) => id switch
  {
    1 => CPF,
    2 => RG,
    3 => CNH,
    4 => CNPJ,
    5 => CPF_CNPJ,
    6 => CPF_RG,
    7 => CPF_RG_CNH,
    _ => None
  };

  #endregion

  #region Methods, Overrides and Implicit Operators

  /// <summary>
  /// Sobrescrita do método <see cref="object.ToString"/> para retornar a descrição do tipo de documento.
  /// </summary>
  /// <returns>A descrição do tipo de documento.</returns>
  public override string ToString() => Description;

  /// <summary>
  /// Método que retorna o id do tipo de documento.
  /// </summary>
  /// <returns>O id do tipo de documento.</returns>
  public int ToInt() => Id;

  /// <summary>
  /// Método que retorna o padrão de regex do tipo de documento.
  /// </summary>
  /// <returns>O padrão de regex do tipo de documento.</returns>
  public string ToRegex() => PatternRegex;

  /// <summary>
  /// Função que verifica os dígitos verificadores do tipo de documento.
  /// </summary>
  /// <remarks>
  /// Verifica apenas o conteúdo: a formatação é responsabilidade de <see cref="ToRegex"/>, e uma validação
  /// completa aplica os dois, como fazem o value object Document e o atributo de validação. A função aceita
  /// o valor com ou sem máscara e nunca lança para entrada inválida.
  /// </remarks>
  /// <returns>A função de validação do tipo de documento.</returns>
  public Func<string, bool> IsValid => Validator;

  /// <summary>
  /// Conversão implícita de <see cref="EDocumentType"/> para <see cref="int"/>.
  /// </summary>
  /// <param name="document">Instância de <see cref="EDocumentType"/>.</param>
  /// <returns>Id do tipo de documento.</returns>
  /// <exception cref="InternalServerErrorException">Lançada quando a instância é nula.</exception>
  public static implicit operator int(EDocumentType document) =>
    document?.Id ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="EDocumentType"/> para <see cref="string"/>.
  /// </summary>
  /// <param name="document">Instância de <see cref="EDocumentType"/>.</param>
  /// <returns>Descrição do tipo de documento.</returns>
  /// <exception cref="InternalServerErrorException">Lançada quando a instância é nula.</exception>
  public static implicit operator string(EDocumentType document) =>
    document?.Description ?? throw new InternalServerErrorException("Invalid.Parameter;null");

  /// <summary>
  /// Conversão implícita de <see cref="int"/> para <see cref="EDocumentType"/>.
  /// </summary>
  /// <param name="id">Id do tipo de documento.</param>
  /// <returns>Uma instância de <see cref="EDocumentType"/>.</returns>
  public static implicit operator EDocumentType(int id) => FromId(id);

  /// <summary>
  /// Conversão implícita de <see cref="string"/> para <see cref="EDocumentType"/>.
  /// </summary>
  /// <param name="description">Descrição do tipo de documento.</param>
  /// <returns>Uma instância de <see cref="EDocumentType"/>.</returns>
  public static implicit operator EDocumentType(string description) => FromDescription(description);

  #endregion

  #region Validates

  /// <summary>
  /// Método que valida um número de CNH.
  /// </summary>
  /// <param name="value">O número da CNH a ser validado.</param>
  /// <returns>True se o número da CNH for válido.</returns>
  private static bool ValidateCnh(string value) => DocumentDigit.IsCnh(value);

  /// <summary>
  /// Método que valida um número de CPF.
  /// </summary>
  /// <param name="value">O número do CPF a ser validado.</param>
  /// <returns>True se o número do CPF for válido.</returns>
  private static bool ValidateCpf(string value) => DocumentDigit.IsCpf(value);

  /// <summary>
  /// Método que valida um número de RG.
  /// </summary>
  /// <param name="value">O número do RG a ser validado.</param>
  /// <returns>True se o número do RG for válido.</returns>
  private static bool ValidateRg(string value) => DocumentDigit.IsRg(value);

  /// <summary>
  /// Método que valida um número de CNPJ.
  /// </summary>
  /// <param name="value">O número do CNPJ a ser validado.</param>
  /// <returns>True se o número do CNPJ for válido.</returns>
  private static bool ValidateCnpj(string value) => DocumentDigit.IsCnpj(value);

  #endregion
}
