<script setup lang="ts">
// Cópia consciente da aba Ocupação de `app/pages/campi/[id].vue`. Aquela tela
// vive dentro do dashboard autenticado; esta roda na landing pública, com dados
// de mock e sem link nenhum pra dentro do app. Mudou o painel lá, vale conferir
// se o desenho daqui ainda bate.
import occupancyMock from '~/mocks/campus-occupancy.json'
import type { CampusOccupancyCell, GetCampusOccupancyOut } from '~/types/campi'

const data = occupancyMock as GetCampusOccupancyOut

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
const dayShort: Record<string, string> = Object.fromEntries(campusWeekDays.map(d => [d.key, d.short]))

function cellFor(dayKey: string, shiftKey: string): CampusOccupancyCell | undefined {
  return data.cells.find(c => c.day === dayKey && c.shift === shiftKey)
}

function isClosed(dayKey: string, shiftKey: string) {
  const cell = cellFor(dayKey, shiftKey)
  return cell !== undefined && !cell.open
}

const visibleDays = campusWeekDays.filter(day => data.cells.some(c => c.day === day.key && c.open))
const visibleShifts = shifts.filter(shift => data.cells.some(c => c.shift === shift.key && c.open))

const dayColumns = `auto repeat(${visibleDays.length}, minmax(0, 1fr))`

const peakCell = data.cells.filter(c => c.open)
  .reduce<CampusOccupancyCell | null>((a, b) => (a === null || b.usedCapacity > a.usedCapacity ? b : a), null)

const peakStudents = peakCell && peakCell.openMinutes > 0
  ? Math.round(peakCell.usedCapacity / peakCell.openMinutes)
  : 0

function formatMinutes(minutes: number): string {
  if (minutes <= 0) return '0min'
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (h === 0) return `${m}min`
  if (m === 0) return `${h}h`
  return `${h}h ${m}min`
}

function formatStudents(students: number): string {
  const rounded = students > 0 ? Math.max(Math.round(students), 1) : 0
  return `${rounded.toLocaleString('pt-BR')} ${rounded === 1 ? 'aluno' : 'alunos'} em média`
}

function cellRingClass(day: string, shift: string): string {
  return (cellFor(day, shift)?.usedMinutesRate ?? 0) > 0 ? 'text-primary' : 'text-dimmed'
}

function cellLabel(day: string, shift: string): string {
  const cell = cellFor(day, shift)
  const label = `${dayLabels[day]} ${shiftLabels[shift]}`
  return `${label}: ${formatRate(cell?.usedMinutesRate ?? 0)} de tempo usado, ${formatRate(cell?.usedCapacityRate ?? 0)} de espaço alocado`
}

// A célula que abre a demo é a mesma dos screenshots que este componente
// substituiu — quarta à tarde, com o campus em meia carga.
const selected = ref({ day: 'Wednesday', shift: 'Afternoon' })

function selectCell(day: string, shift: string) {
  if (isClosed(day, shift)) return
  selected.value = { day, shift }
}
function isSelected(day: string, shift: string) {
  return selected.value.day === day && selected.value.shift === shift
}

const selectedCell = computed(() => cellFor(selected.value.day, selected.value.shift))
const selectedClassrooms = computed(() => selectedCell.value?.classrooms ?? [])
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
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
        <span class="text-sm font-semibold text-highlighted">
          {{ peakCell ? `${dayShort[peakCell.day]} · ${shiftLabels[peakCell.shift]}` : 'Sem alocação' }}
        </span>
        <span class="mt-0.5 text-xl font-bold tabular-nums leading-none text-primary">~{{ peakStudents.toLocaleString('pt-BR') }} <span class="text-base font-medium text-muted">{{ peakStudents === 1 ? 'aluno' : 'alunos' }}</span></span>
        <span class="mt-2 text-xs text-muted">Horário de pico</span>
      </div>

      <div class="flex flex-col justify-center rounded-xl border border-default bg-elevated/40 px-4 py-4">
        <span class="text-2xl font-bold tabular-nums leading-none text-highlighted">{{ data.totalClassrooms }}</span>
        <span class="mt-2 text-xs text-muted">Salas no campus</span>
      </div>
    </div>

    <section class="flex flex-col gap-4">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <h3 class="flex items-center gap-2 font-semibold text-highlighted">
          <UIcon name="i-lucide-table-2" class="size-5 text-primary" />
          Mapa de ocupação
        </h3>

        <div class="ml-auto flex items-center gap-4 text-xs text-muted">
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

      <!-- p-1/-m-1: o ring de seleção é desenhado fora da caixa da célula. -->
      <div class="overflow-x-auto p-1 -m-1">
        <div class="grid min-w-160 gap-2" :style="{ gridTemplateColumns: dayColumns }">
          <div />
          <div
            v-for="day in visibleDays"
            :key="day.key"
            class="pb-1 text-center"
          >
            <span class="text-sm font-semibold text-highlighted">{{ day.label }}</span>
          </div>

          <template v-for="shift in visibleShifts" :key="shift.key">
            <div class="flex flex-col justify-center pr-3 text-right">
              <span class="text-sm font-medium text-highlighted">{{ shift.label }}</span>
              <span class="text-[11px] text-muted tabular-nums">{{ shift.window }}</span>
            </div>

            <button
              v-for="day in visibleDays"
              :key="`${day.key}-${shift.key}`"
              type="button"
              class="flex h-16 items-center justify-center gap-6 rounded-lg border border-default bg-elevated/40 transition-shadow hover:ring-2 hover:ring-primary/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
              :class="isSelected(day.key, shift.key) ? 'ring-2 ring-primary' : ''"
              :aria-label="cellLabel(day.key, shift.key)"
              @click="() => { selectCell(day.key, shift.key) }"
            >
              <ClassroomsUsedMinutesRing
                :percent="cellFor(day.key, shift.key)?.usedMinutesRate ?? 0"
                class="size-8"
                :class="cellRingClass(day.key, shift.key)"
              />
              <ClassroomsUsedCapacityBlocks :percent="cellFor(day.key, shift.key)?.usedCapacityRate ?? 0" class="size-8" />
            </button>
          </template>
        </div>
      </div>
    </section>

    <section v-if="selectedCell" class="flex flex-col gap-4">
      <div class="flex flex-col items-center gap-4">
        <div class="flex w-full items-center gap-4">
          <span class="flex-1 border-t border-dashed border-accented" />
          <h3 class="font-semibold text-highlighted">
            {{ dayLabels[selectedCell.day] }} · {{ shiftLabels[selectedCell.shift] }}
          </h3>
          <span class="flex-1 border-t border-dashed border-accented" />
        </div>

        <div class="flex items-center gap-10">
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

      <div class="grid grid-cols-2 gap-3 lg:grid-cols-3">
        <div
          v-for="room in selectedClassrooms"
          :key="room.id"
          class="flex flex-col gap-4 rounded-lg border border-default bg-default/60 p-4"
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
        </div>
      </div>
    </section>
  </div>
</template>
