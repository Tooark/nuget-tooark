using System.Globalization;
using System.Text;

namespace Tooark.Utils;

/// <summary>
/// Classe estática que fornece métodos para normalização de strings.
/// </summary>
public static class Normalize
{
  /// <summary>
  /// Normaliza um valor removendo espaços, convertendo para maiúscula e substituindo caracteres especiais.
  /// </summary>
  /// <param name="value">O valor a ser normalizado.</param>
  /// <returns>Uma string contendo o valor normalizado.</returns>
  public static string Value(string value)
  {
    return InternalNormalize.Value(value);
  }
}

/// <summary>
/// Classe interna estática que fornece métodos para normalização de strings.
/// </summary>
internal static class InternalNormalize
{
  /// <summary>
  /// Normaliza um valor removendo espaços, convertendo para maiúscula e substituindo caracteres especiais.
  /// </summary>
  /// <param name="value">O valor a ser normalizado.</param>
  /// <returns>Uma string contendo o valor normalizado.</returns>
  internal static string Value(string value)
  {
    // Verifica se o valor é nulo
    if (value == null)
    {
      return "";
    }

    // Normaliza a string para decompor caracteres acentuados
    var normalized = value.Normalize(NormalizationForm.FormD);

    // StringBuilder para construir o valor normalizado
    var normalizedValue = new StringBuilder();

    // Percorre cada caractere do valor
    foreach (char c in normalized)
    {
      // Ignora diacríticos
      if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
      {
        continue;
      }

      // translitera casos especiais que não decompõem bem
      switch (char.ToLowerInvariant(c))
      {
        case '&': // E comercial
          normalizedValue.Append("AND");
          continue;
        case '@': // Arroba
          normalizedValue.Append("AT");
          continue;
        case '$': // Cifrão
          normalizedValue.Append("DOLLAR");
          continue;
        case '€': // Euro
          normalizedValue.Append("EURO");
          continue;
        case '£': // Libra esterlina
          normalizedValue.Append("POUND");
          continue;
        case '¥': // Iene
          normalizedValue.Append("YEN");
          continue;
        case '©': // Símbolo de copyright
          normalizedValue.Append('C');
          continue;
        case '®': // Símbolo de marca registrada
          normalizedValue.Append('R');
          continue;
        case '™': // Símbolo de marca comercial
          normalizedValue.Append("TM");
          continue;
        case 'º': // Masculino ordinal
          normalizedValue.Append('O');
          continue;
        case 'ª': // Feminino ordinal
          normalizedValue.Append('A');
          continue;
        case 'ß': // Eszett alemão
          normalizedValue.Append("SS");
          continue;
        case 'æ': // Ligadura ae
          normalizedValue.Append("AE");
          continue;
        case 'œ': // Ligadura oe
          normalizedValue.Append("OE");
          continue;
        case 'ø': // O com barra, e o maiúsculo 'Ø'
          normalizedValue.Append('O');
          continue;
        case 'đ': // D com barra, e o maiúsculo 'Đ'
        case 'ð': // Eth islandês, e o maiúsculo 'Ð'
          normalizedValue.Append('D');
          continue;
        case 'ł': // L com barra, e o maiúsculo 'Ł'
          normalizedValue.Append('L');
          continue;
        case 'þ': // Thorn islandês
          normalizedValue.Append("TH");
          continue;
        default:
          // Converte o caractere para maiúscula
          var upper = char.ToUpperInvariant(c);

          // Verifica se o caractere é um número ou letra maiúscula
          if ((upper >= '0' && upper <= '9') || (upper >= 'A' && upper <= 'Z'))
          {
            normalizedValue.Append(upper);
          }

          continue;
      }
    }

    // Retorna o valor normalizado
    return normalizedValue.ToString();
  }
}
