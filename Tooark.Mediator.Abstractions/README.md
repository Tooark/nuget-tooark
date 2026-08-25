# Tooark.Mediator.Abstractions

Biblioteca com os contratos base do padrão Mediator para projetos .NET, utilizada por implementações como `Tooark.Mediator`.

## Conteúdo

- [Visão Geral](#visão-geral)
- [Instalação](#instalação)
- [Componentes](#componentes)
- [Exemplos de Uso](#exemplos-de-uso)
- [Dependências](#dependências)
- [Contribuição](#contribuição)
- [Licença](#licença)

## Visão Geral

O pacote `Tooark.Mediator.Abstractions` define os contratos para:

- requests (`IRequest`, `IRequest<TResponse>`);
- commands (`ICommand`, `ICommand<TResponse>`);
- queries (`IQuery<TResponse>`);
- notifications (`INotify`);
- envio e publicação (`ISender`, `IPublisher`);
- interface principal (`IMediator`);
- retorno vazio (`Unit`).

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Mediator.Abstractions
```

---

## 📦 Componentes

### Contratos de mensagem

- `IRequest<TResponse>`: contrato base de requisição com resposta.
- `IRequest`: atalho para requisição sem payload de resposta (`Unit`).
- `ICommand<TResponse>`: comando com resposta.
- `ICommand`: comando sem resposta explícita (`Unit`).
- `IQuery<TResponse>`: consulta com resposta.
- `INotify`: notificação/evento sem resposta.

### Contratos de orquestração

- `ISender`
  - `Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)`
- `IPublisher`
  - `Task PublishAsync(INotify notification, CancellationToken cancellationToken = default)`
- `IMediator`: combina `ISender` e `IPublisher`.

### Tipo utilitário

- `Unit`
  - `Unit.Value`
  - `Unit.Task`

---

## 📝 Exemplos de Uso

### Definindo mensagens

```csharp
using Tooark.Mediator.Abstractions;

public sealed record CreateOrderCommand(string CustomerName) : ICommand<Guid>;

public sealed record GetOrderByIdQuery(Guid Id) : IQuery<string>;

public sealed record OrderCreatedNotify(Guid OrderId) : INotify;
```

### Dependendo dos contratos no serviço

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

### Usando Unit em comandos sem retorno

```csharp
using Tooark.Mediator.Abstractions;

public sealed record DeactivateOrderCommand(Guid Id) : ICommand;

public static Task<Unit> SuccessAsync()
{
  return Unit.Task;
}
```

---

## 📋 Dependências

| Pacote              | Versão | Descrição                             |
| ------------------- | ------ | ------------------------------------- |
| `Tooark.Exceptions` | —      | Exceções (ex.: `BadRequestException`) |

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Mediator.Abstractions](https://github.com/Tooark/nuget-tooark/issues).

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
