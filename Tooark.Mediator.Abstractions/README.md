# Tooark.Mediator.Abstractions

Library with the base contracts of the Mediator pattern for .NET projects, used by implementations such as `Tooark.Mediator`.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.Abstractions/README.pt-BR.md)

## Contents

- [Overview](#overview)
- [Installation](#-installation)
- [Components](#-components)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Contributing](#-contributing)
- [License](#-license)

## Overview

The `Tooark.Mediator.Abstractions` package defines the contracts for:

- requests (`IRequest`, `IRequest<TResponse>`);
- commands (`ICommand`, `ICommand<TResponse>`);
- queries (`IQuery<TResponse>`);
- notifications (`INotify`);
- sending and publishing (`ISender`, `IPublisher`);
- the main interface (`IMediator`);
- the empty return (`Unit`).

---

## 🔧 Installation

```bash
dotnet add package Tooark.Mediator.Abstractions
```

---

## 📦 Components

### Message contracts

- `IRequest<TResponse>`: base contract of a request with a response.
- `IRequest`: shortcut for a request without a response payload (`Unit`).
- `ICommand<TResponse>`: command with a response.
- `ICommand`: command without an explicit response (`Unit`).
- `IQuery<TResponse>`: query with a response.
- `INotify`: notification/event without a response.

### Orchestration contracts

- `ISender`
  - `Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)`
- `IPublisher`
  - `Task PublishAsync(INotify notify, CancellationToken cancellationToken = default)`
- `IMediator`: combines `ISender` and `IPublisher`.

### Utility type

- `Unit`
  - `Unit.Value`: the single instance of `Unit`.
  - `Unit.Task`: a task completed with `Unit.Value`, created once and reused on every access.

---

## 📝 Usage Examples

### Defining messages

```csharp
using Tooark.Mediator.Abstractions;

public sealed record CreateOrderCommand(string CustomerName) : ICommand<Guid>;

public sealed record GetOrderByIdQuery(Guid Id) : IQuery<string>;

public sealed record OrderCreatedNotify(Guid OrderId) : INotify;
```

### Depending on the contracts in a service

```csharp
using Tooark.Mediator.Abstractions;

public sealed class OrderApplicationService(ISender sender, IPublisher publisher)
{
  public async Task<Guid> CreateAsync(string customerName, CancellationToken cancellationToken)
  {
    var id = await sender.SendAsync(new CreateOrderCommand(customerName), cancellationToken);

    await publisher.PublishAsync(new OrderCreatedNotify(id), cancellationToken);

    return id;
  }

  public Task<string> GetByIdAsync(Guid id, CancellationToken cancellationToken)
  {
    return sender.SendAsync(new GetOrderByIdQuery(id), cancellationToken);
  }
}
```

### Using Unit in commands without a return value

```csharp
using Tooark.Mediator.Abstractions;

public sealed record DeactivateOrderCommand(Guid Id) : ICommand;

// ICommand is a shortcut for ICommand<Unit>: sending returns Task<Unit>
public sealed class OrderMaintenanceService(ISender sender)
{
  public Task<Unit> DeactivateAsync(Guid id, CancellationToken cancellationToken)
  {
    // Synchronous short-circuit: Unit.Task reuses the pre-built task, with no new allocation
    if (id == Guid.Empty)
    {
      return Unit.Task;
    }

    return sender.SendAsync(new DeactivateOrderCommand(id), cancellationToken);
  }
}
```

---

## 📋 Dependencies

| Package                                                                 | Version | Description                             |
| ----------------------------------------------------------------------- | ------- | --------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions) | 4.x     | Exceptions (e.g. `BadRequestException`) |

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Mediator.Abstractions](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
