# Tooark.Mediator.Abstractions

Library with the base contracts of the Mediator pattern for .NET projects, used by implementations such as `Tooark.Mediator`.

📖 **Docs:** [Tooark.Mediator.Abstractions on the site](https://tooark.com/nuget-tooark/packages/tooark.mediator.abstractions.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.Abstractions/README.pt-BR.md)

---

## 📑 Contents

- [Overview](#-overview)
- [Installation](#-installation)
- [Components](#-components)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 📖 Overview

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

## 🤝 Contributing

Contributions are welcome! Start with
[CONTRIBUTING.md](https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md) — it covers the development
workflow, the coding and commit conventions and the pull request checklist. Bugs and feature requests go through
the [issue templates](https://github.com/Tooark/nuget-tooark/issues/new/choose) of the
[Tooark](https://github.com/Tooark/nuget-tooark) repository.

By participating you agree to the
[Code of Conduct](https://github.com/Tooark/nuget-tooark/blob/main/CODE_OF_CONDUCT.md).

---

## 🆘 Help & Security

- ❓ **Questions, bugs, feature ideas** — see
  [SUPPORT.md](https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md) for the right channel
- 🔒 **Security vulnerabilities** — do **not** open a public issue; follow
  [SECURITY.md](https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md)

---

## 💖 Support

If Tooark helps your projects, consider supporting its development:

- 💙 [GitHub Sponsors](https://github.com/sponsors/paulosfjunior)
- ☕ [Ko-fi](https://ko-fi.com/paulosfjunior)

Every contribution helps keep the project maintained and improving. Thank you! 🙏

---

## 📄 License

This project is licensed under the [BSD 3-Clause License](https://github.com/Tooark/nuget-tooark/blob/main/LICENSE).
