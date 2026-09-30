# Tooark.Securities

Security library for .NET applications, providing **AES cryptography** and **JWT authentication** services with support for multiple algorithms, plus the configuration of ASP.NET Core **Data Protection**.

📖 **Docs:** [Tooark.Securities on the site](https://tooark.com/nuget-tooark/packages/tooark.securities.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.pt-BR.md)

---

## 📑 Contents

- [Package Contents](#-package-contents)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [JWT - Supported Algorithms](#-jwt---supported-algorithms)
- [Cryptography - Supported Algorithms](#-cryptography---supported-algorithms)
- [Data Protection - Key Ring](#-data-protection---key-ring)
- [Usage Examples](#-usage-examples)
- [Generating Keys](#-generating-keys)
- [Dependencies](#-dependencies)
- [Best Practices](#-best-practices)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 📦 Package Contents

### Services

| Class                                                        | Description                                    |
| ------------------------------------------------------------ | ---------------------------------------------- |
| [`JwtTokenService`](#jwt---creating-and-validating-a-token)  | Service for creating and validating JWT tokens |
| [`CryptographyService`](#cryptography---encrypt-and-decrypt) | AES-256 encryption/decryption service          |

### Interfaces

| Interface              | Description                            |
| ---------------------- | -------------------------------------- |
| `IJwtTokenService`     | Contract for handling JWT tokens       |
| `ICryptographyService` | Contract for AES encryption/decryption |

| Member                           | Description                                                            |
| -------------------------------- | ---------------------------------------------------------------------- |
| `IJwtTokenService.Create`        | Creates the token. Synchronous: the handler exposes no async creation  |
| `IJwtTokenService.ValidateAsync` | Validates the token. Direct path to the handler, which is asynchronous |
| `IJwtTokenService.Validate`      | Validates the token. Synchronous wrapper of `ValidateAsync`            |
| `ICryptographyService.Encrypt`   | Encrypts. Synchronous: in-memory AES, no I/O                           |
| `ICryptographyService.Decrypt`   | Decrypts. Synchronous, for the same reason                             |

### DTOs

| Class          | Description                                     |
| -------------- | ----------------------------------------------- |
| `JwtTokenDto`  | Data for creating a token (id, login, security) |
| `UserTokenDto` | Result of the token validation, with the claims |

### Options

| Class                 | Description                                                            |
| --------------------- | ---------------------------------------------------------------------- |
| `JwtOptions`          | JWT settings (algorithm, keys, issuer(s), audience(s), expiration)     |
| `CryptographyOptions` | Cryptography settings (algorithm, secret or secretBase64)              |
| `KeyRingOptions`      | Data Protection key ring (application, storage, certificate, rotation) |

### Extensions

| Class                 | Description                                                             |
| --------------------- | ----------------------------------------------------------------------- |
| `RoleClaimsExtension` | Extension that converts `JwtTokenDto` into claims (id, login, security) |

---

## 🔧 Installation

```bash
dotnet add package Tooark.Securities
```

---

## ⚙️ Configuration

### appsettings.json

To configure the security services, add the `Jwt`, `Cryptography` and `DataProtection` sections to your `appsettings.json`:

Example configuration for a JWT token with symmetric algorithms (uses _Secret_ for signing and validation):

```json
{
  "Jwt": {
    "Algorithm": "HS256",
    "Secret": "your-secret-key-with-at-least-32-characters",
    "Issuer": "your-application",
    "Audience": "your-clients",
    "ExpirationTime": 60
  }
}
```

Example configuration for a JWT token with asymmetric algorithms (_PrivateKey_ required for signing, _PublicKey_ required for validation):

```json
{
  "Jwt": {
    "Algorithm": "RS256",
    "PrivateKey": "your-private-key-in-base64",
    "PublicKey": "your-public-key-in-base64",
    "Issuer": "your-application",
    "Audience": "your-clients",
    "ExpirationTime": 60
  }
}
```

Example configuration for AES cryptography:

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "Secret": "your-secret-key-with-32-characters"
  }
}
```

Or with a ready-made AES key in Base64 (**recommended** — the key is used directly, without derivation):

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "SecretBase64": "32-byte-key-in-base64"
  }
}
```

#### `JwtOptions` properties

| Property         | Type      | Default | Description                                              |
| ---------------- | --------- | ------- | -------------------------------------------------------- |
| `Algorithm`      | string    | `ES256` | Signing algorithm. Unknown values throw                  |
| `Secret`         | string?   | `null`  | Key for symmetric algorithms (HS256/384/512)             |
| `PrivateKey`     | string?   | `null`  | Base64 private key (PKCS#8) for signing                  |
| `PublicKey`      | string?   | `null`  | Base64 public key (SPKI) for validation                  |
| `Issuer`         | string?   | `null`  | Issuer accepted on validation and written to the token   |
| `Issuers`        | string[]? | `null`  | Additional issuers accepted on validation                |
| `Audience`       | string?   | `null`  | Audience accepted on validation and written to the token |
| `Audiences`      | string[]? | `null`  | Additional audiences accepted on validation              |
| `ExpirationTime` | int       | `5`     | Token expiration in **minutes**                          |

> `Issuer`/`Issuers` and `Audience`/`Audiences` add up: providing any of them turns on the validation of
> that field. When none is provided, the validation of that field is turned off. The `audience` parameter
> of `Create` and `Validate`/`ValidateAsync` takes precedence over the configuration.
>
> `PrivateKey` and `PublicKey` accept both the raw Base64 and the full PEM: the
> `-----BEGIN/END ... KEY-----` delimiters and the line breaks are removed on assignment.

Example with multiple issuers and audiences:

```json
{
  "Jwt": {
    "Algorithm": "ES256",
    "PublicKey": "your-public-key-in-base64",
    "Issuers": ["legacy-api", "new-api"],
    "Audiences": ["web-app", "mobile-app"],
    "ExpirationTime": 60
  }
}
```

#### `CryptographyOptions` properties

| Property       | Type    | Default | Description                                              |
| -------------- | ------- | ------- | -------------------------------------------------------- |
| `Algorithm`    | string  | `GCM`   | AES-256 mode: `GCM`, `CBC` or `CBCUnsafe` (decrypt only) |
| `Secret`       | string? | `null`  | Key derived via SHA256 (compatibility)                   |
| `SecretBase64` | string? | `null`  | 32-byte AES key used directly (**recommended**)          |

> `Secret` **or** `SecretBase64` is required. When both are provided, `SecretBase64` takes precedence.
> `SecretBase64` must represent exactly 32 bytes (AES-256) — invalid values fail at startup. Generate a
> key with `Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`. With `Secret`, the key is derived
> via SHA256 (kept for compatibility).

Example configuration for Data Protection (every key is optional — see
[Data Protection - Key Ring](#-data-protection---key-ring)):

```json
{
  "DataProtection": {
    "ApplicationName": "my-application",
    "RequirePersistentKeyStorage": true,
    "KeysPath": "/var/dataprotection/keys",
    "CertificatePath": "/var/dataprotection/certificate.pfx",
    "CertificatePassword": "certificate-password"
  }
}
```

#### `KeyRingOptions` properties

| Property                        | Type                                                 | Default | Description                                                                       |
| ------------------------------- | ---------------------------------------------------- | ------- | --------------------------------------------------------------------------------- |
| `ApplicationName`               | string?                                              | `null`  | Name that isolates the key ring. Same name and same storage share the keys        |
| `KeysPath`                      | string?                                              | `null`  | Key directory. Null keeps the ASP.NET Core default location                       |
| `KeyLifetimeDays`               | int?                                                 | `null`  | Lifetime of each key in **days** (minimum 7). Null keeps the ASP.NET Core 90 days |
| `DisableAutomaticKeyGeneration` | bool                                                 | `false` | Only reads the keys, never creates new ones (another app rotates the key ring)    |
| `RequirePersistentKeyStorage`   | bool?                                                | `null`  | With `true`, the app does not start without a key storage. Null keeps it off      |
| `CertificatePath`               | string?                                              | `null`  | RSA `.pfx` certificate, with private key, that protects the keys at rest          |
| `CertificatePassword`           | string?                                              | `null`  | Password of the `CertificatePath` certificate                                     |
| `CertificateThumbprint`         | string?                                              | `null`  | Certificate from the `My` store (user or machine), instead of `CertificatePath`   |
| `ReadKeys`                      | `Func<IServiceProvider, Task<IEnumerable<string>>>?` | `null`  | Reads the XML of every key from the custom storage. Code only                     |
| `WriteKey`                      | `Func<IServiceProvider, string, string, Task>?`      | `null`  | Writes a new key (name and XML) to the custom storage. Code only                  |
| `ConfigureDataProtection`       | `Action<IDataProtectionBuilder>?`                    | `null`  | Callback with the native builder, run last. Code only                             |

> No key is required: whatever is not provided follows the ASP.NET Core default. Validation only stops, at
> startup, what would fail later: a lifetime below 7 days, `CertificatePath` together with
> `CertificateThumbprint`, `CertificatePassword` without `CertificatePath`, a certificate that is missing,
> unreadable, without a private key or without an RSA key, only one of the `ReadKeys` and `WriteKey` callbacks,
> and the callbacks together with `KeysPath`. The key storage lock (`RequirePersistentKeyStorage`)
> is opt-in: see [Clusters, containers and autoscaling](#clusters-containers-and-autoscaling).

### Program.cs

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adds the security services
builder.Services.AddTooarkSecurities(builder.Configuration);

var app = builder.Build();
```

Or, to add only the JWT token service:

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adds the security services
builder.Services.AddTooarkJwtToken(builder.Configuration);

var app = builder.Build();
```

Or, to add only the cryptography service:

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adds the security services
builder.Services.AddTooarkCryptography(builder.Configuration);

var app = builder.Build();
```

Or, to add only Data Protection:

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adds Data Protection with the key ring from the DataProtection section
builder.Services.AddTooarkDataProtection(builder.Configuration);

var app = builder.Build();
```

---

## 🔐 JWT - Supported Algorithms

### Symmetric Algorithms (HMAC)

| Algorithm | Description | Requirements       |
| --------- | ----------- | ------------------ |
| `HS256`   | HMAC-SHA256 | Secret (≥32 bytes) |
| `HS384`   | HMAC-SHA384 | Secret (≥48 bytes) |
| `HS512`   | HMAC-SHA512 | Secret (≥64 bytes) |

> The minimums follow RFC 7518 (HMAC key at least as long as the hash output) and are validated at
> startup (`Options.Jwt.SecretTooShort`). The size is measured in **bytes** (UTF-8): non-ASCII characters
> take more than one byte.

### Asymmetric Algorithms (RSA)

| Algorithm | Description    | Requirements                      |
| --------- | -------------- | --------------------------------- |
| `RS256`   | RSA-SHA256     | PrivateKey/PublicKey (≥2048 bits) |
| `RS384`   | RSA-SHA384     | PrivateKey/PublicKey (≥2048 bits) |
| `RS512`   | RSA-SHA512     | PrivateKey/PublicKey (≥2048 bits) |
| `PS256`   | RSA-PSS-SHA256 | PrivateKey/PublicKey (≥2048 bits) |
| `PS384`   | RSA-PSS-SHA384 | PrivateKey/PublicKey (≥2048 bits) |
| `PS512`   | RSA-PSS-SHA512 | PrivateKey/PublicKey (≥2048 bits) |

### Asymmetric Algorithms (ECDsa)

| Algorithm | Description  | Required Curve    |
| --------- | ------------ | ----------------- |
| `ES256`   | ECDSA-SHA256 | P-256 (secp256r1) |
| `ES384`   | ECDSA-SHA384 | P-384 (secp384r1) |
| `ES512`   | ECDSA-SHA512 | P-521 (secp521r1) |

> The default algorithm is `ES256` when `Algorithm` is not provided. Unknown values throw
> (`Options.Jwt.AlgorithmNotSupported`) — in security configuration, an invalid algorithm is never silently
> replaced by another. Internally, creation and validation use the `JsonWebTokenHandler` (the current
> Microsoft.IdentityModel handler); `UserTokenDto` accepts both `JsonWebToken` and the legacy
> `JwtSecurityToken`.

---

## 🔒 Cryptography - Supported Algorithms

| Algorithm   | Mode        | Description                                |
| ----------- | ----------- | ------------------------------------------ |
| `GCM`       | AES-256-GCM | **Recommended** - Authenticated encryption |
| `CBC`       | AES-256-CBC | Traditional mode with random IV            |
| `CBCUnsafe` | AES-256-CBC | ⚠️ Legacy - zero IV, **decryption only**   |

> `CBCUnsafe` exists only to read old data encrypted with a zero IV: encrypting in this mode throws
> (`Options.Cryptography.AlgorithmDecryptOnly`). For new data, use `GCM`.

---

## 🔏 Data Protection - Key Ring

ASP.NET Core Data Protection protects the short-lived payloads of the application — authentication cookie,
OpenID Connect correlation cookies and `state`, antiforgery, TempData — without anyone configuring it. The
default, however, writes the keys to the local disk of each instance: with more than one instance, or in a
container that restarts without a volume, one instance cannot open what another protected (`Correlation failed`,
`Unable to unprotect the message.State`, users signed out). `AddTooarkDataProtection` configures where the key
ring lives, how the keys are protected and which application it isolates.

The feature is optional and does not replace `ICryptographyService`:

| Feature                | Use for                                     | Key                                          |
| ---------------------- | ------------------------------------------- | -------------------------------------------- |
| `ICryptographyService` | Data stored indefinitely (database columns) | Fixed, under the application's control       |
| Data Protection        | Short-lived payloads (cookies, link tokens) | Rotates on its own, every 90 days by default |

> Do not use Data Protection for long-lived data at rest: if the key ring is lost, everything it protected
> becomes unreadable.

### Storage and protection beyond the options

The options cover what Data Protection offers without an extra package: directory, certificate and
[custom storage through callbacks](#custom-storage-with-readkeys-and-writekey). For Redis,
Azure Blob Storage, a database or Azure Key Vault, install the provider package and use
`ConfigureDataProtection`, which receives the native builder and runs last:

```csharp
builder.Services.AddTooarkDataProtection(builder.Configuration, options =>
{
    options.ConfigureDataProtection = dataProtection => dataProtection
        .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
});
```

Nothing is locked: `services.AddDataProtection()` is still available before or after, and whatever is registered
last wins.

### Custom storage with `ReadKeys` and `WriteKey`

To keep the keys in a database, a storage or a vault without installing a provider package, provide two
functions: `ReadKeys`, which returns the XML of every key, and `WriteKey`, which writes a new key. They replace
`KeysPath` (setting both fails at startup) and satisfy the `RequirePersistentKeyStorage` lock.

- Each call receives the service provider of its own scope: scoped services, such as a `DbContext`, are resolved
  directly.
- `WriteKey` receives the key name (`key-{guid}`, unique) and the XML. With a certificate configured, the XML
  arrives already encrypted, and the storage never sees the key in the clear.
- ASP.NET Core calls the storage synchronously, and Tooark waits for the task. Calls are rare: reads when the key
  ring loads (at startup and every 24 hours) and after a key is created; writes on the first run and on every
  rotation (90 days by default).
- An exception in the callbacks reaches whoever uses Data Protection. Let it propagate: handling the failure and
  returning an empty list makes ASP.NET Core create new keys (see the table below).
- The callbacks exist in code only, like `ConfigureDataProtection`.

#### ✅ Do: a table in the application database

```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Database=my-api;..."
  },
  "DataProtection": {
    "ApplicationName": "my-api",
    "RequirePersistentKeyStorage": true,
    "CertificatePath": "/run/secrets/dataprotection.pfx"
  }
}
```

```csharp
using Microsoft.EntityFrameworkCore;
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddTooarkDataProtection(builder.Configuration, options =>
{
    // Every key, expired ones included: they still open what they protected
    options.ReadKeys = async services =>
    {
        var db = services.GetRequiredService<AppDbContext>();

        return await db.DataProtectionKeys.Select(key => key.Xml).ToListAsync();
    };

    // Insert only: each key has a unique name and is never changed or deleted
    options.WriteKey = async (services, name, xml) =>
    {
        var db = services.GetRequiredService<AppDbContext>();

        db.DataProtectionKeys.Add(new DataProtectionKeyRecord { Name = name, Xml = xml });
        await db.SaveChangesAsync();
    };
});

var app = builder.Build();

app.Run();
```

The table is a regular entity of the application `DbContext`, created by a migration before the first startup:

```csharp
public class DataProtectionKeyRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Xml { get; set; } = string.Empty;
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DataProtectionKeyRecord> DataProtectionKeys => Set<DataProtectionKeyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Unique name: a second write with the same name fails instead of overwriting the key
        modelBuilder.Entity<DataProtectionKeyRecord>().HasIndex(key => key.Name).IsUnique();
    }
}
```

The outcome: every instance reads the same keys, a recreated pod still opens the old tokens, and each rotation
adds a row to the table, keeping the previous ones. The certificate password comes from outside
`appsettings.json`, through the `DataProtection__CertificatePassword` environment variable.

#### ❌ Don't: callbacks that lose keys

> ⚠️ **These callbacks lose data, and the lock does not notice:** as far as it knows, a storage is configured.

```csharp
// ❌ In-memory list: each instance has its own, and it is gone on every restart
var keys = new List<string>();

builder.Services.AddTooarkDataProtection(builder.Configuration, options =>
{
    options.ReadKeys = _ => Task.FromResult<IEnumerable<string>>(keys);
    options.WriteKey = (_, _, xml) =>
    {
        keys.Add(xml);
        return Task.CompletedTask;
    };
});
```

```csharp
// ❌ A single row, overwritten on every new key: the previous key disappears
options.ReadKeys = async services =>
{
    var db = services.GetRequiredService<AppDbContext>();
    var current = await db.DataProtectionKeys.SingleOrDefaultAsync();

    return current is null ? [] : [current.Xml];
};

options.WriteKey = async (services, name, xml) =>
{
    var db = services.GetRequiredService<AppDbContext>();
    var current = await db.DataProtectionKeys.SingleOrDefaultAsync();

    if (current is null)
    {
        db.DataProtectionKeys.Add(new DataProtectionKeyRecord { Name = name, Xml = xml });
    }
    else
    {
        current.Name = name;
        current.Xml = xml;
    }

    await db.SaveChangesAsync();
};
```

| Mistake                                                         | What happens                                                                                                                                                                                                                                        |
| --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Keeping the keys in memory (list, `IMemoryCache`, static field) | Each instance has its own keys, and they are gone on restart: `The key {…} was not found in the key ring` on the other instance and after every deploy. It is the same as configuring no storage                                                    |
| Overwriting the key instead of inserting                        | It works until the first rotation (90 days by default) and passes any short test. On rotation the previous key disappears: seconds later, when the key ring reloads, everything it protected stops opening, from sessions and tokens to stored data |
| Reading only the latest key, or filtering out expired ones      | The same effect as the previous row, even with the table intact                                                                                                                                                                                     |
| Deleting old keys (cleanup routine, TTL, cache with expiration) | What they protected becomes unreadable. For data at rest, the loss is permanent                                                                                                                                                                     |
| Handling the read failure and returning an empty list           | With no keys, ASP.NET Core creates and writes new keys, and what was protected before does not open on that instance. Let the exception propagate                                                                                                   |

### Clusters, containers and autoscaling

With no storage configured, ASP.NET Core writes the keys to the user profile, which in a container lives inside
the instance itself. The application starts normally and only logs a warning. The problem shows up when the pod
is recreated or a second instance joins: users signed out, OpenID Connect sign-in failing and, for whatever was
protected at rest, unreadable data.

`RequirePersistentKeyStorage` turns that warning into a startup failure. With `true`, the application does not
start without a key storage (`Options.DataProtection.KeyStorageNotConfigured`): `KeysPath`, the `ReadKeys` and
`WriteKey` callbacks, a `PersistKeysTo*` in `ConfigureDataProtection` or one chained to `services.AddDataProtection()`, before or after
`AddTooarkDataProtection`. The check runs when the host starts and, without a host, on the first use of Data
Protection.

> The lock is off by default in this version, so applications already on v4.3.0 keep their behavior. It is
> planned to be on by default in containers in v5.0. An API that uses no cookies (JWT only) does not depend on
> persistent keys: set `false` to keep it off after that change too.

#### ❌ Don't: keys tied to the instance

```json
{
  "DataProtection": {
    "ApplicationName": "my-api"
  }
}
```

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTooarkDataProtection(builder.Configuration);

var app = builder.Build();

app.Run();
```

> ⚠️ **This configuration loses data.** With no `KeysPath` and no external storage, each container creates its
> own key in `~/.aspnet/DataProtection-Keys`, inside itself. Nothing fails on registration or at startup: the log
> only warns `Storing keys in a directory '…' that may not be persisted outside of the container. Protected data
will be unavailable when container is destroyed.`

| When                                               | What happens                                                                                                                                                                                                                                                                                    |
| -------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| The request reaches another instance               | The other instance does not have the key: `The key {…} was not found in the key ring`. The session cookie is discarded and the user goes back to sign-in, OpenID Connect sign-in fails with `Correlation failed` or `Unable to unprotect the message.State`, and antiforgery forms are rejected |
| Deploy, restart, recreated pod or autoscaling down | The key goes away with the container, and every session drops at once                                                                                                                                                                                                                           |
| A protected value was stored in the database       | **Lost for good**: no new instance has the key, and there is no way to recover it                                                                                                                                                                                                               |

With `"RequirePersistentKeyStorage": true`, this same configuration does not start: the error shows up on deploy, not
later, with users already signed in.

#### ❌ Don't: `KeysPath` without a shared volume

```json
{
  "DataProtection": {
    "ApplicationName": "my-api",
    "RequirePersistentKeyStorage": true,
    "KeysPath": "/var/dataprotection/keys"
  }
}
```

> ⚠️ **The lock passes and the data is lost just the same.** The directory exists, but without a mounted volume
> it sits in each container's writable layer: the outcome is the one from the previous example. The same goes for
> a volume that only one pod mounts, such as a per-replica StatefulSet volume or a `ReadWriteOnce` disk. The lock
> checks that a storage exists, not that it outlives the instance.

#### ✅ Do: a shared, protected key ring

```json
{
  "DataProtection": {
    "ApplicationName": "my-api",
    "RequirePersistentKeyStorage": true,
    "KeysPath": "/var/dataprotection/keys",
    "CertificatePath": "/run/secrets/dataprotection.pfx"
  }
}
```

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Shared key ring: same ApplicationName, same directory and same certificate on every instance
builder.Services.AddTooarkDataProtection(builder.Configuration);

var app = builder.Build();

app.Run();
```

- `/var/dataprotection/keys` is a volume shared by every instance: NFS, Azure Files or EFS, and in Kubernetes a
  `PersistentVolumeClaim` with `ReadWriteMany`.
- `/run/secrets/dataprotection.pfx` is the certificate mounted as a secret, and the password comes from outside
  `appsettings.json`, through the `DataProtection__CertificatePassword` environment variable. To create the
  certificate, see [Data Protection Certificate (.pfx)](#data-protection-certificate-pfx).
- The outcome: one instance opens what another protected, a recreated pod still opens the old tokens, and the
  keys are stored encrypted on the volume.

Without a shared volume, replace `KeysPath` with the `ReadKeys` and `WriteKey` callbacks (see
[Custom storage with `ReadKeys` and `WriteKey`](#custom-storage-with-readkeys-and-writekey)) or with a provider
in `ConfigureDataProtection`. The lock accepts all of them.

#### Other risks

The lock does not check these. Before running more than one instance, also check:

- **Redis without persistence** — without AOF or RDB, or with a `maxmemory-policy` that evicts keys
  (`allkeys-lru`), Redis loses the key ring. Use persistence and `noeviction`.
- **Key certificate** — if the `.pfx` is lost, or replaced without keeping the old one, the key ring becomes
  unreadable. Keep it with the application secrets and keep the old one in `UnprotectKeysWithAnyCertificate`
  while there are keys protected by it.
- **Storage cleanup** — Data Protection does not delete expired keys: they still open what they protected. Keep
  the storage out of cleanup routines and TTLs.
- **`ApplicationName`** — without it, isolation depends on the application path, which can change with the
  image. Set the same value on every instance.

### With `AddTooarkSecurities`

`AddTooarkSecurities` registers Data Protection when the `DataProtection` section exists and has at least one
key. If the application also calls `AddTooarkDataProtection` — to pass `ConfigureDataProtection`, for example —
the explicit call prevails in any order: `AddTooarkSecurities` does not reapply the section on top of it.

---

## 📝 Usage Examples

### JWT - Creating and Validating a Token

#### Configuration with HMAC (Symmetric)

```json
{
  "Jwt": {
    "Algorithm": "HS256",
    "Secret": "my-super-secure-secret-key-32chars",
    "Issuer": "my-api",
    "Audience": "my-clients",
    "ExpirationTime": 60
  }
}
```

#### Configuration with RSA (Asymmetric)

```json
{
  "Jwt": {
    "Algorithm": "RS256",
    "PrivateKey": "MIIEvQIBADANBgkqh...base64-private-key...",
    "PublicKey": "MIIBIjANBgkqhkiG9w...base64-public-key...",
    "Issuer": "my-api",
    "Audience": "my-clients",
    "ExpirationTime": 60
  }
}
```

#### Creating a Token

```csharp
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtService;

    public AuthController(IJwtTokenService jwtService)
    {
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        // Validate credentials...

        // Create the token data
        var tokenData = new JwtTokenDto(
            id: user.Id,
            login: user.Email,
            security: user.SecurityStamp
        );

        // Generate the token
        var token = _jwtService.Create(tokenData);

        return Ok(new { Token = token });
    }

    // With a custom audience and extra claims
    [HttpPost("login-custom")]
    public IActionResult LoginCustom(LoginRequest request)
    {
        var tokenData = new JwtTokenDto(user.Id, user.Email, user.SecurityStamp);

        var extraClaims = new[]
        {
            new Claim("role", "admin"),
            new Claim("department", "IT")
        };

        var token = _jwtService.Create(tokenData, audience: "mobile-app", extraClaims: extraClaims);

        return Ok(new { Token = token });
    }
}
```

#### Validating a Token

The Microsoft.IdentityModel handler exposes validation only asynchronously, so `ValidateAsync` is the
direct path and `Validate` is the synchronous wrapper. In code that is already asynchronous, prefer
`ValidateAsync`:

```csharp
[HttpGet("validate")]
public async Task<IActionResult> ValidateTokenAsync([FromHeader] string authorization)
{
    var token = authorization.Replace("Bearer ", "");

    var result = await _jwtService.ValidateAsync(token);

    if (!string.IsNullOrEmpty(result.ErrorToken))
    {
        return Unauthorized(new { Error = result.ErrorToken });
    }

    return Ok(new { UserId = result.Id, Login = result.Login });
}
```

In synchronous code, the blocking overload delivers the same result:

```csharp
[HttpGet("validate")]
public IActionResult ValidateToken([FromHeader] string authorization)
{
    var token = authorization.Replace("Bearer ", "");

    var result = _jwtService.Validate(token);

    if (!string.IsNullOrEmpty(result.ErrorToken))
    {
        return Unauthorized(new { Error = result.ErrorToken });
    }

    return Ok(new
    {
        UserId = result.Id,
        Login = result.Login,
        // Or using the helpers
        GuidId = result.GetGuidId,
        IntId = result.GetIntId
    });
}
```

#### Reading extra claims

The extra claims passed to `Create` come back in `UserTokenDto`. The `Claims` property holds every claim
in the token — the user's, the extra ones and the ones registered by the issuer (`exp`, `iat`, `iss`,
`aud`) — and the methods below read a claim by type. Like `Id`, `Login` and `Security`, a missing claim
does not throw:

| Method              | Returns                                                                   |
| ------------------- | ------------------------------------------------------------------------- |
| `GetClaim(type)`    | The claim's first value, or empty when it does not exist                  |
| `GetClaim<T>(type)` | The converted value (`Guid`, `int`, `bool`, `decimal`...), or the default |
| `GetClaims(type)`   | Every value of the claim (roles, permissions), or an empty list           |
| `HasClaim(type)`    | Whether the claim exists in the token, even with an empty value           |

```csharp
// Creation: the extra claims go into the token
var extraClaims = new[]
{
    new Claim("tenant", tenantId.ToString()),
    new Claim("role", "admin"),
    new Claim("role", "user")
};

var token = _jwtService.Create(tokenData, extraClaims: extraClaims);

// Validation: the same claims come out in UserTokenDto
var result = await _jwtService.ValidateAsync(token);

var tenant = result.GetClaim<Guid>("tenant"); // Guid.Empty when missing or invalid
var roles = result.GetClaims("role");         // ["admin", "user"]
```

> A claim repeated at creation becomes an array in the payload and comes back as one claim per value: use
> `GetClaims` to read them all. `GetClaim<T>` converts with the invariant culture, so decimal numbers use
> a dot (`1234.56`).

To give the application's own claims a name and a type, keep the names in `UserTokenDto` extensions in
the application project. The rest of the code reads properties, without repeating strings:

```csharp
public static class AppClaims
{
    public const string Tenant = "tenant";
    public const string Role = "role";
}

public static class UserTokenDtoExtensions
{
    public static Guid GetTenantId(this UserTokenDto user) => user.GetClaim<Guid>(AppClaims.Tenant);

    public static IReadOnlyList<string> GetRoles(this UserTokenDto user) => user.GetClaims(AppClaims.Role);
}

// Usage
var tenantId = result.GetTenantId();
```

With C# 14 (.NET 10 SDK), the same shortcuts can be extension properties:

```csharp
public static class UserTokenDtoExtensions
{
    extension(UserTokenDto user)
    {
        public Guid TenantId => user.GetClaim<Guid>(AppClaims.Tenant);

        public IReadOnlyList<string> Roles => user.GetClaims(AppClaims.Role);
    }
}

// Usage
var tenantId = result.TenantId;
```

---

### Cryptography - Encrypt and Decrypt

#### Configuration

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "Secret": "my-secret-key-32-characters-long"
  }
}
```

#### Using ICryptographyService

```csharp
public class SensitiveDataService
{
    private readonly ICryptographyService _crypto;

    public SensitiveDataService(ICryptographyService crypto)
    {
        _crypto = crypto;
    }

    public string ProtectSensitiveData(string plainText)
    {
        // Returns the encrypted text in Base64
        return _crypto.Encrypt(plainText);
    }

    public string UnprotectData(string encryptedText)
    {
        // Returns the original decrypted text
        return _crypto.Decrypt(encryptedText);
    }
}
```

#### Full Example

```csharp
// Configure in Program.cs (registers ICryptographyService as a singleton)
builder.Services.AddTooarkCryptography(builder.Configuration);

// Use in the service
public class UserService
{
    private readonly ICryptographyService _crypto;

    public UserService(ICryptographyService crypto)
    {
        _crypto = crypto;
    }

    public void SaveUser(User user)
    {
        // Encrypt sensitive data before saving
        user.CreditCard = _crypto.Encrypt(user.CreditCard);
        user.SSN = _crypto.Encrypt(user.SSN);

        // Save to the database...
    }

    public User GetUser(int id)
    {
        var user = // Fetch from the database...

        // Decrypt sensitive data
        user.CreditCard = _crypto.Decrypt(user.CreditCard);
        user.SSN = _crypto.Decrypt(user.SSN);

        return user;
    }
}
```

---

### Data Protection - Protecting a Link Token

With Data Protection registered, any service can create its own protector. The purpose isolates each use: a
protector with another purpose cannot open these tokens.

```csharp
public class InviteService
{
    private readonly IDataProtector _protector;

    public InviteService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Invites");
    }

    public string CreateToken(Guid inviteId) => _protector.Protect(inviteId.ToString());

    public Guid? ReadToken(string token)
    {
        try
        {
            return Guid.Parse(_protector.Unprotect(token));
        }
        catch (CryptographicException)
        {
            // Tampered token or one protected by another key ring
            return null;
        }
    }
}
```

> For a token that expires on its own, use `provider.CreateProtector("Invites").ToTimeLimitedDataProtector()` and
> pass the lifetime to `Protect` (package `Microsoft.AspNetCore.DataProtection.Extensions`, already included in
> ASP.NET Core).

---

## 🔑 Generating Keys

### Key for HMAC (HS256/HS384/HS512)

```bash
# Generate a random 32-byte (256-bit) key for HS256
openssl rand -base64 32

# Generate a random 64-byte (512-bit) key for HS512
openssl rand -base64 64
```

### RSA Keys (RS256/PS256)

```bash
# Generate a 2048-bit RSA private key
openssl genrsa -out private.pem 2048

# Extract the public key
openssl rsa -in private.pem -pubout -out public.pem

# Convert to the PKCS8 format (recommended)
openssl pkcs8 -topk8 -inform PEM -outform PEM -nocrypt -in private.pem -out private_pkcs8.pem

# Get the key in Base64 (without headers)
cat private_pkcs8.pem | grep -v "BEGIN\|END" | tr -d '\n'
cat public.pem | grep -v "BEGIN\|END" | tr -d '\n'
```

### ECDsa Keys (ES256/ES384/ES512)

```bash
# ES256 (P-256)
openssl ecparam -genkey -name prime256v1 -noout -out ec_private.pem
openssl ec -in ec_private.pem -pubout -out ec_public.pem

# ES384 (P-384)
openssl ecparam -genkey -name secp384r1 -noout -out ec_private.pem

# ES512 (P-521)
openssl ecparam -genkey -name secp521r1 -noout -out ec_private.pem

# Convert to PKCS8
openssl pkcs8 -topk8 -nocrypt -in ec_private.pem -out ec_private_pkcs8.pem
```

### Data Protection Certificate (.pfx)

```bash
# Self-signed 2048-bit RSA certificate, valid for 2 years
openssl req -x509 -newkey rsa:2048 -sha256 -days 730 -nodes -subj "/CN=dataprotection" \
  -keyout dataprotection.key -out dataprotection.crt

# Bundle the certificate and the private key into PKCS#12
openssl pkcs12 -export -inkey dataprotection.key -in dataprotection.crt \
  -out dataprotection.pfx -passout pass:certificate-password
```

> The certificate must be **RSA** (the XML encryption of the keys does not accept ECDSA). When replacing it, keep
> the old one available with `UnprotectKeysWithAnyCertificate` in `ConfigureDataProtection` until the keys it
> protected expire.

---

## 📋 Dependencies

| Package                                                                                                                         | Version  | Description                                                       |
| ------------------------------------------------------------------------------------------------------------------------------- | -------- | ----------------------------------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                         | 4.x      | Exceptions (e.g. `BadRequestException`)                           |
| [`Microsoft.AspNetCore.Authentication.JwtBearer`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer) | 8.x/10.x | JWT authentication for ASP.NET Core                               |
| [`Microsoft.AspNetCore.DataProtection`](https://www.nuget.org/packages/Microsoft.AspNetCore.DataProtection)                     | 8.x/10.x | Data Protection, standalone package that runs on the base runtime |

---

## 🎯 Best Practices

### JWT

1. **Use asymmetric algorithms (RS/PS/ES) in production** - Allows validating tokens without exposing the signing key
2. **Configure `ExpirationTime` appropriately** - Short-lived tokens are safer
3. **Use `Issuer` and `Audience`** - Prevents tokens from being misused across applications
4. **Store keys in a Secret Manager** - Never commit keys to source code

### Cryptography

1. **Prefer GCM over CBC** - GCM provides built-in authentication
2. **Never use CBCUnsafe for new data** - Decrypt-only mode for compatibility with legacy systems
3. **Prefer `SecretBase64` with a random 32-byte key** - The key is used directly, without derivation
4. **Generate random keys** - Use `openssl rand -base64 32` or `RandomNumberGenerator.GetBytes(32)`

### Data Protection

1. **Set `ApplicationName`** - Isolation stays the same on every instance, regardless of the install path
2. **Share the key ring across instances** - A persistent volume in `KeysPath`, the `ReadKeys` and `WriteKey` callbacks or a storage through `ConfigureDataProtection`
3. **Turn on `RequirePersistentKeyStorage` in containers** - The application no longer starts with the keys tied to the instance
4. **In the callbacks, only insert and read everything** - An overwritten, filtered or deleted key makes what it protected unreadable
5. **Protect the keys at rest** - With `KeysPath` or callbacks and no certificate, the keys are written unencrypted, on any operating system
6. **Do not use it for long-lived data** - Encrypted database columns belong to `ICryptographyService`

---

## ⚠️ Error Codes and Solutions

| Service                   | Message                                               | Description                                            | Solution                                                      | Exception             |
| ------------------------- | ----------------------------------------------------- | ------------------------------------------------------ | ------------------------------------------------------------- | --------------------- |
| `CryptographyService`     | `Options.NotConfigured`                               | `Options` not configured                               | Configure `CryptographyOptions`                               | `InternalServerError` |
| `CryptographyService`     | `Options.Cryptography.SecretNotConfigured`            | No key configured                                      | Configure `Secret` or `SecretBase64` in `CryptographyOptions` | `InternalServerError` |
| `CryptographyService`     | `Options.Cryptography.SecretBase64Invalid`            | `SecretBase64` is not valid Base64                     | Provide a valid Base64 value in `SecretBase64`                | `InternalServerError` |
| `CryptographyService`     | `Options.Cryptography.SecretBase64InvalidSize`        | `SecretBase64` is not 32 bytes                         | Use a key of exactly 32 bytes (AES-256)                       | `InternalServerError` |
| `CryptographyService`     | `Options.Cryptography.AlgorithmDecryptOnly`           | `CBCUnsafe` used to encrypt                            | Use `CBCUnsafe` only to decrypt legacy data                   | `InternalServerError` |
| `CryptographyService`     | `Cryptography.PlainTextNotProvided`                   | `PlainText` not provided                               | Provide the plain text to encrypt                             | `BadRequest`          |
| `CryptographyService`     | `Cryptography.CipherTextNotProvided`                  | `CipherText` not provided                              | Provide the encrypted text to decrypt                         | `BadRequest`          |
| `CryptographyService`     | `Cryptography.InvalidCipherText`                      | Invalid `CipherText`                                   | Provide a valid encrypted text to decrypt                     | `BadRequest`          |
| `AddTooarkDataProtection` | `Options.DataProtection.KeyLifetimeTooShort;7`        | `KeyLifetimeDays` below the minimum                    | Use at least 7 days, or leave it null for the default 90      | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateAmbiguous`         | `CertificatePath` and `CertificateThumbprint` together | Provide only one of them                                      | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificatePathNotConfigured` | `CertificatePassword` without `CertificatePath`        | Provide `CertificatePath` or remove the password              | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateNotFound;{value}`  | File or thumbprint not found                           | Check the path, or install the certificate in the `My` store  | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateInvalid`           | Unreadable `.pfx` or wrong password                    | Check the file and `CertificatePassword`                      | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateWithoutPrivateKey` | Certificate without a private key                      | Export the `.pfx` with the private key                        | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateNotRsa`            | Certificate without an RSA key                         | Use an RSA certificate                                        | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.KeyStorageNotConfigured`      | Lock on and no key storage configured                  | Set a shared `KeysPath`, the callbacks or an external storage | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.KeyCallbacksIncomplete`       | Only one of the `ReadKeys` and `WriteKey` callbacks    | Provide both callbacks                                        | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.KeyStorageAmbiguous`          | `KeysPath` together with `ReadKeys` and `WriteKey`     | Provide a single key storage                                  | `InternalServerError` |
| `JwtTokenService`         | `Options.NotConfigured`                               | `Options` not configured                               | Configure `JwtOptions`                                        | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.SecretNotConfigured`                     | `Secret` not configured                                | Configure `Secret` in `JwtOptions` for a symmetric token      | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.SecretTooShort`                          | `Secret` below the algorithm minimum                   | Use at least 32/48/64 bytes for HS256/HS384/HS512             | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.KeysNotConfigured`                       | `Private` and `Public` not configured                  | Configure the keys in `JwtOptions` for an asymmetric token    | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.PrivateKey.InvalidSize`                  | Invalid `Private` key size                             | Use a `Private` key of at least 2048 bits                     | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.PublicKey.InvalidSize`                   | Invalid `Public` key size                              | Use a `Public` key of at least 2048 bits                      | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.PrivateKey.InvalidCurve`                 | Invalid `Private` key curve                            | Use a `Private` key with the correct curve                    | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.PublicKey.InvalidCurve`                  | Invalid `Public` key curve                             | Use a `Public` key with the correct curve                     | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.InvalidKey`                              | Invalid key                                            | [Use valid keys](#-generating-keys) for the chosen algorithm  | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.AlgorithmNotSupported`                   | Unsupported algorithm                                  | [Use a supported algorithm](#-jwt---supported-algorithms)     | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.KeyNotConfigured;PrivateKey`             | `Private` key not configured                           | Configure `PrivateKey` in `JwtOptions` to generate a token    | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.KeyNotConfigured;PublicKey`              | `Public` key not configured                            | Configure `PublicKey` in `JwtOptions` to validate a token     | `InternalServerError` |
| `JwtTokenService`         | `Token.Expired`                                       | Expired token                                          | Generate a new token                                          | N/A                   |
| `JwtTokenService`         | `Token.InvalidSignature`                              | Token with an invalid signature                        | Use only tokens with a valid signature                        | N/A                   |
| `JwtTokenService`         | `Token.Invalid`                                       | Invalid token                                          | Use only valid tokens                                         | N/A                   |
| `JwtTokenService`         | `InternalServerError`                                 | Internal server error                                  | Check the logs for details                                    | N/A                   |

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
