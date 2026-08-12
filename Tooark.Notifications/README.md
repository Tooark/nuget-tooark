# Tooark.Notifications

Biblioteca para criação e gerenciamento de notificações e alertas, facilitando a comunicação e monitoramento para projetos .NET.

## Conteúdo

- [NotificationItem](#1-item-de-notificação)
- [Notification](#2-notificação)

## Classes

As classes disponíveis são:

### 1. Item de Notificação

**Funcionalidade:**
Representa a estrutura de um item de notificação com código de erro, chave e mensagem.

**Propriedades:**

- `Code`: O código de erro da notificação.
- `Key`: A chave da notificação.
- `Message`: A mensagem da notificação.

**Métodos:**

- `NotificationItem(string message)`: Cria uma nova instância de `NotificationItem` com uma mensagem.
- `NotificationItem(string message, string key)`: Cria uma nova instância de `NotificationItem` com uma chave e uma mensagem.
- `NotificationItem(string message, string key, string code)`: Cria uma nova instância de `NotificationItem` com um código, uma chave e uma mensagem.
- `ToString()`: Retorna a representação da instância de `NotificationItem` como uma string.
- `string`: Converte implicitamente a instância de `NotificationItem` para uma string.
- `NotificationItem`: Converte implicitamente uma string para uma instância de `NotificationItem`.

[**Exemplo de Uso**](#item-de-notificação)

### 2. Notificação

**Funcionalidade:**
Representa uma classe abstrata que gerencia uma lista de notificações.

**Propriedades:**

- `Notifications`: Retorna a lista de notificações.
- `IsValid`: Retorna True se a lista de notificações estiver vazia.
- `Count`: Retorna o tamanho da lista de notificações.
- `Codes`: Retorna a lista de códigos de erros das notificações.
- `Keys`: Retorna a lista de chaves das notificações.
- `Messages`: Retorna a lista de mensagens das notificações.

**Métodos:**

- `AddNotification(NotificationItem notification)`: Adiciona item de notificação à lista de notificações com base em um item de notificação.
- `AddNotification(Type property, string message)`: Adiciona item de notificação à lista de notificações com base em uma propriedade e uma mensagem.
- `AddNotification(Type property, string message, string code)`: Adiciona item de notificação à lista de notificações com base em uma propriedade, uma mensagem e um código de erro.
- `AddNotification(string message, string key)`: Adiciona item de notificação à lista de notificações com base em uma mensagem e uma chave.
- `AddNotification(string message, string key, string code)`: Adiciona item de notificação à lista de notificações com base em uma mensagem, uma chave e um código de erro.
- `AddNotifications(ICollection<NotificationItem> notifications)`: Adiciona uma coleção de notificações à lista de notificações com base em uma coleção de itens de notificação.
- `AddNotifications(Notification notification)`: Adiciona a lista de notificações de uma notificação à lista de notificações com base em uma notificação.
- `AddNotifications(params Notification[] notifications)`: Adiciona uma coleção de notificações à lista de notificações com base em uma coleção de notificações.
- `Clear()`: Limpa a lista de notificações.

[**Exemplo de Uso**](#notificação)

## Exemplo de Uso

### Item de Notificação

```csharp
// Exemplo de uso
var notification1 = new NotificationItem("Mensagem de exemplo");
var notification2 = new NotificationItem("Mensagem de exemplo", "ChaveExemplo");
var notification3 = new NotificationItem("Mensagem de exemplo", "ChaveExemplo", "T.ERR");
```

### Notificação

```csharp
// Exemplo de uso
public class MyNotification : Notification
{
  public void NotifyError(string message)
  {
    AddNotification(new NotificationItem("Error", message, "XPTO1"));
  }
}

var myNotification = new MyNotification();

myNotification.NotifyError("Ocorreu um erro.");

var isValid = myNotification.IsValid; // False
var count = myNotification.Count; // 1
var codes = myNotification.Codes; // ["XPTO1"]
var keys = myNotification.Keys; // ["Error"]
var messages = myNotification.Messages; // ["Ocorreu um erro."]
var notifications = myNotification.Notifications; // [NotificationItem("Error", "Ocorreu um erro.", "XPTO1")]
```

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Notifications](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
