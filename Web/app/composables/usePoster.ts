export const POSTER_SANS = 'Saira'
export const POSTER_MONO = 'JetBrains Mono'

const EXPORT_CSS = `text{font-family:'${POSTER_SANS}',sans-serif}.mono{font-family:'${POSTER_MONO}',monospace}`

let ruler: CanvasRenderingContext2D | null = null

export function measurePosterText(value: string, size: number, weight: number, family = POSTER_SANS): number {
  ruler ??= document.createElement('canvas').getContext('2d')
  if (!ruler) return 0

  ruler.font = `${weight} ${size}px '${family}'`
  return ruler.measureText(value).width
}

function coversLatin(range: string): boolean {
  if (!range) return true

  return range.split(',').some((part) => {
    const [from, to] = part.trim().replace(/^u\+/i, '').split('-')
    const start = Number.parseInt(from!, 16)
    const end = to ? Number.parseInt(to, 16) : start
    return start <= 0x41 && end >= 0x41
  })
}

async function dataUrl(url: string): Promise<string> {
  const blob = await (await fetch(url)).blob()

  return await new Promise<string>((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => { resolve(reader.result as string) }
    reader.onerror = () => { reject(reader.error) }
    reader.readAsDataURL(blob)
  })
}

// O arquivo exportado é aberto fora da página, onde nenhuma webfont é baixada:
// sem as fontes embutidas em base64 o texto cai na fonte do sistema e o layout
// — que é medido em cima do Saira e do JetBrains Mono — desalinha.
async function embeddedFonts(): Promise<string> {
  const rules: string[] = []

  for (const sheet of Array.from(document.styleSheets)) {
    let sheetRules: CSSRuleList

    try {
      sheetRules = sheet.cssRules
    } catch {
      continue
    }

    for (const rule of Array.from(sheetRules)) {
      if (!(rule instanceof CSSFontFaceRule)) continue

      const family = rule.style.getPropertyValue('font-family').replace(/['"]/g, '').trim()
      if (family !== POSTER_SANS && family !== POSTER_MONO) continue
      if (!coversLatin(rule.style.getPropertyValue('unicode-range'))) continue

      const url = /url\(["']?([^"')]+)/.exec(rule.style.getPropertyValue('src'))?.[1]
      if (!url) continue

      rules.push(rule.cssText.replace(/src:[^;]+;/, `src:url(${await dataUrl(url)});`))
    }
  }

  return rules.join('')
}

function save(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  link.href = url
  link.download = name
  link.click()

  URL.revokeObjectURL(url)
}

export function usePoster(width: number, height: number, fileName: string, zoom: Ref<number> = ref(0)) {
  // Todo o layout é medido em cima do Saira e do JetBrains Mono, então o pôster só
  // é montado depois que as webfonts carregam: com a fonte de fallback as larguras
  // mudam e o desenho inteiro desalinha.
  const fontsLoaded = ref(false)

  const posterEl = ref<SVGSVGElement | null>(null)
  const busy = ref<string | null>(null)

  const zooms = [
    { label: 'Ajustar à tela', value: 0 },
    { label: '50%', value: 0.5 },
    { label: '75%', value: 0.75 },
    { label: '100%', value: 1 }
  ]

  const posterStyle = computed(() => zoom.value === 0
    ? { width: '100%', maxWidth: `${width}px`, height: 'auto' }
    : { width: `${width * zoom.value}px`, minWidth: `${width * zoom.value}px`, height: 'auto' })

  async function serialize(): Promise<string> {
    const clone = posterEl.value!.cloneNode(true) as SVGSVGElement
    clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg')
    clone.removeAttribute('style')

    const style = document.createElementNS('http://www.w3.org/2000/svg', 'style')
    style.textContent = `${await embeddedFonts()}${EXPORT_CSS}`
    clone.insertBefore(style, clone.firstChild)

    return new XMLSerializer().serializeToString(clone)
  }

  async function downloadSvg() {
    busy.value = 'svg'

    try {
      save(new Blob([await serialize()], { type: 'image/svg+xml' }), `${fileName}.svg`)
    } finally {
      busy.value = null
    }
  }

  async function downloadPng(scale: number) {
    busy.value = 'png'
    const source = URL.createObjectURL(new Blob([await serialize()], { type: 'image/svg+xml' }))

    try {
      const image = new Image()
      image.src = source
      await image.decode()

      const canvas = document.createElement('canvas')
      canvas.width = width * scale
      canvas.height = height * scale
      canvas.getContext('2d')!.drawImage(image, 0, 0, canvas.width, canvas.height)

      const blob = await new Promise<Blob | null>((resolve) => { canvas.toBlob(resolve, 'image/png') })
      if (blob) save(blob, `${fileName}${scale > 1 ? `@${scale}x` : ''}.png`)
    } finally {
      URL.revokeObjectURL(source)
      busy.value = null
    }
  }

  const pngItems = [
    [
      { label: `${width} × ${height}`, icon: 'i-lucide-image', onSelect: () => { downloadPng(1) } },
      { label: `${width * 2} × ${height * 2} (@2x)`, icon: 'i-lucide-images', onSelect: () => { downloadPng(2) } }
    ]
  ]

  onMounted(async () => {
    const faces = [
      ...[600, 700, 800].map(weight => `${weight} 24px ${POSTER_SANS}`),
      ...[400, 600].map(weight => `${weight} 14px '${POSTER_MONO}'`)
    ]

    // Uma fonte que não baixa rejeita o load e não pode travar o pôster no
    // spinner: a medição usa a mesma fonte que o navegador vai desenhar, então
    // no fallback o desenho continua alinhado — só troca o tipo.
    await Promise.all(faces.map(face => document.fonts.load(face).catch(() => {})))

    fontsLoaded.value = true
  })

  return { fontsLoaded, posterEl, busy, zooms, zoom, posterStyle, downloadSvg, pngItems }
}
