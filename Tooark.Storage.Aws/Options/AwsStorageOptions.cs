using Tooark.Exceptions;
using Tooark.Storage.Options;

namespace Tooark.Storage.Aws.Options;

/// <summary>
/// Classe que representa as opções do storage na AWS (Amazon S3 e serviços compatíveis).
/// </summary>
/// <remarks>
/// Lidas da seção <c>Storage</c>, junto com as opções comuns. Sem <see cref="AccessKey"/> e
/// <see cref="SecretKey"/>, as credenciais vêm da cadeia padrão da AWS: variáveis de ambiente, perfil, role da
/// instância ou do contêiner. É o caminho recomendado em produção, porque não deixa chave na configuração.
/// </remarks>
public class AwsStorageOptions : StorageOptions
{
  #region Properties

  /// <summary>
  /// Região do bucket, como <c>sa-east-1</c>. Opcional: sem ela, vale a região da cadeia padrão da AWS.
  /// </summary>
  /// <remarks>
  /// Com <see cref="ServiceUrl"/>, o endereço define o destino e a região vale só para a assinatura das requisições.
  /// </remarks>
  public string? Region { get; set; }

  /// <summary>
  /// Chave de acesso. Informe junto com <see cref="SecretKey"/>, ou nenhuma das duas.
  /// </summary>
  public string? AccessKey { get; set; }

  /// <summary>
  /// Chave secreta. Informe junto com <see cref="AccessKey"/>, ou nenhuma das duas.
  /// </summary>
  public string? SecretKey { get; set; }

  /// <summary>
  /// Token de sessão, para credenciais temporárias. Opcional, e só vale com as duas chaves.
  /// </summary>
  public string? SessionToken { get; set; }

  /// <summary>
  /// Endereço de um serviço compatível com o S3, como MinIO, LocalStack ou Cloudflare R2. Opcional.
  /// </summary>
  public string? ServiceUrl { get; set; }

  /// <summary>
  /// Indica se o bucket vai no caminho da URL (<c>host/bucket/chave</c>) em vez do subdomínio. Padrão: falso.
  /// </summary>
  /// <remarks>
  /// Serviços compatíveis, como o MinIO, costumam exigir esta forma.
  /// </remarks>
  public bool ForcePathStyle { get; set; }

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções.
  /// </summary>
  /// <exception cref="InternalServerErrorException">
  /// Quando só uma das chaves foi informada, quando o token de sessão vem sem as chaves ou quando o endereço do
  /// serviço não é uma URL http(s) absoluta.
  /// </exception>
  public override void Validate()
  {
    base.Validate();

    // Uma chave sem a outra não autentica, e cair na cadeia padrão em silêncio usaria outra identidade
    var hasAccessKey = !string.IsNullOrWhiteSpace(AccessKey);
    var hasSecretKey = !string.IsNullOrWhiteSpace(SecretKey);

    if (hasAccessKey != hasSecretKey || (!hasAccessKey && !string.IsNullOrWhiteSpace(SessionToken)))
    {
      throw new InternalServerErrorException("Options.Storage.Aws.CredentialsIncomplete");
    }

    if (!string.IsNullOrWhiteSpace(ServiceUrl) &&
        !(Uri.TryCreate(ServiceUrl, UriKind.Absolute, out var url) && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp)))
    {
      throw new InternalServerErrorException($"Options.Storage.Aws.ServiceUrlInvalid;{ServiceUrl}");
    }
  }

  #endregion
}
