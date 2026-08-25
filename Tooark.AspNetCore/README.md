# Tooark.AspNetCore

Biblioteca que concentra o que o Tooark tem de específico de ASP.NET Core, mantendo os demais pacotes livres do requisito de runtime.

## Conteúdo

- [Visão Geral](#visão-geral)
- [Instalação](#-instalação)
- [Componentes](#-componentes)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Contribuição](#-contribuição)
- [Licença](#-licença)

## Visão Geral

O pacote `Tooark.AspNetCore` fornece:

- extensões para tipos do ASP.NET Core, hoje o `ModelStateDictionary`;
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

O pacote **não exige registro no container**: `GetErrors` é um método de extensão e funciona sem
configuração. Quando os filtros e middlewares chegarem, eles trarão o registro correspondente.

> **Requisito de runtime**: por declarar o framework compartilhado, quem consome este pacote precisa do
> runtime do ASP.NET Core instalado. Em uma imagem Docker, use `mcr.microsoft.com/dotnet/aspnet` no lugar de
> `mcr.microsoft.com/dotnet/runtime`. Uma aplicação web já usa essa imagem, então na prática nada muda.

---

## 📦 Componentes

### Extensões de ModelState

- `ModelStateExtension.GetErrors(ModelStateDictionary)`: devolve as mensagens de erro do ModelState.

Percorre todas as entradas do `ModelStateDictionary` e reúne o `ErrorMessage` de cada erro em uma única
lista. Um campo com mais de um erro contribui com todas as mensagens dele. A ordem é a do
`ModelStateDictionary`, que enumera pela chave do campo, e não a ordem em que os erros foram registrados.

### Integração com as validações

As mensagens devolvidas são o que os atributos do
[`Tooark.Attributes`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Attributes) produzem: chaves de
tradução no formato `Chave;Campo`, como `Field.Required;Email`. Elas viram texto pelo `IStringLocalizer` do
[`Tooark.Extensions`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions), que resolve o idioma
pelo fluxo de execução da requisição.

Erros que o próprio model binding gera — um inteiro recebendo texto, por exemplo — vêm com a mensagem do
framework, e não com uma chave. O localizador devolve esse texto inalterado e sinaliza
`ResourceNotFound`, o que permite distinguir os dois casos quando isso importa.

### Filtros e middlewares

Ainda não distribuídos. Quando existirem, entram neste pacote, nas pastas `Filters/` e `Middlewares/`.
A família só divide pacote quando o **perfil de dependência** difere — foi o que motivou separar o
`Tooark.Mediator.EntityFrameworkCore`, que puxa o Entity Framework Core, do `Tooark.Mediator`, que não o usa.
Filtros, middlewares e as extensões daqui compartilham exatamente o mesmo perfil, então dividi-los em pacotes
separados não reduziria nada.

### Namespaces

As extensões ficam em `Tooark.AspNetCore.Extensions`.

---

## 📝 Exemplos de Uso

### Exemplo de leitura dos erros do ModelState

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.AspNetCore.Extensions;

[ApiController]
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

[ApiController]
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

O nome que aparece na mensagem é o do atributo, não o da propriedade: por isso o campo `Cpf` produz
`Field.Invalid;Document`, que é o padrão do `DocumentValidationAttribute`. Para alinhar os dois, informe o
`propertyName`:

```csharp
[DocumentValidation("CPF", propertyName: "Cpf")]
public string Cpf { get; set; } = null!;
```

### Exemplo de filtro para não repetir a checagem

O mesmo bloco em todo endpoint pede um filtro. Enquanto o pacote não traz um pronto, ele cabe em poucas linhas:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Tooark.AspNetCore.Extensions;

public sealed class ValidacaoFilter(IStringLocalizer localizer) : IActionFilter
{
  public void OnActionExecuting(ActionExecutingContext context)
  {
    if (context.ModelState.IsValid)
    {
      return;
    }

    var erros = context.ModelState.GetErrors().Select(erro => localizer[erro].Value);

    context.Result = new BadRequestObjectResult(erros);
  }

  public void OnActionExecuted(ActionExecutedContext context) { }
}
```

```csharp
builder.Services.AddControllers(options => options.Filters.Add<ValidacaoFilter>());
```

---

## 📋 Dependências

| Pacote                                                                                                          | Versão   | Descrição                         |
| --------------------------------------------------------------------------------------------------------------- | -------- | --------------------------------- |
| [`Tooark.Extensions`](https://www.nuget.org/packages/Tooark.Extensions)                                         | 4.x      | Localização das mensagens de erro |
| [`Microsoft.AspNetCore.App`](https://www.nuget.org/packages/Microsoft.AspNetCore.App) (framework compartilhado) | 8.x/10.x | `ModelStateDictionary`            |

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.AspNetCore](https://github.com/Tooark/nuget-tooark/issues).

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
