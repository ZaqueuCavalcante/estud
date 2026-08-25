<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import type { CampusOccupancyCell, GetCampusOccupancyOut, OpeningHoursDay } from '~/types/campi'

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

const route = useRoute()
const config = useRuntimeConfig()
const toast = useToast()
const campusId = Number(route.params.id)

// ── Dados reais: campus + suas salas
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
const campusEditModalOpen = ref(false)
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
  overallUsedCapacityRate: 0,
  openCells: 0,
  cells: [],
  classrooms: [],
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

// ── Insights derivados (stats) ────────────────────────────────────────────────
// O pico é o turno mais movimentado: mais gente por mais tempo. Isso é o
// assento-minuto (alunos x minutos), e não a taxa de horário — que só diz
// quantas salas estão reservadas e conta igual a turma de 3 e a de 200.
const peakCell = computed<CampusOccupancyCell | null>(() =>
  data.value.cells.filter(c => c.open)
    .reduce<CampusOccupancyCell | null>((a, b) => (a === null || b.usedCapacity > a.usedCapacity ? b : a), null),
)

// Quantos alunos, em média, estão no campus ao longo do turno de pico: o
// assento-minuto diluído na janela aberta. Vale mais que a porcentagem aqui —
// o card ao lado já mostra taxa, e "quanta gente" é uma pergunta diferente.
const peakStudents = computed(() => {
  const cell = peakCell.value
  if (!cell || cell.openMinutes <= 0) return 0
  return Math.round(cell.usedCapacity / cell.openMinutes)
})
// Sem sala cadastrada não há o que plotar. O mapa continua na tela, todo
// zerado, com um aviso explicando o que falta pra ele ganhar dados — some um
// grid vazio é menos informativo do que o grid mostrando "nada alocado ainda".
const hasOccupancyData = computed(() => data.value.overallUsedMinutesRate > 0)

// ── Aba Salas: a sala do cadastro + a ocupação da semana ──────────────────────
// A lista continua vindo de /classrooms (é ela que o create/edit atualiza) e a
// ocupação entra por id. Sala sem par fica com `occupancy: null` — é o que
// acontece enquanto o mapa ainda carrega, ou se ele falhar.
const classroomCards = computed(() => {
  const week = new Map(data.value.classrooms.map(c => [c.id, c]))
  return classrooms.value.map(room => ({ room, occupancy: week.get(room.id) ?? null }))
})

// ── Formatação ────────────────────────────────────────────────────────────────
function formatMinutes(minutes: number): string {
  if (minutes <= 0) return '0min'
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (h === 0) return `${m}min`
  if (m === 0) return `${h}h`
  return `${h}h ${m}min`
}

// A média de assentos ocupados é fracionária por natureza (sala usada meio
// período dá 0,5), mas "1,5 aluno" não é gente que exista — o card arredonda.
// Fração abaixo de 1 sobe pra 1: sala com aula não pode exibir "0 alunos".
function formatStudents(students: number): string {
  const rounded = students > 0 ? Math.max(Math.round(students), 1) : 0
  return `${rounded.toLocaleString('pt-BR')} ${rounded === 1 ? 'aluno' : 'alunos'} em média`
}

// Turno sem nenhuma reserva não merece a cor da marca: o mostrador vazio fica
// apagado, do mesmo jeito que nos cards de sala do drilldown.
function cellRingClass(day: string, shift: string): string {
  return (cellFor(day, shift)?.usedMinutesRate ?? 0) > 0 ? 'text-primary' : 'text-dimmed'
}

// A célula mostra as duas taxas só em ícone — não cabe texto no espaço dela.
// Então quem lê por leitor de tela depende deste rótulo pra ter os números.
function cellLabel(day: string, shift: string): string {
  const cell = cellFor(day, shift)
  const label = `${dayLabels[day]} ${shiftLabels[shift]}`
  return `${label}: ${formatRate(cell?.usedMinutesRate ?? 0)} de tempo usado, ${formatRate(cell?.usedCapacityRate ?? 0)} de espaço alocado`
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
// A API já devolve as salas em ordem alfabética — reordenar por ocupação aqui
// faria a lista pular de lugar a cada célula selecionada.
const selectedClassrooms = computed(() => selectedCell.value?.classrooms ?? [])

// ── Horários: GET /campi/{id}/opening-hours ───────────────────────────────────
const {
  openingHours,
  loading: openingHoursLoading,
  error: openingHoursError,
  refresh: refreshOpeningHours,
} = useCampusOpeningHours(campusId)

const openingHoursDays = computed(() => openingHours.value?.days ?? [])

// A semana em edição é um rascunho: o grid só volta a ler a API depois do salvar,
// senão um refresh de aba jogaria fora o que está sendo arrastado.
const editingHours = ref(false)
const savingHours = ref(false)
const draftHours = ref<OpeningHoursDay[]>([])

const shownHours = computed(() => editingHours.value ? draftHours.value : openingHoursDays.value)

const hoursDirty = computed(() =>
  editingHours.value
  && JSON.stringify(draftHours.value) !== JSON.stringify(normalizeHours(openingHoursDays.value)),
)

// O grid devolve os seis dias na ordem da semana, com no máximo uma janela por
// turno. Comparar com a resposta crua da API daria sujo por ordem e por dia
// omitido, então o lado de lá passa pela mesma normalização.
function normalizeHours(days: OpeningHoursDay[]): OpeningHoursDay[] {
  return campusWeekDays.map(day => ({
    day: day.key,
    windows: [...(days.find(d => d.day === day.key)?.windows ?? [])]
      .sort((a, b) => openingHourToMinutes(a.start) - openingHourToMinutes(b.start)),
  }))
}

function startEditingHours() {
  draftHours.value = normalizeHours(openingHoursDays.value)
  editingHours.value = true
}

function cancelEditingHours() {
  editingHours.value = false
  draftHours.value = []
}

async function saveOpeningHours() {
  savingHours.value = true
  try {
    await $fetch(`${config.public.backendUrl}/campi/${campusId}/opening-hours`, {
      method: 'PUT',
      body: { days: draftHours.value },
      credentials: 'include',
    })
    toast.add({ title: 'Horários de funcionamento atualizados', color: 'success' })
    editingHours.value = false
    draftHours.value = []
    await onOpeningHoursSaved()
  }
  catch (err: unknown) {
    const message = (err as { data?: { message?: string } })?.data?.message
      ?? 'Erro ao atualizar os horários de funcionamento.'
    toast.add({ title: 'Erro', description: message, color: 'error' })
  }
  finally {
    savingHours.value = false
  }
}

const closedAllWeek = computed(() =>
  shownHours.value.length > 0 && shownHours.value.every(day => day.windows.length === 0),
)

// Criar ou editar uma sala muda o mapa (mais uma sala no denominador, outra
// capacidade no cálculo de assentos) e os cards da aba Salas.
async function onClassroomsChanged() {
  await Promise.all([refreshClassrooms(), refreshOccupancy()])
}

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
        <!-- Cabeçalho: as abas sobem pra linha do nome do campus onde há
             largura pra isso. No mobile elas voltam pra baixo das infos, senão
             o nome do campus fica espremido num canto. -->
        <div class="flex flex-col gap-6 md:flex-row md:items-center md:justify-between">
          <div class="flex flex-col gap-1">
            <div class="flex items-center gap-1.5">
              <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
                {{ campus.name }}
              </h1>
              <UTooltip text="Editar">
                <UButton
                  icon="i-lucide-pencil"
                  color="neutral"
                  variant="ghost"
                  size="xs"
                  @click="(e) => { (e.currentTarget as HTMLElement).blur(); campusEditModalOpen = true }"
                />
              </UTooltip>
            </div>
            <p class="flex items-center gap-1.5 text-sm text-muted">
              <UIcon name="i-lucide-map-pin" class="size-4 shrink-0" />
              {{ campus.city }} · {{ campus.state }}
            </p>
          </div>

          <UNavigationMenu :items="tabs" highlight class="-mx-1 md:mx-0 md:shrink-0" />
        </div>

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
                  Cadastre as <button
                    type="button"
                    class="font-medium text-primary underline underline-offset-2 hover:text-primary/75 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                    @click="() => { selectTab('rooms') }"
                  >salas</button> e defina
                </template>
                <template v-else>
                  Cadastre
                </template>
                <NuxtLink
                  to="/classes"
                  class="font-medium text-primary underline underline-offset-2 hover:text-primary/75 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                >turmas</NuxtLink>
                com horários para o mapa começar a ganhar dados.
              </p>
            </div>
          </div>

          <!-- Stats: insights, não totais crus. As duas taxas de ocupação vêm
               primeiro e lado a lado, com os mesmos ícones das células do mapa:
               é a comparação entre elas que conta a história do campus — horário
               cheio com assento vazio é sala grande demais pra turma. -->
          <div class="mt-4 grid grid-cols-2 gap-3 lg:grid-cols-4">
            <div class="flex items-center gap-4 rounded-xl border border-primary/25 bg-primary/[0.04] px-4 py-4">
              <ClassroomsUsedMinutesRing :percent="data.overallUsedMinutesRate" class="size-12 text-primary" />
              <div class="flex flex-col leading-none">
                <span class="text-3xl font-bold tabular-nums tracking-tight text-primary">{{ formatRate(data.overallUsedMinutesRate) }}</span>
                <span class="mt-2 text-xs font-medium text-muted">Tempo usado</span>
              </div>
            </div>

            <div class="flex items-center gap-4 rounded-xl border border-primary/25 bg-primary/[0.04] px-4 py-4">
              <ClassroomsUsedCapacityBlocks :percent="data.overallUsedCapacityRate" class="size-12" />
              <div class="flex flex-col leading-none">
                <span class="text-3xl font-bold tabular-nums tracking-tight text-primary">{{ formatRate(data.overallUsedCapacityRate) }}</span>
                <span class="mt-2 text-xs font-medium text-muted">Espaço alocado</span>
              </div>
            </div>

            <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
              <span class="text-sm font-semibold" :class="hasOccupancyData && peakStudents ? 'text-highlighted' : 'text-dimmed'">
                {{ hasOccupancyData && peakCell ? `${dayShort[peakCell.day]} · ${shiftLabels[peakCell.shift]}` : 'Sem alocação' }}
              </span>
              <span
                class="mt-0.5 text-xl font-bold tabular-nums leading-none"
                :class="hasOccupancyData && peakStudents ? 'text-primary' : 'text-dimmed'"
              >~{{ peakStudents.toLocaleString('pt-BR') }} <span class="text-base font-medium text-muted">{{ peakStudents === 1 ? 'aluno' : 'alunos' }}</span></span>
              <span class="mt-2 text-xs text-muted">Horário de pico</span>
            </div>

            <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
              <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">{{ data.totalClassrooms }}</span>
              <span class="mt-2 text-xs text-muted">Salas no campus</span>
            </div>
          </div>

          <!-- Mapa de calor: dia × turno -->
          <section class="mt-4 flex flex-col gap-4">
            <div class="flex flex-wrap items-center justify-between gap-3">
              <h2 class="flex items-center gap-2 font-semibold text-highlighted">
                <UIcon name="i-lucide-table-2" class="size-5 text-primary" />
                Mapa de ocupação
              </h2>

              <!-- Chave dos dois ícones da célula: sem ela, a célula é só dois
                   desenhos sem nome — não sobra espaço lá dentro pra rótulo. -->
              <div class="ml-auto hidden items-center gap-4 text-xs text-muted sm:flex">
                <span class="flex items-center gap-2">
                  <ClassroomsUsedMinutesRing :percent="75" class="size-5 text-primary" />
                  tempo
                </span>
                <span class="flex items-center gap-2">
                  <ClassroomsUsedCapacityBlocks :percent="75" class="size-5" />
                  espaço
                </span>
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
                      class="flex h-14 items-center justify-center gap-5 rounded-lg border border-default bg-elevated/40 transition-shadow hover:ring-2 hover:ring-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                      :class="isSelected(day.key, shift.key) ? 'ring-2 ring-primary' : ''"
                      :aria-label="cellLabel(day.key, shift.key)"
                      @click="() => { selectCell(day.key, shift.key) }"
                    >
                      <ClassroomsUsedMinutesRing :percent="cellFor(day.key, shift.key)?.usedMinutesRate ?? 0" class="size-7" :class="cellRingClass(day.key, shift.key)" />
                      <ClassroomsUsedCapacityBlocks :percent="cellFor(day.key, shift.key)?.usedCapacityRate ?? 0" class="size-7" />
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
                  <!-- Eixo esquerdo: rótulo do turno e a janela dele -->
                  <div class="flex flex-col justify-center pr-3 text-right">
                    <span class="text-sm font-medium text-highlighted">{{ shift.label }}</span>
                    <span class="text-[11px] text-muted tabular-nums">{{ shift.window }}</span>
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
                      class="flex h-16 items-center justify-center gap-6 rounded-lg border border-default bg-elevated/40 transition-shadow hover:ring-2 hover:ring-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                      :class="isSelected(day.key, shift.key) ? 'ring-2 ring-primary' : ''"
                      :aria-label="cellLabel(day.key, shift.key)"
                      @click="() => { selectCell(day.key, shift.key) }"
                    >
                      <ClassroomsUsedMinutesRing :percent="cellFor(day.key, shift.key)?.usedMinutesRate ?? 0" class="size-8" :class="cellRingClass(day.key, shift.key)" />
                      <ClassroomsUsedCapacityBlocks :percent="cellFor(day.key, shift.key)?.usedCapacityRate ?? 0" class="size-8" />
                    </button>
                  </template>
                </template>
              </div>
            </div>
          </section>

          <!-- Drilldown inline (micro) — sem sala não há o que detalhar -->
          <section v-if="selectedCell?.open && selectedCell.classrooms.length" class="flex flex-col gap-4">
            <div class="flex flex-col items-center gap-4">
              <div class="flex w-full items-center gap-4">
                <span class="flex-1 border-t border-dashed border-accented" />
                <h2 class="font-semibold text-highlighted">
                  {{ dayLabels[selectedCell.day] }} · {{ shiftLabels[selectedCell.shift] }}
                </h2>
                <span class="flex-1 border-t border-dashed border-accented" />
              </div>

              <div class="flex items-center gap-8 sm:gap-10">
                <div class="flex items-center gap-3">
                  <ClassroomsUsedMinutesRing
                    :percent="selectedCell.usedMinutesRate"
                    class="size-12"
                    :class="selectedCell.usedMinutesRate > 0 ? 'text-primary' : 'text-dimmed'"
                  />
                  <div class="flex flex-col leading-none">
                    <span class="text-2xl font-bold tabular-nums text-primary">{{ formatRate(selectedCell.usedMinutesRate) }}</span>
                    <span class="mt-1.5 text-xs text-muted">Tempo usado</span>
                  </div>
                </div>

                <div class="flex items-center gap-3">
                  <ClassroomsUsedCapacityBlocks :percent="selectedCell.usedCapacityRate" class="size-12" />
                  <div class="flex flex-col leading-none">
                    <span class="text-2xl font-bold tabular-nums text-primary">{{ formatRate(selectedCell.usedCapacityRate) }}</span>
                    <span class="mt-1.5 text-xs text-muted">Espaço alocado</span>
                  </div>
                </div>
              </div>
            </div>

            <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
              <NuxtLink
                v-for="room in selectedClassrooms"
                :key="room.id"
                :to="`/classrooms/${room.id}`"
                class="flex flex-col gap-4 rounded-lg border border-default bg-default/60 p-4 transition-shadow hover:ring-2 hover:ring-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
              >
                <span class="truncate text-sm font-medium text-highlighted">{{ room.name }}</span>

                <ClassroomsUsedMinutesRingStat
                  :percent="room.usedMinutesRate"
                  :title="`${formatRate(room.usedMinutesRate)} de tempo usado`"
                  :subtitle="`${formatMinutes(room.usedMinutes)} de ${formatMinutes(room.availableMinutes)}`"
                  :color-class="room.usedMinutesRate > 0 ? 'text-primary' : 'text-dimmed'"
                />

                <div class="flex items-center gap-3">
                  <ClassroomsUsedCapacityBlocks :percent="room.usedCapacityRate" class="size-14" />

                  <div class="flex min-w-0 flex-col">
                    <span class="text-sm font-medium text-highlighted tabular-nums">
                      {{ formatRate(room.usedCapacityRate) }} de espaço alocado
                    </span>
                    <span class="text-xs text-muted tabular-nums">
                      {{ formatStudents(room.averageStudents) }}
                    </span>
                  </div>
                </div>
              </NuxtLink>
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
            <div class="flex shrink-0 items-center gap-2">
              <template v-if="editingHours">
                <UButton
                  label="Cancelar"
                  color="neutral"
                  variant="subtle"
                  :disabled="savingHours"
                  @click="() => { cancelEditingHours() }"
                />
                <UButton
                  label="Salvar"
                  :loading="savingHours"
                  :disabled="!hoursDirty"
                  @click="() => { saveOpeningHours() }"
                />
              </template>
              <UButton
                v-else
                icon="i-lucide-pencil"
                label="Editar"
                color="neutral"
                variant="subtle"
                @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); startEditingHours() }"
              />
            </div>
          </div>

          <CampiOpeningHoursWeek
            :days="shownHours"
            :editing="editingHours"
            @update:days="(days) => { draftHours = days }"
          />

          <p v-if="closedAllWeek" class="flex items-start gap-2 text-xs text-muted">
            <UIcon name="i-lucide-triangle-alert" class="size-4 shrink-0 text-warning" />
            <span>Com todos os dias fechados o campus fica sem horário nenhum, e o mapa de ocupação some.</span>
          </p>
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

          <TableEmptyState
            v-if="!classrooms.length"
            :loading="false"
            icon="i-lucide-door-open"
            message="Nenhuma sala neste campus"
            button-label="Sala"
            @create="() => { createModalOpen = true }"
          />

          <!-- Mesmo card do drilldown do mapa, só que com a semana inteira no
               lugar de um turno: as duas leituras da sala e, embaixo da taxa de
               assentos, quantos lugares isso dá de verdade. -->
          <div v-else class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
            <NuxtLink
              v-for="{ room, occupancy: week } in classroomCards"
              :key="room.id"
              :to="`/classrooms/${room.id}`"
              class="flex flex-col gap-4 rounded-lg border border-default bg-default/60 p-4 transition-shadow hover:ring-2 hover:ring-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              <div class="flex min-w-0 flex-col">
                <span class="truncate text-sm font-medium text-highlighted">{{ room.name }}</span>
                <span class="text-xs text-muted tabular-nums">
                  {{ room.capacity.toLocaleString('pt-BR') }} {{ room.capacity === 1 ? 'lugar' : 'lugares' }}
                </span>
              </div>

              <template v-if="week">
                <ClassroomsUsedMinutesRingStat
                  :percent="week.usedMinutesRate"
                  :title="`${formatRate(week.usedMinutesRate)} de tempo usado`"
                  :subtitle="`${formatMinutes(week.usedMinutes)} de ${formatMinutes(week.availableMinutes)}`"
                  :color-class="week.usedMinutesRate > 0 ? 'text-primary' : 'text-dimmed'"
                />

                <div class="flex items-center gap-3">
                  <ClassroomsUsedCapacityBlocks :percent="week.usedCapacityRate" class="size-14" />

                  <div class="flex min-w-0 flex-col">
                    <span class="text-sm font-medium text-highlighted tabular-nums">
                      {{ formatRate(week.usedCapacityRate) }} de espaço alocado
                    </span>
                    <span class="text-xs text-muted tabular-nums">
                      {{ formatStudents(week.averageStudents) }}
                    </span>
                  </div>
                </div>
              </template>

              <p v-else class="text-xs text-muted">
                Ocupação indisponível
              </p>
            </NuxtLink>
          </div>
        </section>
      </div>
    </template>
  </UDashboardPanel>

  <CampiEditModal v-model:open="campusEditModalOpen" :campus="campus" @updated="refreshCampi()" />

  <ClassroomsCreateModal v-model:open="createModalOpen" :campus-id="campusId" @created="onClassroomsChanged()" />
</template>
