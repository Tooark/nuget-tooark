using System.Globalization;
using System.Text;
using Tooark.Utils;

namespace Tooark.Extensions;

/// <summary>
/// Método de extensão para a classe String.
/// </summary>
public static class StringExtensions
{
  /// <summary>
  /// Converte uma string para Base64.
  /// </summary>
  /// <param name="value">A string a ser convertida.</param>
  /// <returns>A string convertida para Base64.</returns>
  public static string ToBase64(this string? value)
  {
    return InternalStringExtensions.ToBase64(value);
  }

  /// <summary>
  /// Converte uma string de Base64 para texto.
  /// </summary>
  /// <param name="value">A string em Base64 a ser convertida.</param>
  /// <returns>A string convertida de Base64 para texto.</returns>
  public static string FromBase64(this string? value)
  {
    return InternalStringExtensions.FromBase64(value);
  }

  /// <summary>
  /// Converte uma string para slug.
  /// </summary>
  /// <param name="value">A string a ser convertida.</param>
  /// <returns>A string convertida para slug.</returns>
  public static string ToSlug(this string? value)
  {
    return InternalStringExtensions.ToSlug(value);
  }

  /// <summary>
  /// Normaliza uma string.
  /// </summary>
  /// <remarks>
  /// Remove espaços e substitui caracteres acentuados por seus equivalentes não acentuados e converte para maiúscula.
  /// </remarks>
  /// <param name="value">A string a ser normalizada.</param>
  /// <returns>Uma string normalizada.</returns>
  public static string ToNormalize(this string? value)
  {
    return InternalStringExtensions.ToNormalize(value);
  }

  /// <summary>
  /// Converte uma string de Snake Case para Pascal Case.
  /// </summary>
  /// <param name="value">A string em Snake Case.</param>
  /// <returns>A string convertida para Pascal Case.</returns>
  public static string FromSnakeToPascalCase(this string? value)
  {
    return InternalStringExtensions.FromSnakeToPascalCase(value);
  }

  /// <summary>
  /// Converte uma string de Snake Case para Camel Case.
  /// </summary>
  /// <param name="value">A string em Snake Case.</param>
  /// <returns>A string convertida para Camel Case.</returns>
  public static string FromSnakeToCamelCase(this string? value)
  {
    return InternalStringExtensions.FromSnakeToCamelCase(value);
  }

  /// <summary>
  /// Converte uma string de Snake Case para Kebab Case.
  /// </summary>
  /// <param name="value">A string em Snake Case.</param>
  /// <returns>A string convertida para Kebab Case.</returns>
  public static string FromSnakeToKebabCase(this string? value)
  {
    return InternalStringExtensions.FromSnakeToKebabCase(value);
  }

  /// <summary>
  /// Converte uma string de Pascal Case para Snake Case.
  /// </summary>
  /// <param name="value">A string em Pascal Case.</param>
  /// <returns>A string convertida para Snake Case.</returns>
  public static string FromPascalToSnakeCase(this string? value)
  {
    return InternalStringExtensions.FromPascalToSnakeCase(value);
  }

  /// <summary>
  /// Converte uma string de Camel Case para Snake Case.
  /// </summary>
  /// <param name="value">A string em Camel Case.</param>
  /// <returns>A string convertida para Snake Case.</returns>
  public static string FromCamelToSnakeCase(this string? value)
  {
    return InternalStringExtensions.FromCamelToSnakeCase(value);
  }

  /// <summary>
  /// Converte uma string de Kebab Case para Snake Case.
  /// </summary>
  /// <param name="value">A string em Kebab Case.</param>
  /// <returns>A string convertida para Snake Case.</returns>
  public static string FromKebabToSnakeCase(this string? value)
  {
    return InternalStringExtensions.FromKebabToSnakeCase(value);
  }
}

/// <summary>
/// Método interno de extensão para a classe String.
/// </summary>
internal static class InternalStringExtensions
{
  #region Internal Methods

  /// <summary>
  /// Converte uma string para Base64.
  /// </summary>
  /// <param name="value">A string a ser convertida.</param>
  /// <returns>A string convertida para Base64.</returns>
  internal static string ToBase64(this string? value)
  {
    // Valor ausente não tem representação em Base64
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
  }

  /// <summary>
  /// Converte uma string de Base64 para texto.
  /// </summary>
  /// <param name="value">A string em Base64 a ser convertida.</param>
  /// <returns>A string convertida de Base64 para texto.</returns>
  internal static string FromBase64(this string? value)
  {
    // Valor ausente não tem texto a recuperar
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    return Encoding.UTF8.GetString(Convert.FromBase64String(value));
  }

  /// <summary>
  /// Converte uma string para slug.
  /// </summary>
  /// <param name="value">A string a ser convertida.</param>
  /// <returns>A string convertida para slug.</returns>
  internal static string ToSlug(this string? value)
  {
    // Verifica se a string é nula ou vazia
    if (string.IsNullOrWhiteSpace(value))
    {
      return string.Empty;
    }

    var normalizedString = value.Normalize(NormalizationForm.FormD);
    var slugBuilder = new StringBuilder(normalizedString.Length);
    var lastWasHyphen = false;

    // Percorre cada caractere da string normalizada
    foreach (var c in normalizedString)
    {
      // Ignora diacríticos
      if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
      {
        continue;
      }

      var lower = char.ToLowerInvariant(c);

      // Transliteração pontual (após remover diacríticos)
      switch (lower)
      {
        case '&': // E comercial
          AppendDiacritics("and", slugBuilder, ref lastWasHyphen);
          continue;
        case '@': // Arroba
          AppendDiacritics("at", slugBuilder, ref lastWasHyphen);
          continue;
        case '$': // Cifrão
          AppendDiacritics("dollar", slugBuilder, ref lastWasHyphen);
          continue;
        case '€': // Euro
          AppendDiacritics("euro", slugBuilder, ref lastWasHyphen);
          continue;
        case '£': // Libra esterlina
          AppendDiacritics("pound", slugBuilder, ref lastWasHyphen);
          continue;
        case '¥': // Iene
          AppendDiacritics("yen", slugBuilder, ref lastWasHyphen);
          continue;
        case '©': // Símbolo de copyright
          AppendDiacritics("c", slugBuilder, ref lastWasHyphen);
          continue;
        case '®': // Símbolo de marca registrada
          AppendDiacritics("r", slugBuilder, ref lastWasHyphen);
          continue;
        case '™': // Símbolo de marca comercial
          AppendDiacritics("tm", slugBuilder, ref lastWasHyphen);
          continue;
        case 'º': // Masculino ordinal
          AppendDiacritics("o", slugBuilder, ref lastWasHyphen);
          continue;
        case 'ª': // Feminino ordinal
          AppendDiacritics("a", slugBuilder, ref lastWasHyphen);
          continue;
        case 'ß': // Eszett alemão
          AppendDiacritics("ss", slugBuilder, ref lastWasHyphen);
          continue;
        case 'æ': // Ligadura ae
          AppendDiacritics("ae", slugBuilder, ref lastWasHyphen);
          continue;
        case 'œ': // Ligadura oe
          AppendDiacritics("oe", slugBuilder, ref lastWasHyphen);
          continue;
        case 'ø': // O com barra, e o maiúsculo 'Ø'
          AppendDiacritics("o", slugBuilder, ref lastWasHyphen);
          continue;
        case 'đ': // D com barra, e o maiúsculo 'Đ'
        case 'ð': // Eth islandês, e o maiúsculo 'Ð'
          AppendDiacritics("d", slugBuilder, ref lastWasHyphen);
          continue;
        case 'ł': // L com barra, e o maiúsculo 'Ł'
          AppendDiacritics("l", slugBuilder, ref lastWasHyphen);
          continue;
        case 'þ': // Thorn islandês
          AppendDiacritics("th", slugBuilder, ref lastWasHyphen);
          continue;
      }

      // Letras e números são mantidos
      if ((lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9'))
      {
        slugBuilder.Append(lower);
        lastWasHyphen = false;
        continue;
      }

      // Espaços e caracteres especiais viram hífen, com compactação
      if (slugBuilder.Length > 0 && !lastWasHyphen)
      {
        slugBuilder.Append('-');
        lastWasHyphen = true;
      }
    }

    // Remove hífen no fim
    if (slugBuilder.Length > 0 && slugBuilder[^1] == '-')
    {
      slugBuilder.Length--;
    }

    return slugBuilder.ToString();
  }

  /// <summary>
  /// Normaliza uma string.
  /// </summary>
  /// <remarks>
  /// Remove espaços e substitui caracteres acentuados por seus equivalentes não acentuados e converte para maiúscula.
  /// </remarks>
  /// <param name="value">A string a ser normalizada.</param>
  /// <returns>Uma string normalizada.</returns>
  internal static string ToNormalize(this string? value)
  {
    return Normalize.Value(value);
  }

  /// <summary>
  /// Converte uma string de Snake Case para Pascal Case.
  /// </summary>
  /// <param name="value">A string em Snake Case.</param>
  /// <returns>A string convertida para Pascal Case.</returns>
  internal static string FromSnakeToPascalCase(this string? value)
  {
    // Verifica se a string é nula ou vazia
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    // Converte a string para Pascal Case
    return string
      .Join("", value
      .Split('_')
      .Where(word => !string.IsNullOrEmpty(word))
      .Select(word => word.Length > 0 ? (char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()) : ""));
  }

  /// <summary>
  /// Converte uma string de Snake Case para Camel Case.
  /// </summary>
  /// <param name="value">A string em Snake Case.</param>
  /// <returns>A string convertida para Camel Case.</returns>
  internal static string FromSnakeToCamelCase(this string? value)
  {
    // Verifica se a string é nula ou vazia
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    // Converte a string para Pascal Case
    var pascalCase = FromSnakeToPascalCase(value);

    // Converte a string para Camel Case, mantendo a primeira letra minúscula
    return pascalCase.Length > 0 ? (char.ToLowerInvariant(pascalCase[0]) + pascalCase[1..]) : "";
  }

  /// <summary>
  /// Converte uma string de Snake Case para Kebab Case.
  /// </summary>
  /// <param name="value">A string em Snake Case.</param>
  /// <returns>A string convertida para Kebab Case.</returns>
  internal static string FromSnakeToKebabCase(this string? value)
  {
    // Verifica se a string é nula ou vazia
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    // Converte a string para Kebab Case
    return string
            .Join('-', value.Split('_')
            .Where(word => !string.IsNullOrEmpty(word))
            .Select(word => word.ToLowerInvariant()));
  }

  /// <summary>
  /// Converte uma string de Pascal Case para Snake Case.
  /// </summary>
  /// <param name="value">A string em Pascal Case.</param>
  /// <returns>A string convertida para Snake Case.</returns>
  internal static string FromPascalToSnakeCase(this string? value)
  {
    // Verifica se a string é nula ou vazia
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    // Converte a string para Snake Case
    return string
            .Concat(value
            .Select((x, i) =>
              i > 0 && char.IsUpper(x) ?
                "_" + char.ToLowerInvariant(x) :
                x.ToString()))
            .ToLowerInvariant();
  }

  /// <summary>
  /// Converte uma string de Camel Case para Snake Case.
  /// </summary>
  /// <param name="value">A string em Camel Case.</param>
  /// <returns>A string convertida para Snake Case.</returns>
  internal static string FromCamelToSnakeCase(this string? value)
  {
    // Verifica se a string é nula ou vazia
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    // Converte a string para Snake Case
    return FromPascalToSnakeCase(value);
  }

  /// <summary>
  /// Converte uma string de Kebab Case para Snake Case.
  /// </summary>
  /// <param name="value">A string em Kebab Case.</param>
  /// <returns>A string convertida para Snake Case.</returns>
  internal static string FromKebabToSnakeCase(this string? value)
  {
    // Verifica se a string é nula ou vazia
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    // Converte a string para Kebab Case
    return string
            .Join('_', value.Split('-')
            .Where(word => !string.IsNullOrEmpty(word))
            .Select(word => word.ToLowerInvariant()));
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Adiciona um diacrítico transliterado ao slug.
  /// </summary>
  /// <param name="text">O texto a ser adicionado.</param>
  /// <param name="builder">O construtor de string do slug.</param>
  /// <param name="lastWasHyphen">Indica se o último caractere adicionado foi um hífen.</param>  ///
  private static void AppendDiacritics(string text, StringBuilder builder, ref bool lastWasHyphen)
  {
    // Adiciona cada caractere do texto
    foreach (var ch in text)
    {
      // Verifica se é letra ou número
      if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
      {
        builder.Append(ch);
        lastWasHyphen = false;
        continue;
      }

      // Espaços e caracteres especiais viram hífen, com compactação
      if (builder.Length > 0 && !lastWasHyphen)
      {
        builder.Append('-');
        lastWasHyphen = true;
      }
    }
  }

  #endregion
}
