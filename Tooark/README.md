# Tooark

Library with every Tooark resource and feature aimed at .NET projects.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark/README.pt-BR.md)

## Installation

```bash
dotnet add package Tooark
```

The aggregator brings every Tooark package at once. Install the packages individually when you only want
some of them — the surface is the same.

## Configuration

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

That single call registers `Tooark.Dtos`, `Tooark.Extensions`, `Tooark.ValueObjects` and
`Tooark.Mediator`, and adds `Tooark.Securities` and `Tooark.Observability` when the corresponding
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

## Available features

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

### [Tooark.Securities](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.md)

Description: This package offers security features, including cryptography and authentication.

### [Tooark.Securities.OpenId](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities.OpenId/README.md)

Description: This package configures the native ASP.NET Core OpenID Connect handlers, with SSO presets for Microsoft Entra ID and Google.

### [Tooark.Utils](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Utils/README.md)

Description: This package contains utilities and helper functions for assorted operations.

### [Tooark.Validations](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Validations/README.md)

Description: This package provides validation features for data and entities.

### [Tooark.ValueObjects](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.ValueObjects/README.md)

Description: This package defines the value objects used by the application.

## Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark](https://github.com/Tooark/nuget-tooark/issues) repository.

## License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
