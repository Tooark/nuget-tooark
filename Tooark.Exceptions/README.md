# Tooark.Exceptions

Biblioteca que fornece exceções padronizadas para projetos .NET, com mapeamento para status HTTP e suporte a múltiplas formas de construção de mensagens de erro.

## Conteúdo

- [Visão Geral](#visão-geral)
- [Recursos Suportados](#recursos-suportados)
- [Exceções Disponíveis](#exceções-disponíveis)
- [Exemplos de Uso](#exemplos-de-uso)
- [Dependências](#dependências)
- [Contribuição](#contribuição)
- [Licença](#licença)

## Visão Geral

Todas as exceções específicas do pacote herdam de `TooarkException`, que por sua vez herda de `Exception`.

A classe base concentra:

- armazenamento de mensagens de erro (`GetErrorMessages()`);
- armazenamento de notificações (`GetNotifications()`);
- contrato para código HTTP (`GetStatusCode()`).

## Recursos Suportados

As classes de exceção suportam os seguintes construtores:

- `ExceptionType(string message)`
- `ExceptionType(IList<string> messages)`
- `ExceptionType(Notification notification)`
- `ExceptionType(string messageFormat, params object[] args)`

### Novos recursos documentados

- **Mensagem formatada com placeholders**: permite criar mensagens dinâmicas com `string.Format`.
- **Integração com `Notification`**: reutiliza mensagens/notificações já agregadas na camada de validação.
- **Múltiplas mensagens**: suporta lista de erros para cenários de validação com mais de uma falha.

## Exceções Disponíveis

| Classe                          | Status HTTP                  |
| ------------------------------- | ---------------------------- |
| `BadRequestException`           | 400 (`BadRequest`)           |
| `GetInfoException`              | 400 (`BadRequest`)           |
| `UnauthorizedException`         | 401 (`Unauthorized`)         |
| `ForbiddenException`            | 403 (`Forbidden`)            |
| `NotFoundException`             | 404 (`NotFound`)             |
| `MethodNotAllowedException`     | 405 (`MethodNotAllowed`)     |
| `ConflictException`             | 409 (`Conflict`)             |
| `PayloadTooLargeException`      | 413 (`PayloadTooLarge`)      |
| `UnsupportedMediaTypeException` | 415 (`UnsupportedMediaType`) |
| `UnprocessableEntityException`  | 422 (`UnprocessableEntity`)  |
| `TooManyRequestsException`      | 429 (`TooManyRequests`)      |
| `InternalServerErrorException`  | 500 (`InternalServerError`)  |
| `BadGatewayException`           | 502 (`BadGateway`)           |
| `ServiceUnavailableException`   | 503 (`ServiceUnavailable`)   |
| `GatewayTimeoutException`       | 504 (`GatewayTimeout`)       |

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

### 3) Mensagem formatada (novo)

```csharp
var userId = 42;
throw new NotFoundException("Usuário com ID {0} não encontrado.", userId);
```

### 4) A partir de `Notification` (novo)

```csharp
using Tooark.Notifications;

public sealed class DomainNotification : Notification { }

var notification = new DomainNotification();
notification.AddNotification("Documento é obrigatório.", "Document");
notification.AddNotification("Telefone inválido.", "Phone");

throw new BadRequestException(notification);
```

### 5) Tratamento padronizado

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
  Console.WriteLine($"Primeiro erro: {errors.FirstOrDefault()}");
  Console.WriteLine($"Total de notificações: {notifications.Count}");
}
```

### 6) Conflito de estado (409)

```csharp
throw new ConflictException("Já existe um usuário com este e-mail.");
```

### 7) Payload muito grande (413)

```csharp
throw new PayloadTooLargeException(
  "Arquivo excede o limite permitido de {0} MB.",
  10
);
```

### 8) Tipo de mídia não suportado (415)

```csharp
throw new UnsupportedMediaTypeException("Content-Type 'text/plain' não é suportado.");
```

### 9) Entidade não processável (422)

```csharp
throw new UnprocessableEntityException([
  "CPF inválido para a regra de negócio.",
  "Data de nascimento incompatível com o cadastro."
]);
```

### 10) Muitas requisições (429)

```csharp
throw new TooManyRequestsException("Limite de requisições excedido. Tente novamente em alguns segundos.");
```

## Dependências

- [Microsoft.AspNetCore.Mvc](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc/)
- [Tooark.Notifications](../Tooark.Notifications/README.md)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja [LICENSE](../LICENSE) para mais detalhes.
