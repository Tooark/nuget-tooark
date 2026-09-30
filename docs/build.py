"""Gera o site de documentação do Tooark (GitHub Pages) a partir dos READMEs dos pacotes.

O conteúdo não é escrito duas vezes: cada página de pacote é o README do pacote, com os ajustes que o site
pede (links do GitHub viram links entre páginas, o sumário e o bloco final saem, porque o site tem navegação
e rodapé próprios). Só a página inicial e a introdução da API são escritas à mão, em docs/<idioma>/.

Saída em docs/_site/: inglês na raiz e português em pt-BR/, cada um com a própria busca.

Uso:
  python docs/build.py            # gera o site
  python docs/build.py --no-api   # sem a referência de API (mais rápido, para revisar texto)
  python docs/build.py --check    # só confere se todo pacote empacotável está em um grupo (o CI roda)

Requer o docfx do manifesto de ferramentas (dotnet tool restore).
"""
import html
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DOCS = ROOT / "docs"
OBJ = DOCS / "obj"
SITE = DOCS / "_site"
BASE_URL = "https://tooark.com/nuget-tooark"
REPO = "https://github.com/Tooark/nuget-tooark"
BLOB = f"{REPO}/blob/main"
TREE = f"{REPO}/tree/main"

LANGS = {
    "en": {
        "readme": "README.md",
        "out": SITE,
        "base_url": BASE_URL,
        "nav": {"home": "Home", "packages": "Packages", "api": "API"},
        "overview": "Overview",
        "overview_sections": ["📖 About", "🔧 Installation", "📦 Packages"],
    },
    "pt-BR": {
        "readme": "README.pt-BR.md",
        "out": SITE / "pt-BR",
        "base_url": f"{BASE_URL}/pt-BR",
        "nav": {"home": "Início", "packages": "Pacotes", "api": "API"},
        "overview": "Visão geral",
        "overview_sections": ["📖 Sobre o Projeto", "🔧 Instalação", "📦 Pacotes"],
    },
}

# Grupos da navegação e da página inicial. Todo projeto empacotável precisa estar em um grupo: o build falha
# quando um pacote novo fica de fora, como o check da lista de publicação.
GROUPS = [
    ({"en": "Aggregator", "pt-BR": "Agregador"}, ["Tooark"]),
    ({"en": "Foundation", "pt-BR": "Fundação"}, [
        "Tooark.Notifications", "Tooark.Exceptions", "Tooark.Validations", "Tooark.Attributes", "Tooark.Enums",
        "Tooark.Utils", "Tooark.Extensions", "Tooark.Dtos", "Tooark.Entities", "Tooark.ValueObjects",
    ]),
    ({"en": "Web", "pt-BR": "Web"}, ["Tooark.AspNetCore", "Tooark.Sanitizers"]),
    ({"en": "Security", "pt-BR": "Segurança"}, ["Tooark.Securities", "Tooark.Securities.OpenId"]),
    ({"en": "Cloud", "pt-BR": "Nuvem"}, [
        "Tooark.Storage", "Tooark.Storage.Aws", "Tooark.Storage.Gcp",
        "Tooark.Secrets", "Tooark.Secrets.Aws", "Tooark.Secrets.Gcp", "Tooark.Secrets.Vault",
    ]),
    ({"en": "Observability", "pt-BR": "Observabilidade"}, ["Tooark.Observability"]),
    ({"en": "Mediator", "pt-BR": "Mediator"}, [
        "Tooark.Mediator.Abstractions", "Tooark.Mediator", "Tooark.Mediator.EntityFrameworkCore",
    ]),
]

# Seções dos READMEs que o site não repete: o sumário (o site tem o "Neste artigo") e o bloco final (vai
# para o rodapé e para a página inicial).
DROP_SECTIONS = {
    "Contents", "Conteúdo", "Contributing", "Contribuindo", "Help & Security", "Ajuda & Segurança",
    "Support", "Apoie", "License", "Licença",
}

FENCE = re.compile(r"^\s*(```|~~~)")

# Textos da interface do template em português. O template só traz o inglês; as chaves são as do token.json
# do template default do DocFX.
TOKENS_PT = {
    "inThisArticle": "Neste artigo", "nextArticle": "Próximo", "prevArticle": "Anterior",
    "backToTop": "Voltar ao topo", "themeLight": "Claro", "themeDark": "Escuro", "themeAuto": "Automático",
    "changeTheme": "Mudar o tema", "copy": "Copiar", "downloadPdf": "Baixar PDF", "tocFilter": "Filtrar por título",
    "tocToggleButton": "Mostrar / ocultar o sumário", "search": "Buscar", "searchResults": "Resultados da busca por",
    "searchResultsCount": "{count} resultados para \"{query}\"", "searchNoResults": "Nenhum resultado para \"{query}\"",
    "pageFirst": "Primeira", "pagePrev": "Anterior", "pageNext": "Próxima", "pageLast": "Última",
    "note": "Observação", "warning": "Aviso", "tip": "Dica", "important": "Importante", "caution": "Cuidado",
    "namespacesInSubtitle": "Namespaces", "classesInSubtitle": "Classes", "structsInSubtitle": "Structs",
    "interfacesInSubtitle": "Interfaces", "enumsInSubtitle": "Enums", "delegatesInSubtitle": "Delegates",
    "constructorsInSubtitle": "Construtores", "fieldsInSubtitle": "Campos", "propertiesInSubtitle": "Propriedades",
    "methodsInSubtitle": "Métodos", "eventsInSubtitle": "Eventos", "operatorsInSubtitle": "Operadores",
    "eiisInSubtitle": "Implementações explícitas de interface", "membersInSubtitle": "Membros",
    "improveThisDoc": "Editar esta página", "viewSource": "Ver o código", "inheritance": "Herança",
    "derived": "Derivados", "inheritedMembers": "Membros herdados", "package": "Pacote", "namespace": "Namespace",
    "assembly": "Assembly", "syntax": "Sintaxe", "overrides": "Sobrescreve", "implements": "Implementa",
    "remarks": "Comentários", "examples": "Exemplos", "seealso": "Veja também", "declaration": "Declaração",
    "parameters": "Parâmetros", "typeParameters": "Parâmetros de tipo", "type": "Tipo", "name": "Nome",
    "description": "Descrição", "returns": "Retorna", "fieldValue": "Valor do campo",
    "propertyValue": "Valor da propriedade", "eventType": "Tipo do evento", "exceptions": "Exceções",
    "condition": "Condição", "extensionMethods": "Métodos de extensão",
}


def run(*args, cwd=None):
    print("$", " ".join(str(a) for a in args), flush=True)
    subprocess.run([str(a) for a in args], cwd=cwd or ROOT, check=True)


def page(package):
    return f"{package.lower()}.md"


def packable_projects():
    out = subprocess.run(["git", "ls-files", "*.csproj"], cwd=ROOT, capture_output=True, text=True, check=True)
    names = set()
    for rel in out.stdout.split():
        if "<IsPackable>false</IsPackable>" not in (ROOT / rel).read_text(encoding="utf-8"):
            names.add(Path(rel).stem)
    return names


def check_groups():
    grouped = [p for _, packages in GROUPS for p in packages]
    missing = packable_projects() - set(grouped)
    unknown = set(grouped) - packable_projects()
    if missing or unknown or len(grouped) != len(set(grouped)):
        sys.exit(f"GROUPS desatualizado: fora dos grupos {sorted(missing)}, sem projeto {sorted(unknown)}")


def split_title(title):
    """'⚙️ Configuration' -> 'Configuration' (o ícone é a primeira palavra quando não tem letra nem dígito)."""
    first, _, rest = title.partition(" ")
    return rest if rest and not any(c.isalnum() for c in first) else title


def fix_anchor(fragment):
    # O DocFX descarta o seletor de variação (U+FE0F) dos títulos com ⚙️ e ⚠️; o GitHub o mantém na âncora.
    return fragment.replace("️", "")


def rewrite_links(text, lang):
    """Troca os links do GitHub para páginas do site no mesmo idioma; o resto continua apontando para o GitHub."""
    readme = LANGS[lang]["readme"]

    def to_package(m):
        package, file, fragment = m.group(1), m.group(2), m.group(3) or ""
        if file != readme:
            return m.group(0)  # README do outro idioma: continua no GitHub
        return f"{page(package)}{fix_anchor(fragment)})"

    text = re.sub(re.escape(BLOB) + r"/(Tooark(?:\.[A-Za-z]+)*)/(README(?:\.pt-BR)?\.md)(#[^)\s]+)?\)",
                  to_package, text)
    text = re.sub(re.escape(TREE) + r"/(Tooark(?:\.[A-Za-z]+)*)/?\)",
                  lambda m: f"{page(m.group(1))})" if (ROOT / m.group(1) / readme).exists()
                  and m.group(1) != "Tooark.Tests" else m.group(0), text)
    text = re.sub(re.escape(BLOB) + r"/" + re.escape(readme) + r"(#[^)\s]+)?\)",
                  lambda m: f"overview.md{fix_anchor(m.group(1) or '')})", text)
    # Âncoras dentro da própria página.
    return re.sub(r"\]\((#[^)\s]+)\)", lambda m: f"]({fix_anchor(m.group(1))})", text)


def sections(lines):
    """Divide em (preâmbulo, [(título, linhas)]) pelas seções ##, ignorando blocos de código."""
    preamble, result, current, in_fence = [], [], None, False
    for line in lines:
        if FENCE.match(line):
            in_fence = not in_fence
        if not in_fence and line.startswith("## "):
            current = (line[3:].strip(), [])
            result.append(current)
            continue
        (current[1] if current else preamble).append(line)
    return preamble, result


def clean(lines):
    """Tira os separadores --- e as linhas em branco repetidas, fora dos blocos de código."""
    out, in_fence = [], False
    for line in lines:
        if FENCE.match(line):
            in_fence = not in_fence
        if not in_fence and (line.strip() == "---" or (not line.strip() and out and not out[-1].strip())):
            continue
        out.append(line)
    while out and not out[-1].strip():
        out.pop()
    return out


def package_page(package, lang):
    text = (ROOT / package / LANGS[lang]["readme"]).read_text(encoding="utf-8")
    preamble, secs = sections(text.split("\n"))
    preamble = [l for l in preamble if not l.startswith(("🌍 **Languages:**", "🌍 **Idiomas:**"))]
    body = list(preamble)
    for title, lines in secs:
        if split_title(title) in DROP_SECTIONS:
            continue
        body += ["", f"## {title}", *lines]
    source = f"{BLOB}/{package}/{LANGS[lang]['readme']}"
    description = re.sub(r"\*\*|`", "", summary(package, lang))
    front = ["---", f"title: {package}", f"description: {json.dumps(description, ensure_ascii=False)}", "---", ""]
    edit = {"en": "Source of this page", "pt-BR": "Fonte desta página"}[lang]
    footer = ["", f"> [!NOTE]", f"> {edit}: [`{package}/{LANGS[lang]['readme']}`]({source})"]
    return "\n".join(front + clean([rewrite_links(l, lang) for l in body]) + footer) + "\n"


def overview_page(lang):
    cfg = LANGS[lang]
    text = (ROOT / cfg["readme"]).read_text(encoding="utf-8")
    _, secs = sections(text.split("\n"))
    body = [f"# {cfg['overview']}", ""]
    for title, lines in secs:
        if title in cfg["overview_sections"]:
            body += ["", f"## {title}", *lines]
    out = []
    for line in clean(body):
        # Na tabela de pacotes, o nome vira link para a página do pacote.
        m = re.match(r"^\| `(Tooark(?:\.[A-Za-z]+)*)` +\|", line)
        if m:
            line = line.replace(f"`{m.group(1)}`", f"[`{m.group(1)}`]({page(m.group(1))})", 1)
        out.append(rewrite_links(line, lang))
    return "\n".join(out) + "\n"


def summary(package, lang):
    """Primeira frase do README, para o cartão da página inicial e a descrição da página."""
    text = (ROOT / package / LANGS[lang]["readme"]).read_text(encoding="utf-8")
    para = []
    for line in text.split("\n")[1:]:
        if not line.strip():
            if para:
                break
            continue
        para.append(line.strip())
    sentence = " ".join(para)
    sentence = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", sentence)
    cut = re.search(r"(?<=[.:])\s", sentence)
    if cut and cut.start() > 40:
        sentence = sentence[: cut.start()].rstrip(":") + ("." if sentence[cut.start() - 1] == ":" else "")
    if len(sentence) > 190:
        sentence = sentence[:190].rsplit(" ", 1)[0] + "…"
    return sentence


def inline_html(markdown):
    out = html.escape(markdown, quote=False)
    out = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", out)
    return re.sub(r"`([^`]+)`", r"<code>\1</code>", out)


def cards(lang):
    blocks = []
    for names, packages in GROUPS:
        items = "\n".join(
            f'<a class="tk-card" href="packages/{page(package)}">'
            f'<span class="tk-card-title">{package}</span>'
            f'<span class="tk-card-text">{inline_html(summary(package, lang))}</span></a>'
            for package in packages
        )
        blocks.append(f'<section class="tk-group">\n<h2>{names[lang]}</h2>\n<div class="tk-grid">\n{items}\n</div>\n'
                      f"</section>")
    return "\n".join(blocks)


def version():
    return re.search(r"<Version>([^<]+)</Version>", (ROOT / "Directory.Build.props").read_text("utf-8")).group(1)


def yaml_toc(items, indent=0):
    out = []
    pad = " " * indent
    for item in items:
        out.append(f"{pad}- name: {json.dumps(item['name'], ensure_ascii=False)}")
        for key in ("href", "homepage"):
            if key in item:
                out.append(f"{pad}  {key}: {item[key]}")
        if "items" in item:
            out.append(f"{pad}  items:")
            out += yaml_toc(item["items"], indent + 2)
    return out


def docfx_config(lang, with_api):
    cfg = LANGS[lang]
    footer = {
        "en": "Tooark · <a href=\"https://github.com/Tooark/nuget-tooark/blob/main/LICENSE\">BSD 3-Clause License</a>"
              " · <a href=\"https://www.nuget.org/profiles/Tooark\">NuGet</a>"
              " · <a href=\"https://tooark.com\">tooark.com</a>",
        "pt-BR": "Tooark · <a href=\"https://github.com/Tooark/nuget-tooark/blob/main/LICENSE\">Licença BSD 3-Clause</a>"
                 " · <a href=\"https://www.nuget.org/profiles/Tooark\">NuGet</a>"
                 " · <a href=\"https://tooark.com\">tooark.com</a>",
    }[lang]
    content = ["*.md", "toc.yml", "packages/*.md", "packages/toc.yml"]
    if with_api:
        content += ["api/*.md", "api/*.yml"]
    return {
        "build": {
            "content": [{"files": content}],
            "resource": [{"files": ["images/**"]}],
            "template": ["default", "modern", "template"],
            "output": "_site",
            "sitemap": {"baseUrl": cfg["base_url"], "changefreq": "weekly"},
            "globalMetadata": {
                "_appName": "Tooark",
                "_appTitle": "Tooark",
                "_appLogoPath": "images/tooark.svg",
                "_appFaviconPath": "images/tooark.svg",
                "_appFooter": footer,
                "_lang": lang,
                "_enableSearch": True,
                "_disableContribution": True,
                "_noindex": False,
            },
        }
    }


def api_metadata():
    """Gera os YAML da API uma vez, a partir dos projetos empacotáveis (docs XML em português)."""
    meta = OBJ / "metadata"
    shutil.rmtree(meta, ignore_errors=True)
    meta.mkdir(parents=True)
    projects = sorted(packable_projects())
    config = {
        "metadata": [{
            "src": [{"src": "../../..", "files": [f"{p}/{p}.csproj" for p in projects]}],
            "dest": "api",
            "properties": {"TargetFramework": "net10.0"},
            "namespaceLayout": "flattened",
            "memberLayout": "samePage",
            "enumSortOrder": "declaringOrder",
        }]
    }
    (meta / "docfx.json").write_text(json.dumps(config, indent=2), encoding="utf-8")
    run("dotnet", "tool", "run", "docfx", "metadata", meta / "docfx.json")
    if not list((meta / "api").glob("*.yml")):
        sys.exit("docfx metadata não gerou a API: confira o log acima")
    return meta / "api"


def build_lang(lang, api_dir):
    cfg = LANGS[lang]
    work = OBJ / lang
    shutil.rmtree(work, ignore_errors=True)
    shutil.copytree(DOCS / lang, work)
    shutil.copytree(DOCS / "template", work / "template")
    if lang == "pt-BR":
        (work / "template" / "token.json").write_text(json.dumps(TOKENS_PT, ensure_ascii=False, indent=2),
                                                     encoding="utf-8")
    (work / "images").mkdir(exist_ok=True)
    shutil.copy(ROOT / "Media" / "tooark.svg", work / "images" / "tooark.svg")

    index = (work / "index.md").read_text(encoding="utf-8")
    index = index.replace("<!-- tooark:packages -->", cards(lang)).replace("<!-- tooark:version -->", version())
    (work / "index.md").write_text(index, encoding="utf-8")

    packages = work / "packages"
    packages.mkdir()
    (packages / "overview.md").write_text(overview_page(lang), encoding="utf-8")
    toc = [{"name": cfg["overview"], "href": "overview.md"}]
    for names, group in GROUPS:
        for package in group:
            (packages / page(package)).write_text(package_page(package, lang), encoding="utf-8")
        toc.append({"name": names[lang], "items": [{"name": p, "href": page(p)} for p in group]})
    (packages / "toc.yml").write_text("\n".join(yaml_toc(toc)) + "\n", encoding="utf-8")

    nav = [{"name": cfg["nav"]["home"], "href": "index.md"},
           {"name": cfg["nav"]["packages"], "href": "packages/", "homepage": "packages/overview.md"}]
    if api_dir:
        shutil.copytree(api_dir, work / "api", dirs_exist_ok=True)
        nav.append({"name": cfg["nav"]["api"], "href": "api/", "homepage": "api/index.md"})
    else:
        shutil.rmtree(work / "api", ignore_errors=True)
    (work / "toc.yml").write_text("\n".join(yaml_toc(nav)) + "\n", encoding="utf-8")

    (work / "docfx.json").write_text(json.dumps(docfx_config(lang, bool(api_dir)), indent=2, ensure_ascii=False),
                                     encoding="utf-8")
    run("dotnet", "tool", "run", "docfx", "build", work / "docfx.json", "--warningsAsErrors")
    # Os source maps do JavaScript do template só servem para depurar o próprio template: fora do site.
    shutil.copytree(work / "_site", cfg["out"], dirs_exist_ok=True, ignore=shutil.ignore_patterns("*.map"))


def main():
    check_groups()
    if "--check" in sys.argv:
        print("OK: todos os projetos empacotáveis estão nos grupos do site.")
        return
    with_api = "--no-api" not in sys.argv
    shutil.rmtree(SITE, ignore_errors=True)
    api_dir = api_metadata() if with_api else None
    for lang in LANGS:
        build_lang(lang, api_dir)
    print(f"Site gerado em {SITE}")


if __name__ == "__main__":
    main()
