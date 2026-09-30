# Tooark.Secrets.Gcp

[`Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets) provider on Google Cloud: it loads
**Secret Manager** secrets and **Parameter Manager** parameters into `IConfiguration`, and implements `ISecretService`
on Secret Manager.

The model, the common options and the conversion of names into keys are in the
[`Tooark.Secrets` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md). This README covers
what is specific to Google Cloud.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Gcp/README.pt-BR.md)

---

## 📑 Contents

- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Permissions](#-permissions)
- [Behavior on Google Cloud](#-behavior-on-google-cloud)
- [Dependencies](#-dependencies)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 🔧 Installation

```bash
dotnet add package Tooark.Secrets.Gcp
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Secrets": {
    "ProjectId": "arkuest",
    "SecretsPrefix": "arkuest-prod",
    "ParametersPrefix": "arkuest-prod"
  }
}
```

### Program.cs

```csharp
using Tooark.Secrets.Gcp.Injections;

// Secrets and parameters in the configuration, on top of appsettings.json
builder.Configuration.AddTooarkSecretsGcp();

// Runtime secret reads, with cache
builder.Services.AddTooarkSecretsGcp(builder.Configuration);
```

The `IConfigurationBuilder` `AddTooarkSecretsGcp` requires `SecretsPrefix`, `ParametersPrefix` or both. The
`IServiceCollection` one registers `ISecretService`; the Secret Manager client is created on first use, and a
`SecretManagerServiceClient` already registered by the application is used instead.

### `GcpSecretsOptions` properties

Besides the [common options](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md#secretsoptions-properties):

| Property           | Type      | Default | Description                                                                                   |
| ------------------ | --------- | ------- | --------------------------------------------------------------------------------------------- |
| `ProjectId`        | `string?` | —       | Google Cloud project holding the secrets and parameters. Required                             |
| `SecretsPrefix`    | `string?` | —       | Name prefix of the Secret Manager secrets that become configuration                           |
| `ParametersPrefix` | `string?` | —       | Name prefix of the Parameter Manager parameters (`global` location) that become configuration |

The credential always comes from **Application Default Credentials**: the Cloud Run, GKE or Compute Engine service
account, the `GOOGLE_APPLICATION_CREDENTIALS` variable, or `gcloud auth application-default login` in development.
There is no key in the options: the store credential cannot come from the store.

### Names with a double underscore

Google only accepts letters, digits, `_` and `-` in names, so levels use `__`:

| Secret or parameter                         | Key                           |
| ------------------------------------------- | ----------------------------- |
| `arkuest-prod__Jwt__Secret`                 | `Jwt:Secret`                  |
| `arkuest-prod__Storage__CredentialsJson`    | `Storage:CredentialsJson`     |
| `arkuest-prod__OpenId__Entra__ClientSecret` | `OpenId:Entra:ClientSecret`   |
| `arkuest-prod`, with `ExpandJson`           | the keys of the JSON document |

---

## 🔑 Permissions

The application's identity needs:

| Role or permission                                  | For                                      |
| --------------------------------------------------- | ---------------------------------------- |
| `roles/secretmanager.viewer` on the project         | Listing the prefix's secrets             |
| `roles/secretmanager.secretAccessor` on the secrets | Reading the secrets and `ISecretService` |
| Listing parameters, listing and rendering versions  | Loading the Parameter Manager parameters |

A parameter that references Secret Manager secrets is rendered with the parameter's own identity, which needs to read
those secrets.

---

## 🟦 Behavior on Google Cloud

- **Secret Manager** — the `latest` version of each secret applies. A secret without an enabled version is skipped.
- **Parameter Manager** — the most recently created enabled version applies, because Parameter Manager has no
  `latest` alias. The version is **rendered**: references to Secret Manager secrets arrive resolved. Only the `global`
  location is read.
- **JSON parameter** — with `ExpandJson`, a parameter in JSON format becomes several keys. YAML parameters arrive as
  text.
- **Same name** — with the same name in a secret and a parameter, the secret wins.
- **`ISecretService`** — the name is the secret's identifier in the options' project; a missing secret returns null.

---

## 📋 Dependencies

| Package                                                                                                                                         | Version  | Description                    |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------ |
| [`Tooark.Secrets`](https://www.nuget.org/packages/Tooark.Secrets)                                                                               | 4.x      | Model and configuration source |
| [`Google.Cloud.SecretManager.V1`](https://www.nuget.org/packages/Google.Cloud.SecretManager.V1)                                                 | 2.x      | Secret Manager client          |
| [`Google.Cloud.ParameterManager.V1`](https://www.nuget.org/packages/Google.Cloud.ParameterManager.V1)                                           | 1.x      | Parameter Manager client       |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Container registration         |

---

## ⚠️ Error Codes and Solutions

The common errors are in the
[`Tooark.Secrets` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md#️-error-codes-and-solutions).
A failure to load the source arrives as `Secrets.LoadFailed;Google Cloud`, with the Google error in `InnerException`.

| Message                                      | Description                                           | Solution                                          |
| -------------------------------------------- | ----------------------------------------------------- | ------------------------------------------------- |
| `Options.Secrets.Gcp.ProjectIdNotConfigured` | Missing `ProjectId`                                   | Configure `Secrets:ProjectId`                     |
| `Options.Secrets.Gcp.CredentialsUnavailable` | No Application Default Credentials in the environment | Run with a service account or configure local ADC |

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
