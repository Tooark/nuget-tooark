# Tooark

Biblioteca com todos os recursos e funcionalidades do Tooark voltadas para projetos .NET.

📖 **Documentação:** [Tooark no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Recursos disponíveis](#-recursos-disponíveis)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 🔧 Instalação

```bash
dotnet add package Tooark
```

O agregador traz todos os pacotes Tooark de uma vez. Instale os pacotes individualmente quando quiser
apenas parte deles — a superfície é a mesma.

A exceção são os provedores de storage e de segredos (`Tooark.Storage.Aws`, `Tooark.Storage.Gcp`,
`Tooark.Secrets.Aws`, `Tooark.Secrets.Gcp` e `Tooark.Secrets.Vault`): o agregador traz só as abstrações do
`Tooark.Storage` e do `Tooark.Secrets`, e a aplicação instala o provedor que usa.

---

## ⚙️ Configuração

As traduções acompanham o assembly, então não há nada a configurar para elas funcionarem.

Adicione a seguinte linha no seu arquivo `Program.cs`:

```csharp
// Importando o namespace necessário
using Tooark.Injections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

IConfiguration configuration = new ConfigurationBuilder()
  .Build();

// Nas suas configurações de serviços
services.AddTooarkService(configuration);
```

Essa única chamada registra `Tooark.Dtos`, `Tooark.Extensions`, `Tooark.ValueObjects`, `Tooark.Mediator`
e `Tooark.Sanitizers`. O restante depende da configuração:

- com a seção `Jwt` ou `Cryptography`, registra o `Tooark.Securities`: o serviço de token JWT e a criptografia
  (cada um quando a própria seção existe), o hash de senha e, quando a seção `DataProtection` existe, o Data
  Protection;
- com a seção `Observability`, registra o `Tooark.Observability`.

Para a aplicação falhar no startup quando uma dessas seções trouxer uma chave digitada errada, ligue a
[configuração estrita](https://github.com/Tooark/nuget-tooark/blob/main/README.pt-BR.md#️-configuração-estrita)
(`"Tooark": { "StrictConfiguration": true }`).

O que depende da sua aplicação fica de fora e é registrado à parte: a unidade de trabalho, a auditoria das
entidades no Entity Framework Core, as respostas do ASP.NET Core, a autenticação JWT Bearer, o SSO com OpenID
Connect, o cliente HTTP, o storage e os segredos. As seções abaixo mostram cada um.

### Onde os manipuladores do mediador são procurados

Sem argumento, no **assembly que chamou o `AddTooarkService`**. Numa aplicação de projeto único, é o
que você quer e não há nada a fazer.

Numa aplicação em camadas, os manipuladores costumam estar em outro projeto. Informe os assemblies:

```csharp
services.AddTooarkService(configuration, typeof(MeuHandler).Assembly);
```

Chamar o `AddTooarkMediator` depois também funciona, para acrescentar assemblies ou configurar as
opções — o registro dos manipuladores não duplica:

```csharp
using Tooark.Mediator.Injections;

services.AddTooarkService(configuration);
services.AddTooarkMediator(typeof(MeuHandler).Assembly);
```

> **Atenção ao chamar o `AddTooarkService` de dentro de uma biblioteca sua.** O assembly procurado é o
> de quem chama, então seria o da sua biblioteca, e não o da aplicação. Repasse o assembly certo.

### Unidade de trabalho

Fica de fora do `AddTooarkService`, porque depende do tipo do seu contexto do Entity Framework:

```csharp
using Tooark.Mediator.EntityFrameworkCore.Injections;

services.AddTooarkMediatorUnitOfWork<MeuDbContext>();
```

### Auditoria e exclusão lógica das entidades

Também ficam de fora do `AddTooarkService`, porque dependem do seu contexto e de quem é o usuário atual na sua
aplicação. O interceptor preenche os campos de auditoria e converte o `Remove()` em exclusão lógica:

```csharp
using Tooark.Entities.EntityFrameworkCore.Extensions;
using Tooark.Entities.EntityFrameworkCore.Injections;

services.AddTooarkEntityFrameworkCore<MeuCurrentUser>(); // MeuCurrentUser implementa ICurrentUser
services.AddDbContext<MeuDbContext>((provider, options) => options
  .UseNpgsql(connectionString)
  .AddTooarkInterceptors(provider));
```

No `OnModelCreating`, chame `modelBuilder.ApplyTooarkConventions()` por último. Os detalhes estão no
[Tooark.Entities.EntityFrameworkCore](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities.EntityFrameworkCore/README.pt-BR.md).

### Respostas do ASP.NET Core

Também ficam de fora do `AddTooarkService`, porque mudam o corpo das respostas HTTP da sua API. Registre o que
a aplicação usa:

```csharp
using Tooark.AspNetCore.Injections;

services.AddTooarkModelStateEnvelope(); // falhas de validação do [ApiController] no envelope do Tooark
services.AddTooarkProblemDetails();     // exceções do Tooark como application/problem+json (RFC 9457)

app.UseExceptionHandler();              // exigido pelo AddTooarkProblemDetails
```

Os detalhes estão no
[Tooark.AspNetCore](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.AspNetCore/README.pt-BR.md).

### Autenticação JWT Bearer

O `AddTooarkService` registra o serviço que emite e valida tokens, mas não o esquema de autenticação. Quem
protege endpoints com `[Authorize]` registra o esquema `Bearer`, que lê a mesma seção `Jwt`:

```csharp
using Tooark.Securities.Injections;

services.AddTooarkJwtBearer(configuration);

app.UseAuthentication();
app.UseAuthorization();
```

### SSO com OpenID Connect

Também fica de fora do `AddTooarkService`: o mesmo provedor pode servir a um login interativo (handler
`OpenIdConnect`) ou a uma API (handler `JwtBearer`), e essa escolha é sua. Registre explicitamente:

```csharp
using Tooark.Securities.OpenId.Injections;

services.AddTooarkEntraSso(configuration);   // lê OpenId:Entra
services.AddTooarkGoogleSso(configuration);  // lê OpenId:Google
```

### Cliente HTTP

Também fica de fora do `AddTooarkService`: nem toda aplicação chama outros serviços, e o registro cria um cliente
com resiliência. Registre quando a aplicação precisar:

```csharp
using Tooark.Http.Injections;

services.AddTooarkHttpClient(configuration); // lê HttpClient
```

Os detalhes, inclusive a guarda de saída contra SSRF, estão no
[Tooark.Http](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Http/README.pt-BR.md).

### Storage

Também fica de fora do `AddTooarkService`, e o provedor não vem no agregador: instale o pacote da nuvem que
você usa e registre-o:

```csharp
using Tooark.Storage.Aws.Injections;

services.AddTooarkStorageAws(configuration); // lê Storage
```

### Segredos

O cofre é uma fonte de configuração, adicionada antes de a configuração chegar ao `AddTooarkService`, e o
provedor não vem no agregador:

```csharp
using Tooark.Secrets.Aws.Injections;

IConfiguration configuration = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json")
  .AddTooarkSecretsAws() // lê Secrets do appsettings.json
  .Build();

services.AddTooarkService(configuration);
```

---

## ✨ Recursos disponíveis

### [Tooark.Attributes](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Attributes/README.pt-BR.md)

Descrição: Este pacote fornece atributos personalizados para uso em projetos .NET.

### [Tooark.AspNetCore](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.AspNetCore/README.pt-BR.md)

Descrição: Este pacote reúne os recursos que dependem do ASP.NET Core: a leitura dos erros do `ModelState`, o envelope das falhas de validação e as respostas Problem Details (RFC 9457) para as exceções do Tooark.

### [Tooark.Dtos](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Dtos/README.pt-BR.md)

Descrição: Este pacote contém objetos de transferência de dados (DTOs) para facilitar a comunicação entre camadas da aplicação.

### [Tooark.Entities](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities/README.pt-BR.md)

Descrição: Este pacote define as entidades do domínio utilizadas na aplicação.

### [Tooark.Entities.EntityFrameworkCore](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities.EntityFrameworkCore/README.pt-BR.md)

Descrição: Este pacote liga as entidades ao Entity Framework Core: preenche os campos de auditoria, converte o `Remove()` em exclusão lógica e aplica o filtro de excluídos e o token de concorrência.

### [Tooark.Enums](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Enums/README.pt-BR.md)

Descrição: Este pacote contém definições de enums utilizados em várias partes da aplicação.

### [Tooark.Exceptions](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Exceptions/README.pt-BR.md)

Descrição: Este pacote fornece exceções personalizadas para uso em projetos .NET.

### [Tooark.Extensions](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Extensions/README.pt-BR.md)

Descrição: Este pacote fornece métodos de extensão para tipos e classes comuns do .NET.

### [Tooark.Http](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Http/README.pt-BR.md)

Descrição: Este pacote faz as chamadas HTTP de saída: um serviço JSON fino sobre o `IHttpClientFactory`, com resiliência, erros no padrão do Tooark e uma guarda de saída contra SSRF.

### [Tooark.Notifications](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Notifications/README.pt-BR.md)

Descrição: Este pacote oferece funcionalidades para gerenciamento de notificações e mensagens na aplicação.

### [Tooark.Mediator.Abstractions](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.Abstractions/README.pt-BR.md)

Descrição: Este pacote fornece contratos base do padrão Mediator para uso em projetos .NET.

### [Tooark.Mediator](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator/README.pt-BR.md)

Descrição: Este pacote oferece uma implementação concreta do padrão Mediator, facilitando a comunicação entre componentes da aplicação.

### [Tooark.Mediator.EntityFrameworkCore](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.EntityFrameworkCore/README.pt-BR.md)

Descrição: Este pacote move a persistência do Entity Framework Core para o pipeline do Mediator, mantendo os handlers livres de SaveChanges.

### [Tooark.Observability](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Observability/README.pt-BR.md)

Descrição: Este pacote integra o OpenTelemetry (traces, métricas e logs) pela configuração, com a identidade do serviço resolvida e a instrumentação pronta para injeção.

### [Tooark.Sanitizers](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Sanitizers/README.pt-BR.md)

Descrição: Este pacote sanitiza HTML com lista de permissão, URLs com lista de esquemas e o conteúdo do editor `@tooark/wysiwyg`, com as mesmas regras que o componente aplica no navegador.

### [Tooark.Secrets](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md)

Descrição: Este pacote carrega segredos e parâmetros de cofres (AWS, Google Cloud, Vault e OpenBao) na configuração e lê segredos em execução. Os provedores, `Tooark.Secrets.Aws`, `Tooark.Secrets.Gcp` e `Tooark.Secrets.Vault`, são instalados à parte.

### [Tooark.Securities](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.pt-BR.md)

Descrição: Este pacote oferece JWT (emissão, validação e autenticação Bearer), criptografia AES com rotação de chaves, hash de senha, tokens seguros, TOTP com códigos de recuperação e a configuração do Data Protection.

### [Tooark.Securities.OpenId](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities.OpenId/README.pt-BR.md)

Descrição: Este pacote configura os handlers nativos de OpenID Connect do ASP.NET Core, com presets de SSO para Microsoft Entra ID e Google.

### [Tooark.Storage](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.pt-BR.md)

Descrição: Este pacote define o contrato de storage de objetos em nuvem (upload, download, exclusão, metadados e URL assinada). Os provedores, `Tooark.Storage.Aws` e `Tooark.Storage.Gcp`, são instalados à parte.

### [Tooark.Utils](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Utils/README.pt-BR.md)

Descrição: Este pacote contém utilitários e funções auxiliares para diversas operações.

### [Tooark.Validations](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Validations/README.pt-BR.md)

Descrição: Este pacote fornece funcionalidades de validação para dados e entidades.

### [Tooark.ValueObjects](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.ValueObjects/README.pt-BR.md)

Descrição: Este pacote define objetos de valor utilizados na aplicação.

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
