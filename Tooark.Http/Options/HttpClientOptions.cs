using Tooark.Exceptions;

namespace Tooark.Http.Options;

/// <summary>
/// Classe que representa as opções de configuração do cliente HTTP do Tooark.
/// </summary>
/// <remarks>
/// Todas as opções têm padrão: a seção é opcional. Os padrões de resiliência são os do handler padrão do
/// <c>Microsoft.Extensions.Http.Resilience</c>, e as relações que ele exige entre os valores são conferidas aqui,
/// no registro, para que uma combinação inválida falhe no startup e não na primeira requisição.
/// </remarks>
public class HttpClientOptions
{
  #region Section

  /// <summary>
  /// Seção de configuração do cliente HTTP.
  /// </summary>
  public const string Section = "HttpClient";

  /// <summary>
  /// Seção de configuração dos clientes HTTP com nome: cada cliente lê <c>HttpClients:{nome}</c>.
  /// </summary>
  /// <remarks>
  /// Fica fora da seção <see cref="Section"/>, que é a do cliente padrão: as duas têm as mesmas chaves, e uma
  /// subseção com o nome de um cliente dentro dela seria uma chave desconhecida para a configuração estrita.
  /// </remarks>
  public const string NamedSection = "HttpClients";

  #endregion

  #region Constants

  /// <summary>
  /// Maior tempo limite aceito, em segundos (uma hora).
  /// </summary>
  public const int MaximumTimeoutSeconds = 3600;

  /// <summary>
  /// Maior quantidade de novas tentativas aceita.
  /// </summary>
  public const int MaximumRetryCount = 10;

  /// <summary>
  /// Maior espera base entre tentativas aceita, em milissegundos (um minuto).
  /// </summary>
  public const int MaximumRetryDelayMilliseconds = 60_000;

  /// <summary>
  /// Menor volume de requisições na janela para o circuito avaliar a taxa de falhas, exigido pelo Polly.
  /// </summary>
  public const int MinimumCircuitBreakerThroughput = 2;

  /// <summary>
  /// Maior janela de amostragem e maior tempo de circuito aberto aceitos, em segundos (um dia).
  /// </summary>
  public const int MaximumCircuitBreakerSeconds = 86_400;

  #endregion

  #region Properties

  /// <summary>
  /// Endereço base do cliente, uma URL absoluta <c>http</c> ou <c>https</c>. Padrão: nenhum.
  /// </summary>
  /// <remarks>
  /// Uma URI relativa passada ao serviço só funciona com o endereço base. Um endereço com caminho ganha a barra
  /// final que faltar (<c>https://api.exemplo.com/v1</c> vira <c>https://api.exemplo.com/v1/</c>), para que
  /// <c>usuarios</c> leve a <c>/v1/usuarios</c> e não a <c>/usuarios</c>.
  /// </remarks>
  public string? BaseAddress { get; set; }

  /// <summary>
  /// Tempo limite da chamada inteira, em segundos, somando as tentativas e a leitura do corpo. Padrão: 30.
  /// </summary>
  public int TotalTimeoutSeconds { get; set; } = 30;

  /// <summary>
  /// Tempo limite de cada tentativa, em segundos, até os cabeçalhos da resposta. Padrão: 10.
  /// </summary>
  public int AttemptTimeoutSeconds { get; set; } = 10;

  /// <summary>
  /// Quantidade de novas tentativas depois da primeira. Padrão: 3. Zero desliga as novas tentativas.
  /// </summary>
  /// <remarks>
  /// A nova tentativa acontece em falha de conexão, tempo limite da tentativa e respostas 408, 429 e 5xx. Os verbos
  /// que não são idempotentes, POST e PATCH, seguem <see cref="RetryNonIdempotentMethods"/>.
  /// </remarks>
  public int RetryCount { get; set; } = 3;

  /// <summary>
  /// Indica se POST e PATCH também são tentados de novo. Padrão: verdadeiro.
  /// </summary>
  /// <remarks>
  /// GET, PUT e DELETE são idempotentes: repetir a chamada leva ao mesmo estado. POST e PATCH não são, e a nova
  /// tentativa pode repetir o efeito de uma chamada que o serviço chamado já tinha processado (um pedido criado
  /// duas vezes, uma chamada paga cobrada duas vezes). Desligue quando o serviço chamado não é idempotente; os
  /// demais verbos continuam sendo tentados de novo. A partir da v5 o padrão passa a ser falso.
  /// </remarks>
  public bool RetryNonIdempotentMethods { get; set; } = true;

  /// <summary>
  /// Espera base entre tentativas, em milissegundos. Padrão: 2000. A espera cresce de forma exponencial, com
  /// variação aleatória.
  /// </summary>
  public int RetryDelayMilliseconds { get; set; } = 2000;

  /// <summary>
  /// Taxa de falhas, de 0 a 1, que abre o circuito dentro da janela de amostragem. Padrão: 0,1.
  /// </summary>
  public double CircuitBreakerFailureRatio { get; set; } = 0.1;

  /// <summary>
  /// Volume mínimo de requisições na janela para a taxa de falhas ser avaliada. Padrão: 100. Mínimo: 2.
  /// </summary>
  public int CircuitBreakerMinimumThroughput { get; set; } = 100;

  /// <summary>
  /// Janela de amostragem do circuito, em segundos. Padrão: 30. Precisa ser pelo menos o dobro de
  /// <see cref="AttemptTimeoutSeconds"/>.
  /// </summary>
  public int CircuitBreakerSamplingSeconds { get; set; } = 30;

  /// <summary>
  /// Tempo em que o circuito fica aberto antes de testar o serviço de novo, em segundos. Padrão: 5.
  /// </summary>
  public int CircuitBreakerBreakSeconds { get; set; } = 5;

  /// <summary>
  /// Indica se o handler de resiliência (novas tentativas, circuito e tempo limite por tentativa) é registrado.
  /// Padrão: verdadeiro.
  /// </summary>
  /// <remarks>
  /// Desligado, só <see cref="TotalTimeoutSeconds"/> continua valendo.
  /// </remarks>
  public bool Resilience { get; set; } = true;

  /// <summary>
  /// Opções da guarda de saída contra SSRF. Desligada por padrão neste cliente.
  /// </summary>
  public OutboundGuardOptions OutboundGuard { get; set; } = new();

  #endregion

  #region Validate

  /// <summary>
  /// Valida as opções, falhando no startup quando um valor está fora da faixa ou quando a combinação seria recusada
  /// pelo handler de resiliência.
  /// </summary>
  /// <exception cref="InternalServerErrorException">Quando alguma opção é inválida.</exception>
  public void Validate()
  {
    // O valor não vai na mensagem: um endereço com usuário e senha levaria a credencial para o log
    if (!TryGetBaseAddress(out _))
    {
      throw new InternalServerErrorException("Options.HttpClient.BaseAddressInvalid");
    }

    if (TotalTimeoutSeconds is < 1 or > MaximumTimeoutSeconds)
    {
      throw new InternalServerErrorException($"Options.HttpClient.TotalTimeoutInvalid;1;{MaximumTimeoutSeconds}");
    }

    // O handler de resiliência recusa uma tentativa mais longa que a chamada inteira
    if (AttemptTimeoutSeconds < 1 || AttemptTimeoutSeconds > TotalTimeoutSeconds)
    {
      throw new InternalServerErrorException($"Options.HttpClient.AttemptTimeoutInvalid;1;{TotalTimeoutSeconds}");
    }

    if (RetryCount is < 0 or > MaximumRetryCount)
    {
      throw new InternalServerErrorException($"Options.HttpClient.RetryCountInvalid;0;{MaximumRetryCount}");
    }

    if (RetryDelayMilliseconds is < 0 or > MaximumRetryDelayMilliseconds)
    {
      throw new InternalServerErrorException($"Options.HttpClient.RetryDelayInvalid;0;{MaximumRetryDelayMilliseconds}");
    }

    // O double.NaN falha nas duas comparações, por isso a faixa é conferida pela forma positiva
    if (!(CircuitBreakerFailureRatio >= 0 && CircuitBreakerFailureRatio <= 1))
    {
      throw new InternalServerErrorException("Options.HttpClient.CircuitBreakerFailureRatioInvalid");
    }

    if (CircuitBreakerMinimumThroughput < MinimumCircuitBreakerThroughput)
    {
      throw new InternalServerErrorException(
        $"Options.HttpClient.CircuitBreakerMinimumThroughputInvalid;{MinimumCircuitBreakerThroughput}");
    }

    // O handler de resiliência exige a janela com pelo menos o dobro do tempo limite da tentativa: menor que
    // isso, uma tentativa lenta sai da janela antes de ser contada
    var minimumSampling = AttemptTimeoutSeconds * 2;

    if (CircuitBreakerSamplingSeconds < minimumSampling || CircuitBreakerSamplingSeconds > MaximumCircuitBreakerSeconds)
    {
      throw new InternalServerErrorException(
        $"Options.HttpClient.CircuitBreakerSamplingInvalid;{minimumSampling};{MaximumCircuitBreakerSeconds}");
    }

    if (CircuitBreakerBreakSeconds is < 1 or > MaximumCircuitBreakerSeconds)
    {
      throw new InternalServerErrorException(
        $"Options.HttpClient.CircuitBreakerBreakInvalid;1;{MaximumCircuitBreakerSeconds}");
    }

    OutboundGuard.Validate();
  }

  #endregion

  #region Internal Methods

  /// <summary>
  /// Monta o endereço base a partir do texto configurado.
  /// </summary>
  /// <param name="baseAddress">O endereço base, com a barra final, ou nulo quando não há endereço configurado.</param>
  /// <returns>Falso quando o texto configurado não é uma URL absoluta <c>http</c> ou <c>https</c> sem consulta.</returns>
  internal bool TryGetBaseAddress(out Uri? baseAddress)
  {
    baseAddress = null;

    if (string.IsNullOrWhiteSpace(BaseAddress))
    {
      return true;
    }

    if (!Uri.TryCreate(BaseAddress.Trim(), UriKind.Absolute, out var uri)
      || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
      || !string.IsNullOrEmpty(uri.Query)
      || !string.IsNullOrEmpty(uri.Fragment)
      || !string.IsNullOrEmpty(uri.UserInfo))
    {
      return false;
    }

    // Sem a barra final, a combinação com uma URI relativa descarta o último segmento do caminho
    baseAddress = uri.AbsolutePath.EndsWith('/') ? uri : new UriBuilder(uri) { Path = uri.AbsolutePath + "/" }.Uri;

    return true;
  }

  #endregion
}
