# Tooark.Entities

Biblioteca com entidades base para aplicações .NET, incluindo suporte a identificadores únicos, auditoria, controle de versão e exclusão lógica.

## 📦 Conteúdo do Pacote

### Entidades

| Classe                                        | Descrição                                                                                                                                         |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`BaseEntity`](#baseentity)                   | Identificador único + suporte a notificações/validações                                                                                           |
| [`InitialEntity`](#initialentity)             | Informações de criação (`CreatedById`/`CreatedAt`)                                                                                                |
| [`DetailedEntity`](#detailedentity)           | Informações de atualização (`UpdatedById`/`UpdatedAt`)                                                                                            |
| [`VersionedEntity`](#versionedentity)         | Controle de versão (`Version`) incrementada em atualizações                                                                                       |
| [`SoftDeletableEntity`](#softdeletableentity) | Exclusão lógica simples (`Deleted`) + atualização via `UpdatedById`                                                                               |
| [`AuditableEntity`](#auditableentity)         | Auditoria completa: versão (`Version`) + exclusão(`Deleted`)/restauração com usuário/data (`DeletedById`/`DeletedAt`/`RestoredById`/`RestoredAt`) |
| [`FileEntity`](#fileentity)                   | Entidade base para arquivos (`FileName`, `Title`, `Link`, `FileFormat`, `Type`, `Size`)                                                           |

### Value Objects usados nas entidades

As entidades recebem Value Objects do pacote `Tooark.ValueObjects` — `CreatedBy`, `UpdatedBy`,
`DeletedBy`, `RestoredBy`, `FileStorage` e `Title` — e guardam o valor já convertido. O que entra é o
objeto de valor (`CreatedBy`); o que a entidade expõe é o `Guid` (`CreatedById`).

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Entities
```

---

## ⚙️ Configuração

Não há configuração adicional.

### Como as operações de escrita reagem a valor inválido

Os métodos `Set*` **lançam `BadRequestException`** e **não deixam notificação na entidade**. Uma chamada
recusada não altera nada: a entidade segue íntegra e a chamada seguinte, se correta, é aceita
normalmente.

```csharp
using Tooark.Entities;
using Tooark.Exceptions;

public static class Exemplo
{
  public static void Excluir(AuditableEntity entidade, Guid usuario)
  {
    try
    {
      // Recusada: o identificador é vazio
      entidade.SetDeleted(Guid.Empty);
    }
    catch (BadRequestException)
    {
      // A entidade não guardou nada da chamada recusada e segue íntegra
    }

    // Aceita normalmente
    entidade.SetDeleted(usuario);
  }
}
```

A exceção do valor ausente é diferente da do valor inválido: `null` produz `Field.Required;<campo>`, e
valor presente mas reprovado produz `Field.Invalid;<campo>`.

O `SetId` do `BaseEntity` é a única exceção: ele **acumula notificação** em vez de lançar, porque roda
no construtor. Verifique `IsValid` depois de construir uma entidade com identificador informado.

Já `ValidateNotDeleted()` acumula notificação de propósito — é o método para checar sem interromper o
fluxo. Quem quer interromper usa `EnsureNotDeleted()`, que lança.

---

## 🧩 Entidades (Detalhes)

### BaseEntity

- **Propriedades**
  - `Id` (Guid) — coluna `id` (`uuid`)
- **Construtores (para classes derivadas)**
  - `BaseEntity()` — gera `Id` automaticamente
  - `BaseEntity(Guid id)` — define `Id` determinístico (seed/testes/factories)
- **Observações**
  - O `Id` tem setter privado. Classes derivadas definem o identificador pelo `SetId` protegido, que
    recusa `Guid.Empty` e recusa trocar a identidade de uma entidade já criada — nos dois casos por
    notificação, sem lançar.
  - A igualdade compara **tipo e identificador**: entidades de tipos diferentes com o mesmo `Id` não
    são iguais. A comparação aceita herança nos dois sentidos, para não quebrar com os proxies de
    carregamento tardio do Entity Framework.
  - [Exemplos de Uso](#entidade-base).

### InitialEntity

- **Propriedades**
  - `CreatedById` (Guid) — coluna `created_by` (`uuid`)
  - `CreatedAt` (DateTime/UTC) — coluna `created_at` (`timestamp with time zone`)
- **Métodos**
  - `SetCreatedBy(CreatedBy createdById)`
- **Observações**
  - Herda de `BaseEntity`.
  - `SetCreatedBy` recebe o Value Object `CreatedBy`, que aceita conversão implícita a partir de `Guid`.
  - Em caso de dados inválidos, lança `BadRequestException`.
  - [Exemplos de Uso](#entidade-inicial).

### DetailedEntity

- **Propriedades**
  - `UpdatedById` (Guid) — coluna `updated_by` (`uuid`)
  - `UpdatedAt` (DateTime/UTC) — coluna `updated_at` (`timestamp with time zone`)
- **Métodos**
  - `SetCreatedBy(CreatedBy createdById)` — define também `UpdatedById` e `UpdatedAt`, iguais aos da criação
  - `SetUpdatedBy(UpdatedBy updatedById)`
- **Observações**
  - Herda de `InitialEntity`.
  - `SetUpdatedBy` recebe o Value Object `UpdatedBy`, que aceita conversão implícita a partir de `Guid`.
  - Em caso de dados inválidos, lança `BadRequestException`.
  - [Exemplos de Uso](#entidade-detalhada).

### VersionedEntity

- **Propriedades**
  - `Version` (long) — coluna `version` (`bigint`), valor padrão `1`
- **Métodos**
  - `SetUpdatedBy(UpdatedBy updatedById)` — atualiza e incrementa a versão
- **Observações**
  - Herda de `DetailedEntity`.
  - Em caso de dados inválidos, lança `BadRequestException`.
  - [Exemplos de Uso](#entidade-versionada).

### SoftDeletableEntity

- **Propriedades**
  - `Deleted` (bool) — coluna `deleted` (`bool`), valor padrão `false`
- **Métodos**
  - `ValidateNotDeleted()` — valida se não está deletada e adiciona notificação
  - `EnsureNotDeleted()` — lança exception se estiver deletada
  - `SetDeleted(UpdatedBy changedById)` — marca como deletada e atualiza; ignorada se já estiver deletada
  - `SetRestored(UpdatedBy changedById)` — restaura e atualiza; ignorada se não estiver deletada
- **Observações**
  - Herda de `DetailedEntity`.
  - Em caso de dados inválidos, lança `BadRequestException`.
  - [Exemplos de Uso](#entidade-deletável).

### AuditableEntity

- **Propriedades**
  - `Version` (long)
  - `Deleted` (bool)
  - `DeletedById` (Guid?) — coluna `deleted_by` (`uuid`)
  - `DeletedAt` (DateTime?) — coluna `deleted_at` (`timestamp with time zone`)
  - `RestoredById` (Guid?) — coluna `restored_by` (`uuid`)
  - `RestoredAt` (DateTime?) — coluna `restored_at` (`timestamp with time zone`)
- **Métodos**
  - `ValidateNotDeleted()` — valida se não está deletada e adiciona notificação
  - `EnsureNotDeleted()` — lança exception se estiver deletada
  - `SetUpdatedBy(UpdatedBy updatedById)` — atualiza e incrementa a versão
  - `SetDeleted(DeletedBy deletedById)` — marca como deletada, registra o usuário e a data da exclusão, atualiza `UpdatedById`/`UpdatedAt` e incrementa a versão
  - `SetRestored(RestoredBy restoredById)` — restaura, registra o usuário e a data da restauração, atualiza `UpdatedById`/`UpdatedAt` e incrementa a versão
- **Observações**
  - Herda de `DetailedEntity`.
  - Em caso de dados inválidos, lança `BadRequestException`.
  - [Exemplos de Uso](#entidade-auditável).

### FileEntity

- **Propriedades**
  - `FileName` (string) — coluna `file_name` (`text`)
  - `Title` (string) — coluna `title` (`varchar(255)`)
  - `Link` (string) — coluna `link` (`text`)
  - `FileFormat` (string?) — coluna `file_format` (`varchar(10)`)
  - `Type` (EFileType) — coluna `type` (`int`)
  - `Size` (long) — coluna `size` (`bigint`)
- **Construtores (para classes derivadas)**
  - `FileEntity(FileStorage file, Title title, CreatedBy createdById)`
  - `FileEntity(FileStorage file, Title title, string fileFormat, EFileType type, long size, CreatedBy createdById)`
- **Observações**
  - Herda de `InitialEntity`.
  - `FileStorage` e `Title` são Value Objects. Em caso de dados inválidos, lança `BadRequestException`.
  - [Exemplos de Uso](#entidade-de-arquivo).

---

## 📝 Exemplos de Uso

### [Entidade Base](#baseentity)

```csharp
using Tooark.Entities;

public class Produto : BaseEntity
{
  public Produto() { }
  public Produto(Guid id) : base(id) { }

  public string Nome { get; set; } = string.Empty;
  public decimal Valor { get; set; }
}

public class Program
{
  public static void Main()
  {
    var produto = new Produto
    {
      Nome = "Produto A",
      Valor = 100.0m
    };

    var idGerado = produto.Id;
    var produtoDeterministico = new Produto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
  }
}
```

### [Entidade Inicial](#initialentity)

```csharp
using Tooark.Entities;

public class Produto : InitialEntity
{
  public string Nome { get; set; } = string.Empty;
  public decimal Valor { get; set; }
}

public class Program
{
  public static void Main()
  {
    var produto = new Produto
    {
      Nome = "Produto A",
      Valor = 100.0m
    };

    // SetCreatedBy recebe CreatedBy (Value Object), mas Guid converte implicitamente.
    produto.SetCreatedBy(Guid.NewGuid());
  }
}
```

### [Entidade Detalhada](#detailedentity)

```csharp
using Tooark.Entities;

public class Produto : DetailedEntity
{
  public string Nome { get; set; } = string.Empty;
  public decimal Valor { get; set; }
}

public class Program
{
  public static void Main()
  {
    var produto = new Produto
    {
      Nome = "Produto A",
      Valor = 100.0m
    };

    produto.SetCreatedBy(Guid.NewGuid());
    produto.SetUpdatedBy(Guid.NewGuid());
  }
}
```

### [Entidade Versionada](#versionedentity)

```csharp
using Tooark.Entities;

public class Produto : VersionedEntity
{
  public string Nome { get; set; } = string.Empty;
  public decimal Valor { get; set; }
}

public class Program
{
  public static void Main()
  {
    var produto = new Produto
    {
      Nome = "Produto A",
      Valor = 100.0m
    };

    produto.SetCreatedBy(Guid.NewGuid());
    produto.SetUpdatedBy(Guid.NewGuid());

    var version = produto.Version;
  }
}
```

### [Entidade Deletável](#softdeletableentity)

```csharp
using Tooark.Entities;

public class Produto : SoftDeletableEntity
{
  public string Nome { get; set; } = string.Empty;
  public decimal Valor { get; set; }
}

public class Program
{
  public static void Main()
  {
    var produto = new Produto
    {
      Nome = "Produto A",
      Valor = 100.0m
    };

    produto.SetCreatedBy(Guid.NewGuid());
    produto.SetDeleted(Guid.NewGuid());
    produto.SetRestored(Guid.NewGuid());
  }
}
```

### [Entidade Auditável](#auditableentity)

```csharp
using Tooark.Entities;

public class Produto : AuditableEntity
{
  public string Nome { get; set; } = string.Empty;
  public decimal Valor { get; set; }
}

public class Program
{
  public static void Main()
  {
    var produto = new Produto
    {
      Nome = "Produto A",
      Valor = 100.0m
    };

    produto.SetCreatedBy(Guid.NewGuid());
    produto.SetUpdatedBy(Guid.NewGuid());
    produto.SetDeleted(Guid.NewGuid());
    produto.SetRestored(Guid.NewGuid());
  }
}
```

### [Entidade de Arquivo](#fileentity)

```csharp
using Tooark.Entities;
using Tooark.Enums;
using Tooark.ValueObjects;

public class Arquivo : FileEntity
{
  public Arquivo(string link, string name, string title, Guid createdById)
    : base(new FileStorage(link, name), new Title(title), new CreatedBy(createdById))
  { }

  public Arquivo(string link, string name, string title, string fileFormat, EFileType type, long size, Guid createdById)
    : base(new FileStorage(link, name), new Title(title), fileFormat, type, size, createdById)
  { }
}

public class Program
{
  public static void Main()
  {
    var arquivo = new Arquivo(
      link: "https://bucket.com/arquivo.pdf",
      name: "Arquivo.pdf",
      title: "Arquivo de teste",
      createdById: Guid.NewGuid()
    );

    var arquivoDetalhado = new Arquivo(
      link: "https://bucket.com/arquivo.pdf",
      name: "Arquivo.pdf",
      title: "Arquivo de teste",
      fileFormat: "pdf",
      type: EFileType.Document,
      size: 1024,
      createdById: Guid.NewGuid()
    );
  }
}
```

---

## 📋 Dependências

| Pacote                                                                                       | Versão | Descrição                                                 |
| -------------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------- |
| [`Tooark.Enums`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Enums)                 | 4.x    | Tipos compartilhados, como o `EFileType`                  |
| [`Tooark.Exceptions`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Exceptions)       | 4.x    | `BadRequestException`, lançada pelas operações de escrita |
| [`Tooark.Notifications`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Notifications) | 4.x    | Base de notificações das entidades                        |
| [`Tooark.Validations`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Validations)     | 4.x    | Regras usadas na validação do `FileEntity`                |
| [`Tooark.ValueObjects`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.ValueObjects)   | 4.x    | Objetos de valor recebidos pelos construtores e métodos   |

---

## ⚠️ Códigos de Erro, Notificações e Soluções

Os códigos de erro para notificações seguem o padrão `T.ENT.<SIGLA><N>` (ex.: `T.ENT.BAS1`).

Códigos emitidos pelas entidades:

| Código       | Emissor                                  | Situação                            |
| ------------ | ---------------------------------------- | ----------------------------------- |
| `T.ENT.BAS1` | `BaseEntity.SetId`                       | Identificador vazio                 |
| `T.ENT.BAS2` | `BaseEntity.SetId`                       | Tentativa de trocar o identificador |
| `T.ENT.INI1` | `InitialEntity.SetCreatedBy`             | Autoria da criação já registrada    |
| `T.ENT.SOF1` | `SoftDeletableEntity.ValidateNotDeleted` | Registro excluído logicamente       |
| `T.ENT.AUD1` | `AuditableEntity.ValidateNotDeleted`     | Registro excluído logicamente       |

O código acompanha a notificação, esteja ela registrada na entidade ou transportada pela exceção. As
mensagens que vêm dos objetos de valor — `Field.Invalid;CreatedBy` e as demais — são emitidas pelo
`Tooark.ValueObjects` e carregam o código dele, não um `T.ENT.*`. Repare que o link reprovado do
`FileEntity` reporta `ProtocolHttp`, que é o objeto de valor interno do `FileStorage`, e não
`FileStorage`.

Tabela de erros/notificações:

| Entidade              | Mensagem                        | Descrição                           | Solução                                                                  | Retorno      |
| --------------------- | ------------------------------- | ----------------------------------- | ------------------------------------------------------------------------ | ------------ |
| `BaseEntity`          | `Field.Empty;Id`                | Identificador vazio                 | Defina um identificador válido para a entidade                           | Notification |
| `BaseEntity`          | `Field.ChangeBlocked;Id`        | Identificador não pode ser alterado | Não troque o identificador de uma entidade já criada                     | Notification |
| `InitialEntity`       | `Field.ChangeBlocked;CreatedBy` | Criador já registrado               | A autoria da criação é definida uma única vez; não a redefina            | Exception    |
| `InitialEntity`       | `Field.Invalid;CreatedBy`       | Campo do Criador inválido           | Informe um criador válido                                                | Exception    |
| `DetailedEntity`      | `Field.ChangeBlocked;CreatedBy` | Criador já registrado               | A autoria da criação é definida uma única vez; não a redefina            | Exception    |
| `DetailedEntity`      | `Field.Invalid;CreatedBy`       | Campo do Criador inválido           | Informe um criador válido                                                | Exception    |
| `DetailedEntity`      | `Field.Invalid;UpdatedBy`       | Campo do Atualizador inválido       | Informe um atualizador válido                                            | Exception    |
| `VersionedEntity`     | `Field.ChangeBlocked;CreatedBy` | Criador já registrado               | A autoria da criação é definida uma única vez; não a redefina            | Exception    |
| `VersionedEntity`     | `Field.Invalid;CreatedBy`       | Campo do Criador inválido           | Informe um criador válido                                                | Exception    |
| `VersionedEntity`     | `Field.Invalid;UpdatedBy`       | Campo do Atualizador inválido       | Informe um atualizador válido                                            | Exception    |
| `SoftDeletableEntity` | `Field.ChangeBlocked;CreatedBy` | Criador já registrado               | A autoria da criação é definida uma única vez; não a redefina            | Exception    |
| `SoftDeletableEntity` | `Field.Invalid;CreatedBy`       | Campo do Criador inválido           | Informe um criador válido                                                | Exception    |
| `SoftDeletableEntity` | `Field.Invalid;UpdatedBy`       | Campo do Atualizador inválido       | Informe um atualizador válido                                            | Exception    |
| `SoftDeletableEntity` | `Record.Deleted`                | Registro deletado                   | Análise se é necessário restaurar o registro antes de realizar operações | Notification |
| `SoftDeletableEntity` | `Record.Deleted`                | Registro deletado                   | Restaure o registro se necessário antes de realizar operações            | Exception    |
| `AuditableEntity`     | `Field.ChangeBlocked;CreatedBy` | Criador já registrado               | A autoria da criação é definida uma única vez; não a redefina            | Exception    |
| `AuditableEntity`     | `Field.Invalid;CreatedBy`       | Campo do Criador inválido           | Informe um criador válido                                                | Exception    |
| `AuditableEntity`     | `Field.Invalid;UpdatedBy`       | Campo do Atualizador inválido       | Informe um atualizador válido                                            | Exception    |
| `AuditableEntity`     | `Field.Invalid;DeletedBy`       | Campo do Deletador inválido         | Informe um deletador válido                                              | Exception    |
| `AuditableEntity`     | `Field.Invalid;RestoredBy`      | Campo do Restaurador inválido       | Informe um restaurador válido                                            | Exception    |
| `AuditableEntity`     | `Record.Deleted`                | Registro deletado                   | Análise se é necessário restaurar o registro antes de realizar operações | Notification |
| `AuditableEntity`     | `Record.Deleted`                | Registro deletado                   | Restaure o registro se necessário antes de realizar operações            | Exception    |
| `FileEntity`          | `Field.Invalid;ProtocolHttp`    | Link do arquivo inválido            | Informe um link HTTP ou HTTPS válido                                     | Exception    |
| `FileEntity`          | `Field.Invalid;Title`           | Título do arquivo inválido          | Informe um título válido                                                 | Exception    |
| `FileEntity`          | `Field.Required;FileStorage`    | Arquivo não informado               | Informe o `FileStorage` do arquivo                                       | Exception    |
| `FileEntity`          | `Field.Required;Title`          | Título não informado                | Informe o `Title` do arquivo                                             | Exception    |
| `FileEntity`          | `Field.Required;FileFormat`     | Formato do arquivo não informado    | Informe o formato do arquivo                                             | Exception    |
| `FileEntity`          | `Field.Invalid;Size`            | Tamanho do arquivo negativo         | Informe um tamanho maior ou igual a zero                                 | Exception    |

Vale para todas as entidades: argumento **nulo** produz `Field.Required;<campo>` — `CreatedBy`,
`UpdatedBy`, `DeletedBy` ou `RestoredBy`, conforme a operação — como `Exception`.

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Entities](https://github.com/Tooark/tooark-cs/issues).

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/tooark-cs/refs/heads/main/LICENSE) para mais detalhes.
