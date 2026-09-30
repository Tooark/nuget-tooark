using Tooark.Exceptions;

namespace Tooark.Secrets.Interfaces;

/// <summary>
/// Interface do serviço que lê segredos do cofre em tempo de execução.
/// </summary>
/// <remarks>
/// Para configuração lida no startup, prefira a fonte de configuração do provedor, que coloca os segredos no
/// <c>IConfiguration</c>. Este serviço atende o que a aplicação só conhece em execução, como a chave de um tenant.
/// Os valores lidos ficam em cache pelo tempo das opções.
/// </remarks>
public interface ISecretService
{
  /// <summary>
  /// Lê o valor atual de um segredo.
  /// </summary>
  /// <param name="name">O nome do segredo no cofre (no Vault, o caminho no KV).</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O valor do segredo, ou nulo quando ele não existe.</returns>
  /// <exception cref="BadRequestException">Quando o nome está em branco.</exception>
  /// <exception cref="InternalServerErrorException">Quando o acesso é negado ou o cofre falha.</exception>
  Task<string?> GetAsync(string name, CancellationToken cancellationToken = default);

  /// <summary>
  /// Lê um campo de um segredo guardado como objeto JSON, como os pares chave e valor do AWS Secrets Manager e do
  /// Vault.
  /// </summary>
  /// <param name="name">O nome do segredo no cofre.</param>
  /// <param name="field">O nome do campo, com a caixa exata.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O valor do campo, ou nulo quando o segredo ou o campo não existe.</returns>
  /// <exception cref="BadRequestException">Quando o nome ou o campo está em branco.</exception>
  /// <exception cref="InternalServerErrorException">
  /// Quando o segredo não é um objeto JSON, o acesso é negado ou o cofre falha.
  /// </exception>
  Task<string?> GetFieldAsync(string name, string field, CancellationToken cancellationToken = default);
}
