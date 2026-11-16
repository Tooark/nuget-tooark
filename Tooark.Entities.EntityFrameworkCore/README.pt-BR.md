# Tooark.Entities.EntityFrameworkCore

Biblioteca que liga as entidades do Tooark ao Entity Framework Core: um interceptor que preenche os campos de auditoria e converte o `Remove()` em exclusão lógica, e convenções de modelo para o filtro de excluídos e o token de concorrência.

📖 **Documentação:** [Tooark.Entities.EntityFrameworkCore no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.entities.entityframeworkcore.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities.EntityFrameworkCore/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Visão Geral](#-visão-geral)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [O que o Interceptor Faz](#-o-que-o-interceptor-faz)
- [Exclusão Lógica](#-exclusão-lógica)
- [Convenções](#-convenções)
- [Tempos de Vida e AddDbContextPool](#-tempos-de-vida-e-adddbcontextpool)
- [Jobs em Segundo Plano](#-jobs-em-segundo-plano)
- [Com o Tooark.Mediator.EntityFrameworkCore](#-com-o-tooarkmediatorentityframeworkcore)
- [Limitações](#-limitações)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Boas Práticas](#-boas-práticas)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 📖 Visão Geral

As entidades do [`Tooark.Entities`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities/README.pt-BR.md) têm campos de auditoria (`CreatedById`, `UpdatedById`, `Version`, `Deleted`, `DeletedById`, …), mas sozinhas nada preenche esses campos quando o contexto salva e nada esconde das consultas as linhas excluídas. Este pacote faz as duas coisas para o Entity Framework Core:

- um interceptor de `SaveChanges` que preenche os campos de criação e de atualização com o usuário atual, chamando os métodos da própria entidade (`SetCreatedBy`, `SetUpdatedBy`);
- o mesmo interceptor converte o `Remove()` em exclusão lógica (`SetDeleted`) nas entidades que a suportam;
- o `ApplyTooarkConventions()` adiciona um filtro de consulta que esconde as linhas excluídas logicamente e marca a `Version` como token de concorrência.

A entidade que a aplicação já carimbou na mesma unidade de trabalho não é carimbada de novo, então a `Version` sobe uma vez por gravação.

O pacote roda no runtime base, sem referência ao ASP.NET Core: funciona do mesmo jeito em uma API, em um worker ou em uma aplicação de console.

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Entities.EntityFrameworkCore
```

---

## ⚙️ Configuração

### Registro

```csharp
using Tooark.Entities.EntityFrameworkCore.Extensions;
using Tooark.Entities.EntityFrameworkCore.Injections;

builder.Services.AddHttpContextAccessor();
builder.Services.AddTooarkEntityFrameworkCore<HttpContextCurrentUser>();

builder.Services.AddDbContext<AppDbContext>((sp, options) => options
  .UseNpgsql(connectionString)
  .AddTooarkInterceptors(sp));
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
  // ... a configuração da própria aplicação ...

  modelBuilder.ApplyTooarkConventions();
}
```

As três chamadas são peças independentes:

| Chamada                                          | O que faz                                                                                       |
| ------------------------------------------------ | ----------------------------------------------------------------------------------------------- |
| `AddTooarkEntityFrameworkCore(configure)`        | Registra as opções e o interceptor. Não registra um `ICurrentUser`                              |
| `AddTooarkEntityFrameworkCore<TUser>(configure)` | O mesmo, e registra o `ICurrentUser` com escopo, a menos que a aplicação já tenha registrado um |
| `AddTooarkInterceptors(sp)`                      | Adiciona o interceptor ao contexto. Use a sobrecarga do `AddDbContext` que recebe o `sp`        |
| `ApplyTooarkConventions()`                       | Adiciona ao modelo o filtro de excluídos e o token de concorrência                              |

### O usuário atual

O `ICurrentUser` informa ao interceptor quem está fazendo a alteração. A implementação é da aplicação, porque a origem do usuário depende do host. Para uma API ASP.NET Core que lê o identificador das claims do token:

```csharp
using System.Security.Claims;
using Tooark.Entities.EntityFrameworkCore.Interfaces;

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
  public Guid? Id
  {
    get
    {
      var user = accessor.HttpContext?.User;
      var value = user?.FindFirst("sub")?.Value
        ?? user?.FindFirst("id")?.Value
        ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      return Guid.TryParse(value, out var id) ? id : null;
    }
  }
}
```

O `Id` devolve `null` quando não há usuário autenticado. O handler de JWT bearer renomeia a `sub` para `ClaimTypes.NameIdentifier`, a menos que `MapInboundClaims` seja `false`, e por isso o exemplo olha as duas.

### Opções

| Opção          | Tipo    | Padrão | Descrição                                                                                                                 |
| -------------- | ------- | ------ | ------------------------------------------------------------------------------------------------------------------------- |
| `SystemUserId` | `Guid?` | `null` | Autor usado quando o `ICurrentUser.Id` é nulo ou vazio, ou quando nenhum `ICurrentUser` está registrado                   |
| `StampAudit`   | `bool`  | `true` | Preenche os campos de criação e de atualização. Desligado, a aplicação chama `SetCreatedBy` e `SetUpdatedBy`              |
| `SoftDelete`   | `bool`  | `true` | Converte o `Remove()` em exclusão lógica. Desligado, o `Remove()` apaga a linha, também nas entidades com exclusão lógica |

```csharp
builder.Services.AddTooarkEntityFrameworkCore<HttpContextCurrentUser>(options =>
{
  options.SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
});
```

As duas chaves são independentes: com `StampAudit = false` e `SoftDelete = true`, o `Remove()` continua registrando quem excluiu, porque é a exclusão lógica que chama o `SetDeleted`.

---

## 🧾 O que o Interceptor Faz

O interceptor roda dentro do `SaveChanges` e do `SaveChangesAsync`, antes do envio dos comandos, e olha o estado de cada entidade rastreada:

| Classe base                   | Adicionada (com `CreatedById` vazio)                   | Modificada (ainda sem carimbo)           | Removida                                                                                     |
| ----------------------------- | ------------------------------------------------------ | ---------------------------------------- | -------------------------------------------------------------------------------------------- |
| `InitialEntity`, `FileEntity` | `CreatedById`, `CreatedAt`                             | nada                                     | a linha é apagada                                                                            |
| `DetailedEntity`              | `CreatedById`, `CreatedAt`, `UpdatedById`, `UpdatedAt` | `UpdatedById`, `UpdatedAt`               | a linha é apagada                                                                            |
| `VersionedEntity`             | igual à `DetailedEntity`; a `Version` fica em 1        | `UpdatedById`, `UpdatedAt`, `Version` +1 | a linha é apagada                                                                            |
| `SoftDeletableEntity`         | igual à `DetailedEntity`                               | `UpdatedById`, `UpdatedAt`               | atualização: `Deleted`, `UpdatedById`, `UpdatedAt`                                           |
| `AuditableEntity`             | igual à `DetailedEntity`; a `Version` fica em 1        | `UpdatedById`, `UpdatedAt`, `Version` +1 | atualização: `Deleted`, `DeletedById`, `DeletedAt`, `UpdatedById`, `UpdatedAt`, `Version` +1 |

As entidades que não descendem das classes base do Tooark passam sem alteração.

### Quem é o autor

1. O `ICurrentUser.Id`, quando tem valor.
2. O `EntityAuditOptions.SystemUserId`, quando o usuário atual é nulo ou `Guid.Empty`, ou quando nenhum `ICurrentUser` está registrado.
3. Nenhum dos dois: a gravação lança `InternalServerErrorException` com `Entities.Audit.CurrentUserRequired` e nada é gravado. Gravar a linha com autor vazio em silêncio seria pior do que falhar.

O usuário é resolvido uma vez por gravação e só quando alguma entidade precisa de autor. A gravação que só mexe em entidades sem campos de auditoria não precisa de usuário.

### Sem carimbo duplo

O `SetUpdatedBy` incrementa a `Version` na `VersionedEntity` e na `AuditableEntity`, e o `SetCreatedBy` lança quando a criação já foi registrada. É comum a aplicação chamar esses métodos no handler, então o interceptor carimba só o que ainda falta:

- **Adicionada:** o `SetCreatedBy` só é chamado quando o `CreatedById` ainda é `Guid.Empty`. A entidade criada com `new Product(createdById)` mantém o autor.
- **Modificada:** o `SetUpdatedBy` só é chamado quando `UpdatedAt`, `UpdatedById` e `Version` ainda têm os valores carregados do banco (ou gravados pelo `SaveChanges` anterior do mesmo contexto). Se algum deles difere, a aplicação já carimbou a entidade nesta unidade de trabalho — com `SetUpdatedBy`, `SetDeleted` ou `SetRestored` — e o interceptor a deixa como está.
- **Sem alteração:** a entidade carregada e não alterada não é carimbada. Atribuir o mesmo valor a uma propriedade não é uma alteração.

O resultado é um incremento da `Version` por `SaveChanges` que alterou a entidade, com ou sem carimbo do handler. Salvar duas vezes no mesmo contexto incrementa uma vez por gravação.

---

## 🧹 Exclusão Lógica

O `Remove()` de uma `SoftDeletableEntity` ou de uma `AuditableEntity` vira uma atualização: o interceptor chama o `SetDeleted` da entidade com o autor e a linha fica no banco com `Deleted = true`. Qualquer outra entidade é apagada de verdade.

```csharp
context.Products.Remove(product);

await context.SaveChangesAsync(cancellationToken); // UPDATE ... SET deleted = TRUE, ...
```

A alteração feita na entidade antes do `Remove()`, na mesma unidade de trabalho, é gravada junto com a exclusão lógica.

### Lendo as linhas excluídas

Com o `ApplyTooarkConventions()`, as consultas não devolvem as linhas excluídas logicamente. Para lê-las:

```csharp
var all = await context.Products.IgnoreQueryFilters().ToListAsync(cancellationToken);
```

### Restaurando

Restaurar é uma atualização comum. Carregue a linha sem os filtros e chame o `SetRestored` da entidade:

```csharp
var product = await context.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == id, cancellationToken);

product.SetRestored(currentUserId);

await context.SaveChangesAsync(cancellationToken);
```

O `SetRestored` já carimba a entidade, então o interceptor não incrementa a `Version` uma segunda vez.

### Removendo uma entidade que já está excluída

O `Remove()` de uma linha que **já está excluída logicamente no banco** apaga a linha de verdade. É o caminho para expurgar uma linha excluída sem desligar a opção:

```csharp
var product = await context.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == id, cancellationToken);

context.Products.Remove(product); // product.Deleted veio como true do banco

await context.SaveChangesAsync(cancellationToken); // DELETE
```

A decisão usa o valor lido do banco, e não o valor em memória: o handler que chama `SetDeleted(...)` e depois `Remove(...)` em uma entidade ativa continua tendo uma exclusão lógica, com um único incremento da `Version`.

### Cascata

Quando a aplicação remove um principal, o Entity Framework Core marca para remoção também os dependentes **carregados** das relações obrigatórias dele. O interceptor decide entidade por entidade:

| Situação                                                                      | Resultado                                                                                                                                         |
| ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Principal com exclusão lógica, dependentes carregados com exclusão lógica     | O principal e os dependentes são excluídos logicamente                                                                                            |
| Principal com exclusão lógica, dependentes carregados sem exclusão lógica     | O principal é excluído logicamente e esses dependentes são **apagados de verdade**                                                                |
| Principal com exclusão lógica, dependentes que **não foram carregados**       | Só o principal é excluído logicamente. Os dependentes continuam ativos: a cascata do banco não dispara, porque a linha do principal não é apagada |
| Principal com exclusão lógica, dependentes carregados de uma relação opcional | O Entity Framework Core grava `null` na chave estrangeira deles quando o principal é removido, embora a linha do principal continue lá            |
| Principal apagado de verdade, dependentes com exclusão lógica                 | Os dependentes das relações obrigatórias também são **apagados de verdade**: o Entity Framework Core reaplica a cascata ao salvar                 |
| Tipos owned de um dono excluído logicamente                                   | Mantidos, com os valores deles                                                                                                                    |
| Dependente obrigatório tirado da coleção do principal (órfão)                 | Excluído logicamente quando suporta, mantendo a chave estrangeira                                                                                 |

O principal é apagado de verdade quando não suporta exclusão lógica, quando já está excluído logicamente ou quando o `SoftDelete` está desligado. O resultado é o mesmo quando o contexto adia a cascata (`ChangeTracker.CascadeDeleteTiming`).

Para excluir logicamente um principal junto com todos os dependentes, carregue-os (`Include`) antes de chamar o `Remove()`.

---

## 📐 Convenções

O `ApplyTooarkConventions()` percorre os tipos de entidade que já estão no modelo e aplica:

| Convenção             | Vale para as descendentes de             | Efeito                                                            |
| --------------------- | ---------------------------------------- | ----------------------------------------------------------------- |
| Filtro de excluídos   | `SoftDeletableEntity`, `AuditableEntity` | Filtro de consulta `!Deleted` na entidade (na raiz da hierarquia) |
| Token de concorrência | `VersionedEntity`, `AuditableEntity`     | A `Version` vira token de concorrência                            |

### Chame por último

Chame o `ApplyTooarkConventions()` no fim do `OnModelCreating`:

- o método só enxerga os tipos de entidade que já estão no modelo;
- um tipo de entidade tem um único filtro de consulta sem nome. Quando a entidade **já tem** um filtro, o método o mantém e combina os dois com E. Um `HasQueryFilter` chamado **depois** do método substitui o filtro de excluídos, e as linhas excluídas voltam a aparecer nas consultas.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
  modelBuilder.Entity<Product>().HasQueryFilter(product => product.Active);

  // Resultado: product.Active && !product.Deleted
  modelBuilder.ApplyTooarkConventions();
}
```

O `IgnoreQueryFilters()` desliga o filtro combinado por inteiro: o da aplicação e o de excluídos.

### Filtros nomeados (Entity Framework Core 10)

O Entity Framework Core 10 tem filtros de consulta nomeados e não aceita um filtro nomeado e um sem nome na mesma entidade. O comportamento é o mesmo nos dois frameworks de destino — um filtro sem nome, combinado com o da aplicação — com uma exceção que só existe no Entity Framework Core 10: quando a entidade já tem filtros **nomeados**, o filtro de excluídos também entra como filtro nomeado, com o nome `ModelBuilderExtensions.SoftDeleteFilterName` (`"Tooark.SoftDelete"`). Assim ele pode ser desligado sozinho:

```csharp
var withDeleted = await context.Products
  .IgnoreQueryFilters([ModelBuilderExtensions.SoftDeleteFilterName])
  .ToListAsync(cancellationToken);
```

### Herança

O Entity Framework Core só aceita filtro de consulta na raiz de uma hierarquia. Quando a raiz tem exclusão lógica, o filtro vai para a raiz e cobre todos os tipos derivados. Um tipo com exclusão lógica abaixo de uma raiz que não a suporta — o que exige mapear uma classe base do próprio Tooark, como a `DetailedEntity`, como raiz — faz o método lançar `Entities.Conventions.SoftDeleteRootNotSupported`, porque não há onde colocar o filtro.

### Tipos owned e sem chave

Os tipos owned são ignorados: eles seguem o dono. Os tipos sem chave recebem o filtro, mas não o token de concorrência.

### Navegações obrigatórias

O dependente que exige um principal com exclusão lógica é carregado com inner join quando o principal é incluído, então some do resultado depois que o principal é excluído logicamente. O Entity Framework Core registra um aviso na validação do modelo para essas relações. Dê ao dependente um filtro equivalente ou torne a navegação opcional.

### Concorrência

Com a `Version` como token de concorrência, cada `UPDATE` e cada `DELETE` leva `WHERE version = <valor lido>`. Quando outro contexto alterou a linha nesse meio-tempo, o `SaveChanges` lança `DbUpdateConcurrencyException` — também na exclusão lógica, que é uma atualização.

A `Version` é incrementada na aplicação, pelo `SetUpdatedBy`. O token só detecta o conflito se algo o incrementa: o interceptor, ou a aplicação quando o `StampAudit` está desligado.

---

## 🔄 Tempos de Vida e AddDbContextPool

O interceptor é registrado como transitório e não guarda estado entre gravações. Ele é criado pelo provedor de serviços passado ao `AddTooarkInterceptors(sp)` e resolve o `ICurrentUser` nesse mesmo provedor a cada gravação. Qual provedor é esse depende de como o contexto é registrado:

| Registro                                                                       | Provedor recebido      | Tempo de vida do `ICurrentUser` que funciona |
| ------------------------------------------------------------------------------ | ---------------------- | -------------------------------------------- |
| `AddDbContext((sp, options) => ...)`                                           | O escopo da requisição | Com escopo, singleton ou transitório         |
| `AddDbContextPool`, `AddDbContextFactory`, `AddDbContext` com opções singleton | O provedor raiz        | Só singleton                                 |

Com as opções criadas no provedor raiz, registre o usuário atual como singleton **antes** do `AddTooarkEntityFrameworkCore`, para que o registro com escopo do pacote não aconteça:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddTooarkEntityFrameworkCore();

builder.Services.AddDbContextPool<AppDbContext>((sp, options) => options
  .UseNpgsql(connectionString)
  .AddTooarkInterceptors(sp));
```

Uma implementação sobre o `IHttpContextAccessor` é segura como singleton: o accessor é singleton e lê a requisição da chamada em curso.

Um `ICurrentUser` com escopo com as opções criadas no provedor raiz é um erro de configuração:

- com a validação de escopos ligada (o padrão do host do ASP.NET Core em Development), a gravação lança `Entities.Audit.CurrentUserNotResolved`;
- com a validação de escopos desligada, o container resolve o serviço com escopo no provedor raiz como uma instância única para a aplicação inteira, que não enxerga o estado da requisição. O pacote não consegue detectar esse caso: mantenha a validação de escopos ligada em desenvolvimento e registre o usuário como singleton.

---

## 🤖 Jobs em Segundo Plano

Um worker, um seeder ou um job agendado não tem usuário autenticado. Defina o `SystemUserId` e o interceptor o usa como autor. O `ICurrentUser` não precisa ser registrado:

```csharp
builder.Services.AddTooarkEntityFrameworkCore(options =>
{
  options.SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
});

builder.Services.AddDbContext<AppDbContext>((sp, options) => options
  .UseNpgsql(connectionString)
  .AddTooarkInterceptors(sp));
```

Em uma aplicação que tem os dois — uma API com serviços hospedados — o usuário atual prevalece quando tem identificador, e o `SystemUserId` cobre o trabalho feito fora de uma requisição.

---

## 🧩 Com o Tooark.Mediator.EntityFrameworkCore

O [`Tooark.Mediator.EntityFrameworkCore`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.EntityFrameworkCore/README.pt-BR.md) leva o `SaveChanges` para o fim do pipeline do comando. O interceptor roda dentro desse `SaveChanges`, então os dois pacotes se combinam sem configuração extra:

```csharp
builder.Services.AddTooarkEntityFrameworkCore<HttpContextCurrentUser>();
builder.Services.AddDbContext<AppDbContext>((sp, options) => options
  .UseNpgsql(connectionString)
  .AddTooarkInterceptors(sp));

builder.Services.AddTooarkMediator(typeof(Program).Assembly);
builder.Services.AddTooarkMediatorUnitOfWork<AppDbContext>();
```

O handler altera a entidade e retorna, sem `SaveChanges` e sem `SetUpdatedBy`:

```csharp
public sealed class RenameProductHandler(AppDbContext context) : ICommandHandler<RenameProduct, Guid>
{
  public async Task<Guid> HandleAsync(RenameProduct request, CancellationToken cancellationToken = default)
  {
    var product = await context.Products.SingleAsync(p => p.Id == request.Id, cancellationToken);

    product.Rename(request.Name);

    // Sem SetUpdatedBy e sem SaveChanges: o pipeline salva e o interceptor carimba
    return product.Id;
  }
}
```

O handler que já chama o `SetUpdatedBy` continua funcionando: o interceptor reconhece o carimbo e a `Version` sobe uma vez. Na estratégia `Transaction`, o handler que salva mais de uma vez recebe um carimbo por gravação que alterou a entidade.

---

## 🚧 Limitações

- **Operações em lote.** O `ExecuteUpdate` e o `ExecuteDelete` não passam pelo `SaveChanges`: não há carimbo, não há exclusão lógica (o `ExecuteDelete` apaga as linhas) e a `Version` não muda.
- **SQL direto.** O filtro de consulta vale para as consultas que o Entity Framework Core monta para a entidade. O SQL enviado direto ao banco enxerga todas as linhas e precisa filtrar a coluna `deleted` por conta própria.
- **Tipos owned e filhos.** A alteração feita só em um tipo owned ou em uma entidade filha não marca o dono como modificado, então o dono não é carimbado. Chame o `SetUpdatedBy` no dono quando a alteração precisa contar como alteração do dono.
- **Entidades desconectadas.** A detecção compara os valores atuais com os originais. A entidade anexada com `Update()` não tem valores originais, então o interceptor a carimba. Não chame o `SetUpdatedBy` antes de anexar: em uma `DetailedEntity` o autor é substituído pelo usuário atual, e em uma entidade com `Version` a gravação falha com `DbUpdateConcurrencyException`, porque a versão tomada como original já não é a que está no banco.
- **Dependentes que não foram carregados.** A exclusão lógica não alcança os dependentes que não estão rastreados (veja [Cascata](#cascata)).
- **Índices únicos.** A linha excluída logicamente continua ocupando os valores dela em um índice único. Use um índice filtrado quando o valor precisa ficar disponível de novo.
- **`FileEntity.Type`.** O `EFileType` é uma classe, e o Entity Framework Core não a mapeia por convenção. O pacote não a configura: `modelBuilder.Entity<Attachment>().Property(a => a.Type).HasConversion(type => type.ToInt(), id => (EFileType)id)`.
- **Multilocação.** O pacote não filtra nem carimba um locatário (tenant).

---

## 📝 Exemplos de Uso

### Entidades

```csharp
using Tooark.Entities;

public sealed class Product : AuditableEntity
{
  private Product() { }

  public Product(string name) => Name = name;

  public string Name { get; private set; } = string.Empty;

  public void Rename(string name) => Name = name;
}
```

### Contexto

```csharp
using Microsoft.EntityFrameworkCore;
using Tooark.Entities.EntityFrameworkCore.Extensions;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<Product> Products => Set<Product>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    modelBuilder.ApplyTooarkConventions();
  }
}
```

### Criar, atualizar e remover

```csharp
// Criada sem autor: o interceptor preenche CreatedById, CreatedAt, UpdatedById e UpdatedAt
var product = new Product("Caneta");

context.Products.Add(product);
await context.SaveChangesAsync(cancellationToken); // Version = 1

// Alterada sem SetUpdatedBy: o interceptor preenche UpdatedById e UpdatedAt
product.Rename("Caneta azul");
await context.SaveChangesAsync(cancellationToken); // Version = 2

// Removida: a linha fica, com Deleted, DeletedById e DeletedAt
context.Products.Remove(product);
await context.SaveChangesAsync(cancellationToken); // Version = 3
```

### Conflito de concorrência

```csharp
try
{
  await context.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateConcurrencyException)
{
  // Outra requisição alterou a linha depois da leitura: recarregue e aplique a alteração de novo, ou devolva 409
}
```

---

## 📋 Dependências

| Pacote                                                                                                                                          | Versão   | Uso                                          |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | -------------------------------------------- |
| [`Tooark.Entities`](https://www.nuget.org/packages/Tooark.Entities)                                                                             | 4.x      | Entidades base e os métodos de auditoria     |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                         | 4.x      | Erros de configuração e de autor ausente     |
| [`Microsoft.EntityFrameworkCore`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore)                                                 | 8.x/10.x | Interceptor, rastreio de alterações e modelo |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Registro no container                        |

O pacote integra o agregador `Tooark`, então quem instala `Tooark` já o recebe.

---

## 🎯 Boas Práticas

- Chame o `ApplyTooarkConventions()` por último no `OnModelCreating`.
- Use a sobrecarga `AddDbContext((sp, options) => ...)`, para que o interceptor receba o escopo da requisição.
- Mantenha a validação de escopos ligada em desenvolvimento: é ela que acusa um `ICurrentUser` com escopo atrás de um contexto em pool.
- Defina o `SystemUserId` em toda aplicação que grava fora de uma requisição, e use um identificador que exista na tabela de usuários quando houver chave estrangeira.
- Carregue os dependentes antes de remover um principal, quando eles precisam ser excluídos logicamente junto com ele.
- Trate a `DbUpdateConcurrencyException` onde entidades com `Version` são atualizadas.
- Não misture operações em lote com entidades que dependem dos campos de auditoria; quando uma atualização em lote for necessária, preencha as colunas de auditoria no mesmo comando.

---

## ⚠️ Códigos de Erro e Soluções

| Mensagem                                                        | Exceção                        | Descrição                                                                                | Solução                                                                                                     |
| --------------------------------------------------------------- | ------------------------------ | ---------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| `Entities.Audit.CurrentUserRequired`                            | `InternalServerErrorException` | A gravação precisa de autor e não há usuário atual nem `SystemUserId`                    | Autentique o usuário, ou defina o `SystemUserId` para o trabalho feito fora de uma requisição               |
| `Entities.Audit.CurrentUserNotResolved`                         | `InternalServerErrorException` | O `ICurrentUser` não pôde ser resolvido no provedor informado ao `AddTooarkInterceptors` | Com contexto em pool ou opções singleton, registre o `ICurrentUser` como singleton. Veja a `InnerException` |
| `Entities.Audit.InterceptorNotRegistered`                       | `InternalServerErrorException` | O `AddTooarkInterceptors` foi chamado sem o registro                                     | Chame o `AddTooarkEntityFrameworkCore` na coleção de serviços                                               |
| `Entities.Conventions.SoftDeleteRootNotSupported;{type};{root}` | `InternalServerErrorException` | Um tipo com exclusão lógica está abaixo de uma raiz de hierarquia que não a suporta      | Não mapeie a classe base do Tooark como raiz, ou faça a hierarquia começar em um tipo com exclusão lógica   |

A `DbUpdateConcurrencyException` vem do Entity Framework Core, quando a linha foi alterada ou apagada por outra pessoa depois da leitura.

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
