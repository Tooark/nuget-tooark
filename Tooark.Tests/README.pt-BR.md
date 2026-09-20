# Tooark.Tests

Suíte de testes de todos os pacotes Tooark. Não é publicada no nuget.org (`IsPackable=false`).

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Tests/README.md) · 🇧🇷 **Português (este arquivo)**

## Conteúdo

- [Visão Geral](#visão-geral)
- [Executando](#️-executando)
- [Cobertura](#-cobertura)
- [Organização](#-organização)
- [Convenções](#️-convenções)
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

A suíte roda pelo [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
(MTP), o runner nativo do xunit.v3 4.x: `UseMicrosoftTestingPlatformRunner` está ligado no projeto e o
`global.json` define `test.runner` como `Microsoft.Testing.Platform`, o que coloca o `dotnet test` no modo
MTP. Nesse modo o projeto é informado com `--project`, e tudo depois de `--` vai para o runner de testes — a
sintaxe antiga do VSTest, `--filter "FullyQualifiedName~..."`, não vale mais.

```bash
# Todos os alvos
dotnet test --project Tooark.Tests/Tooark.Tests.csproj

# Um alvo só, durante o desenvolvimento
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0

# Um pacote só (namespace)
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0 -- --filter-namespace Tooark.Tests.ValueObjects

# Uma classe só (o curinga substitui o prefixo do namespace)
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0 -- --filter-class "*CpfTests"

# Um teste só
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0 -- --filter-method "*Equals_ShouldBeFalse_WhenTypesDiffer"

# Relatório TRX (gravado no diretório de resultados)
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -- --report-xunit-trx
```

Vários valores do mesmo filtro cabem numa chave só (`--filter-class "*CpfTests" "*CnpjTests"`), e cada
filtro tem o par `--filter-not-*`. A lista completa sai em `dotnet test --project Tooark.Tests/Tooark.Tests.csproj -- --help`.

---

## 📊 Cobertura

### Coletando

A cobertura é coletada pelo [coverlet](https://github.com/coverlet-coverage/coverlet) por meio da sua
extensão para o MTP, o `coverlet.MTP` (o `coverlet.msbuild` clássico só funciona no VSTest). Adicione
`--coverlet` a qualquer execução:

```bash
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0 -- \
  --coverlet --coverlet-output-format cobertura
```

O `--coverlet-output-format` aceita `json`, `lcov`, `opencover`, `cobertura` e `teamcity`, e pode ser
repetido. O JSON traz o detalhe necessário para descobrir **qual** caminho falta — escrever teste a
partir da lacuna medida rende mais do que a partir de suposição.

Dois detalhes do coverlet.MTP que costumam confundir:

- o relatório vai para o **diretório de resultados**: `TestResults/` dentro do diretório de onde o
  comando roda, ou onde `--results-directory` apontar. Use caminho absoluto quando o destino importar;
- uma execução nunca sobrescreve um relatório existente: quando já há um, o arquivo novo ganha um
  timestamp no nome (`coverage.cobertura.<timestamp>.xml`). Rodar os dois alvos no mesmo diretório
  deixa, portanto, dois arquivos — use o glob (`coverage.cobertura*.xml`) e deixe o ReportGenerator
  fundi-los.

### Resumo e relatório HTML

O coverlet.MTP não imprime resumo no console. O `ReportGenerator` já é dependência do projeto; aponte-o
para os arquivos Cobertura e peça o resumo em texto e o HTML de uma vez:

```bash
reportgenerator \
  -reports:"TestResults/coverage.cobertura*.xml" \
  -targetdir:TestResults/coveragereport \
  -reporttypes:"Html;TextSummary"
```

O `TestResults/coveragereport/Summary.txt` traz os totais e um bloco por assembly — o suficiente para
o dia a dia. O `index.html` dá a cada tipo uma página com o código-fonte marcando linha coberta, linha
descoberta e branch parcialmente coberta — é a forma mais rápida de ver o caminho que falta.

O comando `reportgenerator` vem da ferramenta global:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Sem instalá-la, chame o executável que já veio com o pacote, em
`~/.nuget/packages/reportgenerator/<versão>/tools/net10.0/ReportGenerator.exe`. No VS Code, a tarefa
`generate coverage report` (`.vscode/tasks.json`) roda a coleta e o relatório em sequência.

O CI faz a mesma coleta em todo pull request, publica os arquivos Cobertura e o relatório como o
artefato `coverage` e escreve o resumo na página do job.

### Meta

100% de linhas, branches e métodos nos pacotes revisados. Onde não foi possível, o motivo fica
registrado nas notas da versão, em `Notes/`.

## 📁 Organização

Uma pasta por pacote, espelhando a estrutura do projeto testado:

| Pasta            | Pacote testado                                               |
| ---------------- | ------------------------------------------------------------ |
| `AspNetCore/`    | `Tooark.AspNetCore`                                          |
| `Attributes/`    | `Tooark.Attributes`                                          |
| `Dtos/`          | `Tooark.Dtos`                                                |
| `Entities/`      | `Tooark.Entities`                                            |
| `Enums/`         | `Tooark.Enums`                                               |
| `Exceptions/`    | `Tooark.Exceptions`                                          |
| `Extensions/`    | `Tooark.Extensions`                                          |
| `Injections/`    | `Tooark` (o agregador)                                       |
| `Mediator/`      | `Tooark.Mediator`, `.Abstractions` e `.EntityFrameworkCore`  |
| `Notifications/` | `Tooark.Notifications`                                       |
| `Observability/` | `Tooark.Observability`                                       |
| `Securities/`    | `Tooark.Securities` e `Tooark.Securities.OpenId` (`OpenId/`) |
| `Utils/`         | `Tooark.Utils`                                               |
| `Validations/`   | `Tooark.Validations`                                         |
| `ValueObjects/`  | `Tooark.ValueObjects`                                        |

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
