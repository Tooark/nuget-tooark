using Tooark.Exceptions;

namespace Tooark.Securities.Interfaces;

/// <summary>
/// Interface para o serviço de criptografia com várias chaves identificadas, que permite rotacionar a chave sem
/// recifrar tudo de uma vez.
/// </summary>
/// <remarks>
/// Os valores cifrados carregam o identificador da chave no formato <c>tk1.&lt;id&gt;.&lt;payload&gt;</c>:
/// <c>tk1</c> é a versão do formato, <c>id</c> é a chave usada e <c>payload</c> é o texto cifrado em Base64.
/// Valores sem prefixo, gravados antes da adoção das chaves identificadas, continuam abrindo com a chave legada
/// (<c>SecretBase64</c>). <see cref="ICryptographyService.Encrypt"/> cifra com a chave corrente
/// e <see cref="ICryptographyService.Decrypt"/> abre com a chave indicada no prefixo.
/// </remarks>
public interface IKeyedCryptographyService : ICryptographyService
{
  /// <summary>
  /// Identificador da chave corrente, usada por <see cref="ICryptographyService.Encrypt"/>.
  /// </summary>
  string CurrentKeyId { get; }

  /// <summary>
  /// Identificadores de todas as chaves configuradas, na ordem da configuração.
  /// </summary>
  IReadOnlyCollection<string> KeyIds { get; }

  /// <summary>
  /// Criptografa um texto com uma chave específica.
  /// </summary>
  /// <param name="plainText">Texto plano para criptografar.</param>
  /// <param name="keyId">Identificador de uma das chaves configuradas.</param>
  /// <returns>Texto criptografado no formato <c>tk1.&lt;id&gt;.&lt;payload&gt;</c>.</returns>
  /// <exception cref="BadRequestException">Quando o texto não é fornecido ou a chave não está configurada.</exception>
  string Encrypt(string plainText, string keyId);

  /// <summary>
  /// Lê o identificador da chave que cifrou um valor.
  /// </summary>
  /// <param name="cipherText">Texto criptografado.</param>
  /// <returns>O identificador do prefixo, ou nulo para um valor legado, sem prefixo.</returns>
  /// <exception cref="BadRequestException">Quando o texto não é fornecido ou tem um prefixo malformado.</exception>
  string? KeyIdOf(string cipherText);

  /// <summary>
  /// Indica se o valor foi cifrado com a chave corrente.
  /// </summary>
  /// <param name="cipherText">Texto criptografado.</param>
  /// <returns>Verdadeiro quando o prefixo aponta a chave corrente; falso para outra chave ou para um valor legado.</returns>
  /// <exception cref="BadRequestException">Quando o texto não é fornecido ou tem um prefixo malformado.</exception>
  bool IsCurrent(string cipherText);

  /// <summary>
  /// Recifra um valor com a chave corrente: decifra com a chave que o gravou e cifra de novo.
  /// </summary>
  /// <param name="cipherText">Texto criptografado com qualquer chave configurada, ou legado.</param>
  /// <returns>O texto criptografado com a chave corrente, ou o próprio valor quando ele já está na chave corrente.</returns>
  /// <exception cref="BadRequestException">Quando o texto não é fornecido, é inválido ou sua chave não está configurada.</exception>
  string ReEncrypt(string cipherText);
}
