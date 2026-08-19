# Tooark.Attributes

Biblioteca com validadores de atributos para propriedades ou campos, integrados ao `System.ComponentModel.DataAnnotations`.

## Instalação

```bash
dotnet add package Tooark.Attributes
```

## Conteúdo

- [DocumentValidationAttribute](#1-validação-de-documento)
- [EmailValidationAttribute](#2-validação-de-email)
- [LinkVideoValidationAttribute](#3-validação-de-link-de-vídeo)
- [PasswordValidationAttribute](#4-validação-de-senha)
- [UrlValidationAttribute](#5-validação-de-url)
- [ZipCodeValidationAttribute](#6-validação-de-código-postal)
- [Comportamento comum](#comportamento-comum)

## Atributos de Validação

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

## Comportamento comum

Todos os atributos herdam de `TooarkValidationAttribute` e compartilham as regras abaixo.

**Mensagens de erro.** A mensagem é devolvida no `ValidationResult` de cada validação, e nunca gravada em `ErrorMessage`. Isso importa porque o framework de validação reaproveita a mesma instância do atributo em todas as validações daquele campo, inclusive concorrentes: gravar no atributo misturaria a mensagem de uma requisição com a de outra.

Sem configuração, a mensagem é uma chave de tradução no formato `Chave;Campo`:

| Situação                      | Mensagem                 |
| ----------------------------- | ------------------------ |
| Campo não informado           | `Field.Required;{campo}` |
| Valor não corresponde à regra | `Field.Invalid;{campo}`  |

Se você configurar `ErrorMessage` ou `ErrorMessageResourceName` no atributo, **essa mensagem é usada no lugar da chave**.

**Valor ausente.** Valor nulo, vazio ou composto apenas por espaços é reportado como `Field.Required`. Ou seja, os atributos **implicam obrigatoriedade** — eles não seguem a convenção do `DataAnnotations`, em que validadores que não são `[Required]` aceitam nulo. Para um campo opcional, valide fora do atributo ou aplique-o condicionalmente.

**Valores que não são texto.** O valor é convertido com `ToString()` antes da validação, então um `Uri` ou um tipo próprio com `ToString()` adequado funciona.

**Tempo limite das expressões regulares.** Cada expressão regular roda com limite de 300 ms. Entrada que provoca retrocesso excessivo é **reprovada**, e não deixa a exceção subir do atributo.

**Erro de configuração.** Configuração impossível — tipo de documento desconhecido, link de vídeo sem provedor — lança `InternalServerErrorException` na primeira validação. Não é falha do dado, e sim do atributo aplicado no código; as chaves estão em `Tooark.Attributes.Messages.AttributeErrorMessages`.

## Exemplo de Uso

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

## Dependências

- [Tooark.Enums](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Enums)
- [Tooark.Exceptions](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Exceptions)
- [Tooark.Validations](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Validations)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Attributes](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/tooark-cs/refs/heads/main/LICENSE) para mais detalhes.
