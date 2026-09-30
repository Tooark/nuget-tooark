# Tooark.Storage

Biblioteca com as abstrações de **storage de objetos em nuvem** da família Tooark: o contrato `IStorageService`
(upload, download, exclusão, metadados e URL assinada), o `StorageObjectDto`, as opções comuns e a classe base dos
provedores. O pacote **não tem SDK de nuvem**: cada provedor fica no próprio pacote, e a aplicação carrega só o SDK
da nuvem que usa.

| Pacote                                                                                      | Provedor                                                 | SDK                       |
| ------------------------------------------------------------------------------------------- | -------------------------------------------------------- | ------------------------- |
| [`Tooark.Storage.Aws`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Storage.Aws) | Amazon S3 e serviços compatíveis (MinIO, LocalStack, R2) | `AWSSDK.S3`               |
| [`Tooark.Storage.Gcp`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Storage.Gcp) | Google Cloud Storage                                     | `Google.Cloud.Storage.V1` |

O Azure Blob Storage está previsto como `Tooark.Storage.Azure`.

📖 **Documentação:** [Tooark.Storage no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.storage.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Visão Geral](#-visão-geral)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Componentes](#-componentes)
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

- **Um contrato para qualquer nuvem** — o código da aplicação depende só de `IStorageService`. Trocar a AWS pelo
  Google Cloud muda o registro no `Program.cs`, e não o código que envia e baixa arquivos.
- **URL assinada de leitura e de escrita** — a de leitura entrega um arquivo privado sem expor o bucket; a de escrita
  deixa o navegador enviar o arquivo direto ao storage, sem passar pela API.
- **Erros do Tooark** — objeto inexistente é `NotFoundException`; bucket inexistente, acesso negado e falha do
  provedor são `InternalServerErrorException`, com a exceção do SDK preservada em `InnerException`.
- **Validação antes do provedor** — chave em branco ou longa demais, bucket ausente e validade fora do intervalo são
  recusados antes de qualquer chamada à nuvem.
- **Um provedor por aplicação** — as opções ficam na seção `Storage`, e cada operação pode apontar para outro bucket.

---

## 🔧 Instalação

Instale o pacote do provedor, que traz este:

```bash
dotnet add package Tooark.Storage.Aws
# ou
dotnet add package Tooark.Storage.Gcp
```

O agregador `Tooark` traz só este pacote, com as abstrações, e nenhum provedor: uma aplicação usa uma nuvem, e não
faz sentido carregar os SDKs das duas em todo consumidor do agregador.

---

## ⚙️ Configuração

### appsettings.json

As opções comuns ficam na seção `Storage`, junto com as do provedor
([AWS](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage.Aws/README.pt-BR.md#️-configuração),
[Google Cloud](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Storage.Gcp/README.pt-BR.md#️-configuração)):

```json
{
  "Storage": {
    "Bucket": "minha-aplicacao-arquivos",
    "SignedUrlExpirationMinutes": 15
  }
}
```

### Propriedades de `StorageOptions`

| Propriedade                  | Tipo      | Padrão | Descrição                                                      |
| ---------------------------- | --------- | ------ | -------------------------------------------------------------- |
| `Bucket`                     | `string?` | —      | Bucket padrão das operações. Cada operação pode informar outro |
| `SignedUrlExpirationMinutes` | `int`     | `5`    | Validade padrão da URL assinada, de 1 a 10080 minutos (7 dias) |

As opções são validadas no registro do provedor: uma validade fora do intervalo falha no startup.

---

## 📦 Componentes

### `IStorageService`

Todas as operações aceitam `bucket` (opcional: sem ele, vale o das opções) e `CancellationToken`.

| Método                                               | Retorno             | Descrição                                                                                           |
| ---------------------------------------------------- | ------------------- | --------------------------------------------------------------------------------------------------- |
| `UploadAsync(key, content, contentType?)`            | `StorageObjectDto`  | Envia o objeto, substituindo o que existir na chave. O stream não é fechado pelo serviço            |
| `DownloadAsync(key)`                                 | `Stream`            | Baixa o conteúdo; quem chama fecha o stream. Objeto inexistente lança `NotFoundException`           |
| `DeleteAsync(key)`                                   | `bool`              | Exclui o objeto: verdadeiro quando excluiu, falso quando ele não existia                            |
| `ExistsAsync(key)`                                   | `bool`              | Indica se o objeto existe                                                                           |
| `GetInfoAsync(key)`                                  | `StorageObjectDto?` | Dados do objeto sem baixar o conteúdo; nulo quando ele não existe                                   |
| `GetSignedUrlAsync(key, expiration?, access = Read)` | `Uri`               | URL temporária para ler (GET) ou escrever (PUT) sem credencial. Sem `expiration`, vale a das opções |

A URL assinada é calculada **localmente**, sem consultar o storage: gerá-la não confere se o objeto existe.

### `StorageObjectDto`

| Propriedade    | Tipo              | Descrição                                                           |
| -------------- | ----------------- | ------------------------------------------------------------------- |
| `Bucket`       | `string`          | Bucket do objeto                                                    |
| `Key`          | `string`          | Chave (nome) do objeto                                              |
| `Size`         | `long?`           | Tamanho em bytes; nulo no upload de um stream que não permite busca |
| `ContentType`  | `string?`         | Tipo do conteúdo (MIME)                                             |
| `ETag`         | `string?`         | Versão do conteúdo, sem aspas; o formato varia entre provedores     |
| `LastModified` | `DateTimeOffset?` | Data da última alteração, quando o provedor a informa               |

### `ESignedUrlAccess`

`Read` (padrão) gera uma URL de leitura (GET); `Write` gera uma de escrita (PUT).

### `StorageServiceBase`

Base para um provedor novo. As operações públicas validam os argumentos e resolvem o bucket e a validade; o provedor
implementa os métodos protegidos `UploadCoreAsync`, `DownloadCoreAsync`, `DeleteCoreAsync`, `GetInfoCoreAsync` e
`GetSignedUrlCoreAsync`, que já recebem tudo resolvido. `ExistsAsync` vem de `GetInfoCoreAsync`.

| Validação                                | Exceção                        | Mensagem                                   |
| ---------------------------------------- | ------------------------------ | ------------------------------------------ |
| Chave em branco                          | `BadRequestException`          | `Storage.KeyRequired`                      |
| Chave com mais de 1024 bytes em UTF-8    | `BadRequestException`          | `Storage.KeyTooLong;1024`                  |
| Nenhum bucket informado nem configurado  | `InternalServerErrorException` | `Storage.BucketNotConfigured`              |
| Validade não positiva ou acima de 7 dias | `BadRequestException`          | `Storage.SignedUrlExpirationInvalid;10080` |

### Erros do provedor

| Situação                         | Exceção                        | Mensagem                  |
| -------------------------------- | ------------------------------ | ------------------------- |
| Objeto inexistente               | `NotFoundException`            | `Storage.ObjectNotFound`  |
| Bucket inexistente               | `InternalServerErrorException` | `Storage.BucketNotFound`  |
| Acesso negado                    | `InternalServerErrorException` | `Storage.AccessDenied`    |
| Qualquer outra falha do provedor | `InternalServerErrorException` | `Storage.OperationFailed` |

Bucket inexistente e acesso negado são `InternalServerErrorException` porque indicam configuração da aplicação, e não
erro de quem chamou. O cancelamento continua chegando como `OperationCanceledException`.

---

## 📝 Exemplos de Uso

### Upload de um arquivo recebido pela API

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Storage.Interfaces;

[ApiController]
[Route("documentos")]
public sealed class DocumentoController(IStorageService storage) : ControllerBase
{
  [HttpPost]
  public async Task<IActionResult> Enviar(IFormFile arquivo, CancellationToken cancellationToken)
  {
    // A chave é gerada no servidor, e não vem do nome do arquivo do usuário
    var chave = $"documentos/{Guid.NewGuid()}{Path.GetExtension(arquivo.FileName)}";

    await using var conteudo = arquivo.OpenReadStream();
    var objeto = await storage.UploadAsync(chave, conteudo, arquivo.ContentType, cancellationToken: cancellationToken);

    return Ok(new { objeto.Key, objeto.Size });
  }
}
```

### Link temporário para um arquivo privado

```csharp
[HttpGet("{**chave}")]
public async Task<IActionResult> Abrir(string chave, CancellationToken cancellationToken)
{
  if (!await storage.ExistsAsync(chave, cancellationToken: cancellationToken))
  {
    return NotFound();
  }

  var url = await storage.GetSignedUrlAsync(chave, TimeSpan.FromMinutes(10), cancellationToken: cancellationToken);

  return Redirect(url.ToString());
}
```

### Upload direto do navegador

A API gera a chave e uma URL de escrita; o navegador envia o arquivo com `PUT`, sem passar o conteúdo pela API. O
bucket precisa de uma regra de CORS que libere o `PUT` para a origem da página.

```csharp
using Tooark.Storage.Enums;

[HttpPost("upload-url")]
public async Task<IActionResult> GerarUrlDeUpload(CancellationToken cancellationToken)
{
  var chave = $"uploads/{Guid.NewGuid()}";
  var url = await storage.GetSignedUrlAsync(chave, access: ESignedUrlAccess.Write, cancellationToken: cancellationToken);

  return Ok(new { chave, url });
}
```

```ts
const { chave, url } = await api.post("/documentos/upload-url");
await fetch(url, { method: "PUT", body: arquivo });
```

### Mídia do `@tooark/wysiwyg`

O editor [`@tooark/wysiwyg`](https://www.npmjs.com/package/@tooark/wysiwyg) guarda imagens e vídeos pelo gancho
`uploadFile`, e o JSON salvo leva a URL. Grave a **chave** e devolva um caminho da própria API, que gera uma URL
assinada nova a cada acesso: uma URL assinada no JSON expiraria em no máximo 7 dias. O caminho relativo começa com
`/`, que o componente e o `Tooark.Sanitizers` aceitam.

```ts
editor.uploadFile = async (file) => {
  const { chave } = await api.enviar(file); // POST /documentos, como no primeiro exemplo
  return { src: `/documentos/${chave}`, alt: file.name }; // GET /documentos/{chave} redireciona
};
```

### Trocando de provedor

Só o registro muda:

```csharp
// builder.Services.AddTooarkStorageAws(builder.Configuration);
builder.Services.AddTooarkStorageGcp(builder.Configuration);
```

---

## 📋 Dependências

| Pacote                                                                  | Versão | Descrição                                                      |
| ----------------------------------------------------------------------- | ------ | -------------------------------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions) | 4.x    | Exceções (`BadRequestException`, `NotFoundException` e demais) |

O pacote roda no runtime base do .NET, sem o ASP.NET Core e sem SDK de nuvem.

---

## 🎯 Boas Práticas

1. **Gere a chave no servidor** — o nome do arquivo do usuário pode sobrescrever outro objeto ou trazer caminhos
   inesperados; use um identificador gerado e guarde o nome original à parte, se precisar dele.
2. **Grave a chave, e não a URL assinada** — a URL expira; gere uma nova quando o arquivo for acessado.
3. **Bucket privado e URL assinada** — em vez de bucket público, entregue cada arquivo com uma URL de validade curta.
4. **Credenciais do ambiente** — prefira a role da AWS ou o Application Default Credentials do Google a chaves na
   configuração; veja o README de cada provedor.
5. **Arquivos grandes por URL assinada** — baixar pela API ocupa a conexão e, no Google Cloud, a memória; a URL de
   leitura entrega o arquivo direto do storage.

---

## ⚠️ Códigos de Erro e Soluções

| Mensagem                                           | Exceção                        | Descrição                                       | Solução                                                       |
| -------------------------------------------------- | ------------------------------ | ----------------------------------------------- | ------------------------------------------------------------- |
| `Storage.KeyRequired`                              | `BadRequestException`          | Chave do objeto em branco                       | Informe a chave                                               |
| `Storage.KeyTooLong;1024`                          | `BadRequestException`          | Chave com mais de 1024 bytes em UTF-8           | Encurte a chave                                               |
| `Storage.SignedUrlExpirationInvalid;10080`         | `BadRequestException`          | Validade da URL não positiva ou acima de 7 dias | Use uma validade entre 1 minuto e 7 dias                      |
| `Storage.BucketNotConfigured`                      | `InternalServerErrorException` | Nenhum bucket na chamada nem nas opções         | Configure `Storage:Bucket` ou informe o bucket na chamada     |
| `Storage.ObjectNotFound`                           | `NotFoundException`            | O objeto não existe                             | Confira a chave; use `ExistsAsync` quando a ausência é normal |
| `Storage.BucketNotFound`                           | `InternalServerErrorException` | O bucket não existe                             | Confira o nome do bucket e a região ou o projeto              |
| `Storage.AccessDenied`                             | `InternalServerErrorException` | A credencial não tem permissão no bucket        | Conceda a permissão à identidade da aplicação                 |
| `Storage.OperationFailed`                          | `InternalServerErrorException` | Falha do provedor ou do SDK                     | Veja a `InnerException`                                       |
| `Options.Storage.SignedUrlExpirationInvalid;10080` | `InternalServerErrorException` | `SignedUrlExpirationMinutes` fora de 1 a 10080  | Ajuste a validade padrão nas opções                           |

Os erros próprios de cada provedor estão no README dele.

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
