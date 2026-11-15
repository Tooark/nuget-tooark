namespace Tooark.Securities;

/// <summary>
/// Detecta se a aplicação roda em contêiner, para os padrões que mudam nesse ambiente.
/// </summary>
/// <remarks>
/// Dois sinais contam: a variável <c>DOTNET_RUNNING_IN_CONTAINER</c>, que as imagens oficiais do .NET definem como
/// <c>true</c>, e a <c>KUBERNETES_SERVICE_HOST</c>, que o Kubernetes injeta em todo pod, inclusive nos de imagens
/// próprias que não definem a primeira. Fora desses sinais a aplicação é tratada como fora de contêiner.
/// </remarks>
internal static class ContainerEnvironment
{
  /// <summary>
  /// Variável que as imagens oficiais do .NET definem como <c>true</c>.
  /// </summary>
  internal const string DotnetRunningInContainer = "DOTNET_RUNNING_IN_CONTAINER";

  /// <summary>
  /// Variável que o Kubernetes injeta em todo pod.
  /// </summary>
  internal const string KubernetesServiceHost = "KUBERNETES_SERVICE_HOST";

  /// <summary>
  /// Indica se a aplicação roda em contêiner, pelas variáveis de ambiente do processo.
  /// </summary>
  /// <returns>Verdadeiro quando um dos sinais de contêiner está presente.</returns>
  internal static bool IsRunningInContainer() => IsRunningInContainer(Environment.GetEnvironmentVariable);

  /// <summary>
  /// Indica se a aplicação roda em contêiner, lendo as variáveis pela função informada.
  /// </summary>
  /// <param name="read">Leitura de uma variável de ambiente pelo nome.</param>
  /// <returns>Verdadeiro quando um dos sinais de contêiner está presente.</returns>
  internal static bool IsRunningInContainer(Func<string, string?> read)
  {
    var dotnet = read(DotnetRunningInContainer)?.Trim();

    // As imagens oficiais gravam "true"; o runtime também aceita "1" nas variáveis booleanas
    if (string.Equals(dotnet, "true", StringComparison.OrdinalIgnoreCase) || dotnet == "1")
    {
      return true;
    }

    // Em um pod a variável do serviço da API do Kubernetes está sempre presente
    return !string.IsNullOrWhiteSpace(read(KubernetesServiceHost));
  }
}
