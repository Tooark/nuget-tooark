using Tooark.Exceptions;

namespace Tooark.Secrets.Options;

/// <summary>
/// Classe que representa as opções comuns aos provedores de segredos.
/// </summary>
/// <remarks>
/// Cada provedor estende estas opções com as próprias (região, projeto, endereço) e as lê da mesma seção
/// <c>Secrets</c>. A credencial de acesso ao cofre nunca vem do cofre: ela vem do ambiente (role da AWS, Application
/// Default Credentials do Google, auth Kubernetes ou AppRole no Vault).
/// </remarks>
public class SecretsOptions
{
  #region Section

  /// <summary>
  /// Seção de configuração dos segredos.
  /// </summary>
  public const string Section = "Secrets";

  #endregion

  #region Properties

  /// <summary>
  /// Indica se um valor que é um objeto JSON vira chaves filhas na configuração. Padrão: falso.
  /// </summary>
  /// <remarks>
  /// Ligado, o segredo <c>arkuest/prod</c> com <c>{"Jwt": {"Secret": "..."}}</c> vira a chave <c>Jwt:Secret</c>. Vale
  /// para todo valor da fonte que é objeto JSON: um segredo com uma chave de conta de serviço do Google também vira
  /// chaves. Os valores de texto dentro do objeto nunca são lidos de novo como JSON, então essa chave, guardada como
  /// texto dentro do documento, continua inteira. Com um segredo por chave, deixe desligado.
  /// </remarks>
  public bool ExpandJson { get; set; }

  /// <summary>
  /// Indica se a falha ao carregar a configuração do cofre é ignorada. Padrão: falso, a aplicação não sobe.
  /// </summary>
  public bool Optional { get; set; }

  /// <summary>
  /// Tempo máximo, em segundos, para carregar a configuração do cofre no startup. Padrão: 30.
  /// </summary>
  public int TimeoutSeconds { get; set; } = 30;

  /// <summary>
  /// Tempo, em minutos, que um segredo lido pelo <see cref="Interfaces.ISecretService"/> fica em cache. Padrão: 5.
  /// Zero desliga o cache.
  /// </summary>
  public int CacheMinutes { get; set; } = 5;

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções.
  /// </summary>
  /// <exception cref="InternalServerErrorException">Quando o tempo limite ou o cache estão fora do intervalo aceito.</exception>
  public virtual void Validate()
  {
    if (TimeoutSeconds < 1)
    {
      throw new InternalServerErrorException("Options.Secrets.TimeoutInvalid");
    }

    if (CacheMinutes < 0)
    {
      throw new InternalServerErrorException("Options.Secrets.CacheMinutesInvalid");
    }
  }

  #endregion
}
