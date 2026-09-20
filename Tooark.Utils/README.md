# Tooark.Utils

Library of general utility functions that help development, including methods for handling strings, files, languages and localized lists.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Utils/README.pt-BR.md)

## Installation

```bash
dotnet add package Tooark.Utils
```

## Contents

- [FileConvert](#1-file-conversion-and-extension-extraction)
- [FileValid](#2-file-validation)
- [GenerateString](#3-string-generation)
- [GetInfo](#4-information-lookup)
- [Language](#5-languages)
- [Normalize](#6-normalization)
- [UtilErrorMessages](#7-error-messages)

## Utilities

The available utilities are:

### 1. File Conversion and Extension Extraction

**Purpose:**
File conversion and extension extraction.

**Methods:**

- `ToMemoryStream(string? stringFile, long maxBytes = 0)`: Converts a base64 string to a `MemoryStream`.
- `ToMemoryStream(IFormFile? fromFile, long maxBytes = 0)`: Converts an `IFormFile` to a `MemoryStream`.
- `ToMemoryStreamAsync(IFormFile? fromFile, long maxBytes = 0, CancellationToken cancellationToken = default)`: Converts an `IFormFile` to a `MemoryStream` asynchronously.
- `Extension(string? stringFile)`: Extracts the file extension from a base64 string.
- `Extension(IFormFile? fromFile)`: Extracts the file extension from an `IFormFile`.

**Constant:**

- `DefaultMaxBytes`: Default maximum size of the converted content. 5242880 bytes (5MB).

The error message key for the limit is in [`UtilErrorMessages`](#7-error-messages).

**Behavior:**

- Converting an `IFormFile` copies the file's binary content. The returned `MemoryStream` is already positioned at the start.
- The conversion returns `null` when the file is null or empty, and when the string does not contain valid base64.
- **Size limit**: above `maxBytes` a `PayloadTooLargeException` (HTTP 413) is thrown, with the message `File.SizeExceeded;{limit}`. A non-positive value applies `DefaultMaxBytes`; the absolute ceiling is `int.MaxValue`, which is what a `MemoryStream` holds. The limit is checked **before** reserving memory, and also block by block while reading the upload, so it holds even when the declared size does not match the actual content.
- The string must carry the `;base64,` marker, and the content is read right after it. A stray `base64,` elsewhere is not accepted.
- The extension extracted from a base64 string comes from the declared type in the `data:type/extension;base64,content` format, and the type may carry parameters (`data:image/png;charset=utf-8;base64,...`).
- **The extension is validated**: only ASCII letters and digits pass, with at most ten characters. Anything else results in `null`. Since the extension comes from what the client declared — the file name or the data URL type — without that validation the return value could carry a directory separator, markup or text of arbitrary length into whoever consumes it. MIME types that have no extension form, such as `image/svg+xml` and `application/vnd.ms-excel`, result in `null`.
- The extension is always returned in uppercase and without the leading dot. It reflects what was **declared**, not the actual file content.

[**Usage Example**](#file-conversion-and-extension-extraction)

### 2. File Validation

**Purpose:**
File validity checks.

**Methods:**

- `IsImage(IFormFile? file, long fileSize = 0)`: Checks whether the file is a valid image.
- `IsDocument(IFormFile? file, long fileSize = 0)`: Checks whether the file is a valid document.
- `IsVideo(IFormFile? file, long fileSize = 0)`: Checks whether the file is a valid video.
- `IsCustom(IFormFile? file, long fileSize = 0, string[]? permittedExtensions = null)`: Checks whether the file is valid for custom extensions.

**Behavior:**

- The check considers the **size** and the **extension declared in the file name**. It does not inspect the content, so it does not replace checking the actual file type when the uploaded content is untrusted.
- The default size is 5242880 bytes (5MB), applied when `fileSize` is zero or negative.
- Custom extensions are compared case-insensitively and may be given **with or without the leading dot**: `"pdf"` and `".PDF"` have the same effect.
- If `permittedExtensions` is null, empty or contains only blank entries, every image, document and video extension is accepted.

[**Usage Example**](#file-validation)

### 3. String Generation

**Purpose:**
Generation of strings by specific and random criteria.

**Methods:**

- `Sequential(int number)`: Converts an integer into its equivalent alphabetic representation.
- `Password(int len = 12, bool upper = true, bool lower = true, bool number = true, bool special = true, bool similarity = false)`: Generates a string with specific criteria.
- `Hexadecimal(int sizeToken = 128)`: Generates a random hexadecimal string.
- `GuidCode()`: Generates a Guid string without hyphens, with 32 characters.
- `Token(int length = 256)`: Generates a token string.

**Behavior:**

- `Sequential` returns **uppercase letters**, and an empty string when the number is not positive.
- `Password` uses a cryptographic generator and guarantees at least one character of each enabled type. Lengths below 8 are raised to 8, and if every type is disabled, all of them are re-enabled.
- `Hexadecimal` returns **exactly** the requested size, including odd sizes. Values below 2 are raised to 2.

[**Usage Example**](#string-generation)

### 4. Information Lookup

**Purpose:**
Retrieval of localized information from a list of objects. The list type must have the `LanguageCode` property and the property being looked up.

**Methods:**

- `Name<T>(IList<T>? list, string? languageCode = null)`: Gets the localized name from a list of objects.
- `Title<T>(IList<T>? list, string? languageCode = null)`: Gets the localized title from a list of objects.
- `Description<T>(IList<T>? list, string? languageCode = null)`: Gets the localized description from a list of objects.
- `Keywords<T>(IList<T>? list, string? languageCode = null)`: Gets the localized keywords from a list of objects.
- `Custom<T>(IList<T>? list, string property, string? languageCode = null)`: Gets a localized value of a property from a list of objects.

**Behavior:**

- When `languageCode` is not provided, the **current language** is used.
- The lookup tries the requested language, then the application's default language, and finally the first item of the list.
- A null or empty list, and a property with a null value, result in an empty string.
- Properties that are not `string` are converted with `ToString()`.
- If `LanguageCode` or the requested property does not exist on the type, a `GetInfoException` is thrown.

[**Usage Example**](#information-lookup)

### 5. Languages

**Purpose:**
Language management.

**Members:**

- `Default`: The default language code used by the application. Default "en-US".
- `Current`: The current language code of the execution environment.
- `CurrentCulture`: The current culture of the execution environment.
- `SetCulture(string? culture)`: Sets the current culture using the culture name in the `xx-XX` format.
- `SetCulture(CultureInfo? culture)`: Sets the current culture using a `CultureInfo` object.
- `Instance`: The `ILanguage` implementation in use, replaceable in tests. Rejects null with `ArgumentNullException`.

The `Tooark.Utils.Interfaces.ILanguage` interface exposes `DefaultLanguage`, `CurrentLanguage`, `CurrentCultureInfo`, `SetCultureInfo(string?)` and `SetCultureInfo(CultureInfo?)`.

**Behavior:**

- The current language follows the culture of the **execution flow** (`CultureInfo.CurrentCulture`), not a process-wide global state. In a web application, each request sees only its own culture.
- The culture name is accepted case-insensitively: `"pt-br"` is normalized to `"pt-BR"`.
- A null or empty name, one outside the `xx-XX` format, or one of a non-existent culture results in the application's default culture.
- `SetCulture` also sets `CultureInfo.CurrentUICulture`.

[**Usage Example**](#languages)

### 6. Normalization

**Purpose:**
String normalization. Removes spaces and punctuation, converts to uppercase and transliterates special characters.

**Methods:**

- `Value(string value)`: Normalizes a value by removing spaces, converting to uppercase and replacing special characters.

[**Usage Example**](#normalization)

### 7. Error Messages

**Purpose:**
Translation keys of the error messages generated by the package itself, in `Tooark.Utils.Messages.UtilErrorMessages`.

**Constants:**

- `FileSizeExceeded`: `"File.SizeExceeded"`. Emitted by `FileConvert` when the content exceeds the limit, in the `File.SizeExceeded;{limit}` format.

The translations ship with the [Tooark.Extensions](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions) resources.

## Usage Example

### File Conversion and Extension Extraction

```csharp
using Tooark.Utils;
using Microsoft.AspNetCore.Http;
using Tooark.Exceptions;

var base64String = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQ...";

// Default 5MB limit
MemoryStream? memoryStream = FileConvert.ToMemoryStream(base64String);
MemoryStream? fromUpload = FileConvert.ToMemoryStream(formFile);

// Custom limit, in bytes
MemoryStream? large = FileConvert.ToMemoryStream(formFile, 20 * 1024 * 1024);
MemoryStream? asynchronous = await FileConvert.ToMemoryStreamAsync(formFile, cancellationToken: cancellationToken);

string? extension = FileConvert.Extension(base64String); // PNG
string? extensionFromFile = FileConvert.Extension(formFile); // TXT

// Above the limit, the error is explicit and already carries the HTTP 413 status
try
{
  FileConvert.ToMemoryStream(formFile, 1024);
}
catch (PayloadTooLargeException ex)
{
  // ex.Message         -> "File.SizeExceeded;1024"
  // ex.GetStatusCode() -> HttpStatusCode.RequestEntityTooLarge
}
```

### File Validation

```csharp
using Tooark.Utils;
using Microsoft.AspNetCore.Http;

bool isImage = FileValid.IsImage(formFile);
bool isDocument = FileValid.IsDocument(formFile, 10485760); // 10MB
bool isVideo = FileValid.IsVideo(formFile);

// With or without the leading dot, the result is the same
bool isCustom = FileValid.IsCustom(formFile, permittedExtensions: ["txt", ".csv"]);
```

### String Generation

```csharp
using Tooark.Utils;

string sequential = GenerateString.Sequential(27); // AA
string password = GenerateString.Password(); // aB3@dE1#fG2$
string onlyLetters = GenerateString.Password(16, number: false, special: false);
string hex = GenerateString.Hexadecimal(); // 1A2B3C4D5E6F...
string guid = GenerateString.GuidCode(); // 1A2B3C4D5E6F7A8B9C0D1E2F3A4B5C6D
string token = GenerateString.Token(); // 1A2B3C4D5E6F7A8B9C0D1E2F3A4B5C6DADE123E21...
```

### Information Lookup

```csharp
using Tooark.Utils;

// The type must have the LanguageCode property
var list = new List<MyObject> { /* ... */ };

string name = GetInfo.Name(list);
string title = GetInfo.Title(list);
string description = GetInfo.Description(list);
string keywords = GetInfo.Keywords(list);

// Explicit language and custom property
string customValue = GetInfo.Custom(list, "CustomProperty", "pt-BR");
```

### Languages

```csharp
using Tooark.Utils;
using System.Globalization;

Language.SetCulture("pt-BR");

string current = Language.Current; // pt-BR
string defaultLanguage = Language.Default; // en-US
CultureInfo currentCulture = Language.CurrentCulture; // pt-BR

// Accepts the name case-insensitively
Language.SetCulture("es-es"); // es-ES

// An invalid name falls back to the default culture
Language.SetCulture("pt"); // en-US
```

### Normalization

```csharp
using Tooark.Utils;

string normalizedValue = Normalize.Value("Olá Mundo!"); // OLAMUNDO
string withSymbols = Normalize.Value("R&D 100$"); // RANDD100DOLLAR
```

## Dependencies

- [Microsoft.AspNetCore.Http](https://www.nuget.org/packages/Microsoft.AspNetCore.Http/) 2.x — brings `IFormFile`
  for `FileConvert` and `FileValid` without requiring the `Microsoft.AspNetCore.App` shared framework, so the
  package also runs in console and worker applications. Inside an ASP.NET Core application the framework's
  own assembly wins and `IFormFile` is the same type.
- [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)
- [`Tooark.Validations`](https://www.nuget.org/packages/Tooark.Validations)

## Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Utils](https://github.com/Tooark/nuget-tooark/issues) repository.

## License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
