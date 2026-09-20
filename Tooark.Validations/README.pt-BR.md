# Tooark.Validations

Biblioteca para validação de tipos e padrões, fornecendo métodos para garantir a integridade e conformidade dos dados para projetos .NET.

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Validations/README.md) · 🇧🇷 **Português (este arquivo)**

## Conteúdo

- [Validação de Boolean](#1-booleano)
- [Validação de Datas](#2-datas)
- [Validação de Decimal](#3-decimal)
- [Validação de Documentos](#4-documentos)
- [Validação de Double](#5-double)
- [Validação de Email](#6-email)
- [Validação de Float](#7-float)
- [Validação de Guid](#8-guid)
- [Validação de Int](#9-int)
- [Validação de Link de Vídeo](#10-link-de-vídeo)
- [Validação de Listas](#11-listas)
- [Validação de Long](#12-long)
- [Validação de Rede (Network)](#13-rede-network)
- [Validação de Objeto](#14-objeto)
- [Validação de Protocolo](#15-protocolo)
- [Validação de Regex](#16-regex)
- [Validação de String](#17-string)
- [Validação de TimeSpan](#18-timespan)
- [Validação de Tipo](#19-tipos)
- [Exemplos de Uso](#exemplos-de-uso)
- [Métodos Disponíveis](#métodos-disponíveis)
- [Mensagens de Erro](#mensagens-de-erro)
- [Códigos de Erro](#códigos-de-erro)
- [Dependências](#dependências)
- [Contribuição](#contribuição)
- [Licença](#licença)

## Instalação

```bash
dotnet add package Tooark.Validations
```

O pacote não tem configuração: `Validation` é usada por composição, criando uma instância e encadeando as
verificações, ou por herança, em classes que já derivam de `Notification` — como as entidades e os value
objects do Tooark.

## Validações

As validações disponíveis são:

### 1. Booleano

**Funcionalidade:**
Validações para valores booleanos.

[**Exemplo de Uso**](#booleano)

### 2. Datas

**Funcionalidade:**
Validações para valores de data.

[**Exemplo de Uso**](#datas)

### 3. Decimal

**Funcionalidade:**
Validações para valores decimais.

[**Exemplo de Uso**](#decimal)

### 4. Documentos

**Funcionalidade:**
Validações para documentos.

**Tipos de Documentos:**

| Documento        | Validação                                |
| ---------------- | ---------------------------------------- |
| `CPF`            | Formato e dígitos verificadores          |
| `CNPJ`           | Formato e dígitos verificadores          |
| `RG`             | Apenas formato                           |
| `CNH`            | Apenas formato                           |
| `CPF ou RG`      | CPF com dígitos, ou RG por formato       |
| `CPF, RG ou CNH` | CPF com dígitos, ou RG e CNH por formato |
| `CPF ou CNPJ`    | Ambos com dígitos verificadores          |

CPF e CNPJ são validados por formato **e** por dígitos verificadores. Sequências de um único caractere repetido (`111.111.111-11`, `00.000.000/0000-00`) satisfazem o módulo 11 e são rejeitadas por regra própria. RG e CNH não possuem dígito verificador de padrão nacional e seguem apenas por formato.

**CNPJ alfanumérico:**
O cálculo segue o material publicado pelo Serpro, vigente desde julho de 2026: os doze primeiros caracteres admitem letras e dígitos, e os dois verificadores permanecem numéricos. O valor de cada caractere é o código ASCII menos 48 — dígitos mantêm o próprio valor e letras assumem de 17 (`A`) a 42 (`Z`) —, com pesos de 2 a 9 distribuídos da direita para a esquerda, recomeçando após o oitavo caractere. Como os dígitos preservam o valor, o mesmo cálculo atende aos CNPJ numéricos anteriores.

A caixa das letras é normalizada antes do cálculo, então `12.abc.345/01DE-35` e `12.ABC.345/01DE-35` são o mesmo CNPJ.

A verificação também está disponível fora da validação, por `DocumentDigit.IsCpf` e `DocumentDigit.IsCnpj`.

[**Exemplo de Uso**](#documentos)

### 5. Double

**Funcionalidade:**
Validações para valores double.

[**Exemplo de Uso**](#double)

### 6. Email

**Funcionalidade:**
Validações para endereços de email.

[**Exemplo de Uso**](#email)

### 7. Float

**Funcionalidade:**
Validações para valores float.

[**Exemplo de Uso**](#float)

### 8. Guid

**Funcionalidade:**
Validações para valores guid.

[**Exemplo de Uso**](#guid)

### 9. Int

**Funcionalidade:**
Validações para valores inteiros.

[**Exemplo de Uso**](#int)

### 10. Link de Vídeo

**Funcionalidade:**
Validações para links de vídeo, pelo formato da URL.

**Plataformas:**

| Método                   | Aceita                        |
| ------------------------ | ----------------------------- |
| `IsLinkVideo`            | YouTube, Vimeo ou Dailymotion |
| `IsLinkVideoYouTube`     | Apenas YouTube                |
| `IsLinkVideoVimeo`       | Apenas Vimeo                  |
| `IsLinkVideoDailymotion` | Apenas Dailymotion            |

A validação é de **formato da URL**, não de existência do vídeo: um link bem formado de um vídeo removido
continua sendo aprovado.

[**Exemplo de Uso**](#link-de-vídeo)

### 11. Listas

**Funcionalidade:**
Validações para listas.

[**Exemplo de Uso**](#listas)

### 12. Long

**Funcionalidade:**
Validações para valores long.

[**Exemplo de Uso**](#long)

### 13. Rede (Network)

**Funcionalidade:**
Validações para endereços de rede.

**Tipos de Endereços:**

- `IP`
- `IPv4`
- `IPv6` — aceita a forma completa e as formas comprimidas (`::1`, `2001:db8::1`)
- `MacAddress`

[**Exemplo de Uso**](#rede-network)

### 14. Objeto

**Funcionalidade:**
Validações para objetos.

[**Exemplo de Uso**](#objeto)

### 15. Protocolo

**Funcionalidade:**
Validações para protocolos.

**Tipos de Protocolos:**

- `Url`
- `Ftp`
- `Sftp`
- `ProtocolFtp`
- `Http`
- `Https`
- `ProtocolHttp`
- `Imap`
- `Pop3`
- `ProtocolEmailReceiver`
- `Smtp`
- `ProtocolEmailSender`
- `Ws`
- `Wss`
- `ProtocolWebSocket`

[**Exemplo de Uso**](#protocolo)

### 16. Regex

**Funcionalidade:**
Validações para expressões regulares. O tempo limite padrão de avaliação é a constante pública
`Validation.DefaultTimeout` (300 ms), disponível para consulta externa.

[**Exemplo de Uso**](#regex)

### 17. String

**Funcionalidade:**
Validações para strings.

[**Exemplo de Uso**](#string)

### 18. TimeSpan

**Funcionalidade:**
Validações para valores de tempo.

[**Exemplo de Uso**](#timespan)

### 19. Tipos

**Funcionalidade:**
Validações para tipos de string.

**Tipos de Disponíveis:**

- `Guid`
- `Letter`
- `LetterLower`
- `LetterUpper`
- `Numeric`
- `LetterNumeric`
- `Hexadecimal`
- `ZipCode`
- `Base64`
- `Password`
- `Culture`
- `CultureIgnoreCase`

[**Exemplo de Uso**](#tipos)

## Exemplos de Uso

### Booleano

```csharp
using Tooark.Validations;

bool value = true;
string property = "Booleano";
var validation = new Validation()
    .IsTrue(value, property, "O valor deve ser verdadeiro.")
    .IsFalse(value, property, "O valor deve ser falso.")
    .Contains(value, bool[], property, "O valor deve estar na lista.")
    .NotContains(value, bool[], property, "O valor não deve estar na lista.")
    .All(value, bool[], property, "Todos os valores da lista devem igual ao valor.")
    .NotAll(value, bool[], property, "Nenhum valor da lista deve igual ao valor.")
    .IsNull(value, property, "O valor deve ser nulo.")
    .IsNotNull(value, property, "O valor não deve ser nulo.")
```

### Datas

```csharp
using Tooark.Validations;

DateTime date = DateTime.Now;
string property = "Data";
var validation = new Validation()
    .IsGreater(date, Comparer, property, "A data deve ser maior que a data comparada.")
    .IsGreaterOrEquals(date, Comparer, property, "A data deve ser maior ou igual que a data comparada.")
    .IsLower(date, Comparer, property, "A data deve ser menor que a data comparada.")
    .IsLowerOrEquals(date, Comparer, property, "A data deve ser menor ou igual que a data comparada.")
    .IsBetween(date, Start, End, property, "A data deve estar entre as datas.")
    .IsNotBetween(date, Start, End, property, "A data não deve estar entre as datas.")
    .IsMin(date, property, "A data deve ser o valor mínimo do tipo.")
    .IsNotMin(date, property, "A data não deve ser o valor mínimo do tipo.")
    .IsMax(date, property, "A data deve ser o valor máximo do tipo.")
    .IsNotMax(date, property, "A data não deve ser o valor máximo do tipo.")
    .AreEquals(date, Comparer, property, "As datas devem ser iguais.")
    .AreNotEquals(date, Comparer, property, "As datas não devem ser iguais.")
    .Contains(date, Datetime[], property, "A data deve estar na lista.")
    .NotContains(date, Datetime[], property, "A data não deve estar na lista.")
    .All(date, Datetime[], property, "Todos os valores da lista devem ser iguais a data.")
    .NotAll(date, Datetime[], property, "Nenhum valor da lista deve ser igual a data.")
    .IsNull(date, property, "A data deve ser nula.")
    .IsNotNull(date, property, "A data não deve ser nula.")
```

### Decimal

```csharp
using Tooark.Validations;

decimal value = 10.5m;
string property = "Decimal";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "O valor deve ser maior que o valor comparado.")
    .IsGreaterOrEquals(value, Comparer, property, "O valor deve ser maior ou igual que o valor comparado.")
    .IsLower(value, Comparer, property, "O valor deve ser menor que o valor comparado.")
    .IsLowerOrEquals(value, Comparer, property, "O valor deve ser menor ou igual que o valor comparado.")
    .IsBetween(value, Start, End, property, "O valor deve estar entre os valores.")
    .IsNotBetween(value, Start, End, property, "O valor não deve estar entre os valores.")
    .IsMin(value, property, "O valor deve ser o valor mínimo do tipo.")
    .IsNotMin(value, property, "O valor não deve ser o valor mínimo do tipo.")
    .IsMax(value, property, "O valor deve ser o valor máximo do tipo.")
    .IsNotMax(value, property, "O valor não deve ser o valor máximo do tipo.")
    .AreEquals(value, Comparer, property, "Os valores devem ser iguais.")
    .AreNotEquals(value, Comparer, property, "Os valores não devem ser iguais.")
    .Contains(value, decimal[], property, "O valor deve estar na lista.")
    .NotContains(value, decimal[], property, "O valor não deve estar na lista.")
    .All(value, decimal[], property, "Todos os valores da lista devem ser iguais o valor.")
    .NotAll(value, decimal[], property, "Nenhum valor da lista deve ser igual o valor.")
    .IsNull(value, property, "O valor deve ser nulo.")
    .IsNotNull(value, property, "O valor não deve ser nulo.");
```

### Documentos

```csharp
using Tooark.Validations;

string property = "Document";
var validation = new Validation()
    .IsCpf(document, property, "Tem que ser um CPF válido")
    .IsRg(document, property, "Tem que ser um RG válido")
    .IsCnh(document, property, "Tem que ser um CNH válido")
    .IsCpfRg(document, property, "Tem que ser um CPF ou RG válido")
    .IsCpfRgCnh(document, property, "Tem que ser um CPF, RG ou CNH válido")
    .IsCnpj(document, property, "Tem que ser um CNPJ válido")
    .IsCpfCnpj(document, property, "Tem que ser um CPF ou CNPJ válido").
```

### Double

```csharp
using Tooark.Validations;

double value = 10.5;
string property = " Double";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "O valor deve ser maior que o valor comparado.")
    .IsGreaterOrEquals(value, Comparer, property, "O valor deve ser maior ou igual que o valor comparado.")
    .IsLower(value, Comparer, property, "O valor deve ser menor que o valor comparado.")
    .IsLowerOrEquals(value, Comparer, property, "O valor deve ser menor ou igual que o valor comparado.")
    .IsBetween(value, Start, End, property, "O valor deve estar entre os valores.")
    .IsNotBetween(value, Start, End, property, "O valor não deve estar entre os valores.")
    .IsMin(value, property, "O valor deve ser o valor mínimo do tipo.")
    .IsNotMin(value, property, "O valor não deve ser o valor mínimo do tipo.")
    .IsMax(value, property, "O valor deve ser o valor máximo do tipo.")
    .IsNotMax(value, property, "O valor não deve ser o valor máximo do tipo.")
    .AreEquals(value, Comparer, property, "Os valores devem ser iguais.")
    .AreNotEquals(value, Comparer, property, "Os valores não devem ser iguais.")
    .Contains(value, double[], property, "O valor deve estar na lista.")
    .NotContains(value, double[], property, "O valor não deve estar na lista.")
    .All(value, double[], property, "Todos os valores da lista devem ser iguais o valor.")
    .NotAll(value, double[], property, "Nenhum valor da lista deve ser igual o valor.")
    .IsNull(value, property, "O valor deve ser nulo.")
    .IsNotNull(value, property, "O valor não deve ser nulo.");
```

### Email

```csharp
using Tooark.Validations;

string email = "exemplo@dominio.com";
string property = "Email";
var validation = new Validation()
    .IsEmail(email, property, "Tem que ser um email válido")
    .IsEmailOrEmpty(email, property, "Tem que ser um email válido ou vazio");
```

### Domínio de Email

```csharp
using Tooark.Validations;

string email = "@domain.com";
string property = "EmailDomain";
var validation = new Validation()
    .IsEmailDomain(email, property, "Tem que ser domínio de email válido")
    .IsEmailDomainOrEmpty(email, property, "Tem que ser domínio de email válido ou vazio");
```

### Float

```csharp
using Tooark.Validations;

float value = 10.5f;
string property = "Float";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "O valor deve ser maior que o valor comparado.")
    .IsGreaterOrEquals(value, Comparer, property, "O valor deve ser maior ou igual que o valor comparado.")
    .IsLower(value, Comparer, property, "O valor deve ser menor que o valor comparado.")
    .IsLowerOrEquals(value, Comparer, property, "O valor deve ser menor ou igual que o valor comparado.")
    .IsBetween(value, Start, End, property, "O valor deve estar entre os valores.")
    .IsNotBetween(value, Start, End, property, "O valor não deve estar entre os valores.")
    .IsMin(value, property, "O valor deve ser o valor mínimo do tipo.")
    .IsNotMin(value, property, "O valor não deve ser o valor mínimo do tipo.")
    .IsMax(value, property, "O valor deve ser o valor máximo do tipo.")
    .IsNotMax(value, property, "O valor não deve ser o valor máximo do tipo.")
    .AreEquals(value, Comparer, property, "Os valores devem ser iguais.")
    .AreNotEquals(value, Comparer, property, "Os valores não devem ser iguais.")
    .Contains(value, float[], property, "O valor deve estar na lista.")
    .NotContains(value, float[], property, "O valor não deve estar na lista.")
    .All(value, float[], property, "Todos os valores da lista devem ser iguais o valor.")
    .NotAll(value, float[], property, "Nenhum valor da lista deve ser igual o valor.")
    .IsNull(value, property, "O valor deve ser nulo.")
    .IsNotNull(value, property, "O valor não deve ser nulo.");
```

### Guid

```csharp
using Tooark.Validations;

Guid guid = Guid.NewGuid();
string property = "Guid";
var validation = new Validation()
    .AreEquals(guid, Comparer, property, "Os valores devem ser iguais.")
    .AreNotEquals(guid, Comparer, property, "Os valores não devem ser iguais.")
    .Contains(guid, Guid[], property, "O valor deve estar na lista.")
    .NotContains(guid, Guid[], property, "O valor não deve estar na lista.")
    .All(guid, Guid[], property, "Todos os valores da lista devem ser iguais o valor.")
    .NotAll(guid, Guid[], property, "Nenhum valor da lista deve ser igual o valor.")
    .IsNull(guid, property, "O valor deve ser nulo.")
    .IsNotNull(guid, property, "O valor não deve ser nulo.")
    .IsEmpty(guid, property, "O valor deve ser vazio.")
    .IsNotEmpty(guid, property, "O valor não deve ser vazio.");
```

### Int

```csharp
using Tooark.Validations;

int value = 10;
string property = "Int";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "O valor deve ser maior que o valor comparado.")
    .IsGreaterOrEquals(value, Comparer, property, "O valor deve ser maior ou igual que o valor comparado.")
    .IsLower(value, Comparer, property, "O valor deve ser menor que o valor comparado.")
    .IsLowerOrEquals(value, Comparer, property, "O valor deve ser menor ou igual que o valor comparado.")
    .IsBetween(value, Start, End, property, "O valor deve estar entre os valores.")
    .IsNotBetween(value, Start, End, property, "O valor não deve estar entre os valores.")
    .IsMin(value, property, "O valor deve ser o valor mínimo do tipo.")
    .IsNotMin(value, property, "O valor não deve ser o valor mínimo do tipo.")
    .IsMax(value, property, "O valor deve ser o valor máximo do tipo.")
    .IsNotMax(value, property, "O valor não deve ser o valor máximo do tipo.")
    .AreEquals(value, Comparer, property, "Os valores devem ser iguais.")
    .AreNotEquals(value, Comparer, property, "Os valores não devem ser iguais.")
    .Contains(value, int[], property, "O valor deve estar na lista.")
    .NotContains(value, int[], property, "O valor não deve estar na lista.")
    .All(value, int[], property, "Todos os valores da lista devem ser iguais o valor.")
    .NotAll(value, int[], property, "Nenhum valor da lista deve ser igual o valor.")
    .IsNull(value, property, "O valor deve ser nulo.")
    .IsNotNull(value, property, "O valor não deve ser nulo.");
```

### Link de Vídeo

```csharp
using Tooark.Validations;

string property = "Video";
var validation = new Validation()
    .IsLinkVideo(Value, property, "Tem que ser um link de vídeo válido.")
    .IsLinkVideoYouTube(Value, property, "Tem que ser um link do YouTube.")
    .IsLinkVideoVimeo(Value, property, "Tem que ser um link do Vimeo.")
    .IsLinkVideoDailymotion(Value, property, "Tem que ser um link do Dailymotion.");
```

### Listas

```csharp
using Tooark.Validations;

int[] list = [1, 2, 3];
string property = "Valores";
var validation = new Validation()
    .IsGreater(list, Value, property, "A lista tem que ser maior que o valor permitido.")
    .IsGreaterOrEquals(list, Value, property, "A lista tem que ser maior ou igual que o valor permitido.")
    .IsLower(list, Value, property, "A lista tem que ser menor que o valor permitido.")
    .IsLowerOrEquals(list, Value, property, "A lista tem que ser menor ou igual que o valor permitido.")
    .AreEquals(list, ListComparer, property, "As listas tem que ser iguais.")
    .AreNotEquals(list, value, property, "As listas não podem ser iguais.")
    .IsNull(list, property, "A lista tem que ser nula.")
    .IsNotNull(list, property, "A lista não pode ser nula.")
    .IsEmpty(list, value, property, "A lista tem que ser vazia.")
    .IsNotEmpty(list, value, property, "A lista não pode ser vazia.");
```

### Long

```csharp
using Tooark.Validations;

long value = 10L;
string property = "Long";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "O valor deve ser maior que o valor comparado.")
    .IsGreaterOrEquals(value, Comparer, property, "O valor deve ser maior ou igual que o valor comparado.")
    .IsLower(value, Comparer, property, "O valor deve ser menor que o valor comparado.")
    .IsLowerOrEquals(value, Comparer, property, "O valor deve ser menor ou igual que o valor comparado.")
    .IsBetween(value, Start, End, property, "O valor deve estar entre os valores.")
    .IsNotBetween(value, Start, End, property, "O valor não deve estar entre os valores.")
    .IsMin(value, property, "O valor deve ser o valor mínimo do tipo.")
    .IsNotMin(value, property, "O valor não deve ser o valor mínimo do tipo.")
    .IsMax(value, property, "O valor deve ser o valor máximo do tipo.")
    .IsNotMax(value, property, "O valor não deve ser o valor máximo do tipo.")
    .AreEquals(value, Comparer, property, "Os valores devem ser iguais.")
    .AreNotEquals(value, Comparer, property, "Os valores não devem ser iguais.")
    .Contains(value, long[], property, "O valor deve estar na lista.")
    .NotContains(value, long[], property, "O valor não deve estar na lista.")
    .All(value, long[], property, "Todos os valores da lista devem ser iguais o valor.")
    .NotAll(value, long[], property, "Nenhum valor da lista deve ser igual o valor.")
    .IsNull(value, property, "O valor deve ser nulo.")
    .IsNotNull(value, property, "O valor não deve ser nulo.");
```

### Rede (Network)

```csharp
using Tooark.Validations;

string property = "IP";
var validation = new Validation()
    .IsIp(Value, property, "Tem que ser um IP válido.")
    .IsIpv4(Value, property, "Tem que ser um IPV4 válido.")
    .IsIpv6(Value, property, "Tem que ser um IPV6 válido.")
    .IsMacAddress(Value, property, "Tem que ser um MacAddress válido.");
```

### Objeto

```csharp
using Tooark.Validations;

object obj = new object();
string property = "Objeto";
var validation = new Validation()
    .AreEquals(obj, Comparer, property, "Os objetos devem ser iguais.")
    .AreNotEquals(obj, Comparer, property, "Os objetos não devem ser iguais.")
    .IsNull(obj, property, "O objeto deve ser nulo.")
    .IsNotNull(obj, property, "O objeto não deve ser nulo.");
```

### Protocolo

```csharp
using Tooark.Validations;

string property = "Protocolo";
var validation = new Validation()
    .IsUrl(Protocol, property, "O protocolo deve ser uma Url válida.")
    .IsFtp(Protocol, property, "O protocolo deve ser um Ftp válido.")
    .IsSftp(Protocol, property, "O protocolo deve ser um Sftp válido.")
    .IsProtocolFtp(Protocol, property, "O protocolo deve ser um ProtocolFtp válido.")
    .IsHttp(Protocol, property, "O protocolo deve ser um Http válido.")
    .IsHttps(Protocol, property, "O protocolo deve ser um Https válido.")
    .IsProtocolHttp(Protocol, property, "O protocolo deve ser um ProtocolHttp válido.")
    .IsImap(Protocol, property, "O protocolo deve ser um Imap válido.")
    .IsPop3(Protocol, property, "O protocolo deve ser um Pop3 válido.")
    .IsProtocolEmailReceiver(Protocol, property, "O protocolo deve ser um ProtocolEmailReceiver válido.")
    .IsSmtp(Protocol, property, "O protocolo deve ser um Smtp válido.")
    .IsProtocolEmailSender(Protocol, property, "O protocolo deve ser um ProtocolEmailSender válido.")
    .IsWs(Protocol, property, "O protocolo deve ser um Ws válido.")
    .IsWss(Protocol, property, "O protocolo deve ser um Wss válido.")
    .IsProtocolWebSocket(Protocol, property, "O protocolo deve ser um ProtocolWebSocket válido.");
```

### Regex

```csharp
using Tooark.Validations;

string property = "Email";
var validation = new Validation()
    .Match(Value, Pattern, property, "O valor deve corresponder ao padrão.")
    .NotMatch(Value, Pattern, property, "O valor não deve corresponder ao padrão.");
```

### String

```csharp
using Tooark.Validations;

string value = "exemplo";
string property = "Texto";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "O tamanho da string do valor deve ser maior que o valor comparado.")
    .IsGreaterOrEquals(value, Comparer, property, "O tamanho da string do valor deve ser maior ou igual que o valor comparado.")
    .IsLower(value, Comparer, property, "O tamanho da string do valor deve ser menor que o valor comparado.")
    .IsLowerOrEquals(value, Comparer, property, "O tamanho da string do valor deve ser menor ou igual que o valor comparado.")
    .IsBetween(value, Start, End, property, "O tamanho da string do valor deve estar entre os valores.")
    .IsNotBetween(value, Start, End, property, "O tamanho da string do valor não deve estar entre os valores.")
    .AreEquals(value, Comparer, property, "Os valores devem ser iguais.")
    .AreNotEquals(value, Comparer, property, "Os valores não devem ser iguais.")
    .Contains(value, string[], property, "O valor deve estar na lista.")
    .NotContains(value, string[], property, "O valor não deve estar na lista.")
    .All(value, string[], property, "Todos os valores da lista devem ser iguais o valor.")
    .NotAll(value, string[], property, "Nenhum valor da lista deve ser igual o valor.")
    .IsNull(value, property, "O valor deve ser nulo.")
    .IsNotNull(value, property, "O valor não deve ser nulo.")
    .IsNullOrEmpty(value, property, "O valor deve ser nulo ou vazio.")
    .IsNotNullOrEmpty(value, property, "O valor não deve ser nulo ou vazio.")
    .IsNullOrWhiteSpace(value, property, "O valor deve ser nulo, vazio ou espaço em branco.")
    .IsNotNull(value, property, "O valor não deve ser nulo, vazio ou espaço em branco.");
```

### TimeSpan

```csharp
TimeSpan value = TimeSpan.FromHours(2);
string property = "Duração";
var validation = new Validation()
    .IsGreater(value, Comparer, property, "O valor deve ser maior que o valor comparado.")
    .IsGreaterOrEquals(value, Comparer, property, "O valor deve ser maior ou igual que o valor comparado.")
    .IsLower(value, Comparer, property, "O valor deve ser menor que o valor comparado.")
    .IsLowerOrEquals(value, Comparer, property, "O valor deve ser menor ou igual que o valor comparado.")
    .IsBetween(value, Start, End, property, "O valor deve estar entre os valores.")
    .IsNotBetween(value, Start, End, property, "O valor não deve estar entre os valores.")
    .IsMin(value, property, "O valor deve ser o valor mínimo do tipo.")
    .IsNotMin(value, property, "O valor não deve ser o valor mínimo do tipo.")
    .IsMax(value, property, "O valor deve ser o valor máximo do tipo.")
    .IsNotMax(value, property, "O valor não deve ser o valor máximo do tipo.")
    .AreEquals(value, Comparer, property, "Os valores devem ser iguais.")
    .AreNotEquals(value, Comparer, property, "Os valores não devem ser iguais.")
    .Contains(value, TimeSpan[], property, "O valor deve estar na lista.")
    .NotContains(value, TimeSpan[], property, "O valor não deve estar na lista.")
    .All(value, TimeSpan[], property, "Todos os valores da lista devem ser iguais o valor.")
    .NotAll(value, TimeSpan[], property, "Nenhum valor da lista deve ser igual o valor.")
    .IsNull(value, property, "O valor deve ser nulo.")
    .IsNotNull(value, property, "O valor não deve ser nulo.");
```

### Tipos

```csharp
string value = "abc";
string zipCode = "10000-000";
string property = "Tipos";
var validation = new Validation()
    .IsGuid(value, property, "Tem que ser um Guid válido.")
    .IsLetter(value, property, "Tem que ser letras.")
    .IsLetterLower(value, property, "Tem que ser letras minúsculas.")
    .IsLetterUpper(value, property, "Tem que ser letras maiúsculas.")
    .IsNumeric(value, property, "Tem que ser números.")
    .IsLetterNumeric(value, property, "Tem que ser letras ou números.")
    .IsHexadecimal(value, property, "Tem que ser hexadecimal.")
    .IsZipCode(value, property, "Tem que ser um código postal.")
    .IsBase64(value, property, "Tem que ser um Base64.")
    .IsPassword(value, property, "Tem que ser uma senha complexa.")
    .IsPassword(value, 10, property, "Tem que ser uma senha complexa e com no mínimo 10 caracteres.")
    .IsCulture(value, property, "Tem que ser uma cultura.")
    .IsCultureIgnoreCase(value, property, "Tem que ser uma cultura ignorando case sensitive.");
```

## Métodos Disponíveis

A biblioteca `Tooark.Validations` oferece uma ampla gama de métodos de validação, incluindo:

- `Join`: Junta as mensagens de notificação.

- `All`: Validação de todos os valores de uma lista.
- `AreEquals`: Validação de valores iguais.
- `AreNotEquals`: Validação de valores diferentes.
- `Contains`: Validação de valor contido em uma lista ou em uma string.
- `IsBase64`: Validação de Base64.
- `IsBetween`: Validação de valor entre dois valores ou tamanho da lista está entre dois tamanhos.
- `IsCnh`: Validação de CNH.
- `IsCnpj`: Validação de CNPJ.
- `IsCpf`: Validação de CPF.
- `IsCpfCnpj`: Validação de CPF ou CNPJ.
- `IsCpfRg`: Validação de CPF ou RG.
- `IsCpfRgCnh`: Validação de CPF, RG ou CNH.
- `IsCulture` Validação de Cultura.
- `IsCultureIgnoreCase` Validação de Cultura ignorando maiúsculas e minúsculas.
- `IsEmail`: Validação de e-mail.
- `IsEmailDomain`: Validação de domínio de e-mail.
- `IsEmailDomainOrEmpty`: Validação de domínio de e-mail ou vazio. Aceita valor ausente.
- `IsEmailOrEmpty`: Validação de e-mail ou vazio. Aceita valor ausente.
- `IsEmpty`: Validação se é vazia.
- `IsFalse`: Validação de valor falso.
- `IsFtp`: Validação de FTP.
- `IsGreater`: Validação de valor maior ou tamanho da lista maior.
- `IsGreaterOrEquals`: Validação de valor maior ou igual ou tamanho da lista maior ou igual.
- `IsGuid`: Validação de GUID.
- `IsHexadecimal` Validação de hexadecimal.
- `IsHttp`: Validação de HTTP.
- `IsHttps`: Validação de HTTPS.
- `IsImap`: Validação de IMAP.
- `IsIp`: Validação de endereço IPV4 ou IPV6.
- `IsIpv4`: Validação de endereço IPV4.
- `IsIpv6`: Validação de endereço IPV6.
- `IsLetter`: Validação de apenas letras.
- `IsLetterLower` Validação de apenas letras minúsculas.
- `IsLetterNumeric` Validação de letras e números.
- `IsLetterUpper` Validação de apenas letras maiúsculas.
- `IsLinkVideo`: Validação de link de vídeo do YouTube, Vimeo ou Dailymotion.
- `IsLinkVideoDailymotion`: Validação de link de vídeo do Dailymotion.
- `IsLinkVideoVimeo`: Validação de link de vídeo do Vimeo.
- `IsLinkVideoYouTube`: Validação de link de vídeo do YouTube.
- `IsLower`: Validação de valor menor ou tamanho da lista menor.
- `IsLowerOrEquals`: Validação de valor menor ou igual ou tamanho da lista menor ou igual.
- `IsMacAddress`: Validação de endereço MAC.
- `IsMax`: Validação de valor máximo.
- `IsMin`: Validação de valor mínimo.
- `IsNotBetween`: Validação de valor não está entre dois valores ou tamanho da lista não está entre dois tamanhos.
- `IsNotEmpty`: Validação se não é vazia.
- `IsNotMax`: Validação de valor não máximo.
- `IsNotMin`: Validação de valor não mínimo.
- `IsNotNull`: Validação se não é nulo.
- `IsNotNullOrEmpty`: Validação se não é nulo ou vazio.
- `IsNotNullOrWhiteSpace`: Validação se não é nulo, vazio ou espaço em branco.
- `IsNull`: Validação se é nulo.
- `IsNullOrEmpty`: Validação se é nulo ou vazio.
- `IsNullOrWhiteSpace`: Validação se é nulo, vazio ou espaço em branco.
- `IsNumeric` Validação de apenas números.
- `IsPassword`: Validação de Senha.
- `IsPop3`: Validação de POP3.
- `IsProtocolEmailReceiver`: Validação de Protocolo de Recebimento de E-mail.
- `IsProtocolEmailSender`: Validação de Protocolo de Envio de E-mail.
- `IsProtocolFtp`: Validação de Protocolo FTP.
- `IsProtocolHttp`: Validação de Protocolo HTTP.
- `IsProtocolWebSocket`: Validação de Protocolo WebSocket.
- `IsRg`: Validação de RG.
- `IsSftp`: Validação de SFTP.
- `IsSmtp`: Validação de SMTP.
- `IsTrue`: Validação de valor verdadeiro.
- `IsUrl`: Validação de URL.
- `IsWs`: Validação de WS.
- `IsWss`: Validação de WSS.
- `IsZipCode` Validação de Código Postal.
- `Match`: Validação de valor correspondente a um padrão.
- `NotAll`: Validação de nenhum valor de uma lista.
- `NotContains`: Validação de valor não contido em uma lista ou em uma string.
- `NotMatch`: Validação de valor não correspondente a um padrão.

Para uma lista completa de métodos e suas descrições, consulte a documentação XML gerada com a biblioteca.

## Mensagens de Erro

A classe estática `ValidationErrorMessages` reúne as mensagens padrão das validações. Cada método devolve uma
**chave de tradução**, e não o texto final, no formato `Validation.{Regra};{Propriedade}` — a propriedade tem
os espaços em branco removidos.

```csharp
ValidationErrorMessages.BooleanIsFalse("Ativo");   // "Validation.IsNotFalse;Ativo"
```

As sobrecargas sem o parâmetro `message` usam essas chaves; as sobrecargas com `message` usam o texto que
você informar, sem passar por elas.

## Códigos de Erro

Cada notificação carrega um código que identifica a família da validação. Os códigos em uso:

| Família        | Códigos                     |
| -------------- | --------------------------- |
| Booleano       | `T.VLD.BOO1`, `T.VLD.BOO2`  |
| Datas          | `T.VLD.DTT1`, `T.VLD.DTT2`  |
| Decimal        | `T.VLD.DEC1`, `T.VLD.DEC2`  |
| Documentos     | `T.VLD.DOC1`                |
| Double         | `T.VLD.DBL1`, `T.VLD.DBL2`  |
| Float          | `T.VLD.FLT1`, `T.VLD.FLT2`  |
| Guid           | `T.VLD.GUI1` a `T.VLD.GUI4` |
| Int            | `T.VLD.INT1`, `T.VLD.INT2`  |
| Listas         | `T.VLD.LST1` a `T.VLD.LST6` |
| Long           | `T.VLD.LNG1`, `T.VLD.LNG2`  |
| Objeto         | `T.VLD.OBJ1`                |
| Regex          | `T.VLD.RGX1`                |
| String         | `T.VLD.STR1` a `T.VLD.STR7` |
| TimeSpan       | `T.VLD.TMS1`, `T.VLD.TMS2`  |
| Validação nula | `T.VLD.NUL1`                |

Duas observações importantes para quem filtra notificações por código:

- **As validações baseadas em expressão regular compartilham o `T.VLD.RGX1`**: Email, Rede, Protocolo, Tipos,
  Link de Vídeo, e também `IsRg` e `IsCnh`, que são validações apenas de formato.
- **`T.VLD.DOC1` cobre somente os documentos com dígito verificador**: `IsCpf`, `IsCnpj`, `IsCpfCnpj`,
  `IsCpfRg` e `IsCpfRgCnh`. Na v3.3.4 essas validações usavam `T.VLD.RGX1`.

## Dependências

| Dependência                                                                   | Versão | Uso                                  |
| ----------------------------------------------------------------------------- | ------ | ------------------------------------ |
| [`Tooark.Notifications`](https://www.nuget.org/packages/Tooark.Notifications) | 4.x    | `Notification`, base de `Validation` |

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Validations](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
