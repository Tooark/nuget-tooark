# Tooark.Secrets.Vault

[`Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets) provider on **HashiCorp Vault**
and **OpenBao**: it loads the KV version 2 secrets into `IConfiguration` and implements `ISecretService` on them.
OpenBao keeps the Vault API, so the same package serves both.

The client is the package's own, on the HTTP API, without a third-party library: it covers reading and listing the KV
and authentication by token, AppRole or Kubernetes.

The model, the common options and the conversion of names into keys are in the
[`Tooark.Secrets` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md). This README covers
what is specific to Vault.

📖 **Docs:** [Tooark.Secrets.Vault on the site](https://tooark.com/nuget-tooark/packages/tooark.secrets.vault.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Vault/README.pt-BR.md)

---

## 📑 Contents

- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Authentication](#-authentication)
- [Access policy](#-access-policy)
- [Behavior on Vault](#-behavior-on-vault)
- [Dependencies](#-dependencies)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 🔧 Installation

```bash
dotnet add package Tooark.Secrets.Vault
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Secrets": {
    "Address": "https://vault.company.com:8200",
    "Path": "arkuest/prod",
    "AuthMethod": "Kubernetes",
    "Role": "arkuest"
  }
}
```

### Program.cs

```csharp
using Tooark.Secrets.Vault.Injections;

// KV secrets in the configuration, on top of appsettings.json
builder.Configuration.AddTooarkSecretsVault();

// Runtime secret reads, with cache
builder.Services.AddTooarkSecretsVault(builder.Configuration);
```

The `IConfigurationBuilder` `AddTooarkSecretsVault` requires `Path`. The `IServiceCollection` one registers
`ISecretService`, which reads any KV path.

### `VaultSecretsOptions` properties

Besides the [common options](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md#secretsoptions-properties):

| Property              | Type      | Default                           | Description                                          |
| --------------------- | --------- | --------------------------------- | ---------------------------------------------------- |
| `Address`             | `string?` | `VAULT_ADDR`/`BAO_ADDR`           | Store address. Required                              |
| `Namespace`           | `string?` | `VAULT_NAMESPACE`/`BAO_NAMESPACE` | Namespace, in Vault Enterprise or OpenBao            |
| `Mount`               | `string`  | `secret`                          | Mount path of the KV version 2                       |
| `Path`                | `string?` | —                                 | KV path that becomes configuration, read recursively |
| `AuthMethod`          | `string`  | `Token`                           | `Token`, `AppRole` or `Kubernetes`                   |
| `AuthMount`           | `string?` | `approle`/`kubernetes`            | Mount path of the authentication method              |
| `Token`               | `string?` | `VAULT_TOKEN`/`BAO_TOKEN`         | Token, in the `Token` method                         |
| `RoleId`              | `string?` | —                                 | Role ID, in the `AppRole` method                     |
| `SecretId`            | `string?` | —                                 | Secret ID, in the `AppRole` method                   |
| `Role`                | `string?` | —                                 | Store role, in the `Kubernetes` method               |
| `KubernetesTokenPath` | `string`  | pod service account token         | Token file read in the `Kubernetes` method           |

A missing address, token and namespace come from the command-line client's environment variables: first `VAULT_*`,
then `BAO_*`. A value set in the configuration wins over them.

---

## 🔑 Authentication

| Method       | When to use                         | How it works                                                                    |
| ------------ | ----------------------------------- | ------------------------------------------------------------------------------- |
| `Kubernetes` | Application in a Kubernetes cluster | The pod's service account token goes into the login; no secret in configuration |
| `AppRole`    | Machine, VM or pipeline             | Role ID in configuration and Secret ID delivered by the deployment environment  |
| `Token`      | Development and tests               | The token goes straight into the requests, without login                        |

The AppRole and Kubernetes token is obtained on first use. When the store rejects it, because it expired or was
revoked, the client logs in again and repeats the request once. The `Token` method's token is not renewed.

---

## 🔒 Access policy

The identity's policy needs to read the secrets and list the path's folders:

```hcl
path "secret/data/arkuest/prod/*" {
  capabilities = ["read"]
}

path "secret/data/arkuest/prod" {
  capabilities = ["read"]
}

path "secret/metadata/arkuest/prod/*" {
  capabilities = ["list"]
}

path "secret/metadata/arkuest/prod" {
  capabilities = ["list"]
}
```

---

## 🧭 Behavior on Vault

- **One field, one key** — the `SecretKey` field of the secret `arkuest/prod/Storage` becomes `Storage:SecretKey`; the
  fields of the secret at `Path` itself sit at the root.
- **Recursive read** — `Path` is walked folder by folder, up to 10 levels. A path that is both a secret and a folder
  has both read.
- **JSON fields** — a field's value stays as text, even when it is JSON, unless `ExpandJson`. A field that is not
  text (number, object) arrives in JSON format.
- **Current version** — the current version of each secret applies; a deleted secret is skipped.
- **`ISecretService`** — the name is the KV path, and the value is the JSON object with the fields; `GetFieldAsync`
  reads one field.

---

## 📋 Dependencies

| Package                                                                                                                                         | Version  | Description                    |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------ |
| [`Tooark.Secrets`](https://www.nuget.org/packages/Tooark.Secrets)                                                                               | 4.x      | Model and configuration source |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Container registration         |

There is no third-party client: the calls to the store use the .NET `HttpClient`.

---

## ⚠️ Error Codes and Solutions

The common errors are in the
[`Tooark.Secrets` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md#️-error-codes-and-solutions).
A failure to load the source arrives as `Secrets.LoadFailed;Vault`, with the store error in `InnerException`.

| Message                                             | Description                                      | Solution                                          |
| --------------------------------------------------- | ------------------------------------------------ | ------------------------------------------------- |
| `Options.Secrets.Vault.AddressNotConfigured`        | No address nor `VAULT_ADDR`/`BAO_ADDR`           | Configure `Secrets:Address`                       |
| `Options.Secrets.Vault.AddressInvalid;{value}`      | Address is not an absolute `http` or `https` URL | Use the full address, with the port               |
| `Options.Secrets.Vault.AuthMethodInvalid;{value}`   | Unknown authentication method                    | Use `Token`, `AppRole` or `Kubernetes`            |
| `Options.Secrets.Vault.TokenNotConfigured`          | `Token` method without a token nor `VAULT_TOKEN` | Provide the token through the environment         |
| `Options.Secrets.Vault.AppRoleNotConfigured`        | `AppRole` method without Role ID or Secret ID    | Provide both                                      |
| `Options.Secrets.Vault.KubernetesRoleNotConfigured` | `Kubernetes` method without `Role`               | Provide the role configured in the store          |
| `Secrets.Vault.LoginFailed`                         | The store rejected the login                     | Check the role, the Role ID and the Secret ID     |
| `Secrets.Vault.KubernetesTokenNotFound;{path}`      | The service account token does not exist         | Check `automountServiceAccountToken` and the path |

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
