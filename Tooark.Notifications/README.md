# Tooark.Notifications

Biblioteca para criação e gerenciamento de notificações e alertas, facilitando a comunicação e monitoramento para projetos .NET.

## Conteúdo

- [NotificationItem](#1-item-de-notificação)
- [Notification](#2-notificação)
- [NotificationErrorMessages](#3-mensagens-de-erro)

## Classes

As classes disponíveis são:

### 1. Item de Notificação

**Funcionalidade:**
Representa a estrutura de um item de notificação com mensagem, chave e código de erro. Os valores são normalizados na construção e a instância é somente leitura.

**Propriedades:**

- `Message`: A mensagem da notificação.
- `Key`: A chave da notificação.
- `Code`: O código de erro da notificação.

**Métodos:**

- `NotificationItem(string message)`: Cria uma nova instância com a chave `Unknown` e o código `T.ERR`.
- `NotificationItem(string message, string key)`: Cria uma nova instância com a chave informada e o código `T.ERR`.
- `NotificationItem(string message, string key, string code)`: Cria uma nova instância com a chave e o código informados.
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

## Exemplo de Uso

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

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Notifications](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/tooark-cs/refs/heads/main/LICENSE) para mais detalhes.
