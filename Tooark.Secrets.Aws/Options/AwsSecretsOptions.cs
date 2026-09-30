using Tooark.Exceptions;
using Tooark.Secrets.Options;

namespace Tooark.Secrets.Aws.Options;

/// <summary>
/// Classe que representa as opções dos segredos na AWS (Secrets Manager e Parameter Store).
/// </summary>
/// <remarks>
/// Lidas da seção <c>Secrets</c>, junto com as opções comuns. As credenciais vêm sempre da cadeia padrão da AWS:
/// variáveis de ambiente, perfil, role da tarefa no ECS, do pod no EKS ou da instância no EC2. A chave de acesso ao
/// cofre não pode vir do próprio cofre.
/// </remarks>
public class AwsSecretsOptions : SecretsOptions
{
  #region Properties

  /// <summary>
  /// Região do cofre, como <c>sa-east-1</c>. Opcional: sem ela, vale a região da cadeia padrão da AWS.
  /// </summary>
  public string? Region { get; set; }

  /// <summary>
  /// Endereço de um serviço compatível, como o LocalStack. Opcional.
  /// </summary>
  public string? ServiceUrl { get; set; }

  /// <summary>
  /// Prefixo do nome dos segredos do Secrets Manager que viram configuração, como <c>arkuest/prod</c>. Opcional.
  /// </summary>
  /// <remarks>
  /// O segredo <c>arkuest/prod/Storage/SecretKey</c> vira a chave <c>Storage:SecretKey</c>. O segredo com o nome
  /// exato do prefixo fica na raiz e só é útil com <see cref="SecretsOptions.ExpandJson"/>.
  /// </remarks>
  public string? SecretsPrefix { get; set; }

  /// <summary>
  /// Caminho dos parâmetros do Parameter Store que viram configuração, como <c>/arkuest/prod</c>. Opcional.
  /// </summary>
  /// <remarks>
  /// O parâmetro <c>/arkuest/prod/Storage/Bucket</c> vira a chave <c>Storage:Bucket</c>. Parâmetros
  /// <c>SecureString</c> chegam descriptografados. Com o mesmo nome, o segredo vence o parâmetro.
  /// </remarks>
  public string? ParametersPath { get; set; }

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções.
  /// </summary>
  /// <exception cref="InternalServerErrorException">
  /// Quando o tempo limite ou o cache estão fora do intervalo, ou o endereço do serviço não é uma URL http(s)
  /// absoluta.
  /// </exception>
  public override void Validate()
  {
    base.Validate();

    if (!string.IsNullOrWhiteSpace(ServiceUrl) &&
        !(Uri.TryCreate(ServiceUrl, UriKind.Absolute, out var url) && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp)))
    {
      throw new InternalServerErrorException($"Options.Secrets.Aws.ServiceUrlInvalid;{ServiceUrl}");
    }
  }

  #endregion
}
