# Tooark.Secrets

Biblioteca com as abstrações de **segredos e parâmetros** da família Tooark. O cofre alimenta o `IConfiguration`,
então os pacotes que já leem as opções da configuração (Storage, Securities, OpenId, Observability) passam a receber
os valores do cofre **sem mudar nada**. Para o que só se conhece em execução, há o `ISecretService`, com cache.

O pacote **não tem SDK de nuvem**: cada cofre fica no próprio pacote.

| Pacote                                                                                          | Segredos                  | Parâmetros               | Credencial do cofre             |
| ----------------------------------------------------------------------------------------------- | ------------------------- | ------------------------ | ------------------------------- |
| [`Tooark.Secrets.Aws`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets.Aws)     | AWS Secrets Manager       | AWS Parameter Store      | Cadeia padrão da AWS (role)     |
| [`Tooark.Secrets.Gcp`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets.Gcp)     | Google Secret Manager     | Google Parameter Manager | Application Default Credentials |
| [`Tooark.Secrets.Vault`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets.Vault) | KV v2 do Vault ou OpenBao | o mesmo KV               | Token, AppRole ou Kubernetes    |

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Visão Geral](#-visão-geral)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Dos nomes do cofre às chaves](#-dos-nomes-do-cofre-às-chaves)
- [Leitura em execução](#-leitura-em-execução)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Boas Práticas](#-boas-práticas)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 📖 Visão Geral

- **Fonte de configuração** — o cofre entra no `IConfiguration` como qualquer outra fonte, e vale por cima das
  anteriores (`appsettings.json`, variáveis de ambiente). `Storage:SecretKey`, `Jwt:Secret` ou
  `OpenId:Entra:ClientSecret` saem do cofre, e os `AddTooark*` continuam lendo e validando as opções como sempre.
- **`ISecretService`** — lê um segredo pelo nome em tempo de execução, como a chave de API de um tenant, com cache.
  Lê só segredos: parâmetros são configuração e entram pela fonte.
- **Leitura no startup** — a fonte lê o cofre uma vez, quando a configuração é montada. Os pacotes Tooark validam e
  guardam as opções no startup, então rotacionar um segredo usado por eles pede reinício ou novo deploy. O
  `ISecretService` lê de novo quando o cache expira.
- **A credencial do cofre vem do ambiente** — role da AWS, Application Default Credentials do Google ou auth
  Kubernetes/AppRole no Vault. Uma chave de acesso ao cofre guardada no `appsettings.json` só mudaria o segredo de
  lugar.
- **Falha no startup** — cofre inacessível, sem permissão ou fora do tempo limite impede a aplicação de subir, salvo
  com `Optional`.

---

## 🔧 Instalação

Instale o pacote do cofre, que traz este:

```bash
dotnet add package Tooark.Secrets.Aws
# ou
dotnet add package Tooark.Secrets.Gcp
# ou
dotnet add package Tooark.Secrets.Vault
```

O agregador `Tooark` traz só este pacote, com as abstrações, e nenhum provedor.

---

## ⚙️ Configuração

### appsettings.json

As opções ficam na seção `Secrets`, junto com as do cofre
([AWS](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Aws/README.pt-BR.md#️-configuração),
[Google Cloud](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Gcp/README.pt-BR.md#️-configuração),
[Vault](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Vault/README.pt-BR.md#️-configuração)):

```json
{
  "Secrets": {
    "SecretsPrefix": "arkuest/prod",
    "TimeoutSeconds": 30,
    "CacheMinutes": 5
  }
}
```

### Program.cs

A fonte vai no `IConfigurationBuilder`, **depois** das outras fontes, e antes dos `AddTooark*` que leem as opções:

```csharp
using Tooark.Secrets.Aws.Injections;

var builder = WebApplication.CreateBuilder(args);

// O cofre entra por cima do appsettings.json e das variáveis de ambiente
builder.Configuration.AddTooarkSecretsAws();

// Storage, Securities e os demais já recebem os valores do cofre
builder.Services.AddTooarkStorageAws(builder.Configuration);

// Opcional: leitura em execução, com cache
builder.Services.AddTooarkSecretsAws(builder.Configuration);
```

As opções da fonte vêm da seção `Secrets` das fontes já adicionadas, e o ajuste no código vale por cima:
`AddTooarkSecretsAws(options => options.SecretsPrefix = "arkuest/prod")`.

### Propriedades de `SecretsOptions`

| Propriedade      | Tipo   | Padrão  | Descrição                                                                           |
| ---------------- | ------ | ------- | ----------------------------------------------------------------------------------- |
| `ExpandJson`     | `bool` | `false` | Um valor que é objeto JSON vira chaves filhas. Veja [ExpandJson](#expandjson)       |
| `Optional`       | `bool` | `false` | Ignora a falha ao carregar a fonte, e a aplicação sobe sem as chaves do cofre       |
| `TimeoutSeconds` | `int`  | `30`    | Tempo máximo para carregar a fonte no startup                                       |
| `CacheMinutes`   | `int`  | `5`     | Tempo que um segredo lido pelo `ISecretService` fica em cache. Zero desliga o cache |

---

## 🔑 Dos nomes do cofre às chaves

Cada segredo ou parâmetro abaixo do prefixo configurado vira uma chave. O nome perde o prefixo, e `/` e `__` viram
`:`:

| Cofre                 | Prefixo         | Nome no cofre                                       | Chave                       |
| --------------------- | --------------- | --------------------------------------------------- | --------------------------- |
| AWS Secrets Manager   | `arkuest/prod`  | `arkuest/prod/Jwt/Secret`                           | `Jwt:Secret`                |
| AWS Parameter Store   | `/arkuest/prod` | `/arkuest/prod/Storage/Bucket`                      | `Storage:Bucket`            |
| Google Secret Manager | `arkuest-prod`  | `arkuest-prod__OpenId__Entra__ClientSecret`         | `OpenId:Entra:ClientSecret` |
| Vault/OpenBao (KV v2) | `arkuest/prod`  | campo `SecretKey` do segredo `arkuest/prod/Storage` | `Storage:SecretKey`         |

- O prefixo respeita o limite de um nível: `arkuest/prod` não pega `arkuest/production`.
- O Google não aceita `/` nem `:` no nome, então lá os níveis usam `__`, como nas variáveis de ambiente.
- No Vault, cada campo de um segredo é uma chave, e os campos do segredo no próprio caminho ficam na raiz.
- Com o mesmo nome num segredo e num parâmetro, o segredo vence.

### ExpandJson

Desligado, o padrão, o valor fica como texto, mesmo quando é JSON. É o que serve para **um segredo por chave**,
inclusive para uma chave de conta de serviço do Google em `Storage:CredentialsJson`.

Ligado, um valor que é objeto JSON vira chaves filhas. É o que serve para **um documento com várias chaves**, como o
segredo "chave e valor" do Secrets Manager:

```json
{ "Jwt": { "Secret": "..." }, "Storage__SecretKey": "...", "OpenId": { "Entra": { "ClientSecret": "..." } } }
```

Guardado no segredo `arkuest/prod`, o próprio prefixo, ele vira `Jwt:Secret`, `Storage:SecretKey` e
`OpenId:Entra:ClientSecret`. O texto dentro do documento nunca é lido de novo como JSON, então uma chave de conta de
serviço guardada ali como texto continua inteira. A expansão vale para **todo** valor da fonte: com um segredo por
chave, deixe-a desligada.

### O que os pacotes Tooark leem do cofre

Qualquer chave de configuração pode vir do cofre. As sensíveis da família:

| Pacote                     | Chaves                                                                                      |
| -------------------------- | ------------------------------------------------------------------------------------------- |
| `Tooark.Storage.Aws`       | `Storage:AccessKey`, `Storage:SecretKey`, `Storage:SessionToken`                            |
| `Tooark.Storage.Gcp`       | `Storage:CredentialsJson`                                                                   |
| `Tooark.Securities`        | `Jwt:Secret`, `Jwt:PrivateKey`, `Cryptography:Secret`, `DataProtection:CertificatePassword` |
| `Tooark.Securities.OpenId` | `OpenId:{provedor}:ClientSecret`                                                            |
| `Tooark.Observability`     | os `Headers` do OTLP, com a chave do coletor                                                |

---

## 🔍 Leitura em execução

O `ISecretService` lê um segredo pelo nome no cofre. O valor fica em cache pelo `CacheMinutes`; um segredo
inexistente não entra no cache, para ser lido assim que for criado.

| Método                       | Retorno   | Descrição                                                                           |
| ---------------------------- | --------- | ----------------------------------------------------------------------------------- |
| `GetAsync(name)`             | `string?` | Valor atual do segredo, ou nulo quando ele não existe                               |
| `GetFieldAsync(name, field)` | `string?` | Campo de um segredo JSON, com a caixa exata; nulo quando o segredo ou o campo falta |

No Vault, o valor é o objeto JSON com os campos do segredo, e o `GetFieldAsync` lê um deles. Os parâmetros não
passam pelo serviço: são configuração.

```csharp
using Tooark.Secrets.Interfaces;

public sealed class TenantService(ISecretService secrets)
{
  public Task<string?> ChaveDaApi(string tenant, CancellationToken cancellationToken) =>
    secrets.GetFieldAsync($"arkuest/tenants/{tenant}", "ApiKey", cancellationToken);
}
```

---

## 📝 Exemplos de Uso

### Um documento com todas as chaves (AWS)

```json
{ "Secrets": { "SecretsPrefix": "arkuest/prod", "ExpandJson": true } }
```

O segredo `arkuest/prod` guarda o documento JSON, e cada campo vira uma chave.

### Um segredo por chave (Google Cloud)

```json
{ "Secrets": { "ProjectId": "arkuest", "SecretsPrefix": "arkuest-prod" } }
```

Os segredos `arkuest-prod__Jwt__Secret` e `arkuest-prod__Storage__CredentialsJson` viram `Jwt:Secret` e
`Storage:CredentialsJson`.

### Fonte opcional em desenvolvimento

```csharp
builder.Configuration.AddTooarkSecretsVault(options => options.Optional = builder.Environment.IsDevelopment());
```

---

## 📋 Dependências

| Pacote                                                                                                                                        | Versão   | Descrição                                 |
| --------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ----------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                       | 4.x      | Exceções (`InternalServerErrorException`) |
| [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions) | 8.x/10.x | Abstrações de configuração e binder       |

O pacote roda no runtime base do .NET, sem o ASP.NET Core e sem SDK de nuvem.

---

## 🎯 Boas Práticas

1. **Nada sensível no `appsettings.json`** — com o cofre, o arquivo versionado fica só com o que não é segredo:
   região, bucket, prefixo.
2. **Identidade do ambiente para o cofre** — role, conta de serviço ou auth Kubernetes; a credencial do cofre não pode
   vir do cofre.
3. **Um prefixo por ambiente** — `arkuest/prod`, `arkuest/homolog`: a permissão da aplicação fica restrita ao dela.
4. **Rotação com reinício** — os pacotes Tooark leem as opções no startup; troque o segredo e faça um novo deploy ou
   reinicie as instâncias.
5. **`Optional` só onde faz sentido** — em produção, subir sem os segredos costuma ser pior do que não subir.

---

## ⚠️ Códigos de Erro e Soluções

| Mensagem                              | Exceção                        | Descrição                                          | Solução                                                |
| ------------------------------------- | ------------------------------ | -------------------------------------------------- | ------------------------------------------------------ |
| `Secrets.LoadFailed;{cofre}`          | `InternalServerErrorException` | A fonte não carregou: acesso, rede ou tempo limite | Veja a `InnerException`; confira a identidade e a rede |
| `Secrets.NameRequired`                | `BadRequestException`          | Nome do segredo em branco                          | Informe o nome                                         |
| `Secrets.FieldRequired`               | `BadRequestException`          | Campo em branco no `GetFieldAsync`                 | Informe o campo                                        |
| `Secrets.NotJsonObject;{nome}`        | `InternalServerErrorException` | `GetFieldAsync` num segredo que não é objeto JSON  | Use `GetAsync`, ou guarde o segredo como objeto        |
| `Secrets.AccessDenied`                | `InternalServerErrorException` | A identidade não pode ler o segredo                | Conceda a permissão de leitura                         |
| `Secrets.OperationFailed`             | `InternalServerErrorException` | Falha do cofre ou da rede                          | Veja a `InnerException`                                |
| `Options.Secrets.TimeoutInvalid`      | `InternalServerErrorException` | `TimeoutSeconds` menor que 1                       | Use pelo menos 1 segundo                               |
| `Options.Secrets.CacheMinutesInvalid` | `InternalServerErrorException` | `CacheMinutes` negativo                            | Use zero ou mais                                       |
| `Options.Secrets.SourceNotConfigured` | `InternalServerErrorException` | Fonte de configuração sem prefixo ou caminho       | Informe o prefixo ou o caminho do cofre                |

Os erros próprios de cada cofre estão no README dele.

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
