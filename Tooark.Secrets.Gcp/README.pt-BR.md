# Tooark.Secrets.Gcp

Provedor do [`Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets) sobre o Google Cloud:
carrega os segredos do **Secret Manager** e os parâmetros do **Parameter Manager** no `IConfiguration`, e implementa o
`ISecretService` sobre o Secret Manager.

O modelo, as opções comuns e a conversão de nomes em chaves estão no
[README do `Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md). Este
README trata do que é próprio do Google Cloud.

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Gcp/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Permissões](#-permissões)
- [Comportamento no Google Cloud](#-comportamento-no-google-cloud)
- [Dependências](#-dependências)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Secrets.Gcp
```

---

## ⚙️ Configuração

### appsettings.json

```json
{
  "Secrets": {
    "ProjectId": "arkuest",
    "SecretsPrefix": "arkuest-prod",
    "ParametersPrefix": "arkuest-prod"
  }
}
```

### Program.cs

```csharp
using Tooark.Secrets.Gcp.Injections;

// Segredos e parâmetros na configuração, por cima do appsettings.json
builder.Configuration.AddTooarkSecretsGcp();

// Leitura de segredos em execução, com cache
builder.Services.AddTooarkSecretsGcp(builder.Configuration);
```

O `AddTooarkSecretsGcp` do `IConfigurationBuilder` exige `SecretsPrefix`, `ParametersPrefix` ou os dois. O do
`IServiceCollection` registra o `ISecretService`; o cliente do Secret Manager é criado no primeiro uso, e um
`SecretManagerServiceClient` já registrado pela aplicação é usado no lugar dele.

### Propriedades de `GcpSecretsOptions`

Além das [opções comuns](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md#propriedades-de-secretsoptions):

| Propriedade        | Tipo      | Padrão | Descrição                                                                                   |
| ------------------ | --------- | ------ | ------------------------------------------------------------------------------------------- |
| `ProjectId`        | `string?` | —      | Projeto do Google Cloud onde ficam os segredos e os parâmetros. Obrigatório                 |
| `SecretsPrefix`    | `string?` | —      | Prefixo do nome dos segredos do Secret Manager que viram configuração                       |
| `ParametersPrefix` | `string?` | —      | Prefixo do nome dos parâmetros do Parameter Manager (local `global`) que viram configuração |

A credencial vem sempre do **Application Default Credentials**: a conta de serviço do Cloud Run, do GKE ou do Compute
Engine, a variável `GOOGLE_APPLICATION_CREDENTIALS` ou o `gcloud auth application-default login` em desenvolvimento.
Não há chave nas opções: a credencial do cofre não pode vir do cofre.

### Nomes com sublinhado duplo

O Google aceita só letras, dígitos, `_` e `-` no nome, então os níveis usam `__`:

| Segredo ou parâmetro                        | Chave                       |
| ------------------------------------------- | --------------------------- |
| `arkuest-prod__Jwt__Secret`                 | `Jwt:Secret`                |
| `arkuest-prod__Storage__CredentialsJson`    | `Storage:CredentialsJson`   |
| `arkuest-prod__OpenId__Entra__ClientSecret` | `OpenId:Entra:ClientSecret` |
| `arkuest-prod`, com `ExpandJson`            | as chaves do documento JSON |

---

## 🔑 Permissões

A identidade da aplicação precisa de:

| Papel ou permissão                                | Para                                        |
| ------------------------------------------------- | ------------------------------------------- |
| `roles/secretmanager.viewer` no projeto           | Listar os segredos do prefixo               |
| `roles/secretmanager.secretAccessor` nos segredos | Ler os segredos e o `ISecretService`        |
| Listar parâmetros, listar e renderizar versões    | Carregar os parâmetros do Parameter Manager |

Um parâmetro que referencia segredos do Secret Manager é renderizado com a identidade do próprio parâmetro, que
precisa ler esses segredos.

---

## 🟦 Comportamento no Google Cloud

- **Secret Manager** — vale a versão `latest` de cada segredo. Um segredo sem versão habilitada é ignorado.
- **Parameter Manager** — vale a versão habilitada criada por último, porque o Parameter Manager não tem o apelido
  `latest`. A versão é **renderizada**: as referências a segredos do Secret Manager chegam resolvidas. Só o local
  `global` é lido.
- **Parâmetro em JSON** — com `ExpandJson`, um parâmetro no formato JSON vira várias chaves. Os parâmetros em YAML
  chegam como texto.
- **Nome igual** — com o mesmo nome num segredo e num parâmetro, o segredo vence.
- **`ISecretService`** — o nome é o identificador do segredo no projeto das opções; segredo inexistente devolve nulo.

---

## 📋 Dependências

| Pacote                                                                                                                                          | Versão   | Descrição                      |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------ |
| [`Tooark.Secrets`](https://www.nuget.org/packages/Tooark.Secrets)                                                                               | 4.x      | Modelo e fonte de configuração |
| [`Google.Cloud.SecretManager.V1`](https://www.nuget.org/packages/Google.Cloud.SecretManager.V1)                                                 | 2.x      | Cliente do Secret Manager      |
| [`Google.Cloud.ParameterManager.V1`](https://www.nuget.org/packages/Google.Cloud.ParameterManager.V1)                                           | 1.x      | Cliente do Parameter Manager   |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Registro no container          |

---

## ⚠️ Códigos de Erro e Soluções

Os erros comuns estão no
[README do `Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md#️-códigos-de-erro-e-soluções).
A falha ao carregar a fonte chega como `Secrets.LoadFailed;Google Cloud`, com o erro do Google na `InnerException`.

| Mensagem                                     | Descrição                                       | Solução                                                |
| -------------------------------------------- | ----------------------------------------------- | ------------------------------------------------------ |
| `Options.Secrets.Gcp.ProjectIdNotConfigured` | `ProjectId` ausente                             | Configure `Secrets:ProjectId`                          |
| `Options.Secrets.Gcp.CredentialsUnavailable` | Sem Application Default Credentials no ambiente | Rode com uma conta de serviço ou configure o ADC local |

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Comece pelo
[CONTRIBUTING.md](https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md) — ele cobre o fluxo de
desenvolvimento, as convenções de código e de commit e o checklist de pull request. Bugs e pedidos de
funcionalidade entram pelos [templates de issue](https://github.com/Tooark/nuget-tooark/issues/new/choose) do
repositório [Tooark](https://github.com/Tooark/nuget-tooark).

Ao participar, você concorda com o
[Código de Conduta](https://github.com/Tooark/nuget-tooark/blob/main/CODE_OF_CONDUCT.md).

---

## 🆘 Ajuda & Segurança

- ❓ **Dúvidas, bugs e ideias** — veja o
  [SUPPORT.md](https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md) para escolher o canal certo
- 🔒 **Vulnerabilidades de segurança** — **não** abra issue pública; siga o
  [SECURITY.md](https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md)

---

## 💖 Apoie

Se o Tooark ajuda nos seus projetos, considere apoiar o desenvolvimento:

- 💙 [GitHub Sponsors](https://github.com/sponsors/paulosfjunior)
- ☕ [Ko-fi](https://ko-fi.com/paulosfjunior)

Cada contribuição ajuda a manter o projeto ativo e em evolução. Obrigado! 🙏

---

## 📄 Licença

Este projeto está licenciado sob a [Licença BSD 3-Clause](https://github.com/Tooark/nuget-tooark/blob/main/LICENSE).
