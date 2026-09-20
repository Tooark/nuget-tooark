# Summary

<!--
Explain what this PR does and why. Reference the issue(s) it closes.
Example: "Closes #46 — adds Tooark.Securities.OpenId with the Entra and Google SSO presets."
-->

## Affected package(s) / area(s)

- [ ] `Tooark` (aggregator)
- [ ] `Tooark.AspNetCore`
- [ ] `Tooark.Attributes`
- [ ] `Tooark.Dtos`
- [ ] `Tooark.Entities`
- [ ] `Tooark.Enums`
- [ ] `Tooark.Exceptions`
- [ ] `Tooark.Extensions` (incl. translations in `Resources/`)
- [ ] `Tooark.Mediator` / `Tooark.Mediator.Abstractions` / `Tooark.Mediator.EntityFrameworkCore`
- [ ] `Tooark.Notifications`
- [ ] `Tooark.Observability`
- [ ] `Tooark.Securities`
- [ ] `Tooark.Securities.OpenId`
- [ ] `Tooark.Utils`
- [ ] `Tooark.Validations`
- [ ] `Tooark.ValueObjects`
- [ ] `Tooark.Tests`
- [ ] Versioning / dependencies (`Directory.Build.props`, `Directory.Packages.props`, `global.json`)
- [ ] CI/CD (`.github/`)
- [ ] Docs / release notes (`README*.md`, package READMEs, `Notes/`)

## Type of change

- [ ] `feat` — new feature or new package
- [ ] `fix` — bug fix
- [ ] `docs` — documentation only
- [ ] `test` — tests only
- [ ] `refactor` — no functional change
- [ ] `build` / `ci` — build, dependency or workflow change
- [ ] `chore` — version bump, release notes, housekeeping
- [ ] Breaking change (describe the migration in "Notes for reviewers" and in the release notes)

## Checklist

- [ ] Commits follow [Conventional Commits](https://www.conventionalcommits.org/)
- [ ] `dotnet format --verify-no-changes` passes (`.editorconfig`)
- [ ] `dotnet build --configuration Release` passes with no warnings
- [ ] `dotnet test --project Tooark.Tests/Tooark.Tests.csproj` passes on `net8.0` and `net10.0`
- [ ] New behavior is covered by tests
- [ ] New message keys are translated in `en-US`, `pt-BR` and `es-ES`
- [ ] Package `README.md` and `README.pt-BR.md` updated **and in sync** (if the public surface, options or behavior changed)
- [ ] Root `README.md` and `README.pt-BR.md` updated **and in sync** (if the root docs changed)
- [ ] Links between Markdown files are absolute GitHub URLs (package READMEs are rendered on NuGet)
- [ ] Release notes updated in `Notes/` (and `Directory.Build.props` bumped when this PR closes the release)
- [ ] Runtime-bound dependency pins changed in pairs (8.0.x and 10.0.x) in `Directory.Packages.props` (`bash scripts/check-package-pairs.sh` passes)

## Security notes

<!--
Does this PR touch cryptography, tokens, authentication, validation of untrusted
input, or how sensitive data is logged/traced? Describe the threat you considered
and why the default is safe. New third-party dependencies: name, why, and license.
-->

## Notes for reviewers

<!-- Anything specific to focus on, alternatives considered, follow-up work, etc. -->
