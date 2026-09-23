<script setup lang="ts">
interface NodeContent {
  title: string
  sub: string
  color: string
  icon: string
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
}

interface Pill {
  x: number
  y: number
  w: number
  label: string
}

interface Container {
  node: NodeContent
  drop: string
  chips: string[]
  chipText: string
  mirror: string
  prod: ItemContent
}

const W = 1080
const H = 1350
const CX = W / 2
const M = 64

const MONO = POSTER_MONO

const measure = measurePosterText

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

const TITLE = { lead: 'Como o Estud é ', accent: 'testado?' }

const RUNNER: NodeContent = { title: 'NUnit', sub: 'dotnet test', color: '#34d399', icon: 'i-lucide-flask-conical' }
const BACK: NodeContent = { title: 'Back', sub: 'WebApplicationFactory', color: '#9B7BFF', icon: 'i-simple-icons-dotnet' }

const ASSERTS = {
  title: 'Asserts',
  icon: 'i-lucide-check-check',
  items: [
    { title: 'FluentAssertions', sub: '.Should().Be(...)', color: '#34d399', icon: 'i-lucide-circle-check-big' },
    { title: 'Result Pattern', sub: 'ShouldBeError()', color: '#c4b5fd', icon: 'i-lucide-shield-check' }
  ] as ItemContent[]
}

const MOCKS = {
  title: 'Mocks',
  icon: 'i-lucide-drama',
  items: [
    { title: 'Mocks API', sub: 'OIDC · DNS · Webhooks', color: ROSE, icon: 'i-lucide-server' },
    { title: 'Fakes', sub: 'E-mails em memória', color: AMBER, icon: 'i-lucide-mail' }
  ] as ItemContent[]
}

const TESTCONTAINERS = { title: 'Testcontainers', hint: 'Docker · infra real, sem mocks', icon: 'i-simple-icons-docker', color: '#2496ED' }

const POSTGRES: Container = {
  node: { title: 'Postgres', sub: 'postgres:18', color: '#5B8DEF', icon: 'i-simple-icons-postgresql' },
  drop: 'EF Core · Dapper',
  chips: ['Banco recriado', 'Reuse'],
  chipText: '#bfdbfe',
  mirror: 'mesmo engine',
  prod: { title: 'Postgres', sub: 'produção · Railway', color: '#5B8DEF', icon: 'i-simple-icons-postgresql' }
}

const MINIO: Container = {
  node: { title: 'MinIO', sub: 'minio:latest', color: '#f0506e', icon: 'i-simple-icons-minio' },
  drop: 'AWSSDK.S3',
  chips: ['Buckets', 'Presigned URL'],
  chipText: '#fecdd3',
  mirror: 'mesma API S3',
  prod: { title: 'Cloudflare R2', sub: 'produção · Storage', color: '#F38020', icon: 'i-estud-r2' }
}

const KEYCLOAK: Container = {
  node: { title: 'Keycloak', sub: 'keycloak:26.4', color: '#38bdf8', icon: 'i-simple-icons-keycloak' },
  drop: 'OpenID Connect',
  chips: ['SSO happy path', 'Realm JSON'],
  chipText: '#bae6fd',
  mirror: 'mesmo OIDC',
  prod: { title: 'Okta · Azure · Auth0', sub: 'produção · SSO', color: TEAL, icon: 'i-lucide-building-2' }
}

const CONCEPTS = [
  {
    title: 'Arrange via API',
    icon: 'i-lucide-route',
    color: TEAL,
    chipText: '#99f6e4',
    chips: ['Arrange', 'Act', 'Assert'],
    rows: [
      { title: 'TestsHttpClient', sub: 'Um helper por endpoint', color: TEAL, icon: 'i-lucide-send' },
      { title: 'Shortcuts', sub: 'Cenários prontos', color: '#fcd34d', icon: 'i-lucide-zap' }
    ] as ItemContent[]
  },
  {
    title: 'Isolamento',
    icon: 'i-lucide-layers',
    color: AMBER,
    chipText: '#fcd34d',
    chips: ['DataSeeder', 'Por classe'],
    rows: [
      { title: 'Banco por classe', sub: 'EnsureDeleted · Created', color: '#5B8DEF', icon: 'i-lucide-database' },
      { title: 'Paralelo', sub: 'ParallelScope.Children', color: AMBER, icon: 'i-lucide-split' }
    ] as ItemContent[]
  },
  {
    title: 'Background',
    icon: 'i-lucide-list-end',
    color: ROSE,
    chipText: '#fecdd3',
    chips: ['Quartz.NET', 'Outbox'],
    rows: [
      { title: 'Commands', sub: 'AwaitCommandsProcessing', color: ROSE, icon: 'i-lucide-list-end' },
      { title: 'Domain Events', sub: 'AwaitDomainEventsProcessing', color: AMBER, icon: 'i-lucide-activity' }
    ] as ItemContent[]
  }
]

const SUITES = {
  title: 'Suítes',
  icon: 'i-lucide-test-tube-diagonal',
  items: [
    { title: 'Unit', sub: 'NUnit · Domínio', color: '#38bdf8', icon: 'i-lucide-box' },
    { title: 'Integration', sub: 'API · Testcontainers', color: '#34d399', icon: 'i-lucide-plug' },
    { title: 'Mutation', sub: 'Stryker.NET', color: '#f472b6', icon: 'i-lucide-dna' },
    { title: 'Web Unit', sub: 'Vitest · Nuxt', color: '#FCC72B', icon: 'i-simple-icons-vitest' }
  ] as ItemContent[],
  coverage: { title: 'Coverage ≥ 95%', icon: 'i-lucide-chart-column', color: '#a3e635' },
  actions: { title: 'GitHub Actions', icon: 'i-simple-icons-githubactions', color: '#2088FF' }
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
  const textW = Math.max(measure(content.title, 24, 600), measure(content.sub, 14, 400, MONO))
  const bx = x + (w - (64 + textW)) / 2
  const by = y + (h - 48) / 2

  return {
    id: `node-${content.title}`,
    rect: { x, y, w, h, rx: 18, fill: C.card },
    tile: { x: bx, y: by, size: 48, rx: 13, color: content.color },
    icon: { name: content.icon, x: bx + 11, y: by + 11, size: 26, color: content.color },
    title: { x: bx + 64, y: by + 22, size: 24, weight: 600, fill: C.text, value: content.title },
    sub: { x: bx + 64, y: by + 46, size: 14, weight: 400, fill: C.muted, value: content.sub, mono: true }
  }
}

function itemWidth(content: ItemContent): number {
  return 48 + Math.max(measure(content.title, 18, 600), measure(content.sub ?? '', 13, 400, MONO))
}

function item(content: ItemContent, x: number, y: number, variant: 'doc' | 'row'): Block {
  const tile = variant === 'row' ? 34 : 36

  return {
    id: `${variant}-${content.title}`,
    tile: { x, y, size: tile, rx: 10, color: content.color },
    icon: { name: content.icon, x: x + 8, y: y + 8, size: variant === 'row' ? 18 : 20, color: content.color },
    title: { x: x + tile + 12, y: y + 15, size: variant === 'row' ? 16 : 18, weight: 600, fill: C.text, value: content.title },
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

function suiteItem(content: ItemContent, x: number, y: number, w: number, h: number): Block {
  const tileY = y + (h - 34) / 2

  return {
    id: `suite-${content.title}`,
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

function point(id: string, x: number, y: number, content: { title: string, icon: string, color: string, mono?: boolean }, size = 17): Point {
  return {
    id,
    icon: { name: content.icon, x, y: y - 15, size: 19, color: content.color },
    title: { x: x + 30, y, size, weight: 600, fill: content.mono ? C.muted : C.text, value: content.title, mono: content.mono }
  }
}

function pill(x: number, y: number, label: string): Pill {
  return { x, y, w: label.length * 9.4 + 28, label }
}

const poster = computed(() => {
  const titleX = M + measure(TITLE.lead, 52, 800)

  const rowA = 136
  const rowAH = 220
  const conceptsH = 200
  const suitesH = 132
  const gap = 38

  const topW = 104 + Math.max(
    ...[RUNNER, BACK].map(n => measure(n.title, 24, 600)),
    ...[RUNNER, BACK].map(n => measure(n.sub, 14, 400, MONO))
  )
  const topX = CX - topW / 2
  const runnerY = rowA
  const backY = rowA + rowAH - 80

  const sideW = 40 + Math.max(...[...ASSERTS.items, ...MOCKS.items].map(itemWidth))
  const asserts: Frame = { x: M, y: rowA, w: sideW, h: rowAH, rx: 22 }
  const mocks: Frame = { x: W - M - sideW, y: rowA, w: sideW, h: rowAH, rx: 22 }

  const card: Frame = { x: M, y: rowA + rowAH + gap, w: W - 2 * M, h: H - 32 - rowA - rowAH - conceptsH - suitesH - 3 * gap, rx: 28 }

  const nodeH = 84
  const prodH = 76
  const spare = card.h - 330
  const splitY = card.y + 76
  const nodeTop = splitY + spare / 2
  const chipsY = nodeTop + 112
  const mirrorTop = nodeTop + 150
  const prodY = mirrorTop + spare / 2

  const containers = [POSTGRES, MINIO, KEYCLOAK]
  const innerX = card.x + 30
  const tcW = (card.w - 60 - 40) / 3
  const tcX = (i: number) => innerX + i * (tcW + 20)
  const tcCx = (i: number) => tcX(i) + tcW / 2

  const pgH = 116
  const pgRy = 16
  const pgY = nodeTop + nodeH / 2 - pgH / 2
  const pgTop = pgY + (2 * pgRy + pgH) / 2 - 24
  const pgTextW = Math.max(measure(POSTGRES.node.title, 24, 600), measure(POSTGRES.node.sub, 14, 400, MONO))
  const pgBx = tcX(0) + (tcW - (64 + pgTextW)) / 2
  const dropEnd = (i: number) => (i === 0 ? pgY : nodeTop)

  const conceptsY = card.y + card.h + gap
  const colW = (W - 2 * M - 32) / 3
  const colX = (i: number) => M + i * (colW + 16)

  const suites: Frame = { x: M, y: conceptsY + conceptsH + gap, w: W - 2 * M, h: suitesH, rx: 22 }
  const suiteW = (suites.w - 40 - 36) / 4

  const covX = suites.x + suites.w - 22 - (30 + measure(SUITES.coverage.title, 17, 600))
  const ghX = covX - 32 - (30 + measure(SUITES.actions.title, 17, 600))

  return {
    titleGrad: { x1: titleX, x2: titleX + measure(TITLE.accent, 52, 800) },
    frames: [asserts, mocks, card, suites, ...[0, 1, 2].map((i): Frame => ({ x: colX(i), y: conceptsY, w: colW, h: conceptsH, rx: 22 }))],
    headers: [
      header('asserts', asserts.x, asserts.y + 8, ASSERTS.icon, ASSERTS.title, '#fff', asserts.w),
      header('mocks', mocks.x, mocks.y + 8, MOCKS.icon, MOCKS.title, '#fff', mocks.w),
      header('suites', suites.x + 4, suites.y, SUITES.icon, SUITES.title, '#fff'),
      ...CONCEPTS.map((c, i) => header(c.title, colX(i), conceptsY, c.icon, c.title, c.color, colW))
    ],
    card: {
      icon: { name: TESTCONTAINERS.icon, x: card.x + 30, y: card.y + 20, size: 30, color: TESTCONTAINERS.color } as IconMark,
      title: { x: card.x + 72, y: card.y + 45, size: 26, weight: 700, fill: C.text, value: TESTCONTAINERS.title } as Label,
      hint: { x: card.x + card.w - 30, y: card.y + 43, size: 14, weight: 400, fill: C.muted, value: TESTCONTAINERS.hint, mono: true, anchor: 'end' } as Label
    },
    arrows: [
      `M ${CX} ${runnerY + 80} L ${CX} ${backY - 6}`,
      ...containers.map((_, i) => `M ${CX} ${backY + 80} L ${CX} ${splitY} L ${tcCx(i)} ${splitY} L ${tcCx(i)} ${dropEnd(i) - 6}`)
    ],
    mirrors: containers.map((_, i) => `M ${tcCx(i)} ${mirrorTop} L ${tcCx(i)} ${prodY}`),
    prodStrip: { x: card.x + 20, y: prodY, w: card.w - 40, h: prodH, rx: 18 } as Frame,
    pills: [
      pill(CX, (runnerY + 80 + backY) / 2, 'TestsHttpClient'),
      ...containers.map((c, i) => pill(tcCx(i), (splitY + pgY) / 2, c.drop)),
      ...containers.map((c, i) => pill(tcCx(i), (mirrorTop + prodY) / 2, c.mirror))
    ],
    cylinder: {
      x: tcX(0),
      w: tcW,
      ry: pgRy,
      rx: tcW / 2,
      cx: tcCx(0),
      top: pgY + pgRy,
      bottom: pgY + pgH - pgRy,
      right: tcX(0) + tcW,
      color: POSTGRES.node.color
    },
    blocks: [
      node(RUNNER, topX, runnerY, topW),
      node(BACK, topX, backY, topW),
      ...[MINIO, KEYCLOAK].map((c, i) => node(c.node, tcX(i + 1), nodeTop, tcW, nodeH)),
      {
        id: 'postgres',
        tile: { x: pgBx, y: pgTop, size: 48, rx: 13, color: POSTGRES.node.color },
        icon: { name: POSTGRES.node.icon, x: pgBx + 11, y: pgTop + 11, size: 26, color: POSTGRES.node.color },
        title: { x: pgBx + 64, y: pgTop + 22, size: 24, weight: 600, fill: C.text, value: POSTGRES.node.title },
        sub: { x: pgBx + 64, y: pgTop + 44, size: 14, weight: 400, fill: C.muted, value: POSTGRES.node.sub, mono: true }
      } as Block,
      ...containers.map((c, i) => item(c.prod, tcCx(i) - itemWidth(c.prod) / 2, prodY + (prodH - 36) / 2, 'doc')),
      ...ASSERTS.items.map((a, i) => item(a, asserts.x + 20, asserts.y + 84 + i * 64, 'doc')),
      ...MOCKS.items.map((m, i) => item(m, mocks.x + 20, mocks.y + 84 + i * 64, 'doc')),
      ...CONCEPTS.flatMap((c, ci) => c.rows.map((r, i) => item(r, colX(ci) + 18, conceptsY + 56 + i * 46, 'row'))),
      ...SUITES.items.map((s, i) => suiteItem(s, suites.x + 20 + i * (suiteW + 12), suites.y + 54, suiteW, 58))
    ],
    points: [
      point('linkedin', M + 4, 102, { title: LINKEDIN.handle, icon: LINKEDIN.icon, color: LINKEDIN.color, mono: true }, 16),
      point('actions', ghX, suites.y + 37, SUITES.actions),
      point('coverage', covX, suites.y + 37, SUITES.coverage)
    ],
    chips: [
      ...containers.flatMap((c, i) => chipRow(tcCx(i) - chipsWidth(c.chips, 12) / 2, chipsY, c.chips, c.node.color, c.chipText, 12)),
      ...CONCEPTS.flatMap((c, i) => chipRow(colX(i) + 18, conceptsY + 152, c.chips, c.color, c.chipText, 12))
    ],
    logo: { x: W - M - 52, y: 28, scale: 52 / 24 }
  }
})

function iconTransform(icon: IconMark): string {
  return `translate(${icon.x} ${icon.y}) scale(${icon.size / 24})`
}

const { fontsLoaded, posterEl, busy, zooms: ZOOMS, zoom, posterStyle, downloadSvg, pngItems } = usePoster(W, H, 'estud-tests-linkedin')
</script>

<template>
  <UDashboardPanel id="dev-tests-diagram">
    <template #header>
      <UDashboardNavbar title="Diagrama de Testes">
        <template #leading>
          <PageIcon icon="i-lucide-flask-conical" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-6">
        <div class="space-y-2">
          <p class="text-sm text-muted">
            Pôster de {{ W }} × {{ H }} (proporção de post do LinkedIn) com a estratégia de testes do Estud: o Back
            sobe em memória e conversa com Postgres, MinIO e Keycloak reais, levantados via Testcontainers.
          </p>
          <p class="text-sm text-muted">
            O desenho é o próprio SVG desta página — para editar, mexa nas listas no topo de
            <code class="text-xs">app/pages/dev/tests-diagram.vue</code>. Os botões abaixo baixam exatamente o que está
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
              aria-label="Estratégia de testes do Estud"
            >
              <defs>
                <pattern id="dots" width="28" height="28" patternUnits="userSpaceOnUse">
                  <circle cx="2" cy="2" r="1.2" fill="#ffffff" fill-opacity="0.06" />
                </pattern>

                <linearGradient
                  id="titleGrad"
                  gradientUnits="userSpaceOnUse"
                  :x1="poster.titleGrad.x1"
                  y1="0"
                  :x2="poster.titleGrad.x2"
                  y2="0"
                >
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

              <g :transform="iconTransform(poster.card.icon)" :style="{ color: poster.card.icon.color }" v-html="archIcons[poster.card.icon.name]" />
              <text
                :x="poster.card.title.x"
                :y="poster.card.title.y"
                :font-size="poster.card.title.size"
                :font-weight="poster.card.title.weight"
                :fill="poster.card.title.fill"
              >{{ poster.card.title.value }}</text>
              <text
                class="mono"
                :x="poster.card.hint.x"
                :y="poster.card.hint.y"
                :font-size="poster.card.hint.size"
                :fill="poster.card.hint.fill"
                text-anchor="end"
              >{{ poster.card.hint.value }}</text>

              <rect
                :x="poster.prodStrip.x"
                :y="poster.prodStrip.y"
                :width="poster.prodStrip.w"
                :height="poster.prodStrip.h"
                :rx="poster.prodStrip.rx"
                :fill="C.surface"
                :stroke="C.cardStroke"
                stroke-width="1.5"
              />

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
                v-for="m in poster.mirrors"
                :key="m"
                :d="m"
                fill="none"
                :stroke="C.line"
                stroke-opacity="0.75"
                stroke-width="2.5"
                stroke-dasharray="7 7"
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
                  :height="poster.cylinder.bottom - poster.cylinder.top"
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

              <g v-for="p in poster.points" :key="p.id">
                <g :transform="iconTransform(p.icon)" :style="{ color: p.icon.color }" v-html="archIcons[p.icon.name]" />
                <text
                  :class="{ mono: p.title.mono }"
                  :x="p.title.x"
                  :y="p.title.y"
                  :font-size="p.title.size"
                  :font-weight="p.title.weight"
                  :fill="p.title.fill"
                >{{ p.title.value }}</text>
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
            </svg>

            <div v-else class="flex items-center justify-center py-20">
              <AppSpinner class="size-6" />
            </div>

            <template #fallback>
              <div class="flex items-center justify-center py-20">
                <AppSpinner class="size-6" />
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
