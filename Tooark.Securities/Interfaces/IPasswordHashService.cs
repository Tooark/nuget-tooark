using Tooark.Exceptions;

namespace Tooark.Securities.Interfaces;

/// <summary>
/// Interface para o serviço de hash de senha.
/// </summary>
/// <remarks>
/// O hash guarda o algoritmo e os parâmetros no próprio texto (formato PHC), então o serviço continua
/// validando hashes antigos depois de uma mudança de parâmetros e aponta, por <see cref="NeedsRehash"/>,
/// quais devem ser refeitos.
/// </remarks>
public interface IPasswordHashService
{
  /// <summary>
  /// Calcula o hash de uma senha com sal aleatório.
  /// </summary>
  /// <param name="password">Senha em texto claro.</param>
  /// <returns>O hash no formato PHC, como <c>$pbkdf2-sha256$i=600000$&lt;sal&gt;$&lt;hash&gt;</c>, a ser guardado no banco.</returns>
  /// <exception cref="BadRequestException">Quando a senha é nula ou vazia.</exception>
  string Hash(string password);

  /// <summary>
  /// Confere se a senha corresponde ao hash guardado, em tempo constante.
  /// </summary>
  /// <param name="password">Senha em texto claro apresentada pelo usuário.</param>
  /// <param name="hash">Hash guardado, gerado por <see cref="Hash"/>.</param>
  /// <returns>Verdadeiro quando a senha confere. Falso para senha ou hash ausentes ou malformados; nunca lança.</returns>
  bool Verify(string? password, string? hash);

  /// <summary>
  /// Indica se o hash foi gerado com parâmetros diferentes dos atuais e deve ser refeito.
  /// </summary>
  /// <param name="hash">Hash guardado.</param>
  /// <returns>Verdadeiro quando o algoritmo, as iterações ou os tamanhos divergem das opções atuais, ou o hash é malformado.</returns>
  /// <remarks>Chame depois de um <see cref="Verify"/> verdadeiro e, quando devolver verdadeiro, grave <c>Hash(senha)</c> no lugar.</remarks>
  bool NeedsRehash(string? hash);
}
