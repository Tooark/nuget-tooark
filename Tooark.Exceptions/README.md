# Tooark.Exceptions

Biblioteca que fornece exceções padronizadas para projetos .NET, com mapeamento para status HTTP e suporte a múltiplas formas de construção de mensagens de erro.

## Conteúdo

- [Instalação](#instalação)
- [Visão Geral](#visão-geral)
- [Recursos Suportados](#recursos-suportados)
- [Mensagens de Erro](#mensagens-de-erro)
- [Exceções Disponíveis](#exceções-disponíveis)
- [Exemplos de Uso](#exemplos-de-uso)
- [Dependências](#dependências)
- [Contribuição](#contribuição)
- [Licença](#licença)

## Instalação

```bash
dotnet add package Tooark.Exceptions
```

O pacote não tem configuração nem registro no container: as exceções são lançadas e capturadas diretamente.

## Visão Geral

Todas as exceções específicas do pacote herdam de `TooarkException`, que por sua vez herda de `Exception`.
A classe base é **abstrata** e seus construtores são **protegidos** — ela não é instanciada diretamente, e
sim estendida, seja pelas exceções do pacote, seja pelas da sua aplicação.

A classe base concentra:

- mensagens de erro (`GetErrorMessages()`);
- notificações equivalentes (`GetNotifications()`);
- contrato para código HTTP (`GetStatusCode()`).

Duas garantias valem para qualquer construtor:

- **As coleções são somente leitura.** `GetErrorMessages()` e `GetNotifications()` devolvem sempre a mesma
  instância somente leitura, então nenhum código externo consegue esvaziar ou alterar o erro que a exceção
  carrega — o que faria uma falha desaparecer no meio do tratamento.
- **As coleções nunca ficam vazias.** Entrada nula, vazia ou sem mensagens registra uma chave conhecida em vez
  de produzir uma exceção que se diz de erro mas não carrega erro algum. As duas leituras andam juntas: a
  mesma posição descreve o mesmo erro em ambas.

## Recursos Suportados

As classes de exceção suportam os seguintes construtores:

| Construtor                                                  | Uso                                                          |
| ----------------------------------------------------------- | ------------------------------------------------------------ |
| `ExceptionType(string message)`                             | Mensagem única                                               |
| `ExceptionType(string message, Exception innerException)`   | Mensagem única preservando a causa raiz                      |
| `ExceptionType(IList<string> messages)`                     | Várias mensagens, para validação com mais de uma falha       |
| `ExceptionType(Notification notification)`                  | Reaproveita notificações já agregadas na camada de validação |
| `ExceptionType(string messageFormat, params object[] args)` | Mensagem dinâmica com marcadores `{0}`, `{1}`, etc.          |

> **Resolução de sobrecarga**: `new NotFoundException("Falha: {0}", exceptionObject)` liga ao construtor de
> exceção interna, não ao de formatação, porque `Exception` é o parâmetro mais específico. Para formatar
> usando uma exceção como argumento, converta-a antes (por exemplo `ex.Message`).

Ao usar a formatação, o formato incompatível com os parâmetros **não** lança: a mensagem é mantida como
recebida. A exceção existe para reportar o erro original, e falhar na própria apresentação trocaria o erro
real por uma `FormatException`. Mensagens com chaves literais, como JSON, caem nesse caso.

## Mensagens de Erro

A classe estática `ExceptionErrorMessages` reúne as mensagens geradas pelo próprio pacote. São chaves de
tradução, e não textos finais.

| Constante              | Valor                          | Quando ocorre                                                   |
| ---------------------- | ------------------------------ | --------------------------------------------------------------- |
| `MessageIsNullOrEmpty` | `Exceptions.MessageNullEmpty`  | Mensagem nula, vazia ou composta apenas por espaços em branco   |
| `ErrorsIsNullOrEmpty`  | `Exceptions.ErrorsNullOrEmpty` | Lista de mensagens ou notificação nula, ou sem nenhuma mensagem |

**Tratamento de valores nulos:**

| Situação                                 | Comportamento                                            |
| ---------------------------------------- | -------------------------------------------------------- |
| `null` como mensagem única               | Registra `Exceptions.MessageNullEmpty`                   |
| `null` ou lista vazia em `IList<string>` | Registra `Exceptions.ErrorsNullOrEmpty`                  |
| `null` ou notificação sem itens          | Registra `Exceptions.ErrorsNullOrEmpty`                  |
| Item nulo dentro da lista                | Vira `Exceptions.MessageNullEmpty` nas **duas** coleções |

## Exceções Disponíveis

| Classe                          | Status HTTP                   |
| ------------------------------- | ----------------------------- |
| `GetInfoException`              | 400 (`BadRequest`)            |
| `BadRequestException`           | 400 (`BadRequest`)            |
| `UnauthorizedException`         | 401 (`Unauthorized`)          |
| `ForbiddenException`            | 403 (`Forbidden`)             |
| `NotFoundException`             | 404 (`NotFound`)              |
| `MethodNotAllowedException`     | 405 (`MethodNotAllowed`)      |
| `ConflictException`             | 409 (`Conflict`)              |
| `PayloadTooLargeException`      | 413 (`RequestEntityTooLarge`) |
| `UnsupportedMediaTypeException` | 415 (`UnsupportedMediaType`)  |
| `UnprocessableEntityException`  | 422 (`UnprocessableEntity`)   |
| `TooManyRequestsException`      | 429 (`TooManyRequests`)       |
| `InternalServerErrorException`  | 500 (`InternalServerError`)   |
| `BadGatewayException`           | 502 (`BadGateway`)            |
| `ServiceUnavailableException`   | 503 (`ServiceUnavailable`)    |
| `GatewayTimeoutException`       | 504 (`GatewayTimeout`)        |

> O parêntese indica o membro de `System.Net.HttpStatusCode`. Para o 413 o .NET mantém o nome antigo
> `RequestEntityTooLarge`, embora o RFC 7231 chame o status de _Payload Too Large_.

## Exemplos de Uso

### 1) Mensagem simples

```csharp
throw new BadRequestException("Payload inválido.");
```

### 2) Múltiplas mensagens

```csharp
throw new BadRequestException([
  "Nome é obrigatório.",
  "E-mail inválido."
]);
```

### 3) Mensagem formatada

```csharp
var userId = 42;
throw new NotFoundException("Usuário com ID {0} não encontrado.", userId);
```

### 4) A partir de `Notification`

A criação de notificações é protegida, então quem notifica é o próprio objeto:

```csharp
using Tooark.Notifications;

public sealed class DomainNotification : Notification
{
  public void NotifyRequired(string campo) =>
    AddNotification($"{campo} é obrigatório.", campo, "T.DOM1");
}

var notification = new DomainNotification();
notification.NotifyRequired("Document");
notification.NotifyRequired("Phone");

throw new BadRequestException(notification);
```

Os itens da notificação são preservados como estão, mantendo a chave e o código de cada um.

### 5) Preservando a causa raiz

```csharp
try
{
  await _httpClient.GetAsync(url, cancellationToken);
}
catch (HttpRequestException ex)
{
  // A exceção original continua acessível em InnerException, para diagnóstico
  throw new BadGatewayException("Serviço de pagamentos indisponível.", ex);
}
```

### 6) Tratamento padronizado

```csharp
using Tooark.Exceptions;

try
{
  throw new ServiceUnavailableException("Serviço externo indisponível.");
}
catch (TooarkException ex)
{
  var statusCode = ex.GetStatusCode();
  var errors = ex.GetErrorMessages();
  var notifications = ex.GetNotifications();

  Console.WriteLine($"Status: {(int)statusCode} - {statusCode}");
  Console.WriteLine($"Primeiro erro: {errors[0]}");
  Console.WriteLine($"Total de notificações: {notifications.Count}");
}
```

`errors[0]` é seguro: as coleções nunca ficam vazias.

### 7) Conflito de estado (409)

```csharp
throw new ConflictException("Já existe um usuário com este e-mail.");
```

### 8) Payload muito grande (413)

```csharp
throw new PayloadTooLargeException(
  "Arquivo excede o limite permitido de {0} MB.",
  10
);
```

### 9) Tipo de mídia não suportado (415)

```csharp
throw new UnsupportedMediaTypeException("Content-Type 'text/plain' não é suportado.");
```

### 10) Entidade não processável (422)

```csharp
throw new UnprocessableEntityException([
  "CPF inválido para a regra de negócio.",
  "Data de nascimento incompatível com o cadastro."
]);
```

### 11) Muitas requisições (429)

```csharp
throw new TooManyRequestsException("Limite de requisições excedido. Tente novamente em alguns segundos.");
```

### 12) Criando a sua própria exceção

A hierarquia é aberta: basta herdar de `TooarkException` e informar o código HTTP. Todos os construtores da
base ficam disponíveis, com as mesmas garantias de coleção somente leitura e nunca vazia.

```csharp
using System.Net;
using Tooark.Exceptions;
using Tooark.Notifications;

public class PaymentRequiredException : TooarkException
{
  public PaymentRequiredException(string message) : base(message) { }

  public PaymentRequiredException(string message, Exception innerException)
    : base(message, innerException) { }

  public PaymentRequiredException(IList<string> messages) : base(messages) { }

  public PaymentRequiredException(Notification notification) : base(notification) { }

  public PaymentRequiredException(string messageFormat, params object[] args)
    : base(messageFormat, args) { }

  public override HttpStatusCode GetStatusCode() => HttpStatusCode.PaymentRequired;
}
```

O `catch (TooarkException ex)` do tratamento padronizado passa a capturá-la junto com as demais.

## Dependências

| Pacote                                                                        | Versão | Uso                                 |
| ----------------------------------------------------------------------------- | ------ | ----------------------------------- |
| [`Tooark.Notifications`](https://www.nuget.org/packages/Tooark.Notifications) | 4.x    | `Notification` e `NotificationItem` |

O pacote não depende de ASP.NET Core: o código HTTP é exposto como `System.Net.HttpStatusCode`, do próprio
runtime, e a tradução para a resposta fica a cargo da aplicação.

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Exceptions](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
