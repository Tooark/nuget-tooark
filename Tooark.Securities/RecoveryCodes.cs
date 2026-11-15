using System.Security.Cryptography;
using System.Text;

namespace Tooark.Securities;

/// <summary>
/// Códigos de recuperação para o segundo fator: o usuário usa um quando perde o aplicativo autenticador.
/// </summary>
/// <remarks>
/// Os códigos são exibidos uma única vez e guardados só como hash (<see cref="SecureToken.Hash"/>), um registro por
/// código. No uso, normalize o que o usuário digitou com <see cref="Normalize"/>, confira com
/// <see cref="SecureToken.Verify"/> e apague o registro: cada código vale uma vez.
/// </remarks>
public static class RecoveryCodes
{
  #region Constants

  /// <summary>
  /// Alfabeto dos códigos: maiúsculas e dígitos sem os caracteres ambíguos <c>0</c>, <c>O</c>, <c>1</c> e <c>I</c>.
  /// </summary>
  /// <remarks>São 32 símbolos, então cada byte aleatório mapeia um caractere sem viés.</remarks>
  public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

  /// <summary>
  /// Quantidade padrão de códigos gerados.
  /// </summary>
  public const int DefaultCount = 10;

  /// <summary>
  /// Quantidade padrão de caracteres por código, sem contar o separador.
  /// </summary>
  public const int DefaultLength = 10;

  /// <summary>
  /// Tamanho dos grupos separados por hífen na exibição.
  /// </summary>
  public const int GroupSize = 5;

  #endregion

  #region Methods

  /// <summary>
  /// Gera códigos de recuperação aleatórios, formatados em grupos de cinco caracteres separados por hífen.
  /// </summary>
  /// <param name="count">Quantidade de códigos, entre 1 e 50. Padrão: 10.</param>
  /// <param name="length">Caracteres por código, entre 8 e 32, sem contar os hífens. Padrão: 10 (<c>XXXXX-XXXXX</c>).</param>
  /// <returns>Os códigos, distintos entre si, na forma exibida ao usuário.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Quando a quantidade ou o tamanho estão fora da faixa.</exception>
  public static IReadOnlyList<string> Generate(int count = DefaultCount, int length = DefaultLength)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 50);
    ArgumentOutOfRangeException.ThrowIfLessThan(length, 8);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 32);

    var codes = new HashSet<string>(StringComparer.Ordinal);

    // Repete até ter a quantidade pedida de códigos distintos (colisão é improvável, mas um código repetido
    // valeria duas vezes)
    while (codes.Count < count)
    {
      codes.Add(Format(CreateCode(length)));
    }

    return [.. codes];
  }

  /// <summary>
  /// Normaliza um código digitado pelo usuário para o hash e a comparação: maiúsculas, sem hífens nem espaços.
  /// </summary>
  /// <param name="code">Código digitado, em qualquer caixa, com ou sem separadores.</param>
  /// <returns>O código normalizado, ou vazio quando ausente.</returns>
  /// <remarks>
  /// Guarde <c>SecureToken.Hash(RecoveryCodes.Normalize(code))</c> e confira com
  /// <c>SecureToken.Verify(RecoveryCodes.Normalize(digitado), hash)</c>.
  /// </remarks>
  public static string Normalize(string? code)
  {
    // Sem código não há o que normalizar
    if (string.IsNullOrWhiteSpace(code))
    {
      return string.Empty;
    }

    var builder = new StringBuilder(code.Length);

    foreach (var character in code)
    {
      // Separadores de exibição saem; o restante vai em maiúsculas
      if (character is not ('-' or ' '))
      {
        builder.Append(char.ToUpperInvariant(character));
      }
    }

    return builder.ToString();
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Sorteia os caracteres de um código.
  /// </summary>
  /// <param name="length">Quantidade de caracteres.</param>
  /// <returns>O código sem separadores.</returns>
  private static string CreateCode(int length)
  {
    var random = RandomNumberGenerator.GetBytes(length);
    var characters = new char[length];

    // O alfabeto tem 32 símbolos: os 5 bits baixos de cada byte escolhem um sem viés
    for (var index = 0; index < length; index++)
    {
      characters[index] = Alphabet[random[index] & 0x1F];
    }

    return new string(characters);
  }

  /// <summary>
  /// Separa o código em grupos de cinco caracteres com hífen.
  /// </summary>
  /// <param name="code">Código sem separadores.</param>
  /// <returns>O código formatado para exibição.</returns>
  private static string Format(string code)
  {
    var builder = new StringBuilder(code.Length + code.Length / GroupSize);

    for (var index = 0; index < code.Length; index++)
    {
      // Um hífen antes de cada grupo, exceto o primeiro
      if (index > 0 && index % GroupSize == 0)
      {
        builder.Append('-');
      }

      builder.Append(code[index]);
    }

    return builder.ToString();
  }

  #endregion
}
