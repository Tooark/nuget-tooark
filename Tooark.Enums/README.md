# Tooark.Enums

Library that provides validated enumerated types, standardizing them for .NET projects. Includes methods to convert and validate enumerated values.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Enums/README.pt-BR.md)

## Contents

- [Installation](#installation)
- [How they work](#how-they-work)
- [ECloudProvider](#1-cloud-provider)
- [EDocumentType](#2-document-type)
- [EFileType](#3-file-type)
- [Usage Examples](#usage-examples)
- [Dependencies](#dependencies)
- [Contributing](#contributing)
- [License](#license)

## Installation

```bash
dotnet add package Tooark.Enums
```

The package has no configuration: the enumerators are static values, used directly.

## How they work

The three types are classes with static instances, not C# `enum`s. That lets each value carry more than a
number — description, format pattern, validation function — at the cost of not being usable as attribute
arguments, which require constants.

All of them expose the same set of operations:

| Operation                | Behavior                                                                   |
| ------------------------ | -------------------------------------------------------------------------- |
| `ToString()`             | Returns the description                                                    |
| `ToInt()`                | Returns the id                                                             |
| implicit `(int)`         | Same as `ToInt()`                                                          |
| implicit `(string)`      | Same as `ToString()`                                                       |
| implicit `int` → enum    | Resolves by id                                                             |
| implicit `string` → enum | Resolves by description, **ignoring case and leading/trailing whitespace** |

Two rules apply to all three:

- **The description is normalized.** `"aws"`, `"AWS"` and `" Aws "` resolve to the same value.
- **An unrecognized id or description resolves to the neutral value** — `None` in `ECloudProvider` and
  `EDocumentType`, `Unknown` in `EFileType` —, which is the member meant for that. **Converting a null
  instance** to `int` or `string`, on the other hand, throws `InternalServerErrorException` with
  `Invalid.Parameter;null`: there is no correct id or description to return, and returning zero would be
  making data up.

## Enumerators

### 1. Cloud Provider

**Purpose:** represents the supported cloud providers.

| Value       | Id  | Description | Also accepts |
| ----------- | --- | ----------- | ------------ |
| `None`      | 0   | `None`      | —            |
| `Amazon`    | 1   | `AWS`       | `Amazon`     |
| `Google`    | 2   | `GCP`       | `Google`     |
| `Microsoft` | 3   | `Azure`     | `Microsoft`  |

[**Usage Example**](#cloud-provider)

### 2. Document Type

**Purpose:** represents the document types, each with its own format and check.

| Value        | Id  | Check                                                  |
| ------------ | --- | ------------------------------------------------------ |
| `None`       | 0   | Accepts any document                                   |
| `CPF`        | 1   | Format and check digits                                |
| `RG`         | 2   | Format and check digit, when provided                  |
| `CNH`        | 3   | Format and check digits                                |
| `CNPJ`       | 4   | Format and check digits, accepts the alphanumeric CNPJ |
| `CPF_CNPJ`   | 5   | CPF or CNPJ, both with check digits                    |
| `CPF_RG`     | 6   | CPF or RG                                              |
| `CPF_RG_CNH` | 7   | CPF, RG or CNH                                         |

**Own methods:**

- `ToRegex()`: returns the **format** pattern of the document type.
- `IsValid`: function that verifies the **check digits**, accepting the value with or without mask.

> The two checks are distinct layers that complement each other: `ToRegex()` handles the shape and `IsValid`
> handles the content. A complete validation applies both, as the `Document` value object in
> `Tooark.ValueObjects` and the `DocumentValidationAttribute` in `Tooark.Attributes` do. Called on its own,
> `IsValid` accepts `529.982.247-25` and `52998224725` alike, and never throws for invalid input — it returns
> `false`.

The digit calculation comes from `DocumentDigit`, in `Tooark.Validations`, the same one used by that package's
validations — so `new Validation().IsCpf(...)` and `EDocumentType.CPF.IsValid(...)` always agree.

[**Usage Example**](#document-type)

### 3. File Type

**Purpose:** represents the file categories.

| Value      | Id  | Description |
| ---------- | --- | ----------- |
| `Unknown`  | 0   | `Unknown`   |
| `Document` | 1   | `Document`  |
| `Image`    | 2   | `Image`     |
| `Video`    | 3   | `Video`     |
| `Audio`    | 4   | `Audio`     |

[**Usage Example**](#file-type)

## Usage Examples

### Cloud Provider

```csharp
using Tooark.Enums;

ECloudProvider provider = ECloudProvider.Amazon;

Console.WriteLine(provider.ToString()); // AWS
Console.WriteLine(provider.ToInt());    // 1

// Implicit conversions, handy to persist and read back
int id = provider;                      // 1
string description = provider;          // "AWS"

// The description is case-insensitive
ECloudProvider fromConfiguration = "aws";  // Amazon
ECloudProvider fromDatabase = 1;           // Amazon
```

### Document Type

```csharp
using Tooark.Enums;

EDocumentType docType = EDocumentType.CPF;

Console.WriteLine(docType.ToString()); // CPF
Console.WriteLine(docType.ToInt());    // 1
Console.WriteLine(docType.ToRegex());  // ^\d{3}\.\d{3}\.\d{3}-\d{2}$

// IsValid verifies the digits, with or without mask
Console.WriteLine(docType.IsValid("529.982.247-25")); // True
Console.WriteLine(docType.IsValid("52998224725"));    // True
Console.WriteLine(docType.IsValid("529.982.247-24")); // False
```

Complete validation, applying format and digits:

```csharp
using System.Text.RegularExpressions;
using Tooark.Enums;

bool IsValidDocument(string document, EDocumentType type) =>
  Regex.IsMatch(document, type.ToRegex()) && type.IsValid(document);
```

### File Type

```csharp
using Tooark.Enums;

EFileType fileType = EFileType.Image;

Console.WriteLine(fileType.ToString()); // Image
Console.WriteLine(fileType.ToInt());    // 2

// An unrecognized value resolves to Unknown
EFileType unknown = "spreadsheet";      // Unknown
```

## Dependencies

| Dependency                                                                | Version | Usage                                   |
| ------------------------------------------------------------------------- | ------- | --------------------------------------- |
| [`Tooark.Validations`](https://www.nuget.org/packages/Tooark.Validations) | 4.x     | `DocumentDigit` and the format patterns |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)   | 4.x     | Null-instance conversion error          |

## Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Enums](https://github.com/Tooark/nuget-tooark/issues) repository.

## License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
