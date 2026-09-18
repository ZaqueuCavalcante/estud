<script setup lang="ts">
interface NodeContent {
  title: string
  sub: string
  color: string
  icon: string
  chips?: string[]
  chipText?: string
}

interface ItemContent {
  title: string
  sub?: string
  color: string
  icon: string
}

interface Frame {
  x: number
  y: number
  w: number
  h: number
  rx: number
}

interface Tile {
  x: number
  y: number
  size: number
  rx: number
  color: string
}

interface IconMark {
  name: string
  x: number
  y: number
  size: number
  color: string
}

interface Label {
  x: number
  y: number
  size: number
  weight: number
  fill: string
  value: string
  mono?: boolean
  anchor?: 'start' | 'middle' | 'end'
}

interface Chip {
  x: number
  y: number
  w: number
  size: number
  color: string
  textColor: string
  label: string
}

interface Block {
  id: string
  rect?: Frame & { fill: string }
  tile: Tile
  icon: IconMark
  title: Label
  sub?: Label
}

interface Header {
  id: string
  icon: IconMark
  title: Label
}

interface Point {
  id: string
  icon: IconMark
  title: Label
  sub?: string
}

interface Pill {
  x: number
  y: number
  w: number
  label: string
}

interface MiniCard {
  id: string
  rect: Frame
  icon: IconMark
  title: Label
}

const W = 1080
const H = 1350
const CX = W / 2
const M = 64

const SANS = 'Saira'
const MONO = 'JetBrains Mono'

const C = {
  bg: '#2f1c51',
  card: '#211439',
  surface: 'rgba(255,255,255,0.06)',
  cardTop: '#35205b',
  cardStroke: 'rgba(255,255,255,0.09)',
  text: '#fafafa',
  muted: '#a1a1aa',
  violet: '#8b5cf6',
  violetLight: '#c4b5fd',
  line: '#a78bfa'
}

const TEAL = '#2dd4bf'
const ROSE = '#fb7185'
const AMBER = '#F59E0B'

// Mesmo path do <EstudIcon>, em viewBox 24x24.
const ESTUD_E = 'M 10.98 20.5Q 9.46 20.5 8.27 19.61Q 7.08 18.72 6.4 17.13Q 5.73 15.53 5.73 13.49Q 5.73 11.37 6.34 9.54Q 6.96 7.72 8.08 6.37Q 9.2 5.02 10.71 4.26Q 12.22 3.5 13.97 3.5Q 16.09 3.5 17.18 4.56Q 18.27 5.62 18.27 7.66Q 18.27 9.19 17.44 10.41Q 16.61 11.63 15.16 12.32Q 13.71 13.01 11.84 13.01Q 10.92 13.01 10.12 12.9Q 9.32 12.8 8.57 12.63L 8.65 11.63H 9.72Q 11.38 11.63 12.62 11.07Q 13.85 10.51 14.54 9.47Q 15.23 8.44 15.23 7.09Q 15.23 6.08 14.76 5.54Q 14.28 4.99 13.42 4.99Q 12.33 4.99 11.47 5.81Q 10.61 6.63 10.0 7.94Q 9.4 9.24 9.09 10.74Q 8.77 12.23 8.77 13.58Q 8.77 16.11 9.53 17.38Q 10.29 18.66 11.78 18.66Q 14.6 18.66 16.41 15.85Q 17.47 16.02 17.47 16.62Q 16.24 18.63 14.67 19.57Q 13.11 20.5 10.98 20.5Z'

const LINKEDIN = { handle: '/in/zaqueu-cavalcante', icon: 'i-simple-icons-linkedin', color: '#4aa3f0' }

const TITLE = { lead: 'Como o Estud roda ', accent: 'em produção?' }

const DOCS: ItemContent[] = [
  { title: 'Documentação', sub: '/docs · Nuxt Content', color: '#c4b5fd', icon: 'i-lucide-book-open-text' },
  { title: 'Referência da API', sub: '/api/docs · Scalar', color: '#e4e4e7', icon: 'i-simple-icons-scalar' }
]

const R2 = {
  title: 'Cloudflare R2',
  icon: 'i-estud-r2',
  color: '#F38020',
  items: [
    { title: 'Imagens', sub: 'Perfil · Plano de Aula', color: '#38bdf8', icon: 'i-lucide-image' },
    { title: 'Documentos', sub: 'Atividades · Entregas', color: '#c4b5fd', icon: 'i-lucide-file-text' }
  ] as ItemContent[]
}

const BROWSER: NodeContent = { title: 'Browser', sub: 'https://estud.com.br', color: '#e4e4e7', icon: 'i-lucide-globe' }
const CLOUDFLARE: NodeContent = { title: 'Cloudflare', sub: 'DNS · Proxy · TLS', color: '#F38020', icon: 'i-simple-icons-cloudflare' }
const CADDY: NodeContent = { title: 'Caddy', sub: 'Reverse Proxy', color: '#22A6E0', icon: 'i-simple-icons-caddy' }
const WEB: NodeContent = { title: 'Web', sub: 'Vue · Nuxt', color: '#00DC82', icon: 'i-simple-icons-nuxt' }
const BACK: NodeContent = { title: 'Back', sub: 'C# · ASP.NET', color: '#9B7BFF', icon: 'i-simple-icons-dotnet' }
const POSTGRES: NodeContent = { title: 'Postgres', sub: 'PostgreSQL', color: '#5B8DEF', icon: 'i-simple-icons-postgresql' }

const ASYNC: NodeContent = {
  title: 'Workers',
  sub: 'Quartz.NET · Outbox',
  color: AMBER,
  icon: 'i-lucide-list-end',
  chips: ['Events', 'Commands', 'Jobs'],
  chipText: '#fcd34d'
}

const RAILWAY = { title: 'Railway', hint: '*.railway.internal', icon: 'i-simple-icons-railway' }

const OTEL = {
  title: 'OpenTelemetry',
  icon: 'i-simple-icons-opentelemetry',
  color: '#8b9cff',
  signals: [
    { label: 'Metrics', icon: 'i-lucide-chart-line', color: '#60a5fa' },
    { label: 'Logs', icon: 'i-lucide-scroll-text', color: '#a3e635' },
    { label: 'Traces', icon: 'i-lucide-waypoints', color: '#f472b6' }
  ],
  points: [
    { title: 'Grafana', sub: 'OTLP · Back', icon: 'i-simple-icons-grafana', color: '#F46800' },
    { title: 'PostHog', sub: 'Analytics · Web', icon: 'i-simple-icons-posthog', color: '#F9BD2B' }
  ]
}

const IDENTITY = {
  title: 'Identity',
  icon: 'i-lucide-fingerprint',
  color: TEAL,
  chipText: '#99f6e4',
  chips: ['RBAC', 'JWT', '2FA', 'Magic Links'],
  rows: [
    { title: 'Google Sign-Up/In', sub: 'OAuth · One Tap', color: '#8ab4f8', icon: 'i-simple-icons-google' },
    { title: 'SSO Multi-Tenant', sub: 'OIDC · Okta · Azure · Auth0', color: TEAL, icon: 'i-lucide-building-2' }
  ] as ItemContent[]
}

const WEBHOOKS = {
  title: 'Webhooks',
  icon: 'i-lucide-webhook',
  color: ROSE,
  chipText: '#fecdd3',
  chips: ['StudentCreated', 'ActivityPublished'],
  rows: [
    { title: 'Subscriptions', sub: 'Domain Events', color: ROSE, icon: 'i-lucide-arrow-up-right' },
    { title: 'Async Callbacks', sub: 'Commands · Retry', color: AMBER, icon: 'i-lucide-activity' }
  ] as ItemContent[]
}

const TESTS = {
  title: 'Testing',
  icon: 'i-lucide-flask-conical',
  items: [
    { title: 'Unit', sub: 'NUnit · FluentAssertions', color: '#38bdf8', icon: 'i-lucide-box' },
    { title: 'Integration', sub: 'API · Postgres', color: '#34d399', icon: 'i-lucide-plug' },
    { title: 'Mutation', sub: 'Stryker.NET', color: '#f472b6', icon: 'i-lucide-dna' }
  ] as ItemContent[],
  coverage: { title: 'Coverage Report', icon: 'i-lucide-chart-column', color: '#a3e635' },
  actions: { title: 'GitHub Actions', icon: 'i-simple-icons-githubactions', color: '#2088FF' }
}

// Todo o layout é medido em cima do Saira e do JetBrains Mono, então o pôster só
// é montado depois que as webfonts carregam: com a fonte de fallback as larguras
// mudam e o desenho inteiro desalinha.
const fontsLoaded = ref(false)

let ruler: CanvasRenderingContext2D | null = null

function measure(value: string, size: number, weight: number, family = SANS): number {
  ruler ??= document.createElement('canvas').getContext('2d')
  if (!ruler) return 0

  ruler.font = `${weight} ${size}px '${family}'`
  return ruler.measureText(value).width
}

function chipsWidth(labels: string[], size = 13): number {
  return labels.reduce((sum, label) => sum + label.length * size * 0.6 + 18, 0) + 6 * (labels.length - 1)
}

function chipRow(x: number, y: number, labels: string[], color: string, textColor: string, size = 13): Chip[] {
  let cursor = x

  return labels.map((label) => {
    const w = label.length * size * 0.6 + 18
    const chip: Chip = { x: cursor, y, w, size, color, textColor, label }
    cursor += w + 6
    return chip
  })
}

function node(content: NodeContent, x: number, y: number, w: number, h = 80): Block {
  const textW = Math.max(
    measure(content.title, 24, 600),
    measure(content.sub, 14, 400, MONO),
    content.chips ? chipsWidth(content.chips) : 0
  )
  const bx = x + (w - (64 + textW)) / 2
  const by = y + (h - (content.chips ? 80 : 48)) / 2

  return {
    id: content.title,
    rect: { x, y, w, h, rx: 18, fill: C.card },
    tile: { x: bx, y: by, size: 48, rx: 13, color: content.color },
    icon: { name: content.icon, x: bx + 11, y: by + 11, size: 26, color: content.color },
    title: { x: bx + 64, y: by + 22, size: 24, weight: 600, fill: C.text, value: content.title },
    sub: { x: bx + 64, y: by + 46, size: 14, weight: 400, fill: C.muted, value: content.sub, mono: true }
  }
}

function item(content: ItemContent, x: number, y: number, variant: 'user' | 'doc' | 'row'): Block {
  const tile = variant === 'row' ? 34 : 36
  const titleSize = { user: 17, doc: 18, row: 16 }[variant]

  return {
    id: `${variant}-${content.title}`,
    tile: { x, y, size: tile, rx: 10, color: content.color },
    icon: { name: content.icon, x: x + 8, y: y + 8, size: variant === 'row' ? 18 : 20, color: content.color },
    title: { x: x + tile + 12, y: y + (variant === 'user' ? 24 : 15), size: titleSize, weight: 600, fill: C.text, value: content.title },
    sub: content.sub
      ? {
          x: x + tile + 12,
          y: y + (variant === 'row' ? 32 : 33),
          size: variant === 'row' ? 12 : 13,
          weight: 400,
          fill: C.muted,
          value: content.sub,
          mono: true
        }
      : undefined
  }
}

function testItem(content: ItemContent, x: number, y: number, w: number, h: number): Block {
  const tileY = y + (h - 34) / 2

  return {
    id: `test-${content.title}`,
    rect: { x, y, w, h, rx: 13, fill: C.surface },
    tile: { x: x + 12, y: tileY, size: 34, rx: 10, color: content.color },
    icon: { name: content.icon, x: x + 20, y: tileY + 8, size: 18, color: content.color },
    title: { x: x + 58, y: y + h / 2 - 3, size: 17, weight: 600, fill: C.text, value: content.title },
    sub: { x: x + 58, y: y + h / 2 + 15, size: 12, weight: 400, fill: C.muted, value: content.sub!, mono: true }
  }
}

function header(id: string, x: number, y: number, icon: string, title: string, color: string, centerIn?: number): Header {
  const bx = centerIn ? x + (centerIn - (34 + measure(title, 22, 700))) / 2 : x + 18

  return {
    id,
    icon: { name: icon, x: bx, y: y + 16, size: 24, color },
    title: { x: bx + 34, y: y + 37, size: 22, weight: 700, fill: C.text, value: title }
  }
}

function point(id: string, x: number, y: number, content: { title: string, icon: string, color: string, sub?: string, mono?: boolean }, size = 17): Point {
  return {
    id,
    icon: { name: content.icon, x, y: y - 15, size: 19, color: content.color },
    title: { x: x + 30, y, size, weight: 600, fill: content.mono ? C.muted : C.text, value: content.title, mono: content.mono },
    sub: content.sub
  }
}

function pill(x: number, y: number, label: string): Pill {
  return { x, y, w: label.length * 9.4 + 28, label }
}

const poster = computed(() => {
  const top = [BROWSER, CLOUDFLARE, CADDY, WEB, BACK]
  const topW = 104 + Math.max(
    ...top.map(n => measure(n.title, 24, 600)),
    ...top.map(n => measure(n.sub, 14, 400, MONO))
  )
  const topX = CX - topW / 2

  const rowAH = 204
  const rowCH = 200
  const testsH = 132
  const nodeH = 80
  const lowH = 114
  const pgH = lowH + 20

  // O card da Railway tem a altura do próprio conteúdo, não a do espaço que
  // sobra entre as linhas de cima e de baixo. O que sobra na altura do pôster é
  // dividido igualmente entre as três folgas que separam as quatro linhas.
  const caddyTop = 44
  const midTop = caddyTop + nodeH + 80
  const lowTop = midTop + nodeH + 94
  const railwayH = lowTop + lowH + (pgH - lowH) / 2 + 28

  const rowA = 136
  const gap = (H - 32 - rowA - rowAH - railwayH - rowCH - testsH) / 3
  const browserY = rowA
  const cloudflareY = rowA + 124

  const sideW = Math.max(
    88 + Math.max(...DOCS.map(d => measure(d.title, 18, 600)), ...DOCS.map(d => measure(d.sub!, 13, 400, MONO))),
    88 + Math.max(...R2.items.map(i => measure(i.title, 18, 600)), ...R2.items.map(i => measure(i.sub!, 13, 400, MONO)))
  )

  const docs: Frame = { x: M, y: rowA, w: sideW, h: rowAH, rx: 22 }
  const r2: Frame = { x: W - M - sideW, y: rowA, w: sideW, h: rowAH, rx: 22 }

  const card: Frame = { x: M, y: rowA + rowAH + gap, w: W - 2 * M, h: railwayH, rx: 28 }
  const rowCY = card.y + railwayH + gap
  const tests: Frame = { x: M, y: rowCY + rowCH + gap, w: W - 2 * M, h: testsH, rx: 22 }
  const colW = (W - 2 * M - 32) / 3
  const colX = (i: number) => M + i * (colW + 16)

  const caddyY = card.y + caddyTop
  const caddyBottom = caddyY + nodeH
  const splitY = caddyBottom + 40
  const webX = card.x + 44
  const backX = card.x + card.w - 44 - 400
  const pgW = 270
  const pgX = backX + 200 - pgW / 2
  const pgRy = 16
  const dashX = backX + 200 - topW / 2 + 24
  const midY = card.y + midTop
  const midBottom = midY + nodeH
  const linkY = midBottom + 39
  const lowY = card.y + lowTop
  const lowCy = lowY + lowH / 2
  const pgY = lowCy - pgH / 2
  const testW = (tests.w - 64) / 3

  const asyncNode = node(ASYNC, webX, lowY, 400, lowH)

  // O corpo visível do cilindro começa embaixo da elipse do topo (2 × ry) e vai
  // até a base; o conteúdo se centraliza nessa faixa, não na caixa inteira.
  const pgTop = pgY + (2 * pgRy + pgH) / 2 - 24
  const pgTextW = Math.max(measure(POSTGRES.title, 24, 600), measure(POSTGRES.sub, 14, 400, MONO))
  const pgBx = pgX + (pgW - (64 + pgTextW)) / 2

  const covX = tests.x + tests.w - 22 - (30 + measure(TESTS.coverage.title, 17, 600))
  const ghX = covX - 32 - (30 + measure(TESTS.actions.title, 17, 600))

  const signalW = (colW - 52) / 3

  return {
    frames: [docs, r2, card, tests, ...[0, 1, 2].map((i): Frame => ({ x: colX(i), y: rowCY, w: colW, h: rowCH, rx: 22 }))],
    headers: [
      header('docs', docs.x, docs.y + 8, 'i-lucide-library-big', 'Docs', '#fff', docs.w),
      header('r2', r2.x, r2.y + 8, R2.icon, R2.title, R2.color, r2.w),
      header('tests', tests.x + 4, tests.y, TESTS.icon, TESTS.title, '#fff'),
      header('otel', colX(0), rowCY, OTEL.icon, OTEL.title, OTEL.color, colW),
      header('identity', colX(1), rowCY, IDENTITY.icon, IDENTITY.title, IDENTITY.color, colW),
      header('webhooks', colX(2), rowCY, WEBHOOKS.icon, WEBHOOKS.title, WEBHOOKS.color, colW)
    ],
    railway: {
      icon: { name: RAILWAY.icon, x: card.x + 30, y: card.y + 20, size: 30, color: '#fff' } as IconMark,
      title: { x: card.x + 72, y: card.y + 45, size: 26, weight: 700, fill: C.text, value: RAILWAY.title } as Label
    },
    texts: [
      { x: card.x + card.w - 30, y: card.y + 43, size: 14, weight: 400, fill: C.muted, value: RAILWAY.hint, mono: true, anchor: 'end' },
      { x: (webX + 400 + pgX) / 2, y: lowCy - 12, size: 12, weight: 400, fill: C.violetLight, value: 'polling', mono: true, anchor: 'middle' }
    ] as Label[],
    arrows: [
      `M ${CX} ${browserY + 80} L ${CX} ${cloudflareY - 6}`,
      `M ${CX} ${cloudflareY + 80} L ${CX} ${caddyY - 6}`,
      `M ${CX} ${caddyBottom} L ${CX} ${splitY} L ${webX + 200} ${splitY} L ${webX + 200} ${midY - 6}`,
      `M ${CX} ${caddyBottom} L ${CX} ${splitY} L ${backX + 200} ${splitY} L ${backX + 200} ${midY - 6}`,
      `M ${backX + 200} ${midBottom} L ${backX + 200} ${pgY - 6}`
    ],
    inProcess: `M ${dashX} ${midBottom} L ${dashX} ${linkY} L ${webX + 200} ${linkY} L ${webX + 200} ${lowY - 6}`,
    polling: `M ${webX + 406} ${lowCy} L ${pgX - 6} ${lowCy}`,
    pills: [
      pill((CX + webX + 200) / 2, splitY, '/*'),
      pill((CX + backX + 200) / 2, splitY, '/api/*'),
      pill((dashX + webX + 200) / 2, linkY, 'in-process'),
      pill(backX + 200, (midBottom + pgY - 10) / 2, 'EF Core · Dapper')
    ],
    cylinder: {
      x: pgX,
      y: pgY,
      w: pgW,
      h: pgH,
      ry: pgRy,
      rx: pgW / 2,
      cx: pgX + pgW / 2,
      top: pgY + pgRy,
      bottom: pgY + pgH - pgRy,
      right: pgX + pgW,
      color: POSTGRES.color
    },
    blocks: [
      node(BROWSER, topX, browserY, topW),
      node(CLOUDFLARE, topX, cloudflareY, topW),
      node(CADDY, topX, caddyY, topW),
      node(WEB, webX + 200 - topW / 2, midY, topW),
      node(BACK, backX + 200 - topW / 2, midY, topW),
      asyncNode,
      ...DOCS.map((d, i) => item(d, docs.x + 20, docs.y + 77 + i * 60, 'doc')),
      ...R2.items.map((r, i) => item(r, r2.x + 20, r2.y + 77 + i * 60, 'doc')),
      ...IDENTITY.rows.map((r, i) => item(r, colX(1) + 18, rowCY + 56 + i * 46, 'row')),
      ...WEBHOOKS.rows.map((r, i) => item(r, colX(2) + 18, rowCY + 56 + i * 46, 'row')),
      ...TESTS.items.map((t, i) => testItem(t, tests.x + 20 + i * (testW + 12), tests.y + 54, testW, 58)),
      {
        id: 'postgres',
        tile: { x: pgBx, y: pgTop, size: 48, rx: 13, color: POSTGRES.color },
        icon: { name: POSTGRES.icon, x: pgBx + 11, y: pgTop + 11, size: 26, color: POSTGRES.color },
        title: { x: pgBx + 64, y: pgTop + 22, size: 24, weight: 600, fill: C.text, value: POSTGRES.title },
        sub: { x: pgBx + 64, y: pgTop + 44, size: 14, weight: 400, fill: C.muted, value: POSTGRES.sub, mono: true }
      } as Block
    ],
    signals: OTEL.signals.map((s, i): MiniCard => {
      const x = colX(0) + 18 + i * (signalW + 8)
      const y = rowCY + 58

      return {
        id: s.label,
        rect: { x, y, w: signalW, h: 58, rx: 12 },
        icon: { name: s.icon, x: x + signalW / 2 - 10, y: y + 9, size: 20, color: s.color },
        title: { x: x + signalW / 2, y: y + 47, size: 14, weight: 600, fill: C.text, value: s.label, anchor: 'middle' }
      }
    }),
    points: [
      point('linkedin', M+4, 102, { title: LINKEDIN.handle, icon: LINKEDIN.icon, color: LINKEDIN.color, mono: true }, 16),
      ...OTEL.points.map((p, i) => point(p.title, colX(0) + 20, rowCY + 146 + i * 30, p, 16)),
      point('actions', ghX, tests.y + 37, TESTS.actions),
      point('coverage', covX, tests.y + 37, TESTS.coverage)
    ],
    chips: [
      ...chipRow(asyncNode.title.x, asyncNode.title.y + 34, ASYNC.chips!, ASYNC.color, ASYNC.chipText!),
      ...chipRow(colX(1) + 18, rowCY + 152, IDENTITY.chips, IDENTITY.color, IDENTITY.chipText, 12),
      ...chipRow(colX(2) + 18, rowCY + 152, WEBHOOKS.chips, WEBHOOKS.color, WEBHOOKS.chipText, 12)
    ],
    logo: { x: W - M - 52, y: 28, scale: 52 / 24 }
  }
})

function iconTransform(icon: IconMark): string {
  return `translate(${icon.x} ${icon.y}) scale(${icon.size / 24})`
}

const ZOOMS = [
  { label: 'Ajustar à tela', value: 0 },
  { label: '50%', value: 0.5 },
  { label: '75%', value: 0.75 },
  { label: '100%', value: 1 }
]

const zoom = ref(0)

const posterStyle = computed(() => zoom.value === 0
  ? { width: '100%', maxWidth: `${W}px`, height: 'auto' }
  : { width: `${W * zoom.value}px`, minWidth: `${W * zoom.value}px`, height: 'auto' })

const posterEl = ref<SVGSVGElement | null>(null)
const busy = ref<string | null>(null)

const EXPORT_CSS = `text{font-family:'${SANS}',sans-serif}.mono{font-family:'${MONO}',monospace}`

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
      if (family !== SANS && family !== MONO) continue
      if (!coversLatin(rule.style.getPropertyValue('unicode-range'))) continue

      const url = /url\(["']?([^"')]+)/.exec(rule.style.getPropertyValue('src'))?.[1]
      if (!url) continue

      rules.push(rule.cssText.replace(/src:[^;]+;/, `src:url(${await dataUrl(url)});`))
    }
  }

  return rules.join('')
}

async function serialize(): Promise<string> {
  const clone = posterEl.value!.cloneNode(true) as SVGSVGElement
  clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg')
  clone.removeAttribute('style')

  const style = document.createElementNS('http://www.w3.org/2000/svg', 'style')
  style.textContent = `${await embeddedFonts()}${EXPORT_CSS}`
  clone.insertBefore(style, clone.firstChild)

  return new XMLSerializer().serializeToString(clone)
}

function save(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  link.href = url
  link.download = name
  link.click()

  URL.revokeObjectURL(url)
}

async function downloadSvg() {
  busy.value = 'svg'

  try {
    save(new Blob([await serialize()], { type: 'image/svg+xml' }), 'estud-linkedin.svg')
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
    canvas.width = W * scale
    canvas.height = H * scale
    canvas.getContext('2d')!.drawImage(image, 0, 0, canvas.width, canvas.height)

    const blob = await new Promise<Blob | null>((resolve) => { canvas.toBlob(resolve, 'image/png') })
    if (blob) save(blob, `estud-linkedin${scale > 1 ? `@${scale}x` : ''}.png`)
  } finally {
    URL.revokeObjectURL(source)
    busy.value = null
  }
}

const pngItems = [
  [
    { label: `${W} × ${H}`, icon: 'i-lucide-image', onSelect: () => { downloadPng(1) } },
    { label: `${W * 2} × ${H * 2} (@2x)`, icon: 'i-lucide-images', onSelect: () => { downloadPng(2) } }
  ]
]

onMounted(async () => {
  const faces = [
    ...[600, 700, 800].map(weight => `${weight} 24px ${SANS}`),
    ...[400, 600].map(weight => `${weight} 14px '${MONO}'`)
  ]

  // Uma fonte que não baixa rejeita o load e não pode travar o pôster no
  // spinner: a medição usa a mesma fonte que o navegador vai desenhar, então
  // no fallback o desenho continua alinhado — só troca o tipo.
  await Promise.all(faces.map(face => document.fonts.load(face).catch(() => {})))

  fontsLoaded.value = true
})
</script>

<template>
  <UDashboardPanel id="dev-arch-diagram">
    <template #header>
      <UDashboardNavbar title="Diagrama de Arquitetura">
        <template #leading>
          <PageIcon icon="i-lucide-image" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-6">
        <div class="space-y-2">
          <p class="text-sm text-muted">
            Pôster de {{ W }} × {{ H }} (proporção de post do LinkedIn) com a arquitetura do Estud em produção: da
            borda até o banco, mais identidade, webhooks, observabilidade e a pipeline de testes.
          </p>
          <p class="text-sm text-muted">
            O desenho é o próprio SVG desta página — para editar, mexa nas listas no topo de
            <code class="text-xs">app/pages/dev/arch-diagram.vue</code>. Os botões abaixo baixam exatamente o que está
            na tela, com as fontes embutidas no arquivo.
          </p>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <USelect
            v-model="zoom"
            :items="ZOOMS"
            value-key="value"
            icon="i-lucide-zoom-in"
            class="w-44"
          />
          <span class="text-xs text-dimmed">Ícones em <code>app/utils/arch-icons.ts</code></span>

          <div class="ml-auto flex items-center gap-2">
            <UButton
              label="SVG"
              icon="i-lucide-download"
              color="neutral"
              variant="outline"
              :loading="busy === 'svg'"
              @click="() => { downloadSvg() }"
            />

            <UDropdownMenu :items="pngItems">
              <UButton
                label="PNG"
                icon="i-lucide-download"
                color="neutral"
                :loading="busy === 'png'"
              />
            </UDropdownMenu>
          </div>
        </div>

        <div class="overflow-auto rounded-lg border border-default" :style="{ backgroundColor: C.bg }">
          <ClientOnly>
            <svg
              v-if="fontsLoaded"
              ref="posterEl"
              class="poster block"
              :width="W"
              :height="H"
              :viewBox="`0 0 ${W} ${H}`"
              :style="posterStyle"
              role="img"
              aria-label="Arquitetura do Estud em produção"
            >
              <defs>
                <pattern id="dots" width="28" height="28" patternUnits="userSpaceOnUse">
                  <circle cx="2" cy="2" r="1.2" fill="#ffffff" fill-opacity="0.06" />
                </pattern>

                <linearGradient id="titleGrad" gradientUnits="userSpaceOnUse" x1="520" y1="0" x2="840" y2="0">
                  <stop offset="0" stop-color="#c4b5fd" />
                  <stop offset="1" stop-color="#8b5cf6" />
                </linearGradient>

                <linearGradient id="railStroke" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0" stop-color="#a78bfa" stop-opacity="0.7" />
                  <stop offset="0.5" stop-color="#ffffff" stop-opacity="0.08" />
                  <stop offset="1" stop-color="#6366f1" stop-opacity="0.5" />
                </linearGradient>

                <filter id="shadow" x="-20%" y="-30%" width="140%" height="180%">
                  <feDropShadow dx="0" dy="10" stdDeviation="14" flood-color="#000" flood-opacity="0.45" />
                </filter>

                <filter id="glow" x="-50%" y="-50%" width="200%" height="200%">
                  <feGaussianBlur stdDeviation="4" />
                </filter>

                <marker
                  id="head"
                  viewBox="0 0 10 10"
                  refX="8"
                  refY="5"
                  markerWidth="5"
                  markerHeight="5"
                  orient="auto"
                  markerUnits="strokeWidth"
                >
                  <path d="M0 0 L10 5 L0 10 z" :fill="C.line" />
                </marker>
              </defs>

              <rect :width="W" :height="H" :fill="C.bg" />
              <rect :width="W" :height="H" fill="url(#dots)" />

              <text
                :x="M"
                y="72"
                font-size="52"
                font-weight="800"
                :fill="C.text"
              >{{ TITLE.lead }}<tspan fill="url(#titleGrad)">{{ TITLE.accent }}</tspan></text>

              <g :transform="`translate(${poster.logo.x} ${poster.logo.y}) scale(${poster.logo.scale})`">
                <rect width="24" height="24" rx="6" fill="#7c3aed" />
                <path fill="#fff" :d="ESTUD_E" />
              </g>

              <rect
                v-for="(f, i) in poster.frames"
                :key="`frame-${i}`"
                :x="f.x"
                :y="f.y"
                :width="f.w"
                :height="f.h"
                :rx="f.rx"
                fill="#ffffff"
                fill-opacity="0.025"
                stroke="url(#railStroke)"
                stroke-width="2"
              />

              <g v-for="h in poster.headers" :key="h.id">
                <g :transform="iconTransform(h.icon)" :style="{ color: h.icon.color }" v-html="archIcons[h.icon.name]" />
                <text
                  :x="h.title.x"
                  :y="h.title.y"
                  :font-size="h.title.size"
                  :font-weight="h.title.weight"
                  :fill="h.title.fill"
                >{{ h.title.value }}</text>
              </g>

              <g :transform="iconTransform(poster.railway.icon)" :style="{ color: poster.railway.icon.color }" v-html="archIcons[poster.railway.icon.name]" />
              <text
                :x="poster.railway.title.x"
                :y="poster.railway.title.y"
                :font-size="poster.railway.title.size"
                :font-weight="poster.railway.title.weight"
                :fill="poster.railway.title.fill"
              >{{ poster.railway.title.value }}</text>

              <g v-for="a in poster.arrows" :key="a">
                <path
                  :d="a"
                  fill="none"
                  :stroke="C.line"
                  stroke-width="8"
                  stroke-opacity="0.18"
                  stroke-linejoin="round"
                  filter="url(#glow)"
                />
                <path
                  :d="a"
                  fill="none"
                  :stroke="C.line"
                  stroke-width="2.5"
                  stroke-linejoin="round"
                  marker-end="url(#head)"
                />
              </g>

              <path
                :d="poster.inProcess"
                fill="none"
                :stroke="C.line"
                stroke-opacity="0.75"
                stroke-width="2.5"
                stroke-dasharray="7 7"
                stroke-linejoin="round"
                marker-end="url(#head)"
              />

              <path
                :d="poster.polling"
                fill="none"
                :stroke="C.line"
                stroke-width="2.5"
                marker-end="url(#head)"
              />

              <g v-for="p in poster.pills" :key="p.label">
                <rect
                  :x="p.x - p.w / 2"
                  :y="p.y - 15"
                  :width="p.w"
                  height="30"
                  rx="15"
                  fill="#1c1830"
                  :stroke="C.violet"
                  stroke-opacity="0.55"
                />
                <text
                  class="mono"
                  :x="p.x"
                  :y="p.y + 5"
                  text-anchor="middle"
                  font-size="15"
                  font-weight="600"
                  :fill="C.violetLight"
                >{{ p.label }}</text>
              </g>

              <g filter="url(#shadow)">
                <rect
                  :x="poster.cylinder.x"
                  :y="poster.cylinder.top"
                  :width="poster.cylinder.w"
                  :height="poster.cylinder.h - 2 * poster.cylinder.ry"
                  :fill="C.card"
                />
                <ellipse
                  :cx="poster.cylinder.cx"
                  :cy="poster.cylinder.bottom"
                  :rx="poster.cylinder.rx"
                  :ry="poster.cylinder.ry"
                  :fill="C.card"
                />
              </g>

              <path
                :d="`M ${poster.cylinder.x} ${poster.cylinder.top} L ${poster.cylinder.x} ${poster.cylinder.bottom} A ${poster.cylinder.rx} ${poster.cylinder.ry} 0 0 0 ${poster.cylinder.right} ${poster.cylinder.bottom} L ${poster.cylinder.right} ${poster.cylinder.top}`"
                fill="none"
                stroke="#ffffff"
                stroke-opacity="0.16"
                stroke-width="1.5"
              />

              <ellipse
                :cx="poster.cylinder.cx"
                :cy="poster.cylinder.top"
                :rx="poster.cylinder.rx"
                :ry="poster.cylinder.ry"
                :fill="C.cardTop"
                :stroke="poster.cylinder.color"
                stroke-opacity="0.45"
                stroke-width="1.5"
              />

              <g v-for="b in poster.blocks" :key="b.id">
                <rect
                  v-if="b.rect"
                  :x="b.rect.x"
                  :y="b.rect.y"
                  :width="b.rect.w"
                  :height="b.rect.h"
                  :rx="b.rect.rx"
                  :fill="b.rect.fill"
                  :stroke="C.cardStroke"
                  stroke-width="1.5"
                  filter="url(#shadow)"
                />
                <rect
                  :x="b.tile.x"
                  :y="b.tile.y"
                  :width="b.tile.size"
                  :height="b.tile.size"
                  :rx="b.tile.rx"
                  :fill="b.tile.color"
                  fill-opacity="0.14"
                  :stroke="b.tile.color"
                  stroke-opacity="0.4"
                />
                <g :transform="iconTransform(b.icon)" :style="{ color: b.icon.color }" v-html="archIcons[b.icon.name]" />
                <text
                  :x="b.title.x"
                  :y="b.title.y"
                  :font-size="b.title.size"
                  :font-weight="b.title.weight"
                  :fill="b.title.fill"
                >{{ b.title.value }}</text>
                <text
                  v-if="b.sub"
                  class="mono"
                  :x="b.sub.x"
                  :y="b.sub.y"
                  :font-size="b.sub.size"
                  :fill="b.sub.fill"
                >{{ b.sub.value }}</text>
              </g>

              <g v-for="s in poster.signals" :key="s.id">
                <rect
                  :x="s.rect.x"
                  :y="s.rect.y"
                  :width="s.rect.w"
                  :height="s.rect.h"
                  :rx="s.rect.rx"
                  :fill="C.surface"
                  :stroke="C.cardStroke"
                  stroke-width="1.5"
                />
                <g :transform="iconTransform(s.icon)" :style="{ color: s.icon.color }" v-html="archIcons[s.icon.name]" />
                <text
                  :x="s.title.x"
                  :y="s.title.y"
                  text-anchor="middle"
                  :font-size="s.title.size"
                  :font-weight="s.title.weight"
                  :fill="s.title.fill"
                >{{ s.title.value }}</text>
              </g>

              <g v-for="p in poster.points" :key="p.id">
                <g :transform="iconTransform(p.icon)" :style="{ color: p.icon.color }" v-html="archIcons[p.icon.name]" />
                <text
                  :class="{ mono: p.title.mono }"
                  :x="p.title.x"
                  :y="p.title.y"
                  :font-size="p.title.size"
                  :font-weight="p.title.weight"
                  :fill="p.title.fill"
                >{{ p.title.value }}<tspan
                  v-if="p.sub"
                  class="mono"
                  dx="10"
                  font-size="12"
                  font-weight="400"
                  :fill="C.muted"
                >{{ p.sub }}</tspan></text>
              </g>

              <g v-for="c in poster.chips" :key="`${c.x}-${c.label}`">
                <rect
                  :x="c.x"
                  :y="c.y"
                  :width="c.w"
                  height="24"
                  rx="7"
                  :fill="c.color"
                  fill-opacity="0.1"
                  :stroke="c.color"
                  stroke-opacity="0.35"
                />
                <text
                  class="mono"
                  :x="c.x + c.w / 2"
                  :y="c.y + 12 + c.size * 0.36"
                  text-anchor="middle"
                  :font-size="c.size"
                  :fill="c.textColor"
                >{{ c.label }}</text>
              </g>

              <text
                v-for="t in poster.texts"
                :key="t.value"
                :class="{ mono: t.mono }"
                :x="t.x"
                :y="t.y"
                :font-size="t.size"
                :fill="t.fill"
                :text-anchor="t.anchor"
              >{{ t.value }}</text>
            </svg>

            <div v-else class="flex items-center justify-center py-20">
              <AppSpinner class="size-6 text-muted" />
            </div>

            <template #fallback>
              <div class="flex items-center justify-center py-20">
                <AppSpinner class="size-6 text-muted" />
              </div>
            </template>
          </ClientOnly>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>

<style scoped>
.poster {
  font-family: 'Saira', sans-serif;
}

.poster .mono {
  font-family: 'JetBrains Mono', monospace;
}
</style>
