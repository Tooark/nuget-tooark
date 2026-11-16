# Tooark.Entities.EntityFrameworkCore

Library that plugs the Tooark entities into Entity Framework Core: an interceptor that stamps the audit fields and turns `Remove()` into a soft delete, and model conventions for the soft delete filter and the concurrency token.

📖 **Docs:** [Tooark.Entities.EntityFrameworkCore on the site](https://tooark.com/nuget-tooark/packages/tooark.entities.entityframeworkcore.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities.EntityFrameworkCore/README.pt-BR.md)

---

## 📑 Contents

- [Overview](#-overview)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [What the Interceptor Does](#-what-the-interceptor-does)
- [Soft Delete](#-soft-delete)
- [Conventions](#-conventions)
- [Lifetimes and AddDbContextPool](#-lifetimes-and-adddbcontextpool)
- [Background Jobs](#-background-jobs)
- [With Tooark.Mediator.EntityFrameworkCore](#-with-tooarkmediatorentityframeworkcore)
- [Limitations](#-limitations)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Best Practices](#-best-practices)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 📖 Overview

The entities of [`Tooark.Entities`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities/README.md) carry audit fields (`CreatedById`, `UpdatedById`, `Version`, `Deleted`, `DeletedById`, …), but on their own nothing fills those fields when the context saves and nothing hides the deleted rows from queries. This package does both for Entity Framework Core:

- a `SaveChanges` interceptor that fills the creation and update fields with the current user, by calling the entity's own methods (`SetCreatedBy`, `SetUpdatedBy`);
- the same interceptor turns `Remove()` into a soft delete (`SetDeleted`) on the entities that support it;
- `ApplyTooarkConventions()` adds a query filter that hides the soft-deleted rows and marks `Version` as a concurrency token.

The entity that the application already stamped in the same unit of work is not stamped again, so `Version` goes up once per save.

The package runs on the base runtime, with no reference to ASP.NET Core: it works the same in an API, a worker or a console application.

---

## 🔧 Installation

```bash
dotnet add package Tooark.Entities.EntityFrameworkCore
```

---

## ⚙️ Configuration

### Registration

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
  // ... the application's own configuration ...

  modelBuilder.ApplyTooarkConventions();
}
```

The three calls are independent pieces:

| Call                                             | What it does                                                                               |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------ |
| `AddTooarkEntityFrameworkCore(configure)`        | Registers the options and the interceptor. Does not register an `ICurrentUser`             |
| `AddTooarkEntityFrameworkCore<TUser>(configure)` | Same, and registers `ICurrentUser` as scoped unless the application already registered one |
| `AddTooarkInterceptors(sp)`                      | Adds the interceptor to the context. Use the `AddDbContext` overload that receives `sp`    |
| `ApplyTooarkConventions()`                       | Adds the soft delete filter and the concurrency token to the model                         |

### The current user

`ICurrentUser` tells the interceptor who is making the change. The application implements it, because where the user comes from depends on the host. For an ASP.NET Core API that reads the identifier from the token claims:

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

`Id` returns `null` when nobody is signed in. The JWT bearer handler renames `sub` to `ClaimTypes.NameIdentifier` unless `MapInboundClaims` is `false`, which is why the example looks at both.

### Options

| Option         | Type    | Default | Description                                                                                               |
| -------------- | ------- | ------- | --------------------------------------------------------------------------------------------------------- |
| `SystemUserId` | `Guid?` | `null`  | Author used when `ICurrentUser.Id` is null or empty, or when no `ICurrentUser` is registered              |
| `StampAudit`   | `bool`  | `true`  | Fills the creation and update fields. Off, the application calls `SetCreatedBy` and `SetUpdatedBy` itself |
| `SoftDelete`   | `bool`  | `true`  | Turns `Remove()` into a soft delete. Off, `Remove()` deletes the row, also on the soft-deletable entities |

```csharp
builder.Services.AddTooarkEntityFrameworkCore<HttpContextCurrentUser>(options =>
{
  options.SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
});
```

The two switches are independent: with `StampAudit = false` and `SoftDelete = true`, `Remove()` still records who deleted, because the soft delete is what calls `SetDeleted`.

---

## 🧾 What the Interceptor Does

The interceptor runs inside `SaveChanges` and `SaveChangesAsync`, before the commands are sent, and looks at the state of each tracked entity:

| Base class                    | Added (when `CreatedById` is empty)                    | Modified (when not stamped yet)          | Removed                                                                                 |
| ----------------------------- | ------------------------------------------------------ | ---------------------------------------- | --------------------------------------------------------------------------------------- |
| `InitialEntity`, `FileEntity` | `CreatedById`, `CreatedAt`                             | nothing                                  | the row is deleted                                                                      |
| `DetailedEntity`              | `CreatedById`, `CreatedAt`, `UpdatedById`, `UpdatedAt` | `UpdatedById`, `UpdatedAt`               | the row is deleted                                                                      |
| `VersionedEntity`             | same as `DetailedEntity`; `Version` stays at 1         | `UpdatedById`, `UpdatedAt`, `Version` +1 | the row is deleted                                                                      |
| `SoftDeletableEntity`         | same as `DetailedEntity`                               | `UpdatedById`, `UpdatedAt`               | update: `Deleted`, `UpdatedById`, `UpdatedAt`                                           |
| `AuditableEntity`             | same as `DetailedEntity`; `Version` stays at 1         | `UpdatedById`, `UpdatedAt`, `Version` +1 | update: `Deleted`, `DeletedById`, `DeletedAt`, `UpdatedById`, `UpdatedAt`, `Version` +1 |

Entities that do not derive from the Tooark base classes pass through untouched.

### Who the author is

1. `ICurrentUser.Id`, when it has a value.
2. `EntityAuditOptions.SystemUserId`, when the current user is null or `Guid.Empty`, or when no `ICurrentUser` is registered.
3. Neither: the save throws `InternalServerErrorException` with `Entities.Audit.CurrentUserRequired` and nothing is written. Saving a row with an empty author silently would be worse than failing.

The user is resolved once per save and only when some entity needs an author. A save that only touches entities without audit fields does not need a user.

### No double stamping

`SetUpdatedBy` increments `Version` on `VersionedEntity` and `AuditableEntity`, and `SetCreatedBy` throws when the creation is already recorded. Applications often call these methods in the handler, so the interceptor stamps only what is still missing:

- **Added:** `SetCreatedBy` is called only when `CreatedById` is still `Guid.Empty`. An entity created with `new Product(createdById)` keeps its author.
- **Modified:** `SetUpdatedBy` is called only when `UpdatedAt`, `UpdatedById` and `Version` still have the values that were loaded from the database (or saved by the previous `SaveChanges` of the same context). If any of them differs, the application already stamped the entity in this unit of work — with `SetUpdatedBy`, `SetDeleted` or `SetRestored` — and the interceptor leaves it as it is.
- **Unchanged:** an entity that was loaded and not changed is not stamped. Assigning the same value to a property is not a change.

The result is one increment of `Version` per `SaveChanges` that changed the entity, whether the handler stamps it or not. Saving twice in the same context increments once per save.

---

## 🧹 Soft Delete

`Remove()` on a `SoftDeletableEntity` or an `AuditableEntity` becomes an update: the interceptor calls the entity's `SetDeleted` with the author and the row stays in the database with `Deleted = true`. Any other entity is deleted for real.

```csharp
context.Products.Remove(product);

await context.SaveChangesAsync(cancellationToken); // UPDATE ... SET deleted = TRUE, ...
```

A change made to the entity before `Remove()` in the same unit of work is saved together with the soft delete.

### Reading deleted rows

With `ApplyTooarkConventions()`, queries do not return the soft-deleted rows. To read them:

```csharp
var all = await context.Products.IgnoreQueryFilters().ToListAsync(cancellationToken);
```

### Restoring

Restoring is a regular update. Load the row without the filters and call the entity's `SetRestored`:

```csharp
var product = await context.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == id, cancellationToken);

product.SetRestored(currentUserId);

await context.SaveChangesAsync(cancellationToken);
```

`SetRestored` already stamps the entity, so the interceptor does not increment `Version` a second time.

### Removing an entity that is already deleted

`Remove()` on a row that is **already soft-deleted in the database** deletes the row for real. It is the way to purge a soft-deleted row without turning the option off:

```csharp
var product = await context.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == id, cancellationToken);

context.Products.Remove(product); // product.Deleted came as true from the database

await context.SaveChangesAsync(cancellationToken); // DELETE
```

The decision uses the value read from the database, not the value in memory: a handler that calls `SetDeleted(...)` and then `Remove(...)` on an active entity still gets a soft delete, with a single increment of `Version`.

### Cascade

When the application removes a principal, Entity Framework Core marks the **loaded** dependents of its required relationships for removal as well. The interceptor decides entity by entity:

| Situation                                                               | Result                                                                                                                                     |
| ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Soft-deletable principal, loaded soft-deletable dependents              | The principal and the dependents are soft-deleted                                                                                          |
| Soft-deletable principal, loaded dependents without soft delete         | The principal is soft-deleted and those dependents are **deleted for real**                                                                |
| Soft-deletable principal, dependents that were **not loaded**           | Only the principal is soft-deleted. The dependents stay active: the database cascade never fires, because the principal row is not deleted |
| Soft-deletable principal, loaded dependents of an optional relationship | Entity Framework Core sets their foreign key to `null` when the principal is removed, although the principal row stays                     |
| Principal deleted for real, soft-deletable dependents                   | The dependents of required relationships are **deleted for real** too: Entity Framework Core reapplies the cascade when saving             |
| Owned types of a soft-deleted owner                                     | Kept, with their values                                                                                                                    |
| Required dependent taken out of the principal's collection (orphan)     | Soft-deleted when it supports it, keeping its foreign key                                                                                  |

A principal is deleted for real when it does not support soft delete, when it is already soft-deleted, or when `SoftDelete` is off. Removing a principal gives the same result when the context defers the cascade (`ChangeTracker.CascadeDeleteTiming`).

To soft-delete a principal together with all its dependents, load them (`Include`) before calling `Remove()`.

---

## 📐 Conventions

`ApplyTooarkConventions()` goes over the entity types that are already in the model and applies:

| Convention         | Applies to the descendants of            | Effect                                                               |
| ------------------ | ---------------------------------------- | -------------------------------------------------------------------- |
| Soft delete filter | `SoftDeletableEntity`, `AuditableEntity` | Query filter `!Deleted` on the entity (on the root of the hierarchy) |
| Concurrency token  | `VersionedEntity`, `AuditableEntity`     | `Version` becomes a concurrency token                                |

### Call it last

Call `ApplyTooarkConventions()` at the end of `OnModelCreating`:

- the method only sees the entity types that are already in the model;
- an entity type has a single unnamed query filter. When the entity **already has** a filter, the method keeps it and combines the two with AND. A `HasQueryFilter` called **after** the method replaces the soft delete filter, and the deleted rows come back in the queries.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
  modelBuilder.Entity<Product>().HasQueryFilter(product => product.Active);

  // Result: product.Active && !product.Deleted
  modelBuilder.ApplyTooarkConventions();
}
```

`IgnoreQueryFilters()` turns off the combined filter as a whole: the application's filter and the soft delete filter.

### Named filters (Entity Framework Core 10)

Entity Framework Core 10 has named query filters and does not accept a named and an unnamed filter on the same entity. The behavior is the same on both target frameworks — an unnamed filter, combined with the application's — with one exception that only exists on Entity Framework Core 10: when the entity already has **named** filters, the soft delete filter is added as a named filter too, under `ModelBuilderExtensions.SoftDeleteFilterName` (`"Tooark.SoftDelete"`). It can then be turned off on its own:

```csharp
var withDeleted = await context.Products
  .IgnoreQueryFilters([ModelBuilderExtensions.SoftDeleteFilterName])
  .ToListAsync(cancellationToken);
```

### Inheritance

Entity Framework Core only accepts a query filter on the root of a hierarchy. When the root is soft-deletable, the filter goes on the root and covers every derived type. A soft-deletable type under a root that is not soft-deletable — which requires mapping a Tooark base class itself, such as `DetailedEntity`, as the root — makes the method throw `Entities.Conventions.SoftDeleteRootNotSupported`, because there is nowhere to put the filter.

### Owned and keyless types

Owned types are skipped: they follow their owner. Keyless types get the filter, but not the concurrency token.

### Required navigations

A dependent that requires a soft-deletable principal is loaded with an inner join when the principal is included, so it disappears from the result once the principal is soft-deleted. Entity Framework Core logs a model validation warning for these relationships. Give the dependent a matching filter or make the navigation optional.

### Concurrency

With `Version` as a concurrency token, each `UPDATE` and `DELETE` carries `WHERE version = <value read>`. When another context changed the row in the meantime, `SaveChanges` throws `DbUpdateConcurrencyException` — also for a soft delete, which is an update.

`Version` is incremented in the application, by `SetUpdatedBy`. The token only detects a conflict if something increments it: the interceptor, or the application when `StampAudit` is off.

---

## 🔄 Lifetimes and AddDbContextPool

The interceptor is registered as transient and holds no state between saves. It is created by the service provider passed to `AddTooarkInterceptors(sp)` and resolves `ICurrentUser` from that same provider at each save. Which provider that is depends on how the context is registered:

| Registration                                                                     | Provider received | `ICurrentUser` lifetime that works |
| -------------------------------------------------------------------------------- | ----------------- | ---------------------------------- |
| `AddDbContext((sp, options) => ...)`                                             | The request scope | Scoped, singleton or transient     |
| `AddDbContextPool`, `AddDbContextFactory`, `AddDbContext` with singleton options | The root provider | Singleton only                     |

With options built from the root provider, register the current user as a singleton **before** `AddTooarkEntityFrameworkCore`, so that the scoped registration of the package does not take place:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddTooarkEntityFrameworkCore();

builder.Services.AddDbContextPool<AppDbContext>((sp, options) => options
  .UseNpgsql(connectionString)
  .AddTooarkInterceptors(sp));
```

An implementation over `IHttpContextAccessor` is safe as a singleton: the accessor is a singleton and reads the request of the current call.

A scoped `ICurrentUser` with options built from the root provider is a configuration error:

- with scope validation on (the default of the ASP.NET Core host in Development), the save throws `Entities.Audit.CurrentUserNotResolved`;
- with scope validation off, the container resolves the scoped service from the root provider as a single instance for the whole application, which does not see the state of the request. The package cannot detect this case: keep scope validation on in development and register the user as a singleton.

---

## 🤖 Background Jobs

A worker, a seeder or a scheduled job has no signed-in user. Set `SystemUserId` and the interceptor uses it as the author. `ICurrentUser` does not need to be registered:

```csharp
builder.Services.AddTooarkEntityFrameworkCore(options =>
{
  options.SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
});

builder.Services.AddDbContext<AppDbContext>((sp, options) => options
  .UseNpgsql(connectionString)
  .AddTooarkInterceptors(sp));
```

In an application that has both — an API with hosted services — the current user wins when it has an identifier, and `SystemUserId` covers the work done outside a request.

---

## 🧩 With Tooark.Mediator.EntityFrameworkCore

[`Tooark.Mediator.EntityFrameworkCore`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.EntityFrameworkCore/README.md) moves `SaveChanges` to the end of the command pipeline. The interceptor runs inside that `SaveChanges`, so the two packages combine without extra configuration:

```csharp
builder.Services.AddTooarkEntityFrameworkCore<HttpContextCurrentUser>();
builder.Services.AddDbContext<AppDbContext>((sp, options) => options
  .UseNpgsql(connectionString)
  .AddTooarkInterceptors(sp));

builder.Services.AddTooarkMediator(typeof(Program).Assembly);
builder.Services.AddTooarkMediatorUnitOfWork<AppDbContext>();
```

The handler changes the entity and returns, with no `SaveChanges` and no `SetUpdatedBy`:

```csharp
public sealed class RenameProductHandler(AppDbContext context) : ICommandHandler<RenameProduct, Guid>
{
  public async Task<Guid> HandleAsync(RenameProduct request, CancellationToken cancellationToken = default)
  {
    var product = await context.Products.SingleAsync(p => p.Id == request.Id, cancellationToken);

    product.Rename(request.Name);

    // No SetUpdatedBy and no SaveChanges: the pipeline saves and the interceptor stamps
    return product.Id;
  }
}
```

A handler that already calls `SetUpdatedBy` keeps working: the interceptor recognizes the stamp and `Version` goes up once. In the `Transaction` strategy, a handler that saves more than once gets one stamp per save that changed the entity.

---

## 🚧 Limitations

- **Bulk operations.** `ExecuteUpdate` and `ExecuteDelete` do not go through `SaveChanges`: no stamping, no soft delete (`ExecuteDelete` deletes the rows) and no change of `Version`.
- **Raw SQL.** The query filter applies to the queries Entity Framework Core builds for the entity. SQL sent directly to the database sees every row and must filter `deleted` itself.
- **Owned types and children.** A change made only in an owned type or in a child entity does not mark the owner as modified, so the owner is not stamped. Call `SetUpdatedBy` on the owner when the change must count as a change of the owner.
- **Disconnected entities.** The detection compares current and original values. An entity attached with `Update()` has no original values, so the interceptor stamps it. Do not call `SetUpdatedBy` before attaching: on a `DetailedEntity` the author is replaced by the current user, and on an entity with `Version` the save fails with `DbUpdateConcurrencyException`, because the version taken as original is no longer the one in the database.
- **Dependents that were not loaded.** A soft delete does not reach the dependents that are not tracked (see [Cascade](#cascade)).
- **Unique indexes.** A soft-deleted row still occupies its values in a unique index. Use a filtered index when the value must be reusable.
- **`FileEntity.Type`.** `EFileType` is a class, and Entity Framework Core does not map it by convention. The package does not configure it: `modelBuilder.Entity<Attachment>().Property(a => a.Type).HasConversion(type => type.ToInt(), id => (EFileType)id)`.
- **Tenancy.** The package does not filter or stamp a tenant.

---

## 📝 Usage Examples

### Entities

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

### Context

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

### Create, update and remove

```csharp
// Created without an author: the interceptor fills CreatedById, CreatedAt, UpdatedById and UpdatedAt
var product = new Product("Pen");

context.Products.Add(product);
await context.SaveChangesAsync(cancellationToken); // Version = 1

// Changed without SetUpdatedBy: the interceptor fills UpdatedById and UpdatedAt
product.Rename("Blue pen");
await context.SaveChangesAsync(cancellationToken); // Version = 2

// Removed: the row stays, with Deleted, DeletedById and DeletedAt
context.Products.Remove(product);
await context.SaveChangesAsync(cancellationToken); // Version = 3
```

### Concurrency conflict

```csharp
try
{
  await context.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateConcurrencyException)
{
  // Another request changed the row after it was read: reload and apply the change again, or return 409
}
```

---

## 📋 Dependencies

| Package                                                                                                                                         | Version  | Usage                                      |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------------ |
| [`Tooark.Entities`](https://www.nuget.org/packages/Tooark.Entities)                                                                             | 4.x      | Base entities and their audit methods      |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                         | 4.x      | Configuration and missing author errors    |
| [`Microsoft.EntityFrameworkCore`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore)                                                 | 8.x/10.x | Interceptor, change tracking and the model |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Container registration                     |

The package is part of the `Tooark` aggregator, so whoever installs `Tooark` already gets it.

---

## 🎯 Best Practices

- Call `ApplyTooarkConventions()` last in `OnModelCreating`.
- Use the `AddDbContext((sp, options) => ...)` overload, so that the interceptor receives the request scope.
- Keep scope validation on in development: it is what reports a scoped `ICurrentUser` behind a pooled context.
- Set `SystemUserId` in every application that writes outside a request, and use an identifier that exists in the users table when there is a foreign key.
- Load the dependents before removing a principal, when they must be soft-deleted with it.
- Handle `DbUpdateConcurrencyException` where entities with `Version` are updated.
- Do not mix bulk operations with entities that depend on the audit fields; when a bulk update is needed, set the audit columns in the same statement.

---

## ⚠️ Error Codes and Solutions

| Message                                                         | Exception                      | Description                                                                             | Solution                                                                                                     |
| --------------------------------------------------------------- | ------------------------------ | --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `Entities.Audit.CurrentUserRequired`                            | `InternalServerErrorException` | The save needs an author and there is no current user and no `SystemUserId`             | Sign the user in, or set `SystemUserId` for the work done outside a request                                  |
| `Entities.Audit.CurrentUserNotResolved`                         | `InternalServerErrorException` | `ICurrentUser` could not be resolved from the provider given to `AddTooarkInterceptors` | With a pooled context or singleton options, register `ICurrentUser` as a singleton. See the `InnerException` |
| `Entities.Audit.InterceptorNotRegistered`                       | `InternalServerErrorException` | `AddTooarkInterceptors` was called without the registration                             | Call `AddTooarkEntityFrameworkCore` on the service collection                                                |
| `Entities.Conventions.SoftDeleteRootNotSupported;{type};{root}` | `InternalServerErrorException` | A soft-deletable type is under a hierarchy root that does not support soft delete       | Do not map the Tooark base class as the root, or make the hierarchy start at a soft-deletable type           |

`DbUpdateConcurrencyException` comes from Entity Framework Core, when the row was changed or deleted by someone else after it was read.

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
