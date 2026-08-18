namespace Tooark.Observability.Options;

/// <summary>
/// Opções de configuração para tratamento de dados sensíveis em OpenTelemetry.
/// </summary>
public class DataSensitiveOptions
{
  #region Properties

  /// <summary>
  /// Indica se os parâmetros de query devem ser removidos dos atributos de span. Padrão: true.
  /// </summary>
  /// <remarks>
  /// Remove o atributo url.query nas requisições ASP.NET Core e a query do atributo url.full nas
  /// requisições HttpClient, conforme as convenções semânticas atuais do OpenTelemetry.
  /// O atributo legado http.target é reescrito apenas quando já presente com query.
  /// </remarks>
  public bool HideQueryParameters { get; set; } = true;

  /// <summary>
  /// Indica se os headers sensíveis devem ser mascarados/removidos dos atributos de span.
  /// </summary>
  public bool HideHeaders { get; set; } = true;

  /// <summary>
  /// Lista de headers que devem ser tratados como sensíveis e mascarados quando a sanitização estiver ativa.
  /// </summary>
  /// <remarks>
  /// Valores são comparados de forma case-insensitive.
  /// </remarks>
  public string[] SensitiveRequestHeaders { get; set; } =
  [
    "authorization",
    "proxy-authorization",
    "cookie",
    "set-cookie",
    "x-api-key",
    "api-key",
    "apikey",
    "x-functions-key",
    "x-amz-security-token",
    "x-google-oauth-access-token",
    "x-azure-access-token"
  ];

  #endregion
}
