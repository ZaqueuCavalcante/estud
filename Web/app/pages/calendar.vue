<script setup lang="ts">
import type { CalendarItem, GetCalendarOut, GetPeriodsOut, HighlightItem, PeriodItem } from '~/types/calendar'

const { can } = usePolicy()
const config = useRuntimeConfig()

const year = ref(new Date().getFullYear())

// Sem campus na query, o endpoint devolve o calendário da instituição.
const { data, status, refresh } = await useFetch<GetCalendarOut>(`${config.public.backendUrl}/calendar`, {
  credentials: 'include',
  server: false,
  query: { year },
})

// Períodos e calendário são recursos separados, com permissões separadas
// (ManagePeriods x ManageCalendar): quem só administra o calendário não pode
// ler os períodos, então nem buscamos — e o card some.
const canGetAcademicPeriods = can('GetAcademicPeriods')
const canGetEnrollmentPeriods = can('GetEnrollmentPeriods')

// Os endpoints não filtram por ano: a lista inteira vem uma vez e o recorte do
// ano exibido acontece aqui.
const { data: academicPeriods, status: academicPeriodsStatus } = await useFetch<GetPeriodsOut>(
  `${config.public.backendUrl}/periods/academic`,
  { credentials: 'include', server: false, immediate: canGetAcademicPeriods.value },
)

const { data: enrollmentPeriods, status: enrollmentPeriodsStatus } = await useFetch<GetPeriodsOut>(
  `${config.public.backendUrl}/periods/enrollment`,
  { credentials: 'include', server: false, immediate: canGetEnrollmentPeriods.value },
)

type PeriodKind = 'academic' | 'enrollment'

// Um período entra no ano exibido quando cruza qualquer parte dele — período que
// começa em dezembro e termina em março aparece nos dois anos.
function periodsOfYear(periods: PeriodItem[] | null | undefined) {
  return (periods ?? []).filter(p => p.startAt.slice(0, 4) <= String(year.value) && p.endAt.slice(0, 4) >= String(year.value))
}

const yearAcademicPeriods = computed(() => periodsOfYear(academicPeriods.value?.items))
const yearEnrollmentPeriods = computed(() => periodsOfYear(enrollmentPeriods.value?.items))

function dayAndMonth(date: string) {
  const [, month, day] = date.slice(0, 10).split('-')
  return `${day}/${month}`
}

function periodItems(kind: PeriodKind, periods: PeriodItem[]): HighlightItem[] {
  return periods.map(period => ({
    key: `${kind}:${period.id}`,
    label: period.name,
    hint: `${dayAndMonth(period.startAt)} – ${dayAndMonth(period.endAt)}`,
  }))
}

const academicItems = computed(() => periodItems('academic', yearAcademicPeriods.value))
const enrollmentItems = computed(() => periodItems('enrollment', yearEnrollmentPeriods.value))

// Feriado nacional vem do calendário global; os demais são os que a instituição
// ou o campus cadastrou.
const holidays = computed(() => {
  const items = (data.value?.items ?? []).filter(item => item.dayType === 'Holiday')

  return {
    national: items.filter(item => item.source === 'Global').length,
    regional: items.filter(item => item.source !== 'Global').length,
  }
})

const holidayItems = computed<HighlightItem[]>(() => [
  { key: 'holidays:national', label: `${holidays.value.national} ${holidays.value.national === 1 ? 'Feriado Nacional' : 'Feriados Nacionais'}` },
  { key: 'holidays:regional', label: `${holidays.value.regional} ${holidays.value.regional === 1 ? 'Feriado Regional' : 'Feriados Regionais'}` },
])

// O clique fixa o destaque; o hover só prevalece enquanto o cursor está em cima
// de outro item, e ao sair volta pro que está fixado.
const pinnedKey = ref<string | null>(null)
const hoveredKey = ref<string | null>(null)

const activeKey = computed(() => hoveredKey.value ?? pinnedKey.value)

function toggleHighlight(key: string) {
  const wasPinned = pinnedKey.value === key
  pinnedKey.value = wasPinned ? null : key

  // Ao desselecionar, o cursor segue em cima do item e o hover manteria o
  // destaque; só volta a valer quando o cursor sair e entrar de novo.
  if (wasPinned) hoveredKey.value = null
}

const highlightedPeriod = computed(() => {
  const [kind, id] = activeKey.value?.split(':') ?? []

  if (kind === 'academic') return yearAcademicPeriods.value.find(p => p.id === Number(id)) ?? null
  if (kind === 'enrollment') return yearEnrollmentPeriods.value.find(p => p.id === Number(id)) ?? null

  return null
})

const highlightedHolidays = computed(() => {
  if (activeKey.value === 'holidays:national') return 'national'
  if (activeKey.value === 'holidays:regional') return 'regional'

  return null
})

const dayModalOpen = ref(false)
const selectedDay = ref<CalendarItem | null>(null)

function selectDay(item: CalendarItem) {
  selectedDay.value = item
  dayModalOpen.value = true
}

const monthNames = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
]

const weekDayNames = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S']

interface CalendarCell {
  day: number
  weekDay: number
  item: CalendarItem
}

interface CalendarMonth {
  name: string
  index: number
  offset: number
  cells: CalendarCell[]
}

function weekDayOf(date: string) {
  return new Date(`${date.slice(0, 10)}T00:00:00`).getDay()
}

const todayIso = new Date().toLocaleDateString('sv-SE') // yyyy-MM-dd no fuso local

function isToday(item: CalendarItem) {
  return item.date.slice(0, 10) === todayIso
}

// Sábado e domingo não são editáveis: não faz sentido mudar o tipo de um dia de
// fim de semana.
function isWeekend(cell: CalendarCell) {
  return cell.weekDay === 0 || cell.weekDay === 6
}

// Os itens vêm ordenados, um por dia do ano, então basta agrupá-los por mês na ordem em que chegam.
const months = computed<CalendarMonth[]>(() => {
  const items = data.value?.items ?? []

  return monthNames.map((name, index) => {
    const cells = items
      .filter(item => Number(item.date.slice(5, 7)) === index + 1)
      .map(item => ({
        day: Number(item.date.slice(8, 10)),
        weekDay: weekDayOf(item.date),
        item,
      }))

    return {
      name,
      index,
      offset: cells.length ? cells[0]!.weekDay : 0,
      cells,
    }
  })
})

const now = new Date()

// Só destaca o mês atual quando o ano exibido é o ano corrente.
function isCurrentMonth(month: CalendarMonth) {
  return year.value === now.getFullYear() && month.index === now.getMonth()
}

// Tanto a data do dia quanto as do período estão em yyyy-MM-dd, formato em que a
// ordem lexicográfica já é a cronológica.
function isInHighlightedPeriod(cell: CalendarCell) {
  const period = highlightedPeriod.value
  if (!period || isWeekend(cell)) return false

  const date = cell.item.date.slice(0, 10)
  return date >= period.startAt && date <= period.endAt
}

function isHighlightedHoliday(cell: CalendarCell) {
  if (!highlightedHolidays.value || cell.item.dayType !== 'Holiday') return false

  const isNational = cell.item.source === 'Global'
  return highlightedHolidays.value === 'national' ? isNational : !isNational
}

// A cor do texto sai de um ramo só: o destaque de feriado troca a cor do número,
// e o de período pinta o fundo — dois valores da mesma propriedade no `class`
// dependeriam da ordem em que o Tailwind gera as regras.
function cellClass(cell: CalendarCell) {
  return [
    'relative flex h-7 w-full items-center justify-center rounded text-xs tabular-nums',
    isToday(cell.item) && 'ring-2 ring-inset ring-primary font-semibold',
    isHighlightedHoliday(cell)
      ? 'text-error font-medium'
      : isInHighlightedPeriod(cell)
        ? 'bg-primary/15 text-primary font-medium'
        : 'text-default',
  ]
}
</script>

<template>
  <UDashboardPanel id="calendar">
    <template #header>
      <UDashboardNavbar title="Calendário">
        <template #leading>
          <PageIcon />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-6 pb-8">
        <div class="flex flex-col items-start gap-4 sm:flex-row sm:justify-between">
          <div class="flex items-center gap-1 sm:order-2">
            <UButton
              icon="i-lucide-chevron-left"
              color="neutral"
              variant="ghost"
              @click="() => { year-- }"
            />
            <span class="w-14 text-center text-sm font-medium tabular-nums">{{ year }}</span>
            <UButton
              icon="i-lucide-chevron-right"
              color="neutral"
              variant="ghost"
              @click="() => { year++ }"
            />
          </div>

          <div class="flex w-full flex-wrap items-start gap-4 sm:order-1 sm:w-auto">
            <CalendarHighlightCard
              v-if="canGetAcademicPeriods"
              title="Períodos letivos"
              empty-text="Nenhum período neste ano."
              :items="academicItems"
              :loading="academicPeriodsStatus === 'pending'"
              :selected-key="pinnedKey"
              @hover="(key) => { hoveredKey = key }"
              @select="toggleHighlight"
            />
            <CalendarHighlightCard
              v-if="canGetEnrollmentPeriods"
              title="Períodos de matrícula"
              empty-text="Nenhum período neste ano."
              :items="enrollmentItems"
              :loading="enrollmentPeriodsStatus === 'pending'"
              :selected-key="pinnedKey"
              @hover="(key) => { hoveredKey = key }"
              @select="toggleHighlight"
            />
            <CalendarHighlightCard
              title="Feriados"
              :items="holidayItems"
              :loading="status === 'pending'"
              :selected-key="pinnedKey"
              @hover="(key) => { hoveredKey = key }"
              @select="toggleHighlight"
            />
          </div>
        </div>

        <div v-if="status === 'pending'" class="flex justify-center py-16">
          <UIcon name="i-lucide-loader-circle" class="size-8 animate-spin text-muted" />
        </div>

        <div v-else class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-6">
          <div
            v-for="month in months"
            :key="month.name"
            class="rounded-lg border border-default p-3"
          >
            <p class="mb-2">
              <span
                class="inline-flex items-center rounded-full px-2.5 py-0.5 text-sm font-semibold"
                :class="isCurrentMonth(month) ? 'bg-primary text-inverted' : 'text-highlighted'"
              >
                {{ month.name }}
              </span>
            </p>

            <div class="grid grid-cols-7 gap-0.5 text-center">
              <span
                v-for="(weekDay, index) in weekDayNames"
                :key="index"
                class="py-1 text-xs text-dimmed"
              >
                {{ weekDay }}
              </span>

              <span v-for="blank in month.offset" :key="`blank-${blank}`" />

              <template v-for="cell in month.cells" :key="cell.day">
                <span
                  v-if="isWeekend(cell)"
                  :class="cellClass(cell)"
                >
                  {{ cell.day }}
                </span>
                <button
                  v-else
                  type="button"
                  :class="[cellClass(cell), 'cursor-pointer hover:bg-elevated']"
                  @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); selectDay(cell.item) }"
                >
                  {{ cell.day }}
                </button>
              </template>
            </div>
          </div>
        </div>
      </div>
    </template>
  </UDashboardPanel>

  <CalendarDayModal
    v-model:open="dayModalOpen"
    :day="selectedDay"
    @saved="refresh()"
  />
</template>
