# Tooark.ValueObjects

Library of value objects that validate themselves on construction, for .NET projects.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.ValueObjects/README.pt-BR.md)

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

The `Tooark.ValueObjects` package provides:

- 33 value objects that validate themselves on construction, without throwing;
- translated notifications instead of exceptions, inherited from `Notification`;
- implicit conversion in both directions, so the type behaves like the value it represents;
- Brazilian documents with check digit verification: CPF, CNPJ, RG and CNH.

**How it works.** A value object never throws for invalid data: it is born with notifications. Whoever
receives it checks `IsValid` before using the value.

```csharp
var cpf = new Cpf("111.111.111-11");

cpf.IsValid       // false
cpf.Number        // "" — an invalid object carries no value
cpf.Notifications // the messages of what failed
```

An invalid object **never returns null**: it returns the empty value of its own type — an empty string in
the 29 text objects, `Guid.Empty` in the four audit ones. That also holds for `ToString()`, so
interpolation, concatenation and serialization are safe without a prior check.

---

## 🔧 Installation

```bash
dotnet add package Tooark.ValueObjects
```

---

## ⚙️ Configuration

**Value objects require no registration at all.** They validate on their own and, when something fails,
keep the message _key_ — `Field.Invalid;Email`, for instance. The layer that builds the response
translates the key, not the value object.

The registration below only forwards to `AddTooarkExtensions()`, which makes the `IStringLocalizer` and
the language files available to the application:

```csharp
using Tooark.ValueObjects.Injections;

builder.Services.AddTooarkValueObjects();
```

---

## 📦 Components

### Documents

Validate format **and** check digit.

| Type       | Constructor                                   | Property         |
| ---------- | --------------------------------------------- | ---------------- |
| `Cpf`      | `(string number)`                             | `Number`         |
| `Cnpj`     | `(string number)`                             | `Number`         |
| `Rg`       | `(string number)`                             | `Number`         |
| `Cnh`      | `(string number)`                             | `Number`         |
| `CpfCnpj`  | `(string number)`                             | `Number`         |
| `CpfRg`    | `(string number)`                             | `Number`         |
| `CpfRgCnh` | `(string number)`                             | `Number`         |
| `Document` | `(string number, EDocumentType? type = null)` | `Number`, `Type` |

`Document` is the generic form: without a type it assumes `EDocumentType.None`, which accepts any
alphanumeric text — `new Document("111")` is valid, `new Document("111", EDocumentType.CPF)` is not.
A rejected document has an empty `Number` and `Type` equal to `None`.

### Text

| Type            | Constructor      | Properties            |
| --------------- | ---------------- | --------------------- |
| `Name`          | `(string value)` | `Value`, `Normalized` |
| `Title`         | `(string value)` | `Value`, `Normalized` |
| `Description`   | `(string value)` | `Value`, `Normalized` |
| `Keyword`       | `(string value)` | `Value`, `Normalized` |
| `Letter`        | `(string value)` | `Value`               |
| `Numeric`       | `(string value)` | `Value`               |
| `LetterNumeric` | `(string value)` | `Value`               |
| `LanguageCode`  | `(string code)`  | `Code`                |
| `ZipCode`       | `(string value)` | `Value`               |

`Normalized` returns the value without accents, without spaces and in uppercase — useful for searching
and sorting. `LanguageCode` normalizes to the `xx-XX` format (e.g. `pt-BR`) and exposes the
`LanguageCode.Length` constant (5) for external use, such as sizing columns.

### Addresses and protocols

| Type                    | Constructor                                                                      | Properties     | Accepts                                              |
| ----------------------- | -------------------------------------------------------------------------------- | -------------- | ---------------------------------------------------- |
| `Email`                 | `(string value)`                                                                 | `Value`        | Email address                                        |
| `EmailDomain`           | `(string value)`                                                                 | `Value`        | Email domain                                         |
| `Url`                   | `(string value)`                                                                 | `Value`        | FTP, SFTP, HTTP, HTTPS, IMAP, POP3, SMTP, WS and WSS |
| `ProtocolHttp`          | `(string value)`                                                                 | `Value`        | HTTP and HTTPS                                       |
| `ProtocolFtp`           | `(string value)`                                                                 | `Value`        | FTP and SFTP                                         |
| `ProtocolWs`            | `(string value)`                                                                 | `Value`        | WS and WSS                                           |
| `ProtocolEmailSender`   | `(string value)`                                                                 | `Value`        | SMTP                                                 |
| `ProtocolEmailReceiver` | `(string value)`                                                                 | `Value`        | IMAP and POP3                                        |
| `LinkVideo`             | `(string link, bool youtube = true, bool vimeo = true, bool dailymotion = true)` | `Link`         | YouTube, Vimeo and Dailymotion                       |
| `FileStorage`           | `(ProtocolHttp link, string? name = null)`                                       | `Link`, `Name` | File link and name                                   |

Note that `LinkVideo` and `FileStorage` expose `Link`, not `Value`. A `FileStorage` built without a name
uses the link itself as `Name`. `Email` also has the constants `Email.MinLength` (6) and
`Email.MaxLength` (255).

`Email` keeps the value in lowercase. The required format is stricter than RFC 5322: the local part and
the domain need at least two characters, `+` is not accepted and leading/trailing spaces reject the value
instead of being trimmed.

### Auditing

| Type         | Constructor    | Property |
| ------------ | -------------- | -------- |
| `CreatedBy`  | `(Guid value)` | `Value`  |
| `UpdatedBy`  | `(Guid value)` | `Value`  |
| `DeletedBy`  | `(Guid value)` | `Value`  |
| `RestoredBy` | `(Guid value)` | `Value`  |

Reject `Guid.Empty`. An invalid object has `Value` equal to `Guid.Empty`.

### Password

| Member                                                                                                                          | Description                                                         |
| ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------- |
| `Password(string? value, bool lowercase = true, bool uppercase = true, bool number = true, bool symbol = true, int length = 8)` | Complexity criteria                                                 |
| `Value`                                                                                                                         | The plain-text value                                                |
| `Mask`                                                                                                                          | The `********` mask, returned by `ToString()` when there is a value |

`Password` **does not leak the value**. `ToString()` returns the mask, and there is no implicit
conversion to text — the value only comes out through `Value`, explicitly:

```csharp
var password = new Password("Password@123");

password.ToString()      // "********"
$"{password}"            // "********"
logger.LogInformation("{Password}", password);   // logs the mask
password.Value           // "Password@123" — the only path, and it is obvious when reading
```

The criteria apply exactly as given: disabling all of them means requiring only the length, which is a
legitimate passphrase policy. The rule is the same applied by the `PasswordValidationAttribute` of
[`Tooark.Attributes`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Attributes), because both
use the `PasswordPattern` of `Tooark.Validations`.

### Delimited string

| Member                                     | Description                        |
| ------------------------------------------ | ---------------------------------- |
| `DelimitedString(string? value)`           | From a `;`-separated text          |
| `DelimitedString(params string[]? values)` | From an array                      |
| `DelimitedString(List<string>? values)`    | From a list                        |
| `Value`                                    | The delimited text                 |
| `Values`, `ToArray()`, `ToList()`          | The items, always as a copy        |
| `DefaultDelimiter`                         | The default delimiter constant `;` |

Collections come in and go out as copies: changing the given array, or the returned one, does not change
the object.

### Implicit conversions

Every object converts in both directions with the type it represents — `DelimitedString` converts with
three: `string`, `string[]` and `List<string>`:

```csharp
Cpf cpf = "529.982.247-25";   // string -> value object
string number = cpf;          // value object -> string
```

The **outbound** conversion requires an instance: converting a null reference throws
`InternalServerErrorException` with `Invalid.Parameter;null`, instead of a context-less
`NullReferenceException`.

The **inbound** conversion creates the object and validates it. It does not throw for invalid data — it
produces an object with notifications, and whoever receives it checks `IsValid`.

---

## 📝 Usage Examples

### Validating on input

```csharp
using Tooark.ValueObjects;

var cpf = new Cpf(dto.Cpf);
var email = new Email(dto.Email);

if (!cpf.IsValid || !email.IsValid)
{
  // ["Field.Invalid;Document", "Field.Invalid;Email"] — keys, not yet translated
  IEnumerable<string> errors = [.. cpf.Messages, .. email.Messages];

  return BadRequest(errors);
}
```

The messages come out as keys, not as final text. To return them translated, hand the notifications to
the `ResponseDto` of [`Tooark.Dtos`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Dtos), which
resolves the consumer's language:

```csharp
var notifications = cpf.Notifications.Concat(email.Notifications).ToList();

// {"Errors": ["The Document field is invalid", "The E-mail field is invalid"], ...}
return BadRequest(new ResponseDto<string>(notifications));
```

### Composing in an entity

```csharp
using Tooark.Notifications;
using Tooark.ValueObjects;

public sealed class Person : Notification
{
  public Person(string name, string email, string cpf)
  {
    var nameVo = new Name(name);
    var emailVo = new Email(email);
    var cpfVo = new Cpf(cpf);

    // The value object notifications bubble up to the entity
    AddNotifications(nameVo, emailVo, cpfVo);

    if (IsValid)
    {
      Name = nameVo;
      Email = emailVo;
      Cpf = cpfVo;
    }
  }

  public Name Name { get; } = null!;
  public Email Email { get; } = null!;
  public Cpf Cpf { get; } = null!;
}
```

### Searching with the normalized value

```csharp
using Tooark.ValueObjects;

var title = new Title("Ação e Reação");

title.Value       // "Ação e Reação"
title.Normalized  // "ACAOEREACAO"
```

### Invalid object as text

```csharp
using Tooark.ValueObjects;

var cpf = new Cpf("111");

cpf.IsValid          // false
cpf.Number           // ""
cpf.ToString()       // "" — never null
$"document: {cpf}"   // "document: "
```

### Working with a delimited string

```csharp
using Tooark.ValueObjects;

DelimitedString tags = "csharp;dotnet;tooark";

tags.Values   // ["csharp", "dotnet", "tooark"]
tags.Value    // "csharp;dotnet;tooark"

DelimitedString others = new[] { "a", "b" };
string text = others;   // "a;b"
```

---

## 📋 Dependencies

| Package                                                                       | Version | Description                           |
| ----------------------------------------------------------------------------- | ------- | ------------------------------------- |
| [`Tooark.Enums`](https://www.nuget.org/packages/Tooark.Enums)                 | 4.x     | Document types                        |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)       | 4.x     | Error of conversions without instance |
| [`Tooark.Extensions`](https://www.nuget.org/packages/Tooark.Extensions)       | 4.x     | Text normalization                    |
| [`Tooark.Notifications`](https://www.nuget.org/packages/Tooark.Notifications) | 4.x     | Notification base                     |
| [`Tooark.Validations`](https://www.nuget.org/packages/Tooark.Validations)     | 4.x     | Validation rules                      |

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.ValueObjects](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
