# Tooark.Utils

Library of general utility functions that help development, including methods for handling strings, files, languages and localized lists.

📖 **Docs:** [Tooark.Utils on the site](https://tooark.com/nuget-tooark/packages/tooark.utils.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Utils/README.pt-BR.md)

---

## 📑 Contents

- [Installation](#-installation)
- [Utilities](#-utilities)
  - [FileConvert](#1-file-conversion-and-extension-extraction)
  - [FileValid](#2-file-validation)
  - [GenerateString](#3-string-generation)
  - [GetInfo](#4-information-lookup)
  - [Language](#5-languages)
  - [Normalize](#6-normalization)
  - [UtilErrorMessages](#7-error-messages)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 🔧 Installation

```bash
dotnet add package Tooark.Utils
```

---

## 🧰 Utilities

The available utilities are:

### 1. File Conversion and Extension Extraction

**Purpose:**
File conversion and extension extraction.

**Methods:**

- `ToMemoryStream(string? stringFile, long maxBytes = 0)`: Converts a base64 string to a `MemoryStream`.
- `CopyToMemoryStream(Stream? stream, long maxBytes = 0)`: Copies the content of a `Stream` to a `MemoryStream`.
- `CopyToMemoryStreamAsync(Stream? stream, long maxBytes = 0, CancellationToken cancellationToken = default)`: Copies the content of a `Stream` to a `MemoryStream` asynchronously.
- `Extension(string? stringFile)`: Extracts the file extension from a base64 string.
- `ExtensionFromFileName(string? fileName)`: Extracts the extension from the file name.

`CopyToMemoryStream`, `CopyToMemoryStreamAsync` and `ExtensionFromFileName` exist since v4.11.0.

**Constant:**

- `DefaultMaxBytes`: Default maximum size of the converted content. 5242880 bytes (5MB).

The error message key for the limit is in [`UtilErrorMessages`](#7-error-messages).

**Behavior:**

- Copying a `Stream` starts at the **current position** of the source and takes the binary content up to the end. The source is not closed: whoever opened the stream disposes it. The returned `MemoryStream` is already positioned at the start.
- The conversion returns `null` when the stream is null, is empty or has already been read to the end, and when the string does not contain valid base64.
- **Size limit**: above `maxBytes` a `PayloadTooLargeException` (HTTP 413) is thrown, with the message `File.SizeExceeded;{limit}`. A non-positive value applies `DefaultMaxBytes`; the absolute ceiling is `int.MaxValue`, which is what a `MemoryStream` holds. For a string, the limit is checked from the length of the base64 part, **before** reserving memory. For a `Stream` that reports its length, the excess is also refused before any allocation; for the others, such as a request body, the limit is checked block by block during the copy.
- The string must carry the `;base64,` marker, and the content is read right after it. A stray `base64,` elsewhere is not accepted.
- The extension extracted from a base64 string comes from the declared type in the `data:type/extension;base64,content` format, and the type may carry parameters (`data:image/png;charset=utf-8;base64,...`).
- The extension extracted from a file name is what comes after the last dot. Whatever comes before a directory separator is discarded.
- **The extension is validated**: only ASCII letters and digits pass, with at most ten characters. Anything else results in `null`. Since the extension comes from what the client declared — the file name or the data URL type — without that validation the return value could carry a directory separator, markup or text of arbitrary length into whoever consumes it. MIME types that have no extension form, such as `image/svg+xml` and `application/vnd.ms-excel`, result in `null`.
- The extension is always returned in uppercase and without the leading dot. It reflects what was **declared**, not the actual file content.

**Methods with `IFormFile`:**

The three `FileConvert` methods that took an `IFormFile` **were removed in v5**, together with the package dependency on `Microsoft.AspNetCore.Http` 2.x: `IFormFile` is an ASP.NET Core type, and the package only referenced ASP.NET Core because of them. They were obsolete since v4.11.0, which brought the replacements. The versions that take a string (`ToMemoryStream(string?)` and `Extension(string?)`) remain.

| Removed                                                    | With `IFormFile` (`Tooark.AspNetCore.Files`) | Without `IFormFile`               |
| ---------------------------------------------------------- | -------------------------------------------- | --------------------------------- |
| `ToMemoryStream(IFormFile?, long)`                         | `file.ToMemoryStream()`                      | `CopyToMemoryStream(stream)`      |
| `ToMemoryStreamAsync(IFormFile?, long, CancellationToken)` | `file.ToMemoryStreamAsync()`                 | `CopyToMemoryStreamAsync(stream)` |
| `Extension(IFormFile?)`                                    | `file.GetExtension()`                        | `ExtensionFromFileName(name)`     |

In an ASP.NET Core application, use the `IFormFile` extensions of [`Tooark.AspNetCore`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.AspNetCore), which apply the same rules. Outside one, use the versions that take a `Stream` and a file name.

[**Usage Example**](#file-conversion-and-extension-extraction)

### 2. File Validation

**Purpose:**
File validity checks.

**Methods:**

- `IsImageFile(string? fileName, long length, long maxBytes = 0)`: Checks, by name and size, whether the file is a valid image.
- `IsDocumentFile(string? fileName, long length, long maxBytes = 0)`: Checks, by name and size, whether the file is a valid document.
- `IsVideoFile(string? fileName, long length, long maxBytes = 0)`: Checks, by name and size, whether the file is a valid video.
- `IsCustomFile(string? fileName, long length, long maxBytes = 0, string[]? permittedExtensions = null)`: Checks, by name and size, whether the file is valid for custom extensions.

The four exist since v4.11.0. `fileName` is the name of the file, where the extension comes from, `length` is its size in bytes and `maxBytes` is the maximum size accepted.

**Behavior:**

- The check considers the **size** and the **extension declared in the file name**. It does not inspect the content, so it does not replace checking the actual file type when the uploaded content is untrusted.
- The image extensions are `.JPG`, `.JPEG`, `.PNG`, `.GIF`, `.BMP`, `.SVG` and `.WEBP`; the document ones, `.TXT`, `.CSV`, `.LOG`, `.PDF`, `.DOC`, `.DOCX`, `.XLS`, `.XLSX`, `.PPT` and `.PPTX`; the video ones, `.AVI`, `.MP4`, `.MPG`, `.MPEG` and `.WMV`.
- The default maximum size is 5242880 bytes (5MB), applied when `maxBytes` is zero or negative. A file with a zero or negative `length` is invalid.
- Custom extensions are compared case-insensitively and may be given **with or without the leading dot**: `"pdf"` and `".PDF"` have the same effect.
- If `permittedExtensions` is null, empty or contains only blank entries, every image, document and video extension is accepted.

**Methods with `IFormFile`:**

The four `FileValid` methods that took an `IFormFile` **were removed in v5**, for the same reason as the [`FileConvert` methods](#1-file-conversion-and-extension-extraction). They were obsolete since v4.11.0.

| Removed                                 | With `IFormFile` (`Tooark.AspNetCore.Files`) | Without `IFormFile`            |
| --------------------------------------- | -------------------------------------------- | ------------------------------ |
| `IsImage(IFormFile?, long)`             | `file.IsImage()`                             | `IsImageFile(name, length)`    |
| `IsDocument(IFormFile?, long)`          | `file.IsDocument()`                          | `IsDocumentFile(name, length)` |
| `IsVideo(IFormFile?, long)`             | `file.IsVideo()`                             | `IsVideoFile(name, length)`    |
| `IsCustom(IFormFile?, long, string[]?)` | `file.IsCustom()`                            | `IsCustomFile(name, length)`   |

The maximum size parameter, called `fileSize` in the removed methods, is called `maxBytes` in the replacements: whoever passes it by name (`fileSize: ...`) has to rename it when migrating.

[**Usage Example**](#file-validation)

### 3. String Generation

**Purpose:**
Generation of strings by specific and random criteria.

**Methods:**

- `Sequential(int number)`: Converts an integer into its equivalent alphabetic representation.
- `Password(int len = 12, bool upper = true, bool lower = true, bool number = true, bool special = true, bool similarity = false)`: Generates a string with specific criteria.
- `Hexadecimal(int sizeToken = 128)`: Generates a random hexadecimal string.
- `GuidCode()`: Generates a Guid string without hyphens, with 32 characters.

> **Changed in v5:** `Token(int length = 256)` of the 4.x line was removed. It only generated the text, with no
> hash or verification. Use `SecureToken.Create()` from `Tooark.Securities`, which comes with `Hash` and
> constant-time `Verify`.

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

---

## 📝 Usage Examples

### File Conversion and Extension Extraction

```csharp
using Tooark.Utils;
using Tooark.Exceptions;

var base64String = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQ...";

// Default 5MB limit
MemoryStream? memoryStream = FileConvert.ToMemoryStream(base64String);

// From a Stream: the copy starts at the current position, and whoever opened the source disposes it
using var report = File.OpenRead("report.pdf");
MemoryStream? fromStream = FileConvert.CopyToMemoryStream(report);

// Custom limit, in bytes, in the asynchronous version
await using var video = File.OpenRead("video.mp4");
MemoryStream? large = await FileConvert.CopyToMemoryStreamAsync(video, 20 * 1024 * 1024, cancellationToken);

string? extension = FileConvert.Extension(base64String); // PNG
string? extensionFromName = FileConvert.ExtensionFromFileName("report.pdf"); // PDF

// Above the limit, the error is explicit and already carries the HTTP 413 status
try
{
  using var spreadsheet = File.OpenRead("spreadsheet.xlsx");
  FileConvert.CopyToMemoryStream(spreadsheet, 1024);
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

// The name and the size come from wherever the file is: disk, storage, message
var photo = new FileInfo("photo.png");

bool isImage = FileValid.IsImageFile(photo.Name, photo.Length);
bool isDocument = FileValid.IsDocumentFile("contract.pdf", 2097152, 10485760); // 2MB, with a 10MB limit
bool isVideo = FileValid.IsVideoFile("lesson.mp4", 4194304);

// With or without the leading dot, the result is the same
bool isCustom = FileValid.IsCustomFile("data.csv", 1024, permittedExtensions: ["txt", ".csv"]);
```

### String Generation

```csharp
using Tooark.Utils;

string sequential = GenerateString.Sequential(27); // AA
string password = GenerateString.Password(); // aB3@dE1#fG2$
string onlyLetters = GenerateString.Password(16, number: false, special: false);
string hex = GenerateString.Hexadecimal(); // 1A2B3C4D5E6F...
string guid = GenerateString.GuidCode(); // 1A2B3C4D5E6F7A8B9C0D1E2F3A4B5C6D
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

---

## 📋 Dependencies

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
