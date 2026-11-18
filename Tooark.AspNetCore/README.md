# Tooark.AspNetCore

Library that concentrates the ASP.NET Core specific pieces of Tooark, keeping the general-purpose packages free of the runtime requirement. Packages whose purpose is to integrate with ASP.NET Core (`Tooark.Dtos`, `Tooark.Observability`, `Tooark.Securities.OpenId`) declare the shared framework themselves.

📖 **Docs:** [Tooark.AspNetCore on the site](https://tooark.com/nuget-tooark/packages/tooark.aspnetcore.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.AspNetCore/README.pt-BR.md)

---

## 📑 Contents

- [Overview](#-overview)
- [Installation](#-installation)
- [Components](#-components)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 📖 Overview

The `Tooark.AspNetCore` package provides:

- extensions for ASP.NET Core types, today the `ModelStateDictionary`;
- the `ResponseDto` body for validation failures in `[ApiController]` controllers, with `AddTooarkModelStateEnvelope`;
- MVC validation of the `Tooark.Attributes` attributes without the duplicated missing-field message, with `AddTooarkValidationAttributes`;
- Problem Details (RFC 9457) for the `Tooark.Exceptions` exceptions, in controllers and minimal APIs, with `AddTooarkProblemDetails`;
- results for minimal APIs that answer a notification or an exception with the same Problem Details, with `TooarkResults`;
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

The [Problem Details for exceptions](#problem-details-for-exceptions) is opt-in as well, and needs the
`UseExceptionHandler` middleware in the pipeline, without arguments:

```csharp
using Tooark.AspNetCore.Injections;

builder.Services.AddTooarkProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
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

With [`AddTooarkProblemDetails`](#problem-details-for-exceptions), the ASP.NET Core column comes out with the Tooark
`errors` list, translated, unless `ConvertValidationProblems` is turned off.

**The envelope does not choose the messages.** It changes the body of the response, not what validation records
in the ModelState. With `<Nullable>enable</Nullable>`, a non-nullable field with a `Tooark.Attributes` attribute
arrives missing with two messages: the `Field.Required` key, translated, and the `The Email field is required.`
of the `[Required]` that MVC infers, which is not a translation key and arrives as the framework writes it. That
is a defect of neither the envelope nor the attribute: the framework's message is removed by
[`AddTooarkValidationAttributes`](#tooark-attributes-in-mvc-validation), a separate registration. An application
that validates its DTOs with the Tooark attributes calls both.

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

**The envelope does not turn it on.** `AddTooarkModelStateEnvelope` and this registration are independent, and
neither calls the other: this one chooses the messages of a missing field, and the envelope, the body in which
they reach the client.

### Problem Details for exceptions

- `TooarkDependencyInjection.AddTooarkProblemDetails(IServiceCollection, Action<ProblemDetailsOptions>?)`: makes
  the [`Tooark.Exceptions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Exceptions) exceptions answer
  with Problem Details ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)), in the status each exception
  defines, in controllers and minimal APIs.
- `TooarkExceptionHandler`: the `IExceptionHandler` the registration adds. It can be registered on its own with
  `AddExceptionHandler<TooarkExceptionHandler>()`, next to `AddProblemDetails()`.
- `ProblemDetailsError`: an item of the `errors` list of the body, with `code`, `key` and `message`.
- `TooarkProblemDetailsOptions`: the log level of each handled exception and the conversion of the ASP.NET Core
  validation failures to the `errors` list.

Without it, a `NotFoundException` thrown in an endpoint is an exception like any other: ASP.NET Core answers 500,
and the status and the message the exception carries are lost. The registration adds the `AddProblemDetails` of
ASP.NET Core and an `IExceptionHandler` that recognizes the `TooarkException` family: the status comes from
`GetStatusCode()` and the body from `GetNotifications()`, with the messages translated by the
[`Tooark.Extensions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions) localizer in the language
of the request.

`GET /users/1` throwing `new NotFoundException("NotFound.User")`, in `en-US`:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "User not found",
  "instance": "/users/1",
  "errors": [{ "code": "T.ERR", "key": "Unknown", "message": "User not found" }],
  "traceId": "0HNPTVUT6D34Q"
}
```

| Member     | Source                                                                                               |
| ---------- | ---------------------------------------------------------------------------------------------------- |
| `status`   | `TooarkException.GetStatusCode()`                                                                    |
| `title`    | The standard reason phrase of the status (`Not Found`, `Bad Request`)                                |
| `type`     | The RFC 9110 section of the status, applied by ASP.NET Core                                          |
| `detail`   | The first message of `errors`                                                                        |
| `instance` | The request path                                                                                     |
| `errors`   | One item per `NotificationItem` of the exception: `code` and `key` as recorded, `message` translated |
| `traceId`  | Added by ASP.NET Core                                                                                |

An exception built from a `Notification` carries one item per failure, with the code and the key of each
validation; one built from a message carries a single item with the defaults `T.ERR` and `Unknown`. A message
that is not a translation key, such as `"User.NotFound"`, arrives as written.

**Which exceptions.** Every class of `Tooark.Exceptions`, in its status: `BadRequestException` 400,
`UnauthorizedException` 401, `ForbiddenException` 403, `NotFoundException` 404, `ConflictException` 409,
`UnprocessableEntityException` 422, `TooManyRequestsException` 429, `InternalServerErrorException` 500 and the
others of the [package](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Exceptions/README.md). Any other
exception is left to ASP.NET Core, which answers the default 500 Problem Details of `AddProblemDetails`, without
the exception's message: a bug does not leak its text to the client.

**Where it applies.** Wherever the exception reaches the `UseExceptionHandler` middleware: minimal API handlers,
controller actions and filters, and the middlewares after it. The middleware has to be in the pipeline, without
arguments and before the middlewares that may throw. `AddProblemDetails` is what lets `UseExceptionHandler` be
called without an error path:

```csharp
builder.Services.AddTooarkProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();   // no arguments: the Problem Details service is the fallback
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

**Writing the body.** The body is written by the `IProblemDetailsService` of ASP.NET Core, so the
`CustomizeProblemDetails` of the application applies to it and `type` gets the default of the status. When the
service refuses to write — a client that sent an `Accept` without JSON, for instance — the handler writes the
body itself, in `application/problem+json`: an exception of the Tooark family never ends in a 500 without a
body. The `configure` parameter is the `ProblemDetailsOptions` of ASP.NET Core:

```csharp
builder.Services.AddTooarkProblemDetails(options =>
  options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["application"] = "orders-api");
```

**Independent of the envelope.** [`AddTooarkModelStateEnvelope`](#validation-response) shapes the validation failure of
`[ApiController]` controllers, which happens before the action, with `ResponseDto`. This registration shapes the
exceptions, which happen during the action, and neither changes the other. An API can use both, or choose Problem
Details for everything by not registering the envelope: without it, the validation failure also comes out in the Tooark
`errors` list (see below). Calling the registration twice does not repeat the handler.

**Logging.** The handled exception is logged in the `Tooark.AspNetCore.ExceptionHandling.TooarkExceptionHandler`
category: `Information` for a 4xx status, with the status and the message, and `Error` for a 5xx, with the
exception and its stack trace. On .NET 10 `UseExceptionHandler` does not log an exception that an
`IExceptionHandler` handled, so this is the only entry. A 404 or a 409 is part of normal operation; to change the
level, per range or per status, configure `TooarkProblemDetailsOptions`:

```csharp
builder.Services.Configure<TooarkProblemDetailsOptions>(options =>
{
  options.ClientErrorLogLevel = LogLevel.Information;                // 4xx (default)
  options.ServerErrorLogLevel = LogLevel.Error;                      // 5xx (default)
  options.StatusCodeLogLevels[StatusCodes.Status401Unauthorized] = LogLevel.Warning;
  options.StatusCodeLogLevels[StatusCodes.Status404NotFound] = LogLevel.Debug;
});
```

The level of a status in `StatusCodeLogLevels` wins over the level of its range, and `LogLevel.None` leaves the
status out of the log. The options are code only: there is no configuration section for them. On 4.x, where they
appeared (v4.10.0), the 4xx default was `Warning`.

**Validation failures in the Tooark format.** The ASP.NET Core `ValidationProblemDetails` carries `errors` as an object
with the messages per field, and the Tooark Problem Details carries `errors` as the list of `code`, `key` and `message`.
So that a client receives a single format, the ASP.NET Core validation failures also come out in the Tooark list, one
entry per message, with the field in `key`, the default code `T.ERR` and the translated message: a key such as
`Field.Required;Email` arrives translated, and the framework text arrives as the framework writes it. `status`, `type`,
`traceId` and the extensions of `CustomizeProblemDetails` stay in the body, and `title` becomes the standard reason
phrase of the status, as for the exceptions. `CustomizeProblemDetails` runs before the conversion, on the ASP.NET Core
problem: a `title` or a `detail` set there is replaced.

```json
// ASP.NET Core format
{ "status": 400, "title": "One or more validation errors occurred.", "errors": { "Email": ["The Email field is required."] } }

// As it comes out with AddTooarkProblemDetails
{ "status": 400, "title": "Bad Request", "detail": "The Email field is required.", "errors": [{ "code": "T.ERR", "key": "Email", "message": "The Email field is required." }] }
```

| Source of the failure                                            | Converted                                                           |
| ---------------------------------------------------------------- | ------------------------------------------------------------------- |
| Automatic validation of `[ApiController]`                        | Yes                                                                 |
| Automatic validation of `[ApiController]` with the envelope      | No: the [envelope](#validation-response) answers with `ResponseDto` |
| `ValidationProblem()` of a controller                            | Yes                                                                 |
| Minimal API validation (`AddValidation`)                         | Yes                                                                 |
| `Results.ValidationProblem` and `TypedResults.ValidationProblem` | Yes                                                                 |

The conversion is on since v5; on 4.x it appeared off, in v4.10.0. To keep the ASP.NET Core format, turn it off:

```csharp
builder.Services.Configure<TooarkProblemDetailsOptions>(options => options.ConvertValidationProblems = false);
```

To convert by hand, in both directions, see [conversion between the formats](#conversion-between-the-validation-formats).

**OpenAPI.** `ProblemDetails` is already the error type ASP.NET Core assumes for `[ApiController]`, so a
`[ProducesResponseType(StatusCodes.Status404NotFound)]` documents the response; in minimal APIs,
`.ProducesProblem(StatusCodes.Status404NotFound)` does the same. The `errors` extension is not described by the
framework. With the validation failure conversion, which is on by default, document the validation failure as
`ProblemDetails` (`.ProducesProblem(StatusCodes.Status400BadRequest)`, or
`[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]` in controllers), not with
`.ProducesValidationProblem()`: the `HttpValidationProblemDetails` schema describes `errors` as an object per field.

### Results for minimal APIs

- `TooarkResults.Problem(TooarkException)`: answers an exception without throwing it, with the body the handler
  writes.
- `TooarkResults.ValidationProblem(Notification, int statusCode = 400)`: answers the notifications of an entity
  or a value object, 400 by default.
- `TooarkResults.ValidationProblem(IEnumerable<NotificationItem>, int statusCode = 400)`: the same, from a list
  of items.

Minimal APIs have no ModelState. The [`Tooark.Entities`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Entities)
entities and the [`Tooark.ValueObjects`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.ValueObjects)
value objects validate themselves and accumulate notifications, and the endpoint returns the problem when
`IsValid` is false, with one item in `errors` per notification. The result fills `instance` with the request
path when it runs, and the status must be an error status, from 400 to 599. A valid notification produces a
problem with an empty `errors` list: check `IsValid` before calling it.

The results do not depend on `AddTooarkProblemDetails`: they translate the messages with the `Tooark.Extensions`
localizer by themselves, in the language of the request.

### Conversion between the validation formats

- `ValidationProblemExtension.ToNotificationItems(IDictionary<string, string[]>)`: converts the ASP.NET Core
  messages per field into Tooark notification items, one per message, with the field as the key and the default
  code `T.ERR`.
- `ValidationProblemExtension.ToNotificationItems(HttpValidationProblemDetails)`: the same, from the validation
  problem.
- `ValidationProblemExtension.ToValidationErrors(IEnumerable<NotificationItem>)`: the way back, with the items
  grouped by key and the messages translated. The code of each item is lost: the ASP.NET Core format has nowhere
  to keep it.

The conversion is explicit, not an implicit operator: a field with several messages becomes several items, and the
way back loses the code, which an implicit conversion would hide. The framework would also never call an operator
when writing the response; that is what `ConvertValidationProblems` is for. A blank message becomes
`Field.Invalid;{field}`, or `BadRequest` without a field, as in `GetErrors`.

```csharp
using Tooark.AspNetCore.Extensions;
using Tooark.AspNetCore.Results;

// From the ASP.NET Core format to the Tooark list
var items = problem.ToNotificationItems();
return TooarkResults.ValidationProblem(items);

// From the Tooark list to the ASP.NET Core format (with the conversion off)
return Results.ValidationProblem(order.Notifications.ToValidationErrors());
```

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
it decides the body of a validation failure, and replacing that factory was enough. The exception handling is
not a middleware either: ASP.NET Core 8 has the `IExceptionHandler` contract, which the `UseExceptionHandler`
middleware already calls, and implementing it was enough. When filters and middlewares exist, they go in this
package, under the `Filters/` and `Middlewares/` folders.
The family only splits a package when the **dependency profile** differs — that is what motivated separating
`Tooark.Mediator.EntityFrameworkCore`, which pulls Entity Framework Core, from `Tooark.Mediator`, which does not
use it. Filters, middlewares and the extensions here share exactly the same profile, so splitting them into
separate packages would reduce nothing.

### Namespaces

The extensions live in `Tooark.AspNetCore.Extensions`, the registrations in `Tooark.AspNetCore.Injections`, the
exception handler and its options in `Tooark.AspNetCore.ExceptionHandling` and the minimal API results in
`Tooark.AspNetCore.Results`.

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

With only the envelope registered, a missing field carries two messages: without `email` in the body, `errors`
gets `"Field Email is required"` and also `"The Email field is required."`, the `[Required]` that MVC infers. The
[next example](#missing-field-without-the-frameworks-message) adds `AddTooarkValidationAttributes`, which keeps
only the first.

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

### Problem Details for the Tooark exceptions

```csharp
using Tooark.AspNetCore.Injections;
using Tooark.Exceptions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTooarkProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();

app.MapGet("/users/{id:guid}", async (Guid id, IUserRepository users) =>
  await users.FindAsync(id) ?? throw new NotFoundException("NotFound.User"));

app.Run();
```

`GET /users/3fa85f64-5717-4562-b3fc-2c963f66afa6`, in `en-US`, for a user that does not exist:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "User not found",
  "instance": "/users/3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "errors": [{ "code": "T.ERR", "key": "Unknown", "message": "User not found" }],
  "traceId": "0HNPTVUT6D34Q"
}
```

`NotFound.User` is a key of the `Tooark.Extensions` resources, so it arrives translated. A message that is not a
key, such as `"User.NotFound"`, arrives as written.

An exception built from a notification carries one item per failure, with the code and the key of each
validation:

```csharp
using Tooark.Exceptions;
using Tooark.ValueObjects;

app.MapPut("/users/{id:guid}/email", (Guid id, ChangeEmailRequest request) =>
{
  var email = new Email(request.Email);

  if (!email.IsValid)
  {
    // 400 with errors: [{ "code": "T.VLD.RGX1", "key": "Email", "message": "Field Email is invalid" }]
    throw new BadRequestException(email);
  }

  // ...
  return Results.NoContent();
});
```

### Validation of an entity in a minimal API

Without an exception, returning the notifications of the entity:

```csharp
using Tooark.AspNetCore.Results;

app.MapPost("/users", (CreateUserRequest request) =>
{
  var user = new User(request.Name, request.Email);

  return user.IsValid
    ? Results.Created($"/users/{user.Id}", user)
    : TooarkResults.ValidationProblem(user);
});
```

`POST /users` with an invalid e-mail, in `pt-BR`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "O campo E-mail é inválido",
  "instance": "/users",
  "errors": [{ "code": "T.VLD.RGX1", "key": "Email", "message": "O campo E-mail é inválido" }],
  "traceId": "0HNPTVUT6D34Q"
}
```

For a 422 instead of a 400, pass the status: `TooarkResults.ValidationProblem(user, StatusCodes.Status422UnprocessableEntity)`.
To answer an exception without throwing it — a result the caller already has in hand — use
`TooarkResults.Problem(new ConflictException("Record.Exists"))`.

### Problem Details in a controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Exceptions;

[ApiController]
[Route("users")]
public sealed class UsersController(IUserRepository users) : ControllerBase
{
  [HttpGet("{id:guid}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Get(Guid id)
  {
    var user = await users.FindAsync(id) ?? throw new NotFoundException("NotFound.User");

    return Ok(user);
  }
}
```

The same `AddTooarkProblemDetails` and `UseExceptionHandler` serve the controller: the exception leaves the
action, crosses the MVC filters and reaches the middleware. The [envelope](#validation-response), when
registered, keeps answering the validation of the `[ApiController]`, which happens before the action.

---

## 📋 Dependencies

| Package                                                                                                  | Version  | Description                                                                                                  |
| -------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------------------------------------------------------------------------------ |
| [`Tooark.Attributes`](https://www.nuget.org/packages/Tooark.Attributes)                                  | 4.x      | The attributes recognized by `AddTooarkValidationAttributes`                                                 |
| [`Tooark.Dtos`](https://www.nuget.org/packages/Tooark.Dtos)                                              | 4.x      | `ResponseDto`, the body of the validation response                                                           |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                  | 4.x      | The exceptions answered with Problem Details, and their notification items                                   |
| [`Tooark.Extensions`](https://www.nuget.org/packages/Tooark.Extensions)                                  | 4.x      | Error message localization                                                                                   |
| [`Microsoft.AspNetCore.App`](https://www.nuget.org/packages/Microsoft.AspNetCore.App) (shared framework) | 10.x     | `ModelStateDictionary`, `ApiBehaviorOptions`, `MvcOptions`, `IExceptionHandler` and `IProblemDetailsService` |

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
