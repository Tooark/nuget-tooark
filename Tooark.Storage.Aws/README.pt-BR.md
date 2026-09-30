# Tooark.Storage.Aws

Provedor do [`Tooark.Storage`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Storage) sobre o
**Amazon S3** e serviços compatíveis com a API do S3, como MinIO, LocalStack e Cloudflare R2. Implementa o
`IStorageService` com upload, download, exclusão, metadados e **URLs pré-assinadas** de leitura e escrita.

O contrato, as opções comuns e os erros compartilhados estão no
[README do `Tooark.Storage`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.pt-BR.md). Este
README trata do que é próprio da AWS.

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage.Aws/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Credenciais](#-credenciais)
- [Serviços compatíveis](#-serviços-compatíveis)
- [Comportamento no S3](#-comportamento-no-s3)
- [Dependências](#-dependências)
- [Boas Práticas](#-boas-práticas)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Storage.Aws
```

---

## ⚙️ Configuração

### appsettings.json

```json
{
  "Storage": {
    "Bucket": "minha-aplicacao-arquivos",
    "Region": "sa-east-1",
    "SignedUrlExpirationMinutes": 15
  }
}
```

### Program.cs

```csharp
using Tooark.Storage.Aws.Injections;

builder.Services.AddTooarkStorageAws(builder.Configuration);

// Com ajuste no código, aplicado por cima da seção
builder.Services.AddTooarkStorageAws(builder.Configuration, options => options.Bucket = "outro-bucket");
```

O registro valida as opções e falha no startup quando elas estão incompletas. O cliente do S3 é registrado como
`IAmazonS3`, com uma instância para toda a aplicação.

### Propriedades de `AwsStorageOptions`

Além de `Bucket` e `SignedUrlExpirationMinutes`, as
[opções comuns](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.pt-BR.md#propriedades-de-storageoptions):

| Propriedade      | Tipo      | Padrão  | Descrição                                                                                             |
| ---------------- | --------- | ------- | ----------------------------------------------------------------------------------------------------- |
| `Region`         | `string?` | —       | Região do bucket, como `sa-east-1`. Sem ela, vale a região da cadeia padrão da AWS                    |
| `AccessKey`      | `string?` | —       | Chave de acesso. Informe junto com `SecretKey`, ou nenhuma das duas                                   |
| `SecretKey`      | `string?` | —       | Chave secreta. Informe junto com `AccessKey`, ou nenhuma das duas                                     |
| `SessionToken`   | `string?` | —       | Token de sessão, para credenciais temporárias. Só vale com as duas chaves                             |
| `ServiceUrl`     | `string?` | —       | Endereço de um serviço compatível com o S3. URL `http` ou `https` absoluta                            |
| `ForcePathStyle` | `bool`    | `false` | Coloca o bucket no caminho da URL (`host/bucket/chave`), como costumam exigir os serviços compatíveis |

---

## 🔑 Credenciais

Sem `AccessKey` e `SecretKey`, as credenciais vêm da **cadeia padrão da AWS**, na ordem do SDK: variáveis de ambiente
(`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`), perfil do `~/.aws/credentials`, role da tarefa no ECS, role do pod no
EKS e perfil da instância no EC2. **É o caminho recomendado em produção**: nenhuma chave fica na configuração, e a
role é trocada pela AWS sem intervenção.

Com as chaves nas opções, elas têm prioridade. Uma chave sem a outra falha no registro: cair na cadeia padrão em
silêncio faria a aplicação usar outra identidade.

A identidade precisa de `s3:PutObject`, `s3:GetObject` e `s3:DeleteObject` nos objetos do bucket, e de
`s3:ListBucket` no bucket. Sem o `s3:ListBucket`, o S3 responde 403, e não 404, para um objeto que não existe: o
`ExistsAsync` lança `Storage.AccessDenied` em vez de devolver `false`, e o `DownloadAsync` lança o mesmo erro em vez de
`Storage.ObjectNotFound`.

### Cliente próprio

Se a aplicação registra o próprio `IAmazonS3` antes, por exemplo com o `AddAWSService<IAmazonS3>()` do
`AWSSDK.Extensions.NETCore.Setup`, o dela é usado. Nesse caso, `Region`, credenciais, `ServiceUrl` e
`ForcePathStyle` das opções não se aplicam; `Bucket` e `SignedUrlExpirationMinutes` continuam valendo.

---

## 🔌 Serviços compatíveis

`ServiceUrl` aponta o cliente para outro serviço com a API do S3. Com ele, o endereço define o destino e `Region`
vale só para a assinatura das requisições. O MinIO local, por exemplo:

```json
{
  "Storage": {
    "Bucket": "arquivos",
    "ServiceUrl": "http://localhost:9000",
    "ForcePathStyle": true,
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin"
  }
}
```

Guarde as chaves de desenvolvimento no `appsettings.Development.json` ou no Secret Manager, e nunca no
`appsettings.json` versionado.

---

## 🪣 Comportamento no S3

- **Download em streaming** — `DownloadAsync` devolve o stream da resposta do S3, sem carregar o arquivo em memória.
  Fechar o stream libera a conexão.
- **Exclusão com uma consulta antes** — o S3 responde sucesso ao excluir um objeto que não existe. Para devolver
  `false` nesse caso, `DeleteAsync` consulta os metadados antes, o que custa uma chamada a mais.
- **Bucket inexistente na consulta de metadados** — a resposta da consulta não tem corpo, então `ExistsAsync` e
  `GetInfoAsync` não distinguem bucket e objeto inexistentes: os dois dão `false` e `null`. Nas demais operações, o
  bucket inexistente lança `Storage.BucketNotFound`.
- **URL pré-assinada com Signature V4** — até 7 dias. Com credenciais temporárias (role), a URL deixa de valer quando
  a sessão expira, mesmo antes da validade pedida.
- **Upload** — o stream de quem chamou não é fechado. Um stream que permite busca é enviado desde o início, e o
  tamanho volta em `Size`.

---

## 📋 Dependências

| Pacote                                                                                                                                          | Versão   | Descrição                          |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------------------------------- |
| [`Tooark.Storage`](https://www.nuget.org/packages/Tooark.Storage)                                                                               | 4.x      | Contrato e validações comuns       |
| [`AWSSDK.S3`](https://www.nuget.org/packages/AWSSDK.S3)                                                                                         | 4.x      | Cliente do Amazon S3               |
| [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions)   | 8.x/10.x | Leitura das opções da configuração |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Registro no container              |

---

## 🎯 Boas Práticas

1. **Role em vez de chave** — em ECS, EKS ou EC2, deixe `AccessKey` e `SecretKey` vazias e dê a permissão à role.
2. **Permissão só no bucket da aplicação** — restrinja a política ao ARN do bucket, e não a `s3:*` em `*`.
3. **Bucket privado** — mantenha o Block Public Access ligado e entregue os arquivos por URL pré-assinada.
4. **CORS para o upload direto** — a URL de escrita só funciona no navegador com uma regra de CORS que libere `PUT`
   para a origem da página.

---

## ⚠️ Códigos de Erro e Soluções

Os erros de configuração são `InternalServerErrorException` lançadas **no registro** (`AddTooarkStorageAws`). Os
erros das operações estão no
[README do `Tooark.Storage`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.pt-BR.md#️-códigos-de-erro-e-soluções).

| Mensagem                                        | Descrição                                             | Solução                                                      |
| ----------------------------------------------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| `Options.Storage.Aws.CredentialsIncomplete`     | Só uma das chaves, ou token de sessão sem as chaves   | Informe as duas chaves, ou nenhuma para usar a cadeia padrão |
| `Options.Storage.Aws.ServiceUrlInvalid;{valor}` | `ServiceUrl` não é uma URL `http` ou `https` absoluta | Use o endereço completo, como `http://localhost:9000`        |

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
