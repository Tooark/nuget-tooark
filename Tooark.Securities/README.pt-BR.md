# Tooark.Securities

Biblioteca de segurança para aplicações .NET, fornecendo serviços de **criptografia AES** e **autenticação JWT** com suporte a múltiplos algoritmos.

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.md) · 🇧🇷 **Português (este arquivo)**

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
| `UserTokenDto` | Resultado da validação do token                   |

### Options

| Classe                | Descrição                                                                   |
| --------------------- | --------------------------------------------------------------------------- |
| `JwtOptions`          | Configurações do JWT (algoritmo, chaves, issuer(s), audience(s), expiração) |
| `CryptographyOptions` | Configurações de criptografia (algoritmo, secret ou secretBase64)           |

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

Para configurar os serviços de segurança, adicione as seções `Jwt` e `Cryptography` no seu arquivo `appsettings.json`:

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
public class DataProtectionService
{
    private readonly ICryptographyService _crypto;

    public DataProtectionService(ICryptographyService crypto)
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

---

## 📋 Dependências

| Pacote                                                                                                                          | Versão   | Descrição                             |
| ------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                         | 4.x      | Exceções (ex.: `BadRequestException`) |
| [`Microsoft.AspNetCore.Authentication.JwtBearer`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer) | 8.x/10.x | Autenticação JWT para ASP.NET Core    |

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

---

## ⚠️ Códigos de Erro e Soluções

| Serviço               | Mensagem                                       | Descrição                              | Solução                                                               | Exception             |
| --------------------- | ---------------------------------------------- | -------------------------------------- | --------------------------------------------------------------------- | --------------------- |
| `CryptographyService` | `Options.NotConfigured`                        | `Options` não configurado              | Configure `CryptographyOptions`                                       | `InternalServerError` |
| `CryptographyService` | `Options.Cryptography.SecretNotConfigured`     | Nenhuma chave configurada              | Configure `Secret` ou `SecretBase64` em `CryptographyOptions`         | `InternalServerError` |
| `CryptographyService` | `Options.Cryptography.SecretBase64Invalid`     | `SecretBase64` não é Base64 válido     | Forneça um valor Base64 válido em `SecretBase64`                      | `InternalServerError` |
| `CryptographyService` | `Options.Cryptography.SecretBase64InvalidSize` | `SecretBase64` não tem 32 bytes        | Use uma chave de exatamente 32 bytes (AES-256)                        | `InternalServerError` |
| `CryptographyService` | `Options.Cryptography.AlgorithmDecryptOnly`    | `CBCUnsafe` usado para criptografar    | Use `CBCUnsafe` apenas para descriptografar dados legados             | `InternalServerError` |
| `CryptographyService` | `Cryptography.PlainTextNotProvided`            | `PlainText` não fornecido              | Forneça o texto plano para criptografar                               | `BadRequest`          |
| `CryptographyService` | `Cryptography.CipherTextNotProvided`           | `CipherText` não fornecido             | Forneça o texto criptografado para descriptografar                    | `BadRequest`          |
| `CryptographyService` | `Cryptography.InvalidCipherText`               | `CipherText` inválido                  | Forneça um texto criptografado válido para descriptografar            | `BadRequest`          |
| `JwtTokenService`     | `Options.NotConfigured`                        | `Options` não configurado              | Configure `JwtOptions`                                                | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.SecretNotConfigured`              | `Secret` não configurado               | Configure `Secret` dentro de `JwtOptions` para token simétrico        | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.SecretTooShort`                   | `Secret` abaixo do mínimo do algoritmo | Use ao menos 32/48/64 bytes para HS256/HS384/HS512                    | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.KeysNotConfigured`                | `Private` e `Public` não configurado   | Configure as chaves dentro de `JwtOptions` para token assimétrico     | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.PrivateKey.InvalidSize`           | Tamanho da chave `Private` inválido    | Use uma chave `Private` de pelo menos 2048 bits                       | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.PublicKey.InvalidSize`            | Tamanho da chave `Public` inválido     | Use uma chave `Public` de pelo menos 2048 bits                        | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.PrivateKey.InvalidCurve`          | Curva da chave `Private` inválida      | Use uma chave `Private` com a curva correta                           | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.PublicKey.InvalidCurve`           | Curva da chave `Public` inválida       | Use uma chave `Public` com a curva correta                            | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.InvalidKey`                       | Chave inválida                         | [Utilize chaves válidas](#-gerando-chaves) para o algoritmo escolhido | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.AlgorithmNotSupported`            | Algoritmo não suportado                | [Utilize um algoritmo suportado](#-jwt---algoritmos-suportados)       | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.KeyNotConfigured;PrivateKey`      | Chave `Private` não configurado        | Configure `PrivateKey` dentro de `JwtOptions` para gerar um token     | `InternalServerError` |
| `JwtTokenService`     | `Options.Jwt.KeyNotConfigured;PublicKey`       | Chave `Public` não configurado         | Configure `PublicKey` dentro de `JwtOptions` para validar um token    | `InternalServerError` |
| `JwtTokenService`     | `Token.Expired`                                | Token expirado                         | Gere um novo token                                                    | N/A                   |
| `JwtTokenService`     | `Token.InvalidSignature`                       | Token com assinatura inválida          | Utilize apenas token com assinatura válida                            | N/A                   |
| `JwtTokenService`     | `Token.Invalid`                                | Token inválido                         | Utilize apenas token válido                                           | N/A                   |
| `JwtTokenService`     | `InternalServerError`                          | Erro interno do servidor               | Analise os logs para mais detalhes                                    | N/A                   |

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Securities](https://github.com/Tooark/nuget-tooark/issues).

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
