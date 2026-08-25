# Tooark.Utils

Biblioteca de funções utilitárias gerais que auxiliam no desenvolvimento, incluindo métodos para manipulação de strings, arquivos, idiomas e listas localizadas.

## Instalação

```bash
dotnet add package Tooark.Utils
```

## Conteúdo

- [FileConvert](#1-conversão-de-arquivos-e-extração-de-extensões)
- [FileValid](#2-validação-de-arquivos)
- [GenerateString](#3-geração-de-strings)
- [GetInfo](#4-busca-de-informações)
- [Language](#5-idiomas)
- [Normalize](#6-normalização)
- [UtilErrorMessages](#7-mensagens-de-erro)

## Utilitários

Os utilitários disponíveis são:

### 1. Conversão de Arquivos e Extração de Extensões

**Funcionalidade:**
Conversão de arquivos e extração de extensões.

**Métodos:**

- `ToMemoryStream(string? stringFile, long maxBytes = 0)`: Converte uma string base64 para `MemoryStream`.
- `ToMemoryStream(IFormFile? fromFile, long maxBytes = 0)`: Converte um `IFormFile` para `MemoryStream`.
- `ToMemoryStreamAsync(IFormFile? fromFile, long maxBytes = 0, CancellationToken cancellationToken = default)`: Converte um `IFormFile` para `MemoryStream` de forma assíncrona.
- `Extension(string? stringFile)`: Extrai a extensão do arquivo de uma string base64.
- `Extension(IFormFile? fromFile)`: Extrai a extensão do arquivo de um `IFormFile`.

**Constante:**

- `DefaultMaxBytes`: Tamanho máximo padrão do conteúdo convertido. 5242880 bytes (5MB).

A chave da mensagem de erro do limite está em [`UtilErrorMessages`](#7-mensagens-de-erro).

**Comportamento:**

- A conversão de um `IFormFile` copia o conteúdo binário do arquivo. O `MemoryStream` retornado já vem posicionado no início.
- A conversão retorna `null` quando o arquivo é nulo ou vazio, e quando a string não contém um base64 válido.
- **Limite de tamanho**: acima de `maxBytes` é lançada uma `PayloadTooLargeException` (HTTP 413), com a mensagem `File.SizeExceeded;{limite}`. Valor não positivo aplica `DefaultMaxBytes`; o teto absoluto é `int.MaxValue`, que é o que um `MemoryStream` comporta. O limite é conferido **antes** de reservar memória, e também bloco a bloco durante a leitura do upload, para valer mesmo quando o tamanho declarado não corresponde ao conteúdo real.
- A string precisa trazer o marcador `;base64,`, e o conteúdo é lido logo depois dele. Um `base64,` solto em outra posição não é aceito.
- A extensão extraída de uma string base64 vem do tipo declarado no formato `data:tipo/extensao;base64,conteudo`, e o tipo pode trazer parâmetros (`data:image/png;charset=utf-8;base64,...`).
- **A extensão é validada**: só passam letras e dígitos ASCII, com no máximo dez caracteres. Qualquer outra coisa resulta em `null`. Como a extensão vem do que o cliente declarou — o nome do arquivo ou o tipo da data URL — sem essa validação o retorno poderia carregar separador de diretório, marcação ou texto de tamanho arbitrário para dentro de quem a consome. Tipos MIME que não têm forma de extensão, como `image/svg+xml` e `application/vnd.ms-excel`, resultam em `null`.
- A extensão é sempre retornada em maiúsculas e sem o ponto inicial. Ela reflete o que foi **declarado**, e não o conteúdo real do arquivo.

[**Exemplo de Uso**](#conversão-de-arquivos-e-extração-de-extensões)

### 2. Validação de Arquivos

**Funcionalidade:**
Verificação da validade de arquivos.

**Métodos:**

- `IsImage(IFormFile? file, long fileSize = 0)`: Verifica se o arquivo é uma imagem válida.
- `IsDocument(IFormFile? file, long fileSize = 0)`: Verifica se o arquivo é um documento válido.
- `IsVideo(IFormFile? file, long fileSize = 0)`: Verifica se o arquivo é um vídeo válido.
- `IsCustom(IFormFile? file, long fileSize = 0, string[]? permittedExtensions = null)`: Verifica se o arquivo é válido para extensões personalizadas.

**Comportamento:**

- A verificação considera o **tamanho** e a **extensão declarada no nome do arquivo**. Ela não inspeciona o conteúdo, portanto não substitui a checagem do tipo real do arquivo quando o conteúdo enviado não é confiável.
- O tamanho padrão é 5242880 bytes (5MB), aplicado quando `fileSize` é zero ou negativo.
- As extensões personalizadas são comparadas sem diferenciar maiúsculas de minúsculas e podem ser informadas **com ou sem o ponto inicial**: `"pdf"` e `".PDF"` têm o mesmo efeito.
- Se `permittedExtensions` for nulo, vazio ou contiver apenas entradas em branco, todas as extensões de imagem, documento e vídeo são aceitas.

[**Exemplo de Uso**](#validação-de-arquivos)

### 3. Geração de Strings

**Funcionalidade:**
Geração de strings segundo critérios específicos e aleatórios.

**Métodos:**

- `Sequential(int number)`: Converte um número inteiro em uma representação equivalente alfabética do número.
- `Password(int len = 12, bool upper = true, bool lower = true, bool number = true, bool special = true, bool similarity = false)`: Gera uma string com critérios específicos.
- `Hexadecimal(int sizeToken = 128)`: Gera uma string hexadecimal aleatória.
- `GuidCode()`: Gera uma string Guid sem hífens, com 32 caracteres.
- `Token(int length = 256)`: Gera uma string de token.

**Comportamento:**

- `Sequential` retorna **letras maiúsculas**, e uma string vazia quando o número não é positivo.
- `Password` usa um gerador criptográfico e garante ao menos um caractere de cada tipo ativado. Comprimentos menores que 8 são elevados para 8, e se todos os tipos forem desativados, todos são reativados.
- `Hexadecimal` retorna **exatamente** o tamanho solicitado, inclusive para tamanhos ímpares. Valores menores que 2 são elevados para 2.

[**Exemplo de Uso**](#geração-de-strings)

### 4. Busca de Informações

**Funcionalidade:**
Obtenção de informações localizadas de uma lista de objetos. O tipo da lista precisa ter a propriedade `LanguageCode` e a propriedade buscada.

**Métodos:**

- `Name<T>(IList<T>? list, string? languageCode = null)`: Obtém o nome localizado de uma lista de objetos.
- `Title<T>(IList<T>? list, string? languageCode = null)`: Obtém o título localizado de uma lista de objetos.
- `Description<T>(IList<T>? list, string? languageCode = null)`: Obtém a descrição localizada de uma lista de objetos.
- `Keywords<T>(IList<T>? list, string? languageCode = null)`: Obtém as palavras-chave localizadas de uma lista de objetos.
- `Custom<T>(IList<T>? list, string property, string? languageCode = null)`: Obtém um valor localizado de uma propriedade em uma lista de objetos.

**Comportamento:**

- Quando `languageCode` não é informado, o **idioma atual** é usado.
- A busca tenta o idioma solicitado, depois o idioma padrão da aplicação, e por fim o primeiro item da lista.
- Lista nula ou vazia, e propriedade com valor nulo, resultam em uma string vazia.
- Propriedades que não são `string` são convertidas com `ToString()`.
- Se `LanguageCode` ou a propriedade buscada não existir no tipo, é lançada uma `GetInfoException`.

[**Exemplo de Uso**](#busca-de-informações)

### 5. Idiomas

**Funcionalidade:**
Gerenciamento de idiomas.

**Membros:**

- `Default`: O código de idioma padrão usado na aplicação. Padrão "en-US".
- `Current`: O código de idioma atual do ambiente de execução.
- `CurrentCulture`: A cultura atual do ambiente de execução.
- `SetCulture(string? culture)`: Define a cultura atual usando o nome da cultura no formato `xx-XX`.
- `SetCulture(CultureInfo? culture)`: Define a cultura atual usando um objeto `CultureInfo`.
- `Instance`: A implementação de `ILanguage` usada, substituível em testes. Rejeita valor nulo com `ArgumentNullException`.

A interface `Tooark.Utils.Interfaces.ILanguage` expõe `DefaultLanguage`, `CurrentLanguage`, `CurrentCultureInfo`, `SetCultureInfo(string?)` e `SetCultureInfo(CultureInfo?)`.

**Comportamento:**

- O idioma atual acompanha a cultura do **fluxo de execução** (`CultureInfo.CurrentCulture`), e não um estado global do processo. Em uma aplicação web, cada requisição enxerga apenas a própria cultura.
- O nome da cultura é aceito sem diferenciar maiúsculas de minúsculas: `"pt-br"` é normalizado para `"pt-BR"`.
- Nome nulo, vazio, fora do formato `xx-XX` ou de uma cultura inexistente resulta na cultura padrão da aplicação.
- `SetCulture` também define `CultureInfo.CurrentUICulture`.

[**Exemplo de Uso**](#idiomas)

### 6. Normalização

**Funcionalidade:**
Normalização de strings. Remove espaços e pontuação, converte para maiúscula e translitera caracteres especiais.

**Métodos:**

- `Value(string value)`: Normaliza um valor removendo espaços, convertendo para maiúscula e substituindo caracteres especiais.

[**Exemplo de Uso**](#normalização)

### 7. Mensagens de Erro

**Funcionalidade:**
Chaves de tradução das mensagens de erro geradas pelo próprio pacote, em `Tooark.Utils.Messages.UtilErrorMessages`.

**Constantes:**

- `FileSizeExceeded`: `"File.SizeExceeded"`. Emitida pelo `FileConvert` quando o conteúdo excede o limite, no formato `File.SizeExceeded;{limite}`.

As traduções acompanham os recursos do [Tooark.Extensions](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Extensions).

## Exemplo de Uso

### Conversão de Arquivos e Extração de Extensões

```csharp
using Tooark.Utils;
using Microsoft.AspNetCore.Http;
using Tooark.Exceptions;

var base64String = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQ...";

// Limite padrão de 5MB
MemoryStream? memoryStream = FileConvert.ToMemoryStream(base64String);
MemoryStream? fromUpload = FileConvert.ToMemoryStream(formFile);

// Limite próprio, em bytes
MemoryStream? grande = FileConvert.ToMemoryStream(formFile, 20 * 1024 * 1024);
MemoryStream? assincrono = await FileConvert.ToMemoryStreamAsync(formFile, cancellationToken: cancellationToken);

string? extension = FileConvert.Extension(base64String); // PNG
string? extensionFromFile = FileConvert.Extension(formFile); // TXT

// Acima do limite, o erro é explícito e já carrega o status HTTP 413
try
{
  FileConvert.ToMemoryStream(formFile, 1024);
}
catch (PayloadTooLargeException ex)
{
  // ex.Message      -> "File.SizeExceeded;1024"
  // ex.GetStatusCode() -> HttpStatusCode.RequestEntityTooLarge
}
```

### Validação de Arquivos

```csharp
using Tooark.Utils;
using Microsoft.AspNetCore.Http;

bool isImage = FileValid.IsImage(formFile);
bool isDocument = FileValid.IsDocument(formFile, 10485760); // 10MB
bool isVideo = FileValid.IsVideo(formFile);

// Com ou sem o ponto inicial, o resultado é o mesmo
bool isCustom = FileValid.IsCustom(formFile, permittedExtensions: ["txt", ".csv"]);
```

### Geração de Strings

```csharp
using Tooark.Utils;

string sequential = GenerateString.Sequential(27); // AA
string password = GenerateString.Password(); // aB3@dE1#fG2$
string onlyLetters = GenerateString.Password(16, number: false, special: false);
string hex = GenerateString.Hexadecimal(); // 1A2B3C4D5E6F...
string guid = GenerateString.GuidCode(); // 1A2B3C4D5E6F7A8B9C0D1E2F3A4B5C6D
string token = GenerateString.Token(); // 1A2B3C4D5E6F7A8B9C0D1E2F3A4B5C6DADE123E21...
```

### Busca de Informações

```csharp
using Tooark.Utils;

// O tipo precisa ter a propriedade LanguageCode
var list = new List<MyObject> { /* ... */ };

string name = GetInfo.Name(list);
string title = GetInfo.Title(list);
string description = GetInfo.Description(list);
string keywords = GetInfo.Keywords(list);

// Idioma explícito e propriedade personalizada
string customValue = GetInfo.Custom(list, "CustomProperty", "pt-BR");
```

### Idiomas

```csharp
using Tooark.Utils;
using System.Globalization;

Language.SetCulture("pt-BR");

string current = Language.Current; // pt-BR
string defaultLanguage = Language.Default; // en-US
CultureInfo currentCulture = Language.CurrentCulture; // pt-BR

// Aceita o nome sem diferenciar maiúsculas de minúsculas
Language.SetCulture("es-es"); // es-ES

// Nome inválido cai na cultura padrão
Language.SetCulture("pt"); // en-US
```

### Normalização

```csharp
using Tooark.Utils;

string normalizedValue = Normalize.Value("Olá Mundo!"); // OLAMUNDO
string withSymbols = Normalize.Value("R&D 100$"); // RANDD100DOLLAR
```

## Dependências

- [Microsoft.AspNetCore.Http](https://www.nuget.org/packages/Microsoft.AspNetCore.Http/)
- [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)
- [`Tooark.Validations`](https://www.nuget.org/packages/Tooark.Validations)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Utils](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
