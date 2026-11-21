# Tooark.Http

Biblioteca para HTTP de saída em projetos .NET: um serviço JSON fino sobre o `IHttpClientFactory`, com resiliência, erros mapeados para as exceções do Tooark e uma guarda de saída contra SSRF.

O serviço não substitui o `HttpClient` nem reimplementa o pipeline dele: o cliente é criado pelo
`IHttpClientFactory`, as novas tentativas, o circuito e os tempos limite vêm do
[Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/dotnet/core/resilience/http-resilience), e a
guarda é um `SocketsHttpHandler.ConnectCallback`. O pacote acrescenta as convenções de JSON, as opções no padrão do
Tooark e a conversão de cada falha em uma exceção que carrega `NotificationItem`s.

📖 **Documentação:** [Tooark.Http no site](https://tooark.com/nuget-tooark/pt-BR/packages/tooark.http.html) · [Todos os pacotes](https://tooark.com/nuget-tooark/pt-BR/) · [Referência da API](https://tooark.com/nuget-tooark/pt-BR/api/index.html)

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Http/README.md) · 🇧🇷 **Português (este arquivo)**

---

## 📑 Conteúdo

- [Visão Geral](#-visão-geral)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Serviço](#-serviço)
- [Resiliência](#-resiliência)
- [Tratamento de Erros](#-tratamento-de-erros)
- [Guarda de Saída](#-guarda-de-saída)
- [Observabilidade](#-observabilidade)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Boas Práticas](#-boas-práticas)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuindo](#-contribuindo)
- [Ajuda & Segurança](#-ajuda--segurança)
- [Apoie](#-apoie)
- [Licença](#-licença)

---

## 📖 Visão Geral

| Peça                         | O que faz                                                                                   |
| ---------------------------- | ------------------------------------------------------------------------------------------- |
| `IHttpClientService`         | GET, POST, PUT, PATCH e DELETE com JSON; `SendAsync` e `SendStreamingAsync` para o resto    |
| `AddTooarkHttpClient`        | Registra o serviço e o cliente padrão, `Tooark`, com a resiliência configurada pelas opções |
| `AddTooarkNamedHttpClient`   | Registra um cliente com nome e as opções dele, com o serviço como serviço com chave         |
| `HttpClientServiceExtension` | Os métodos por verbo com ajuste da requisição por chamada, como um cabeçalho                |
| `AddTooarkOutboundGuard`     | Adiciona a guarda de saída a qualquer cliente nomeado ou tipado da aplicação                |
| `OutboundGuard.IsBlocked`    | A lista de bloqueio como função pura, para validações da própria aplicação                  |
| `HttpRequestFailedException` | A resposta de erro do serviço chamado, com o status original, os erros e o corpo dela       |
| `OutboundBlockedException`   | O destino recusado pela guarda                                                              |

- **Camada fina**: sem `new HttpClient()` e sem pipeline próprio. O cliente nomeado é pedido ao
  `IHttpClientFactory` a cada chamada, então a rotação dos handlers e a atualização do DNS continuam funcionando.
- **Falhas no padrão do Tooark**: um status de erro, um tempo limite, um circuito aberto, uma falha de conexão e um
  destino bloqueado viram, cada um, uma exceção do Tooark com uma chave de mensagem.
- **Falha no startup**: opções inválidas são recusadas no registro, inclusive as combinações que o handler de
  resiliência só recusaria na primeira requisição.
- **Sem estado por chamada**: o serviço é um singleton e seguro para uso concorrente.

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Http
```

O pacote roda no runtime base do .NET, sem o ASP.NET Core. Ele também vem no agregador `Tooark`, mas o
`AddTooarkService` não registra o cliente: chame o `AddTooarkHttpClient` na aplicação.

---

## ⚙️ Configuração

### appsettings.json

Tudo é opcional. O exemplo mostra todas as opções com o valor padrão, menos o endereço base e as listas da guarda:

```json
{
  "HttpClient": {
    "BaseAddress": "https://api.exemplo.com/v1",
    "TotalTimeoutSeconds": 30,
    "AttemptTimeoutSeconds": 10,
    "RetryCount": 3,
    "RetryNonIdempotentMethods": true,
    "RetryDelayMilliseconds": 2000,
    "CircuitBreakerFailureRatio": 0.1,
    "CircuitBreakerMinimumThroughput": 100,
    "CircuitBreakerSamplingSeconds": 30,
    "CircuitBreakerBreakSeconds": 5,
    "Resilience": true,
    "OutboundGuard": {
      "Enabled": false,
      "AllowedHosts": [".interna.exemplo.com"],
      "AllowedNetworks": ["10.20.0.0/16"]
    }
  }
}
```

### Program.cs

```csharp
using Tooark.Http.Injections;

// Lê a seção HttpClient
builder.Services.AddTooarkHttpClient(builder.Configuration);

// Ou sem ler a configuração, só com ajustes no código
builder.Services.AddTooarkHttpClient(options =>
{
  options.BaseAddress = "https://api.exemplo.com/v1";
  options.RetryCount = 2;
});
```

Com configuração e código, o ajuste do código vale por cima da seção. As opções são validadas no registro. Só o
primeiro registro do cliente padrão vale: os handlers de um cliente nomeado se acumulam, e um segundo registro
empilharia outro handler de resiliência e multiplicaria as novas tentativas. Para outro destino, com outras
opções, registre um [cliente com nome](#clientes-com-nome).

O cliente padrão é o cliente nomeado `HttpClientService.ClientName` (`"Tooark"`) do `IHttpClientFactory`. O que
mais ele precisar entra pela API padrão do `IHttpClientFactory`, depois do registro:

```csharp
using Tooark.Http;

builder.Services
  .AddHttpClient(HttpClientService.ClientName)
  .ConfigureHttpClient(client => client.DefaultRequestHeaders.Add("X-Api-Key", apiKey))
  .AddHttpMessageHandler<MeuHandlerDeAutenticacao>();
```

Um handler adicionado assim fica dentro do handler de resiliência, então roda uma vez por tentativa. Um cabeçalho
que muda a cada chamada não entra aqui: use os [métodos com ajuste da requisição](#cabeçalhos-por-chamada).

### Opções do cliente — `HttpClient`

| Propriedade                       | Tipo     | Padrão | Descrição                                                                                           |
| --------------------------------- | -------- | ------ | --------------------------------------------------------------------------------------------------- |
| `BaseAddress`                     | `string` | nenhum | URL absoluta `http` ou `https`, sem credenciais, consulta ou fragmento. Exigida para URIs relativas |
| `TotalTimeoutSeconds`             | `int`    | `30`   | Tempo limite da chamada: as tentativas e o corpo (no streaming, só até os cabeçalhos). De 1 a 3600  |
| `AttemptTimeoutSeconds`           | `int`    | `10`   | Tempo limite de cada tentativa, até os cabeçalhos da resposta. De 1 a `TotalTimeoutSeconds`         |
| `RetryCount`                      | `int`    | `3`    | Novas tentativas depois da primeira. De 0 a 10; `0` desliga as novas tentativas                     |
| `RetryNonIdempotentMethods`       | `bool`   | `true` | Tenta de novo também POST e PATCH; com `false`, só os demais verbos. Na v5 o padrão é `false`       |
| `RetryDelayMilliseconds`          | `int`    | `2000` | Espera base entre tentativas; cresce de forma exponencial, com variação aleatória. De 0 a 60000     |
| `CircuitBreakerFailureRatio`      | `double` | `0.1`  | Taxa de falhas, de 0 a 1, que abre o circuito dentro da janela de amostragem                        |
| `CircuitBreakerMinimumThroughput` | `int`    | `100`  | Volume mínimo de requisições na janela para a taxa ser avaliada. No mínimo 2                        |
| `CircuitBreakerSamplingSeconds`   | `int`    | `30`   | Janela de amostragem. Do dobro de `AttemptTimeoutSeconds` a 86400                                   |
| `CircuitBreakerBreakSeconds`      | `int`    | `5`    | Tempo em que o circuito fica aberto antes de o serviço ser testado de novo. De 1 a 86400            |
| `Resilience`                      | `bool`   | `true` | Registra o handler de resiliência. Com `false`, só o `TotalTimeoutSeconds` continua valendo         |
| `OutboundGuard`                   | objeto   | —      | Opções da [guarda de saída](#-guarda-de-saída), abaixo                                              |

A `RetryNonIdempotentMethods` existe desde a v4.11.0. **Na v5 o padrão dela passa a ser `false`**: uma falha de um
POST ou de um PATCH é devolvida na primeira tentativa, e os demais verbos continuam sendo tentados de novo. Para
manter o comportamento da 4.x depois da migração, informe `"RetryNonIdempotentMethods": true`; para antecipar o da
v5, informe `false` desde já. Os detalhes estão em [Resiliência](#-resiliência).

Um [cliente com nome](#clientes-com-nome) tem as mesmas opções, na seção `HttpClients:{nome}`.

### Opções da guarda — `HttpClient:OutboundGuard`

| Propriedade       | Tipo       | Padrão  | Descrição                                                                                                                  |
| ----------------- | ---------- | ------- | -------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`         | `bool`     | `false` | Liga a guarda no cliente. Vem desligada porque o cliente padrão em geral chama serviços internos                           |
| `AllowedHosts`    | `string[]` | nenhum  | Nomes de host liberados da conferência de endereço: um nome exato ou um sufixo iniciado por ponto (`.interna.exemplo.com`) |
| `AllowedNetworks` | `string[]` | nenhuma | Blocos CIDR, IPv4 ou IPv6, liberados da lista de bloqueio (`10.20.0.0/16`, `fd00:1::/64`)                                  |

Em um cliente com nome, a seção da guarda é `HttpClients:{nome}:OutboundGuard`.

### Clientes com nome

Desde a v4.11.0, o `AddTooarkNamedHttpClient` registra um cliente por destino, cada um com as próprias opções:
endereço base, tempos limite, novas tentativas, circuito e guarda de saída. As opções de cada cliente vêm da seção
`HttpClients:{nome}` (a constante `HttpClientOptions.NamedSection`), com as mesmas chaves da seção `HttpClient`. A
seção `HttpClients`, no plural, fica **ao lado** da seção `HttpClient`, e não dentro dela:

```json
{
  "HttpClient": {
    "BaseAddress": "https://api.exemplo.com/v1"
  },
  "HttpClients": {
    "modelos": {
      "BaseAddress": "https://modelos.exemplo.com/",
      "TotalTimeoutSeconds": 300,
      "AttemptTimeoutSeconds": 300,
      "CircuitBreakerSamplingSeconds": 600
    },
    "pagamentos": {
      "BaseAddress": "https://pagamentos.exemplo.com/v2",
      "RetryNonIdempotentMethods": false
    }
  }
}
```

```csharp
using Tooark.Http.Injections;

// O cliente padrão, da seção HttpClient
builder.Services.AddTooarkHttpClient(builder.Configuration);

// Cada cliente com nome lê a própria seção: HttpClients:modelos e HttpClients:pagamentos
builder.Services.AddTooarkNamedHttpClient("modelos", builder.Configuration);
builder.Services.AddTooarkNamedHttpClient("pagamentos", builder.Configuration, options => options.RetryCount = 1);

// Ou sem ler a configuração, só com ajustes no código
builder.Services.AddTooarkNamedHttpClient("relatorios", options =>
{
  options.BaseAddress = "https://relatorios.exemplo.com";
  options.RetryCount = 0;
});
```

O `IHttpClientService` de cada cliente é um serviço com chave (keyed service), e a chave é o nome:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tooark.Http.Interfaces;

public sealed record PerguntaDto(string Texto);
public sealed record RespostaDto(string Texto);

public sealed class ModeloGateway([FromKeyedServices("modelos")] IHttpClientService http)
{
  // Vai para https://modelos.exemplo.com/v1/respostas, com os tempos limite do cliente "modelos"
  public Task<RespostaDto?> PerguntarAsync(PerguntaDto pergunta, CancellationToken cancellationToken) =>
    http.PostAsync<PerguntaDto, RespostaDto>("v1/respostas", pergunta, cancellationToken);
}

// Ou direto do provedor de serviços
var pagamentos = provider.GetRequiredKeyedService<IHttpClientService>("pagamentos");
```

- **O cliente padrão não muda.** O `IHttpClientService` sem chave continua sendo o do cliente `Tooark`, e ele não
  precisa estar registrado para um cliente com nome funcionar.
- **O nome é também o do cliente no `IHttpClientFactory`.** Um cabeçalho fixo ou um handler de autenticação entra
  por `AddHttpClient("modelos")`, depois do registro, como no cliente padrão. Por isso, não use o nome de um cliente
  que a aplicação já registra com outra finalidade.
- **Nomes recusados**: o nome não pode ser vazio, ter `:` nem ser `Tooark`. O registro lança `ArgumentException`.
- **Só o primeiro registro de cada nome vale**, como no cliente padrão.
- **As opções são validadas no registro**, com as mesmas mensagens `Options.HttpClient.*` do cliente padrão, sem o
  nome do cliente.
- **Configuração estrita**: a seção `HttpClients:{nome}` de cada cliente registrado é conferida, como a seção
  `HttpClient`. Com `"Tooark": { "StrictConfiguration": true }`, uma chave desconhecida falha no registro com
  uma mensagem como `Options.Configuration.UnknownKey;HttpClients:modelos;TotalTimeout`. Veja a
  [configuração estrita](https://github.com/Tooark/nuget-tooark/blob/main/README.pt-BR.md#️-configuração-estrita).

---

## 🌐 Serviço

O `IHttpClientService` é registrado como singleton. O corpo das requisições é serializado e o das respostas é lido
com o `System.Text.Json` e o `JsonSerializerDefaults.Web`: nomes em camelCase, leitura sem diferenciar maiúsculas e
números aceitos como texto.

| Método                                       | Corpo da requisição | Corpo da resposta                   |
| -------------------------------------------- | ------------------- | ----------------------------------- |
| `GetAsync<TResponse>(uri)`                   | —                   | JSON                                |
| `PostAsync<TRequest, TResponse>(uri, body)`  | JSON                | JSON                                |
| `PostAsync<TRequest>(uri, body)`             | JSON                | não é lido                          |
| `PutAsync<TRequest, TResponse>(uri, body)`   | JSON                | JSON                                |
| `PutAsync<TRequest>(uri, body)`              | JSON                | não é lido                          |
| `PatchAsync<TRequest, TResponse>(uri, body)` | JSON                | JSON                                |
| `PatchAsync<TRequest>(uri, body)`            | JSON                | não é lido                          |
| `DeleteAsync(uri)`                           | —                   | não é lido                          |
| `DeleteAsync<TResponse>(uri)`                | —                   | JSON                                |
| `SendAsync(HttpRequestMessage)`              | o de quem chama     | resposta bruta                      |
| `SendStreamingAsync(HttpRequestMessage)`     | o de quem chama     | resposta bruta, com o corpo por ler |

Todo método recebe um `CancellationToken` opcional como último parâmetro. Os métodos por verbo têm também versões
que recebem um ajuste da requisição antes do token: veja [Cabeçalhos por chamada](#cabeçalhos-por-chamada).

```csharp
using Tooark.Http.Interfaces;

public sealed record ClienteDto(int Id, string Nome);
public sealed record CriarClienteDto(string Nome);
public sealed record RenomearClienteDto(string Nome);

public sealed class ClienteGateway(IHttpClientService http)
{
  // GET: nulo quando a resposta é 204 ou não tem corpo
  public Task<ClienteDto?> ObterAsync(int id, CancellationToken cancellationToken) =>
    http.GetAsync<ClienteDto>($"clientes/{id}", cancellationToken);

  // POST com corpo de resposta
  public Task<ClienteDto?> CriarAsync(CriarClienteDto cliente, CancellationToken cancellationToken) =>
    http.PostAsync<CriarClienteDto, ClienteDto>("clientes", cliente, cancellationToken);

  // PUT sem ler a resposta
  public Task SubstituirAsync(ClienteDto cliente, CancellationToken cancellationToken) =>
    http.PutAsync($"clientes/{cliente.Id}", cliente, cancellationToken);

  // PATCH com corpo de resposta
  public Task<ClienteDto?> RenomearAsync(int id, RenomearClienteDto nome, CancellationToken cancellationToken) =>
    http.PatchAsync<RenomearClienteDto, ClienteDto>($"clientes/{id}", nome, cancellationToken);

  // DELETE
  public Task RemoverAsync(int id, CancellationToken cancellationToken) =>
    http.DeleteAsync($"clientes/{id}", cancellationToken);
}
```

As regras dos métodos com JSON:

- **Corpo da requisição**: serializado antes do envio, como `application/json; charset=utf-8` com `Content-Length`,
  então uma nova tentativa envia os mesmos bytes de novo.
- **Corpo da resposta**: um `204` ou um corpo vazio devolve `default` (`null` em um tipo de referência). Um corpo
  que não é um JSON válido para `TResponse` lança `BadGatewayException` com `Http.InvalidResponse`. Os métodos sem
  `TResponse` não leem o corpo.
- **Status de erro**: qualquer status fora de 2xx lança [`HttpRequestFailedException`](#-tratamento-de-erros).

### URI da requisição e endereço base

- Uma URI **absoluta** é usada como está e precisa ser `http` ou `https`; qualquer outra lança
  `BadRequestException` com `Http.RequestUriInvalid`, porque uma URI absoluta pode ter vindo do usuário.
- Uma URI **relativa** é combinada com o `BaseAddress`. Sem endereço base, lança `InternalServerErrorException` com
  `Http.BaseAddressNotConfigured`.
- Um endereço base com caminho ganha a barra final que faltar: com `https://api.exemplo.com/v1`, `clientes/7` vai
  para `/v1/clientes/7`. Uma URI relativa que **começa com barra** substitui o caminho inteiro: `/clientes/7` vai
  para `/clientes/7`, sem o `/v1`.

### Cabeçalhos por chamada

Para um cabeçalho que muda a cada chamada (a autorização do usuário, uma chave de idempotência, um `Accept`
diferente), os métodos por verbo têm, desde a v4.11.0, versões que recebem um `Action<HttpRequestMessage>` logo
antes do token de cancelamento. São métodos de extensão do namespace `Tooark.Http.Extensions`, para `GetAsync`,
`PostAsync`, `PutAsync`, `PatchAsync` e `DeleteAsync`:

```csharp
using System.Net.Http.Headers;
using Tooark.Http.Extensions;
using Tooark.Http.Interfaces;

public sealed record PedidoDto(int ProdutoId, int Quantidade);
public sealed record ReciboDto(string Protocolo);

public sealed class PedidoGateway(IHttpClientService http)
{
  // A autorização é a do usuário da chamada, e não uma fixa do cliente
  public Task<ReciboDto?> CriarAsync(PedidoDto pedido, string tokenDoUsuario, CancellationToken cancellationToken) =>
    http.PostAsync<PedidoDto, ReciboDto>(
      "pedidos",
      pedido,
      request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenDoUsuario),
      cancellationToken);

  // A chave de idempotência deixa o serviço chamado reconhecer uma nova tentativa do mesmo pedido
  public Task ConfirmarAsync(PedidoDto pedido, Guid chave, CancellationToken cancellationToken) =>
    http.PostAsync(
      "pedidos/confirmacoes",
      pedido,
      request => request.Headers.Add("Idempotency-Key", chave.ToString()),
      cancellationToken);
}
```

- **Mesmo comportamento dos métodos da interface**: JSON na ida e na volta, `HttpRequestFailedException` no status
  de erro e as falhas de transporte nas exceções do Tooark.
- **A ação roda uma vez**, antes da primeira tentativa, e as novas tentativas reenviam a mesma requisição. Um valor
  que precisa mudar a cada tentativa vai em um handler do cliente, que roda uma vez por tentativa.
- **Cabeçalho igual em todas as chamadas**: configure o cliente, com o `ConfigureHttpClient` mostrado na seção de
  configuração.
- **Outra implementação do `IHttpClientService`** (um substituto de teste, um decorador): a chamada passa pelo
  `SendAsync` dela, com o mesmo JSON e a mesma conversão dos erros.

### `SendAsync`: a saída para o resto

Para o que os métodos por verbo não cobrem (um corpo que não é JSON, a leitura do status, outro verbo), monte o
`HttpRequestMessage` e chame o `SendAsync`. A requisição passa pelo mesmo cliente, com a resiliência e a guarda, e
a resposta volta com o corpo já carregado em memória. Um status de erro **não** lança aqui; as falhas de transporte
(tempo limite, circuito aberto, conexão, destino bloqueado) lançam, como nos demais métodos. Quem chama descarta a
resposta. Só para incluir um cabeçalho em uma chamada com JSON, prefira os métodos de
[Cabeçalhos por chamada](#cabeçalhos-por-chamada), que mantêm a leitura do JSON e a conversão do status de erro.

```csharp
using var request = new HttpRequestMessage(HttpMethod.Head, $"arquivos/{id}");
request.Headers.Add("X-Origem", "faturamento");

using var response = await http.SendAsync(request, cancellationToken);

var existe = response.StatusCode == HttpStatusCode.OK;
```

### `SendStreamingAsync`: a resposta em streaming

Desde a v4.11.0, o `SendStreamingAsync` devolve a resposta assim que os cabeçalhos chegam, sem carregar o corpo em
memória. É o envio para respostas longas ou contínuas: eventos enviados pelo servidor (`text/event-stream`), a
resposta de um modelo de linguagem em pedaços, um arquivo grande. A requisição passa pelo mesmo cliente, com a
resiliência e a guarda, e, como no `SendAsync`, um status de erro **não** lança.

```csharp
using System.Net.Http.Headers;
using Tooark.Http.Exceptions;

using var request = new HttpRequestMessage(HttpMethod.Get, "eventos");
request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

// O token vale até os cabeçalhos; quem chama descarta a resposta
using var response = await http.SendStreamingAsync(request, cancellationToken);

if (!response.IsSuccessStatusCode)
{
  // O status de erro não lança: a decisão é de quem chama
  throw new HttpRequestFailedException(response.StatusCode, response.RequestMessage?.RequestUri);
}

// O prazo da leitura do corpo é o do token passado a ela, e não o tempo limite da chamada
await using var corpo = await response.Content.ReadAsStreamAsync(cancellationToken);
using var reader = new StreamReader(corpo);

while (await reader.ReadLineAsync(cancellationToken) is { } linha)
{
  // Cada linha chega assim que o serviço chamado a envia
}
```

- **Tempo limite**: o `TotalTimeoutSeconds` e o token passado ao método valem até os cabeçalhos da resposta. A
  leitura do corpo pode durar mais que o tempo limite da chamada, e nada a corta sozinho: controle o prazo dela com
  o próprio token, passado ao `ReadAsStreamAsync` e a cada leitura.
- **Descarte**: quem chama descarta a resposta, o que devolve a conexão, inclusive quando desiste do corpo no meio.
- **Falhas**: até os cabeçalhos, valem as novas tentativas e as exceções do Tooark (tempo limite, circuito aberto,
  conexão, destino bloqueado), como nos demais métodos. Uma falha **durante a leitura do corpo** chega como a
  exceção do transporte (`IOException`, `HttpRequestException`), e não como exceção do Tooark, e não é tentada de
  novo.
- **Outras implementações**: o método tem implementação padrão na interface. Uma implementação própria do
  `IHttpClientService`, escrita antes dele, continua compilando e o atende pelo `SendAsync`, com o corpo já
  carregado.

---

## 🔁 Resiliência

Com `Resilience` ligado (o padrão), o cliente recebe o **handler padrão de resiliência** do
`Microsoft.Extensions.Http.Resilience`, configurado pelas opções. As estratégias dele, de fora para dentro:

| Estratégia                 | Padrão                                                              | Opção                                                               |
| -------------------------- | ------------------------------------------------------------------- | ------------------------------------------------------------------- |
| Limite de concorrência     | 1000 requisições simultâneas, sem fila                              | não configurável aqui                                               |
| Tempo limite total         | 30 s                                                                | `TotalTimeoutSeconds`                                               |
| Novas tentativas           | 3, com espera exponencial a partir de 2 s e variação aleatória      | `RetryCount`, `RetryDelayMilliseconds`, `RetryNonIdempotentMethods` |
| Circuito                   | 10% de falhas em pelo menos 100 requisições em 30 s; aberto por 5 s | `CircuitBreaker*`                                                   |
| Tempo limite por tentativa | 10 s                                                                | `AttemptTimeoutSeconds`                                             |

O que é preciso saber para ajustar:

- **O que é tentado de novo**: a falha de conexão, o tempo limite da tentativa e as respostas `408`, `429` e `5xx`.
  O cabeçalho `Retry-After` da resposta é respeitado. Os demais `4xx` não são tentados de novo.
- **POST e PATCH também são tentados de novo, por padrão.** É o comportamento do handler padrão, e o da 4.x. Os
  dois verbos não são idempotentes: a nova tentativa pode repetir o efeito de uma chamada que o serviço chamado já
  tinha processado (um pedido criado duas vezes, uma cobrança feita duas vezes). Desde a v4.11.0,
  `RetryNonIdempotentMethods: false` desliga a nova tentativa só dos dois: eles não são repetidos nem em uma
  resposta `5xx` nem em uma falha de conexão. GET, PUT e DELETE continuam sendo tentados de novo, e a falha do POST
  ou do PATCH segue contando para o circuito. A opção olha o verbo da requisição, então vale também para o
  `SendAsync`, o `SendStreamingAsync` e os métodos com ajuste da requisição; com `RetryCount: 0` ou
  `Resilience: false` nada é tentado de novo, e ela não tem efeito. A alternativa é tornar a operação idempotente,
  com uma chave de idempotência enviada [por chamada](#cabeçalhos-por-chamada).
- **Na v5, POST e PATCH deixam de ser tentados de novo por padrão**: `RetryNonIdempotentMethods` passa a valer
  `false` quando não informada. Para manter o comportamento da 4.x, informe `"RetryNonIdempotentMethods": true`;
  para antecipar o da v5, informe `false` desde já.
- **Tempos limite**: o tempo limite por tentativa e o total do handler terminam nos cabeçalhos da resposta. O
  serviço aplica o `TotalTimeoutSeconds` de novo à chamada inteira, então um corpo que para de chegar também é
  cortado. Nos dois casos a falha é `GatewayTimeoutException`. A exceção é o
  [`SendStreamingAsync`](#sendstreamingasync-a-resposta-em-streaming): nele o tempo limite termina nos cabeçalhos,
  e o prazo da leitura do corpo é de quem chama.
- **Um circuito por cliente.** O circuito conta as falhas de um cliente como um todo, e não por host. É o grão
  certo para um cliente com `BaseAddress`: registre um [cliente com nome](#clientes-com-nome) para cada destino
  conhecido, e as falhas de um destino não abrem o circuito dos outros. Se o mesmo cliente chama muitos hosts sem
  relação entre si (webhooks de clientes diferentes), as falhas de um podem abrir o circuito para todos: aumente
  `CircuitBreakerFailureRatio` e `CircuitBreakerMinimumThroughput`, e mantenha esse tráfego em um cliente com nome
  só dele, separado do circuito das chamadas internas.
- **Combinações válidas**: `AttemptTimeoutSeconds` não pode passar de `TotalTimeoutSeconds`, e
  `CircuitBreakerSamplingSeconds` precisa ser pelo menos o dobro de `AttemptTimeoutSeconds`. Subir o tempo limite
  por tentativa acima de 15 s exige, portanto, subir também a janela de amostragem. As duas são conferidas no
  registro.
- **Desligar**: `Resilience: false` tira o handler. O `TotalTimeoutSeconds` continua valendo, pelo
  `HttpClient.Timeout` e pelo serviço.

```csharp
builder.Services.AddTooarkHttpClient(builder.Configuration, options =>
{
  // Um endpoint lento de relatório, que não pode ser chamado duas vezes
  options.TotalTimeoutSeconds = 120;
  options.AttemptTimeoutSeconds = 120;
  options.CircuitBreakerSamplingSeconds = 240;
  options.RetryCount = 0;
});

// Um serviço de pagamentos: POST e PATCH sem nova tentativa, os demais verbos com ela
builder.Services.AddTooarkNamedHttpClient("pagamentos", builder.Configuration, options =>
{
  options.RetryNonIdempotentMethods = false;
});
```

---

## 🚨 Tratamento de Erros

Toda falha de uma chamada vira uma exceção do Tooark. Com o `Tooark.AspNetCore`, o tratador de exceções a converte
na resposta Problem Details da aplicação com o status abaixo.

| Falha                                               | Exceção                                              | Status | Mensagem                                                      |
| --------------------------------------------------- | ---------------------------------------------------- | ------ | ------------------------------------------------------------- |
| O serviço chamado responde com um status de erro    | `HttpRequestFailedException` (`BadGatewayException`) | 502    | os erros do serviço chamado, ou `Http.RequestFailed;{status}` |
| Tempo limite de uma tentativa ou da chamada inteira | `GatewayTimeoutException`                            | 504    | `Http.RequestTimeout`                                         |
| Circuito aberto                                     | `ServiceUnavailableException`                        | 503    | `Http.CircuitOpen`                                            |
| Limite de concorrência do cliente atingido          | `ServiceUnavailableException`                        | 503    | `Http.ConcurrencyLimitReached`                                |
| Falha de conexão, sem resposta                      | `BadGatewayException`                                | 502    | `Http.ConnectionFailed`                                       |
| Resposta de sucesso com JSON inválido               | `BadGatewayException`                                | 502    | `Http.InvalidResponse`                                        |
| Destino recusado pela guarda de saída               | `OutboundBlockedException` (`BadRequestException`)   | 400    | `Http.OutboundBlocked;{host}`                                 |
| URI absoluta que não é `http` nem `https`           | `BadRequestException`                                | 400    | `Http.RequestUriInvalid`                                      |
| URI relativa sem `BaseAddress`                      | `InternalServerErrorException`                       | 500    | `Http.BaseAddressNotConfigured`                               |
| Cancelamento pelo token de quem chamou              | `OperationCanceledException`, sem alteração          | —      | —                                                             |
| Falha ao ler o corpo de um `SendStreamingAsync`     | `IOException`/`HttpRequestException`, sem alteração  | —      | —                                                             |

A exceção original (a `HttpRequestException`, a recusa do Polly) fica em `InnerException`, para o log.

### Respostas de erro do serviço chamado

Um status de erro do serviço chamado **não** é o status da aplicação: um `404` ou um `401` de outro serviço não
pode virar um `404` ou um `401` para quem chamou a aplicação. A `HttpRequestFailedException` é sempre um `502`, e
expõe o status original em `UpstreamStatusCode`, a URI da requisição em `RequestUri` e o corpo da resposta de erro
em [`UpstreamBody`](#corpo-da-resposta-de-erro).

```csharp
using System.Net;
using Tooark.Http.Exceptions;
using Tooark.Http.Interfaces;

public sealed class ClienteGateway(IHttpClientService http)
{
  public async Task<ClienteDto?> BuscarAsync(int id, CancellationToken cancellationToken)
  {
    try
    {
      return await http.GetAsync<ClienteDto>($"clientes/{id}", cancellationToken);
    }
    catch (HttpRequestFailedException exception) when (exception.UpstreamStatusCode == HttpStatusCode.NotFound)
    {
      // Para esta aplicação, um cliente que não existe não é um erro
      return null;
    }
  }
}
```

As notificações da exceção dependem do corpo da resposta de erro:

- **Problem Details do Tooark** — `Content-Type: application/problem+json` com a lista `errors` de `code`, `key` e
  `message`, que é o que o `Tooark.AspNetCore` devolve. Cada item vira um `NotificationItem` com o mesmo código, a
  mesma chave e a mesma mensagem, então um serviço Tooark que chama outro mantém os erros originais:

  ```json
  {
    "status": 400,
    "title": "Bad Request",
    "errors": [
      {
        "code": "T.VLD.STR1",
        "key": "Email",
        "message": "O campo Email é obrigatório"
      },
      {
        "code": "T.VLD.NUM3",
        "key": "Idade",
        "message": "O campo Idade deve ser maior que 18"
      }
    ]
  }
  ```

- **Qualquer outro corpo** — um único item com a mensagem `Http.RequestFailed;{status}`. O corpo bruto nunca entra
  na mensagem nem nas notificações: ele pode carregar detalhes internos do serviço chamado.

O corpo de erro é lido até 64 KB, em qualquer formato; um maior é ignorado. São aproveitados no máximo 50 erros, e
cada texto é cortado em 1024 caracteres. A `RequestUri` pode carregar dados sensíveis na consulta: não a devolva em
uma resposta.

### Corpo da resposta de erro

Desde a v4.11.0, a `HttpRequestFailedException` expõe em `UpstreamBody` o corpo da resposta de erro, como texto e
em qualquer formato: o JSON de erro de um provedor, um texto, uma página HTML. Serve para a aplicação ler o motivo
da falha e montar a própria mensagem:

```csharp
using System.Net;
using Tooark.Exceptions;
using Tooark.Http.Exceptions;
using Tooark.Http.Interfaces;

public sealed record AssinaturaDto(int PlanoId);

public sealed class AssinaturaGateway(IHttpClientService http)
{
  public async Task AssinarAsync(AssinaturaDto assinatura, CancellationToken cancellationToken)
  {
    try
    {
      await http.PostAsync("assinaturas", assinatura, cancellationToken);
    }
    catch (HttpRequestFailedException exception) when (exception.UpstreamStatusCode == HttpStatusCode.PaymentRequired)
    {
      // A aplicação lê o que o provedor disse e responde com a própria chave, e não com o texto dele
      throw new UnprocessableEntityException(
        SemSaldo(exception.UpstreamBody) ? "Assinatura.SemSaldo" : "Assinatura.PagamentoRecusado");
    }
  }

  // O corpo vem de fora: pode ser nulo, não ser JSON ou não ter o formato esperado
  private static bool SemSaldo(string? corpo) =>
    corpo is not null && corpo.Contains("insufficient_funds", StringComparison.Ordinal);
}
```

- **Nulo** quando a resposta não tem corpo, quando o corpo passa de 64 KB ou quando a leitura dele falha.
- **Fora da mensagem**: a mensagem e as notificações da exceção continuam as mesmas, `Http.RequestFailed;{status}`
  ou os erros do Problem Details do Tooark. O pacote não coloca o corpo na mensagem, nas notificações nem no log.
- **Não é confiável**: é o que o serviço chamado escreveu, e pode trazer dados internos dele ou texto controlado
  por terceiros. **Não o devolva como veio na resposta da aplicação**: leia o que precisa e monte a própria
  mensagem.

---

## 🔒 Guarda de Saída

Quando a aplicação chama uma URL **informada pelo usuário** (um webhook, um avatar para importar, um link para
pré-visualizar), é o usuário quem escolhe para onde o servidor conecta. Sem conferência, `http://169.254.169.254/`
lê as credenciais de nuvem da instância e `http://localhost:9200/` alcança um serviço que nunca foi exposto. Isso é
server-side request forgery (SSRF).

A guarda confere o **endereço IP no momento da conexão**, e não o texto da URL. Ela é o `ConnectCallback` do
`SocketsHttpHandler`: resolve o nome uma vez, confere cada endereço contra a lista de bloqueio e conecta ao
endereço que conferiu. Um nome público que resolve para um endereço interno é recusado, e também um nome cujo DNS
responde diferente entre a conferência e a conexão (DNS rebinding), porque não há segunda resolução.

### O que é bloqueado

| Faixa                                                                | O que é                                                                  |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| `0.0.0.0/8`                                                          | "Esta rede"; `0.0.0.0` chega ao próprio host em vários sistemas          |
| `127.0.0.0/8`, `::1`                                                 | Loopback                                                                 |
| `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`                      | Redes privadas                                                           |
| `169.254.0.0/16`, `fe80::/10`                                        | Link-local, incluindo o endereço de metadados `169.254.169.254`          |
| `100.64.0.0/10`                                                      | CGNAT, também usado por redes internas de nuvem                          |
| `fc00::/7`, `fec0::/10`                                              | Endereços locais únicos e site-local do IPv6                             |
| `192.0.0.0/24`                                                       | Atribuições de protocolo da IETF                                         |
| `192.0.2.0/24`, `198.51.100.0/24`, `203.0.113.0/24`, `2001:db8::/32` | Documentação                                                             |
| `198.18.0.0/15`                                                      | Testes de desempenho                                                     |
| `224.0.0.0/4`, `ff00::/8`                                            | Multicast                                                                |
| `240.0.0.0/4`                                                        | Reservado, incluindo o broadcast `255.255.255.255`                       |
| `::/96`                                                              | Não especificado (`::`) e os endereços IPv4-compatíveis, obsoletos       |
| `64:ff9b:1::/48`, `100::/64`                                         | NAT64 de uso local e descarte                                            |
| `::ffff:a.b.c.d`, `64:ff9b::/96`, `2002::/16`, `2001::/32`           | IPv4 mapeado, NAT64, 6to4 e Teredo: classificados pelo IPv4 que carregam |

A mesma regra está disponível como função pura, `OutboundGuard.IsBlocked(IPAddress)`.

Se **qualquer** endereço para o qual um nome resolve está bloqueado, o host inteiro é recusado: um nome que mistura
um endereço público e um interno é o desenho de um ataque de rebinding. A recusa é uma `OutboundBlockedException`,
uma `BadRequestException` com `Http.OutboundBlocked;{host}`. A mensagem carrega o host da URL, nunca o endereço
para o qual ele resolveu.

### Exemplo de webhook

Ligue a guarda no cliente `Tooark` quando ele envia para URLs informadas pelo usuário:

```csharp
using Tooark.Http.Injections;
using Tooark.Http.Interfaces;

builder.Services.AddTooarkHttpClient(builder.Configuration, options => options.OutboundGuard.Enabled = true);

public sealed record PedidoPagoEvent(int PedidoId, decimal Valor);

public sealed class WebhookSender(IHttpClientService http)
{
  // A URL foi cadastrada pelo cliente: a guarda a recusa quando ela leva à rede interna
  public Task EnviarAsync(string webhookUrl, PedidoPagoEvent payload, CancellationToken cancellationToken) =>
    http.PostAsync(webhookUrl, payload, cancellationToken);
}
```

Ou mantenha o cliente `Tooark` para os serviços internos e dê aos webhooks um
[cliente com nome](#clientes-com-nome), com a guarda ligada só nele e o próprio circuito:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tooark.Http.Injections;
using Tooark.Http.Interfaces;

builder.Services.AddTooarkHttpClient(builder.Configuration);
builder.Services.AddTooarkNamedHttpClient("notificacoes", options => options.OutboundGuard.Enabled = true);

public sealed class WebhookSender([FromKeyedServices("notificacoes")] IHttpClientService http)
{
  public Task EnviarAsync(string webhookUrl, PedidoPagoEvent payload, CancellationToken cancellationToken) =>
    http.PostAsync(webhookUrl, payload, cancellationToken);
}
```

Em um cliente que não é do Tooark (um `HttpClient` nomeado ou tipado da própria aplicação), a guarda entra pelo
`AddTooarkOutboundGuard`, em que ela já vem ligada:

```csharp
using Tooark.Http.Injections;

var webhooks = builder.Services.AddHttpClient("webhooks");

// Resiliência primeiro, guarda depois: a recusa não é tentada de novo nem conta no circuito
webhooks.AddStandardResilienceHandler();
webhooks.AddTooarkOutboundGuard();
```

Em um cliente assim, o próprio `HttpClient` lança a `OutboundBlockedException`. Chame o `AddTooarkOutboundGuard`
**depois** do handler de resiliência e de qualquer configuração do handler primário: um handler de resiliência
adicionado depois trataria a recusa como falha transitória, e um `ConfigurePrimaryHttpMessageHandler` que troca o
handler depois remove a conferência na conexão. Em um cliente registrado pelo `AddTooarkHttpClient` ou pelo
`AddTooarkNamedHttpClient`, ligue a guarda pela opção `OutboundGuard.Enabled`, que já a coloca nessa ordem.

A guarda precisa de um `SocketsHttpHandler` como handler primário. Um que a aplicação já configurou é mantido, só
com o `ConnectCallback` trocado. Um handler primário de outro tipo, como um `HttpClientHandler` com certificados de
cliente, é **substituído** por um `SocketsHttpHandler` novo, e a configuração dele se perde: configure o
`SocketsHttpHandler` diretamente (`SslOptions`, `Proxy`, `PooledConnectionLifetime`) antes de adicionar a guarda.

### Listas de liberação

```csharp
webhooks.AddTooarkOutboundGuard(options =>
{
  options.AllowedHosts = ["parceiro.interna.exemplo.com", ".svc.cluster.local"];
  options.AllowedNetworks = ["10.20.0.0/16", "fd00:1::/64"];
});
```

- **`AllowedHosts`** — o nome é de confiança, resolva para onde resolver. Um nome exato, ou um sufixo iniciado por
  ponto: `.interna.exemplo.com` libera `api.interna.exemplo.com`, mas não o próprio `interna.exemplo.com` nem
  `falsainterna.exemplo.com`. Maiúsculas e minúsculas são ignoradas. Só nomes: um endereço vai em
  `AllowedNetworks`.
- **`AllowedNetworks`** — blocos CIDR liberados da lista de bloqueio. Um endereço único é escrito com `/32` ou
  `/128`, e o endereço base precisa ser o da rede (`10.20.0.1/16` é recusado). Uma rede IPv4 liberada também cobre
  a forma mapeada em IPv6 (`::ffff:10.20.0.5`). Endereços NAT64, 6to4 e Teredo são bloqueados pelo IPv4 que
  carregam, mas **não** são liberados por ele: libere o bloco IPv6 deles se precisar.

Uma entrada inválida falha no registro.

### Redirecionamentos

O `SocketsHttpHandler` segue os redirecionamentos sozinho, e cada salto para outro host abre a própria conexão, que
passa pela mesma conferência. Um destino que passa e depois redireciona para `http://169.254.169.254/` é recusado
no segundo salto.

### Com um proxy de saída

Com um proxy (as variáveis `HTTP_PROXY`/`HTTPS_PROXY` ou o `SocketsHttpHandler.Proxy`), a conexão é aberta **para o
proxy**, e é o proxy quem resolve o nome do destino. A conferência na conexão passa a ver só o endereço do proxy. O
que acontece nesse caso:

- **Coberto**: um IP literal na URL da requisição (`http://10.0.0.5/`, `http://[::1]/`, `http://2130706433/`), que
  um handler da requisição confere antes da conexão.
- **Não coberto**: um nome que resolve para um endereço interno, e os saltos de um redirecionamento. Só o proxy os
  vê. Bloqueie as faixas internas nas regras do próprio proxy e considere desligar o `AllowAutoRedirect`.
- **O próprio proxy**: um proxy interno é um endereço bloqueado. Libere-o pelo nome em `AllowedHosts`; liberar a
  rede dele em `AllowedNetworks` liberaria também os IPs literais dessa rede.

### Do que a guarda não protege

- **Chamadas que não passam por um cliente protegido**: outro `HttpClient`, um driver de banco, um cliente de SMTP
  ou de SDK.
- **Destinos públicos.** A guarda não sabe com quais hosts públicos a aplicação deveria falar. Uma lista de
  destinos permitidos por cliente é regra da aplicação.
- **Hosts em `AllowedHosts`**: não são conferidos.
- **A resposta.** A guarda não limita o tamanho nem o conteúdo do que o destino devolve. O serviço carrega em
  memória o corpo de uma resposta de sucesso (menos no `SendStreamingAsync`, em que a leitura é de quem chama) e
  até 64 KB do corpo de uma resposta de erro, em `UpstreamBody`. Não devolva ao usuário, como veio, a resposta de
  uma URL informada por ele, nem o `UpstreamBody` dela.
- **HTTP/3.** A conexão QUIC não passa pelo `ConnectCallback`, então um cliente protegido mantém as requisições em
  HTTP/1.1 ou HTTP/2.
- **Portas.** Qualquer porta de um endereço permitido é alcançável.

---

## 📡 Observabilidade

Os traces e as métricas das chamadas vêm do
[`Tooark.Observability`](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Observability/README.pt-BR.md), sem
código neste pacote e sem referência entre os dois: o `AddTooarkObservability` registra a instrumentação de
`HttpClient` do OpenTelemetry para tracing e para métricas, e essa instrumentação cobre todo `HttpClient` do
processo, inclusive os criados pelo `IHttpClientFactory`.

- **Traces**: um span de cliente por tentativa, então as novas tentativas de uma chamada aparecem como spans
  irmãos. O cabeçalho `traceparent` é propagado, e um serviço chamado que também usa o `Tooark.Observability`
  continua o mesmo trace. A consulta da URL é removida dos atributos do span por padrão.
- **Métricas**: `http.client.request.duration` e os demais instrumentos `http.client.*` do .NET.
- **Eventos de resiliência** (novas tentativas, estado do circuito, tempos limite) são publicados pelo Polly no
  medidor `Polly`. Para exportá-los, acrescente o medidor às opções de observabilidade:

  ```json
  { "Observability": { "Metrics": { "AdditionalMeters": ["Polly"] } } }
  ```

Sem o `Tooark.Observability` o pacote funciona do mesmo jeito, só sem telemetria.

---

## 📝 Exemplos de Uso

### Chamando outro serviço Tooark

Os erros do serviço chamado chegam a quem chamou a aplicação com os códigos e as chaves originais, dentro de um
`502`:

```csharp
using Tooark.Http.Interfaces;

public sealed record CriarFaturaDto(int ClienteId, decimal Valor);
public sealed record FaturaDto(int Id, string Numero);

public sealed class FaturamentoGateway(IHttpClientService http)
{
  // Um erro de validação do serviço de faturamento vira uma HttpRequestFailedException com as mesmas notificações
  public Task<FaturaDto?> CriarAsync(CriarFaturaDto fatura, CancellationToken cancellationToken) =>
    http.PostAsync<CriarFaturaDto, FaturaDto>("faturas", fatura, cancellationToken);
}
```

### Avisando de uma URL de webhook recusada já no cadastro

```csharp
using System.Net;
using Tooark.Http;

public static bool LiteralPermitido(Uri webhookUrl) =>
  !IPAddress.TryParse(webhookUrl.IdnHost, out var address) || !OutboundGuard.IsBlocked(address);
```

Isso é só uma mensagem antecipada para o usuário. Um nome não é resolvido aqui: a conferência que protege a
aplicação é a do momento da conexão.

### Sem injeção de dependência

O serviço tem construtores públicos, para ferramentas e testes: o do cliente padrão, com
`IOptions<HttpClientOptions>`, e, desde a v4.11.0, o de um cliente com nome,
`HttpClientService(IHttpClientFactory, HttpClientOptions, string)`. Os dois precisam de um `IHttpClientFactory`, e
a resiliência e a guarda são as do cliente registrado na fábrica com aquele nome. O caminho mais curto é montar um
provedor de serviços:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tooark.Http.Injections;
using Tooark.Http.Interfaces;

using var provider = new ServiceCollection()
  .AddTooarkHttpClient(options => options.BaseAddress = "https://api.exemplo.com")
  .BuildServiceProvider();

var http = provider.GetRequiredService<IHttpClientService>();
```

Nos testes da aplicação, simule o `IHttpClientService`, ou troque o handler primário do cliente nomeado:

```csharp
services.AddHttpClient(HttpClientService.ClientName).ConfigurePrimaryHttpMessageHandler(() => handlerFalso);

// Em um cliente com nome, o nome é o do registro
services.AddHttpClient("modelos").ConfigurePrimaryHttpMessageHandler(() => handlerFalso);
```

Ao simular o `IHttpClientService`:

- **Métodos com ajuste da requisição**: são extensões, e chamam o `SendAsync` do substituto, e não o `PostAsync` ou
  o `GetAsync` dele. É o `SendAsync` que precisa devolver a resposta, com o status e o corpo em JSON.
- **`SendStreamingAsync`**: um substituto escrito à mão que não o implementa responde pelo próprio `SendAsync`, pela
  implementação padrão da interface.
- **Cliente com nome**: registre o substituto com a mesma chave, com
  `services.AddKeyedSingleton<IHttpClientService>("modelos", substituto)`.

---

## 📋 Dependências

| Pacote                                                                                                        | Versão   | Descrição                                                   |
| ------------------------------------------------------------------------------------------------------------- | -------- | ----------------------------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                       | 4.x      | Exceções e, por ele, o `NotificationItem`                   |
| [`Microsoft.Extensions.Http.Resilience`](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience) | 8.x/10.x | Handler padrão de resiliência (Polly), `IHttpClientFactory` |

O `Tooark.Observability` **não** é uma dependência: veja [Observabilidade](#-observabilidade).

---

## 🎯 Boas Práticas

1. **Injete o `IHttpClientService`, nunca crie um `HttpClient`** — a fábrica roda os handlers e mantém as conexões;
   um `new HttpClient()` por chamada esgota sockets, e um estático nunca vê uma mudança de DNS.
2. **Ligue a guarda onde a URL vem do usuário** — e mantenha desligada, ou com listas de liberação, no cliente que
   chama serviços internos.
3. **Prefira `AllowedHosts` com nomes exatos** — um sufixo confia em todo nome abaixo dele, e uma rede confia em
   todo endereço dentro dela.
4. **Decida as novas tentativas por serviço** — na 4.x, POST e PATCH são tentados de novo como os demais verbos.
   Use `RetryNonIdempotentMethods: false` para um serviço que não é idempotente, ou `RetryCount: 0` para desligar
   todas. Na v5 o padrão de `RetryNonIdempotentMethods` passa a ser `false`: informe a opção desde já se a
   aplicação depende de um dos dois comportamentos.
5. **Não exponha a falha do serviço chamado** — registre `UpstreamStatusCode` e `RequestUri` no log; a resposta da
   aplicação é o `502` com as notificações. O `UpstreamBody` não é confiável: leia o que precisa dele e não o
   devolva como veio.
6. **Passe o `CancellationToken` da requisição** — o cancelamento de quem chamou interrompe as novas tentativas e
   não é reportado como tempo limite.
7. **Não capture `Exception` em volta das chamadas** — as exceções já carregam o status certo; capture a
   `HttpRequestFailedException` só onde um status do serviço chamado tem significado para a aplicação.
8. **Use um cliente com nome por destino** — cada destino fica com o próprio endereço base, os próprios tempos
   limite e o próprio circuito, em vez de um cliente padrão ajustado para o pior caso.
9. **Descarte a resposta do `SendStreamingAsync` e dê um prazo à leitura** — o descarte devolve a conexão, e o
   tempo limite da chamada não cobre o corpo.

---

## ⚠️ Códigos de Erro e Soluções

Os erros de configuração são `InternalServerErrorException` lançadas **no registro** (`AddTooarkHttpClient`,
`AddTooarkNamedHttpClient`, `AddTooarkOutboundGuard`) ou ao criar o serviço sem injeção de dependência. Um cliente
com nome usa as mesmas mensagens, sem o nome do cliente. Um nome de cliente inválido (vazio, com `:` ou `Tooark`)
não entra na tabela: é uma `ArgumentException`.

| Mensagem                                                          | Descrição                                                                                      | Solução                                                                |
| ----------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| `Options.HttpClient.BaseAddressInvalid`                           | `BaseAddress` não é uma URL absoluta `http`/`https`, ou tem credenciais, consulta ou fragmento | Use `https://host/caminho`; envie credenciais em um cabeçalho          |
| `Options.HttpClient.TotalTimeoutInvalid;{min};{max}`              | `TotalTimeoutSeconds` fora de 1 a 3600                                                         | Use um valor da faixa                                                  |
| `Options.HttpClient.AttemptTimeoutInvalid;{min};{max}`            | `AttemptTimeoutSeconds` abaixo de 1 ou acima de `TotalTimeoutSeconds`                          | Reduza o tempo limite por tentativa ou aumente o total                 |
| `Options.HttpClient.RetryCountInvalid;{min};{max}`                | `RetryCount` fora de 0 a 10                                                                    | Use um valor da faixa; `0` desliga as novas tentativas                 |
| `Options.HttpClient.RetryDelayInvalid;{min};{max}`                | `RetryDelayMilliseconds` fora de 0 a 60000                                                     | Use um valor da faixa                                                  |
| `Options.HttpClient.CircuitBreakerFailureRatioInvalid`            | `CircuitBreakerFailureRatio` fora de 0 a 1                                                     | Use uma taxa, como `0.1` para 10%                                      |
| `Options.HttpClient.CircuitBreakerMinimumThroughputInvalid;{min}` | `CircuitBreakerMinimumThroughput` abaixo de 2                                                  | Use 2 ou mais                                                          |
| `Options.HttpClient.CircuitBreakerSamplingInvalid;{min};{max}`    | `CircuitBreakerSamplingSeconds` abaixo do dobro de `AttemptTimeoutSeconds`, ou acima de 86400  | Aumente a janela ou reduza o tempo limite por tentativa                |
| `Options.HttpClient.CircuitBreakerBreakInvalid;{min};{max}`       | `CircuitBreakerBreakSeconds` fora de 1 a 86400                                                 | Use um valor da faixa                                                  |
| `Options.HttpClient.OutboundGuard.AllowedHostInvalid;{value}`     | Entrada de `AllowedHosts` que não é um nome de host                                            | Tire esquema, porta, caminho e `*`; endereços vão em `AllowedNetworks` |
| `Options.HttpClient.OutboundGuard.AllowedNetworkInvalid;{value}`  | Entrada de `AllowedNetworks` que não é um bloco CIDR com o endereço da rede                    | Use `10.20.0.0/16`; um endereço único leva `/32` ou `/128`             |
| `Options.Configuration.UnknownKey;{section};{key}`                | Chave desconhecida em `HttpClient` ou `HttpClients:{nome}`, com a configuração estrita ligada  | Corrija o nome da chave, ou tire-a da seção                            |

Os erros de chamada são lançados pelo serviço:

| Mensagem                        | Exceção                        | Descrição                                                         | Solução                                                                     |
| ------------------------------- | ------------------------------ | ----------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `Http.RequestFailed;{status}`   | `HttpRequestFailedException`   | O serviço chamado respondeu com um status de erro                 | Confira o `UpstreamStatusCode`, o `UpstreamBody` e o log do serviço chamado |
| `Http.RequestTimeout`           | `GatewayTimeoutException`      | Sem resposta dentro do tempo limite da tentativa ou do total      | Confira o serviço chamado; ajuste os tempos se ele é lento por desenho      |
| `Http.CircuitOpen`              | `ServiceUnavailableException`  | Falhas demais: o circuito está aberto e as chamadas são recusadas | Aguarde o `CircuitBreakerBreakSeconds`; investigue o serviço chamado        |
| `Http.ConcurrencyLimitReached`  | `ServiceUnavailableException`  | Mais de 1000 requisições simultâneas no cliente                   | Reduza a concorrência das chamadas                                          |
| `Http.ConnectionFailed`         | `BadGatewayException`          | Conexão recusada, nome não resolvido, falha de TLS, conexão caída | Confira o endereço, o DNS e a rede                                          |
| `Http.InvalidResponse`          | `BadGatewayException`          | O corpo de sucesso não é um JSON válido para o tipo da resposta   | Confira o contrato e o DTO                                                  |
| `Http.OutboundBlocked;{host}`   | `OutboundBlockedException`     | A guarda recusou o destino                                        | Corrija a URL; para um destino interno legítimo, use as listas de liberação |
| `Http.RequestUriInvalid`        | `BadRequestException`          | A URI da requisição não é uma URI `http` ou `https` válida        | Valide a URL antes de chamar                                                |
| `Http.BaseAddressNotConfigured` | `InternalServerErrorException` | URI relativa e nenhum `BaseAddress`                               | Configure o `BaseAddress` do cliente ou passe uma URI absoluta              |

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
