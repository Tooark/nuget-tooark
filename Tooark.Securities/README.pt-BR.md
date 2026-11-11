# Tooark.Securities

Biblioteca de segurança para aplicações .NET, fornecendo **criptografia AES** (chave única ou várias chaves com rotação), emissão, validação e **autenticação Bearer** de **JWT** com suporte a múltiplos algoritmos, **hash de senha** (PBKDF2), **tokens seguros**, segundo fator **TOTP** com códigos de recuperação, além da configuração do **Data Protection** do ASP.NET Core.

📖 **Documentação:** [Tooark.Securities no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.securities.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Conteúdo do Pacote](#-conteúdo-do-pacote)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [JWT - Algoritmos Suportados](#-jwt---algoritmos-suportados)
- [JWT - Autenticação Bearer](#-jwt---autenticação-bearer)
- [Criptografia - Algoritmos Suportados](#-criptografia---algoritmos-suportados)
- [Criptografia - Várias Chaves e Rotação](#-criptografia---várias-chaves-e-rotação)
- [Data Protection - Key Ring](#-data-protection---key-ring)
- [Hash de Senha](#-hash-de-senha)
- [Tokens Seguros, TOTP e Códigos de Recuperação](#-tokens-seguros-totp-e-códigos-de-recuperação)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Gerando Chaves](#-gerando-chaves)
- [Dependências](#-dependências)
- [Boas Práticas](#-boas-práticas)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 📦 Conteúdo do Pacote

### Serviços

| Classe                                                                 | Descrição                                                                                   |
| ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| [`JwtTokenService`](#jwt---criação-e-validação-de-token)               | Serviço para criação e validação de tokens JWT                                              |
| [`CryptographyService`](#criptografia---encrypt-e-decrypt)             | Serviço de criptografia/descriptografia AES-256 com chave única                             |
| [`KeyedCryptographyService`](#-criptografia---várias-chaves-e-rotação) | AES-256 com várias chaves identificadas e rotação; atende também ao contrato de chave única |
| [`PasswordHashService`](#-hash-de-senha)                               | Hash de senha com PBKDF2-HMAC-SHA256 no formato PHC                                         |

### Interfaces

| Interface                   | Descrição                                                                    |
| --------------------------- | ---------------------------------------------------------------------------- |
| `IJwtTokenService`          | Contrato para manipulação de tokens JWT                                      |
| `ICryptographyService`      | Contrato para criptografia/descriptografia AES                               |
| `IKeyedCryptographyService` | `ICryptographyService` mais identificação de chave, rotação e recriptografia |
| `IPasswordHashService`      | Contrato para gerar e conferir hashes de senha                               |

| Membro                                           | Descrição                                                                         |
| ------------------------------------------------ | --------------------------------------------------------------------------------- |
| `IJwtTokenService.Create`                        | Cria o token. Síncrono: o handler não expõe criação assíncrona                    |
| `IJwtTokenService.ValidateAsync`                 | Valida o token. Caminho direto ao handler, que é assíncrono                       |
| `IJwtTokenService.Validate`                      | Valida o token. Encapsulamento síncrono de `ValidateAsync`                        |
| `ICryptographyService.Encrypt`                   | Criptografa. Síncrono: AES em memória, sem operação de I/O                        |
| `ICryptographyService.Decrypt`                   | Descriptografa. Síncrono, pelo mesmo motivo                                       |
| `IKeyedCryptographyService.CurrentKeyId`         | Identificador da chave que cifra os valores novos                                 |
| `IKeyedCryptographyService.KeyIds`               | Identificadores de todas as chaves configuradas                                   |
| `IKeyedCryptographyService.Encrypt(text, keyId)` | Cifra com uma chave específica                                                    |
| `IKeyedCryptographyService.KeyIdOf`              | Lê o identificador da chave de um valor; `null` para um valor legado, sem prefixo |
| `IKeyedCryptographyService.IsCurrent`            | Se o valor foi cifrado com a chave corrente                                       |
| `IKeyedCryptographyService.ReEncrypt`            | Decifra com a chave que gravou o valor e cifra com a corrente                     |
| `IPasswordHashService.Hash`                      | Gera o hash de uma senha com salt aleatório                                       |
| `IPasswordHashService.Verify`                    | Confere a senha contra o hash guardado, em tempo constante; nunca lança           |
| `IPasswordHashService.NeedsRehash`               | Se o hash usa parâmetros mais antigos que as opções atuais                        |

### Classes estáticas

| Classe             | Descrição                                                                                                          |
| ------------------ | ------------------------------------------------------------------------------------------------------------------ |
| `SecureToken`      | Tokens aleatórios (redefinição de senha, confirmação de e-mail, chave de API): `Create`, `Hash` e `Verify`         |
| `Totp`             | Senha de uso único por tempo (RFC 6238): segredo, URI `otpauth://`, `Compute` e `Verify` com proteção contra reuso |
| `RecoveryCodes`    | Códigos de recuperação do segundo fator: `Generate` e `Normalize`, guardados como hashes do `SecureToken`          |
| `TooarkClaimNames` | Nomes das claims que o serviço de token emite e lê (`id`, `login`, `security`, `sub`, `preferred_username`)        |

### DTOs

| Classe         | Descrição                                         |
| -------------- | ------------------------------------------------- |
| `JwtTokenDto`  | Dados para criação de token (id, login, security) |
| `UserTokenDto` | Resultado da validação do token, com as claims    |

### Options

| Classe                | Descrição                                                                                    |
| --------------------- | -------------------------------------------------------------------------------------------- |
| `JwtOptions`          | Configurações do JWT (algoritmo, chaves, issuer(s), audience(s), expiração, nomes de claims) |
| `CryptographyOptions` | Configurações de criptografia (algoritmo, secret ou secretBase64, chaves identificadas)      |
| `PasswordHashOptions` | Configurações do hash de senha (iterações, tamanho do sal, tamanho do hash)                  |
| `KeyRingOptions`      | Key ring do Data Protection (aplicação, armazenamento, certificado, rotação)                 |

### Extensions

| Classe                | Descrição                                                                                       |
| --------------------- | ----------------------------------------------------------------------------------------------- |
| `RoleClaimsExtension` | Extensão que converte `JwtTokenDto` em claims, com os nomes legados, os nomes padrão ou os dois |

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Securities
```

---

## ⚙️ Configuração

### appsettings.json

Para configurar os serviços de segurança, adicione as seções `Jwt`, `Cryptography`, `PasswordHash` e `DataProtection` no seu arquivo `appsettings.json`. Só as seções dos serviços usados são necessárias, e a `PasswordHash` é opcional mesmo assim (padrões seguros):

Configuração exemplo para token JWT com algoritmos simétricos (utiliza _Secret_ para assinatura e validação):

```json
{
  "Jwt": {
    "Algorithm": "HS256",
    "Secret": "sua-chave-secreta-com-pelo-menos-32-caracteres",
    "Issuer": "sua-aplicacao",
    "Audience": "seus-clientes",
    "ExpirationTime": 60
  }
}
```

Configuração exemplo para token JWT com algoritmos assimétricos (_PrivateKey_ obrigatória para assinatura, _PublicKey_ obrigatória para validação):

```json
{
  "Jwt": {
    "Algorithm": "RS256",
    "PrivateKey": "sua-chave-privada-em-base64",
    "PublicKey": "sua-chave-publica-em-base64",
    "Issuer": "sua-aplicacao",
    "Audience": "seus-clientes",
    "ExpirationTime": 60
  }
}
```

Configuração exemplo para criptografia AES:

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "Secret": "sua-chave-secreta-com-32-caracteres"
  }
}
```

Ou com chave AES pronta em Base64 (**recomendado** — a chave é usada diretamente, sem derivação):

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "SecretBase64": "chave-de-32-bytes-em-base64"
  }
}
```

Ou com várias chaves identificadas, para rotacionar a chave sem recifrar tudo de uma vez (veja
[Criptografia - Várias Chaves e Rotação](#-criptografia---várias-chaves-e-rotação)). O `SecretBase64` fica só
enquanto existirem valores gravados antes da adoção das chaves:

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "CurrentKeyId": "k2",
    "Keys": {
      "k1": "chave-antiga-de-32-bytes-em-base64",
      "k2": "chave-nova-de-32-bytes-em-base64"
    },
    "SecretBase64": "chave-legada-de-32-bytes-em-base64"
  }
}
```

Configuração exemplo para o hash de senha (todas as chaves são opcionais — estes são os padrões):

```json
{
  "PasswordHash": {
    "Iterations": 600000,
    "SaltSize": 16,
    "HashSize": 32
  }
}
```

#### Propriedades de `JwtOptions`

| Propriedade          | Tipo      | Padrão  | Descrição                                                                  |
| -------------------- | --------- | ------- | -------------------------------------------------------------------------- |
| `Algorithm`          | string    | `ES256` | Algoritmo de assinatura. Valores desconhecidos lançam exceção              |
| `Secret`             | string?   | `null`  | Chave para algoritmos simétricos (HS256/384/512)                           |
| `PrivateKey`         | string?   | `null`  | Chave privada Base64 (PKCS#8) para assinatura                              |
| `PublicKey`          | string?   | `null`  | Chave pública Base64 (SPKI) para validação                                 |
| `Issuer`             | string?   | `null`  | Emissor aceito na validação e gravado no token                             |
| `Issuers`            | string[]? | `null`  | Emissores adicionais aceitos na validação                                  |
| `Audience`           | string?   | `null`  | Destinatário aceito na validação e gravado no token                        |
| `Audiences`          | string[]? | `null`  | Destinatários adicionais aceitos na validação                              |
| `ExpirationTime`     | int       | `5`     | Expiração do token em **minutos**                                          |
| `EmitLegacyClaims`   | bool      | `true`  | Emite as claims `id` e `login` (nomes do Tooark)                           |
| `EmitStandardClaims` | bool      | `false` | Emite as claims `sub` e `preferred_username` (nomes do JWT/OpenID Connect) |

> `Issuer`/`Issuers` e `Audience`/`Audiences` se somam: informar qualquer um deles liga a validação do
> respectivo campo. Quando nenhum é informado, a validação daquele campo é desligada. O parâmetro
> `audience` de `Create` e `Validate`/`ValidateAsync` tem prioridade sobre a configuração.
>
> `PrivateKey` e `PublicKey` aceitam tanto o Base64 puro quanto o PEM completo: os delimitadores
> `-----BEGIN/END ... KEY-----` e as quebras de linha são removidos ao atribuir.
>
> ⚠️ **Muda na v5:** os padrões das claims se invertem — `EmitStandardClaims` passa a `true` e
> `EmitLegacyClaims` a `false`. Defina os dois sinalizadores de forma explícita hoje, para que a troca de versão
> não mude os seus tokens. Ao menos um precisa estar ligado; os dois desligados falham no startup
> (`Options.Jwt.ClaimsNotConfigured`). Veja [Nomes das claims](#nomes-das-claims).

Exemplo com múltiplos emissores e destinatários:

```json
{
  "Jwt": {
    "Algorithm": "ES256",
    "PublicKey": "sua-chave-publica-em-base64",
    "Issuers": ["api-legada", "api-nova"],
    "Audiences": ["app-web", "app-mobile"],
    "ExpirationTime": 60
  }
}
```

#### Propriedades de `CryptographyOptions`

| Propriedade    | Tipo                         | Padrão | Descrição                                                     |
| -------------- | ---------------------------- | ------ | ------------------------------------------------------------- |
| `Algorithm`    | string                       | `GCM`  | Modo AES-256: `GCM`, `CBC` ou `CBCUnsafe` (somente decrypt)   |
| `Secret`       | string?                      | `null` | Chave derivada via SHA256 (compatibilidade)                   |
| `SecretBase64` | string?                      | `null` | Chave AES de 32 bytes usada diretamente (**recomendado**)     |
| `CurrentKeyId` | string?                      | `null` | Identificador da chave em `Keys` que cifra os valores novos   |
| `Keys`         | `Dictionary<string, string>` | vazio  | Chaves AES de 32 bytes em Base64, identificadas, para rotação |

> Sem `Keys`, é obrigatório informar `Secret` **ou** `SecretBase64`. Quando ambos são informados,
> `SecretBase64` tem prioridade. O `SecretBase64` deve representar exatamente 32 bytes (AES-256) — valores
> inválidos falham no startup. Gere uma chave com `Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`.
> Com `Secret`, a chave é derivada via SHA256 (mantido por compatibilidade).
>
> Com `Keys`, o `CurrentKeyId` é obrigatório e precisa ser um dos identificadores; cada identificador usa só
> letras, dígitos, hífen e sublinhado (nunca ponto), e cada chave tem exatamente 32 bytes em Base64 — qualquer
> outra coisa falha no startup. `Secret`/`SecretBase64` viram a **chave legada**: só abrem os valores gravados
> sem prefixo de chave, e podem ser removidos quando não restar nenhum.

#### Propriedades de `PasswordHashOptions`

| Propriedade  | Tipo | Padrão   | Descrição                                                   |
| ------------ | ---- | -------- | ----------------------------------------------------------- |
| `Iterations` | int  | `600000` | Iterações do PBKDF2 (recomendação da OWASP). Mínimo 100.000 |
| `SaltSize`   | int  | `16`     | Tamanho do salt em bytes. Mínimo 16                         |
| `HashSize`   | int  | `32`     | Tamanho do hash derivado em bytes. Mínimo 32                |

> Valores abaixo dos mínimos falham no startup: configuração fraca é erro de configuração, nunca é substituída
> em silêncio. Aumentar um valor não quebra os hashes guardados — eles continuam validando e o
> [`NeedsRehash`](#-hash-de-senha) avisa o fluxo de login para regravá-los.

Configuração exemplo para o Data Protection (todas as chaves são opcionais — veja
[Data Protection - Key Ring](#-data-protection---key-ring)):

```json
{
  "DataProtection": {
    "ApplicationName": "minha-aplicacao",
    "RequirePersistentKeyStorage": true,
    "KeysPath": "/var/dataprotection/keys",
    "CertificatePath": "/var/dataprotection/certificado.pfx",
    "CertificatePassword": "senha-do-certificado"
  }
}
```

#### Propriedades de `KeyRingOptions`

| Propriedade                     | Tipo                                                 | Padrão  | Descrição                                                                              |
| ------------------------------- | ---------------------------------------------------- | ------- | -------------------------------------------------------------------------------------- |
| `ApplicationName`               | string?                                              | `null`  | Nome que isola o key ring. Mesmo nome e mesmo armazenamento compartilham as chaves     |
| `KeysPath`                      | string?                                              | `null`  | Diretório das chaves. Nulo mantém o local padrão do ASP.NET Core                       |
| `KeyLifetimeDays`               | int?                                                 | `null`  | Vida útil de cada chave em **dias** (mínimo 7). Nulo mantém os 90 dias do ASP.NET Core |
| `DisableAutomaticKeyGeneration` | bool                                                 | `false` | Só lê as chaves, sem gerar novas (outra aplicação gira o key ring)                     |
| `RequirePersistentKeyStorage`   | bool?                                                | `null`  | Com `true`, a aplicação não sobe sem armazenamento de chaves. Nulo mantém desligada    |
| `CertificatePath`               | string?                                              | `null`  | Certificado RSA `.pfx`, com chave privada, que protege as chaves em repouso            |
| `CertificatePassword`           | string?                                              | `null`  | Senha do certificado de `CertificatePath`                                              |
| `CertificateThumbprint`         | string?                                              | `null`  | Certificado do repositório `My` (usuário ou máquina), no lugar de `CertificatePath`    |
| `ReadKeys`                      | `Func<IServiceProvider, Task<IEnumerable<string>>>?` | `null`  | Lê o XML de todas as chaves do armazenamento próprio. Só em código                     |
| `WriteKey`                      | `Func<IServiceProvider, string, string, Task>?`      | `null`  | Grava uma chave nova (nome e XML) no armazenamento próprio. Só em código               |
| `ConfigureDataProtection`       | `Action<IDataProtectionBuilder>?`                    | `null`  | Callback com o builder nativo, executado por último. Só em código                      |

> Nenhuma chave é obrigatória: o que não é informado segue o padrão do ASP.NET Core. A validação só barra, no
> startup, o que falharia mais tarde: vida útil abaixo de 7 dias, `CertificatePath` junto com
> `CertificateThumbprint`, `CertificatePassword` sem `CertificatePath`, certificado ausente, ilegível, sem
> chave privada ou sem chave RSA, só um dos callbacks `ReadKeys` e `WriteKey`, e os callbacks junto com
> `KeysPath`. A trava do armazenamento de chaves (`RequirePersistentKeyStorage`) é opcional:
> veja [Clusters, contêineres e autoscale](#clusters-contêineres-e-autoscale).

### Program.cs

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços de segurança
builder.Services.AddTooarkSecurities(builder.Configuration);

var app = builder.Build();
```

Ou, para adicionar apenas o serviço de token JWT:

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços de segurança
builder.Services.AddTooarkJwtToken(builder.Configuration);

var app = builder.Build();
```

Ou, para adicionar apenas o serviço de criptografia:

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços de segurança
builder.Services.AddTooarkCryptography(builder.Configuration);

var app = builder.Build();
```

Ou, para adicionar apenas o Data Protection:

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adiciona o Data Protection com o key ring da seção DataProtection
builder.Services.AddTooarkDataProtection(builder.Configuration);

var app = builder.Build();
```

Ou, para adicionar apenas o serviço de hash de senha (`AddTooarkSecurities` já o registra, com ou sem a seção
`PasswordHash`):

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adiciona o serviço de hash de senha; a configuração e o callback são opcionais
builder.Services.AddTooarkPasswordHash(builder.Configuration, options => options.Iterations = 800_000);

var app = builder.Build();
```

Para autenticar as requisições de uma API com os tokens que o próprio serviço emite, registre o esquema `Bearer`
a partir da mesma seção `Jwt` (veja [JWT - Autenticação Bearer](#-jwt---autenticação-bearer)):

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTooarkJwtToken(builder.Configuration);   // emite os tokens
builder.Services.AddTooarkJwtBearer(builder.Configuration);  // valida-os em cada requisição
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

> `SecureToken`, `Totp` e `RecoveryCodes` são classes estáticas: nada a registrar.

---

## 🔐 JWT - Algoritmos Suportados

### Algoritmos Simétricos (HMAC)

| Algoritmo | Descrição   | Requisitos         |
| --------- | ----------- | ------------------ |
| `HS256`   | HMAC-SHA256 | Secret (≥32 bytes) |
| `HS384`   | HMAC-SHA384 | Secret (≥48 bytes) |
| `HS512`   | HMAC-SHA512 | Secret (≥64 bytes) |

> Os mínimos seguem a RFC 7518 (chave HMAC com ao menos o tamanho da saída do hash) e são validados no
> startup (`Options.Jwt.SecretTooShort`). O tamanho é medido em **bytes** (UTF-8): caracteres não-ASCII
> ocupam mais de um byte.

### Algoritmos Assimétricos (RSA)

| Algoritmo | Descrição      | Requisitos                        |
| --------- | -------------- | --------------------------------- |
| `RS256`   | RSA-SHA256     | PrivateKey/PublicKey (≥2048 bits) |
| `RS384`   | RSA-SHA384     | PrivateKey/PublicKey (≥2048 bits) |
| `RS512`   | RSA-SHA512     | PrivateKey/PublicKey (≥2048 bits) |
| `PS256`   | RSA-PSS-SHA256 | PrivateKey/PublicKey (≥2048 bits) |
| `PS384`   | RSA-PSS-SHA384 | PrivateKey/PublicKey (≥2048 bits) |
| `PS512`   | RSA-PSS-SHA512 | PrivateKey/PublicKey (≥2048 bits) |

### Algoritmos Assimétricos (ECDsa)

| Algoritmo | Descrição    | Curva Requerida   |
| --------- | ------------ | ----------------- |
| `ES256`   | ECDSA-SHA256 | P-256 (secp256r1) |
| `ES384`   | ECDSA-SHA384 | P-384 (secp384r1) |
| `ES512`   | ECDSA-SHA512 | P-521 (secp521r1) |

> O algoritmo padrão é `ES256` quando `Algorithm` não é informado. Valores desconhecidos lançam exceção
> (`Options.Jwt.AlgorithmNotSupported`) — em configuração de segurança, um algoritmo inválido nunca é
> substituído silenciosamente por outro. Internamente, criação e validação usam o `JsonWebTokenHandler`
> (handler atual da Microsoft.IdentityModel); o `UserTokenDto` aceita tanto `JsonWebToken` quanto o
> legado `JwtSecurityToken`.

---

## 🪪 JWT - Autenticação Bearer

O `AddTooarkJwtBearer` registra o esquema de autenticação `Bearer` do ASP.NET Core para os tokens que o
`JwtTokenService` emite. Ele lê a mesma seção `Jwt` e monta os `TokenValidationParameters` do handler com as
mesmas regras do `Validate`: mesmo algoritmo e chave (segredo HMAC ou chave pública), emissor(es),
destinatário(s), expiração e assinatura obrigatórias, tolerância de relógio zero. Um token aceito por um é
aceito pelo outro, e ninguém repete chave, emissor e destinatário em um `AddJwtBearer` escrito à mão.

```csharp
builder.Services.AddTooarkJwtToken(builder.Configuration);
builder.Services.AddTooarkJwtBearer(builder.Configuration, bearer =>
{
    // Opcional: as opções nativas, aplicadas por último
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

O que o registro define:

- **Esquema `Bearer`** (`JwtBearerDefaults.AuthenticationScheme`) como esquema padrão de autenticação, a menos
  que a aplicação já tenha definido um — o primeiro esquema registrado vence, como no `Tooark.Securities.OpenId`.
- **`MapInboundClaims = false`**: as claims chegam ao `ClaimsPrincipal` com os nomes gravados no token (`id`,
  `sub`, `login`), e não com os URIs do `ClaimTypes`. Leia-as pelas constantes de `TooarkClaimNames`.
- **Fail fast**: a chave é carregada e validada no registro. Sem chave de validação (algoritmo assimétrico só
  com `PrivateKey`) o startup falha com `Options.Jwt.KeyNotConfigured;PublicKey`.
- O callback `configure` recebe as `JwtBearerOptions` nativas e roda por último, então qualquer coisa pode ser
  ajustada, inclusive os padrões Tooark.

A aplicação ainda precisa de `app.UseAuthentication()` e `app.UseAuthorization()`. Um servidor de recursos que só
valida tokens (sem emitir) configura a seção `Jwt` só com a `PublicKey` e chama `AddTooarkJwtBearer` sozinho.

### Nomes das claims

O `JwtTokenService` grava os dados do usuário como claims. Existem dois conjuntos de nomes, escolhidos nas
`JwtOptions`:

| Dado       | Nome legado (`EmitLegacyClaims`) | Nome padrão (`EmitStandardClaims`)  | Padrão              |
| ---------- | -------------------------------- | ----------------------------------- | ------------------- |
| `Id`       | `id`                             | `sub`                               | RFC 7519            |
| `Login`    | `login`                          | `preferred_username`                | OpenID Connect Core |
| `Security` | `security`                       | `security` (sem equivalente padrão) | —                   |

- Na linha **4.x**, o padrão emite só os nomes legados (`EmitLegacyClaims = true`,
  `EmitStandardClaims = false`), então os consumidores atuais não veem mudança.
- Com os **dois sinalizadores ligados**, o token carrega os dois conjuntos: o caminho de migração. Os
  consumidores novos leem `sub` enquanto os antigos ainda leem `id`.
- O `UserTokenDto` lê `id` e cai para `sub`, `login` e cai para `preferred_username`, então um token emitido em
  qualquer modo é lido.
- As constantes ficam em `TooarkClaimNames`; o `RoleClaimsExtension.GetClaims` tem sobrecargas que recebem os
  sinalizadores ou as `JwtOptions`.

> ⚠️ **Muda na v5:** os padrões se invertem — `EmitStandardClaims = true` e `EmitLegacyClaims = false`. Um token
> emitido por um serviço v5 sem sinalizadores explícitos tem `sub` e não tem `id`. Defina os dois sinalizadores
> de forma explícita na seção `Jwt` hoje, e leve os leitores para `sub`/`preferred_username` (ou para o
> `UserTokenDto`, que trata os dois), para que a troca de versão não mude nada.

### Testando emissão e expiração

O `JwtTokenService` tem um construtor que recebe um `TimeProvider`: `iat`, `nbf` e `exp` saem dele, e a validação
da vida útil também o usa. Um relógio falso testa a expiração sem esperar:

```csharp
var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
var service = new JwtTokenService(Options.Create(jwtOptions), clock);

var token = service.Create(new JwtTokenDto(userId, login, securityStamp));

clock.Advance(TimeSpan.FromMinutes(jwtOptions.ExpirationTime + 1));
var result = service.Validate(token); // result.ErrorToken == "Token.Expired"
```

Quando o container registra um `TimeProvider`, a injeção de dependência escolhe esse construtor; sem ele, o
serviço usa o `TimeProvider.System`, com a validação de vida útil do próprio handler.

---

## 🔒 Criptografia - Algoritmos Suportados

| Algoritmo   | Modo        | Descrição                                          |
| ----------- | ----------- | -------------------------------------------------- |
| `GCM`       | AES-256-GCM | **Recomendado** - Authenticated encryption         |
| `CBC`       | AES-256-CBC | Modo tradicional com IV aleatório                  |
| `CBCUnsafe` | AES-256-CBC | ⚠️ Legado - IV zerado, **somente descriptografia** |

> `CBCUnsafe` existe apenas para ler dados antigos criptografados com IV zero: criptografar neste modo
> lança exceção (`Options.Cryptography.AlgorithmDecryptOnly`). Para novos dados, use `GCM`.

---

## 🔁 Criptografia - Várias Chaves e Rotação

Com uma chave só, rotacioná-la significa decifrar tudo com a chave antiga e cifrar com a nova, à mão e de uma
vez. O `IKeyedCryptographyService` mantém várias chaves identificadas: os valores novos saem com a chave corrente,
os antigos continuam abrindo com a chave que os gravou, e cada registro é recifrado quando for conveniente — na
leitura, em um job em lote, ou nunca.

### Configurando as chaves

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "CurrentKeyId": "k2",
    "Keys": {
      "k1": "<Base64 da chave antiga de 32 bytes>",
      "k2": "<Base64 da chave nova de 32 bytes>"
    },
    "SecretBase64": "<Base64 da chave única usada antes, se houver valores gravados com ela>"
  }
}
```

Com `Keys` preenchido, o `AddTooarkCryptography` (e o `AddTooarkSecurities`) registra o `KeyedCryptographyService`
como `ICryptographyService` **e** como `IKeyedCryptographyService`: o código que já injeta `ICryptographyService`
passa a cifrar com a chave corrente e a ler os valores legados sem mudar nada. Sem `Keys`, o `CryptographyService`
de chave única é registrado como sempre. As chaves identificadas são validadas no startup (veja as
[propriedades de `CryptographyOptions`](#propriedades-de-cryptographyoptions)).

### Formato do valor

```text
tk1.k2.AbCd...                       valor identificado: versão do formato . id da chave . payload em Base64
AbCd...                              valor legado, gravado antes da adoção das chaves
```

O ponto não existe no alfabeto Base64, então um valor legado nunca se confunde com um identificado. O payload é
exatamente o que o `CryptographyService` produz com aquela chave e o algoritmo configurado.

### Fluxo de rotação

1. Gere a chave nova (`openssl rand -base64 32`), adicione-a em `Keys` com um identificador novo e aponte o
   `CurrentKeyId` para ela. Mantenha a chave antiga em `Keys`.
2. Faça o deploy. Os valores novos saem com a chave nova; os antigos continuam abrindo.
3. Recifre os valores guardados no seu ritmo — na leitura, como abaixo, ou em um job em lote.
4. Quando nenhum valor usar a chave antiga (`KeyIdOf` devolve o identificador antigo, ou `null` para os
   legados), remova-a de `Keys` — e `SecretBase64`/`Secret` quando não restar valor legado.

```csharp
public class CustomerRepository(IKeyedCryptographyService crypto, AppDbContext db)
{
    public async Task<string> ReadDocumentAsync(Customer customer)
    {
        var document = crypto.Decrypt(customer.EncryptedDocument);

        // Recifra na leitura: o registro passa para a chave corrente na primeira vez em que é usado
        if (!crypto.IsCurrent(customer.EncryptedDocument))
        {
            customer.EncryptedDocument = crypto.ReEncrypt(customer.EncryptedDocument);
            await db.SaveChangesAsync();
        }

        return document;
    }
}
```

### Limitações

- **Remover uma chave torna ilegíveis os valores dela**: o `Decrypt` falha com `Cryptography.KeyNotFound;{id}`.
  Só remova uma chave depois de recifrar tudo o que foi gravado com ela.
- **Valores legados precisam da chave legada**: sem `Secret`/`SecretBase64`, um valor sem prefixo falha com
  `Cryptography.LegacyKeyNotConfigured`.
- **Um algoritmo para todas as chaves**: `Algorithm` vale para todas, inclusive a legada. Trocar o algoritmo é
  uma recriptografia, não uma rotação.
- O `ReEncrypt` devolve o mesmo texto quando o valor já está na chave corrente.
- O valor cresce pelo prefixo (`tk1.` mais o identificador e um ponto): dimensione a coluna do banco.

---

## 🔏 Data Protection - Key Ring

O Data Protection do ASP.NET Core protege os payloads de vida curta da aplicação — cookie de autenticação,
cookies de correlação e `state` do OpenID Connect, antiforgery, TempData — sem que ninguém o configure. O
padrão, porém, grava as chaves no disco local de cada instância: com mais de uma instância, ou num contêiner
que reinicia sem volume, uma instância não abre o que a outra protegeu (`Correlation failed`,
`Unable to unprotect the message.State`, usuários deslogados). `AddTooarkDataProtection` configura onde o key
ring fica, como as chaves são protegidas e qual aplicação ele isola.

O recurso é opcional e não substitui o `ICryptographyService`:

| Recurso                | Use para                                                  | Chave                                   |
| ---------------------- | --------------------------------------------------------- | --------------------------------------- |
| `ICryptographyService` | Dados gravados por tempo indeterminado (colunas no banco) | Fixa, sob controle da aplicação         |
| Data Protection        | Payloads de vida curta (cookies, tokens de link)          | Gira sozinha, a cada 90 dias por padrão |

> Não use o Data Protection para dados em repouso de longa duração: se o key ring se perder, tudo o que ele
> protegeu fica ilegível.

### Armazenamentos e proteções fora das opções

As opções cobrem o que o Data Protection oferece sem pacote extra: diretório, certificado e
[armazenamento próprio por callbacks](#armazenamento-próprio-com-readkeys-e-writekey). Para Redis, Azure
Blob Storage, banco de dados ou Azure Key Vault, instale o pacote do provedor e use `ConfigureDataProtection`,
que recebe o builder nativo e roda por último:

```csharp
builder.Services.AddTooarkDataProtection(builder.Configuration, options =>
{
    options.ConfigureDataProtection = dataProtection => dataProtection
        .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
});
```

Para o banco da aplicação com Entity Framework Core, o pacote oficial
`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` faz o mesmo pelo `DbContext`: o contexto implementa
`IDataProtectionKeyContext` (um `DbSet<DataProtectionKey>`) e o builder persiste as chaves nele. O
`Tooark.Securities` não ganha dependência do Entity Framework Core por isso — só a aplicação que escolhe esse
armazenamento instala o pacote:

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

Nada fica travado: `services.AddDataProtection()` continua disponível antes ou depois, e o que for registrado
por último vence.

### Armazenamento próprio com `ReadKeys` e `WriteKey`

Para guardar as chaves no banco, num storage ou num vault sem instalar pacote de provedor, informe duas funções:
`ReadKeys`, que devolve o XML de todas as chaves, e `WriteKey`, que grava uma chave nova. Elas substituem o
`KeysPath` (informar os dois falha no startup) e satisfazem a trava `RequirePersistentKeyStorage`.

- Cada chamada recebe o provedor de serviços de um escopo próprio: serviços com escopo, como o `DbContext`, são
  resolvidos direto.
- `WriteKey` recebe o nome da chave (`key-{guid}`, único) e o XML. Com certificado configurado, o XML já chega
  cifrado, e o armazenamento nunca vê a chave aberta.
- O ASP.NET Core chama o armazenamento de forma síncrona, e o Tooark aguarda a tarefa. As chamadas são raras: a
  leitura ao carregar o key ring (no startup e a cada 24 horas) e depois de criar uma chave; a gravação na
  primeira execução e a cada rotação (90 dias por padrão).
- Uma exceção nos callbacks chega a quem usa o Data Protection. Deixe-a subir: tratar a falha e devolver uma
  lista vazia faz o ASP.NET Core gerar chaves novas (veja a tabela abaixo).
- Os callbacks só existem em código, como o `ConfigureDataProtection`.

#### ✅ Faça: tabela no banco da aplicação

```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Database=minha-api;..."
  },
  "DataProtection": {
    "ApplicationName": "minha-api",
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
    // Todas as chaves, inclusive as expiradas: elas ainda abrem o que protegeram
    options.ReadKeys = async services =>
    {
        var db = services.GetRequiredService<AppDbContext>();

        return await db.DataProtectionKeys.Select(key => key.Xml).ToListAsync();
    };

    // Só insere: cada chave tem nome único e nunca é alterada nem apagada
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

A tabela é uma entidade comum do `DbContext` da aplicação, criada por migration antes do primeiro startup:

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
        // Nome único: uma segunda gravação com o mesmo nome falha, em vez de sobrescrever a chave
        modelBuilder.Entity<DataProtectionKeyRecord>().HasIndex(key => key.Name).IsUnique();
    }
}
```

O resultado: todas as instâncias leem as mesmas chaves, um pod recriado continua abrindo os tokens antigos, e a
cada rotação a tabela ganha uma linha, com as anteriores preservadas. A senha do certificado vem de fora do
`appsettings.json`, pela variável de ambiente `DataProtection__CertificatePassword`.

#### ❌ Não faça: callbacks que perdem chaves

> ⚠️ **Estes callbacks perdem dados, e a trava não percebe:** para ela, há um armazenamento configurado.

```csharp
// ❌ Lista em memória: cada instância tem a sua, e ela some a cada restart
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
// ❌ Uma linha só, sobrescrita a cada chave nova: a chave anterior desaparece
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

| Erro                                                                | O que acontece                                                                                                                                                                                                                                     |
| ------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Guardar em memória (lista, `IMemoryCache`, campo estático)          | Cada instância tem as próprias chaves, e elas somem no restart: `The key {…} was not found in the key ring` na outra instância e depois de cada deploy. É o mesmo que não configurar armazenamento                                                 |
| Sobrescrever a chave em vez de inserir                              | Funciona até a primeira rotação (90 dias por padrão) e passa em qualquer teste curto. Na rotação, a chave anterior some: segundos depois, quando o key ring recarrega, tudo o que ela protegeu para de abrir, de sessões e tokens a dados gravados |
| Ler só a chave mais recente, ou filtrar as expiradas                | O mesmo efeito da linha anterior, mesmo com a tabela intacta                                                                                                                                                                                       |
| Apagar chaves antigas (rotina de limpeza, TTL, cache com expiração) | O que elas protegeram fica ilegível. Para dados em repouso, a perda é definitiva                                                                                                                                                                   |
| Tratar a falha de leitura e devolver lista vazia                    | Sem chaves, o ASP.NET Core gera e grava chaves novas, e o que foi protegido antes não abre nessa instância. Deixe a exceção subir                                                                                                                  |

### Clusters, contêineres e autoscale

Sem armazenamento configurado, o ASP.NET Core grava as chaves no perfil do usuário, que em contêiner fica dentro
da própria instância. A aplicação sobe normalmente e só registra um aviso no log. O problema aparece quando o
pod é recriado ou quando entra uma segunda instância: usuários deslogados, login OpenID Connect falhando e, para
o que foi protegido em repouso, dados ilegíveis.

`RequirePersistentKeyStorage` transforma esse aviso em falha no startup. Com `true`, a aplicação não sobe sem um
armazenamento de chaves (`Options.DataProtection.KeyStorageNotConfigured`): `KeysPath`, os callbacks `ReadKeys`
e `WriteKey`, um `PersistKeysTo*` em `ConfigureDataProtection` ou um encadeado a `services.AddDataProtection()`, antes ou depois do
`AddTooarkDataProtection`. A conferência roda no startup do host e, sem host, no primeiro uso do Data Protection.

> A trava vem desligada nesta versão para não mudar o comportamento de quem já usa a v4.3.0. Está previsto
> ligá-la por padrão em contêiner na v5.0. Uma API que não usa cookies (só JWT) não depende de chaves
> persistentes: informe `false` para mantê-la desligada também depois dessa mudança.

#### ❌ Não faça: chaves presas à instância

```json
{
  "DataProtection": {
    "ApplicationName": "minha-api"
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

> ⚠️ **Esta configuração perde dados.** Sem `KeysPath` e sem armazenamento externo, cada contêiner gera a própria
> chave em `~/.aspnet/DataProtection-Keys`, dentro dele. Nada falha no registro nem no startup: o log só avisa
> `Storing keys in a directory '…' that may not be persisted outside of the container. Protected data will be
unavailable when container is destroyed.`

| Quando                                               | O que acontece                                                                                                                                                                                                                                                                                |
| ---------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| A requisição cai em outra instância                  | A outra instância não tem a chave: `The key {…} was not found in the key ring`. O cookie de sessão é descartado e o usuário volta para o login, o login OpenID Connect falha com `Correlation failed` ou `Unable to unprotect the message.State`, e formulários com antiforgery são recusados |
| Deploy, restart, pod recriado ou autoscale reduzindo | A chave some com o contêiner, e todas as sessões caem de uma vez                                                                                                                                                                                                                              |
| Um valor protegido foi gravado no banco              | **Perdido para sempre**: nenhuma instância nova tem a chave, e não há como recuperá-la                                                                                                                                                                                                        |

Com `"RequirePersistentKeyStorage": true`, esta mesma configuração não sobe: o erro aparece no deploy, e não depois,
com os usuários já conectados.

#### ❌ Não faça: `KeysPath` sem volume compartilhado

```json
{
  "DataProtection": {
    "ApplicationName": "minha-api",
    "RequirePersistentKeyStorage": true,
    "KeysPath": "/var/dataprotection/keys"
  }
}
```

> ⚠️ **A trava passa e os dados se perdem do mesmo jeito.** O diretório existe, mas, sem um volume montado, ele
> fica na camada gravável de cada contêiner: o resultado é o do exemplo anterior. O mesmo vale para um volume que
> só um pod monta, como o volume por réplica de um StatefulSet ou um disco `ReadWriteOnce`. A trava confere que o
> armazenamento existe, não que ele sobrevive à instância.

#### ✅ Faça: key ring compartilhado e protegido

```json
{
  "DataProtection": {
    "ApplicationName": "minha-api",
    "RequirePersistentKeyStorage": true,
    "KeysPath": "/var/dataprotection/keys",
    "CertificatePath": "/run/secrets/dataprotection.pfx"
  }
}
```

```csharp
using Tooark.Securities.Injections;

var builder = WebApplication.CreateBuilder(args);

// Key ring compartilhado: mesmo ApplicationName, mesmo diretório e mesmo certificado em todas as instâncias
builder.Services.AddTooarkDataProtection(builder.Configuration);

var app = builder.Build();

app.Run();
```

- `/var/dataprotection/keys` é um volume compartilhado por todas as instâncias: NFS, Azure Files ou EFS, e em
  Kubernetes um `PersistentVolumeClaim` com `ReadWriteMany`.
- `/run/secrets/dataprotection.pfx` é o certificado montado como secret, e a senha vem de fora do
  `appsettings.json`, pela variável de ambiente `DataProtection__CertificatePassword`. Para gerar o certificado,
  veja [Certificado do Data Protection (.pfx)](#certificado-do-data-protection-pfx).
- O resultado: uma instância abre o que outra protegeu, um pod recriado continua abrindo os tokens antigos, e as
  chaves ficam cifradas no volume.

Sem volume compartilhado, troque `KeysPath` pelos callbacks `ReadKeys` e `WriteKey` (veja
[Armazenamento próprio com `ReadKeys` e `WriteKey`](#armazenamento-próprio-com-readkeys-e-writekey)) ou por um
provedor em `ConfigureDataProtection`. A trava aceita todos.

#### Outros riscos

A trava não confere estes pontos. Antes de subir mais de uma instância, verifique também:

- **Redis sem persistência** — sem AOF ou RDB, ou com uma `maxmemory-policy` que descarta chaves
  (`allkeys-lru`), o Redis perde o key ring. Use persistência e `noeviction`.
- **Certificado das chaves** — se o `.pfx` se perder, ou for trocado sem manter o antigo, o key ring fica
  ilegível. Guarde-o com os segredos da aplicação e mantenha o antigo em `UnprotectKeysWithAnyCertificate`
  enquanto houver chaves protegidas por ele.
- **Limpeza do armazenamento** — o Data Protection não apaga chaves expiradas: elas continuam abrindo o que
  protegeram. Deixe o armazenamento fora de rotinas de limpeza e de TTL.
- **`ApplicationName`** — sem ele, o isolamento depende do caminho da aplicação, que pode mudar com a imagem.
  Informe o mesmo valor em todas as instâncias.

### Com `AddTooarkSecurities`

`AddTooarkSecurities` registra o Data Protection quando a seção `DataProtection` existe e tem ao menos uma
chave. Se a aplicação também chamar `AddTooarkDataProtection` — para passar `ConfigureDataProtection`, por
exemplo —, a chamada explícita prevalece em qualquer ordem: `AddTooarkSecurities` não reaplica a seção por
cima dela.

---

## 🧂 Hash de Senha

O `IPasswordHashService` gera hashes de senha com **PBKDF2-HMAC-SHA256**, nativo do .NET
(`Rfc2898DeriveBytes.Pbkdf2`), com salt aleatório por senha e 600.000 iterações por padrão — a recomendação atual
da OWASP. O value object `Password` do `Tooark.ValueObjects` valida e mascara uma senha; este serviço é o que vai
para o banco.

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

        // Os parâmetros mudaram desde que este hash foi gravado: regrava enquanto a senha está à mão
        if (passwords.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = passwords.Hash(password);
            await users.UpdateAsync(user);
        }

        return true;
    }
}
```

### Formato do hash

```text
$pbkdf2-sha256$i=600000$<salt em Base64>$<hash em Base64>
```

A string PHC carrega o algoritmo e os parâmetros, então o serviço continua validando hashes gravados com
parâmetros antigos e o `NeedsRehash` aponta quais regravar. Salt e hash usam Base64 sem preenchimento, como o
formato prescreve; o `Verify` aceita também a forma com preenchimento.

- O `Hash` lança `PasswordHash.PasswordNotProvided` para senha nula ou vazia.
- O `Verify` nunca lança: senha nula, hash nulo ou hash em outro formato (outro algoritmo, um texto puro)
  devolve `false`.
- O `NeedsRehash` devolve `true` quando as iterações, o tamanho do salt ou o tamanho do hash diferem das opções
  atuais, e também para um hash que ele não consegue ler.

### Por que PBKDF2 e não Argon2

O .NET não tem Argon2 nativo. Trazê-lo aqui obrigaria todos os consumidores do pacote a receber uma biblioteca
de terceiro, contra a política de dependências da família. O PBKDF2-HMAC-SHA256 com 600.000 iterações é a
configuração recomendada pela OWASP para o algoritmo, e o prefixo PHC deixa espaço para outro algoritmo conviver
na mesma coluna no futuro: um hash PBKDF2 antigo continua validando e é regravado no login seguinte.

### Ajuste

Cada login custa uma derivação; cada tentativa de um atacante que roubou os hashes também. Meça o tempo no
hardware de produção e aumente `Iterations` enquanto o login couber no orçamento (dezenas de milissegundos são
típicos). Baixar de 100.000 é recusado no startup (`Options.PasswordHash.IterationsTooLow;100000`).

---

## 🎫 Tokens Seguros, TOTP e Códigos de Recuperação

Três classes estáticas cobrem os tokens que uma aplicação entrega aos usuários. Nenhuma precisa de registro ou
configuração.

### `SecureToken` — links de redefinição, confirmações e chaves de API

```csharp
// Emissão: o token vai para o usuário (link, e-mail, chave de API), só o hash vai para o banco
var token = SecureToken.Create();                 // 32 bytes aleatórios em Base64Url (43 caracteres)
var hash = SecureToken.Hash(token);               // SHA-256 em Base64Url, determinístico: indexe a coluna
await resets.StoreAsync(userId, hash, expiresAt: DateTime.UtcNow.AddHours(1));

// Resgate: busca o registro pelo hash do que voltou e compara em tempo constante
var record = await resets.FindByHashAsync(SecureToken.Hash(received));

if (record is null || record.ExpiresAt < DateTime.UtcNow || !SecureToken.Verify(received, record.Hash))
{
    return Results.Unauthorized();
}
```

O hash é SHA-256 sem salt nem fator de custo de propósito: o token tem no mínimo 128 bits aleatórios, então a
força bruta é inviável mesmo com um hash rápido, e um hash determinístico permite achar o registro por ele. Hash
lento com salt é para senha, que tem pouca entropia (`IPasswordHashService`). O `Create` aceita de 16 a 1024
bytes; o `Verify` devolve `false` para qualquer coisa ausente ou malformada, nunca lança.

### `Totp` — segundo fator com aplicativos autenticadores

RFC 6238 sobre HMAC-SHA1, 6 dígitos e passos de 30 segundos: a configuração que o Google Authenticator, o
Microsoft Authenticator e similares esperam.

```csharp
// Cadastro: gera o segredo, guarda cifrado, exibe o QR code montado a partir da URI
var secret = Totp.GenerateSecret();                                   // Base32, 20 bytes
user.TotpSecret = crypto.Encrypt(secret);                             // ICryptographyService: quem o lê gera códigos válidos
var uri = Totp.BuildUri(secret, issuer: "MinhaApp", account: user.Email); // otpauth://totp/MinhaApp:ana%40empresa.com?secret=...

// Confirma o cadastro com o primeiro código que o aplicativo mostra
if (!Totp.Verify(secret, typedCode, lastUsedStep: -1, out var step))
{
    return Results.BadRequest();
}

user.TotpLastUsedStep = step;                                         // o mesmo código nunca é aceito duas vezes
```

```csharp
// Login: confere contra o segredo guardado e o último passo aceito
var secret = crypto.Decrypt(user.TotpSecret);

if (!Totp.Verify(secret, typedCode, user.TotpLastUsedStep, out var step))
{
    return Results.Unauthorized();
}

user.TotpLastUsedStep = step;
```

- **Tolerância de relógio**: `window` (padrão 1) aceita o passo anterior e o seguinte além do atual — 30 segundos
  para cada lado. Todos os passos são calculados e comparados em tempo constante, mesmo depois de um acerto.
- **Proteção contra reuso**: passos iguais ou anteriores a `lastUsedStep` são recusados. Grave o `matchedStep` de
  cada código aceito e informe-o na verificação seguinte; informe um valor negativo antes do primeiro.
- Código ausente, com tamanho errado ou com caracteres que não são dígitos devolve `false`; espaços são
  ignorados. Um segredo que não decodifica lança `ArgumentException`, porque um segredo corrompido trancaria o
  usuário em silêncio.
- `Compute(secret, step, digits)` e `GetTimeStep(at, period)` expõem as peças para testes e para exibir um código
  no servidor; o `TimeProvider` do `Verify` deixa um teste controlar o relógio.
- `digits` (6 a 8) e `periodSeconds` são configuráveis, mas nem todo aplicativo honra valores diferentes de 6 e 30.

### `RecoveryCodes` — quando o autenticador se perde

```csharp
// Cadastro: gera, exibe uma vez, guarda só os hashes
var codes = RecoveryCodes.Generate();                                 // 10 códigos como "7KQ2M-8XR4N"
user.RecoveryCodeHashes = codes
    .Select(code => SecureToken.Hash(RecoveryCodes.Normalize(code)))
    .ToList();

// Resgate: normaliza o que o usuário digitou (caixa, hífens, espaços), acha o hash, apaga-o
var normalized = RecoveryCodes.Normalize(typedCode);
var match = user.RecoveryCodeHashes.FirstOrDefault(hash => SecureToken.Verify(normalized, hash));

if (match is null)
{
    return Results.Unauthorized();
}

user.RecoveryCodeHashes.Remove(match);                                // cada código vale um uso
```

O alfabeto não tem `0`/`O` nem `1`/`I`, então um código impresso é digitado sem ambiguidade. O `Generate` devolve
códigos distintos, 10 de 10 caracteres por padrão (1 a 50 códigos, 8 a 32 caracteres).

---

## 📝 Exemplos de Uso

### JWT - Criação e Validação de Token

#### Configuração com HMAC (Simétrico)

```json
{
  "Jwt": {
    "Algorithm": "HS256",
    "Secret": "minha-chave-secreta-super-segura-32chars",
    "Issuer": "minha-api",
    "Audience": "meus-clientes",
    "ExpirationTime": 60
  }
}
```

#### Configuração com RSA (Assimétrico)

```json
{
  "Jwt": {
    "Algorithm": "RS256",
    "PrivateKey": "MIIEvQIBADANBgkqh...chave-privada-base64...",
    "PublicKey": "MIIBIjANBgkqhkiG9w...chave-publica-base64...",
    "Issuer": "minha-api",
    "Audience": "meus-clientes",
    "ExpirationTime": 60
  }
}
```

#### Criando um Token

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
        // Validar credenciais...

        // Criar dados do token
        var tokenData = new JwtTokenDto(
            id: user.Id,
            login: user.Email,
            security: user.SecurityStamp
        );

        // Gerar token
        var token = _jwtService.Create(tokenData);

        return Ok(new { Token = token });
    }

    // Com audience customizada e claims extras
    [HttpPost("login-custom")]
    public IActionResult LoginCustom(LoginRequest request)
    {
        var tokenData = new JwtTokenDto(user.Id, user.Email, user.SecurityStamp);

        var extraClaims = new[]
        {
            new Claim("role", "admin"),
            new Claim("department", "IT")
        };

        var token = _jwtService.Create(tokenData, audience: "app-mobile", extraClaims: extraClaims);

        return Ok(new { Token = token });
    }
}
```

#### Validando um Token

O handler da Microsoft.IdentityModel expõe a validação apenas de forma assíncrona, então `ValidateAsync` é o
caminho direto e `Validate` é o encapsulamento síncrono. Em código que já é assíncrono, prefira `ValidateAsync`:

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

Em código síncrono, a sobrecarga bloqueante entrega o mesmo resultado:

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
        // Ou usando helpers
        GuidId = result.GetGuidId,
        IntId = result.GetIntId
    });
}
```

#### Lendo claims extras

As claims extras informadas no `Create` voltam no `UserTokenDto`. A propriedade `Claims` traz todas as
claims do token — as do usuário, as extras e as registradas pelo emissor (`exp`, `iat`, `iss`, `aud`) — e
os métodos abaixo leem uma claim pelo tipo. Como `Id`, `Login` e `Security`, uma claim ausente não lança
exceção:

| Método              | Retorno                                                                       |
| ------------------- | ----------------------------------------------------------------------------- |
| `GetClaim(type)`    | O primeiro valor da claim, ou vazio quando ela não existe                     |
| `GetClaim<T>(type)` | O valor convertido (`Guid`, `int`, `bool`, `decimal`...), ou o padrão do tipo |
| `GetClaims(type)`   | Todos os valores da claim (papéis, permissões), ou uma lista vazia            |
| `HasClaim(type)`    | Se a claim existe no token, mesmo com valor vazio                             |

```csharp
// Criação: as claims extras entram no token
var extraClaims = new[]
{
    new Claim("tenant", tenantId.ToString()),
    new Claim("role", "admin"),
    new Claim("role", "user")
};

var token = _jwtService.Create(tokenData, extraClaims: extraClaims);

// Validação: as mesmas claims saem no UserTokenDto
var result = await _jwtService.ValidateAsync(token);

var tenant = result.GetClaim<Guid>("tenant"); // Guid.Empty quando ausente ou inválida
var roles = result.GetClaims("role");         // ["admin", "user"]
```

> Uma claim repetida na criação vira um array no payload e volta como uma claim por valor: use
> `GetClaims` para ler todos. `GetClaim<T>` converte com a cultura invariante, então números decimais
> usam ponto (`1234.56`).

Para dar nome e tipo às claims próprias da aplicação, concentre os nomes em extensões do `UserTokenDto`
no projeto da aplicação. O restante do código lê propriedades, sem repetir strings:

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

// Uso
var tenantId = result.GetTenantId();
```

Com C# 14 (.NET 10 SDK), os mesmos atalhos podem ser propriedades de extensão:

```csharp
public static class UserTokenDtoExtensions
{
    extension(UserTokenDto user)
    {
        public Guid TenantId => user.GetClaim<Guid>(AppClaims.Tenant);

        public IReadOnlyList<string> Roles => user.GetClaims(AppClaims.Role);
    }
}

// Uso
var tenantId = result.TenantId;
```

---

### Criptografia - Encrypt e Decrypt

#### Configuração

```json
{
  "Cryptography": {
    "Algorithm": "GCM",
    "Secret": "minha-chave-secreta-32-caracteres"
  }
}
```

#### Usando ICryptographyService

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
        // Retorna texto criptografado em Base64
        return _crypto.Encrypt(plainText);
    }

    public string UnprotectData(string encryptedText)
    {
        // Retorna texto original descriptografado
        return _crypto.Decrypt(encryptedText);
    }
}
```

#### Exemplo Completo

```csharp
// Configurar no Program.cs (registra ICryptographyService como singleton)
builder.Services.AddTooarkCryptography(builder.Configuration);

// Usar no serviço
public class UserService
{
    private readonly ICryptographyService _crypto;

    public UserService(ICryptographyService crypto)
    {
        _crypto = crypto;
    }

    public void SaveUser(User user)
    {
        // Criptografar dados sensíveis antes de salvar
        user.CreditCard = _crypto.Encrypt(user.CreditCard);
        user.SSN = _crypto.Encrypt(user.SSN);

        // Salvar no banco...
    }

    public User GetUser(int id)
    {
        var user = // Buscar do banco...

        // Descriptografar dados sensíveis
        user.CreditCard = _crypto.Decrypt(user.CreditCard);
        user.SSN = _crypto.Decrypt(user.SSN);

        return user;
    }
}
```

---

### Data Protection - Protegendo um Token de Link

Com o Data Protection registrado, qualquer serviço pode criar um protetor próprio. O propósito isola cada uso:
um protetor com outro propósito não abre estes tokens.

```csharp
public class InviteService
{
    private readonly IDataProtector _protector;

    public InviteService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Convites");
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
            // Token adulterado ou protegido por outro key ring
            return null;
        }
    }
}
```

> Para um token que expira sozinho, use `provider.CreateProtector("Convites").ToTimeLimitedDataProtector()` e
> informe a validade em `Protect` (pacote `Microsoft.AspNetCore.DataProtection.Extensions`, já incluso no
> ASP.NET Core).

---

## 🔑 Gerando Chaves

### Chave para HMAC (HS256/HS384/HS512)

```bash
# Gerar chave aleatória de 32 bytes (256 bits) para HS256
openssl rand -base64 32

# Gerar chave aleatória de 64 bytes (512 bits) para HS512
openssl rand -base64 64
```

### Chaves AES para `SecretBase64` e `Keys`

```bash
# Uma chave aleatória de 32 bytes em Base64, para SecretBase64 ou para cada entrada de Keys
openssl rand -base64 32
```

```csharp
// Ou no .NET
Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
```

### Chaves RSA (RS256/PS256)

```bash
# Gerar chave privada RSA de 2048 bits
openssl genrsa -out private.pem 2048

# Extrair chave pública
openssl rsa -in private.pem -pubout -out public.pem

# Converter para formato PKCS8 (recomendado)
openssl pkcs8 -topk8 -inform PEM -outform PEM -nocrypt -in private.pem -out private_pkcs8.pem

# Obter chave em Base64 (sem headers)
cat private_pkcs8.pem | grep -v "BEGIN\|END" | tr -d '\n'
cat public.pem | grep -v "BEGIN\|END" | tr -d '\n'
```

### Chaves ECDsa (ES256/ES384/ES512)

```bash
# ES256 (P-256)
openssl ecparam -genkey -name prime256v1 -noout -out ec_private.pem
openssl ec -in ec_private.pem -pubout -out ec_public.pem

# ES384 (P-384)
openssl ecparam -genkey -name secp384r1 -noout -out ec_private.pem

# ES512 (P-521)
openssl ecparam -genkey -name secp521r1 -noout -out ec_private.pem

# Converter para PKCS8
openssl pkcs8 -topk8 -nocrypt -in ec_private.pem -out ec_private_pkcs8.pem
```

### Certificado do Data Protection (.pfx)

```bash
# Certificado autoassinado RSA de 2048 bits, válido por 2 anos
openssl req -x509 -newkey rsa:2048 -sha256 -days 730 -nodes -subj "/CN=dataprotection" \
  -keyout dataprotection.key -out dataprotection.crt

# Empacota o certificado e a chave privada em PKCS#12
openssl pkcs12 -export -inkey dataprotection.key -in dataprotection.crt \
  -out dataprotection.pfx -passout pass:senha-do-certificado
```

> O certificado precisa ser **RSA** (a criptografia XML das chaves não aceita ECDSA). Ao trocá-lo, mantenha o
> antigo disponível com `UnprotectKeysWithAnyCertificate` em `ConfigureDataProtection` até as chaves
> protegidas por ele expirarem.

---

## 📋 Dependências

| Pacote                                                                                                                          | Versão   | Descrição                                               |
| ------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                         | 4.x      | Exceções (ex.: `BadRequestException`)                   |
| [`Microsoft.AspNetCore.Authentication.JwtBearer`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer) | 8.x/10.x | Autenticação JWT para ASP.NET Core                      |
| [`Microsoft.AspNetCore.DataProtection`](https://www.nuget.org/packages/Microsoft.AspNetCore.DataProtection)                     | 8.x/10.x | Data Protection, pacote avulso que roda no runtime base |

---

## 🎯 Boas Práticas

### JWT

1. **Use algoritmos assimétricos (RS/PS/ES) em produção** - Permite validar tokens sem expor a chave de assinatura
2. **Configure `ExpirationTime` apropriadamente** - Tokens de curta duração são mais seguros
3. **Use `Issuer` e `Audience`** - Previne uso indevido de tokens entre aplicações
4. **Armazene chaves em Secret Manager** - Nunca commite chaves no código fonte
5. **Registre `AddTooarkJwtBearer` em vez de um `AddJwtBearer` à mão** - Mesma chave, emissor e destinatário do emissor, validados no startup
6. **Defina `EmitLegacyClaims` e `EmitStandardClaims` explicitamente** - Os padrões da v5 se invertem; sinalizadores explícitos mantêm os tokens estáveis na troca de versão
7. **Leia as claims por `TooarkClaimNames` ou `UserTokenDto`** - Eles seguem os dois conjuntos de nomes

### Criptografia

1. **Prefira GCM sobre CBC** - GCM fornece autenticação integrada
2. **Nunca use CBCUnsafe para novos dados** - Modo somente-descriptografia para compatibilidade com sistemas legados
3. **Prefira `SecretBase64` com chave aleatória de 32 bytes** - A chave é usada diretamente, sem derivação
4. **Gere chaves aleatórias** - Use `openssl rand -base64 32` ou `RandomNumberGenerator.GetBytes(32)`
5. **Adote `Keys` antes que a primeira rotação seja urgente** - Valores gravados com chaves identificadas rotacionam sem recriptografia em massa
6. **Nunca remova uma chave com valores ainda cifrados por ela** - Confira com `KeyIdOf` ou recifre na leitura antes

### Hash de senha

1. **Guarde só a string PHC** - Nunca a senha, nunca um hash rápido dela
2. **Chame `NeedsRehash` depois de um `Verify` verdadeiro** - O hash é regravado enquanto a senha está à mão
3. **Aumente `Iterations` com o hardware** - Meça o tempo do login e mantenha-o no orçamento; nunca abaixo do mínimo
4. **Limite as tentativas** - Um hash lento não substitui o limite de taxa no endpoint de login

### Tokens e segundo fator

1. **Guarde hashes, não tokens** - `SecureToken.Hash` para tokens de redefinição, chaves de API e códigos de recuperação
2. **Dê expiração aos tokens** - O hash não expira sozinho; mantenha um `ExpiresAt` ao lado
3. **Cifre o segredo TOTP em repouso** - Quem o lê gera códigos válidos; use o `ICryptographyService`
4. **Persista o `matchedStep`** - Sem ele, um código interceptado dentro da janela é aceito duas vezes
5. **Mostre os códigos de recuperação uma vez** - E invalide cada um no uso

### Data Protection

1. **Defina `ApplicationName`** - O isolamento fica igual em todas as instâncias, independente do caminho de instalação
2. **Compartilhe o key ring entre as instâncias** - Volume persistente em `KeysPath`, os callbacks `ReadKeys` e `WriteKey` ou um armazenamento via `ConfigureDataProtection`
3. **Ligue `RequirePersistentKeyStorage` em contêiner** - A aplicação deixa de subir com as chaves presas à instância
4. **Nos callbacks, só insira e leia tudo** - Chave sobrescrita, filtrada ou apagada torna ilegível o que ela protegeu
5. **Proteja as chaves em repouso** - Com `KeysPath` ou callbacks e sem certificado, as chaves ficam gravadas sem criptografia, em qualquer sistema operacional
6. **Não use para dados de longa duração** - Colunas criptografadas no banco ficam com o `ICryptographyService`

---

## ⚠️ Códigos de Erro e Soluções

| Serviço                    | Mensagem                                              | Descrição                                                   | Solução                                                               | Exception             |
| -------------------------- | ----------------------------------------------------- | ----------------------------------------------------------- | --------------------------------------------------------------------- | --------------------- |
| `CryptographyService`      | `Options.NotConfigured`                               | `Options` não configurado                                   | Configure `CryptographyOptions`                                       | `InternalServerError` |
| `CryptographyService`      | `Options.Cryptography.SecretNotConfigured`            | Nenhuma chave configurada                                   | Configure `Secret` ou `SecretBase64` em `CryptographyOptions`         | `InternalServerError` |
| `CryptographyService`      | `Options.Cryptography.SecretBase64Invalid`            | `SecretBase64` não é Base64 válido                          | Forneça um valor Base64 válido em `SecretBase64`                      | `InternalServerError` |
| `CryptographyService`      | `Options.Cryptography.SecretBase64InvalidSize`        | `SecretBase64` não tem 32 bytes                             | Use uma chave de exatamente 32 bytes (AES-256)                        | `InternalServerError` |
| `CryptographyService`      | `Options.Cryptography.AlgorithmDecryptOnly`           | `CBCUnsafe` usado para criptografar                         | Use `CBCUnsafe` apenas para descriptografar dados legados             | `InternalServerError` |
| `CryptographyService`      | `Cryptography.PlainTextNotProvided`                   | `PlainText` não fornecido                                   | Forneça o texto plano para criptografar                               | `BadRequest`          |
| `CryptographyService`      | `Cryptography.CipherTextNotProvided`                  | `CipherText` não fornecido                                  | Forneça o texto criptografado para descriptografar                    | `BadRequest`          |
| `CryptographyService`      | `Cryptography.InvalidCipherText`                      | `CipherText` inválido                                       | Forneça um texto criptografado válido para descriptografar            | `BadRequest`          |
| `KeyedCryptographyService` | `Options.Cryptography.Keys.NotConfigured`             | Serviço identificado sem chaves                             | Preencha `Keys` ou use `Secret`/`SecretBase64` de chave única         | `InternalServerError` |
| `KeyedCryptographyService` | `Options.Cryptography.CurrentKeyId.NotConfigured`     | `Keys` sem `CurrentKeyId`                                   | Aponte `CurrentKeyId` para um dos identificadores de `Keys`           | `InternalServerError` |
| `KeyedCryptographyService` | `Options.Cryptography.CurrentKeyId.NotFound;{id}`     | `CurrentKeyId` fora de `Keys`                               | Use um identificador que exista em `Keys`                             | `InternalServerError` |
| `KeyedCryptographyService` | `Options.Cryptography.KeyId.Invalid;{id}`             | Identificador com ponto, espaço ou outro caractere inválido | Use só letras, dígitos, hífen e sublinhado                            | `InternalServerError` |
| `KeyedCryptographyService` | `Options.Cryptography.Keys.Invalid;{id}`              | Chave que não tem 32 bytes em Base64                        | Gere a chave com `openssl rand -base64 32`                            | `InternalServerError` |
| `KeyedCryptographyService` | `Cryptography.KeyNotFound;{id}`                       | Valor cifrado com uma chave que não está configurada        | Devolva a chave a `Keys`; só remova chaves depois de recifrar         | `BadRequest`          |
| `KeyedCryptographyService` | `Cryptography.LegacyKeyNotConfigured`                 | Valor sem prefixo e sem `Secret`/`SecretBase64`             | Mantenha a chave legada até recifrar todos os valores legados         | `BadRequest`          |
| `PasswordHashService`      | `Options.NotConfigured`                               | `Options` não configurado                                   | Registre com `AddTooarkPasswordHash`                                  | `InternalServerError` |
| `PasswordHashService`      | `Options.PasswordHash.IterationsTooLow;100000`        | `Iterations` abaixo do mínimo                               | Use ao menos 100.000 iterações (padrão 600.000)                       | `InternalServerError` |
| `PasswordHashService`      | `Options.PasswordHash.SaltSizeTooSmall;16`            | `SaltSize` abaixo do mínimo                                 | Use ao menos 16 bytes                                                 | `InternalServerError` |
| `PasswordHashService`      | `Options.PasswordHash.HashSizeTooSmall;32`            | `HashSize` abaixo do mínimo                                 | Use ao menos 32 bytes                                                 | `InternalServerError` |
| `PasswordHashService`      | `PasswordHash.PasswordNotProvided`                    | Senha nula ou vazia em `Hash`                               | Valide a senha antes de gerar o hash                                  | `BadRequest`          |
| `AddTooarkDataProtection`  | `Options.DataProtection.KeyLifetimeTooShort;7`        | `KeyLifetimeDays` abaixo do mínimo                          | Use ao menos 7 dias, ou deixe nulo para os 90 dias padrão             | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateAmbiguous`         | `CertificatePath` e `CertificateThumbprint` juntos          | Informe apenas um dos dois                                            | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificatePathNotConfigured` | `CertificatePassword` sem `CertificatePath`                 | Informe `CertificatePath` ou remova a senha                           | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateNotFound;{valor}`  | Arquivo ou thumbprint não encontrado                        | Confira o caminho, ou instale o certificado no repositório `My`       | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateInvalid`           | `.pfx` ilegível ou senha incorreta                          | Confira o arquivo e `CertificatePassword`                             | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateWithoutPrivateKey` | Certificado sem chave privada                               | Exporte o `.pfx` com a chave privada                                  | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.CertificateNotRsa`            | Certificado sem chave RSA                                   | Use um certificado RSA                                                | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.KeyStorageNotConfigured`      | Trava ligada e nenhum armazenamento de chaves               | Informe `KeysPath`, os callbacks ou um armazenamento externo          | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.KeyCallbacksIncomplete`       | Só um dos callbacks `ReadKeys` e `WriteKey`                 | Informe os dois callbacks                                             | `InternalServerError` |
| `AddTooarkDataProtection`  | `Options.DataProtection.KeyStorageAmbiguous`          | `KeysPath` junto com `ReadKeys` e `WriteKey`                | Informe apenas um armazenamento                                       | `InternalServerError` |
| `JwtTokenService`          | `Options.NotConfigured`                               | `Options` não configurado                                   | Configure `JwtOptions`                                                | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.SecretNotConfigured`                     | `Secret` não configurado                                    | Configure `Secret` dentro de `JwtOptions` para token simétrico        | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.SecretTooShort`                          | `Secret` abaixo do mínimo do algoritmo                      | Use ao menos 32/48/64 bytes para HS256/HS384/HS512                    | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.KeysNotConfigured`                       | `Private` e `Public` não configurado                        | Configure as chaves dentro de `JwtOptions` para token assimétrico     | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.PrivateKey.InvalidSize`                  | Tamanho da chave `Private` inválido                         | Use uma chave `Private` de pelo menos 2048 bits                       | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.PublicKey.InvalidSize`                   | Tamanho da chave `Public` inválido                          | Use uma chave `Public` de pelo menos 2048 bits                        | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.PrivateKey.InvalidCurve`                 | Curva da chave `Private` inválida                           | Use uma chave `Private` com a curva correta                           | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.PublicKey.InvalidCurve`                  | Curva da chave `Public` inválida                            | Use uma chave `Public` com a curva correta                            | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.InvalidKey`                              | Chave inválida                                              | [Utilize chaves válidas](#-gerando-chaves) para o algoritmo escolhido | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.AlgorithmNotSupported`                   | Algoritmo não suportado                                     | [Utilize um algoritmo suportado](#-jwt---algoritmos-suportados)       | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.KeyNotConfigured;PrivateKey`             | Chave `Private` não configurado                             | Configure `PrivateKey` dentro de `JwtOptions` para gerar um token     | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.KeyNotConfigured;PublicKey`              | Chave `Public` não configurado                              | Configure `PublicKey` dentro de `JwtOptions` para validar um token    | `InternalServerError` |
| `JwtTokenService`          | `Options.Jwt.ClaimsNotConfigured`                     | `EmitLegacyClaims` e `EmitStandardClaims` desligados        | Ligue ao menos um dos dois conjuntos de claims                        | `InternalServerError` |
| `AddTooarkJwtBearer`       | `Options.Jwt.KeyNotConfigured;PublicKey`              | Sem chave de validação para o esquema `Bearer`              | Configure `Secret` (HMAC) ou `PublicKey` na seção `Jwt`               | `InternalServerError` |
| `JwtTokenService`          | `Token.Expired`                                       | Token expirado                                              | Gere um novo token                                                    | N/A                   |
| `JwtTokenService`          | `Token.InvalidSignature`                              | Token com assinatura inválida                               | Utilize apenas token com assinatura válida                            | N/A                   |
| `JwtTokenService`          | `Token.Invalid`                                       | Token inválido                                              | Utilize apenas token válido                                           | N/A                   |
| `JwtTokenService`          | `InternalServerError`                                 | Erro interno do servidor                                    | Analise os logs para mais detalhes                                    | N/A                   |

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Comece pelo
[CONTRIBUTING.md](https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md) — ele cobre o fluxo de
desenvolvimento, as convenções de código e de commit e o checklist de pull request. Bugs e pedidos de
funcionalidade entram pelos [templates de issue](https://github.com/Tooark/nuget-tooark/issues/new/choose) do
repositório [Tooark](https://github.com/Tooark/nuget-tooark).

Ao participar, você concorda com o
[Código de Conduta](https://github.com/Tooark/nuget-tooark/blob/main/CODE_OF_CONDUCT.md).

---

## 🆘 Ajuda & Segurança

- ❓ **Dúvidas, bugs e ideias** — veja o
  [SUPPORT.md](https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md) para escolher o canal certo
- 🔒 **Vulnerabilidades de segurança** — **não** abra issue pública; siga o
  [SECURITY.md](https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md)

---

## 💖 Apoie

Se o Tooark ajuda nos seus projetos, considere apoiar o desenvolvimento:

- 💙 [GitHub Sponsors](https://github.com/sponsors/paulosfjunior)
- ☕ [Ko-fi](https://ko-fi.com/paulosfjunior)

Cada contribuição ajuda a manter o projeto ativo e em evolução. Obrigado! 🙏

---

## 📄 Licença

Este projeto está licenciado sob a [Licença BSD 3-Clause](https://github.com/Tooark/nuget-tooark/blob/main/LICENSE).
