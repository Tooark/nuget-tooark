using Microsoft.Extensions.DependencyInjection;
using Tooark.Http;
using Tooark.Http.Injections;
using Tooark.Http.Interfaces;
using Tooark.Http.Options;

namespace Tooark.Tests.Http.Support;

/// <summary>
/// Monta o container dos testes do cliente HTTP, com o registro real do pacote.
/// </summary>
internal static class HttpTestHost
{
  // Registra o cliente do Tooark com o handler primário falso no lugar da rede
  public static ServiceProvider WithHandler(FakeHttpMessageHandler handler, Action<HttpClientOptions>? configure = null)
  {
    var services = new ServiceCollection();

    services.AddTooarkHttpClient(options =>
    {
      // Espera mínima entre tentativas, para os testes de nova tentativa não dormirem
      options.RetryDelayMilliseconds = 1;
      configure?.Invoke(options);
    });

    services.AddHttpClient(HttpClientService.ClientName).ConfigurePrimaryHttpMessageHandler(() => handler);

    return services.BuildServiceProvider();
  }

  // Registra o cliente do Tooark com a guarda ligada, a conexão real e o DNS falso
  public static ServiceProvider WithGuard(FakeHostResolver resolver, Action<HttpClientOptions>? configure = null)
  {
    var services = new ServiceCollection();

    services.AddSingleton<Tooark.Http.Internal.IHostResolver>(resolver);
    services.AddTooarkHttpClient(options =>
    {
      options.RetryDelayMilliseconds = 1;
      options.OutboundGuard.Enabled = true;
      configure?.Invoke(options);
    });

    return services.BuildServiceProvider();
  }

  // Resolve o serviço
  public static IHttpClientService Service(this ServiceProvider provider) =>
    provider.GetRequiredService<IHttpClientService>();
}
