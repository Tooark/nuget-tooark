using System.Text;

namespace Tooark.Securities;

/// <summary>
/// Codificação Base32 (RFC 4648, seção 6) sem preenchimento, o formato dos segredos TOTP.
/// </summary>
/// <remarks>
/// O .NET não traz um codificador Base32. Os aplicativos autenticadores leem o segredo neste alfabeto
/// (<c>A-Z</c> e <c>2-7</c>), então o pacote implementa a codificação em vez de depender de biblioteca externa.
/// </remarks>
internal static class Base32
{
  /// <summary>
  /// Alfabeto do Base32 (RFC 4648).
  /// </summary>
  private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

  /// <summary>
  /// Codifica os bytes em Base32 sem preenchimento.
  /// </summary>
  /// <param name="bytes">Bytes a codificar.</param>
  /// <returns>Texto em Base32, em maiúsculas, sem os sinais <c>=</c> finais.</returns>
  internal static string Encode(ReadOnlySpan<byte> bytes)
  {
    // Sem bytes não há o que codificar
    if (bytes.IsEmpty)
    {
      return string.Empty;
    }

    var result = new StringBuilder((bytes.Length * 8 + 4) / 5);
    var buffer = 0;
    var bitsInBuffer = 0;

    // Acumula os bits e emite um caractere a cada 5 bits completos
    foreach (var value in bytes)
    {
      buffer = (buffer << 8) | value;
      bitsInBuffer += 8;

      while (bitsInBuffer >= 5)
      {
        bitsInBuffer -= 5;
        result.Append(Alphabet[(buffer >> bitsInBuffer) & 0x1F]);
      }
    }

    // Os bits restantes completam o último caractere com zeros à direita
    if (bitsInBuffer > 0)
    {
      result.Append(Alphabet[(buffer << (5 - bitsInBuffer)) & 0x1F]);
    }

    return result.ToString();
  }

  /// <summary>
  /// Tenta decodificar um texto em Base32.
  /// </summary>
  /// <param name="text">Texto em Base32, em qualquer caixa, com ou sem preenchimento, espaços ou hífens.</param>
  /// <param name="bytes">Bytes decodificados, ou um vetor vazio quando o texto é inválido.</param>
  /// <returns>Verdadeiro quando o texto é um Base32 válido e não vazio.</returns>
  /// <remarks>
  /// Espaços e hífens são ignorados porque os aplicativos exibem o segredo em grupos para facilitar a digitação.
  /// </remarks>
  internal static bool TryDecode(string? text, out byte[] bytes)
  {
    bytes = [];

    // Texto ausente não tem o que decodificar
    if (string.IsNullOrWhiteSpace(text))
    {
      return false;
    }

    var buffer = 0;
    var bitsInBuffer = 0;
    var output = new List<byte>(text.Length * 5 / 8);

    foreach (var character in text)
    {
      // Separadores de exibição e preenchimento não carregam dados
      if (character is ' ' or '-' or '=')
      {
        continue;
      }

      var index = Alphabet.IndexOf(char.ToUpperInvariant(character));

      // Qualquer caractere fora do alfabeto invalida o texto
      if (index < 0)
      {
        return false;
      }

      buffer = (buffer << 5) | index;
      bitsInBuffer += 5;

      // Emite um byte a cada 8 bits completos
      if (bitsInBuffer >= 8)
      {
        bitsInBuffer -= 8;
        output.Add((byte)((buffer >> bitsInBuffer) & 0xFF));
      }
    }

    // Só separadores e preenchimento não formam um segredo
    if (output.Count == 0)
    {
      return false;
    }

    bytes = [.. output];

    return true;
  }
}
