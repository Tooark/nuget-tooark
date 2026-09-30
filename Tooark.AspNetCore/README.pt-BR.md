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
que decide o corpo de uma falha de validação, e trocar essa factory bastou. Quando filtros e middlewares
existirem, entram neste pacote, nas pastas `Filters/` e `Middlewares/`.
A família só divide pacote quando o **perfil de dependência** difere — foi o que motivou separar o
`Tooark.Mediator.EntityFrameworkCore`, que puxa o Entity Framework Core, do `Tooark.Mediator`, que não o usa.
Filtros, middlewares e as extensões daqui compartilham exatamente o mesmo perfil, então dividi-los em pacotes
separados não reduziria nada.

### Namespaces

As extensões ficam em `Tooark.AspNetCore.Extensions`, e os registros em `Tooark.AspNetCore.Injections`.

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

---

## 📋 Dependências

| Pacote                                                                                                          | Versão   | Descrição                                                      |
| --------------------------------------------------------------------------------------------------------------- | -------- | -------------------------------------------------------------- |
| [`Tooark.Attributes`](https://www.nuget.org/packages/Tooark.Attributes)                                         | 4.x      | Os atributos reconhecidos pelo `AddTooarkValidationAttributes` |
| [`Tooark.Dtos`](https://www.nuget.org/packages/Tooark.Dtos)                                                     | 4.x      | `ResponseDto`, o corpo da resposta de validação                |
| [`Tooark.Extensions`](https://www.nuget.org/packages/Tooark.Extensions)                                         | 4.x      | Localização das mensagens de erro                              |
| [`Microsoft.AspNetCore.App`](https://www.nuget.org/packages/Microsoft.AspNetCore.App) (framework compartilhado) | 8.x/10.x | `ModelStateDictionary`, `ApiBehaviorOptions` e `MvcOptions`    |

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
