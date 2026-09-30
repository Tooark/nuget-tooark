# Tooark.Attributes

Biblioteca com validadores de atributos para propriedades, campos ou parâmetros, integrados ao `System.ComponentModel.DataAnnotations`.

📖 **Documentação:** [Tooark.Attributes no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.attributes.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Attributes/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Instalação](#-instalação)
- [Atributos de Validação](#-atributos-de-validação)
  - [DocumentValidationAttribute](#1-validação-de-documento)
  - [EmailValidationAttribute](#2-validação-de-email)
  - [LinkVideoValidationAttribute](#3-validação-de-link-de-vídeo)
  - [PasswordValidationAttribute](#4-validação-de-senha)
  - [UrlValidationAttribute](#5-validação-de-url)
  - [ZipCodeValidationAttribute](#6-validação-de-código-postal)
- [Comportamento comum](#-comportamento-comum)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Attributes
```

---

## ✅ Atributos de Validação

Todos os atributos aceitam `propertyName`, que define o nome do campo usado na mensagem de erro.

### 1. Validação de Documento

**Funcionalidade:**
Valida o documento pelo formato, com expressão regular, e pelos dígitos verificadores.

**Parâmetros:**

- `string type`: Tipo de documento a ser validado. Obrigatório.
- `string propertyName`: Nome do campo na mensagem de erro. Padrão: `"Document"`.

O tipo é recebido como **texto** porque argumento de atributo aceita apenas constante — um parâmetro do tipo `EDocumentType` impediria o atributo de ser aplicado (`CS0181`). Valores aceitos, sem diferenciar caixa: `CPF`, `RG`, `CNH`, `CNPJ`, `CPF_CNPJ`, `CPF_RG`, `CPF_RG_CNH` e `None`.

Um tipo não reconhecido é erro de configuração e faz o atributo lançar `InternalServerErrorException` na primeira validação, com a mensagem `Attributes.DocumentTypeUnknown;{tipo}`.

[**Exemplo de Uso**](#validação-de-documento)

### 2. Validação de Email

**Funcionalidade:**
Valida se o valor é um endereço de email válido.

**Parâmetros:**

- `string propertyName`: Nome do campo na mensagem de erro. Padrão: `"Email"`.

[**Exemplo de Uso**](#validação-de-email)

### 3. Validação de Link de Vídeo

**Funcionalidade:**
Valida se o valor é um link de vídeo de algum dos provedores habilitados.

**Parâmetros:**

- `string propertyName`: Nome do campo na mensagem de erro. Padrão: `"Link"`.
- `bool youtube`: Permite link do YouTube. Padrão: `true`.
- `bool vimeo`: Permite link do Vimeo. Padrão: `true`.
- `bool dailymotion`: Permite link do Dailymotion. Padrão: `true`.

Desabilitar os três provedores é erro de configuração — nenhum link poderia ser aceito — e faz o atributo lançar `InternalServerErrorException` com a mensagem `Attributes.LinkVideoNoProvider;{campo}`.

[**Exemplo de Uso**](#validação-de-link-de-vídeo)

### 4. Validação de Senha

**Funcionalidade:**
Valida se a senha atende aos critérios de complexidade configurados.

**Parâmetros:**

- `bool lowercase`: Exige carácter minúsculo. Padrão: `true`.
- `bool uppercase`: Exige carácter maiúsculo. Padrão: `true`.
- `bool number`: Exige carácter numérico. Padrão: `true`.
- `bool symbol`: Exige carácter especial. Padrão: `true`.
- `int length`: Comprimento mínimo. Padrão: `8`. Valor não positivo assume `1`.
- `string propertyName`: Nome do campo na mensagem de erro. Padrão: `"Password"`.

Os critérios valem exatamente como configurados. Desabilitar todos significa exigir **apenas o comprimento**, que é uma política legítima: senhas longas sem regra de composição.

[**Exemplo de Uso**](#validação-de-senha)

### 5. Validação de URL

**Funcionalidade:**
Valida se o valor é uma URL válida nos protocolos de email (envio e recebimento), FTP, HTTP ou WebSocket.

**Parâmetros:**

- `string propertyName`: Nome do campo na mensagem de erro. Padrão: `"Url"`.

[**Exemplo de Uso**](#validação-de-url)

### 6. Validação de Código Postal

**Funcionalidade:**
Valida se o valor é um código postal válido.

**Parâmetros:**

- `string propertyName`: Nome do campo na mensagem de erro. Padrão: `"ZipCode"`.

[**Exemplo de Uso**](#validação-de-código-postal)

---

## 🧠 Comportamento comum

Todos os atributos herdam de `TooarkValidationAttribute` e compartilham as regras abaixo.

**Mensagens de erro.** A mensagem é devolvida no `ValidationResult` de cada validação, e nunca gravada em `ErrorMessage`. Isso importa porque o framework de validação reaproveita a mesma instância do atributo em todas as validações daquele campo, inclusive concorrentes: gravar no atributo misturaria a mensagem de uma requisição com a de outra.

Sem configuração, a mensagem é uma chave de tradução no formato `Chave;Campo`:

| Situação                      | Mensagem                 |
| ----------------------------- | ------------------------ |
| Campo não informado           | `Field.Required;{campo}` |
| Valor não corresponde à regra | `Field.Invalid;{campo}`  |

Se você configurar `ErrorMessage` ou `ErrorMessageResourceName` no atributo, **essa mensagem é usada no lugar da chave**.

**Valor ausente.** Valor nulo, vazio ou composto apenas por espaços é reportado como `Field.Required`. Ou seja, os atributos **implicam obrigatoriedade** — eles não seguem a convenção do `DataAnnotations`, em que validadores que não são `[Required]` aceitam nulo. Para um campo opcional, valide fora do atributo ou aplique-o condicionalmente.

**Onde aplicar.** Os atributos valem em propriedade, campo e parâmetro, como os do `DataAnnotations`. Em um record posicional, aplique o atributo sem alvo, no parâmetro do construtor (`record Dto([EmailValidation] string Email)`). É ali que o MVC procura a validação de um record, e ele lança `InvalidOperationException` quando a encontra na propriedade gerada (`[property: EmailValidation]`). O parâmetro de uma action também aceita o atributo (`[FromQuery][EmailValidation] string email`). O `Validator.TryValidateObject` segue o caminho inverso: só lê propriedades, e ignora o atributo no parâmetro do construtor. Um record validado fora do MVC precisa do atributo na propriedade, que o MVC recusa, então o mesmo record não atende aos dois caminhos.

**Obrigatoriedade implícita do ASP.NET Core.** Com `<Nullable>enable</Nullable>`, o MVC trata todo tipo de referência não anulável como se tivesse `[Required]`, com a mensagem do framework (`The Email field is required.`), que não é chave de tradução e chega ao cliente como está. Como o atributo já reporta o valor ausente, um campo não anulável ausente recebe as duas mensagens. Para ficar só com a chave do atributo, registre o `AddTooarkValidationAttributes()` do [`Tooark.AspNetCore`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.AspNetCore/README.pt-BR.md#atributos-do-tooark-na-validação-do-mvc). O MVC deixa de inferir o `[Required]` só nos membros com atributo do Tooark, e mantém a inferência nos demais e o `[Required]` declarado. Sem o `Tooark.AspNetCore`, há duas saídas manuais:

- declare o campo como anulável (`[EmailValidation] string? Email`). O MVC não infere o `[Required]`, e o atributo continua recusando o valor ausente. Vale só para aquele campo; o custo é o tipo anulável no código que lê o DTO;
- ou desligue a inferência na aplicação inteira, com `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true` nas `MvcOptions`. Alcança todos os DTOs: um campo sem atributo do Tooark deixa de ser obrigatório até receber `[Required]` explícito.

**Valores que não são texto.** O valor é convertido com `ToString()` antes da validação, então um `Uri` ou um tipo próprio com `ToString()` adequado funciona.

**Tempo limite das expressões regulares.** Cada expressão regular roda com limite de 300 ms. Entrada que provoca retrocesso excessivo é **reprovada**, e não deixa a exceção subir do atributo.

**Erro de configuração.** Configuração impossível — tipo de documento desconhecido, link de vídeo sem provedor — lança `InternalServerErrorException` na primeira validação. Não é falha do dado, e sim do atributo aplicado no código; as chaves estão em `Tooark.Attributes.Messages.AttributeErrorMessages`.

---

## 📝 Exemplos de Uso

### Validação de Documento

```csharp
using Tooark.Attributes;

public class Pessoa
{
  [DocumentValidation("CPF")]
  public string Cpf { get; set; } = null!;

  [DocumentValidation("CPF_CNPJ", propertyName: "Documento")]
  public string Documento { get; set; } = null!;
}
```

### Validação de Email

```csharp
using Tooark.Attributes;

public class Contato
{
  [EmailValidation]
  public string Email { get; set; } = null!;

  // Com mensagem própria, que tem precedência sobre a chave padrão
  [EmailValidation(ErrorMessage = "Informe um e-mail corporativo")]
  public string EmailCorporativo { get; set; } = null!;
}
```

### Validação de Link de Vídeo

```csharp
using Tooark.Attributes;

public class Aula
{
  [LinkVideoValidation]
  public string Video { get; set; } = null!;

  // Apenas YouTube, com nome de campo próprio na mensagem
  [LinkVideoValidation("Apresentacao", youtube: true, vimeo: false, dailymotion: false)]
  public string Apresentacao { get; set; } = null!;
}
```

### Validação de Senha

```csharp
using Tooark.Attributes;

public class Credencial
{
  [PasswordValidation]
  public string Senha { get; set; } = null!;

  // Frase secreta: sem regra de composição, com comprimento mínimo de 20
  [PasswordValidation(false, false, false, false, 20)]
  public string FraseSecreta { get; set; } = null!;
}
```

### Validação de URL

```csharp
using Tooark.Attributes;

public class Site
{
  [UrlValidation]
  public string Endereco { get; set; } = null!;
}
```

### Validação de Código Postal

```csharp
using Tooark.Attributes;

public class Endereco
{
  [ZipCodeValidation]
  public string Cep { get; set; } = null!;
}
```

### Records e parâmetros de action

```csharp
using Microsoft.AspNetCore.Mvc;
using Tooark.Attributes;

// Record posicional: o atributo vai no parâmetro do construtor, sem alvo
public sealed record CriarContatoDto(
  [EmailValidation] string Email,
  [DocumentValidation("CPF")] string Cpf
);

[ApiController]
[Route("contatos")]
public sealed class ContatoController : ControllerBase
{
  [HttpPost]
  public IActionResult Criar([FromBody] CriarContatoDto dto) => Ok();

  // Parâmetro de action validado direto pelo atributo
  [HttpGet]
  public IActionResult Buscar([FromQuery][EmailValidation] string email) => Ok();
}
```

### Lendo o resultado da validação

```csharp
using System.ComponentModel.DataAnnotations;

var pessoa = new Pessoa { Cpf = "11111111111" };
var resultados = new List<ValidationResult>();

Validator.TryValidateObject(pessoa, new ValidationContext(pessoa), resultados, true);

foreach (var resultado in resultados)
{
  // resultado.ErrorMessage -> "Field.Invalid;Document"
  // resultado.MemberNames  -> ["Cpf"]
}
```

---

## 📋 Dependências

- [`Tooark.Enums`](https://www.nuget.org/packages/Tooark.Enums)
- [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)
- [`Tooark.Validations`](https://www.nuget.org/packages/Tooark.Validations)

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
