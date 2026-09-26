<script setup lang="ts">
import { breakpointsTailwind, useBreakpoints, useElementSize } from '@vueuse/core'
import type {
  AttendanceCell,
  AttendanceDay,
  AttendanceMonthSpan,
  GetStudentAttendanceCalendarOut,
  StudentDayAttendanceStatus
} from '~/types/frequencies'

interface MonthGrid {
  key: string
  label: string
  weeks: AttendanceCell[][]
}

interface GridRow {
  key: string
  weeks: AttendanceCell[][]
  months: AttendanceMonthSpan[]
}

const config = useRuntimeConfig()

// Calendário de frequência do aluno logado (ano corrente).
const { data, status } = await useAsyncData<GetStudentAttendanceCalendarOut>(
  'student-attendance-calendar',
  () => $fetch<GetStudentAttendanceCalendarOut>(
    `${config.public.backendUrl}/students/attendances/calendar`,
    { credentials: 'include' }
  ),
  { server: false }
)

// Normaliza os itens da API pra grid: reduz a data pra 'YYYY-MM-DD'.
const days = computed<AttendanceDay[]>(() =>
  (data.value?.items ?? []).map(item => ({ date: item.date.slice(0, 10), status: item.status }))
)

function toIso(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

// Monta as colunas (semanas) x linhas (dias da semana), igual ao mapa de commits
// do GitHub: as semanas seguem contínuas, atravessando a virada de mês.
function buildWeeks(items: AttendanceDay[]): AttendanceCell[][] {
  const byDate = new Map<string, StudentDayAttendanceStatus>()
  for (const d of items) byDate.set(d.date, d.status)

  const sorted = [...byDate.keys()].sort()
  const first = new Date(`${sorted[0]}T00:00:00`)
  const last = new Date(`${sorted[sorted.length - 1]}T00:00:00`)

  // Alinha o início ao domingo da primeira semana e o fim ao sábado da última.
  const start = new Date(first)
  start.setDate(first.getDate() - first.getDay())
  const end = new Date(last)
  end.setDate(last.getDate() + (6 - last.getDay()))

  const weeks: AttendanceCell[][] = []
  let col: AttendanceCell[] = []
  for (const d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
    const iso = toIso(d)
    const hit = byDate.has(iso)
    col.push({ date: hit ? iso : null, status: hit ? byDate.get(iso)! : null })
    if (d.getDay() === 6) {
      weeks.push(col)
      col = []
    }
  }
  if (col.length > 0) weeks.push(col)

  return weeks
}

function monthSpans(weeks: AttendanceCell[][]): AttendanceMonthSpan[] {
  const spans: AttendanceMonthSpan[] = []
  const byMonth = new Map<string, AttendanceMonthSpan>()

  weeks.forEach((week, col) => week.forEach((cell, row) => {
    if (!cell.date) return

    const key = cell.date.slice(0, 7)
    const span = byMonth.get(key)
    if (span) {
      span.endCol = col
      span.endRow = row
      return
    }

    const label = attendanceMonths[new Date(`${cell.date}T00:00:00`).getMonth()]!
    const created = { key, label, startCol: col, startRow: row, endCol: col, endRow: row }
    byMonth.set(key, created)
    spans.push(created)
  }))

  return spans
}

// Agrupa os dias por mês-calendário ('YYYY-MM'), em ordem cronológica.
const monthGroups = computed<AttendanceDay[][]>(() => {
  const map = new Map<string, AttendanceDay[]>()
  for (const d of days.value) {
    const key = d.date.slice(0, 7)
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(d)
  }
  return [...map.keys()].sort().map(k => map.get(k)!)
})

const breakpoints = useBreakpoints(breakpointsTailwind)
const isSm = breakpoints.greaterOrEqual('sm')

const gridArea = useTemplateRef<HTMLElement | null>('gridArea')
const { width: gridAreaWidth } = useElementSize(gridArea)

// Largura mínima do container pra cada densidade. Só divisores de 12, pra todas
// as linhas terem o mesmo número de meses.
const DENSITY_STEPS = [
  { months: 12, minWidth: 1200 },
  { months: 6, minWidth: 451 },
  { months: 4, minWidth: 316 },
  { months: 3, minWidth: 256 },
  { months: 2, minWidth: 200 }
]

const monthsPerRow = computed(() => {
  const step = DENSITY_STEPS.find(s => gridAreaWidth.value >= s.minWidth)
  return step?.months ?? 1
})

// Mobile: um mês por grid, com os eixos invertidos.
const monthGrids = computed<MonthGrid[]>(() => monthGroups.value.map(group => ({
  key: group[0]!.date,
  label: attendanceMonths[new Date(`${group[0]!.date}T00:00:00`).getMonth()]!,
  weeks: buildWeeks(group)
})))

const rows = computed<GridRow[]>(() => {
  const groups = monthGroups.value
  const result: GridRow[] = []
  for (let i = 0; i < groups.length; i += monthsPerRow.value) {
    const chunk = groups.slice(i, i + monthsPerRow.value).flat()
    const weeks = buildWeeks(chunk)
    result.push({ key: chunk[0]!.date, weeks, months: monthSpans(weeks) })
  }
  return result
})

const legend: { status: StudentDayAttendanceStatus, label: string }[] = [
  { status: 'NoClass', label: 'Sem aula' },
  { status: 'Undefined', label: 'Aguardando' },
  { status: 'Present', label: 'Presença' },
  { status: 'Absent', label: 'Falta' }
]
</script>

<template>
  <UDashboardPanel id="frequencies">
    <template #header>
      <UDashboardNavbar title="Frequência">
        <template #leading>
          <PageIcon />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="status === 'idle' || status === 'pending'" class="flex flex-1 items-center justify-center">
        <AppSpinner class="size-8" />
      </div>

      <UPageCard v-else variant="subtle">
        <div ref="gridArea" class="flex flex-col gap-6">
          <!-- Mobile: eixos invertidos — dias da semana no topo, meses na esquerda -->
          <div v-if="!isSm" class="flex flex-col gap-4">
            <div class="flex items-center gap-2">
              <div class="w-7 shrink-0" />
              <div class="flex min-w-0 flex-1 gap-[3px]">
                <div
                  v-for="(label, i) in attendanceWeekDays"
                  :key="i"
                  class="min-w-0 flex-1 text-center text-2xs leading-none text-muted"
                >
                  {{ label }}
                </div>
              </div>
            </div>

            <div
              v-for="month in monthGrids"
              :key="month.key"
              class="flex items-center gap-2"
            >
              <div class="w-7 shrink-0 text-2xs leading-none text-muted">
                {{ month.label }}
              </div>

              <div class="flex min-w-0 flex-1 flex-col gap-[3px]">
                <div
                  v-for="(week, wi) in month.weeks"
                  :key="wi"
                  class="flex gap-[3px]"
                >
                  <template
                    v-for="(cell, di) in week"
                    :key="di"
                  >
                    <FrequenciesDayCell
                      v-if="cell.date"
                      :date="cell.date"
                      :status="cell.status!"
                      class="min-w-0 flex-1"
                    />

                    <div v-else class="aspect-square min-w-0 flex-1" />
                  </template>
                </div>
              </div>
            </div>
          </div>

          <!-- Uma ou mais linhas de meses, conforme a largura disponível -->
          <div v-else-if="gridAreaWidth > 0" class="flex flex-col gap-6">
            <div
              v-for="row in rows"
              :key="row.key"
              class="flex items-stretch gap-2"
            >
              <!-- Coluna de rótulos dos dias da semana (eixo Y) -->
              <div class="flex shrink-0 flex-col gap-1">
                <div class="h-4" />
                <div class="flex flex-1 flex-col gap-[3px]">
                  <div
                    v-for="(label, i) in attendanceWeekDays"
                    :key="i"
                    class="flex flex-1 items-center justify-end text-2xs leading-none text-muted"
                  >
                    {{ label }}
                  </div>
                </div>
              </div>

              <FrequenciesMonthRow
                :weeks="row.weeks"
                :months="row.months"
                class="min-w-0 flex-1"
              />
            </div>
          </div>

          <!-- Legenda -->
          <div class="flex flex-wrap items-center gap-4 text-xs text-muted">
            <div
              v-for="item in legend"
              :key="item.status"
              class="flex items-center gap-1.5"
            >
              <div
                class="size-3 rounded-[2px] ring-1 ring-inset ring-default/40"
                :class="attendanceCellClass(item.status)"
              />
              <span>{{ item.label }}</span>
            </div>
          </div>
        </div>
      </UPageCard>
    </template>
  </UDashboardPanel>
</template>
