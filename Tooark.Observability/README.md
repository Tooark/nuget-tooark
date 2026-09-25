# Tooark.Observability

Observability library for .NET applications, providing a simplified integration with **OpenTelemetry** to collect **traces**, **metrics** and **logs**, configured through `appsettings.json` with sensible defaults.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Observability/README.pt-BR.md)

## 📦 Package Contents

### Configuration Classes (Options)

| Class                      | Description                                                                  |
| -------------------------- | ---------------------------------------------------------------------------- |
| `ObservabilityOptions`     | Main Observability settings                                                  |
| `TracingOptions`           | Tracing settings                                                             |
| `MetricsOptions`           | Metrics settings                                                             |
| `LoggingOptions`           | Logging settings                                                             |
| `OtlpOptions`              | OTLP exporter settings                                                       |
| `OtlpBatchOptions`         | Batch processor settings (OTLP)                                              |
| `OtlpOverrideOptions`      | Per-signal OTLP overrides (Tracing/Metrics/Logging), inherit from the global |
| `OtlpBatchOverrideOptions` | Per-signal Batch overrides, inherit from the global                          |
| `DataSensitiveOptions`     | Sensitive data sanitization settings for Tracing                             |

### Enumerations

| Enum             | Description                              |
| ---------------- | ---------------------------------------- |
| `EProtocolOtlp`  | OTLP communication protocol (grpc, http) |
| `EProcessorType` | Export processor type (batch, simple)    |

### Dependency Injection Extensions

| Method                     | Description                                            |
| -------------------------- | ------------------------------------------------------ |
| `AddTooarkOpenTelemetry()` | Configures OpenTelemetry with traces, metrics and logs |
| `AddTooarkObservability()` | Alias of `AddTooarkOpenTelemetry()`                    |

Both receive an `IConfiguration` and an optional `Action<ObservabilityOptions>`, and produce exactly the same
registration — `AddTooarkObservability` delegates to `AddTooarkOpenTelemetry`. Use whichever reads better in
your `Program.cs`.

---

## 🔧 Installation

```bash
dotnet add package Tooark.Observability
```

---

## ⚙️ Configuration

### appsettings.json - Full Configuration

Example with **every option available** in the package:

```json
{
  "Observability": {
    "Enabled": true,
    "ServiceName": "MyService",
    "ServiceVersion": "1.0.0",
    "ServiceInstanceId": "instance-001",
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
      "AdditionalSources": ["MyActivitySource", "OtherSource"],
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
      "AdditionalMeters": ["MyMeter", "OtherMeter"],
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

### Per-signal OTLP override

The settings in `Observability:Otlp` work as the base for `Tracing`, `Metrics` and `Logging`.

If you define `Tracing:Otlp`, `Metrics:Otlp` or `Logging:Otlp`, only the fields provided for that signal override the global OTLP. The others keep being inherited from the main block.

Since the overrides use nullable fields (`OtlpOverrideOptions`), any provided value is applied — including values equal to the defaults. For instance, `"Tracing": { "Otlp": { "Enabled": false } }` disables OTLP for tracing only, even with the global OTLP enabled.

Example: here `Tracing` reuses `Enabled`, `Headers`, `Batch` and the other fields of the global OTLP, changing only `Endpoint` and `Protocol`.

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

### appsettings.json - Minimal Configuration

For most cases, a minimal configuration is enough to enable **tracing**, **metrics** and **logging** exported through **OTLP**:

```json
{
  "Observability": {
    "ServiceName": "MyService",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://localhost:4317"
    }
  }
}
```

or with a custom header:

```json
{
  "Observability": {
    "ServiceName": "MyService",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://localhost:4317",
      "Headers": "api-key=your-api-key"
    }
  }
}
```

### appsettings.json - Serverless Configuration

For serverless environments (AWS ECS, GCP Cloud Run, Azure Container Apps) with scale-to-zero:

```json
{
  "Observability": {
    "ServiceName": "MyService",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://otel-collector:4317",
      "ServerlessOptimized": true
    }
  }
}
```

For AWS Lambda, GCP Cloud Functions or Azure Functions (immediate export):

```json
{
  "Observability": {
    "ServiceName": "MyService",
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

// Adds OpenTelemetry with the appsettings.json settings
builder.Services.AddTooarkOpenTelemetry(builder.Configuration);

var app = builder.Build();

app.Run();
```

### Programmatic Configuration

You can also configure programmatically or combine with `appsettings.json`:

```csharp
using Tooark.Observability.Injections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTooarkOpenTelemetry(builder.Configuration, options =>
{
    // Overrides the appsettings.json settings
    options.ServiceName = "MyCustomService";
    options.Tracing.SamplingRatio = 0.5; // 50% of the traces

    // Adds custom sources/meters
    options.Tracing.AdditionalSources = ["MyActivitySource"];
    options.Metrics.AdditionalMeters = ["MyMeter"];

    // Advanced configuration through callbacks
    options.ConfigureTracing = builder =>
    {
        // Add extra instrumentations
        // builder.AddSqlClientInstrumentation();
    };

    options.ConfigureMetrics = builder =>
    {
        // Extra metrics configuration
    };

    options.ConfigureLogging = loggerOptions =>
    {
        // Extra logging configuration
    };
});

var app = builder.Build();

app.Run();
```

---

## 📊 Configuration Options

### ObservabilityOptions

| Property                          | Type                      | Default | Description                                      |
| --------------------------------- | ------------------------- | ------- | ------------------------------------------------ |
| `Enabled`                         | bool                      | `true`  | Enables/disables Observability                   |
| `ServiceName`                     | string?                   | `null`  | Service name (inferred when not set)             |
| `ServiceVersion`                  | string?                   | `null`  | Service version (inferred when not set)          |
| `ServiceInstanceId`               | string?                   | `null`  | Unique instance ID (GUID when not set)           |
| `ResourceAttributes`              | Dictionary<string,string> | `{}`    | Additional attributes for the Resource           |
| `AllowSensitiveData`              | bool                      | `false` | Allow sensitive data without global sanitization |
| `UseConsoleExporterInDevelopment` | bool                      | `true`  | Use the Console exporter in Development          |
| `Tracing`                         | TracingOptions            | (new)   | Tracing settings                                 |
| `Metrics`                         | MetricsOptions            | (new)   | Metrics settings                                 |
| `Logging`                         | LoggingOptions            | (new)   | Logging settings                                 |
| `Otlp`                            | OtlpOptions               | (new)   | OTLP exporter settings                           |

Besides the options bindable from `appsettings.json`, `ObservabilityOptions` exposes three callbacks that can
only be set programmatically, through the overload with `Action<ObservabilityOptions>`:

| Callback           | Type                                  | Usage                                       |
| ------------------ | ------------------------------------- | ------------------------------------------- |
| `ConfigureTracing` | `Action<TracerProviderBuilder>?`      | Extra trace instrumentations and processors |
| `ConfigureMetrics` | `Action<MeterProviderBuilder>?`       | Extra metrics instrumentations and readers  |
| `ConfigureLogging` | `Action<OpenTelemetryLoggerOptions>?` | Extra adjustments to OpenTelemetry logging  |

### TracingOptions

| Property             | Type                 | Default                      | Description                           |
| -------------------- | -------------------- | ---------------------------- | ------------------------------------- |
| `Enabled`            | bool                 | `true`                       | Enables tracing                       |
| `SamplingRatio`      | double               | `1.0`                        | Sampling ratio (0.0-1.0)              |
| `IgnorePathPrefix`   | string?              | `null`                       | Prefix added to the ignored paths     |
| `IgnorePaths`        | string[]             | health, metrics, traces, etc | Paths ignored by tracing              |
| `ActivitySourceName` | string               | `Tooark`                     | Name of the default ActivitySource    |
| `AdditionalSources`  | string[]             | `[]`                         | Additional ActivitySources to capture |
| `DataSensitive`      | DataSensitiveOptions | (defaults)                   | Granular sensitive data settings      |

### DataSensitiveOptions (Tracing)

| Property                  | Type     | Default                    | Description                                   |
| ------------------------- | -------- | -------------------------- | --------------------------------------------- |
| `HideQueryParameters`     | bool     | `true`                     | Removes the query string from span attributes |
| `HideHeaders`             | bool     | `true`                     | Masks sensitive headers                       |
| `SensitiveRequestHeaders` | string[] | authorization, cookie, etc | List of headers to mask                       |

### MetricsOptions

| Property                     | Type     | Default  | Description                                               |
| ---------------------------- | -------- | -------- | --------------------------------------------------------- |
| `Enabled`                    | bool     | `true`   | Enables metrics                                           |
| `ExportIntervalMilliseconds` | int?     | `null`   | OTLP export interval (null: 60000, or 5000 in serverless) |
| `RuntimeMetricsEnabled`      | bool     | `true`   | Enables .NET runtime metrics                              |
| `ProcessMetricsEnabled`      | bool     | `true`   | Enables process metrics                                   |
| `MeterName`                  | string   | `Tooark` | Name of the default Meter                                 |
| `AdditionalMeters`           | string[] | `[]`     | Additional Meters to register                             |

### LoggingOptions

| Property                  | Type | Default | Description                   |
| ------------------------- | ---- | ------- | ----------------------------- |
| `Enabled`                 | bool | `true`  | Enables OpenTelemetry logging |
| `IncludeFormattedMessage` | bool | `true`  | Include the formatted message |
| `IncludeScopes`           | bool | `true`  | Include scopes                |
| `ParseStateValues`        | bool | `true`  | Parse the state values        |

### OtlpOptions

| Property              | Type             | Default  | Description                                       |
| --------------------- | ---------------- | -------- | ------------------------------------------------- |
| `Enabled`             | bool             | `false`  | Enables the OTLP exporter                         |
| `Endpoint`            | string?          | `null`   | Collector endpoint (e.g. `http://localhost:4317`) |
| `Protocol`            | EProtocolOtlp    | `grpc`   | Protocol: `grpc` or `http`                        |
| `ExportProcessorType` | EProcessorType   | `batch`  | Processor: `batch` or `simple`                    |
| `ServerlessOptimized` | bool             | `false`  | Optimizes the batch for serverless environments   |
| `Headers`             | string?          | `null`   | Headers (format: `key1=value1,key2=value2`)       |
| `Batch`               | OtlpBatchOptions | defaults | Batch options                                     |

### OtlpBatchOptions

| Property                      | Type | Default | Description                         |
| ----------------------------- | ---- | ------- | ----------------------------------- |
| `MaxQueueSize`                | int  | `2048`  | Maximum size of the internal queue  |
| `MaxExportBatchSize`          | int  | `512`   | Maximum batch size (≤ MaxQueueSize) |
| `ScheduledDelayMilliseconds`  | int  | `5000`  | Interval between batch exports (ms) |
| `ExporterTimeoutMilliseconds` | int  | `30000` | Export timeout (ms)                 |

### OtlpOverrideOptions and OtlpBatchOverrideOptions

Used in `Tracing:Otlp`, `Metrics:Otlp` and `Logging:Otlp`. They have the same fields as `OtlpOptions` and `OtlpBatchOptions`, but all nullable: fields not provided (`null`) inherit the value of the global OTLP; provided fields override the global one, even when equal to the defaults (e.g. `Enabled: false` disables OTLP for that signal only).

---

## 🎯 Default Behavior

### Resource (Service Identification)

OpenTelemetry uses the Resource to identify the origin of the telemetry data:

| Attribute                     | Source                                                                    |
| ----------------------------- | ------------------------------------------------------------------------- |
| `service.name`                | `ServiceName` → `AssemblyName` → `"unknown_service"`                      |
| `service.version`             | `ServiceVersion` → `AssemblyVersion` → `"unknown_version"`                |
| `service.instance.id`         | `ServiceInstanceId` → `OTEL_SERVICE_INSTANCE_ID` → `Guid.NewGuid()`       |
| `deployment.environment.name` | `ASPNETCORE_ENVIRONMENT` → `DOTNET_ENVIRONMENT` → `"unknown_environment"` |
| `deployment.environment`      | Same value as above (legacy key, kept for compatibility)                  |
| `host.name`                   | `Environment.MachineName`                                                 |
| `process.pid`                 | `Environment.ProcessId`                                                   |
| `process.runtime.*`           | .NET runtime information                                                  |

### Tracing

When enabled (`Tracing.Enabled = true`):

- **ASP.NET Core Instrumentation**: automatically captures traces of incoming HTTP requests
- **HTTP Client Instrumentation**: captures traces of outgoing HTTP requests (HttpClient)
- **Filter Paths**: by default ignores: `/health`, `/healthz`, `/ready`, `/traces`, `/metrics`, `/logs`, `/favicon.ico`
- **RecordException**: exceptions are automatically recorded on the spans
- **Sampling**: configurable through `SamplingRatio` (0.0 = 0%, 1.0 = 100%)

### Sensitive Data Sanitization

When `AllowSensitiveData = false` (default):

- **Query Parameters**: removed from the attributes of the current semantic conventions — `url.query` (ASP.NET Core) and `url.full` (HttpClient); the legacy `http.target` attribute is rewritten when present with a query (e.g. `/api/users?token=xxx` → `/api/users`)
- **Sensitive Headers**: masked on the spans (Authorization, Cookie, API keys, etc.)

With `AllowSensitiveData = true`, sanitization is turned off globally and the granular `Tracing.DataSensitive` options are ignored.

### Metrics

When enabled (`Metrics.Enabled = true`):

- **ASP.NET Core Instrumentation**: metrics of incoming HTTP requests
- **HTTP Client Instrumentation**: metrics of outgoing HTTP calls
- **Runtime Instrumentation**: .NET runtime metrics (GC, threads, etc.) when `RuntimeMetricsEnabled = true`
- **Process Instrumentation**: process metrics (CPU, memory) when `ProcessMetricsEnabled = true`
- **Export interval**: metrics are exported through OTLP every `ExportIntervalMilliseconds` (default: 60s; 5s when the effective OTLP has `ServerlessOptimized` enabled)

### Logging

When enabled (`Logging.Enabled = true`):

- Integrates with `Microsoft.Extensions.Logging`
- Exports logs through OTLP together with traces and metrics
- Automatically correlates logs with traces (TraceId/SpanId)

### Exporters

| Exporter    | Activation                                                                    | Type |
| ----------- | ----------------------------------------------------------------------------- | ---- |
| **OTLP**    | `Otlp.Enabled = true` + `Otlp.Endpoint` configured                            | Push |
| **Console** | `Development` environment + `UseConsoleExporterInDevelopment` + OTLP disabled | Push |

> **Note**: This package **does not expose HTTP endpoints** for metrics collection (such as Prometheus' `/metrics`). It uses the **push** model through OTLP only.

---

## 🔌 Collector Integration

### OpenTelemetry Collector (gRPC)

```json
{
  "Observability": {
    "ServiceName": "MyService",
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
    "ServiceName": "MyService",
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
    "ServiceName": "MyService",
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
    "ServiceName": "MyService",
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

For Azure Monitor, use the `Azure.Monitor.OpenTelemetry.Exporter` package and configure it through the callback:

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

## 🚀 Serverless Environments

Serverless containers can be shut down at any time (scale-to-zero), causing **telemetry data loss** if the shutdown happens before the buffer is flushed.

### ServerlessOptimized

Automatically tunes the batch settings to minimize data loss:

| Parameter                              | Default Value | Serverless Value | Impact                                 |
| -------------------------------------- | ------------- | ---------------- | -------------------------------------- |
| `ScheduledDelayMilliseconds`           | 5000ms        | 1000ms           | Flush every second                     |
| `MaxExportBatchSize`                   | 512           | 128              | Smaller batches, more frequent exports |
| `MaxQueueSize`                         | 2048          | 512              | Less data at risk of loss              |
| `Metrics.ExportIntervalMilliseconds`\* | 60000ms       | 5000ms           | Metrics exported more often            |

\* Metrics use a periodic reader (not the Batch processor); the serverless interval is only applied when `Metrics.ExportIntervalMilliseconds` was not provided.

The serverless values are applied only to the batch fields **not customized**: any value explicitly set by the user is kept, even when equal to the standard default.

### Recommendations by Environment

| Scenario                              | Recommended Configuration              |
| ------------------------------------- | -------------------------------------- |
| Traditional servers (VMs, bare metal) | Default (`ServerlessOptimized: false`) |
| Kubernetes with persistent pods       | Default (`ServerlessOptimized: false`) |
| AWS ECS (Fargate or EC2)              | `ServerlessOptimized: true`            |
| GCP Cloud Run                         | `ServerlessOptimized: true`            |
| Azure Container Apps                  | `ServerlessOptimized: true`            |
| AWS Lambda                            | `ExportProcessorType: simple`          |
| GCP Cloud Functions                   | `ExportProcessorType: simple`          |
| Azure Functions                       | `ExportProcessorType: simple`          |

### Graceful Shutdown

The OpenTelemetry SDK does a `ForceFlush` automatically during shutdown. Configure enough time:

- **AWS ECS**: `stopTimeout` in the task definition (default: 30s)
- **GCP Cloud Run**: `terminationGracePeriodSeconds` (default: 10s)
- **Kubernetes**: `terminationGracePeriodSeconds` in the Pod spec

---

## 📝 Usage Examples

### Full Example with an API

```csharp
using Tooark.Observability.Injections;

var builder = WebApplication.CreateBuilder(args);

// Configures services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configures OpenTelemetry
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

With `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Observability": {
    "ServiceName": "MyAPI",
    "ServiceVersion": "1.0.0",
    "Otlp": {
      "Enabled": true,
      "Endpoint": "http://otel-collector:4317"
    }
  }
}
```

### Creating Custom Traces

```csharp
using System.Diagnostics;

public class MyService
{
    private static readonly ActivitySource _activitySource = new("Tooark");

    public async Task ProcessOrder(int orderId)
    {
        using var activity = _activitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", orderId);

        try
        {
            // Processing logic...
            activity?.SetTag("order.status", "success");
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

### Creating Custom Metrics

```csharp
using System.Diagnostics.Metrics;

public class MyService
{
    private static readonly Meter _meter = new("Tooark");
    private static readonly Counter<long> _ordersProcessed = _meter.CreateCounter<long>("orders.processed");
    private static readonly Histogram<double> _processingTime = _meter.CreateHistogram<double>("orders.time_ms");

    public async Task ProcessOrder(int orderId)
    {
        var sw = Stopwatch.StartNew();

        // Processing logic...

        sw.Stop();
        _ordersProcessed.Add(1, new KeyValuePair<string, object?>("type", "new"));
        _processingTime.Record(sw.ElapsedMilliseconds);
    }
}
```

---

## 📋 Dependencies

| Package                                                                 | Description                            |
| ----------------------------------------------------------------------- | -------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions) | Custom exceptions                      |
| `OpenTelemetry`                                                         | OpenTelemetry base SDK                 |
| `OpenTelemetry.Exporter.Console`                                        | Console exporter (development)         |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol`                          | OTLP exporter (gRPC/HTTP)              |
| `OpenTelemetry.Extensions.Hosting`                                      | Integration with the .NET Host         |
| `OpenTelemetry.Instrumentation.AspNetCore`                              | Automatic ASP.NET Core instrumentation |
| `OpenTelemetry.Instrumentation.Http`                                    | Automatic HttpClient instrumentation   |
| `OpenTelemetry.Instrumentation.Process`                                 | Automatic Process instrumentation      |
| `OpenTelemetry.Instrumentation.Runtime`                                 | .NET runtime metrics                   |

> `OpenTelemetry.Instrumentation.Process` is only published as a pre-release (`rc`) by the OpenTelemetry
> project. This package pins it to the `rc` that matches the OpenTelemetry line in use, which is why NuGet
> shows a pre-release dependency on a stable package (warning `NU5104`). It is the only pre-release dependency
> in the Tooark family.

---

## ⚠️ Important Notes

1. **Lifecycle managed by the host**: OpenTelemetry is registered through `services.AddOpenTelemetry()`, ensuring proper management of `TracerProvider`, `MeterProvider` and graceful shutdown.

2. **No silent break**: If no exporter is configured, the application keeps working normally, just without exporting telemetry.

3. **Console Exporter in Development**: Enabled automatically when `UseConsoleExporterInDevelopment = true` and OTLP is not configured.

4. **No /metrics endpoint**: This package uses the **push** model (OTLP). It does not expose HTTP endpoints for Prometheus-style scraping.

5. **ResourceAttributes normalization**: Keys are normalized automatically: `lowercase`, spaces → `.`, invalid characters → `_`.

6. **Callbacks for customization**: Use `ConfigureTracing`, `ConfigureMetrics` and `ConfigureLogging` to add instrumentations or advanced settings.

---

## 🪪 Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Observability](https://github.com/Tooark/nuget-tooark/issues) repository.

## 📄 License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
