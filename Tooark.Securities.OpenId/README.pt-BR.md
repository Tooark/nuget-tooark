# Tooark.Securities.OpenId

Biblioteca que padroniza como os projetos Tooark configuram **autenticação OpenID Connect** no ASP.NET Core, com
presets de **SSO para Microsoft Entra ID e Google** e um caminho genérico para qualquer provedor OIDC (Keycloak,
Auth0, Okta...).

O pacote **configura os handlers nativos** do ASP.NET Core — `OpenIdConnect`, `JwtBearer` e cookie — e não
implementa nenhum fluxo do protocolo. Não há SDK de provedor, servidor de autorização, emissão de códigos nem
verificação PKCE própria: esse território é do OpenIddict, Duende e Keycloak. O valor está no **padrão de
configuração**: fluxo `code` com PKCE, validação de token e mapeamento de claims iguais em todos os projetos.

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities.OpenId/README.md) · 🇧🇷 **Português (este arquivo)**

## Conteúdo

- [Visão Geral](#visão-geral)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Presets](#-presets)
- [Provedor genérico e presets próprios](#-provedor-genérico-e-presets-próprios)
- [Componentes](#-componentes)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Boas Práticas](#-boas-práticas)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuição](#-contribuição)
- [Licença](#-licença)

## Visão Geral

Cada provedor é lido da subseção **`OpenId:{nome}`** do `appsettings.json`, e o **nome vira o esquema de
autenticação** registrado no ASP.NET Core. Há dois handlers por provedor:

| Método                      | Handler         | Uso                                                   |
| --------------------------- | --------------- | ----------------------------------------------------- |
| `AddTooark{Provedor}Sso`    | `OpenIdConnect` | Login interativo pelo navegador, com sessão em cookie |
| `AddTooark{Provedor}Bearer` | `JwtBearer`     | API que valida o token emitido pelo provedor          |

Os padrões Tooark aplicados aos handlers:

- **Fluxo `code` com PKCE** — o handler do ASP.NET Core nasce em `id_token` (fluxo implícito, abandonado pelo
  OAuth 2.1); o pacote fixa `authorization code` e liga o PKCE.
- **Claims com os nomes do OpenID Connect** — `MapInboundClaims = false`: `sub`, `email` e `name` chegam como
  estão, sem virar `ClaimTypes.NameIdentifier` e afins.
- **Validação de emissor, destinatário e expiração** sempre ligada, com tolerância de relógio configurável.
- **HTTPS obrigatório** no documento de descoberta, salvo liberação explícita para desenvolvimento.
- **Falha no startup**: `ClientId`, `Authority`/tenant e segredo ausentes, endereços inválidos e tenant
  multi-tenant sem emissores são reportados ao registrar, não no primeiro login.

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Securities.OpenId
```

---

## ⚙️ Configuração

### appsettings.json

Todos os provedores ficam sob `OpenId`, um por subseção. Os presets têm nome padrão (`Entra`, `Google`); o
provedor genérico usa o nome que você passar ao registrar.

```json
{
  "OpenId": {
    "Entra": {
      "TenantId": "11111111-2222-3333-4444-555555555555",
      "ClientId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      "ClientSecret": "segredo-do-registro-no-entra"
    },
    "Google": {
      "ClientId": "1234567890-abc.apps.googleusercontent.com",
      "ClientSecret": "segredo-do-console-do-google",
      "HostedDomain": "tooark.com"
    },
    "Keycloak": {
      "Authority": "https://sso.example.com/realms/tooark",
      "ClientId": "app-web",
      "ClientSecret": "segredo-do-client"
    }
  }
}
```

> Segredos vão para o Secret Manager, variáveis de ambiente ou cofre — nunca para o repositório. A chave de
> ambiente equivalente é `OpenId__Entra__ClientSecret`.

### Program.cs

```csharp
using Tooark.Securities.OpenId.Injections;

var builder = WebApplication.CreateBuilder(args);

// Login com Entra ID (aplicação web)
builder.Services.AddTooarkEntraSso(builder.Configuration);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

Cada método aceita o nome do provedor e uma ação de configuração programática, aplicada por cima do
`appsettings.json`:

```csharp
builder.Services.AddTooarkEntraSso(builder.Configuration, "EntraCorp", options =>
{
  options.Scopes = ["openid", "profile", "email", "User.Read"];
  options.Prompt = "select_account";
});
```

### Propriedades de `OpenIdOptions`

Valem para os presets e para o provedor genérico. As colunas **SSO** e **Bearer** indicam em qual handler a
propriedade tem efeito.

| Propriedade                     | Tipo      | Padrão                   | SSO | Bearer | Descrição                                                                              |
| ------------------------------- | --------- | ------------------------ | :-: | :----: | -------------------------------------------------------------------------------------- |
| `Authority`                     | string?   | `null`                   |  ✔  |   ✔    | Endereço do emissor; a descoberta é lida em `/.well-known/openid-configuration`        |
| `MetadataAddress`               | string?   | `null`                   |  ✔  |   ✔    | Endereço explícito do documento de descoberta, quando fora do caminho padrão           |
| `ClientId`                      | string?   | `null`                   |  ✔  |   ✔    | Identificador da aplicação no provedor. **Obrigatório**                                |
| `ClientSecret`                  | string?   | `null`                   |  ✔  |        | Segredo da aplicação. Obrigatório nos presets; opcional no genérico (PKCE)             |
| `Scopes`                        | string[]? | `openid profile email`   |  ✔  |        | Escopos solicitados. `openid` é sempre incluído                                        |
| `UsePkce`                       | bool      | `true`                   |  ✔  |        | PKCE na troca do código. Desligue só para provedores sem suporte                       |
| `CallbackPath`                  | string    | `/signin-oidc`           |  ✔  |        | Redirect URI registrada no provedor: `https://{host}/signin-oidc`                      |
| `SignedOutCallbackPath`         | string    | `/signout-callback-oidc` |  ✔  |        | Retorno após o logout no provedor                                                      |
| `SignInScheme`                  | string?   | `null`                   |  ✔  |        | Esquema da sessão. Nulo registra o cookie padrão (`Cookies`) uma única vez             |
| `SaveTokens`                    | bool      | `false`                  |  ✔  |   ✔    | Guarda os tokens na sessão (cookie maior). Ligue só se chamar outras APIs              |
| `GetClaimsFromUserInfoEndpoint` | bool      | `false`                  |  ✔  |        | Complementa as claims consultando o endpoint de userinfo                               |
| `RequireHttpsMetadata`          | bool      | `true`                   |  ✔  |   ✔    | Rejeita `http://` na Authority/MetadataAddress. Desligue só em desenvolvimento         |
| `MapInboundClaims`              | bool      | `false`                  |  ✔  |   ✔    | Renomeia claims para os tipos legados `ClaimTypes.*`. Ligue só para código legado      |
| `NameClaimType`                 | string    | `name`                   |  ✔  |   ✔    | Claim de `User.Identity.Name`. Presets: `preferred_username` (Entra), `email` (Google) |
| `RoleClaimType`                 | string    | `roles`                  |  ✔  |   ✔    | Claim de `User.IsInRole`                                                               |
| `ValidIssuers`                  | string[]? | `null`                   |  ✔  |   ✔    | Emissores aceitos, além do publicado na descoberta. Presets derivam os seus            |
| `ValidAudiences`                | string[]? | `null`                   |  ✔  |   ✔    | Destinatários aceitos. Bearer: padrão `ClientId`; Entra soma `api://{ClientId}`        |
| `ClockSkewSeconds`              | int       | `300`                    |  ✔  |   ✔    | Tolerância de relógio na validação de expiração                                        |
| `Prompt`                        | string?   | `null`                   |  ✔  |        | Parâmetro `prompt` da tela de login (`login`, `select_account`...)                     |
| `DisplayName`                   | string?   | nome do esquema          |  ✔  |        | Nome nas listas de provedores externos de login                                        |
| `ConfigureOpenIdConnect`        | Action?   | `null`                   |  ✔  |        | Callback final sobre `OpenIdConnectOptions` — o consumidor fala por último             |
| `ConfigureJwtBearer`            | Action?   | `null`                   |     |   ✔    | Callback final sobre `JwtBearerOptions` — o consumidor fala por último                 |

> Os emissores em `ValidIssuers` **somam-se** ao emissor do documento de descoberta, que o handler sempre
> aceita. Já `ValidAudiences` no handler interativo apenas complementa o `ClientId`, que é o destinatário do
> `id_token` por definição.

### Esquemas padrão

O **primeiro provedor registrado** define os esquemas padrão do ASP.NET Core, e apenas quando a aplicação ainda
não os definiu:

| Método    | `DefaultScheme`                      | `DefaultChallengeScheme` |
| --------- | ------------------------------------ | ------------------------ |
| `*Sso`    | cookie (`SignInScheme` ou `Cookies`) | o provedor               |
| `*Bearer` | o provedor                           | —                        |

Para cenários mistos (site + API na mesma aplicação), registre os dois com nomes diferentes e aponte o esquema
nos endpoints de API com `[Authorize(AuthenticationSchemes = "EntraApi")]`, ou defina os padrões
explicitamente em `AddAuthentication(options => ...)` — o que a aplicação definir é preservado.

---

## 🔑 Presets

### Microsoft Entra ID — `EntraOptions`

| Propriedade | Tipo    | Padrão                               | Descrição                                                           |
| ----------- | ------- | ------------------------------------ | ------------------------------------------------------------------- |
| `TenantId`  | string? | `null`                               | GUID ou domínio do tenant. **Obrigatório**                          |
| `Instance`  | string  | `https://login.microsoftonline.com/` | Troque apenas para nuvens soberanas (`login.microsoftonline.us`...) |

Derivações do preset:

- `Authority` = `{Instance}{TenantId}/v2.0` (uma `Authority` explícita tem prioridade).
- `NameClaimType` = `preferred_username` (UPN/e-mail de login); `RoleClaimType` = `roles` (app roles).
- `ValidIssuers` = emissor v2 do tenant **e** emissor v1 (`https://sts.windows.net/{tenant}/`), na nuvem
  pública. APIs registradas sem `requestedAccessTokenVersion: 2` recebem tokens v1 — e o emissor v1 só confere
  quando `TenantId` é o **GUID**.
- `ValidAudiences` (Bearer) = `{ClientId}` **e** `api://{ClientId}`, o Application ID URI padrão. Um URI
  personalizado precisa ser informado em `ValidAudiences`.
- **Multi-tenant** (`TenantId` = `common`, `organizations` ou `consumers`): o documento de descoberta publica um
  emissor com o marcador `{tenantid}`, que nunca confere; o preset **exige** `ValidIssuers` com os emissores
  dos tenants aceitos e falha no startup sem eles.
- `ClientSecret` é obrigatório no SSO: o Entra não troca o código de uma aplicação web sem o segredo
  (`AADSTS7000218`).

Fora do escopo: Graph API, cache de token distribuído e fluxo on-behalf-of exigem o `Microsoft.Identity.Web`.
O critério para pacote próprio é "exige SDK do provedor" — nesse caso, um satélite `Tooark.Securities.Entra`.

### Google — `GoogleOptions`

| Propriedade    | Tipo    | Padrão | Descrição                                                                     |
| -------------- | ------- | ------ | ----------------------------------------------------------------------------- |
| `HostedDomain` | string? | `null` | Domínio do Google Workspace ao qual o login fica restrito (ex.: `tooark.com`) |

Derivações do preset:

- `Authority` = `https://accounts.google.com`.
- `NameClaimType` = `email`.
- `ValidIssuers` = `https://accounts.google.com` **e** `accounts.google.com` — o Google usa as duas formas.
- `HostedDomain`: enviado como parâmetro `hd` na tela de login **e conferido na claim `hd` do token**. A
  sugestão na tela não restringe nada por si só; contas de outro domínio e contas pessoais (sem a claim) são
  rejeitadas com `OpenId.HostedDomain.Invalid`. A conferência é encadeada **depois** do callback do consumidor,
  então sobrevive a `ConfigureOpenIdConnect`/`ConfigureJwtBearer` mesmo quando eles substituem `Events`.
- `ClientSecret` é obrigatório no SSO.
- **Bearer**: valida o `id_token` do Google (JWT com `aud` = `ClientId`). O access token do Google é opaco e
  não passa por este handler.

---

## 🧩 Provedor genérico e presets próprios

Para qualquer provedor OpenID Connect, informe a `Authority` e registre pelo nome:

```csharp
builder.Services.AddTooarkOpenIdSso(builder.Configuration, "Keycloak");
builder.Services.AddTooarkOpenIdBearer(builder.Configuration, "Keycloak");
```

Para um provedor com convenções próprias, herde de `OpenIdOptions` e sobrescreva o que precisar — é exatamente
como `EntraOptions` e `GoogleOptions` são construídos:

```csharp
public class KeycloakOptions : OpenIdOptions
{
  public string? BaseUrl { get; set; }
  public string? Realm { get; set; }

  // Deriva a Authority quando não informada
  public override string? Authority
  {
    get => base.Authority ?? (Realm is null ? null : $"{BaseUrl}/realms/{Realm}");
    set => base.Authority = value;
  }

  // Validações próprias, além das comuns
  public override void Validate(bool interactive)
  {
    if (string.IsNullOrWhiteSpace(Realm))
    {
      throw new InternalServerErrorException("Options.OpenId.Keycloak.RealmNotConfigured");
    }

    base.Validate(interactive);
  }

  // Comportamento extra no handler (o callback do consumidor já rodou dentro do base)
  public override void ConfigureHandler(OpenIdConnectOptions target)
  {
    base.ConfigureHandler(target);
    target.ClaimActions.MapJsonKey("realm_roles", "realm_access.roles");
  }
}

builder.Services.AddTooarkOpenIdSso<KeycloakOptions>(builder.Configuration, "Keycloak");
```

Membros virtuais disponíveis: `Authority`, `ResolveValidIssuers()`, `ResolveValidAudiences()`,
`Validate(bool interactive)`, `ConfigureHandler(OpenIdConnectOptions)` e `ConfigureHandler(JwtBearerOptions)`.

---

## 📦 Componentes

### Extensões de Injeção de Dependência

| Método                              | Handler         | Seção padrão    | Esquema padrão |
| ----------------------------------- | --------------- | --------------- | -------------- |
| `AddTooarkEntraSso()`               | `OpenIdConnect` | `OpenId:Entra`  | `Entra`        |
| `AddTooarkEntraBearer()`            | `JwtBearer`     | `OpenId:Entra`  | `Entra`        |
| `AddTooarkGoogleSso()`              | `OpenIdConnect` | `OpenId:Google` | `Google`       |
| `AddTooarkGoogleBearer()`           | `JwtBearer`     | `OpenId:Google` | `Google`       |
| `AddTooarkOpenIdSso()`              | `OpenIdConnect` | `OpenId:{nome}` | `{nome}`       |
| `AddTooarkOpenIdBearer()`           | `JwtBearer`     | `OpenId:{nome}` | `{nome}`       |
| `AddTooarkOpenIdSso<TOptions>()`    | `OpenIdConnect` | `OpenId:{nome}` | `{nome}`       |
| `AddTooarkOpenIdBearer<TOptions>()` | `JwtBearer`     | `OpenId:{nome}` | `{nome}`       |

Assinatura comum: `(IConfiguration configuration, string name, Action<TOptions>? configure = null)` — nos
presets, `name` tem valor padrão. Todos retornam `IServiceCollection`.

### Options

| Classe          | Descrição                                                                   |
| --------------- | --------------------------------------------------------------------------- |
| `OpenIdOptions` | Opções de um provedor OpenID Connect genérico; base dos presets             |
| `EntraOptions`  | Preset do Microsoft Entra ID: `TenantId`, `Instance` e derivações do tenant |
| `GoogleOptions` | Preset do Google: `HostedDomain` e as convenções do Google                  |

---

## 📝 Exemplos de Uso

### Aplicação web com login corporativo (Entra)

```csharp
builder.Services.AddTooarkEntraSso(builder.Configuration);
builder.Services.AddAuthorization();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/perfil", (ClaimsPrincipal user) => new
{
  Nome = user.Identity?.Name,                 // preferred_username
  Id = user.FindFirst("oid")?.Value,          // id do usuário no tenant
  Papeis = user.FindAll("roles").Select(claim => claim.Value)
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

### Dois provedores na mesma aplicação

Os provedores compartilham o mesmo cookie; o primeiro registrado é o desafio padrão. A tela de login escolhe
o provedor pelo esquema:

```csharp
builder.Services.AddTooarkEntraSso(builder.Configuration);
builder.Services.AddTooarkGoogleSso(builder.Configuration);

app.MapGet("/login/{provedor}", (string provedor) => Results.Challenge(
  new AuthenticationProperties { RedirectUri = "/" },
  [provedor]   // "Entra" ou "Google"
));
```

### API protegida por tokens do Entra

```csharp
builder.Services.AddTooarkEntraBearer(builder.Configuration);
builder.Services.AddAuthorization();

app.MapGet("/api/pedidos", () => Results.Ok()).RequireAuthorization();
```

### Site e API na mesma aplicação (BFF)

```csharp
builder.Services.AddTooarkEntraSso(builder.Configuration);                  // OpenId:Entra    → cookie + desafio
builder.Services.AddTooarkEntraBearer(builder.Configuration, "EntraApi");   // OpenId:EntraApi → API

app.MapGet("/api/pedidos", () => Results.Ok())
  .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "EntraApi" });
```

### Google restrito ao domínio da empresa

```json
{
  "OpenId": {
    "Google": {
      "ClientId": "1234567890-abc.apps.googleusercontent.com",
      "ClientSecret": "segredo",
      "HostedDomain": "tooark.com"
    }
  }
}
```

```csharp
builder.Services.AddTooarkGoogleSso(builder.Configuration);
```

### Cookie personalizado

Registre o cookie antes e informe `SignInScheme`; o pacote deixa de registrar o cookie padrão:

```csharp
builder.Services.AddAuthentication().AddCookie("AppCookie", options =>
{
  options.LoginPath = "/login";
  options.ExpireTimeSpan = TimeSpan.FromHours(8);
  options.SlidingExpiration = true;
});

builder.Services.AddTooarkEntraSso(builder.Configuration, configure: options => options.SignInScheme = "AppCookie");
```

### Ajuste fino do handler

O callback é o último a rodar, então pode sobrescrever qualquer padrão:

```csharp
builder.Services.AddTooarkOpenIdSso(builder.Configuration, "Keycloak", options =>
{
  options.ConfigureOpenIdConnect = oidc =>
  {
    oidc.ResponseMode = OpenIdConnectResponseMode.Query;
    oidc.Events.OnTokenValidated = context =>
    {
      // enriquecer o principal, auditar o login...
      return Task.CompletedTask;
    };
  };
});
```

### Desenvolvimento contra um provedor local

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

## 📋 Dependências

| Pacote                                                                                                                                  | Versão   | Descrição                                 |
| --------------------------------------------------------------------------------------------------------------------------------------- | -------- | ----------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                 | 4.x      | Exceções (`InternalServerErrorException`) |
| [`Microsoft.AspNetCore.Authentication.OpenIdConnect`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.OpenIdConnect) | 8.x/10.x | Handler `OpenIdConnect` do ASP.NET Core   |
| [`Microsoft.AspNetCore.Authentication.JwtBearer`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer)         | 8.x/10.x | Handler `JwtBearer` do ASP.NET Core       |

O pacote declara o framework compartilhado `Microsoft.AspNetCore.App`: ele existe para configurar os handlers
de autenticação do ASP.NET Core e não tem uso fora de uma aplicação ASP.NET Core.

---

## 🎯 Boas Práticas

1. **Um registro por aplicação no provedor** — site e API devem ter `ClientId` próprios; o BFF acima usa dois.
2. **Segredos fora do repositório** — Secret Manager em desenvolvimento, cofre ou variáveis de ambiente em
   produção (`OpenId__Entra__ClientSecret`).
3. **Tenant específico no Entra** — prefira o GUID do tenant a `common`/`organizations`; multi-tenant exige
   listar os emissores aceitos e amplia a superfície de quem consegue autenticar.
4. **Confira o `hd` no Google** — `HostedDomain` faz isso; sem ele, qualquer conta Google entra.
5. **Não ligue `SaveTokens` sem necessidade** — os tokens vão para o cookie; ligue apenas quando a aplicação
   chama outras APIs em nome do usuário.
6. **Mantenha `MapInboundClaims = false`** — código novo consulta `sub`, `email`, `name`; ligue apenas para
   código legado preso aos `ClaimTypes.*`.
7. **`RequireHttpsMetadata = false` só em desenvolvimento** — e nunca no `appsettings.json` versionado; use
   `appsettings.Development.json`.

---

## ⚠️ Códigos de Erro e Soluções

Os erros de configuração são `InternalServerErrorException` lançadas **no registro** (`Program.cs`). A rejeição
de domínio ocorre na validação do token e resulta em falha de autenticação (401), sem exceção.

| Mensagem                                                        | Descrição                                        | Solução                                                              |
| --------------------------------------------------------------- | ------------------------------------------------ | -------------------------------------------------------------------- |
| `Options.OpenId.NameNotConfigured`                              | Nome do provedor vazio                           | Informe o nome (subseção e esquema) ao registrar                     |
| `Options.OpenId.ClientIdNotConfigured`                          | `ClientId` ausente                               | Configure `ClientId` na subseção do provedor                         |
| `Options.OpenId.ClientSecretNotConfigured`                      | `ClientSecret` ausente no SSO de um preset       | Configure `ClientSecret`; Entra e Google não trocam o código sem ele |
| `Options.OpenId.AuthorityNotConfigured`                         | Sem `Authority` nem `MetadataAddress`            | Configure a `Authority` do provedor                                  |
| `Options.OpenId.Authority.Invalid;{valor}`                      | `Authority` não é URL absoluta ou usa HTTP       | Use `https://`; em desenvolvimento, `RequireHttpsMetadata = false`   |
| `Options.OpenId.MetadataAddress.Invalid;{valor}`                | `MetadataAddress` não é URL absoluta ou usa HTTP | Idem                                                                 |
| `Options.OpenId.CallbackPath.Invalid;{valor}`                   | `CallbackPath` não começa com `/`                | Use um caminho relativo à raiz, ex.: `/signin-oidc`                  |
| `Options.OpenId.SignedOutCallbackPath.Invalid;{valor}`          | `SignedOutCallbackPath` não começa com `/`       | Idem                                                                 |
| `Options.OpenId.ClockSkew.Invalid;{valor}`                      | `ClockSkewSeconds` negativo                      | Use zero ou um valor positivo                                        |
| `Options.OpenId.Entra.TenantIdNotConfigured`                    | `TenantId` ausente                               | Configure o GUID ou domínio do tenant                                |
| `Options.OpenId.Entra.Instance.Invalid;{valor}`                 | `Instance` não é URL HTTPS absoluta              | Use a instância da nuvem, ex.: `https://login.microsoftonline.com/`  |
| `Options.OpenId.Entra.MultiTenantRequiresValidIssuers;{tenant}` | Tenant multi-tenant sem `ValidIssuers`           | Liste os emissores dos tenants aceitos, ou use o GUID de um tenant   |
| `OpenId.HostedDomain.Invalid`                                   | Token de conta fora do `HostedDomain`            | Entrar com uma conta do domínio configurado                          |

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório
[Tooark](https://github.com/Tooark/nuget-tooark/issues).

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo
[LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
