using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tooark.AspNetCore.Injections;

namespace Tooark.Tests.AspNetCore;

/// <summary>
/// Apoio dos testes de Problem Details: monta o contexto HTTP e lê o corpo escrito na resposta.
/// </summary>
internal static class ProblemDetailsTestHelper
{
  /// <summary>
  /// Monta o container como a aplicação monta, com o logging e o registro do Problem Details.
  /// </summary>
  /// <param name="configure">Configuração extra do container.</param>
  /// <returns>O provedor de serviços.</returns>
  internal static ServiceProvider CreateProvider(Action<IServiceCollection>? configure = null)
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddTooarkProblemDetails();
    configure?.Invoke(services);

    return services.BuildServiceProvider();
  }

  /// <summary>
  /// Monta uma requisição com o caminho e o Accept informados, com um corpo de resposta legível.
  /// </summary>
  /// <param name="provider">Provedor de serviços da requisição.</param>
  /// <param name="path">Caminho da requisição.</param>
  /// <param name="accept">Valor do header Accept; nulo não envia o header.</param>
  /// <returns>O contexto HTTP.</returns>
  internal static DefaultHttpContext CreateContext(IServiceProvider provider, string path = "/users/1", string? accept = null)
  {
    var context = new DefaultHttpContext { RequestServices = provider };
    context.Request.Path = path;
    context.Response.Body = new MemoryStream();

    if (accept is not null)
    {
      context.Request.Headers.Accept = accept;
    }

    return context;
  }

  /// <summary>
  /// Lê o corpo da resposta como JSON.
  /// </summary>
  /// <param name="context">Contexto HTTP com a resposta escrita.</param>
  /// <returns>O documento JSON do corpo.</returns>
  internal static async Task<JsonDocument> ReadBodyAsync(HttpContext context)
  {
    context.Response.Body.Position = 0;

    return await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
  }
}
