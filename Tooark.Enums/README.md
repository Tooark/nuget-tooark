# Tooark.Enums

Biblioteca que fornece tipos enumerados validados, permitindo a padronização para projetos .NET. Inclui métodos para conversão e validação de valores enumerados.

## Conteúdo

- [Instalação](#instalação)
- [Como funcionam](#como-funcionam)
- [ECloudProvider](#1-provedor-de-cloud)
- [EDocumentType](#2-tipo-de-documento)
- [EFileType](#3-tipo-de-arquivo)
- [Exemplos de Uso](#exemplos-de-uso)
- [Dependências](#dependências)
- [Contribuição](#contribuição)
- [Licença](#licença)

## Instalação

```bash
dotnet add package Tooark.Enums
```

O pacote não tem configuração: os enumeradores são valores estáticos, usados diretamente.

## Como funcionam

Os três tipos são classes com instâncias estáticas, e não `enum` do C#. Isso permite que cada valor carregue
mais do que um número — descrição, padrão de formato, função de validação — ao custo de não poderem ser usados
como argumento de atributo, que exige constante.

Todos expõem o mesmo conjunto de operações:

| Operação                  | Comportamento                                                            |
| ------------------------- | ------------------------------------------------------------------------ |
| `ToString()`              | Devolve a descrição                                                      |
| `ToInt()`                 | Devolve o id                                                             |
| `(int)` implícito         | Mesmo que `ToInt()`                                                      |
| `(string)` implícito      | Mesmo que `ToString()`                                                   |
| `int` → enum implícito    | Resolve pelo id                                                          |
| `string` → enum implícito | Resolve pela descrição, **sem diferenciar caixa nem espaços nas pontas** |

Duas regras valem para os três:

- **A descrição é normalizada.** `"aws"`, `"AWS"` e `" Aws "` resolvem para o mesmo valor.
- **Id ou descrição não reconhecidos resolvem para o valor neutro** — `None` em `ECloudProvider` e
  `EDocumentType`, `Unknown` em `EFileType` —, que é o membro previsto para isso. Já **converter uma
  instância nula** para `int` ou `string` lança `InternalServerErrorException` com `Invalid.Parameter;null`:
  não existe id nem descrição correta para devolver, e devolver zero seria inventar um dado.

## Enumeradores

### 1. Provedor de Cloud

**Funcionalidade:** representa os provedores de cloud suportados.

| Valor       | Id  | Descrição | Também aceita |
| ----------- | --- | --------- | ------------- |
| `None`      | 0   | `None`    | —             |
| `Amazon`    | 1   | `AWS`     | `Amazon`      |
| `Google`    | 2   | `GCP`     | `Google`      |
| `Microsoft` | 3   | `Azure`   | `Microsoft`   |

[**Exemplo de Uso**](#provedor-de-cloud)

### 2. Tipo de Documento

**Funcionalidade:** representa os tipos de documento, cada um com o próprio formato e verificação.

| Valor        | Id  | Verificação                                               |
| ------------ | --- | --------------------------------------------------------- |
| `None`       | 0   | Aceita qualquer documento                                 |
| `CPF`        | 1   | Formato e dígitos verificadores                           |
| `RG`         | 2   | Formato e dígito verificador, quando informado            |
| `CNH`        | 3   | Formato e dígitos verificadores                           |
| `CNPJ`       | 4   | Formato e dígitos verificadores, aceita CNPJ alfanumérico |
| `CPF_CNPJ`   | 5   | CPF ou CNPJ, ambos com dígitos                            |
| `CPF_RG`     | 6   | CPF ou RG                                                 |
| `CPF_RG_CNH` | 7   | CPF, RG ou CNH                                            |

**Métodos próprios:**

- `ToRegex()`: devolve o padrão de **formato** do tipo de documento.
- `IsValid`: função que verifica os **dígitos verificadores**, aceitando o valor com ou sem máscara.

> As duas verificações são camadas distintas e se complementam: `ToRegex()` cuida da forma e `IsValid` cuida
> do conteúdo. Uma validação completa aplica as duas, como fazem o value object `Document` do
> `Tooark.ValueObjects` e o `DocumentValidationAttribute` do `Tooark.Attributes`. Chamada isolada, `IsValid`
> aceita `529.982.247-25` e `52998224725` igualmente, e nunca lança para entrada inválida — devolve `false`.

O cálculo dos dígitos vem de `DocumentDigit`, no `Tooark.Validations`, o mesmo usado pelas validações daquele
pacote — então `new Validation().IsCpf(...)` e `EDocumentType.CPF.IsValid(...)` sempre concordam.

[**Exemplo de Uso**](#tipo-de-documento)

### 3. Tipo de Arquivo

**Funcionalidade:** representa as categorias de arquivo.

| Valor      | Id  | Descrição  |
| ---------- | --- | ---------- |
| `Unknown`  | 0   | `Unknown`  |
| `Document` | 1   | `Document` |
| `Image`    | 2   | `Image`    |
| `Video`    | 3   | `Video`    |
| `Audio`    | 4   | `Audio`    |

[**Exemplo de Uso**](#tipo-de-arquivo)

## Exemplos de Uso

### Provedor de Cloud

```csharp
using Tooark.Enums;

ECloudProvider provider = ECloudProvider.Amazon;

Console.WriteLine(provider.ToString()); // AWS
Console.WriteLine(provider.ToInt());    // 1

// Conversões implícitas, úteis para persistir e ler de volta
int id = provider;                      // 1
string description = provider;          // "AWS"

// A descrição não diferencia caixa
ECloudProvider daConfiguracao = "aws";  // Amazon
ECloudProvider doBanco = 1;             // Amazon
```

### Tipo de Documento

```csharp
using Tooark.Enums;

EDocumentType docType = EDocumentType.CPF;

Console.WriteLine(docType.ToString()); // CPF
Console.WriteLine(docType.ToInt());    // 1
Console.WriteLine(docType.ToRegex());  // ^\d{3}\.\d{3}\.\d{3}-\d{2}$

// IsValid confere os dígitos, com ou sem máscara
Console.WriteLine(docType.IsValid("529.982.247-25")); // True
Console.WriteLine(docType.IsValid("52998224725"));    // True
Console.WriteLine(docType.IsValid("529.982.247-24")); // False
```

Validação completa, aplicando formato e dígitos:

```csharp
using System.Text.RegularExpressions;
using Tooark.Enums;

bool EhValido(string documento, EDocumentType tipo) =>
  Regex.IsMatch(documento, tipo.ToRegex()) && tipo.IsValid(documento);
```

### Tipo de Arquivo

```csharp
using Tooark.Enums;

EFileType fileType = EFileType.Image;

Console.WriteLine(fileType.ToString()); // Image
Console.WriteLine(fileType.ToInt());    // 2

// Valor não reconhecido resolve para Unknown
EFileType desconhecido = "planilha";    // Unknown
```

## Dependências

| Dependência                                                                              | Versão | Uso                                     |
| ---------------------------------------------------------------------------------------- | ------ | --------------------------------------- |
| [`Tooark.Validations`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Validations) | 4.x    | `DocumentDigit` e os padrões de formato |
| [`Tooark.Exceptions`](https://github.com/Tooark/tooark-cs/tree/main/Tooark.Exceptions)   | 4.x    | Erro de conversão de instância nula     |

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Enums](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/tooark-cs/refs/heads/main/LICENSE) para mais detalhes.
