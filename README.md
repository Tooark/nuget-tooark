# Tooark

<p align="center">
  <img src="https://raw.githubusercontent.com/Tooark/nuget-tooark/main/Media/tooark.png" alt="Tooark" width="220">
</p>

[![NuGet](https://img.shields.io/nuget/v/Tooark.svg?label=NuGet)](https://www.nuget.org/packages/Tooark)
[![Downloads](https://img.shields.io/nuget/dt/Tooark.svg)](https://www.nuget.org/packages/Tooark)
[![Build and deploy](https://github.com/Tooark/nuget-tooark/actions/workflows/tooark.yml/badge.svg)](https://github.com/Tooark/nuget-tooark/actions/workflows/tooark.yml)
[![License: BSD-3-Clause](https://img.shields.io/badge/license-BSD--3--Clause-blue.svg)](https://github.com/Tooark/nuget-tooark/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4)](https://github.com/Tooark/nuget-tooark/blob/main/global.json)

Modular building blocks for .NET applications: validations, value objects, notifications, security,
observability, mediator and more — each one a package, all released together.

🌍 **Languages:** ![USA Flag](https://flagcdn.com/w20/us.png) **English (this file)** · [![Brazil Flag](https://flagcdn.com/w20/br.png) Português](https://github.com/Tooark/nuget-tooark/blob/main/README.pt-BR.md)

---

## Table of contents

- [About](#about)
- [Installation](#installation)
- [Packages](#packages)
- [Documentation](#documentation)
- [Local development](#local-development)
- [Support](#support)
- [Contributing](#contributing)
- [License](#license)

---

## About

Tooark is a library for .NET projects, offering a collection of modular packages that make it easier to build
robust and scalable applications. Each package targets a specific need — validations, notifications, value
objects, JWT and cryptography, OpenID Connect SSO, OpenTelemetry observability, a mediator pipeline — and can
be installed on its own or through the `Tooark` aggregator, which references all of them.

All packages target **.NET 8.0** and **.NET 10.0**, share a single version number and are published together
to the [Tooark profile on NuGet](https://www.nuget.org/profiles/Tooark).

---

## Installation

Everything at once, through the aggregator:

```bash
dotnet add package Tooark
```

Or only what you need — every package can be installed individually (see the table below). The source code of
each package lives in its own folder of this repository, together with its README.

---

## Packages

| Package                               | Version                                                                                                                                            | Downloads                                                                                                                                           | Individual Install                                       |
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

## Documentation

- **Per package** — every package folder has a `README.md` in English (the same file shown on the NuGet page)
  and a `README.pt-BR.md` in Portuguese, e.g.
  [`Tooark.Securities`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.md),
  [`Tooark.Securities.OpenId`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities.OpenId/README.md),
  [`Tooark.Observability`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Observability/README.md).
- **Release notes** — one file per version in
  [`Notes/`](https://github.com/Tooark/nuget-tooark/tree/main/Notes); the latest is the body of the
  [GitHub Release](https://github.com/Tooark/nuget-tooark/releases).
- **Tests** — [`Tooark.Tests/README.md`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Tests/README.md)
  describes how the suite is organized and the test conventions.

---

## Local development

### Prerequisites

- **.NET SDK 10.0.100 or later 10.x** — the version is pinned in
  [`global.json`](https://github.com/Tooark/nuget-tooark/blob/main/global.json) with `rollForward: latestMinor`.
  The solution builds and tests **both** `net8.0` and `net10.0`, so the **.NET 8 runtime** must also be
  installed to run the `net8.0` tests (`dotnet --list-runtimes` shows what you have).
- **Git**.
- Optional: Docker, to run the same security scanner as the release pipeline locally (see
  [Tooark/base-images](https://github.com/Tooark/base-images) → `samples/security-scanner-local.sh`).

### Clone and restore

```bash
git clone https://github.com/Tooark/nuget-tooark.git
cd nuget-tooark
dotnet restore
```

Package versions are managed centrally in
[`Directory.Packages.props`](https://github.com/Tooark/nuget-tooark/blob/main/Directory.Packages.props);
the shared version number and build settings live in
[`Directory.Build.props`](https://github.com/Tooark/nuget-tooark/blob/main/Directory.Build.props).

### Build

Warnings are treated as errors, so build in `Release` before opening a pull request — it is what CI runs:

```bash
# Whole solution, both target frameworks
dotnet build --configuration Release

# A single package
dotnet build Tooark.Securities/Tooark.Securities.csproj --configuration Release

# Pack a package locally (output in <package>/bin/Release/*.nupkg)
dotnet pack Tooark.Securities/Tooark.Securities.csproj --configuration Release
```

### Tests

A single project, [`Tooark.Tests`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Tests), covers
every package, with one folder per package. It runs on both target frameworks; a test that passes on one and
fails on the other points to a runtime difference, not to a flaky test.

```bash
# Everything, both target frameworks
dotnet test Tooark.Tests/Tooark.Tests.csproj

# One target framework, during development
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0

# One package
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 --filter "FullyQualifiedName~Tooark.Tests.ValueObjects"

# One test
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 --filter "FullyQualifiedName~Equals_ShouldBeFalse_WhenTypesDiffer"
```

### Test coverage

Coverage is collected by [coverlet](https://github.com/coverlet-coverage/coverlet), already referenced by the
test project.

**Summary table in the console** — one line per package with lines, branches and methods:

```bash
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 -p:CollectCoverage=true
```

**HTML report** — generate the Cobertura file and render it with
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

Open `cobertura/html/index.html`: every type gets a page with the source marked as covered, uncovered or
partially covered branch. The `reportgenerator` command comes from the global tool:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Two coverlet details worth knowing: the output path is resolved from the directory you run the command in,
not from the test project; and because the project is multi-targeted the file name carries the target
(`coverage.net10.0.cobertura.xml`, not `coverage.cobertura.xml`). The `cobertura/` folder is git-ignored.

**Goal:** 100% of lines, branches and methods on the packages already reviewed; where that was not possible,
the reason is recorded in the release notes under `Notes/`.

### Before opening a pull request

[`CONTRIBUTING.md`](https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md) has the full
checklist — in short: `Release` build with no warnings, tests green on both target frameworks, new behavior
covered by tests, new message keys translated in the three resource files of `Tooark.Extensions`, package
READMEs (English and Portuguese) and release notes updated.

---

## Support

Choose the channel by what you need:

| I want to…                          | Go to                                                                                                                                                                                                     |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Report a **bug**                    | [Open a bug report](https://github.com/Tooark/nuget-tooark/issues/new?template=bug_report.yml) — package, version, target framework, minimal reproduction                                                 |
| Suggest a **feature**               | [Open a feature request](https://github.com/Tooark/nuget-tooark/issues/new?template=feature_request.yml) — the problem first, then the solution                                                           |
| Ask a **question**                  | [Open a blank issue](https://github.com/Tooark/nuget-tooark/issues/new) after searching the [existing ones](https://github.com/Tooark/nuget-tooark/issues)                                                |
| Report a **security vulnerability** | **Not** an issue — use the [private security advisory](https://github.com/Tooark/nuget-tooark/security/advisories/new), see [`SECURITY.md`](https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md) |

Response targets and other contact channels are in
[`SUPPORT.md`](https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md).

---

## Contributing

Contributions are welcome! Please read
[`CONTRIBUTING.md`](https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md) (how to propose changes,
coding and commit conventions, PR checklist) and the
[`CODE_OF_CONDUCT.md`](https://github.com/Tooark/nuget-tooark/blob/main/CODE_OF_CONDUCT.md) (Contributor
Covenant 2.1) before opening a pull request.

---

## License

This project is licensed under the
[BSD 3-Clause License](https://github.com/Tooark/nuget-tooark/blob/main/LICENSE). See the `LICENSE` file for
details.
