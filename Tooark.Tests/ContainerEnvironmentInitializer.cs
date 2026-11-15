using System.Runtime.CompilerServices;

namespace Tooark.Tests;

/// <summary>
/// Remove os sinais de contêiner do processo de testes antes de qualquer teste rodar.
/// </summary>
/// <remarks>
/// O <c>AddTooarkDataProtection</c> liga a trava do armazenamento de chaves por padrão quando detecta um contêiner.
/// Sem esta limpeza, a suíte passaria em uma máquina e falharia em um devcontainer ou em um job de CI que roda em
/// contêiner. Os testes que exercitam o padrão de contêiner informam o ambiente de forma explícita.
/// </remarks>
internal static class ContainerEnvironmentInitializer
{
  [ModuleInitializer]
  internal static void Initialize()
  {
    Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
    Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_HOST", null);
  }
}
