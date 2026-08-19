namespace Tooark.Attributes.Messages;

/// <summary>
/// Classe com as mensagens de erro geradas pelos atributos de validação.
/// </summary>
/// <remarks>
/// São chaves de tradução, e não textos finais, no formato <c>Chave;parametro</c>. As traduções
/// acompanham os recursos do <c>Tooark.Extensions</c>.
/// </remarks>
public static class AttributeErrorMessages
{
  #region Constants

  /// <summary>
  /// Mensagem de erro para quando o campo não foi informado. Acompanha o nome do campo.
  /// </summary>
  public const string FieldRequired = "Field.Required";

  /// <summary>
  /// Mensagem de erro para quando o valor do campo não é válido. Acompanha o nome do campo.
  /// </summary>
  public const string FieldInvalid = "Field.Invalid";

  /// <summary>
  /// Erro de configuração para tipo de documento não reconhecido. Acompanha o tipo informado.
  /// </summary>
  /// <remarks>
  /// Não é erro do valor validado, e sim do atributo aplicado no código.
  /// </remarks>
  public const string DocumentTypeUnknown = "Attributes.DocumentTypeUnknown";

  /// <summary>
  /// Erro de configuração para link de vídeo sem nenhum provedor habilitado. Acompanha o nome do campo.
  /// </summary>
  /// <remarks>
  /// Não é erro do valor validado, e sim do atributo aplicado no código: sem provedor habilitado,
  /// nenhum link poderia ser aceito.
  /// </remarks>
  public const string LinkVideoNoProvider = "Attributes.LinkVideoNoProvider";

  #endregion
}
