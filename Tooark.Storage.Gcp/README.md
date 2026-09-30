# Tooark.Storage.Gcp

[`Tooark.Storage`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Storage) provider on **Google Cloud
Storage**. It implements `IStorageService` with upload, download, delete, metadata and **V4 signed URLs** for reading
and writing.

The contract, the common options and the shared errors are in the
[`Tooark.Storage` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.md). This README covers
what is specific to Google Cloud.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage.Gcp/README.pt-BR.md)

---

## 📑 Contents

- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Credentials and URL signing](#-credentials-and-url-signing)
- [Behavior on Google Cloud Storage](#-behavior-on-google-cloud-storage)
- [Dependencies](#-dependencies)
- [Best Practices](#-best-practices)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 🔧 Installation

```bash
dotnet add package Tooark.Storage.Gcp
```

---

## ⚙️ Configuration

### appsettings.json

On Cloud Run, GKE or Compute Engine, the bucket is enough: the credential comes from the environment's service
account.

```json
{
  "Storage": {
    "Bucket": "my-application-files",
    "SignedUrlExpirationMinutes": 15
  }
}
```

### Program.cs

```csharp
using Tooark.Storage.Gcp.Injections;

builder.Services.AddTooarkStorageGcp(builder.Configuration);

// With an adjustment in code, applied on top of the section
builder.Services.AddTooarkStorageGcp(builder.Configuration, options => options.Bucket = "another-bucket");
```

The registration validates the options and fails at startup when they are invalid. The credential is loaded on first
use, and the client is registered as `StorageClient`, with one instance for the whole application.

### `GcpStorageOptions` properties

Besides `Bucket` and `SignedUrlExpirationMinutes`, the
[common options](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.md#storageoptions-properties):

| Property          | Type      | Default | Description                                                                                   |
| ----------------- | --------- | ------- | --------------------------------------------------------------------------------------------- |
| `CredentialsJson` | `string?` | —       | Content of the service account key, as JSON. Meant for a vault that delivers the JSON as text |
| `CredentialsPath` | `string?` | —       | Path of the service account key file                                                          |

Provide one of them, or neither to use Application Default Credentials. Both together fail at registration.

---

## 🔑 Credentials and URL signing

Without `CredentialsJson` and `CredentialsPath`, the credential comes from **Application Default Credentials** (ADC):
the `GOOGLE_APPLICATION_CREDENTIALS` variable, `gcloud auth application-default login` or the Cloud Run, GKE or
Compute Engine service account. **It is the recommended path in production**: no key stays in the configuration.

With `CredentialsJson` or `CredentialsPath`, **only a service account key** is accepted. Loading any credential type
from external JSON is what Google advises against, because the JSON can point to another token source.

URL signing depends on the credential:

| Credential                                       | Signs URLs?                                                                             |
| ------------------------------------------------ | --------------------------------------------------------------------------------------- |
| Service account key (`CredentialsJson`/`Path`)   | Yes, locally, with the private key                                                      |
| Cloud Run, GKE or Compute Engine service account | Yes, through the IAM Credentials API (`signBlob`)                                       |
| `gcloud auth application-default login` (user)   | No: `GetSignedUrlAsync` throws `Storage.SigningNotSupported`; the other operations work |

To sign with the environment's service account, it needs the **Service Account Token Creator** role
(`roles/iam.serviceAccountTokenCreator`) on itself, and the IAM Service Account Credentials API must be enabled in the
project.

The identity needs **Storage Object Admin** (`roles/storage.objectAdmin`) on the bucket, or the
`storage.objects.create`, `storage.objects.get` and `storage.objects.delete` permissions.

### Your own client

If the application registers its own `StorageClient` first, that one is used for the operations, and the options'
credential applies only to URL signing.

---

## 📁 Behavior on Google Cloud Storage

- **Download in memory** — the Google client writes the object to a destination stream, so `DownloadAsync` returns a
  `MemoryStream` with the whole file. For large files, deliver a read signed URL.
- **Delete in one call** — `DeleteAsync` returns `false` when the object did not exist, without a query first.
- **Missing bucket and object** — Google answers 404 for both, and only the message tells them apart. The service
  throws `Storage.BucketNotFound` for the bucket and treats the object as missing.
- **V4 signed URL** — up to 7 days, with the verb (`GET` or `PUT`) inside the signature: the read URL cannot be used to
  write.
- **Upload** — the caller's stream is not closed, and the size comes back in `Size`, as reported by Google.

---

## 📋 Dependencies

| Package                                                                                                                                         | Version  | Description                            |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | -------------------------------------- |
| [`Tooark.Storage`](https://www.nuget.org/packages/Tooark.Storage)                                                                               | 4.x      | Contract and common validations        |
| [`Google.Cloud.Storage.V1`](https://www.nuget.org/packages/Google.Cloud.Storage.V1)                                                             | 5.x      | Google Cloud Storage client            |
| [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions)   | 8.x/10.x | Reading the options from configuration |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Container registration                 |

---

## 🎯 Best Practices

1. **Environment service account instead of a key** — on Cloud Run and GKE, leave the credentials empty and grant the
   roles to the application's service account.
2. **Key in a vault, never in the repository** — when you need a key, deliver the JSON from Secret Manager into
   `CredentialsJson` (`Storage__CredentialsJson`).
3. **Uniform bucket-level access** — control access through the bucket's IAM, not object ACLs.
4. **CORS for direct upload** — the write URL only works in the browser with a CORS configuration on the bucket that
   allows `PUT` from the page's origin.

---

## ⚠️ Error Codes and Solutions

The configuration errors are `InternalServerErrorException`. Option errors come **at registration**
(`AddTooarkStorageGcp`); credential errors come **on first use**, when it is loaded. The common operation errors are in
the [`Tooark.Storage` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.md#️-error-codes-and-solutions).

| Message                                          | When         | Description                                         | Solution                                                         |
| ------------------------------------------------ | ------------ | --------------------------------------------------- | ---------------------------------------------------------------- |
| `Options.Storage.Gcp.CredentialsAmbiguous`       | Registration | `CredentialsJson` and `CredentialsPath` together    | Provide only one of them                                         |
| `Options.Storage.Gcp.CredentialsNotFound;{path}` | Registration | The `CredentialsPath` file does not exist           | Check the path and the mounted volume                            |
| `Options.Storage.Gcp.CredentialsInvalid`         | First use    | The JSON is not a valid service account key         | Use the JSON key generated for the service account               |
| `Options.Storage.Gcp.CredentialsUnavailable`     | First use    | No key in the options and no ADC in the environment | Provide the key or configure Application Default Credentials     |
| `Storage.SigningNotSupported`                    | Signed URL   | The credential cannot sign URLs                     | Use a service account key or grant Service Account Token Creator |

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
