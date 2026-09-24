<script setup lang="ts">
interface ItemContent {
  title: string
  sub: string
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
  tile: { x: number, y: number, size: number, rx: number, color: string }
  icon: IconMark
  title: Label
  sub: Label
}

interface Header {
  id: string
  icon: IconMark
  title: Label
}

interface Curve {
  label: string
  color: string
  width: number
  value: (t: number) => number
  labelAt: number
  labelSide: 'left' | 'right' | 'end'
}

const props = defineProps<{ zoom: number }>()

const W = 1080
const H = 1350
const M = 64

const MONO = POSTER_MONO

const measure = measurePosterText

const C = {
  bg: '#2f1c51',
  card: '#211439',
  text: '#fafafa',
  muted: '#a1a1aa',
  violet: '#8b5cf6',
  violetLight: '#c4b5fd',
  axis: '#d4d4d8'
}

const ROSE = '#fb7185'
const AMBER = '#F59E0B'
const GREEN = '#34d399'

// Mesmo path do <EstudIcon>, em viewBox 24x24.
const ESTUD_E = 'M 10.98 20.5Q 9.46 20.5 8.27 19.61Q 7.08 18.72 6.4 17.13Q 5.73 15.53 5.73 13.49Q 5.73 11.37 6.34 9.54Q 6.96 7.72 8.08 6.37Q 9.2 5.02 10.71 4.26Q 12.22 3.5 13.97 3.5Q 16.09 3.5 17.18 4.56Q 18.27 5.62 18.27 7.66Q 18.27 9.19 17.44 10.41Q 16.61 11.63 15.16 12.32Q 13.71 13.01 11.84 13.01Q 10.92 13.01 10.12 12.9Q 9.32 12.8 8.57 12.63L 8.65 11.63H 9.72Q 11.38 11.63 12.62 11.07Q 13.85 10.51 14.54 9.47Q 15.23 8.44 15.23 7.09Q 15.23 6.08 14.76 5.54Q 14.28 4.99 13.42 4.99Q 12.33 4.99 11.47 5.81Q 10.61 6.63 10.0 7.94Q 9.4 9.24 9.09 10.74Q 8.77 12.23 8.77 13.58Q 8.77 16.11 9.53 17.38Q 10.29 18.66 11.78 18.66Q 14.6 18.66 16.41 15.85Q 17.47 16.02 17.47 16.62Q 16.24 18.63 14.67 19.57Q 13.11 20.5 10.98 20.5Z'

const LINKEDIN = { handle: '/in/zaqueu-cavalcante', icon: 'i-simple-icons-linkedin', color: '#4aa3f0' }

const TITLE = { lead: 'Quanto custa ', accent: 'não testar?' }

const CHART = {
  title: 'Ritmo de desenvolvimento',
  hint: 'custo de cada nova entrega',
  icon: 'i-lucide-trending-up',
  yAxis: 'Horas gastas',
  xAxis: 'Progresso',
  turn: 'ponto de virada',
  before: 'parece mais rápido',
  after: 'os testes se pagam',
  source: 'baseado em Khorikov, Unit Testing: Principles, Practices, and Patterns (2020)'
}

// As três curvas se cruzam no mesmo ponto, como no gráfico original: antes dele
// não testar parece mais barato, depois fica cada vez mais caro.
const TURN = 0.62
const TURN_VALUE = 0.4 + 0.18 * TURN

const CURVES: Curve[] = [
  {
    label: 'Sem testes',
    color: ROSE,
    width: 3.5,
    value: t => 0.08 + (TURN_VALUE - 0.08) * (Math.exp(6 * t) - 1) / (Math.exp(6 * TURN) - 1),
    labelAt: 0.93,
    labelSide: 'left'
  },
  {
    label: 'Com testes ruins',
    color: AMBER,
    width: 3.5,
    value: t => 0.22 + (TURN_VALUE - 0.22) * (Math.exp(3 * t) - 1) / (Math.exp(3 * TURN) - 1),
    labelAt: 0.72,
    labelSide: 'right'
  },
  {
    label: 'Com testes bons',
    color: GREEN,
    width: 4.5,
    value: t => 0.4 + 0.18 * t,
    labelAt: 1,
    labelSide: 'end'
  }
]

const CARDS = [
  {
    title: 'Sem testes',
    icon: 'i-lucide-circle-alert',
    color: ROSE,
    chipText: '#fecdd3',
    chips: ['Regressões', 'Débito técnico'],
    rows: [
      { title: 'Largada rápida', sub: 'Só código de produção', color: ROSE, icon: 'i-lucide-zap' },
      { title: 'Medo de mexer', sub: 'Conferência manual a cada PR', color: ROSE, icon: 'i-lucide-triangle-alert' }
    ] as ItemContent[]
  },
  {
    title: 'Testes ruins',
    icon: 'i-lucide-hourglass',
    color: AMBER,
    chipText: '#fcd34d',
    chips: ['Mocks demais', 'Flaky'],
    rows: [
      { title: 'Frágeis', sub: 'Presos à implementação', color: AMBER, icon: 'i-lucide-unlink' },
      { title: 'Falsos alarmes', sub: 'Quebram a cada refatoração', color: AMBER, icon: 'i-lucide-bell-ring' }
    ] as ItemContent[]
  },
  {
    title: 'Testes bons',
    icon: 'i-lucide-circle-check-big',
    color: GREEN,
    chipText: '#a7f3d0',
    chips: ['Checkpoints', 'Feedback rápido'],
    rows: [
      { title: 'Testam comportamento', sub: 'Resistentes a refatoração', color: GREEN, icon: 'i-lucide-shield-check' },
      { title: 'Ritmo sustentável', sub: 'Custo quase constante', color: GREEN, icon: 'i-lucide-trending-up' }
    ] as ItemContent[]
  }
]

const QUOTE = {
  icon: 'i-lucide-quote',
  lead: 'Cada teste é um checkpoint de comportamento.',
  sub: 'O git lembra como o código era; os testes lembram como o sistema deve ser.'
}

function chipRow(x: number, y: number, labels: string[], color: string, textColor: string, size = 12): Chip[] {
  let cursor = x

  return labels.map((label) => {
    const w = label.length * size * 0.6 + 18
    const chip: Chip = { x: cursor, y, w, size, color, textColor, label }
    cursor += w + 6
    return chip
  })
}

function item(content: ItemContent, x: number, y: number): Block {
  return {
    id: `row-${content.title}`,
    tile: { x, y, size: 34, rx: 10, color: content.color },
    icon: { name: content.icon, x: x + 8, y: y + 8, size: 18, color: content.color },
    title: { x: x + 46, y: y + 15, size: 16, weight: 600, fill: C.text, value: content.title },
    sub: { x: x + 46, y: y + 32, size: 12, weight: 400, fill: C.muted, value: content.sub, mono: true }
  }
}

function header(id: string, x: number, y: number, icon: string, title: string, color: string, centerIn: number): Header {
  const bx = x + (centerIn - (34 + measure(title, 22, 700))) / 2

  return {
    id,
    icon: { name: icon, x: bx, y: y + 16, size: 24, color },
    title: { x: bx + 34, y: y + 37, size: 22, weight: 700, fill: C.text, value: title }
  }
}

const poster = computed(() => {
  const titleX = M + measure(TITLE.lead, 52, 800)

  const gap = 38
  const cardsH = 200
  const quoteH = 110

  const chart: Frame = { x: M, y: 136, w: W - 2 * M, h: H - 32 - 136 - cardsH - quoteH - 2 * gap, rx: 28 }
  const cardsY = chart.y + chart.h + gap
  const quote: Frame = { x: M, y: cardsY + cardsH + gap, w: W - 2 * M, h: quoteH, rx: 22 }
  const colW = (W - 2 * M - 32) / 3
  const colX = (i: number) => M + i * (colW + 16)

  const ox = chart.x + 90
  const oy = chart.y + chart.h - 96
  const axisEnd = chart.x + chart.w - 40
  const axisTop = chart.y + 96
  const plotX = ox + 6
  const plotW = axisEnd - plotX - 36
  const plotH = oy - axisTop - 40

  const px = (t: number) => plotX + t * plotW
  const py = (v: number) => oy - v * plotH

  const curves = CURVES.map((curve) => {
    const points: string[] = []

    for (let i = 0; i <= 200; i++) {
      const t = i / 200
      const v = curve.value(t)

      if (v > 1) {
        let lo = (i - 1) / 200
        let hi = t
        while (hi - lo > 1e-4) {
          const mid = (lo + hi) / 2
          if (curve.value(mid) > 1) hi = mid
          else lo = mid
        }
        points.push(`${px(lo).toFixed(1)} ${py(1).toFixed(1)}`)
        break
      }

      points.push(`${px(t).toFixed(1)} ${py(v).toFixed(1)}`)
    }

    let labelT = 1
    if (curve.labelSide !== 'end') {
      let lo = 0
      let hi = 1
      while (hi - lo > 1e-4) {
        const mid = (lo + hi) / 2
        if (curve.value(mid) > curve.labelAt) hi = mid
        else lo = mid
      }
      labelT = lo
    }

    const lx = px(labelT)
    const ly = py(curve.value(labelT))
    const label: Label = curve.labelSide === 'left'
      ? { x: lx - 18, y: ly + 7, size: 20, weight: 700, fill: curve.color, value: curve.label, anchor: 'end' }
      : curve.labelSide === 'right'
        ? { x: lx + 18, y: ly + 7, size: 20, weight: 700, fill: curve.color, value: curve.label, anchor: 'start' }
        : { x: lx, y: ly - 20, size: 20, weight: 700, fill: curve.color, value: curve.label, anchor: 'end' }

    return { id: curve.label, d: `M ${points.join(' L ')}`, color: curve.color, width: curve.width, label }
  })

  const turnX = px(TURN)
  const turnY = py(TURN_VALUE)

  return {
    titleGrad: { x1: titleX, x2: titleX + measure(TITLE.accent, 52, 800) },
    frames: [chart, quote, ...[0, 1, 2].map((i): Frame => ({ x: colX(i), y: cardsY, w: colW, h: cardsH, rx: 22 }))],
    chartHeader: {
      icon: { name: CHART.icon, x: chart.x + 30, y: chart.y + 20, size: 30, color: GREEN } as IconMark,
      title: { x: chart.x + 72, y: chart.y + 45, size: 26, weight: 700, fill: C.text, value: CHART.title } as Label
    },
    before: { x: plotX, y: axisTop, w: turnX - plotX, h: oy - axisTop },
    after: { x: turnX, y: axisTop, w: axisEnd - turnX, h: oy - axisTop },
    grid: [0.25, 0.5, 0.75, 1].map(v => py(v)),
    axes: [
      `M ${ox} ${oy} L ${axisEnd} ${oy}`,
      `M ${ox} ${oy} L ${ox} ${axisTop}`
    ],
    turn: { x: turnX, y: turnY, line: `M ${turnX} ${turnY + 12} L ${turnX} ${oy}` },
    curves,
    pill: { x: turnX, y: py(0.22), w: CHART.turn.length * 9.4 + 28, label: CHART.turn },
    texts: [
      { x: chart.x + chart.w - 30, y: chart.y + 43, size: 14, weight: 400, fill: C.muted, value: CHART.hint, mono: true, anchor: 'end' },
      { x: ox + 20, y: axisTop + 14, size: 18, weight: 700, fill: C.text, value: CHART.yAxis },
      { x: axisEnd, y: oy - 16, size: 18, weight: 700, fill: C.text, value: CHART.xAxis, anchor: 'end' },
      { x: (plotX + turnX) / 2, y: oy + 34, size: 14, weight: 400, fill: ROSE, value: CHART.before, mono: true, anchor: 'middle' },
      { x: (turnX + axisEnd) / 2, y: oy + 34, size: 14, weight: 400, fill: GREEN, value: CHART.after, mono: true, anchor: 'middle' },
      { x: chart.x + chart.w - 30, y: chart.y + chart.h - 22, size: 12, weight: 400, fill: C.muted, value: CHART.source, mono: true, anchor: 'end' },
      { x: quote.x + 84, y: quote.y + 48, size: 24, weight: 700, fill: C.text, value: QUOTE.lead },
      { x: quote.x + 84, y: quote.y + 80, size: 17, weight: 400, fill: C.muted, value: QUOTE.sub }
    ] as Label[],
    headers: CARDS.map((c, i) => header(c.title, colX(i), cardsY, c.icon, c.title, c.color, colW)),
    blocks: CARDS.flatMap((c, ci) => c.rows.map((r, i) => item(r, colX(ci) + 18, cardsY + 56 + i * 46))),
    chips: CARDS.flatMap((c, i) => chipRow(colX(i) + 18, cardsY + 152, c.chips, c.color, c.chipText)),
    quoteIcon: { name: QUOTE.icon, x: quote.x + 30, y: quote.y + 30, size: 34, color: C.violetLight } as IconMark,
    logo: { x: W - M - 52, y: 28, scale: 52 / 24 },
    linkedin: {
      icon: { name: LINKEDIN.icon, x: M + 4, y: 87, size: 19, color: LINKEDIN.color } as IconMark,
      title: { x: M + 34, y: 102, size: 16, weight: 600, fill: C.muted, value: LINKEDIN.handle } as Label
    }
  }
})

function iconTransform(icon: IconMark): string {
  return `translate(${icon.x} ${icon.y}) scale(${icon.size / 24})`
}

const { fontsLoaded, posterEl, busy, posterStyle, downloadSvg, pngItems } = usePoster(W, H, 'estud-tests-pace-linkedin', toRef(props, 'zoom'))
</script>

<template>
  <div class="space-y-3">
    <div class="flex items-center gap-2">
      <span class="text-sm font-medium">Ritmo com e sem testes</span>

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
          aria-label="Ritmo de desenvolvimento com e sem testes automatizados"
        >
          <defs>
            <pattern id="pace-dots" width="28" height="28" patternUnits="userSpaceOnUse">
              <circle cx="2" cy="2" r="1.2" fill="#ffffff" fill-opacity="0.06" />
            </pattern>

            <linearGradient
              id="pace-titleGrad"
              gradientUnits="userSpaceOnUse"
              :x1="poster.titleGrad.x1"
              y1="0"
              :x2="poster.titleGrad.x2"
              y2="0"
            >
              <stop offset="0" stop-color="#c4b5fd" />
              <stop offset="1" stop-color="#8b5cf6" />
            </linearGradient>

            <linearGradient id="pace-railStroke" x1="0" y1="0" x2="1" y2="1">
              <stop offset="0" stop-color="#a78bfa" stop-opacity="0.7" />
              <stop offset="0.5" stop-color="#ffffff" stop-opacity="0.08" />
              <stop offset="1" stop-color="#6366f1" stop-opacity="0.5" />
            </linearGradient>

            <linearGradient id="pace-before" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0" :stop-color="ROSE" stop-opacity="0" />
              <stop offset="1" :stop-color="ROSE" stop-opacity="0.07" />
            </linearGradient>

            <linearGradient id="pace-after" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0" :stop-color="GREEN" stop-opacity="0.07" />
              <stop offset="1" :stop-color="GREEN" stop-opacity="0" />
            </linearGradient>

            <filter id="pace-glow" x="-50%" y="-50%" width="200%" height="200%">
              <feGaussianBlur stdDeviation="5" />
            </filter>

            <marker
              id="pace-axis"
              viewBox="0 0 10 10"
              refX="8"
              refY="5"
              markerWidth="4"
              markerHeight="4"
              orient="auto"
              markerUnits="strokeWidth"
            >
              <path d="M0 0 L10 5 L0 10 z" :fill="C.axis" />
            </marker>
          </defs>

          <rect :width="W" :height="H" :fill="C.bg" />
          <rect :width="W" :height="H" fill="url(#pace-dots)" />

          <text
            :x="M"
            y="72"
            font-size="52"
            font-weight="800"
            :fill="C.text"
          >{{ TITLE.lead }}<tspan fill="url(#pace-titleGrad)">{{ TITLE.accent }}</tspan></text>

          <g :transform="`translate(${poster.logo.x} ${poster.logo.y}) scale(${poster.logo.scale})`">
            <rect width="24" height="24" rx="6" fill="#7c3aed" />
            <path fill="#fff" :d="ESTUD_E" />
          </g>

          <g :transform="iconTransform(poster.linkedin.icon)" :style="{ color: poster.linkedin.icon.color }" v-html="archIcons[poster.linkedin.icon.name]" />
          <text
            class="mono"
            :x="poster.linkedin.title.x"
            :y="poster.linkedin.title.y"
            :font-size="poster.linkedin.title.size"
            :font-weight="poster.linkedin.title.weight"
            :fill="poster.linkedin.title.fill"
          >{{ poster.linkedin.title.value }}</text>

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
            stroke="url(#pace-railStroke)"
            stroke-width="2"
          />

          <g :transform="iconTransform(poster.chartHeader.icon)" :style="{ color: poster.chartHeader.icon.color }" v-html="archIcons[poster.chartHeader.icon.name]" />
          <text
            :x="poster.chartHeader.title.x"
            :y="poster.chartHeader.title.y"
            :font-size="poster.chartHeader.title.size"
            :font-weight="poster.chartHeader.title.weight"
            :fill="poster.chartHeader.title.fill"
          >{{ poster.chartHeader.title.value }}</text>

          <rect
            :x="poster.before.x"
            :y="poster.before.y"
            :width="poster.before.w"
            :height="poster.before.h"
            fill="url(#pace-before)"
          />
          <rect
            :x="poster.after.x"
            :y="poster.after.y"
            :width="poster.after.w"
            :height="poster.after.h"
            fill="url(#pace-after)"
          />

          <line
            v-for="y in poster.grid"
            :key="`grid-${y}`"
            :x1="poster.before.x"
            :y1="y"
            :x2="poster.after.x + poster.after.w"
            :y2="y"
            stroke="#ffffff"
            stroke-opacity="0.06"
            stroke-dasharray="4 8"
          />

          <path
            v-for="a in poster.axes"
            :key="a"
            :d="a"
            fill="none"
            :stroke="C.axis"
            stroke-opacity="0.8"
            stroke-width="3"
            stroke-linecap="round"
            marker-end="url(#pace-axis)"
          />

          <path
            :d="poster.turn.line"
            fill="none"
            :stroke="C.violetLight"
            stroke-opacity="0.6"
            stroke-width="2"
            stroke-dasharray="3 7"
            stroke-linecap="round"
          />

          <g v-for="c in poster.curves" :key="c.id">
            <path
              :d="c.d"
              fill="none"
              :stroke="c.color"
              :stroke-width="c.width * 3"
              stroke-opacity="0.22"
              stroke-linecap="round"
              stroke-linejoin="round"
              filter="url(#pace-glow)"
            />
            <path
              :d="c.d"
              fill="none"
              :stroke="c.color"
              :stroke-width="c.width"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
            <text
              :x="c.label.x"
              :y="c.label.y"
              :font-size="c.label.size"
              :font-weight="c.label.weight"
              :fill="c.label.fill"
              :text-anchor="c.label.anchor"
            >{{ c.label.value }}</text>
          </g>

          <circle
            :cx="poster.turn.x"
            :cy="poster.turn.y"
            r="16"
            :fill="C.violetLight"
            fill-opacity="0.25"
            filter="url(#pace-glow)"
          />
          <circle
            :cx="poster.turn.x"
            :cy="poster.turn.y"
            r="8"
            :fill="C.card"
            stroke="#ffffff"
            stroke-width="3"
          />

          <g>
            <rect
              :x="poster.pill.x - poster.pill.w / 2"
              :y="poster.pill.y - 15"
              :width="poster.pill.w"
              height="30"
              rx="15"
              fill="#1c1830"
              :stroke="C.violet"
              stroke-opacity="0.55"
            />
            <text
              class="mono"
              :x="poster.pill.x"
              :y="poster.pill.y + 5"
              text-anchor="middle"
              font-size="15"
              font-weight="600"
              :fill="C.violetLight"
            >{{ poster.pill.label }}</text>
          </g>

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

          <g v-for="b in poster.blocks" :key="b.id">
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
              class="mono"
              :x="b.sub.x"
              :y="b.sub.y"
              :font-size="b.sub.size"
              :fill="b.sub.fill"
            >{{ b.sub.value }}</text>
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

          <g :transform="iconTransform(poster.quoteIcon)" :style="{ color: poster.quoteIcon.color }" v-html="archIcons[poster.quoteIcon.name]" />

          <text
            v-for="t in poster.texts"
            :key="t.value"
            :class="{ mono: t.mono }"
            :x="t.x"
            :y="t.y"
            :font-size="t.size"
            :font-weight="t.weight"
            :fill="t.fill"
            :text-anchor="t.anchor"
          >{{ t.value }}</text>
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

<style scoped>
.poster {
  font-family: 'Saira', sans-serif;
}

.poster .mono {
  font-family: 'JetBrains Mono', monospace;
}
</style>
