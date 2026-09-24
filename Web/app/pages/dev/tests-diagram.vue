<script setup lang="ts">
interface NodeContent {
  title: string
  sub: string
  color: string
  icon: string
}

interface Service extends NodeContent {
  light: string
  address: string
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

interface Block {
  id: string
  rect?: Frame & { fill: string }
  tile: Tile
  icon: IconMark
  title: Label
  sub?: Label
  corner?: Label
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
  sub?: Label
}

const W = 1080
const H = 1350
const CX = W / 2
const M = 64

const measure = measurePosterText

const C = {
  bg: '#2f1c51',
  card: '#211439',
  surface: 'rgba(255,255,255,0.06)',
  cardStroke: 'rgba(255,255,255,0.09)',
  text: '#fafafa',
  muted: '#a1a1aa',
  line: '#a78bfa'
}

// Mesmo path do <EstudIcon>, em viewBox 24x24.
const ESTUD_E = 'M 10.98 20.5Q 9.46 20.5 8.27 19.61Q 7.08 18.72 6.4 17.13Q 5.73 15.53 5.73 13.49Q 5.73 11.37 6.34 9.54Q 6.96 7.72 8.08 6.37Q 9.2 5.02 10.71 4.26Q 12.22 3.5 13.97 3.5Q 16.09 3.5 17.18 4.56Q 18.27 5.62 18.27 7.66Q 18.27 9.19 17.44 10.41Q 16.61 11.63 15.16 12.32Q 13.71 13.01 11.84 13.01Q 10.92 13.01 10.12 12.9Q 9.32 12.8 8.57 12.63L 8.65 11.63H 9.72Q 11.38 11.63 12.62 11.07Q 13.85 10.51 14.54 9.47Q 15.23 8.44 15.23 7.09Q 15.23 6.08 14.76 5.54Q 14.28 4.99 13.42 4.99Q 12.33 4.99 11.47 5.81Q 10.61 6.63 10.0 7.94Q 9.4 9.24 9.09 10.74Q 8.77 12.23 8.77 13.58Q 8.77 16.11 9.53 17.38Q 10.29 18.66 11.78 18.66Q 14.6 18.66 16.41 15.85Q 17.47 16.02 17.47 16.62Q 16.24 18.63 14.67 19.57Q 13.11 20.5 10.98 20.5Z'

const LINKEDIN = { handle: '/in/zaqueu-cavalcante', icon: 'i-simple-icons-linkedin', color: '#4aa3f0' }

const TITLE = { lead: 'Como o Estud é ', accent: 'testado?' }

const BACK: Service = { title: 'Back', sub: 'WebApplicationFactory', color: '#9B7BFF', icon: 'i-simple-icons-dotnet', light: '#ddd6fe', address: 'localhost:5100' }

const FAKES = {
  title: 'Fakes',
  hint: 'localhost:5678',
  icon: 'i-simple-icons-dotnet',
  color: '#F59E0B',
  items: [
    { title: 'Brevo', sub: 'E-mails', icon: 'i-lucide-mail' },
    { title: 'OIDC', sub: 'Login SSO', icon: 'i-lucide-fingerprint' },
    { title: 'DNS', sub: 'Registros TXT', icon: 'i-lucide-globe' },
    { title: 'Google', sub: 'Social login', icon: 'i-simple-icons-google' },
    { title: 'Webhooks', sub: 'Callbacks', icon: 'i-lucide-webhook' }
  ]
}

const TESTCONTAINERS = {
  title: 'Testcontainers',
  hint: 'Docker',
  icon: 'i-simple-icons-docker',
  color: '#2496ED',
  items: [
    { title: 'Postgres', sub: 'postgres:18', color: '#5B8DEF', icon: 'i-simple-icons-postgresql', light: '#bfdbfe', address: 'localhost:5445' },
    { title: 'MinIO', sub: 'minio:latest', color: '#f0506e', icon: 'i-simple-icons-minio', light: '#fecdd3', address: 'localhost:5300' },
    { title: 'Keycloak', sub: 'keycloak:26.4', color: '#38bdf8', icon: 'i-simple-icons-keycloak', light: '#bae6fd', address: 'localhost:5446' }
  ] as Service[]
}

const TAGLINE = {
  title: 'O Back não sabe que está em teste',
  sub: 'roda o mesmo código de produção, só apontando pra outros endereços'
}

function stackedNode(content: NodeContent, x: number, y: number, w: number, h: number): Block {
  const cx = x + w / 2
  const top = y + (h - 146) / 2

  return {
    id: `node-${content.title}`,
    rect: { x, y, w, h, rx: 18, fill: C.card },
    tile: { x: cx - 36, y: top, size: 72, rx: 18, color: content.color },
    icon: { name: content.icon, x: cx - 20, y: top + 16, size: 40, color: content.color },
    title: { x: cx, y: top + 114, size: 28, weight: 700, fill: C.text, value: content.title, anchor: 'middle' },
    sub: { x: cx, y: top + 142, size: 15, weight: 400, fill: C.muted, value: content.sub, mono: true, anchor: 'middle' }
  }
}

function withAddress(block: Block, service: Service, right: number, top: number): Block {
  return {
    ...block,
    corner: { x: right - 16, y: top + 26, size: 13, weight: 400, fill: service.light, value: service.address, mono: true, anchor: 'end' }
  }
}

function header(id: string, frame: Frame, icon: string, title: string, color: string): Header {
  return {
    id,
    icon: { name: icon, x: frame.x + 18, y: frame.y + 24, size: 24, color },
    title: { x: frame.x + 52, y: frame.y + 45, size: 22, weight: 700, fill: C.text, value: title }
  }
}

function hint(frame: Frame, value: string): Label {
  return { x: frame.x + frame.w - 22, y: frame.y + 43, size: 14, weight: 400, fill: C.muted, value, mono: true, anchor: 'end' }
}

function point(id: string, x: number, y: number, content: { title: string, icon: string, color: string, mono?: boolean }, size = 17): Point {
  return {
    id,
    icon: { name: content.icon, x, y: y - 15, size: 19, color: content.color },
    title: { x: x + 30, y, size, weight: 600, fill: content.mono ? C.muted : C.text, value: content.title, mono: content.mono }
  }
}

const poster = computed(() => {
  const titleX = M + measure(TITLE.lead, 52, 800)
  const innerW = W - 2 * M

  const rowY = 140
  const fakeH = 42
  const fakesH = 72 + FAKES.items.length * fakeH + 14

  const backW = 300
  const backH = 220
  const fakes: Frame = { x: M + backW + 80, y: rowY, w: innerW - backW - 80, h: fakesH, rx: 22 }
  const back = { x: M, y: rowY + fakesH / 2 - backH / 2 }

  const tagline: Frame = { x: M, y: H - 32 - 190, w: innerW, h: 190, rx: 22 }
  const tcY = rowY + fakesH + 70
  const tc: Frame = { x: M, y: tcY, w: innerW, h: tagline.y - 40 - tcY, rx: 22 }
  const tcW = (tc.w - 60 - 40) / 3
  const tcX = (i: number) => tc.x + 30 + i * (tcW + 20)

  return {
    titleGrad: { x1: titleX, x2: titleX + measure(TITLE.accent, 52, 800) },
    frames: [fakes, tc, tagline],
    headers: [
      header('fakes', fakes, FAKES.icon, FAKES.title, FAKES.color),
      header('tc', tc, TESTCONTAINERS.icon, TESTCONTAINERS.title, TESTCONTAINERS.color)
    ],
    hints: [hint(fakes, FAKES.hint), hint(tc, TESTCONTAINERS.hint)],
    arrows: [
      `M ${back.x + backW} ${back.y + backH / 2} L ${fakes.x - 6} ${back.y + backH / 2}`,
      `M ${back.x + backW / 2} ${back.y + backH} L ${back.x + backW / 2} ${tc.y - 6}`
    ],
    blocks: [
      withAddress(stackedNode(BACK, back.x, back.y, backW, backH), BACK, back.x + backW, back.y),
      ...TESTCONTAINERS.items.map((c, i) => {
        const x = tcX(i)
        const y = tc.y + 72
        const h = tc.h - 94

        return withAddress(stackedNode(c, x, y, tcW, h), c, x + tcW, y)
      })
    ],
    tagline: [
      { x: CX, y: tagline.y + tagline.h / 2 - 6, size: 30, weight: 700, fill: C.text, value: TAGLINE.title, anchor: 'middle' },
      { x: CX, y: tagline.y + tagline.h / 2 + 32, size: 19, weight: 400, fill: C.muted, value: TAGLINE.sub, anchor: 'middle' }
    ] as Label[],
    points: [
      point('linkedin', M + 4, 102, { title: LINKEDIN.handle, icon: LINKEDIN.icon, color: LINKEDIN.color, mono: true }, 16),
      ...FAKES.items.map((f, i) => {
        const x = fakes.x + 32
        const y = rowY + 96 + i * fakeH

        return {
          id: `fake-${f.title}`,
          icon: { name: f.icon, x, y: y - 18, size: 22, color: FAKES.color },
          title: { x: x + 36, y, size: 20, weight: 600, fill: C.text, value: f.title },
          sub: { x: x + 36 + measure(f.title, 20, 600) + 12, y, size: 15, weight: 400, fill: C.muted, value: f.sub, mono: true }
        }
      })
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
            Dois pôsteres de {{ W }} × {{ H }} (proporção de post do LinkedIn): a estratégia de testes do Estud — o
            Back e o Fakes sobem em memória, o Testcontainers levanta Postgres, MinIO e Keycloak, e o Back só aponta
            pra eles via configuração — e o
            gráfico de ritmo de desenvolvimento com e sem testes, baseado no livro do Vladimir Khorikov.
          </p>
          <p class="text-sm text-muted">
            O desenho é o próprio SVG desta página — para editar, mexa nas listas no topo de
            <code class="text-xs">app/pages/dev/tests-diagram.vue</code> e de
            <code class="text-xs">app/components/dev/TestsPacePoster.vue</code>. Os botões abaixo baixam exatamente o que está
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
        </div>

        <div class="grid grid-cols-1 items-start gap-6 2xl:grid-cols-2">
          <div class="space-y-3">
            <div class="flex items-center gap-2">
              <span class="text-sm font-medium">Arquitetura dos testes</span>

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

                  <text
                    v-for="h in poster.hints"
                    :key="h.value"
                    class="mono"
                    :x="h.x"
                    :y="h.y"
                    :font-size="h.size"
                    :fill="h.fill"
                    text-anchor="end"
                  >{{ h.value }}</text>

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
                      :text-anchor="b.title.anchor ?? 'start'"
                    >{{ b.title.value }}</text>
                    <text
                      v-if="b.sub"
                      class="mono"
                      :x="b.sub.x"
                      :y="b.sub.y"
                      :font-size="b.sub.size"
                      :fill="b.sub.fill"
                      :text-anchor="b.sub.anchor ?? 'start'"
                    >{{ b.sub.value }}</text>
                    <text
                      v-if="b.corner"
                      class="mono"
                      :x="b.corner.x"
                      :y="b.corner.y"
                      :font-size="b.corner.size"
                      :fill="b.corner.fill"
                      text-anchor="end"
                    >{{ b.corner.value }}</text>
                  </g>

                  <text
                    v-for="t in poster.tagline"
                    :key="t.value"
                    :x="t.x"
                    :y="t.y"
                    :font-size="t.size"
                    :font-weight="t.weight"
                    :fill="t.fill"
                    text-anchor="middle"
                  >{{ t.value }}</text>

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
                    <text
                      v-if="p.sub"
                      class="mono"
                      :x="p.sub.x"
                      :y="p.sub.y"
                      :font-size="p.sub.size"
                      :fill="p.sub.fill"
                    >{{ p.sub.value }}</text>
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

          <DevTestsPacePoster :zoom="zoom" />
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
