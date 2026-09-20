# Tooark.Mediator.EntityFrameworkCore

Library that moves Entity Framework Core persistence into the `Tooark.Mediator` pipeline, keeping handlers free of `SaveChanges`.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.EntityFrameworkCore/README.pt-BR.md)

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

Every command handler ends with `await context.SaveChangesAsync(...)`. Repeated across dozens of handlers, the line becomes noise — and forgetting it produces a command that "works" without writing anything.

The package registers a behavior in the `Tooark.Mediator` pipeline that persists the changes once, at the end of the command:

- handlers only describe the changes on the `DbContext`;
- persistence happens only when the pipeline completes without an exception;
- queries do not go through persistence;
- nested commands take part in the same unit of work.

---

## 🔧 Installation

```bash
dotnet add package Tooark.Mediator.EntityFrameworkCore
```

---

## ⚙️ Configuration

```csharp
using Tooark.Mediator.EntityFrameworkCore.Injections;
using Tooark.Mediator.Injections;

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddTooarkMediator(typeof(Program).Assembly);
builder.Services.AddTooarkMediatorUnitOfWork<AppDbContext>();
```

With an explicit transaction, for when the handler persists more than once or combines the `DbContext` with another transactional resource:

```csharp
using Tooark.Mediator.EntityFrameworkCore.Enums;

builder.Services.AddTooarkMediatorUnitOfWork<AppDbContext>(EUnitOfWorkStrategy.Transaction);
```

> **Registration order**: the behavior takes the position where `AddTooarkMediatorUnitOfWork` is called, and
> the pipeline behaviors run in registration order. Register validation **before** the unit of work — it
> makes no sense to prepare persistence and then reject the request.

---

## 📦 Components

### Persistence strategies

| Strategy      | Behavior                                                                              |
| ------------- | ------------------------------------------------------------------------------------- |
| `SaveChanges` | Persists at the end of the command, with Entity Framework Core's implicit transaction |
| `Transaction` | Runs the command inside an explicit transaction, persisting before committing         |

`SaveChanges` is the default and covers the common case: a single call to `SaveChangesAsync` is already atomic, since Entity Framework Core wraps the batch in a transaction. The explicit transaction is only needed when there is more than one persistence in the same command, or when the `DbContext` is combined with another transactional resource.

In the `Transaction` strategy, the transaction is opened inside the provider's resiliency strategy. That avoids the error that occurs when opening an explicit transaction with retries configured (`EnableRetryOnFailure`), but it means the operation may run more than once on a transient failure — the handler must be idempotent.

### Dependency injection

- `TooarkDependencyInjection.AddTooarkMediatorUnitOfWork<TContext>(IServiceCollection, EUnitOfWorkStrategy)`

### Abstraction

- `IUnitOfWork` (`Tooark.Mediator.EntityFrameworkCore.Interfaces`): exposes `SaveChangesAsync` and `ExecuteInTransactionAsync`, with no Entity Framework types, allowing the implementation to be replaced in tests.

### Behavior with nested commands

A command dispatched from within another command takes part in the unit of work already started: only the outermost command persists. Without that control, the inner command would write in the middle of the outer one's operation, and a later failure would leave the database in a partial state.

### Behavior with notifications

Notifications do not go through the pipeline: their handlers run inside the command handler, therefore **before** persistence. Whatever they write to the `DbContext` is saved together, in the same unit of work.

---

## 📝 Usage Examples

### Handler without persistence

```csharp
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Handlers;

public sealed record CreateProduct(string Name) : ICommand<Guid>;

public sealed class CreateProductHandler(AppDbContext context) : ICommandHandler<CreateProduct, Guid>
{
  public Task<Guid> HandleAsync(CreateProduct request, CancellationToken cancellationToken = default)
  {
    var product = new Product(request.Name);

    context.Products.Add(product);

    // No SaveChanges: persistence is the pipeline's responsibility
    return Task.FromResult(product.Id);
  }
}
```

### Query, which does not go through persistence

```csharp
public sealed record CountProducts : IQuery<int>;

public sealed class CountProductsHandler(AppDbContext context) : IQueryHandler<CountProducts, int>
{
  public Task<int> HandleAsync(CountProducts request, CancellationToken cancellationToken = default)
  {
    return context.Products.CountAsync(cancellationToken);
  }
}
```

The behavior is constrained to `ICommand<TResponse>`, so the container skips it when dispatching the query — with no runtime type check.

### Full pipeline

```csharp
builder.Services.AddTooarkMediator(typeof(Program).Assembly);
builder.Services.AddTooarkMediatorBehavior(typeof(LoggingBehavior<,>));
builder.Services.AddTooarkMediatorBehavior(typeof(ValidationBehavior<,>));
builder.Services.AddTooarkMediatorUnitOfWork<AppDbContext>();
```

From outermost to innermost: logging, validation, unit of work and, last, the handler.

---

## 📋 Dependencies

| Package                                                                                                                                         | Version  | Usage                                 |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------- |
| [`Tooark.Mediator`](https://www.nuget.org/packages/Tooark.Mediator)                                                                             | 4.x      | Behavior pipeline and registration    |
| [`Tooark.Mediator.Abstractions`](https://www.nuget.org/packages/Tooark.Mediator.Abstractions)                                                   | 4.x      | Message contracts (`ICommand`)        |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                         | 4.x      | Configuration errors                  |
| [`Microsoft.EntityFrameworkCore`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore)                                                 | 8.x/10.x | Context, persistence and transactions |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Container registration                |

The package is part of the `Tooark` aggregator, so whoever installs `Tooark` already gets it.

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Mediator.EntityFrameworkCore](https://github.com/Tooark/nuget-tooark/issues) repository.

---

## 📄 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
