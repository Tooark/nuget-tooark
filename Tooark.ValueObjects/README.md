# Tooark.ValueObjects

Biblioteca gerenciamento de Value Objects pré-definidos e validados, garantindo a integridade e consistência dos dados para projetos .NET.

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
using Tooark.ValueObjects.Injections;

// Nas suas configurações de serviços
services.AddTooarkValueObjects();
```

## Conteúdo

- [Cpf](#1-cpf)
- [Cnh](#2-cnh)
- [Rg](#3-rg)
- [Cnpj](#4-cnpj)
- [CpfRg](#5-cpfrg)
- [CpfRgCnh](#6-cpfrgcnh)
- [CpfCnpj](#7-cpfcnpj)
- [Document](#8-document)
- [Email](#9-email)
- [EmailDomain](#10-emaildomain)
- [LanguageCode](#11-languagecode)
- [Letter](#12-letter)
- [LetterNumeric](#13-letternumeric)
- [Numeric](#14-numeric)
- [Password](#15-password)
- [ZipCode](#16-zipcode)
- [ProtocolEmailReceiver](#17-protocolemailreceiver)
- [ProtocolEmailSender](#18-protocolemailsender)
- [ProtocolFtp](#19-protocolftp)
- [ProtocolHttp](#20-protocolhttp)
- [ProtocolWs](#21-protocolws)
- [Url](#22-url)
- [Name](#23-name)
- [Title](#24-title)
- [Description](#25-description)
- [Keyword](#26-keyword)
- [DeletedBy](#27-deletedby)
- [RestoredBy](#28-restoredby)
- [FileStorage](#29-filestorage)
- [DelimitedString](#30-delimitedstring)

## Value Objects

Os Value Objects disponíveis são:

### 1. Cpf

**Funcionalidade:**
Representa um CPF (Cadastro de Pessoas Físicas).

**Métodos:**

- `Cpf.Number`: Obtém o valor do número do CPF.
- `ToString()`: Retorna o valor do CPF.
- `string(Cpf document)`: Converte implicitamente um objeto Cpf para uma string.
- `Cpf(string value)`: Converte implicitamente uma string para um objeto Cpf.

[**Exemplo de Uso**](#cpf)

### 2. Cnh

**Funcionalidade:**
Representa uma CNH (Carteira Nacional de Habilitação).

**Métodos:**

- `Cnh.Number`: Obtém o valor do número da CNH.
- `ToString()`: Retorna o valor da CNH.
- `string(Cnh document)`: Converte implicitamente um objeto Cnh para uma string.
- `Cnh(string value)`: Converte implicitamente uma string para um objeto Cnh.

[**Exemplo de Uso**](#cnh)

### 3. Rg

**Funcionalidade:**
Representa um RG (Registro Geral).

**Métodos:**

- `Rg.Number`: Obtém o valor do número do RG.
- `ToString()`: Retorna o valor do RG.
- `string(Rg document)`: Converte implicitamente um objeto Rg para uma string.
- `Rg(string value)`: Converte implicitamente uma string para um objeto Rg.

[**Exemplo de Uso**](#rg)

### 4. Cnpj

**Funcionalidade:**
Representa um CNPJ.

**Métodos:**

- `Cnpj.Number`: Obtém o valor do número do CNPJ.
- `ToString()`: Retorna o valor do CNPJ.
- `string(Cnpj document)`: Converte implicitamente um objeto Cnpj para uma string.
- `Cnpj(string value)`: Converte implicitamente uma string para um objeto Cnpj.

[**Exemplo de Uso**](#cnpj)

### 5. CpfRg

**Funcionalidade:**
Representa um CPF ou RG.

**Métodos:**

- `CpfRg.Number`: Obtém o valor do número do CPF ou RG.
- `ToString()`: Retorna o valor do CPF ou RG.
- `string(CpfRg document)`: Converte implicitamente um objeto CpfRg para uma string.
- `CpfRg(string value)`: Converte implicitamente uma string para um objeto CpfRg.

[**Exemplo de Uso**](#cpfrg)

### 6. CpfRgCnh

**Funcionalidade:**
Representa um CPF, RG ou CNH.

**Métodos:**

- `CpfRgCnh.Number`: Obtém o valor do número do CPF, RG ou CNH.
- `ToString()`: Retorna o valor do CPF, RG ou CNH.
- `string(CpfRgCnh document)`: Converte implicitamente um objeto CpfRgCnh para uma string.
- `CpfRgCnh(string value)`: Converte implicitamente uma string para um objeto CpfRgCnh.

[**Exemplo de Uso**](#cpfrgcnh)

### 7. CpfCnpj

**Funcionalidade:**
Representa um CPF ou CNPJ.

**Métodos:**

- `CpfCnpj.Number`: Obtém o valor do número do CPF ou CNPJ.
- `ToString()`: Retorna o valor do CPF ou CNPJ.
- `string(CpfCnpj document)`: Converte implicitamente um objeto CpfCnpj para uma string.
- `CpfCnpj(string value)`: Converte implicitamente uma string para um objeto CpfCnpj.

[**Exemplo de Uso**](#cpfcnpj)

### 8. Document

**Funcionalidade:**
Representa um Documento (CPF, CNH, RG ou CNPJ).

**Métodos:**

- `Document.Number`: Obtém o valor do número do CPF, CNH, RG ou CNPJ.
- `ToString()`: Retorna o valor do CPF, CNH, RG ou CNPJ.
- `string(Document document)`: Converte implicitamente um objeto Document para uma string.
- `Document(string value)`: Converte implicitamente uma string para um objeto Document.

[**Exemplo de Uso**](#document)

### 9. Email

**Funcionalidade:**
Representa um endereço de email válido.

**Métodos:**

- `Email.Value`: Obtém o valor do domínio de email.
- `ToString()`: Retorna o valor do domínio de email.
- `string(Email email)`: Converte implicitamente um objeto Email para uma string.
- `Email(string value)`: Converte implicitamente uma string para um objeto Email.

[**Exemplo de Uso**](#email)

### 10. EmailDomain

**Funcionalidade:**
Representa um domínio de email válido.

**Métodos:**

- `EmailDomain.Value`: Obtém o valor do domínio de email.
- `ToString()`: Retorna o valor do domínio de email.
- `string(EmailDomain emailDomain)`: Converte implicitamente um objeto EmailDomain para uma string.
- `EmailDomain(string value)`: Converte implicitamente uma string para um objeto EmailDomain.

[**Exemplo de Uso**](#emaildomain)

### 11. LanguageCode

**Funcionalidade:**
Representa um código de idioma válido.

**Métodos:**

- `LanguageCode.Code`: Obtém o código do idioma.
- `ToString()`: Retorna o código do idioma.
- `string(LanguageCode languageCode)`: Converte implicitamente um objeto LanguageCode para uma string.
- `LanguageCode(string value)`: Converte implicitamente uma string para um objeto LanguageCode.

[**Exemplo de Uso**](#languagecode)

### 12. Letter

**Funcionalidade:**
Representa uma string com apenas letras válida.

**Métodos:**

- `Letter.Value`: Obtém o valor da string com apenas letras.
- `ToString()`: Retorna o valor da string com apenas letras.
- `string(Letter letter)`: Converte implicitamente um objeto Letter para uma string.
- `Letter(string value)`: Converte implicitamente uma string para um objeto Letter.

[**Exemplo de Uso**](#letter)

### 13. LetterNumeric

**Funcionalidade:**
Representa uma string com apenas letras e números válida.

**Métodos:**

- `LetterNumeric.Value`: Obtém o valor da string com apenas letras e números.
- `ToString()`: Retorna o valor da string com apenas letras e números.
- `string(LetterNumeric letterNumeric)`: Converte implicitamente um objeto LetterNumeric para uma string.
- `LetterNumeric(string value)`: Converte implicitamente uma string para um objeto LetterNumeric.

[**Exemplo de Uso**](#letternumeric)

### 14. Numeric

**Funcionalidade:**
Representa uma string com apenas números válida.

**Métodos:**

- `Numeric.Value`: Obtém o valor da string com apenas números.
- `ToString()`: Retorna o valor da string com apenas números.
- `string(Numeric numeric)`: Converte implicitamente um objeto Numeric para uma string.
- `Numeric(string value)`: Converte implicitamente uma string para um objeto Numeric.

[**Exemplo de Uso**](#numeric)

### 15. Password

**Funcionalidade:**
Representa uma senha válida com complexidade especificada.
Parâmetros suportados para a complexidade da senha:

- `lowercase`: A senha deve conter letras minúsculas.
- `uppercase`: A senha deve conter letras maiúsculas.
- `number`: A senha deve conter números.
- `symbol`: A senha deve conter símbolos.
- `length`: A senha deve ter um tamanho mínimo especificado. Mínimo suportado: 8.

**Métodos:**

- `Password.Value`: Obtém o valor da senha.
- `ToString()`: Retorna o valor da senha.
- `string(Password password)`: Converte implicitamente um objeto Password para uma string.
- `Password(string value)`: Converte implicitamente uma string para um objeto Password.

[**Exemplo de Uso**](#password)

### 16. ZipCode

**Funcionalidade:**
Representa um código postal válido.

**Métodos:**

- `ZipCode.Value`: Obtém o valor do código postal.
- `ToString()`: Retorna o valor do código postal.
- `string(ZipCode zipCode)`: Converte implicitamente um objeto ZipCode para uma string.
- `ZipCode(string value)`: Converte implicitamente uma string para um objeto ZipCode.

[**Exemplo de Uso**](#zipcode)

### 17. ProtocolEmailReceiver

**Funcionalidade:**
Representa um protocolo de recebimento de email válido.

**Métodos:**

- `ProtocolEmailReceiver.Value`: Obtém o valor do protocolo de recebimento de email.
- `ToString()`: Retorna o valor do protocolo de recebimento de email.
- `string(ProtocolEmailReceiver protocol)`: Converte implicitamente um objeto ProtocolEmailReceiver para uma string.
- `ProtocolEmailReceiver(string value)`: Converte implicitamente uma string para um objeto ProtocolEmailReceiver.

[**Exemplo de Uso**](#protocolemailreceiver)

### 18. ProtocolEmailSender

**Funcionalidade:**
Representa um protocolo de envio de email válido.

**Métodos:**

- `ProtocolEmailSender.Value`: Obtém o valor do protocolo de envio de email.
- `ToString()`: Retorna o valor do protocolo de envio de email.
- `string(ProtocolEmailSender protocol)`: Converte implicitamente um objeto ProtocolEmailSender para uma string.
- `ProtocolEmailSender(string value)`: Converte implicitamente uma string para um objeto ProtocolEmailSender.

[**Exemplo de Uso**](#protocolemailsender)

### 19. ProtocolFtp

**Funcionalidade:**
Representa um protocolo de FTP válido.

**Métodos:**

- `ProtocolFtp.Value`: Obtém o valor do protocolo de FTP.
- `ToString()`: Retorna o valor do protocolo de FTP.
- `string(ProtocolFtp protocol)`: Converte implicitamente um objeto ProtocolFtp para uma string.
- `ProtocolFtp(string value)`: Converte implicitamente uma string para um objeto ProtocolFtp.

[**Exemplo de Uso**](#protocolftp)

### 20. ProtocolHttp

**Funcionalidade:**
Representa um protocolo de HTTP válido.

**Métodos:**

- `ProtocolHttp.Value`: Obtém o valor do protocolo de HTTP.
- `ToString()`: Retorna o valor do protocolo de HTTP.
- `string(ProtocolHttp protocol)`: Converte implicitamente um objeto ProtocolHttp para uma string.
- `ProtocolHttp(string value)`: Converte implicitamente uma string para um objeto ProtocolHttp.

[**Exemplo de Uso**](#protocolhttp)

### 21. ProtocolWs

**Funcionalidade:**
Representa um protocolo Websocket válido.

**Métodos:**

- `ProtocolWs.Value`: Obtém o valor do protocolo Websocket.
- `ToString()`: Retorna o valor do protocolo Websocket.
- `string(ProtocolWs protocol)`: Converte implicitamente um objeto ProtocolWs para uma string.
- `ProtocolWs(string value)`: Converte implicitamente uma string para um objeto ProtocolWs.

[**Exemplo de Uso**](#protocolws)

### 22. Url

**Funcionalidade:**
Representa uma URL válida.

**Métodos:**

- `Url.Value`: Obtém o valor da URL.
- `ToString()`: Retorna o valor da URL.
- `string(Url url)`: Converte implicitamente um objeto Url para uma string.
- `Url(string value)`: Converte implicitamente uma string para um objeto Url.

[**Exemplo de Uso**](#url)

### 23. Name

**Funcionalidade:**
Representa um nome válido.

**Métodos:**

- `Name.Value`: Obtém o valor do nome.
- `ToString()`: Retorna o valor do nome.
- `string(Name name)`: Converte implicitamente um objeto Name para uma string.
- `Name(string value)`: Converte implicitamente uma string para um objeto Name.

[**Exemplo de Uso**](#name)

### 24. Title

**Funcionalidade:**
Representa um título válido.

**Métodos:**

- `Title.Value`: Obtém o valor do título.
- `ToString()`: Retorna o valor do título.
- `string(Title title)`: Converte implicitamente um objeto Title para uma string.
- `Title(string value)`: Converte implicitamente uma string para um objeto Title.

[**Exemplo de Uso**](#title)

### 25. Description

**Funcionalidade:**
Representa uma descrição válida.

**Métodos:**

- `Description.Value`: Obtém o valor da descrição.
- `ToString()`: Retorna o valor da descrição.
- `string(Description description)`: Converte implicitamente um objeto Description para uma string.
- `Description(string value)`: Converte implicitamente uma string para um objeto Description.

[**Exemplo de Uso**](#description)

### 26. Keyword

**Funcionalidade:**
Representa uma palavra-chave válida.

**Métodos:**

- `Keyword.Value`: Obtém o valor da palavra-chave.
- `ToString()`: Retorna o valor da palavra-chave.
- `string(Keyword keyword)`: Converte implicitamente um objeto Keyword para uma string.
- `Keyword(string value)`: Converte implicitamente uma string para um objeto Keyword.

[**Exemplo de Uso**](#keyword)

### 27. DeletedBy

**Funcionalidade:**
Representa um usuário que excluiu um item.

**Métodos:**

- `DeletedBy.Value`: Obtém o valor do usuário que excluiu o item.
- `Guid(DeletedBy deletedBy)`: Converte implicitamente um objeto DeletedBy para uma Guid.
- `DeletedBy(Guid value)`: Converte implicitamente uma Guid para um objeto DeletedBy.

[**Exemplo de Uso**](#deletedby)

### 28. RestoredBy

**Funcionalidade:**
Representa um usuário que restaurou um item.

**Métodos:**

- `RestoredBy.Value`: Obtém o valor do usuário que restaurou o item.
- `Guid(RestoredBy restoredBy)`: Converte implicitamente um objeto RestoredBy para uma Guid.
- `RestoredBy(Guid value)`: Converte implicitamente uma Guid para um objeto RestoredBy.

[**Exemplo de Uso**](#restoredby)

### 29. FileStorage

**Funcionalidade:**
Representa um dados de um objeto em um bucket.

**Métodos:**

- `FileStorage.Value`: Obtém o valor dos dados armazenados.
- `ToString()`: Retorna o valor dos dados armazenados.
- `string(FileStorage fileStorage)`: Converte implicitamente um objeto FileStorage para uma string.
- `FileStorage(string value)`: Converte implicitamente uma string para um objeto FileStorage.

[**Exemplo de Uso**](#filestorage)

### 30. DelimitedString

**Funcionalidade:**
Representa uma string delimitada por ponto e vírgula.

**Métodos:**

- `DelimitedString.Value`: Obtém o valor da string delimitada.
- `DelimitedString.Values`: Obtém a lista de valores da string delimitada.
- `ToString()`: Retorna o valor da string delimitada.
- `ToList()`: Retorna a lista de valores da string delimitada.
- `string(DelimitedString delimitedString)`: Converte implicitamente um objeto DelimitedString para uma string.
- `string[](DelimitedString delimitedString)`: Converte implicitamente um objeto DelimitedString para uma lista de strings.
- `DelimitedString(string value)`: Converte implicitamente uma string para um objeto DelimitedString.
- `DelimitedString(string[] values)`: Converte implicitamente uma lista de strings para um objeto DelimitedString.

[**Exemplo de Uso**](#delimitedstring)

## Exemplos de Uso

Os exemplos de uso abaixo:

### Cpf

[Informações](#1-cpf)

```csharp
var documento = new Cpf("118.214.830-14");

if (cpf.IsValid)
{
  Console.WriteLine($"CPF válido: {documento.Number}");
}
```

```csharp
Cpf documento = "118.214.830-14";

if (cpf.IsValid)
{
  Console.WriteLine($"CPF válido: {documento.Number}");
}
```

### Cnh

[Informações](#2-cnh)

```csharp
var documento = new Cnh("17932463758");

if (cnh.IsValid)
{
  Console.WriteLine($"CNH válida: {documento.Number}");
}
```

```csharp
Cnh documento = "17932463758";

if (cnh.IsValid)
{
  Console.WriteLine($"CNH válida: {documento.Number}");
}
```

### Rg

[Informações](#3-rg)

```csharp
var documento = new Rg("28.589.200-9");

if (rg.IsValid)
{
  Console.WriteLine($"RG válido: {documento.Number}");
}
```

```csharp
Rg documento = "28.589.200-9";

if (rg.IsValid)
{
  Console.WriteLine($"RG válido: {documento.Number}");
}
```

### Cnpj

[Informações](#4-cnpj)

```csharp
var documento = new Cnpj("78.293.721/0001-24");

if (cnpj.IsValid)
{
  Console.WriteLine($"CNPJ válido: {documento.Number}");
}
```

```csharp
Cnpj documento = "78.293.721/0001-24";

if (cnpj.IsValid)
{
  Console.WriteLine($"CNPJ válido: {documento.Number}");
}
```

### CpfRg

[Informações](#5-cpfrg)

```csharp
var documento = new CpfRg("118.214.830-14");

if (documento.IsValid)
{
  Console.WriteLine($"Documento válido: {documento.Number}");
}
```

```csharp
CpfRg documento = "118.214.830-14";

if (documento.IsValid)
{
  Console.WriteLine($"Documento válido: {documento.Number}");
}
```

### CpfRgCnh

[Informações](#6-cpfrgcnh)

```csharp
var documento = new CpfRgCnh("118.214.830-14");

if (documento.IsValid)
{
  Console.WriteLine($"Documento válido: {documento.Number}");
}
```

```csharp
CpfRgCnh documento = "118.214.830-14";

if (documento.IsValid)
{
  Console.WriteLine($"Documento válido: {documento.Number}");
}
```

### CpfCnpj

[Informações](#7-cpfcnpj)

```csharp
var documento = new CpfCnpj("78.293.721/0001-24");

if (documento.IsValid)
{
  Console.WriteLine($"Documento válido: {documento.Number}");
}
```

```csharp
CpfCnpj documento = "78.293.721/0001-24";

if (documento.IsValid)
{
  Console.WriteLine($"Documento válido: {documento.Number}");
}
```

### Document

[Informações](#8-document)

```csharp
var documento = new Document("78.293.721/0001-24");

if (documento.IsValid)
{
  Console.WriteLine($"Documento válido: {documento.Number}");
}
```

```csharp
Document documento = "78.293.721/0001-24";

if (documento.IsValid)
{
  Console.WriteLine($"Documento válido: {documento.Number}");
}
```

### Email

[Informações](#9-email)

```csharp
var email = new Email("test@example.com");

if (email.IsValid)
{
  Console.WriteLine($"Endereço de email válido: {email.Value}");
}
```

```csharp
Email email = "test@example.com";

if (email.IsValid)
{
  Console.WriteLine($"Endereço de email válido: {email.Value}");
}
```

### EmailDomain

[Informações](#10-emaildomain)

```csharp
var emailDomain = new EmailDomain("example.com");

if (emailDomain.IsValid)
{
  Console.WriteLine($"Domínio de email válido: {emailDomain.Value}");
}
```

```csharp
EmailDomain emailDomain = "example.com";

if (emailDomain.IsValid)
{
  Console.WriteLine($"Domínio de email válido: {emailDomain.Value}");
}
```

### LanguageCode

[Informações](#11-languagecode)

```csharp
var languageCode = new LanguageCode("pt-BR");

if (languageCode.IsValid)
{
  Console.WriteLine($"Código de idioma válido: {languageCode.Code}");
}
```

```csharp
LanguageCode languageCode = "pt-BR";

if (languageCode.IsValid)
{
  Console.WriteLine($"Código de idioma válido: {languageCode.Code}");
}
```

### Letter

[Informações](#12-letter)

```csharp
var letter = new Letter("abc");

if (letter.IsValid)
{
  Console.WriteLine($"Valor válido: {letter.Value}");
}
```

```csharp
Letter letter = "abc";

if (letter.IsValid)
{
  Console.WriteLine($"Valor válido: {letter.Value}");
}
```

### LetterNumeric

[Informações](#13-letternumeric)

```csharp
var letterNumeric = new LetterNumeric("abc123");

if (letterNumeric.IsValid)
{
  Console.WriteLine($"Valor válido: {letterNumeric.Value}");
}
```

```csharp
LetterNumeric letterNumeric = "abc123";

if (letterNumeric.IsValid)
{
  Console.WriteLine($"Valor válido: {letterNumeric.Value}");
}
```

### Numeric

[Informações](#14-numeric)

```csharp
var numeric = new Numeric("123456");

if (numeric.IsValid)
{
  Console.WriteLine($"Número válido: {numeric.Value}");
}
```

```csharp
Numeric numeric = "123456";

if (numeric.IsValid)
{
  Console.WriteLine($"Número válido: {numeric.Value}");
}
```

### Password

[Informações](#15-password)

```csharp
var password = new Password("P@ssw0rd");

if (password.IsValid)
{
  Console.WriteLine($"Senha válida: {password.Value}");
}
```

```csharp
Password password = "P@ssw0rd";

if (password.IsValid)
{
  Console.WriteLine($"Senha válida: {password.Value}");
}
```

### ZipCode

[Informações](#16-zipcode)

```csharp
var zipCode = new ZipCode("12345-678");

if (zipCode.IsValid)
{
  Console.WriteLine($"Código postal válido: {zipCode.Value}");
}
```

```csharp
ZipCode zipCode = "12345-678";

if (zipCode.IsValid)
{
  Console.WriteLine($"Código postal válido: {zipCode.Value}");
}
```

### ProtocolEmailReceiver

[Informações](#17-protocolemailreceiver)

```csharp
var protocol = new ProtocolEmailReceiver("imap://example.com");

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

```csharp
ProtocolEmailReceiver protocol = "imap://example.com";

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

### ProtocolEmailSender

[Informações](#18-protocolemailsender)

```csharp
var protocol = new ProtocolEmailSender("smtp://example.com");

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

```csharp
ProtocolEmailSender protocol = "smtp://example.com";

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

### ProtocolFtp

[Informações](#19-protocolftp)

```csharp
var protocol = new ProtocolFtp("sftp://example.com");

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

```csharp
ProtocolEmailReceiver protocol = "sftp://example.com";

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

### ProtocolHttp

[Informações](#20-protocolhttp)

```csharp
var protocol = new ProtocolHttp("https://example.com");

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

```csharp
ProtocolHttp protocol = "https://example.com";

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

### ProtocolWs

[Informações](#21-protocolws)

```csharp
var protocol = new ProtocolWs("wss://example.com");

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

```csharp
ProtocolWs protocol = "wss://example.com";

if (protocol.IsValid)
{
  Console.WriteLine($"Protocolo válido: {protocol.Value}");
}
```

### Url

[Informações](#22-url)

```csharp
var url = new Url("https://example.com");

if (url.IsValid)
{
  Console.WriteLine($"Protocolo válido: {url.Value}");
}
```

```csharp
Url url = "https://example.com";

if (url.IsValid)
{
  Console.WriteLine($"Protocolo válido: {url.Value}");
}
```

### Name

[Informações](#23-name)

```csharp
var name = new Name("John Doe");

if (name.IsValid)
{
  Console.WriteLine($"Nome válido: {name.Value}");
  Console.WriteLine($"Nome normalizado: {name.Normalized}");
}
```

```csharp
Name name = "John Doe";

if (name.IsValid)
{
  Console.WriteLine($"Nome válido: {name.Value}");
  Console.WriteLine($"Nome normalizado: {name.Normalized}");
}
```

### Title

[Informações](#24-title)

```csharp
var title = new Title("Software Engineer");

if (title.IsValid)
{
  Console.WriteLine($"Título válido: {title.Value}");
  Console.WriteLine($"Título normalizado: {title.Normalized}");
}
```

```csharp
Title title = "Software Engineer";

if (title.IsValid)
{
  Console.WriteLine($"Título válido: {title.Value}");
  Console.WriteLine($"Título normalizado: {title.Normalized}");
}
```

### Description

[Informações](#25-description)

```csharp
var description = new Description("This is a sample description.");

if (description.IsValid)
{
  Console.WriteLine($"Descrição válida: {description.Value}");
  Console.WriteLine($"Descrição normalizada: {description.Normalized}");
}
```

```csharp
Description description = "This is a sample description.";

if (description.IsValid)
{
  Console.WriteLine($"Descrição válida: {description.Value}");
  Console.WriteLine($"Descrição normalizada: {description.Normalized}");
}
```

### Keyword

[Informações](#26-keyword)

```csharp
var keyword = new Keyword("sample");

if (keyword.IsValid)
{
  Console.WriteLine($"Palavra-chave válida: {keyword.Value}");
  Console.WriteLine($"Palavra-chave normalizada: {keyword.Normalized}");
}
```

```csharp
Keyword keyword = "sample";

if (keyword.IsValid)
{
  Console.WriteLine($"Palavra-chave válida: {keyword.Value}");
  Console.WriteLine($"Palavra-chave normalizada: {keyword.Normalized}");
}
```

### DeletedBy

[Informações](#27-deletedby)

```csharp
var deletedBy = new DeletedBy(Guid.NewGuid());

if (deletedBy.IsValid)
{
  Console.WriteLine($"Usuário que excluiu: {deletedBy.Value}");
}
```

```csharp
DeletedBy deletedBy = Guid.NewGuid();

if (deletedBy.IsValid)
{
  Console.WriteLine($"Usuário que excluiu: {deletedBy.Value}");
}
```

### RestoredBy

[Informações](#28-restoredby)

```csharp
var restoredBy = new RestoredBy(Guid.NewGuid());

if (restoredBy.IsValid)
{
  Console.WriteLine($"Usuário que restaurou: {restoredBy.Value}");
  Console.WriteLine($"Usuário que restaurou: {restoredBy.Normalized}");
}
```

```csharp
RestoredBy restoredBy = Guid.NewGuid();

if (restoredBy.IsValid)
{
  Console.WriteLine($"Usuário que restaurou: {restoredBy.Value}");
  Console.WriteLine($"Usuário que restaurou: {restoredBy.Normalized}");
}
```

### FileStorage

[Informações](#29-filestorage)

```csharp
var fileStorage = new FileStorage("https://example.com/path/file.txt", "/path/file.txt");

if (fileStorage.IsValid)
{
  Console.WriteLine($"Dados armazenados: {fileStorage.Link}"); // output: https://example.com/path/file.txt
  Console.WriteLine($"Nome do arquivo: {fileStorage.Name}"); // output: /path/file.txt
}
```

```csharp
FileStorage fileStorage = "https://example.com/file.txt";

if (fileStorage.IsValid)
{
  Console.WriteLine($"Dados armazenados: {fileStorage.Link}"); // output: https://example.com/file.txt
  Console.WriteLine($"Nome do arquivo: {fileStorage.Name}"); // output: https://example.com/file.txt
}
```

### DelimitedString

[Informações](#30-delimitedstring)

```csharp
var delimitedString = new DelimitedString("value1;value2;value3");

if (delimitedString.IsValid)
{
  Console.WriteLine($"String delimitada: {delimitedString.Value}");
  Console.WriteLine($"Lista de valores: {string.Join(", ", delimitedString.Values)}");
}
```

```csharp
DelimitedString delimitedString = ["value1", "value2", "value3"];

if (delimitedString.IsValid)
{
  Console.WriteLine($"String delimitada: {delimitedString.Value}");
  Console.WriteLine($"Lista de valores: {string.Join(", ", delimitedString.Values)}");
}
```

## Dependências

- [Tooark.Enums](../Tooark.Enums/README.md)
- [Tooark.Extensions](../Tooark.Extensions/README.md)
- [Tooark.Validations](../Tooark.Validations/README.md)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.ValueObjects](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
