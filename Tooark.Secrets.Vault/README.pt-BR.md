# Tooark.Secrets.Vault

Provedor do [`Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets) sobre o
**HashiCorp Vault** e o **OpenBao**: carrega os segredos do KV versão 2 no `IConfiguration` e implementa o
`ISecretService` sobre eles. O OpenBao mantém a API do Vault, então o mesmo pacote atende os dois.

O cliente é próprio, sobre a API HTTP, sem biblioteca de terceiros: cobre a leitura e a listagem do KV e a
autenticação por token, AppRole ou Kubernetes.

O modelo, as opções comuns e a conversão de nomes em chaves estão no
[README do `Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md). Este
README trata do que é próprio do Vault.

📖 **Documentação:** [Tooark.Secrets.Vault no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.secrets.vault.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Vault/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Autenticação](#-autenticação)
- [Política de acesso](#-política-de-acesso)
- [Comportamento no Vault](#-comportamento-no-vault)
- [Dependências](#-dependências)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Secrets.Vault
```

---

## ⚙️ Configuração

### appsettings.json

```json
{
  "Secrets": {
    "Address": "https://vault.empresa.com:8200",
    "Path": "arkuest/prod",
    "AuthMethod": "Kubernetes",
    "Role": "arkuest"
  }
}
```

### Program.cs

```csharp
using Tooark.Secrets.Vault.Injections;

// Segredos do KV na configuração, por cima do appsettings.json
builder.Configuration.AddTooarkSecretsVault();

// Leitura de segredos em execução, com cache
builder.Services.AddTooarkSecretsVault(builder.Configuration);
```

O `AddTooarkSecretsVault` do `IConfigurationBuilder` exige o `Path`. O do `IServiceCollection` registra o
`ISecretService`, que lê qualquer caminho do KV.

### Propriedades de `VaultSecretsOptions`

Além das [opções comuns](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md#propriedades-de-secretsoptions):

| Propriedade           | Tipo      | Padrão                            | Descrição                                                |
| --------------------- | --------- | --------------------------------- | -------------------------------------------------------- |
| `Address`             | `string?` | `VAULT_ADDR`/`BAO_ADDR`           | Endereço do cofre. Obrigatório                           |
| `Namespace`           | `string?` | `VAULT_NAMESPACE`/`BAO_NAMESPACE` | Namespace, no Vault Enterprise ou no OpenBao             |
| `Mount`               | `string`  | `secret`                          | Caminho de montagem do KV versão 2                       |
| `Path`                | `string?` | —                                 | Caminho no KV que vira configuração, lido recursivamente |
| `AuthMethod`          | `string`  | `Token`                           | `Token`, `AppRole` ou `Kubernetes`                       |
| `AuthMount`           | `string?` | `approle`/`kubernetes`            | Caminho de montagem do método de autenticação            |
| `Token`               | `string?` | `VAULT_TOKEN`/`BAO_TOKEN`         | Token, no método `Token`                                 |
| `RoleId`              | `string?` | —                                 | Role ID, no método `AppRole`                             |
| `SecretId`            | `string?` | —                                 | Secret ID, no método `AppRole`                           |
| `Role`                | `string?` | —                                 | Role do cofre, no método `Kubernetes`                    |
| `KubernetesTokenPath` | `string`  | token da conta de serviço do pod  | Arquivo do token lido no método `Kubernetes`             |

Endereço, token e namespace ausentes vêm das variáveis de ambiente do cliente de linha de comando: primeiro as
`VAULT_*`, depois as `BAO_*`. O valor informado na configuração vale por cima delas.

---

## 🔑 Autenticação

| Método       | Quando usar                        | Como funciona                                                                   |
| ------------ | ---------------------------------- | ------------------------------------------------------------------------------- |
| `Kubernetes` | Aplicação em um cluster Kubernetes | O token da conta de serviço do pod vai no login; nenhum segredo na configuração |
| `AppRole`    | Máquina, VM ou pipeline            | Role ID na configuração e Secret ID entregue pelo ambiente de implantação       |
| `Token`      | Desenvolvimento e testes           | O token vai direto nas requisições, sem login                                   |

O token de AppRole e de Kubernetes é obtido no primeiro uso. Quando o cofre o recusa, por ter expirado ou sido
revogado, o cliente faz um novo login e repete a requisição uma vez. O token do método `Token` não se renova.

---

## 🔒 Política de acesso

A política da identidade precisa ler os segredos e listar as pastas do caminho:

```hcl
path "secret/data/arkuest/prod/*" {
  capabilities = ["read"]
}

path "secret/data/arkuest/prod" {
  capabilities = ["read"]
}

path "secret/metadata/arkuest/prod/*" {
  capabilities = ["list"]
}

path "secret/metadata/arkuest/prod" {
  capabilities = ["list"]
}
```

---

## 🧭 Comportamento no Vault

- **Um campo, uma chave** — o campo `SecretKey` do segredo `arkuest/prod/Storage` vira `Storage:SecretKey`; os campos
  do segredo no próprio `Path` ficam na raiz.
- **Leitura recursiva** — o `Path` é percorrido pasta a pasta, até 10 níveis. Um caminho que é segredo e pasta ao
  mesmo tempo tem os dois lidos.
- **Campos em JSON** — o valor de um campo fica como texto, mesmo quando é JSON, salvo com `ExpandJson`. Um campo que
  não é texto (número, objeto) chega no formato JSON.
- **Versão atual** — vale a versão atual de cada segredo; um segredo excluído é ignorado.
- **`ISecretService`** — o nome é o caminho no KV, e o valor é o objeto JSON com os campos; o `GetFieldAsync` lê um
  campo.

---

## 📋 Dependências

| Pacote                                                                                                                                          | Versão   | Descrição                      |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------ |
| [`Tooark.Secrets`](https://www.nuget.org/packages/Tooark.Secrets)                                                                               | 4.x      | Modelo e fonte de configuração |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Registro no container          |

Não há cliente de terceiros: as chamadas ao cofre usam o `HttpClient` do .NET.

---

## ⚠️ Códigos de Erro e Soluções

Os erros comuns estão no
[README do `Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md#️-códigos-de-erro-e-soluções).
A falha ao carregar a fonte chega como `Secrets.LoadFailed;Vault`, com o erro do cofre na `InnerException`.

| Mensagem                                            | Descrição                                     | Solução                                            |
| --------------------------------------------------- | --------------------------------------------- | -------------------------------------------------- |
| `Options.Secrets.Vault.AddressNotConfigured`        | Sem endereço nem `VAULT_ADDR`/`BAO_ADDR`      | Configure `Secrets:Address`                        |
| `Options.Secrets.Vault.AddressInvalid;{valor}`      | Endereço não é URL `http` ou `https` absoluta | Use o endereço completo, com a porta               |
| `Options.Secrets.Vault.AuthMethodInvalid;{valor}`   | Método de autenticação desconhecido           | Use `Token`, `AppRole` ou `Kubernetes`             |
| `Options.Secrets.Vault.TokenNotConfigured`          | Método `Token` sem token nem `VAULT_TOKEN`    | Informe o token pelo ambiente                      |
| `Options.Secrets.Vault.AppRoleNotConfigured`        | Método `AppRole` sem Role ID ou Secret ID     | Informe os dois                                    |
| `Options.Secrets.Vault.KubernetesRoleNotConfigured` | Método `Kubernetes` sem `Role`                | Informe a role configurada no cofre                |
| `Secrets.Vault.LoginFailed`                         | O cofre recusou o login                       | Confira a role, o Role ID e o Secret ID            |
| `Secrets.Vault.KubernetesTokenNotFound;{caminho}`   | O token da conta de serviço não existe        | Confira `automountServiceAccountToken` e o caminho |

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
