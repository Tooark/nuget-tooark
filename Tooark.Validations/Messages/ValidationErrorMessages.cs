namespace Tooark.Validations.Messages;

/// <summary>
/// Classe com as mensagens de erro geradas pelas validações.
/// </summary>
/// <remarks>
/// Cada método devolve uma chave de tradução, e não o texto final, no formato
/// <c>Validation.{Regra};{Propriedade}</c>. A propriedade tem os espaços em branco removidos.
/// </remarks>
public static class ValidationErrorMessages
{
  #region Private Methods

  /// <summary>
  /// Normaliza a propriedade.
  /// </summary>
  /// <remarks>
  /// Remove os espaços em branco e os substitui por vazio.
  /// </remarks>
  /// <param name="property">Propriedade a ser normalizada.</param>
  /// <returns>Propriedade normalizada.</returns>
  private static string PropertyNormalize(string property) => property.Trim().Replace(" ", "");

  #endregion

  #region Methods

  /// <summary>
  /// Mensagem de erro para validação se o boolean é falso.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é falso.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string BooleanIsFalse(string property) => $"Validation.IsNotFalse;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se o boolean é verdadeiro.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é verdadeiro.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string BooleanIsTrue(string property) => $"Validation.IsNotTrue;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se é maior que valor informado.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é maior que {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsGreater(string property, string comparer) => $"Validation.IsNotGreater;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se é maior ou igual ao valor informado.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é maior ou igual a {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsGreaterOrEquals(string property, string comparer) => $"Validation.IsNotGreaterOrEquals;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se é menor que valor informado.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é menor que {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsLower(string property, string comparer) => $"Validation.IsNotLower;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se é menor ou igual ao valor informado.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é menor ou igual a {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsLowerOrEquals(string property, string comparer) => $"Validation.IsNotLowerOrEquals;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se está entre os valores informados.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não está entre {1} e {2}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="start">Valor inicial.</param>
  /// <param name="end">Valor final.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsBetween(string property, string start, string end) => $"Validation.IsNotBetween;{PropertyNormalize(property)};{start};{end}";

  /// <summary>
  /// Mensagem de erro para validação se não está entre os valores informados.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} está entre {1} e {2}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="start">Valor inicial.</param>
  /// <param name="end">Valor final.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNotBetween(string property, string start, string end) => $"Validation.IsBetween;{PropertyNormalize(property)};{start};{end}";

  /// <summary>
  /// Mensagem de erro para validação se é igual ao menor valor do tipo.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é o menor valor: {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsMin(string property, string comparer) => $"Validation.IsNotMin;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se não é igual ao menor valor do tipo.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} é o menor valor: {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNotMin(string property, string comparer) => $"Validation.IsMin;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se é igual ao maior valor do tipo.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é o maior valor: {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsMax(string property, string comparer) => $"Validation.IsNotMax;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se não é igual ao maior valor do tipo.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} é o maior valor: {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNotMax(string property, string comparer) => $"Validation.IsMax;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se é igual ao valor informado.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é igual a {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string AreEquals(string property, string comparer) => $"Validation.AreNotEquals;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se não é igual ao valor informado.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} é igual a {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="comparer">Valor de comparação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string AreNotEquals(string property, string comparer) => $"Validation.AreEquals;{PropertyNormalize(property)};{comparer}";

  /// <summary>
  /// Mensagem de erro para validação se contém o valor informado.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não contém {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="value">Valor buscado.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string Contains(string property, string value) => $"Validation.NotContains;{PropertyNormalize(property)};{value}";

  /// <summary>
  /// Mensagem de erro para validação se não contém o valor informado.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} contém {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="value">Valor buscado.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string NotContains(string property, string value) => $"Validation.Contains;{PropertyNormalize(property)};{value}";

  /// <summary>
  /// Mensagem de erro para validação se todos são o valor informado.
  /// </summary>
  /// <remarks>
  /// Os valores da propriedade {0} não são todos {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="value">Valor buscado.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string All(string property, string value) => $"Validation.NotAll;{PropertyNormalize(property)};{value}";

  /// <summary>
  /// Mensagem de erro para validação se nem todos são o valor informado.
  /// </summary>
  /// <remarks>
  /// Os valores da propriedade {0} são todos {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="value">Valor buscado.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string NotAll(string property, string value) => $"Validation.All;{PropertyNormalize(property)};{value}";

  /// <summary>
  /// Mensagem de erro para validação se é nulo.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é nulo.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNull(string property) => $"Validation.IsNotNull;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se não é nulo.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} é nulo.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNotNull(string property) => $"Validation.IsNull;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se é vazio.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é vazio.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsEmpty(string property) => $"Validation.IsNotEmpty;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se não é vazio.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} é vazio.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNotEmpty(string property) => $"Validation.IsEmpty;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se corresponde a um regex.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não corresponde ao padrão {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="value">Valor buscado.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string Match(string property, string value) => $"Validation.NotMatch;{PropertyNormalize(property)};{value}";

  /// <summary>
  /// Mensagem de erro para validação se não corresponde a um regex.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} corresponde ao padrão {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="value">Valor buscado.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string NotMatch(string property, string value) => $"Validation.Match;{PropertyNormalize(property)};{value}";

  /// <summary>
  /// Mensagem de erro para validação se é nulo ou vazio.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é nulo nem vazio.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNullOrEmpty(string property) => $"Validation.IsNotNullOrEmpty;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se não é nulo nem vazio.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} é nulo ou vazio.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNotNullOrEmpty(string property) => $"Validation.IsNullOrEmpty;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se é nulo, vazio ou espaço em branco.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é nulo, vazio ou espaço em branco.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNullOrWhiteSpace(string property) => $"Validation.IsNotNullOrWhiteSpace;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se não é nulo, vazio nem espaço em branco.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} é nulo, vazio ou espaço em branco.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsNotNullOrWhiteSpace(string property) => $"Validation.IsNullOrWhiteSpace;{PropertyNormalize(property)}";

  /// <summary>
  /// Mensagem de erro para validação se Documento é valido.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é um documento válido: {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="formatter">Formatado do documento.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsDocument(string property, string formatter) => $"Validation.IsNotDocument;{PropertyNormalize(property)};{formatter}";

  /// <summary>
  /// Mensagem de erro para validação campo genérico.
  /// </summary>
  /// <remarks>
  /// O valor da propriedade {0} não é válido para {1}.
  /// </remarks>
  /// <param name="property">Propriedade da validação.</param>
  /// <param name="type">Tipo da validação.</param>
  /// <returns>Mensagem de erro.</returns>
  public static string IsValid(string property, string type) => $"Validation.IsNotValid;{PropertyNormalize(property)};{type}";

  #endregion

  #region Properties

  /// <summary>
  /// Formatação de CPF.
  /// </summary>
  /// <returns>Formato do CPF.</returns>
  public static string CpfFormatter => "xxx.xxx.xxx-xx";

  /// <summary>
  /// Formatação de RG.
  /// </summary>
  /// <returns>Formato do RG.</returns>
  public static string RgFormatter => "xx.xxx.xxx-x";

  /// <summary>
  /// Formatação de CNH.
  /// </summary>
  /// <returns>Formato do CNH.</returns>
  public static string CnhFormatter => "xxxxxxxxxxx";

  /// <summary>
  /// Formatação de CNPJ.
  /// </summary>
  /// <returns>Formato do CNPJ.</returns>
  public static string CnpjFormatter => "xx.xxx.xxx/xxxx-xx";

  /// <summary>
  /// Formatação de CPF e RG.
  /// </summary>
  /// <returns>Formato do CPF e RG.</returns>
  public static string CpfRgFormatter => "xxx.xxx.xxx-xx | xx.xxx.xxx-x";

  /// <summary>
  /// Formatação de CPF, RG e CNH.
  /// </summary>
  /// <returns>Formato do CPF, RG e CNH.</returns>
  public static string CpfRgCnhFormatter => "xxx.xxx.xxx-xx | xx.xxx.xxx-x | xxxxxxxxxxx";

  /// <summary>
  /// Formatação de CPF e CNPJ.
  /// </summary>
  /// <returns>Formato do CPF e CNPJ.</returns>
  public static string CpfCnpjFormatter => "xxx.xxx.xxx-xx | xx.xxx.xxx/xxxx-xx";

  #endregion
}
