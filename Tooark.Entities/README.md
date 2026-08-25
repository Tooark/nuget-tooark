# Tooark.Entities

Biblioteca para gerenciamento e manutenção de entidades base em projetos .NET.

## Conteúdo

- [BaseEntity](#1-entidade-base)
- [InitialEntity](#2-entidade-inicial)
- [DetailedEntity](#3-entidade-detalhada)
- [VersionedEntity](#4-entidade-versionada)
- [SoftDeletableEntity](#5-entidade-deletável)
- [AuditableEntity](#6-entidade-auditável)
- [FileEntity](#7-entidade-de-arquivo)

## Entidades

As entidades disponíveis são:

### 1. Entidade Base

**Funcionalidade:**
Classe base abstrata contemplando a definição de um identificador único para a entidade.

- **Propriedades:**

  - `Id` (Guid): Identificador único para a entidade.

- **Coluna de Banco de Dados:**

  - `id`: Tipo: `uuid`.

- **Métodos:**

  - `SetId(Guid id)`: Define o identificador único para a entidade.

[Exemplo de Uso](#entidade-base)

### 2. Entidade Inicial

**Funcionalidade:**
Classe base abstrata que herda de [`BaseEntity`](#1-entidade-base) e define campos para rastrear a criação da entidade.

- **Propriedades:**

  - `CreatedBy` (Guid): Identificador do usuário que criou a entidade.
  - `CreatedAt` (DateTime): Data e hora de criação da entidade.

- **Coluna de Banco de Dados:**

  - `created_by`: Tipo: `uuid`.
  - `created_at`: Tipo: `timestamp with time zone`.

- **Métodos:**

  - `SetCreatedBy(Guid createdBy)`: Define o identificador do criador da entidade e a data e hora de criação.

### 3. Entidade Detalhada

**Funcionalidade:**
Classe base abstrata que herda de [`InitialEntity`](#2-entidade-inicial) e define campos para rastrear a última atualização da entidade.

- **Propriedades:**

  - `UpdatedBy` (Guid): Identificador do usuário que atualizou a entidade pela última vez.
  - `UpdatedAt` (DateTime): Data e hora da última atualização da entidade.

- **Coluna de Banco de Dados:**

  - `updated_by`: Tipo: `uuid`.
  - `updated_at`: Tipo: `timestamp with time zone`.

- **Métodos:**

  - `SetCreatedBy(Guid createdBy)`: Define o identificador do criador e o atualizador da entidade.
  - `SetUpdatedBy(Guid updatedBy)`: Define o identificador do atualizador da entidade e a data e hora da última atualização.

### 4. Entidade Versionada

**Funcionalidade:**
Classe base abstrata que herda de [`DetailedEntity`](#3-entidade-detalhada) e define um campo para rastrear a versão da entidade.

- **Propriedades:**

  - `Version` (long): Versão da entidade.

- **Coluna de Banco de Dados:**

  - `version`: Tipo: `bigint`.

- **Métodos:**

  - `SetUpdatedBy(Guid updatedBy)`: Incrementa a versão da entidade ao atualizar.

### 5. Entidade Deletável

**Funcionalidade:**
Classe base abstrata que herda de [`DetailedEntity`](#3-entidade-detalhada) e define um campo para rastrear a exclusão lógica da entidade.

- **Propriedades:**

  - `Deleted` (bool): Indica se a entidade foi excluída logicamente.

- **Coluna de Banco de Dados:**

  - `deleted`: Tipo: `boolean`.

- **Métodos:**

  - `SetDeleted(Guid changedBy)`: Marca a entidade como excluída logicamente.
  - `SetRestored(Guid changedBy)`: Marca a entidade como não excluída logicamente.

### 6. Entidade Auditável

**Funcionalidade:**
Classe base abstrata que herda de [`DetailedEntity`](#3-entidade-detalhada) e define campos para rastrear a exclusão lógica e restauração da entidade.

- **Propriedades:**

  - `Version` (long): Versão da entidade.
  - `Deleted` (bool): Indica se a entidade foi excluída logicamente.
  - `DeletedBy` (Guid): Identificador do usuário que excluiu a entidade.
  - `DeletedAt` (DateTime?): Data e hora da exclusão da entidade.
  - `RestoredBy` (Guid): Identificador do usuário que restaurou a entidade.
  - `RestoredAt` (DateTime?): Data e hora da restauração da entidade.

- **Coluna de Banco de Dados:**

  - `deleted`: Tipo: `boolean`.
  - `deleted_by`: Tipo: `uuid`.
  - `deleted_at`: Tipo: `timestamp with time zone`.
  - `restored_by`: Tipo: `uuid`.
  - `restored_at`: Tipo: `timestamp with time zone`.

- **Métodos:**

  - `SetDeleted(Guid deletedBy)`: Marca a entidade como excluída, definindo o usuário e a data e hora da exclusão e incrementando a versão.
  - `SetRestored(Guid restoredBy)`: Marca a entidade como restaurada, definindo o usuário e a data e hora da restauração e incrementando a versão.

### .7 Entidade de Arquivo

**Funcionalidade:**
Classe base abstrata que herda de [`InitialEntity`](#2-entidade-inicial) e define campos para armazenar informações de arquivos.

- **Propriedades:**

  - `FileName` (string): Nome do arquivo.
  - `Title` (string): Título do arquivo.
  - `Link` (string): Link do arquivo no bucket.
  - `FileFormat` (string): Formato do arquivo.
  - `Type` (EFileType): Tipo do arquivo.
  - `Size` (long): Tamanho do arquivo em bytes.

- **Coluna de Banco de Dados:**

  - `file_name`: Tipo: `text`.
  - `title`: Tipo: `varchar(255)`.
  - `link`: Tipo: `text`.
  - `file_format`: Tipo: `varchar(10)`.
  - `type`: Tipo: `int`.
  - `size`: Tipo: `bigint`.

- **Construtores:**

  - `FileEntity(string fileUrl, string name, Guid createdBy)`: Inicializa uma nova instância da classe `FileEntity`.
  - `FileEntity(string fileUrl, string name, string publicUrl, Guid createdBy)`: Inicializa uma nova instância da classe `FileEntity`.
  - `FileEntity(string fileUrl, string name, string publicUrl, string fileFormat, EFileType type, Guid createdBy)`: Inicializa uma nova instância da classe `FileEntity`.

## Exemplo de Uso

### Entidade Base

```csharp
using Tooark.Entities;

public class Produto : BaseEntity
{
  public string Nome { get; set; }
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

    produto.SetId(Guid.NewGuid()); // Define o identificador único para a entidade.
  }
}
```

### Entidade Inicial

```csharp
using Tooark.Entities;

public class Produto : InitialEntity
{
  public string Nome { get; set; }
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

    // Definindo o criador da entidade
    produto.SetCreatedBy(Guid.NewGuid()); // Define o identificador do criador da entidade e a data e hora de criação.
  }
}
```

### Entidade Detalhada

```csharp
using Tooark.Entities;

public class Produto : DetailedEntity
{
  public string Nome { get; set; }
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

    // Definindo o criador da entidade
    produto.SetCreatedBy(Guid.NewGuid()); // Define o identificador do criador e o atualizador da entidade.

    // Atualizando a entidade
    produto.SetUpdatedBy(Guid.NewGuid()); // Define o identificador do atualizador da entidade e a data e hora da última atualização.
  }
}
```

### Entidade Versionada

```csharp
using Tooark.Entities;

public class Produto : VersionedEntity
{
  public string Nome { get; set; }
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

    // Definindo o criador da entidade
    produto.SetCreatedBy(Guid.NewGuid()); // Define o identificador do criador e o atualizador da entidade.

    // Atualizando a entidade
    produto.SetUpdatedBy(Guid.NewGuid()); // Incrementa a versão da entidade ao atualizar.

    var version = produto.Version; // Obtém a versão da entidade.
  }
}
```

### Entidade Deletável

```csharp
using Tooark.Entities;

public class Produto : SoftDeletableEntity
{
  public string Nome { get; set; }
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

    // Definindo o criador da entidade
    produto.SetCreatedBy(Guid.NewGuid()); // Define o identificador do criador e o atualizador da entidade.

    // Excluindo logicamente a entidade
    produto.SetDeleted(Guid.NewGuid()); // Marca a entidade como excluída logicamente.

    // Restaurando a entidade
    produto.SetRestored(Guid.NewGuid()); // Marca a entidade como não excluída logicamente.
  }
}
```

### Entidade Auditável

```csharp
using Tooark.Entities;

public class Produto : AuditableEntity
{
  public string Nome { get; set; }
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

    // Definindo o criador da entidade
    produto.SetCreatedBy(Guid.NewGuid()); // Define o identificador do criador e o atualizador da entidade.

    // Atualizando a entidade
    produto.SetUpdatedBy(Guid.NewGuid()); // Define o identificador do atualizador da entidade e a data e hora da última atualização.

    // Excluindo logicamente a entidade
    produto.SetDeleted(Guid.NewGuid()); // Marca a entidade como excluída, definindo o usuário e a data e hora da exclusão e incrementando a versão.

    // Restaurando a entidade
    produto.SetRestored(Guid.NewGuid()); // Marca a entidade como restaurada, definindo o usuário e a data e hora da restauração e incrementando a versão.
  }
}
```

### Entidade de Arquivo

```csharp
using Tooark.Entities;

public class Arquivo : FileEntity
{
  public string Descricao { get; set; }
}

public class Program
{
  public static void Main()
  {
    var arquivo = new Arquivo("https://bucket.com/arquivo.pdf", "Arquivo.pdf", Guid.NewGuid())
    {
      Descricao = "Arquivo de teste"
    };
  }
}
```

## Dependências

- [Tooark.Enums](../Tooark.Enums/README.md)
- [Tooark.Notifications](../Tooark.Notifications/README.md)
- [Tooark.Utils](../Tooark.Utils/README.md)
- [Tooark.ValueObjects](../Tooark.ValueObjects/README.md)

## Códigos de Erro para notificações

Os códigos de erro para notificações são:

- `Base`: `T.ENT.BAS`
- `Initial`: `T.ENT.INI`
- `Detailed`: `T.ENT.DET`
- `SoftDeletable`: `T.ENT.SOF`
- `Auditable`: `T.ENT.AUD`

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Entities](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
