# Tooark.Tests

Suíte de testes de todos os pacotes Tooark. Não é publicada no nuget.org (`IsPackable=false`).

## Conteúdo

- [Visão Geral](#visão-geral)
- [Executando](#-executando)
- [Cobertura](#-cobertura)
- [Organização](#-organização)
- [Convenções](#-convenções)
- [Recursos Compartilhados](#-recursos-compartilhados)
- [Testes Sensíveis a Estado Global](#️-testes-sensíveis-a-estado-global)

## Visão Geral

Um único projeto cobre todos os pacotes, com uma pasta por pacote. Ele referencia o agregador
`Tooark` e o `Tooark.AspNetCore`, então alcança toda a superfície pública sem uma referência por
pacote.

Os testes rodam nos **dois alvos** do repositório, `net8.0` e `net10.0`. Um teste que passe em um e
falhe no outro indica diferença de comportamento entre os runtimes, e não um teste instável.

---

## ▶️ Executando

```bash
# Todos os alvos
dotnet test Tooark.Tests/Tooark.Tests.csproj

# Um alvo só, durante o desenvolvimento
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0

# Um pacote só
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 --filter "FullyQualifiedName~Tooark.Tests.ValueObjects"

# Um teste só
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 --filter "FullyQualifiedName~Equals_ShouldBeFalse_WhenTypesDiffer"
```

---

## 📊 Cobertura

```bash
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 \
  -p:CollectCoverage=true \
  -p:CoverletOutputFormat=json \
  -p:CoverletOutput=../cobertura/
```

A tabela por pacote sai no console ao fim da execução. O arquivo JSON traz o detalhe por linha e por
branch, útil para descobrir **qual** caminho falta — escrever teste a partir da lacuna medida rende
mais do que a partir de suposição.

A meta praticada é 100% de linhas, branches e métodos nos pacotes revisados. Onde não é possível,
o motivo fica registrado em `Notes/revisao-pendencias.md`.

---

## 📁 Organização

Uma pasta por pacote, espelhando a estrutura do projeto testado:

| Pasta | Pacote testado |
| --- | --- |
| `AspNetCore/` | `Tooark.AspNetCore` |
| `Attributes/` | `Tooark.Attributes` |
| `Dtos/` | `Tooark.Dtos` |
| `Entities/` | `Tooark.Entities` |
| `Enums/` | `Tooark.Enums` |
| `Exceptions/` | `Tooark.Exceptions` |
| `Extensions/` | `Tooark.Extensions` |
| `Injections/` | `Tooark` (o agregador) |
| `Mediator/` | `Tooark.Mediator`, `.Abstractions` e `.EntityFrameworkCore` |
| `Notifications/` | `Tooark.Notifications` |
| `Observability/` | `Tooark.Observability` |
| `Securities/` | `Tooark.Securities` |
| `Utils/` | `Tooark.Utils` |
| `Validations/` | `Tooark.Validations` |
| `ValueObjects/` | `Tooark.ValueObjects` |

Duas pastas não contêm testes:

- **`Moq/`** — dublês compartilhados: entidades de exemplo, DTOs e utilitários usados por mais de um
  arquivo de teste. Um dublê usado em dois lugares mora aqui, não duplicado em cada um.
- **`Resources/`** — arquivos `{idioma}.json` que exercitam a **sobrescrita do consumidor** sobre as
  traduções embutidas no `Tooark.Extensions`. Não confundir com os `{idioma}.default.json` do
  pacote, que vão embutidos no assembly e são lidos pelo manifesto.

---

## ✏️ Convenções

### Nome do teste

`Metodo_Deve_Quando`, em inglês, com o "quando" apenas onde há condição a distinguir:

```csharp
SetUpdatedBy_ShouldIncrementVersion_WhenCalledThroughBaseReference
Equals_ShouldBeFalse_WhenTypesDiffer
Value_ShouldReturnUnit
```

### Comentário de intenção

**Todo teste tem um comentário imediatamente acima do atributo**, dizendo o que se verifica e, quando
o teste nasceu de um defeito, por que ele existe. O nome diz o quê; o comentário diz por quê:

```csharp
// Testa se SetUpdatedBy incrementa a versão quando chamado por referência de DetailedEntity.
// Com 'new' no lugar de 'override', esta chamada executava o método da base e não incrementava.
[Fact]
public void SetUpdatedBy_ShouldIncrementVersion_WhenCalledThroughBaseReference()
```

Comentário que apenas repete o nome do teste não acrescenta nada. O que vale registrar é o defeito
que o teste impede de voltar, ou a razão não óbvia de a asserção ser aquela.

### Arrange / Act / Assert

As três seções são marcadas por comentário, e as etapas que se fundem aparecem juntas
(`// Arrange & Act & Assert`):

```csharp
[Fact]
public void SetDeleted_ShouldRecordWhoChanged()
{
  // Arrange
  var entidade = new Auditavel(Guid.NewGuid());
  var autor = Guid.NewGuid();

  // Act
  entidade.SetDeleted(new DeletedBy(autor));

  // Assert
  Assert.Equal(autor, entidade.UpdatedById);
}
```

### Token de cancelamento

Chamadas assíncronas usam `TestContext.Current.CancellationToken`, para o cancelamento da execução
chegar ao teste (regra `xUnit1051`, tratada como erro).

### Teste de revisão

Cada revisão de pacote da v4.0.0 deixou um arquivo `{Pacote}ReviewTests.cs` com os testes dos
defeitos corrigidos. Eles ficam separados dos testes originais de propósito: agrupam o que não pode
regredir, com o contexto do defeito no comentário.

---

## 🔁 Recursos Compartilhados

Antes de declarar um dublê dentro do arquivo de teste, verifique se ele já existe em `Moq/`. O
`TestException` chegou a existir em quinze cópias idênticas, uma por arquivo de teste de exceção,
até ser consolidado em `Moq/Notifications/`.

Um dublê usado por um único arquivo pode continuar nele, como classe aninhada privada — o custo de
compartilhar só compensa a partir do segundo uso.

---

## ⚠️ Testes Sensíveis a Estado Global

A cultura corrente é estado de processo. Testes que a alteram precisam da coleção
`CultureSensitive`, definida em `CultureSensitiveCollection.cs`:

```csharp
[Collection("CultureSensitive")]
public class MeuTeste
```

A coleção tem `DisableParallelization`, e roda em fase isolada. Sem isso, um teste que troca a
cultura corre em paralelo com outro que a lê, e a falha aparece de forma intermitente — o que piora
quando os dois alvos, `net8.0` e `net10.0`, disputam CPU na mesma máquina.

O mesmo cuidado vale para qualquer outro estado compartilhado de processo que venha a ser exercitado.
