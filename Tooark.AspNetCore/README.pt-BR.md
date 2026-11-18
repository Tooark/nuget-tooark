# Tooark.AspNetCore

Biblioteca que concentra o que o Tooark tem de específico de ASP.NET Core, mantendo os pacotes de uso geral livres do requisito de runtime. Os pacotes cujo propósito é integrar com o ASP.NET Core (`Tooark.Dtos`, `Tooark.Observability`, `Tooark.Securities.OpenId`) declaram o framework compartilhado por conta própria.

📖 **Documentação:** [Tooark.AspNetCore no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.aspnetcore.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.AspNetCore/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Visão Geral](#-visão-geral)
- [Instalação](#-instalação)
- [Componentes](#-componentes)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 📖 Visão Geral

O pacote `Tooark.AspNetCore` fornece:

- extensões para tipos do ASP.NET Core, hoje o `ModelStateDictionary`;
- o corpo `ResponseDto` para as falhas de validação dos controllers com `[ApiController]`, com `AddTooarkModelStateEnvelope`;
- a validação dos atributos do `Tooark.Attributes` no MVC sem a mensagem duplicada do campo ausente, com `AddTooarkValidationAttributes`;
- Problem Details (RFC 9457) para as exceções do `Tooark.Exceptions`, em controllers e minimal APIs, com `AddTooarkProblemDetails`;
- resultados para minimal APIs que respondem uma notificação ou uma exceção com o mesmo Problem Details, com `TooarkResults`;
- o lugar para onde o requisito de ASP.NET Core foi movido, tirando-o dos pacotes de uso geral;
- integração com o localizador do `Tooark.Extensions`, traduzindo as chaves de erro das validações;
- o lugar previsto para os filtros e middlewares da família.

**Por que o pacote existe.** O `Microsoft.AspNetCore.App` não é uma dependência NuGet comum: declará-lo faz a
aplicação **exigir o runtime do ASP.NET Core instalado**, e esse requisito é contagioso — propaga para todo
pacote que referencia, e para quem referencia esses.

Até a v3 o requisito vinha do `Tooark.Extensions`, por causa de um único arquivo. Na prática,
um worker service ou uma ferramenta de linha de comando que usasse o `Tooark.ValueObjects` recebia
`Microsoft.AspNetCore.App` no próprio `runtimeconfig.json` e não subia em uma imagem
`mcr.microsoft.com/dotnet/runtime`.

A partir da v4 o requisito mora aqui. Quem faz web referencia este pacote; quem não faz, não paga por ele.

| Pacote                                                                                                         | Exige o runtime do ASP.NET Core                                                    |
| -------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| `Tooark.AspNetCore`                                                                                            | **sim** — é o propósito dele                                                       |
| `Tooark.Dtos`                                                                                                  | **sim** — `SearchDto`, `PaginationDto` e `ResponseDto` usam tipos do MVC e do Http |
| `Tooark` (agregador)                                                                                           | **sim** — por referenciar os dois acima                                            |
| `Tooark.Extensions`, `Tooark.Utils`, `Tooark.ValueObjects`, `Tooark.Entities`, `Tooark.Attributes` e os demais | não                                                                                |

---

## 🔧 Instalação

```bash
dotnet add package Tooark.AspNetCore
```

`GetErrors` é um método de extensão e funciona sem configuração. A [resposta de validação](#resposta-de-validação)
e a [validação dos atributos](#atributos-do-tooark-na-validação-do-mvc) têm registro, e os dois são
**opcionais**: nem o pacote nem o `AddTooarkService` do agregador `Tooark` os ligam por conta própria. A resposta
muda o corpo de toda falha de validação da API, e a validação dos atributos muda as mensagens de um campo ausente.

```csharp
using Tooark.AspNetCore.Injections;

builder.Services.AddControllers();
builder.Services.AddTooarkModelStateEnvelope();
builder.Services.AddTooarkValidationAttributes();
```

O [Problem Details para as exceções](#problem-details-para-as-exceções) também é opcional, e precisa do
middleware `UseExceptionHandler` no pipeline, sem argumentos:

```csharp
using Tooark.AspNetCore.Injections;

builder.Services.AddTooarkProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
```

> **Requisito de runtime**: por declarar o framework compartilhado, quem consome este pacote precisa do
> runtime do ASP.NET Core instalado. Em uma imagem Docker, use `mcr.microsoft.com/dotnet/aspnet` no lugar de
> `mcr.microsoft.com/dotnet/runtime`. Uma aplicação web já usa essa imagem, então na prática nada muda.

---

## 📦 Componentes

### Extensões de ModelState

- `ModelStateExtension.GetErrors(ModelStateDictionary)`: devolve as mensagens de erro do ModelState.

Percorre todas as entradas do `ModelStateDictionary` e reúne o `ErrorMessage` de cada erro em uma única
lista. Um campo com mais de um erro contribui com todas as mensagens dele, juntas e na ordem em que foram
registradas. Entre os campos, a ordem é a do `ModelStateDictionary`: vem das chaves, e não da ordem em que os
campos foram validados, mas também não é alfabética — hoje, os campos de primeiro nível vêm antes dos aninhados
e as chaves mais curtas antes das mais longas (`Cpf` antes de `Email`). Não dependa da posição de um erro na
lista.

Um erro sem texto — registrado só com uma exceção, ou com uma mensagem em branco — vira a chave
`Field.Invalid;{chave}`, ou `BadRequest` quando não está ligado a um campo. O ASP.NET Core registra erros só com
a exceção quando a mensagem dela não é segura para o cliente — um corpo JSON malformado com
`AllowInputFormatterExceptionMessages = false`, por exemplo — e quando o `MaxModelValidationErrors` é atingido.
A mensagem da exceção nunca é usada. Até a v4.5.0 esses erros voltavam com o texto vazio ou em branco.

### Resposta de validação

- `TooarkDependencyInjection.AddTooarkModelStateEnvelope(IServiceCollection)`: faz a falha de validação de um
  controller com `[ApiController]` responder com um `ResponseDto<object>`, no lugar do
  `ValidationProblemDetails` do ASP.NET Core.

Em um controller com `[ApiController]`, o ASP.NET Core confere o ModelState antes de a action rodar e, quando ele
é inválido, responde 400 com o corpo montado pelo `ApiBehaviorOptions.InvalidModelStateResponseFactory`. O
método troca essa factory por uma que responde 400 com um
[`ResponseDto<object>`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Dtos): `Data` nulo e `Errors`
com as mensagens do `GetErrors`, já traduzidas pelo próprio `ResponseDto`. As chaves viram texto, e as mensagens
do model binding chegam inalteradas.

O que muda para o cliente:

|                     | `ValidationProblemDetails` (padrão do ASP.NET Core) | `ResponseDto<object>` (com o envelope) |
| ------------------- | --------------------------------------------------- | -------------------------------------- |
| Content type        | `application/problem+json`                          | `application/json`                     |
| Erros               | por campo: `{"Email": ["..."]}`, sem tradução       | uma lista única: `["..."]`, traduzida  |
| Demais propriedades | `type`, `title`, `status`, `traceId`                | `data`, `pagination`, `metadata`       |

Com o [`AddTooarkProblemDetails`](#problem-details-para-as-exceções) e a `ConvertValidationProblems` ligada, a coluna do
ASP.NET Core passa a sair com a lista `errors` do Tooark, traduzida.

**O envelope não escolhe as mensagens.** Ele muda o corpo da resposta, não o que a validação registra no
ModelState. Com `<Nullable>enable</Nullable>`, um campo não anulável com atributo do `Tooark.Attributes` chega
ausente com duas mensagens: a chave `Field.Required`, traduzida, e o `The Email field is required.` do
`[Required]` que o MVC infere, que não é chave de tradução e chega como o framework o escreve. Não é defeito do
envelope nem do atributo: quem retira a mensagem do framework é o
[`AddTooarkValidationAttributes`](#atributos-do-tooark-na-validação-do-mvc), um registro à parte. A aplicação
que valida os DTOs com os atributos do Tooark chama os dois.

**A ordem do registro não importa.** O `AddControllers` atribui a factory padrão enquanto as opções são montadas,
e venceria se viesse depois de um `Configure` comum. O envelope é aplicado depois de todas as configurações
(`PostConfigure`), então vence tanto registrado antes quanto depois do `AddControllers`. Pelo mesmo motivo, uma
factory própria da aplicação definida com `ConfigureApiBehaviorOptions` é substituída: a aplicação que precisa da
própria factory não chama este método. Chamá-lo duas vezes não tem efeito colateral.

**Onde não se aplica.** Só onde o ASP.NET Core chama a factory:

- controllers sem `[ApiController]`, que não têm a checagem automática: use o `GetErrors` na action ou um
  [filtro](#exemplo-de-filtro-para-controllers-sem-apicontroller);
- `SuppressModelStateInvalidFilter = true`, que desliga a checagem automática;
- minimal APIs, que não têm ModelState.

**OpenAPI.** O método muda a resposta, não a descrição da API. Um `[ProducesResponseType(400)]` sem tipo continua
documentado como `ProblemDetails`, o tipo de erro que o ASP.NET Core assume para `[ApiController]`. Para a
documentação bater com a resposta, declare o tipo de erro uma vez no projeto dos controllers:

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Dtos;

[assembly: ProducesErrorResponseType(typeof(ResponseDto<object>))]
```

### Atributos do Tooark na validação do MVC

- `TooarkDependencyInjection.AddTooarkValidationAttributes(IServiceCollection)`: faz o MVC deixar de inferir
  `[Required]` nos membros validados por um atributo do
  [`Tooark.Attributes`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Attributes).

Com `<Nullable>enable</Nullable>`, o MVC trata todo tipo de referência não anulável como se tivesse `[Required]`,
com a mensagem do framework (`The Email field is required.`), que não é chave de tradução. Os atributos do
`Tooark.Attributes` já reportam o valor ausente com a chave `Field.Required`, então, sem o registro, um campo
ausente recebe as duas mensagens. Com o registro, fica só a chave do atributo:

| Membro ausente                             | Sem o registro                                          | Com o registro                |
| ------------------------------------------ | ------------------------------------------------------- | ----------------------------- |
| `[EmailValidation] string Email`           | `The Email field is required.` e `Field.Required;Email` | `Field.Required;Email`        |
| `string Nome`, sem atributo do Tooark      | `The Nome field is required.`                           | `The Nome field is required.` |
| `[Required][EmailValidation] string Email` | as duas mensagens                                       | as duas mensagens             |

**O alcance é o dos atributos do Tooark.** O `[Required]` inferido só sai de um membro que tem atributo do
Tooark. Um `[Required]` declarado no membro fica, porque é escolha de quem escreveu o DTO, e os membros sem
atributo do Tooark mantêm a inferência. Essa é a diferença para o
`SuppressImplicitRequiredAttributeForNonNullableReferenceTypes`, que desliga a inferência em todos os DTOs da
aplicação. O membro continua marcado como obrigatório nos metadados do MVC, o que é verdade: o atributo recusa o
valor ausente.

**A ordem do registro não importa.** O provedor que infere o `[Required]` entra nas opções do MVC com o
`AddControllers`. O registro é aplicado depois de todas as configurações (`PostConfigure`), então roda depois
desse provedor, chamado antes ou depois do `AddControllers`. Chamá-lo duas vezes não repete o registro.

**Onde se aplica.** Na validação do MVC: record posicional, classe e parâmetro de action, em controllers com ou
sem `[ApiController]` e com ou sem o [envelope](#resposta-de-validação). As minimal APIs não passam pelas opções
do MVC.

**Não é ligado pelo envelope.** O `AddTooarkModelStateEnvelope` e este registro são independentes, e nenhum dos
dois chama o outro: este escolhe as mensagens do campo ausente, e o envelope, o corpo em que elas chegam ao
cliente.

### Problem Details para as exceções

- `TooarkDependencyInjection.AddTooarkProblemDetails(IServiceCollection, Action<ProblemDetailsOptions>?)`: faz as
  exceções do [`Tooark.Exceptions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Exceptions)
  responderem com Problem Details ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)), no status que cada
  exceção define, em controllers e minimal APIs.
- `TooarkExceptionHandler`: o `IExceptionHandler` que o registro acrescenta. Pode ser registrado sozinho com
  `AddExceptionHandler<TooarkExceptionHandler>()`, ao lado do `AddProblemDetails()`.
- `ProblemDetailsError`: um item da lista `errors` do corpo, com `code`, `key` e `message`.
- `TooarkProblemDetailsOptions`: o nível do log de cada exceção tratada e a conversão das falhas de validação do
  ASP.NET Core para a lista `errors`.

Sem ele, uma `NotFoundException` lançada em um endpoint é uma exceção como outra qualquer: o ASP.NET Core
responde 500, e o status e a mensagem que a exceção carrega se perdem. O registro acrescenta o
`AddProblemDetails` do ASP.NET Core e um `IExceptionHandler` que reconhece a família `TooarkException`: o status
vem de `GetStatusCode()` e o corpo de `GetNotifications()`, com as mensagens traduzidas pelo localizador do
[`Tooark.Extensions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions) no idioma da
requisição.

`GET /users/1` lançando `new NotFoundException("NotFound.User")`, em `pt-BR`:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Usuário não encontrado",
  "instance": "/users/1",
  "errors": [{ "code": "T.ERR", "key": "Unknown", "message": "Usuário não encontrado" }],
  "traceId": "0HNPTVUT6D34Q"
}
```

| Membro     | Origem                                                                                          |
| ---------- | ----------------------------------------------------------------------------------------------- |
| `status`   | `TooarkException.GetStatusCode()`                                                               |
| `title`    | A frase padrão do status (`Not Found`, `Bad Request`)                                           |
| `type`     | A seção da RFC 9110 do status, aplicada pelo ASP.NET Core                                       |
| `detail`   | A primeira mensagem de `errors`                                                                 |
| `instance` | O caminho da requisição                                                                         |
| `errors`   | Um item por `NotificationItem` da exceção: `code` e `key` como registrados, `message` traduzida |
| `traceId`  | Acrescentado pelo ASP.NET Core                                                                  |

Uma exceção construída a partir de uma `Notification` traz um item por falha, com o código e a chave de cada
validação; uma construída a partir de uma mensagem traz um único item com os padrões `T.ERR` e `Unknown`. Uma
mensagem que não é chave de tradução, como `"User.NotFound"`, chega como foi escrita.

**Quais exceções.** Toda classe do `Tooark.Exceptions`, no status dela: `BadRequestException` 400,
`UnauthorizedException` 401, `ForbiddenException` 403, `NotFoundException` 404, `ConflictException` 409,
`UnprocessableEntityException` 422, `TooManyRequestsException` 429, `InternalServerErrorException` 500 e as
demais do [pacote](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Exceptions/README.pt-BR.md). Qualquer
outra exceção fica com o ASP.NET Core, que responde o Problem Details 500 padrão do `AddProblemDetails`, sem a
mensagem da exceção: um bug não vaza o próprio texto para o cliente.

**Onde se aplica.** Onde a exceção alcança o middleware `UseExceptionHandler`: handlers de minimal API, actions e
filtros de controllers, e os middlewares depois dele. O middleware precisa estar no pipeline, sem argumentos e
antes dos middlewares que podem lançar. O `AddProblemDetails` é o que permite chamar o `UseExceptionHandler` sem
um caminho de erro:

```csharp
builder.Services.AddTooarkProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();   // sem argumentos: o serviço de Problem Details é o fallback
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

**Escrita do corpo.** O corpo é escrito pelo `IProblemDetailsService` do ASP.NET Core, então o
`CustomizeProblemDetails` da aplicação vale para ele e o `type` recebe o padrão do status. Quando o serviço
recusa a escrita — um cliente que enviou um `Accept` sem JSON, por exemplo — o manipulador escreve o corpo por
conta própria, em `application/problem+json`: uma exceção da família Tooark nunca termina em um 500 sem corpo. O
parâmetro `configure` é o `ProblemDetailsOptions` do ASP.NET Core:

```csharp
builder.Services.AddTooarkProblemDetails(options =>
  options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["application"] = "orders-api");
```

**Independente do envelope.** O [`AddTooarkModelStateEnvelope`](#resposta-de-validação) molda a falha de validação dos
controllers com `[ApiController]`, que acontece antes da action, com o `ResponseDto`. Este registro molda as exceções,
que acontecem durante a action, e nenhum dos dois altera o outro. Uma API pode usar os dois, ou escolher Problem Details
para tudo deixando de registrar o envelope: com a `ConvertValidationProblems` ligada, a falha de validação também sai na
lista `errors` do Tooark (veja abaixo). Chamar o registro duas vezes não repete o manipulador.

**Log.** A exceção tratada é registrada na categoria `Tooark.AspNetCore.ExceptionHandling.TooarkExceptionHandler`:
`Warning` para um status 4xx, com o status e a mensagem, e `Error` para um 5xx, com a exceção e a pilha de
chamadas. Um 404 ou um 409 que faz parte da operação normal enche o log de avisos; para mudar o nível, por faixa
ou por status, configure as `TooarkProblemDetailsOptions`:

```csharp
builder.Services.Configure<TooarkProblemDetailsOptions>(options =>
{
  options.ClientErrorLogLevel = LogLevel.Information;                // 4xx (padrão: Warning)
  options.ServerErrorLogLevel = LogLevel.Error;                      // 5xx (padrão)
  options.StatusCodeLogLevels[StatusCodes.Status401Unauthorized] = LogLevel.Warning;
  options.StatusCodeLogLevels[StatusCodes.Status404NotFound] = LogLevel.Debug;
});
```

O nível de um status em `StatusCodeLogLevels` vence o nível da faixa dele, e `LogLevel.None` tira o status do
log. As opções existem desde a v4.10.0, são só por código (não há seção de configuração) e mantêm o `Warning`
como padrão dos 4xx; a v5 passa esse padrão para `Information`.

**O registro do próprio ASP.NET Core.** No .NET 10 o `UseExceptionHandler` não registra a exceção que um
`IExceptionHandler` tratou, então o registro do Tooark é o único. No .NET 8 o middleware registra toda exceção
como `Error`, na categoria `Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware`, antes de chamar o
manipulador: o nível do Tooark não muda essa entrada. Não a desligue pela categoria, porque ela também é o único
registro das exceções que não são do Tooark.

**Falhas de validação no formato do Tooark.** O `ValidationProblemDetails` do ASP.NET Core traz `errors` como um objeto
com as mensagens por campo, e o Problem Details do Tooark traz `errors` como a lista de `code`, `key` e `message`: com
os dois na mesma API, o cliente não sabe, pela chave, qual formato recebeu. Com `ConvertValidationProblems` ligada, as
falhas de validação do ASP.NET Core também saem na lista do Tooark, uma entrada por mensagem, com o campo em `key`, o
código padrão `T.ERR` e a mensagem traduzida: uma chave como `Field.Required;Email` chega traduzida, e o texto do
framework chega como ele o escreve. O `status`, o `type`, o `traceId` e as extensões do `CustomizeProblemDetails`
continuam no corpo, e o `title` passa a ser a frase padrão do status, como nas exceções. O `CustomizeProblemDetails`
roda antes da conversão, sobre o problema do ASP.NET Core: um `title` ou um `detail` definido nele é substituído.

```csharp
builder.Services.AddTooarkProblemDetails();
builder.Services.Configure<TooarkProblemDetailsOptions>(options => options.ConvertValidationProblems = true);
```

```json
// Sem a conversão
{ "status": 400, "title": "One or more validation errors occurred.", "errors": { "Email": ["The Email field is required."] } }

// Com a conversão
{ "status": 400, "title": "Bad Request", "detail": "The Email field is required.", "errors": [{ "code": "T.ERR", "key": "Email", "message": "The Email field is required." }] }
```

| Origem da falha                                                | Convertida                                                                      |
| -------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| Validação automática do `[ApiController]`                      | Sim                                                                             |
| Validação automática do `[ApiController]` com o envelope       | Não: o [envelope](#resposta-de-validação) responde o `ResponseDto`              |
| `ValidationProblem()` de um controller                         | Sim                                                                             |
| Validação das minimal APIs (`AddValidation`, .NET 10)          | Sim                                                                             |
| `Results.ValidationProblem` e `TypedResults.ValidationProblem` | No .NET 10. No .NET 8 o framework escreve o corpo direto: use o `TooarkResults` |

A conversão vem desligada na 4.x, porque muda o corpo que os clientes recebem, e passa a vir ligada na v5. Para
converter à mão, nos dois sentidos, veja a [conversão entre os formatos](#conversão-entre-os-formatos-de-validação).

**OpenAPI.** O `ProblemDetails` já é o tipo de erro que o ASP.NET Core assume para `[ApiController]`, então um
`[ProducesResponseType(StatusCodes.Status404NotFound)]` documenta a resposta; nas minimal APIs,
`.ProducesProblem(StatusCodes.Status404NotFound)` faz o mesmo. A extensão `errors` não é descrita pelo framework. Com a
`ConvertValidationProblems` ligada, documente a falha de validação como `ProblemDetails`
(`.ProducesProblem(StatusCodes.Status400BadRequest)`, ou
`[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]` nos controllers), e não com
`.ProducesValidationProblem()`: o esquema do `HttpValidationProblemDetails` descreve `errors` como um objeto por campo.

### Resultados para minimal APIs

- `TooarkResults.Problem(TooarkException)`: responde uma exceção sem lançá-la, com o corpo que o manipulador
  escreve.
- `TooarkResults.ValidationProblem(Notification, int statusCode = 400)`: responde as notificações de uma entidade
  ou de um objeto de valor, 400 por padrão.
- `TooarkResults.ValidationProblem(IEnumerable<NotificationItem>, int statusCode = 400)`: o mesmo, a partir de uma
  lista de itens.

As minimal APIs não têm ModelState. As entidades do
[`Tooark.Entities`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Entities) e os objetos de valor do
[`Tooark.ValueObjects`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.ValueObjects) validam a si
mesmos e acumulam notificações, e o endpoint devolve o problema quando `IsValid` é falso, com um item em
`errors` por notificação. O resultado preenche o `instance` com o caminho da requisição ao executar, e o status
precisa ser um status de erro, de 400 a 599. Uma notificação válida gera um problema com a lista `errors` vazia:
confira `IsValid` antes de chamá-lo.

Os resultados não dependem do `AddTooarkProblemDetails`: traduzem as mensagens com o localizador do
`Tooark.Extensions` por conta própria, no idioma da requisição.

### Conversão entre os formatos de validação

- `ValidationProblemExtension.ToNotificationItems(IDictionary<string, string[]>)`: converte as mensagens por
  campo do ASP.NET Core em itens de notificação do Tooark, um por mensagem, com o campo como chave e o código
  padrão `T.ERR`.
- `ValidationProblemExtension.ToNotificationItems(HttpValidationProblemDetails)`: o mesmo, a partir do problema de
  validação.
- `ValidationProblemExtension.ToValidationErrors(IEnumerable<NotificationItem>)`: o caminho de volta, com os
  itens agrupados pela chave e as mensagens traduzidas. O código de cada item se perde: o formato do ASP.NET Core
  não tem onde guardá-lo.

A conversão é explícita, e não um operador implícito: um campo com várias mensagens vira vários itens, e o caminho
de volta perde o código, o que uma conversão implícita esconderia. O framework também nunca chamaria um operador
ao escrever a resposta; para isso existe o `ConvertValidationProblems`. Uma mensagem em branco vira
`Field.Invalid;{campo}`, ou `BadRequest` sem campo, como no `GetErrors`.

```csharp
using Tooark.AspNetCore.Extensions;
using Tooark.AspNetCore.Results;

// Do formato do ASP.NET Core para a lista do Tooark
var itens = problema.ToNotificationItems();
return TooarkResults.ValidationProblem(itens);

// Da lista do Tooark para o formato do ASP.NET Core
return Results.ValidationProblem(pedido.Notifications.ToValidationErrors());
```

### Integração com as validações

As mensagens devolvidas são o que os atributos do
[`Tooark.Attributes`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Attributes) produzem: chaves de
tradução no formato `Chave;Campo`, como `Field.Required;Email`. Elas viram texto pelo `IStringLocalizer` do
[`Tooark.Extensions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions), que resolve o idioma
pelo fluxo de execução da requisição.

Erros que o próprio model binding gera — um inteiro recebendo texto, por exemplo — vêm com a mensagem do
framework, e não com uma chave. O localizador devolve esse texto inalterado e sinaliza
`ResourceNotFound`, o que permite distinguir os dois casos quando isso importa.

Um campo não anulável ausente pode trazer as duas mensagens: a chave `Field.Required` do atributo e o `[Required]`
que o MVC infere, com o texto do framework. O `AddTooarkValidationAttributes` deixa só a chave: veja
[Atributos do Tooark na validação do MVC](#atributos-do-tooark-na-validação-do-mvc).

### Filtros e middlewares

Ainda não distribuídos. A resposta de validação não é um filtro: o ASP.NET Core já chama uma factory no ponto em
que decide o corpo de uma falha de validação, e trocar essa factory bastou. O tratamento de exceções também não
é um middleware: o ASP.NET Core 8 tem o contrato `IExceptionHandler`, que o middleware `UseExceptionHandler` já
chama, e implementá-lo bastou. Quando filtros e middlewares existirem, entram neste pacote, nas pastas
`Filters/` e `Middlewares/`.
A família só divide pacote quando o **perfil de dependência** difere — foi o que motivou separar o
`Tooark.Mediator.EntityFrameworkCore`, que puxa o Entity Framework Core, do `Tooark.Mediator`, que não o usa.
Filtros, middlewares e as extensões daqui compartilham exatamente o mesmo perfil, então dividi-los em pacotes
separados não reduziria nada.

### Namespaces

As extensões ficam em `Tooark.AspNetCore.Extensions`, os registros em `Tooark.AspNetCore.Injections`, o
manipulador de exceções e as opções dele em `Tooark.AspNetCore.ExceptionHandling` e os resultados de minimal API em
`Tooark.AspNetCore.Results`.

---

## 📝 Exemplos de Uso

### Exemplo de resposta padrão para as falhas de validação

```csharp
using Tooark.AspNetCore.Injections;

builder.Services.AddControllers();
builder.Services.AddTooarkModelStateEnvelope();
```

O DTO validado pelos atributos do `Tooark.Attributes`:

```csharp
using Tooark.Attributes;

public sealed class CriarPessoaDto
{
  [EmailValidation]
  public string Email { get; set; } = null!;

  [DocumentValidation("CPF")]
  public string Cpf { get; set; } = null!;
}
```

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("pessoas")]
public sealed class PessoaController : ControllerBase
{
  [HttpPost]
  public IActionResult Criar([FromBody] CriarPessoaDto dto)
  {
    // Só chega aqui com o ModelState válido: o [ApiController] responde 400 antes de a action rodar
    return Ok();
  }
}
```

`POST /pessoas` com `{"email": "x", "cpf": "123"}`, em `pt-BR`:

```json
{
  "data": null,
  "errors": ["O campo Document é inválido", "O campo E-mail é inválido"],
  "pagination": null,
  "metadata": []
}
```

Uma falha do model binding — texto em um campo numérico, um corpo vazio — chega no mesmo corpo, com a mensagem
do framework em `errors`.

Só com o envelope registrado, um campo ausente traz duas mensagens: sem o `email` no corpo, `errors` recebe
`"O campo E-mail é obrigatório"` e também `"The Email field is required."`, o `[Required]` que o MVC infere. O
[exemplo seguinte](#exemplo-de-campo-ausente-sem-a-mensagem-do-framework) acrescenta o
`AddTooarkValidationAttributes`, que deixa só a primeira.

O nome que aparece na mensagem é o do atributo, não o da propriedade: por isso o campo `Cpf` produz
`Field.Invalid;Document`, que é o padrão do `DocumentValidationAttribute`. Para alinhar os dois, informe o
`propertyName`:

```csharp
[DocumentValidation("CPF", propertyName: "Cpf")]
public string Cpf { get; set; } = null!;
```

### Exemplo de campo ausente sem a mensagem do framework

```csharp
using Tooark.AspNetCore.Injections;

builder.Services.AddControllers();
builder.Services.AddTooarkModelStateEnvelope();
builder.Services.AddTooarkValidationAttributes();
```

Um record posicional, com o atributo no parâmetro do construtor:

```csharp
using Tooark.Attributes;

public sealed record CriarContatoDto([EmailValidation] string Email, string Nome);
```

`POST /contatos` com `{"nome": "Ana"}`, em `pt-BR`:

```json
{
  "data": null,
  "errors": ["O campo E-mail é obrigatório"],
  "pagination": null,
  "metadata": []
}
```

Sem o `AddTooarkValidationAttributes`, `errors` traria também `"The Email field is required."`.
Com `{"email": "ana@teste.com"}`, o `Nome` ausente responde `"The Nome field is required."`: o campo não tem
atributo do Tooark, e a inferência do MVC continua valendo para ele.

### Exemplo de leitura dos erros do ModelState na action

Sem `[ApiController]`, o ASP.NET Core não confere o ModelState antes da action, e é a action que confere. Em um
controller com o atributo, esta checagem nunca roda: o framework responde 400 antes.

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.AspNetCore.Extensions;

[Route("pessoas")]
public sealed class PessoaController : ControllerBase
{
  [HttpPost]
  public IActionResult Criar([FromBody] CriarPessoaDto dto)
  {
    if (!ModelState.IsValid)
    {
      // ["Field.Invalid;Document", "Field.Invalid;Email"]
      var erros = ModelState.GetErrors();

      return BadRequest(erros);
    }

    return Ok();
  }
}
```

### Exemplo com as mensagens traduzidas

As chaves viram texto pelo `IStringLocalizer`, que vem do
[`Tooark.Extensions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions) e é registrado por lá:

```csharp
using Tooark.Extensions.Injections;

builder.Services.AddTooarkExtensions();
```

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Tooark.AspNetCore.Extensions;

[Route("pessoas")]
public sealed class PessoaController(IStringLocalizer localizer) : ControllerBase
{
  [HttpPost]
  public IActionResult Criar([FromBody] CriarPessoaDto dto)
  {
    if (!ModelState.IsValid)
    {
      // ["O campo Document é inválido", "O campo E-mail é inválido"]
      var erros = ModelState.GetErrors().Select(erro => localizer[erro].Value);

      return BadRequest(erros);
    }

    return Ok();
  }
}
```

### Exemplo de filtro para controllers sem `[ApiController]`

A mesma checagem em todo endpoint pede um filtro. O pacote não traz um, porque nos controllers com
`[ApiController]` a resposta de validação já faz esse papel. Para os controllers sem o atributo ele cabe em poucas
linhas e, com o `ResponseDto`, responde com o mesmo corpo:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tooark.AspNetCore.Extensions;
using Tooark.Dtos;

public sealed class ValidacaoFilter : IActionFilter
{
  public void OnActionExecuting(ActionExecutingContext context)
  {
    if (!context.ModelState.IsValid)
    {
      context.Result = new BadRequestObjectResult(new ResponseDto<object>(context.ModelState.GetErrors()));
    }
  }

  public void OnActionExecuted(ActionExecutedContext context) { }
}
```

```csharp
builder.Services.AddControllers(options => options.Filters.Add<ValidacaoFilter>());
```

### Exemplo de Problem Details para as exceções do Tooark

```csharp
using Tooark.AspNetCore.Injections;
using Tooark.Exceptions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTooarkProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();

app.MapGet("/usuarios/{id:guid}", async (Guid id, IUsuarioRepository usuarios) =>
  await usuarios.BuscarAsync(id) ?? throw new NotFoundException("NotFound.User"));

app.Run();
```

`GET /usuarios/3fa85f64-5717-4562-b3fc-2c963f66afa6`, em `pt-BR`, para um usuário que não existe:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Usuário não encontrado",
  "instance": "/usuarios/3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "errors": [{ "code": "T.ERR", "key": "Unknown", "message": "Usuário não encontrado" }],
  "traceId": "0HNPTVUT6D34Q"
}
```

`NotFound.User` é uma chave dos recursos do `Tooark.Extensions`, então chega traduzida. Uma mensagem que não é
chave, como `"User.NotFound"`, chega como foi escrita.

Uma exceção construída a partir de uma notificação traz um item por falha, com o código e a chave de cada
validação:

```csharp
using Tooark.Exceptions;
using Tooark.ValueObjects;

app.MapPut("/usuarios/{id:guid}/email", (Guid id, AlterarEmailRequest request) =>
{
  var email = new Email(request.Email);

  if (!email.IsValid)
  {
    // 400 com errors: [{ "code": "T.VLD.RGX1", "key": "Email", "message": "O campo E-mail é inválido" }]
    throw new BadRequestException(email);
  }

  // ...
  return Results.NoContent();
});
```

### Exemplo de validação de uma entidade em uma minimal API

Sem exceção, devolvendo as notificações da entidade:

```csharp
using Tooark.AspNetCore.Results;

app.MapPost("/usuarios", (CriarUsuarioRequest request) =>
{
  var usuario = new Usuario(request.Nome, request.Email);

  return usuario.IsValid
    ? Results.Created($"/usuarios/{usuario.Id}", usuario)
    : TooarkResults.ValidationProblem(usuario);
});
```

`POST /usuarios` com um e-mail inválido, em `pt-BR`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "O campo E-mail é inválido",
  "instance": "/usuarios",
  "errors": [{ "code": "T.VLD.RGX1", "key": "Email", "message": "O campo E-mail é inválido" }],
  "traceId": "0HNPTVUT6D34Q"
}
```

Para um 422 no lugar do 400, informe o status: `TooarkResults.ValidationProblem(usuario, StatusCodes.Status422UnprocessableEntity)`.
Para responder uma exceção sem lançá-la — um resultado que quem chama já tem em mãos — use
`TooarkResults.Problem(new ConflictException("Record.Exists"))`.

### Exemplo de Problem Details em um controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Exceptions;

[ApiController]
[Route("usuarios")]
public sealed class UsuariosController(IUsuarioRepository usuarios) : ControllerBase
{
  [HttpGet("{id:guid}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Buscar(Guid id)
  {
    var usuario = await usuarios.BuscarAsync(id) ?? throw new NotFoundException("NotFound.User");

    return Ok(usuario);
  }
}
```

O mesmo `AddTooarkProblemDetails` e `UseExceptionHandler` atendem o controller: a exceção sai da action, atravessa
os filtros do MVC e chega ao middleware. O [envelope](#resposta-de-validação), quando registrado, continua
respondendo a validação do `[ApiController]`, que acontece antes da action.

---

## 📋 Dependências

| Pacote                                                                                                          | Versão   | Descrição                                                                                                  |
| --------------------------------------------------------------------------------------------------------------- | -------- | ---------------------------------------------------------------------------------------------------------- |
| [`Tooark.Attributes`](https://www.nuget.org/packages/Tooark.Attributes)                                         | 4.x      | Os atributos reconhecidos pelo `AddTooarkValidationAttributes`                                             |
| [`Tooark.Dtos`](https://www.nuget.org/packages/Tooark.Dtos)                                                     | 4.x      | `ResponseDto`, o corpo da resposta de validação                                                            |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                         | 4.x      | As exceções respondidas com Problem Details, e os itens de notificação delas                               |
| [`Tooark.Extensions`](https://www.nuget.org/packages/Tooark.Extensions)                                         | 4.x      | Localização das mensagens de erro                                                                          |
| [`Microsoft.AspNetCore.App`](https://www.nuget.org/packages/Microsoft.AspNetCore.App) (framework compartilhado) | 8.x/10.x | `ModelStateDictionary`, `ApiBehaviorOptions`, `MvcOptions`, `IExceptionHandler` e `IProblemDetailsService` |

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
