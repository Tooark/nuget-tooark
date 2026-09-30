# Tooark.Notifications

Biblioteca para criação e gerenciamento de notificações e alertas, facilitando a comunicação e monitoramento para projetos .NET.

📖 **Documentação:** [Tooark.Notifications no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.notifications.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Notifications/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Instalação](#-instalação)
- [Classes](#-classes)
  - [NotificationItem](#1-item-de-notificação)
  - [Notification](#2-notificação)
  - [NotificationErrorMessages](#3-mensagens-de-erro)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Notifications
```

O pacote não tem configuração. `Notification` é usada por herança, em classes que acumulam o resultado
de várias verificações — como os objetos de valor e as entidades do Tooark —, e os mutadores são
protegidos: quem acumula a notificação é o próprio objeto, não quem o consome.

As mensagens são guardadas como **chave**, e não como texto final. Quem traduz é a camada que monta a
resposta, como o `ResponseDto` do
[`Tooark.Dtos`](https://www.nuget.org/packages/Tooark.Dtos).

---

## 🧱 Classes

As classes disponíveis são:

### 1. Item de Notificação

**Funcionalidade:**
Representa a estrutura de um item de notificação com mensagem, chave e código de erro. Os valores são normalizados na construção e a instância é somente leitura.

**Propriedades:**

- `Message`: A mensagem da notificação.
- `Key`: A chave da notificação.
- `Code`: O código de erro da notificação.

**Métodos:**

- `NotificationItem(string? message)`: Cria uma nova instância com a chave `Unknown` e o código `T.ERR`.
- `NotificationItem(string? message, string? key)`: Cria uma nova instância com a chave informada e o código `T.ERR`.
- `NotificationItem(string? message, string? key, string? code)`: Cria uma nova instância com a chave e o código informados.

Os três parâmetros aceitam ausência de valor porque o construtor já a tratava: mensagem em branco vira
`Notifications.MessageUnknown`, chave em branco vira `Unknown` e código em branco vira `T.ERR`. A
assinatura declarava não anulável e o corpo verificava nulo — quem passava um `string?` recebia aviso do
compilador para um caminho que sempre funcionou.

- `ToString()`: Retorna a mensagem da notificação.
- `string`: Converte implicitamente a instância de `NotificationItem` para uma string, retornando a mensagem. Instância nula retorna string vazia.
- `NotificationItem`: Converte implicitamente uma string para uma instância de `NotificationItem`, com a chave `Unknown` e o código `T.ERR`.

**Normalização dos valores:**

| Parâmetro | Nulo, vazio ou em branco         | Demais valores                                                              |
| --------- | -------------------------------- | --------------------------------------------------------------------------- |
| `message` | `Notifications.MessageNullEmpty` | Espaços das extremidades removidos                                          |
| `key`     | `Unknown`                        | Todos os espaços em branco removidos, incluindo tabulação e quebra de linha |
| `code`    | `T.ERR`                          | Espaços das extremidades removidos                                          |

[**Exemplo de Uso**](#item-de-notificação)

### 2. Notificação

**Funcionalidade:**
Classe abstrata que gerencia a lista de notificações do objeto que a herda. É a base de `ValueObject`, `BaseEntity` e `Validation`.

A criação e a limpeza de notificações são protegidas: apenas o próprio objeto decide o que notifica. A agregação de notificações já existentes é pública, permitindo compor o resultado da validação de outros objetos.

**Propriedades:**

- `Notifications`: Retorna a lista somente leitura de notificações.
- `IsValid`: Retorna True se a lista de notificações estiver vazia.
- `Count`: Retorna a quantidade de notificações.
- `Codes`: Retorna a lista de códigos de erros das notificações.
- `Keys`: Retorna a lista de chaves das notificações.
- `Messages`: Retorna a lista de mensagens das notificações.

**Métodos públicos:**

- `AddNotifications(Notification notification)`: Adiciona as notificações de outra notificação à lista de notificações.
- `AddNotifications(params Notification[] notifications)`: Adiciona as notificações de uma coleção de notificações à lista de notificações.

**Métodos protegidos:**

- `AddNotification(NotificationItem notification)`: Adiciona um item de notificação à lista de notificações.
- `AddNotification(Type property, string message)`: Adiciona um item de notificação usando o nome do tipo como chave.
- `AddNotification(Type property, string message, string code)`: Adiciona um item de notificação usando o nome do tipo como chave, com código de erro.
- `AddNotification(string message, string key)`: Adiciona um item de notificação com mensagem e chave.
- `AddNotification(string message, string key, string code)`: Adiciona um item de notificação com mensagem, chave e código de erro.
- `AddNotifications(ICollection<NotificationItem> notifications)`: Adiciona uma coleção de itens de notificação à lista de notificações.
- `Clear()`: Limpa a lista de notificações.

**Tratamento de valores nulos:**

| Situação                                                  | Comportamento                                           |
| --------------------------------------------------------- | ------------------------------------------------------- |
| Argumento nulo em `AddNotification` ou `AddNotifications` | Adiciona a notificação `Notifications.NotificationNull` |
| Item nulo dentro de uma coleção                           | Ignorado, sem alterar a lista                           |
| Adicionar a própria instância em `AddNotifications`       | Sem efeito, a lista não é alterada                      |

[**Exemplo de Uso**](#notificação)

### 3. Mensagens de Erro

**Funcionalidade:**
Classe estática `NotificationErrorMessages` com as mensagens geradas pela própria biblioteca. São chaves de tradução, e não textos finais.

| Constante              | Valor                            | Quando ocorre                                                 |
| ---------------------- | -------------------------------- | ------------------------------------------------------------- |
| `MessageIsNullOrEmpty` | `Notifications.MessageNullEmpty` | Mensagem nula, vazia ou composta apenas por espaços em branco |
| `NotificationIsNull`   | `Notifications.NotificationNull` | Notificação ou coleção recebida como argumento é nula         |

---

## 📝 Exemplos de Uso

### Item de Notificação

```csharp
// Mensagem, com chave 'Unknown' e código 'T.ERR'
var notification1 = new NotificationItem("Mensagem de exemplo");

// Mensagem e chave, com código 'T.ERR'
var notification2 = new NotificationItem("Mensagem de exemplo", "ChaveExemplo");

// Mensagem, chave e código
var notification3 = new NotificationItem("Mensagem de exemplo", "ChaveExemplo", "XPTO1");

// A chave tem os espaços em branco removidos
var notification4 = new NotificationItem("Mensagem de exemplo", "Chave Exemplo");
var key = notification4.Key; // "ChaveExemplo"
```

### Notificação

```csharp
// A adição de notificações é protegida: só o próprio objeto notifica os seus erros
public class MyNotification : Notification
{
  public void NotifyError(string message)
  {
    AddNotification(message, "Error", "XPTO1");
  }
}

var myNotification = new MyNotification();

myNotification.NotifyError("Ocorreu um erro.");

var isValid = myNotification.IsValid; // False
var count = myNotification.Count; // 1
var codes = myNotification.Codes; // ["XPTO1"]
var keys = myNotification.Keys; // ["Error"]
var messages = myNotification.Messages; // ["Ocorreu um erro."]
var notifications = myNotification.Notifications; // [NotificationItem { Message = "Ocorreu um erro.", Key = "Error", Code = "XPTO1" }]
```

```csharp
// A agregação é pública: notificações já existentes podem ser compostas de fora do objeto
public class MyValidation : Notification
{ }

var validation = new MyValidation();

validation.AddNotifications(myNotification, myOtherNotification);
```

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
