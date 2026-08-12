# Tooark

Biblioteca com todos os recursos e funcionalidades do Tooark voltadas para projetos .NET.

## Configuração

Instale o pacote NuGet `Tooark` para acessar todos os recursos disponíveis. Use o seguinte comando no seu terminal:

```bash
dotnet add package Tooark
```

Para utilizar os resources disponíveis, adicione a seguinte linha no seu arquivo `.csproj`:

```xml
<Target Name="CopyNugetContentFiles" AfterTargets="Build">
  <ItemGroup>
    <NugetContentFiles Include="$(NuGetPackageRoot)\**\Resources\**\*.json" />
  </ItemGroup>
  <Copy SourceFiles="@(NugetContentFiles)" DestinationFolder="$(OutDir)Resources" SkipUnchangedFiles="true" />
</Target>
```

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

## Recursos disponíveis

### [Tooark.Attributes](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Attributes/README.md)

Descrição: Este pacote fornece atributos personalizados para uso em projetos .NET.

### [Tooark.Dtos](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Dtos/README.md)

Descrição: Este pacote contém objetos de transferência de dados (DTOs) para facilitar a comunicação entre camadas da aplicação.

### [Tooark.Entities](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Entities/README.md)

Descrição: Este pacote define as entidades do domínio utilizadas na aplicação.

### [Tooark.Enums](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Enums/README.md)

Descrição: Este pacote contém definições de enums utilizados em várias partes da aplicação.

### [Tooark.Exceptions](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Exceptions/README.md)

Descrição: Este pacote fornece exceções personalizadas para uso em projetos .NET.

### [Tooark.Extensions](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Extensions/README.md)

Descrição: Este pacote fornece métodos de extensão para tipos e classes comuns do .NET.

### [Tooark.Notifications](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Notifications/README.md)

Descrição: Este pacote oferece funcionalidades para gerenciamento de notificações e mensagens na aplicação.

### [Tooark.Mediator.Abstractions](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Mediator.Abstractions/README.md)

Descrição: Este pacote fornece contratos base do padrão Mediator para uso em projetos .NET.

### [Tooark.Mediator](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Mediator/README.md)

Descrição: Este pacote oferece uma implementação concreta do padrão Mediator, facilitando a comunicação entre componentes da aplicação.

### [Tooark.Observability](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Observability/README.md)

Descrição: Este pacote fornece ferramentas para monitoramento e observabilidade da aplicação.

### [Tooark.Securities](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Securities/README.md)

Descrição: Este pacote oferece funcionalidades para segurança, incluindo criptografia e autenticação.

### [Tooark.Utils](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Utils/README.md)

Descrição: Este pacote contém utilitários e funções auxiliares para diversas operações.

### [Tooark.Validations](https://github.com/Tooark/tooark-cs/blob/main/Tooark.Validations/README.md)

Descrição: Este pacote fornece funcionalidades de validação para dados e entidades.

### [Tooark.ValueObjects](https://github.com/Tooark/tooark-cs/blob/main/Tooark.ValueObjects/README.md)

Descrição: Este pacote define objetos de valor utilizados na aplicação.

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
