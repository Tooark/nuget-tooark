# Tooark.Sanitizers

Biblioteca com três sanitizadores para conteúdo que vem de fora da aplicação: **HTML** com lista de permissão,
**URL** com lista de esquemas e o conteúdo do editor
[**`@tooark/wysiwyg`**](https://www.npmjs.com/package/@tooark/wysiwyg) (o JSON do Tiptap), com as mesmas regras que o
componente aplica no navegador.

O HTML não é sanitizado por código próprio: o serviço embrulha o
[HtmlSanitizer](https://github.com/mganss/HtmlSanitizer), padrão de fato em .NET, porque encodings, tags aninhadas e
atributos de evento dão uma superfície de bypass grande demais. As regras de URL e do JSON do wysiwyg são próprias,
pequenas e testadas contra os bypasses conhecidos.

🌍 **Idiomas:** [🇺🇸 English](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Sanitizers/README.md) · 🇧🇷 **Português (este arquivo)**

## Conteúdo

- [Visão Geral](#visão-geral)
- [Instalação](#-instalação)
- [Configuração](#️-configuração)
- [Sanitizadores](#-sanitizadores)
- [Exemplos de Uso](#-exemplos-de-uso)
- [Dependências](#-dependências)
- [Boas Práticas](#-boas-práticas)
- [Códigos de Erro e Soluções](#️-códigos-de-erro-e-soluções)
- [Contribuição](#-contribuição)
- [Licença](#-licença)

## Visão Geral

| Serviço                    | Entrada                                   | Saída                                                   | Uso típico                                             |
| -------------------------- | ----------------------------------------- | ------------------------------------------------------- | ------------------------------------------------------ |
| `IHtmlSanitizerService`    | HTML                                      | HTML só com o permitido; vazio para entrada em branco   | HTML de fonte externa: importação, e-mail, legado      |
| `IUrlSanitizerService`     | URL                                       | URL limpa, ou vazio quando recusada; `IsSafe`           | Link informado pelo usuário: site, perfil, rede social |
| `IWysiwygSanitizerService` | Documento do Tiptap (`JsonNode` ou texto) | Documento novo, sanitizado; nulo quando não é documento | Conteúdo do `@tooark/wysiwyg` antes de gravar          |

- **Padrão seguro**: a seção de configuração é opcional, e sem ela os três sanitizadores funcionam.
- **Guardas que a configuração não desliga**: `javascript:`, `vbscript:` e `data:` nunca são liberados; no HTML,
  também não `<script>`, `<object>`, `<embed>` e atributos de evento (`onclick`...).
- **Falha no startup**: uma configuração que tenta liberar algo desses é recusada no registro, e não na primeira
  requisição.
- **Sem estado por chamada**: os serviços são singletons e seguros para uso concorrente.

---

## 🔧 Instalação

```bash
dotnet add package Tooark.Sanitizers
```

O pacote roda no runtime base do .NET, sem o ASP.NET Core. Ele também vem no agregador `Tooark`, cujo
`AddTooarkService` já registra os sanitizadores.

---

## ⚙️ Configuração

### appsettings.json

Tudo é opcional. O exemplo restringe o HTML a um conjunto de formatação, aceita links relativos e desliga vídeos no
conteúdo do editor:

```json
{
  "Sanitizers": {
    "Html": {
      "AllowedTags": ["p", "br", "strong", "em", "u", "a", "ul", "ol", "li"],
      "AllowedAttributes": ["href", "title"],
      "AllowedSchemes": ["https", "mailto"]
    },
    "Url": {
      "AllowedSchemes": ["https"],
      "AllowRelative": true
    },
    "Wysiwyg": {
      "AllowVideos": false
    }
  }
}
```

### Program.cs

```csharp
using Tooark.Sanitizers.Injections;

// Lê a seção Sanitizers
builder.Services.AddTooarkSanitizers(builder.Configuration);

// Ou sem ler a configuração, só com ajustes no código
builder.Services.AddTooarkSanitizers(options => options.Url.AllowRelative = true);
```

Com configuração e ajuste no código, o ajuste vale por cima da seção. Chamar o registro mais de uma vez não duplica
os serviços.

### Opções de HTML — `Sanitizers:Html`

Cada lista informada **substitui** a lista padrão do HtmlSanitizer. Ausente ou vazia, fica a padrão, que já é uma
lista de permissão segura para HTML de formatação.

| Propriedade            | Tipo       | Padrão                 | Descrição                                                                                                          |
| ---------------------- | ---------- | ---------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `AllowedTags`          | `string[]` | lista do HtmlSanitizer | Tags permitidas. `script`, `object`, `embed`, `applet`, `base`, `meta`, `link`, `frame` e `frameset` são recusadas |
| `AllowedAttributes`    | `string[]` | lista do HtmlSanitizer | Atributos permitidos. Atributos de evento (`on*`) são recusados                                                    |
| `AllowedSchemes`       | `string[]` | `http`, `https`        | Esquemas nos atributos de URL (`href`, `src`...). `javascript`, `vbscript` e `data` são recusados                  |
| `AllowedCssProperties` | `string[]` | lista do HtmlSanitizer | Propriedades CSS permitidas no atributo `style`                                                                    |
| `AllowedClasses`       | `string[]` | nenhuma                | Classes permitidas. Informar a lista libera o atributo `class` só com elas                                         |
| `KeepChildNodes`       | `bool`     | `false`                | Mantém o conteúdo de uma tag removida. Com `false`, a tag sai com o conteúdo                                       |

### Opções de URL — `Sanitizers:Url`

| Propriedade        | Tipo       | Padrão          | Descrição                                                                                                      |
| ------------------ | ---------- | --------------- | -------------------------------------------------------------------------------------------------------------- |
| `AllowedSchemes`   | `string[]` | `http`, `https` | Esquemas aceitos em URL absoluta, como `mailto` e `tel`. `javascript`, `vbscript` e `data` são recusados       |
| `AllowRelative`    | `bool`     | `false`         | Aceita URL relativa com prefixo `/`, `#`, `?`, `./` ou `../`. Caminho sem prefixo (`uploads/x.png`) é recusado |
| `AllowCredentials` | `bool`     | `false`         | Aceita usuário e senha na URL. `https://banco.com@golpe.com` leva a `golpe.com` enquanto exibe outro nome      |

### Opções do wysiwyg — `Sanitizers:Wysiwyg`

| Propriedade   | Tipo   | Padrão | Descrição                                                                            |
| ------------- | ------ | ------ | ------------------------------------------------------------------------------------ |
| `AllowImages` | `bool` | `true` | Mantém os nós de imagem. Desligue quando a toolbar do editor não tem o grupo `media` |
| `AllowVideos` | `bool` | `true` | Mantém os nós de vídeo                                                               |

As demais regras do wysiwyg são as do componente e não são configuráveis: o conteúdo aceito no servidor precisa
ser o mesmo que o editor e o viewer aceitam.

---

## 🧹 Sanitizadores

### HTML — `IHtmlSanitizerService`

`Sanitize(html)` devolve o fragmento só com as tags, os atributos, os esquemas e o CSS permitidos. Entrada nula ou
em branco vira texto vazio. Scripts, atributos de evento, `javascript:` em qualquer grafia (maiúsculas, entidades,
tabulação no meio) e `expression()` no CSS não sobrevivem.

### URL — `IUrlSanitizerService`

`Sanitize(url)` devolve a URL limpa quando ela é aceita, ou texto vazio quando não é; `IsSafe(url)` responde o mesmo
como booleano. As regras são as do `isSafeUrl` do `@tooark/wysiwyg`, para que uma URL tenha o mesmo destino no
navegador e no servidor:

1. caracteres de controle e espaços saem de **toda** a URL, porque o navegador os ignora e `java\tscript:` vira
   `javascript:`;
2. relativa é a que começa com `/`, `#`, `?`, `./` ou `../`, e só passa com `AllowRelative`;
3. absoluta precisa de um esquema da lista;
4. usuário e senha na URL são recusados, salvo com `AllowCredentials`. No `mailto`, o que vem antes do `@` é o
   endereço, e não credencial.

A URL devolvida é a limpa, **sem normalização**: o que o navegador vai interpretar é exatamente o que foi validado.

### Conteúdo do `@tooark/wysiwyg` — `IWysiwygSanitizerService`

O componente nunca trabalha com HTML: o conteúdo é o JSON do Tiptap, e ele o sanitiza no navegador com
`sanitizeWysiwygContent`. Só que o JSON que chega à API pode não ter passado pelo editor: uma requisição montada à
mão manda o que quiser. O serviço aplica as mesmas regras no servidor e **reconstrói** o documento só com o que a
lista de permissão conhece. Chaves e atributos desconhecidos não são copiados.

Nós e marcas aceitos, os do schema do `@tooark/wysiwyg` 1.4:

| Nó                                                                           | Atributos                                              |
| ---------------------------------------------------------------------------- | ------------------------------------------------------ |
| `doc`, `blockquote`, `bulletList`, `listItem`, `hardBreak`, `horizontalRule` | —                                                      |
| `paragraph`                                                                  | `textAlign`                                            |
| `heading`                                                                    | `level` (1 a 4), `textAlign`                           |
| `text`                                                                       | o texto, não vazio, e as marcas                        |
| `orderedList`                                                                | `start` (inteiro), `type` (`1`, `a`, `A`, `i`, `I`)    |
| `codeBlock`                                                                  | `language`                                             |
| `image`                                                                      | `src` (obrigatório), `alt`, `title`, `width`, `height` |
| `video`                                                                      | `src` (obrigatório), `poster`, `title`                 |

| Marca                                           | Atributos                                      |
| ----------------------------------------------- | ---------------------------------------------- |
| `bold`, `italic`, `underline`, `strike`, `code` | —                                              |
| `link`                                          | `href` (obrigatório), `target`, `rel`, `title` |
| `textStyle`                                     | `color`, `backgroundColor`                     |
| `highlight`                                     | `color`                                        |

As regras:

- **nó ou marca fora do schema** cai; o nó cai com os filhos;
- **URLs** (`href`, `src`, `poster`) seguem a regra de URL acima com `http`, `https`, `mailto`, `tel` e relativas
  com prefixo, como no componente. O link recusado sai da marca e o texto fica; a imagem ou o vídeo recusado sai do
  documento; o `poster` recusado sai sozinho;
- **cores** só em hex, nome CSS ou `rgb()`/`hsl()` sem `;`. As demais (`oklch()`, `var()`) caem, como no componente;
- **`textAlign`** só em `left`, `center`, `right` e `justify`; o **`level`** do título vira inteiro entre 1 e 4, e 1
  quando não é inteiro;
- **nó de texto vazio** cai, porque o ProseMirror o recusa e o documento inteiro deixaria de abrir;
- **documento que perde todos os blocos** vira o documento vazio do componente (um parágrafo);
- **aninhamento** além de 64 níveis é cortado.

`Sanitize(JsonNode)` devolve um `JsonObject` novo; `Sanitize(string)` devolve o JSON em texto, com os acentos
legíveis e `<`, `>`, `&` e aspas escapados, para continuar seguro se for embutido numa página. Entrada nula, JSON
inválido ou raiz que não é `doc` devolvem nulo.

> **Versão do componente.** O schema acompanha o `@tooark/wysiwyg` 1.4. Um nó ou atributo novo no componente
> precisa de uma versão nova deste pacote; até lá, o servidor o remove.

---

## 📝 Exemplos de Uso

### Gravando o conteúdo do editor

O `<ark-wysiwyg-editor>` emite o JSON no evento `ark-wysiwyg-change`. A API sanitiza antes de gravar, e o mesmo JSON
volta para o `<ark-wysiwyg-viewer>`:

```csharp
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Tooark.Sanitizers.Interfaces;

public sealed record SalvarArtigoDto(string Titulo, JsonObject Conteudo);

[ApiController]
[Route("artigos")]
public sealed class ArtigoController(IWysiwygSanitizerService wysiwyg) : ControllerBase
{
  [HttpPost]
  public IActionResult Salvar([FromBody] SalvarArtigoDto dto)
  {
    // Nulo quando o JSON não é um documento do editor
    var conteudo = wysiwyg.Sanitize(dto.Conteudo);

    if (conteudo is null)
    {
      return BadRequest();
    }

    // Grave conteudo.ToJsonString(): é o que o viewer recebe de volta
    return Ok();
  }
}
```

### Link informado pelo usuário

```csharp
using Tooark.Sanitizers.Interfaces;

public sealed class PerfilService(IUrlSanitizerService urls)
{
  // Texto vazio quando o link é recusado
  public string SiteDoPerfil(string? site) => urls.Sanitize(site);
}
```

```csharp
urls.Sanitize("https://tooark.com");        // "https://tooark.com"
urls.Sanitize(" java\tscript:alert(1)");    // ""
urls.Sanitize("data:text/html,<script>");   // ""
urls.IsSafe("https://banco.com@golpe.com"); // false
```

### HTML de uma fonte externa

```csharp
using Tooark.Sanitizers.Interfaces;

public sealed class ImportacaoService(IHtmlSanitizerService html)
{
  public string Limpar(string conteudoImportado) => html.Sanitize(conteudoImportado);
}
```

```csharp
html.Sanitize("<p onclick=\"alert(1)\">Olá <script>alert(1)</script></p>"); // "<p>Olá </p>"
html.Sanitize("<a href=\"javascript:alert(1)\">link</a>");                  // "<a>link</a>"
```

### Sem injeção de dependência

Os serviços têm construtor público, para uso em ferramentas e testes:

```csharp
using Microsoft.Extensions.Options;
using Tooark.Sanitizers;
using Tooark.Sanitizers.Options;

var opcoes = Options.Create(new SanitizerOptions());
var wysiwyg = new WysiwygSanitizerService(opcoes);
```

---

## 📋 Dependências

| Pacote                                                                                                                                          | Versão   | Descrição                                 |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ----------------------------------------- |
| [`Tooark.Exceptions`](https://www.nuget.org/packages/Tooark.Exceptions)                                                                         | 4.x      | Exceções (`InternalServerErrorException`) |
| [`HtmlSanitizer`](https://www.nuget.org/packages/HtmlSanitizer)                                                                                 | 9.x      | Sanitização de HTML, sobre o AngleSharp   |
| [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions)   | 8.x/10.x | Leitura das opções da configuração        |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) | 8.x/10.x | Registro no container                     |

---

## 🎯 Boas Práticas

1. **Sanitize na entrada e continue codificando na saída** — sanitizar antes de gravar não dispensa o encoding do
   contexto na exibição (Razor, `textContent`, atributos entre aspas).
2. **Sanitize o JSON do editor no servidor** — o componente sanitiza no navegador, mas a API recebe o que qualquer
   cliente mandar.
3. **Liste só o que o conteúdo precisa** — reduza a lista de tags do HTML em vez de ampliá-la.
4. **Desligue a mídia que a aplicação não usa** — sem o grupo `media` no editor, `AllowImages` e `AllowVideos` em
   `false` evitam uma imagem apontando para um servidor de rastreamento.
5. **Não use o sanitizador de URL para validar redirecionamento** — com `AllowRelative`, `//outro.com` passa como
   relativa, como no componente, e leva a outro host. Para um `returnUrl`, use o `Url.IsLocalUrl` do ASP.NET Core.

---

## ⚠️ Códigos de Erro e Soluções

Os erros são de configuração: `InternalServerErrorException` lançada **no registro** (`AddTooarkSanitizers`) ou
na criação do serviço sem injeção de dependência. A sanitização em si não lança: o que não é aceito é removido.

| Mensagem                                             | Descrição                                               | Solução                                                      |
| ---------------------------------------------------- | ------------------------------------------------------- | ------------------------------------------------------------ |
| `Options.Sanitizers.SchemeNotAllowed;{esquema}`      | `javascript`, `vbscript` ou `data` na lista de esquemas | Remova o esquema; ele executa código ou embute conteúdo      |
| `Options.Sanitizers.SchemeInvalid;{esquema}`         | Esquema com sintaxe inválida                            | Use letras, dígitos, `+`, `-` ou `.`, começando por letra    |
| `Options.Sanitizers.Html.TagNotAllowed;{tag}`        | Tag bloqueada em `AllowedTags`                          | Remova a tag; ela executa código ou carrega conteúdo externo |
| `Options.Sanitizers.Html.AttributeNotAllowed;{nome}` | Atributo de evento (`on*`) em `AllowedAttributes`       | Remova o atributo; ele executa código em qualquer tag        |

---

## 🪪 Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório
[Tooark](https://github.com/Tooark/nuget-tooark/issues).

## 📄 Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo
[LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) para mais detalhes.
