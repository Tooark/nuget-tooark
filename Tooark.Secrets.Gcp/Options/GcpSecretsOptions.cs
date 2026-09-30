using Tooark.Exceptions;
using Tooark.Secrets.Options;

namespace Tooark.Secrets.Gcp.Options;

/// <summary>
/// Classe que representa as opções dos segredos no Google Cloud (Secret Manager e Parameter Manager).
/// </summary>
/// <remarks>
/// Lidas da seção <c>Secrets</c>, junto com as opções comuns. A credencial vem sempre do Application Default
/// Credentials: a conta de serviço do Cloud Run, do GKE ou do Compute Engine, ou a variável
/// <c>GOOGLE_APPLICATION_CREDENTIALS</c>. A chave de acesso ao cofre não pode vir do próprio cofre.
/// </remarks>
public class GcpSecretsOptions : SecretsOptions
{
  #region Properties

  /// <summary>
  /// Projeto do Google Cloud onde ficam os segredos e os parâmetros. Obrigatório.
  /// </summary>
  public string? ProjectId { get; set; }

  /// <summary>
  /// Prefixo do nome dos segredos do Secret Manager que viram configuração, como <c>arkuest-prod</c>. Opcional.
  /// </summary>
  /// <remarks>
  /// O nome de um segredo no Google aceita só letras, dígitos, <c>_</c> e <c>-</c>, então os níveis usam
  /// <c>__</c>: o segredo <c>arkuest-prod__Storage__CredentialsJson</c> vira a chave
  /// <c>Storage:CredentialsJson</c>. Vale a versão <c>latest</c>.
  /// </remarks>
  public string? SecretsPrefix { get; set; }

  /// <summary>
  /// Prefixo do nome dos parâmetros do Parameter Manager (local <c>global</c>) que viram configuração. Opcional.
  /// </summary>
  /// <remarks>
  /// Vale a versão habilitada mais recente de cada parâmetro, já renderizada: as referências a segredos do Secret
  /// Manager chegam resolvidas. Com o mesmo nome, o segredo vence o parâmetro.
  /// </remarks>
  public string? ParametersPrefix { get; set; }

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções.
  /// </summary>
  /// <exception cref="InternalServerErrorException">
  /// Quando o tempo limite ou o cache estão fora do intervalo, ou o projeto não foi informado.
  /// </exception>
  public override void Validate()
  {
    base.Validate();

    if (string.IsNullOrWhiteSpace(ProjectId))
    {
      throw new InternalServerErrorException("Options.Secrets.Gcp.ProjectIdNotConfigured");
    }
  }

  #endregion
}
