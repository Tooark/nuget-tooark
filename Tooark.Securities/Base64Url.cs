namespace Tooark.Securities;

/// <summary>
/// Codificação Base64Url (RFC 4648, seção 5) sem preenchimento, segura para URL, cabeçalho e banco.
/// </summary>
/// <remarks>
/// O .NET só traz um codificador Base64Url nativo a partir do .NET 9. Este helper cobre os dois TFMs do pacote
/// sem dependência extra: usa o Base64 padrão e troca os caracteres <c>+</c> e <c>/</c> por <c>-</c> e <c>_</c>.
/// </remarks>
internal static class Base64Url
{
  /// <summary>
  /// Codifica os bytes em Base64Url sem preenchimento.
  /// </summary>
  /// <param name="bytes">Bytes a codificar.</param>
  /// <returns>Texto em Base64Url, sem os sinais <c>=</c> finais.</returns>
  internal static string Encode(ReadOnlySpan<byte> bytes) =>
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

  /// <summary>
  /// Tenta decodificar um texto em Base64Url.
  /// </summary>
  /// <param name="text">Texto em Base64Url, com ou sem preenchimento.</param>
  /// <param name="bytes">Bytes decodificados, ou um vetor vazio quando o texto é inválido.</param>
  /// <returns>Verdadeiro quando o texto é um Base64Url válido.</returns>
  internal static bool TryDecode(string? text, out byte[] bytes)
  {
    bytes = [];

    // Texto ausente não tem o que decodificar
    if (string.IsNullOrWhiteSpace(text))
    {
      return false;
    }

    // Volta ao alfabeto do Base64 padrão e repõe o preenchimento
    var standard = text.Trim().TrimEnd('=').Replace('-', '+').Replace('_', '/');
    var padding = (4 - (standard.Length % 4)) % 4;

    // Um resto de 1 caractere nunca é um Base64 válido
    if (padding == 3)
    {
      return false;
    }

    try
    {
      bytes = Convert.FromBase64String(standard + new string('=', padding));

      return true;
    }
    catch (FormatException)
    {
      return false;
    }
  }
}
