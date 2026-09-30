# Tooark

Library with every Tooark resource and feature aimed at .NET projects.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark/README.pt-BR.md)

---

## 📑 Contents

- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Available features](#-available-features)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 🔧 Installation

```bash
dotnet add package Tooark
```

The aggregator brings every Tooark package at once. Install the packages individually when you only want
some of them — the surface is the same.

The exception is the storage and secrets providers (`Tooark.Storage.Aws`, `Tooark.Storage.Gcp`,
`Tooark.Secrets.Aws`, `Tooark.Secrets.Gcp` and `Tooark.Secrets.Vault`): the aggregator brings only the
`Tooark.Storage` and `Tooark.Secrets` abstractions, and the application installs the provider it uses.

---

## ⚙️ Configuration

The translations ship with the assembly, so there is nothing to configure for them to work.

Add the following line to your `Program.cs`:

```csharp
// Importing the required namespaces
using Tooark.Injections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

IConfiguration configuration = new ConfigurationBuilder()
  .Build();

// In your service configuration
services.AddTooarkService(configuration);
```

That single call registers `Tooark.Dtos`, `Tooark.Extensions`, `Tooark.ValueObjects`, `Tooark.Mediator`
and `Tooark.Sanitizers`, and adds `Tooark.Securities` and `Tooark.Observability` when the corresponding
configuration sections exist.

### Where the mediator handlers are looked up

Without an argument, in the **assembly that called `AddTooarkService`**. In a single-project application
that is what you want, and there is nothing to do.

In a layered application the handlers usually live in another project. Pass the assemblies:

```csharp
services.AddTooarkService(configuration, typeof(MyHandler).Assembly);
```

Calling `AddTooarkMediator` afterwards also works, to add assemblies or configure the options — the
handler registration does not duplicate:

```csharp
using Tooark.Mediator.Injections;

services.AddTooarkService(configuration);
services.AddTooarkMediator(typeof(MyHandler).Assembly);
```

> **Careful when calling `AddTooarkService` from inside a library of yours.** The assembly looked up is the
> caller's, so it would be your library's, not the application's. Pass the right assembly along.

### Unit of work

Stays out of `AddTooarkService`, because it depends on the type of your Entity Framework context:

```csharp
using Tooark.Mediator.EntityFrameworkCore.Injections;

services.AddTooarkMediatorUnitOfWork<MyDbContext>();
```

### OpenID Connect SSO

Also stays out of `AddTooarkService`: the same provider can back an interactive login (`OpenIdConnect`
handler) or an API (`JwtBearer` handler), and that choice is yours. Register it explicitly:

```csharp
using Tooark.Securities.OpenId.Injections;

services.AddTooarkEntraSso(configuration);   // reads OpenId:Entra
services.AddTooarkGoogleSso(configuration);  // reads OpenId:Google
```

### Storage

Also stays out of `AddTooarkService`, and the provider does not come with the aggregator: install the package
of the cloud you use and register it:

```csharp
using Tooark.Storage.Aws.Injections;

services.AddTooarkStorageAws(configuration); // reads Storage
```

### Secrets

The store is a configuration source, added before the configuration reaches `AddTooarkService`, and the
provider does not come with the aggregator:

```csharp
using Tooark.Secrets.Aws.Injections;

IConfiguration configuration = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json")
  .AddTooarkSecretsAws() // reads Secrets from appsettings.json
  .Build();

services.AddTooarkService(configuration);
```

---

## ✨ Available features

### [Tooark.Attributes](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Attributes/README.md)

Description: This package provides custom attributes for use in .NET projects.

### [Tooark.AspNetCore](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.AspNetCore/README.md)

Description: This package gathers the features that depend on ASP.NET Core, such as reading `ModelState` errors.

### [Tooark.Dtos](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Dtos/README.md)

Description: This package contains data transfer objects (DTOs) that ease the communication between application layers.

### [Tooark.Entities](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities/README.md)

Description: This package defines the domain entities used by the application.

### [Tooark.Enums](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Enums/README.md)

Description: This package contains the enum definitions used across the application.

### [Tooark.Exceptions](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Exceptions/README.md)

Description: This package provides custom exceptions for use in .NET projects.

### [Tooark.Extensions](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Extensions/README.md)

Description: This package provides extension methods for common .NET types and classes.

### [Tooark.Notifications](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Notifications/README.md)

Description: This package offers features to manage notifications and messages in the application.

### [Tooark.Mediator.Abstractions](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.Abstractions/README.md)

Description: This package provides the base contracts of the Mediator pattern for use in .NET projects.

### [Tooark.Mediator](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator/README.md)

Description: This package offers a concrete implementation of the Mediator pattern, easing the communication between application components.

### [Tooark.Mediator.EntityFrameworkCore](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.EntityFrameworkCore/README.md)

Description: This package moves Entity Framework Core persistence into the Mediator pipeline, keeping handlers free of SaveChanges.

### [Tooark.Observability](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Observability/README.md)

Description: This package provides tools for monitoring and observability of the application.

### [Tooark.Sanitizers](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Sanitizers/README.md)

Description: This package sanitizes HTML with an allowlist, URLs with a scheme allowlist and the `@tooark/wysiwyg` editor content, with the same rules the component applies in the browser.

### [Tooark.Secrets](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md)

Description: This package loads secrets and parameters from vaults (AWS, Google Cloud, Vault and OpenBao) into the configuration and reads secrets at runtime. The providers, `Tooark.Secrets.Aws`, `Tooark.Secrets.Gcp` and `Tooark.Secrets.Vault`, are installed separately.

### [Tooark.Securities](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.md)

Description: This package offers security features, including cryptography and authentication.

### [Tooark.Securities.OpenId](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities.OpenId/README.md)

Description: This package configures the native ASP.NET Core OpenID Connect handlers, with SSO presets for Microsoft Entra ID and Google.

### [Tooark.Storage](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.md)

Description: This package defines the cloud object storage contract (upload, download, delete, metadata and signed URLs). The providers, `Tooark.Storage.Aws` and `Tooark.Storage.Gcp`, are installed separately.

### [Tooark.Utils](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Utils/README.md)

Description: This package contains utilities and helper functions for assorted operations.

### [Tooark.Validations](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Validations/README.md)

Description: This package provides validation features for data and entities.

### [Tooark.ValueObjects](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.ValueObjects/README.md)

Description: This package defines the value objects used by the application.

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
