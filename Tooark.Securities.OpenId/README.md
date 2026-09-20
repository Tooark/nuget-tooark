# Tooark.Securities.OpenId

Library that standardizes how Tooark projects configure **OpenID Connect authentication** in ASP.NET Core, with
**SSO presets for Microsoft Entra ID and Google** and a generic path for any OIDC provider (Keycloak, Auth0,
Okta...).

The package **configures the native handlers** of ASP.NET Core — `OpenIdConnect`, `JwtBearer` and cookie — and
implements no protocol flow. There is no provider SDK, no authorization server, no code issuance and no PKCE
verification of its own: that territory belongs to OpenIddict, Duende and Keycloak. The value is in the
**configuration standard**: `code` flow with PKCE, token validation and claim mapping that are the same across
every project.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities.OpenId/README.pt-BR.md)

## Contents

- [Overview](#overview)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Presets](#-presets)
- [Generic provider and custom presets](#-generic-provider-and-custom-presets)
- [Components](#-components)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Best Practices](#-best-practices)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [License](#-license)

## Overview

Each provider is read from the **`OpenId:{name}`** subsection of `appsettings.json`, and the **name becomes the
authentication scheme** registered in ASP.NET Core. There are two handlers per provider:

| Method                      | Handler         | Usage                                                   |
| --------------------------- | --------------- | ------------------------------------------------------- |
| `AddTooark{Provider}Sso`    | `OpenIdConnect` | Interactive browser login, with the session in a cookie |
| `AddTooark{Provider}Bearer` | `JwtBearer`     | API that validates the token issued by the provider     |

The Tooark defaults applied to the handlers:

- **`code` flow with PKCE** — the ASP.NET Core handler is born in `id_token` (implicit flow, abandoned by
  OAuth 2.1); the package pins `authorization code` and turns PKCE on.
- **Claims with their OpenID Connect names** — `MapInboundClaims = false`: `sub`, `email` and `name` arrive as
  they are, without becoming `ClaimTypes.NameIdentifier` and the like.
- **Issuer, audience and lifetime validation** always on, with a configurable clock skew.
- **HTTPS required** for the discovery document, unless explicitly relaxed for development.
- **Fail at startup**: missing `ClientId`, `Authority`/tenant and secret, invalid addresses and a multi-tenant
  tenant without issuers are reported on registration, not on the first login.

---

## 🔧 Installation

```bash
dotnet add package Tooark.Securities.OpenId
```

---

## ⚙️ Configuration

### appsettings.json

Every provider lives under `OpenId`, one per subsection. The presets have a default name (`Entra`, `Google`);
the generic provider uses the name you pass on registration.

```json
{
  "OpenId": {
    "Entra": {
      "TenantId": "11111111-2222-3333-4444-555555555555",
      "ClientId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      "ClientSecret": "secret-from-the-entra-registration"
    },
    "Google": {
      "ClientId": "1234567890-abc.apps.googleusercontent.com",
      "ClientSecret": "secret-from-the-google-console",
      "HostedDomain": "tooark.com"
    },
    "Keycloak": {
      "Authority": "https://sso.example.com/realms/tooark",
      "ClientId": "app-web",
      "ClientSecret": "client-secret"
    }
  }
}
```

> Secrets go to the Secret Manager, environment variables or a vault — never to the repository. The
> equivalent environment key is `OpenId__Entra__ClientSecret`.

### Program.cs

```csharp
using Tooark.Securities.OpenId.Injections;

var builder = WebApplication.CreateBuilder(args);

// Login with Entra ID (web application)
builder.Services.AddTooarkEntraSso(builder.Configuration);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

Every method accepts the provider name and a programmatic configuration action, applied on top of
`appsettings.json`:

```csharp
builder.Services.AddTooarkEntraSso(builder.Configuration, "EntraCorp", options =>
{
  options.Scopes = ["openid", "profile", "email", "User.Read"];
  options.Prompt = "select_account";
});
```

### `OpenIdOptions` properties

Apply to the presets and to the generic provider. The **SSO** and **Bearer** columns show in which handler the
property takes effect.

| Property                        | Type      | Default                  | SSO | Bearer | Description                                                                            |
| ------------------------------- | --------- | ------------------------ | :-: | :----: | -------------------------------------------------------------------------------------- |
| `Authority`                     | string?   | `null`                   |  ✔  |   ✔    | Issuer address; discovery is read from `/.well-known/openid-configuration`             |
| `MetadataAddress`               | string?   | `null`                   |  ✔  |   ✔    | Explicit address of the discovery document, when outside the default path              |
| `ClientId`                      | string?   | `null`                   |  ✔  |   ✔    | Application identifier at the provider. **Required**                                   |
| `ClientSecret`                  | string?   | `null`                   |  ✔  |        | Application secret. Required in the presets; optional in the generic one (PKCE)        |
| `Scopes`                        | string[]? | `openid profile email`   |  ✔  |        | Requested scopes. `openid` is always included                                          |
| `UsePkce`                       | bool      | `true`                   |  ✔  |        | PKCE on the code exchange. Turn off only for providers without support                 |
| `CallbackPath`                  | string    | `/signin-oidc`           |  ✔  |        | Redirect URI registered at the provider: `https://{host}/signin-oidc`                  |
| `SignedOutCallbackPath`         | string    | `/signout-callback-oidc` |  ✔  |        | Return after logging out at the provider                                               |
| `SignInScheme`                  | string?   | `null`                   |  ✔  |        | Session scheme. Null registers the default cookie (`Cookies`) once                     |
| `SaveTokens`                    | bool      | `false`                  |  ✔  |   ✔    | Keeps the tokens in the session (bigger cookie). Turn on only to call other APIs       |
| `GetClaimsFromUserInfoEndpoint` | bool      | `false`                  |  ✔  |        | Complements the claims by querying the userinfo endpoint                               |
| `RequireHttpsMetadata`          | bool      | `true`                   |  ✔  |   ✔    | Rejects `http://` in Authority/MetadataAddress. Turn off only in development           |
| `MapInboundClaims`              | bool      | `false`                  |  ✔  |   ✔    | Renames claims to the legacy `ClaimTypes.*` types. Turn on only for legacy code        |
| `NameClaimType`                 | string    | `name`                   |  ✔  |   ✔    | Claim of `User.Identity.Name`. Presets: `preferred_username` (Entra), `email` (Google) |
| `RoleClaimType`                 | string    | `roles`                  |  ✔  |   ✔    | Claim of `User.IsInRole`                                                               |
| `ValidIssuers`                  | string[]? | `null`                   |  ✔  |   ✔    | Accepted issuers, besides the one published in discovery. Presets derive their own     |
| `ValidAudiences`                | string[]? | `null`                   |  ✔  |   ✔    | Accepted audiences. Bearer: default `ClientId`; Entra adds `api://{ClientId}`          |
| `ClockSkewSeconds`              | int       | `300`                    |  ✔  |   ✔    | Clock skew tolerance in lifetime validation                                            |
| `Prompt`                        | string?   | `null`                   |  ✔  |        | `prompt` parameter of the login screen (`login`, `select_account`...)                  |
| `DisplayName`                   | string?   | scheme name              |  ✔  |        | Name in the external login provider lists                                              |
| `ConfigureOpenIdConnect`        | Action?   | `null`                   |  ✔  |        | Final callback over `OpenIdConnectOptions` — the consumer has the last word            |
| `ConfigureJwtBearer`            | Action?   | `null`                   |     |   ✔    | Final callback over `JwtBearerOptions` — the consumer has the last word                |

> The issuers in `ValidIssuers` **add to** the issuer of the discovery document, which the handler always
> accepts. `ValidAudiences` in the interactive handler only complements the `ClientId`, which is the audience of
> the `id_token` by definition.

### Default schemes

The **first registered provider** sets the ASP.NET Core default schemes, and only when the application has not
set them yet:

| Method    | `DefaultScheme`                      | `DefaultChallengeScheme` |
| --------- | ------------------------------------ | ------------------------ |
| `*Sso`    | cookie (`SignInScheme` or `Cookies`) | the provider             |
| `*Bearer` | the provider                         | —                        |

For mixed scenarios (site + API in the same application), register both with different names and point to the
scheme in the API endpoints with `[Authorize(AuthenticationSchemes = "EntraApi")]`, or set the defaults
explicitly in `AddAuthentication(options => ...)` — whatever the application sets is preserved.

---

## 🔑 Presets

### Microsoft Entra ID — `EntraOptions`

| Property   | Type    | Default                              | Description                                                      |
| ---------- | ------- | ------------------------------------ | ---------------------------------------------------------------- |
| `TenantId` | string? | `null`                               | Tenant GUID or domain. **Required**                              |
| `Instance` | string  | `https://login.microsoftonline.com/` | Change only for sovereign clouds (`login.microsoftonline.us`...) |

Preset derivations:

- `Authority` = `{Instance}{TenantId}/v2.0` (an explicit `Authority` takes precedence).
- `NameClaimType` = `preferred_username` (UPN/login email); `RoleClaimType` = `roles` (app roles).
- `ValidIssuers` = the tenant's v2 issuer **and** the v1 issuer (`https://sts.windows.net/{tenant}/`), in the
  public cloud. APIs registered without `requestedAccessTokenVersion: 2` receive v1 tokens — and the v1 issuer
  only matches when `TenantId` is the **GUID**.
- `ValidAudiences` (Bearer) = `{ClientId}` **and** `api://{ClientId}`, the default Application ID URI. A custom
  URI must be provided in `ValidAudiences`.
- **Multi-tenant** (`TenantId` = `common`, `organizations` or `consumers`): the discovery document publishes an
  issuer with the `{tenantid}` placeholder, which never matches; the preset **requires** `ValidIssuers` with the
  issuers of the accepted tenants and fails at startup without them.
- `ClientSecret` is required for SSO: Entra does not exchange the code of a web application without the secret
  (`AADSTS7000218`).

Out of scope: Graph API, distributed token cache and the on-behalf-of flow require `Microsoft.Identity.Web`.
The criterion for a package of its own is "requires the provider SDK" — in that case, a `Tooark.Securities.Entra`
satellite.

### Google — `GoogleOptions`

| Property       | Type    | Default | Description                                                            |
| -------------- | ------- | ------- | ---------------------------------------------------------------------- |
| `HostedDomain` | string? | `null`  | Google Workspace domain the login is restricted to (e.g. `tooark.com`) |

Preset derivations:

- `Authority` = `https://accounts.google.com`.
- `NameClaimType` = `email`.
- `ValidIssuers` = `https://accounts.google.com` **and** `accounts.google.com` — Google uses both forms.
- `HostedDomain`: sent as the `hd` parameter on the login screen **and checked against the token's `hd` claim**.
  The hint on the screen restricts nothing by itself; accounts from another domain and personal accounts
  (without the claim) are rejected with `OpenId.HostedDomain.Invalid`. The check is chained **after** the
  consumer callback, so it survives `ConfigureOpenIdConnect`/`ConfigureJwtBearer` even when they replace `Events`.
- `ClientSecret` is required for SSO.
- **Bearer**: validates Google's `id_token` (JWT with `aud` = `ClientId`). Google's access token is opaque and
  does not go through this handler.

---

## 🧩 Generic provider and custom presets

For any OpenID Connect provider, provide the `Authority` and register by name:

```csharp
builder.Services.AddTooarkOpenIdSso(builder.Configuration, "Keycloak");
builder.Services.AddTooarkOpenIdBearer(builder.Configuration, "Keycloak");
```

For a provider with its own conventions, inherit from `OpenIdOptions` and override what you need — that is
exactly how `EntraOptions` and `GoogleOptions` are built:

```csharp
public class KeycloakOptions : OpenIdOptions
{
  public string? BaseUrl { get; set; }
  public string? Realm { get; set; }

  // Derives the Authority when not provided
  public override string? Authority
  {
    get => base.Authority ?? (Realm is null ? null : $"{BaseUrl}/realms/{Realm}");
    set => base.Authority = value;
  }

  // Own validations, besides the common ones
  public override void Validate(bool interactive)
  {
    if (string.IsNullOrWhiteSpace(Realm))
    {
      throw new InternalServerErrorException("Options.OpenId.Keycloak.RealmNotConfigured");
    }

    base.Validate(interactive);
  }

  // Extra behavior on the handler (the consumer callback already ran inside base)
  public override void ConfigureHandler(OpenIdConnectOptions target)
  {
    base.ConfigureHandler(target);
    target.ClaimActions.MapJsonKey("realm_roles", "realm_access.roles");
  }
}

builder.Services.AddTooarkOpenIdSso<KeycloakOptions>(builder.Configuration, "Keycloak");
```

Available virtual members: `Authority`, `ResolveValidIssuers()`, `ResolveValidAudiences()`,
`Validate(bool interactive)`, `ConfigureHandler(OpenIdConnectOptions)` and `ConfigureHandler(JwtBearerOptions)`.

---

## 📦 Components

### Dependency Injection Extensions

| Method                              | Handler         | Default section | Default scheme |
| ----------------------------------- | --------------- | --------------- | -------------- |
| `AddTooarkEntraSso()`               | `OpenIdConnect` | `OpenId:Entra`  | `Entra`        |
| `AddTooarkEntraBearer()`            | `JwtBearer`     | `OpenId:Entra`  | `Entra`        |
| `AddTooarkGoogleSso()`              | `OpenIdConnect` | `OpenId:Google` | `Google`       |
| `AddTooarkGoogleBearer()`           | `JwtBearer`     | `OpenId:Google` | `Google`       |
| `AddTooarkOpenIdSso()`              | `OpenIdConnect` | `OpenId:{name}` | `{name}`       |
| `AddTooarkOpenIdBearer()`           | `JwtBearer`     | `OpenId:{name}` | `{name}`       |
| `AddTooarkOpenIdSso<TOptions>()`    | `OpenIdConnect` | `OpenId:{name}` | `{name}`       |
| `AddTooarkOpenIdBearer<TOptions>()` | `JwtBearer`     | `OpenId:{name}` | `{name}`       |

Common signature: `(IConfiguration configuration, string name, Action<TOptions>? configure = null)` — in the
presets, `name` has a default value. All of them return `IServiceCollection`.

### Options

| Class           | Description                                                                  |
| --------------- | ---------------------------------------------------------------------------- |
| `OpenIdOptions` | Options of a generic OpenID Connect provider; base of the presets            |
| `EntraOptions`  | Microsoft Entra ID preset: `TenantId`, `Instance` and the tenant derivations |
| `GoogleOptions` | Google preset: `HostedDomain` and Google's conventions                       |

---

## 📝 Usage Examples

### Web application with corporate login (Entra)

```csharp
builder.Services.AddTooarkEntraSso(builder.Configuration);
builder.Services.AddAuthorization();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/profile", (ClaimsPrincipal user) => new
{
  Name = user.Identity?.Name,                 // preferred_username
  Id = user.FindFirst("oid")?.Value,          // user id in the tenant
  Roles = user.FindAll("roles").Select(claim => claim.Value)
}).RequireAuthorization();

app.MapGet("/login", () => Results.Challenge(
  new AuthenticationProperties { RedirectUri = "/" },
  ["Entra"]
));

app.MapGet("/logout", () => Results.SignOut(
  new AuthenticationProperties { RedirectUri = "/" },
  [CookieAuthenticationDefaults.AuthenticationScheme, "Entra"]
));
```

### Two providers in the same application

The providers share the same cookie; the first registered is the default challenge. The login screen picks the
provider by scheme:

```csharp
builder.Services.AddTooarkEntraSso(builder.Configuration);
builder.Services.AddTooarkGoogleSso(builder.Configuration);

app.MapGet("/login/{provider}", (string provider) => Results.Challenge(
  new AuthenticationProperties { RedirectUri = "/" },
  [provider]   // "Entra" or "Google"
));
```

### API protected by Entra tokens

```csharp
builder.Services.AddTooarkEntraBearer(builder.Configuration);
builder.Services.AddAuthorization();

app.MapGet("/api/orders", () => Results.Ok()).RequireAuthorization();
```

### Site and API in the same application (BFF)

```csharp
builder.Services.AddTooarkEntraSso(builder.Configuration);                  // OpenId:Entra    → cookie + challenge
builder.Services.AddTooarkEntraBearer(builder.Configuration, "EntraApi");   // OpenId:EntraApi → API

app.MapGet("/api/orders", () => Results.Ok())
  .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "EntraApi" });
```

### Google restricted to the company domain

```json
{
  "OpenId": {
    "Google": {
      "ClientId": "1234567890-abc.apps.googleusercontent.com",
      "ClientSecret": "secret",
      "HostedDomain": "tooark.com"
    }
  }
}
```

```csharp
builder.Services.AddTooarkGoogleSso(builder.Configuration);
```

### Custom cookie

Register the cookie first and provide `SignInScheme`; the package stops registering the default cookie:

```csharp
builder.Services.AddAuthentication().AddCookie("AppCookie", options =>
{
  options.LoginPath = "/login";
  options.ExpireTimeSpan = TimeSpan.FromHours(8);
  options.SlidingExpiration = true;
});

builder.Services.AddTooarkEntraSso(builder.Configuration, configure: options => options.SignInScheme = "AppCookie");
```

### Fine-tuning the handler

The callback is the last to run, so it can override any default:

```csharp
builder.Services.AddTooarkOpenIdSso(builder.Configuration, "Keycloak", options =>
{
  options.ConfigureOpenIdConnect = oidc =>
  {
    oidc.ResponseMode = OpenIdConnectResponseMode.Query;
    oidc.Events.OnTokenValidated = context =>
    {
      // enrich the principal, audit the login...
      return Task.CompletedTask;
    };
  };
});
```

### Development against a local provider

```json
{
  "OpenId": {
    "Keycloak": {
      "Authority": "http://localhost:8080/realms/dev",
      "ClientId": "app-web",
      "RequireHttpsMetadata": false
    }
  }
}
```

---

## 📋 Dependencies

| Package                                                                                                                                 | Version  | Description                                 |
| --------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                 | 4.x      | Exceptions (`InternalServerErrorException`) |
| [`Microsoft.AspNetCore.Authentication.OpenIdConnect`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.OpenIdConnect) | 8.x/10.x | ASP.NET Core `OpenIdConnect` handler        |
| [`Microsoft.AspNetCore.Authentication.JwtBearer`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer)         | 8.x/10.x | ASP.NET Core `JwtBearer` handler            |

The package declares the `Microsoft.AspNetCore.App` shared framework: it exists to configure the ASP.NET Core
authentication handlers and has no use outside an ASP.NET Core application.

---

## 🎯 Best Practices

1. **One registration per application at the provider** — site and API must have their own `ClientId`; the BFF
   above uses two.
2. **Secrets out of the repository** — Secret Manager in development, a vault or environment variables in
   production (`OpenId__Entra__ClientSecret`).
3. **A specific tenant in Entra** — prefer the tenant GUID over `common`/`organizations`; multi-tenant requires
   listing the accepted issuers and widens the surface of who can authenticate.
4. **Check `hd` in Google** — `HostedDomain` does that; without it, any Google account gets in.
5. **Do not turn on `SaveTokens` without need** — the tokens go into the cookie; turn it on only when the
   application calls other APIs on behalf of the user.
6. **Keep `MapInboundClaims = false`** — new code reads `sub`, `email`, `name`; turn it on only for legacy code
   tied to `ClaimTypes.*`.
7. **`RequireHttpsMetadata = false` only in development** — and never in the versioned `appsettings.json`; use
   `appsettings.Development.json`.

---

## ⚠️ Error Codes and Solutions

Configuration errors are `InternalServerErrorException`s thrown **on registration** (`Program.cs`). The domain
rejection happens during token validation and results in an authentication failure (401), with no exception.

| Message                                                         | Description                                           | Solution                                                                       |
| --------------------------------------------------------------- | ----------------------------------------------------- | ------------------------------------------------------------------------------ |
| `Options.OpenId.NameNotConfigured`                              | Empty provider name                                   | Provide the name (subsection and scheme) on registration                       |
| `Options.OpenId.ClientIdNotConfigured`                          | Missing `ClientId`                                    | Configure `ClientId` in the provider subsection                                |
| `Options.OpenId.ClientSecretNotConfigured`                      | Missing `ClientSecret` in a preset's SSO              | Configure `ClientSecret`; Entra and Google do not exchange the code without it |
| `Options.OpenId.AuthorityNotConfigured`                         | Neither `Authority` nor `MetadataAddress`             | Configure the provider's `Authority`                                           |
| `Options.OpenId.Authority.Invalid;{value}`                      | `Authority` is not an absolute URL or uses HTTP       | Use `https://`; in development, `RequireHttpsMetadata = false`                 |
| `Options.OpenId.MetadataAddress.Invalid;{value}`                | `MetadataAddress` is not an absolute URL or uses HTTP | Same as above                                                                  |
| `Options.OpenId.CallbackPath.Invalid;{value}`                   | `CallbackPath` does not start with `/`                | Use a root-relative path, e.g. `/signin-oidc`                                  |
| `Options.OpenId.SignedOutCallbackPath.Invalid;{value}`          | `SignedOutCallbackPath` does not start with `/`       | Same as above                                                                  |
| `Options.OpenId.ClockSkew.Invalid;{value}`                      | Negative `ClockSkewSeconds`                           | Use zero or a positive value                                                   |
| `Options.OpenId.Entra.TenantIdNotConfigured`                    | Missing `TenantId`                                    | Configure the tenant GUID or domain                                            |
| `Options.OpenId.Entra.Instance.Invalid;{value}`                 | `Instance` is not an absolute HTTPS URL               | Use the cloud instance, e.g. `https://login.microsoftonline.com/`              |
| `Options.OpenId.Entra.MultiTenantRequiresValidIssuers;{tenant}` | Multi-tenant tenant without `ValidIssuers`            | List the issuers of the accepted tenants, or use a tenant GUID                 |
| `OpenId.HostedDomain.Invalid`                                   | Token from an account outside `HostedDomain`          | Sign in with an account of the configured domain                               |

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the
[Tooark](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the
[LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
