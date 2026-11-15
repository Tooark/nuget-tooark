# Tooark.Securities

Security library for .NET applications, providing **AES cryptography** (single key or multiple keys with rotation), **JWT** issuing, validation and **Bearer authentication** with support for multiple algorithms, **password hashing** (PBKDF2), **secure tokens**, **TOTP** second factor with recovery codes, plus the configuration of ASP.NET Core **Data Protection**.

📖 **Docs:** [Tooark.Securities on the site](https://tooark.com/nuget-tooark/packages/tooark.securities.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.pt-BR.md)

---

## 📑 Contents

- [Package Contents](#-package-contents)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [JWT - Supported Algorithms](#-jwt---supported-algorithms)
- [JWT - Bearer Authentication](#-jwt---bearer-authentication)
- [Cryptography - Supported Algorithms](#-cryptography---supported-algorithms)
- [Cryptography - Multiple Keys and Rotation](#-cryptography---multiple-keys-and-rotation)
- [Data Protection - Key Ring](#-data-protection---key-ring)
- [Password Hashing](#-password-hashing)
- [Secure Tokens, TOTP and Recovery Codes](#-secure-tokens-totp-and-recovery-codes)
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

| Class                                                                     | Description                                                                             |
| ------------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| [`JwtTokenService`](#jwt---creating-and-validating-a-token)               | Service for creating and validating JWT tokens                                          |
| [`CryptographyService`](#cryptography---encrypt-and-decrypt)              | AES-256 encryption/decryption service with a single key                                 |
| [`KeyedCryptographyService`](#-cryptography---multiple-keys-and-rotation) | AES-256 with multiple identified keys and rotation; also serves the single-key contract |
| [`PasswordHashService`](#-password-hashing)                               | Password hashing with PBKDF2-HMAC-SHA256 in the PHC format                              |

### Interfaces

| Interface                   | Description                                                                |
| --------------------------- | -------------------------------------------------------------------------- |
| `IJwtTokenService`          | Contract for handling JWT tokens                                           |
| `ICryptographyService`      | Contract for AES encryption/decryption                                     |
| `IKeyedCryptographyService` | `ICryptographyService` plus key identification, rotation and re-encryption |
| `IPasswordHashService`      | Contract for hashing and verifying passwords                               |

| Member                                           | Description                                                                   |
| ------------------------------------------------ | ----------------------------------------------------------------------------- |
| `IJwtTokenService.Create`                        | Creates the token. Synchronous: the handler exposes no async creation         |
| `IJwtTokenService.ValidateAsync`                 | Validates the token. Direct path to the handler, which is asynchronous        |
| `IJwtTokenService.Validate`                      | Validates the token. Synchronous wrapper of `ValidateAsync`                   |
| `ICryptographyService.Encrypt`                   | Encrypts. Synchronous: in-memory AES, no I/O                                  |
| `ICryptographyService.Decrypt`                   | Decrypts. Synchronous, for the same reason                                    |
| `IKeyedCryptographyService.CurrentKeyId`         | Identifier of the key that encrypts new values                                |
| `IKeyedCryptographyService.KeyIds`               | Identifiers of every configured key                                           |
| `IKeyedCryptographyService.Encrypt(text, keyId)` | Encrypts with a specific key                                                  |
| `IKeyedCryptographyService.KeyIdOf`              | Reads the key identifier of a value; `null` for a legacy value without prefix |
| `IKeyedCryptographyService.IsCurrent`            | Whether the value was encrypted with the current key                          |
| `IKeyedCryptographyService.ReEncrypt`            | Decrypts with the key that wrote the value and encrypts with the current one  |
| `IPasswordHashService.Hash`                      | Hashes a password with a random salt                                          |
| `IPasswordHashService.Verify`                    | Checks a password against a stored hash, in constant time; never throws       |
| `IPasswordHashService.NeedsRehash`               | Whether the hash uses parameters older than the current options               |

### Static classes

| Class              | Description                                                                                                       |
| ------------------ | ----------------------------------------------------------------------------------------------------------------- |
| `SecureToken`      | Random tokens (password reset, e-mail confirmation, API key): `Create`, `Hash` and `Verify`                       |
| `Totp`             | Time-based one-time passwords (RFC 6238): secret, `otpauth://` URI, `Compute` and `Verify` with replay protection |
| `RecoveryCodes`    | Second-factor recovery codes: `Generate` and `Normalize`, stored as `SecureToken` hashes                          |
| `TooarkClaimNames` | Names of the claims the token service emits and reads (`id`, `login`, `security`, `sub`, `preferred_username`)    |

### DTOs

| Class          | Description                                     |
| -------------- | ----------------------------------------------- |
| `JwtTokenDto`  | Data for creating a token (id, login, security) |
| `UserTokenDto` | Result of the token validation, with the claims |

### Options

| Class                 | Description                                                                     |
| --------------------- | ------------------------------------------------------------------------------- |
| `JwtOptions`          | JWT settings (algorithm, keys, issuer(s), audience(s), expiration, claim names) |
| `CryptographyOptions` | Cryptography settings (algorithm, secret or secretBase64, identified keys)      |
| `PasswordHashOptions` | Password hashing settings (iterations, salt size, hash size)                    |
| `KeyRingOptions`      | Data Protection key ring (application, storage, certificate, rotation)          |

### Extensions

| Class                 | Description                                                                                          |
| --------------------- | ---------------------------------------------------------------------------------------------------- |
| `RoleClaimsExtension` | Extension that converts `JwtTokenDto` into claims, with the legacy names, the standard names or both |

---

## 🔧 Installation

```bash
dotnet add package Tooark.Securities
```

---

## ⚙️ Configuration

### appsettings.json

To configure the security services, add the `Jwt`, `Cryptography`, `PasswordHash` and `DataProtection` sections to your `appsettings.json`. Only the sections of the services you use are needed, and `PasswordHash` is optional even then (safe defaults):

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

Example configuration for AES cryptography, with the 32-byte AES key in Base64 (generate it with
`openssl rand -base64 32`; the key is used directly, without derivation):

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "SecretBase64": "32-byte-key-in-base64"
  }
}
```

Or with several identified keys, to rotate the key without re-encrypting everything at once (see
[Cryptography - Multiple Keys and Rotation](#-cryptography---multiple-keys-and-rotation)). `SecretBase64` stays
only while there are values written before the keys were adopted:

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "CurrentKeyId": "k2",
    "Keys": {
      "k1": "old-32-byte-key-in-base64",
      "k2": "new-32-byte-key-in-base64"
    },
    "SecretBase64": "legacy-32-byte-key-in-base64"
  }
}
```

Example configuration for password hashing (every key is optional — these are the defaults):

```json
{
  "PasswordHash": {
    "Iterations": 600000,
    "SaltSize": 16,
    "HashSize": 32
  }
}
```

#### `JwtOptions` properties

| Property             | Type      | Default | Description                                                                |
| -------------------- | --------- | ------- | -------------------------------------------------------------------------- |
| `Algorithm`          | string    | `ES256` | Signing algorithm. Unknown values throw                                    |
| `Secret`             | string?   | `null`  | Key for symmetric algorithms (HS256/384/512)                               |
| `PrivateKey`         | string?   | `null`  | Base64 private key (PKCS#8) for signing                                    |
| `PublicKey`          | string?   | `null`  | Base64 public key (SPKI) for validation                                    |
| `Issuer`             | string?   | `null`  | Issuer accepted on validation and written to the token                     |
| `Issuers`            | string[]? | `null`  | Additional issuers accepted on validation                                  |
| `Audience`           | string?   | `null`  | Audience accepted on validation and written to the token                   |
| `Audiences`          | string[]? | `null`  | Additional audiences accepted on validation                                |
| `ExpirationTime`     | int       | `5`     | Token expiration in **minutes**                                            |
| `EmitLegacyClaims`   | bool      | `false` | Emits the `id` and `login` claims (Tooark names of the 4.x line)           |
| `EmitStandardClaims` | bool      | `true`  | Emits the `sub` and `preferred_username` claims (JWT/OpenID Connect names) |

> `Issuer`/`Issuers` and `Audience`/`Audiences` add up: providing any of them turns on the validation of
> that field. When none is provided, the validation of that field is turned off. The `audience` parameter
> of `Create` and `Validate`/`ValidateAsync` takes precedence over the configuration.
>
> `PrivateKey` and `PublicKey` accept both the raw Base64 and the full PEM: the
> `-----BEGIN/END ... KEY-----` delimiters and the line breaks are removed on assignment.
>
> ⚠️ **Changed in v5:** the claim defaults inverted — `EmitStandardClaims` is now `true` and `EmitLegacyClaims`
> is `false`. A token issued without explicit flags carries `sub` and `preferred_username`, not `id` and `login`.
> To keep the 4.x tokens, set `EmitLegacyClaims = true`. At least one of them must be on; both off fail at startup
> (`Options.Jwt.ClaimsNotConfigured`). See [Claim names](#claim-names).

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

| Property       | Type                         | Default | Description                                              |
| -------------- | ---------------------------- | ------- | -------------------------------------------------------- |
| `Algorithm`    | string                       | `GCM`   | AES-256 mode: `GCM`, `CBC` or `CBCUnsafe` (decrypt only) |
| `SecretBase64` | string?                      | `null`  | 32-byte AES key used directly                            |
| `CurrentKeyId` | string?                      | `null`  | Identifier of the key in `Keys` that encrypts new values |
| `Keys`         | `Dictionary<string, string>` | empty   | Identified 32-byte AES keys in Base64, for rotation      |

> Without `Keys`, `SecretBase64` is required. It must represent exactly 32 bytes (AES-256) — invalid values
> fail at startup. Generate a key with `Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`.
>
> With `Keys`, `CurrentKeyId` is required and must be one of the identifiers; each identifier uses only letters,
> digits, hyphen and underscore (never a dot), and each key is exactly 32 bytes in Base64 — anything else fails
> at startup. `SecretBase64` becomes the **legacy key**: it only opens values written without a key prefix, and
> can be removed once none is left.
>
> ⚠️ **Changed in v5:** the text `Secret` of the 4.x line was removed. The key it derived is the SHA-256 of the
> text: put that value in `SecretBase64` and everything encrypted on 4.x keeps opening, with no re-encryption,
> including the legacy values read with `CBCUnsafe`:
> `Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret)))`. A `"Secret"` left in the
> `Cryptography` section is ignored, and without `SecretBase64` or `Keys` the service fails with
> `Options.Cryptography.SecretNotConfigured`.
>
> ⚠️ **Changed in v5:** an unknown `Algorithm` (a typo such as `GMC`) fails at startup with
> `Options.Cryptography.AlgorithmNotSupported`. On the 4.x line it silently fell back to `GCM`. Missing or empty
> still means `GCM`.

#### `PasswordHashOptions` properties

| Property     | Type | Default  | Description                                               |
| ------------ | ---- | -------- | --------------------------------------------------------- |
| `Iterations` | int  | `600000` | PBKDF2 iterations (OWASP recommendation). Minimum 100.000 |
| `SaltSize`   | int  | `16`     | Salt size in bytes. Minimum 16                            |
| `HashSize`   | int  | `32`     | Derived hash size in bytes. Minimum 32                    |

> Values below the minimums fail at startup: a weak configuration is a configuration error, never silently
> replaced. Raising a value does not break stored hashes — they keep validating and
> [`NeedsRehash`](#-password-hashing) tells the login flow to rewrite them.

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
| `RequirePersistentKeyStorage`   | bool?                                                | `null`  | With `true`, the app does not start without a key storage. Null: on in containers |
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
> is on in containers and off elsewhere: see
> [Clusters, containers and autoscaling](#clusters-containers-and-autoscaling).

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

Or, to add only the password hashing service (`AddTooarkSecurities` already registers it, with or without the
`PasswordHash` section):

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adds the password hashing service; the configuration and the callback are optional
builder.Services.AddTooarkPasswordHash(builder.Configuration, options => options.Iterations = 800_000);

var app = builder.Build();
```

To authenticate the requests of an API with the tokens the service itself issues, register the `Bearer` scheme
from the same `Jwt` section (see [JWT - Bearer Authentication](#-jwt---bearer-authentication)):

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTooarkJwtToken(builder.Configuration);   // issues the tokens
builder.Services.AddTooarkJwtBearer(builder.Configuration);  // validates them on every request
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

> `SecureToken`, `Totp` and `RecoveryCodes` are static classes: nothing to register.

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

## 🪪 JWT - Bearer Authentication

`AddTooarkJwtBearer` registers the ASP.NET Core `Bearer` authentication scheme for the tokens that
`JwtTokenService` issues. It reads the same `Jwt` section and builds the handler's `TokenValidationParameters`
with the same rules as `Validate`: same algorithm and key (HMAC secret or public key), issuer(s), audience(s),
required expiration and signature, zero clock skew. A token accepted by one is accepted by the other, and
nobody repeats the key, issuer and audience in a hand-written `AddJwtBearer`.

```csharp
builder.Services.AddTooarkJwtToken(builder.Configuration);
builder.Services.AddTooarkJwtBearer(builder.Configuration, bearer =>
{
    // Optional: the native options, applied last
    bearer.Challenge = "Bearer realm=\"orders-api\"";
});
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/me", (ClaimsPrincipal user) => new
{
    Id = user.FindFirstValue(TooarkClaimNames.Id),
    Login = user.FindFirstValue(TooarkClaimNames.Login)
}).RequireAuthorization();
```

What the registration sets:

- **Scheme `Bearer`** (`JwtBearerDefaults.AuthenticationScheme`) as the default authentication scheme, unless the
  application already defined one — the first registered scheme wins, as in `Tooark.Securities.OpenId`.
- **`MapInboundClaims = false`**: the claims reach the `ClaimsPrincipal` with the names written in the token
  (`id`, `sub`, `login`), not with the `ClaimTypes` URIs. Read them with the `TooarkClaimNames` constants.
- **Fail fast**: the key is loaded and validated at registration. Without a validation key (asymmetric algorithm
  with `PrivateKey` only) the startup fails with `Options.Jwt.KeyNotConfigured;PublicKey`.
- The `configure` callback receives the native `JwtBearerOptions` and runs last, so anything can be adjusted,
  including the Tooark defaults.

The application still needs `app.UseAuthentication()` and `app.UseAuthorization()`. A resource server that only
validates tokens (no issuing) configures the `Jwt` section with the `PublicKey` only and calls
`AddTooarkJwtBearer` alone.

### Claim names

`JwtTokenService` writes the user data as claims. Two naming sets exist, chosen by `JwtOptions`:

| Data       | Legacy name (`EmitLegacyClaims`) | Standard name (`EmitStandardClaims`) | Standard            |
| ---------- | -------------------------------- | ------------------------------------ | ------------------- |
| `Id`       | `id`                             | `sub`                                | RFC 7519            |
| `Login`    | `login`                          | `preferred_username`                 | OpenID Connect Core |
| `Security` | `security`                       | `security` (no standard equivalent)  | —                   |

- Since **v5**, the default emits the standard names only (`EmitStandardClaims = true`,
  `EmitLegacyClaims = false`). The 4.x line emitted the legacy names only; a consumer that still reads `id` or
  `login` needs `EmitLegacyClaims = true` until it moves to `sub`/`preferred_username`.
- With **both flags on**, the token carries both sets: the migration path. New consumers read `sub` while the old
  ones still read `id`.
- `UserTokenDto` reads `id` and falls back to `sub`, `login` and falls back to `preferred_username`, so a token
  issued in any mode is read.
- The constants live in `TooarkClaimNames`; `RoleClaimsExtension.GetClaims` has overloads taking the flags or the
  `JwtOptions`. The parameterless overload of the 4.x line, which hard-coded the legacy names, was **removed in
  v5**: use `GetClaims(options)`.

> ⚠️ **Migrating from 4.x:** a token issued by a v5 service without explicit flags has `sub` and no `id`. Turn
> `EmitLegacyClaims` on while old readers exist (the token then carries both sets), move the readers to
> `sub`/`preferred_username` (or to `UserTokenDto`, which handles both) and turn it off afterwards. Tokens issued
> by a 4.x service are still read by `UserTokenDto` and by `AddTooarkJwtBearer`.

### Testing emission and expiration

`JwtTokenService` has a constructor that takes a `TimeProvider`: `iat`, `nbf` and `exp` come from it, and the
lifetime validation uses it too. A fake clock tests the expiration without waiting:

```csharp
var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
var service = new JwtTokenService(Options.Create(jwtOptions), clock);

var token = service.Create(new JwtTokenDto(userId, login, securityStamp));

clock.Advance(TimeSpan.FromMinutes(jwtOptions.ExpirationTime + 1));
var result = service.Validate(token); // result.ErrorToken == "Token.Expired"
```

When the container registers a `TimeProvider`, dependency injection picks that constructor; otherwise the
service uses `TimeProvider.System`, with the handler's own lifetime validation.

---

## 🔒 Cryptography - Supported Algorithms

| Algorithm   | Mode        | Description                                |
| ----------- | ----------- | ------------------------------------------ |
| `GCM`       | AES-256-GCM | **Recommended** - Authenticated encryption |
| `CBC`       | AES-256-CBC | Traditional mode with random IV            |
| `CBCUnsafe` | AES-256-CBC | ⚠️ Legacy - zero IV, **decryption only**   |

> `CBCUnsafe` exists only to read old data encrypted with a zero IV: encrypting in this mode throws
> (`Options.Cryptography.AlgorithmDecryptOnly`). For new data, use `GCM`. A value outside this table fails at
> startup (`Options.Cryptography.AlgorithmNotSupported`).

---

## 🔁 Cryptography - Multiple Keys and Rotation

With a single key, rotating it means decrypting everything with the old key and encrypting with the new one, by
hand and all at once. `IKeyedCryptographyService` keeps several identified keys: new values go out with the
current key, old values keep opening with the key that wrote them, and each record is re-encrypted when it is
convenient — on read, in a batch job, or never.

### Configuring the keys

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "CurrentKeyId": "k2",
    "Keys": {
      "k1": "<Base64 of the old 32-byte key>",
      "k2": "<Base64 of the new 32-byte key>"
    },
    "SecretBase64": "<Base64 of the single key used before, if there are values written with it>"
  }
}
```

When `Keys` is filled, `AddTooarkCryptography` (and `AddTooarkSecurities`) registers `KeyedCryptographyService` as
**both** `ICryptographyService` and `IKeyedCryptographyService`: the code that already injects
`ICryptographyService` starts encrypting with the current key and reading legacy values with no change. Without
`Keys`, the single-key `CryptographyService` is registered as always. The identified keys are validated at
startup (see the [`CryptographyOptions` properties](#cryptographyoptions-properties)).

### Value format

```text
tk1.k2.AbCd...                       identified value: format version . key id . Base64 payload
AbCd...                              legacy value, written before the keys were adopted
```

The dot does not exist in the Base64 alphabet, so a legacy value is never mistaken for an identified one. The
payload is exactly what `CryptographyService` produces with that key and the configured algorithm.

### Rotation flow

1. Generate the new key (`openssl rand -base64 32`), add it to `Keys` under a new identifier and point
   `CurrentKeyId` to it. Keep the old key in `Keys`.
2. Deploy. New values go out with the new key; old values keep opening.
3. Re-encrypt the stored values at your own pace — on read, as below, or in a batch job.
4. When no value uses the old key anymore (`KeyIdOf` returns the old identifier, or `null` for legacy values),
   remove it from `Keys` — and `SecretBase64` once no legacy value is left.

```csharp
public class CustomerRepository(IKeyedCryptographyService crypto, AppDbContext db)
{
    public async Task<string> ReadDocumentAsync(Customer customer)
    {
        var document = crypto.Decrypt(customer.EncryptedDocument);

        // Re-encrypt on read: the record moves to the current key the first time it is used
        if (!crypto.IsCurrent(customer.EncryptedDocument))
        {
            customer.EncryptedDocument = crypto.ReEncrypt(customer.EncryptedDocument);
            await db.SaveChangesAsync();
        }

        return document;
    }
}
```

### Limitations

- **Removing a key makes its values unreadable**: `Decrypt` fails with `Cryptography.KeyNotFound;{id}`. Only
  remove a key after re-encrypting everything written with it.
- **Legacy values need the legacy key**: without `SecretBase64`, a value without prefix fails with
  `Cryptography.LegacyKeyNotConfigured`.
- **One algorithm for all keys**: `Algorithm` applies to every key, including the legacy one. Changing the
  algorithm is a re-encryption, not a rotation.
- `ReEncrypt` returns the same text when the value is already on the current key.
- The value grows by the prefix (`tk1.` plus the identifier and a dot): size the database column accordingly.

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

For the application database with Entity Framework Core, the official package
`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` does the same through the `DbContext`: the context
implements `IDataProtectionKeyContext` (a `DbSet<DataProtectionKey>`) and the builder persists the keys to it.
`Tooark.Securities` adds no Entity Framework Core dependency for that — only the application that chooses this
storage installs the package:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}

builder.Services.AddTooarkDataProtection(builder.Configuration, options =>
{
    options.ConfigureDataProtection = dataProtection => dataProtection
        .PersistKeysToDbContext<AppDbContext>();
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

With no value set, the default depends on where the application runs:

| `RequirePersistentKeyStorage` | Outside a container | In a container |
| ----------------------------- | ------------------- | -------------- |
| not set                       | Lock off            | Lock on        |
| `true`                        | Lock on             | Lock on        |
| `false`                       | Lock off            | Lock off       |

The application is treated as running in a container when one of these signals is in the process environment:

- `DOTNET_RUNNING_IN_CONTAINER` set to `true` or `1`, which the official .NET images (`mcr.microsoft.com/dotnet/*`)
  already define;
- `KUBERNETES_SERVICE_HOST` with any value, which Kubernetes injects into every pod, including those of custom
  images that do not define the first one.

A custom image outside Kubernetes, with neither of them, is not detected: set
`DOTNET_RUNNING_IN_CONTAINER=true` in the image or set `"RequirePersistentKeyStorage": true`.

A value that was set always wins over the default, in the section or in code, and across several calls to
`AddTooarkDataProtection` the last one that set a value wins. An API that uses no cookies (JWT only) and
protects no data with Data Protection does not depend on persistent keys: set `false` to start in a container
without a storage.

```json
{
  "DataProtection": {
    "ApplicationName": "my-api",
    "RequirePersistentKeyStorage": false
  }
}
```

The lock only exists for applications that register Data Protection through Tooark: `AddTooarkDataProtection`,
or `AddTooarkSecurities` with the `DataProtection` section present. `AddTooarkService`, from the `Tooark`
aggregator, calls `AddTooarkSecurities` when the `Jwt` or `Cryptography` section exists, and then the same rule
applies. An application that goes through none of them, or uses only the native `services.AddDataProtection()`,
is not affected. The environment is read once, when the registration is called.

**Azure App Service:** with no storage configured, ASP.NET Core writes the keys to
`%HOME%/ASP.NET/DataProtection-Keys`, a network folder that App Service shares across instances. The lock does
not recognize that native fallback, because it only checks the configured storage: in a container on App
Service, point `KeysPath` to the persistent folder (`/home`, with App Service storage enabled) or set `false`
to stay on the fallback.

> ⚠️ **Changed in v5:** on the 4.x line the lock was off in every environment. An application that registers
> Data Protection through Tooark in a container, without a key storage, no longer starts after the upgrade:
> configure the storage or set `false`.

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
> own key in `~/.aspnet/DataProtection-Keys`, inside itself. With the lock off (`false`, or a container that is
> not detected), nothing fails on registration or at startup: the log only warns `Storing keys in a directory '…' that may not be persisted outside of the container. Protected data
will be unavailable when container is destroyed.`

| When                                               | What happens                                                                                                                                                                                                                                                                                    |
| -------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| The request reaches another instance               | The other instance does not have the key: `The key {…} was not found in the key ring`. The session cookie is discarded and the user goes back to sign-in, OpenID Connect sign-in fails with `Correlation failed` or `Unable to unprotect the message.State`, and antiforgery forms are rejected |
| Deploy, restart, recreated pod or autoscaling down | The key goes away with the container, and every session drops at once                                                                                                                                                                                                                           |
| A protected value was stored in the database       | **Lost for good**: no new instance has the key, and there is no way to recover it                                                                                                                                                                                                               |

In a container, with the lock at its default, this same configuration does not start: the error shows up on
deploy, not later, with users already signed in.

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

## 🧂 Password Hashing

`IPasswordHashService` hashes passwords with **PBKDF2-HMAC-SHA256**, native to .NET (`Rfc2898DeriveBytes.Pbkdf2`),
with a random salt per password and 600.000 iterations by default — the current OWASP recommendation. The
`Password` value object of `Tooark.ValueObjects` validates and masks a password; this service is what goes to the
database.

```csharp
public class AccountService(IPasswordHashService passwords, IUserRepository users)
{
    public async Task RegisterAsync(string login, string password)
    {
        var user = new User(login, passwordHash: passwords.Hash(password));
        await users.AddAsync(user);
    }

    public async Task<bool> LoginAsync(string login, string password)
    {
        var user = await users.FindByLoginAsync(login);

        if (user is null || !passwords.Verify(password, user.PasswordHash))
        {
            return false;
        }

        // Parameters changed since this hash was written: rewrite it while the password is at hand
        if (passwords.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = passwords.Hash(password);
            await users.UpdateAsync(user);
        }

        return true;
    }
}
```

### Hash format

```text
$pbkdf2-sha256$i=600000$<salt in Base64>$<hash in Base64>
```

The PHC string carries the algorithm and the parameters, so the service keeps validating hashes written with
older parameters and `NeedsRehash` points out which ones to rewrite. Salt and hash use unpadded Base64, as the
format prescribes; `Verify` also accepts the padded form.

- `Hash` throws `PasswordHash.PasswordNotProvided` for a null or empty password.
- `Verify` never throws: a null password, a null hash or a hash in another format (another algorithm, a plain
  text) returns `false`.
- `NeedsRehash` returns `true` when the iterations, the salt size or the hash size differ from the current
  options, and also for a hash it cannot read.

### Why PBKDF2 and not Argon2

.NET has no native Argon2. Shipping it here would force a third-party library on every consumer of the package,
against the family's dependency policy. PBKDF2-HMAC-SHA256 with 600.000 iterations is the OWASP-recommended
configuration for the algorithm, and the PHC prefix leaves room for another algorithm to coexist in the same
column in the future: an old PBKDF2 hash keeps validating and is rewritten on the next login.

### Tuning

Each login costs one derivation; so does each attempt of an attacker who stole the hashes. Measure the time on
your production hardware and raise `Iterations` while the login stays within budget (tens of milliseconds are
typical). Lowering below 100.000 is refused at startup (`Options.PasswordHash.IterationsTooLow;100000`).

---

## 🎫 Secure Tokens, TOTP and Recovery Codes

Three static classes cover the tokens an application hands to its users. None of them needs registration or
configuration.

### `SecureToken` — reset links, confirmations and API keys

```csharp
// Issue: the token goes to the user (link, e-mail, API key), only the hash goes to the database
var token = SecureToken.Create();                 // 32 random bytes in Base64Url (43 characters)
var hash = SecureToken.Hash(token);               // SHA-256 in Base64Url, deterministic: index the column
await resets.StoreAsync(userId, hash, expiresAt: DateTime.UtcNow.AddHours(1));

// Redeem: find the record by the hash of what came back, then compare in constant time
var record = await resets.FindByHashAsync(SecureToken.Hash(received));

if (record is null || record.ExpiresAt < DateTime.UtcNow || !SecureToken.Verify(received, record.Hash))
{
    return Results.Unauthorized();
}
```

The hash is SHA-256 without salt or cost factor on purpose: the token has at least 128 random bits, so brute
force is infeasible even with a fast hash, and a deterministic hash lets the record be found by it. Slow,
salted hashing is for passwords, which have little entropy (`IPasswordHashService`). `Create` accepts 16 to 1024
bytes; `Verify` returns `false` for anything missing or malformed, never throws. `Hash` throws
`SecureToken.TokenNotProvided` (`BadRequestException`) for a null, empty or blank token.

> **`SecureToken` or Data Protection?** `SecureToken` is an opaque value: the hash lives in the database, so the
> token is revocable, can be single-use (delete the record on redemption) and lasts as long as the record does.
> The Data Protection protector ([link token](#data-protection---protecting-a-link-token)) carries its own
> encrypted content and needs no table, but only suits short lifetimes and cannot be revoked without separate
> tracking. `GenerateString.Token` from `Tooark.Utils`, which only generated the text, was removed in v5: use
> `SecureToken.Create`.

### `Totp` — second factor with authenticator apps

RFC 6238 over HMAC-SHA1, 6 digits and 30-second steps: the configuration Google Authenticator, Microsoft
Authenticator and similar apps expect.

```csharp
// Enrollment: generate the secret, store it encrypted, show the QR code built from the URI
var secret = Totp.GenerateSecret();                                   // Base32, 20 bytes
user.TotpSecret = crypto.Encrypt(secret);                             // ICryptographyService: whoever reads it generates valid codes
var uri = Totp.BuildUri(secret, issuer: "MyApp", account: user.Email); // otpauth://totp/MyApp:ana%40empresa.com?secret=...

// Confirm the enrollment with the first code the app shows
if (!Totp.Verify(secret, typedCode, lastUsedStep: -1, out var step))
{
    return Results.BadRequest();
}

user.TotpLastUsedStep = step;                                         // the same code is never accepted twice
```

```csharp
// Login: verify against the stored secret and the last accepted step
var secret = crypto.Decrypt(user.TotpSecret);

if (!Totp.Verify(secret, typedCode, user.TotpLastUsedStep, out var step))
{
    return Results.Unauthorized();
}

user.TotpLastUsedStep = step;
```

- **Clock tolerance**: `window` (default 1) accepts the previous and the next step besides the current one — 30
  seconds each way. All the steps are computed and compared in constant time, even after a match.
- **Replay protection**: steps equal to or earlier than `lastUsedStep` are refused. Store the `matchedStep` of
  every accepted code and pass it back on the next verification; pass a negative value before the first one.
- A missing code, with the wrong length or with non-digit characters returns `false`; spaces are ignored. An
  undecodable secret throws `Totp.InvalidSecret` (`InternalServerErrorException`), because a corrupt secret would
  silently lock the user out. It is an internal error, not a request error: the secret comes from the database
  or the service, never from the user.
- `BuildUri` throws `Totp.IssuerNotProvided` or `Totp.AccountNotProvided` (`BadRequestException`) for an empty
  issuer or account. Out-of-range `digits`, `periodSeconds`, `window` and sizes throw
  `ArgumentOutOfRangeException`: they are caller errors, not input data, so they have no translation key.
- `Compute(secret, step, digits)` and `GetTimeStep(at, period)` expose the pieces for tests and for showing a code
  server-side; `TimeProvider` on `Verify` lets a test drive the clock.
- `digits` (6 to 8) and `periodSeconds` are configurable, but not every app honours values other than 6 and 30.

### `RecoveryCodes` — when the authenticator is lost

```csharp
// Enrollment: generate, show once, store only the hashes
var codes = RecoveryCodes.Generate();                                 // 10 codes like "7KQ2M-8XR4N"
user.RecoveryCodeHashes = codes
    .Select(code => SecureToken.Hash(RecoveryCodes.Normalize(code)))
    .ToList();

// Redeem: normalize what the user typed (case, hyphens, spaces), find the hash, delete it
var normalized = RecoveryCodes.Normalize(typedCode);
var match = user.RecoveryCodeHashes.FirstOrDefault(hash => SecureToken.Verify(normalized, hash));

if (match is null)
{
    return Results.Unauthorized();
}

user.RecoveryCodeHashes.Remove(match);                                // each code is worth one use
```

The alphabet has no `0`/`O` nor `1`/`I`, so a printed code is typed without ambiguity. `Generate` returns distinct
codes, 10 of 10 characters by default (1 to 50 codes, 8 to 32 characters).

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
    "SecretBase64": "32-byte-key-in-base64"
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

### AES keys for `SecretBase64` and `Keys`

```bash
# One random 32-byte key in Base64, for SecretBase64 or for each entry of Keys
openssl rand -base64 32
```

```csharp
// Or in .NET
Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
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
| [`Microsoft.AspNetCore.Authentication.JwtBearer`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer) | 10.x     | JWT authentication for ASP.NET Core                               |
| [`Microsoft.AspNetCore.DataProtection`](https://www.nuget.org/packages/Microsoft.AspNetCore.DataProtection)                     | 10.x     | Data Protection, standalone package that runs on the base runtime |

---

## 🎯 Best Practices

### JWT

1. **Use asymmetric algorithms (RS/PS/ES) in production** - Allows validating tokens without exposing the signing key
2. **Configure `ExpirationTime` appropriately** - Short-lived tokens are safer
3. **Use `Issuer` and `Audience`** - Prevents tokens from being misused across applications
4. **Store keys in a Secret Manager** - Never commit keys to source code
5. **Register `AddTooarkJwtBearer` instead of a hand-written `AddJwtBearer`** - Same key, issuer and audience as the issuer, validated at startup
6. **Set `EmitLegacyClaims` and `EmitStandardClaims` explicitly** - Explicit flags keep the tokens stable across upgrades; turn the legacy set on only while readers of `id`/`login` exist
7. **Read claims through `TooarkClaimNames` or `UserTokenDto`** - They follow both naming sets

### Cryptography

1. **Prefer GCM over CBC** - GCM provides built-in authentication
2. **Never use CBCUnsafe for new data** - Decrypt-only mode for compatibility with legacy systems
3. **Use `SecretBase64` with a random 32-byte key** - The key is used directly, without derivation
4. **Generate random keys** - Use `openssl rand -base64 32` or `RandomNumberGenerator.GetBytes(32)`
5. **Adopt `Keys` before the first rotation is urgent** - Values written with identified keys rotate without a big-bang re-encryption
6. **Never remove a key with values still encrypted by it** - Check with `KeyIdOf` or re-encrypt on read first

### Password hashing

1. **Store only the PHC string** - Never the password, never a fast hash of it
2. **Call `NeedsRehash` after a successful `Verify`** - The hash is rewritten while the password is at hand
3. **Raise `Iterations` with the hardware** - Measure the login time and keep it within budget; never go below the minimum
4. **Limit attempts** - A slow hash does not replace rate limiting on the login endpoint

### Tokens and second factor

1. **Store hashes, not tokens** - `SecureToken.Hash` for reset tokens, API keys and recovery codes
2. **Give tokens an expiration** - The hash does not expire on its own; keep an `ExpiresAt` next to it
3. **Encrypt the TOTP secret at rest** - Whoever reads it generates valid codes; use `ICryptographyService`
4. **Persist `matchedStep`** - Without it, a code intercepted within the window is accepted twice
5. **Show recovery codes once** - And invalidate each one on use

### Data Protection

1. **Set `ApplicationName`** - Isolation stays the same on every instance, regardless of the install path
2. **Share the key ring across instances** - A persistent volume in `KeysPath`, the `ReadKeys` and `WriteKey` callbacks or a storage through `ConfigureDataProtection`
3. **Do not turn `RequirePersistentKeyStorage` off in containers without a reason** - It is on there and stops the application from starting with the keys tied to the instance; set `false` only where persistent keys are not needed
4. **In the callbacks, only insert and read everything** - An overwritten, filtered or deleted key makes what it protected unreadable
5. **Protect the keys at rest** - With `KeysPath` or callbacks and no certificate, the keys are written unencrypted, on any operating system
6. **Do not use it for long-lived data** - Encrypted database columns belong to `ICryptographyService`

---

## ⚠️ Error Codes and Solutions

| Service                    | Message                                               | Description                                             | Solution                                                         | Exception             |
| -------------------------- | ----------------------------------------------------- | ------------------------------------------------------- | ---------------------------------------------------------------- | --------------------- |
| `CryptographyService`      | `Options.NotConfigured`                               | `Options` not configured                                | Configure `CryptographyOptions`                                  | `InternalServerError` |
| `CryptographyService`      | `Options.Cryptography.SecretNotConfigured`            | No key configured                                       | Configure `SecretBase64` (or `Keys`) in `CryptographyOptions`    | `InternalServerError` |
| `CryptographyService`      | `Options.Cryptography.SecretBase64Invalid`            | `SecretBase64` is not valid Base64                      | Provide a valid Base64 value in `SecretBase64`                   | `InternalServerError` |
| `CryptographyService`      | `Options.Cryptography.SecretBase64InvalidSize`        | `SecretBase64` is not 32 bytes                          | Use a key of exactly 32 bytes (AES-256)                          | `InternalServerError` |
| `CryptographyService`      | `Options.Cryptography.AlgorithmDecryptOnly`           | `CBCUnsafe` used to encrypt                             | Use `CBCUnsafe` only to decrypt legacy data                      | `InternalServerError` |
| `CryptographyService`      | `Options.Cryptography.AlgorithmNotSupported;{value}`  | `Algorithm` other than `GCM`, `CBC` and `CBCUnsafe`     | Fix the value; missing or empty means `GCM`                      | `InternalServerError` |
| `CryptographyService`      | `Cryptography.PlainTextNotProvided`                   | `PlainText` not provided                                | Provide the plain text to encrypt                                | `BadRequest`          |
| `CryptographyService`      | `Cryptography.CipherTextNotProvided`                  | `CipherText` not provided                               | Provide the encrypted text to decrypt                            | `BadRequest`          |
| `CryptographyService`      | `Cryptography.InvalidCipherText`                      | Invalid `CipherText`                                    | Provide a valid encrypted text to decrypt                        | `BadRequest`          |
| `KeyedCryptographyService` | `Options.Cryptography.Keys.NotConfigured`             | Keyed service without keys                              | Fill `Keys` or use the single-key `SecretBase64`                 | `InternalServerError` |
| `KeyedCryptographyService` | `Options.Cryptography.CurrentKeyId.NotConfigured`     | `Keys` without `CurrentKeyId`                           | Point `CurrentKeyId` to one of the identifiers in `Keys`         | `InternalServerError` |
| `KeyedCryptographyService` | `Options.Cryptography.CurrentKeyId.NotFound;{id}`     | `CurrentKeyId` not among `Keys`                         | Use an identifier that exists in `Keys`                          | `InternalServerError` |
| `KeyedCryptographyService` | `Options.Cryptography.KeyId.Invalid;{id}`             | Identifier with a dot, space or other invalid character | Use only letters, digits, hyphen and underscore                  | `InternalServerError` |
| `KeyedCryptographyService` | `Options.Cryptography.Keys.Invalid;{id}`              | Key that is not 32 bytes in Base64                      | Generate the key with `openssl rand -base64 32`                  | `InternalServerError` |
| `KeyedCryptographyService` | `Cryptography.KeyNotFound;{id}`                       | Value encrypted with a key that is not configured       | Put the key back in `Keys`; only remove keys after re-encrypting | `BadRequest`          |
| `KeyedCryptographyService` | `Cryptography.LegacyKeyNotConfigured`                 | Value without prefix and no `SecretBase64`              | Keep the legacy key until every legacy value is re-encrypted     | `BadRequest`          |
| `PasswordHashService`      | `Options.NotConfigured`                               | `Options` not configured                                | Register with `AddTooarkPasswordHash`                            | `InternalServerError` |
| `PasswordHashService`      | `Options.PasswordHash.IterationsTooLow;100000`        | `Iterations` below the minimum                          | Use at least 100.000 iterations (default 600.000)                | `InternalServerError` |
| `PasswordHashService`      | `Options.PasswordHash.SaltSizeTooSmall;16`            | `SaltSize` below the minimum                            | Use at least 16 bytes                                            | `InternalServerError` |
| `PasswordHashService`      | `Options.PasswordHash.HashSizeTooSmall;32`            | `HashSize` below the minimum                            | Use at least 32 bytes                                            | `InternalServerError` |
| `PasswordHashService`      | `PasswordHash.PasswordNotProvided`                    | Null or empty password in `Hash`                        | Validate the password before hashing                             | `BadRequest`          |
| `SecureToken`              | `SecureToken.TokenNotProvided`                        | Null, empty or blank token in `Hash`                    | Validate the token before hashing                                | `BadRequest`          |
| `Totp`                     | `Totp.InvalidSecret`                                  | Secret that is not a valid Base32 value                 | Check the stored secret; enroll the second factor again          | `InternalServerError` |
| `Totp`                     | `Totp.IssuerNotProvided`                              | Null or empty issuer in `BuildUri`                      | Provide the application name                                     | `BadRequest`          |
| `Totp`                     | `Totp.AccountNotProvided`                             | Null or empty account in `BuildUri`                     | Provide the account identification, such as the e-mail           | `BadRequest`          |
| `AddTooarkDataProtection`  | `Options.DataProtection.KeyLifetimeTooShort;7`        | `KeyLifetimeDays` below the minimum                     | Use at least 7 days, or leave it null for the default 90         | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateAmbiguous`         | `CertificatePath` and `CertificateThumbprint` together  | Provide only one of them                                         | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificatePathNotConfigured` | `CertificatePassword` without `CertificatePath`         | Provide `CertificatePath` or remove the password                 | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateNotFound;{value}`  | File or thumbprint not found                            | Check the path, or install the certificate in the `My` store     | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateInvalid`           | Unreadable `.pfx` or wrong password                     | Check the file and `CertificatePassword`                         | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateWithoutPrivateKey` | Certificate without a private key                       | Export the `.pfx` with the private key                           | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateNotRsa`            | Certificate without an RSA key                          | Use an RSA certificate                                           | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.KeyStorageNotConfigured`      | Lock on and no key storage configured                   | Set a shared `KeysPath`, the callbacks or an external storage    | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.KeyCallbacksIncomplete`       | Only one of the `ReadKeys` and `WriteKey` callbacks     | Provide both callbacks                                           | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.KeyStorageAmbiguous`          | `KeysPath` together with `ReadKeys` and `WriteKey`      | Provide a single key storage                                     | `InternalServerError` |
| `JwtTokenService`          | `Options.NotConfigured`                               | `Options` not configured                                | Configure `JwtOptions`                                           | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.SecretNotConfigured`                     | `Secret` not configured                                 | Configure `Secret` in `JwtOptions` for a symmetric token         | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.SecretTooShort`                          | `Secret` below the algorithm minimum                    | Use at least 32/48/64 bytes for HS256/HS384/HS512                | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.KeysNotConfigured`                       | `Private` and `Public` not configured                   | Configure the keys in `JwtOptions` for an asymmetric token       | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.PrivateKey.InvalidSize`                  | Invalid `Private` key size                              | Use a `Private` key of at least 2048 bits                        | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.PublicKey.InvalidSize`                   | Invalid `Public` key size                               | Use a `Public` key of at least 2048 bits                         | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.PrivateKey.InvalidCurve`                 | Invalid `Private` key curve                             | Use a `Private` key with the correct curve                       | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.PublicKey.InvalidCurve`                  | Invalid `Public` key curve                              | Use a `Public` key with the correct curve                        | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.InvalidKey`                              | Invalid key                                             | [Use valid keys](#-generating-keys) for the chosen algorithm     | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.AlgorithmNotSupported`                   | Unsupported algorithm                                   | [Use a supported algorithm](#-jwt---supported-algorithms)        | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.KeyNotConfigured;PrivateKey`             | `Private` key not configured                            | Configure `PrivateKey` in `JwtOptions` to generate a token       | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.KeyNotConfigured;PublicKey`              | `Public` key not configured                             | Configure `PublicKey` in `JwtOptions` to validate a token        | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.ClaimsNotConfigured`                     | `EmitLegacyClaims` and `EmitStandardClaims` both off    | Turn on at least one of the two claim sets                       | `InternalServerError` |
| `AddTooarkJwtBearer`       | `Options.Jwt.KeyNotConfigured;PublicKey`              | No validation key for the `Bearer` scheme               | Configure `Secret` (HMAC) or `PublicKey` in the `Jwt` section    | `InternalServerError` |
| `JwtTokenService`          | `Token.Expired`                                       | Expired token                                           | Generate a new token                                             | N/A                   |
| `JwtTokenService`          | `Token.InvalidSignature`                              | Token with an invalid signature                         | Use only tokens with a valid signature                           | N/A                   |
| `JwtTokenService`          | `Token.Invalid`                                       | Invalid token                                           | Use only valid tokens                                            | N/A                   |
| `JwtTokenService`          | `InternalServerError`                                 | Internal server error                                   | Check the logs for details                                       | N/A                   |

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
