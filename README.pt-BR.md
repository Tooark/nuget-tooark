# Tooark

<p align="center">
  <img src="https://raw.githubusercontent.com/Tooark/nuget-tooark/main/Media/tooark.png" alt="Tooark" width="220">
</p>

[![NuGet](https://img.shields.io/nuget/v/Tooark.svg?label=NuGet)](https://www.nuget.org/packages/Tooark)
[![Downloads](https://img.shields.io/nuget/dt/Tooark.svg)](https://www.nuget.org/packages/Tooark)
[![Build and deploy](https://github.com/Tooark/nuget-tooark/actions/workflows/tooark.yml/badge.svg)](https://github.com/Tooark/nuget-tooark/actions/workflows/tooark.yml)
[![License: BSD-3-Clause](https://img.shields.io/badge/license-BSD--3--Clause-blue.svg)](https://github.com/Tooark/nuget-tooark/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4)](https://github.com/Tooark/nuget-tooark/blob/main/global.json)

Blocos modulares para aplicações .NET: validações, objetos de valor, notificações, segurança,
observabilidade, mediator e mais — cada um em um pacote, todos lançados juntos.

🌍 **Idiomas:** [![USA Flag](https://flagcdn.com/w20/us.png) English](https://github.com/Tooark/nuget-tooark/blob/main/README.md) · ![Brazil Flag](https://flagcdn.com/w20/br.png) **Português (este arquivo)**

---

## Sumário

- [Sobre o Projeto](#sobre-o-projeto)
- [Instalação](#instalação)
- [Pacotes](#pacotes)
- [Documentação](#documentação)
- [Desenvolvimento local](#desenvolvimento-local)
- [Suporte](#suporte)
- [Contribuição](#contribuição)
- [Licença](#licença)

---

## Sobre o Projeto

Tooark é uma biblioteca voltada para projetos .NET, oferecendo uma coleção de pacotes modulares que facilitam
o desenvolvimento de aplicações robustas e escaláveis. Cada pacote foi projetado para atender a uma necessidade
específica — validações, notificações, objetos de valor, JWT e criptografia, SSO com OpenID Connect,
observabilidade com OpenTelemetry, pipeline de mediator — e pode ser instalado sozinho ou pelo agregador
`Tooark`, que referencia todos.

Todos os pacotes têm como alvo o **.NET 8.0** e o **.NET 10.0**, compartilham um único número de versão e são
publicados juntos no [perfil da Tooark no NuGet](https://www.nuget.org/profiles/Tooark).

---

## Instalação

Tudo de uma vez, pelo agregador:

```bash
dotnet add package Tooark
```

Ou apenas o que você precisa — cada pacote pode ser instalado individualmente (veja a tabela abaixo). O código
de cada pacote fica na sua própria pasta deste repositório, junto com o README.

---

## Pacotes

| Pacote                                | Versão                                                                                                                                             | Downloads                                                                                                                                           | Instalação individual                                    |
| ------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| `Tooark`                              | [![NuGet](https://img.shields.io/nuget/v/Tooark.svg)](https://nuget.org/packages/Tooark)                                                           | [![Nuget](https://img.shields.io/nuget/dt/Tooark.svg)](https://nuget.org/packages/Tooark)                                                           | `dotnet add package Tooark`                              |
| `Tooark.AspNetCore`                   | [![NuGet](https://img.shields.io/nuget/v/Tooark.AspNetCore.svg)](https://nuget.org/packages/Tooark.AspNetCore)                                     | [![Nuget](https://img.shields.io/nuget/dt/Tooark.AspNetCore.svg)](https://nuget.org/packages/Tooark.AspNetCore)                                     | `dotnet add package Tooark.AspNetCore`                   |
| `Tooark.Attributes`                   | [![NuGet](https://img.shields.io/nuget/v/Tooark.Attributes.svg)](https://nuget.org/packages/Tooark.Attributes)                                     | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Attributes.svg)](https://nuget.org/packages/Tooark.Attributes)                                     | `dotnet add package Tooark.Attributes`                   |
| `Tooark.Dtos`                         | [![NuGet](https://img.shields.io/nuget/v/Tooark.Dtos.svg)](https://nuget.org/packages/Tooark.Dtos)                                                 | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Dtos.svg)](https://nuget.org/packages/Tooark.Dtos)                                                 | `dotnet add package Tooark.Dtos`                         |
| `Tooark.Entities`                     | [![NuGet](https://img.shields.io/nuget/v/Tooark.Entities.svg)](https://nuget.org/packages/Tooark.Entities)                                         | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Entities.svg)](https://nuget.org/packages/Tooark.Entities)                                         | `dotnet add package Tooark.Entities`                     |
| `Tooark.Enums`                        | [![NuGet](https://img.shields.io/nuget/v/Tooark.Enums.svg)](https://nuget.org/packages/Tooark.Enums)                                               | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Enums.svg)](https://nuget.org/packages/Tooark.Enums)                                               | `dotnet add package Tooark.Enums`                        |
| `Tooark.Exceptions`                   | [![NuGet](https://img.shields.io/nuget/v/Tooark.Exceptions.svg)](https://nuget.org/packages/Tooark.Exceptions)                                     | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Exceptions.svg)](https://nuget.org/packages/Tooark.Exceptions)                                     | `dotnet add package Tooark.Exceptions`                   |
| `Tooark.Extensions`                   | [![NuGet](https://img.shields.io/nuget/v/Tooark.Extensions.svg)](https://nuget.org/packages/Tooark.Extensions)                                     | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Extensions.svg)](https://nuget.org/packages/Tooark.Extensions)                                     | `dotnet add package Tooark.Extensions`                   |
| `Tooark.Mediator`                     | [![NuGet](https://img.shields.io/nuget/v/Tooark.Mediator.svg)](https://nuget.org/packages/Tooark.Mediator)                                         | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Mediator.svg)](https://nuget.org/packages/Tooark.Mediator)                                         | `dotnet add package Tooark.Mediator`                     |
| `Tooark.Mediator.Abstractions`        | [![NuGet](https://img.shields.io/nuget/v/Tooark.Mediator.Abstractions.svg)](https://nuget.org/packages/Tooark.Mediator.Abstractions)               | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Mediator.Abstractions.svg)](https://nuget.org/packages/Tooark.Mediator.Abstractions)               | `dotnet add package Tooark.Mediator.Abstractions`        |
| `Tooark.Mediator.EntityFrameworkCore` | [![NuGet](https://img.shields.io/nuget/v/Tooark.Mediator.EntityFrameworkCore.svg)](https://nuget.org/packages/Tooark.Mediator.EntityFrameworkCore) | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Mediator.EntityFrameworkCore.svg)](https://nuget.org/packages/Tooark.Mediator.EntityFrameworkCore) | `dotnet add package Tooark.Mediator.EntityFrameworkCore` |
| `Tooark.Notifications`                | [![NuGet](https://img.shields.io/nuget/v/Tooark.Notifications.svg)](https://nuget.org/packages/Tooark.Notifications)                               | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Notifications.svg)](https://nuget.org/packages/Tooark.Notifications)                               | `dotnet add package Tooark.Notifications`                |
| `Tooark.Observability`                | [![NuGet](https://img.shields.io/nuget/v/Tooark.Observability.svg)](https://nuget.org/packages/Tooark.Observability)                               | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Observability.svg)](https://nuget.org/packages/Tooark.Observability)                               | `dotnet add package Tooark.Observability`                |
| `Tooark.Securities`                   | [![NuGet](https://img.shields.io/nuget/v/Tooark.Securities.svg)](https://nuget.org/packages/Tooark.Securities)                                     | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Securities.svg)](https://nuget.org/packages/Tooark.Securities)                                     | `dotnet add package Tooark.Securities`                   |
| `Tooark.Securities.OpenId`            | [![NuGet](https://img.shields.io/nuget/v/Tooark.Securities.OpenId.svg)](https://nuget.org/packages/Tooark.Securities.OpenId)                       | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Securities.OpenId.svg)](https://nuget.org/packages/Tooark.Securities.OpenId)                       | `dotnet add package Tooark.Securities.OpenId`            |
| `Tooark.Utils`                        | [![NuGet](https://img.shields.io/nuget/v/Tooark.Utils.svg)](https://nuget.org/packages/Tooark.Utils)                                               | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Utils.svg)](https://nuget.org/packages/Tooark.Utils)                                               | `dotnet add package Tooark.Utils`                        |
| `Tooark.Validations`                  | [![NuGet](https://img.shields.io/nuget/v/Tooark.Validations.svg)](https://nuget.org/packages/Tooark.Validations)                                   | [![Nuget](https://img.shields.io/nuget/dt/Tooark.Validations.svg)](https://nuget.org/packages/Tooark.Validations)                                   | `dotnet add package Tooark.Validations`                  |
| `Tooark.ValueObjects`                 | [![NuGet](https://img.shields.io/nuget/v/Tooark.ValueObjects.svg)](https://nuget.org/packages/Tooark.ValueObjects)                                 | [![Nuget](https://img.shields.io/nuget/dt/Tooark.ValueObjects.svg)](https://nuget.org/packages/Tooark.ValueObjects)                                 | `dotnet add package Tooark.ValueObjects`                 |

---

## Documentação

- **Por pacote** — cada pasta de pacote tem um `README.md` em inglês (o mesmo arquivo exibido na página do
  NuGet) e um `README.pt-BR.md` em português, ex.:
  [`Tooark.Securities`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.pt-BR.md),
  [`Tooark.Securities.OpenId`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities.OpenId/README.pt-BR.md),
  [`Tooark.Observability`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Observability/README.pt-BR.md).
- **Notas de release** — um arquivo por versão em
  [`Notes/`](https://github.com/Tooark/nuget-tooark/tree/main/Notes); a mais recente é o corpo da
  [GitHub Release](https://github.com/Tooark/nuget-tooark/releases).
- **Testes** — [`Tooark.Tests/README.pt-BR.md`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Tests/README.pt-BR.md)
  descreve a organização da suíte e as convenções de teste.

---

## Desenvolvimento local

### Pré-requisitos

- **.NET SDK 10.0.100 ou 10.x posterior** — a versão está fixada no
  [`global.json`](https://github.com/Tooark/nuget-tooark/blob/main/global.json) com `rollForward: latestMinor`.
  A solução compila e testa **os dois** alvos, `net8.0` e `net10.0`, então o **runtime do .NET 8** também
  precisa estar instalado para rodar os testes em `net8.0` (`dotnet --list-runtimes` mostra o que você tem).
- **Git**.
- Opcional: Docker, para rodar localmente o mesmo scanner de segurança do pipeline de release (veja
  [Tooark/base-images](https://github.com/Tooark/base-images) → `samples/security-scanner-local.sh`).

### Clonar e restaurar

```bash
git clone https://github.com/Tooark/nuget-tooark.git
cd nuget-tooark
dotnet restore
```

As versões dos pacotes são gerenciadas centralmente no
[`Directory.Packages.props`](https://github.com/Tooark/nuget-tooark/blob/main/Directory.Packages.props); o
número de versão compartilhado e as configurações de build ficam no
[`Directory.Build.props`](https://github.com/Tooark/nuget-tooark/blob/main/Directory.Build.props).

### Build

Warnings são tratados como erros, então compile em `Release` antes de abrir um pull request — é o que o CI
executa:

```bash
# Solução inteira, nos dois alvos
dotnet build --configuration Release

# Um único pacote
dotnet build Tooark.Securities/Tooark.Securities.csproj --configuration Release

# Empacotar localmente (saída em <pacote>/bin/Release/*.nupkg)
dotnet pack Tooark.Securities/Tooark.Securities.csproj --configuration Release
```

### Testes

Um único projeto, [`Tooark.Tests`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Tests), cobre
todos os pacotes, com uma pasta por pacote. Ele roda nos dois alvos; um teste que passa em um e falha no
outro aponta diferença entre os runtimes, e não um teste instável.

```bash
# Tudo, nos dois alvos
dotnet test Tooark.Tests/Tooark.Tests.csproj

# Um alvo só, durante o desenvolvimento
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0

# Um pacote só
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 --filter "FullyQualifiedName~Tooark.Tests.ValueObjects"

# Um teste só
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 --filter "FullyQualifiedName~Equals_ShouldBeFalse_WhenTypesDiffer"
```

### Cobertura de testes

A cobertura é coletada pelo [coverlet](https://github.com/coverlet-coverage/coverlet), já referenciado pelo
projeto de testes.

**Tabela resumida no console** — uma linha por pacote, com linhas, branches e métodos:

```bash
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 -p:CollectCoverage=true
```

**Relatório HTML** — gere o arquivo Cobertura e renderize com o
[ReportGenerator](https://github.com/danielpalme/ReportGenerator):

```bash
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 \
  -p:CollectCoverage=true \
  -p:CoverletOutputFormat=cobertura \
  -p:CoverletOutput=cobertura/

reportgenerator \
  -reports:cobertura/coverage.net10.0.cobertura.xml \
  -targetdir:cobertura/html \
  -reporttypes:Html
```

Abra `cobertura/html/index.html`: cada tipo ganha uma página com o código-fonte marcando linha coberta, linha
descoberta e branch parcialmente coberta. O comando `reportgenerator` vem da ferramenta global:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Dois detalhes do coverlet que costumam confundir: o caminho de saída é resolvido a partir do diretório de onde
o comando roda, e não do projeto de teste; e, como o projeto é multi-alvo, o nome do arquivo recebe o alvo
(`coverage.net10.0.cobertura.xml`, e não `coverage.cobertura.xml`). A pasta `cobertura/` está no
`.gitignore`.

**Meta:** 100% de linhas, branches e métodos nos pacotes já revisados; onde não foi possível, o motivo fica
registrado nas notas da versão, em `Notes/`.

### Antes de abrir um pull request

O [`CONTRIBUTING.md`](https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md) tem o checklist
completo — em resumo: build `Release` sem warnings, testes verdes nos dois alvos, comportamento novo coberto por
testes, chaves de mensagem novas traduzidas nos três arquivos de recurso do `Tooark.Extensions`, READMEs dos
pacotes (inglês e português) e notas de release atualizados.

---

## Suporte

Escolha o canal pelo que você precisa:

| Quero…                           | Onde                                                                                                                                                                                                             |
| -------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Reportar um **bug**              | [Abrir um bug report](https://github.com/Tooark/nuget-tooark/issues/new?template=bug_report.yml) — pacote, versão, alvo, reprodução mínima                                                                       |
| Sugerir uma **funcionalidade**   | [Abrir um feature request](https://github.com/Tooark/nuget-tooark/issues/new?template=feature_request.yml) — o problema primeiro, depois a solução                                                               |
| Tirar uma **dúvida**             | [Abrir uma issue em branco](https://github.com/Tooark/nuget-tooark/issues/new) depois de procurar nas [existentes](https://github.com/Tooark/nuget-tooark/issues)                                                |
| Reportar uma **vulnerabilidade** | **Não** abra issue — use o [advisory privado de segurança](https://github.com/Tooark/nuget-tooark/security/advisories/new), veja o [`SECURITY.md`](https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md) |

Prazos de resposta e outros canais de contato estão no
[`SUPPORT.md`](https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md).

---

## Contribuição

Contribuições são bem-vindas! Leia o
[`CONTRIBUTING.md`](https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md) (como propor mudanças,
convenções de código e de commit, checklist de PR) e o
[`CODE_OF_CONDUCT.md`](https://github.com/Tooark/nuget-tooark/blob/main/CODE_OF_CONDUCT.md) (Contributor
Covenant 2.1) antes de abrir um pull request.

---

## Licença

Este projeto está licenciado sob a
[Licença BSD 3-Clause](https://github.com/Tooark/nuget-tooark/blob/main/LICENSE). Consulte o arquivo `LICENSE`
para mais detalhes.
