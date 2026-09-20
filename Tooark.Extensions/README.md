# Tooark.Extensions

Library that manages extensions and utilities, easing the development and maintenance of .NET projects.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Extensions/README.pt-BR.md)

## Installation

```bash
dotnet add package Tooark.Extensions
```

## Configuration

The language files ship with the assembly, so **there is nothing to configure** for the translations to
work — not in the application, not in a container, not in a single-file publish.

Add the following line to your `Program.cs`:

```csharp
// Importing the required namespace
using Tooark.Extensions.Injections;

// In your service configuration
services.AddTooarkExtensions();
```

## Contents

- [EnumerableExtensions](#1-enumerable-extension)
- [AddJsonStringLocalizer](#2-jsonstringlocalizer-configuration-json-string-localization-extension)
- [JsonStringLocalizerExtensions](#3-json-string-localization-extension-extension-for-istringlocalizer)
- [StringExtensions](#4-string-extensions)
- [Message catalog](#message-catalog)

## Extensions

The available extensions are:

### 1. Enumerable Extension

**Purpose:**
Sorts collections of objects by specific properties and by properties of nested classes. Supports ascending and descending ordering of object collections.

**Methods:**

- `OrderByProperty<T>(string sortProperty)`: Sorts a collection of objects in ascending order by a specific property.
- `OrderByPropertyDescending<T>(string sortProperty)`: Sorts a collection of objects in descending order by a specific property.

[**Usage Example**](#enumerable-extension)

### 2. JsonStringLocalizer Configuration (JSON String Localization Extension)

**Purpose:**
Adds the dependency injection of the string localization service based on JSON files.

**Methods:**

- `AddJsonStringLocalizer`: Adds the dependency injection of the string localization service based on JSON files.

[**Usage Example**](#jsonstringlocalizer-configuration)

### 3. JSON String Localization Extension (Extension for IStringLocalizer)

**Purpose:**
Uses the default multicultural resource files for string localization.

**Methods:**

- `LocalizedString this[string name]`: Represents a localized value.
- `LocalizedString this[string name, params object[] arguments]`: Represents a localized value with arguments.
- `GetAllStrings(bool includeParentCultures)`: Gets every localized value. `includeParentCultures` selects the `default` culture values when `true` or the `current` ones when `false`.

[**Usage Example**](#json-string-localization-extension-extensions-for-istringlocalizer)

### 4. String Extensions

**Purpose:**
Extensions for string manipulation.

**Methods:**

- `ToBase64`: Converts a string to Base64.
- `FromBase64`: Converts a Base64 string back to a regular string.
- `ToSlug`: Converts a string to a slug format.
- `ToNormalize`: Normalizes a string by removing spaces, converting to uppercase and replacing special characters.
- `FromSnakeToPascalCase`: Converts a string from snake_case to PascalCase.
- `FromSnakeToCamelCase`: Converts a string from snake_case to camelCase.
- `FromSnakeToKebabCase`: Converts a string from snake_case to kebab-case.
- `FromPascalToSnakeCase`: Converts a string from PascalCase to snake_case.
- `FromCamelToSnakeCase`: Converts a string from camelCase to snake_case.
- `FromKebabToSnakeCase`: Converts a string from kebab-case to snake_case.

[**Usage Examples**](#string-extensions)

## Usage Examples

### Enumerable Extension

**OrderByProperty with a simple parameter:**

```csharp
using Tooark.Extensions;

List<MyObject> list = [{"Name": "B", "Age": 20}, {"Name": "A", "Age": 30}, {"Name": "C", "Age": 10}];

var sortedList = list.OrderByProperty("Name").toList();
// [{"Name": "A", "Age": 30}, {"Name": "B", "Age": 20}, {"Name": "C", "Age": 10}]
```

**OrderByPropertyDescending with a simple parameter:**

```csharp
using Tooark.Extensions;

List<MyObject> list = [{"Name": "B", "Age": 20}, {"Name": "A", "Age": 30}, {"Name": "C", "Age": 10}];

var sortedList = list.OrderByPropertyDescending("Name").toList();
// [{"Name": "C", "Age": 10}, {"Name": "B", "Age": 20}, {"Name": "A", "Age": 30}]
```

**OrderByPropertyDescending with a nested parameter:**

```csharp
using Tooark.Extensions;

List<MyObject> list = [
  {"Name": "B", "Age": 20, "Address": {"City": "City C"}},
  {"Name": "A", "Age": 30, "Address": {"City": "City B"}},
  {"Name": "C", "Age": 10, "Address": {"City": "City A"}}
];

var sortedList = list.OrderByProperty("Address.City").toList();
// [
//  {"Name": "C", "Age": 10, "Address": {"City": "City A"}},
//  {"Name": "A", "Age": 30, "Address": {"City": "City B"}},
//  {"Name": "B", "Age": 20, "Address": {"City": "City C"}}
//]
```

### JsonStringLocalizer Configuration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tooark.Extensions.Options;
using Tooark.Extensions.Injections;

var services = new ServiceCollection();

services.AddJsonStringLocalizer();
```

### JSON String Localization Extension (Extensions for IStringLocalizer)

**Get a localized value:**

```csharp
using Tooark.Extensions;

var localizedString = _localizer["Field"]; // "Field"
```

**Get a localized value with inline arguments:**

```csharp
using Tooark.Extensions;

var localizedString = _localizer["Field.Empty;Name"]; // "The Name field is empty"
```

**Get a localized value with arguments:**

```csharp
using Tooark.Extensions;

var localizedString = _localizer["Field.Empty", "Name"]; // "The Name field is empty"
```

### String Extensions

**ToBase64:**

```csharp
using Tooark.Extensions;
string value = "Hello World!";
string base64Value = value.ToBase64(); // SGVsbG8gV29ybGQh
```

**FromBase64:**

```csharp
using Tooark.Extensions;
string base64Value = "SGVsbG8gV29ybGQh";
string normalValue = base64Value.FromBase64(); // Hello World!
```

**ToSlug:**

```csharp
using Tooark.Extensions;
string value = "Hello World!";
string slugValue = value.ToSlug(); // hello-world
```

**ToNormalize:**

```csharp
using Tooark.Extensions;

string value = "Olá Mundo!";
string normalizedValue = value.ToNormalize(); // OLAMUNDO
```

**FromSnakeToPascalCase:**

```csharp
using Tooark.Extensions;

string value = "hello_world";
string pascalCaseValue = value.FromSnakeToPascalCase(); // HelloWorld
```

**FromSnakeToCamelCase:**

```csharp
using Tooark.Extensions;

string value = "hello_world";
string camelCaseValue = value.FromSnakeToCamelCase(); // helloWorld
```

**FromSnakeToKebabCase:**

```csharp
using Tooark.Extensions;

string value = "hello_world";
string kebabCaseValue = value.FromSnakeToKebabCase(); // hello-world
```

**FromPascalToSnakeCase:**

```csharp
using Tooark.Extensions;

string value = "HelloWorld";
string snakeCaseValue = value.FromPascalToSnakeCase(); // hello_world
```

**FromCamelToSnakeCase:**

```csharp
using Tooark.Extensions;

string value = "helloWorld";
string snakeCaseValue = value.FromCamelToSnakeCase(); // hello_world
```

**FromKebabToSnakeCase:**

```csharp
using Tooark.Extensions;

string value = "hello-world";
string snakeCaseValue = value.FromKebabToSnakeCase(); // hello_world
```

## Message catalog

The resource files cover **every message emitted by the Tooark packages**: validations, attributes,
exceptions, notifications, mediator, unit of work, cryptography, JWT, OpenID Connect, observability,
enumerators and utilities. There are 148 keys, with the same set in the three languages.

| Language   | File                                                                                                                  |
| ---------- | --------------------------------------------------------------------------------------------------------------------- |
| English    | [en-US.default.json](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Extensions/Resources/en-US.default.json) |
| Spanish    | [es-ES.default.json](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Extensions/Resources/es-ES.default.json) |
| Portuguese | [pt-BR.default.json](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Extensions/Resources/pt-BR.default.json) |

The application's default language is `en-US`, and that is where the lookup falls back to when the current
language has no translation for the key. A language without its own file — `pt-PT`, for instance, which is
no longer shipped in this version — works through the same path, answering in English.

### Key format

The key may carry parameters separated by semicolons, in the `Key;parameter1;parameter2` format. The
parameters replace the `{0}`, `{1}` and so on placeholders, and are themselves translated when they match an
existing key:

```csharp
localizer["Field.Required;Email"]              // en-US: "The E-mail field is required"
localizer["Validation.IsBetween;Age;18;65"]    // en-US: "The value of property Age is between 18 and 65."
```

When the key does not exist, `LocalizedString.ResourceNotFound` is true and `Value` carries the received
text unchanged — the same behavior as the framework's `ResourceManagerStringLocalizer`. That lets a text that
is not a Tooark key, such as the messages ASP.NET Core model binding generates, pass through the localizer
without being altered.

### Overriding or adding translations

Place a `Resources/{language}.json` file in your application output. It is merged over the
`{language}.default.json` embedded in the package, **key by key**: you override only what you want and can
add your own keys. The translations are read once per language, on first use.

```xml
<ItemGroup>
  <None Update="Resources\**\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

The consumer's file does **not** carry `.default` in its name — that suffix identifies what comes from the
package.

## Dependencies

| Dependency                                                                                              | Version  | Usage                                             |
| ------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------------------- |
| [`Tooark.Utils`](https://www.nuget.org/packages/Tooark.Utils)                                           | 4.x      | Normalization and language                        |
| [`Microsoft.Extensions.Localization`](https://www.nuget.org/packages/Microsoft.Extensions.Localization) | 8.x/10.x | `IStringLocalizer` and the container registration |

> **The package does not require the ASP.NET Core runtime.** Until v3 it declared the shared framework
> because of `ModelStateExtension`, and that requirement propagated to `Tooark.Dtos`, `Tooark.ValueObjects`,
> `Tooark.Entities` and the aggregator. In v4 `ModelStateExtension` moved to
> [`Tooark.AspNetCore`](https://www.nuget.org/packages/Tooark.AspNetCore), and this package is
> general-purpose again: it works in console, worker and serverless function without the ASP.NET Core runtime
> installed.

## Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Extensions](https://github.com/Tooark/nuget-tooark/issues) repository.

## License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
