# Tooark.Dtos

Biblioteca para gerenciamento e manutenção de DTOs base em projetos .NET.

## Configuração

Para utilizar os resources disponíveis, adicione a seguinte linha no seu arquivo `.csproj`:

```xml
<Target Name="CopyNugetContentFiles" AfterTargets="Build">
  <ItemGroup>
    <NugetContentFiles Include="$(NuGetPackageRoot)\**\Resources\**\*.json" />
  </ItemGroup>
  <Copy SourceFiles="@(NugetContentFiles)" DestinationFolder="$(OutDir)Resources" SkipUnchangedFiles="true" />
</Target>
```

Adicione a seguinte linha no seu arquivo `Program.cs`:

```csharp
// Importando o namespace necessário
using Tooark.Dtos.Injections;

// Nas suas configurações de serviços
services.AddTooarkDtos();
```

## Conteúdo

- [Dto](#1-dto)
- [SearchDto](#2-searchdto)
- [SearchOrderDto](#3-searchorderdto)
- [ResponseDto](#4-responsedto)
- [PaginationDto](#5-paginationdto)
- [MetadataDto](#6-metadatadto)

## DTOs (Data Transfer Objects)

### 1. Dto

**Funcionalidade:**
Classe base para DTOs com configuração de localizador de strings.

- **Métodos:**

  - `Configure(IStringLocalizer localizer)`: Configura o localizador de strings.

### 2. SearchDto

**Funcionalidade:**
Classe para parâmetros de busca.

- **Propriedades:**

  - `Search`: Informação a ser procurada. Padrão: `nulo`.
  - `PageIndex`: Índice da paginação. Padrão: `0`.
  - `PageSize`: Tamanho da paginação. Padrão: `10`.

[**Exemplo de Uso**](#searchdto)

### 3. SearchOrderDto

**Funcionalidade:**
Classe para parâmetros de busca com parâmetro de ordenação.

- **Propriedades:**

  - `OrderBy`: Referencia a ser ordenada. Padrão: `nulo`.
  - `OrderAsc`: Sentido da ordenação. Crescente=`true` ou Decrescente=`false`. Padrão: `true`.

[**Exemplo de Uso**](#searchorderdto)

### 4. ResponseDto

**Funcionalidade:**
Classe de resposta padrão para operações de API.

- **Propriedades:**

  - `Data`: Dados de resposta. Padrão: `nulo`.
  - `Errors`: Lista de erros. Padrão: `vazio`.
  - `Pagination`: Dados de paginação. Padrão: `nulo`.
  - `Metadata`: Metadados. Padrão: `nulo`.

- **Métodos:**

  - `ResponseDto(T? data)`: Construtor da classe com dados de resposta.
  - `ResponseDto(T data, IList<string> errors)`: Construtor da classe com dados de resposta e lista de erros.
  - `ResponseDto(T? data, int total, HttpRequest request)`: Construtor da classe com dados de resposta, total de registros e requisição. Para montar a paginação.
  - `ResponseDto(string error)`: Construtor da classe com erro.
  - `ResponseDto(IList<string> errors)`: Construtor da classe com lista de erros.
  - `ResponseDto(Exception exception)`: Construtor da classe com exceção.
  - `ResponseDto(string message, bool isSuccess)`: Construtor da classe com mensagem e status de sucesso.
  - `ResponseDto(IReadOnlyCollection<NotificationItem> notifications)`: Construtor da classe com notificações.
  - `SetPagination(PaginationDto pagination)`: Adiciona dados de paginação.
  - `SetMetadata(IList<MetadataDto> metadata)`: Adiciona metadados.
  - `AddMetadata(MetadataDto metadata)`: Adiciona um metadado.

[**Exemplo de Uso**](#responsedto)

### 5. PaginationDto

**Funcionalidade:**
Classe de parâmetros de paginação para resposta de API.

- **Propriedades:**

  - `Total`: Total de registros. Padrão: `0`.
  - `PageSize`: Tamanho da página. Padrão: `0`.
  - `PageIndex`: Índice da página. Padrão: `0`.
  - `Previous`: Índice da página anterior. Padrão: `nulo`.
  - `Next`: Índice da página seguinte. Padrão: `nulo`.
  - `CurrentLink`: Link da página atual. Padrão: `nulo`.
  - `PreviousLink`: Link da página anterior. Padrão: `nulo`.
  - `NextLink`: Link da página seguinte. Padrão: `nulo`.

- **Métodos:**

  - `PaginationDto(long total, HttpRequest request)`: Construtor da classe com total de registros e requisição.
  - `PaginationDto(long total, long pageSize, long pageIndex, long previous, long next, HttpRequest request)`: Construtor da classe com parâmetros de paginação e requisição.
  - `public PaginationDto(long total, SearchDto searchDto, HttpRequest request)`: Construtor da classe com total de registros, parâmetros de busca e requisição.

[**Exemplo de Uso**](#paginationdto)

### 6. MetadataDto

**Funcionalidade:**
Classe de metadados para resposta de API.

- **Propriedades:**

  - `Key`: Chave do metadado. Padrão: `nulo`.
  - `Value`: Valor do metadado. Padrão: `nulo`.

[**Exemplo de Uso**](#metadatadto)

## Exemplos de Uso

### Dto

```csharp
using Tooark.Dtos;

Dto.Configure(localizer);
```

### SearchDto

```csharp
using Tooark.Dtos;

var search = new SearchDto
{
  Search = "Exemplo",
  PageIndex = 1,
  PageSize = 20
};
```

### SearchOrderDto

```csharp
using Tooark.Dtos;

var searchOrder = new SearchOrderDto
{
  Search = "Exemplo",
  PageIndex = 1,
  PageSize = 20,
  OrderBy = "Nome",
  OrderAsc = true
};
```

### ResponseDto

**Dados de Resposta:**

```csharp
using Tooark.Dtos;

var example = new ExampleDto() { Id = 1, Name = "Exemplo" };
var response = new ResponseDto<ExampleDto>(example);

response.SetPagination(new PaginationDto(100, request));
response.AddMetadata(new MetadataDto("Chave", "Valor"));
```

**Dados de Resposta e Lista de Erros:**

```csharp
using Tooark.Dtos;

var example = new ExampleDto() { Id = 1, Name = "Exemplo" };
var errors = new List<string> { "Erro 1", "Erro 2" };
var response = new ResponseDto<ExampleDto>(example, errors);

response.SetPagination(new PaginationDto(100, request));
response.AddMetadata(new MetadataDto("Chave", "Valor"));
```

**Dados de Resposta, total de registros e requisição:**

```csharp
using Tooark.Dtos;

var example = new ExampleDto() { Id = 1, Name = "Exemplo" };
var total = 100;
var response = new ResponseDto<ExampleDto>(example, total, request);

response.SetMetadata(new List<MetadataDto>() {new MetadataDto("Chave", "Valor")});
```

**Erro único:**

```csharp
using Tooark.Dtos;

var response = new ResponseDto<ExampleDto>("Erro");

response.SetPagination(new PaginationDto(100, request));
response.AddMetadata(new MetadataDto("Chave", "Valor"));
```

**Lista de Erros:**

```csharp
using Tooark.Dtos;

var errors = new List<string> { "Erro 1", "Erro 2" };
var response = new ResponseDto<ExampleDto>(erros);

response.SetPagination(new PaginationDto(100, request));
response.AddMetadata(new MetadataDto("Chave", "Valor"));
```

**Exception:**

```csharp
using Tooark.Dtos;

var response = new ResponseDto<ExampleDto>(exception);

response.AddMetadata(new MetadataDto("Chave", "Valor"));
```

**String de dados:**

```csharp
using Tooark.Dtos;

var data = "Exemplo";
var response = new ResponseDto<ExampleDto>(data, true);

response.AddMetadata(new MetadataDto("Chave", "Valor"));
```

**Itens de Notificação:**

```csharp
using Tooark.Dtos;

NotificationItem notification = new NotificationItem("Chave", "Valor");
var response = new ResponseDto<ExampleDto>(notification);

response.AddMetadata(new MetadataDto("Chave", "Valor"));
```

### PaginationDto

**Total de Registros e Requisição:**

```csharp
using Tooark.Dtos;

var pagination = new PaginationDto(100, request);
```

**Parâmetros de Paginação e Requisição:**

```csharp
using Tooark.Dtos;

var pagination = new PaginationDto(100, 20, 1, 0, 2, request);
```

**Parâmetros de Paginação, Parâmetros de Busca e Requisição:**

```csharp
using Tooark.Dtos;

var search = new SearchDto
{
  Search = "Exemplo",
  PageIndex = 1,
  PageSize = 20
};

var pagination = new PaginationDto(100, search, request);
```

### MetadataDto

```csharp
using Tooark.Dtos;

var metadata = new MetadataDto("Chave", "Valor");
```

## Dependências

- [Microsoft.AspNetCore.Http](https://www.nuget.org/packages/Microsoft.AspNetCore.Http/)
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/)
- [Microsoft.Extensions.Localization](https://www.nuget.org/packages/Microsoft.Extensions.Localization/)
- [Tooark.Extensions](../Tooark.Extensions/README.md)
- [Tooark.Notifications](../Tooark.Notifications/README.md)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Dtos](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
