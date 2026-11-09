using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Tooark.Exceptions;
using Tooark.Securities.Interfaces;
using Tooark.Securities.Options;

namespace Tooark.Securities;

/// <summary>
/// Serviço de hash de senha com PBKDF2-HMAC-SHA256, nativo do .NET, no formato PHC.
/// </summary>
/// <remarks>
/// Formato do hash: <c>$pbkdf2-sha256$i=&lt;iterações&gt;$&lt;sal&gt;$&lt;hash&gt;</c>, com sal e hash em Base64 sem
/// preenchimento. O prefixo identifica o algoritmo, então outro algoritmo pode conviver no mesmo campo no futuro.
/// </remarks>
public class PasswordHashService : IPasswordHashService
{
  #region Constants

  /// <summary>
  /// Identificador do algoritmo no formato PHC.
  /// </summary>
  public const string AlgorithmId = "pbkdf2-sha256";

  #endregion

  #region Private Properties

  /// <summary>
  /// Opções validadas do hash.
  /// </summary>
  private readonly PasswordHashOptions _options;

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor do serviço de hash de senha.
  /// </summary>
  /// <param name="options">Opções de configuração do hash.</param>
  /// <exception cref="InternalServerErrorException">Quando as opções não estão configuradas ou ficam abaixo do mínimo seguro.</exception>
  public PasswordHashService(IOptions<PasswordHashOptions> options)
  {
    // Valida se as opções foram configuradas corretamente
    _options = options.Value
      ?? throw new InternalServerErrorException("Options.NotConfigured");

    // Falha no startup, nunca com um hash fraco em produção
    _options.Validate();
  }

  #endregion

  #region Methods

  /// <inheritdoc/>
  public string Hash(string password)
  {
    // Senha ausente não tem o que proteger
    if (string.IsNullOrEmpty(password))
    {
      throw new BadRequestException("PasswordHash.PasswordNotProvided");
    }

    var salt = RandomNumberGenerator.GetBytes(_options.SaltSize);
    var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _options.Iterations, HashAlgorithmName.SHA256, _options.HashSize);

    return $"${AlgorithmId}$i={_options.Iterations.ToString(CultureInfo.InvariantCulture)}${Encode(salt)}${Encode(hash)}";
  }

  /// <inheritdoc/>
  public bool Verify(string? password, string? hash)
  {
    // Sem senha ou com um hash que não é deste serviço não há o que comparar
    if (string.IsNullOrEmpty(password) || !TryParse(hash, out var parsed))
    {
      return false;
    }

    var computed = Rfc2898DeriveBytes.Pbkdf2(password, parsed.Salt, parsed.Iterations, HashAlgorithmName.SHA256, parsed.Hash.Length);

    // A comparação em tempo constante impede inferir o hash pelo tempo de resposta
    return CryptographicOperations.FixedTimeEquals(computed, parsed.Hash);
  }

  /// <inheritdoc/>
  public bool NeedsRehash(string? hash)
  {
    // Um hash que não se lê também precisa ser refeito
    if (!TryParse(hash, out var parsed))
    {
      return true;
    }

    return parsed.Iterations != _options.Iterations
      || parsed.Salt.Length != _options.SaltSize
      || parsed.Hash.Length != _options.HashSize;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Codifica bytes em Base64 sem preenchimento, como o formato PHC pede.
  /// </summary>
  /// <param name="bytes">Bytes a codificar.</param>
  /// <returns>Texto em Base64 sem os sinais <c>=</c> finais.</returns>
  private static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=');

  /// <summary>
  /// Decodifica um Base64 sem preenchimento.
  /// </summary>
  /// <param name="text">Texto em Base64, com ou sem preenchimento.</param>
  /// <param name="bytes">Bytes decodificados.</param>
  /// <returns>Verdadeiro quando o texto é um Base64 válido e não vazio.</returns>
  private static bool TryDecode(string text, out byte[] bytes)
  {
    bytes = [];

    // O Base64 sem preenchimento nunca termina com um resto de 1 caractere
    var padding = (4 - (text.Length % 4)) % 4;

    if (text.Length == 0 || padding == 3)
    {
      return false;
    }

    try
    {
      bytes = Convert.FromBase64String(text + new string('=', padding));

      return bytes.Length > 0;
    }
    catch (FormatException)
    {
      return false;
    }
  }

  /// <summary>
  /// Lê os componentes de um hash no formato PHC deste serviço.
  /// </summary>
  /// <param name="hash">Hash guardado.</param>
  /// <param name="parsed">Iterações, sal e hash lidos.</param>
  /// <returns>Verdadeiro quando o hash tem o formato <c>$pbkdf2-sha256$i=N$sal$hash</c> com valores válidos.</returns>
  private static bool TryParse(string? hash, out (int Iterations, byte[] Salt, byte[] Hash) parsed)
  {
    parsed = default;

    // Sem hash não há o que ler
    if (string.IsNullOrWhiteSpace(hash))
    {
      return false;
    }

    // Formato: vazio, algoritmo, parâmetros, sal, hash
    var parts = hash.Split('$');

    if (parts.Length != 5 || parts[0].Length != 0 || parts[1] != AlgorithmId || !parts[2].StartsWith("i=", StringComparison.Ordinal))
    {
      return false;
    }

    // As iterações precisam ser um inteiro positivo
    if (!int.TryParse(parts[2][2..], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) || iterations <= 0)
    {
      return false;
    }

    // Sal e hash precisam ser Base64 válidos
    if (!TryDecode(parts[3], out var salt) || !TryDecode(parts[4], out var digest))
    {
      return false;
    }

    parsed = (iterations, salt, digest);

    return true;
  }

  #endregion
}
