# Security Policy

## Reporting a vulnerability

The Tooark maintainers take security seriously — these packages sit inside the
authentication, cryptography, validation and observability paths of production
.NET applications. If you believe you have found a security vulnerability in any
Tooark package, its build, or the CI/CD workflows of this repository, please
report it **privately** so we can address it before public disclosure.

### How to report

**Do NOT** open a public GitHub issue for security vulnerabilities.

Instead, use one of the following channels:

1. **Preferred** — GitHub Security Advisories:
   [Report a vulnerability](https://github.com/Tooark/nuget-tooark/security/advisories/new)
2. **Email** — `security@tooark.com` (PGP key available on request)

Please include:

- A description of the vulnerability and its impact
- Steps to reproduce (proof of concept if possible)
- The affected package(s) and version(s) (e.g. `Tooark.Securities 4.1.0`)
- The target framework and runtime (e.g. `net10.0` on Linux)
- Your name / handle for credit (optional)

### What to expect

| Milestone                            | Target time                                             |
| ------------------------------------ | ------------------------------------------------------- |
| Acknowledgment of report             | Within **72 hours**                                     |
| Initial triage & severity assessment | Within **5 business days**                              |
| Fix and coordinated disclosure plan  | Within **30 days** (may be extended for complex issues) |
| Public advisory (if applicable)      | After a fixed release is published on NuGet             |

We follow the principles of
[Coordinated Vulnerability Disclosure (CVD)](https://en.wikipedia.org/wiki/Coordinated_vulnerability_disclosure).

## Supported versions

All packages in this repository share one version number and are released
together. Security fixes are published as a new patch of the **current major
line** on NuGet.

| Version | Supported                                               |
| ------- | ------------------------------------------------------- |
| 4.x     | ✅ Active — features, bug fixes and security fixes      |
| 3.x     | ⚠️ Security fixes only, best effort, on the `v3` branch |
| ≤ 2.x   | ❌ End of life — please upgrade                         |

Packages target `net8.0` and `net10.0`. When a .NET runtime reaches end of
support by Microsoft, the corresponding target framework stops receiving
security fixes at the next major release.

Always update to the latest version before reporting a bug or vulnerability.

## Dependency scanning

Every release build runs the Tooark
[`security-scanner`](https://github.com/Tooark/base-images) image (Trivy for
dependencies and secrets) as a gate before anything is published; `HIGH` and
`CRITICAL` findings block the release. Dependabot keeps NuGet and GitHub Actions
dependencies up to date on a weekly schedule. Runtime-bound packages
(`Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore`, …) are pinned per
target framework in [`Directory.Packages.props`](Directory.Packages.props); a
CVE in one of them is fixed by bumping that pin.

## Scope

In scope:

- Vulnerabilities in the code of any Tooark package published from this
  repository (cryptography, JWT and OpenID Connect configuration, input
  validation, value objects, notifications, mediator, observability, …)
- Unsafe defaults — for example, a configuration that silently falls back to a
  weaker algorithm or disables a validation
- Sensitive-data leaks through logging, tracing or error messages
- Vulnerabilities in the CI/CD workflows and scripts of this repository (e.g.
  secret exposure, tag/release spoofing, supply-chain issues in the build)

Out of scope:

- CVEs in upstream dependencies (Microsoft.IdentityModel, OpenTelemetry, EF
  Core, …) — report those upstream; here they are handled via version bumps
- Vulnerabilities in the .NET runtime, the operating system or the CI platform
  itself
- Misconfiguration in consuming applications that the package documentation
  explicitly warns against (e.g. committing secrets to `appsettings.json`)
- Social engineering, physical attacks, and denial of service

## Safe harbor

We support security research conducted in good faith. If you follow this policy,
we will:

- Not pursue legal action against you
- Work with you to understand and resolve the issue
- Publicly credit you (if you wish) in the security advisory

## Bounties

Tooark is an open-source project maintained by volunteers. **No monetary bounty
program is currently offered**, but we deeply appreciate responsible disclosure
and will credit reporters publicly.
