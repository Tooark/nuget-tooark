# Tooark.Validations

Library for validating types and patterns, providing methods that ensure data integrity and conformity for .NET projects.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Validations/README.pt-BR.md)

## Contents

- [Boolean Validation](#1-boolean)
- [Date Validation](#2-dates)
- [Decimal Validation](#3-decimal)
- [Document Validation](#4-documents)
- [Double Validation](#5-double)
- [Email Validation](#6-email)
- [Float Validation](#7-float)
- [Guid Validation](#8-guid)
- [Int Validation](#9-int)
- [Video Link Validation](#10-video-link)
- [List Validation](#11-lists)
- [Long Validation](#12-long)
- [Network Validation](#13-network)
- [Object Validation](#14-object)
- [Protocol Validation](#15-protocol)
- [Regex Validation](#16-regex)
- [String Validation](#17-string)
- [TimeSpan Validation](#18-timespan)
- [Type Validation](#19-types)
- [Usage Examples](#usage-examples)
- [Available Methods](#available-methods)
- [Error Messages](#error-messages)
- [Error Codes](#error-codes)
- [Dependencies](#dependencies)
- [Contributing](#contributing)
- [License](#license)

## Installation

```bash
dotnet add package Tooark.Validations
```

The package has no configuration: `Validation` is used by composition, creating an instance and chaining the
checks, or by inheritance, in classes that already derive from `Notification` — such as the Tooark entities
and value objects.

## Validations

The available validations are:

### 1. Boolean

**Purpose:**
Validations for boolean values.

[**Usage Example**](#boolean)

### 2. Dates

**Purpose:**
Validations for date values.

[**Usage Example**](#dates)

### 3. Decimal

**Purpose:**
Validations for decimal values.

[**Usage Example**](#decimal)

### 4. Documents

**Purpose:**
Validations for documents.

**Document Types:**

| Document         | Validation                               |
| ---------------- | ---------------------------------------- |
| `CPF`            | Format and check digits                  |
| `CNPJ`           | Format and check digits                  |
| `RG`             | Format only                              |
| `CNH`            | Format only                              |
| `CPF or RG`      | CPF with digits, or RG by format         |
| `CPF, RG or CNH` | CPF with digits, or RG and CNH by format |
| `CPF or CNPJ`    | Both with check digits                   |

CPF and CNPJ are validated by format **and** by check digits. Sequences of a single repeated character (`111.111.111-11`, `00.000.000/0000-00`) satisfy modulo 11 and are rejected by a dedicated rule. RG and CNH have no nationwide standard check digit and are validated by format only.

**Alphanumeric CNPJ:**
The calculation follows the material published by Serpro, in force since July 2026: the first twelve characters admit letters and digits, and the two check digits remain numeric. The value of each character is its ASCII code minus 48 — digits keep their own value and letters take 17 (`A`) through 42 (`Z`) —, with weights 2 to 9 distributed from right to left, restarting after the eighth character. Since digits keep their value, the same calculation covers the previous numeric CNPJs.

Letter case is normalized before the calculation, so `12.abc.345/01DE-35` and `12.ABC.345/01DE-35` are the same CNPJ.

The check is also available outside the validation, through `DocumentDigit.IsCpf` and `DocumentDigit.IsCnpj`.

[**Usage Example**](#documents)

### 5. Double

**Purpose:**
Validations for double values.

[**Usage Example**](#double)

### 6. Email

**Purpose:**
Validations for email addresses.

[**Usage Example**](#email)

### 7. Float

**Purpose:**
Validations for float values.

[**Usage Example**](#float)

### 8. Guid

**Purpose:**
Validations for guid values.

[**Usage Example**](#guid)

### 9. Int

**Purpose:**
Validations for integer values.

[**Usage Example**](#int)

### 10. Video Link

**Purpose:**
Validations for video links, by URL format.

**Platforms:**

| Method                   | Accepts                       |
| ------------------------ | ----------------------------- |
| `IsLinkVideo`            | YouTube, Vimeo or Dailymotion |
| `IsLinkVideoYouTube`     | YouTube only                  |
| `IsLinkVideoVimeo`       | Vimeo only                    |
| `IsLinkVideoDailymotion` | Dailymotion only              |

The validation is of the **URL format**, not of the video's existence: a well-formed link to a removed video
is still accepted.

[**Usage Example**](#video-link)

### 11. Lists

**Purpose:**
Validations for lists.

[**Usage Example**](#lists)

### 12. Long

**Purpose:**
Validations for long values.

[**Usage Example**](#long)

### 13. Network

**Purpose:**
Validations for network addresses.

**Address Types:**

- `IP`
- `IPv4`
- `IPv6` — accepts the full form and the compressed forms (`::1`, `2001:db8::1`)
- `MacAddress`

[**Usage Example**](#network)

### 14. Object

**Purpose:**
Validations for objects.

[**Usage Example**](#object)

### 15. Protocol

**Purpose:**
Validations for protocols.

**Protocol Types:**

- `Url`
- `Ftp`
- `Sftp`
- `ProtocolFtp`
- `Http`
- `Https`
- `ProtocolHttp`
- `Imap`
- `Pop3`
- `ProtocolEmailReceiver`
- `Smtp`
- `ProtocolEmailSender`
- `Ws`
- `Wss`
- `ProtocolWebSocket`

[**Usage Example**](#protocol)

### 16. Regex

**Purpose:**
Validations for regular expressions. The default evaluation timeout is the public constant
`Validation.DefaultTimeout` (300 ms), available for external use.

[**Usage Example**](#regex)

### 17. String

**Purpose:**
Validations for strings.

[**Usage Example**](#string)

### 18. TimeSpan

**Purpose:**
Validations for time values.

[**Usage Example**](#timespan)

### 19. Types

**Purpose:**
Validations for string types.

**Available Types:**

- `Guid`
- `Letter`
- `LetterLower`
- `LetterUpper`
- `Numeric`
- `LetterNumeric`
- `Hexadecimal`
- `ZipCode`
- `Base64`
- `Password`
- `Culture`
- `CultureIgnoreCase`

[**Usage Example**](#types)

## Usage Examples

### Boolean

```csharp
using Tooark.Validations;

bool value = true;
string property = "Boolean";
var validation = new Validation()
    .IsTrue(value, property, "The value must be true.")
    .IsFalse(value, property, "The value must be false.")
    .Contains(value, bool[], property, "The value must be in the list.")
    .NotContains(value, bool[], property, "The value must not be in the list.")
    .All(value, bool[], property, "Every value in the list must equal the value.")
    .NotAll(value, bool[], property, "No value in the list may equal the value.")
    .IsNull(value, property, "The value must be null.")
    .IsNotNull(value, property, "The value must not be null.")
```

### Dates

```csharp
using Tooark.Validations;

DateTime date = DateTime.Now;
string property = "Date";
var validation = new Validation()
    .IsGreater(date, Comparer, property, "The date must be greater than the compared date.")
    .IsGreaterOrEquals(date, Comparer, property, "The date must be greater than or equal to the compared date.")
    .IsLower(date, Comparer, property, "The date must be lower than the compared date.")
    .IsLowerOrEquals(date, Comparer, property, "The date must be lower than or equal to the compared date.")
    .IsBetween(date, Start, End, property, "The date must be between the dates.")
    .IsNotBetween(date, Start, End, property, "The date must not be between the dates.")
    .IsMin(date, property, "The date must be the minimum value of the type.")
    .IsNotMin(date, property, "The date must not be the minimum value of the type.")
    .IsMax(date, property, "The date must be the maximum value of the type.")
    .IsNotMax(date, property, "The date must not be the maximum value of the type.")
    .AreEquals(date, Comparer, property, "The dates must be equal.")
    .AreNotEquals(date, Comparer, property, "The dates must not be equal.")
    .Contains(date, Datetime[], property, "The date must be in the list.")
    .NotContains(date, Datetime[], property, "The date must not be in the list.")
    .All(date, Datetime[], property, "Every value in the list must equal the date.")
    .NotAll(date, Datetime[], property, "No value in the list may equal the date.")
    .IsNull(date, property, "The date must be null.")
    .IsNotNull(date, property, "The date must not be null.")
```

### Decimal

```csharp
using Tooark.Validations;

decimal value = 10.5m;
string property = "Decimal";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "The value must be greater than the compared value.")
    .IsGreaterOrEquals(value, Comparer, property, "The value must be greater than or equal to the compared value.")
    .IsLower(value, Comparer, property, "The value must be lower than the compared value.")
    .IsLowerOrEquals(value, Comparer, property, "The value must be lower than or equal to the compared value.")
    .IsBetween(value, Start, End, property, "The value must be between the values.")
    .IsNotBetween(value, Start, End, property, "The value must not be between the values.")
    .IsMin(value, property, "The value must be the minimum value of the type.")
    .IsNotMin(value, property, "The value must not be the minimum value of the type.")
    .IsMax(value, property, "The value must be the maximum value of the type.")
    .IsNotMax(value, property, "The value must not be the maximum value of the type.")
    .AreEquals(value, Comparer, property, "The values must be equal.")
    .AreNotEquals(value, Comparer, property, "The values must not be equal.")
    .Contains(value, decimal[], property, "The value must be in the list.")
    .NotContains(value, decimal[], property, "The value must not be in the list.")
    .All(value, decimal[], property, "Every value in the list must equal the value.")
    .NotAll(value, decimal[], property, "No value in the list may equal the value.")
    .IsNull(value, property, "The value must be null.")
    .IsNotNull(value, property, "The value must not be null.");
```

### Documents

```csharp
using Tooark.Validations;

string property = "Document";
var validation = new Validation()
    .IsCpf(document, property, "Must be a valid CPF")
    .IsRg(document, property, "Must be a valid RG")
    .IsCnh(document, property, "Must be a valid CNH")
    .IsCpfRg(document, property, "Must be a valid CPF or RG")
    .IsCpfRgCnh(document, property, "Must be a valid CPF, RG or CNH")
    .IsCnpj(document, property, "Must be a valid CNPJ")
    .IsCpfCnpj(document, property, "Must be a valid CPF or CNPJ").
```

### Double

```csharp
using Tooark.Validations;

double value = 10.5;
string property = " Double";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "The value must be greater than the compared value.")
    .IsGreaterOrEquals(value, Comparer, property, "The value must be greater than or equal to the compared value.")
    .IsLower(value, Comparer, property, "The value must be lower than the compared value.")
    .IsLowerOrEquals(value, Comparer, property, "The value must be lower than or equal to the compared value.")
    .IsBetween(value, Start, End, property, "The value must be between the values.")
    .IsNotBetween(value, Start, End, property, "The value must not be between the values.")
    .IsMin(value, property, "The value must be the minimum value of the type.")
    .IsNotMin(value, property, "The value must not be the minimum value of the type.")
    .IsMax(value, property, "The value must be the maximum value of the type.")
    .IsNotMax(value, property, "The value must not be the maximum value of the type.")
    .AreEquals(value, Comparer, property, "The values must be equal.")
    .AreNotEquals(value, Comparer, property, "The values must not be equal.")
    .Contains(value, double[], property, "The value must be in the list.")
    .NotContains(value, double[], property, "The value must not be in the list.")
    .All(value, double[], property, "Every value in the list must equal the value.")
    .NotAll(value, double[], property, "No value in the list may equal the value.")
    .IsNull(value, property, "The value must be null.")
    .IsNotNull(value, property, "The value must not be null.");
```

### Email

```csharp
using Tooark.Validations;

string email = "example@domain.com";
string property = "Email";
var validation = new Validation()
    .IsEmail(email, property, "Must be a valid email")
    .IsEmailOrEmpty(email, property, "Must be a valid email or empty");
```

### Email Domain

```csharp
using Tooark.Validations;

string email = "@domain.com";
string property = "EmailDomain";
var validation = new Validation()
    .IsEmailDomain(email, property, "Must be a valid email domain")
    .IsEmailDomainOrEmpty(email, property, "Must be a valid email domain or empty");
```

### Float

```csharp
using Tooark.Validations;

float value = 10.5f;
string property = "Float";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "The value must be greater than the compared value.")
    .IsGreaterOrEquals(value, Comparer, property, "The value must be greater than or equal to the compared value.")
    .IsLower(value, Comparer, property, "The value must be lower than the compared value.")
    .IsLowerOrEquals(value, Comparer, property, "The value must be lower than or equal to the compared value.")
    .IsBetween(value, Start, End, property, "The value must be between the values.")
    .IsNotBetween(value, Start, End, property, "The value must not be between the values.")
    .IsMin(value, property, "The value must be the minimum value of the type.")
    .IsNotMin(value, property, "The value must not be the minimum value of the type.")
    .IsMax(value, property, "The value must be the maximum value of the type.")
    .IsNotMax(value, property, "The value must not be the maximum value of the type.")
    .AreEquals(value, Comparer, property, "The values must be equal.")
    .AreNotEquals(value, Comparer, property, "The values must not be equal.")
    .Contains(value, float[], property, "The value must be in the list.")
    .NotContains(value, float[], property, "The value must not be in the list.")
    .All(value, float[], property, "Every value in the list must equal the value.")
    .NotAll(value, float[], property, "No value in the list may equal the value.")
    .IsNull(value, property, "The value must be null.")
    .IsNotNull(value, property, "The value must not be null.");
```

### Guid

```csharp
using Tooark.Validations;

Guid guid = Guid.NewGuid();
string property = "Guid";
var validation = new Validation()
    .AreEquals(guid, Comparer, property, "The values must be equal.")
    .AreNotEquals(guid, Comparer, property, "The values must not be equal.")
    .Contains(guid, Guid[], property, "The value must be in the list.")
    .NotContains(guid, Guid[], property, "The value must not be in the list.")
    .All(guid, Guid[], property, "Every value in the list must equal the value.")
    .NotAll(guid, Guid[], property, "No value in the list may equal the value.")
    .IsNull(guid, property, "The value must be null.")
    .IsNotNull(guid, property, "The value must not be null.")
    .IsEmpty(guid, property, "The value must be empty.")
    .IsNotEmpty(guid, property, "The value must not be empty.");
```

### Int

```csharp
using Tooark.Validations;

int value = 10;
string property = "Int";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "The value must be greater than the compared value.")
    .IsGreaterOrEquals(value, Comparer, property, "The value must be greater than or equal to the compared value.")
    .IsLower(value, Comparer, property, "The value must be lower than the compared value.")
    .IsLowerOrEquals(value, Comparer, property, "The value must be lower than or equal to the compared value.")
    .IsBetween(value, Start, End, property, "The value must be between the values.")
    .IsNotBetween(value, Start, End, property, "The value must not be between the values.")
    .IsMin(value, property, "The value must be the minimum value of the type.")
    .IsNotMin(value, property, "The value must not be the minimum value of the type.")
    .IsMax(value, property, "The value must be the maximum value of the type.")
    .IsNotMax(value, property, "The value must not be the maximum value of the type.")
    .AreEquals(value, Comparer, property, "The values must be equal.")
    .AreNotEquals(value, Comparer, property, "The values must not be equal.")
    .Contains(value, int[], property, "The value must be in the list.")
    .NotContains(value, int[], property, "The value must not be in the list.")
    .All(value, int[], property, "Every value in the list must equal the value.")
    .NotAll(value, int[], property, "No value in the list may equal the value.")
    .IsNull(value, property, "The value must be null.")
    .IsNotNull(value, property, "The value must not be null.");
```

### Video Link

```csharp
using Tooark.Validations;

string property = "Video";
var validation = new Validation()
    .IsLinkVideo(Value, property, "Must be a valid video link.")
    .IsLinkVideoYouTube(Value, property, "Must be a YouTube link.")
    .IsLinkVideoVimeo(Value, property, "Must be a Vimeo link.")
    .IsLinkVideoDailymotion(Value, property, "Must be a Dailymotion link.");
```

### Lists

```csharp
using Tooark.Validations;

int[] list = [1, 2, 3];
string property = "Values";
var validation = new Validation()
    .IsGreater(list, Value, property, "The list must be larger than the allowed value.")
    .IsGreaterOrEquals(list, Value, property, "The list must be larger than or equal to the allowed value.")
    .IsLower(list, Value, property, "The list must be smaller than the allowed value.")
    .IsLowerOrEquals(list, Value, property, "The list must be smaller than or equal to the allowed value.")
    .AreEquals(list, ListComparer, property, "The lists must be equal.")
    .AreNotEquals(list, value, property, "The lists must not be equal.")
    .IsNull(list, property, "The list must be null.")
    .IsNotNull(list, property, "The list must not be null.")
    .IsEmpty(list, value, property, "The list must be empty.")
    .IsNotEmpty(list, value, property, "The list must not be empty.");
```

### Long

```csharp
using Tooark.Validations;

long value = 10L;
string property = "Long";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "The value must be greater than the compared value.")
    .IsGreaterOrEquals(value, Comparer, property, "The value must be greater than or equal to the compared value.")
    .IsLower(value, Comparer, property, "The value must be lower than the compared value.")
    .IsLowerOrEquals(value, Comparer, property, "The value must be lower than or equal to the compared value.")
    .IsBetween(value, Start, End, property, "The value must be between the values.")
    .IsNotBetween(value, Start, End, property, "The value must not be between the values.")
    .IsMin(value, property, "The value must be the minimum value of the type.")
    .IsNotMin(value, property, "The value must not be the minimum value of the type.")
    .IsMax(value, property, "The value must be the maximum value of the type.")
    .IsNotMax(value, property, "The value must not be the maximum value of the type.")
    .AreEquals(value, Comparer, property, "The values must be equal.")
    .AreNotEquals(value, Comparer, property, "The values must not be equal.")
    .Contains(value, long[], property, "The value must be in the list.")
    .NotContains(value, long[], property, "The value must not be in the list.")
    .All(value, long[], property, "Every value in the list must equal the value.")
    .NotAll(value, long[], property, "No value in the list may equal the value.")
    .IsNull(value, property, "The value must be null.")
    .IsNotNull(value, property, "The value must not be null.");
```

### Network

```csharp
using Tooark.Validations;

string property = "IP";
var validation = new Validation()
    .IsIp(Value, property, "Must be a valid IP.")
    .IsIpv4(Value, property, "Must be a valid IPv4.")
    .IsIpv6(Value, property, "Must be a valid IPv6.")
    .IsMacAddress(Value, property, "Must be a valid MAC address.");
```

### Object

```csharp
using Tooark.Validations;

object obj = new object();
string property = "Object";
var validation = new Validation()
    .AreEquals(obj, Comparer, property, "The objects must be equal.")
    .AreNotEquals(obj, Comparer, property, "The objects must not be equal.")
    .IsNull(obj, property, "The object must be null.")
    .IsNotNull(obj, property, "The object must not be null.");
```

### Protocol

```csharp
using Tooark.Validations;

string property = "Protocol";
var validation = new Validation()
    .IsUrl(Protocol, property, "The protocol must be a valid Url.")
    .IsFtp(Protocol, property, "The protocol must be a valid Ftp.")
    .IsSftp(Protocol, property, "The protocol must be a valid Sftp.")
    .IsProtocolFtp(Protocol, property, "The protocol must be a valid ProtocolFtp.")
    .IsHttp(Protocol, property, "The protocol must be a valid Http.")
    .IsHttps(Protocol, property, "The protocol must be a valid Https.")
    .IsProtocolHttp(Protocol, property, "The protocol must be a valid ProtocolHttp.")
    .IsImap(Protocol, property, "The protocol must be a valid Imap.")
    .IsPop3(Protocol, property, "The protocol must be a valid Pop3.")
    .IsProtocolEmailReceiver(Protocol, property, "The protocol must be a valid ProtocolEmailReceiver.")
    .IsSmtp(Protocol, property, "The protocol must be a valid Smtp.")
    .IsProtocolEmailSender(Protocol, property, "The protocol must be a valid ProtocolEmailSender.")
    .IsWs(Protocol, property, "The protocol must be a valid Ws.")
    .IsWss(Protocol, property, "The protocol must be a valid Wss.")
    .IsProtocolWebSocket(Protocol, property, "The protocol must be a valid ProtocolWebSocket.");
```

### Regex

```csharp
using Tooark.Validations;

string property = "Email";
var validation = new Validation()
    .Match(Value, Pattern, property, "The value must match the pattern.")
    .NotMatch(Value, Pattern, property, "The value must not match the pattern.");
```

### String

```csharp
using Tooark.Validations;

string value = "example";
string property = "Text";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "The string length must be greater than the compared value.")
    .IsGreaterOrEquals(value, Comparer, property, "The string length must be greater than or equal to the compared value.")
    .IsLower(value, Comparer, property, "The string length must be lower than the compared value.")
    .IsLowerOrEquals(value, Comparer, property, "The string length must be lower than or equal to the compared value.")
    .IsBetween(value, Start, End, property, "The string length must be between the values.")
    .IsNotBetween(value, Start, End, property, "The string length must not be between the values.")
    .AreEquals(value, Comparer, property, "The values must be equal.")
    .AreNotEquals(value, Comparer, property, "The values must not be equal.")
    .Contains(value, string[], property, "The value must be in the list.")
    .NotContains(value, string[], property, "The value must not be in the list.")
    .All(value, string[], property, "Every value in the list must equal the value.")
    .NotAll(value, string[], property, "No value in the list may equal the value.")
    .IsNull(value, property, "The value must be null.")
    .IsNotNull(value, property, "The value must not be null.")
    .IsNullOrEmpty(value, property, "The value must be null or empty.")
    .IsNotNullOrEmpty(value, property, "The value must not be null or empty.")
    .IsNullOrWhiteSpace(value, property, "The value must be null, empty or whitespace.")
    .IsNotNull(value, property, "The value must not be null, empty or whitespace.");
```

### TimeSpan

```csharp
TimeSpan value = TimeSpan.FromHours(2);
string property = "Duration";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "The value must be greater than the compared value.")
    .IsGreaterOrEquals(value, Comparer, property, "The value must be greater than or equal to the compared value.")
    .IsLower(value, Comparer, property, "The value must be lower than the compared value.")
    .IsLowerOrEquals(value, Comparer, property, "The value must be lower than or equal to the compared value.")
    .IsBetween(value, Start, End, property, "The value must be between the values.")
    .IsNotBetween(value, Start, End, property, "The value must not be between the values.")
    .IsMin(value, property, "The value must be the minimum value of the type.")
    .IsNotMin(value, property, "The value must not be the minimum value of the type.")
    .IsMax(value, property, "The value must be the maximum value of the type.")
    .IsNotMax(value, property, "The value must not be the maximum value of the type.")
    .AreEquals(value, Comparer, property, "The values must be equal.")
    .AreNotEquals(value, Comparer, property, "The values must not be equal.")
    .Contains(value, TimeSpan[], property, "The value must be in the list.")
    .NotContains(value, TimeSpan[], property, "The value must not be in the list.")
    .All(value, TimeSpan[], property, "Every value in the list must equal the value.")
    .NotAll(value, TimeSpan[], property, "No value in the list may equal the value.")
    .IsNull(value, property, "The value must be null.")
    .IsNotNull(value, property, "The value must not be null.");
```

### Types

```csharp
string value = "abc";
string zipCode = "10000-000";
string property = "Types";
var validation = new Validation()
    .IsGuid(value, property, "Must be a valid Guid.")
    .IsLetter(value, property, "Must be letters.")
    .IsLetterLower(value, property, "Must be lowercase letters.")
    .IsLetterUpper(value, property, "Must be uppercase letters.")
    .IsNumeric(value, property, "Must be numbers.")
    .IsLetterNumeric(value, property, "Must be letters or numbers.")
    .IsHexadecimal(value, property, "Must be hexadecimal.")
    .IsZipCode(value, property, "Must be a zip code.")
    .IsBase64(value, property, "Must be Base64.")
    .IsPassword(value, property, "Must be a complex password.")
    .IsPassword(value, 10, property, "Must be a complex password with at least 10 characters.")
    .IsCulture(value, property, "Must be a culture.")
    .IsCultureIgnoreCase(value, property, "Must be a culture, ignoring case.");
```

## Available Methods

The `Tooark.Validations` library offers a wide range of validation methods, including:

- `Join`: Joins the notification messages.

- `All`: Validates every value in a list.
- `AreEquals`: Validates equal values.
- `AreNotEquals`: Validates different values.
- `Contains`: Validates a value contained in a list or in a string.
- `IsBase64`: Validates Base64.
- `IsBetween`: Validates a value between two values or a list size between two sizes.
- `IsCnh`: Validates a CNH.
- `IsCnpj`: Validates a CNPJ.
- `IsCpf`: Validates a CPF.
- `IsCpfCnpj`: Validates a CPF or CNPJ.
- `IsCpfRg`: Validates a CPF or RG.
- `IsCpfRgCnh`: Validates a CPF, RG or CNH.
- `IsCulture` Validates a culture.
- `IsCultureIgnoreCase` Validates a culture ignoring case.
- `IsEmail`: Validates an email.
- `IsEmailDomain`: Validates an email domain.
- `IsEmailDomainOrEmpty`: Validates an email domain or empty. Accepts a missing value.
- `IsEmailOrEmpty`: Validates an email or empty. Accepts a missing value.
- `IsEmpty`: Validates whether it is empty.
- `IsFalse`: Validates a false value.
- `IsFtp`: Validates FTP.
- `IsGreater`: Validates a greater value or a larger list size.
- `IsGreaterOrEquals`: Validates a greater or equal value or a larger or equal list size.
- `IsGuid`: Validates a GUID.
- `IsHexadecimal` Validates hexadecimal.
- `IsHttp`: Validates HTTP.
- `IsHttps`: Validates HTTPS.
- `IsImap`: Validates IMAP.
- `IsIp`: Validates an IPv4 or IPv6 address.
- `IsIpv4`: Validates an IPv4 address.
- `IsIpv6`: Validates an IPv6 address.
- `IsLetter`: Validates letters only.
- `IsLetterLower` Validates lowercase letters only.
- `IsLetterNumeric` Validates letters and numbers.
- `IsLetterUpper` Validates uppercase letters only.
- `IsLinkVideo`: Validates a YouTube, Vimeo or Dailymotion video link.
- `IsLinkVideoDailymotion`: Validates a Dailymotion video link.
- `IsLinkVideoVimeo`: Validates a Vimeo video link.
- `IsLinkVideoYouTube`: Validates a YouTube video link.
- `IsLower`: Validates a lower value or a smaller list size.
- `IsLowerOrEquals`: Validates a lower or equal value or a smaller or equal list size.
- `IsMacAddress`: Validates a MAC address.
- `IsMax`: Validates the maximum value.
- `IsMin`: Validates the minimum value.
- `IsNotBetween`: Validates a value not between two values or a list size not between two sizes.
- `IsNotEmpty`: Validates whether it is not empty.
- `IsNotMax`: Validates a non-maximum value.
- `IsNotMin`: Validates a non-minimum value.
- `IsNotNull`: Validates whether it is not null.
- `IsNotNullOrEmpty`: Validates whether it is not null or empty.
- `IsNotNullOrWhiteSpace`: Validates whether it is not null, empty or whitespace.
- `IsNull`: Validates whether it is null.
- `IsNullOrEmpty`: Validates whether it is null or empty.
- `IsNullOrWhiteSpace`: Validates whether it is null, empty or whitespace.
- `IsNumeric` Validates numbers only.
- `IsPassword`: Validates a password.
- `IsPop3`: Validates POP3.
- `IsProtocolEmailReceiver`: Validates an email receiving protocol.
- `IsProtocolEmailSender`: Validates an email sending protocol.
- `IsProtocolFtp`: Validates an FTP protocol.
- `IsProtocolHttp`: Validates an HTTP protocol.
- `IsProtocolWebSocket`: Validates a WebSocket protocol.
- `IsRg`: Validates an RG.
- `IsSftp`: Validates SFTP.
- `IsSmtp`: Validates SMTP.
- `IsTrue`: Validates a true value.
- `IsUrl`: Validates a URL.
- `IsWs`: Validates WS.
- `IsWss`: Validates WSS.
- `IsZipCode` Validates a zip code.
- `Match`: Validates a value matching a pattern.
- `NotAll`: Validates no value in a list.
- `NotContains`: Validates a value not contained in a list or in a string.
- `NotMatch`: Validates a value not matching a pattern.

For the full list of methods and their descriptions, see the XML documentation generated with the library.

## Error Messages

The static class `ValidationErrorMessages` gathers the default validation messages. Each method returns a
**translation key**, not the final text, in the `Validation.{Rule};{Property}` format — the property has its
whitespace removed.

```csharp
ValidationErrorMessages.BooleanIsFalse("Active");   // "Validation.IsNotFalse;Active"
```

The overloads without the `message` parameter use those keys; the overloads with `message` use the text you
provide, without going through them.

## Error Codes

Every notification carries a code that identifies the validation family. The codes in use:

| Family          | Codes                        |
| --------------- | ---------------------------- |
| Boolean         | `T.VLD.BOO1`, `T.VLD.BOO2`   |
| Dates           | `T.VLD.DTT1`, `T.VLD.DTT2`   |
| Decimal         | `T.VLD.DEC1`, `T.VLD.DEC2`   |
| Documents       | `T.VLD.DOC1`                 |
| Double          | `T.VLD.DBL1`, `T.VLD.DBL2`   |
| Float           | `T.VLD.FLT1`, `T.VLD.FLT2`   |
| Guid            | `T.VLD.GUI1` to `T.VLD.GUI4` |
| Int             | `T.VLD.INT1`, `T.VLD.INT2`   |
| Lists           | `T.VLD.LST1` to `T.VLD.LST6` |
| Long            | `T.VLD.LNG1`, `T.VLD.LNG2`   |
| Object          | `T.VLD.OBJ1`                 |
| Regex           | `T.VLD.RGX1`                 |
| String          | `T.VLD.STR1` to `T.VLD.STR7` |
| TimeSpan        | `T.VLD.TMS1`, `T.VLD.TMS2`   |
| Null validation | `T.VLD.NUL1`                 |

Two important notes for whoever filters notifications by code:

- **Regular-expression-based validations share `T.VLD.RGX1`**: Email, Network, Protocol, Types, Video Link,
  and also `IsRg` and `IsCnh`, which are format-only validations.
- **`T.VLD.DOC1` covers only the documents with check digits**: `IsCpf`, `IsCnpj`, `IsCpfCnpj`, `IsCpfRg`
  and `IsCpfRgCnh`. In v3.3.4 those validations used `T.VLD.RGX1`.

## Dependencies

| Dependency                                                                    | Version | Usage                                |
| ----------------------------------------------------------------------------- | ------- | ------------------------------------ |
| [`Tooark.Notifications`](https://www.nuget.org/packages/Tooark.Notifications) | 4.x     | `Notification`, base of `Validation` |

## Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Validations](https://github.com/Tooark/nuget-tooark/issues) repository.

## License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
