# Tooark.Observability

Biblioteca de observabilidade para aplicações .NET, fornecendo integração simplificada com **OpenTelemetry** para coleta de **traces**, **metrics** e **logs** com configuração via `appsettings.json` e defaults sensatos.

📖 **Documentação:** [Tooark.Observability no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.observability.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Observability/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Conteúdo do Pacote](#-conteúdo-do-pacote)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Opções de Configuração](#-opções-de-configuração)
- [Comportamento Padrão](#-comportamento-padrão)
- [Integração com Coletores](#-integração-com-coletores)
- [Ambientes Serverless](#-ambientes-serverless)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Observações Importantes](#️-observações-importantes)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 📦 Conteúdo do Pacote

### Classes de Configuração (Options)

| Classe                     | Descrição                                                               |
| -------------------------- | ----------------------------------------------------------------------- |
| `ObservabilityOptions`     | Configurações principais de Observability                               |
| `TracingOptions`           | Configurações de rastreamento (tracing)                                 |
| `MetricsOptions`           | Configurações de métricas                                               |
| `LoggingOptions`           | Configurações de logging                                                |
| `OtlpOptions`              | Configurações do exportador OTLP                                        |
| `OtlpBatchOptions`         | Configurações do processador Batch (OTLP)                               |
| `OtlpOverrideOptions`      | Overrides do OTLP por sinal (Tracing/Metrics/Logging), herdam do global |
| `OtlpBatchOverrideOptions` | Overrides do Batch por sinal, herdam do global                          |
| `DataSensitiveOptions`     | Configurações de sanitização de dados sensíveis para Tracing            |

### Enumerações

| Enum             | Descrição                                         |
| ---------------- | ------------------------------------------------- |
| `EProtocolOtlp`  | Protocolo de comunicação OTLP (grpc, http)        |
| `EProcessorType` | Tipo de processador de exportação (batch, simple) |

### Extensões de Injeção de Dependência

| Método                     | Descrição                                          |
| -------------------------- | -------------------------------------------------- |
| `AddTooarkOpenTelemetry()` | Configura OpenTelemetry com traces, metrics e logs |
| `AddTooarkObservability()` | Alias de `AddTooarkOpenTelemetry()`                |

Ambos recebem `IConfiguration` e uma `Action<ObservabilityOptions>` opcional, e produzem exatamente o mesmo
registro — `AddTooarkObservability` delega para `AddTooarkOpenTelemetry`. Use o que ficar mais legível no seu
`Program.cs`.

### Instrumentação

| Classe                  | Descrição                                                                                             |
| ----------------------- | ----------------------------------------------------------------------------------------------------- |
| `TooarkInstrumentation` | Singleton com o `ActivitySource` e o `Meter` já registrados, para os spans e as métricas da aplicação |

Registrado pelo `AddTooarkOpenTelemetry` mesmo com `Enabled = false` (uma fonte sem ouvinte não faz nada),
então injetá-lo nunca falha. Também expõe o `ServiceName` e o `ServiceVersion` resolvidos. Veja
[Criando Traces Customizados](#criando-traces-customizados).

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Observability
```

---

## ⚙️ Configuração

### appsettings.json - Configuração Completa

Exemplo com **todas as opções disponíveis** no pacote:

```json
{
  "Observability": {
    "Enabled": true,
    "ServiceName": "MeuServico",
    "ServiceVersion": "1.0.0",
    "ServiceInstanceId": "instancia-001",
    "UseConsoleExporterInDevelopment": true,
    "AllowSensitiveData": false,
    "ResourceAttributes": {
      "provider.name": "aws",
      "provider.region": "us-east-1",
      "provider.cluster": "cluster-a",
      "tenant.id": "tenant-123"
    },
    "Tracing": {
      "Enabled": true,
      "SamplingRatio": 1.0,
      "Otlp": {
        "Endpoint": "http://localhost:4318",
        "Protocol": "http"
      },
      "IgnorePathPrefix": "/api",
      "IgnorePaths": [
        "/health",
        "/healthz",
        "/ready",
        "/traces",
        "/metrics",
        "/logs",
        "/favicon.ico"
      ],
      "ActivitySourceName": "Tooark",
      "AdditionalSources": ["MinhaActivitySource", "OutraSource"],
      "DataSensitive": {
        "HideQueryParameters": true,
        "HideHeaders": true,
        "SensitiveRequestHeaders": [
          "authorization",
          "proxy-authorization",
          "cookie",
          "set-cookie",
          "x-api-key",
          "api-key",
          "apikey",
          "x-functions-key",
          "x-amz-security-token",
          "x-google-oauth-access-token",
          "x-azure-access-token"
        ]
      }
    },
    "Metrics": {
      "Enabled": true,
      "ExportIntervalMilliseconds": 60000,
      "RuntimeMetricsEnabled": true,
      "ProcessMetricsEnabled": true,
      "MeterName": "Tooark",
      "AdditionalMeters": ["MeuMeter", "OutroMeter"],
      "Otlp": {
        "Headers": "tenant-id=metrics-tenant"
      }
    },
    "Logging": {
      "Enabled": true,
      "IncludeFormattedMessage": true,
      "IncludeScopes": true,
      "ParseStateValues": true,
      "Otlp": {
        "Endpoint": "http://localhost:4320"
      }
    },
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://localhost:4317",
      "Protocol": "grpc",
      "ExportProcessorType": "batch",
      "ServerlessOptimized": false,
      "Headers": "api-key=your-api-key,tenant-id=tenant-123",
      "Batch": {
        "MaxQueueSize": 2048,
        "MaxExportBatchSize": 512,
        "ScheduledDelayMilliseconds": 5000,
        "ExporterTimeoutMilliseconds": 30000
      }
    }
  }
}
```

### Override de OTLP por recurso

As configurações em `Observability:Otlp` funcionam como base para `Tracing`, `Metrics` e `Logging`.

Se você definir `Tracing:Otlp`, `Metrics:Otlp` ou `Logging:Otlp`, apenas os campos informados naquele recurso sobrescrevem o OTLP global. Os demais continuam herdados do bloco principal.

Como os overrides usam campos anuláveis (`OtlpOverrideOptions`), qualquer valor informado é aplicado — inclusive valores iguais aos defaults. Por exemplo, `"Tracing": { "Otlp": { "Enabled": false } }` desabilita o OTLP apenas para tracing, mesmo com o OTLP global habilitado.

Exemplo: neste caso, `Tracing` reutiliza `Enabled`, `Headers`, `Batch` e demais campos do OTLP global, alterando apenas `Endpoint` e `Protocol`.

```json
{
  "Observability": {
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://localhost:4317",
      "Headers": "api-key=global-key"
    },
    "Tracing": {
      "Otlp": {
        "Endpoint": "http://localhost:4318",
        "Protocol": "http"
      }
    }
  }
}
```

### appsettings.json - Configuração Mínima

Para a maioria dos casos, uma configuração mínima é suficiente para habilitar **tracing**, **metrics** e **logging** com export via **OTLP**:

```json
{
  "Observability": {
    "ServiceName": "MeuServico",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://localhost:4317"
    }
  }
}
```

ou com header personalizado:

```json
{
  "Observability": {
    "ServiceName": "MeuServico",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://localhost:4317",
      "Headers": "api-key=your-api-key"
    }
  }
}
```

### appsettings.json - Configuração Serverless

Para ambientes serverless (AWS ECS, GCP Cloud Run, Azure Container Apps) com scale-to-zero:

```json
{
  "Observability": {
    "ServiceName": "MeuServico",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://otel-collector:4317",
      "ServerlessOptimized": true
    }
  }
}
```

Para AWS Lambda, GCP Cloud Functions ou Azure Functions (envio imediato):

```json
{
  "Observability": {
    "ServiceName": "MeuServico",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://otel-collector:4317",
      "ExportProcessorType": "simple"
    }
  }
}
```

### Program.cs

```csharp
using Tooark.Observability.Injections;

var builder = WebApplication.CreateBuilder(args);

// Adiciona OpenTelemetry com configurações do appsettings.json
builder.Services.AddTooarkOpenTelemetry(builder.Configuration);

var app = builder.Build();

app.Run();
```

### Configuração Programática

Você também pode configurar programaticamente ou combinar com `appsettings.json`:

```csharp
using Tooark.Observability.Injections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTooarkOpenTelemetry(builder.Configuration, options =>
{
    // Sobrescreve configurações do appsettings.json
    options.ServiceName = "MeuServicoCustomizado";
    options.Tracing.SamplingRatio = 0.5; // 50% dos traces

    // Adiciona sources/meters customizados
    options.Tracing.AdditionalSources = ["MinhaActivitySource"];
    options.Metrics.AdditionalMeters = ["MeuMeter"];

    // Configuração avançada via callbacks
    options.ConfigureTracing = builder =>
    {
        // Adicionar instrumentações adicionais
        // builder.AddSqlClientInstrumentation();
    };

    options.ConfigureMetrics = builder =>
    {
        // Configuração adicional de métricas
    };

    options.ConfigureLogging = loggerOptions =>
    {
        // Configuração adicional de logging
    };

    options.ConfigureResource = resource =>
    {
        // Detectores de recurso dos pacotes contrib do OpenTelemetry (opcional, veja Comportamento Padrão)
        // resource.AddContainerDetector();
    };
});

var app = builder.Build();

app.Run();
```

---

## 📊 Opções de Configuração

### ObservabilityOptions

| Propriedade                       | Tipo                      | Padrão  | Descrição                                                                                      |
| --------------------------------- | ------------------------- | ------- | ---------------------------------------------------------------------------------------------- |
| `Enabled`                         | bool                      | `true`  | Habilita/desabilita Observability                                                              |
| `ServiceName`                     | string?                   | `null`  | Nome do serviço (`OTEL_SERVICE_NAME`, depois o assembly de entrada, se não definido)           |
| `ServiceVersion`                  | string?                   | `null`  | Versão do serviço (`OTEL_RESOURCE_ATTRIBUTES`, depois a versão informacional, se não definida) |
| `ServiceInstanceId`               | string?                   | `null`  | ID único da instância (GUID se não definido)                                                   |
| `ResourceAttributes`              | Dictionary<string,string> | `{}`    | Atributos adicionais para Resource (mesclados com `OTEL_RESOURCE_ATTRIBUTES`)                  |
| `AllowSensitiveData`              | bool                      | `false` | Permitir dados sensíveis sem sanitização global                                                |
| `UseConsoleExporterInDevelopment` | bool                      | `true`  | Usar Console exporter em Development                                                           |
| `Tracing`                         | TracingOptions            | (novo)  | Configurações de rastreamento                                                                  |
| `Metrics`                         | MetricsOptions            | (novo)  | Configurações de métricas                                                                      |
| `Logging`                         | LoggingOptions            | (novo)  | Configurações de logging                                                                       |
| `Otlp`                            | OtlpOptions               | (novo)  | Configurações do exportador OTLP                                                               |

Depois do registro, o `IOptions<ObservabilityOptions>` do container carrega os valores resolvidos:
`ServiceName`, `ServiceVersion`, `Tracing.ActivitySourceName` e `Metrics.MeterName` deixam de ser `null` ou o
padrão puro quando foram inferidos.

Além das opções vinculáveis por `appsettings.json`, `ObservabilityOptions` expõe quatro callbacks que só podem
ser definidos programaticamente, pela sobrecarga com `Action<ObservabilityOptions>`:

| Callback            | Tipo                                  | Uso                                                                       |
| ------------------- | ------------------------------------- | ------------------------------------------------------------------------- |
| `ConfigureTracing`  | `Action<TracerProviderBuilder>?`      | Instrumentações e processadores extras de trace                           |
| `ConfigureMetrics`  | `Action<MeterProviderBuilder>?`       | Instrumentações e readers extras de métricas                              |
| `ConfigureLogging`  | `Action<OpenTelemetryLoggerOptions>?` | Ajustes extras do logging OpenTelemetry                                   |
| `ConfigureResource` | `Action<ResourceBuilder>?`            | Detectores de recurso (nuvem, contêiner); roda por último, após os demais |

### TracingOptions

| Propriedade          | Tipo                 | Padrão                       | Descrição                                                                                                                  |
| -------------------- | -------------------- | ---------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`            | bool                 | `true`                       | Habilita tracing                                                                                                           |
| `SamplingRatio`      | double               | `1.0`                        | Taxa de amostragem (0.0-1.0)                                                                                               |
| `IgnorePathPrefix`   | string?              | `null`                       | Prefixo a adicionar aos paths ignorados                                                                                    |
| `IgnorePaths`        | string[]             | health, metrics, traces, etc | Paths a serem ignorados no tracing                                                                                         |
| `ActivitySourceName` | string               | `Tooark`                     | ActivitySource principal; deixado no padrão, vira o nome do serviço, com `Tooark` como fallback e sempre registrado também |
| `AdditionalSources`  | string[]             | `[]`                         | ActivitySources adicionais para capturar                                                                                   |
| `DataSensitive`      | DataSensitiveOptions | (defaults)                   | Configurações granulares de dados sensíveis                                                                                |

### DataSensitiveOptions (Tracing)

| Propriedade               | Tipo     | Padrão                     | Descrição                                     |
| ------------------------- | -------- | -------------------------- | --------------------------------------------- |
| `HideQueryParameters`     | bool     | `true`                     | Remove a query string dos atributos dos spans |
| `HideHeaders`             | bool     | `true`                     | Mascara headers sensíveis                     |
| `SensitiveRequestHeaders` | string[] | authorization, cookie, etc | Lista de headers a serem mascarados           |

### MetricsOptions

| Propriedade                  | Tipo     | Padrão   | Descrição                                                                                                         |
| ---------------------------- | -------- | -------- | ----------------------------------------------------------------------------------------------------------------- |
| `Enabled`                    | bool     | `true`   | Habilita métricas                                                                                                 |
| `ExportIntervalMilliseconds` | int?     | `null`   | Intervalo de export OTLP (null: 60000, ou 5000 em serverless)                                                     |
| `RuntimeMetricsEnabled`      | bool     | `true`   | Habilita métricas de runtime .NET                                                                                 |
| `ProcessMetricsEnabled`      | bool     | `true`   | Habilita métricas de processo                                                                                     |
| `MeterName`                  | string   | `Tooark` | Meter principal; deixado no padrão, vira o nome do serviço, com `Tooark` como fallback e sempre registrado também |
| `AdditionalMeters`           | string[] | `[]`     | Meters adicionais a registrar                                                                                     |

### LoggingOptions

| Propriedade               | Tipo | Padrão | Descrição                         |
| ------------------------- | ---- | ------ | --------------------------------- |
| `Enabled`                 | bool | `true` | Habilita logging OpenTelemetry    |
| `IncludeFormattedMessage` | bool | `true` | Incluir mensagem formatada        |
| `IncludeScopes`           | bool | `true` | Incluir scopes                    |
| `ParseStateValues`        | bool | `true` | Fazer parse dos valores de estado |

### OtlpOptions

| Propriedade           | Tipo             | Padrão   | Descrição                                         |
| --------------------- | ---------------- | -------- | ------------------------------------------------- |
| `Enabled`             | bool             | `false`  | Habilita exportador OTLP                          |
| `Endpoint`            | string?          | `null`   | Endpoint do coletor (ex: `http://localhost:4317`) |
| `Protocol`            | EProtocolOtlp    | `grpc`   | Protocolo: `grpc` ou `http`                       |
| `ExportProcessorType` | EProcessorType   | `batch`  | Processador: `batch` ou `simple`                  |
| `ServerlessOptimized` | bool             | `false`  | Otimiza batch para ambientes serverless           |
| `Headers`             | string?          | `null`   | Headers (formato: `key1=value1,key2=value2`)      |
| `Batch`               | OtlpBatchOptions | defaults | Opções do batch                                   |

**Protocolo HTTP e o caminho do sinal.** Com `"Protocol": "http"`, cada sinal é enviado para o seu caminho: `/v1/traces`, `/v1/metrics` e `/v1/logs`. Informe só o endereço do coletor (`http://otel-collector:4318`) ou a URL base do provedor (`https://.../otlp`), no OTLP global ou no de um sinal: o pacote acrescenta o caminho. Um endereço que já termina em um desses caminhos é usado como está. No gRPC o endereço nunca é alterado.

`http/protobuf`, como o OpenTelemetry escreve, é aceito como `http`. Um valor que não existe em `Protocol` ou em `ExportProcessorType` impede a aplicação de subir, com `Invalid.Parameter;{valor}`.

### OtlpBatchOptions

| Propriedade                   | Tipo | Padrão  | Descrição                               |
| ----------------------------- | ---- | ------- | --------------------------------------- |
| `MaxQueueSize`                | int  | `2048`  | Tamanho máximo da fila interna          |
| `MaxExportBatchSize`          | int  | `512`   | Tamanho máximo do lote (≤ MaxQueueSize) |
| `ScheduledDelayMilliseconds`  | int  | `5000`  | Intervalo entre envios em lote (ms)     |
| `ExporterTimeoutMilliseconds` | int  | `30000` | Timeout do export (ms)                  |

### OtlpOverrideOptions e OtlpBatchOverrideOptions

Usadas em `Tracing:Otlp`, `Metrics:Otlp` e `Logging:Otlp`. Possuem os mesmos campos de `OtlpOptions` e `OtlpBatchOptions`, porém todos anuláveis: campos não informados (`null`) herdam o valor do OTLP global; campos informados sobrescrevem o global, mesmo quando iguais aos defaults (ex: `Enabled: false` desabilita o OTLP apenas naquele sinal).

---

## 🎯 Comportamento Padrão

### Resource (Identificação do Serviço)

O OpenTelemetry usa Resource para identificar a origem dos dados de telemetria. Cada valor vem da primeira fonte
preenchida na linha; valores vazios ou só com espaços passam para a próxima:

| Atributo                      | Fonte                                                                                                                                                         |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `service.name`                | `ServiceName` → `OTEL_SERVICE_NAME` → nome do assembly de entrada → `"unknown_service"`                                                                       |
| `service.version`             | `ServiceVersion` → `service.version` em `OTEL_RESOURCE_ATTRIBUTES` → `AssemblyInformationalVersion` sem o `+commit` → `AssemblyVersion` → `"unknown_version"` |
| `service.instance.id`         | `ServiceInstanceId` → `OTEL_SERVICE_INSTANCE_ID` → `Guid.NewGuid()`                                                                                           |
| `deployment.environment.name` | `ASPNETCORE_ENVIRONMENT` → `DOTNET_ENVIRONMENT` → `"unknown_environment"`                                                                                     |
| `deployment.environment`      | Mesmo valor acima (chave legada, mantida para compatibilidade)                                                                                                |
| `host.name`                   | `Environment.MachineName`                                                                                                                                     |
| `process.pid`                 | `Environment.ProcessId`, como número                                                                                                                          |
| `process.runtime.*`           | Informações do runtime .NET                                                                                                                                   |

A versão informacional é o `<Version>` do projeto (`2.3.0`, `2.3.0-beta.1`); o sufixo `+<commit>` que o SDK
anexa é descartado. Sem `<Version>`, o padrão do SDK resulta em `1.0.0`.

> **Mudou na 4.8.0**: o `service.version` era lido da `AssemblyVersion` de quatro partes (`2.3.0.0`) e agora
> vem da versão informacional (`2.3.0`); o `process.pid` era texto e agora é número; e a `OTEL_SERVICE_NAME`
> era sobrescrita pelo nome do assembly e agora vence sobre ele. Revise filtros de dashboard que dependam dos
> valores antigos.

#### Atributos do ambiente (`OTEL_*`)

As variáveis padrão do OpenTelemetry são respeitadas sem nenhum código. A `OTEL_RESOURCE_ATTRIBUTES` recebe uma
lista `chave=valor` separada por vírgula e coloca cada par no Resource de traces, métricas e logs:

```bash
OTEL_SERVICE_NAME=orders-api
OTEL_RESOURCE_ATTRIBUTES=cloud.provider=aws,cloud.region=us-east-1,k8s.cluster.name=cluster-a,tenant.id=tenant-123
```

Quando a mesma chave também está em `Observability:ResourceAttributes`, o valor do `appsettings.json` vence.
Prefira as chaves das [convenções semânticas do OpenTelemetry](https://opentelemetry.io/docs/specs/semconv/resource/)
(`cloud.provider`, `cloud.region`, `k8s.cluster.name`): os backends as reconhecem, ao contrário de chaves
livres. Um valor que muda a cada requisição, como o tenant em um deploy multi-tenant, não é atributo de
Resource — vai no span ou no Baggage.

#### Detectores de recurso (`ConfigureResource`)

A detecção automática de metadados de nuvem, contêiner e host fica nos pacotes **contrib** do OpenTelemetry
(`OpenTelemetry.Resources.AWS`, `.Azure`, `.Gcp`, `.Container`, `.Host`, `.Process`). Eles consultam o serviço
de metadados da nuvem na inicialização, e alguns ainda estão em beta, então **não** são dependências do
`Tooark.Observability`. Instale só os que quiser e ligue-os pelo callback `ConfigureResource`, que roda por
último, depois dos atributos do Tooark e de `ResourceAttributes`:

```csharp
// dotnet add package OpenTelemetry.Resources.AWS
// dotnet add package OpenTelemetry.Resources.Container
builder.Services.AddTooarkOpenTelemetry(builder.Configuration, options =>
{
    options.ConfigureResource = resource => resource
        .AddAWSECSDetector()       // OpenTelemetry.Resources.AWS
        .AddContainerDetector();   // OpenTelemetry.Resources.Container
});
```

O callback é opcional: o Tooark funciona igual sem ele e não depende desses pacotes. Os nomes dos métodos dos
detectores variam entre as versões dos pacotes contrib, então confira na versão instalada. O Tooark não garante
que os atributos adicionados por detectores de terceiros sejam aplicados — isso depende da versão do pacote e do
ambiente em que a aplicação roda (um detector fora da sua nuvem não adiciona nada).

### Tracing

Quando habilitado (`Tracing.Enabled = true`):

- **Fontes**: o `ActivitySource` nomeado por `ActivitySourceName` (o nome do serviço quando o padrão é mantido)
  mais `Tooark`, sempre registrado, mais `AdditionalSources`. Um span de uma fonte não registrada é descartado
  em silêncio.
- **ASP.NET Core Instrumentation**: captura automaticamente traces de requisições HTTP de entrada
- **HTTP Client Instrumentation**: captura traces de requisições HTTP de saída (HttpClient)
- **Filter Paths**: por padrão ignora: `/health`, `/healthz`, `/ready`, `/traces`, `/metrics`, `/logs`, `/favicon.ico`
- **RecordException**: exceções são registradas automaticamente nos spans
- **Sampling**: configurável via `SamplingRatio` (0.0 = 0%, 1.0 = 100%)

### Sanitização de Dados Sensíveis

Quando `AllowSensitiveData = false` (padrão):

- **Query Parameters**: removidos dos atributos das convenções semânticas atuais — `url.query` (ASP.NET Core) e `url.full` (HttpClient); o atributo legado `http.target` é reescrito quando presente com query (ex: `/api/users?token=xxx` → `/api/users`)
- **Headers Sensíveis**: mascarados nos spans (Authorization, Cookie, API keys, etc.)

Com `AllowSensitiveData = true`, a sanitização é desligada globalmente e as opções granulares de `Tracing.DataSensitive` são ignoradas.

### Metrics

Quando habilitado (`Metrics.Enabled = true`):

- **Medidores**: o `Meter` nomeado por `MeterName` (o nome do serviço quando o padrão é mantido) mais `Tooark`,
  sempre registrado, mais `AdditionalMeters`. Uma medição de um medidor não registrado é descartada em silêncio.
- **ASP.NET Core Instrumentation**: métricas de requisições HTTP de entrada
- **HTTP Client Instrumentation**: métricas de chamadas HTTP de saída
- **Runtime Instrumentation**: métricas do runtime .NET (GC, threads, etc.) quando `RuntimeMetricsEnabled = true`
- **Process Instrumentation**: métricas de processo (CPU, memória) quando `ProcessMetricsEnabled = true`
- **Intervalo de exportação**: métricas são exportadas via OTLP a cada `ExportIntervalMilliseconds` (padrão: 60s; 5s quando o OTLP efetivo tem `ServerlessOptimized` habilitado)

### Logging

Quando habilitado (`Logging.Enabled = true`):

- Integra com `Microsoft.Extensions.Logging`
- Exporta logs via OTLP junto com traces e metrics
- Correlaciona automaticamente logs com traces (TraceId/SpanId)

### Exportadores

| Exportador  | Ativação                                                                       | Tipo |
| ----------- | ------------------------------------------------------------------------------ | ---- |
| **OTLP**    | `Otlp.Enabled = true` + `Otlp.Endpoint` configurado                            | Push |
| **Console** | Ambiente `Development` + `UseConsoleExporterInDevelopment` + OTLP desabilitado | Push |

> **Nota**: Este pacote **não expõe endpoints HTTP** para coleta de métricas (como `/metrics` do Prometheus). Usa apenas o modelo **push** via OTLP.

---

## 🔌 Integração com Coletores

### OpenTelemetry Collector (gRPC)

```json
{
  "Observability": {
    "ServiceName": "MeuServico",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://otel-collector:4317",
      "Protocol": "grpc"
    }
  }
}
```

### OpenTelemetry Collector (HTTP)

```json
{
  "Observability": {
    "ServiceName": "MeuServico",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://otel-collector:4318",
      "Protocol": "http"
    }
  }
}
```

### Jaeger

```json
{
  "Observability": {
    "ServiceName": "MeuServico",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://jaeger:4317",
      "Protocol": "grpc"
    }
  }
}
```

### Grafana Cloud / Tempo

```json
{
  "Observability": {
    "ServiceName": "MeuServico",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "https://otlp-gateway-prod-us-central-0.grafana.net/otlp",
      "Protocol": "http",
      "Headers": "Authorization=Basic <base64-encoded-credentials>"
    }
  }
}
```

### Azure Monitor / Application Insights

Para Azure Monitor, use o pacote `Azure.Monitor.OpenTelemetry.Exporter` e configure via callback:

```csharp
builder.Services.AddTooarkOpenTelemetry(builder.Configuration, options =>
{
    options.ConfigureTracing = builder =>
    {
        builder.AddAzureMonitorTraceExporter(o =>
        {
            o.ConnectionString = "<connection-string>";
        });
    };

    options.ConfigureMetrics = builder =>
    {
        builder.AddAzureMonitorMetricExporter(o =>
        {
            o.ConnectionString = "<connection-string>";
        });
    };
});
```

---

## 🚀 Ambientes Serverless

Containers serverless podem ser desligados a qualquer momento (scale-to-zero), causando **perda de dados de telemetria** se o shutdown acontecer antes do flush do buffer.

### ServerlessOptimized

Otimiza automaticamente as configurações de batch para minimizar perda de dados:

| Parâmetro                              | Valor Padrão | Valor Serverless | Impacto                                 |
| -------------------------------------- | ------------ | ---------------- | --------------------------------------- |
| `ScheduledDelayMilliseconds`           | 5000ms       | 1000ms           | Flush a cada 1 segundo                  |
| `MaxExportBatchSize`                   | 512          | 128              | Lotes menores, envios mais frequentes   |
| `MaxQueueSize`                         | 2048         | 512              | Menos dados em risco de perda           |
| `Metrics.ExportIntervalMilliseconds`\* | 60000ms      | 5000ms           | Métricas exportadas com mais frequência |

\* Métricas usam um reader periódico (não o processador Batch); o intervalo serverless só é aplicado quando `Metrics.ExportIntervalMilliseconds` não foi informado.

Os valores serverless são aplicados apenas aos campos de batch **não customizados**: qualquer valor definido explicitamente pelo usuário é mantido, mesmo que seja igual ao default padrão.

### Recomendações por Ambiente

| Cenário                                   | Configuração Recomendada              |
| ----------------------------------------- | ------------------------------------- |
| Servidores tradicionais (VMs, bare metal) | Padrão (`ServerlessOptimized: false`) |
| Kubernetes com pods persistentes          | Padrão (`ServerlessOptimized: false`) |
| AWS ECS (Fargate ou EC2)                  | `ServerlessOptimized: true`           |
| GCP Cloud Run                             | `ServerlessOptimized: true`           |
| Azure Container Apps                      | `ServerlessOptimized: true`           |
| AWS Lambda                                | `ExportProcessorType: simple`         |
| GCP Cloud Functions                       | `ExportProcessorType: simple`         |
| Azure Functions                           | `ExportProcessorType: simple`         |

### Graceful Shutdown

O OpenTelemetry SDK faz `ForceFlush` automaticamente durante o shutdown. Configure tempo suficiente:

- **AWS ECS**: `stopTimeout` no task definition (padrão: 30s)
- **GCP Cloud Run**: `terminationGracePeriodSeconds` (padrão: 10s)
- **Kubernetes**: `terminationGracePeriodSeconds` no Pod spec

---

## 📝 Exemplos de Uso

### Exemplo Completo com API

```csharp
using Tooark.Observability.Injections;

var builder = WebApplication.CreateBuilder(args);

// Configura serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura OpenTelemetry
builder.Services.AddTooarkOpenTelemetry(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

Com `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Observability": {
    "ServiceName": "MinhaAPI",
    "ServiceVersion": "1.0.0",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://otel-collector:4317"
    }
  }
}
```

### Criando Traces Customizados

Injete `TooarkInstrumentation`: o `ActivitySource` dele já está registrado com o nome resolvido (o nome do
serviço, `orders-api` por exemplo), então os spans aparecem no backend sob o serviço, e não sob `Tooark`, e a
aplicação nunca repete a string:

```csharp
using System.Diagnostics;
using Tooark.Observability;

public class PedidoService
{
    private readonly ActivitySource _activitySource;

    public PedidoService(TooarkInstrumentation telemetry)
    {
        _activitySource = telemetry.ActivitySource;   // nome: "orders-api"
    }

    public async Task ProcessarPedido(int pedidoId)
    {
        using var activity = _activitySource.StartActivity("ProcessarPedido");
        activity?.SetTag("pedido.id", pedidoId);

        try
        {
            // Lógica de processamento...
            activity?.SetTag("pedido.status", "sucesso");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

Um `new ActivitySource("Tooark")` estático, como nas versões anteriores, continua funcionando: a fonte `Tooark`
está sempre registrada. Uma fonte estática com qualquer outro nome precisa constar em `Tracing.AdditionalSources`,
ou os spans dela são descartados sem nenhum erro.

### Criando Métricas Customizadas

O mesmo singleton expõe o `Meter`, registrado com o `MeterName` resolvido:

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Tooark.Observability;

public class PedidoService
{
    private readonly Counter<long> _pedidosProcessados;
    private readonly Histogram<double> _tempoProcessamento;

    public PedidoService(TooarkInstrumentation telemetry)
    {
        _pedidosProcessados = telemetry.Meter.CreateCounter<long>("pedidos.processados");
        _tempoProcessamento = telemetry.Meter.CreateHistogram<double>("pedidos.tempo_ms");
    }

    public async Task ProcessarPedido(int pedidoId)
    {
        var sw = Stopwatch.StartNew();

        // Lógica de processamento...

        sw.Stop();
        _pedidosProcessados.Add(1, new KeyValuePair<string, object?>("tipo", "novo"));
        _tempoProcessamento.Record(sw.ElapsedMilliseconds);
    }
}
```

Os instrumentos são criados uma vez por nome de medidor, então mantenha-os em campos de um singleton ou crie-os no
construtor, como acima, em vez de a cada chamada. Um `new Meter("Tooark")` estático também continua funcionando.

---

## 📋 Dependências

| Pacote                                                                  | Descrição                               |
| ----------------------------------------------------------------------- | --------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions) | Exceções customizadas                   |
| `OpenTelemetry`                                                         | SDK base do OpenTelemetry               |
| `OpenTelemetry.Exporter.Console`                                        | Exporter para console (desenvolvimento) |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol`                          | Exporter OTLP (gRPC/HTTP)               |
| `OpenTelemetry.Extensions.Hosting`                                      | Integração com Host do .NET             |
| `OpenTelemetry.Instrumentation.AspNetCore`                              | Instrumentação automática ASP.NET Core  |
| `OpenTelemetry.Instrumentation.Http`                                    | Instrumentação automática HttpClient    |
| `OpenTelemetry.Instrumentation.Process`                                 | Instrumentação automática Process       |
| `OpenTelemetry.Instrumentation.Runtime`                                 | Métricas do runtime .NET                |

> O `OpenTelemetry.Instrumentation.Process` só é publicado como pré-lançamento (`rc`) pelo projeto
> OpenTelemetry. Este pacote fixa o `rc` correspondente à linha do OpenTelemetry em uso, e por isso o NuGet
> mostra uma dependência pré-lançamento num pacote estável (aviso `NU5104`). É a única dependência
> pré-lançamento da família Tooark.

---

## ⚠️ Observações Importantes

1. **Lifecycle gerenciado pelo host**: O OpenTelemetry é registrado via `services.AddOpenTelemetry()`, garantindo gerenciamento correto de `TracerProvider`, `MeterProvider` e shutdown graceful.

2. **Sem quebra silenciosa**: Se nenhum exportador estiver configurado, a aplicação continua funcionando normalmente, apenas sem exportar telemetria.

3. **Console Exporter em Development**: Ativado automaticamente quando `UseConsoleExporterInDevelopment = true` e OTLP não está configurado.

4. **Sem endpoint /metrics**: Este pacote usa modelo **push** (OTLP). Não expõe endpoints HTTP para scraping estilo Prometheus.

5. **Normalização de ResourceAttributes**: Chaves são normalizadas automaticamente: `lowercase`, espaços → `.`, caracteres inválidos → `_`. Os pares de `OTEL_RESOURCE_ATTRIBUTES` também são aplicados; na mesma chave, o `appsettings.json` vence.

6. **Callbacks para customização**: Use `ConfigureTracing`, `ConfigureMetrics` e `ConfigureLogging` para adicionar instrumentações ou configurações avançadas, e `ConfigureResource` para os detectores de recurso dos pacotes contrib.

7. **Nomes da fonte e do medidor**: com `ActivitySourceName` e `MeterName` deixados no padrão, a fonte e o medidor principais recebem o nome do serviço, e `Tooark` segue registrado também. O `TooarkInstrumentation` entrega os dois à aplicação já registrados.

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
