# Tooark.Securities

Security library for .NET applications, providing **AES cryptography** and **JWT authentication** services with support for multiple algorithms, plus the configuration of ASP.NET Core **Data Protection**.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.pt-BR.md)

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
| `UserTokenDto` | Result of the token validation                  |

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
    "KeysPath": "/var/dataprotection/keys",
    "CertificatePath": "/var/dataprotection/certificate.pfx",
    "CertificatePassword": "certificate-password"
  }
}
```

#### `KeyRingOptions` properties

| Property                        | Type                              | Default | Description                                                                       |
| ------------------------------- | --------------------------------- | ------- | --------------------------------------------------------------------------------- |
| `ApplicationName`               | string?                           | `null`  | Name that isolates the key ring. Same name and same storage share the keys        |
| `KeysPath`                      | string?                           | `null`  | Key directory. Null keeps the ASP.NET Core default location                       |
| `KeyLifetimeDays`               | int?                              | `null`  | Lifetime of each key in **days** (minimum 7). Null keeps the ASP.NET Core 90 days |
| `DisableAutomaticKeyGeneration` | bool                              | `false` | Only reads the keys, never creates new ones (another app rotates the key ring)    |
| `CertificatePath`               | string?                           | `null`  | RSA `.pfx` certificate, with private key, that protects the keys at rest          |
| `CertificatePassword`           | string?                           | `null`  | Password of the `CertificatePath` certificate                                     |
| `CertificateThumbprint`         | string?                           | `null`  | Certificate from the `My` store (user or machine), instead of `CertificatePath`   |
| `ConfigureDataProtection`       | `Action<IDataProtectionBuilder>?` | `null`  | Callback with the native builder, run last. Code only                             |

> No key is required: whatever is not provided follows the ASP.NET Core default. Validation only stops, at
> startup, what would fail later: a lifetime below 7 days, `CertificatePath` together with
> `CertificateThumbprint`, `CertificatePassword` without `CertificatePath`, and a certificate that is missing,
> unreadable, without a private key or without an RSA key.

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

The options cover what Data Protection offers without an extra package (directory and certificate). For Redis,
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
2. **Share the key ring across instances** - A persistent volume in `KeysPath` or a storage through `ConfigureDataProtection`
3. **Protect the keys at rest** - With `KeysPath` set and no certificate, the keys are written unencrypted, on any operating system
4. **Do not use it for long-lived data** - Encrypted database columns belong to `ICryptographyService`

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

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Securities](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
