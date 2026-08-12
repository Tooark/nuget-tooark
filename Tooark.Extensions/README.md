# Tooark.Extensions

Biblioteca gerenciar extensões e utilitários, facilitando o desenvolvimento e a manutenção de projetos .NET.

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
using Tooark.Extensions.Injections;

// Nas suas configurações de serviços
services.AddTooarkExtensions();
```

## Conteúdo

- [EnumerableExtensions](#1-extensão-de-enumeráveis)
- [AddJsonStringLocalizer](#2-configuração-do-jsonstringlocalizer-extensão-localiza-string-dentro-de-json)
- [JsonStringLocalizerExtensions](#3-extensão-localiza-string-dentro-de-json-extensão-para-istringlocalizer)
- [StringExtensions](#4-extensões-de-string)

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
Utiliza os [arquivos](#arquivos-de-recursos-multiculturais) padrão de recursos multiculturais para localização de strings.

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

## Arquivos de Recursos Multiculturais

- [en-US.default.json](./Resources/en-US.default.json)
- [es-ES.default.json](./Resources/es-ES.default.json)
- [pt-BR.default.json](./Resources/pt-BR.default.json)
- [pt-PT.default.json](./Resources/pt-PT.default.json)

## Dependências

- [Microsoft.AspNetCore.Mvc.Core](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Core/)
- [Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)
- [Microsoft.Extensions.Localization](https://www.nuget.org/packages/Microsoft.Extensions.Localization/)
- [Tooark.Utils](../Tooark.Utils/README.md)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Extensions](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
