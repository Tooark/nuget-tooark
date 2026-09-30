---
_layout: landing
title: Tooark for .NET
description: Modular building blocks for .NET applications — validations, value objects, notifications, security, observability, mediator, sanitizers, cloud storage and secrets.
---

<!-- markdownlint-disable MD033 -- página inicial montada em HTML de propósito -->

<div class="tk-hero">
  <img class="tk-hero-logo" src="images/tooark.svg" alt="Tooark" width="120" height="120">
  <div>
    <p class="tk-eyebrow">.NET 8 · .NET 10 · v<!-- tooark:version --></p>
    <h1>Tooark for .NET</h1>
    <p class="tk-lead">Modular building blocks for .NET applications: validations, value objects, notifications,
    security, observability, mediator and more — each one a package, all released together.</p>
    <div class="tk-actions">
      <a class="tk-button tk-button-primary" href="packages/overview.md">Get started</a>
      <a class="tk-button" href="https://www.nuget.org/profiles/Tooark">NuGet</a>
      <a class="tk-button" href="https://github.com/Tooark/nuget-tooark">GitHub</a>
    </div>
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

  <p>Installing through the aggregator does not register everything: <code>AddTooarkService</code> leaves out the
  unit of work, OpenID Connect SSO, storage and secrets. <a href="packages/tooark.md#-configuration">See how to
  set them up</a>.</p>
</div>

<!-- tooark:packages -->

<section class="tk-group tk-community">
<h2>Community</h2>
<div class="tk-grid">
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md"><span class="tk-card-title">🤝 Contributing</span><span class="tk-card-text">Development workflow, coding and commit conventions and the pull request checklist.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md"><span class="tk-card-title">🆘 Help</span><span class="tk-card-text">Questions, bugs and feature ideas — the right channel for each.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md"><span class="tk-card-title">🔒 Security</span><span class="tk-card-text">Report vulnerabilities privately — never through a public issue.</span></a>
<a class="tk-card" href="https://github.com/sponsors/paulosfjunior"><span class="tk-card-title">💖 Support</span><span class="tk-card-text">GitHub Sponsors or Ko-fi — every contribution keeps the project maintained.</span></a>
</div>
</section>
