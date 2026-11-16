# Tooark.Http

Library for outbound HTTP in .NET projects: a thin JSON service over `IHttpClientFactory`, with resilience, errors mapped to the Tooark exceptions and an outbound guard against SSRF.

The service does not replace `HttpClient` and does not re-implement its pipeline: the client is created by
`IHttpClientFactory`, retries, circuit breaker and timeouts come from
[Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/dotnet/core/resilience/http-resilience), and the
guard is a `SocketsHttpHandler.ConnectCallback`. The package adds the JSON conventions, the options in the Tooark
pattern and the conversion of every failure into an exception that carries `NotificationItem`s.

📖 **Docs:** [Tooark.Http on the site](https://tooark.com/nuget-tooark/packages/tooark.http.html) · [All packages](https://tooark.com/nuget-tooark/) · [API reference](https://tooark.com/nuget-tooark/api/index.html)

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Http/README.pt-BR.md)

---

## 📑 Contents

- [Overview](#-overview)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Service](#-service)
- [Resilience](#-resilience)
- [Error Handling](#-error-handling)
- [Outbound Guard](#-outbound-guard)
- [Observability](#-observability)
- [Usage Examples](#-usage-examples)
- [Dependencies](#-dependencies)
- [Best Practices](#-best-practices)
- [Error Codes and Solutions](#️-error-codes-and-solutions)
- [Contributing](#-contributing)
- [Help & Security](#-help--security)
- [Support](#-support)
- [License](#-license)

---

## 📖 Overview

| Piece                        | What it does                                                                                   |
| ---------------------------- | ---------------------------------------------------------------------------------------------- |
| `IHttpClientService`         | GET, POST, PUT, PATCH and DELETE with JSON, plus `SendAsync` for everything else               |
| `AddTooarkHttpClient`        | Registers the service and the named client `Tooark`, with resilience configured by the options |
| `AddTooarkOutboundGuard`     | Adds the outbound guard to any named or typed client of the application                        |
| `OutboundGuard.IsBlocked`    | The block list as a pure function, for validations of the application itself                   |
| `HttpRequestFailedException` | The error response of the called service, with the original status and its errors              |
| `OutboundBlockedException`   | The destination refused by the guard                                                           |

- **Thin layer**: no `new HttpClient()`, no pipeline of its own. The named client is resolved from
  `IHttpClientFactory` on every call, so handler rotation and DNS refresh keep working.
- **Failures in the Tooark standard**: an error status, a timeout, an open circuit, a connection failure and a
  blocked destination each become a Tooark exception with a message key.
- **Fails at startup**: invalid options are rejected at registration, including the combinations the resilience
  handler would only reject on the first request.
- **No per-call state**: the service is a singleton and safe for concurrent use.

---

## 🔧 Installation

```bash
dotnet add package Tooark.Http
```

The package runs on the base .NET runtime, without ASP.NET Core. It also comes with the `Tooark` aggregator, but
`AddTooarkService` does not register the client: call `AddTooarkHttpClient` in the application.

---

## ⚙️ Configuration

### appsettings.json

Everything is optional. The example shows every option with its default value, except the base address and the
guard lists:

```json
{
  "HttpClient": {
    "BaseAddress": "https://api.example.com/v1",
    "TotalTimeoutSeconds": 30,
    "AttemptTimeoutSeconds": 10,
    "RetryCount": 3,
    "RetryDelayMilliseconds": 2000,
    "CircuitBreakerFailureRatio": 0.1,
    "CircuitBreakerMinimumThroughput": 100,
    "CircuitBreakerSamplingSeconds": 30,
    "CircuitBreakerBreakSeconds": 5,
    "Resilience": true,
    "OutboundGuard": {
      "Enabled": false,
      "AllowedHosts": [".internal.example.com"],
      "AllowedNetworks": ["10.20.0.0/16"]
    }
  }
}
```

### Program.cs

```csharp
using Tooark.Http.Injections;

// Reads the HttpClient section
builder.Services.AddTooarkHttpClient(builder.Configuration);

// Or without reading the configuration, only with adjustments in code
builder.Services.AddTooarkHttpClient(options =>
{
  options.BaseAddress = "https://api.example.com/v1";
  options.RetryCount = 2;
});
```

With both configuration and code, the code adjustment applies on top of the section. The options are validated at
registration. Only the first registration counts: the handlers of a named client accumulate, and a second
registration would stack another resilience handler and multiply the retries.

The client is the named client `HttpClientService.ClientName` (`"Tooark"`). Anything else it needs goes through the
standard `IHttpClientFactory` API, after the registration:

```csharp
using Tooark.Http;

builder.Services
  .AddHttpClient(HttpClientService.ClientName)
  .ConfigureHttpClient(client => client.DefaultRequestHeaders.Add("X-Api-Key", apiKey))
  .AddHttpMessageHandler<MyAuthenticationHandler>();
```

A handler added this way sits inside the resilience handler, so it runs once per attempt.

### Client options — `HttpClient`

| Property                          | Type     | Default | Description                                                                                               |
| --------------------------------- | -------- | ------- | --------------------------------------------------------------------------------------------------------- |
| `BaseAddress`                     | `string` | none    | Absolute `http` or `https` URL, without credentials, query string or fragment. Required for relative URIs |
| `TotalTimeoutSeconds`             | `int`    | `30`    | Time limit of the whole call, all attempts and the body read included. From 1 to 3600                     |
| `AttemptTimeoutSeconds`           | `int`    | `10`    | Time limit of each attempt, up to the response headers. From 1 to `TotalTimeoutSeconds`                   |
| `RetryCount`                      | `int`    | `3`     | Retries after the first attempt. From 0 to 10; `0` turns retries off                                      |
| `RetryDelayMilliseconds`          | `int`    | `2000`  | Base delay between attempts; it grows exponentially, with jitter. From 0 to 60000                         |
| `CircuitBreakerFailureRatio`      | `double` | `0.1`   | Failure ratio, from 0 to 1, that opens the circuit within the sampling window                             |
| `CircuitBreakerMinimumThroughput` | `int`    | `100`   | Minimum number of requests in the window before the ratio is evaluated. At least 2                        |
| `CircuitBreakerSamplingSeconds`   | `int`    | `30`    | Sampling window. From twice `AttemptTimeoutSeconds` to 86400                                              |
| `CircuitBreakerBreakSeconds`      | `int`    | `5`     | How long the circuit stays open before the service is tried again. From 1 to 86400                        |
| `Resilience`                      | `bool`   | `true`  | Registers the resilience handler. With `false`, only `TotalTimeoutSeconds` still applies                  |
| `OutboundGuard`                   | object   | —       | Options of the [outbound guard](#-outbound-guard), below                                                  |

### Guard options — `HttpClient:OutboundGuard`

| Property          | Type       | Default | Description                                                                                                          |
| ----------------- | ---------- | ------- | -------------------------------------------------------------------------------------------------------------------- |
| `Enabled`         | `bool`     | `false` | Turns the guard on for the `Tooark` client. It is off by default because this client usually calls internal services |
| `AllowedHosts`    | `string[]` | none    | Host names exempt from the address check: an exact name, or a suffix starting with a dot (`.internal.example.com`)   |
| `AllowedNetworks` | `string[]` | none    | CIDR blocks, IPv4 or IPv6, exempt from the block list (`10.20.0.0/16`, `fd00:1::/64`)                                |

---

## 🌐 Service

`IHttpClientService` is registered as a singleton. Request bodies are serialized and response bodies are read with
`System.Text.Json` and `JsonSerializerDefaults.Web`: camelCase names, case-insensitive reading, numbers accepted as
strings.

| Method                                       | Request body | Response body |
| -------------------------------------------- | ------------ | ------------- |
| `GetAsync<TResponse>(uri)`                   | —            | JSON          |
| `PostAsync<TRequest, TResponse>(uri, body)`  | JSON         | JSON          |
| `PostAsync<TRequest>(uri, body)`             | JSON         | not read      |
| `PutAsync<TRequest, TResponse>(uri, body)`   | JSON         | JSON          |
| `PutAsync<TRequest>(uri, body)`              | JSON         | not read      |
| `PatchAsync<TRequest, TResponse>(uri, body)` | JSON         | JSON          |
| `PatchAsync<TRequest>(uri, body)`            | JSON         | not read      |
| `DeleteAsync(uri)`                           | —            | not read      |
| `DeleteAsync<TResponse>(uri)`                | —            | JSON          |
| `SendAsync(HttpRequestMessage)`              | the caller's | raw response  |

Every method takes an optional `CancellationToken` as the last parameter.

```csharp
using Tooark.Http.Interfaces;

public sealed record CustomerDto(int Id, string Name);
public sealed record CreateCustomerDto(string Name);
public sealed record RenameCustomerDto(string Name);

public sealed class CustomerGateway(IHttpClientService http)
{
  // GET: null when the response is 204 or has no body
  public Task<CustomerDto?> GetAsync(int id, CancellationToken cancellationToken) =>
    http.GetAsync<CustomerDto>($"customers/{id}", cancellationToken);

  // POST with a response body
  public Task<CustomerDto?> CreateAsync(CreateCustomerDto customer, CancellationToken cancellationToken) =>
    http.PostAsync<CreateCustomerDto, CustomerDto>("customers", customer, cancellationToken);

  // PUT without reading the response
  public Task ReplaceAsync(CustomerDto customer, CancellationToken cancellationToken) =>
    http.PutAsync($"customers/{customer.Id}", customer, cancellationToken);

  // PATCH with a response body
  public Task<CustomerDto?> RenameAsync(int id, RenameCustomerDto rename, CancellationToken cancellationToken) =>
    http.PatchAsync<RenameCustomerDto, CustomerDto>($"customers/{id}", rename, cancellationToken);

  // DELETE
  public Task RemoveAsync(int id, CancellationToken cancellationToken) =>
    http.DeleteAsync($"customers/{id}", cancellationToken);
}
```

The rules of the JSON methods:

- **Request body**: serialized before sending, as `application/json; charset=utf-8` with `Content-Length`, so a retry
  sends the same bytes again.
- **Response body**: a `204` or an empty body returns `default` (`null` for a reference type). A body that is not
  valid JSON for `TResponse` throws `BadGatewayException` with `Http.InvalidResponse`. The methods without
  `TResponse` do not read the body.
- **Error status**: any status outside 2xx throws [`HttpRequestFailedException`](#-error-handling).

### Request URI and base address

- An **absolute** URI is used as it is and must be `http` or `https`; anything else throws `BadRequestException`
  with `Http.RequestUriInvalid`, because an absolute URI may have come from the user.
- A **relative** URI is combined with `BaseAddress`. Without a base address it throws
  `InternalServerErrorException` with `Http.BaseAddressNotConfigured`.
- A base address with a path gets the missing trailing slash: with `https://api.example.com/v1`, `customers/7` goes
  to `/v1/customers/7`. A relative URI that **starts with a slash** replaces the whole path: `/customers/7` goes to
  `/customers/7`, without `/v1`.

### `SendAsync`: the escape hatch

For what the verb methods do not cover (custom headers, a body that is not JSON, reading the status), build the
`HttpRequestMessage` and call `SendAsync`. The request goes through the same client, with the resilience and the
guard, and the response comes back with its body already loaded in memory. An error status does **not** throw
here; transport failures (timeout, open circuit, connection, blocked destination) do, as in the other methods. The
caller disposes the response.

```csharp
using var request = new HttpRequestMessage(HttpMethod.Head, $"files/{id}");
request.Headers.Add("X-Request-Source", "billing");

using var response = await http.SendAsync(request, cancellationToken);

var exists = response.StatusCode == HttpStatusCode.OK;
```

---

## 🔁 Resilience

With `Resilience` on (the default), the client gets the **standard resilience handler** of
`Microsoft.Extensions.Http.Resilience`, configured from the options. Its strategies, from the outside in:

| Strategy            | Default                                                     | Option                                 |
| ------------------- | ----------------------------------------------------------- | -------------------------------------- |
| Concurrency limiter | 1000 simultaneous requests, no queue                        | not configurable here                  |
| Total timeout       | 30 s                                                        | `TotalTimeoutSeconds`                  |
| Retry               | 3 retries, exponential from 2 s, jitter                     | `RetryCount`, `RetryDelayMilliseconds` |
| Circuit breaker     | 10% failures of at least 100 requests in 30 s; open for 5 s | `CircuitBreaker*`                      |
| Attempt timeout     | 10 s                                                        | `AttemptTimeoutSeconds`                |

What you need to know to tune it:

- **What is retried**: a connection failure, an attempt timeout and the responses `408`, `429` and `5xx`. A
  `Retry-After` header of the response is honored. The other `4xx` are not retried.
- **Every verb is retried, POST and PATCH included.** That is the behavior of the standard handler. If the called
  service is not idempotent, set `RetryCount` to `0`, or make the operation idempotent (an idempotency key, for
  instance).
- **Timeouts**: the attempt timeout and the handler's total timeout end at the response headers. The service
  applies `TotalTimeoutSeconds` again to the whole call, so a body that stops arriving is also cut. Either way the
  failure is `GatewayTimeoutException`.
- **One circuit per client.** The circuit counts the failures of the `Tooark` client as a whole, not per host. It is
  the right grain for a client with a `BaseAddress`. If the same client calls many unrelated hosts (webhooks of
  different customers), the failures of one can open the circuit for all: raise `CircuitBreakerFailureRatio` and
  `CircuitBreakerMinimumThroughput`, or use a dedicated client per destination.
- **Valid combinations**: `AttemptTimeoutSeconds` cannot exceed `TotalTimeoutSeconds`, and
  `CircuitBreakerSamplingSeconds` must be at least twice `AttemptTimeoutSeconds`. Raising the attempt timeout above
  15 s therefore requires raising the sampling window too. Both are checked at registration.
- **Turning it off**: `Resilience: false` removes the handler. `TotalTimeoutSeconds` still applies, through
  `HttpClient.Timeout` and the service.

```csharp
builder.Services.AddTooarkHttpClient(builder.Configuration, options =>
{
  // A slow report endpoint that must not be called twice
  options.TotalTimeoutSeconds = 120;
  options.AttemptTimeoutSeconds = 120;
  options.CircuitBreakerSamplingSeconds = 240;
  options.RetryCount = 0;
});
```

---

## 🚨 Error Handling

Every failure of a call becomes a Tooark exception. With `Tooark.AspNetCore`, the exception handler turns it into
the Problem Details response of the application with the status below.

| Failure                                         | Exception                                            | Status | Message                                                            |
| ----------------------------------------------- | ---------------------------------------------------- | ------ | ------------------------------------------------------------------ |
| The called service answers with an error status | `HttpRequestFailedException` (`BadGatewayException`) | 502    | the errors of the called service, or `Http.RequestFailed;{status}` |
| Timeout of an attempt or of the whole call      | `GatewayTimeoutException`                            | 504    | `Http.RequestTimeout`                                              |
| Open circuit                                    | `ServiceUnavailableException`                        | 503    | `Http.CircuitOpen`                                                 |
| Concurrency limit of the client reached         | `ServiceUnavailableException`                        | 503    | `Http.ConcurrencyLimitReached`                                     |
| Connection failure, no response                 | `BadGatewayException`                                | 502    | `Http.ConnectionFailed`                                            |
| Success response with invalid JSON              | `BadGatewayException`                                | 502    | `Http.InvalidResponse`                                             |
| Destination refused by the outbound guard       | `OutboundBlockedException` (`BadRequestException`)   | 400    | `Http.OutboundBlocked;{host}`                                      |
| Absolute URI that is not `http` or `https`      | `BadRequestException`                                | 400    | `Http.RequestUriInvalid`                                           |
| Relative URI without `BaseAddress`              | `InternalServerErrorException`                       | 500    | `Http.BaseAddressNotConfigured`                                    |
| Cancellation by the caller's token              | `OperationCanceledException`, untouched              | —      | —                                                                  |

The original exception (the `HttpRequestException`, the Polly rejection) is kept in `InnerException` for the log.

### Error responses of the called service

An error status of the called service is **not** the status of the application: a `404` or a `401` from another
service must not become a `404` or a `401` for whoever called the application. `HttpRequestFailedException` is
always a `502`, and exposes the original status in `UpstreamStatusCode` and the request URI in `RequestUri`.

```csharp
using System.Net;
using Tooark.Http.Exceptions;
using Tooark.Http.Interfaces;

public sealed class CustomerGateway(IHttpClientService http)
{
  public async Task<CustomerDto?> FindAsync(int id, CancellationToken cancellationToken)
  {
    try
    {
      return await http.GetAsync<CustomerDto>($"customers/{id}", cancellationToken);
    }
    catch (HttpRequestFailedException exception) when (exception.UpstreamStatusCode == HttpStatusCode.NotFound)
    {
      // For this application, a customer that does not exist is not an error
      return null;
    }
  }
}
```

The notifications of the exception depend on the body of the error response:

- **Tooark Problem Details** — `Content-Type: application/problem+json` with the `errors` list of `code`, `key` and
  `message`, which is what `Tooark.AspNetCore` returns. Each entry becomes a `NotificationItem` with the same code,
  key and message, so a Tooark service that calls another keeps the original errors:

  ```json
  {
    "status": 400,
    "title": "Bad Request",
    "errors": [
      {
        "code": "T.VLD.STR1",
        "key": "Email",
        "message": "Field Email is required"
      },
      {
        "code": "T.VLD.NUM3",
        "key": "Age",
        "message": "Field Age must be greater than 18"
      }
    ]
  }
  ```

- **Any other body** — a single item with the message `Http.RequestFailed;{status}`. The raw body never goes into
  the message: it may carry internal details of the called service.

The error body is read up to 64 KB; a larger one is ignored. At most 50 errors are used, and each text is cut at
1024 characters. `RequestUri` may carry sensitive data in the query string: do not return it in a response.

---

## 🔒 Outbound Guard

When the application calls a URL **provided by the user** (a webhook, an avatar to import, a link to preview), the
user chooses where the server connects. Without a check, `http://169.254.169.254/` reads the cloud credentials of
the instance and `http://localhost:9200/` reaches a service that was never exposed. That is server-side request
forgery (SSRF).

The guard checks the **IP address at connection time**, not the text of the URL. It is the `ConnectCallback` of
`SocketsHttpHandler`: it resolves the name once, checks every address against the block list and connects to the
address it checked. A public name that resolves to an internal address is refused, and so is a name whose DNS
answers differently between the check and the connection (DNS rebinding), because there is no second resolution.

### What is blocked

| Range                                                                | What it is                                                             |
| -------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| `0.0.0.0/8`                                                          | "This network"; `0.0.0.0` reaches the host itself on several systems   |
| `127.0.0.0/8`, `::1`                                                 | Loopback                                                               |
| `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`                      | Private networks                                                       |
| `169.254.0.0/16`, `fe80::/10`                                        | Link-local, including the metadata address `169.254.169.254`           |
| `100.64.0.0/10`                                                      | Carrier-grade NAT, also used by internal cloud networks                |
| `fc00::/7`, `fec0::/10`                                              | IPv6 unique local and site-local                                       |
| `192.0.0.0/24`                                                       | IETF protocol assignments                                              |
| `192.0.2.0/24`, `198.51.100.0/24`, `203.0.113.0/24`, `2001:db8::/32` | Documentation                                                          |
| `198.18.0.0/15`                                                      | Benchmarking                                                           |
| `224.0.0.0/4`, `ff00::/8`                                            | Multicast                                                              |
| `240.0.0.0/4`                                                        | Reserved, including the broadcast `255.255.255.255`                    |
| `::/96`                                                              | Unspecified (`::`) and the deprecated IPv4-compatible addresses        |
| `64:ff9b:1::/48`, `100::/64`                                         | Local-use NAT64 and discard-only                                       |
| `::ffff:a.b.c.d`, `64:ff9b::/96`, `2002::/16`, `2001::/32`           | IPv4-mapped, NAT64, 6to4 and Teredo: classified by the IPv4 they carry |

The same rule is available as a pure function, `OutboundGuard.IsBlocked(IPAddress)`.

If **any** address a name resolves to is blocked, the whole host is refused: a name that mixes a public and an
internal address is the shape of a rebinding attack. The refusal is an `OutboundBlockedException`, a
`BadRequestException` with `Http.OutboundBlocked;{host}`. The message carries the host of the URL, never the
address it resolved to.

### Webhook example

Turn the guard on for the `Tooark` client when it sends to user-provided URLs:

```csharp
using Tooark.Http.Injections;
using Tooark.Http.Interfaces;

builder.Services.AddTooarkHttpClient(builder.Configuration, options => options.OutboundGuard.Enabled = true);

public sealed record OrderPaidEvent(int OrderId, decimal Amount);

public sealed class WebhookSender(IHttpClientService http)
{
  // The URL was registered by the customer: the guard refuses it when it leads to the internal network
  public Task SendAsync(string webhookUrl, OrderPaidEvent payload, CancellationToken cancellationToken) =>
    http.PostAsync(webhookUrl, payload, cancellationToken);
}
```

Or keep the `Tooark` client for internal services and guard a client of the application with
`AddTooarkOutboundGuard`, where the guard is on by default:

```csharp
using Tooark.Http.Injections;

var webhooks = builder.Services.AddHttpClient("webhooks");

// Resilience first, guard after: the refusal is not retried and does not count in the circuit
webhooks.AddStandardResilienceHandler();
webhooks.AddTooarkOutboundGuard();
```

On such a client, `HttpClient` throws the `OutboundBlockedException` itself. Call `AddTooarkOutboundGuard`
**after** the resilience handler and after any configuration of the primary handler: a resilience handler added
later would treat the refusal as a transient failure, and a `ConfigurePrimaryHttpMessageHandler` that replaces the
handler later removes the connection check.

The guard needs a `SocketsHttpHandler` as the primary handler. One that the application already configured is kept,
with only its `ConnectCallback` replaced. A primary handler of another type, such as an `HttpClientHandler` with
client certificates, is **replaced** by a new `SocketsHttpHandler` and its settings are lost: configure the
`SocketsHttpHandler` directly (`SslOptions`, `Proxy`, `PooledConnectionLifetime`) before adding the guard.

### Allow lists

```csharp
webhooks.AddTooarkOutboundGuard(options =>
{
  options.AllowedHosts = ["partner.internal.example.com", ".svc.cluster.local"];
  options.AllowedNetworks = ["10.20.0.0/16", "fd00:1::/64"];
});
```

- **`AllowedHosts`** — the name is trusted, whatever it resolves to. An exact name, or a suffix starting with a
  dot: `.internal.example.com` allows `api.internal.example.com`, but not `internal.example.com` itself nor
  `evilinternal.example.com`. Case is ignored. Only names: an address goes into `AllowedNetworks`.
- **`AllowedNetworks`** — CIDR blocks exempt from the block list. A single address is written with `/32` or `/128`,
  and the base address must be the network's (`10.20.0.1/16` is rejected). An allowed IPv4 network also covers the
  IPv4-mapped form (`::ffff:10.20.0.5`). NAT64, 6to4 and Teredo addresses are blocked by the IPv4 they carry, but
  are **not** allowed by it: allow their own IPv6 block if you need them.

An invalid entry fails at registration.

### Redirects

`SocketsHttpHandler` follows redirects by itself, and each hop to another host opens its own connection, which goes
through the same check. A destination that passes and then redirects to `http://169.254.169.254/` is refused at the
second hop.

### With an outbound proxy

With a proxy (the `HTTP_PROXY`/`HTTPS_PROXY` variables or `SocketsHttpHandler.Proxy`), the connection is opened
**to the proxy**, and the proxy resolves the destination name. The connection check then sees only the proxy
address. What happens in that case:

- **Covered**: an IP literal in the request URL (`http://10.0.0.5/`, `http://[::1]/`, `http://2130706433/`), which
  a request handler checks before the connection.
- **Not covered**: a name that resolves to an internal address, and the hops of a redirect. Only the proxy sees
  them. Block the internal ranges in the proxy's own rules, and consider turning `AllowAutoRedirect` off.
- **The proxy itself**: an internal proxy is a blocked address. Allow it by name in `AllowedHosts`; allowing its
  network in `AllowedNetworks` would also allow IP literals in that network.

### What the guard does not protect against

- **Calls that do not go through a guarded client**: another `HttpClient`, a database driver, an SMTP or SDK
  client.
- **Public destinations.** The guard does not know which public hosts the application should talk to. An allowlist
  of destinations per customer is the application's rule.
- **Hosts in `AllowedHosts`**: they are not checked at all.
- **The response.** The guard does not limit the size or the content of what the destination returns, and the
  service buffers a success body in memory. Do not return the response of a user-provided URL to the user as it is.
- **HTTP/3.** A QUIC connection does not go through `ConnectCallback`, so a guarded client keeps requests on
  HTTP/1.1 or HTTP/2.
- **Ports.** Any port of an allowed address is reachable.

---

## 📡 Observability

Traces and metrics of the calls come from
[`Tooark.Observability`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Observability/README.md), with no
code in this package and no reference between the two: `AddTooarkObservability` registers the OpenTelemetry
`HttpClient` instrumentation for tracing and for metrics, and that instrumentation covers every `HttpClient` of the
process, the ones created by `IHttpClientFactory` included.

- **Traces**: one client span per attempt, so the retries of a call appear as sibling spans. The `traceparent`
  header is propagated, and a called service that also uses `Tooark.Observability` continues the same trace. The
  query string is removed from the span attributes by default.
- **Metrics**: `http.client.request.duration` and the other `http.client.*` instruments of .NET.
- **Resilience events** (retries, circuit state, timeouts) are published by Polly on the `Polly` meter. To export
  them, add the meter to the observability options:

  ```json
  { "Observability": { "Metrics": { "AdditionalMeters": ["Polly"] } } }
  ```

Without `Tooark.Observability` the package works the same way, only without telemetry.

---

## 📝 Usage Examples

### Calling another Tooark service

The errors of the called service reach the caller of the application with their original codes and keys, inside a
`502`:

```csharp
using Tooark.Http.Interfaces;

public sealed record CreateInvoiceDto(int CustomerId, decimal Amount);
public sealed record InvoiceDto(int Id, string Number);

public sealed class BillingGateway(IHttpClientService http)
{
  // A validation error of the billing service becomes an HttpRequestFailedException with the same notifications
  public Task<InvoiceDto?> CreateAsync(CreateInvoiceDto invoice, CancellationToken cancellationToken) =>
    http.PostAsync<CreateInvoiceDto, InvoiceDto>("invoices", invoice, cancellationToken);
}
```

### Reporting a refused webhook URL when it is registered

```csharp
using System.Net;
using Tooark.Http;

public static bool IsLiteralAllowed(Uri webhookUrl) =>
  !IPAddress.TryParse(webhookUrl.IdnHost, out var address) || !OutboundGuard.IsBlocked(address);
```

This is only an early message for the user. A name is not resolved here: the check that protects the application
is the one at connection time.

### Without dependency injection

The service has a public constructor, for tools and tests. It needs an `IHttpClientFactory`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tooark.Http.Injections;
using Tooark.Http.Interfaces;

using var provider = new ServiceCollection()
  .AddTooarkHttpClient(options => options.BaseAddress = "https://api.example.com")
  .BuildServiceProvider();

var http = provider.GetRequiredService<IHttpClientService>();
```

In tests of the application, mock `IHttpClientService`, or replace the primary handler of the named client:

```csharp
services.AddHttpClient(HttpClientService.ClientName).ConfigurePrimaryHttpMessageHandler(() => fakeHandler);
```

---

## 📋 Dependencies

| Package                                                                                                       | Version  | Description                                               |
| ------------------------------------------------------------------------------------------------------------- | -------- | --------------------------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                       | 4.x      | Exceptions and, through it, `NotificationItem`            |
| [`Microsoft.Extensions.Http.Resilience`](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience) | 10.x     | Standard resilience handler (Polly), `IHttpClientFactory` |

`Tooark.Observability` is **not** a dependency: see [Observability](#-observability).

---

## 🎯 Best Practices

1. **Inject `IHttpClientService`, never create an `HttpClient`** — the factory rotates the handlers and keeps the
   connections; a `new HttpClient()` per call exhausts sockets, and a static one never sees a DNS change.
2. **Turn the guard on wherever the URL comes from the user** — and keep it off, or with allow lists, for the client
   that calls internal services.
3. **Prefer `AllowedHosts` with exact names** — a suffix trusts every name under it, and a network trusts every
   address in it.
4. **Decide the retries per service** — POST and PATCH are retried like the other verbs. Use `RetryCount: 0` for a
   service that is not idempotent.
5. **Do not expose the upstream failure** — log `UpstreamStatusCode` and `RequestUri`; the response of the
   application is the `502` with the notifications.
6. **Pass the `CancellationToken` of the request** — the caller's cancellation stops the retries and is not
   reported as a timeout.
7. **Do not catch `Exception` around the calls** — the exceptions already carry the right status; catch
   `HttpRequestFailedException` only where an upstream status has a meaning for the application.

---

## ⚠️ Error Codes and Solutions

Configuration errors are `InternalServerErrorException` thrown **at registration** (`AddTooarkHttpClient`,
`AddTooarkOutboundGuard`) or when creating the service without dependency injection.

| Message                                                           | Description                                                                                       | Solution                                                              |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| `Options.HttpClient.BaseAddressInvalid`                           | `BaseAddress` is not an absolute `http`/`https` URL, or has credentials, query string or fragment | Use `https://host/path`; send credentials in a header                 |
| `Options.HttpClient.TotalTimeoutInvalid;{min};{max}`              | `TotalTimeoutSeconds` outside 1 to 3600                                                           | Use a value in the range                                              |
| `Options.HttpClient.AttemptTimeoutInvalid;{min};{max}`            | `AttemptTimeoutSeconds` below 1 or above `TotalTimeoutSeconds`                                    | Lower the attempt timeout or raise the total                          |
| `Options.HttpClient.RetryCountInvalid;{min};{max}`                | `RetryCount` outside 0 to 10                                                                      | Use a value in the range; `0` turns retries off                       |
| `Options.HttpClient.RetryDelayInvalid;{min};{max}`                | `RetryDelayMilliseconds` outside 0 to 60000                                                       | Use a value in the range                                              |
| `Options.HttpClient.CircuitBreakerFailureRatioInvalid`            | `CircuitBreakerFailureRatio` outside 0 to 1                                                       | Use a ratio, such as `0.1` for 10%                                    |
| `Options.HttpClient.CircuitBreakerMinimumThroughputInvalid;{min}` | `CircuitBreakerMinimumThroughput` below 2                                                         | Use 2 or more                                                         |
| `Options.HttpClient.CircuitBreakerSamplingInvalid;{min};{max}`    | `CircuitBreakerSamplingSeconds` below twice `AttemptTimeoutSeconds`, or above 86400               | Raise the window or lower the attempt timeout                         |
| `Options.HttpClient.CircuitBreakerBreakInvalid;{min};{max}`       | `CircuitBreakerBreakSeconds` outside 1 to 86400                                                   | Use a value in the range                                              |
| `Options.HttpClient.OutboundGuard.AllowedHostInvalid;{value}`     | Entry of `AllowedHosts` that is not a host name                                                   | Remove scheme, port, path and `*`; put addresses in `AllowedNetworks` |
| `Options.HttpClient.OutboundGuard.AllowedNetworkInvalid;{value}`  | Entry of `AllowedNetworks` that is not a CIDR block with the network address                      | Use `10.20.0.0/16`; a single address takes `/32` or `/128`            |

Call errors are thrown by the service:

| Message                         | Exception                      | Description                                                            | Solution                                                                |
| ------------------------------- | ------------------------------ | ---------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| `Http.RequestFailed;{status}`   | `HttpRequestFailedException`   | The called service answered with an error status                       | Check `UpstreamStatusCode` and the log of the called service            |
| `Http.RequestTimeout`           | `GatewayTimeoutException`      | No response within the attempt or the total timeout                    | Check the called service; adjust the timeouts if it is slow by design   |
| `Http.CircuitOpen`              | `ServiceUnavailableException`  | Too many failures: the circuit is open and calls are refused           | Wait for `CircuitBreakerBreakSeconds`; investigate the called service   |
| `Http.ConcurrencyLimitReached`  | `ServiceUnavailableException`  | More than 1000 simultaneous requests on the client                     | Reduce the concurrency of the calls                                     |
| `Http.ConnectionFailed`         | `BadGatewayException`          | Connection refused, name not resolved, TLS failure, connection dropped | Check the address, the DNS and the network                              |
| `Http.InvalidResponse`          | `BadGatewayException`          | The success body is not valid JSON for the response type               | Check the contract and the DTO                                          |
| `Http.OutboundBlocked;{host}`   | `OutboundBlockedException`     | The guard refused the destination                                      | Fix the URL; for a legitimate internal destination, use the allow lists |
| `Http.RequestUriInvalid`        | `BadRequestException`          | The request URI is not a valid `http` or `https` URI                   | Validate the URL before calling                                         |
| `Http.BaseAddressNotConfigured` | `InternalServerErrorException` | Relative URI and no `BaseAddress`                                      | Configure `HttpClient:BaseAddress` or pass an absolute URI              |

---

## 🤝 Contributing

Contributions are welcome! Start with
[CONTRIBUTING.md](https://github.com/Tooark/nuget-tooark/blob/main/CONTRIBUTING.md) — it covers the development
workflow, the coding and commit conventions and the pull request checklist. Bugs and feature requests go through
the [issue templates](https://github.com/Tooark/nuget-tooark/issues/new/choose) of the
[Tooark](https://github.com/Tooark/nuget-tooark) repository.

By participating you agree to the
[Code of Conduct](https://github.com/Tooark/nuget-tooark/blob/main/CODE_OF_CONDUCT.md).

---

## 🆘 Help & Security

- ❓ **Questions, bugs, feature ideas** — see
  [SUPPORT.md](https://github.com/Tooark/nuget-tooark/blob/main/SUPPORT.md) for the right channel
- 🔒 **Security vulnerabilities** — do **not** open a public issue; follow
  [SECURITY.md](https://github.com/Tooark/nuget-tooark/blob/main/SECURITY.md)

---

## 💖 Support

If Tooark helps your projects, consider supporting its development:

- 💙 [GitHub Sponsors](https://github.com/sponsors/paulosfjunior)
- ☕ [Ko-fi](https://ko-fi.com/paulosfjunior)

Every contribution helps keep the project maintained and improving. Thank you! 🙏

---

## 📄 License

This project is licensed under the [BSD 3-Clause License](https://github.com/Tooark/nuget-tooark/blob/main/LICENSE).
