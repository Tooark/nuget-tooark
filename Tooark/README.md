# Tooark

Biblioteca com todos os recursos e funcionalidades do Tooark voltadas para projetos .NET.

## Instalação

```bash
dotnet add package Tooark
```

O agregador traz todos os pacotes Tooark de uma vez. Instale os pacotes individualmente quando quiser
apenas parte deles — a superfície é a mesma.

## Configuração

As traduções acompanham o assembly, então não há nada a configurar para elas funcionarem.

Adicione a seguinte linha no seu arquivo `Program.cs`:

```csharp
// Importando o namespace necessário
using Tooark.Injections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

IConfiguration configuration = new ConfigurationBuilder()
  .Build();

// Nas suas configurações de serviços
services.AddTooarkService(configuration);
```

Essa única chamada registra `Tooark.Dtos`, `Tooark.Extensions`, `Tooark.ValueObjects` e
`Tooark.Mediator`, e acrescenta `Tooark.Securities` e `Tooark.Observability` quando as seções de
configuração correspondentes existem.

### Onde os manipuladores do mediador são procurados

Sem argumento, no **assembly que chamou o `AddTooarkService`**. Numa aplicação de projeto único, é o
que você quer e não há nada a fazer.

Numa aplicação em camadas, os manipuladores costumam estar em outro projeto. Informe os assemblies:

```csharp
services.AddTooarkService(configuration, typeof(MeuHandler).Assembly);
```

Chamar o `AddTooarkMediator` depois também funciona, para acrescentar assemblies ou configurar as
opções — o registro dos manipuladores não duplica:

```csharp
using Tooark.Mediator.Injections;

services.AddTooarkService(configuration);
services.AddTooarkMediator(typeof(MeuHandler).Assembly);
```

> **Atenção ao chamar o `AddTooarkService` de dentro de uma biblioteca sua.** O assembly procurado é o
> de quem chama, então seria o da sua biblioteca, e não o da aplicação. Repasse o assembly certo.

### Unidade de trabalho

Fica de fora do `AddTooarkService`, porque depende do tipo do seu contexto do Entity Framework:

```csharp
using Tooark.Mediator.EntityFrameworkCore.Injections;

services.AddTooarkMediatorUnitOfWork<MeuDbContext>();
```

## Recursos disponíveis

### [Tooark.Attributes](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Attributes/README.md)

Descrição: Este pacote fornece atributos personalizados para uso em projetos .NET.

### [Tooark.AspNetCore](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.AspNetCore/README.md)

Descrição: Este pacote reúne os recursos que dependem do ASP.NET Core, como a leitura de erros do `ModelState`.

### [Tooark.Dtos](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Dtos/README.md)

Descrição: Este pacote contém objetos de transferência de dados (DTOs) para facilitar a comunicação entre camadas da aplicação.

### [Tooark.Entities](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Entities/README.md)

Descrição: Este pacote define as entidades do domínio utilizadas na aplicação.

### [Tooark.Enums](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Enums/README.md)

Descrição: Este pacote contém definições de enums utilizados em várias partes da aplicação.

### [Tooark.Exceptions](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Exceptions/README.md)

Descrição: Este pacote fornece exceções personalizadas para uso em projetos .NET.

### [Tooark.Extensions](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Extensions/README.md)

Descrição: Este pacote fornece métodos de extensão para tipos e classes comuns do .NET.

### [Tooark.Notifications](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Notifications/README.md)

Descrição: Este pacote oferece funcionalidades para gerenciamento de notificações e mensagens na aplicação.

### [Tooark.Mediator.Abstractions](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.Abstractions/README.md)

Descrição: Este pacote fornece contratos base do padrão Mediator para uso em projetos .NET.

### [Tooark.Mediator](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator/README.md)

Descrição: Este pacote oferece uma implementação concreta do padrão Mediator, facilitando a comunicação entre componentes da aplicação.

### [Tooark.Mediator.EntityFrameworkCore](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Mediator.EntityFrameworkCore/README.md)

Descrição: Este pacote move a persistência do Entity Framework Core para o pipeline do Mediator, mantendo os handlers livres de SaveChanges.

### [Tooark.Observability](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Observability/README.md)

Descrição: Este pacote fornece ferramentas para monitoramento e observabilidade da aplicação.

### [Tooark.Securities](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Securities/README.md)

Descrição: Este pacote oferece funcionalidades para segurança, incluindo criptografia e autenticação.

### [Tooark.Utils](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Utils/README.md)

Descrição: Este pacote contém utilitários e funções auxiliares para diversas operações.

### [Tooark.Validations](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Validations/README.md)

Descrição: Este pacote fornece funcionalidades de validação para dados e entidades.

### [Tooark.ValueObjects](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.ValueObjects/README.md)

Descrição: Este pacote define objetos de valor utilizados na aplicação.

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
