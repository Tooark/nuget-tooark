using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Behaviors;
using Tooark.Mediator.Handlers;
using Tooark.Mediator.Injections;

namespace Tooark.Tests.Mediator;

public class PipelineBehaviorTests
{
  // Teste para garantir que o behavior envolve a execução do manipulador.
  [Fact]
  public async Task SendAsync_ShouldWrapHandler_WhenBehaviorIsRegistered()
  {
    // Arrange
    ExecutionLog.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<PipelineRequest, string>, PipelineRequestHandler>();
    services.AddTooarkMediatorBehavior<FirstBehavior>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.SendAsync(new PipelineRequest("valor"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("valor", result);
    Assert.Equal(["first:antes", "handler", "first:depois"], ExecutionLog.Steps);
  }

  // Teste para garantir que os behaviors executam na ordem de registro.
  [Fact]
  public async Task SendAsync_ShouldExecuteBehaviorsInRegistrationOrder()
  {
    // Arrange - o primeiro registrado é o mais externo, então conclui por último
    ExecutionLog.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<PipelineRequest, string>, PipelineRequestHandler>();
    services.AddTooarkMediatorBehavior<FirstBehavior>();
    services.AddTooarkMediatorBehavior<SecondBehavior>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.SendAsync(new PipelineRequest("valor"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(
      ["first:antes", "second:antes", "handler", "second:depois", "first:depois"],
      ExecutionLog.Steps);
  }

  // Teste para garantir que o behavior pode interromper o pipeline sem invocar o manipulador.
  [Fact]
  public async Task SendAsync_ShouldShortCircuit_WhenBehaviorDoesNotCallNext()
  {
    // Arrange
    ExecutionLog.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<PipelineRequest, string>, PipelineRequestHandler>();
    services.AddTooarkMediatorBehavior<ShortCircuitBehavior>();
    services.AddTooarkMediatorBehavior<FirstBehavior>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.SendAsync(new PipelineRequest("valor"), TestContext.Current.CancellationToken);

    // Assert - nem o behavior seguinte nem o manipulador executam
    Assert.Equal("curto-circuito", result);
    Assert.Equal(["short:antes"], ExecutionLog.Steps);
  }

  // Teste para garantir que o behavior pode alterar a resposta do manipulador.
  [Fact]
  public async Task SendAsync_ShouldAllowBehaviorToChangeResponse()
  {
    // Arrange
    ExecutionLog.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<PipelineRequest, string>, PipelineRequestHandler>();
    services.AddTooarkMediatorBehavior<UpperCaseBehavior>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.SendAsync(new PipelineRequest("valor"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("VALOR", result);
  }

  // Teste para garantir que o token de cancelamento chega ao manipulador através do pipeline.
  [Fact]
  public async Task SendAsync_ShouldFlowCancellationToken_ThroughPipeline()
  {
    // Arrange
    ExecutionLog.Reset();
    using var cts = new CancellationTokenSource();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<TokenRequest, bool>, TokenRequestHandler>();
    services.AddTooarkMediatorBehavior<TokenBehavior>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.SendAsync(new TokenRequest(cts.Token), cts.Token);

    // Assert - o manipulador recebeu o mesmo token informado no envio
    Assert.True(result);
  }

  // Teste para garantir que um behavior genérico aberto se aplica a qualquer requisição.
  [Fact]
  public async Task SendAsync_ShouldApplyOpenGenericBehavior()
  {
    // Arrange
    ExecutionLog.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<PipelineRequest, string>, PipelineRequestHandler>();
    services.AddTooarkMediatorBehavior(typeof(OpenBehavior<,>));

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.SendAsync(new PipelineRequest("valor"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(["open:antes", "handler", "open:depois"], ExecutionLog.Steps);
  }

  // Teste para garantir que o mesmo behavior registrado duas vezes executa apenas uma vez.
  [Fact]
  public async Task AddTooarkMediatorBehavior_ShouldRegisterOnce_WhenCalledTwiceForSameBehavior()
  {
    // Arrange
    ExecutionLog.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<PipelineRequest, string>, PipelineRequestHandler>();
    services.AddTooarkMediatorBehavior<FirstBehavior>();
    services.AddTooarkMediatorBehavior<FirstBehavior>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.SendAsync(new PipelineRequest("valor"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(["first:antes", "handler", "first:depois"], ExecutionLog.Steps);
  }

  // Teste para garantir que um tipo que não é behavior falha no registro.
  [Fact]
  public void AddTooarkMediatorBehavior_ShouldThrow_WhenTypeIsNotBehavior()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    var exception = Assert.Throws<InternalServerErrorException>(
      () => services.AddTooarkMediatorBehavior<PipelineRequestHandler>());

    Assert.Contains("Behavior.NotSupported", exception.Message);
  }

  // Teste para garantir que um behavior genérico aberto sem correspondência direta falha no registro.
  [Fact]
  public void AddTooarkMediatorBehavior_ShouldThrow_WhenOpenBehaviorDoesNotMatchInterface()
  {
    // Arrange - o container não consegue fechar a definição genérica a partir de IPipelineBehavior<,>
    var services = new ServiceCollection();

    // Act & Assert
    var exception = Assert.Throws<InternalServerErrorException>(
      () => services.AddTooarkMediatorBehavior(typeof(PartialOpenBehavior<>)));

    Assert.Contains("Behavior.NotSupported", exception.Message);
  }

  // Teste para garantir que o tipo nulo falha no registro.
  [Fact]
  public void AddTooarkMediatorBehavior_ShouldThrow_WhenTypeIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    var exception = Assert.Throws<InternalServerErrorException>(
      () => services.AddTooarkMediatorBehavior(null!));

    Assert.Contains("Mediator.Null.Behavior", exception.Message);
  }

  // Teste para garantir que a coleção de serviços nula falha no registro.
  [Fact]
  public void AddTooarkMediatorBehavior_ShouldThrow_WhenServiceCollectionIsNull()
  {
    // Act & Assert
    var exception = Assert.Throws<InternalServerErrorException>(
      () => TooarkDependencyInjection.AddTooarkMediatorBehavior(null!, typeof(FirstBehavior)));

    Assert.Contains("Mediator.Null.Service", exception.Message);
  }

  // Teste para garantir que um behavior que retorna tarefa nula falha com erro identificável.
  [Fact]
  public async Task SendAsync_ShouldThrow_WhenBehaviorReturnsNullTask()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<PipelineRequest, string>, PipelineRequestHandler>();
    services.AddTooarkMediatorBehavior<NullTaskBehavior>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(
      () => mediator.SendAsync(new PipelineRequest("valor"), TestContext.Current.CancellationToken));

    Assert.Contains("Behavior.ExecutionFailed", exception.Message);
    Assert.Contains(nameof(NullTaskBehavior), exception.Message);
  }

  // Teste para garantir que a ausência do manipulador falha antes de executar os behaviors.
  [Fact]
  public async Task SendAsync_ShouldThrowHandlerNotFound_BeforeRunningBehaviors()
  {
    // Arrange
    ExecutionLog.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTooarkMediatorBehavior<FirstBehavior>();

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(
      () => mediator.SendAsync(new PipelineRequest("valor"), TestContext.Current.CancellationToken));

    Assert.Contains("Handler.NotFound", exception.Message);
    Assert.Empty(ExecutionLog.Steps);
  }

  // Teste para garantir o funcionamento com provedores que devolvem os behaviors como enumerável preguiçoso.
  [Fact]
  public async Task SendAsync_ShouldMaterializeBehaviors_WhenProviderReturnsLazyEnumerable()
  {
    // Arrange - containers de terceiros não necessariamente devolvem uma lista para IEnumerable<T>
    ExecutionLog.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IRequestHandler<PipelineRequest, string>, PipelineRequestHandler>();

    var provider = services.BuildServiceProvider();
    var mediator = new global::Tooark.Mediator.Mediator(new LazyBehaviorServiceProvider(provider));

    // Act
    var result = await mediator.SendAsync(new PipelineRequest("valor"), TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal("valor", result);
    Assert.Equal(["first:antes", "handler", "first:depois"], ExecutionLog.Steps);
  }

  // Teste para garantir que um behavior genérico aberto com restrição só se aplica às requisições que a satisfazem.
  [Fact]
  public async Task SendAsync_ShouldApplyConstrainedOpenGenericBehavior_OnlyToMatchingRequests()
  {
    // Arrange - o behavior restringe TRequest a ICommand<TResponse>, então não deve alcançar as consultas
    ExecutionLog.Reset();

    var services = new ServiceCollection();
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<SampleCommand, string>, SampleCommandHandler>();
    services.AddTransient<IRequestHandler<SampleQuery, string>, SampleQueryHandler>();
    services.AddTooarkMediatorBehavior(typeof(CommandOnlyBehavior<,>));

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.SendAsync(new SampleCommand(), TestContext.Current.CancellationToken);
    await mediator.SendAsync(new SampleQuery(), TestContext.Current.CancellationToken);

    // Assert - o behavior envolveu o comando e foi ignorado na consulta
    Assert.Equal(["command-only:antes", "comando", "command-only:depois", "consulta"], ExecutionLog.Steps);
  }

  #region Fixtures

  // Registra a ordem de execução das etapas do pipeline
  private static class ExecutionLog
  {
    private static readonly List<string> _steps = [];

    public static IReadOnlyList<string> Steps
    {
      get
      {
        lock (_steps)
        {
          return [.. _steps];
        }
      }
    }

    public static void Add(string step)
    {
      lock (_steps)
      {
        _steps.Add(step);
      }
    }

    public static void Reset()
    {
      lock (_steps)
      {
        _steps.Clear();
      }
    }
  }

  public sealed record PipelineRequest(string Value) : IRequest<string>;

  public sealed class PipelineRequestHandler : IRequestHandler<PipelineRequest, string>
  {
    public Task<string> HandleAsync(PipelineRequest request, CancellationToken cancellationToken = default)
    {
      ExecutionLog.Add("handler");

      return Task.FromResult(request.Value);
    }
  }

  public sealed class FirstBehavior : IPipelineBehavior<PipelineRequest, string>
  {
    public async Task<string> HandleAsync(PipelineRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken = default)
    {
      ExecutionLog.Add("first:antes");

      var response = await next(cancellationToken);

      ExecutionLog.Add("first:depois");

      return response;
    }
  }

  public sealed class SecondBehavior : IPipelineBehavior<PipelineRequest, string>
  {
    public async Task<string> HandleAsync(PipelineRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken = default)
    {
      ExecutionLog.Add("second:antes");

      var response = await next(cancellationToken);

      ExecutionLog.Add("second:depois");

      return response;
    }
  }

  public sealed class ShortCircuitBehavior : IPipelineBehavior<PipelineRequest, string>
  {
    public Task<string> HandleAsync(PipelineRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken = default)
    {
      ExecutionLog.Add("short:antes");

      return Task.FromResult("curto-circuito");
    }
  }

  public sealed class UpperCaseBehavior : IPipelineBehavior<PipelineRequest, string>
  {
    public async Task<string> HandleAsync(PipelineRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken = default)
    {
      var response = await next(cancellationToken);

      return response.ToUpperInvariant();
    }
  }

  public sealed class NullTaskBehavior : IPipelineBehavior<PipelineRequest, string>
  {
    public Task<string> HandleAsync(PipelineRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken = default)
    {
      return null!;
    }
  }

  public sealed class OpenBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
  {
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
      ExecutionLog.Add("open:antes");

      var response = await next(cancellationToken);

      ExecutionLog.Add("open:depois");

      return response;
    }
  }

  // Behavior genérico aberto cujos parâmetros não correspondem aos da interface
  public sealed class PartialOpenBehavior<TResponse> : IPipelineBehavior<PipelineRequest, string>
  {
    public Task<string> HandleAsync(PipelineRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken = default)
    {
      return next(cancellationToken);
    }
  }

  // Provedor que devolve os behaviors como enumerável preguiçoso, sem implementar IReadOnlyList
  private sealed class LazyBehaviorServiceProvider(IServiceProvider inner) : IServiceProvider
  {
    public object? GetService(Type serviceType)
    {
      // Intercepta apenas a resolução dos behaviors da requisição usada no teste
      if (serviceType == typeof(IEnumerable<IPipelineBehavior<PipelineRequest, string>>))
      {
        return Enumerate();
      }

      return inner.GetService(serviceType);
    }

    private static IEnumerable<IPipelineBehavior<PipelineRequest, string>> Enumerate()
    {
      yield return new FirstBehavior();
    }
  }

  public sealed record SampleCommand : ICommand<string>;

  public sealed class SampleCommandHandler : IRequestHandler<SampleCommand, string>
  {
    public Task<string> HandleAsync(SampleCommand request, CancellationToken cancellationToken = default)
    {
      ExecutionLog.Add("comando");

      return Task.FromResult("comando");
    }
  }

  public sealed record SampleQuery : IQuery<string>;

  public sealed class SampleQueryHandler : IRequestHandler<SampleQuery, string>
  {
    public Task<string> HandleAsync(SampleQuery request, CancellationToken cancellationToken = default)
    {
      ExecutionLog.Add("consulta");

      return Task.FromResult("consulta");
    }
  }

  // Behavior restrito a comandos: o container só o aplica às requisições que satisfazem a restrição
  public sealed class CommandOnlyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
  {
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
      ExecutionLog.Add("command-only:antes");

      var response = await next(cancellationToken);

      ExecutionLog.Add("command-only:depois");

      return response;
    }
  }

  public sealed record TokenRequest(CancellationToken Expected) : IRequest<bool>;

  public sealed class TokenRequestHandler : IRequestHandler<TokenRequest, bool>
  {
    public Task<bool> HandleAsync(TokenRequest request, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(request.Expected == cancellationToken);
    }
  }

  public sealed class TokenBehavior : IPipelineBehavior<TokenRequest, bool>
  {
    public Task<bool> HandleAsync(TokenRequest request, RequestHandlerDelegate<bool> next, CancellationToken cancellationToken = default)
    {
      return next(cancellationToken);
    }
  }

  #endregion
}
