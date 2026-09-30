// Extensões do template moderno do DocFX para o site do Tooark: ícones do navbar, busca em português na
// versão pt-BR e o seletor de idioma, que leva à mesma página no outro idioma.

const lang = document.documentElement.lang === 'pt-BR' ? 'pt-BR' : 'en'

const switchTo = {
  en: { label: 'Ler em português', short: 'PT', hreflang: 'pt-BR' },
  'pt-BR': { label: 'Read in English', short: 'EN', hreflang: 'en' },
}[lang]

// O inglês fica na raiz do site e o português em pt-BR/; o docfx:rel leva à raiz do idioma atual.
function counterpartUrl() {
  const rel = document.querySelector('meta[name="docfx:rel"]')?.getAttribute('content') || './'
  const docsetRoot = new URL(rel, location.href)
  const page = (location.origin + location.pathname).slice(docsetRoot.href.length)
  return new URL((lang === 'pt-BR' ? '../' : 'pt-BR/') + page, docsetRoot).href
}

function addLanguageSwitch() {
  const panel = document.getElementById('navpanel')
  if (!panel || document.getElementById('tk-lang')) return
  const link = document.createElement('a')
  link.id = 'tk-lang'
  link.className = 'btn border-0 tk-lang'
  link.href = counterpartUrl()
  link.hreflang = switchTo.hreflang
  link.title = switchTo.label
  link.setAttribute('aria-label', switchTo.label)
  link.innerHTML = `<i class="bi bi-translate" aria-hidden="true"></i><span>${switchTo.short}</span>`
  panel.appendChild(link)
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
