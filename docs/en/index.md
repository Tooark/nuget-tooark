---
_layout: landing
title: Tooark for .NET
description: Modular building blocks for .NET applications — validations, value objects, notifications, security, observability, mediator, sanitizers, cloud storage and secrets.
---

<!-- markdownlint-disable MD033 -- página inicial montada em HTML de propósito -->
<!-- Os marcadores "tooark:*" são preenchidos pelo docs/build.py: versão, alvos e contagens vêm do repositório. -->

<div class="tk-hero">
  <img class="tk-hero-logo" src="images/tooark.svg" alt="" width="132" height="132">
  <p class="tk-eyebrow"><!-- tooark:frameworks --> · v<!-- tooark:version --></p>
  <h1>Tooark for .NET</h1>
  <p class="tk-lead">Modular building blocks for .NET applications: validations, value objects, notifications,
  security, observability, mediator, sanitizers, cloud storage and secrets. Each one is a package, and all of
  them are released together.</p>
  <div class="tk-actions">
    <a class="tk-button tk-button-primary" href="packages/overview.md">Get started</a>
    <!-- tooark:api-button -->
    <a class="tk-button" href="<!-- tooark:release-notes -->">What's new in v<!-- tooark:version --></a>
  </div>
  <div class="tk-stats">
    <div class="tk-stat"><span class="tk-stat-value"><!-- tooark:package-count --></span><span class="tk-stat-label">Packages</span></div>
    <!-- tooark:downloads-stat -->
    <div class="tk-stat"><span class="tk-stat-value"><!-- tooark:framework-count --></span><span class="tk-stat-label">.NET targets</span></div>
    <div class="tk-stat"><span class="tk-stat-value"><!-- tooark:language-count --></span><span class="tk-stat-label">Message languages</span></div>
  </div>
</div>

<div class="tk-install">
  <p>The <code>Tooark</code> aggregator brings every package except the storage and secrets providers, installed
  apart for the cloud you use — or install only the packages you need:</p>

```bash
dotnet add package Tooark
dotnet add package Tooark.Storage.Aws   # storage provider: Aws or Gcp
dotnet add package Tooark.Secrets.Aws   # secrets provider: Aws, Gcp or Vault
```

  <p>Installing through the aggregator does not register everything: <code>AddTooarkService</code> leaves out
  what depends on your application — the unit of work, entity auditing, the ASP.NET Core responses, JWT Bearer
  authentication, OpenID Connect SSO, the HTTP client, storage and secrets.
  <a href="packages/tooark.md#-configuration">See how to set them up</a>.</p>
</div>

<!-- tooark:packages -->

<section class="tk-group tk-community">
<h2>Community</h2>
<div class="tk-grid">
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md"><span class="tk-card-title">🤝 Contributing</span><span class="tk-card-text">Development workflow, coding and commit conventions and the pull request checklist.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md"><span class="tk-card-title">🆘 Help</span><span class="tk-card-text">Questions, bugs and feature ideas — the right channel for each.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md"><span class="tk-card-title">🔒 Security</span><span class="tk-card-text">Supported versions, and how to report vulnerabilities privately — never through a public issue.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/releases"><span class="tk-card-title">📦 Releases</span><span class="tk-card-text">Release notes of every version, with what changed and what to do when upgrading.</span></a>
<a class="tk-card" href="https://github.com/sponsors/paulosfjunior"><span class="tk-card-title">💖 Support</span><span class="tk-card-text">GitHub Sponsors or Ko-fi — every contribution keeps the project maintained.</span></a>
</div>
</section>
