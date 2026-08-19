using Tooark.Exceptions;

namespace Tooark.Securities.Interfaces;

/// <summary>
/// Interface para o serviço de criptografia.
/// </summary>
public interface ICryptographyService
{
  /// <summary>
  /// Criptografa um texto usando o algoritmo configurado.
  /// </summary>
  /// <remarks>
  /// Suporta os algoritmos:
  /// - AES-256-CBC (Opção "CBC")
  /// - AES-256-GCM (Opção "GCM", padrão)
  /// </remarks>
  /// <param name="plainText">Texto plano para criptografar.</param>
  /// <returns>Texto criptografado em Base64.</returns>
  /// <exception cref="BadRequestException">Quando o texto plano não é fornecido.</exception>
  string Encrypt(string plainText);

  /// <summary>
  /// Descriptografa um texto usando o algoritmo configurado.
  /// </summary>
  /// <remarks>
  /// Suporta os algoritmos:
  /// - AES-256-CBC (Opção "CBC")
  /// - AES-256-GCM (Opção "GCM", padrão)
  /// </remarks>
  /// <param name="cipherText">Texto criptografado em Base64.</param>
  /// <returns>Texto plano descriptografado.</returns>
  /// <exception cref="BadRequestException">Quando o texto criptografado não é fornecido.</exception>
  /// <exception cref="BadRequestException">Quando o texto criptografado é inválido.</exception>
  string Decrypt(string cipherText);
}
