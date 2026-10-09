<script setup lang="ts">
import { ptBR } from 'date-fns/locale'
import { format, parseISO } from 'date-fns'
import type { PeriodItem } from '~/types/calendar'
import type { GetAttendanceOut } from '~/types/frequencies'
import { VisXYContainer, VisStackedBar, VisScatter, VisAxis, VisCrosshair, VisTooltip } from '@unovis/vue'

type DataRecord = { date: Date, attendance: number }

const { frequencyLimit, period } = defineProps<{ frequencyLimit: number, period?: PeriodItem }>()

const config = useRuntimeConfig()
const { can } = usePolicy()
const canGetAttendance = can('GetAttendance')

const { data: attendanceData, status, execute } = await useFetch<GetAttendanceOut>(`${config.public.backendUrl}/insights/attendance`, {
  query: computed(() => ({ periodId: period?.id })),
  credentials: 'include',
  server: false,
  immediate: false,
  watch: false,
})

watch(() => period?.id, (id) => {
  if (id && canGetAttendance.value) execute()
}, { immediate: true })

const data = computed<DataRecord[]>(() => period
  ? (attendanceData.value?.days ?? []).map(d => ({ date: parseISO(d.date), attendance: d.attendance }))
  : [])

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

const average = computed(() => attendanceData.value?.average ?? 0)

const limitSegments = computed(() => [
  { label: 'Abaixo da mínima', value: attendanceData.value?.belowLimitClasses ?? 0, color: 'bg-error' },
  { label: 'Acima da mínima', value: attendanceData.value?.aboveLimitClasses ?? 0, color: 'bg-success' },
])

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
      <div class="flex items-start justify-between gap-4">
        <div>
          <p class="text-xs text-muted uppercase mb-1.5">
            Frequência média das turmas
          </p>
          <p
            class="text-3xl font-semibold"
            :class="status === 'pending' || !data.length ? 'text-highlighted' : average < frequencyLimit ? 'text-error' : 'text-success'"
          >
            {{ status !== 'pending' && data.length ? formatPercent(average) : '-' }}
          </p>
        </div>

        <ul v-if="status !== 'pending' && data.length" class="space-y-1.5">
          <li v-for="s in limitSegments" :key="s.label" class="flex items-center gap-2 text-sm">
            <span class="size-2 rounded-full shrink-0" :class="s.color" />
            <span class="text-muted">{{ s.label }}</span>
            <span class="font-medium text-highlighted tabular-nums">{{ s.value }}</span>
          </li>
        </ul>
      </div>
    </template>

    <div v-if="status === 'pending'" class="flex h-80 items-center justify-center">
      <AppSpinner class="size-8" />
    </div>

    <div v-else-if="!data.length" class="flex flex-col items-center justify-center gap-3 py-12 text-center">
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
