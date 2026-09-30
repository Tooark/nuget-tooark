# Tooark.Secrets

Library with the Tooark family's **secrets and parameters** abstractions. The store feeds `IConfiguration`, so the
packages that already read their options from configuration (Storage, Securities, OpenId, Observability) receive the
store's values **without any change**. For what is only known at runtime, there is the `ISecretService`, with cache.

The package has **no cloud SDK**: each store lives in its own package.

| Package                                                                                         | Secrets                | Parameters               | Store credential                |
| ----------------------------------------------------------------------------------------------- | ---------------------- | ------------------------ | ------------------------------- |
| [`Tooark.Secrets.Aws`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets.Aws)     | AWS Secrets Manager    | AWS Parameter Store      | AWS default chain (role)        |
| [`Tooark.Secrets.Gcp`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets.Gcp)     | Google Secret Manager  | Google Parameter Manager | Application Default Credentials |
| [`Tooark.Secrets.Vault`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets.Vault) | Vault or OpenBao KV v2 | the same KV              | Token, AppRole or Kubernetes    |

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md)

## Contents

- [Overview](#overview)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [From store names to keys](#-from-store-names-to-keys)
- [Runtime reads](#-runtime-reads)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Best Practices](#-best-practices)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [License](#-license)

## Overview

- **Configuration source** — the store enters `IConfiguration` like any other source, and wins over the earlier
  ones (`appsettings.json`, environment variables). `Storage:SecretKey`, `Jwt:Secret` or `OpenId:Entra:ClientSecret`
  come from the store, and the `AddTooark*` calls keep reading and validating the options as always.
- **`ISecretService`** — reads a secret by name at runtime, such as a tenant's API key, with cache. It reads secrets
  only: parameters are configuration and come through the source.
- **Read at startup** — the source reads the store once, when the configuration is built. Tooark packages validate
  and keep their options at startup, so rotating a secret they use takes a restart or a new deploy. The
  `ISecretService` reads again when the cache expires.
- **The store credential comes from the environment** — AWS role, Google Application Default Credentials, or
  Kubernetes/AppRole auth in Vault. A store access key kept in `appsettings.json` would only move the secret around.
- **Fails at startup** — an unreachable store, a missing permission or a timeout keeps the application from starting,
  unless `Optional`.

---

## 🔧 Installation

Install the store package, which brings this one:

```bash
dotnet add package Tooark.Secrets.Aws
# or
dotnet add package Tooark.Secrets.Gcp
# or
dotnet add package Tooark.Secrets.Vault
```

The `Tooark` aggregator brings only this package, with the abstractions, and no provider.

---

## ⚙️ Configuration

### appsettings.json

The options live in the `Secrets` section, together with the store's own
([AWS](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Aws/README.md#️-configuration),
[Google Cloud](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Gcp/README.md#️-configuration),
[Vault](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Vault/README.md#️-configuration)):

```json
{
  "Secrets": {
    "SecretsPrefix": "arkuest/prod",
    "TimeoutSeconds": 30,
    "CacheMinutes": 5
  }
}
```

### Program.cs

The source goes on the `IConfigurationBuilder`, **after** the other sources and before the `AddTooark*` calls that
read the options:

```csharp
using Tooark.Secrets.Aws.Injections;

var builder = WebApplication.CreateBuilder(args);

// The store goes on top of appsettings.json and environment variables
builder.Configuration.AddTooarkSecretsAws();

// Storage, Securities and the others already receive the store's values
builder.Services.AddTooarkStorageAws(builder.Configuration);

// Optional: runtime reads, with cache
builder.Services.AddTooarkSecretsAws(builder.Configuration);
```

The source's options come from the `Secrets` section of the sources already added, and the code adjustment applies on
top: `AddTooarkSecretsAws(options => options.SecretsPrefix = "arkuest/prod")`.

### `SecretsOptions` properties

| Property         | Type   | Default | Description                                                                               |
| ---------------- | ------ | ------- | ----------------------------------------------------------------------------------------- |
| `ExpandJson`     | `bool` | `false` | A value that is a JSON object becomes child keys. See [ExpandJson](#expandjson)           |
| `Optional`       | `bool` | `false` | Ignores a failure to load the source, and the application starts without the store's keys |
| `TimeoutSeconds` | `int`  | `30`    | Maximum time to load the source at startup                                                |
| `CacheMinutes`   | `int`  | `5`     | How long a secret read by `ISecretService` stays cached. Zero turns the cache off         |

---

## 🔑 From store names to keys

Each secret or parameter under the configured prefix becomes a key. The name loses the prefix, and `/` and `__`
become `:`:

| Store                 | Prefix          | Name in the store                                  | Key                         |
| --------------------- | --------------- | -------------------------------------------------- | --------------------------- |
| AWS Secrets Manager   | `arkuest/prod`  | `arkuest/prod/Jwt/Secret`                          | `Jwt:Secret`                |
| AWS Parameter Store   | `/arkuest/prod` | `/arkuest/prod/Storage/Bucket`                     | `Storage:Bucket`            |
| Google Secret Manager | `arkuest-prod`  | `arkuest-prod__OpenId__Entra__ClientSecret`        | `OpenId:Entra:ClientSecret` |
| Vault/OpenBao (KV v2) | `arkuest/prod`  | field `SecretKey` of secret `arkuest/prod/Storage` | `Storage:SecretKey`         |

- The prefix respects the level boundary: `arkuest/prod` does not take `arkuest/production`.
- Google does not accept `/` or `:` in names, so levels there use `__`, as in environment variables.
- In Vault, each field of a secret is a key, and the fields of the secret at the path itself sit at the root.
- With the same name in a secret and a parameter, the secret wins.

### ExpandJson

Off, the default, the value stays as text, even when it is JSON. That fits **one secret per key**, including a Google
service account key in `Storage:CredentialsJson`.

On, a value that is a JSON object becomes child keys. That fits **one document with many keys**, such as the Secrets
Manager "key/value" secret:

```json
{ "Jwt": { "Secret": "..." }, "Storage__SecretKey": "...", "OpenId": { "Entra": { "ClientSecret": "..." } } }
```

Kept in the secret `arkuest/prod`, the prefix itself, it becomes `Jwt:Secret`, `Storage:SecretKey` and
`OpenId:Entra:ClientSecret`. Text inside the document is never parsed as JSON again, so a service account key kept
there as text stays whole. The expansion applies to **every** value of the source: with one secret per key, leave it
off.

### What the Tooark packages read from the store

Any configuration key can come from the store. The family's sensitive ones:

| Package                    | Keys                                                                                        |
| -------------------------- | ------------------------------------------------------------------------------------------- |
| `Tooark.Storage.Aws`       | `Storage:AccessKey`, `Storage:SecretKey`, `Storage:SessionToken`                            |
| `Tooark.Storage.Gcp`       | `Storage:CredentialsJson`                                                                   |
| `Tooark.Securities`        | `Jwt:Secret`, `Jwt:PrivateKey`, `Cryptography:Secret`, `DataProtection:CertificatePassword` |
| `Tooark.Securities.OpenId` | `OpenId:{provider}:ClientSecret`                                                            |
| `Tooark.Observability`     | the OTLP `Headers`, with the collector key                                                  |

---

## 🔍 Runtime reads

`ISecretService` reads a secret by its name in the store. The value stays cached for `CacheMinutes`; a missing secret
is not cached, so it is read as soon as it is created.

| Method                       | Returns   | Description                                                                      |
| ---------------------------- | --------- | -------------------------------------------------------------------------------- |
| `GetAsync(name)`             | `string?` | Current value of the secret, or null when it does not exist                      |
| `GetFieldAsync(name, field)` | `string?` | Field of a JSON secret, exact case; null when the secret or the field is missing |

In Vault, the value is the JSON object with the secret's fields, and `GetFieldAsync` reads one of them. Parameters do
not go through the service: they are configuration.

```csharp
using Tooark.Secrets.Interfaces;

public sealed class TenantService(ISecretService secrets)
{
  public Task<string?> ApiKey(string tenant, CancellationToken cancellationToken) =>
    secrets.GetFieldAsync($"arkuest/tenants/{tenant}", "ApiKey", cancellationToken);
}
```

---

## 📝 Usage Examples

### One document with every key (AWS)

```json
{ "Secrets": { "SecretsPrefix": "arkuest/prod", "ExpandJson": true } }
```

The secret `arkuest/prod` holds the JSON document, and each field becomes a key.

### One secret per key (Google Cloud)

```json
{ "Secrets": { "ProjectId": "arkuest", "SecretsPrefix": "arkuest-prod" } }
```

The secrets `arkuest-prod__Jwt__Secret` and `arkuest-prod__Storage__CredentialsJson` become `Jwt:Secret` and
`Storage:CredentialsJson`.

### Optional source in development

```csharp
builder.Configuration.AddTooarkSecretsVault(options => options.Optional = builder.Environment.IsDevelopment());
```

---

## 📋 Dependencies

| Package                                                                                                                                       | Version  | Description                                 |
| --------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                       | 4.x      | Exceptions (`InternalServerErrorException`) |
| [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions) | 8.x/10.x | Configuration abstractions and binder       |

The package runs on the base .NET runtime, without ASP.NET Core and without any cloud SDK.

---

## 🎯 Best Practices

1. **Nothing sensitive in `appsettings.json`** — with the store, the versioned file keeps only what is not secret:
   region, bucket, prefix.
2. **Environment identity for the store** — role, service account or Kubernetes auth; the store credential cannot
   come from the store.
3. **One prefix per environment** — `arkuest/prod`, `arkuest/staging`: the application's permission stays scoped to
   its own.
4. **Rotation with a restart** — Tooark packages read their options at startup; change the secret and deploy again or
   restart the instances.
5. **`Optional` only where it makes sense** — in production, starting without the secrets is usually worse than not
   starting.

---

## ⚠️ Error Codes and Solutions

| Message                               | Exception                      | Description                                           | Solution                                                 |
| ------------------------------------- | ------------------------------ | ----------------------------------------------------- | -------------------------------------------------------- |
| `Secrets.LoadFailed;{store}`          | `InternalServerErrorException` | The source did not load: access, network or timeout   | See the `InnerException`; check the identity and network |
| `Secrets.NameRequired`                | `BadRequestException`          | Blank secret name                                     | Provide the name                                         |
| `Secrets.FieldRequired`               | `BadRequestException`          | Blank field in `GetFieldAsync`                        | Provide the field                                        |
| `Secrets.NotJsonObject;{name}`        | `InternalServerErrorException` | `GetFieldAsync` on a secret that is not a JSON object | Use `GetAsync`, or keep the secret as an object          |
| `Secrets.AccessDenied`                | `InternalServerErrorException` | The identity cannot read the secret                   | Grant the read permission                                |
| `Secrets.OperationFailed`             | `InternalServerErrorException` | Store or network failure                              | See the `InnerException`                                 |
| `Options.Secrets.TimeoutInvalid`      | `InternalServerErrorException` | `TimeoutSeconds` below 1                              | Use at least 1 second                                    |
| `Options.Secrets.CacheMinutesInvalid` | `InternalServerErrorException` | Negative `CacheMinutes`                               | Use zero or more                                         |
| `Options.Secrets.SourceNotConfigured` | `InternalServerErrorException` | Configuration source without a prefix or path         | Provide the store prefix or path                         |

Each store's own errors are in its README.

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the
[Tooark](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the
[LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
