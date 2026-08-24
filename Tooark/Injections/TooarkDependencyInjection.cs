using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Dtos.Injections;
using Tooark.Extensions.Injections;
using Tooark.Mediator.Injections;
using Tooark.Observability.Injections;
using Tooark.Observability.Options;
using Tooark.Securities.Injections;
using Tooark.Securities.Options;
using Tooark.ValueObjects.Injections;

namespace Tooark.Injections;

/// <summary>
/// Classe de extensão para injeção de dependência do projeto Tooark.
/// </summary>
public static partial class TooarkDependencyInjection
{
  #region Methods

  /// <summary>
  /// Adiciona as injeções de dependência dos projetos Tooark.
  /// </summary>
  /// <remarks>
  /// Registra o <c>Tooark.Dtos</c>, o <c>Tooark.Extensions</c>, o <c>Tooark.ValueObjects</c> e o
  /// <c>Tooark.Mediator</c>, e acrescenta o <c>Tooark.Securities</c> e o <c>Tooark.Observability</c>
  /// quando as seções de configuração correspondentes existem.
  /// <para>
  /// Sem <paramref name="assemblies"/>, os manipuladores do mediador são procurados no assembly que
  /// chamou este método. Informe os assemblies quando os manipuladores estiverem em outro projeto,
  /// como é comum em aplicação em camadas. Chamar <c>AddTooarkMediator</c> depois, para acrescentar
  /// outros assemblies ou configurar as opções, é seguro: o registro dos manipuladores não duplica.
  /// </para>
  /// <para>
  /// A unidade de trabalho fica de fora, porque <c>AddTooarkMediatorUnitOfWork</c> precisa do tipo do
  /// contexto do Entity Framework. Registre-a na aplicação quando for usá-la.
  /// </para>
  /// </remarks>
  /// <param name="services">A coleção de serviços para adicionar o serviço.</param>
  /// <param name="configuration">A configuração da aplicação.</param>
  /// <param name="assemblies">Assemblies a varrer em busca dos manipuladores do mediador.</param>
  /// <returns>A coleção de serviços adicionados.</returns>
  [MethodImpl(MethodImplOptions.NoInlining)] // NoInlining garante que GetCallingAssembly retorne o assembly do usuário
  public static IServiceCollection AddTooarkService(
    this IServiceCollection services,
    IConfiguration? configuration = null,
    params Assembly[] assemblies
  )
  {
    // Captura o assembly chamador antes de descer a pilha. Repassar o assembly é obrigatório: sem ele,
    // o GetCallingAssembly de dentro do AddTooarkMediator devolveria este assembly, o do agregador, que
    // não tem manipulador algum — o mediador subiria vazio e falharia só em execução.
    var handlerAssemblies = assemblies is { Length: > 0 } ? assemblies : [Assembly.GetCallingAssembly()];

    // Adiciona as injeções de dependência Dtos
    services.AddTooarkDtos();

    // Adiciona as injeções de dependência Extensions
    services.AddTooarkExtensions();

    // Adiciona as injeções de dependência ValueObjects
    services.AddTooarkValueObjects();

    // Adiciona as injeções de dependência Mediator
    services.AddTooarkMediator(handlerAssemblies);

    // Verifica se as configurações para JWT Token ou Criptografia existem
    if (configuration is not null)
    {
      // Verifica se as seções de configuração existem
      if (
        configuration.GetSection(JwtOptions.Section).Exists() ||
        configuration.GetSection(CryptographyOptions.Section).Exists()
      )
      {
        // Adiciona as injeções de dependência Securities
        services.AddTooarkSecurities(configuration);
      }

      // Verifica se a seção de configuração de Observability existe
      if (configuration.GetSection(ObservabilityOptions.Section).Exists())
      {
        // Adiciona as injeções de dependência Observability
        services.AddTooarkObservability(configuration);
      }
    }

    // Retorna a coleção de serviços
    return services;
  }

  #endregion
}
