# Tooark.Exceptions

Biblioteca que fornece exceções para projetos .NET, permitindo padronização para tratamento de erros. Inclui métodos para criar e gerenciar exceções.

## Conteúdo

- [TooarkException](#0-tooarkexception)
- [BadRequestException](#400-badrequestexception)
- [UnauthorizedException](#401-unauthorizedexception)
- [ForbiddenException](#403-forbiddenexception)
- [NotFoundException](#404-notfoundexception)
- [MethodNotAllowedException](#405-methodnotallowedexception)
- [InternalServerErrorException](#500-internalservererrorexception)
- [BadGatewayException](#502-badgatewayexception)
- [ServiceUnavailableException](#503-serviceunavailableexception)
- [GatewayTimeoutException](#504-gatewaytimeoutexception)
- [GetInfoException](#400-getinfoexception)

## Exceções

### 0. TooarkException

Classe base abstrata para todas as exceções do Tooark estende a classe `Exception`.

- **Construtores:**

  - `TooarkException(string message)`: Inicializa uma nova instância da classe `TooarkException` com uma mensagem de erro.
  - `TooarkException(IList<string> messages)`: Inicializa uma nova instância da classe `TooarkException` com uma lista de mensagens de erros.
  - `TooarkException(Notification notification)`: Inicializa uma nova instância da classe `TooarkException` com uma notificação com as mensagens de erros.

- **Propriedades:**

  - `_errors`: Lista privada de mensagens de erro.

- **Métodos:**
  - `GetErrorMessages()`: Retorna a lista de mensagens de erro.
  - `GetNotifications()`: Retorna a lista de itens de notificação.
  - `GetStatusCode()`: Método abstrato para retornar o código de status HTTP associado à exceção.

### 400. BadRequestException

**Funcionalidade:**
Representa uma exceção de requisição inválida (HTTP 400).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.BadRequest`.

### 401. UnauthorizedException

**Funcionalidade:**
Representa uma exceção de não autorizado (HTTP 401).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.Unauthorized`.

### 403. ForbiddenException

**Funcionalidade:**
Representa uma exceção de acesso negado (HTTP 403).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.Forbidden`.

### 404. NotFoundException

**Funcionalidade:**
Representa uma exceção de recurso não encontrado (HTTP 404).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.NotFound`.

### 405. MethodNotAllowedException

**Funcionalidade:**
Representa uma exceção de método não permitido (HTTP 405).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.MethodNotAllowed`.

### 500. InternalServerErrorException

**Funcionalidade:**
Representa uma exceção de erro interno do servidor (HTTP 500).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.InternalServerError`.

### 502. BadGatewayException

**Funcionalidade:**
Representa uma exceção de gateway inválido (HTTP 502).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.BadGateway`.

### 503. ServiceUnavailableException

**Funcionalidade:**
Representa uma exceção de serviço indisponível (HTTP 503).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.ServiceUnavailable`.

### 504. GatewayTimeoutException

**Funcionalidade:**
Representa uma exceção de tempo limite de gateway (HTTP 504).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.GatewayTimeout`.

### 400. GetInfoException

**Funcionalidade:**
Representa erro ao tentar buscar dados gerando uma exceção de requisição inválida (HTTP 400).

- **Métodos:**
  - `GetStatusCode()`: Configurado para retornar `HttpStatusCode.BadRequest`.

## Exemplo de Uso

```csharp
using Tooark.Exceptions;

public class ExampleService
{
  public void Execute()
  {
    try
    {
      // Lógica que pode lançar exceções
      ValidateUserInput(null);
      FetchResource(0);
    }
    catch (TooarkException ex)
    {
      Console.WriteLine($"Erro: {ex.GetErrorMessages().FirstOrDefault()}");
      Console.WriteLine($"Code: {ex.GetNotifications().FirstOrDefault()?.Code}, Message: {ex.GetNotifications().FirstOrDefault()?.Message}");
      Console.WriteLine($"Status Code: {(int)ex.GetStatusCode()}");
    }
  }

  private void ValidateUserInput(string input)
  {
    if (string.IsNullOrEmpty(input))
    {
      throw new BadRequestException("Input inválido.");
    }
  }

  private void FetchResource(int resourceId)
  {
    if (resourceId <= 0)
    {
      throw new NotFoundException("Recurso não encontrado.");
    }
  }
}
```

## Dependências

- [Microsoft.AspNetCore.Mvc](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc/)
- [Tooark.Notifications](../Tooark.Notifications/README.md)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Exceptions](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
