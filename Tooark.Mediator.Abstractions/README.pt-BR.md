# Tooark.Mediator.Abstractions

Biblioteca com os contratos base do padrão Mediator para projetos .NET, utilizada por implementações como `Tooark.Mediator`.

📖 **Documentação:** [Tooark.Mediator.Abstractions no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.mediator.abstractions.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.Abstractions/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Visão Geral](#-visão-geral)
- [Instalação](#-instalação)
- [Componentes](#-componentes)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 📖 Visão Geral

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
  - `Task PublishAsync(INotify notify, CancellationToken cancellationToken = default)`
- `IMediator`: combina `ISender` e `IPublisher`.

### Tipo utilitário

- `Unit`
  - `Unit.Value`: a única instância de `Unit`.
  - `Unit.Task`: tarefa concluída com `Unit.Value`, criada uma única vez e reutilizada a cada acesso.

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

// ICommand é atalho para ICommand<Unit>: o envio retorna Task<Unit>
public sealed class OrderMaintenanceService(ISender sender)
{
  public Task<Unit> DeactivateAsync(Guid id, CancellationToken cancellationToken)
  {
    // Curto-circuito síncrono: Unit.Task reutiliza a tarefa pré-construída, sem nova alocação
    if (id == Guid.Empty)
    {
      return Unit.Task;
    }

    return sender.SendAsync(new DeactivateOrderCommand(id), cancellationToken);
  }
}
```

---

## 📋 Dependências

| Pacote                                                                  | Versão | Descrição                             |
| ----------------------------------------------------------------------- | ------ | ------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions) | 4.x    | Exceções (ex.: `BadRequestException`) |

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Comece pelo
[CONTRIBUTING.md](https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md) — ele cobre o fluxo de
desenvolvimento, as convenções de código e de commit e o checklist de pull request. Bugs e pedidos de
funcionalidade entram pelos [templates de issue](https://github.com/Tooark/nuget-tooark/issues/new/choose) do
repositório [Tooark](https://github.com/Tooark/nuget-tooark).

Ao participar, você concorda com o
[Código de Conduta](https://github.com/Tooark/nuget-tooark/blob/main/CODE_OF_CONDUCT.md).

---

## 🆘 Ajuda & Segurança

- ❓ **Dúvidas, bugs e ideias** — veja o
  [SUPPORT.md](https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md) para escolher o canal certo
- 🔒 **Vulnerabilidades de segurança** — **não** abra issue pública; siga o
  [SECURITY.md](https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md)

---

## 💖 Apoie

Se o Tooark ajuda nos seus projetos, considere apoiar o desenvolvimento:

- 💙 [GitHub Sponsors](https://github.com/sponsors/paulosfjunior)
- ☕ [Ko-fi](https://ko-fi.com/paulosfjunior)

Cada contribuição ajuda a manter o projeto ativo e em evolução. Obrigado! 🙏

---

## 📄 Licença

Este projeto está licenciado sob a [Licença BSD 3-Clause](https://github.com/Tooark/nuget-tooark/blob/main/LICENSE).
