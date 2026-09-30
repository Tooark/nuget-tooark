# Tooark.AspNetCore

Library that concentrates the ASP.NET Core specific pieces of Tooark, keeping the general-purpose packages free of the runtime requirement. Packages whose purpose is to integrate with ASP.NET Core (`Tooark.Dtos`, `Tooark.Observability`, `Tooark.Securities.OpenId`) declare the shared framework themselves.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.AspNetCore/README.pt-BR.md)

## Contents

- [Overview](#overview)
- [Installation](#-installation)
- [Components](#-components)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Contributing](#-contributing)
- [License](#-license)

## Overview

The `Tooark.AspNetCore` package provides:

- extensions for ASP.NET Core types, today the `ModelStateDictionary`;
- the `ResponseDto` body for validation failures in `[ApiController]` controllers, with `AddTooarkModelStateEnvelope`;
- MVC validation of the `Tooark.Attributes` attributes without the duplicated missing-field message, with `AddTooarkValidationAttributes`;
- the place where the ASP.NET Core requirement was moved to, taking it out of the general-purpose packages;
- integration with the `Tooark.Extensions` localizer, translating the validation error keys;
- the intended home for the family's filters and middlewares.

**Why the package exists.** `Microsoft.AspNetCore.App` is not an ordinary NuGet dependency: declaring it makes
the application **require the ASP.NET Core runtime to be installed**, and that requirement is contagious — it
propagates to every package that references it, and to whoever references those.

Until v3 the requirement came from `Tooark.Extensions`, because of a single file. In practice, a worker
service or a command-line tool that used `Tooark.ValueObjects` got `Microsoft.AspNetCore.App` in its own
`runtimeconfig.json` and would not start on a `mcr.microsoft.com/dotnet/runtime` image.

From v4 on the requirement lives here. Web projects reference this package; the others do not pay for it.

| Package                                                                                                         | Requires the ASP.NET Core runtime                                               |
| --------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| `Tooark.AspNetCore`                                                                                             | **yes** — it is its purpose                                                     |
| `Tooark.Dtos`                                                                                                   | **yes** — `SearchDto`, `PaginationDto` and `ResponseDto` use MVC and Http types |
| `Tooark` (aggregator)                                                                                           | **yes** — by referencing the two above                                          |
| `Tooark.Extensions`, `Tooark.Utils`, `Tooark.ValueObjects`, `Tooark.Entities`, `Tooark.Attributes` and the rest | no                                                                              |

---

## 🔧 Installation

```bash
dotnet add package Tooark.AspNetCore
```

`GetErrors` is an extension method and works without configuration. The [validation response](#validation-response)
and the [attribute validation](#tooark-attributes-in-mvc-validation) have a registration, and both are **opt-in**:
neither the package nor the `AddTooarkService` of the `Tooark` aggregator turns them on by itself. The response
changes the body of every validation failure in the API, and the attribute validation changes the messages of a
missing field.

```csharp
using Tooark.AspNetCore.Injections;

builder.Services.AddControllers();
builder.Services.AddTooarkModelStateEnvelope();
builder.Services.AddTooarkValidationAttributes();
```

> **Runtime requirement**: because it declares the shared framework, whoever consumes this package needs the
> ASP.NET Core runtime installed. In a Docker image, use `mcr.microsoft.com/dotnet/aspnet` instead of
> `mcr.microsoft.com/dotnet/runtime`. A web application already uses that image, so in practice nothing changes.

---

## 📦 Components

### ModelState extensions

- `ModelStateExtension.GetErrors(ModelStateDictionary)`: returns the ModelState error messages.

Walks every entry of the `ModelStateDictionary` and gathers the `ErrorMessage` of each error into a single
list. A field with more than one error contributes all of its messages, together and in the order they were
recorded. Between fields, the order is the `ModelStateDictionary`'s: it comes from the keys, not from the order
in which the fields were validated, but it is not alphabetical either — today, top-level fields come before
nested ones and shorter keys before longer ones (`Cpf` before `Email`). Do not rely on the position of an error
in the list.

An error without text — recorded with just an exception, or with a blank message — becomes the key
`Field.Invalid;{key}`, or `BadRequest` when it is not tied to a field. ASP.NET Core records errors with just an
exception when the exception message is not safe for the client — a malformed JSON body with
`AllowInputFormatterExceptionMessages = false`, for instance — and when `MaxModelValidationErrors` is reached.
The exception message is never used. Up to v4.5.0 these errors came back with their empty or blank text.

### Validation response

- `TooarkDependencyInjection.AddTooarkModelStateEnvelope(IServiceCollection)`: makes a validation failure in an
  `[ApiController]` controller answer with a `ResponseDto<object>`, instead of ASP.NET Core's
  `ValidationProblemDetails`.

In a controller with `[ApiController]`, ASP.NET Core checks the ModelState before the action runs and, when it
is invalid, answers 400 with the body built by `ApiBehaviorOptions.InvalidModelStateResponseFactory`. The method
replaces that factory with one that answers 400 with a
[`ResponseDto<object>`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Dtos): `Data` null and `Errors`
with the messages of `GetErrors`, already translated by the `ResponseDto` itself. Keys become text, and the
model binding messages arrive unchanged.

What changes for the client:

|              | `ValidationProblemDetails` (ASP.NET Core default) | `ResponseDto<object>` (with the envelope) |
| ------------ | ------------------------------------------------- | ----------------------------------------- |
| Content type | `application/problem+json`                        | `application/json`                        |
| Errors       | per field: `{"Email": ["..."]}`, not translated   | a single list: `["..."]`, translated      |
| Other fields | `type`, `title`, `status`, `traceId`              | `data`, `pagination`, `metadata`          |

**Registration order does not matter.** `AddControllers` sets the default factory while the options are being
built, and would win if it came after a plain `Configure`. The envelope is applied after every configuration
(`PostConfigure`), so it wins whether it is registered before or after `AddControllers`. For the same reason, a
factory of the application's own set with `ConfigureApiBehaviorOptions` is replaced: an application that needs
its own factory does not call this method. Calling it twice is harmless.

**Where it does not apply.** Only where ASP.NET Core calls the factory:

- controllers without `[ApiController]`, which have no automatic check: use `GetErrors` in the action or a
  [filter](#a-filter-for-controllers-without-apicontroller);
- `SuppressModelStateInvalidFilter = true`, which turns the automatic check off;
- minimal APIs, which have no ModelState.

**OpenAPI.** The method changes the response, not the API description. A `[ProducesResponseType(400)]` without a
type is still documented as `ProblemDetails`, the error type ASP.NET Core assumes for `[ApiController]`. To make
the documentation match the response, declare the error type once in the project with the controllers:

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Dtos;

[assembly: ProducesErrorResponseType(typeof(ResponseDto<object>))]
```

### Tooark attributes in MVC validation

- `TooarkDependencyInjection.AddTooarkValidationAttributes(IServiceCollection)`: makes MVC stop inferring
  `[Required]` on members validated by a
  [`Tooark.Attributes`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Attributes) attribute.

With `<Nullable>enable</Nullable>`, MVC treats every non-nullable reference type as if it had `[Required]`, with
the framework's message (`The Email field is required.`), which is not a translation key. The `Tooark.Attributes`
attributes already report the missing value with the `Field.Required` key, so, without the registration, a
missing field gets both messages. With the registration, only the attribute's key remains:

| Missing member                             | Without the registration                                  | With the registration         |
| ------------------------------------------ | --------------------------------------------------------- | ----------------------------- |
| `[EmailValidation] string Email`           | `The Email field is required.` and `Field.Required;Email` | `Field.Required;Email`        |
| `string Name`, without a Tooark attribute  | `The Name field is required.`                             | `The Name field is required.` |
| `[Required][EmailValidation] string Email` | both messages                                             | both messages                 |

**The reach is that of the Tooark attributes.** The inferred `[Required]` only leaves a member that has a Tooark
attribute. A `[Required]` declared on the member stays, because it is the choice of whoever wrote the DTO, and
members without a Tooark attribute keep the inference. That is the difference from
`SuppressImplicitRequiredAttributeForNonNullableReferenceTypes`, which turns the inference off for every DTO in the
application. The member is still flagged as required in the MVC metadata, which is true: the attribute rejects
the missing value.

**Registration order does not matter.** The provider that infers `[Required]` enters the MVC options with
`AddControllers`. The registration is applied after every configuration (`PostConfigure`), so it runs after that
provider, whether called before or after `AddControllers`. Calling it twice does not repeat the registration.

**Where it applies.** In MVC validation: positional record, class and action parameter, in controllers with or
without `[ApiController]` and with or without the [envelope](#validation-response). Minimal APIs do not go through
the MVC options.

### Integration with the validations

The returned messages are what the
[`Tooark.Attributes`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Attributes) attributes produce:
translation keys in the `Key;Field` format, such as `Field.Required;Email`. They become text through the
`IStringLocalizer` of [`Tooark.Extensions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions),
which resolves the language from the request's execution flow.

Errors generated by model binding itself — an integer receiving text, for instance — come with the framework's
message, not with a key. The localizer returns that text unchanged and flags `ResourceNotFound`, which lets you
tell the two cases apart when it matters.

A missing non-nullable field may carry both messages: the attribute's `Field.Required` key and the `[Required]`
that MVC infers, with the framework's text. `AddTooarkValidationAttributes` keeps only the key: see
[Tooark attributes in MVC validation](#tooark-attributes-in-mvc-validation).

### Filters and middlewares

Not shipped yet. The validation response is not a filter: ASP.NET Core already calls a factory at the point where
it decides the body of a validation failure, and replacing that factory was enough. When filters and middlewares
exist, they go in this package, under the `Filters/` and `Middlewares/` folders.
The family only splits a package when the **dependency profile** differs — that is what motivated separating
`Tooark.Mediator.EntityFrameworkCore`, which pulls Entity Framework Core, from `Tooark.Mediator`, which does not
use it. Filters, middlewares and the extensions here share exactly the same profile, so splitting them into
separate packages would reduce nothing.

### Namespaces

The extensions live in `Tooark.AspNetCore.Extensions`, and the registrations in `Tooark.AspNetCore.Injections`.

---

## 📝 Usage Examples

### Standard response for validation failures

```csharp
using Tooark.AspNetCore.Injections;

builder.Services.AddControllers();
builder.Services.AddTooarkModelStateEnvelope();
```

The DTO validated by the `Tooark.Attributes` attributes:

```csharp
using Tooark.Attributes;

public sealed class CreatePersonDto
{
  [EmailValidation]
  public string Email { get; set; } = null!;

  [DocumentValidation("CPF")]
  public string Cpf { get; set; } = null!;
}
```

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("people")]
public sealed class PersonController : ControllerBase
{
  [HttpPost]
  public IActionResult Create([FromBody] CreatePersonDto dto)
  {
    // Only reached with a valid ModelState: [ApiController] answers 400 before the action runs
    return Ok();
  }
}
```

`POST /people` with `{"email": "x", "cpf": "123"}`, in `en-US`:

```json
{
  "data": null,
  "errors": ["Field Document is invalid", "Field Email is invalid"],
  "pagination": null,
  "metadata": []
}
```

A model binding failure — text in a numeric field, an empty body — comes in the same body, with the framework's
message in `errors`.

The name that appears in the message is the attribute's, not the property's: that is why the `Cpf` field
produces `Field.Invalid;Document`, the default of `DocumentValidationAttribute`. To align the two, pass the
`propertyName`:

```csharp
[DocumentValidation("CPF", propertyName: "Cpf")]
public string Cpf { get; set; } = null!;
```

### Missing field without the framework's message

```csharp
using Tooark.AspNetCore.Injections;

builder.Services.AddControllers();
builder.Services.AddTooarkModelStateEnvelope();
builder.Services.AddTooarkValidationAttributes();
```

A positional record, with the attribute on the constructor parameter:

```csharp
using Tooark.Attributes;

public sealed record CreateContactDto([EmailValidation] string Email, string Name);
```

`POST /contacts` with `{"name": "Ana"}`, in `en-US`:

```json
{
  "data": null,
  "errors": ["Field Email is required"],
  "pagination": null,
  "metadata": []
}
```

Without `AddTooarkValidationAttributes`, `errors` would also carry `"The Email field is required."`.
With `{"email": "ana@test.com"}`, the missing `Name` answers `"The Name field is required."`: the field has no
Tooark attribute, and the MVC inference still applies to it.

### Reading the ModelState errors in the action

Without `[ApiController]`, ASP.NET Core does not check the ModelState before the action, and the action does it.
In a controller with the attribute this check never runs: the framework answers 400 first.

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.AspNetCore.Extensions;

[Route("people")]
public sealed class PersonController : ControllerBase
{
  [HttpPost]
  public IActionResult Create([FromBody] CreatePersonDto dto)
  {
    if (!ModelState.IsValid)
    {
      // ["Field.Invalid;Document", "Field.Invalid;Email"]
      var errors = ModelState.GetErrors();

      return BadRequest(errors);
    }

    return Ok();
  }
}
```

### With translated messages

The keys become text through the `IStringLocalizer`, which comes from
[`Tooark.Extensions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions) and is registered there:

```csharp
using Tooark.Extensions.Injections;

builder.Services.AddTooarkExtensions();
```

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Tooark.AspNetCore.Extensions;

[Route("people")]
public sealed class PersonController(IStringLocalizer localizer) : ControllerBase
{
  [HttpPost]
  public IActionResult Create([FromBody] CreatePersonDto dto)
  {
    if (!ModelState.IsValid)
    {
      // ["Field Document is invalid", "Field Email is invalid"]
      var errors = ModelState.GetErrors().Select(error => localizer[error].Value);

      return BadRequest(errors);
    }

    return Ok();
  }
}
```

### A filter for controllers without `[ApiController]`

The same check in every endpoint calls for a filter. The package does not ship one, because in `[ApiController]`
controllers the validation response already does the job. For controllers without the attribute it fits in a few
lines, and with `ResponseDto` it answers with the same body:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tooark.AspNetCore.Extensions;
using Tooark.Dtos;

public sealed class ValidationFilter : IActionFilter
{
  public void OnActionExecuting(ActionExecutingContext context)
  {
    if (!context.ModelState.IsValid)
    {
      context.Result = new BadRequestObjectResult(new ResponseDto<object>(context.ModelState.GetErrors()));
    }
  }

  public void OnActionExecuted(ActionExecutedContext context) { }
}
```

```csharp
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
```

---

## 📋 Dependencies

| Package                                                                                                  | Version  | Description                                                   |
| -------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------------------------------- |
| [`Tooark.Attributes`](https://www.nuget.org/packages/Tooark.Attributes)                                  | 4.x      | The attributes recognized by `AddTooarkValidationAttributes`  |
| [`Tooark.Dtos`](https://www.nuget.org/packages/Tooark.Dtos)                                              | 4.x      | `ResponseDto`, the body of the validation response            |
| [`Tooark.Extensions`](https://www.nuget.org/packages/Tooark.Extensions)                                  | 4.x      | Error message localization                                    |
| [`Microsoft.AspNetCore.App`](https://www.nuget.org/packages/Microsoft.AspNetCore.App) (shared framework) | 8.x/10.x | `ModelStateDictionary`, `ApiBehaviorOptions` and `MvcOptions` |

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.AspNetCore](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
