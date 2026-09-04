<script setup lang="ts">
import type { ClassStudentItem } from '~/types/classes'
import { VisXYContainer, VisScatter, VisAxis, VisPlotline, VisTooltip, VisScatterSelectors, VisPlotlineSelectors } from '@unovis/vue'

const props = defineProps<{
  students: ClassStudentItem[]
  noteLimit: number
  frequencyLimit: number
}>()

interface StudentPoint {
  name: string
  grade: number
  attendance: number
  withinLimits: boolean
}

const chartRef = useTemplateRef<HTMLElement | null>('chartRef')
const { width } = useElementSize(chartRef)

const points = computed<StudentPoint[]>(() => props.students.map((student) => {
  const grade = student.averageGrade ?? 0
  const attendance = student.averageAttendance

  return {
    name: student.name,
    grade,
    attendance,
    withinLimits: grade >= props.noteLimit && attendance >= props.frequencyLimit,
  }
}))

const formatGrade = (grade: number) => grade.toFixed(1).replace('.', ',')
const formatPercent = (percent: number) => `${Math.round(percent)}%`

const x = (d: StudentPoint) => d.attendance
const y = (d: StudentPoint) => d.grade
const color = (d: StudentPoint) => d.withinLimits ? 'var(--ui-success)' : 'var(--ui-error)'
const shape = (d: StudentPoint) => d.withinLimits ? 'circle' : 'triangle'

// O nome do aluno vem do banco: monta o conteúdo como nós de texto em vez de
// string de HTML, pra não abrir espaço pra injeção no tooltip.
function tooltip(d: StudentPoint) {
  const root = document.createElement('div')
  root.className = 'flex flex-col gap-0.5'

  const name = document.createElement('span')
  name.className = 'font-medium'
  name.textContent = d.name

  const grade = document.createElement('span')
  grade.className = 'text-xs opacity-70'
  grade.textContent = `Nota ${formatGrade(d.grade)}`

  const attendance = document.createElement('span')
  attendance.className = 'text-xs opacity-70'
  attendance.textContent = `Frequência ${formatPercent(d.attendance)}`

  root.append(name, grade, attendance)
  return root
}

function limitTooltip(_: unknown, i: number, elements: (HTMLElement | SVGElement)[]) {
  const line = elements[i] as SVGLineElement | undefined
  if (!line) return

  const horizontal = line.getAttribute('y1') === line.getAttribute('y2')

  return horizontal
    ? `Nota mínima para aprovação: ${formatGrade(props.noteLimit)}`
    : `Frequência mínima para aprovação: ${formatPercent(props.frequencyLimit)}`
}

const triggers = {
  [VisScatterSelectors.point]: tooltip,
  [VisPlotlineSelectors.plotline]: limitTooltip,
}
</script>

<template>
  <div ref="chartRef" class="flex flex-col gap-3">
    <h3 class="text-center text-sm font-medium text-highlighted">
      Nota e frequência por aluno
    </h3>

    <div v-if="!points.length" class="flex flex-col items-center justify-center gap-3 py-12 text-center">
      <UIcon name="i-lucide-chart-scatter" class="size-8 text-muted" />
      <p class="text-sm text-muted">
        Nenhum aluno matriculado
      </p>
    </div>

    <VisXYContainer
      v-else
      :data="points"
      :x-domain="[0, 100]"
      :y-domain="[0, 10]"
      :margin="{ top: 8, right: 12, left: 4 }"
      class="h-96"
      :width="width"
    >
      <VisPlotline
        axis="y"
        :value="noteLimit"
        color="var(--ui-border-accented)"
        :line-width="1"
        :line-style="[4, 4]"
      />

      <VisPlotline
        axis="x"
        :value="frequencyLimit"
        color="var(--ui-border-accented)"
        :line-width="1"
        :line-style="[4, 4]"
      />

      <!-- Linhas transparentes e largas por cima das tracejadas: com 1px de
           traço não dá pra acertar a linha com o mouse pra ver o tooltip. -->
      <VisPlotline
        axis="y"
        :value="noteLimit"
        color="transparent"
        :line-width="16"
      />

      <VisPlotline
        axis="x"
        :value="frequencyLimit"
        color="transparent"
        :line-width="16"
      />

      <VisScatter
        :x="x"
        :y="y"
        :color="color"
        :shape="shape"
        :size="11"
        stroke-color="var(--ui-bg)"
        :stroke-width="2"
        cursor="pointer"
      />

      <VisAxis
        type="x"
        label="Frequência média"
        :domain-line="false"
        :tick-values="[0, 25, 50, 75, 100]"
        :tick-format="(value: number) => formatPercent(value)"
      />

      <VisAxis
        type="y"
        label="Nota média"
        :domain-line="false"
        :tick-values="[0, 2, 4, 6, 8, 10]"
        :tick-format="(value: number) => String(value)"
      />

      <VisTooltip :triggers="triggers" />
    </VisXYContainer>
  </div>
</template>

<style scoped>
.unovis-xy-container {
  --vis-font-family: var(--font-sans);

  --vis-axis-grid-color: var(--ui-border);
  --vis-axis-tick-color: var(--ui-border);
  --vis-axis-tick-label-color: var(--ui-text-dimmed);
  --vis-axis-label-color: var(--ui-text-muted);

  --vis-tooltip-background-color: var(--ui-bg);
  --vis-tooltip-border-color: var(--ui-border);
  --vis-tooltip-text-color: var(--ui-text-highlighted);
}
</style>
