using Tooark.Exceptions;

namespace Tooark.Securities.Options;

/// <summary>
/// Classe que representa as opções de configuração do hash de senha.
/// </summary>
/// <remarks>
/// Todas as opções têm padrão seguro: a seção é opcional. Os mínimos existem porque um valor baixo demais
/// tornaria o hash rápido de atacar; a configuração abaixo deles falha no startup.
/// </remarks>
public class PasswordHashOptions
{
  #region Section

  /// <summary>
  /// Seção de configuração do hash de senha.
  /// </summary>
  public const string Section = "PasswordHash";

  #endregion

  #region Constants

  /// <summary>
  /// Iterações padrão do PBKDF2-HMAC-SHA256, a recomendação atual da OWASP.
  /// </summary>
  public const int DefaultIterations = 600_000;

  /// <summary>
  /// Menor quantidade de iterações aceita.
  /// </summary>
  public const int MinimumIterations = 100_000;

  /// <summary>
  /// Tamanho padrão e mínimo do salt em bytes (128 bits).
  /// </summary>
  public const int DefaultSaltSize = 16;

  /// <summary>
  /// Tamanho padrão e mínimo do hash em bytes (256 bits, a saída do SHA-256).
  /// </summary>
  public const int DefaultHashSize = 32;

  #endregion

  #region Properties

  /// <summary>
  /// Iterações do PBKDF2. Padrão: 600.000. Mínimo: 100.000.
  /// </summary>
  /// <remarks>
  /// Mais iterações encarecem cada tentativa do atacante e cada login legítimo na mesma proporção. Ao aumentar,
  /// os hashes antigos passam a responder verdadeiro em <see cref="Interfaces.IPasswordHashService.NeedsRehash"/>
  /// e são refeitos no login seguinte.
  /// </remarks>
  public int Iterations { get; set; } = DefaultIterations;

  /// <summary>
  /// Tamanho do salt em bytes. Padrão e mínimo: 16.
  /// </summary>
  public int SaltSize { get; set; } = DefaultSaltSize;

  /// <summary>
  /// Tamanho do hash derivado em bytes. Padrão e mínimo: 32.
  /// </summary>
  public int HashSize { get; set; } = DefaultHashSize;

  #endregion

  #region Methods

  /// <summary>
  /// Valida as opções, falhando no startup quando algum valor fica abaixo do mínimo seguro.
  /// </summary>
  /// <exception cref="InternalServerErrorException">Quando as iterações, o salt ou o hash ficam abaixo do mínimo.</exception>
  public void Validate()
  {
    // Nenhum dos mínimos é substituído em silêncio: configuração fraca é erro de configuração
    if (Iterations < MinimumIterations)
    {
      throw new InternalServerErrorException($"Options.PasswordHash.IterationsTooLow;{MinimumIterations}");
    }

    if (SaltSize < DefaultSaltSize)
    {
      throw new InternalServerErrorException($"Options.PasswordHash.SaltSizeTooSmall;{DefaultSaltSize}");
    }

    if (HashSize < DefaultHashSize)
    {
      throw new InternalServerErrorException($"Options.PasswordHash.HashSizeTooSmall;{DefaultHashSize}");
    }
  }

  #endregion
}
