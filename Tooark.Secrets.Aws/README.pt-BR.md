# Tooark.Secrets.Aws

Provedor do [`Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/tree/main/Tooark.Secrets) sobre a AWS: carrega
os segredos do **Secrets Manager** e os parâmetros do **Parameter Store** no `IConfiguration`, e implementa o
`ISecretService` sobre o Secrets Manager.

O modelo, as opções comuns e a conversão de nomes em chaves estão no
[README do `Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md). Este
README trata do que é próprio da AWS.

📖 **Documentação:** [Tooark.Secrets.Aws no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.secrets.aws.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets.Aws/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Permissões](#-permissões)
- [Comportamento na AWS](#-comportamento-na-aws)
- [Dependências](#-dependências)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Secrets.Aws
```

---

## ⚙️ Configuração

### appsettings.json

```json
{
  "Secrets": {
    "Region": "sa-east-1",
    "SecretsPrefix": "arkuest/prod",
    "ParametersPath": "/arkuest/prod"
  }
}
```

### Program.cs

```csharp
using Tooark.Secrets.Aws.Injections;

// Segredos e parâmetros na configuração, por cima do appsettings.json
builder.Configuration.AddTooarkSecretsAws();

// Leitura de segredos em execução, com cache
builder.Services.AddTooarkSecretsAws(builder.Configuration);
```

O `AddTooarkSecretsAws` do `IConfigurationBuilder` exige `SecretsPrefix`, `ParametersPath` ou os dois. O do
`IServiceCollection` registra o `ISecretService` e o cliente do Secrets Manager como `IAmazonSecretsManager`; um
cliente já registrado pela aplicação é mantido.

### Propriedades de `AwsSecretsOptions`

Além das [opções comuns](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md#propriedades-de-secretsoptions):

| Propriedade      | Tipo      | Padrão | Descrição                                                                                   |
| ---------------- | --------- | ------ | ------------------------------------------------------------------------------------------- |
| `Region`         | `string?` | —      | Região do cofre, como `sa-east-1`. Sem ela, vale a região da cadeia padrão da AWS           |
| `ServiceUrl`     | `string?` | —      | Endereço de um serviço compatível, como o LocalStack                                        |
| `SecretsPrefix`  | `string?` | —      | Prefixo do nome dos segredos do Secrets Manager que viram configuração, como `arkuest/prod` |
| `ParametersPath` | `string?` | —      | Caminho dos parâmetros do Parameter Store que viram configuração, como `/arkuest/prod`      |

As credenciais vêm sempre da **cadeia padrão da AWS**: variáveis de ambiente, perfil do `~/.aws/credentials`, role da
tarefa no ECS, do pod no EKS ou da instância no EC2. Não há chave de acesso nas opções: a credencial do cofre não pode
vir do cofre.

---

## 🔑 Permissões

A identidade da aplicação precisa de:

| Permissão                       | Recurso                                    | Para                                        |
| ------------------------------- | ------------------------------------------ | ------------------------------------------- |
| `secretsmanager:ListSecrets`    | `*` (a ação não aceita recurso específico) | Listar os segredos do prefixo               |
| `secretsmanager:GetSecretValue` | ARN dos segredos do prefixo                | Ler os segredos e o `ISecretService`        |
| `ssm:GetParametersByPath`       | ARN do caminho dos parâmetros              | Listar e ler os parâmetros                  |
| `kms:Decrypt`                   | Chave KMS própria, quando usada            | Segredos e `SecureString` com chave própria |

---

## 🟧 Comportamento na AWS

- **Secrets Manager** — o prefixo filtra a listagem na AWS e depois é conferido com o limite de um nível. Um segredo
  agendado para exclusão não aparece, e um excluído entre a listagem e a leitura é ignorado. Segredo binário é lido
  como texto UTF-8.
- **Segredo "chave e valor"** — o Secrets Manager guarda esse tipo como JSON. Com `ExpandJson`, cada par vira uma
  chave; sem ele, a chave recebe o JSON inteiro.
- **Parameter Store** — o caminho é lido recursivamente, com `SecureString` descriptografado. Um `StringList` chega
  como o texto separado por vírgulas.
- **Nome igual** — com o mesmo nome num segredo e num parâmetro, o segredo vence.
- **`ISecretService`** — o nome é o do segredo ou o ARN dele; segredo inexistente devolve nulo.

---

## 📋 Dependências

| Pacote                                                                                                                                          | Versão   | Descrição                      |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------------------ |
| [`Tooark.Secrets`](https://www.nuget.org/packages/Tooark.Secrets)                                                                               | 4.x      | Modelo e fonte de configuração |
| [`AWSSDK.SecretsManager`](https://www.nuget.org/packages/AWSSDK.SecretsManager)                                                                 | 4.x      | Cliente do Secrets Manager     |
| [`AWSSDK.SimpleSystemsManagement`](https://www.nuget.org/packages/AWSSDK.SimpleSystemsManagement)                                               | 4.x      | Cliente do Parameter Store     |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Registro no container          |

---

## ⚠️ Códigos de Erro e Soluções

Os erros comuns estão no
[README do `Tooark.Secrets`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Secrets/README.pt-BR.md#️-códigos-de-erro-e-soluções).
A falha ao carregar a fonte chega como `Secrets.LoadFailed;AWS`, com o erro da AWS na `InnerException`.

| Mensagem                                        | Descrição                                             | Solução                                               |
| ----------------------------------------------- | ----------------------------------------------------- | ----------------------------------------------------- |
| `Options.Secrets.Aws.ServiceUrlInvalid;{valor}` | `ServiceUrl` não é uma URL `http` ou `https` absoluta | Use o endereço completo, como `http://localhost:4566` |

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
