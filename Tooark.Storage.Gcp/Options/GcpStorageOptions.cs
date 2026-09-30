using Tooark.Exceptions;
using Tooark.Storage.Options;

namespace Tooark.Storage.Gcp.Options;

/// <summary>
/// Classe que representa as opções do storage no Google Cloud Storage.
/// </summary>
/// <remarks>
/// Lidas da seção <c>Storage</c>, junto com as opções comuns. Sem <see cref="CredentialsJson"/> e
/// <see cref="CredentialsPath"/>, a credencial vem do Application Default Credentials: a conta de serviço do Cloud
/// Run, do GKE ou do Compute Engine, ou a variável <c>GOOGLE_APPLICATION_CREDENTIALS</c>. É o caminho recomendado em
/// produção, porque não deixa chave na configuração.
/// </remarks>
public class GcpStorageOptions : StorageOptions
{
  #region Properties

  /// <summary>
  /// Conteúdo do arquivo de chave da conta de serviço, em JSON. Opcional; não combine com
  /// <see cref="CredentialsPath"/>.
  /// </summary>
  /// <remarks>
  /// Pensado para um cofre de segredos que entrega o JSON como texto. Só chave de conta de serviço é aceita.
  /// </remarks>
  public string? CredentialsJson { get; set; }

  /// <summary>
  /// Caminho do arquivo de chave da conta de serviço. Opcional; não combine com <see cref="CredentialsJson"/>.
  /// </summary>
  public string? CredentialsPath { get; set; }

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções.
  /// </summary>
  /// <exception cref="InternalServerErrorException">
  /// Quando as duas formas de credencial foram informadas ou quando o arquivo de chave não existe.
  /// </exception>
  public override void Validate()
  {
    base.Validate();

    var hasJson = !string.IsNullOrWhiteSpace(CredentialsJson);
    var hasPath = !string.IsNullOrWhiteSpace(CredentialsPath);

    // Com as duas, não há como saber qual identidade a aplicação pretendia usar
    if (hasJson && hasPath)
    {
      throw new InternalServerErrorException("Options.Storage.Gcp.CredentialsAmbiguous");
    }

    if (hasPath && !File.Exists(CredentialsPath))
    {
      throw new InternalServerErrorException($"Options.Storage.Gcp.CredentialsNotFound;{CredentialsPath}");
    }
  }

  #endregion
}
