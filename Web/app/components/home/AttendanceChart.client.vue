<script setup lang="ts">
import { ptBR } from 'date-fns/locale'
import { addDays, format, isAfter, isWeekend, parseISO, startOfDay } from 'date-fns'
import type { PeriodItem } from '~/types/calendar'
import { VisXYContainer, VisStackedBar, VisScatter, VisAxis, VisCrosshair, VisTooltip } from '@unovis/vue'

type DataRecord = { date: Date, attendance: number }

const { frequencyLimit, period } = defineProps<{ frequencyLimit: number, period?: PeriodItem }>()

// TODO: trocar pelos dados do backend
function mockAttendance(): DataRecord[] {
  if (!period) return []

  const periodStart = parseISO(period.startAt)
  const periodEnd = parseISO(period.endAt)
  const today = startOfDay(new Date())
  const records: DataRecord[] = []

  for (let date = periodStart, i = 0; !isAfter(date, periodEnd) && !isAfter(date, today); date = addDays(date, 1), i++) {
    if (isWeekend(date)) continue

    const trend = 80 - i * 0.1
    const noise = Math.sin(i * 1.7) * 9 + Math.cos(i * 0.6) * 6 + Math.sin(i * 4.3) * 4
    records.push({ date, attendance: Math.min(100, Math.max(0, trend + noise)) })
  }

  if (records[9]) records[9].attendance = 0

  return records
}

const data = computed(() => mockAttendance())

const cardRef = useTemplateRef<HTMLElement | null>('cardRef')
const { width } = useElementSize(cardRef)

const x = (_: DataRecord, i: number) => i
// Dia com 0% ainda precisa de uma barra visível para o usuário notar e passar o mouse.
const y = (d: DataRecord) => Math.max(d.attendance, 1.5)
const color = computed(() => {
  const limit = frequencyLimit
  return (d: DataRecord) => d.attendance < limit ? 'var(--ui-error)' : 'var(--ui-success)'
})

const hoveredIndex = ref<number>()
const arrowIndex = ref(0)
const onCrosshairMove = (_x?: number | Date, _d?: DataRecord, i?: number) => {
  hoveredIndex.value = i
  if (i !== undefined) arrowIndex.value = i
}

// O container força os dados das barras no scatter. Só o ponto 0 aparece e ele segue a barra
// em hover: como a chave do ponto não muda, o Unovis anima o deslocamento entre as barras.
const arrowX = computed(() => {
  const i = arrowIndex.value
  return () => i
})
const arrowY = computed(() => {
  const d = data.value[arrowIndex.value]
  const top = d ? y(d) + 6 : 0
  return () => top
})
const arrowSize = computed(() => {
  const visible = hoveredIndex.value !== undefined
  return (_: DataRecord, i: number) => visible && i === 0 ? 10 : 0
})

const average = computed(() => data.value.length
  ? data.value.reduce((acc, d) => acc + d.attendance, 0) / data.value.length
  : 0)

const formatPercent = (value: number) => `${value.toFixed(1).replace('.', ',')}%`
const formatDate = (date: Date) => format(date, 'd MMM', { locale: ptBR })

const xTicks = computed(() => {
  const count = Math.min(6, data.value.length)
  if (count <= 1) return [0]
  return Array.from({ length: count }, (_, i) => Math.round(i * (data.value.length - 1) / (count - 1)))
})

const template = (d: DataRecord) =>
  `${format(d.date, "EEE, d 'de' MMM", { locale: ptBR })}: ${formatPercent(d.attendance)}`
</script>

<template>
  <UCard ref="cardRef" :ui="{ root: 'overflow-visible', body: 'px-0! pt-0! pb-3!' }">
    <template #header>
      <p class="text-xs text-muted uppercase mb-1.5">
        Frequência média das turmas
      </p>
      <p class="text-3xl text-highlighted font-semibold">
        {{ data.length ? formatPercent(average) : '-' }}
      </p>
    </template>

    <div v-if="!data.length" class="flex flex-col items-center justify-center gap-3 py-12 text-center">
      <UIcon name="i-lucide-chart-column" class="size-8 text-muted" />
      <p class="text-sm text-muted">
        Nenhuma frequência registrada no período
      </p>
    </div>

    <VisXYContainer
      v-else
      :data="data"
      :x-domain="[-0.5, data.length - 0.5]"
      :y-domain="[0, 100]"
      :padding="{ top: 40 }"
      :margin="{ left: 24, right: 24 }"
      class="h-80"
      :width="width"
    >
      <VisStackedBar
        :x="x"
        :y="y"
        :color="color"
        :data-step="1"
        :bar-padding="0.2"
        :bar-max-width="32"
        :rounded-corners="2"
      />

      <VisScatter
        :x="arrowX"
        :y="arrowY"
        :size="arrowSize"
        :duration="120"
        shape="triangle"
        color="var(--ui-primary)"
      />

      <VisAxis
        type="x"
        :x="x"
        :tick-values="xTicks"
        :tick-format="(value: number) => formatDate(data[value]!.date)"
      />

      <VisAxis
        type="y"
        :tick-values="[0, 25, 50, 75, 100]"
        :tick-format="(value: number) => `${value}%`"
      />

      <!-- O VisCrosshair não declara props: os attrs chegam crus ao Unovis, então precisam ficar em camelCase. -->
      <VisCrosshair
        :x="x"
        :y="y"
        :getCircles="() => []"
        :template="template"
        :onCrosshairMove="onCrosshairMove"
      />

      <VisTooltip />
    </VisXYContainer>
  </UCard>
</template>

<style scoped>
.unovis-xy-container {
  --vis-crosshair-line-stroke-opacity: 0;

  --vis-axis-grid-color: var(--ui-border);
  --vis-axis-tick-color: var(--ui-border);
  --vis-axis-tick-label-color: var(--ui-text-dimmed);

  --vis-tooltip-background-color: var(--ui-bg);
  --vis-tooltip-border-color: var(--ui-border);
  --vis-tooltip-text-color: var(--ui-text-highlighted);
}
.unovis-xy-container :deep([class*="scatter-component"] path) {
  transform: rotate(180deg);
  transform-box: fill-box;
  transform-origin: center;
}
</style>
