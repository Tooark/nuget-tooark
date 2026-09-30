# Tooark.Attributes

Library with attribute validators for properties, fields or parameters, integrated with `System.ComponentModel.DataAnnotations`.

📖 **Docs:** [Tooark.Attributes on the site](https://tooark.com/nuget-tooark/packages/tooark.attributes.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Attributes/README.pt-BR.md)

---

## 📑 Contents

- [Installation](#-installation)
- [Validation Attributes](#-validation-attributes)
  - [DocumentValidationAttribute](#1-document-validation)
  - [EmailValidationAttribute](#2-email-validation)
  - [LinkVideoValidationAttribute](#3-video-link-validation)
  - [PasswordValidationAttribute](#4-password-validation)
  - [UrlValidationAttribute](#5-url-validation)
  - [ZipCodeValidationAttribute](#6-zip-code-validation)
- [Common behavior](#-common-behavior)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 🔧 Installation

```bash
dotnet add package Tooark.Attributes
```

---

## ✅ Validation Attributes

Every attribute accepts `propertyName`, which sets the field name used in the error message.

### 1. Document Validation

**Purpose:**
Validates the document by format, with a regular expression, and by check digits.

**Parameters:**

- `string type`: Type of document to validate. Required.
- `string propertyName`: Field name in the error message. Default: `"Document"`.

The type is received as **text** because attribute arguments accept constants only — a parameter of type `EDocumentType` would prevent the attribute from being applied (`CS0181`). Accepted values, case-insensitive: `CPF`, `RG`, `CNH`, `CNPJ`, `CPF_CNPJ`, `CPF_RG`, `CPF_RG_CNH` and `None`.

An unrecognized type is a configuration error and makes the attribute throw `InternalServerErrorException` on the first validation, with the message `Attributes.DocumentTypeUnknown;{type}`.

[**Usage Example**](#document-validation)

### 2. Email Validation

**Purpose:**
Validates whether the value is a valid email address.

**Parameters:**

- `string propertyName`: Field name in the error message. Default: `"Email"`.

[**Usage Example**](#email-validation)

### 3. Video Link Validation

**Purpose:**
Validates whether the value is a video link from one of the enabled providers.

**Parameters:**

- `string propertyName`: Field name in the error message. Default: `"Link"`.
- `bool youtube`: Allows YouTube links. Default: `true`.
- `bool vimeo`: Allows Vimeo links. Default: `true`.
- `bool dailymotion`: Allows Dailymotion links. Default: `true`.

Disabling all three providers is a configuration error — no link could be accepted — and makes the attribute throw `InternalServerErrorException` with the message `Attributes.LinkVideoNoProvider;{field}`.

[**Usage Example**](#video-link-validation)

### 4. Password Validation

**Purpose:**
Validates whether the password meets the configured complexity criteria.

**Parameters:**

- `bool lowercase`: Requires a lowercase character. Default: `true`.
- `bool uppercase`: Requires an uppercase character. Default: `true`.
- `bool number`: Requires a numeric character. Default: `true`.
- `bool symbol`: Requires a special character. Default: `true`.
- `int length`: Minimum length. Default: `8`. A non-positive value assumes `1`.
- `string propertyName`: Field name in the error message. Default: `"Password"`.

The criteria apply exactly as configured. Disabling all of them means requiring **only the length**, which is a legitimate policy: long passwords with no composition rule.

[**Usage Example**](#password-validation)

### 5. URL Validation

**Purpose:**
Validates whether the value is a valid URL in the email (sending and receiving), FTP, HTTP or WebSocket protocols.

**Parameters:**

- `string propertyName`: Field name in the error message. Default: `"Url"`.

[**Usage Example**](#url-validation)

### 6. Zip Code Validation

**Purpose:**
Validates whether the value is a valid zip code.

**Parameters:**

- `string propertyName`: Field name in the error message. Default: `"ZipCode"`.

[**Usage Example**](#zip-code-validation)

---

## 🧠 Common behavior

Every attribute inherits from `TooarkValidationAttribute` and shares the rules below.

**Error messages.** The message is returned in the `ValidationResult` of each validation, and never written to `ErrorMessage`. That matters because the validation framework reuses the same attribute instance across every validation of that field, including concurrent ones: writing to the attribute would mix one request's message with another's.

Without configuration, the message is a translation key in the `Key;Field` format:

| Situation                     | Message                  |
| ----------------------------- | ------------------------ |
| Field not provided            | `Field.Required;{field}` |
| Value does not match the rule | `Field.Invalid;{field}`  |

If you configure `ErrorMessage` or `ErrorMessageResourceName` on the attribute, **that message is used instead of the key**.

**Missing value.** A null, empty or whitespace-only value is reported as `Field.Required`. In other words, the attributes **imply being required** — they do not follow the `DataAnnotations` convention, in which validators other than `[Required]` accept null. For an optional field, validate outside the attribute or apply it conditionally.

**Where to apply.** The attributes are valid on properties, fields and parameters, like the `DataAnnotations` ones. On a positional record, apply the attribute with no target, on the constructor parameter (`record Dto([EmailValidation] string Email)`). That is where MVC looks for a record's validation, and it throws `InvalidOperationException` when it finds it on the generated property (`[property: EmailValidation]`). An action parameter also accepts the attribute (`[FromQuery][EmailValidation] string email`). `Validator.TryValidateObject` goes the other way: it only reads properties, and ignores the attribute on the constructor parameter. A record validated outside MVC needs the attribute on the property, which MVC rejects, so the same record cannot serve both paths.

**ASP.NET Core implicit required.** With `<Nullable>enable</Nullable>`, MVC treats every non-nullable reference type as if it had `[Required]`, with the framework's message (`The Email field is required.`), which is not a translation key and reaches the client as is. Since the attribute already reports the missing value, a missing non-nullable field gets both messages. To keep only the attribute's key, register `AddTooarkValidationAttributes()` from [`Tooark.AspNetCore`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.AspNetCore/README.md#tooark-attributes-in-mvc-validation). MVC stops inferring `[Required]` only on members with a Tooark attribute, and keeps the inference on the others and the declared `[Required]`. Without `Tooark.AspNetCore`, there are two manual ways out:

- declare the field as nullable (`[EmailValidation] string? Email`). MVC does not infer `[Required]`, and the attribute still rejects the missing value. It applies to that field only; the cost is the nullable type in the code that reads the DTO;
- or turn the inference off for the whole application, with `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true` in `MvcOptions`. It reaches every DTO: a field without a Tooark attribute is no longer required until it gets an explicit `[Required]`.

**Non-text values.** The value is converted with `ToString()` before validation, so a `Uri` or a custom type with a suitable `ToString()` works.

**Regular expression timeout.** Every regular expression runs with a 300 ms limit. Input that causes excessive backtracking is **rejected**, and the exception does not escape the attribute.

**Configuration error.** An impossible configuration — unknown document type, video link with no provider — throws `InternalServerErrorException` on the first validation. It is not a failure of the data but of the attribute applied in code; the keys are in `Tooark.Attributes.Messages.AttributeErrorMessages`.

---

## 📝 Usage Examples

### Document Validation

```csharp
using Tooark.Attributes;

public class Person
{
  [DocumentValidation("CPF")]
  public string Cpf { get; set; } = null!;

  [DocumentValidation("CPF_CNPJ", propertyName: "Document")]
  public string Document { get; set; } = null!;
}
```

### Email Validation

```csharp
using Tooark.Attributes;

public class Contact
{
  [EmailValidation]
  public string Email { get; set; } = null!;

  // With a custom message, which takes precedence over the default key
  [EmailValidation(ErrorMessage = "Provide a corporate email")]
  public string CorporateEmail { get; set; } = null!;
}
```

### Video Link Validation

```csharp
using Tooark.Attributes;

public class Lesson
{
  [LinkVideoValidation]
  public string Video { get; set; } = null!;

  // YouTube only, with a custom field name in the message
  [LinkVideoValidation("Presentation", youtube: true, vimeo: false, dailymotion: false)]
  public string Presentation { get; set; } = null!;
}
```

### Password Validation

```csharp
using Tooark.Attributes;

public class Credential
{
  [PasswordValidation]
  public string Password { get; set; } = null!;

  // Passphrase: no composition rule, minimum length of 20
  [PasswordValidation(false, false, false, false, 20)]
  public string Passphrase { get; set; } = null!;
}
```

### URL Validation

```csharp
using Tooark.Attributes;

public class Site
{
  [UrlValidation]
  public string Address { get; set; } = null!;
}
```

### Zip Code Validation

```csharp
using Tooark.Attributes;

public class Address
{
  [ZipCodeValidation]
  public string ZipCode { get; set; } = null!;
}
```

### Records and action parameters

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Attributes;

// Positional record: the attribute goes on the constructor parameter, with no target
public sealed record CreateContactDto(
  [EmailValidation] string Email,
  [DocumentValidation("CPF")] string Cpf
);

[ApiController]
[Route("contacts")]
public sealed class ContactController : ControllerBase
{
  [HttpPost]
  public IActionResult Create([FromBody] CreateContactDto dto) => Ok();

  // Action parameter validated directly by the attribute
  [HttpGet]
  public IActionResult Find([FromQuery][EmailValidation] string email) => Ok();
}
```

### Reading the validation result

```csharp
using System.ComponentModel.DataAnnotations;

var person = new Person { Cpf = "11111111111" };
var results = new List<ValidationResult>();

Validator.TryValidateObject(person, new ValidationContext(person), results, true);

foreach (var result in results)
{
  // result.ErrorMessage -> "Field.Invalid;Document"
  // result.MemberNames  -> ["Cpf"]
}
```

---

## 📋 Dependencies

- [`Tooark.Enums`](https://www.nuget.org/packages/Tooark.Enums)
- [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)
- [`Tooark.Validations`](https://www.nuget.org/packages/Tooark.Validations)

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
