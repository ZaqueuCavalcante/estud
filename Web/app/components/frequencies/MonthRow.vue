<script setup lang="ts">
import type { AttendanceCell, AttendanceMonthSpan } from '~/types/frequencies'

const props = defineProps<{
  weeks: AttendanceCell[][]
  months: AttendanceMonthSpan[]
}>()

const CELL_GAP = 3
// Folga em volta do SVG: o contorno das bordas externas corre meio gap pra fora da grid.
const OVERLAY_PAD = 2
const CORNER_RADIUS = 4

const grid = useTemplateRef<HTMLElement | null>('grid')
const { width } = useElementSize(grid)

// Distância entre o início de uma coluna e o da próxima (quadradinho + gap).
const pitch = computed(() => {
  if (props.weeks.length === 0 || width.value === 0) return 0
  return (width.value + CELL_GAP) / props.weeks.length
})

const overlayWidth = computed(() => props.weeks.length * pitch.value - CELL_GAP + OVERLAY_PAD * 2)
const overlayHeight = computed(() => 7 * pitch.value - CELL_GAP + OVERLAY_PAD * 2)

// Coordenada da fronteira `k` (0 = antes da primeira coluna/linha): corre no meio do gap.
function edge(k: number): number {
  return k * pitch.value - CELL_GAP / 2 + OVERLAY_PAD
}

// O mês ocupa o fim da primeira coluna, as colunas inteiras do meio e o começo
// da última — um retângulo com um degrau em cada ponta.
function outline(month: AttendanceMonthSpan): [number, number][] {
  const { startCol, startRow, endCol, endRow } = month
  const points: [number, number][] = [
    [edge(startCol), edge(startRow)],
    [edge(startCol + 1), edge(startRow)],
    [edge(startCol + 1), edge(0)],
    [edge(endCol + 1), edge(0)],
    [edge(endCol + 1), edge(endRow + 1)],
    [edge(endCol), edge(endRow + 1)],
    [edge(endCol), edge(7)],
    [edge(startCol), edge(7)],
  ]
  return points.filter((p, i) => {
    const prev = points[(i - 1 + points.length) % points.length]!
    return p[0] !== prev[0] || p[1] !== prev[1]
  })
}

function towards(from: [number, number], to: [number, number], radius: number): [number, number] {
  const dx = to[0] - from[0]
  const dy = to[1] - from[1]
  const length = Math.hypot(dx, dy) || 1
  const ratio = Math.min(radius, length / 2) / length
  return [from[0] + dx * ratio, from[1] + dy * ratio]
}

function monthPath(month: AttendanceMonthSpan): string {
  const points = outline(month)
  let d = ''
  for (let i = 0; i < points.length; i++) {
    const current = points[i]!
    const previous = points[(i - 1 + points.length) % points.length]!
    const next = points[(i + 1) % points.length]!
    const from = towards(current, previous, CORNER_RADIUS)
    const to = towards(current, next, CORNER_RADIUS)
    d += `${i === 0 ? 'M' : 'L'} ${from[0].toFixed(1)} ${from[1].toFixed(1)}`
    d += ` Q ${current[0].toFixed(1)} ${current[1].toFixed(1)} ${to[0].toFixed(1)} ${to[1].toFixed(1)} `
  }
  return `${d}Z`
}

const labels = computed(() => props.months.map(month => ({
  key: month.key,
  label: month.label,
  left: (month.startCol + month.endCol + 1) / 2 * pitch.value - CELL_GAP / 2,
})))
</script>

<template>
  <div class="flex flex-col gap-1">
    <div class="relative h-4">
      <span
        v-for="label in labels"
        :key="label.key"
        class="absolute -translate-x-1/2 whitespace-nowrap text-2xs leading-4 text-muted"
        :style="{ left: `${label.left}px` }"
      >
        {{ label.label }}
      </span>
    </div>

    <div ref="grid" class="relative flex gap-[3px]">
      <svg
        v-if="pitch > 0"
        class="pointer-events-none absolute stroke-default"
        :style="{ left: `${-OVERLAY_PAD}px`, top: `${-OVERLAY_PAD}px` }"
        :width="overlayWidth"
        :height="overlayHeight"
        :viewBox="`0 0 ${overlayWidth} ${overlayHeight}`"
        fill="none"
      >
        <path
          v-for="month in months"
          :key="month.key"
          :d="monthPath(month)"
          stroke-width="1"
        />
      </svg>

      <div
        v-for="(week, wi) in weeks"
        :key="wi"
        class="flex min-w-0 flex-1 flex-col gap-[3px]"
      >
        <template
          v-for="(cell, di) in week"
          :key="di"
        >
          <FrequenciesDayCell
            v-if="cell.date"
            :date="cell.date"
            :status="cell.status!"
            class="w-full"
          />

          <div v-else class="aspect-square w-full" />
        </template>
      </div>
    </div>
  </div>
</template>
