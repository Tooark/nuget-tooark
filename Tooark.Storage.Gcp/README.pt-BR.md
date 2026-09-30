# Tooark.Storage.Gcp

Provedor do [`Tooark.Storage`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Storage) sobre o
**Google Cloud Storage**. Implementa o `IStorageService` com upload, download, exclusão, metadados e **URLs
assinadas V4** de leitura e escrita.

O contrato, as opções comuns e os erros compartilhados estão no
[README do `Tooark.Storage`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.pt-BR.md). Este
README trata do que é próprio do Google Cloud.

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage.Gcp/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Credenciais e assinatura de URL](#-credenciais-e-assinatura-de-url)
- [Comportamento no Google Cloud Storage](#-comportamento-no-google-cloud-storage)
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
dotnet add package Tooark.Storage.Gcp
```

---

## ⚙️ Configuração

### appsettings.json

No Cloud Run, no GKE ou no Compute Engine, basta o bucket: a credencial vem da conta de serviço do ambiente.

```json
{
  "Storage": {
    "Bucket": "minha-aplicacao-arquivos",
    "SignedUrlExpirationMinutes": 15
  }
}
```

### Program.cs

```csharp
using Tooark.Storage.Gcp.Injections;

builder.Services.AddTooarkStorageGcp(builder.Configuration);

// Com ajuste no código, aplicado por cima da seção
builder.Services.AddTooarkStorageGcp(builder.Configuration, options => options.Bucket = "outro-bucket");
```

O registro valida as opções e falha no startup quando elas são inválidas. A credencial é carregada no primeiro uso, e
o cliente é registrado como `StorageClient`, com uma instância para toda a aplicação.

### Propriedades de `GcpStorageOptions`

Além de `Bucket` e `SignedUrlExpirationMinutes`, as
[opções comuns](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.pt-BR.md#propriedades-de-storageoptions):

| Propriedade       | Tipo      | Padrão | Descrição                                                                                           |
| ----------------- | --------- | ------ | --------------------------------------------------------------------------------------------------- |
| `CredentialsJson` | `string?` | —      | Conteúdo da chave da conta de serviço, em JSON. Pensado para um cofre que entrega o JSON como texto |
| `CredentialsPath` | `string?` | —      | Caminho do arquivo de chave da conta de serviço                                                     |

Informe uma das duas, ou nenhuma para usar o Application Default Credentials. As duas juntas falham no registro.

---

## 🔑 Credenciais e assinatura de URL

Sem `CredentialsJson` e `CredentialsPath`, a credencial vem do **Application Default Credentials** (ADC): a variável
`GOOGLE_APPLICATION_CREDENTIALS`, o `gcloud auth application-default login` ou a conta de serviço do Cloud Run, do
GKE ou do Compute Engine. **É o caminho recomendado em produção**: nenhuma chave fica na configuração.

Com `CredentialsJson` ou `CredentialsPath`, **só chave de conta de serviço** é aceita. Carregar qualquer tipo de
credencial de um JSON externo é o que o Google desaconselha, porque o JSON pode apontar para outra origem de token.

A assinatura da URL depende da credencial:

| Credencial                                           | Assina URL?                                                                                 |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| Chave de conta de serviço (`CredentialsJson`/`Path`) | Sim, localmente, com a chave privada                                                        |
| Conta de serviço do Cloud Run, GKE ou Compute Engine | Sim, pela API IAM Credentials (`signBlob`)                                                  |
| `gcloud auth application-default login` (usuário)    | Não: `GetSignedUrlAsync` lança `Storage.SigningNotSupported`; as demais operações funcionam |

Para assinar com a conta de serviço do ambiente, ela precisa do papel **Service Account Token Creator**
(`roles/iam.serviceAccountTokenCreator`) sobre si mesma, e a API IAM Service Account Credentials precisa estar
ativa no projeto.

A identidade precisa de **Storage Object Admin** (`roles/storage.objectAdmin`) no bucket, ou das permissões
`storage.objects.create`, `storage.objects.get` e `storage.objects.delete`.

### Cliente próprio

Se a aplicação registra o próprio `StorageClient` antes, o dela é usado nas operações, e a credencial das opções vale
só para assinar URLs.

---

## 📁 Comportamento no Google Cloud Storage

- **Download em memória** — o cliente do Google escreve o objeto num stream de destino, então `DownloadAsync`
  devolve um `MemoryStream` com o arquivo inteiro. Para arquivos grandes, entregue uma URL assinada de leitura.
- **Exclusão em uma chamada** — `DeleteAsync` devolve `false` quando o objeto não existia, sem consulta antes.
- **Bucket e objeto inexistentes** — o Google responde 404 para os dois, e só a mensagem os diferencia. O serviço
  lança `Storage.BucketNotFound` para o bucket e trata o objeto como inexistente.
- **URL assinada V4** — até 7 dias, com o verbo (`GET` ou `PUT`) dentro da assinatura: a URL de leitura não serve
  para escrever.
- **Upload** — o stream de quem chamou não é fechado, e o tamanho volta em `Size`, informado pelo Google.

---

## 📋 Dependências

| Pacote                                                                                                                                          | Versão   | Descrição                          |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------------------------------- |
| [`Tooark.Storage`](https://www.nuget.org/packages/Tooark.Storage)                                                                               | 4.x      | Contrato e validações comuns       |
| [`Google.Cloud.Storage.V1`](https://www.nuget.org/packages/Google.Cloud.Storage.V1)                                                             | 5.x      | Cliente do Google Cloud Storage    |
| [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions)   | 8.x/10.x | Leitura das opções da configuração |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Registro no container              |

---

## 🎯 Boas Práticas

1. **Conta de serviço do ambiente em vez de chave** — no Cloud Run e no GKE, deixe as credenciais vazias e dê os
   papéis à conta de serviço da aplicação.
2. **Chave em cofre, nunca no repositório** — quando precisar de chave, entregue o JSON pelo Secret Manager em
   `CredentialsJson` (`Storage__CredentialsJson`).
3. **Uniform bucket-level access** — controle o acesso pelo IAM do bucket, e não por ACL de objeto.
4. **CORS para o upload direto** — a URL de escrita só funciona no navegador com uma configuração de CORS no bucket
   que libere `PUT` para a origem da página.

---

## ⚠️ Códigos de Erro e Soluções

Os erros de configuração são `InternalServerErrorException`. Os de opções saem **no registro**
(`AddTooarkStorageGcp`); os de credencial, **no primeiro uso**, quando ela é carregada. Os erros comuns das operações
estão no
[README do `Tooark.Storage`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.pt-BR.md#️-códigos-de-erro-e-soluções).

| Mensagem                                            | Quando       | Descrição                                         | Solução                                                                      |
| --------------------------------------------------- | ------------ | ------------------------------------------------- | ---------------------------------------------------------------------------- |
| `Options.Storage.Gcp.CredentialsAmbiguous`          | Registro     | `CredentialsJson` e `CredentialsPath` juntos      | Informe só um dos dois                                                       |
| `Options.Storage.Gcp.CredentialsNotFound;{caminho}` | Registro     | O arquivo de `CredentialsPath` não existe         | Confira o caminho e o volume montado                                         |
| `Options.Storage.Gcp.CredentialsInvalid`            | Primeiro uso | O JSON não é uma chave de conta de serviço válida | Use a chave JSON gerada para a conta de serviço                              |
| `Options.Storage.Gcp.CredentialsUnavailable`        | Primeiro uso | Sem chave nas opções e sem ADC no ambiente        | Informe a chave ou configure o Application Default Credentials               |
| `Storage.SigningNotSupported`                       | URL assinada | A credencial não assina URLs                      | Use uma chave de conta de serviço ou conceda o Service Account Token Creator |

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
