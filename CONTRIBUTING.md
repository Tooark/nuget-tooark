# Contributing to Tooark

First off, thank you for considering contributing to **Tooark**! 🎉

This repository holds the Tooark family of NuGet packages for .NET — modular
building blocks for validations, value objects, notifications, security,
observability, mediator and more. This document explains how to propose
changes, report bugs, and submit code.

## Table of contents

- [Ways to contribute](#ways-to-contribute)
- [Repository layout](#repository-layout)
- [Development workflow](#development-workflow)
- [Coding conventions](#coding-conventions)
- [Versioning and dependencies](#versioning-and-dependencies)
- [Commit convention](#commit-convention)
- [Documentation standards](#documentation-standards)
- [Releasing](#releasing)
- [Pull Request checklist](#pull-request-checklist)
- [Community](#community)

---

## Ways to contribute

- 🐛 **Report bugs** — open an issue with the `bug` template.
- ✨ **Suggest improvements or new packages** — open an issue with the
  `feature` template. Explain the problem before the solution.
- 📖 **Improve documentation** — every package and the repository root ship a
  `README.md` in English and a `README.pt-BR.md` in Portuguese.
- 🔒 **Review security** — question a default, a validation that can be
  bypassed, a message that leaks data. Vulnerabilities go through the private
  channel in [`SECURITY.md`](SECURITY.md), never a public issue.
- 💻 **Write code** — new features, fixes, tests, translations.

---

## Repository layout

Each package lives in its own folder and follows the same structure:

| Path                               | Purpose                                                                |
| ---------------------------------- | ---------------------------------------------------------------------- |
| `Tooark.<Package>/`                | One project per package (`Tooark.Validations`, `Tooark.Securities`, …) |
| `Tooark.<Package>/README.md`       | Package docs in English, packed into the NuGet package                 |
| `Tooark.<Package>/README.pt-BR.md` | Package docs in Portuguese, linked from the English one                |
| `Tooark/`                          | Aggregator package that references every other package                 |
| `Tooark.Tests/`                    | Single test project covering every package, one folder each            |
| `Tooark.Benchmarks/`               | BenchmarkDotNet project (not part of the solution)                     |
| `Notes/vX.Y.Z.md`                  | Release notes, one file per version (used as the GitHub Release body)  |
| `Media/`                           | Package icon and logo                                                  |
| `scripts/`                         | Local SonarQube examples                                               |

Shared, repo-wide files:

- [`Directory.Build.props`](Directory.Build.props) — the **single version
  number** of all packages, plus build settings (`TreatWarningsAsErrors`,
  nullable, XML docs, SourceLink)
- [`Directory.Packages.props`](Directory.Packages.props) — central package
  management; runtime-bound packages are pinned per target framework
- [`Tooark.slnx`](Tooark.slnx) — the solution
- [`global.json`](global.json) — the .NET SDK version
- [`.github/workflows/`](.github/workflows/) — CI on pull requests and the
  release pipeline on `main`

---

## Development workflow

**Prerequisites:** the .NET SDK pinned in [`global.json`](global.json) (10.x;
it builds and tests both `net8.0` and `net10.0`), Git, and optionally Docker
to run the security scanner locally.

1. **Fork** the repository and clone your fork.
2. Create a branch named after the issue: `git checkout -b 46-feat/short-description`
   (issue number, type, short slug — see the history for examples).
3. Make your changes.
4. **Build and test locally** before pushing — warnings are errors:

   ```bash
   dotnet build --configuration Release
   dotnet test Tooark.Tests/Tooark.Tests.csproj

   # One target and one package, during development
   dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 --filter "FullyQualifiedName~Tooark.Tests.ValueObjects"

   # Coverage table in the console
   dotnet test Tooark.Tests/Tooark.Tests.csproj -f net10.0 -p:CollectCoverage=true
   ```

   [`Tooark.Tests/README.md`](Tooark.Tests/README.md) explains the test
   organization, coverage reports and the state-sensitive tests.

5. Every new behavior comes with tests, in the package's folder under
   `Tooark.Tests/`. Every new message key emitted by a package gets a
   translation in the three resource files of `Tooark.Extensions`
   (`en-US`, `pt-BR`, `es-ES`) — a test enforces that the keys match.
6. Update the package `README.md` **and** `README.pt-BR.md` if the public
   surface, options or behavior changed, and add a line to the release notes
   in `Notes/` (see [Releasing](#releasing)).
7. Push and open a Pull Request against `main`.

> Note: CI builds and runs the full test suite on both target frameworks for
> every pull request. The release pipeline additionally runs the security
> scanner before publishing. A failing build, test or scan blocks the merge.

---

## Coding conventions

- **Language.** Code comments and XML documentation are written in
  **Brazilian Portuguese**, the project's working language. Identifiers are in
  English. READMEs are bilingual (see [Documentation standards](#documentation-standards)).
  The [`.editorconfig`](.editorconfig) carries the formatting rules (2-space
  indentation, LF, file-scoped namespaces).
- **Every public member has XML docs** (`GenerateDocumentationFile` is on and
  warnings are errors).
- **Nullable is enabled** across the solution — no `!` to silence a real
  possibility of null.
- **Fail fast, never silently.** A security-sensitive option with an invalid
  value throws at startup; it never falls back to another algorithm, key or
  default. Look at `JwtOptions.Algorithm` or `OpenIdOptions.Validate` for the
  pattern.
- **Messages are keys, not text.** Packages throw and notify with keys such as
  `Options.Jwt.SecretTooShort;32`; the text lives in the resource files of
  `Tooark.Extensions`.
- **Dependency injection** follows the `TooarkDependencyInjection` partial
  class pattern: `AddTooark<Thing>(IConfiguration, Action<Options>?)`,
  options bound from a named section with an `Options.Section` constant.
- **ASP.NET Core stays in the packages that need it.** Anything that requires
  the `Microsoft.AspNetCore.App` shared framework goes into a package that
  declares it (`Tooark.AspNetCore`, `Tooark.Observability`,
  `Tooark.Securities.OpenId`); the others must keep running on the base
  runtime.
- **Tests** use xUnit with the Arrange/Act/Assert layout and a one-line
  Portuguese comment describing each test.

---

## Versioning and dependencies

- All packages share the version in `Directory.Build.props` and are released
  together. **SemVer**: breaking changes bump the major, new features the
  minor, fixes the patch. A new public constant is a minor; a change of
  behavior that consumers may rely on is a major, even when it looks like a
  fix — document it in the release notes under "Mudanças Incompatíveis".
- Package versions are managed **centrally** in `Directory.Packages.props`.
  Runtime-bound packages (`Microsoft.AspNetCore.*`, `Microsoft.Extensions.*`,
  `Microsoft.EntityFrameworkCore*`) have one pin per target framework and
  must be bumped in pairs (8.0.x and 10.0.x). Do not put a `Version` on a
  `PackageReference`.
- Prefer the official Tooark packages and the .NET framework over new
  third-party dependencies. Adding one needs a justification in the PR; a
  dependency that drags the ASP.NET Core shared framework into a base
  package will be rejected.

---

## Commit convention

We use [**Conventional Commits**](https://www.conventionalcommits.org/).

Format:

```text
<type>(<scope>): <short summary>
```

Common types: `feat`, `fix`, `docs`, `test`, `refactor`, `build`, `ci`, `chore`.
The scope is the package or area when it applies; the summary is written in
Portuguese, matching the history:

```text
feat(securities): adiciona o preset de SSO com Google
fix(validations): corrige a mensagem padrão de IsLinkVideo
docs: documenta as novas constantes públicas nos READMEs dos pacotes
chore(release): define a data de lançamento da v4.1.0
```

---

## Documentation standards

- **Every README is bilingual.** `README.md` is the English default — it is
  the file NuGet renders on the package page — and `README.pt-BR.md` is the
  Portuguese variant, at the repository root and in every package folder.
  **Keep both in sync** — a change in one requires the same change in the
  other.
- **Language selector at the top**, right after the intro paragraph. Package
  READMEs use emoji flags (`🇺🇸`/`🇧🇷`) because NuGet does not render images
  from arbitrary domains; the root README follows the family style with flag
  images.
- **Links between Markdown files are absolute GitHub URLs**
  (`https://github.com/Tooark/nuget-tooark/blob/main/...`), never relative:
  a package README is published on NuGet, where a relative link has nothing to
  point to. Switching the language from the NuGet page opens the file on
  GitHub. Anchors within the same file (`#section`) stay relative.
- Each package README has the same sections: contents, installation,
  configuration (options table), examples, dependencies, best practices,
  error codes, contribution and license. Copy the structure of a recent
  package (`Tooark.Securities.OpenId`, `Tooark.Observability`) when creating
  a new one.
- Release notes live in `Notes/vX.Y.Z.md`, in Portuguese, and follow the
  existing layout (summary, breaking changes, added, changed, fixed, impact,
  additional information). The file becomes the body of the GitHub Release.

---

## Releasing

Releases are fully automated by
[`.github/workflows/tooark.yml`](.github/workflows/tooark.yml):

1. Bump `<Version>` in `Directory.Build.props` and write `Notes/v<version>.md`
   in the same PR as the last change of the release.
2. Merge to `main`. If the tag `v<version>` does not exist yet, the workflow
   builds, runs the tests, runs the security scanner, packs every package,
   creates the tag and the GitHub Release (with the notes file as body) and
   publishes every package to NuGet. If the tag already exists, the workflow
   stops with a warning and publishes nothing.

Do **not** hand-create tags or releases — the workflow derives them from
`Directory.Build.props`.

---

## Pull Request checklist

Before opening a PR, confirm:

- [ ] Commits follow Conventional Commits
- [ ] `dotnet build --configuration Release` passes with no warnings
- [ ] `dotnet test Tooark.Tests/Tooark.Tests.csproj` passes on both target frameworks
- [ ] New behavior is covered by tests
- [ ] New message keys are translated in `en-US`, `pt-BR` and `es-ES`
- [ ] Package `README.md` and `README.pt-BR.md` updated **and in sync** when the public surface, options or behavior changed
- [ ] Root `README.md` and `README.pt-BR.md` updated **and in sync** when the root docs changed
- [ ] Links between Markdown files are absolute GitHub URLs
- [ ] Release notes updated in `Notes/` (and `Directory.Build.props` bumped when this PR closes the release)
- [ ] Breaking changes are called out in the release notes with a migration note
- [ ] Linked to at least one issue (`Closes #123`) when applicable

---

## Community

- 🐛 [Issues](https://github.com/Tooark/nuget-tooark/issues)
- 📦 [Packages on NuGet](https://www.nuget.org/profiles/Tooark)
- 🌐 [Tooark](https://tooark.com)

Thank you for making Tooark better! 💙
