# Tooark.Mediator

Biblioteca com implementação de Mediator para projetos .NET, focada em CQRS/CQS com baixo acoplamento entre camadas.

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator/README.md) · 🇧🇷 **Português (este arquivo)**

## Conteúdo

- [Visão Geral](#visão-geral)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Componentes](#-componentes)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Contribuição](#-contribuição)
- [Licença](#-licença)

## Visão Geral

O pacote `Tooark.Mediator` fornece:

- implementação concreta de `IMediator`;
- registro automático de handlers por assembly;
- pipeline de behaviors para preocupações transversais;
- estratégia configurável de publicação de notificações;
- integração com DI do `Microsoft.Extensions.DependencyInjection`.

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Mediator
```

---

## ⚙️ Configuração

```csharp
using Tooark.Mediator.Injections;

builder.Services.AddTooarkMediator(typeof(Program).Assembly);
```

Também é possível configurar as opções do mediador:

```csharp
using Tooark.Mediator.Enums;
using Tooark.Mediator.Injections;

builder.Services.AddTooarkMediator(options =>
{
  options.NotifyPublishStrategy = ENotifyStrategy.Sequential;
}, typeof(Program).Assembly);
```

> **Recomendação**: informe sempre os assemblies explicitamente (ex: `typeof(Program).Assembly`).
> Sem assemblies, o assembly chamador é escaneado como fallback — funciona para o caso comum,
> mas a forma explícita é imune a refatorações que movam a chamada para outro projeto.
> Classes genéricas abertas são ignoradas pelo scan (o dispatch não suporta open generics).

---

## 📦 Componentes

### Classe principal

- `Mediator`: implementação de `IMediator`.

### Injeção de dependência

- `TooarkDependencyInjection.AddTooarkMediator(IServiceCollection, params Assembly[])`
- `TooarkDependencyInjection.AddTooarkMediator(IServiceCollection, Action<MediatorOptions>, params Assembly[])`
- `TooarkDependencyInjection.AddTooarkMediatorBehavior<TBehavior>(IServiceCollection)`
- `TooarkDependencyInjection.AddTooarkMediatorBehavior(IServiceCollection, Type)`

### Opções

- `MediatorOptions`
  - `NotifyPublishStrategy` (padrão: `ENotifyStrategy.ParallelWhenAll`)
  - Registrado via padrão Options: chamadas múltiplas de `AddTooarkMediator` compõem as configurações em ordem de registro
  - O `Mediator` recebe `IOptions<MediatorOptions>`. Para configurar fora do `AddTooarkMediator`, use
    `services.Configure<MediatorOptions>(...)` — registrar `MediatorOptions` diretamente no container não tem efeito

### Estratégias de publicação

- `ENotifyStrategy.ParallelWhenAll` (padrão): inicia todos os handlers e aguarda a conclusão de todos
  com `Task.WhenAll`. Melhor latência quando os handlers são independentes. Todos os handlers são
  iniciados mesmo que algum falhe ao iniciar, e a primeira falha é propagada ao chamador.
- `ENotifyStrategy.Sequential`: inicia cada handler somente após o anterior concluir, na ordem de
  registro. Se um handler falhar, os seguintes não são executados (fail-fast). Use quando a ordem dos
  efeitos colaterais importa ou os handlers compartilham recursos não thread-safe.

### Pipeline de behaviors

`IPipelineBehavior<TRequest, TResponse>` envolve a execução do handler, permitindo tratar preocupações
transversais — validação, log, transação, cache, autorização — sem repeti-las em cada handler. Cada
behavior decide se invoca a etapa seguinte: não invocar interrompe o pipeline (curto-circuito) e a
resposta do próprio behavior é devolvida ao chamador.

Os behaviors se aplicam apenas a requisições; notificações não passam pelo pipeline. O handler é
resolvido antes da montagem da cadeia, então `Handler.NotFound` continua falhando antes de qualquer
behavior executar. Sem behaviors registrados, o despacho permanece sendo a invocação direta do handler.

O `CancellationToken` é parâmetro obrigatório da etapa seguinte — repasse o token recebido para propagar
o cancelamento, ou informe um token encadeado para aplicar um limite próprio.

Um behavior genérico aberto pode restringir a quais requisições se aplica pela restrição de tipo. Um
behavior declarado com `where TRequest : ICommand<TResponse>` envolve apenas comandos, e o container o
ignora ao despachar consultas — sem necessidade de verificação de tipo em tempo de execução. O mesmo vale
para restrições sobre interfaces próprias da aplicação.

### Validação no registro

Uma requisição é processada por um único handler. Se o scan ou o registro manual resultar em mais de um
handler para a mesma requisição, `AddTooarkMediator` lança `InternalServerErrorException` com o código
`Handler.Duplicated`, identificando a requisição e os handlers em conflito. Notificações continuam
aceitando quantos handlers forem registrados.

### Desempenho do dispatch

O despacho usa wrappers genéricos em cache estático: reflection ocorre apenas na primeira chamada de
cada tipo de mensagem. As exceções lançadas pelos handlers chegam ao chamador diretamente (sem
`TargetInvocationException`).

### Handlers suportados

- `IRequestHandler<TRequest, TResponse>`
- `ICommandHandler<TCommand, TResponse>`
- `ICommandHandler<TCommand>`
- `IQueryHandler<TQuery, TResponse>`
- `INotifyHandler<TNotify>`

### Behaviors suportados

- `IPipelineBehavior<TRequest, TResponse>`

Os handlers ficam no namespace `Tooark.Mediator.Handlers` e os behaviors em `Tooark.Mediator.Behaviors`.

---

## 📝 Exemplos de Uso

### Exemplo CQRS

```csharp
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Handlers;

public sealed record CreateUserCommand(string Name) : ICommand<Guid>;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
  public Task<Guid> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(Guid.NewGuid());
  }
}

public sealed record GetUserByIdQuery(Guid Id) : IQuery<string>;

public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, string>
{
  public Task<string> HandleAsync(GetUserByIdQuery request, CancellationToken cancellationToken = default)
  {
    return Task.FromResult($"Usuário {request.Id}");
  }
}
```

### Exemplo de uso com IMediator

```csharp
using Tooark.Mediator.Abstractions;

public sealed class UsersService(IMediator mediator)
{
  public Task<Guid> CreateAsync(string name, CancellationToken cancellationToken)
  {
    return mediator.SendAsync(new CreateUserCommand(name), cancellationToken);
  }

  public Task<string> GetAsync(Guid id, CancellationToken cancellationToken)
  {
    return mediator.SendAsync(new GetUserByIdQuery(id), cancellationToken);
  }
}
```

### Exemplo de publicação de notificação

```csharp
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Handlers;

public sealed record UserCreatedNotify(Guid UserId) : INotify;

public sealed class UserCreatedNotifyHandler : INotifyHandler<UserCreatedNotify>
{
  public Task HandleAsync(UserCreatedNotify notification, CancellationToken cancellationToken = default)
  {
    Console.WriteLine($"Usuário criado: {notification.UserId}");
    return Task.CompletedTask;
  }
}

await mediator.PublishAsync(new UserCreatedNotify(Guid.NewGuid()), cancellationToken);
```

### Exemplo de behavior de pipeline

Behavior genérico aberto, aplicado a todas as requisições:

```csharp
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IRequest<TResponse>
{
  public async Task<TResponse> HandleAsync(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Iniciando {Request}", typeof(TRequest).Name);

    var response = await next(cancellationToken);

    logger.LogInformation("Concluído {Request}", typeof(TRequest).Name);

    return response;
  }
}
```

Behavior fechado, aplicado a uma requisição específica, que interrompe o pipeline quando a requisição é inválida:

```csharp
using Tooark.Exceptions;
using Tooark.Mediator.Behaviors;
using Tooark.Validations;

public sealed class CreateUserValidationBehavior : IPipelineBehavior<CreateUserCommand, Guid>
{
  public Task<Guid> HandleAsync(
    CreateUserCommand request,
    RequestHandlerDelegate<Guid> next,
    CancellationToken cancellationToken = default)
  {
    var validation = new Validation()
      .IsNotNullOrEmpty(request.Name, nameof(request.Name), "User.NameRequired");

    // Curto-circuito: o handler não é executado
    if (!validation.IsValid)
    {
      throw new BadRequestException(validation);
    }

    return next(cancellationToken);
  }
}
```

Behavior de unidade de trabalho, movendo o `SaveChanges` do Entity Framework para a esteira — os handlers
apenas descrevem as alterações e a persistência acontece uma única vez, ao final:

```csharp
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Behaviors;

public sealed class UnitOfWorkBehavior<TRequest, TResponse>(AppDbContext context)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : ICommand<TResponse>
{
  public async Task<TResponse> HandleAsync(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken = default)
  {
    var response = await next(cancellationToken);

    // Exceção do handler impede esta linha: nada é persistido
    await context.SaveChangesAsync(cancellationToken);

    return response;
  }
}
```

A restrição `where TRequest : ICommand<TResponse>` mantém as consultas fora da esteira — sem ela, toda
leitura chamaria `SaveChanges` sem ter o que persistir. Um único `SaveChangesAsync` já é atômico, pois o
Entity Framework envolve o lote em uma transação; `BeginTransaction` explícito só é necessário quando o
handler persiste mais de uma vez ou combina o `DbContext` com outro recurso transacional.

> O exemplo acima ilustra o padrão. Para Entity Framework Core, o pacote
> [`Tooark.Mediator.EntityFrameworkCore`](https://www.nuget.org/packages/Tooark.Mediator.EntityFrameworkCore) entrega esse
> behavior pronto, com tratamento de comandos aninhados e estratégia de transação explícita.

Com esse behavior registrado, o handler não toca em persistência:

```csharp
public sealed class CreateUserHandler(AppDbContext context) : ICommandHandler<CreateUserCommand, Guid>
{
  public Task<Guid> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
  {
    var user = new User(request.Name);

    context.Users.Add(user);

    return Task.FromResult(user.Id);
  }
}
```

Registro, na ordem em que devem executar — o primeiro registrado é o mais externo:

```csharp
using Tooark.Mediator.Injections;

builder.Services.AddTooarkMediator(typeof(Program).Assembly);
builder.Services.AddTooarkMediatorBehavior(typeof(LoggingBehavior<,>));
builder.Services.AddTooarkMediatorBehavior<CreateUserValidationBehavior>();
builder.Services.AddTooarkMediatorBehavior(typeof(UnitOfWorkBehavior<,>));
```

A validação vem antes da unidade de trabalho: não faz sentido preparar a persistência para em seguida
rejeitar a requisição.

> Os behaviors não são descobertos pelo scan de assemblies: a ordem de execução é semântica e a ordem
> retornada pelo scan não é garantida. O registro é sempre explícito.
>
> Dois pontos de atenção com a unidade de trabalho na esteira. Um comando despachado de dentro de outro
> comando persiste no meio da operação do externo, que persiste novamente ao final — evite o aninhamento
> ou trate a reentrância. E notificações não passam pelo pipeline: os handlers delas executam dentro do
> handler do comando, portanto antes do `SaveChanges`, e o que escreverem no contexto é persistido junto.

---

## 📋 Dependências

| Pacote                                                                                                                                          | Versão   | Descrição                             |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                         | 4.x      | Exceções (ex.: `BadRequestException`) |
| [`Tooark.Mediator.Abstractions`](https://www.nuget.org/packages/Tooark.Mediator.Abstractions)                                                   | 4.x      | Contratos base do padrão Mediator     |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Abstrações de injeção de dependência  |
| [`Microsoft.Extensions.Options`](https://www.nuget.org/packages/Microsoft.Extensions.Options)                                                   | 8.x/10.x | Padrão Options para `MediatorOptions` |

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Mediator](https://github.com/Tooark/nuget-tooark/issues).

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
