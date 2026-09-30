# Tooark.Securities

Biblioteca de segurança para aplicações .NET, fornecendo serviços de **criptografia AES** e **autenticação JWT** com suporte a múltiplos algoritmos, além da configuração do **Data Protection** do ASP.NET Core.

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Conteúdo do Pacote](#-conteúdo-do-pacote)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [JWT - Algoritmos Suportados](#-jwt---algoritmos-suportados)
- [Criptografia - Algoritmos Suportados](#-criptografia---algoritmos-suportados)
- [Data Protection - Key Ring](#-data-protection---key-ring)
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

| Classe                                                     | Descrição                                       |
| ---------------------------------------------------------- | ----------------------------------------------- |
| [`JwtTokenService`](#jwt---criação-e-validação-de-token)   | Serviço para criação e validação de tokens JWT  |
| [`CryptographyService`](#criptografia---encrypt-e-decrypt) | Serviço de criptografia/descriptografia AES-256 |

### Interfaces

| Interface              | Descrição                                      |
| ---------------------- | ---------------------------------------------- |
| `IJwtTokenService`     | Contrato para manipulação de tokens JWT        |
| `ICryptographyService` | Contrato para criptografia/descriptografia AES |

| Membro                           | Descrição                                                      |
| -------------------------------- | -------------------------------------------------------------- |
| `IJwtTokenService.Create`        | Cria o token. Síncrono: o handler não expõe criação assíncrona |
| `IJwtTokenService.ValidateAsync` | Valida o token. Caminho direto ao handler, que é assíncrono    |
| `IJwtTokenService.Validate`      | Valida o token. Encapsulamento síncrono de `ValidateAsync`     |
| `ICryptographyService.Encrypt`   | Criptografa. Síncrono: AES em memória, sem operação de I/O     |
| `ICryptographyService.Decrypt`   | Descriptografa. Síncrono, pelo mesmo motivo                    |

### DTOs

| Classe         | Descrição                                         |
| -------------- | ------------------------------------------------- |
| `JwtTokenDto`  | Dados para criação de token (id, login, security) |
| `UserTokenDto` | Resultado da validação do token, com as claims    |

### Options

| Classe                | Descrição                                                                    |
| --------------------- | ---------------------------------------------------------------------------- |
| `JwtOptions`          | Configurações do JWT (algoritmo, chaves, issuer(s), audience(s), expiração)  |
| `CryptographyOptions` | Configurações de criptografia (algoritmo, secret ou secretBase64)            |
| `KeyRingOptions`      | Key ring do Data Protection (aplicação, armazenamento, certificado, rotação) |

### Extensions

| Classe                | Descrição                                                           |
| --------------------- | ------------------------------------------------------------------- |
| `RoleClaimsExtension` | Extensão que converte `JwtTokenDto` em claims (id, login, security) |

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Securities
```

---

## ⚙️ Configuração

### appsettings.json

Para configurar os serviços de segurança, adicione as seções `Jwt`, `Cryptography` e `DataProtection` no seu arquivo `appsettings.json`:

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

#### Propriedades de `JwtOptions`

| Propriedade      | Tipo      | Padrão  | Descrição                                                     |
| ---------------- | --------- | ------- | ------------------------------------------------------------- |
| `Algorithm`      | string    | `ES256` | Algoritmo de assinatura. Valores desconhecidos lançam exceção |
| `Secret`         | string?   | `null`  | Chave para algoritmos simétricos (HS256/384/512)              |
| `PrivateKey`     | string?   | `null`  | Chave privada Base64 (PKCS#8) para assinatura                 |
| `PublicKey`      | string?   | `null`  | Chave pública Base64 (SPKI) para validação                    |
| `Issuer`         | string?   | `null`  | Emissor aceito na validação e gravado no token                |
| `Issuers`        | string[]? | `null`  | Emissores adicionais aceitos na validação                     |
| `Audience`       | string?   | `null`  | Destinatário aceito na validação e gravado no token           |
| `Audiences`      | string[]? | `null`  | Destinatários adicionais aceitos na validação                 |
| `ExpirationTime` | int       | `5`     | Expiração do token em **minutos**                             |

> `Issuer`/`Issuers` e `Audience`/`Audiences` se somam: informar qualquer um deles liga a validação do
> respectivo campo. Quando nenhum é informado, a validação daquele campo é desligada. O parâmetro
> `audience` de `Create` e `Validate`/`ValidateAsync` tem prioridade sobre a configuração.
>
> `PrivateKey` e `PublicKey` aceitam tanto o Base64 puro quanto o PEM completo: os delimitadores
> `-----BEGIN/END ... KEY-----` e as quebras de linha são removidos ao atribuir.

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

| Propriedade    | Tipo    | Padrão | Descrição                                                   |
| -------------- | ------- | ------ | ----------------------------------------------------------- |
| `Algorithm`    | string  | `GCM`  | Modo AES-256: `GCM`, `CBC` ou `CBCUnsafe` (somente decrypt) |
| `Secret`       | string? | `null` | Chave derivada via SHA256 (compatibilidade)                 |
| `SecretBase64` | string? | `null` | Chave AES de 32 bytes usada diretamente (**recomendado**)   |

> É obrigatório informar `Secret` **ou** `SecretBase64`. Quando ambos são informados, `SecretBase64` tem
> prioridade. O `SecretBase64` deve representar exatamente 32 bytes (AES-256) — valores inválidos falham
> no startup. Gere uma chave com `Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`.
> Com `Secret`, a chave é derivada via SHA256 (mantido por compatibilidade).

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

## 🔒 Criptografia - Algoritmos Suportados

| Algoritmo   | Modo        | Descrição                                          |
| ----------- | ----------- | -------------------------------------------------- |
| `GCM`       | AES-256-GCM | **Recomendado** - Authenticated encryption         |
| `CBC`       | AES-256-CBC | Modo tradicional com IV aleatório                  |
| `CBCUnsafe` | AES-256-CBC | ⚠️ Legado - IV zerado, **somente descriptografia** |

> `CBCUnsafe` existe apenas para ler dados antigos criptografados com IV zero: criptografar neste modo
> lança exceção (`Options.Cryptography.AlgorithmDecryptOnly`). Para novos dados, use `GCM`.

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

### Criptografia

1. **Prefira GCM sobre CBC** - GCM fornece autenticação integrada
2. **Nunca use CBCUnsafe para novos dados** - Modo somente-descriptografia para compatibilidade com sistemas legados
3. **Prefira `SecretBase64` com chave aleatória de 32 bytes** - A chave é usada diretamente, sem derivação
4. **Gere chaves aleatórias** - Use `openssl rand -base64 32` ou `RandomNumberGenerator.GetBytes(32)`

### Data Protection

1. **Defina `ApplicationName`** - O isolamento fica igual em todas as instâncias, independente do caminho de instalação
2. **Compartilhe o key ring entre as instâncias** - Volume persistente em `KeysPath`, os callbacks `ReadKeys` e `WriteKey` ou um armazenamento via `ConfigureDataProtection`
3. **Ligue `RequirePersistentKeyStorage` em contêiner** - A aplicação deixa de subir com as chaves presas à instância
4. **Nos callbacks, só insira e leia tudo** - Chave sobrescrita, filtrada ou apagada torna ilegível o que ela protegeu
5. **Proteja as chaves em repouso** - Com `KeysPath` ou callbacks e sem certificado, as chaves ficam gravadas sem criptografia, em qualquer sistema operacional
6. **Não use para dados de longa duração** - Colunas criptografadas no banco ficam com o `ICryptographyService`

---

## ⚠️ Códigos de Erro e Soluções

| Serviço                   | Mensagem                                              | Descrição                                          | Solução                                                               | Exception             |
| ------------------------- | ----------------------------------------------------- | -------------------------------------------------- | --------------------------------------------------------------------- | --------------------- |
| `CryptographyService`     | `Options.NotConfigured`                               | `Options` não configurado                          | Configure `CryptographyOptions`                                       | `InternalServerError` |
| `CryptographyService`     | `Options.Cryptography.SecretNotConfigured`            | Nenhuma chave configurada                          | Configure `Secret` ou `SecretBase64` em `CryptographyOptions`         | `InternalServerError` |
| `CryptographyService`     | `Options.Cryptography.SecretBase64Invalid`            | `SecretBase64` não é Base64 válido                 | Forneça um valor Base64 válido em `SecretBase64`                      | `InternalServerError` |
| `CryptographyService`     | `Options.Cryptography.SecretBase64InvalidSize`        | `SecretBase64` não tem 32 bytes                    | Use uma chave de exatamente 32 bytes (AES-256)                        | `InternalServerError` |
| `CryptographyService`     | `Options.Cryptography.AlgorithmDecryptOnly`           | `CBCUnsafe` usado para criptografar                | Use `CBCUnsafe` apenas para descriptografar dados legados             | `InternalServerError` |
| `CryptographyService`     | `Cryptography.PlainTextNotProvided`                   | `PlainText` não fornecido                          | Forneça o texto plano para criptografar                               | `BadRequest`          |
| `CryptographyService`     | `Cryptography.CipherTextNotProvided`                  | `CipherText` não fornecido                         | Forneça o texto criptografado para descriptografar                    | `BadRequest`          |
| `CryptographyService`     | `Cryptography.InvalidCipherText`                      | `CipherText` inválido                              | Forneça um texto criptografado válido para descriptografar            | `BadRequest`          |
| `AddTooarkDataProtection` | `Options.DataProtection.KeyLifetimeTooShort;7`        | `KeyLifetimeDays` abaixo do mínimo                 | Use ao menos 7 dias, ou deixe nulo para os 90 dias padrão             | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateAmbiguous`         | `CertificatePath` e `CertificateThumbprint` juntos | Informe apenas um dos dois                                            | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificatePathNotConfigured` | `CertificatePassword` sem `CertificatePath`        | Informe `CertificatePath` ou remova a senha                           | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateNotFound;{valor}`  | Arquivo ou thumbprint não encontrado               | Confira o caminho, ou instale o certificado no repositório `My`       | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateInvalid`           | `.pfx` ilegível ou senha incorreta                 | Confira o arquivo e `CertificatePassword`                             | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateWithoutPrivateKey` | Certificado sem chave privada                      | Exporte o `.pfx` com a chave privada                                  | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.CertificateNotRsa`            | Certificado sem chave RSA                          | Use um certificado RSA                                                | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.KeyStorageNotConfigured`      | Trava ligada e nenhum armazenamento de chaves      | Informe `KeysPath`, os callbacks ou um armazenamento externo          | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.KeyCallbacksIncomplete`       | Só um dos callbacks `ReadKeys` e `WriteKey`        | Informe os dois callbacks                                             | `InternalServerError` |
| `AddTooarkDataProtection` | `Options.DataProtection.KeyStorageAmbiguous`          | `KeysPath` junto com `ReadKeys` e `WriteKey`       | Informe apenas um armazenamento                                       | `InternalServerError` |
| `JwtTokenService`         | `Options.NotConfigured`                               | `Options` não configurado                          | Configure `JwtOptions`                                                | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.SecretNotConfigured`                     | `Secret` não configurado                           | Configure `Secret` dentro de `JwtOptions` para token simétrico        | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.SecretTooShort`                          | `Secret` abaixo do mínimo do algoritmo             | Use ao menos 32/48/64 bytes para HS256/HS384/HS512                    | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.KeysNotConfigured`                       | `Private` e `Public` não configurado               | Configure as chaves dentro de `JwtOptions` para token assimétrico     | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.PrivateKey.InvalidSize`                  | Tamanho da chave `Private` inválido                | Use uma chave `Private` de pelo menos 2048 bits                       | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.PublicKey.InvalidSize`                   | Tamanho da chave `Public` inválido                 | Use uma chave `Public` de pelo menos 2048 bits                        | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.PrivateKey.InvalidCurve`                 | Curva da chave `Private` inválida                  | Use uma chave `Private` com a curva correta                           | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.PublicKey.InvalidCurve`                  | Curva da chave `Public` inválida                   | Use uma chave `Public` com a curva correta                            | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.InvalidKey`                              | Chave inválida                                     | [Utilize chaves válidas](#-gerando-chaves) para o algoritmo escolhido | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.AlgorithmNotSupported`                   | Algoritmo não suportado                            | [Utilize um algoritmo suportado](#-jwt---algoritmos-suportados)       | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.KeyNotConfigured;PrivateKey`             | Chave `Private` não configurado                    | Configure `PrivateKey` dentro de `JwtOptions` para gerar um token     | `InternalServerError` |
| `JwtTokenService`         | `Options.Jwt.KeyNotConfigured;PublicKey`              | Chave `Public` não configurado                     | Configure `PublicKey` dentro de `JwtOptions` para validar um token    | `InternalServerError` |
| `JwtTokenService`         | `Token.Expired`                                       | Token expirado                                     | Gere um novo token                                                    | N/A                   |
| `JwtTokenService`         | `Token.InvalidSignature`                              | Token com assinatura inválida                      | Utilize apenas token com assinatura válida                            | N/A                   |
| `JwtTokenService`         | `Token.Invalid`                                       | Token inválido                                     | Utilize apenas token válido                                           | N/A                   |
| `JwtTokenService`         | `InternalServerError`                                 | Erro interno do servidor                           | Analise os logs para mais detalhes                                    | N/A                   |

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
