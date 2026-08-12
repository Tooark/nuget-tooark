using Microsoft.IdentityModel.Tokens;
using Tooark.Exceptions;

namespace Tooark.Securities.Options;

/// <summary>
/// Classe que representa as opções de configuração do JWT.
/// </summary>
public class JwtOptions
{
  #region Section

  /// <summary>
  /// Seção de configuração do JWT.
  /// </summary>
  public const string Section = "Jwt";

  #endregion

  #region Private Properties

  /// <summary>
  /// Algoritmo de assinatura do token JWT. Padrão: ES256.
  /// </summary>
  private string _algorithm = SecurityAlgorithms.EcdsaSha256;

  /// <summary>
  /// Chave private usada para assinar o token JWT.
  /// </summary>
  private string _privateKey = null!;

  /// <summary>
  /// Chave pública usada para verificar a assinatura do token JWT.
  /// </summary>
  private string _publicKey = null!;

  #endregion

  #region Properties

  /// <summary>
  /// Algoritmo de assinatura do token JWT. Padrão: "ES256".
  /// </summary>
  /// <remarks>
  /// Valores válidos (case insensitive):
  ///   - "HS256" que representa um algoritmo HmacSha256
  ///   - "HS384" que representa um algoritmo HmacSha384
  ///   - "HS512" que representa um algoritmo HmacSha512
  ///   - "RS256" que representa um algoritmo RsaSha256
  ///   - "RS384" que representa um algoritmo RsaSha384
  ///   - "RS512" que representa um algoritmo RsaSha512
  ///   - "PS256" que representa um algoritmo RsaSsaPssSha256
  ///   - "PS384" que representa um algoritmo RsaSsaPssSha384
  ///   - "PS512" que representa um algoritmo RsaSsaPssSha512
  ///   - "ES256" que representa um algoritmo EcdsaSha256
  ///   - "ES384" que representa um algoritmo EcdsaSha384
  ///   - "ES512" que representa um algoritmo EcdsaSha512
  /// Valores desconhecidos lançam exceção: em configuração de segurança,
  /// um algoritmo inválido nunca deve ser substituído silenciosamente por outro.
  /// </remarks>
  /// <exception cref="InternalServerErrorException">Quando o valor não corresponde a um algoritmo suportado.</exception>
  public string Algorithm
  {
    get => _algorithm;
    set => _algorithm = value?.ToUpperInvariant() switch
    {
      "HS256" => SecurityAlgorithms.HmacSha256,
      "HS384" => SecurityAlgorithms.HmacSha384,
      "HS512" => SecurityAlgorithms.HmacSha512,
      "RS256" => SecurityAlgorithms.RsaSha256,
      "RS384" => SecurityAlgorithms.RsaSha384,
      "RS512" => SecurityAlgorithms.RsaSha512,
      "PS256" => SecurityAlgorithms.RsaSsaPssSha256,
      "PS384" => SecurityAlgorithms.RsaSsaPssSha384,
      "PS512" => SecurityAlgorithms.RsaSsaPssSha512,
      "ES256" => SecurityAlgorithms.EcdsaSha256,
      "ES384" => SecurityAlgorithms.EcdsaSha384,
      "ES512" => SecurityAlgorithms.EcdsaSha512,
      _ => throw new InternalServerErrorException($"Options.Jwt.AlgorithmNotSupported;{value}")
    };
  }

  /// <summary>
  /// Chave secreta para algoritmos simétricos (ex: HS256, HS384, HS512).
  /// </summary>
  public string? Secret { get; set; } = null;

  /// <summary>
  /// Chave privada para algoritmos assimétricos (ex: RS256, RS384, RS512, RSASS256, RSASS384, RSASS512).
  /// </summary>
  public string? PrivateKey
  {
    get => _privateKey;
    set => _privateKey = value?
      .Replace("-----BEGIN PRIVATE KEY-----", "")
      .Replace("-----END PRIVATE KEY-----", "")
      .Replace("\n", "")
      .Replace("\r", "")
      .Trim() ?? "";
  }

  /// <summary>
  /// Chave pública para algoritmos assimétricos (ex: RS256, RS384, RS512, RSASS256, RSASS384, RSASS512).
  /// </summary>
  public string? PublicKey
  {
    get => _publicKey;
    set => _publicKey = value?
      .Replace("-----BEGIN PUBLIC KEY-----", "")
      .Replace("-----END PUBLIC KEY-----", "")
      .Replace("\n", "")
      .Replace("\r", "")
      .Trim()
      ?? "";
  }

  /// <summary>
  /// Emissor do token JWT.
  /// </summary>
  public string? Issuer { get; set; } = null;

  /// <summary>
  /// Destinatário do token JWT.
  /// </summary>
  public string? Audience { get; set; } = null;

  /// <summary>
  /// Lista de emissores do token JWT.
  /// </summary>
  public string[]? Issuers { get; set; } = null;

  /// <summary>
  /// Lista de destinatários do token JWT.
  /// </summary>
  public string[]? Audiences { get; set; } = null;

  /// <summary>
  /// Tempo de expiração do token JWT em minutos.
  /// </summary>
  public int ExpirationTime { get; set; } = 5;

  #endregion
}
