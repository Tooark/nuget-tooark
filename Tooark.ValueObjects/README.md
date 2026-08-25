# Tooark.ValueObjects

Biblioteca de objetos de valor que validam a si mesmos na construção, para projetos .NET.

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

O pacote `Tooark.ValueObjects` fornece:

- 33 objetos de valor que validam a si mesmos na construção, sem lançar exceção;
- notificações traduzidas em vez de exceções, herdadas de `Notification`;
- conversão implícita nos dois sentidos, para o tipo se comportar como o valor que representa;
- documentos brasileiros com conferência de dígito verificador: CPF, CNPJ, RG e CNH.

**Como funciona.** Um objeto de valor nunca lança por dado inválido: ele nasce com notificações. Quem
recebe consulta `IsValid` antes de usar o valor.

```csharp
var cpf = new Cpf("111.111.111-11");

cpf.IsValid       // false
cpf.Number        // "" — objeto inválido não carrega valor
cpf.Notifications // as mensagens do que falhou
```

Objeto inválido **nunca devolve nulo**: devolve o vazio do próprio tipo — string vazia nos 29 objetos de
texto, `Guid.Empty` nos quatro de auditoria. Vale também para `ToString()`, então interpolação,
concatenação e serialização são seguras sem verificação prévia.

---

## 🔧 Instalação

```bash
dotnet add package Tooark.ValueObjects
```

---

## ⚙️ Configuração

**Os objetos de valor não exigem registro algum.** Eles validam sozinhos e, quando algo falha, guardam a
_chave_ da mensagem — `Field.Invalid;Email`, por exemplo. Quem traduz a chave é a camada que monta a
resposta, não o objeto de valor.

O registro abaixo apenas encaminha para o `AddTooarkExtensions()`, que disponibiliza o `IStringLocalizer`
e os arquivos de idioma para a aplicação:

```csharp
using Tooark.ValueObjects.Injections;

builder.Services.AddTooarkValueObjects();
```

---

## 📦 Componentes

### Documentos

Validam formato **e** dígito verificador.

| Tipo       | Construtor                                    | Propriedade      |
| ---------- | --------------------------------------------- | ---------------- |
| `Cpf`      | `(string number)`                             | `Number`         |
| `Cnpj`     | `(string number)`                             | `Number`         |
| `Rg`       | `(string number)`                             | `Number`         |
| `Cnh`      | `(string number)`                             | `Number`         |
| `CpfCnpj`  | `(string number)`                             | `Number`         |
| `CpfRg`    | `(string number)`                             | `Number`         |
| `CpfRgCnh` | `(string number)`                             | `Number`         |
| `Document` | `(string number, EDocumentType? type = null)` | `Number`, `Type` |

O `Document` é a forma genérica: sem tipo informado, assume `EDocumentType.None`, que aceita qualquer
texto alfanumérico — `new Document("111")` é válido, `new Document("111", EDocumentType.CPF)` não é.
Documento reprovado fica com `Number` vazio e `Type` igual a `None`.

### Texto

| Tipo            | Construtor       | Propriedades          |
| --------------- | ---------------- | --------------------- |
| `Name`          | `(string value)` | `Value`, `Normalized` |
| `Title`         | `(string value)` | `Value`, `Normalized` |
| `Description`   | `(string value)` | `Value`, `Normalized` |
| `Keyword`       | `(string value)` | `Value`, `Normalized` |
| `Letter`        | `(string value)` | `Value`               |
| `Numeric`       | `(string value)` | `Value`               |
| `LetterNumeric` | `(string value)` | `Value`               |
| `LanguageCode`  | `(string code)`  | `Code`                |
| `ZipCode`       | `(string value)` | `Value`               |

`Normalized` devolve o valor sem acentos, sem espaços e em maiúsculas — útil para busca e ordenação.
O `LanguageCode` normaliza para o formato `xx-XX` (ex.: `pt-BR`) e expõe a constante
`LanguageCode.Length` (5) para consulta externa, como no dimensionamento de colunas.

### Endereços e protocolos

| Tipo                    | Construtor                                                                       | Propriedades   | Aceita                                             |
| ----------------------- | -------------------------------------------------------------------------------- | -------------- | -------------------------------------------------- |
| `Email`                 | `(string value)`                                                                 | `Value`        | Endereço de email                                  |
| `EmailDomain`           | `(string value)`                                                                 | `Value`        | Domínio de email                                   |
| `Url`                   | `(string value)`                                                                 | `Value`        | FTP, SFTP, HTTP, HTTPS, IMAP, POP3, SMTP, WS e WSS |
| `ProtocolHttp`          | `(string value)`                                                                 | `Value`        | HTTP e HTTPS                                       |
| `ProtocolFtp`           | `(string value)`                                                                 | `Value`        | FTP e SFTP                                         |
| `ProtocolWs`            | `(string value)`                                                                 | `Value`        | WS e WSS                                           |
| `ProtocolEmailSender`   | `(string value)`                                                                 | `Value`        | SMTP                                               |
| `ProtocolEmailReceiver` | `(string value)`                                                                 | `Value`        | IMAP e POP3                                        |
| `LinkVideo`             | `(string link, bool youtube = true, bool vimeo = true, bool dailymotion = true)` | `Link`         | YouTube, Vimeo e Dailymotion                       |
| `FileStorage`           | `(ProtocolHttp link, string? name = null)`                                       | `Link`, `Name` | Link e nome de arquivo                             |

Repare que `LinkVideo` e `FileStorage` expõem `Link`, e não `Value`. O `FileStorage` construído sem nome
usa o próprio link como `Name`. O `Email` tem ainda as constantes `Email.MinLength` (6) e
`Email.MaxLength` (255).

O `Email` guarda o valor em minúsculas. O formato exigido é mais restritivo que o RFC 5322: a parte
local e o domínio precisam de ao menos dois caracteres, `+` não é aceito e espaços nas extremidades
reprovam o valor em vez de serem aparados.

### Auditoria

| Tipo         | Construtor     | Propriedade |
| ------------ | -------------- | ----------- |
| `CreatedBy`  | `(Guid value)` | `Value`     |
| `UpdatedBy`  | `(Guid value)` | `Value`     |
| `DeletedBy`  | `(Guid value)` | `Value`     |
| `RestoredBy` | `(Guid value)` | `Value`     |

Reprovam `Guid.Empty`. Objeto inválido tem `Value` igual a `Guid.Empty`.

### Senha

| Membro                                                                                                                          | Descrição                                                        |
| ------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| `Password(string? value, bool lowercase = true, bool uppercase = true, bool number = true, bool symbol = true, int length = 8)` | Critérios de complexidade                                        |
| `Value`                                                                                                                         | O valor em texto puro                                            |
| `Mask`                                                                                                                          | A máscara `********`, devolvida por `ToString()` quando há valor |

O `Password` **não vaza o valor**. `ToString()` devolve a máscara, e não existe conversão implícita para
texto — o valor só sai por `Value`, explicitamente:

```csharp
var senha = new Password("Senha@123");

senha.ToString()      // "********"
$"{senha}"            // "********"
logger.LogInformation("{Senha}", senha);   // grava a máscara
senha.Value           // "Senha@123" — único caminho, e é evidente na leitura
```

Os critérios valem exatamente como informados: desabilitar todos significa exigir apenas o comprimento,
que é uma política legítima de frase secreta. A regra é a mesma aplicada pelo
`PasswordValidationAttribute` do
[`Tooark.Attributes`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Attributes), porque os dois
usam o `PasswordPattern` do `Tooark.Validations`.

### String delimitada

| Membro                                     | Descrição                             |
| ------------------------------------------ | ------------------------------------- |
| `DelimitedString(string? value)`           | A partir de um texto separado por `;` |
| `DelimitedString(params string[]? values)` | A partir de uma array                 |
| `DelimitedString(List<string>? values)`    | A partir de uma lista                 |
| `Value`                                    | O texto delimitado                    |
| `Values`, `ToArray()`, `ToList()`          | Os itens, sempre em uma cópia         |
| `DefaultDelimiter`                         | A constante do delimitador padrão `;` |

As coleções entram e saem copiadas: alterar a array informada, ou a devolvida, não altera o objeto.

### Conversões implícitas

Cada objeto converte nos dois sentidos com o tipo que representa — o `DelimitedString` converte com os
três: `string`, `string[]` e `List<string>`:

```csharp
Cpf cpf = "529.982.247-25";   // string -> value object
string numero = cpf;          // value object -> string
```

A conversão **de saída** exige uma instância: converter uma referência nula lança
`InternalServerErrorException` com `Invalid.Parameter;null`, em vez de `NullReferenceException` sem
contexto.

A conversão **de entrada** cria o objeto e valida. Ela não lança por dado inválido — produz um objeto com
notificações, e cabe a quem recebe consultar `IsValid`.

---

## 📝 Exemplos de Uso

### Validando na entrada

```csharp
using Tooark.ValueObjects;

var cpf = new Cpf(dto.Cpf);
var email = new Email(dto.Email);

if (!cpf.IsValid || !email.IsValid)
{
  // ["Field.Invalid;Document", "Field.Invalid;Email"] — chaves, ainda sem tradução
  IEnumerable<string> erros = [.. cpf.Messages, .. email.Messages];

  return BadRequest(erros);
}
```

As mensagens saem como chave, não como texto final. Para devolvê-las traduzidas, entregue as
notificações ao `ResponseDto` do
[`Tooark.Dtos`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Dtos), que resolve o idioma do
consumidor:

```csharp
var notificacoes = cpf.Notifications.Concat(email.Notifications).ToList();

// {"Errors": ["O campo Document é inválido", "O campo E-mail é inválido"], ...}
return BadRequest(new ResponseDto<string>(notificacoes));
```

### Compondo em uma entidade

```csharp
using Tooark.Notifications;
using Tooark.ValueObjects;

public sealed class Pessoa : Notification
{
  public Pessoa(string nome, string email, string cpf)
  {
    var nomeVo = new Name(nome);
    var emailVo = new Email(email);
    var cpfVo = new Cpf(cpf);

    // As notificações dos objetos de valor sobem para a entidade
    AddNotifications(nomeVo, emailVo, cpfVo);

    if (IsValid)
    {
      Nome = nomeVo;
      Email = emailVo;
      Cpf = cpfVo;
    }
  }

  public Name Nome { get; } = null!;
  public Email Email { get; } = null!;
  public Cpf Cpf { get; } = null!;
}
```

### Busca com o valor normalizado

```csharp
using Tooark.ValueObjects;

var titulo = new Title("Ação e Reação");

titulo.Value       // "Ação e Reação"
titulo.Normalized  // "ACAOEREACAO"
```

### Objeto inválido em texto

```csharp
using Tooark.ValueObjects;

var cpf = new Cpf("111");

cpf.IsValid          // false
cpf.Number           // ""
cpf.ToString()       // "" — nunca nulo
$"documento: {cpf}"  // "documento: "
```

### Trabalhando com string delimitada

```csharp
using Tooark.ValueObjects;

DelimitedString tags = "csharp;dotnet;tooark";

tags.Values   // ["csharp", "dotnet", "tooark"]
tags.Value    // "csharp;dotnet;tooark"

DelimitedString outras = new[] { "a", "b" };
string texto = outras;   // "a;b"
```

---

## 📋 Dependências

| Pacote                                                                        | Versão | Descrição                         |
| ----------------------------------------------------------------------------- | ------ | --------------------------------- |
| [`Tooark.Enums`](https://www.nuget.org/packages/Tooark.Enums)                 | 4.x    | Tipos de documento                |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)       | 4.x    | Erro das conversões sem instância |
| [`Tooark.Extensions`](https://www.nuget.org/packages/Tooark.Extensions)       | 4.x    | Normalização de texto             |
| [`Tooark.Notifications`](https://www.nuget.org/packages/Tooark.Notifications) | 4.x    | Base de notificações              |
| [`Tooark.Validations`](https://www.nuget.org/packages/Tooark.Validations)     | 4.x    | Regras de validação               |

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.ValueObjects](https://github.com/Tooark/nuget-tooark/issues).

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
