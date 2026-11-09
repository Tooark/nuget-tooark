using System.Security.Cryptography;
using System.Text;

namespace Tooark.Securities;

/// <summary>
/// Tokens aleatórios de uso único ou de longa duração: redefinição de senha, confirmação de e-mail, chave de API.
/// </summary>
/// <remarks>
/// O token vai para o usuário e só o hash vai para o banco: um vazamento do banco não expõe tokens válidos.
/// O hash é SHA-256 sem sal e sem fator de custo de propósito: o token tem no mínimo 128 bits aleatórios,
/// então a força bruta é inviável mesmo com um hash rápido, e o hash determinístico permite buscar o registro pelo
/// próprio hash. Hash lento com sal é para senha, que tem pouca entropia (<see cref="Interfaces.IPasswordHashService"/>).
/// </remarks>
public static class SecureToken
{
  #region Constants

  /// <summary>
  /// Tamanho padrão do token em bytes (256 bits).
  /// </summary>
  public const int DefaultSize = 32;

  /// <summary>
  /// Menor tamanho aceito em bytes (128 bits).
  /// </summary>
  public const int MinimumSize = 16;

  /// <summary>
  /// Maior tamanho aceito em bytes.
  /// </summary>
  public const int MaximumSize = 1024;

  #endregion

  #region Methods

  /// <summary>
  /// Cria um token aleatório em Base64Url, pronto para ir em URL, cabeçalho ou corpo.
  /// </summary>
  /// <param name="sizeInBytes">Quantidade de bytes aleatórios, entre 16 e 1024. Padrão: 32 (256 bits).</param>
  /// <returns>Token em Base64Url sem preenchimento (43 caracteres para 32 bytes).</returns>
  /// <exception cref="ArgumentOutOfRangeException">Quando o tamanho está fora da faixa aceita.</exception>
  public static string Create(int sizeInBytes = DefaultSize)
  {
    // Tamanhos pequenos tornam o token adivinhável; tamanhos enormes não acrescentam segurança
    ArgumentOutOfRangeException.ThrowIfLessThan(sizeInBytes, MinimumSize);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeInBytes, MaximumSize);

    return Base64Url.Encode(RandomNumberGenerator.GetBytes(sizeInBytes));
  }

  /// <summary>
  /// Calcula o hash SHA-256 do token, o valor a ser guardado no banco.
  /// </summary>
  /// <param name="token">Token recebido de <see cref="Create"/> ou de <see cref="RecoveryCodes"/>.</param>
  /// <returns>Hash em Base64Url sem preenchimento (43 caracteres).</returns>
  /// <exception cref="ArgumentException">Quando o token é nulo, vazio ou só espaços.</exception>
  public static string Hash(string token)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(token);

    return Base64Url.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
  }

  /// <summary>
  /// Confere se o token corresponde ao hash guardado, em tempo constante.
  /// </summary>
  /// <param name="token">Token apresentado pelo usuário.</param>
  /// <param name="hash">Hash guardado, gerado por <see cref="Hash"/>.</param>
  /// <returns>Verdadeiro quando o token confere. Falso para token ou hash ausentes ou malformados; nunca lança.</returns>
  public static bool Verify(string? token, string? hash)
  {
    // Sem token ou sem hash não há o que comparar; um hash malformado nunca confere
    if (string.IsNullOrWhiteSpace(token) || !Base64Url.TryDecode(hash, out var expected))
    {
      return false;
    }

    var computed = SHA256.HashData(Encoding.UTF8.GetBytes(token));

    // A comparação em tempo constante impede inferir o hash pelo tempo de resposta
    return expected.Length == computed.Length && CryptographicOperations.FixedTimeEquals(expected, computed);
  }

  #endregion
}
