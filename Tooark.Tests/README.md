# Tooark.Tests

Test suite of every Tooark package. It is not published to nuget.org (`IsPackable=false`).

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Tests/README.pt-BR.md)

## Contents

- [Overview](#overview)
- [Running](#️-running)
- [Coverage](#-coverage)
- [Organization](#-organization)
- [Conventions](#️-conventions)
- [Shared Resources](#-shared-resources)
- [Tests Sensitive to Global State](#️-tests-sensitive-to-global-state)

## Overview

A single project covers every package, with one folder per package. It references the `Tooark`
aggregator and `Tooark.AspNetCore`, so it reaches the whole public surface without one reference per
package.

The tests run on the repository's **two targets**, `net8.0` and `net10.0`. A test that passes on one
and fails on the other points to a behavior difference between the runtimes, not to a flaky test.

---

## ▶️ Running

```bash
# Every target
dotnet test Tooark.Tests/Tooark.Tests.csproj

# A single target, during development
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0

# A single package
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 --filter "FullyQualifiedName~Tooark.Tests.ValueObjects"

# A single test
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 --filter "FullyQualifiedName~Equals_ShouldBeFalse_WhenTypesDiffer"
```

---

## 📊 Coverage

### Table in the console

```bash
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 -p:CollectCoverage=true
```

Prints one line per package, with lines, branches and methods, plus the total and the average at the
end. It is enough for day-to-day work.

### Detail per line and per branch

```bash
dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 \
  -p:CollectCoverage=true \
  -p:CoverletOutputFormat=json \
  -p:CoverletOutput=cobertura/
```

The JSON carries the detail needed to find out **which** path is missing — writing a test from the
measured gap pays more than writing it from a guess.

Two coverlet details that tend to confuse:

- the relative path is resolved from the directory the command runs in, not from the test project.
  Use an absolute path when the destination matters;
- the file name gets the target, because the project is multi-targeted: it comes out as
  `coverage.net10.0.json`, not `coverage.json`.

### HTML report

`ReportGenerator` is already a dependency of the project. Generate the coverage in the `cobertura`
format and point the report at it:

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

Open `cobertura/html/index.html`. Every type gets a page with the source code marking covered lines,
uncovered lines and partially covered branches — it is the fastest way to see the missing path.

The `reportgenerator` command comes from the global tool:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Without installing it, call the executable that already came with the package, at
`~/.nuget/packages/reportgenerator/<version>/tools/net10.0/ReportGenerator.exe`.

### Goal

100% of lines, branches and methods on the reviewed packages. Where that was not possible, the reason
is recorded in the release notes, under `Notes/`.

## 📁 Organization

One folder per package, mirroring the structure of the package under test:

| Folder           | Package under test                                             |
| ---------------- | -------------------------------------------------------------- |
| `AspNetCore/`    | `Tooark.AspNetCore`                                            |
| `Attributes/`    | `Tooark.Attributes`                                            |
| `Dtos/`          | `Tooark.Dtos`                                                  |
| `Entities/`      | `Tooark.Entities`                                              |
| `Enums/`         | `Tooark.Enums`                                                 |
| `Exceptions/`    | `Tooark.Exceptions`                                            |
| `Extensions/`    | `Tooark.Extensions`                                            |
| `Injections/`    | `Tooark` (the aggregator)                                      |
| `Mediator/`      | `Tooark.Mediator`, `.Abstractions` and `.EntityFrameworkCore`  |
| `Notifications/` | `Tooark.Notifications`                                         |
| `Observability/` | `Tooark.Observability`                                         |
| `Securities/`    | `Tooark.Securities` and `Tooark.Securities.OpenId` (`OpenId/`) |
| `Utils/`         | `Tooark.Utils`                                                 |
| `Validations/`   | `Tooark.Validations`                                           |
| `ValueObjects/`  | `Tooark.ValueObjects`                                          |

Two folders hold no tests:

- **`Moq/`** — shared test doubles: sample entities, DTOs and utilities used by more than one test
  file. A double used in two places lives here, not duplicated in each one.
- **`Resources/`** — `{language}.json` files that exercise the **consumer override** over the
  translations embedded in `Tooark.Extensions`. Not to be confused with the package's
  `{language}.default.json`, which are embedded in the assembly and read from the manifest.

---

## ✏️ Conventions

### Test name

`Method_Should_When`, in English, with the "when" only where there is a condition to distinguish:

```csharp
SetUpdatedBy_ShouldIncrementVersion_WhenCalledThroughBaseReference
Equals_ShouldBeFalse_WhenTypesDiffer
Value_ShouldReturnUnit
```

### Intent comment

**Every test has a comment right above the attribute**, saying what is verified and, when the test was
born from a defect, why it exists. The name says what; the comment says why:

```csharp
// Testa se SetUpdatedBy incrementa a versão quando chamado por referência de DetailedEntity.
// Com 'new' no lugar de 'override', esta chamada executava o método da base e não incrementava.
[Fact]
public void SetUpdatedBy_ShouldIncrementVersion_WhenCalledThroughBaseReference()
```

A comment that merely repeats the test name adds nothing. What is worth recording is the defect the test
keeps from coming back, or the non-obvious reason for the assertion being what it is. Comments are
written in Portuguese, the project's working language.

### Arrange / Act / Assert

The three sections are marked by comment, and steps that merge appear together
(`// Arrange & Act & Assert`):

```csharp
[Fact]
public void SetDeleted_ShouldRecordWhoChanged()
{
  // Arrange
  var entity = new Auditable(Guid.NewGuid());
  var author = Guid.NewGuid();

  // Act
  entity.SetDeleted(new DeletedBy(author));

  // Assert
  Assert.Equal(author, entity.UpdatedById);
}
```

### Cancellation token

Asynchronous calls use `TestContext.Current.CancellationToken`, so that cancelling the run reaches the
test (rule `xUnit1051`, treated as an error).

### Review tests

Each package review of v4.0.0 left a `{Package}ReviewTests.cs` file with the tests of the fixed
defects. They are kept apart from the original tests on purpose: they group what must not regress,
with the defect's context in the comment.

---

## 🔁 Shared Resources

Before declaring a double inside the test file, check whether it already exists in `Moq/`. The
`TestException` once existed in fifteen identical copies, one per exception test file, until it was
consolidated in `Moq/Notifications/`.

A double used by a single file can stay in it, as a private nested class — sharing only pays off from
the second use on.

---

## ⚠️ Tests Sensitive to Global State

The current culture is process state. Tests that change it need the `CultureSensitive` collection,
defined in `CultureSensitiveCollection.cs`:

```csharp
[Collection("CultureSensitive")]
public class MyTest
```

The collection has `DisableParallelization`, and runs in an isolated phase. Without it, a test that
switches the culture runs in parallel with another that reads it, and the failure shows up
intermittently — which gets worse when the two targets, `net8.0` and `net10.0`, compete for CPU on the
same machine.

The same care applies to any other shared process state that comes to be exercised.
