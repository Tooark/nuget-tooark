# Tooark.Secrets.Aws

[`Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets) provider on AWS: it loads
**Secrets Manager** secrets and **Parameter Store** parameters into `IConfiguration`, and implements `ISecretService`
on Secrets Manager.

The model, the common options and the conversion of names into keys are in the
[`Tooark.Secrets` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md). This README covers
what is specific to AWS.

📖 **Docs:** [Tooark.Secrets.Aws on the site](https://tooark.com/nuget-tooark/packages/tooark.secrets.aws.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Aws/README.pt-BR.md)

---

## 📑 Contents

- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Permissions](#-permissions)
- [Behavior on AWS](#-behavior-on-aws)
- [Dependencies](#-dependencies)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 🔧 Installation

```bash
dotnet add package Tooark.Secrets.Aws
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Secrets": {
    "Region": "sa-east-1",
    "SecretsPrefix": "arkuest/prod",
    "ParametersPath": "/arkuest/prod"
  }
}
```

### Program.cs

```csharp
using Tooark.Secrets.Aws.Injections;

// Secrets and parameters in the configuration, on top of appsettings.json
builder.Configuration.AddTooarkSecretsAws();

// Runtime secret reads, with cache
builder.Services.AddTooarkSecretsAws(builder.Configuration);
```

The `IConfigurationBuilder` `AddTooarkSecretsAws` requires `SecretsPrefix`, `ParametersPath` or both. The
`IServiceCollection` one registers `ISecretService` and the Secrets Manager client as `IAmazonSecretsManager`; a
client already registered by the application is kept.

### `AwsSecretsOptions` properties

Besides the [common options](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md#secretsoptions-properties):

| Property         | Type      | Default | Description                                                                                  |
| ---------------- | --------- | ------- | -------------------------------------------------------------------------------------------- |
| `Region`         | `string?` | —       | Store region, such as `sa-east-1`. Without it, the region from the AWS default chain applies |
| `ServiceUrl`     | `string?` | —       | Address of a compatible service, such as LocalStack                                          |
| `SecretsPrefix`  | `string?` | —       | Name prefix of the Secrets Manager secrets that become configuration, such as `arkuest/prod` |
| `ParametersPath` | `string?` | —       | Path of the Parameter Store parameters that become configuration, such as `/arkuest/prod`    |

Credentials always come from the **AWS default chain**: environment variables, the `~/.aws/credentials` profile, the
ECS task role, the EKS pod role or the EC2 instance profile. There is no access key in the options: the store
credential cannot come from the store.

---

## 🔑 Permissions

The application's identity needs:

| Permission                      | Resource                                    | For                                          |
| ------------------------------- | ------------------------------------------- | -------------------------------------------- |
| `secretsmanager:ListSecrets`    | `*` (the action takes no specific resource) | Listing the prefix's secrets                 |
| `secretsmanager:GetSecretValue` | ARN of the prefix's secrets                 | Reading the secrets and `ISecretService`     |
| `ssm:GetParametersByPath`       | ARN of the parameters path                  | Listing and reading the parameters           |
| `kms:Decrypt`                   | Your own KMS key, when used                 | Secrets and `SecureString` with your own key |

---

## 🟧 Behavior on AWS

- **Secrets Manager** — the prefix filters the listing on AWS and is then checked against the level boundary. A
  secret scheduled for deletion does not show up, and one deleted between listing and reading is skipped. A binary
  secret is read as UTF-8 text.
- **"Key/value" secret** — Secrets Manager stores this type as JSON. With `ExpandJson`, each pair becomes a key;
  without it, the key receives the whole JSON.
- **Parameter Store** — the path is read recursively, with `SecureString` decrypted. A `StringList` arrives as the
  comma-separated text.
- **Same name** — with the same name in a secret and a parameter, the secret wins.
- **`ISecretService`** — the name is the secret's name or its ARN; a missing secret returns null.

---

## 📋 Dependencies

| Package                                                                                                                                         | Version  | Description                    |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------ |
| [`Tooark.Secrets`](https://www.nuget.org/packages/Tooark.Secrets)                                                                               | 4.x      | Model and configuration source |
| [`AWSSDK.SecretsManager`](https://www.nuget.org/packages/AWSSDK.SecretsManager)                                                                 | 4.x      | Secrets Manager client         |
| [`AWSSDK.SimpleSystemsManagement`](https://www.nuget.org/packages/AWSSDK.SimpleSystemsManagement)                                               | 4.x      | Parameter Store client         |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Container registration         |

---

## ⚠️ Error Codes and Solutions

The common errors are in the
[`Tooark.Secrets` README](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md#️-error-codes-and-solutions).
A failure to load the source arrives as `Secrets.LoadFailed;AWS`, with the AWS error in `InnerException`.

| Message                                         | Description                                           | Solution                                              |
| ----------------------------------------------- | ----------------------------------------------------- | ----------------------------------------------------- |
| `Options.Secrets.Aws.ServiceUrlInvalid;{value}` | `ServiceUrl` is not an absolute `http` or `https` URL | Use the full address, such as `http://localhost:4566` |

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
