using Tooark.Exceptions;

namespace Tooark.Storage.Options;

/// <summary>
/// Classe que representa as opções comuns aos provedores de storage.
/// </summary>
/// <remarks>
/// Cada provedor estende estas opções com as próprias (região, credenciais) e as lê da mesma seção
/// <c>Storage</c>: a aplicação usa um provedor por vez.
/// </remarks>
public class StorageOptions
{
  #region Section

  /// <summary>
  /// Seção de configuração do storage.
  /// </summary>
  public const string Section = "Storage";

  #endregion

  #region Constants

  /// <summary>
  /// Validade máxima de uma URL assinada, em minutos: 7 dias, o limite da assinatura V4 na AWS e no Google Cloud.
  /// </summary>
  public const int MaxSignedUrlExpirationMinutes = 7 * 24 * 60;

  #endregion

  #region Properties

  /// <summary>
  /// Bucket padrão das operações. Cada operação pode informar outro.
  /// </summary>
  public string? Bucket { get; set; }

  /// <summary>
  /// Validade padrão de uma URL assinada, em minutos. Padrão: 5.
  /// </summary>
  public int SignedUrlExpirationMinutes { get; set; } = 5;

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções.
  /// </summary>
  /// <exception cref="InternalServerErrorException">Quando a validade da URL assinada está fora do intervalo aceito.</exception>
  public virtual void Validate()
  {
    if (SignedUrlExpirationMinutes is < 1 or > MaxSignedUrlExpirationMinutes)
    {
      throw new InternalServerErrorException($"Options.Storage.SignedUrlExpirationInvalid;{MaxSignedUrlExpirationMinutes}");
    }
  }

  #endregion
}
