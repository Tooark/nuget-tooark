using Tooark.Exceptions;
using Tooark.Storage.Dtos;
using Tooark.Storage.Enums;

namespace Tooark.Storage.Interfaces;

/// <summary>
/// Interface do serviço de gerenciamento de objetos em um storage de nuvem.
/// </summary>
/// <remarks>
/// O contrato não depende de provedor: a aplicação troca a AWS pelo Google Cloud mudando o registro, e não o código
/// que usa o serviço. Sem <c>bucket</c>, as operações usam o bucket das opções.
/// <para>
/// Falhas do provedor chegam como exceções do Tooark, com a original em <see cref="Exception.InnerException"/>:
/// objeto inexistente é <see cref="NotFoundException"/>; bucket inexistente, acesso negado ou falha do provedor são
/// <see cref="InternalServerErrorException"/>, porque indicam configuração, e não erro de quem chamou.
/// </para>
/// </remarks>
public interface IStorageService
{
  /// <summary>
  /// Envia um objeto ao storage, substituindo o que existir na mesma chave.
  /// </summary>
  /// <param name="key">A chave (nome) do objeto.</param>
  /// <param name="content">O conteúdo do objeto. O stream não é fechado pelo serviço.</param>
  /// <param name="contentType">O tipo do conteúdo (MIME). Opcional.</param>
  /// <param name="bucket">O bucket. Opcional: sem ele, vale o das opções.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Os dados do objeto enviado.</returns>
  /// <exception cref="BadRequestException">Quando a chave é inválida.</exception>
  /// <exception cref="InternalServerErrorException">Quando não há bucket ou o provedor falha.</exception>
  Task<StorageObjectDto> UploadAsync(
    string key,
    Stream content,
    string? contentType = null,
    string? bucket = null,
    CancellationToken cancellationToken = default
  );

  /// <summary>
  /// Baixa um objeto do storage.
  /// </summary>
  /// <param name="key">A chave (nome) do objeto.</param>
  /// <param name="bucket">O bucket. Opcional: sem ele, vale o das opções.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O conteúdo do objeto. Quem chama fecha o stream.</returns>
  /// <exception cref="BadRequestException">Quando a chave é inválida.</exception>
  /// <exception cref="NotFoundException">Quando o objeto não existe.</exception>
  /// <exception cref="InternalServerErrorException">Quando não há bucket ou o provedor falha.</exception>
  Task<Stream> DownloadAsync(string key, string? bucket = null, CancellationToken cancellationToken = default);

  /// <summary>
  /// Exclui um objeto do storage.
  /// </summary>
  /// <param name="key">A chave (nome) do objeto.</param>
  /// <param name="bucket">O bucket. Opcional: sem ele, vale o das opções.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Verdadeiro quando o objeto foi excluído; falso quando ele não existia.</returns>
  /// <exception cref="BadRequestException">Quando a chave é inválida.</exception>
  /// <exception cref="InternalServerErrorException">Quando não há bucket ou o provedor falha.</exception>
  Task<bool> DeleteAsync(string key, string? bucket = null, CancellationToken cancellationToken = default);

  /// <summary>
  /// Indica se um objeto existe.
  /// </summary>
  /// <param name="key">A chave (nome) do objeto.</param>
  /// <param name="bucket">O bucket. Opcional: sem ele, vale o das opções.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Verdadeiro quando o objeto existe.</returns>
  /// <exception cref="BadRequestException">Quando a chave é inválida.</exception>
  /// <exception cref="InternalServerErrorException">Quando não há bucket ou o provedor falha.</exception>
  Task<bool> ExistsAsync(string key, string? bucket = null, CancellationToken cancellationToken = default);

  /// <summary>
  /// Obtém os dados de um objeto, sem baixar o conteúdo.
  /// </summary>
  /// <param name="key">A chave (nome) do objeto.</param>
  /// <param name="bucket">O bucket. Opcional: sem ele, vale o das opções.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>Os dados do objeto, ou nulo quando ele não existe.</returns>
  /// <exception cref="BadRequestException">Quando a chave é inválida.</exception>
  /// <exception cref="InternalServerErrorException">Quando não há bucket ou o provedor falha.</exception>
  Task<StorageObjectDto?> GetInfoAsync(string key, string? bucket = null, CancellationToken cancellationToken = default);

  /// <summary>
  /// Gera uma URL assinada e temporária para ler ou escrever um objeto sem credencial.
  /// </summary>
  /// <remarks>
  /// A assinatura é calculada localmente, sem consultar o storage: gerar a URL não confere se o objeto existe.
  /// </remarks>
  /// <param name="key">A chave (nome) do objeto.</param>
  /// <param name="expiration">A validade da URL. Opcional: sem ela, vale a das opções. Máximo de 7 dias.</param>
  /// <param name="access">O acesso concedido. Padrão: leitura.</param>
  /// <param name="bucket">O bucket. Opcional: sem ele, vale o das opções.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>A URL assinada.</returns>
  /// <exception cref="BadRequestException">Quando a chave ou a validade é inválida.</exception>
  /// <exception cref="InternalServerErrorException">Quando não há bucket ou a credencial não assina URLs.</exception>
  Task<Uri> GetSignedUrlAsync(
    string key,
    TimeSpan? expiration = null,
    ESignedUrlAccess access = ESignedUrlAccess.Read,
    string? bucket = null,
    CancellationToken cancellationToken = default
  );
}
