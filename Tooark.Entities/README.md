# Tooark.Entities

Library with base entities for .NET applications, including support for unique identifiers, auditing, versioning and soft delete.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities/README.pt-BR.md)

## 📦 Package Contents

### Entities

| Class                                         | Description                                                                                                                                  |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| [`BaseEntity`](#baseentity)                   | Unique identifier + notification/validation support                                                                                          |
| [`InitialEntity`](#initialentity)             | Creation information (`CreatedById`/`CreatedAt`)                                                                                             |
| [`DetailedEntity`](#detailedentity)           | Update information (`UpdatedById`/`UpdatedAt`)                                                                                               |
| [`VersionedEntity`](#versionedentity)         | Versioning (`Version`) incremented on updates                                                                                                |
| [`SoftDeletableEntity`](#softdeletableentity) | Simple soft delete (`Deleted`) + update through `UpdatedById`                                                                                |
| [`AuditableEntity`](#auditableentity)         | Full auditing: version (`Version`) + deletion (`Deleted`)/restoration with user/date (`DeletedById`/`DeletedAt`/`RestoredById`/`RestoredAt`) |
| [`FileEntity`](#fileentity)                   | Base entity for files (`FileName`, `Title`, `Link`, `FileFormat`, `Type`, `Size`)                                                            |

### Value Objects used by the entities

The entities receive Value Objects from the `Tooark.ValueObjects` package — `CreatedBy`, `UpdatedBy`,
`DeletedBy`, `RestoredBy`, `FileStorage` and `Title` — and keep the already converted value. What goes in
is the value object (`CreatedBy`); what the entity exposes is the `Guid` (`CreatedById`).

---

## 🔧 Installation

```bash
dotnet add package Tooark.Entities
```

---

## ⚙️ Configuration

There is no additional configuration.

### How write operations react to an invalid value

The `Set*` methods **throw `BadRequestException`** and **leave no notification on the entity**. A rejected
call changes nothing: the entity stays intact and the next call, if correct, is accepted normally.

```csharp
using Tooark.Entities;
using Tooark.Exceptions;

public static class Example
{
  public static void Delete(AuditableEntity entity, Guid user)
  {
    try
    {
      // Rejected: the identifier is empty
      entity.SetDeleted(Guid.Empty);
    }
    catch (BadRequestException)
    {
      // The entity kept nothing from the rejected call and stays intact
    }

    // Accepted normally
    entity.SetDeleted(user);
  }
}
```

The exception for a missing value differs from the one for an invalid value: `null` produces
`Field.Required;<field>`, and a value that is present but rejected produces `Field.Invalid;<field>`.

`BaseEntity.SetId` is the only exception: it **accumulates a notification** instead of throwing, because it
runs in the constructor. Check `IsValid` after building an entity with a provided identifier.

`ValidateNotDeleted()`, on the other hand, accumulates a notification on purpose — it is the method to check
without interrupting the flow. To interrupt, use `EnsureNotDeleted()`, which throws.

---

## 🧩 Entities (Details)

### BaseEntity

- **Properties**
  - `Id` (Guid) — column `id` (`uuid`)
- **Constructors (for derived classes)**
  - `BaseEntity()` — generates `Id` automatically
  - `BaseEntity(Guid id)` — sets a deterministic `Id` (seed/tests/factories)
- **Notes**
  - `Id` has a private setter. Derived classes set the identifier through the protected `SetId`, which
    rejects `Guid.Empty` and rejects changing the identity of an already created entity — in both cases by
    notification, without throwing.
  - Equality compares **type and identifier**: entities of different types with the same `Id` are not
    equal. The comparison accepts inheritance in both directions, so it does not break with Entity
    Framework lazy-loading proxies.
  - [Usage Examples](#base-entity).

### InitialEntity

- **Properties**
  - `CreatedById` (Guid) — column `created_by` (`uuid`)
  - `CreatedAt` (DateTime/UTC) — column `created_at` (`timestamp with time zone`)
- **Methods**
  - `SetCreatedBy(CreatedBy createdById)`
- **Notes**
  - Inherits from `BaseEntity`.
  - `SetCreatedBy` receives the `CreatedBy` Value Object, which accepts implicit conversion from `Guid`.
  - On invalid data, throws `BadRequestException`.
  - [Usage Examples](#initial-entity).

### DetailedEntity

- **Properties**
  - `UpdatedById` (Guid) — column `updated_by` (`uuid`)
  - `UpdatedAt` (DateTime/UTC) — column `updated_at` (`timestamp with time zone`)
- **Methods**
  - `SetCreatedBy(CreatedBy createdById)` — also sets `UpdatedById` and `UpdatedAt`, equal to the creation ones
  - `SetUpdatedBy(UpdatedBy updatedById)`
- **Notes**
  - Inherits from `InitialEntity`.
  - `SetUpdatedBy` receives the `UpdatedBy` Value Object, which accepts implicit conversion from `Guid`.
  - On invalid data, throws `BadRequestException`.
  - [Usage Examples](#detailed-entity).

### VersionedEntity

- **Properties**
  - `Version` (long) — column `version` (`bigint`), default value `1`
- **Methods**
  - `SetUpdatedBy(UpdatedBy updatedById)` — updates and increments the version
- **Notes**
  - Inherits from `DetailedEntity`.
  - On invalid data, throws `BadRequestException`.
  - [Usage Examples](#versioned-entity).

### SoftDeletableEntity

- **Properties**
  - `Deleted` (bool) — column `deleted` (`bool`), default value `false`
- **Methods**
  - `ValidateNotDeleted()` — validates that it is not deleted and adds a notification
  - `EnsureNotDeleted()` — throws an exception if it is deleted
  - `SetDeleted(UpdatedBy changedById)` — marks as deleted and updates; ignored if already deleted
  - `SetRestored(UpdatedBy changedById)` — restores and updates; ignored if not deleted
- **Notes**
  - Inherits from `DetailedEntity`.
  - On invalid data, throws `BadRequestException`.
  - [Usage Examples](#soft-deletable-entity).

### AuditableEntity

- **Properties**
  - `Version` (long)
  - `Deleted` (bool)
  - `DeletedById` (Guid?) — column `deleted_by` (`uuid`)
  - `DeletedAt` (DateTime?) — column `deleted_at` (`timestamp with time zone`)
  - `RestoredById` (Guid?) — column `restored_by` (`uuid`)
  - `RestoredAt` (DateTime?) — column `restored_at` (`timestamp with time zone`)
- **Methods**
  - `ValidateNotDeleted()` — validates that it is not deleted and adds a notification
  - `EnsureNotDeleted()` — throws an exception if it is deleted
  - `SetUpdatedBy(UpdatedBy updatedById)` — updates and increments the version
  - `SetDeleted(DeletedBy deletedById)` — marks as deleted, records the user and date of the deletion, updates `UpdatedById`/`UpdatedAt` and increments the version
  - `SetRestored(RestoredBy restoredById)` — restores, records the user and date of the restoration, updates `UpdatedById`/`UpdatedAt` and increments the version
- **Notes**
  - Inherits from `DetailedEntity`.
  - On invalid data, throws `BadRequestException`.
  - [Usage Examples](#auditable-entity).

### FileEntity

- **Properties**
  - `FileName` (string) — column `file_name` (`text`)
  - `Title` (string) — column `title` (`varchar(255)`)
  - `Link` (string) — column `link` (`text`)
  - `FileFormat` (string?) — column `file_format` (`varchar(10)`)
  - `Type` (EFileType) — column `type` (`int`)
  - `Size` (long) — column `size` (`bigint`)
- **Constructors (for derived classes)**
  - `FileEntity(FileStorage file, Title title, CreatedBy createdById)`
  - `FileEntity(FileStorage file, Title title, string fileFormat, EFileType type, long size, CreatedBy createdById)`
- **Notes**
  - Inherits from `InitialEntity`.
  - `FileStorage` and `Title` are Value Objects. On invalid data, throws `BadRequestException`.
  - [Usage Examples](#file-entity).

---

## 📝 Usage Examples

### [Base Entity](#baseentity)

```csharp
using Tooark.Entities;

public class Product : BaseEntity
{
  public Product() { }
  public Product(Guid id) : base(id) { }

  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
}

public class Program
{
  public static void Main()
  {
    var product = new Product
    {
      Name = "Product A",
      Price = 100.0m
    };

    var generatedId = product.Id;
    var deterministicProduct = new Product(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
  }
}
```

### [Initial Entity](#initialentity)

```csharp
using Tooark.Entities;

public class Product : InitialEntity
{
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
}

public class Program
{
  public static void Main()
  {
    var product = new Product
    {
      Name = "Product A",
      Price = 100.0m
    };

    // SetCreatedBy receives CreatedBy (Value Object), but Guid converts implicitly.
    product.SetCreatedBy(Guid.NewGuid());
  }
}
```

### [Detailed Entity](#detailedentity)

```csharp
using Tooark.Entities;

public class Product : DetailedEntity
{
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
}

public class Program
{
  public static void Main()
  {
    var product = new Product
    {
      Name = "Product A",
      Price = 100.0m
    };

    product.SetCreatedBy(Guid.NewGuid());
    product.SetUpdatedBy(Guid.NewGuid());
  }
}
```

### [Versioned Entity](#versionedentity)

```csharp
using Tooark.Entities;

public class Product : VersionedEntity
{
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
}

public class Program
{
  public static void Main()
  {
    var product = new Product
    {
      Name = "Product A",
      Price = 100.0m
    };

    product.SetCreatedBy(Guid.NewGuid());
    product.SetUpdatedBy(Guid.NewGuid());

    var version = product.Version;
  }
}
```

### [Soft Deletable Entity](#softdeletableentity)

```csharp
using Tooark.Entities;

public class Product : SoftDeletableEntity
{
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
}

public class Program
{
  public static void Main()
  {
    var product = new Product
    {
      Name = "Product A",
      Price = 100.0m
    };

    product.SetCreatedBy(Guid.NewGuid());
    product.SetDeleted(Guid.NewGuid());
    product.SetRestored(Guid.NewGuid());
  }
}
```

### [Auditable Entity](#auditableentity)

```csharp
using Tooark.Entities;

public class Product : AuditableEntity
{
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
}

public class Program
{
  public static void Main()
  {
    var product = new Product
    {
      Name = "Product A",
      Price = 100.0m
    };

    product.SetCreatedBy(Guid.NewGuid());
    product.SetUpdatedBy(Guid.NewGuid());
    product.SetDeleted(Guid.NewGuid());
    product.SetRestored(Guid.NewGuid());
  }
}
```

### [File Entity](#fileentity)

```csharp
using Tooark.Entities;
using Tooark.Enums;
using Tooark.ValueObjects;

public class Attachment : FileEntity
{
  public Attachment(string link, string name, string title, Guid createdById)
    : base(new FileStorage(link, name), new Title(title), new CreatedBy(createdById))
  { }

  public Attachment(string link, string name, string title, string fileFormat, EFileType type, long size, Guid createdById)
    : base(new FileStorage(link, name), new Title(title), fileFormat, type, size, createdById)
  { }
}

public class Program
{
  public static void Main()
  {
    var attachment = new Attachment(
      link: "https://bucket.com/file.pdf",
      name: "File.pdf",
      title: "Test file",
      createdById: Guid.NewGuid()
    );

    var detailedAttachment = new Attachment(
      link: "https://bucket.com/file.pdf",
      name: "File.pdf",
      title: "Test file",
      fileFormat: "pdf",
      type: EFileType.Document,
      size: 1024,
      createdById: Guid.NewGuid()
    );
  }
}
```

---

## 📋 Dependencies

| Package                                                                       | Version | Description                                            |
| ----------------------------------------------------------------------------- | ------- | ------------------------------------------------------ |
| [`Tooark.Enums`](https://www.nuget.org/packages/Tooark.Enums)                 | 4.x     | Shared types, such as `EFileType`                      |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)       | 4.x     | `BadRequestException`, thrown by the write operations  |
| [`Tooark.Notifications`](https://www.nuget.org/packages/Tooark.Notifications) | 4.x     | Notification base of the entities                      |
| [`Tooark.Validations`](https://www.nuget.org/packages/Tooark.Validations)     | 4.x     | Rules used in the `FileEntity` validation              |
| [`Tooark.ValueObjects`](https://www.nuget.org/packages/Tooark.ValueObjects)   | 4.x     | Value objects received by the constructors and methods |

---

## ⚠️ Error Codes, Notifications and Solutions

Notification error codes follow the `T.ENT.<ABBR><N>` pattern (e.g. `T.ENT.BAS1`).

Codes emitted by the entities:

| Code         | Emitter                                  | Situation                            |
| ------------ | ---------------------------------------- | ------------------------------------ |
| `T.ENT.BAS1` | `BaseEntity.SetId`                       | Empty identifier                     |
| `T.ENT.BAS2` | `BaseEntity.SetId`                       | Attempt to change the identifier     |
| `T.ENT.INI1` | `InitialEntity.SetCreatedBy`             | Creation authorship already recorded |
| `T.ENT.SOF1` | `SoftDeletableEntity.ValidateNotDeleted` | Record soft deleted                  |
| `T.ENT.AUD1` | `AuditableEntity.ValidateNotDeleted`     | Record soft deleted                  |

The code travels with the notification, whether it is recorded on the entity or carried by the exception.
Messages coming from the value objects — `Field.Invalid;CreatedBy` and the others — are emitted by
`Tooark.ValueObjects` and carry its code, not a `T.ENT.*` one. Note that a rejected `FileEntity` link
reports `ProtocolHttp`, which is the internal value object of `FileStorage`, not `FileStorage`.

Error/notification table:

| Entity                | Message                         | Description                  | Solution                                                            | Returns      |
| --------------------- | ------------------------------- | ---------------------------- | ------------------------------------------------------------------- | ------------ |
| `BaseEntity`          | `Field.Empty;Id`                | Empty identifier             | Set a valid identifier for the entity                               | Notification |
| `BaseEntity`          | `Field.ChangeBlocked;Id`        | Identifier cannot be changed | Do not change the identifier of an already created entity           | Notification |
| `InitialEntity`       | `Field.ChangeBlocked;CreatedBy` | Creator already recorded     | Creation authorship is set once; do not redefine it                 | Exception    |
| `InitialEntity`       | `Field.Invalid;CreatedBy`       | Invalid Creator field        | Provide a valid creator                                             | Exception    |
| `DetailedEntity`      | `Field.ChangeBlocked;CreatedBy` | Creator already recorded     | Creation authorship is set once; do not redefine it                 | Exception    |
| `DetailedEntity`      | `Field.Invalid;CreatedBy`       | Invalid Creator field        | Provide a valid creator                                             | Exception    |
| `DetailedEntity`      | `Field.Invalid;UpdatedBy`       | Invalid Updater field        | Provide a valid updater                                             | Exception    |
| `VersionedEntity`     | `Field.ChangeBlocked;CreatedBy` | Creator already recorded     | Creation authorship is set once; do not redefine it                 | Exception    |
| `VersionedEntity`     | `Field.Invalid;CreatedBy`       | Invalid Creator field        | Provide a valid creator                                             | Exception    |
| `VersionedEntity`     | `Field.Invalid;UpdatedBy`       | Invalid Updater field        | Provide a valid updater                                             | Exception    |
| `SoftDeletableEntity` | `Field.ChangeBlocked;CreatedBy` | Creator already recorded     | Creation authorship is set once; do not redefine it                 | Exception    |
| `SoftDeletableEntity` | `Field.Invalid;CreatedBy`       | Invalid Creator field        | Provide a valid creator                                             | Exception    |
| `SoftDeletableEntity` | `Field.Invalid;UpdatedBy`       | Invalid Updater field        | Provide a valid updater                                             | Exception    |
| `SoftDeletableEntity` | `Record.Deleted`                | Record deleted               | Consider whether the record must be restored before operating on it | Notification |
| `SoftDeletableEntity` | `Record.Deleted`                | Record deleted               | Restore the record if needed before operating on it                 | Exception    |
| `AuditableEntity`     | `Field.ChangeBlocked;CreatedBy` | Creator already recorded     | Creation authorship is set once; do not redefine it                 | Exception    |
| `AuditableEntity`     | `Field.Invalid;CreatedBy`       | Invalid Creator field        | Provide a valid creator                                             | Exception    |
| `AuditableEntity`     | `Field.Invalid;UpdatedBy`       | Invalid Updater field        | Provide a valid updater                                             | Exception    |
| `AuditableEntity`     | `Field.Invalid;DeletedBy`       | Invalid Deleter field        | Provide a valid deleter                                             | Exception    |
| `AuditableEntity`     | `Field.Invalid;RestoredBy`      | Invalid Restorer field       | Provide a valid restorer                                            | Exception    |
| `AuditableEntity`     | `Record.Deleted`                | Record deleted               | Consider whether the record must be restored before operating on it | Notification |
| `AuditableEntity`     | `Record.Deleted`                | Record deleted               | Restore the record if needed before operating on it                 | Exception    |
| `FileEntity`          | `Field.Invalid;ProtocolHttp`    | Invalid file link            | Provide a valid HTTP or HTTPS link                                  | Exception    |
| `FileEntity`          | `Field.Invalid;Title`           | Invalid file title           | Provide a valid title                                               | Exception    |
| `FileEntity`          | `Field.Required;FileStorage`    | File not provided            | Provide the file's `FileStorage`                                    | Exception    |
| `FileEntity`          | `Field.Required;Title`          | Title not provided           | Provide the file's `Title`                                          | Exception    |
| `FileEntity`          | `Field.Required;FileFormat`     | File format not provided     | Provide the file format                                             | Exception    |
| `FileEntity`          | `Field.Invalid;Size`            | Negative file size           | Provide a size greater than or equal to zero                        | Exception    |

Applies to every entity: a **null** argument produces `Field.Required;<field>` — `CreatedBy`, `UpdatedBy`,
`DeletedBy` or `RestoredBy`, depending on the operation — as an `Exception`.

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Entities](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
