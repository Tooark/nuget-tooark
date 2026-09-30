# Tooark.Storage

Library with the Tooark family's **cloud object storage** abstractions: the `IStorageService` contract (upload,
download, delete, metadata and signed URL), the `StorageObjectDto`, the common options and the providers' base class.
The package has **no cloud SDK**: each provider lives in its own package, and the application only loads the SDK of
the cloud it uses.

| Package                                                                                     | Provider                                                  | SDK                       |
| ------------------------------------------------------------------------------------------- | --------------------------------------------------------- | ------------------------- |
| [`Tooark.Storage.Aws`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Storage.Aws) | Amazon S3 and compatible services (MinIO, LocalStack, R2) | `AWSSDK.S3`               |
| [`Tooark.Storage.Gcp`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Storage.Gcp) | Google Cloud Storage                                      | `Google.Cloud.Storage.V1` |

Azure Blob Storage is planned as `Tooark.Storage.Azure`.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.pt-BR.md)

## Contents

- [Overview](#overview)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Components](#-components)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Best Practices](#-best-practices)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [License](#-license)

## Overview

- **One contract for any cloud** — the application code depends only on `IStorageService`. Switching from AWS to
  Google Cloud changes the registration in `Program.cs`, not the code that uploads and downloads files.
- **Read and write signed URLs** — the read URL delivers a private file without exposing the bucket; the write URL
  lets the browser send the file straight to the storage, without going through the API.
- **Tooark errors** — a missing object is a `NotFoundException`; a missing bucket, denied access and provider
  failures are `InternalServerErrorException`, with the SDK exception kept in `InnerException`.
- **Validation before the provider** — a blank or too long key, a missing bucket and an expiration out of range are
  rejected before any call to the cloud.
- **One provider per application** — the options live in the `Storage` section, and each operation can point to
  another bucket.

---

## 🔧 Installation

Install the provider package, which brings this one:

```bash
dotnet add package Tooark.Storage.Aws
# or
dotnet add package Tooark.Storage.Gcp
```

The `Tooark` aggregator brings only this package, with the abstractions, and no provider: an application uses one
cloud, and it makes no sense to load both SDKs into every aggregator consumer.

---

## ⚙️ Configuration

### appsettings.json

The common options live in the `Storage` section, together with the provider's
([AWS](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage.Aws/README.md#️-configuration),
[Google Cloud](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage.Gcp/README.md#️-configuration)):

```json
{
  "Storage": {
    "Bucket": "my-application-files",
    "SignedUrlExpirationMinutes": 15
  }
}
```

### `StorageOptions` properties

| Property                     | Type      | Default | Description                                                          |
| ---------------------------- | --------- | ------- | -------------------------------------------------------------------- |
| `Bucket`                     | `string?` | —       | Default bucket of the operations. Each operation can provide another |
| `SignedUrlExpirationMinutes` | `int`     | `5`     | Default signed URL expiration, from 1 to 10080 minutes (7 days)      |

The options are validated when the provider is registered: an expiration out of range fails at startup.

---

## 📦 Components

### `IStorageService`

Every operation accepts `bucket` (optional: without it, the options' bucket applies) and a `CancellationToken`.

| Method                                               | Returns             | Description                                                                                                    |
| ---------------------------------------------------- | ------------------- | -------------------------------------------------------------------------------------------------------------- |
| `UploadAsync(key, content, contentType?)`            | `StorageObjectDto`  | Sends the object, replacing whatever exists at the key. The service does not close the stream                  |
| `DownloadAsync(key)`                                 | `Stream`            | Downloads the content; the caller closes the stream. A missing object throws `NotFoundException`               |
| `DeleteAsync(key)`                                   | `bool`              | Deletes the object: true when deleted, false when it did not exist                                             |
| `ExistsAsync(key)`                                   | `bool`              | Tells whether the object exists                                                                                |
| `GetInfoAsync(key)`                                  | `StorageObjectDto?` | Object data without downloading the content; null when it does not exist                                       |
| `GetSignedUrlAsync(key, expiration?, access = Read)` | `Uri`               | Temporary URL to read (GET) or write (PUT) without credentials. Without `expiration`, the options' one applies |

The signed URL is computed **locally**, without querying the storage: generating it does not check whether the object
exists.

### `StorageObjectDto`

| Property       | Type              | Description                                                          |
| -------------- | ----------------- | -------------------------------------------------------------------- |
| `Bucket`       | `string`          | Object bucket                                                        |
| `Key`          | `string`          | Object key (name)                                                    |
| `Size`         | `long?`           | Size in bytes; null when uploading a non-seekable stream             |
| `ContentType`  | `string?`         | Content type (MIME)                                                  |
| `ETag`         | `string?`         | Content version, without quotes; the format varies between providers |
| `LastModified` | `DateTimeOffset?` | Last modification date, when the provider reports it                 |

### `ESignedUrlAccess`

`Read` (default) generates a read URL (GET); `Write` generates a write URL (PUT).

### `StorageServiceBase`

Base for a new provider. The public operations validate the arguments and resolve the bucket and the expiration; the
provider implements the protected methods `UploadCoreAsync`, `DownloadCoreAsync`, `DeleteCoreAsync`,
`GetInfoCoreAsync` and `GetSignedUrlCoreAsync`, which receive everything already resolved. `ExistsAsync` comes from
`GetInfoCoreAsync`.

| Validation                              | Exception                      | Message                                    |
| --------------------------------------- | ------------------------------ | ------------------------------------------ |
| Blank key                               | `BadRequestException`          | `Storage.KeyRequired`                      |
| Key longer than 1024 UTF-8 bytes        | `BadRequestException`          | `Storage.KeyTooLong;1024`                  |
| No bucket provided nor configured       | `InternalServerErrorException` | `Storage.BucketNotConfigured`              |
| Expiration not positive or above 7 days | `BadRequestException`          | `Storage.SignedUrlExpirationInvalid;10080` |

### Provider errors

| Situation                  | Exception                      | Message                   |
| -------------------------- | ------------------------------ | ------------------------- |
| Missing object             | `NotFoundException`            | `Storage.ObjectNotFound`  |
| Missing bucket             | `InternalServerErrorException` | `Storage.BucketNotFound`  |
| Access denied              | `InternalServerErrorException` | `Storage.AccessDenied`    |
| Any other provider failure | `InternalServerErrorException` | `Storage.OperationFailed` |

A missing bucket and denied access are `InternalServerErrorException` because they point to the application's
configuration, not to a caller error. Cancellation still arrives as `OperationCanceledException`.

---

## 📝 Usage Examples

### Uploading a file received by the API

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Storage.Interfaces;

[ApiController]
[Route("documents")]
public sealed class DocumentController(IStorageService storage) : ControllerBase
{
  [HttpPost]
  public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
  {
    // The key is generated on the server, not taken from the user's file name
    var key = $"documents/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

    await using var content = file.OpenReadStream();
    var stored = await storage.UploadAsync(key, content, file.ContentType, cancellationToken: cancellationToken);

    return Ok(new { stored.Key, stored.Size });
  }
}
```

### Temporary link to a private file

```csharp
[HttpGet("{**key}")]
public async Task<IActionResult> Open(string key, CancellationToken cancellationToken)
{
  if (!await storage.ExistsAsync(key, cancellationToken: cancellationToken))
  {
    return NotFound();
  }

  var url = await storage.GetSignedUrlAsync(key, TimeSpan.FromMinutes(10), cancellationToken: cancellationToken);

  return Redirect(url.ToString());
}
```

### Direct upload from the browser

The API generates the key and a write URL; the browser sends the file with `PUT`, without the content going through
the API. The bucket needs a CORS rule that allows `PUT` from the page's origin.

```csharp
using Tooark.Storage.Enums;

[HttpPost("upload-url")]
public async Task<IActionResult> CreateUploadUrl(CancellationToken cancellationToken)
{
  var key = $"uploads/{Guid.NewGuid()}";
  var url = await storage.GetSignedUrlAsync(key, access: ESignedUrlAccess.Write, cancellationToken: cancellationToken);

  return Ok(new { key, url });
}
```

```ts
const { key, url } = await api.post("/documents/upload-url");
await fetch(url, { method: "PUT", body: file });
```

### `@tooark/wysiwyg` media

The [`@tooark/wysiwyg`](https://www.npmjs.com/package/@tooark/wysiwyg) editor stores images and videos through the
`uploadFile` hook, and the saved JSON carries the URL. Save the **key** and return a path of your own API, which
generates a fresh signed URL on each access: a signed URL in the JSON would expire within 7 days at most. The relative
path starts with `/`, which the component and `Tooark.Sanitizers` accept.

```ts
editor.uploadFile = async (file) => {
  const { key } = await api.upload(file); // POST /documents, as in the first example
  return { src: `/documents/${key}`, alt: file.name }; // GET /documents/{key} redirects
};
```

### Switching providers

Only the registration changes:

```csharp
// builder.Services.AddTooarkStorageAws(builder.Configuration);
builder.Services.AddTooarkStorageGcp(builder.Configuration);
```

---

## 📋 Dependencies

| Package                                                                 | Version | Description                                                      |
| ----------------------------------------------------------------------- | ------- | ---------------------------------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions) | 4.x     | Exceptions (`BadRequestException`, `NotFoundException` and more) |

The package runs on the base .NET runtime, without ASP.NET Core and without any cloud SDK.

---

## 🎯 Best Practices

1. **Generate the key on the server** — the user's file name can overwrite another object or carry unexpected
   paths; use a generated identifier and keep the original name elsewhere if you need it.
2. **Save the key, not the signed URL** — the URL expires; generate a new one when the file is accessed.
3. **Private bucket and signed URL** — instead of a public bucket, deliver each file with a short-lived URL.
4. **Credentials from the environment** — prefer the AWS role or Google's Application Default Credentials to keys
   in the configuration; see each provider's README.
5. **Large files through a signed URL** — downloading through the API holds the connection and, on Google Cloud,
   the memory; the read URL delivers the file straight from the storage.

---

## ⚠️ Error Codes and Solutions

| Message                                            | Exception                      | Description                                     | Solution                                                     |
| -------------------------------------------------- | ------------------------------ | ----------------------------------------------- | ------------------------------------------------------------ |
| `Storage.KeyRequired`                              | `BadRequestException`          | Blank object key                                | Provide the key                                              |
| `Storage.KeyTooLong;1024`                          | `BadRequestException`          | Key longer than 1024 UTF-8 bytes                | Shorten the key                                              |
| `Storage.SignedUrlExpirationInvalid;10080`         | `BadRequestException`          | URL expiration not positive or above 7 days     | Use an expiration between 1 minute and 7 days                |
| `Storage.BucketNotConfigured`                      | `InternalServerErrorException` | No bucket in the call nor in the options        | Configure `Storage:Bucket` or provide the bucket in the call |
| `Storage.ObjectNotFound`                           | `NotFoundException`            | The object does not exist                       | Check the key; use `ExistsAsync` when absence is expected    |
| `Storage.BucketNotFound`                           | `InternalServerErrorException` | The bucket does not exist                       | Check the bucket name and the region or project              |
| `Storage.AccessDenied`                             | `InternalServerErrorException` | The credential has no permission on the bucket  | Grant the permission to the application's identity           |
| `Storage.OperationFailed`                          | `InternalServerErrorException` | Provider or SDK failure                         | See the `InnerException`                                     |
| `Options.Storage.SignedUrlExpirationInvalid;10080` | `InternalServerErrorException` | `SignedUrlExpirationMinutes` outside 1 to 10080 | Fix the default expiration in the options                    |

Each provider's own errors are in its README.

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the
[Tooark](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the
[LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
