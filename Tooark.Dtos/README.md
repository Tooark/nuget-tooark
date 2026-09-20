# Tooark.Dtos

Library for managing and maintaining base DTOs in .NET projects.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Dtos/README.pt-BR.md)

## Contents

- [Overview](#overview)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Components](#-components)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Contributing](#-contributing)
- [License](#-license)

## Overview

The `Tooark.Dtos` package provides:

- search and pagination DTOs for listing endpoints;
- a standard response with data, errors, pagination and metadata;
- automatic translation of the error keys produced by the Tooark validations;
- a page size limit, so that a request cannot ask for every record.

> **Runtime requirement**: `SearchDto`, `PaginationDto` and `ResponseDto` use MVC, `Http` and `WebUtilities`
> types, so the package requires the ASP.NET Core runtime to be installed. That is expected for API DTOs.

---

## 🔧 Installation

```bash
dotnet add package Tooark.Dtos
```

---

## ⚙️ Configuration

Message translation **requires no configuration**: `ResponseDto` resolves the language from the request's
execution flow and reads the translations from the files shipped with
[`Tooark.Extensions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions).

The registration below exists so the application can inject `IStringLocalizer` into its own types:

```csharp
using Tooark.Dtos.Injections;

builder.Services.AddTooarkDtos();
```

To add or override translations, place a `Resources/{language}.json` in the application output.

---

## 📦 Components

### Dto

Base class of the DTOs that produce translated messages. It has no public members: it exists so that
`ResponseDto` and `SearchDto` share the localizer.

### SearchDto

Search parameters with pagination.

| Member               | Type         | Description                                                                   |
| -------------------- | ------------ | ----------------------------------------------------------------------------- |
| `Search`             | `string?`    | Information to look for. Default: null                                        |
| `SearchNormalized`   | `string?`    | Normalized `Search`, computed once per value. Neither bound nor serialized    |
| `PageIndex`          | `long`       | Page index, starting at 1. A smaller value assumes 1. Default: 1              |
| `PageIndexLogical`   | `long`       | `PageIndex` minus one, for direct use in `Skip`. Neither bound nor serialized |
| `PageSize`           | `long`       | Page size. Negative assumes 0, which means ignoring the size. Default: 10     |
| `PageSizeMax`        | `long`       | `protected virtual`. Ceiling of `PageSize`. Default: `DefaultPageSizeMax`     |
| `DefaultPageSizeMax` | `const long` | Default ceiling: 100                                                          |

### SearchOrderDto

Inherits from `SearchDto` and adds ordering.

| Member     | Type      | Description                          |
| ---------- | --------- | ------------------------------------ |
| `OrderBy`  | `string?` | Name of the column to order by       |
| `OrderAsc` | `bool`    | Ascending when true. Default: `true` |

### PaginationDto

Record total and navigation between pages. Every property is read-only.

| Member                                      | Type      | Description                                        |
| ------------------------------------------- | --------- | -------------------------------------------------- |
| `Total`                                     | `long`    | Total records                                      |
| `PageSize` / `PageIndex`                    | `long`    | Page size and index                                |
| `Previous` / `Next`                         | `long?`   | Indexes of the neighboring pages, null when absent |
| `CurrentLink` / `PreviousLink` / `NextLink` | `string?` | Corresponding URLs                                 |

### ResponseDto&lt;T&gt;

Standard API response.

| Member                                          | Type                         | Description                                    |
| ----------------------------------------------- | ---------------------------- | ---------------------------------------------- |
| `Data`                                          | `T?`                         | Response data                                  |
| `Errors`                                        | `IReadOnlyList<string>`      | Error messages, already translated             |
| `Pagination`                                    | `PaginationDto?`             | Pagination data                                |
| `Metadata`                                      | `IReadOnlyList<MetadataDto>` | Metadata                                       |
| `SetPagination` / `SetMetadata` / `AddMetadata` |                              | Set pagination and metadata after construction |

`Errors` and `Metadata` are read-only collections: casting them to `IList` compiles, but changing them throws
`NotSupportedException`. Use `SetMetadata` or `AddMetadata`.

### MetadataDto

Key/value pair. Key and value are independent — passing one as null results in an empty string, without
discarding the other.

### Page size limit

Without a ceiling, a single request could ask for every record. `PageSize` is limited to
`DefaultPageSizeMax`, which is 100. An endpoint that needs larger pages overrides the limit in its own DTO:

```csharp
public sealed class ReportSearchDto : SearchDto
{
  protected override long PageSizeMax => 5000;
}
```

Use the expression-bodied form. An auto-property with an initializer does not work: the limit is read by
the base class constructor, which runs before the derived class initializers.

### Pagination links

The links reuse the request's query string, replacing only `PageIndex`, so that the endpoint's filters keep
applying while navigating.

Two consequences worth knowing:

- **Every request parameter shows up in the response body.** Do not send credentials in the query string —
  they would come back in the links and from there into logs, caches and browser history.
- **The links are not an access control.** Whoever can call one page can call the others by editing the URL,
  and `Total` already says how many exist. Against mass harvesting, what counts is the `PageSize` ceiling,
  rate limiting and authorization — not hiding the links.

---

## 📝 Usage Examples

### Search with pagination

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Dtos;

[HttpGet]
public async Task<IActionResult> List([FromQuery] SearchDto filter)
{
  var query = _context.People.AsQueryable();

  if (!string.IsNullOrEmpty(filter.SearchNormalized))
  {
    query = query.Where(p => p.NormalizedName.Contains(filter.SearchNormalized));
  }

  var total = await query.LongCountAsync();

  var people = await query
    .Skip((int)(filter.PageIndexLogical * filter.PageSize))
    .Take((int)filter.PageSize)
    .ToListAsync();

  var response = new ResponseDto<List<Person>>(people);
  response.SetPagination(new PaginationDto(total, filter, Request));

  return Ok(response);
}
```

The response:

```json
{
  "data": [ ... ],
  "errors": [],
  "pagination": {
    "total": 95,
    "pageSize": 10,
    "pageIndex": 9,
    "previous": 8,
    "next": 10,
    "currentLink": "https://api.example.com/people?PageIndex=9&PageSize=10",
    "previousLink": "https://api.example.com/people?PageIndex=8&PageSize=10",
    "nextLink": "https://api.example.com/people?PageIndex=10&PageSize=10"
  },
  "metadata": []
}
```

> **Do not name the parameter `search`.** `SearchDto` has a `Search` property, and ASP.NET Core emits the
> `MVC1004` warning when the parameter name matches a property of the bound type, because prefix resolution
> becomes ambiguous. Any other name works.

### Search with ordering

```csharp
using Tooark.Dtos;

[HttpGet]
public IActionResult List([FromQuery] SearchOrderDto filter)
{
  var query = _context.People.OrderByProperty(filter.OrderBy ?? nameof(Person.Name));

  // ...
}
```

### Response with validation errors

```csharp
using Tooark.Dtos;

var person = new Person(name, email);

// The invalid notification becomes the error list, already translated
if (!person.IsValid)
{
  // ["The Name field is required"]
  return BadRequest(new ResponseDto<Person>(person));
}
```

With the error code in front of the message:

```csharp
// ["T.VLD.STR5: The Name field is required"]
return BadRequest(new ResponseDto<Person>(person.Notification, withCode: true));
```

### Endpoint with a larger page

```csharp
using Tooark.Dtos;

public sealed class ExportSearchDto : SearchDto
{
  protected override long PageSizeMax => 5000;
}

[HttpGet("export")]
public IActionResult Export([FromQuery] ExportSearchDto filter)
{
  // filter.PageSize accepts up to 5000 in this endpoint
}
```

### Metadata

```csharp
using Tooark.Dtos;

var response = new ResponseDto<List<Person>>(people);

response.AddMetadata(new MetadataDto("version", "2024-01"));
response.SetMetadata([new MetadataDto("source", "cache")]);
```

---

## 📋 Dependencies

| Package                                                                                                  | Version  | Description                                   |
| -------------------------------------------------------------------------------------------------------- | -------- | --------------------------------------------- |
| [`Tooark.Extensions`](https://www.nuget.org/packages/Tooark.Extensions)                                  | 4.x      | Message localization and search normalization |
| [`Tooark.Notifications`](https://www.nuget.org/packages/Tooark.Notifications)                            | 4.x      | Notifications that become the response errors |
| [`Microsoft.AspNetCore.App`](https://www.nuget.org/packages/Microsoft.AspNetCore.App) (shared framework) | 8.x/10.x | `HttpRequest`, `QueryHelpers` and `BindNever` |

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Dtos](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
