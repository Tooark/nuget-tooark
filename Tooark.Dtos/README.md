# Tooark.Dtos

Biblioteca para gerenciamento e manutenção de DTOs base em projetos .NET.

## Conteúdo

- [Visão Geral](#visão-geral)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Componentes](#-componentes)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Contribuição](#-contribuição)
- [Licença](#-licença)

## Visão Geral

O pacote `Tooark.Dtos` fornece:

- DTOs de busca e paginação para endpoints de listagem;
- uma resposta padrão com dados, erros, paginação e metadados;
- tradução automática das chaves de erro produzidas pelas validações do Tooark;
- limite de tamanho de página, para que uma requisição não possa pedir todos os registros.

> **Requisito de runtime**: `SearchDto`, `PaginationDto` e `ResponseDto` usam tipos do MVC, do `Http` e do
> `WebUtilities`, então o pacote exige o runtime do ASP.NET Core instalado. É o esperado para DTOs de API.

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Dtos
```

---

## ⚙️ Configuração

A tradução das mensagens **não exige configuração**: o `ResponseDto` resolve o idioma pelo fluxo de execução
da requisição e lê as traduções dos arquivos distribuídos com o
[`Tooark.Extensions`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Extensions).

O registro abaixo existe para a aplicação poder injetar `IStringLocalizer` nos próprios tipos:

```csharp
using Tooark.Dtos.Injections;

builder.Services.AddTooarkDtos();
```

Para acrescentar ou sobrescrever traduções, coloque um `Resources/{idioma}.json` na saída da aplicação.

---

## 📦 Componentes

### Dto

Classe base dos DTOs que produzem mensagens traduzidas. Não tem membros públicos: serve para o
`ResponseDto` e o `SearchDto` compartilharem o localizador.

### SearchDto

Parâmetros de busca com paginação.

| Membro               | Tipo         | Descrição                                                                          |
| -------------------- | ------------ | ---------------------------------------------------------------------------------- |
| `Search`             | `string?`    | Informação a ser procurada. Padrão: nulo                                           |
| `SearchNormalized`   | `string?`    | `Search` normalizado, calculado uma vez por valor. Não é vinculado nem serializado |
| `PageIndex`          | `long`       | Índice da página, começando em 1. Valor menor assume 1. Padrão: 1                  |
| `PageIndexLogical`   | `long`       | `PageIndex` menos um, para uso direto em `Skip`. Não é vinculado nem serializado   |
| `PageSize`           | `long`       | Tamanho da página. Negativo assume 0, que significa ignorar o tamanho. Padrão: 10  |
| `PageSizeMax`        | `long`       | `protected virtual`. Teto do `PageSize`. Padrão: `DefaultPageSizeMax`              |
| `DefaultPageSizeMax` | `const long` | Teto padrão: 100                                                                   |

### SearchOrderDto

Herda de `SearchDto` e acrescenta ordenação.

| Membro     | Tipo      | Descrição                                   |
| ---------- | --------- | ------------------------------------------- |
| `OrderBy`  | `string?` | Nome da coluna a ordenar                    |
| `OrderAsc` | `bool`    | Crescente quando verdadeiro. Padrão: `true` |

### PaginationDto

Total de registros e navegação entre páginas. Todas as propriedades são somente leitura.

| Membro                                      | Tipo      | Descrição                                              |
| ------------------------------------------- | --------- | ------------------------------------------------------ |
| `Total`                                     | `long`    | Total de registros                                     |
| `PageSize` / `PageIndex`                    | `long`    | Tamanho e índice da página                             |
| `Previous` / `Next`                         | `long?`   | Índices das páginas vizinhas, nulos quando não existem |
| `CurrentLink` / `PreviousLink` / `NextLink` | `string?` | URLs correspondentes                                   |

### ResponseDto&lt;T&gt;

Resposta padrão de API.

| Membro                                          | Tipo                         | Descrição                                       |
| ----------------------------------------------- | ---------------------------- | ----------------------------------------------- |
| `Data`                                          | `T?`                         | Dados da resposta                               |
| `Errors`                                        | `IReadOnlyList<string>`      | Mensagens de erro, já traduzidas                |
| `Pagination`                                    | `PaginationDto?`             | Dados de paginação                              |
| `Metadata`                                      | `IReadOnlyList<MetadataDto>` | Metadados                                       |
| `SetPagination` / `SetMetadata` / `AddMetadata` |                              | Definem paginação e metadados após a construção |

`Errors` e `Metadata` são coleções somente leitura: convertê-las para `IList` compila, mas alterá-las lança
`NotSupportedException`. Use `SetMetadata` ou `AddMetadata`.

### MetadataDto

Par chave/valor. Chave e valor são independentes — informar um como nulo resulta em string vazia, sem
descartar o outro.

### Limite de tamanho de página

Sem um teto, uma única requisição poderia pedir todos os registros. O `PageSize` é limitado a
`DefaultPageSizeMax`, que vale 100. Um endpoint que precise de páginas maiores sobrescreve o limite no
próprio DTO:

```csharp
public sealed class RelatorioSearchDto : SearchDto
{
  protected override long PageSizeMax => 5000;
}
```

Use a forma de expressão. Uma propriedade automática com inicializador não serve: o limite é consultado pelo
construtor da classe base, que roda antes dos inicializadores da classe derivada.

### Links de paginação

Os links reaproveitam a query string da requisição, trocando apenas o `PageIndex`, para que os filtros do
endpoint sigam valendo na navegação.

Duas consequências que valem conhecer:

- **Todo parâmetro da requisição aparece no corpo da resposta.** Não trafegue credenciais na query string —
  elas voltariam nos links e daí para logs, cache e histórico do navegador.
- **Os links não são um controle de acesso.** Quem consegue chamar uma página consegue chamar as outras
  editando a URL, e o `Total` já informa quantas existem. Contra coleta em massa, o que vale é o teto de
  `PageSize`, o limite de taxa e a autorização — não esconder os links.

---

## 📝 Exemplos de Uso

### Busca com paginação

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Dtos;

[HttpGet]
public async Task<IActionResult> Listar([FromQuery] SearchDto filtro)
{
  var query = _context.Pessoas.AsQueryable();

  if (!string.IsNullOrEmpty(filtro.SearchNormalized))
  {
    query = query.Where(p => p.NomeNormalizado.Contains(filtro.SearchNormalized));
  }

  var total = await query.LongCountAsync();

  var pessoas = await query
    .Skip((int)(filtro.PageIndexLogical * filtro.PageSize))
    .Take((int)filtro.PageSize)
    .ToListAsync();

  var resposta = new ResponseDto<List<Pessoa>>(pessoas);
  resposta.SetPagination(new PaginationDto(total, filtro, Request));

  return Ok(resposta);
}
```

A resposta:

```json
{
  "data": [ ... ],
  "errors": [],
  "pagination": {
    "total": 95,
    "pageSize": 10,
    "pageIndex": 9,
    "previous": 8,
    "next": 10,
    "currentLink": "https://api.exemplo.com/pessoas?PageIndex=9&PageSize=10",
    "previousLink": "https://api.exemplo.com/pessoas?PageIndex=8&PageSize=10",
    "nextLink": "https://api.exemplo.com/pessoas?PageIndex=10&PageSize=10"
  },
  "metadata": []
}
```

> **Não chame o parâmetro de `search`.** O `SearchDto` tem uma propriedade `Search`, e o ASP.NET Core emite
> o aviso `MVC1004` quando o nome do parâmetro coincide com o de uma propriedade do tipo vinculado, porque a
> resolução de prefixo fica ambígua. Qualquer outro nome resolve.

### Busca com ordenação

```csharp
using Tooark.Dtos;

[HttpGet]
public IActionResult Listar([FromQuery] SearchOrderDto filtro)
{
  var query = _context.Pessoas.OrderByProperty(filtro.OrderBy ?? nameof(Pessoa.Nome));

  // ...
}
```

### Resposta com erros de validação

```csharp
using Tooark.Dtos;

var pessoa = new Pessoa(nome, email);

// A notificação inválida vira a lista de erros, já traduzida
if (!pessoa.IsValid)
{
  // ["O campo Nome é obrigatório"]
  return BadRequest(new ResponseDto<Pessoa>(pessoa));
}
```

Com o código do erro na frente da mensagem:

```csharp
// ["T.VLD.STR5: O campo Nome é obrigatório"]
return BadRequest(new ResponseDto<Pessoa>(pessoa.Notification, withCode: true));
```

### Endpoint com página maior

```csharp
using Tooark.Dtos;

public sealed class ExportacaoSearchDto : SearchDto
{
  protected override long PageSizeMax => 5000;
}

[HttpGet("exportacao")]
public IActionResult Exportar([FromQuery] ExportacaoSearchDto filtro)
{
  // filtro.PageSize aceita até 5000 neste endpoint
}
```

### Metadados

```csharp
using Tooark.Dtos;

var resposta = new ResponseDto<List<Pessoa>>(pessoas);

resposta.AddMetadata(new MetadataDto("versao", "2024-01"));
resposta.SetMetadata([new MetadataDto("origem", "cache")]);
```

---

## 📋 Dependências

| Pacote                                                                                       | Versão   | Descrição                                         |
| -------------------------------------------------------------------------------------------- | -------- | ------------------------------------------------- |
| [`Tooark.Extensions`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Extensions)       | 4.x      | Localização das mensagens e normalização da busca |
| [`Tooark.Notifications`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Notifications) | 4.x      | Notificações que viram os erros da resposta       |
| `Microsoft.AspNetCore.App` (framework compartilhado)                                         | 8.x/10.x | `HttpRequest`, `QueryHelpers` e `BindNever`       |

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Dtos](https://github.com/Tooark/tooark-cs/issues).

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/tooark-cs/refs/heads/main/LICENSE) para mais detalhes.
