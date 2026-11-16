using Tooark.Exceptions;

namespace Tooark.Http.Exceptions;

/// <summary>
/// Representa a recusa de uma requisição pela guarda de saída: o destino é um endereço que uma URL informada pelo
/// usuário não pode alcançar.
/// </summary>
/// <remarks>
/// É uma <see cref="BadRequestException"/> porque a URL veio de quem chamou a aplicação. A mensagem é a chave
/// <c>Http.OutboundBlocked</c> com o host como parâmetro; o endereço IP para o qual o nome resolveu não é exposto.
/// </remarks>
public sealed class OutboundBlockedException : BadRequestException
{
  #region Constructors

  /// <summary>
  /// Cria a exceção para o host recusado.
  /// </summary>
  /// <param name="host">O host da requisição recusada.</param>
  public OutboundBlockedException(string host) : base(BuildMessage(host))
  {
    Host = host ?? string.Empty;
  }

  /// <summary>
  /// Cria a exceção para o host recusado, preservando a exceção de transporte que a carregava.
  /// </summary>
  /// <param name="host">O host da requisição recusada.</param>
  /// <param name="innerException">A exceção que originou esta exceção, preservada para diagnóstico.</param>
  public OutboundBlockedException(string host, Exception innerException) : base(BuildMessage(host), innerException)
  {
    Host = host ?? string.Empty;
  }

  #endregion

  #region Properties

  /// <summary>
  /// O host da requisição recusada, como estava na URL.
  /// </summary>
  public string Host { get; }

  #endregion

  #region Private Methods

  /// <summary>
  /// Monta a chave da mensagem com o host como parâmetro.
  /// </summary>
  /// <param name="host">O host da requisição recusada.</param>
  /// <returns>A chave com o parâmetro.</returns>
  private static string BuildMessage(string? host) =>
    // O ponto e vírgula separa os parâmetros da chave, e não aparece em um host válido
    $"Http.OutboundBlocked;{(host ?? string.Empty).Replace(';', ' ')}";

  #endregion
}
