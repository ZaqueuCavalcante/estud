<script setup lang="ts">
import type { OpeningHoursDay, OpeningHoursWindow } from '~/types/campi'

const props = defineProps<{ days: OpeningHoursDay[] }>()

// ── Layout do grid ────────────────────────────────────────────────────────────
// Menor que os 64px da agenda: aqui as faixas são longas (um campus padrão abre
// 15h por dia), e 64px/hora jogaria o grid pra quase mil pixels de altura.
const HOUR_HEIGHT = 44
const BLOCK_GAP = 3

const windowsByDay = computed(() => {
  const map = new Map<string, OpeningHoursWindow[]>()
  for (const day of props.days) map.set(day.day, day.windows)
  return map
})

function windowsOf(dayKey: string) {
  return windowsByDay.value.get(dayKey) ?? []
}

function isClosed(dayKey: string) {
  return windowsOf(dayKey).length === 0
}

// Ao contrário da agenda, o sábado aparece sempre: "Sáb / Fechado" é justamente
// a informação que a tela existe pra dar.
const visibleDays = campusWeekDays

const gridColumns = computed(() => `56px repeat(${visibleDays.length}, 1fr)`)
const dayColumns = computed(() => `repeat(${visibleDays.length}, 1fr)`)

// Faixa de horas exibida: do início mais cedo ao fim mais tarde da semana.
// Campus fechado a semana inteira não tem faixa, então cai no padrão 07h–22h.
const range = computed(() => {
  let min = Infinity
  let max = -Infinity
  for (const day of props.days) {
    for (const interval of day.windows) {
      min = Math.min(min, openingHourToMinutes(interval.start))
      max = Math.max(max, openingHourToMinutes(interval.end))
    }
  }
  if (!isFinite(min) || !isFinite(max)) {
    min = 7 * 60
    max = 22 * 60
  }
  return { startHour: Math.floor(min / 60), endHour: Math.ceil(max / 60) }
})

const hourLabels = computed(() => {
  const labels: string[] = []
  for (let h = range.value.startHour; h <= range.value.endHour; h++) {
    labels.push(`${h.toString().padStart(2, '0')}:00`)
  }
  return labels
})

const gridHeight = computed(() => (range.value.endHour - range.value.startHour) * HOUR_HEIGHT)

function blockStyle(interval: OpeningHoursWindow) {
  const startMin = openingHourToMinutes(interval.start)
  const endMin = openingHourToMinutes(interval.end)
  const top = ((startMin - range.value.startHour * 60) / 60) * HOUR_HEIGHT
  const height = Math.max(((endMin - startMin) / 60) * HOUR_HEIGHT, 24)
  return {
    top: `${top + BLOCK_GAP}px`,
    height: `${height - BLOCK_GAP * 2}px`,
  }
}

// Bloco curto não comporta duas linhas de texto sem estourar.
function isCompact(interval: OpeningHoursWindow) {
  const minutes = openingHourToMinutes(interval.end) - openingHourToMinutes(interval.start)
  return (minutes / 60) * HOUR_HEIGHT < 52
}

function formatDuration(interval: OpeningHoursWindow) {
  const minutes = openingHourToMinutes(interval.end) - openingHourToMinutes(interval.start)
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (h === 0) return `${m}min`
  if (m === 0) return `${h}h`
  return `${h}h ${m}min`
}
</script>

<template>
  <div class="shrink-0 overflow-x-auto">
    <div class="min-w-180 pb-3">
      <!-- Cabeçalho: dias da semana -->
      <div class="grid" :style="{ gridTemplateColumns: gridColumns }">
        <div />
        <div
          v-for="day in visibleDays"
          :key="day.key"
          class="flex flex-col items-center gap-0.5 px-2 py-2"
        >
          <!-- No mobile a coluna é estreita demais pro nome inteiro; a partir do
               md sobra largura e "Segunda" lê melhor que "Seg". -->
          <span class="text-sm font-semibold text-highlighted">
            <span class="md:hidden">{{ day.short }}</span>
            <span class="hidden md:inline">{{ day.label }}</span>
          </span>
        </div>
      </div>

      <!-- Corpo: eixo de horas + grade dos dias -->
      <div class="grid" :style="{ gridTemplateColumns: '56px 1fr' }">
        <!-- Eixo de horas -->
        <div class="relative" :style="{ height: `${gridHeight}px` }">
          <div
            v-for="(label, i) in hourLabels"
            :key="label"
            class="absolute right-2 -translate-y-1/2 text-xs text-muted tabular-nums"
            :style="{ top: `${i * HOUR_HEIGHT}px` }"
          >
            {{ label }}
          </div>
        </div>

        <!-- Grade dos dias (moldura com quinas arredondadas) -->
        <div
          class="relative grid overflow-hidden rounded-lg border border-default"
          :style="{ gridTemplateColumns: dayColumns }"
        >
          <div
            v-for="(day, dayIdx) in visibleDays"
            :key="day.key"
            class="relative"
            :class="[
              dayIdx > 0 ? 'border-l border-default' : '',
              // Dia fechado ganha hachura, o mesmo tratamento da célula fechada
              // no mapa de ocupação — os dois falam da mesma configuração.
              isClosed(day.key)
                ? 'bg-elevated/40 [background-image:repeating-linear-gradient(45deg,transparent,transparent_5px,var(--ui-border)_5px,var(--ui-border)_6px)]'
                : '',
            ]"
            :style="{ height: `${gridHeight}px` }"
          >
            <!-- Linhas de grade por hora -->
            <div
              v-for="i in (range.endHour - range.startHour - 1)"
              :key="i"
              class="absolute inset-x-0 border-t border-default/60"
              :style="{ top: `${i * HOUR_HEIGHT}px` }"
            />

            <span
              v-if="isClosed(day.key)"
              class="absolute inset-0 flex items-center justify-center text-xs font-medium text-dimmed"
            >
              Fechado
            </span>

            <!-- Faixas de funcionamento. Cor única, e não a paleta por disciplina
                 da agenda: aqui todos os blocos são a mesma coisa. -->
            <div
              v-for="(interval, idx) in windowsOf(day.key)"
              :key="`${interval.start}-${interval.end}-${idx}`"
              class="absolute inset-x-1 overflow-hidden rounded-md bg-primary/15 px-2 pb-1 pt-2 text-center shadow-sm"
              :style="blockStyle(interval)"
            >
              <div class="text-[11px] font-semibold leading-tight tabular-nums text-highlighted">
                {{ formatOpeningHour(interval.start) }} – {{ formatOpeningHour(interval.end) }}
              </div>
              <div v-if="!isCompact(interval)" class="text-[11px] leading-tight text-muted">
                {{ formatDuration(interval) }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
