# Tooark.Sanitizers

Library with three sanitizers for content that comes from outside the application: **HTML** with an allowlist,
**URLs** with a scheme allowlist, and the content of the
[**`@tooark/wysiwyg`**](https://www.npmjs.com/package/@tooark/wysiwyg) editor (the Tiptap JSON), with the same rules
the component applies in the browser.

HTML is not sanitized by hand-written code: the service wraps
[HtmlSanitizer](https://github.com/mganss/HtmlSanitizer), the de facto standard in .NET, because encodings, nested
tags and event attributes make the bypass surface too large. The URL and wysiwyg JSON rules are the package's own,
small and tested against the known bypasses.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Sanitizers/README.pt-BR.md)

---

## 📑 Contents

- [Overview](#-overview)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Sanitizers](#-sanitizers)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Best Practices](#-best-practices)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 📖 Overview

| Service                    | Input                                | Output                                                | Typical use                                                 |
| -------------------------- | ------------------------------------ | ----------------------------------------------------- | ----------------------------------------------------------- |
| `IHtmlSanitizerService`    | HTML                                 | HTML with only what is allowed; empty for blank input | HTML from an external source: import, email, legacy         |
| `IUrlSanitizerService`     | URL                                  | Cleaned URL, or empty when rejected; `IsSafe`         | Link provided by the user: website, profile, social network |
| `IWysiwygSanitizerService` | Tiptap document (`JsonNode` or text) | New, sanitized document; null when not a document     | `@tooark/wysiwyg` content before saving                     |

- **Safe defaults**: the configuration section is optional, and the three sanitizers work without it.
- **Guards the configuration cannot turn off**: `javascript:`, `vbscript:` and `data:` are never allowed; in HTML,
  neither are `<script>`, `<object>`, `<embed>` nor event attributes (`onclick`...).
- **Fails at startup**: a configuration that tries to allow any of those is rejected at registration, not on the
  first request.
- **No per-call state**: the services are singletons and safe for concurrent use.

---

## 🔧 Installation

```bash
dotnet add package Tooark.Sanitizers
```

The package runs on the base .NET runtime, without ASP.NET Core. It also comes with the `Tooark` aggregator, whose
`AddTooarkService` already registers the sanitizers.

---

## ⚙️ Configuration

### appsettings.json

Everything is optional. The example restricts HTML to a formatting set, accepts relative links and turns videos off
in the editor content:

```json
{
  "Sanitizers": {
    "Html": {
      "AllowedTags": ["p", "br", "strong", "em", "u", "a", "ul", "ol", "li"],
      "AllowedAttributes": ["href", "title"],
      "AllowedSchemes": ["https", "mailto"]
    },
    "Url": {
      "AllowedSchemes": ["https"],
      "AllowRelative": true
    },
    "Wysiwyg": {
      "AllowVideos": false
    }
  }
}
```

### Program.cs

```csharp
using Tooark.Sanitizers.Injections;

// Reads the Sanitizers section
builder.Services.AddTooarkSanitizers(builder.Configuration);

// Or without reading the configuration, only with adjustments in code
builder.Services.AddTooarkSanitizers(options => options.Url.AllowRelative = true);
```

With both configuration and code, the code adjustment applies on top of the section. Calling the registration more
than once does not duplicate the services.

### HTML options — `Sanitizers:Html`

Each list provided **replaces** HtmlSanitizer's default list. Missing or empty, the default stays, which is already a
safe allowlist for formatting HTML.

| Property               | Type       | Default              | Description                                                                                                      |
| ---------------------- | ---------- | -------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `AllowedTags`          | `string[]` | HtmlSanitizer's list | Allowed tags. `script`, `object`, `embed`, `applet`, `base`, `meta`, `link`, `frame` and `frameset` are rejected |
| `AllowedAttributes`    | `string[]` | HtmlSanitizer's list | Allowed attributes. Event attributes (`on*`) are rejected                                                        |
| `AllowedSchemes`       | `string[]` | `http`, `https`      | Schemes in URL attributes (`href`, `src`...). `javascript`, `vbscript` and `data` are rejected                   |
| `AllowedCssProperties` | `string[]` | HtmlSanitizer's list | CSS properties allowed in the `style` attribute                                                                  |
| `AllowedClasses`       | `string[]` | none                 | Allowed classes. Providing the list allows the `class` attribute with only those                                 |
| `KeepChildNodes`       | `bool`     | `false`              | Keeps the content of a removed tag. With `false`, the tag goes with its content                                  |

### URL options — `Sanitizers:Url`

| Property           | Type       | Default         | Description                                                                                                              |
| ------------------ | ---------- | --------------- | ------------------------------------------------------------------------------------------------------------------------ |
| `AllowedSchemes`   | `string[]` | `http`, `https` | Schemes accepted in an absolute URL, such as `mailto` and `tel`. `javascript`, `vbscript` and `data` are rejected        |
| `AllowRelative`    | `bool`     | `false`         | Accepts a relative URL prefixed with `/`, `#`, `?`, `./` or `../`. A path without a prefix (`uploads/x.png`) is rejected |
| `AllowCredentials` | `bool`     | `false`         | Accepts a user and password in the URL. `https://bank.com@scam.com` leads to `scam.com` while showing another name       |

### Wysiwyg options — `Sanitizers:Wysiwyg`

| Property      | Type   | Default | Description                                                                 |
| ------------- | ------ | ------- | --------------------------------------------------------------------------- |
| `AllowImages` | `bool` | `true`  | Keeps image nodes. Turn it off when the editor toolbar has no `media` group |
| `AllowVideos` | `bool` | `true`  | Keeps video nodes                                                           |

The other wysiwyg rules are the component's and are not configurable: the content accepted on the server must be the
same the editor and the viewer accept.

---

## 🧹 Sanitizers

### HTML — `IHtmlSanitizerService`

`Sanitize(html)` returns the fragment with only the allowed tags, attributes, schemes and CSS. Null or blank input
becomes empty text. Scripts, event attributes, `javascript:` in any spelling (uppercase, entities, a tab in the
middle) and `expression()` in CSS do not survive.

### URL — `IUrlSanitizerService`

`Sanitize(url)` returns the cleaned URL when it is accepted, or empty text when it is not; `IsSafe(url)` answers the
same as a boolean. The rules are those of `isSafeUrl` in `@tooark/wysiwyg`, so a URL has the same destination in the
browser and on the server:

1. control characters and whitespace are removed from the **whole** URL, because the browser ignores them and
   `java\tscript:` becomes `javascript:`;
2. relative is a URL that starts with `/`, `#`, `?`, `./` or `../`, and it only passes with `AllowRelative`;
3. absolute needs a scheme from the list;
4. a user and password in the URL are rejected unless `AllowCredentials`. In `mailto`, what comes before the `@` is
   the address, not a credential.

The returned URL is the cleaned one, **not normalized**: what the browser will interpret is exactly what was
validated.

### `@tooark/wysiwyg` content — `IWysiwygSanitizerService`

The component never works with HTML: the content is the Tiptap JSON, and it sanitizes it in the browser with
`sanitizeWysiwygContent`. But the JSON that reaches the API may not have gone through the editor: a hand-made request
sends whatever it wants. The service applies the same rules on the server and **rebuilds** the document with only
what the allowlist knows. Unknown keys and attributes are not copied.

Accepted nodes and marks, those of the `@tooark/wysiwyg` 1.4 schema:

| Node                                                                         | Attributes                                          |
| ---------------------------------------------------------------------------- | --------------------------------------------------- |
| `doc`, `blockquote`, `bulletList`, `listItem`, `hardBreak`, `horizontalRule` | —                                                   |
| `paragraph`                                                                  | `textAlign`                                         |
| `heading`                                                                    | `level` (1 to 4), `textAlign`                       |
| `text`                                                                       | the text, not empty, and the marks                  |
| `orderedList`                                                                | `start` (integer), `type` (`1`, `a`, `A`, `i`, `I`) |
| `codeBlock`                                                                  | `language`                                          |
| `image`                                                                      | `src` (required), `alt`, `title`, `width`, `height` |
| `video`                                                                      | `src` (required), `poster`, `title`                 |

| Mark                                            | Attributes                                  |
| ----------------------------------------------- | ------------------------------------------- |
| `bold`, `italic`, `underline`, `strike`, `code` | —                                           |
| `link`                                          | `href` (required), `target`, `rel`, `title` |
| `textStyle`                                     | `color`, `backgroundColor`                  |
| `highlight`                                     | `color`                                     |

The rules:

- **a node or mark outside the schema** is dropped; a node goes with its children;
- **URLs** (`href`, `src`, `poster`) follow the URL rule above with `http`, `https`, `mailto`, `tel` and prefixed
  relative URLs, as in the component. A rejected link leaves the mark and the text stays; a rejected image or video
  leaves the document; a rejected `poster` leaves alone;
- **colors** only in hex, a CSS name or `rgb()`/`hsl()` without `;`. Others (`oklch()`, `var()`) are dropped, as in
  the component;
- **`textAlign`** only in `left`, `center`, `right` and `justify`; the heading **`level`** becomes an integer between
  1 and 4, and 1 when it is not an integer;
- **an empty text node** is dropped, because ProseMirror rejects it and the whole document would no longer open;
- **a document that loses all its blocks** becomes the component's empty document (one paragraph);
- **nesting** beyond 64 levels is cut.

`Sanitize(JsonNode)` returns a new `JsonObject`; `Sanitize(string)` returns the JSON as text, with accents readable
and `<`, `>`, `&` and quotes escaped, so it stays safe if embedded in a page. Null input, invalid JSON or a root that
is not `doc` return null.

> **Component version.** The schema follows `@tooark/wysiwyg` 1.4. A new node or attribute in the component needs a
> new version of this package; until then, the server removes it.

---

## 📝 Usage Examples

### Saving the editor content

`<ark-wysiwyg-editor>` emits the JSON in the `ark-wysiwyg-change` event. The API sanitizes it before saving, and the
same JSON goes back to `<ark-wysiwyg-viewer>`:

```csharp
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Tooark.Sanitizers.Interfaces;

public sealed record SaveArticleDto(string Title, JsonObject Content);

[ApiController]
[Route("articles")]
public sealed class ArticleController(IWysiwygSanitizerService wysiwyg) : ControllerBase
{
  [HttpPost]
  public IActionResult Save([FromBody] SaveArticleDto dto)
  {
    // Null when the JSON is not an editor document
    var content = wysiwyg.Sanitize(dto.Content);

    if (content is null)
    {
      return BadRequest();
    }

    // Save content.ToJsonString(): it is what the viewer gets back
    return Ok();
  }
}
```

### Link provided by the user

```csharp
using Tooark.Sanitizers.Interfaces;

public sealed class ProfileService(IUrlSanitizerService urls)
{
  // Empty text when the link is rejected
  public string ProfileWebsite(string? website) => urls.Sanitize(website);
}
```

```csharp
urls.Sanitize("https://tooark.com");       // "https://tooark.com"
urls.Sanitize(" java\tscript:alert(1)");   // ""
urls.Sanitize("data:text/html,<script>");  // ""
urls.IsSafe("https://bank.com@scam.com");  // false
```

### HTML from an external source

```csharp
using Tooark.Sanitizers.Interfaces;

public sealed class ImportService(IHtmlSanitizerService html)
{
  public string Clean(string importedContent) => html.Sanitize(importedContent);
}
```

```csharp
html.Sanitize("<p onclick=\"alert(1)\">Hello <script>alert(1)</script></p>"); // "<p>Hello </p>"
html.Sanitize("<a href=\"javascript:alert(1)\">link</a>");                    // "<a>link</a>"
```

### Without dependency injection

The services have a public constructor, for tools and tests:

```csharp
using Microsoft.Extensions.Options;
using Tooark.Sanitizers;
using Tooark.Sanitizers.Options;

var options = Options.Create(new SanitizerOptions());
var wysiwyg = new WysiwygSanitizerService(options);
```

---

## 📋 Dependencies

| Package                                                                                                                                         | Version  | Description                                 |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                         | 4.x      | Exceptions (`InternalServerErrorException`) |
| [`HtmlSanitizer`](https://www.nuget.org/packages/HtmlSanitizer)                                                                                 | 9.x      | HTML sanitization, on top of AngleSharp     |
| [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions)   | 8.x/10.x | Reading the options from configuration      |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Container registration                      |

---

## 🎯 Best Practices

1. **Sanitize on input and keep encoding on output** — sanitizing before saving does not replace context encoding
   when displaying (Razor, `textContent`, quoted attributes).
2. **Sanitize the editor JSON on the server** — the component sanitizes in the browser, but the API receives whatever
   any client sends.
3. **List only what the content needs** — shrink the HTML tag list instead of widening it.
4. **Turn off the media the application does not use** — without the `media` group in the editor, `AllowImages` and
   `AllowVideos` set to `false` prevent an image pointing to a tracking server.
5. **Do not use the URL sanitizer to validate redirects** — with `AllowRelative`, `//other.com` passes as relative, as
   in the component, and leads to another host. For a `returnUrl`, use ASP.NET Core's `Url.IsLocalUrl`.

---

## ⚠️ Error Codes and Solutions

The errors are configuration errors: `InternalServerErrorException` thrown **at registration**
(`AddTooarkSanitizers`) or when creating the service without dependency injection. Sanitization itself does not
throw: what is not accepted is removed.

| Message                                              | Description                                         | Solution                                                     |
| ---------------------------------------------------- | --------------------------------------------------- | ------------------------------------------------------------ |
| `Options.Sanitizers.SchemeNotAllowed;{scheme}`       | `javascript`, `vbscript` or `data` in a scheme list | Remove the scheme; it runs code or embeds content            |
| `Options.Sanitizers.SchemeInvalid;{scheme}`          | Scheme with invalid syntax                          | Use letters, digits, `+`, `-` or `.`, starting with a letter |
| `Options.Sanitizers.Html.TagNotAllowed;{tag}`        | Blocked tag in `AllowedTags`                        | Remove the tag; it runs code or loads external content       |
| `Options.Sanitizers.Html.AttributeNotAllowed;{name}` | Event attribute (`on*`) in `AllowedAttributes`      | Remove the attribute; it runs code on any tag                |

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
