# Tooark.Attributes

Library with attribute validators for properties or fields, integrated with `System.ComponentModel.DataAnnotations`.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Attributes/README.pt-BR.md)

## Installation

```bash
dotnet add package Tooark.Attributes
```

## Contents

- [DocumentValidationAttribute](#1-document-validation)
- [EmailValidationAttribute](#2-email-validation)
- [LinkVideoValidationAttribute](#3-video-link-validation)
- [PasswordValidationAttribute](#4-password-validation)
- [UrlValidationAttribute](#5-url-validation)
- [ZipCodeValidationAttribute](#6-zip-code-validation)
- [Common behavior](#common-behavior)

## Validation Attributes

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

## Common behavior

Every attribute inherits from `TooarkValidationAttribute` and shares the rules below.

**Error messages.** The message is returned in the `ValidationResult` of each validation, and never written to `ErrorMessage`. That matters because the validation framework reuses the same attribute instance across every validation of that field, including concurrent ones: writing to the attribute would mix one request's message with another's.

Without configuration, the message is a translation key in the `Key;Field` format:

| Situation                     | Message                  |
| ----------------------------- | ------------------------ |
| Field not provided            | `Field.Required;{field}` |
| Value does not match the rule | `Field.Invalid;{field}`  |

If you configure `ErrorMessage` or `ErrorMessageResourceName` on the attribute, **that message is used instead of the key**.

**Missing value.** A null, empty or whitespace-only value is reported as `Field.Required`. In other words, the attributes **imply being required** — they do not follow the `DataAnnotations` convention, in which validators other than `[Required]` accept null. For an optional field, validate outside the attribute or apply it conditionally.

**Non-text values.** The value is converted with `ToString()` before validation, so a `Uri` or a custom type with a suitable `ToString()` works.

**Regular expression timeout.** Every regular expression runs with a 300 ms limit. Input that causes excessive backtracking is **rejected**, and the exception does not escape the attribute.

**Configuration error.** An impossible configuration — unknown document type, video link with no provider — throws `InternalServerErrorException` on the first validation. It is not a failure of the data but of the attribute applied in code; the keys are in `Tooark.Attributes.Messages.AttributeErrorMessages`.

## Usage Example

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

## Dependencies

- [`Tooark.Enums`](https://www.nuget.org/packages/Tooark.Enums)
- [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)
- [`Tooark.Validations`](https://www.nuget.org/packages/Tooark.Validations)

## Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Attributes](https://github.com/Tooark/nuget-tooark/issues) repository.

## License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
