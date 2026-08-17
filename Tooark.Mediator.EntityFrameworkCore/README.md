# Tooark.Mediator.EntityFrameworkCore

Biblioteca que move a persistência do Entity Framework Core para o pipeline do `Tooark.Mediator`, mantendo os handlers livres de `SaveChanges`.

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

Cada handler de comando termina com `await context.SaveChangesAsync(...)`. Repetida em dezenas de handlers, a linha vira ruído — e esquecê-la produz um comando que "funciona" sem gravar nada.

O pacote registra um behavior no pipeline do `Tooark.Mediator` que persiste as alterações uma única vez, ao final do comando:

- os handlers apenas descrevem as alterações no `DbContext`;
- a persistência ocorre somente quando o pipeline conclui sem exceção;
- consultas não passam pela persistência;
- comandos aninhados participam da mesma unidade de trabalho.

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Mediator.EntityFrameworkCore
```

---

## ⚙️ Configuração

```csharp
using Tooark.Mediator.EntityFrameworkCore.Injections;
using Tooark.Mediator.Injections;

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddTooarkMediator(typeof(Program).Assembly);
builder.Services.AddTooarkMediatorUnitOfWork<AppDbContext>();
```

Com transação explícita, para quando o handler persiste mais de uma vez ou combina o `DbContext` com outro recurso transacional:

```csharp
using Tooark.Mediator.EntityFrameworkCore.Enums;

builder.Services.AddTooarkMediatorUnitOfWork<AppDbContext>(EUnitOfWorkStrategy.Transaction);
```

> **Ordem de registro**: o behavior ocupa a posição em que `AddTooarkMediatorUnitOfWork` é chamado, e os
> behaviors do pipeline executam na ordem de registro. Registre a validação **antes** da unidade de
> trabalho — não faz sentido preparar a persistência para em seguida rejeitar a requisição.

---

## 📦 Componentes

### Estratégias de persistência

| Estratégia    | Comportamento                                                                       |
| ------------- | ----------------------------------------------------------------------------------- |
| `SaveChanges` | Persiste ao final do comando, com a transação implícita do Entity Framework Core    |
| `Transaction` | Executa o comando dentro de uma transação explícita, persistindo antes de confirmar |

`SaveChanges` é o padrão e atende o caso comum: uma única chamada a `SaveChangesAsync` já é atômica, pois o Entity Framework Core envolve o lote em uma transação. A transação explícita só é necessária quando há mais de uma persistência no mesmo comando, ou quando o `DbContext` é combinado com outro recurso transacional.

Na estratégia `Transaction`, a transação é aberta dentro da estratégia de resiliência do provedor. Isso evita o erro que ocorre ao abrir transação explícita com retentativa configurada (`EnableRetryOnFailure`), mas implica que a operação pode ser executada mais de uma vez em caso de falha transitória — o handler precisa ser idempotente.

### Injeção de dependência

- `TooarkDependencyInjection.AddTooarkMediatorUnitOfWork<TContext>(IServiceCollection, EUnitOfWorkStrategy)`

### Abstração

- `IUnitOfWork` (`Tooark.Mediator.EntityFrameworkCore.Interfaces`): expõe `SaveChangesAsync` e `ExecuteInTransactionAsync`, sem tipos do Entity Framework, permitindo substituir a implementação em testes.

### Comportamento com comandos aninhados

Um comando despachado de dentro de outro comando participa da unidade de trabalho já iniciada: apenas o comando mais externo persiste. Sem esse controle, o comando interno gravaria no meio da operação do externo, e uma falha posterior deixaria o banco em estado parcial.

### Comportamento com notificações

Notificações não passam pelo pipeline: os handlers delas executam dentro do handler do comando, portanto **antes** da persistência. O que escreverem no `DbContext` é gravado junto, na mesma unidade de trabalho.

---

## 📝 Exemplos de Uso

### Handler sem persistência

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

    // Sem SaveChanges: a persistência é responsabilidade do pipeline
    return Task.FromResult(product.Id);
  }
}
```

### Consulta, que não passa pela persistência

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

O behavior é restrito a `ICommand<TResponse>`, então o container o ignora ao despachar a consulta — sem verificação de tipo em tempo de execução.

### Pipeline completo

```csharp
builder.Services.AddTooarkMediator(typeof(Program).Assembly);
builder.Services.AddTooarkMediatorBehavior(typeof(LoggingBehavior<,>));
builder.Services.AddTooarkMediatorBehavior(typeof(ValidationBehavior<,>));
builder.Services.AddTooarkMediatorUnitOfWork<AppDbContext>();
```

Do mais externo para o mais interno: log, validação, unidade de trabalho e, por último, o handler.

---

## 📋 Dependências

| Pacote                                                  | Versão   | Uso                                 |
| ------------------------------------------------------- | -------- | ----------------------------------- |
| `Tooark.Mediator`                                       | 4.x      | Pipeline de behaviors e registro    |
| `Tooark.Mediator.Abstractions`                          | 4.x      | Contratos de mensagens (`ICommand`) |
| `Tooark.Exceptions`                                     | 4.x      | Erros de configuração               |
| `Microsoft.EntityFrameworkCore`                         | 8.x/10.x | Contexto, persistência e transações |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.x/10.x | Registro no container               |

O pacote integra o agregador `Tooark`, então quem instala `Tooark` já o recebe.

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Mediator.EntityFrameworkCore](https://github.com/Tooark/tooark-cs/issues).

---

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/tooark-cs/refs/heads/main/LICENSE) para mais detalhes.
