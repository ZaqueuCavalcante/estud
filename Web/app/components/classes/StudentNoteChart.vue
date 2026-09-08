<script setup lang="ts">
import type { StudentClassActivityItem } from '~/types/classes'

const props = defineProps<{ note: string, activities: StudentClassActivityItem[] }>()

const MAX_NOTE = 10
const MAX_WEIGHT = 100
const HEIGHT = 240
const MARGIN = { top: 16, right: 16, bottom: 28, left: 28 }
const NOTE_TICKS = [0, 2, 4, 6, 8, 10]
const WEIGHT_TICKS = [0, 25, 50, 75, 100]
const LABEL_MIN_WEIGHT = 12
const TOOLTIP_GAP = 12

interface Bar {
  activity: StudentClassActivityItem
  graded: boolean
  value: number
  x: number
  width: number
  y: number
  height: number
  labelY: number
}

const chartRef = useTemplateRef<HTMLElement | null>('chartRef')
const tooltipRef = useTemplateRef<HTMLElement | null>('tooltipRef')
const { width } = useElementSize(chartRef)
const { width: tooltipWidth, height: tooltipHeight } = useElementSize(tooltipRef)

const plotHeight = HEIGHT - MARGIN.top - MARGIN.bottom
const plotWidth = computed(() => Math.max(width.value - MARGIN.left - MARGIN.right, 0))

const xScale = (weight: number) => MARGIN.left + weight / MAX_WEIGHT * plotWidth.value
const yScale = (note: number) => MARGIN.top + plotHeight - note / MAX_NOTE * plotHeight

const bars = computed<Bar[]>(() => {
  let weightOffset = 0

  return props.activities.map((activity) => {
    const start = weightOffset
    weightOffset += activity.weight

    const graded = activity.workStatus === 'Finalized'
    const value = graded ? Math.min(Math.max(activity.value, 0), MAX_NOTE) : 0

    return {
      activity,
      graded,
      value,
      x: xScale(start),
      width: xScale(start + activity.weight) - xScale(start),
      y: yScale(value),
      height: yScale(0) - yScale(value),
      labelY: Math.max(yScale(value) - 5, MARGIN.top - 6),
    }
  })
})

const earned = computed(() => props.activities.reduce((total, a) => total + a.ponderedValue, 0))
const tooltip = ref<{ x: number, y: number, bar: Bar } | null>(null)

const tooltipStyle = computed(() => {
  if (!tooltip.value) return
  const half = tooltipWidth.value / 2
  return {
    left: `${Math.min(Math.max(tooltip.value.x, half), Math.max(width.value - half, half))}px`,
    top: `${Math.max(tooltip.value.y - tooltipHeight.value - TOOLTIP_GAP, 0)}px`,
  }
})

function moveTooltip(event: MouseEvent, bar: Bar) {
  const rect = chartRef.value?.getBoundingClientRect()
  if (!rect) return

  tooltip.value = { x: event.clientX - rect.left, y: event.clientY - rect.top, bar }
}

function formatNote(note: number) {
  return note.toFixed(2).replace(/0+$/, '').replace(/\.$/, '').replace('.', ',')
}

function barLabel(bar: Bar) {
  const note = bar.graded ? `nota ${formatNote(bar.activity.value)}` : 'sem nota'
  return `${bar.activity.title} · peso ${bar.activity.weight}% · ${note}`
}
</script>

<template>
  <div class="flex flex-col gap-2 rounded-lg border border-default bg-elevated/40 p-4">
    <div class="flex items-baseline justify-between gap-2">
      <h3 class="font-medium text-highlighted">
        {{ note }}
      </h3>
      <span class="text-sm text-muted">
        <span class="font-medium text-highlighted">{{ formatNote(earned) }}</span> / {{ MAX_NOTE }}
      </span>
    </div>

    <div ref="chartRef" class="relative">
      <svg
        :width="width"
        :height="HEIGHT"
        role="img"
        :aria-label="`Desempenho na ${note}: ${formatNote(earned)} de ${MAX_NOTE} pontos`"
        @mouseleave="() => { tooltip = null }"
      >
        <rect
          :width="width"
          :height="HEIGHT"
          fill="transparent"
          @mousemove="() => { tooltip = null }"
        />

        <g>
          <template v-for="tick in NOTE_TICKS" :key="tick">
            <line
              :x1="MARGIN.left"
              :x2="MARGIN.left + plotWidth"
              :y1="yScale(tick)"
              :y2="yScale(tick)"
              stroke="var(--ui-border)"
            />
            <text
              :x="MARGIN.left - 6"
              :y="yScale(tick)"
              text-anchor="end"
              dominant-baseline="middle"
              fill="var(--ui-text-dimmed)"
              class="text-[10px]"
            >
              {{ tick }}
            </text>
          </template>
        </g>

        <g
          v-for="bar in bars"
          :key="bar.activity.id"
          role="img"
          :aria-label="barLabel(bar)"
          cursor="pointer"
          @mousemove="(e) => { moveTooltip(e, bar) }"
        >
          <rect
            v-if="bar.height"
            :x="bar.x"
            :y="bar.y"
            :width="bar.width"
            :height="bar.height"
            fill="var(--ui-primary)"
            :opacity="!tooltip || tooltip.bar.activity.id === bar.activity.id ? 1 : 0.4"
          />

          <rect
            :x="bar.x"
            :y="MARGIN.top"
            :width="bar.width"
            :height="plotHeight"
            fill="none"
            :stroke="tooltip?.bar.activity.id === bar.activity.id ? 'var(--ui-primary)' : 'var(--ui-border-accented)'"
            :stroke-dasharray="bar.graded ? undefined : '3 3'"
            pointer-events="all"
          />

          <text
            v-if="bar.graded && bar.activity.weight >= LABEL_MIN_WEIGHT"
            :x="bar.x + bar.width / 2"
            :y="bar.labelY"
            text-anchor="middle"
            fill="var(--ui-text-muted)"
            class="text-[10px]"
          >
            {{ formatNote(bar.value) }}
          </text>
        </g>

        <g>
          <text
            v-for="tick in WEIGHT_TICKS"
            :key="tick"
            :x="xScale(tick)"
            :y="MARGIN.top + plotHeight + 16"
            text-anchor="middle"
            fill="var(--ui-text-dimmed)"
            class="text-[10px]"
          >
            {{ tick }}%
          </text>
        </g>
      </svg>

      <div
        v-if="tooltip"
        ref="tooltipRef"
        class="pointer-events-none absolute flex w-max max-w-56 -translate-x-1/2 flex-col gap-0.5 rounded-md border border-default bg-default px-2.5 py-1.5 shadow-lg"
        :class="tooltipHeight ? 'opacity-100' : 'opacity-0'"
        :style="tooltipStyle"
      >
        <span class="font-medium text-highlighted">{{ tooltip.bar.activity.title }}</span>
        <span class="text-xs opacity-70">
          {{ classActivityTypeLabels[tooltip.bar.activity.type] ?? tooltip.bar.activity.type }} · peso {{ tooltip.bar.activity.weight }}%
        </span>
        <span v-if="tooltip.bar.graded" class="text-xs opacity-70">
          Nota {{ formatNote(tooltip.bar.activity.value) }} · {{ formatNote(tooltip.bar.activity.ponderedValue) }} de {{ formatNote(tooltip.bar.activity.weight / MAX_WEIGHT * MAX_NOTE) }} pontos
        </span>
        <span v-else class="text-xs opacity-70">
          {{ classActivityWorkStatusLabels[tooltip.bar.activity.workStatus] ?? tooltip.bar.activity.workStatus }} · sem nota
        </span>
        <span class="text-xs opacity-70">
          Entrega até {{ formatClassActivityDueDate(tooltip.bar.activity.dueDate, tooltip.bar.activity.dueHour) }}
        </span>
      </div>
    </div>
  </div>
</template>
