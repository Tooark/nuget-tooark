// Extensões do template moderno do DocFX para o site do Tooark: ícones do navbar, busca em português na
// versão pt-BR e o seletor de idioma, que leva à mesma página no outro idioma.

const lang = document.documentElement.lang === 'pt-BR' ? 'pt-BR' : 'en'

const languages = [
  { code: 'pt-BR', short: 'PT', label: 'Ler em português' },
  { code: 'en', short: 'EN', label: 'Read in English' },
]

// O inglês fica na raiz do site e o português em pt-BR/; o docfx:rel leva à raiz do idioma atual.
function counterpartUrl() {
  const rel = document.querySelector('meta[name="docfx:rel"]')?.getAttribute('content') || './'
  const docsetRoot = new URL(rel, location.href)
  const page = (location.origin + location.pathname).slice(docsetRoot.href.length)
  return new URL((lang === 'pt-BR' ? '../' : 'pt-BR/') + page, docsetRoot).href
}

// A pílula PT | EN do tooark.com: o idioma atual fica marcado e o outro é o link.
function addLanguageSwitch() {
  const panel = document.getElementById('navpanel')
  if (!panel || document.getElementById('tk-lang')) return
  const group = document.createElement('div')
  group.id = 'tk-lang'
  group.className = 'tk-lang'
  group.setAttribute('role', 'group')
  group.setAttribute('aria-label', lang === 'pt-BR' ? 'Idioma' : 'Language')
  for (const item of languages) {
    const current = item.code === lang
    const element = document.createElement(current ? 'span' : 'a')
    element.className = 'tk-lang-item'
    element.textContent = item.short
    if (current) {
      element.setAttribute('aria-current', 'true')
    } else {
      element.href = counterpartUrl()
      element.hreflang = item.code
      element.title = item.label
      element.setAttribute('aria-label', item.label)
    }
    group.appendChild(element)
  }
  panel.appendChild(group)
}

export default {
  defaultTheme: 'auto',
  lunrLanguages: lang === 'pt-BR' ? ['en', 'pt'] : ['en'],
  iconLinks: [
    { icon: 'github', href: 'https://github.com/Tooark/nuget-tooark', title: 'GitHub' },
    { icon: 'box-seam', href: 'https://www.nuget.org/profiles/Tooark', title: 'NuGet' },
  ],
  start: () => addLanguageSwitch(),
}
