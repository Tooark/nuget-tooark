# Tooark.Utils

Biblioteca de funções utilitárias gerais que auxiliam no desenvolvimento, incluindo métodos para manipulação de strings, arquivos, idiomas e listas localizadas.

📖 **Documentação:** [Tooark.Utils no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.utils.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Utils/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Instalação](#-instalação)
- [Utilitários](#-utilitários)
  - [FileConvert](#1-conversão-de-arquivos-e-extração-de-extensões)
  - [FileValid](#2-validação-de-arquivos)
  - [GenerateString](#3-geração-de-strings)
  - [GetInfo](#4-busca-de-informações)
  - [Language](#5-idiomas)
  - [Normalize](#6-normalização)
  - [UtilErrorMessages](#7-mensagens-de-erro)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Utils
```

---

## 🧰 Utilitários

Os utilitários disponíveis são:

### 1. Conversão de Arquivos e Extração de Extensões

**Funcionalidade:**
Conversão de arquivos e extração de extensões.

**Métodos:**

- `ToMemoryStream(string? stringFile, long maxBytes = 0)`: Converte uma string base64 para `MemoryStream`.
- `CopyToMemoryStream(Stream? stream, long maxBytes = 0)`: Copia o conteúdo de um `Stream` para um `MemoryStream`.
- `CopyToMemoryStreamAsync(Stream? stream, long maxBytes = 0, CancellationToken cancellationToken = default)`: Copia o conteúdo de um `Stream` para um `MemoryStream` de forma assíncrona.
- `Extension(string? stringFile)`: Extrai a extensão do arquivo de uma string base64.
- `ExtensionFromFileName(string? fileName)`: Extrai a extensão a partir do nome do arquivo.

`CopyToMemoryStream`, `CopyToMemoryStreamAsync` e `ExtensionFromFileName` existem desde a v4.11.0.

**Constante:**

- `DefaultMaxBytes`: Tamanho máximo padrão do conteúdo convertido. 5242880 bytes (5MB).

A chave da mensagem de erro do limite está em [`UtilErrorMessages`](#7-mensagens-de-erro).

**Comportamento:**

- A cópia de um `Stream` começa na **posição atual** da origem e leva o conteúdo binário até o fim. A origem não é fechada: quem abriu o fluxo é quem o descarta. O `MemoryStream` retornado já vem posicionado no início.
- A conversão retorna `null` quando o fluxo é nulo, está vazio ou já foi lido até o fim, e quando a string não contém um base64 válido.
- **Limite de tamanho**: acima de `maxBytes` é lançada uma `PayloadTooLargeException` (HTTP 413), com a mensagem `File.SizeExceeded;{limite}`. Valor não positivo aplica `DefaultMaxBytes`; o teto absoluto é `int.MaxValue`, que é o que um `MemoryStream` comporta. Em uma string, o limite é conferido pelo comprimento do trecho base64, **antes** de reservar memória. Em um `Stream` que informa o tamanho, o excesso também é recusado antes de qualquer alocação; nos demais, como o corpo de uma requisição, o limite é conferido bloco a bloco durante a cópia.
- A string precisa trazer o marcador `;base64,`, e o conteúdo é lido logo depois dele. Um `base64,` solto em outra posição não é aceito.
- A extensão extraída de uma string base64 vem do tipo declarado no formato `data:tipo/extensao;base64,conteudo`, e o tipo pode trazer parâmetros (`data:image/png;charset=utf-8;base64,...`).
- A extensão extraída de um nome de arquivo é o que vem depois do último ponto. O que vier antes de um separador de diretório é descartado.
- **A extensão é validada**: só passam letras e dígitos ASCII, com no máximo dez caracteres. Qualquer outra coisa resulta em `null`. Como a extensão vem do que o cliente declarou — o nome do arquivo ou o tipo da data URL — sem essa validação o retorno poderia carregar separador de diretório, marcação ou texto de tamanho arbitrário para dentro de quem a consome. Tipos MIME que não têm forma de extensão, como `image/svg+xml` e `application/vnd.ms-excel`, resultam em `null`.
- A extensão é sempre retornada em maiúsculas e sem o ponto inicial. Ela reflete o que foi **declarado**, e não o conteúdo real do arquivo.

**Métodos com `IFormFile`:**

Os três métodos do `FileConvert` que recebiam um `IFormFile` **foram removidos na v5**, junto com a dependência do pacote no `Microsoft.AspNetCore.Http` 2.x: o `IFormFile` é um tipo do ASP.NET Core, e o pacote só referenciava o ASP.NET Core por causa deles. Estavam obsoletos desde a v4.11.0, que trouxe os substitutos. As versões que recebem uma string (`ToMemoryStream(string?)` e `Extension(string?)`) continuam.

| Removido                                                   | Com `IFormFile` (`Tooark.AspNetCore.Files`) | Sem `IFormFile`                   |
| ---------------------------------------------------------- | ------------------------------------------- | --------------------------------- |
| `ToMemoryStream(IFormFile?, long)`                         | `arquivo.ToMemoryStream()`                  | `CopyToMemoryStream(stream)`      |
| `ToMemoryStreamAsync(IFormFile?, long, CancellationToken)` | `arquivo.ToMemoryStreamAsync()`             | `CopyToMemoryStreamAsync(stream)` |
| `Extension(IFormFile?)`                                    | `arquivo.GetExtension()`                    | `ExtensionFromFileName(nome)`     |

Em uma aplicação ASP.NET Core, use as extensões de `IFormFile` do [`Tooark.AspNetCore`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.AspNetCore), que aplicam as mesmas regras. Fora dela, use as versões por `Stream` e por nome de arquivo.

[**Exemplo de Uso**](#conversão-de-arquivos-e-extração-de-extensões)

### 2. Validação de Arquivos

**Funcionalidade:**
Verificação da validade de arquivos.

**Métodos:**

- `IsImageFile(string? fileName, long length, long maxBytes = 0)`: Verifica, pelo nome e pelo tamanho, se o arquivo é uma imagem válida.
- `IsDocumentFile(string? fileName, long length, long maxBytes = 0)`: Verifica, pelo nome e pelo tamanho, se o arquivo é um documento válido.
- `IsVideoFile(string? fileName, long length, long maxBytes = 0)`: Verifica, pelo nome e pelo tamanho, se o arquivo é um vídeo válido.
- `IsCustomFile(string? fileName, long length, long maxBytes = 0, string[]? permittedExtensions = null)`: Verifica, pelo nome e pelo tamanho, se o arquivo é válido para extensões personalizadas.

Os quatro existem desde a v4.11.0. `fileName` é o nome do arquivo, de onde sai a extensão, `length` é o tamanho dele em bytes e `maxBytes` é o tamanho máximo aceito.

**Comportamento:**

- A verificação considera o **tamanho** e a **extensão declarada no nome do arquivo**. Ela não inspeciona o conteúdo, portanto não substitui a checagem do tipo real do arquivo quando o conteúdo enviado não é confiável.
- As extensões de imagem são `.JPG`, `.JPEG`, `.PNG`, `.GIF`, `.BMP`, `.SVG` e `.WEBP`; as de documento, `.TXT`, `.CSV`, `.LOG`, `.PDF`, `.DOC`, `.DOCX`, `.XLS`, `.XLSX`, `.PPT` e `.PPTX`; as de vídeo, `.AVI`, `.MP4`, `.MPG`, `.MPEG` e `.WMV`.
- O tamanho máximo padrão é 5242880 bytes (5MB), aplicado quando `maxBytes` é zero ou negativo. Um arquivo com `length` zero ou negativo é inválido.
- As extensões personalizadas são comparadas sem diferenciar maiúsculas de minúsculas e podem ser informadas **com ou sem o ponto inicial**: `"pdf"` e `".PDF"` têm o mesmo efeito.
- Se `permittedExtensions` for nulo, vazio ou contiver apenas entradas em branco, todas as extensões de imagem, documento e vídeo são aceitas.

**Métodos com `IFormFile`:**

Os quatro métodos do `FileValid` que recebiam um `IFormFile` **foram removidos na v5**, pelo mesmo motivo dos [métodos do `FileConvert`](#1-conversão-de-arquivos-e-extração-de-extensões). Estavam obsoletos desde a v4.11.0.

| Removido                                | Com `IFormFile` (`Tooark.AspNetCore.Files`) | Sem `IFormFile`                 |
| --------------------------------------- | ------------------------------------------- | ------------------------------- |
| `IsImage(IFormFile?, long)`             | `arquivo.IsImage()`                         | `IsImageFile(nome, tamanho)`    |
| `IsDocument(IFormFile?, long)`          | `arquivo.IsDocument()`                      | `IsDocumentFile(nome, tamanho)` |
| `IsVideo(IFormFile?, long)`             | `arquivo.IsVideo()`                         | `IsVideoFile(nome, tamanho)`    |
| `IsCustom(IFormFile?, long, string[]?)` | `arquivo.IsCustom()`                        | `IsCustomFile(nome, tamanho)`   |

O parâmetro do tamanho máximo, que nos métodos removidos se chamava `fileSize`, chama-se `maxBytes` nos substitutos: quem o informa pelo nome (`fileSize: ...`) precisa trocá-lo na migração.

[**Exemplo de Uso**](#validação-de-arquivos)

### 3. Geração de Strings

**Funcionalidade:**
Geração de strings segundo critérios específicos e aleatórios.

**Métodos:**

- `Sequential(int number)`: Converte um número inteiro em uma representação equivalente alfabética do número.
- `Password(int len = 12, bool upper = true, bool lower = true, bool number = true, bool special = true, bool similarity = false)`: Gera uma string com critérios específicos.
- `Hexadecimal(int sizeToken = 128)`: Gera uma string hexadecimal aleatória.
- `GuidCode()`: Gera uma string Guid sem hífens, com 32 caracteres.

> **Mudou na v5:** o `Token(int length = 256)` da linha 4.x foi removido. Ele gerava só o texto, sem hash nem
> verificação. Use o `SecureToken.Create()` do `Tooark.Securities`, que acompanha `Hash` e `Verify` em tempo
> constante.

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

---

## 📝 Exemplos de Uso

### Conversão de Arquivos e Extração de Extensões

```csharp
using Tooark.Utils;
using Tooark.Exceptions;

var base64String = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQ...";

// Limite padrão de 5MB
MemoryStream? memoryStream = FileConvert.ToMemoryStream(base64String);

// De um Stream: a cópia começa na posição atual, e quem abriu a origem é quem a descarta
using var relatorio = File.OpenRead("relatorio.pdf");
MemoryStream? fromStream = FileConvert.CopyToMemoryStream(relatorio);

// Limite próprio, em bytes, na versão assíncrona
await using var video = File.OpenRead("video.mp4");
MemoryStream? grande = await FileConvert.CopyToMemoryStreamAsync(video, 20 * 1024 * 1024, cancellationToken);

string? extension = FileConvert.Extension(base64String); // PNG
string? extensionFromName = FileConvert.ExtensionFromFileName("relatorio.pdf"); // PDF

// Acima do limite, o erro é explícito e já carrega o status HTTP 413
try
{
  using var planilha = File.OpenRead("planilha.xlsx");
  FileConvert.CopyToMemoryStream(planilha, 1024);
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

// O nome e o tamanho vêm de onde o arquivo estiver: disco, armazenamento, mensagem
var foto = new FileInfo("foto.png");

bool isImage = FileValid.IsImageFile(foto.Name, foto.Length);
bool isDocument = FileValid.IsDocumentFile("contrato.pdf", 2097152, 10485760); // 2MB, com limite de 10MB
bool isVideo = FileValid.IsVideoFile("aula.mp4", 4194304);

// Com ou sem o ponto inicial, o resultado é o mesmo
bool isCustom = FileValid.IsCustomFile("dados.csv", 1024, permittedExtensions: ["txt", ".csv"]);
```

### Geração de Strings

```csharp
using Tooark.Utils;

string sequential = GenerateString.Sequential(27); // AA
string password = GenerateString.Password(); // aB3@dE1#fG2$
string onlyLetters = GenerateString.Password(16, number: false, special: false);
string hex = GenerateString.Hexadecimal(); // 1A2B3C4D5E6F...
string guid = GenerateString.GuidCode(); // 1A2B3C4D5E6F7A8B9C0D1E2F3A4B5C6D
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

---

## 📋 Dependências

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
