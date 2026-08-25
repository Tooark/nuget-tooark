# Tooark.Utils

Biblioteca de funções utilitárias gerais que auxiliam no desenvolvimento, incluindo métodos para manipulação de strings, datas, coleções e mais.

## Conteúdo

- [FileConvert](#1-conversão-de-arquivos-e-extração-de-extensões)
- [FileValid](#2-validação-de-arquivos)
- [GenerateString](#3-geração-de-strings)
- [GetInfo](#4-busca-de-informações)
- [Language](#5-idiomas)
- [Normalize](#6-normalização)

## Utilitários

Os utilitários disponíveis são:

### 1. Conversão de Arquivos e Extração de Extensões

**Funcionalidade:**
Conversão de arquivos e extração de extensões.

**Métodos:**

- `ToMemoryStream(string stringFile)`: Converte uma string base64 para `MemoryStream`.
- `ToMemoryStream(IFormFile fromFile)`: Converte um `IFormFile` para `MemoryStream`.
- `Extension(string stringFile)`: Extrai a extensão do arquivo de uma string base64.
- `Extension(IFormFile fromFile)`: Extrai a extensão do arquivo de um `IFormFile`.

[**Exemplo de Uso**](#conversão-de-arquivos-e-extração-de-extensões)

### 2. Validação de Arquivos

**Funcionalidade:**
Verificação da validade de arquivos.

**Métodos:**

- `IsImage(IFormFile file, long fileSize = 0)`: Verifica se o arquivo é uma imagem válida.
- `IsDocument(IFormFile file, long fileSize = 0)`: Verifica se o arquivo é um documento válido.
- `IsVideo(IFormFile file, long fileSize = 0)`: Verifica se o arquivo é um vídeo válido.
- `IsCustom(IFormFile file, long fileSize = 0, string[]? permittedExtensions = null)`: Verifica se o arquivo é válido para extensões personalizadas.

[**Exemplo de Uso**](#validação-de-arquivos)

### 3. Geração de Strings

**Funcionalidade:**
Geração de strings segundo critérios específicos e aleatórios.

**Métodos:**

- `Sequential(int number)`: Converte um número inteiro em uma representação equivalente alfabética do número.
- `Password(int len = 12, bool upper = true, bool lower = true, bool number = true, bool special = true, bool similarity = false)`: Gera uma string com critérios específicos.
- `Hexadecimal(int sizeToken = 128)`: Gera uma string hexadecimal aleatória.
- `GuidCode()`: Gera uma string Guid sem hífens.
- `Token(int length = 256)`: Gera uma string de token.

[**Exemplo de Uso**](#geração-de-strings)

### 4. Busca de Informações

**Funcionalidade:**
Obtenção de informações de uma lista de objetos.

**Métodos:**

- `Name<T>(IList<T> list, string? languageCode = null)`: Obtém o nome localizado de uma lista de objetos.
- `Title<T>(IList<T> list, string? languageCode = null)`: Obtém o título localizado de uma lista de objetos.
- `Description<T>(IList<T> list, string? languageCode = null)`: Obtém a descrição localizada de uma lista de objetos.
- `Keywords<T>(IList<T> list, string? languageCode = null)`: Obtém as palavras-chave localizadas de uma lista de objetos.
- `Custom<T>(IList<T> list, string property, string? languageCode = null)`: Obtém um valor localizado de uma propriedade em uma lista de objetos.

[**Exemplo de Uso**](#busca-de-informações)

### 5. Idiomas

**Funcionalidade:**
Gerenciamento de idiomas.

**Métodos:**

- `Default`: O código de idioma padrão usado na aplicação. Padrão "en-US".
- `Current`: O código de idioma atual do ambiente de execução.
- `SetCulture(string culture)`: Define a cultura atual para a aplicação usando o nome da cultura no formato `xx-XX`.
- `SetCulture(CultureInfo culture)`: Define a cultura atual para a aplicação usando um objeto `CultureInfo`.

[**Exemplo de Uso**](#idiomas)

### 6. Normalização

**Funcionalidade:**
Normalização de strings. Remove espaços, converte para maiúscula e substitui caracteres especiais.

**Métodos:**

- `Value(string value)`: Normaliza um valor removendo espaços, convertendo para maiúscula e substituindo caracteres especiais.
- `ValueRegex(string value)`: Normaliza um valor removendo espaços, convertendo para maiúscula e substituindo caracteres especiais usando expressões regulares.

[**Exemplo de Uso**](#normalização)

## Exemplo de Uso

### Conversão de Arquivos e Extração de Extensões

```csharp
using Tooark.Utils;
using Microsoft.AspNetCore.Http;

var base64String = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQ...";
var formFile = new FormFile(new MemoryStream(), 0, 0, "file", "file.txt");

MemoryStream? memoryStream = FileConvert.ToMemoryStream(base64String);
MemoryStream? memoryStreamFromFile = FileConvert.ToMemoryStream(formFile);

string? extension = FileConvert.Extension(base64String); // png
string? extensionFromFile = FileConvert.Extension(formFile); // txt
```

### Validação de Arquivos

```csharp
using Tooark.Utils;
using Microsoft.AspNetCore.Http;

bool isImage = FileValid.IsImage(formFile);
bool isDocument = FileValid.IsDocument(formFile);
bool isVideo = FileValid.IsVideo(formFile);
bool isCustom = FileValid.IsCustom(formFile, permittedExtensions: new[] { ".TXT", ".CSV" });
```

### Geração de Strings

```csharp
using Tooark.Utils;

string sequential = GenerateString.Sequential(27); // AA
string password = GenerateString.Password(); // 1aB2cD3eF4!@
string hex = GenerateString.Hexadecimal(); // 1A2B3C4D5E6F...
string guid = GenerateString.GuidCode(); // 1A2B3C4D5E6F7G8H9I0J1K2L3M4N5O6P
string token = GenerateString.Token(); // 1A2B3C4D5E6F7G8H9I0J1K2L3M4N5O6PADE123E21...
```

### Busca de Informações

```csharp
using Tooark.Utils;

var list = new List<MyObject> { /* ... */ };
string name = GetInfo.Name(list);
string title = GetInfo.Title(list);
string description = GetInfo.Description(list);
string keywords = GetInfo.Keywords(list);
string customValue = GetInfo.Custom(list, "CustomProperty");
```

### Idiomas

```csharp
using Tooark.Utils;
using System.Globalization;

Language.SetCulture("pt-BR");
CultureInfo currentCulture = Language.Current; // pt-BR
CultureInfo defaultCulture = Language.Default; // en-US
```

### Normalização

```csharp
using Tooark.Utils;

string normalizedValue = Normalize.Value("Olá Mundo!"); // OLAMUNDO
string normalizedValueRegex = Normalize.ValueRegex("Olá Mundo!"); // OLAMUNDO
```

## Dependências

- [Microsoft.AspNetCore.Http](https://www.nuget.org/packages/Microsoft.AspNetCore.Http/)
- [Tooark.Exceptions](../Tooark.Exceptions/README.md)
- [Tooark.Validations](../Tooark.Validations/README.md)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Utils](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
