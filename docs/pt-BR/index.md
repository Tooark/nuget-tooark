---
_layout: landing
title: Tooark para .NET
description: Blocos modulares para aplicações .NET — validações, objetos de valor, notificações, segurança, observabilidade, mediator, sanitizadores, storage em nuvem e segredos.
---

<!-- markdownlint-disable MD033 -- página inicial montada em HTML de propósito -->

<div class="tk-hero">
  <img class="tk-hero-logo" src="images/tooark.svg" alt="Tooark" width="120" height="120">
  <div>
    <p class="tk-eyebrow">.NET 8 · .NET 10 · v<!-- tooark:version --></p>
    <h1>Tooark para .NET</h1>
    <p class="tk-lead">Blocos modulares para aplicações .NET: validações, objetos de valor, notificações, segurança,
    observabilidade, mediator e mais — cada um em um pacote, todos lançados juntos.</p>
    <div class="tk-actions">
      <a class="tk-button tk-button-primary" href="packages/overview.md">Começar</a>
      <a class="tk-button" href="https://www.nuget.org/profiles/Tooark">NuGet</a>
      <a class="tk-button" href="https://github.com/Tooark/nuget-tooark">GitHub</a>
    </div>
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

  <p>Instalar pelo agregador não registra tudo: o <code>AddTooarkService</code> deixa de fora a unidade de
  trabalho, o SSO com OpenID Connect, o storage e os segredos. <a href="packages/tooark.md#-configuração">Veja
  como configurá-los</a>.</p>
</div>

<!-- tooark:packages -->

<section class="tk-group tk-community">
<h2>Comunidade</h2>
<div class="tk-grid">
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md"><span class="tk-card-title">🤝 Contribuindo</span><span class="tk-card-text">Fluxo de desenvolvimento, convenções de código e de commit e o checklist de pull request.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md"><span class="tk-card-title">🆘 Ajuda</span><span class="tk-card-text">Dúvidas, bugs e ideias — o canal certo para cada um.</span></a>
<a class="tk-card" href="https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md"><span class="tk-card-title">🔒 Segurança</span><span class="tk-card-text">Reporte vulnerabilidades de forma privada — nunca por issue pública.</span></a>
<a class="tk-card" href="https://github.com/sponsors/paulosfjunior"><span class="tk-card-title">💖 Apoie</span><span class="tk-card-text">GitHub Sponsors ou Ko-fi — cada contribuição mantém o projeto ativo.</span></a>
</div>
</section>
