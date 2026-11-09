namespace Tooark.Observability;

/// <summary>
/// Identidade do serviço resolvida para o Resource: o nome e a versão que vão em <c>service.name</c> e
/// <c>service.version</c>, com a origem do nome, que decide o nome padrão da fonte e do medidor.
/// </summary>
/// <param name="name">Nome do serviço resolvido.</param>
/// <param name="isNameKnown">
/// Indica se o nome veio de uma fonte real (opções, <c>OTEL_SERVICE_NAME</c> ou assembly de entrada), e não do
/// fallback <c>unknown_service</c>.
/// </param>
/// <param name="version">Versão do serviço resolvida.</param>
internal sealed class ServiceIdentity(string name, bool isNameKnown, string version)
{
  /// <summary>
  /// Nome do serviço resolvido (<c>service.name</c>).
  /// </summary>
  public string Name { get; } = name;

  /// <summary>
  /// Indica se o nome veio de uma fonte real, e não do fallback <c>unknown_service</c>.
  /// </summary>
  public bool IsNameKnown { get; } = isNameKnown;

  /// <summary>
  /// Versão do serviço resolvida (<c>service.version</c>).
  /// </summary>
  public string Version { get; } = version;
}
