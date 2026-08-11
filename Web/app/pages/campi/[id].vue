<script setup lang="ts">
import type { NavigationMenuItem, TableColumn } from '@nuxt/ui'
import type { CampusOccupancyCell, GetCampusOccupancyOut } from '~/types/campi'

interface CampusItem {
  id: number
  name: string
  state: string
  city: string
  students: number
  teachers: number
}
interface GetCampiOut {
  total: number
  items: CampusItem[]
}
interface ClassroomItem {
  id: number
  name: string
  campusId: number
  campus: string
  capacity: number
}

const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

const route = useRoute()
const config = useRuntimeConfig()
const campusId = Number(route.params.id)

// ── Dados reais: campus + suas salas ─────────────────────────────────────────
const { data: campiData, status: campiStatus, refresh: refreshCampi } = await useFetch<GetCampiOut>(`${config.public.backendUrl}/campi`, {
  credentials: 'include',
  server: false,
})

const { data: classroomsData, status: classroomsStatus, refresh: refreshClassrooms }
  = await useFetch<ClassroomItem[]>(`${config.public.backendUrl}/classrooms`, {
    credentials: 'include',
    server: false,
  })

const campus = computed(() => campiData.value?.items?.find(c => c.id === campusId) ?? null)
const classrooms = computed(() => (classroomsData.value ?? []).filter(c => c.campusId === campusId))

// Só a primeira carga tem direito ao spinner de página inteira: o refresh da
// aba Salas mantém os dados antigos em `data`, e apagar a tela toda por causa
// dele seria pior do que a espera.
const status = computed(() =>
  (campiStatus.value === 'pending' && campiData.value === null)
  || (classroomsStatus.value === 'pending' && classroomsData.value === null)
    ? 'pending'
    : 'success',
)

const createModalOpen = ref(false)
const editModalOpen = ref(false)
const campusEditModalOpen = ref(false)
const openingHoursModalOpen = ref(false)
const selectedClassroom = ref<ClassroomItem | null>(null)

function openEdit(classroom: ClassroomItem) {
  selectedClassroom.value = classroom
  editModalOpen.value = true
}

const classroomColumns: TableColumn<ClassroomItem>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
    cell: ({ row }) => h('span', { class: 'font-medium text-highlighted' }, row.original.name),
  },
  {
    accessorKey: 'capacity',
    header: 'Capacidade',
    cell: ({ row }) => row.original.capacity.toLocaleString('pt-BR'),
  },
  {
    id: 'actions',
    header: '',
    cell: ({ row }) => h('div', { class: 'flex justify-end gap-1' }, [
      h(UTooltip, { text: 'Editar' }, () => h(UButton, {
        icon: 'i-lucide-pencil',
        color: 'neutral',
        variant: 'ghost',
        size: 'sm',
        onClick: (e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); openEdit(row.original) },
      })),
      h(UTooltip, { text: 'Ver detalhes' }, () => h(UButton, {
        icon: 'i-lucide-arrow-right',
        color: 'neutral',
        variant: 'ghost',
        size: 'sm',
        to: `/classrooms/${row.original.id}`,
        'aria-label': 'Ver detalhes',
      })),
    ]),
  },
]

// ── Constantes compartilhadas (serão promovidas pra utils/classes.ts) ─────────
const weekDays = campusWeekDays
// A duração de cada janela vem da API (availableMinutes por célula); aqui ficam
// só os rótulos.
const shifts = [
  { key: 'Morning', label: 'Manhã', window: '07h–12h' },
  { key: 'Afternoon', label: 'Tarde', window: '12h–18h' },
  { key: 'Evening', label: 'Noite', window: '18h–22h' },
]
const shiftLabels: Record<string, string> = {
  Morning: 'Manhã',
  Afternoon: 'Tarde',
  Evening: 'Noite',
}
const dayLabels: Record<string, string> = {
  Monday: 'Segunda',
  Tuesday: 'Terça',
  Wednesday: 'Quarta',
  Thursday: 'Quinta',
  Friday: 'Sexta',
  Saturday: 'Sábado',
}
const dayShort: Record<string, string> = Object.fromEntries(weekDays.map(d => [d.key, d.short]))

function round2(n: number) {
  return Math.round(n * 100) / 100
}

// ── Ocupação: GET /campi/{id}/occupancy ───────────────────────────────────────
// Com `?mock=<arquivo>` na URL os dados saem de app/Mocks/ no lugar da API.
const {
  occupancy,
  loading: occupancyLoading,
  error: occupancyError,
  refresh: refreshOccupancy,
  mock,
} = useCampusOccupancy(campusId)

const emptyOccupancy: GetCampusOccupancyOut = {
  campusId,
  campus: '',
  totalClassrooms: 0,
  overallUsedMinutesRate: 0,
  openCells: 0,
  cells: [],
}

const data = computed<GetCampusOccupancyOut>(() => occupancy.value ?? emptyOccupancy)

// ── Lookup de célula por dia+turno ────────────────────────────────────────────
function cellFor(dayKey: string, shiftKey: string): CampusOccupancyCell | undefined {
  return data.value.cells.find(c => c.day === dayKey && c.shift === shiftKey)
}

function isClosed(dayKey: string, shiftKey: string) {
  const cell = cellFor(dayKey, shiftKey)
  return cell !== undefined && !cell.open
}

// ── Eixos do mapa: só o que o campus abre ─────────────────────────────────────
// Dia fechado a semana inteira vira coluna de seis células apagadas, e turno
// fechado vira uma faixa vazia — nos dois casos é ruído, não informação. Antes
// da resposta chegar não há o que filtrar, então tudo aparece.
const visibleDays = computed(() =>
  data.value.cells.length === 0
    ? weekDays
    : weekDays.filter(day => data.value.cells.some(c => c.day === day.key && c.open)),
)
const visibleShifts = computed(() =>
  data.value.cells.length === 0
    ? shifts
    : shifts.filter(shift => data.value.cells.some(c => c.shift === shift.key && c.open)),
)

const dayColumns = computed(() => `auto repeat(${visibleDays.value.length}, minmax(0, 1fr))`)
const shiftColumns = computed(() => `auto repeat(${visibleShifts.value.length}, minmax(0, 1fr))`)

// O campus não abre nos 18 turnos possíveis, e o mapa encolhido precisa dizer
// por quê — senão o gestor lê o vazio como bug.
const hasClosedCells = computed(() => data.value.cells.length > 0 && data.value.openCells < data.value.cells.length)

// ── Agregado por turno (vai no eixo esquerdo do grid) ─────────────────────────
const shiftRate = computed<Record<string, number>>(() => {
  const out: Record<string, number> = {}
  for (const shift of shifts) {
    const cells = data.value.cells.filter(c => c.shift === shift.key)
    const used = cells.reduce((s, c) => s + c.usedMinutes, 0)
    const available = cells.reduce((s, c) => s + c.availableMinutes, 0)
    out[shift.key] = available > 0 ? round2((used / available) * 100) : 0
  }
  return out
})

// ── Insights derivados (stats) ────────────────────────────────────────────────
const peakCell = computed<CampusOccupancyCell | null>(() =>
  data.value.cells.filter(c => c.open)
    .reduce<CampusOccupancyCell | null>((a, b) => (a === null || b.usedMinutesRate > a.usedMinutesRate ? b : a), null),
)
// Turno fechado não é turno livre: é horário que o campus não tem.
const freeSlots = computed(() => data.value.cells.filter(c => c.open && c.usedMinutesRate === 0).length)
const totalSlots = computed(() => data.value.openCells)

// Sem sala cadastrada não há o que plotar. O mapa continua na tela, todo
// zerado, com um aviso explicando o que falta pra ele ganhar dados — some um
// grid vazio é menos informativo do que o grid mostrando "nada alocado ainda".
const hasOccupancyData = computed(() => data.value.overallUsedMinutesRate > 0)

// ── Formatação ────────────────────────────────────────────────────────────────
function formatMinutes(minutes: number): string {
  if (minutes <= 0) return '0min'
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (h === 0) return `${m}min`
  if (m === 0) return `${h}h`
  return `${h}h ${m}min`
}
function formatRate(rate: number): string {
  return `${rate.toFixed(0)}%`
}

// Rampa sequencial de intensidade do primary — ocupação baixa não é "ruim",
// então nada de vermelho/verde: só uma escala de saturação.
function cellClass(rate: number): string {
  if (rate === 0) return 'bg-elevated text-dimmed ring-1 ring-inset ring-default/60'
  if (rate < 20) return 'bg-primary/10 text-highlighted'
  if (rate < 40) return 'bg-primary/25 text-highlighted'
  if (rate < 60) return 'bg-primary/40 text-highlighted'
  if (rate < 80) return 'bg-primary/60 text-inverted'
  return 'bg-primary/85 text-inverted'
}

// ── Drilldown selecionado ─────────────────────────────────────────────────────
const selected = ref<{ day: string, shift: string }>({ day: 'Monday', shift: 'Morning' })

function selectCell(day: string, shift: string) {
  if (isClosed(day, shift)) return
  selected.value = { day, shift }
}
function isSelected(day: string, shift: string) {
  return selected.value.day === day && selected.value.shift === shift
}

// A seleção inicial é segunda de manhã, que num campus só noturno está fechada.
// Assim que os dados chegam, cai na primeira célula que o campus realmente abre.
watch(() => data.value.cells, (cells) => {
  if (!cells.length || cellFor(selected.value.day, selected.value.shift)?.open) return

  const firstOpen = cells.find(c => c.open)
  if (firstOpen) selected.value = { day: firstOpen.day, shift: firstOpen.shift }
}, { immediate: true })

const selectedCell = computed(() => cellFor(selected.value.day, selected.value.shift))
const selectedClassrooms = computed(() =>
  [...(selectedCell.value?.classrooms ?? [])].sort((a, b) => b.usedMinutesRate - a.usedMinutesRate),
)

// ── Horários: GET /campi/{id}/opening-hours ───────────────────────────────────
const {
  openingHours,
  loading: openingHoursLoading,
  error: openingHoursError,
  refresh: refreshOpeningHours,
} = useCampusOpeningHours(campusId)

const openingHoursDays = computed(() => openingHours.value?.days ?? [])

// Editar os horários muda o denominador do mapa, então as duas abas
// precisam voltar juntas do backend.
async function onOpeningHoursSaved() {
  await Promise.all([refreshOpeningHours(), refreshOccupancy()])
}

// ── Abas ──────────────────────────────────────────────────────────────────────
// Cada aba busca seus dados de novo ao ser selecionada: o mapa de ocupação e as
// salas mudam por fora desta tela (turmas alocadas, salas criadas em outro
// campus), então voltar pra uma aba nunca pode mostrar número velho.
const activeTab = ref('occupancy')

const tabRefreshers: Record<string, () => Promise<unknown>> = {
  'occupancy': () => refreshOccupancy(),
  'opening-hours': () => refreshOpeningHours(),
  'rooms': () => refreshClassrooms(),
}

function selectTab(tab: string) {
  activeTab.value = tab
  tabRefreshers[tab]?.()
}

const tabs = computed(() => [[
  { label: 'Ocupação', icon: 'i-lucide-layout-grid', active: activeTab.value === 'occupancy', onSelect: () => { selectTab('occupancy') } },
  { label: 'Horários', icon: 'i-lucide-clock', active: activeTab.value === 'opening-hours', onSelect: () => { selectTab('opening-hours') } },
  { label: 'Salas', icon: 'i-lucide-door-open', active: activeTab.value === 'rooms', onSelect: () => { selectTab('rooms') } },
]] satisfies NavigationMenuItem[][])

const breadcrumb = [
  { label: 'Campi', to: '/campi', icon: 'i-lucide-map-pin' },
  { label: 'Detalhes' },
]

// Degraus da legenda — um por faixa da rampa do cellClass
const legendSteps = [0, 10, 30, 50, 70, 90]
</script>

<template>
  <UDashboardPanel id="campus-details">
    <template #header>
      <UDashboardNavbar>
        <template #title>
          <UBreadcrumb :items="breadcrumb" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="status === 'pending'" class="flex justify-center py-12">
        <UIcon name="i-lucide-loader-circle" class="size-8 animate-spin text-muted" />
      </div>

      <div v-else-if="!campus" class="flex flex-col items-center gap-4 py-12">
        <UIcon name="i-lucide-triangle-alert" class="size-16 text-muted" />
        <p class="text-muted text-sm">
          Campus não encontrado
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" to="/campi" />
      </div>

      <div v-else class="flex flex-col gap-6 py-2">
        <!-- Cabeçalho -->
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div class="flex flex-col gap-1">
            <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
              {{ campus.name }}
            </h1>
            <p class="flex items-center gap-1.5 text-sm text-muted">
              <UIcon name="i-lucide-map-pin" class="size-4 shrink-0" />
              {{ campus.city }} · {{ campus.state }}
            </p>
          </div>
          <UButton
            icon="i-lucide-pencil"
            label="Editar"
            color="neutral"
            variant="subtle"
            class="shrink-0"
            @click="() => { campusEditModalOpen = true }"
          />
        </div>

        <UNavigationMenu :items="tabs" highlight class="-mx-1" />

        <!-- Ocupação -->
        <div v-if="activeTab === 'occupancy' && occupancyLoading" class="flex justify-center py-12">
          <UIcon name="i-lucide-loader-circle" class="size-8 animate-spin text-muted" />
        </div>

        <div v-else-if="activeTab === 'occupancy' && occupancyError" class="flex flex-col items-center gap-4 py-12">
          <UIcon name="i-lucide-triangle-alert" class="size-16 text-muted" />
          <p class="text-sm text-muted">
            Não foi possível carregar a ocupação do campus
          </p>
          <UButton
            icon="i-lucide-refresh-cw"
            label="Tentar novamente"
            color="neutral"
            variant="subtle"
            @click="() => { refreshOccupancy() }"
          />
        </div>

        <template v-else-if="activeTab === 'occupancy'">
          <!-- Mock ativo: sinaliza que a tela não está falando com a API -->
          <div
            v-if="mock"
            class="flex items-center gap-2 rounded-xl border border-warning/25 bg-warning/[0.06] px-4 py-2.5 text-sm"
          >
            <UIcon name="i-lucide-flask-conical" class="size-4 shrink-0 text-warning" />
            <span class="text-muted">
              Exibindo o mock <span class="font-medium text-highlighted">{{ mock }}</span> em vez dos dados da API.
            </span>
          </div>

          <!-- Nada alocado ainda: o mapa fica zerado, e o aviso explica o
               caminho pra ele ganhar dados. -->
          <div
            v-if="!hasOccupancyData"
            class="flex items-start gap-3 rounded-xl border border-info/25 bg-info/[0.06] p-4"
          >
            <UIcon name="i-lucide-info" class="size-5 shrink-0 text-info" />
            <div class="flex flex-col gap-1">
              <p class="text-sm font-semibold text-highlighted">
                Ainda não há ocupação para exibir
              </p>
              <p class="text-sm text-muted">
                O mapa é montado a partir dos horários das turmas alocadas nas salas deste campus.
                <template v-if="!data.totalClassrooms">
                  Cadastre as salas na aba <button
                    type="button"
                    class="font-medium text-primary underline underline-offset-2 hover:text-primary/75 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                    @click="() => { selectTab('rooms') }"
                  >Salas</button> e defina
                </template>
                <template v-else>
                  Cadastre
                </template>
                turmas com horários para o mapa começar a ganhar dados.
              </p>
            </div>
          </div>

          <!-- Stats: insights, não totais crus -->
          <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
            <div class="flex items-center gap-4 rounded-xl border border-primary/25 bg-primary/[0.04] px-4 py-4">
              <div class="flex flex-col leading-none">
                <span class="text-4xl font-bold tabular-nums tracking-tight text-primary">{{ formatRate(data.overallUsedMinutesRate) }}</span>
                <span class="mt-2 text-xs font-medium text-muted">Ocupação geral</span>
              </div>
            </div>

            <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
              <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">{{ data.totalClassrooms }}</span>
              <span class="mt-2 text-xs text-muted">Salas no campus</span>
            </div>

            <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
              <span class="text-sm font-semibold" :class="hasOccupancyData && peakCell ? 'text-highlighted' : 'text-dimmed'">
                {{ hasOccupancyData && peakCell ? `${dayShort[peakCell.day]} · ${shiftLabels[peakCell.shift]}` : 'Sem alocação' }}
              </span>
              <span
                class="mt-0.5 text-xl font-bold tabular-nums leading-none"
                :class="hasOccupancyData && peakCell ? 'text-primary' : 'text-dimmed'"
              >{{ formatRate(peakCell?.usedMinutesRate ?? 0) }}</span>
              <span class="mt-2 text-xs text-muted">Horário de pico</span>
            </div>

            <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
              <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">
                {{ freeSlots }}<span class="text-base font-medium text-muted"> / {{ totalSlots }}</span>
              </span>
              <span class="mt-2 text-xs text-muted">Turnos livres</span>
            </div>
          </div>

          <!-- Mapa de calor: dia × turno -->
          <section class="flex flex-col gap-4">
            <div class="flex flex-wrap items-center justify-between gap-3">
              <h2 class="font-semibold text-highlighted">
                Mapa de ocupação
              </h2>
              <!-- Legenda: rampa de intensidade. No mobile ela sai — o mapa já
                   mostra o número em cada célula. -->
              <div class="ml-auto hidden items-center gap-2 text-xs text-muted md:flex">
                <span>0%</span>
                <div class="flex items-center gap-1">
                  <div
                    v-for="step in legendSteps"
                    :key="step"
                    class="size-4 rounded-sm"
                    :class="cellClass(step)"
                  />
                </div>
                <span>100%</span>
              </div>
            </div>

            <!-- Mobile: o mapa é transposto — dias na vertical, turnos na
                 horizontal. São 3 colunas em vez de 6, então cabe na largura da
                 tela sem virar scroll horizontal. -->
            <div class="p-1 -m-1 md:hidden">
              <div class="grid gap-2" :style="{ gridTemplateColumns: shiftColumns }">
                <!-- Header: turnos + agregado -->
                <div />
                <div
                  v-for="shift in visibleShifts"
                  :key="shift.key"
                  class="flex flex-col items-center pb-1 leading-tight"
                >
                  <span class="text-sm font-semibold text-highlighted">{{ shift.label }}</span>
                  <span class="text-[11px] text-muted tabular-nums">{{ shift.window }}</span>
                  <span class="mt-0.5 text-xs font-semibold tabular-nums text-primary">{{ formatRate(shiftRate[shift.key] ?? 0) }}</span>
                </div>

                <!-- Linhas por dia -->
                <template v-for="day in visibleDays" :key="day.key">
                  <div class="flex items-center justify-end pr-1">
                    <span class="text-xs font-semibold text-highlighted">{{ day.short }}</span>
                  </div>

                  <template v-for="shift in visibleShifts" :key="`${day.key}-${shift.key}`">
                    <!-- Célula fechada: o campus não abre nesse dia e turno. Não é
                         0% de ocupação — é horário que não existe, então não tem
                         número nem drilldown. -->
                    <UTooltip v-if="isClosed(day.key, shift.key)" text="Fechado">
                      <div class="flex h-14 w-full items-center justify-center rounded-lg bg-elevated/40 text-dimmed ring-1 ring-inset ring-default/40 [background-image:repeating-linear-gradient(45deg,transparent,transparent_5px,var(--ui-border)_5px,var(--ui-border)_6px)]">
                        <UIcon name="i-lucide-minus" class="size-4" />
                      </div>
                    </UTooltip>

                    <button
                      v-else
                      type="button"
                      class="flex h-14 items-center justify-center rounded-lg text-sm font-semibold tabular-nums transition-shadow hover:ring-2 hover:ring-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                      :class="[
                        cellClass(cellFor(day.key, shift.key)?.usedMinutesRate ?? 0),
                        isSelected(day.key, shift.key) ? 'ring-2 ring-primary' : '',
                      ]"
                      @click="() => { selectCell(day.key, shift.key) }"
                    >
                      {{ formatRate(cellFor(day.key, shift.key)?.usedMinutesRate ?? 0) }}
                    </button>
                  </template>
                </template>
              </div>
            </div>

            <!-- p-1/-m-1: o ring de hover/seleção é desenhado FORA da caixa da célula,
                 então o container de scroll precisa de folga pra não recortá-lo nas bordas. -->
            <div class="hidden overflow-x-auto p-1 -m-1 md:block">
              <div class="grid min-w-160 gap-2" :style="{ gridTemplateColumns: dayColumns }">
                <!-- Header: dias da semana -->
                <div />
                <div
                  v-for="day in visibleDays"
                  :key="day.key"
                  class="pb-1 text-center"
                >
                  <span class="text-sm font-semibold text-highlighted">{{ day.label }}</span>
                </div>

                <!-- Linhas por turno -->
                <template v-for="shift in visibleShifts" :key="shift.key">
                  <!-- Eixo esquerdo: rótulo do turno + agregado (dupla função) -->
                  <div class="flex flex-col justify-center pr-3 text-right">
                    <span class="text-sm font-medium text-highlighted">{{ shift.label }}</span>
                    <span class="text-[11px] text-muted tabular-nums">{{ shift.window }}</span>
                    <span class="mt-0.5 text-xs font-semibold tabular-nums text-primary">{{ formatRate(shiftRate[shift.key] ?? 0) }}</span>
                  </div>

                  <!-- Célula do mapa de calor -->
                  <template v-for="day in visibleDays" :key="`${day.key}-${shift.key}`">
                    <UTooltip v-if="isClosed(day.key, shift.key)" text="Fechado">
                      <div class="flex h-16 w-full items-center justify-center rounded-lg bg-elevated/40 text-dimmed ring-1 ring-inset ring-default/40 [background-image:repeating-linear-gradient(45deg,transparent,transparent_5px,var(--ui-border)_5px,var(--ui-border)_6px)]">
                        <UIcon name="i-lucide-minus" class="size-4" />
                      </div>
                    </UTooltip>

                    <button
                      v-else
                      type="button"
                      class="flex h-16 items-center justify-center rounded-lg text-sm font-semibold tabular-nums transition-shadow hover:ring-2 hover:ring-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                      :class="[
                        cellClass(cellFor(day.key, shift.key)?.usedMinutesRate ?? 0),
                        isSelected(day.key, shift.key) ? 'ring-2 ring-primary' : '',
                      ]"
                      @click="() => { selectCell(day.key, shift.key) }"
                    >
                      {{ formatRate(cellFor(day.key, shift.key)?.usedMinutesRate ?? 0) }}
                    </button>
                  </template>
                </template>
              </div>
            </div>
          </section>

          <!-- Drilldown inline (micro) — sem sala não há o que detalhar -->
          <section v-if="selectedCell?.open && selectedCell.classrooms.length" class="flex flex-col gap-4 rounded-xl border border-default bg-elevated/30 p-5">
            <div class="flex items-center justify-between gap-3">
              <div class="flex flex-col gap-0.5">
                <h2 class="font-semibold text-highlighted">
                  {{ dayLabels[selectedCell.day] }} · {{ shiftLabels[selectedCell.shift] }}
                </h2>
                <span class="text-sm text-muted">
                  {{ formatMinutes(selectedCell.usedMinutes) }} reservados de {{ formatMinutes(selectedCell.availableMinutes) }}
                </span>
              </div>
              <span class="text-2xl font-bold tabular-nums text-primary">{{ formatRate(selectedCell.usedMinutesRate) }}</span>
            </div>

            <!-- Uma sala por card, com as duas leituras do turno: quanto do
                 horário aberto a sala passa reservada (o mostrador) e quanto
                 dos assentos as turmas ocupam (os dez quadrados). Sala cheia de
                 horário e vazia de gente é o caso que só aparece com as duas. -->
            <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              <div
                v-for="room in selectedClassrooms"
                :key="room.id"
                class="flex flex-col gap-4 rounded-lg border border-default bg-default/60 p-4"
              >
                <span class="truncate text-sm font-medium text-highlighted">{{ room.name }}</span>

                <ClassroomsUsedMinutesRingStat
                  :percent="room.usedMinutesRate"
                  :title="`${formatRate(room.usedMinutesRate)} do horário`"
                  :subtitle="`${formatMinutes(room.usedMinutes)} de ${formatMinutes(room.availableMinutes)}`"
                  :color-class="room.usedMinutesRate > 0 ? 'text-primary' : 'text-dimmed'"
                />

                <div class="flex flex-col gap-2">
                  <ClassroomsUsedCapacityBlocks :percent="room.usedCapacityRate" />

                  <span class="text-xs text-muted tabular-nums">
                    {{ formatRate(room.usedCapacityRate) }} dos assentos
                  </span>
                </div>
              </div>
            </div>
          </section>
        </template>

        <!-- Horários: a configuração de que o mapa de ocupação vive -->
        <div v-else-if="activeTab === 'opening-hours' && openingHoursLoading" class="flex justify-center py-12">
          <UIcon name="i-lucide-loader-circle" class="size-8 animate-spin text-muted" />
        </div>

        <div v-else-if="activeTab === 'opening-hours' && openingHoursError" class="flex flex-col items-center gap-4 py-12">
          <UIcon name="i-lucide-triangle-alert" class="size-16 text-muted" />
          <p class="text-sm text-muted">
            Não foi possível carregar os horários de funcionamento
          </p>
          <UButton
            icon="i-lucide-refresh-cw"
            label="Tentar novamente"
            color="neutral"
            variant="subtle"
            @click="() => { refreshOpeningHours() }"
          />
        </div>

        <section v-else-if="activeTab === 'opening-hours'" class="flex flex-col gap-4">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <p class="text-sm text-muted">
              Os dias e horários em que este campus abre. É o que define o tamanho do mapa de ocupação.
            </p>
            <UButton
              icon="i-lucide-pencil"
              label="Editar"
              color="neutral"
              variant="subtle"
              class="shrink-0"
              @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); openingHoursModalOpen = true }"
            />
          </div>

          <CampiOpeningHoursWeek :days="openingHoursDays" />
        </section>

        <!-- Salas do campus: uma sala sempre vive dentro de um campus -->
        <section v-else-if="activeTab === 'rooms'" class="flex flex-col gap-4">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <p class="text-sm text-muted">
              As salas de aula deste campus.
            </p>
            <UButton
              v-if="classrooms.length"
              icon="i-lucide-plus"
              label="Sala"
              class="shrink-0"
              @click="() => { createModalOpen = true }"
            />
          </div>

          <DataTable :data="classrooms" :columns="classroomColumns">
            <template #empty>
              <TableEmptyState
                :loading="false"
                icon="i-lucide-door-open"
                message="Nenhuma sala neste campus"
                button-label="Sala"
                @create="() => { createModalOpen = true }"
              />
            </template>
          </DataTable>

          <div v-if="classrooms.length" class="flex items-center gap-2">
            <UBadge color="neutral" variant="subtle" class="h-8 px-3">
              {{ classrooms.length }} {{ classrooms.length === 1 ? 'sala encontrada' : 'salas encontradas' }}
            </UBadge>
          </div>
        </section>
      </div>
    </template>
  </UDashboardPanel>

  <CampiEditModal v-model:open="campusEditModalOpen" :campus="campus" @updated="refreshCampi()" />
  <CampiOpeningHoursModal
    v-model:open="openingHoursModalOpen"
    :campus-id="campusId"
    :days="openingHoursDays"
    @saved="onOpeningHoursSaved()"
  />

  <ClassroomsCreateModal v-model:open="createModalOpen" :campus-id="campusId" @created="refreshClassrooms()" />
  <ClassroomsEditModal v-model:open="editModalOpen" :classroom="selectedClassroom" @updated="refreshClassrooms()" />
</template>
