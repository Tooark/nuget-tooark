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

### [Tooark.Attributes](../Tooark.Attributes/README.md)

Descrição: Este pacote fornece atributos personalizados para uso em projetos .NET.

### [Tooark.Dtos](../Tooark.Dtos/README.md)

Descrição: Este pacote contém objetos de transferência de dados (DTOs) para facilitar a comunicação entre camadas da aplicação.

### [Tooark.Entities](../Tooark.Entities/README.md)

Descrição: Este pacote define as entidades do domínio utilizadas na aplicação.

### [Tooark.Enums](../Tooark.Enums/README.md)

Descrição: Este pacote contém definições de enums utilizados em várias partes da aplicação.

### [Tooark.Exceptions](../Tooark.Exceptions/README.md)

Descrição: Este pacote fornece exceções personalizadas para uso em projetos .NET.

### [Tooark.Extensions](../Tooark.Extensions/README.md)

Descrição: Este pacote fornece métodos de extensão para tipos e classes comuns do .NET.

### [Tooark.Notifications](../Tooark.Notifications/README.md)

Descrição: Este pacote oferece funcionalidades para gerenciamento de notificações e mensagens na aplicação.

### [Tooark.Observability](../Tooark.Observability/README.md)

Descrição: Este pacote fornece ferramentas para monitoramento e observabilidade da aplicação.

### [Tooark.Securities](../Tooark.Securities/README.md)

Descrição: Este pacote oferece funcionalidades para segurança, incluindo criptografia e autenticação.

### [Tooark.Utils](../Tooark.Utils/README.md)

Descrição: Este pacote contém utilitários e funções auxiliares para diversas operações.

### [Tooark.Validations](../Tooark.Validations/README.md)

Descrição: Este pacote fornece funcionalidades de validação para dados e entidades.

### [Tooark.ValueObjects](../Tooark.ValueObjects/README.md)

Descrição: Este pacote define objetos de valor utilizados na aplicação.

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark](https://github.com/Tooark/nuget-tooark/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
