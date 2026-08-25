using Tooark.Attributes.Messages;
using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.Attributes;

/// <summary>
/// Atributo de validação de documento.
/// </summary>
/// <remarks>
/// O documento é validado pelo formato, com expressão regular, e pelos dígitos verificadores.
/// <para>
/// O tipo é recebido como texto porque argumento de atributo aceita apenas constante: um parâmetro do tipo
/// <see cref="EDocumentType"/> impediria o atributo de ser aplicado (CS0181). Valores aceitos, sem
/// diferenciar caixa: CPF, RG, CNH, CNPJ, CPF_CNPJ, CPF_RG, CPF_RG_CNH e None.
/// </para>
/// <para>
/// Um valor não reconhecido é erro de configuração e faz o atributo lançar. Antes ele virava
/// <see cref="EDocumentType.None"/>, que aceita qualquer documento: um erro de digitação no tipo
/// desligava a validação inteira sem aviso.
/// </para>
/// </remarks>
/// <param name="type">Tipo de documento a ser validado.</param>
/// <param name="propertyName">Nome do campo usado na mensagem de erro. Padrão: "Document".</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class DocumentValidationAttribute(string type, string propertyName = "Document") : TooarkValidationAttribute(propertyName)
{
  #region Private Fields

  /// <summary>
  /// Tipo de documento informado no atributo.
  /// </summary>
  private readonly string _type = type;

  /// <summary>
  /// Tipo de documento já resolvido, reaproveitado entre as validações.
  /// </summary>
  private EDocumentType? _resolved;

  #endregion

  #region Methods

  /// <summary>
  /// Verifica se o valor é um documento válido no formato e nos dígitos verificadores.
  /// </summary>
  /// <param name="value">O valor a ser verificado.</param>
  /// <returns>Verdadeiro quando o documento é válido.</returns>
  /// <exception cref="InternalServerErrorException">Se o tipo de documento informado não for reconhecido.</exception>
  protected override bool IsSatisfied(string value)
  {
    // Resolve o tipo na primeira validação: resolver no construtor faria a exceção ser engolida pelo
    // framework de validação, que descartaria o atributo e deixaria o campo sem validação alguma
    var documentType = _resolved ??= Resolve(_type);

    // Confere o formato e, em seguida, os dígitos verificadores
    return Matches(value, documentType.ToRegex()) && documentType.IsValid(value);
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Resolve o tipo de documento a partir do texto informado no atributo.
  /// </summary>
  /// <param name="type">Tipo de documento informado. Ausente é tratado como desconhecido.</param>
  /// <returns>O tipo de documento correspondente.</returns>
  /// <exception cref="InternalServerErrorException">Se o tipo informado não for reconhecido.</exception>
  private static EDocumentType Resolve(string? type)
  {
    // O tipo normalizado é o que de fato foi considerado, e é ele que a mensagem deve mostrar.
    // Tipo ausente vira texto vazio, que a conversão trata como desconhecido.
    var normalized = type?.Trim() ?? string.Empty;

    // A conversão implícita devolve None tanto para "None" quanto para um valor desconhecido
    EDocumentType resolved = normalized;

    // Só é None legítimo quando foi isso que o consumidor pediu
    if (ReferenceEquals(resolved, EDocumentType.None) &&
        !"None".Equals(normalized, StringComparison.OrdinalIgnoreCase))
    {
      throw new InternalServerErrorException($"{AttributeErrorMessages.DocumentTypeUnknown};{normalized}");
    }

    return resolved;
  }

  #endregion
}
