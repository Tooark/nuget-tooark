# Tooark.Storage.Aws

[`Tooark.Storage`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Storage) provider on **Amazon S3** and
services compatible with the S3 API, such as MinIO, LocalStack and Cloudflare R2. It implements `IStorageService`
with upload, download, delete, metadata and **presigned URLs** for reading and writing.

The contract, the common options and the shared errors are in the
[`Tooark.Storage` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.md). This README covers
what is specific to AWS.

📖 **Docs:** [Tooark.Storage.Aws on the site](https://tooark.com/nuget-tooark/packages/tooark.storage.aws.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage.Aws/README.pt-BR.md)

---

## 📑 Contents

- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Credentials](#-credentials)
- [Compatible services](#-compatible-services)
- [Behavior on S3](#-behavior-on-s3)
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
dotnet add package Tooark.Storage.Aws
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Storage": {
    "Bucket": "my-application-files",
    "Region": "sa-east-1",
    "SignedUrlExpirationMinutes": 15
  }
}
```

### Program.cs

```csharp
using Tooark.Storage.Aws.Injections;

builder.Services.AddTooarkStorageAws(builder.Configuration);

// With an adjustment in code, applied on top of the section
builder.Services.AddTooarkStorageAws(builder.Configuration, options => options.Bucket = "another-bucket");
```

The registration validates the options and fails at startup when they are incomplete. The S3 client is registered as
`IAmazonS3`, with one instance for the whole application.

### `AwsStorageOptions` properties

Besides `Bucket` and `SignedUrlExpirationMinutes`, the
[common options](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.md#storageoptions-properties):

| Property         | Type      | Default | Description                                                                                   |
| ---------------- | --------- | ------- | --------------------------------------------------------------------------------------------- |
| `Region`         | `string?` | —       | Bucket region, such as `sa-east-1`. Without it, the region from the AWS default chain applies |
| `AccessKey`      | `string?` | —       | Access key. Provide it together with `SecretKey`, or neither                                  |
| `SecretKey`      | `string?` | —       | Secret key. Provide it together with `AccessKey`, or neither                                  |
| `SessionToken`   | `string?` | —       | Session token, for temporary credentials. Only valid with both keys                           |
| `ServiceUrl`     | `string?` | —       | Address of an S3-compatible service. Absolute `http` or `https` URL                           |
| `ForcePathStyle` | `bool`    | `false` | Puts the bucket in the URL path (`host/bucket/key`), as compatible services usually require   |

---

## 🔑 Credentials

Without `AccessKey` and `SecretKey`, the credentials come from the **AWS default chain**, in the SDK's order:
environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`), the `~/.aws/credentials` profile, the ECS task
role, the EKS pod role and the EC2 instance profile. **It is the recommended path in production**: no key stays in the
configuration, and AWS rotates the role without intervention.

With the keys in the options, they take precedence. One key without the other fails at registration: silently falling
back to the default chain would make the application use another identity.

The identity needs `s3:PutObject`, `s3:GetObject` and `s3:DeleteObject` on the bucket objects, and `s3:ListBucket` on
the bucket. Without `s3:ListBucket`, S3 answers 403, not 404, for an object that does not exist: `ExistsAsync` throws
`Storage.AccessDenied` instead of returning `false`, and `DownloadAsync` throws the same error instead of
`Storage.ObjectNotFound`.

### Your own client

If the application registers its own `IAmazonS3` first, for example with `AddAWSService<IAmazonS3>()` from
`AWSSDK.Extensions.NETCore.Setup`, that one is used. In that case, `Region`, the credentials, `ServiceUrl` and
`ForcePathStyle` from the options do not apply; `Bucket` and `SignedUrlExpirationMinutes` still do.

---

## 🔌 Compatible services

`ServiceUrl` points the client to another service with the S3 API. With it, the address defines the destination and
`Region` applies only to request signing. A local MinIO, for example:

```json
{
  "Storage": {
    "Bucket": "files",
    "ServiceUrl": "http://localhost:9000",
    "ForcePathStyle": true,
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin"
  }
}
```

Keep development keys in `appsettings.Development.json` or in the Secret Manager, never in the versioned
`appsettings.json`.

---

## 🪣 Behavior on S3

- **Streaming download** — `DownloadAsync` returns the S3 response stream, without loading the file into memory.
  Closing the stream releases the connection.
- **Delete with a query first** — S3 answers success when deleting an object that does not exist. To return `false`
  in that case, `DeleteAsync` queries the metadata first, which costs one more call.
- **Missing bucket in the metadata query** — the query response has no body, so `ExistsAsync` and `GetInfoAsync`
  cannot tell a missing bucket from a missing object: both give `false` and `null`. In the other operations, a missing
  bucket throws `Storage.BucketNotFound`.
- **Presigned URL with Signature V4** — up to 7 days. With temporary credentials (role), the URL stops working when the
  session expires, even before the requested expiration.
- **Upload** — the caller's stream is not closed. A seekable stream is sent from the start, and its size comes back in
  `Size`.

---

## 📋 Dependencies

| Package                                                                                                                                         | Version  | Description                            |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | -------------------------------------- |
| [`Tooark.Storage`](https://www.nuget.org/packages/Tooark.Storage)                                                                               | 4.x      | Contract and common validations        |
| [`AWSSDK.S3`](https://www.nuget.org/packages/AWSSDK.S3)                                                                                         | 4.x      | Amazon S3 client                       |
| [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions)   | 8.x/10.x | Reading the options from configuration |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Container registration                 |

---

## 🎯 Best Practices

1. **Role instead of keys** — on ECS, EKS or EC2, leave `AccessKey` and `SecretKey` empty and grant the permission to
   the role.
2. **Permission only on the application's bucket** — scope the policy to the bucket ARN, not `s3:*` on `*`.
3. **Private bucket** — keep Block Public Access on and deliver the files through presigned URLs.
4. **CORS for direct upload** — the write URL only works in the browser with a CORS rule that allows `PUT` from the
   page's origin.

---

## ⚠️ Error Codes and Solutions

The configuration errors are `InternalServerErrorException` thrown **at registration** (`AddTooarkStorageAws`). The
operation errors are in the
[`Tooark.Storage` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.md#️-error-codes-and-solutions).

| Message                                         | Description                                               | Solution                                               |
| ----------------------------------------------- | --------------------------------------------------------- | ------------------------------------------------------ |
| `Options.Storage.Aws.CredentialsIncomplete`     | Only one of the keys, or a session token without the keys | Provide both keys, or neither to use the default chain |
| `Options.Storage.Aws.ServiceUrlInvalid;{value}` | `ServiceUrl` is not an absolute `http` or `https` URL     | Use the full address, such as `http://localhost:9000`  |

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
