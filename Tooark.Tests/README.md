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

The suite runs on the [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
(MTP), the native runner of xunit.v3 4.x: `UseMicrosoftTestingPlatformRunner` is on in the project and
`global.json` sets `test.runner` to `Microsoft.Testing.Platform`, which puts `dotnet test` in MTP mode. In
that mode the project is passed with `--project`, and everything after `--` goes to the test runner — the
old VSTest `--filter "FullyQualifiedName~..."` syntax no longer applies.

```bash
# Every target
dotnet test --project Tooark.Tests/Tooark.Tests.csproj

# A single target, during development
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0

# A single package (namespace)
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0 -- --filter-namespace Tooark.Tests.ValueObjects

# A single class (the wildcard stands for the namespace prefix)
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0 -- --filter-class "*CpfTests"

# A single test
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0 -- --filter-method "*Equals_ShouldBeFalse_WhenTypesDiffer"

# TRX report (written to the results directory)
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -- --report-xunit-trx
```

Several values of the same filter can be given in one switch (`--filter-class "*CpfTests" "*CnpjTests"`),
and each filter has a `--filter-not-*` counterpart. The full list is in `dotnet test --project Tooark.Tests/Tooark.Tests.csproj -- --help`.

---

## 📊 Coverage

### Collecting

Coverage is collected by [coverlet](https://github.com/coverlet-coverage/coverlet) through its MTP
extension, `coverlet.MTP` (the classic `coverlet.msbuild` only works under VSTest). Add `--coverlet` to
any run:

```bash
dotnet test --project Tooark.Tests/Tooark.Tests.csproj -f net10.0 -- \
  --coverlet --coverlet-output-format cobertura
```

`--coverlet-output-format` accepts `json`, `lcov`, `opencover`, `cobertura` and `teamcity`, and can be
repeated. The JSON carries the detail needed to find out **which** path is missing — writing a test from
the measured gap pays more than writing it from a guess.

Two coverlet.MTP details that tend to confuse:

- the report goes to the **results directory**: `TestResults/` under the directory the command runs in,
  or wherever `--results-directory` points. Use an absolute path when the destination matters;
- a run never overwrites an existing report: when one is already there, the new file gets a timestamp
  in its name (`coverage.cobertura.<timestamp>.xml`). Running both targets into the same directory
  therefore leaves two files — glob them (`coverage.cobertura*.xml`) and let ReportGenerator merge them.

### Summary and HTML report

coverlet.MTP prints no summary in the console. `ReportGenerator` is already a dependency of the project;
point it at the Cobertura files and ask for the text summary and the HTML at once:

```bash
reportgenerator \
  -reports:"TestResults/coverage.cobertura*.xml" \
  -targetdir:TestResults/coveragereport \
  -reporttypes:"Html;TextSummary"
```

`TestResults/coveragereport/Summary.txt` has the totals and one block per assembly — enough for
day-to-day work. `index.html` gives every type a page with the source code marking covered lines,
uncovered lines and partially covered branches — it is the fastest way to see the missing path.

The `reportgenerator` command comes from the global tool:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Without installing it, call the executable that already came with the package, at
`~/.nuget/packages/reportgenerator/<version>/tools/net10.0/ReportGenerator.exe`. In VS Code, the
`generate coverage report` task (`.vscode/tasks.json`) runs the collection and the report in sequence.

CI runs the same collection on every pull request, uploads the Cobertura files and the report as the
`coverage` artifact and writes the summary on the job page.

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
