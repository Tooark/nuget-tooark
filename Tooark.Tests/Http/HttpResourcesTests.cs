using System.Text.Json;
using Tooark.Extensions;

namespace Tooark.Tests.Http;

/// <summary>
/// Testes das traduções das chaves de mensagem emitidas pelo pacote de HTTP.
/// </summary>
public class HttpResourcesTests
{
  // Idiomas distribuídos com o pacote de extensões
  private static readonly string[] Idiomas = ["en-US", "pt-BR", "es-ES"];

  // Lê o arquivo de recurso do idioma embutido no assembly, que é o que o consumidor recebe
  private static Dictionary<string, string> Recurso(string idioma)
  {
    var assembly = typeof(JsonStringLocalizerExtension).Assembly;

    using var conteudo = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.{idioma}.default.json")
      ?? throw new InvalidOperationException($"O idioma {idioma} não está embutido no assembly.");
    using var documento = JsonDocument.Parse(conteudo);

    return documento.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
  }

  // Testa se cada chave emitida pelo pacote tem texto nos três idiomas, com um marcador para cada parâmetro
  [Theory]
  [InlineData("Http.BaseAddressNotConfigured", 0)]
  [InlineData("Http.CircuitOpen", 0)]
  [InlineData("Http.ConcurrencyLimitReached", 0)]
  [InlineData("Http.ConnectionFailed", 0)]
  [InlineData("Http.InvalidResponse", 0)]
  [InlineData("Http.OutboundBlocked", 1)]
  [InlineData("Http.RequestFailed", 1)]
  [InlineData("Http.RequestTimeout", 0)]
  [InlineData("Http.RequestUriInvalid", 0)]
  [InlineData("Options.HttpClient.AttemptTimeoutInvalid", 2)]
  [InlineData("Options.HttpClient.BaseAddressInvalid", 0)]
  [InlineData("Options.HttpClient.CircuitBreakerBreakInvalid", 2)]
  [InlineData("Options.HttpClient.CircuitBreakerFailureRatioInvalid", 0)]
  [InlineData("Options.HttpClient.CircuitBreakerMinimumThroughputInvalid", 1)]
  [InlineData("Options.HttpClient.CircuitBreakerSamplingInvalid", 2)]
  [InlineData("Options.HttpClient.OutboundGuard.AllowedHostInvalid", 1)]
  [InlineData("Options.HttpClient.OutboundGuard.AllowedNetworkInvalid", 1)]
  [InlineData("Options.HttpClient.RetryCountInvalid", 2)]
  [InlineData("Options.HttpClient.RetryDelayInvalid", 2)]
  [InlineData("Options.HttpClient.TotalTimeoutInvalid", 2)]
  public void Keys_ShouldBeTranslatedInEveryLanguage(string chave, int parametros)
  {
    // Act & Assert
    foreach (var idioma in Idiomas)
    {
      var recurso = Recurso(idioma);

      Assert.True(recurso.TryGetValue(chave, out var texto), $"{idioma}: {chave}");
      Assert.False(string.IsNullOrWhiteSpace(texto));

      for (int indice = 0; indice < 3; indice++)
      {
        Assert.Equal(indice < parametros, texto.Contains($"{{{indice}}}"));
      }
    }
  }
}
