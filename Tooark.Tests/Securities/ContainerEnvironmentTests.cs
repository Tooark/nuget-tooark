using Tooark.Securities;

namespace Tooark.Tests.Securities;

public class ContainerEnvironmentTests
{
  // Monta a leitura de variáveis de ambiente a partir de um dicionário
  private static Func<string, string?> Variables(Dictionary<string, string?> values) =>
    name => values.TryGetValue(name, out var value) ? value : null;

  // Testa se a variável das imagens oficiais do .NET indica contêiner, em qualquer caixa e também como "1"
  [Theory]
  [InlineData("true")]
  [InlineData("True")]
  [InlineData(" TRUE ")]
  [InlineData("1")]
  public void IsRunningInContainer_ShouldBeTrue_WhenDotnetVariableIsOn(string value)
  {
    // Act
    var result = ContainerEnvironment.IsRunningInContainer(Variables(new() { ["DOTNET_RUNNING_IN_CONTAINER"] = value }));

    // Assert
    Assert.True(result);
  }

  // Testa se um pod do Kubernetes é tratado como contêiner mesmo em uma imagem que não define a variável do .NET
  [Fact]
  public void IsRunningInContainer_ShouldBeTrue_WhenKubernetesServiceHostIsSet()
  {
    // Act
    var result = ContainerEnvironment.IsRunningInContainer(Variables(new() { ["KUBERNETES_SERVICE_HOST"] = "10.0.0.1" }));

    // Assert
    Assert.True(result);
  }

  // Testa se a falta dos dois sinais, ou valores que não ligam a variável, tratam a aplicação como fora de contêiner
  [Theory]
  [InlineData(null, null)]
  [InlineData("false", null)]
  [InlineData("0", "")]
  [InlineData("", "   ")]
  public void IsRunningInContainer_ShouldBeFalse_WhenNoSignalIsPresent(string? dotnet, string? kubernetes)
  {
    // Act
    var result = ContainerEnvironment.IsRunningInContainer(Variables(new()
    {
      ["DOTNET_RUNNING_IN_CONTAINER"] = dotnet,
      ["KUBERNETES_SERVICE_HOST"] = kubernetes
    }));

    // Assert
    Assert.False(result);
  }

  // Testa se o processo de testes roda sem os sinais de contêiner, como o inicializador do assembly garante
  [Fact]
  public void IsRunningInContainer_ShouldBeFalse_InTheTestProcess()
  {
    // Act & Assert
    Assert.False(ContainerEnvironment.IsRunningInContainer());
  }
}
