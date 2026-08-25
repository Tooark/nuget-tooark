# Tooark.Attributes

Biblioteca com validadores de atributos para propriedades ou campos.

## Conteúdo

- [DocumentValidationAttribute](#1-validação-de-documento)
- [EmailValidationAttribute](#2-validação-de-email)
- [PasswordValidationAttribute](#3-validação-de-senha)
- [UrlValidationAttribute](#4-validação-de-url)
- [ZipCodeValidationAttribute](#5-validação-de-código-postal)

## Atributos de Validação

### 1. Validação de Documento

**Funcionalidade:**
Atributo de validação de documento.

- **Parâmetros:**
  - `EDocumentType type`: Tipo de documento a ser validado.

[**Exemplo de Uso**](#validação-de-documento)

### 2. Validação de Email

**Funcionalidade:**
Atributo de validação de email.

[**Exemplo de Uso**](#validação-de-email)

### 3. Validação de Senha

**Funcionalidade:**
Valida se a senha atende aos critérios de complexidade especificados. Padrão é exigir pelo menos um caractere minúsculo, maiúsculo, numérico, caractere especial e ter um comprimento mínimo de 8 caracteres.

**Parâmetros:**

- `bool lowercase`: Exige carácter minúsculo. Padrão: true.
- `bool uppercase`: Exige carácter maiúsculo. Padrão: true.
- `bool number`: Exige carácter numérico. Padrão: true.
- `bool symbol`: Exige carácter especial. Padrão: true.
- `int length`: Tamanho mínimo da senha. Padrão: 8.

[**Exemplo de Uso**](#validação-de-senha)

### 4. Validação de URL

**Funcionalidade:**
Valida se o valor é uma URL válida.

[**Exemplo de Uso**](#validação-de-url)

### 5. Validação de Código Postal

**Funcionalidade:**
Valida se o valor é um código postal válido.

[**Exemplo de Uso**](#validação-de-código-postal)

## Exemplo de Uso

### Validação de Documento

```csharp
using Tooark.Attributes;

public class Pessoa
{
  [DocumentValidation(EDocumentType.CPF)]
  public string Documento { get; set; }
}
```

### Validação de Email

```csharp
using Tooark.Attributes;

public class Contato
{
  [EmailValidation]
  public string Email { get; set; }
}
```

### Validação de Senha

**Validação de Senha com critérios padrão:**

```csharp
using Tooark.Attributes;

public class Usuario
{
  [PasswordValidation]
  public string Senha { get; set; }
}
```

**Validação utilizando critérios:**

```csharp
using Tooark.Attributes;

public class Usuario
{
  [PasswordValidation(lowercase: true, uppercase: true, number: true, symbol: true, length: 8)]
  public string Senha { get; set; }
}
```

### Validação de URL

```csharp
using Tooark.Attributes;

public class Website
{
  [UrlValidation]
  public string Url { get; set; }
}
```

### Validação de Código Postal

```csharp
using Tooark.Attributes;

public class Endereco
{
  [ZipCodeValidation]
  public string CodigoPostal { get; set; }
}
```

## Dependências

- [Tooark.Enums](../Tooark.Enums/README.md)
- [Tooark.Validations](../Tooark.Validations/README.md)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Attributes](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
