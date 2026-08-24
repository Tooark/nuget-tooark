# Tooark.Extensions

Biblioteca para gerenciar extensões e utilitários, facilitando o desenvolvimento e a manutenção de projetos .NET.

## Instalação

```bash
dotnet add package Tooark.Extensions
```

## Configuração

Os arquivos de idioma acompanham o assembly, então **não há nada a configurar** para as traduções
funcionarem — nem em aplicação, nem em contêiner, nem em publicação single-file.

Adicione a seguinte linha no seu arquivo `Program.cs`:

```csharp
// Importando o namespace necessário
using Tooark.Extensions.Injections;

// Nas suas configurações de serviços
services.AddTooarkExtensions();
```

## Conteúdo

- [EnumerableExtensions](#1-extensão-de-enumeráveis)
- [AddJsonStringLocalizer](#2-configuração-do-jsonstringlocalizer-extensão-localiza-string-dentro-de-json)
- [JsonStringLocalizerExtensions](#3-extensão-localiza-string-dentro-de-json-extensão-para-istringlocalizer)
- [StringExtensions](#4-extensões-de-string)
- [Catálogo de mensagens](#catálogo-de-mensagens)

## Extensões

As extensões disponíveis são:

### 1. Extensão de Enumeráveis

**Funcionalidade:**
Ordenação de coleções de objetos por propriedades específicas e propriedades de sub classes. Suporta ordenação ascendente e descendente de coleções de objetos.

**Métodos:**

- `OrderByProperty<T>(string sortProperty)`: Ordena uma coleção de objetos de forma ascendente por uma propriedade específica.
- `OrderByPropertyDescending<T>(string sortProperty)`: Ordena uma coleção de objetos de forma descendente por uma propriedade específica.

[**Exemplo de Uso**](#extensão-de-enumeráveis)

### 2. Configuração do JsonStringLocalizer (Extensão Localiza String dentro de Json)

**Funcionalidade:**
Adiciona a injeção de dependência do serviço de localização de strings com base em arquivos JSON.

**Métodos:**

- `AddJsonStringLocalizer`: Adiciona a injeção de dependência do serviço de localização de strings com base em arquivos JSON.

[**Exemplo de Uso**](#configuração-do-jsonstringlocalizer)

### 3. Extensão Localiza String dentro de Json (Extensão para IStringLocalizer)

**Funcionalidade:**
Utiliza os arquivos padrão de recursos multiculturais para localização de strings.

**Métodos:**

- `LocalizedString this[string name]`: Representa um valor localizado.
- `LocalizedString this[string name, params object[] arguments]`: Representa um valor localizado com argumentos.
- `GetAllStrings(bool includeParentCultures)`: Obtém todos os valores localizados. Se `includeParentCultures` utilizado para valores da cultura `default` caso `true` ou `current` caso `false`.

[**Exemplo de Uso**](#extensão-localiza-string-dentro-de-json-extensões-para-istringlocalizer)

### 4. Extensões de String

**Funcionalidade:**
Extensões para manipulação de strings.

**Métodos:**

- `ToBase64`: Converte uma string para Base64.
- `FromBase64`: Converte uma string Base64 de volta para uma string normal.
- `ToSlug`: Converte uma string para um formato de slug.
- `ToNormalize`: Normaliza uma string removendo espaços, convertendo para maiúscula e substituindo caracteres especiais.
- `FromSnakeToPascalCase`: Converte uma string de snake_case para PascalCase.
- `FromSnakeToCamelCase`: Converte uma string de snake_case para camelCase.
- `FromSnakeToKebabCase`: Converte uma string de snake_case para kebab-case.
- `FromPascalToSnakeCase`: Converte uma string de PascalCase para snake_case.
- `FromCamelToSnakeCase`: Converte uma string de camelCase para snake_case.
- `FromKebabToSnakeCase`: Converte uma string de kebab-case para snake_case.

[**Exemplos de Uso**](#extensões-de-string)

## Exemplos de Uso

### Extensão de Enumeráveis

**OrderByProperty com Parâmetro Simples:**

```csharp
using Tooark.Extensions;

List<MyObject> list = [{"Name": "B", "Age": 20}, {"Name": "A", "Age": 30}, {"Name": "C", "Age": 10}];

var sortedList = list.OrderByProperty("Name").toList();
// [{"Name": "A", "Age": 30}, {"Name": "B", "Age": 20}, {"Name": "C", "Age": 10}]
```

**OrderByPropertyDescending com Parâmetro Simples:**

```csharp
using Tooark.Extensions;

List<MyObject> list = [{"Name": "B", "Age": 20}, {"Name": "A", "Age": 30}, {"Name": "C", "Age": 10}];

var sortedList = list.OrderByPropertyDescending("Name").toList();
// [{"Name": "C", "Age": 10}, {"Name": "B", "Age": 20}, {"Name": "A", "Age": 30}]
```

**OrderByPropertyDescending com Parâmetro Complexo:**

```csharp
using Tooark.Extensions;

List<MyObject> list = [
  {"Name": "B", "Age": 20, "Address": {"City": "City C"}},
  {"Name": "A", "Age": 30, "Address": {"City": "City B"}},
  {"Name": "C", "Age": 10, "Address": {"City": "City A"}}
];

var sortedList = list.OrderByProperty("Address.City").toList();
// [
//  {"Name": "C", "Age": 10, "Address": {"City": "City A"}},
//  {"Name": "A", "Age": 30, "Address": {"City": "City B"}},
//  {"Name": "B", "Age": 20, "Address": {"City": "City C"}}
//]
```

### Configuração do JsonStringLocalizer

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tooark.Extensions.Options;
using Tooark.Extensions.Injections;

var services = new ServiceCollection();

services.AddJsonStringLocalizer();
```

### Extensão Localiza String dentro de Json (Extensões para IStringLocalizer)

**Obter Valor Localizado:**

```csharp
using Tooark.Extensions;

var localizedString = _localizer["Field"]; // "Campo"
```

**Obter Valor Localizado com Argumentos juntos:**

```csharp
using Tooark.Extensions;

var localizedString = _localizer["Field.Empty;Name"]; // "O campo Name está vazio"
```

**Obter Valor Localizado com Argumentos:**

```csharp
using Tooark.Extensions;

var localizedString = _localizer["Field.Empty", "Name"]; // "O campo Name está vazio"
```

### Extensões de String

**ToBase64:**

```csharp
using Tooark.Extensions;
string value = "Hello World!";
string base64Value = value.ToBase64(); // SGVsbG8gV29ybGQh
```

**FromBase64:**

```csharp
using Tooark.Extensions;
string base64Value = "SGVsbG8gV29ybGQh";
string normalValue = base64Value.FromBase64(); // Hello World!
```

**ToSlug:**

```csharp
using Tooark.Extensions;
string value = "Hello World!";
string slugValue = value.ToSlug(); // hello-world
```

**ToNormalize:**

```csharp
using Tooark.Extensions;

string value = "Olá Mundo!";
string normalizedValue = value.ToNormalize(); // OLAMUNDO
```

**FromSnakeToPascalCase:**

```csharp
using Tooark.Extensions;

string value = "hello_world";
string pascalCaseValue = value.FromSnakeToPascalCase(); // HelloWorld
```

**FromSnakeToCamelCase:**

```csharp
using Tooark.Extensions;

string value = "hello_world";
string camelCaseValue = value.FromSnakeToCamelCase(); // helloWorld
```

**FromSnakeToKebabCase:**

```csharp
using Tooark.Extensions;

string value = "hello_world";
string kebabCaseValue = value.FromSnakeToKebabCase(); // hello-world
```

**FromPascalToSnakeCase:**

```csharp
using Tooark.Extensions;

string value = "HelloWorld";
string snakeCaseValue = value.FromPascalToSnakeCase(); // hello_world
```

**FromCamelToSnakeCase:**

```csharp
using Tooark.Extensions;

string value = "helloWorld";
string snakeCaseValue = value.FromCamelToSnakeCase(); // hello_world
```

**FromKebabToSnakeCase:**

```csharp
using Tooark.Extensions;

string value = "hello-world";
string snakeCaseValue = value.FromKebabToSnakeCase(); // hello_world
```

## Catálogo de mensagens

Os arquivos de recurso cobrem **todas as mensagens emitidas pelos pacotes Tooark**: validações, atributos,
exceções, notificações, mediador, unidade de trabalho, criptografia, JWT, observabilidade, enumeradores e
utilitários. São 135 chaves, com o mesmo conjunto nos três idiomas.

| Idioma    | Arquivo                                                                                                            |
| --------- | ------------------------------------------------------------------------------------------------------------------ |
| Inglês    | [en-US.default.json](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Extensions/Resources/en-US.default.json) |
| Espanhol  | [es-ES.default.json](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Extensions/Resources/es-ES.default.json) |
| Português | [pt-BR.default.json](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Extensions/Resources/pt-BR.default.json) |

O idioma padrão da aplicação é o `en-US`, e é para ele que a busca cai quando o idioma atual não tem
tradução para a chave. Um idioma sem arquivo próprio — o `pt-PT`, por exemplo, que deixou de ser
distribuído nesta versão — funciona pelo mesmo caminho, respondendo em inglês.

### Formato das chaves

A chave pode trazer parâmetros separados por ponto e vírgula, no formato `Chave;parametro1;parametro2`.
Os parâmetros substituem os marcadores `{0}`, `{1}` e assim por diante, e são eles próprios traduzidos
quando correspondem a uma chave existente:

```csharp
localizer["Field.Required;Email"]        // pt-BR: "O campo E-mail é obrigatório"
localizer["Validation.IsBetween;Idade;18;65"]  // pt-BR: "O valor da propriedade Idade está entre 18 e 65."
```

Quando a chave não existe, `LocalizedString.ResourceNotFound` é verdadeiro e `Value` traz o texto recebido
inalterado — mesmo comportamento do `ResourceManagerStringLocalizer` do framework. Isso permite passar pelo
localizador um texto que não é chave do Tooark, como as mensagens que o model binding do ASP.NET Core gera,
sem que ele seja alterado.

### Sobrescrevendo ou acrescentando traduções

Coloque um arquivo `Resources/{idioma}.json` na saída da sua aplicação. Ele é mesclado sobre o
`{idioma}.default.json` embutido no pacote, **chave a chave**: você sobrescreve apenas o que quiser e
pode acrescentar chaves próprias. As traduções são lidas uma vez por idioma, no primeiro uso.

```xml
<ItemGroup>
  <None Update="Resources\**\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

O arquivo do consumidor **não** leva o `.default` no nome — esse sufixo identifica o que vem do pacote.

## Dependências

| Dependência                         | Versão   | Uso                                          |
| ----------------------------------- | -------- | -------------------------------------------- |
| `Tooark.Utils`                      | 4.x      | Normalização e idioma                        |
| `Microsoft.Extensions.Localization` | 8.x/10.x | `IStringLocalizer` e o registro no container |

> **O pacote não exige o runtime do ASP.NET Core.** Até a v3 ele declarava o framework compartilhado por
> causa do `ModelStateExtension`, e esse requisito se propagava para o `Tooark.Dtos`, o `Tooark.ValueObjects`,
> o `Tooark.Entities` e o agregador. Na v4 o `ModelStateExtension` passou para o
> [Tooark.AspNetCore](https://github.com/Tooark/tooark-cs/tree/main/Tooark.AspNetCore), e este pacote voltou a
> ser de uso geral: funciona em console, worker e função serverless sem o runtime do ASP.NET Core instalado.

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Extensions](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/tooark-cs/refs/heads/main/LICENSE) para mais detalhes.
