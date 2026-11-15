---
_layout: landing
title: Tooark para .NET
description: Blocos modulares para aplicações .NET — validações, objetos de valor, notificações, segurança, observabilidade, mediator, sanitizadores, storage em nuvem e segredos.
---

<!-- markdownlint-disable MD033 -- página inicial montada em HTML de propósito -->
<!-- Os marcadores "tooark:*" são preenchidos pelo docs/build.py: versão, alvos e contagens vêm do repositório. -->

<div class="tk-hero">
  <img class="tk-hero-logo" src="images/tooark.svg" alt="" width="132" height="132">
  <p class="tk-eyebrow"><!-- tooark:frameworks --> · v<!-- tooark:version --></p>
  <h1>Tooark para .NET</h1>
  <p class="tk-lead">Blocos modulares para aplicações .NET: validações, objetos de valor, notificações, segurança,
  observabilidade, mediator, sanitizadores, storage em nuvem e segredos. Cada um é um pacote, e todos são
  lançados juntos.</p>
  <div class="tk-actions">
    <a class="tk-button tk-button-primary" href="packages/overview.md">Começar</a>
    <!-- tooark:api-button -->
    <a class="tk-button" href="<!-- tooark:release-notes -->">Novidades da v<!-- tooark:version --></a>
  </div>
  <div class="tk-stats">
    <div class="tk-stat"><span class="tk-stat-value"><!-- tooark:package-count --></span><span class="tk-stat-label">Pacotes</span></div>
    <!-- tooark:downloads-stat -->
    <div class="tk-stat"><span class="tk-stat-value"><!-- tooark:framework-count --></span><span class="tk-stat-label">Alvos .NET</span></div>
    <div class="tk-stat"><span class="tk-stat-value"><!-- tooark:language-count --></span><span class="tk-stat-label">Idiomas das mensagens</span></div>
  </div>
</div>

<div class="tk-install">
  <p>O agregador <code>Tooark</code> traz todos os pacotes, menos os provedores de storage e de segredos,
  instalados à parte conforme a nuvem que você usa — ou instale só os pacotes de que você precisa:</p>

```bash
dotnet add package Tooark
dotnet add package Tooark.Storage.Aws   # provedor de storage: Aws ou Gcp
dotnet add package Tooark.Secrets.Aws   # provedor de segredos: Aws, Gcp ou Vault
```

  <p>Instalar pelo agregador não registra tudo: o <code>AddTooarkService</code> deixa de fora o que depende da
  sua aplicação — a unidade de trabalho, as respostas do ASP.NET Core, a autenticação JWT Bearer, o SSO com
  OpenID Connect, o storage e os segredos. <a href="packages/tooark.md#-configuração">Veja como
  configurá-los</a>.</p>
</div>

<!-- tooark:packages -->

<section class="tk-group tk-community">
<h2>Comunidade</h2>
<div class="tk-grid">
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md"><span class="tk-card-title">🤝 Contribuindo</span><span class="tk-card-text">Fluxo de desenvolvimento, convenções de código e de commit e o checklist do pull request.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md"><span class="tk-card-title">🆘 Ajuda</span><span class="tk-card-text">Dúvidas, bugs e ideias — o canal certo para cada um.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md"><span class="tk-card-title">🔒 Segurança</span><span class="tk-card-text">Versões com suporte, e como reportar vulnerabilidades de forma privada — nunca por issue pública.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/releases"><span class="tk-card-title">📦 Versões</span><span class="tk-card-text">Notas de cada versão, com o que mudou e o que fazer ao atualizar.</span></a>
<a class="tk-card" href="https://github.com/sponsors/paulosfjunior"><span class="tk-card-title">💖 Apoie</span><span class="tk-card-text">GitHub Sponsors ou Ko-fi — cada contribuição mantém o projeto ativo.</span></a>
</div>
</section>
