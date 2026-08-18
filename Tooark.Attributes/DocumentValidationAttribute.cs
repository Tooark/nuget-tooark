using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Tooark.Enums;

namespace Tooark.Attributes;

/// <summary>
/// Atributo de validação de documento.
/// </summary>
/// <remarks>
/// O documento é validado pelo formato, com expressão regular, e pelos dígitos verificadores.
/// O tipo é recebido como texto porque argumento de atributo aceita apenas constante: um parâmetro
/// do tipo <see cref="EDocumentType"/> impediria o atributo de ser aplicado (CS0181).
/// Valores aceitos, sem diferenciar caixa: CPF, RG, CNH, CNPJ, CPF_CNPJ, CPF_RG e CPF_RG_CNH.
/// Um valor não reconhecido resulta em <see cref="EDocumentType.None"/>, que aceita qualquer documento.
/// </remarks>
/// <param name="type">Tipo de documento a ser validado.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public partial class DocumentValidationAttribute(string type) : ValidationAttribute
{
  /// <summary>
  /// Tipo de documento resolvido a partir do texto informado no atributo.
  /// </summary>
  private readonly EDocumentType _type = type;

  /// <summary>
  /// Sobrescreve o método de validação para verificar se o valor é um documento válido.
  /// </summary>
  /// <param name="value">O objeto a ser validado.</param>
  /// <returns>Retornar verdadeiro se o valor for um documento válido.</returns>
  public override bool IsValid(object? value)
  {
    // Converta o valor para uma string.
    string? document = value?.ToString();

    // Verifique se o valor é nulo.
    if (string.IsNullOrEmpty(document))
    {
      // Defina a mensagem de erro padrão.
      ErrorMessage = "Field.Required;Document";

      // Retorne falso.
      return false;
    }

    // Verifique se o valor é um document válido, por formato e por dígitos verificadores.
    if (!HasFormat(document) || !_type.IsValid(document))
    {
      // Defina a mensagem de erro padrão.
      ErrorMessage = "Field.Invalid;Document";

      // Retorne falso.
      return false;
    }

    // Retorne verdadeiro.
    return true;
  }

  /// <summary>
  /// Verifica se o documento corresponde ao formato do tipo configurado.
  /// </summary>
  /// <remarks>
  /// O tempo limite protege contra entradas patológicas; atingi-lo significa que o valor não corresponde
  /// ao padrão, e não uma exceção subindo de um atributo de validação.
  /// </remarks>
  /// <param name="document">Documento a ser verificado.</param>
  /// <returns>Verdadeiro quando o documento corresponde ao formato.</returns>
  private bool HasFormat(string document)
  {
    try
    {
      return Regex.IsMatch(document, _type.ToRegex(), RegexOptions.None, TimeSpan.FromMilliseconds(300));
    }
    catch (RegexMatchTimeoutException)
    {
      return false;
    }
  }
}
