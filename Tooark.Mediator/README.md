# Tooark.Mediator

Library with a Mediator implementation for .NET projects, focused on CQRS/CQS with low coupling between layers.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator/README.pt-BR.md)

## Contents

- [Overview](#overview)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Components](#-components)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Contributing](#-contributing)
- [License](#-license)

## Overview

The `Tooark.Mediator` package provides:

- a concrete implementation of `IMediator`;
- automatic handler registration by assembly;
- a behavior pipeline for cross-cutting concerns;
- a configurable notification publishing strategy;
- integration with `Microsoft.Extensions.DependencyInjection`.

---

## 🔧 Installation

```bash
dotnet add package Tooark.Mediator
```

---

## ⚙️ Configuration

```csharp
using Tooark.Mediator.Injections;

builder.Services.AddTooarkMediator(typeof(Program).Assembly);
```

The mediator options can also be configured:

```csharp
using Tooark.Mediator.Enums;
using Tooark.Mediator.Injections;

builder.Services.AddTooarkMediator(options =>
{
  options.NotifyPublishStrategy = ENotifyStrategy.Sequential;
}, typeof(Program).Assembly);
```

> **Recommendation**: always pass the assemblies explicitly (e.g. `typeof(Program).Assembly`).
> Without assemblies, the calling assembly is scanned as a fallback — it works for the common case,
> but the explicit form is immune to refactorings that move the call to another project.
> Open generic classes are skipped by the scan (dispatch does not support open generics).

---

## 📦 Components

### Main class

- `Mediator`: implementation of `IMediator`.

### Dependency injection

- `TooarkDependencyInjection.AddTooarkMediator(IServiceCollection, params Assembly[])`
- `TooarkDependencyInjection.AddTooarkMediator(IServiceCollection, Action<MediatorOptions>, params Assembly[])`
- `TooarkDependencyInjection.AddTooarkMediatorBehavior<TBehavior>(IServiceCollection)`
- `TooarkDependencyInjection.AddTooarkMediatorBehavior(IServiceCollection, Type)`

### Options

- `MediatorOptions`
  - `NotifyPublishStrategy` (default: `ENotifyStrategy.ParallelWhenAll`)
  - Registered through the Options pattern: multiple `AddTooarkMediator` calls compose the configuration in registration order
  - `Mediator` receives `IOptions<MediatorOptions>`. To configure outside `AddTooarkMediator`, use
    `services.Configure<MediatorOptions>(...)` — registering `MediatorOptions` directly in the container has no effect

### Publishing strategies

- `ENotifyStrategy.ParallelWhenAll` (default): starts every handler and waits for all of them to complete
  with `Task.WhenAll`. Best latency when the handlers are independent. Every handler is started even if one
  fails to start, and the first failure is propagated to the caller.
- `ENotifyStrategy.Sequential`: starts each handler only after the previous one completes, in registration
  order. If a handler fails, the following ones do not run (fail-fast). Use it when the order of side effects
  matters or the handlers share resources that are not thread-safe.

### Behavior pipeline

`IPipelineBehavior<TRequest, TResponse>` wraps the handler execution, allowing cross-cutting concerns —
validation, logging, transaction, caching, authorization — to be handled without repeating them in every
handler. Each behavior decides whether to invoke the next step: not invoking it interrupts the pipeline
(short-circuit) and the behavior's own response is returned to the caller.

Behaviors apply only to requests; notifications do not go through the pipeline. The handler is resolved
before the chain is built, so `Handler.NotFound` still fails before any behavior runs. With no behaviors
registered, dispatch remains the direct invocation of the handler.

The `CancellationToken` is a required parameter of the next step — pass along the received token to
propagate cancellation, or pass a linked token to apply your own limit.

An open generic behavior can restrict which requests it applies to through its type constraint. A behavior
declared with `where TRequest : ICommand<TResponse>` wraps commands only, and the container skips it when
dispatching queries — with no need for a runtime type check. The same applies to constraints on the
application's own interfaces.

### Registration validation

A request is processed by a single handler. If the scan or manual registration results in more than one
handler for the same request, `AddTooarkMediator` throws `InternalServerErrorException` with the
`Handler.Duplicated` code, identifying the request and the conflicting handlers. Notifications keep accepting
as many handlers as are registered.

### Dispatch performance

Dispatch uses generic wrappers in a static cache: reflection happens only on the first call for each message
type. Exceptions thrown by handlers reach the caller directly (no `TargetInvocationException`).

### Supported handlers

- `IRequestHandler<TRequest, TResponse>`
- `ICommandHandler<TCommand, TResponse>`
- `ICommandHandler<TCommand>`
- `IQueryHandler<TQuery, TResponse>`
- `INotifyHandler<TNotify>`

### Supported behaviors

- `IPipelineBehavior<TRequest, TResponse>`

Handlers live in the `Tooark.Mediator.Handlers` namespace and behaviors in `Tooark.Mediator.Behaviors`.

---

## 📝 Usage Examples

### CQRS example

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
    return Task.FromResult($"User {request.Id}");
  }
}
```

### Using IMediator

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

### Publishing a notification

```csharp
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Handlers;

public sealed record UserCreatedNotify(Guid UserId) : INotify;

public sealed class UserCreatedNotifyHandler : INotifyHandler<UserCreatedNotify>
{
  public Task HandleAsync(UserCreatedNotify notification, CancellationToken cancellationToken = default)
  {
    Console.WriteLine($"User created: {notification.UserId}");
    return Task.CompletedTask;
  }
}

await mediator.PublishAsync(new UserCreatedNotify(Guid.NewGuid()), cancellationToken);
```

### Pipeline behavior example

Open generic behavior, applied to every request:

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
    logger.LogInformation("Starting {Request}", typeof(TRequest).Name);

    var response = await next(cancellationToken);

    logger.LogInformation("Completed {Request}", typeof(TRequest).Name);

    return response;
  }
}
```

Closed behavior, applied to a specific request, which interrupts the pipeline when the request is invalid:

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

    // Short-circuit: the handler is not executed
    if (!validation.IsValid)
    {
      throw new BadRequestException(validation);
    }

    return next(cancellationToken);
  }
}
```

Unit of work behavior, moving Entity Framework's `SaveChanges` into the pipeline — handlers only describe
the changes and persistence happens once, at the end:

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

    // An exception from the handler prevents this line: nothing is persisted
    await context.SaveChangesAsync(cancellationToken);

    return response;
  }
}
```

The `where TRequest : ICommand<TResponse>` constraint keeps queries out of the pipeline — without it, every
read would call `SaveChanges` with nothing to persist. A single `SaveChangesAsync` is already atomic, since
Entity Framework wraps the batch in a transaction; an explicit `BeginTransaction` is only needed when the
handler persists more than once or combines the `DbContext` with another transactional resource.

> The example above illustrates the pattern. For Entity Framework Core, the
> [`Tooark.Mediator.EntityFrameworkCore`](https://www.nuget.org/packages/Tooark.Mediator.EntityFrameworkCore)
> package ships that behavior ready-made, with nested command handling and an explicit transaction strategy.

With that behavior registered, the handler does not touch persistence:

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

Registration, in the order they must run — the first registered is the outermost:

```csharp
using Tooark.Mediator.Injections;

builder.Services.AddTooarkMediator(typeof(Program).Assembly);
builder.Services.AddTooarkMediatorBehavior(typeof(LoggingBehavior<,>));
builder.Services.AddTooarkMediatorBehavior<CreateUserValidationBehavior>();
builder.Services.AddTooarkMediatorBehavior(typeof(UnitOfWorkBehavior<,>));
```

Validation comes before the unit of work: it makes no sense to prepare persistence and then reject the
request.

> Behaviors are not discovered by the assembly scan: the execution order is semantic and the order
> returned by the scan is not guaranteed. Registration is always explicit.
>
> Two points of attention with the unit of work in the pipeline. A command dispatched from within another
> command persists in the middle of the outer one's operation, which persists again at the end — avoid
> nesting or handle reentrancy. And notifications do not go through the pipeline: their handlers run inside
> the command handler, therefore before `SaveChanges`, and whatever they write to the context is persisted
> together.

---

## 📋 Dependencies

| Package                                                                                                                                         | Version  | Description                             |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | --------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                         | 4.x      | Exceptions (e.g. `BadRequestException`) |
| [`Tooark.Mediator.Abstractions`](https://www.nuget.org/packages/Tooark.Mediator.Abstractions)                                                   | 4.x      | Base contracts of the Mediator pattern  |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Dependency injection abstractions       |
| [`Microsoft.Extensions.Options`](https://www.nuget.org/packages/Microsoft.Extensions.Options)                                                   | 8.x/10.x | Options pattern for `MediatorOptions`   |

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Mediator](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
